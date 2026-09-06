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
        if (_save.ActiveRunReadError is { } readError)
        {
            LastActiveRunLoadDiagnostic = new(ActiveRunLoadFailureKind.ReadFailed, readError);
            return null;
        }
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
        if (requiresPublication && loaded.PendingNode && RunDecisionService.KindFor(loaded.SelectedNode) is not null)
        {
            // Before v6, applying a noncombat reward and consuming its node were
            // separate saves. PendingNode cannot prove whether its reward was
            // already granted; recreating an offer would guess that entitlement.
            LastActiveRunLoadDiagnostic = new(ActiveRunLoadFailureKind.MigrationRejected,
                "旧征程停在非战斗节点，旧格式无法证明收益是否已领取，不能安全重建选择资格；原存档已保留，未重复发奖或重置进度。");
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
        var working = CloneRun(run);
        working.FloorIndex++;
        working.PendingNode = false;
        if (ValidateRun(working)) TryPublish(working, run);
    }

    public bool TryCompleteTerminal(ActiveRunDto run)
    {
        if (string.IsNullOrWhiteSpace(run.TerminalCompletionId)) return false;
        if (run.TerminalVictory && !Meta.AppliedRunCompletionIds.Contains(run.TerminalCompletionId))
        {
            if (Meta.Victories == int.MaxValue) return false;
            var next = new MetaProgressDto
            {
                Version = Meta.Version, Victories = checked(Meta.Victories + 1),
                HighestRegion = _project.Campaign.Regions.Length,
                UnlockedHeroIds = [.. Meta.UnlockedHeroIds],
                AppliedRunCompletionIds = [.. Meta.AppliedRunCompletionIds, run.TerminalCompletionId]
            };
            var locked = _content.Catalog.Heroes.FirstOrDefault(entry => !next.UnlockedHeroIds.Contains(entry.StableId));
            if (locked is not null) next.UnlockedHeroIds.Add(locked.StableId);
            try { if (!_save.SaveMeta(next)) return false; }
            catch { return false; }
            Meta.Victories = next.Victories;
            Meta.HighestRegion = next.HighestRegion;
            Meta.UnlockedHeroIds = next.UnlockedHeroIds;
            Meta.AppliedRunCompletionIds = next.AppliedRunCompletionIds;
        }
        try { _save.DeleteActiveRun(); return true; }
        catch { return false; }
    }

    public void EndRun() => _save.DeleteActiveRun();

    public ActiveRunDto CloneRun(ActiveRunDto source) => Clone(source);

    public bool ValidateRun(ActiveRunDto run)
        => run is not null && RunFormationPolicy.Validate(run, _rules) &&
           ActiveRunConfigurationValidator.Validate(run, _content, _project);

    private void EnsureMetaDefaults()
    {
        if (Meta.UnlockedHeroIds is null || Meta.AppliedRunCompletionIds is null || Meta.Victories < 0 || Meta.HighestRegion < 0 ||
            Meta.AppliedRunCompletionIds.Any(string.IsNullOrWhiteSpace) || Meta.AppliedRunCompletionIds.Distinct().Count() != Meta.AppliedRunCompletionIds.Count)
            throw new InvalidOperationException("Meta 存档结构无效，拒绝写回以保护原文件。");
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
        // The authoritative Run has already committed. A secondary high-water
        // mark must never turn that success into a reported transaction failure.
        try { _save.SaveMeta(Meta); }
        catch (Exception exception) { Godot.GD.PushWarning("征程已保存，历史最高区域暂未同步：" + exception.Message); }
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
        EquipmentInventory = source.EquipmentInventory is null ? null! : source.EquipmentInventory
            .Select(item => item is null ? null! : new EquipmentInstanceState
            {
                InstanceId = item.InstanceId,
                ContentId = item.ContentId,
                OwnerHeroInstanceId = item.OwnerHeroInstanceId,
                SlotIndex = item.SlotIndex
            }).ToList(),
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
        PendingOffer = source.PendingOffer,
        TerminalCompletionId = source.TerminalCompletionId,
        TerminalVictory = source.TerminalVictory,
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
        target.EquipmentInventory = copy.EquipmentInventory;
        target.EquippedTacticalCommandIds = copy.EquippedTacticalCommandIds;
        target.Gold = copy.Gold;
        target.FloorIndex = copy.FloorIndex;
        target.BattleNumber = copy.BattleNumber;
        target.PendingNode = copy.PendingNode;
        target.SelectedNode = copy.SelectedNode;
        target.PendingOffer = copy.PendingOffer;
        target.TerminalCompletionId = copy.TerminalCompletionId;
        target.TerminalVictory = copy.TerminalVictory;
        target.LegacyHeroId = copy.LegacyHeroId;
        target.LegacyHeroHealthRatio = copy.LegacyHeroHealthRatio;
        target.LegacyHeroCell = copy.LegacyHeroCell;
        target.LegacyDeploymentCells = copy.LegacyDeploymentCells;
    }
}
