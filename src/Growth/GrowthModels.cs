using System.Collections.Generic;
using System.Collections.Immutable;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.Growth;

public enum GrowthProductionMode
{
    Attack,
    Vitality,
    Research
}

public sealed record CompiledGrowthHero(
    string ContentId,
    ImmutableArray<GrowthProductionMode> ProductionModes,
    float GrowthRate,
    int ResearchYield,
    string AscensionId,
    string AscensionName,
    string AscensionDescription,
    CompiledAbilityLoadout? AscendedLoadout = null,
    CompiledAbilityLoadout? BaseLoadout = null,
    string MaterialCategory = "");

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
    public float AddedAttack { get; set; }
    public float AddedMaxHealth { get; set; }
    public string AscensionId { get; set; } = string.Empty;
    public GrowthProductionMode ProductionMode { get; set; } = GrowthProductionMode.Research;
    public string ProductionTargetInstanceId { get; set; } = string.Empty;
    public List<GrowthGainDto> History { get; set; } = [];
}

public sealed class GrowthGainDto
{
    public int FloorIndex { get; set; }
    public int BattleNumber { get; set; }
    public string ProducerInstanceId { get; set; } = string.Empty;
    public string TargetInstanceId { get; set; } = string.Empty;
    public GrowthProductionMode Mode { get; set; }
    public float Amount { get; set; }
    public string AbilityId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
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
    public int FloorIndex { get; set; }
    public int BattleNumber { get; set; }
    public bool IsBattle { get; set; }
    public List<GrowthGainDto> Gains { get; set; } = [];
    public int MaterialsGranted { get; set; }
}
