using System.Collections.Generic;
using System.Collections.Immutable;
using TowerAutobattler.Abilities;
using TowerAutobattler.Run;

namespace TowerAutobattler.Growth;

public enum GrowthProductionMode
{
    Attack = 0,
    Vitality = 1,
    Research = 2,
    Cultivation = 3
}

public sealed record CompiledGrowthHero(
    string ContentId,
    ImmutableArray<GrowthProductionMode> ProductionModes,
    float GrowthRate,
    int ResearchYield,
    string AscensionId,
    CompiledGrowthAscensionDisplay? AscensionDisplay,
    CompiledGrowthAscensionBenefit? AscensionBenefit = null,
    CompiledAbilityLoadout? BaseLoadout = null,
    string MaterialCategory = "",
    float FixedAttackGain = 0,
    float FixedMaxHealthGain = 0,
    int BreedingYield = 0,
    float AttackGainPerBreedingPoint = 0,
    float MaxHealthGainPerBreedingPoint = 0)
{
    public CompiledAbilityLoadout? ResolveBattleLoadout(bool ascended) =>
        ascended ? AscensionBenefit?.BattleLoadout ?? BaseLoadout : BaseLoadout;
}

public sealed record CompiledGrowthAscensionDisplay(string DisplayName, string CompletedLabel);

public sealed record CompiledGrowthAscensionBenefit(string Description, CompiledAbilityLoadout BattleLoadout);

public sealed record CompiledGrowthSpell(
    string StableId,
    string DisplayName,
    string Description,
    int ResearchCost,
    CompiledAbilityLoadout BattleLoadout);

public sealed record CompiledGrowthRules(
    string StableId,
    ImmutableDictionary<string, CompiledGrowthHero> Heroes,
    ImmutableArray<string> FirstDiscoveryPool,
    ImmutableArray<string> AdvancedDiscoveryPool,
    ImmutableDictionary<string, CompiledGrowthSpell> Spells,
    int MaterialsPerNode = 1,
    int AscensionCost = 4);

public sealed record GrowthCommandResult(bool Succeeded, string Message)
{
    public static GrowthCommandResult Success(string message = "") => new(true, message);
    public static GrowthCommandResult Reject(string message) => new(false, message);
}

public sealed class HeroGrowthDto
{
    public int BreedingPoints { get; set; }
    public float AddedAttack { get; set; }
    public float AddedMaxHealth { get; set; }
    public string AscensionId { get; set; } = string.Empty;
    public GrowthProductionMode ProductionMode { get; set; } = GrowthProductionMode.Research;
    public string ProductionTargetInstanceId { get; set; } = string.Empty;
    public List<GrowthGainDto> History { get; set; } = [];
}

public sealed class GrowthGainDto
{
    public RunActionSource? ActionSource { get; set; }
    public int FloorIndex { get; set; }
    public int BattleNumber { get; set; }
    public string ProducerInstanceId { get; set; } = string.Empty;
    public string TargetInstanceId { get; set; } = string.Empty;
    public GrowthProductionMode Mode { get; set; }
    public float Amount { get; set; }
    public string AbilityId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string OperationKey { get; set; } = string.Empty;
    public string RememberAbilityId { get; set; } = string.Empty;
    public string RememberOperationKey { get; set; } = string.Empty;
    public int GrantOrdinal { get; set; }
    public int GrantedTick { get; set; }
}

public sealed class GrowthRunDto
{
    public string RulesId { get; set; } = string.Empty;
    public int Materials { get; set; }
    public Dictionary<string, int> CategoryMaterials { get; set; } = new(System.StringComparer.Ordinal);
    public int Research { get; set; }
    public Dictionary<string, int> SpellInventory { get; set; } = new(System.StringComparer.Ordinal);
    public string EquippedSpellId { get; set; } = string.Empty;
    public string SpellTargetInstanceId { get; set; } = string.Empty;
    public GrowthDiscoveryDto? PendingDiscovery { get; set; }
    // Compatibility slot for Tower saves only. Expedition snapshots belong to
    // ActiveRunDto.Action.GrowthSnapshot and must never be copied here.
    public GrowthNodeSnapshotDto? PendingNode { get; set; }
    public int LastSettledFloorIndex { get; set; } = -1;
    public List<GrowthSettlementDto> History { get; set; } = [];
}

public sealed class GrowthDiscoveryDto
{
    public string OfferId { get; set; } = string.Empty;
    public string UpgradedHeroInstanceId { get; set; } = string.Empty;
    public List<string> CandidateIds { get; set; } = [];
}

public sealed class GrowthNodeSnapshotDto
{
    public GrowthBreedingSnapshotDto? Breeding { get; set; }
    public RunActionSource? Source { get; set; }
    public int FloorIndex { get; set; }
    public int BattleNumber { get; set; }
    public bool IsBattle { get; set; }
    public List<string> ParticipantInstanceIds { get; set; } = [];
    public List<GrowthAssignmentDto> Assignments { get; set; } = [];
    public string ConsumedSpellId { get; set; } = string.Empty;
    public string SpellTargetInstanceId { get; set; } = string.Empty;
}

public sealed class GrowthAssignmentDto
{
    public string ProducerInstanceId { get; set; } = string.Empty;
    public string TargetInstanceId { get; set; } = string.Empty;
    public GrowthProductionMode Mode { get; set; }
}

public sealed class GrowthSettlementDto
{
    public GrowthBreedingGainDto? Breeding { get; set; }
    public RunActionSource? Source { get; set; }
    public int FloorIndex { get; set; }
    public int BattleNumber { get; set; }
    public bool IsBattle { get; set; }
    public List<GrowthGainDto> Gains { get; set; } = [];
    public int MaterialsGranted { get; set; }
}

public sealed class GrowthBreedingSnapshotDto
{
    public string ProducerInstanceId { get; set; } = string.Empty;
    public int Points { get; set; }
}

public sealed class GrowthBreedingGainDto
{
    public string ProducerInstanceId { get; set; } = string.Empty;
    public int PreviousPoints { get; set; }
    public int Amount { get; set; }
}
