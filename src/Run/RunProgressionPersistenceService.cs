using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;
using TowerAutobattler.Growth;
using TowerAutobattler.Composition;
using TowerAutobattler.Expedition;

namespace TowerAutobattler.Run;

// Owns versioned Run validation, transactional publication, and Meta/Settings persistence.
// Other Run services mutate working projections and publish them through this boundary.
public sealed class RunProgressionPersistenceService : IRunFormationPersistence, IRunEquipmentPersistence
{
    private readonly ContentRegistry _content;
    private readonly IRunSaveService _save;
    private readonly CompiledGameProject _project;
    private readonly CompiledRunRules _rules;
    private readonly CompiledGrowthRules? _growthRules;
    private readonly CompiledRunProfile? _profile;
    private readonly bool _initializeMeta;

    public RunProgressionPersistenceService(
        ContentRegistry content,
        IRunSaveService save,
        CompiledGameProject project,
        CompiledGrowthRules? growthRules = null,
        CompiledRunProfile? profile = null,
        bool initializeMeta = true)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _rules = project.RunRules;
        _growthRules = growthRules;
        _profile = profile;
        _initializeMeta = initializeMeta;
        if (profile is not null && (!ReferenceEquals(profile.Project, project) || !ReferenceEquals(profile.GrowthRules, growthRules)))
            throw new ArgumentException("Persistence services must use the selected profile's complete configuration.", nameof(profile));
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
        if (stored is null)
        {
            if (_initializeMeta) _save.SaveMeta(Meta);
            return null;
        }
        // Migration and validation always operate on a detached, shape-preserving
        // copy so rejected or unpublished migrations cannot alter the stored v2/v3 object.
        ActiveRunDto loaded;
        try { loaded = CloneUntrusted(stored); }
        catch (Exception exception) when (exception is ArgumentException or NullReferenceException or InvalidOperationException)
        {
            LastActiveRunLoadDiagnostic = new(ActiveRunLoadFailureKind.ValidationRejected,
                "活动征程结构不完整，原存档已保留：" + exception.Message);
            return null;
        }
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
        if (requiresPublication) InitHeader(loaded);
        if (stored.Version == 7 && _growthRules is not null && loaded.Growth is null)
        {
            loaded.Growth = new GrowthRunDto { RulesId = _growthRules.StableId };
            if (loaded.PendingNode && RunDecisionService.KindFor(loaded.SelectedNode) is not null)
                loaded.Growth.PendingNode = new GrowthNodeSnapshotDto
                {
                    FloorIndex = loaded.FloorIndex,
                    BattleNumber = loaded.BattleNumber,
                    IsBattle = false,
                    ParticipantInstanceIds = [],
                    Assignments = []
                };
        }
        if (stored.Version < 6 && loaded.PendingNode && RunDecisionService.KindFor(loaded.SelectedNode) is not null)
        {
            // Before v6, applying a noncombat reward and consuming its node were
            // separate saves. PendingNode cannot prove whether its reward was
            // already granted; recreating an offer would guess that entitlement.
            LastActiveRunLoadDiagnostic = new(ActiveRunLoadFailureKind.MigrationRejected,
                "旧征程停在非战斗节点，旧格式无法证明收益是否已领取，不能安全重建选择资格；原存档已保留，未重复发奖或重置进度。");
            return null;
        }
        if (_project.Campaign.Progression is CompiledTowerProgression && stored.Version < 9 && loaded.Growth is not null && loaded.PendingNode &&
            loaded.SelectedNode is TowerNodeType.Combat or TowerNodeType.Elite &&
            new TowerGenerator(_project.Campaign).OpponentOptions(loaded, loaded.SelectedNode).Count > 0)
            loaded.LegacyPendingOpponent = true;
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
        if (_initializeMeta) _save.SaveMeta(Meta);
        return loaded;
    }

    public bool SaveActiveRun(ActiveRunDto run) => _save.SaveActiveRun(run);

    public void InitHeader(ActiveRunDto run)
    {
        if (_project.Campaign.Progression is CompiledExpedition) run.Tower = null;
        ArgumentNullException.ThrowIfNull(run);
        if (_profile is not null)
        {
            run.ProfileId = _profile.ProfileId;
            run.ContentFingerprint = _profile.ContentFingerprint;
            run.ProfileCompatibilityVersion = _profile.CompatibilityVersion;
            run.ProgressionProtocolVersion = _profile.ProgressionProtocolVersion;
            run.RewardProtocolVersion = _profile.RewardProtocolVersion;
            run.IdentityProtocolVersion = _profile.IdentityProtocolVersion;
        }
        if (string.IsNullOrEmpty(run.RunId)) run.RunId = Guid.NewGuid().ToString("N");
    }

    public void DeleteActiveRun() => _save.DeleteActiveRun();

    public void SaveSettings() => _save.SaveSettings(Settings);

    public bool TryCommitFormation(ActiveRunDto run, Action mutation)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(mutation);
        var snapshot = FormationSnapshot.Capture(run);
        mutation();
        var revision = run.Revision;
        if (revision < long.MaxValue && ValidateRun(run))
        {
            run.Revision = checked(revision + 1);
            try { if (_save.SaveActiveRun(run)) return true; }
            catch { /* The formation snapshot below restores the unpublished command. */ }
        }
        run.Revision = revision;
        snapshot.Restore(run);
        return false;
    }

    public bool TryPublish(ActiveRunDto working, ActiveRunDto authoritative)
    {
        if (working.Revision != authoritative.Revision || authoritative.Revision == long.MaxValue || !ValidateRun(working)) return false;
        working.Revision = checked(authoritative.Revision + 1);
        try
        {
            if (!_save.SaveActiveRun(working)) { working.Revision = authoritative.Revision; return false; }
        }
        catch { working.Revision = authoritative.Revision; return false; }
        CopyRun(working, authoritative);
        UpdateHighestRegion(authoritative);
        return true;
    }

    public void AdvanceFloor(ActiveRunDto run)
    {
        var working = CloneRun(run);
        working.FloorIndex++;
        working.PendingNode = false;
        working.SelectedOpponentOptionId = string.Empty;
        working.LegacyPendingOpponent = false;
        if (ValidateRun(working)) TryPublish(working, run);
    }

    public bool TryCompleteTerminal(ActiveRunDto run)
    {
        if (string.IsNullOrWhiteSpace(run.TerminalCompletionId)) return false;
        var grantVictory = run.TerminalVictory && !Meta.AppliedRunCompletionIds.Contains(run.TerminalCompletionId);
        if (grantVictory && Meta.Victories == int.MaxValue) return false;
        var next = new MetaProgressDto
        {
            Version = Meta.Version, Victories = grantVictory ? checked(Meta.Victories + 1) : Meta.Victories,
            HighestRegion = Math.Max(Meta.HighestRegion, ReachedRegion(run)),
            UnlockedHeroIds = [.. Meta.UnlockedHeroIds],
            AppliedRunCompletionIds = [.. Meta.AppliedRunCompletionIds]
        };
        if (grantVictory)
        {
            next.AppliedRunCompletionIds.Add(run.TerminalCompletionId);
            var locked = _content.Catalog.Heroes.FirstOrDefault(entry => !next.UnlockedHeroIds.Contains(entry.StableId));
            if (locked is not null) next.UnlockedHeroIds.Add(locked.StableId);
        }
        // Even defeat must durably record the reached stage before the only
        // retryable terminal record is deleted. A failed delete retries idempotently.
        try { if (!_save.SaveMeta(next)) return false; }
        catch { return false; }
        Meta.Victories = next.Victories;
        Meta.HighestRegion = next.HighestRegion;
        Meta.UnlockedHeroIds = next.UnlockedHeroIds;
        Meta.AppliedRunCompletionIds = next.AppliedRunCompletionIds;
        try { _save.DeleteActiveRun(); return true; }
        catch { return false; }
    }

    public void EndRun() => _save.DeleteActiveRun();

    public ActiveRunDto CloneRun(ActiveRunDto source) => Clone(source);

    public bool ValidateRun(ActiveRunDto run)
        => run is not null && ValidHeader(run) && RunFormationPolicy.Validate(run, _rules) &&
           ActiveRunConfigurationValidator.Validate(run, _content, _project) &&
           new GrowthRunService(_content, _project, _growthRules, this).Validate(run);

    private bool ValidHeader(ActiveRunDto run) => run.Revision >= 0 && run.NextActionSequence > 0 &&
        (_profile is null || run.ProfileId == _profile.ProfileId && run.ContentFingerprint == _profile.ContentFingerprint &&
            run.ProfileCompatibilityVersion == _profile.CompatibilityVersion &&
            run.ProgressionProtocolVersion == _profile.ProgressionProtocolVersion &&
            run.RewardProtocolVersion == _profile.RewardProtocolVersion && run.IdentityProtocolVersion == _profile.IdentityProtocolVersion &&
            Guid.TryParseExact(run.RunId, "N", out _));

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
    }

    private void UpdateHighestRegion(ActiveRunDto run)
    {
        var next = new MetaProgressDto
        {
            Version = Meta.Version, Victories = Meta.Victories,
            HighestRegion = Math.Max(Meta.HighestRegion, ReachedRegion(run)),
            UnlockedHeroIds = [.. Meta.UnlockedHeroIds], AppliedRunCompletionIds = [.. Meta.AppliedRunCompletionIds]
        };
        // The authoritative Run has already committed. A secondary high-water
        // mark must never turn that success into a reported transaction failure.
        try { if (_save.SaveMeta(next)) Meta.HighestRegion = next.HighestRegion; }
        catch (Exception exception) { Godot.GD.PushWarning("征程已保存，历史最高区域暂未同步：" + exception.Message); }
    }

    private int ReachedRegion(ActiveRunDto run) => _project.Campaign.Progression switch
    {
        CompiledExpedition map => run.Expedition is { } state ? Math.Min(map.Stages.Length, state.StageIndex + 1) : 0,
        CompiledTowerProgression tower => Math.Min(tower.Regions.Length, run.FloorIndex / tower.FloorsPerRegion + 1),
        _ => throw new InvalidOperationException("Unsupported progression configuration.")
    };

    private sealed record FormationSnapshot(List<string> Deployment)
    {
        public static FormationSnapshot Capture(ActiveRunDto run) => new([.. run.Deployment]);

        public void Restore(ActiveRunDto run) => run.Deployment = [.. Deployment];
    }

    private static ActiveRunDto Clone(ActiveRunDto source) => CloneUntrusted(source);

    private static ActiveRunDto CloneUntrusted(ActiveRunDto source) => new()
    {
        ProfileId = source.ProfileId, ContentFingerprint = source.ContentFingerprint,
        ProfileCompatibilityVersion = source.ProfileCompatibilityVersion,
        ProgressionProtocolVersion = source.ProgressionProtocolVersion,
        RewardProtocolVersion = source.RewardProtocolVersion, IdentityProtocolVersion = source.IdentityProtocolVersion,
        RunId = source.RunId, Revision = source.Revision, NextActionSequence = source.NextActionSequence,
        Tower = source.Tower is null ? null : new(),
        Expedition = source.Expedition is null ? null : ExpeditionMapState.DeepClone(source.Expedition),
        Action = CloneAction(source.Action),
        CompletedActionIds = source.CompletedActionIds is null ? null! : [.. source.CompletedActionIds],
        Version = source.Version,
        Seed = source.Seed,
        CurrentRunHealth = source.CurrentRunHealth,
        MaximumRunHealth = source.MaximumRunHealth,
        LastBattleConsequence = source.LastBattleConsequence,
        OpeningRecruitment = source.OpeningRecruitment,
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
                    Growth = unit.Growth is null ? null! : new HeroGrowthDto
                    {
                        BreedingPoints = unit.Growth.BreedingPoints,
                        AddedAttack = unit.Growth.AddedAttack,
                        AddedMaxHealth = unit.Growth.AddedMaxHealth,
                        AscensionId = unit.Growth.AscensionId,
                        ProductionMode = unit.Growth.ProductionMode,
                        ProductionTargetInstanceId = unit.Growth.ProductionTargetInstanceId,
                        History = unit.Growth.History is null ? null! : unit.Growth.History.Select(gain => gain is null ? null! : CloneGain(gain)).ToList()
                    },
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
                    RunRuleStates = item.RunRuleStates is null ? null! : item.RunRuleStates.Select(state => state is null ? null! :
                        new RelicRunRuleStateDto { RuleId = state.RuleId, GrantedAtSequence = state.GrantedAtSequence, Remainder = state.Remainder,
                            ClearedSites = state.ClearedSites is null ? null! : state.ClearedSites.Select(site => site is null ? null! :
                                new RelicClearedSiteDto { SiteId = site.SiteId, ActionId = site.ActionId, Sequence = site.Sequence, NativeKills = site.NativeKills }).ToList() }).ToList(),
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
        SelectedOpponentOptionId = source.SelectedOpponentOptionId,
        LegacyPendingOpponent = source.LegacyPendingOpponent,
        PendingOffer = source.PendingOffer,
        PendingRewardGroups = source.PendingRewardGroups,
        TerminalCompletionId = source.TerminalCompletionId,
        TerminalVictory = source.TerminalVictory,
        Growth = source.Growth is null ? null! : new GrowthRunDto
        {
            RulesId = source.Growth.RulesId,
            Materials = source.Growth.Materials,
            CategoryMaterials = source.Growth.CategoryMaterials is null ? null! : new Dictionary<string, int>(source.Growth.CategoryMaterials, StringComparer.Ordinal),
            Research = source.Growth.Research,
            SpellInventory = source.Growth.SpellInventory is null ? null! : new Dictionary<string, int>(source.Growth.SpellInventory, StringComparer.Ordinal),
            EquippedSpellId = source.Growth.EquippedSpellId,
            SpellTargetInstanceId = source.Growth.SpellTargetInstanceId,
            PendingDiscovery = source.Growth.PendingDiscovery is null ? null : new GrowthDiscoveryDto
            {
                OfferId = source.Growth.PendingDiscovery.OfferId,
                UpgradedHeroInstanceId = source.Growth.PendingDiscovery.UpgradedHeroInstanceId,
                CandidateIds = source.Growth.PendingDiscovery.CandidateIds is null ? null! : [.. source.Growth.PendingDiscovery.CandidateIds]
            },
            PendingNode = CloneNode(source.Growth.PendingNode),
            LastSettledFloorIndex = source.Growth.LastSettledFloorIndex,
            History = source.Growth.History is null ? null! : source.Growth.History.Select(value => value is null ? null! : CloneSettlement(value)).ToList()
        },
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
        target.ProfileId = copy.ProfileId;
        target.ContentFingerprint = copy.ContentFingerprint;
        target.ProfileCompatibilityVersion = copy.ProfileCompatibilityVersion;
        target.ProgressionProtocolVersion = copy.ProgressionProtocolVersion;
        target.RewardProtocolVersion = copy.RewardProtocolVersion;
        target.IdentityProtocolVersion = copy.IdentityProtocolVersion;
        target.RunId = copy.RunId;
        target.Revision = copy.Revision;
        target.NextActionSequence = copy.NextActionSequence;
        target.Tower = copy.Tower;
        target.Expedition = copy.Expedition;
        target.Action = copy.Action;
        target.CompletedActionIds = copy.CompletedActionIds;
        target.Version = copy.Version;
        target.Seed = copy.Seed;
        target.CurrentRunHealth = copy.CurrentRunHealth;
        target.MaximumRunHealth = copy.MaximumRunHealth;
        target.LastBattleConsequence = copy.LastBattleConsequence;
        target.OpeningRecruitment = copy.OpeningRecruitment;
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
        target.SelectedOpponentOptionId = copy.SelectedOpponentOptionId;
        target.LegacyPendingOpponent = copy.LegacyPendingOpponent;
        target.PendingOffer = copy.PendingOffer;
        target.PendingRewardGroups = copy.PendingRewardGroups;
        target.TerminalCompletionId = copy.TerminalCompletionId;
        target.TerminalVictory = copy.TerminalVictory;
        target.Growth = copy.Growth;
        target.LegacyHeroId = copy.LegacyHeroId;
        target.LegacyHeroHealthRatio = copy.LegacyHeroHealthRatio;
        target.LegacyHeroCell = copy.LegacyHeroCell;
        target.LegacyDeploymentCells = copy.LegacyDeploymentCells;
    }

    private static GrowthGainDto CloneGain(GrowthGainDto gain) => new()
    {
        ActionSource = gain.ActionSource,
        FloorIndex = gain.FloorIndex, BattleNumber = gain.BattleNumber,
        ProducerInstanceId = gain.ProducerInstanceId, TargetInstanceId = gain.TargetInstanceId,
        Mode = gain.Mode, Amount = gain.Amount, AbilityId = gain.AbilityId, Source = gain.Source,
        OperationKey = gain.OperationKey, RememberAbilityId = gain.RememberAbilityId,
        RememberOperationKey = gain.RememberOperationKey, GrantOrdinal = gain.GrantOrdinal, GrantedTick = gain.GrantedTick
    };

    private static GrowthNodeSnapshotDto? CloneNode(GrowthNodeSnapshotDto? node) => node is null ? null : new()
    {
        Breeding = node.Breeding is null ? null : new GrowthBreedingSnapshotDto
        {
            ProducerInstanceId = node.Breeding.ProducerInstanceId, Points = node.Breeding.Points
        },
        Source = node.Source,
        FloorIndex = node.FloorIndex, BattleNumber = node.BattleNumber, IsBattle = node.IsBattle,
        ParticipantInstanceIds = node.ParticipantInstanceIds is null ? null! : [.. node.ParticipantInstanceIds],
        Assignments = node.Assignments is null ? null! : node.Assignments.Select(value => value is null ? null! : new GrowthAssignmentDto
        {
            ProducerInstanceId = value.ProducerInstanceId, TargetInstanceId = value.TargetInstanceId, Mode = value.Mode
        }).ToList(),
        ConsumedSpellId = node.ConsumedSpellId, SpellTargetInstanceId = node.SpellTargetInstanceId
    };

    private static GrowthSettlementDto CloneSettlement(GrowthSettlementDto settlement) => new()
    {
        Breeding = settlement.Breeding is null ? null : new GrowthBreedingGainDto
        {
            ProducerInstanceId = settlement.Breeding.ProducerInstanceId,
            PreviousPoints = settlement.Breeding.PreviousPoints, Amount = settlement.Breeding.Amount
        },
        Source = settlement.Source,
        FloorIndex = settlement.FloorIndex, BattleNumber = settlement.BattleNumber,
        IsBattle = settlement.IsBattle, MaterialsGranted = settlement.MaterialsGranted,
        Gains = settlement.Gains is null ? null! : settlement.Gains.Select(gain => gain is null ? null! : CloneGain(gain)).ToList()
    };

    private static RunActionDto? CloneAction(RunActionDto? action) => action is null ? null : new()
    {
        Source = action.Source, Phase = action.Phase, Encounter = action.Encounter,
        GrowthSnapshot = CloneNode(action.GrowthSnapshot),
        BattleFreeze = action.BattleFreeze is null ? null : new RunBattleFreezeDto
        {
            BattleNumber = action.BattleFreeze.BattleNumber, Gold = action.BattleFreeze.Gold,
            InputsFingerprint = action.BattleFreeze.InputsFingerprint
        },
        ServiceOffer = action.ServiceOffer, RewardGroups = action.RewardGroups, RewardCursor = action.RewardCursor,
        IsStageBoss = action.IsStageBoss, ActionPaid = action.ActionPaid
    };
}
