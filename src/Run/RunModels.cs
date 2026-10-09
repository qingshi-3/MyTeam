using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using TowerAutobattler.Equipment;
using TowerAutobattler.Growth;

namespace TowerAutobattler.Run;

public sealed record OpeningRecruitmentDto(ImmutableArray<string> CandidateIds, ImmutableArray<string> SelectedIds);

public sealed class RosterHeroInstanceDto
{
    public string InstanceId { get; set; } = string.Empty;
    public string ContentId { get; set; } = string.Empty;
    // Historical saves/operations retain this field; Run battle preparation always starts full.
    public float HealthRatio { get; set; } = 1f;
    public int Rank { get; set; } = 1;
    public List<EquipmentInstanceState> Equipment { get; set; } = [];
    public HeroGrowthDto Growth { get; set; } = new();
}

public sealed class ItemInstanceDto
{
    public string InstanceId { get; set; } = string.Empty;
    public string ContentId { get; set; } = string.Empty;
    public int Stacks { get; set; } = 1;
    public int Charges { get; set; }
    public int Roll { get; set; }
    public List<RelicCounterStateDto> Counters { get; set; } = [];
    public List<RelicRunRuleStateDto> RunRuleStates { get; set; } = [];
}

// Run-owned clear receipts, deliberately separate from per-battle reactive counters.
public sealed class RelicRunRuleStateDto
{
    public string RuleId { get; set; } = string.Empty;
    public long GrantedAtSequence { get; set; }
    public int Remainder { get; set; }
    public List<RelicClearedSiteDto> ClearedSites { get; set; } = [];
}

public sealed class RelicClearedSiteDto
{
    public string SiteId { get; set; } = string.Empty;
    public string ActionId { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public int NativeKills { get; set; }
}

public sealed class RelicCounterStateDto
{
    public string CounterId { get; set; } = string.Empty;
    public int Value { get; set; }
}

public sealed class PopulationCapSourceDto
{
    public string SourceId { get; set; } = string.Empty;
    public int Amount { get; set; }
}

public sealed class TowerProgressionStateDto
{
    public int FloorIndex { get; set; } = 0;
    public bool PendingNode { get; set; } = false;
    public TowerNodeType SelectedNode { get; set; } = TowerNodeType.Combat;
    public string SelectedOpponentOptionId { get; set; } = string.Empty;
    public bool LegacyPendingOpponent { get; set; } = false;
    public PendingRunOffer? PendingOffer { get; set; } = null;
    public ImmutableArray<PendingRunOffer> PendingRewardGroups { get; set; } = [];
}

public sealed class ActiveRunDto
{
    public string ProfileId { get; set; } = string.Empty;
    public string ContentFingerprint { get; set; } = string.Empty;
    public int ProfileCompatibilityVersion { get; set; }
    public int ProgressionProtocolVersion { get; set; }
    public int RewardProtocolVersion { get; set; }
    public int IdentityProtocolVersion { get; set; }
    public string RunId { get; set; } = string.Empty;
    public long Revision { get; set; }
    public long NextActionSequence { get; set; } = 1;
    public TowerProgressionStateDto? Tower { get; set; } = new();
    private TowerAutobattler.Expedition.ExpeditionStateDto? _expedition;
    public TowerAutobattler.Expedition.ExpeditionStateDto? Expedition
    {
        get => _expedition;
        set { _expedition = value; if (value is not null) Tower = null; }
    }
    public RunActionDto? Action { get; set; }
    public List<string> CompletedActionIds { get; set; } = [];

    public int Version { get; set; } = ActiveRunFormationSchema.CurrentVersion;
    public ulong Seed { get; set; }
    // Missing v7 fields are invalid, not an implicit full heal. Older schemas migrate explicitly.
    public int CurrentRunHealth { get; set; } = -1;
    public int MaximumRunHealth { get; set; } = -1;
    public RunBattleConsequence? LastBattleConsequence { get; set; }
    public OpeningRecruitmentDto? OpeningRecruitment { get; set; }
    public List<RosterHeroInstanceDto> Roster { get; set; } = [];
    public int CurrentPopulation { get; set; } = 1;
    public List<PopulationCapSourceDto> PopulationCapSources { get; set; } = [];
    public List<string> Deployment { get; set; } = ActiveRunFormationSchema.EmptyDeployment();
    public List<ItemInstanceDto> Items { get; set; } = [];
    // Unequipped instances retain their identity; only roster-owned instances project into battle.
    public List<EquipmentInstanceState> EquipmentInventory { get; set; } = [];
    public List<string> EquippedTacticalCommandIds { get; set; } = [];
    public int Gold { get; set; } = 16;
    public int BattleNumber { get; set; }
    [JsonIgnore]
    public int FloorIndex
    {
        get => Tower?.FloorIndex ?? 0;
        set { if (Tower is not null) Tower.FloorIndex = value; }
    }
    [JsonPropertyName("FloorIndex"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LegacyFloorIndexInput
    {
        get => null;
        set { if (value is not null) { Tower ??= new(); Tower.FloorIndex = value.Value; } }
    }
    [JsonIgnore]
    public bool PendingNode
    {
        get => Tower?.PendingNode ?? false;
        set { if (Tower is not null) Tower.PendingNode = value; }
    }
    [JsonPropertyName("PendingNode"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? LegacyPendingNodeInput
    {
        get => null;
        set { if (value is not null) { Tower ??= new(); Tower.PendingNode = value.Value; } }
    }
    [JsonIgnore]
    public TowerNodeType SelectedNode
    {
        get => Tower?.SelectedNode ?? TowerNodeType.Combat;
        set { if (Tower is not null) Tower.SelectedNode = value; }
    }
    [JsonPropertyName("SelectedNode"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TowerNodeType? LegacySelectedNodeInput
    {
        get => null;
        set { if (value is not null) { Tower ??= new(); Tower.SelectedNode = value.Value; } }
    }
    [JsonIgnore]
    public string SelectedOpponentOptionId
    {
        get => Tower?.SelectedOpponentOptionId ?? string.Empty;
        set { if (Tower is not null) Tower.SelectedOpponentOptionId = value; }
    }
    [JsonPropertyName("SelectedOpponentOptionId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacySelectedOpponentOptionIdInput
    {
        get => null;
        set { if (value is not null) { Tower ??= new(); Tower.SelectedOpponentOptionId = value; } }
    }
    [JsonIgnore]
    public bool LegacyPendingOpponent
    {
        get => Tower?.LegacyPendingOpponent ?? false;
        set { if (Tower is not null) Tower.LegacyPendingOpponent = value; }
    }
    [JsonPropertyName("LegacyPendingOpponent"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? LegacyLegacyPendingOpponentInput
    {
        get => null;
        set { if (value is not null) { Tower ??= new(); Tower.LegacyPendingOpponent = value.Value; } }
    }
    [JsonIgnore]
    public PendingRunOffer? PendingOffer
    {
        get => Expedition is null ? Tower?.PendingOffer : Action?.CurrentOffer;
        set { if (Tower is not null) Tower.PendingOffer = value; }
    }
    [JsonPropertyName("PendingOffer"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PendingRunOffer? LegacyPendingOfferInput
    {
        get => null;
        set { if (value is not null) { Tower ??= new(); Tower.PendingOffer = value; } }
    }
    [JsonIgnore]
    public ImmutableArray<PendingRunOffer> PendingRewardGroups
    {
        get => Tower?.PendingRewardGroups ?? [];
        set { if (Tower is not null) Tower.PendingRewardGroups = value; }
    }
    [JsonPropertyName("PendingRewardGroups"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ImmutableArray<PendingRunOffer>? LegacyPendingRewardGroupsInput
    {
        get => null;
        set { if (value is not null) { Tower ??= new(); Tower.PendingRewardGroups = value.Value; } }
    }
    public string TerminalCompletionId { get; set; } = string.Empty;
    public bool TerminalVictory { get; set; }
    public GrowthRunDto? Growth { get; set; }

    // Schema-v3 compatibility input only. A successful migration clears these
    // fields so current saves publish only the unified roster/formation model.
    [JsonPropertyName("HeroId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacyHeroId { get; set; }

    [JsonPropertyName("HeroHealthRatio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public float LegacyHeroHealthRatio { get; set; }

    [JsonPropertyName("HeroCell")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FormationCellDto? LegacyHeroCell { get; set; }

    [JsonPropertyName("DeploymentCells")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<FormationCellDto>? LegacyDeploymentCells { get; set; }
}

public sealed class MetaProgressDto
{
    public int Version { get; set; } = 1;
    public List<string> UnlockedHeroIds { get; set; } = [];
    public int Victories { get; set; }
    public int HighestRegion { get; set; }
    public List<string> AppliedRunCompletionIds { get; set; } = [];
}

public sealed class SettingsDto
{
    public int Version { get; set; } = 1;
    public float MasterVolume { get; set; } = .8f;
    public float DefaultBattleSpeed { get; set; } = 1f;
    public bool ShowDamageNumbers { get; set; } = true;
    public bool ReduceUiMotion { get; set; }
}

public enum ActiveRunLoadFailureKind
{
    ReadFailed,
    MigrationRejected,
    ValidationRejected,
    MigrationPublicationFailed
}

public sealed record ActiveRunLoadDiagnostic(ActiveRunLoadFailureKind Kind, string Message);

public sealed record TowerNodeOption(TowerNodeType Type, string Title, string Description, int Risk);
public sealed record OpponentOption(string OptionId, TowerNodeType NodeType, string Title,
    string PressureSummary, EncounterPlan Encounter)
{
    public int RewardGold { get; init; }
    public int RewardResearch { get; init; }
    public int RewardMaterials { get; init; }
}
public sealed record EncounterPlan(
    string Title,
    string FloorRuleId,
    IReadOnlyList<string> EnemyIds,
    bool IsBoss,
    bool IsElite,
    string EncounterId = "",
    TowerNodeType NodeType = TowerNodeType.Combat)
{
    public string CompositionId { get; init; } = string.Empty;
    public string ExpeditionProfileId { get; init; } = string.Empty;
    public string ExpeditionSiteId { get; init; } = string.Empty;
    public bool ExpeditionBoss { get; init; }
    [JsonIgnore]
    public ImmutableArray<Godot.Vector2I> EnemyCells { get; init; } = [];
    [JsonPropertyName("EnemyCells")]
    public ImmutableArray<FormationCellDto> SerializedEnemyCells
    {
        get => EnemyCells.IsDefault ? default : EnemyCells.Select(FormationCellDto.FromCell).ToImmutableArray();
        init => EnemyCells = value.IsDefault ? default : value.Select(cell => cell.ToCell()).ToImmutableArray();
    }
}
