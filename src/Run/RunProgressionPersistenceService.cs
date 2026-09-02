using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;

namespace TowerAutobattler.Run;

// Owns versioned Run validation, transactional publication, and Meta/Settings persistence.
// Other Run services mutate working projections and publish them through this boundary.
public sealed class RunProgressionPersistenceService : IRunFormationPersistence, IRunEquipmentPersistence
{
    private readonly ContentRegistry _content;
    private readonly IRunSaveService _save;
    private readonly CompiledGameProject _project;
    private readonly CompiledRunRules _rules;

    public RunProgressionPersistenceService(
        ContentRegistry content,
        IRunSaveService save,
        CompiledGameProject project)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _rules = project.RunRules;
        Meta = save.LoadMeta();
        Settings = save.LoadSettings();
        EnsureMetaDefaults();
    }

    public MetaProgressDto Meta { get; }
    public SettingsDto Settings { get; }
    public ActiveRunLoadDiagnostic? LastActiveRunLoadDiagnostic { get; private set; }

    public ActiveRunDto? LoadActiveRun()
    {
        LastActiveRunLoadDiagnostic = null;
        var stored = _save.LoadActiveRun();
        if (stored is null) return null;
        // Migration and validation always operate on a detached, shape-preserving
        // copy so rejected or unpublished migrations cannot alter the stored v2/v3 object.
        var loaded = CloneUntrusted(stored);
        var requiresPublication = loaded.Version != ActiveRunFormationSchema.CurrentVersion;
        if (!ActiveRunFormationSchema.TryMigrateToCurrent(loaded, _rules))
        {
            LastActiveRunLoadDiagnostic = new ActiveRunLoadDiagnostic(
                requiresPublication
                    ? ActiveRunLoadFailureKind.MigrationRejected
                    : ActiveRunLoadFailureKind.ValidationRejected,
                requiresPublication
                    ? "活动征程无法无损迁移，已拒绝载入；Meta 与设置保持不变。"
                    : "当前活动征程含旧 schema 残留或非法结构，已拒绝载入；Meta 与设置保持不变。");
            return null;
        }
        if (!ValidateRun(loaded))
        {
            LastActiveRunLoadDiagnostic = new ActiveRunLoadDiagnostic(
                requiresPublication
                    ? ActiveRunLoadFailureKind.MigrationRejected
                    : ActiveRunLoadFailureKind.ValidationRejected,
                requiresPublication
                    ? "活动征程迁移后无法通过当前内容与人口/阵型校验，已拒绝载入；Meta 与设置保持不变。"
                    : "活动征程未通过当前内容与人口/阵型校验，已拒绝载入；Meta 与设置保持不变。");
            return null;
        }
        if (requiresPublication && !_save.SaveActiveRun(loaded))
        {
            LastActiveRunLoadDiagnostic = new ActiveRunLoadDiagnostic(
                ActiveRunLoadFailureKind.MigrationPublicationFailed,
                "活动征程迁移结果无法安全写回，已拒绝载入；旧存档、Meta 与设置保持不变。");
            return null;
        }
        return loaded;
    }

    public bool SaveActiveRun(ActiveRunDto run) => _save.SaveActiveRun(run);

    public void DeleteActiveRun() => _save.DeleteActiveRun();

    public void SaveSettings() => _save.SaveSettings(Settings);

    public bool TryCommitFormation(ActiveRunDto run, Action mutation)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(mutation);
        var snapshot = FormationSnapshot.Capture(run);
        mutation();
        if (_save.SaveActiveRun(run)) return true;
        snapshot.Restore(run);
        return false;
    }

    public bool TryPublish(ActiveRunDto working, ActiveRunDto authoritative)
    {
        if (!_save.SaveActiveRun(working)) return false;
        CopyRun(working, authoritative);
        UpdateHighestRegion(authoritative);
        return true;
    }

    public void AdvanceFloor(ActiveRunDto run)
    {
        run.FloorIndex++;
        run.PendingNode = false;
        UpdateHighestRegion(run);
        _save.SaveActiveRun(run);
    }

    public void CompleteFinalVictory()
    {
        Meta.Victories++;
        Meta.HighestRegion = _project.Campaign.Regions.Length;
        UnlockNextHero();
        _save.SaveMeta(Meta);
        _save.DeleteActiveRun();
    }

    public void EndRun() => _save.DeleteActiveRun();

    public ActiveRunDto CloneRun(ActiveRunDto source) => Clone(source);

    public bool ValidateRun(ActiveRunDto run)
        => run is not null && RunFormationPolicy.Validate(run, _rules) &&
           ActiveRunConfigurationValidator.Validate(run, _content, _project);

    private void EnsureMetaDefaults()
    {
        Meta.UnlockedHeroIds.RemoveAll(id => !_content.TryGet(id, out var entry) ||
            entry.Definition is not UnitDefinition { IsHero: true });
        if (Meta.UnlockedHeroIds.Count == 0)
            Meta.UnlockedHeroIds.AddRange(_content.Catalog.Heroes
                .Take(_rules.InitialUnlockedHeroCount)
                .Select(entry => entry.StableId));
        _save.SaveMeta(Meta);
    }

    private void UpdateHighestRegion(ActiveRunDto run)
    {
        Meta.HighestRegion = Math.Max(Meta.HighestRegion, Math.Min(
            _project.Campaign.Regions.Length,
            run.FloorIndex / _project.Campaign.FloorsPerRegion + 1));
        _save.SaveMeta(Meta);
    }

    private void UnlockNextHero()
    {
        var locked = _content.Catalog.Heroes.FirstOrDefault(entry =>
            !Meta.UnlockedHeroIds.Contains(entry.StableId));
        if (locked is not null) Meta.UnlockedHeroIds.Add(locked.StableId);
    }

    private sealed record FormationSnapshot(List<string> Deployment)
    {
        public static FormationSnapshot Capture(ActiveRunDto run) => new([.. run.Deployment]);

        public void Restore(ActiveRunDto run) => run.Deployment = [.. Deployment];
    }

    private static ActiveRunDto Clone(ActiveRunDto source) => CloneUntrusted(source);

    private static ActiveRunDto CloneUntrusted(ActiveRunDto source) => new()
    {
        Version = source.Version,
        Seed = source.Seed,
        Roster = source.Roster is null
            ? null!
            : source.Roster.Select(unit => unit is null
                ? null!
                : new RosterHeroInstanceDto
                {
                    InstanceId = unit.InstanceId,
                    ContentId = unit.ContentId,
                    HealthRatio = unit.HealthRatio,
                    Rank = unit.Rank,
                    Equipment = unit.Equipment is null
                        ? null!
                        : unit.Equipment.Select(item => item is null
                            ? null!
                            : new EquipmentInstanceState
                            {
                                InstanceId = item.InstanceId,
                                ContentId = item.ContentId,
                                OwnerHeroInstanceId = item.OwnerHeroInstanceId,
                                SlotIndex = item.SlotIndex
                            }).ToList()
                }).ToList(),
        CurrentPopulation = source.CurrentPopulation,
        PopulationCapSources = source.PopulationCapSources is null
            ? null!
            : source.PopulationCapSources.Select(cap => cap is null
                ? null!
                : new PopulationCapSourceDto
                {
                    SourceId = cap.SourceId,
                    Amount = cap.Amount
                }).ToList(),
        Deployment = source.Deployment is null ? null! : source.Deployment.ToList(),
        Items = source.Items is null
            ? null!
            : source.Items.Select(item => item is null
                ? null!
                : new ItemInstanceDto
                {
                    InstanceId = item.InstanceId,
                    ContentId = item.ContentId,
                    Stacks = item.Stacks,
                    Charges = item.Charges,
                    Roll = item.Roll,
                    Counters = item.Counters is null
                        ? null!
                        : item.Counters.Select(counter => counter is null
                            ? null!
                            : new RelicCounterStateDto
                            {
                                CounterId = counter.CounterId,
                                Value = counter.Value
                            }).ToList()
                }).ToList(),
        EquippedTacticalCommandIds = source.EquippedTacticalCommandIds is null
            ? null!
            : source.EquippedTacticalCommandIds.ToList(),
        Gold = source.Gold,
        FloorIndex = source.FloorIndex,
        BattleNumber = source.BattleNumber,
        PendingNode = source.PendingNode,
        SelectedNode = source.SelectedNode,
        LegacyHeroId = source.LegacyHeroId,
        LegacyHeroHealthRatio = source.LegacyHeroHealthRatio,
        LegacyHeroCell = source.LegacyHeroCell?.Clone(),
        LegacyDeploymentCells = source.LegacyDeploymentCells?.Select(cell => cell is null
            ? null!
            : cell.Clone()).ToList()
    };

    private static void CopyRun(ActiveRunDto source, ActiveRunDto target)
    {
        var copy = Clone(source);
        target.Version = copy.Version;
        target.Seed = copy.Seed;
        target.Roster = copy.Roster;
        target.CurrentPopulation = copy.CurrentPopulation;
        target.PopulationCapSources = copy.PopulationCapSources;
        target.Deployment = copy.Deployment;
        target.Items = copy.Items;
        target.EquippedTacticalCommandIds = copy.EquippedTacticalCommandIds;
        target.Gold = copy.Gold;
        target.FloorIndex = copy.FloorIndex;
        target.BattleNumber = copy.BattleNumber;
        target.PendingNode = copy.PendingNode;
        target.SelectedNode = copy.SelectedNode;
        target.LegacyHeroId = copy.LegacyHeroId;
        target.LegacyHeroHealthRatio = copy.LegacyHeroHealthRatio;
        target.LegacyHeroCell = copy.LegacyHeroCell;
        target.LegacyDeploymentCells = copy.LegacyDeploymentCells;
    }
}
