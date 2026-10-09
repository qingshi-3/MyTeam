using System.Linq;
using System.Collections.Immutable;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Abilities;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;
using TowerAutobattler.UI;
using TowerAutobattler.Run;

namespace TowerAutobattler.Project;

public sealed record CompiledContentPool(
    string StableId,
    ContentPoolKind Kind,
    ImmutableArray<string> ContentIds);

public sealed record CompiledTowerNode(
    TowerNodeType Type,
    string TitlePattern,
    string DescriptionPattern,
    int Risk)
{
    public string Title(string regionName) => TitlePattern.Replace("{region}", regionName);
    public string Description(string regionName) => DescriptionPattern.Replace("{region}", regionName);
}

public sealed record CompiledTowerNodeTable(
    ImmutableDictionary<TowerNodeType, CompiledTowerNode> Nodes,
    ImmutableArray<TowerNodeType> Rotation,
    int BossLocalFloor,
    int RegularOptionCount,
    int RotationStride,
    int FloorSeedStride);

public sealed record CompiledBossPhase(
    string StableId,
    string DisplayName,
    float StartHealthRatio,
    CompiledAbilityLoadout? AbilityLoadout);

public sealed record CompiledBossTimeline(
    string StableId,
    string BossContentId,
    ImmutableArray<CompiledBossPhase> Phases);

public sealed record CompiledEncounterComposition(string StableId, string DisplayName, int MinLocalFloor,
    int MaxLocalFloor, ImmutableArray<string> EnemyIds, ImmutableArray<Vector2I> Cells)
{
    public string PressureSummary { get; init; } = string.Empty;
    public int RewardGold { get; init; }
    public int RewardResearch { get; init; }
    public int RewardMaterials { get; init; }
    public bool OpponentChoiceOnly { get; init; }
}

public sealed record CompiledEncounter(
    string StableId,
    TowerNodeType NodeType,
    string TitlePattern,
    CompiledContentPool EnemyPool,
    CompiledContentPool FloorRulePool,
    string LeadEnemyId,
    int BaseEnemyCount,
    bool AddRegionIndexToCount,
    int SeedSalt,
    CompiledBossTimeline? BossTimeline,
    float EnemyHealthMultiplier = 1, float EnemyDamageMultiplier = 1, ImmutableArray<string> AlternateLeadEnemyIds = default)
{
    public ImmutableArray<float> LocalHealthMultipliers { get; init; } = [];
    public ImmutableArray<float> LocalDamageMultipliers { get; init; } = [];
    public ImmutableArray<CompiledEncounterComposition> Compositions { get; init; } = [];
    public int OpponentChoiceCount { get; init; }
    public string Title(string regionName) => TitlePattern.Replace("{region}", regionName);
}

public sealed record CompiledTowerRegion(
    string StableId,
    string DisplayName,
    string Description,
    Color AccentColor,
    ImmutableDictionary<TowerNodeType, CompiledEncounter> Encounters);

public sealed record CompiledCampaign(
    string StableId,
    CompiledProgression Progression,
    CompiledContentPool StarterPool,
    CompiledContentPool RecruitmentPool,
    CompiledContentPool ItemRewardPool,
    CompiledContentPool ShopPool)
{
    public CompiledCampaign(string stableId, int floorsPerRegion, ImmutableArray<CompiledTowerRegion> regions,
        CompiledTowerNodeTable nodeTable, CompiledContentPool starterPool, CompiledContentPool recruitmentPool,
        CompiledContentPool itemRewardPool, CompiledContentPool shopPool)
        : this(stableId, new CompiledTowerProgression(floorsPerRegion, regions, nodeTable), starterPool,
            recruitmentPool, itemRewardPool, shopPool) { }
    public CompiledTowerProgression Tower => Progression as CompiledTowerProgression
        ?? throw new System.InvalidOperationException("Tower rules requested for an expedition.");
    public int FloorsPerRegion { get => Tower.FloorsPerRegion; init => Progression = Tower with { FloorsPerRegion = value }; }
    public ImmutableArray<CompiledTowerRegion> Regions { get => Tower.Regions; init => Progression = Tower with { Regions = value }; }
    public CompiledTowerNodeTable NodeTable { get => Tower.NodeTable; init => Progression = Tower with { NodeTable = value }; }
    public int TotalFloors => Tower.FloorsPerRegion * Tower.Regions.Length;
    public ImmutableDictionary<string, CompiledRunOffer> OfferRecipesById { get; init; } = ImmutableDictionary<string, CompiledRunOffer>.Empty;
    public System.Collections.Generic.IEnumerable<CompiledEncounter> Encounters => Progression is CompiledExpedition expedition
        ? expedition.Encounters.Values.Select(value => value.Encounter).DistinctBy(value => value.StableId)
        : Tower.Regions.SelectMany(region => region.Encounters.Values);
    public CompiledRecruitmentSupply? RecruitmentSupply { get; init; }
    public bool SeparateItemRewardCategories { get; init; }
    public ImmutableDictionary<RunOfferKind, CompiledRunOffer> RunOffers { get; init; } = ImmutableDictionary<RunOfferKind, CompiledRunOffer>.Empty;
}

public sealed record CompiledRunRules(
    int OrdinaryPopulationCap,
    int PhysicalDeploymentCeiling,
    int ReserveCapacity,
    int StarterRosterHeroCount,
    int InitialPopulation,
    int EquipmentSlotCapacity,
    int TacticalCommandSlotCount,
    ImmutableArray<string> StarterTacticalCommandIds,
    ImmutableDictionary<string, string> LegacyTacticalCommandByHeroId,
    int RecruitmentChoiceCount,
    int ItemChoiceCount,
    int StartingGold,
    int NormalBattleGold,
    int EliteBattleGold,
    int BossBattleGold,
    float VictoryHeroRecovery,
    float VictorySoldierRecovery,
    float MinimumVictoryHeroHealth,
    float MinimumLivingSoldierHealth,
    float DefeatedSoldierHealth,
    int RiskyEventSuccessGold,
    float RiskyEventSuccessChance,
    float RiskyEventHealthLoss,
    float RiskyEventMinimumHealth,
    int SafeEventGold,
    float RestHeroHealing,
    float RestSoldierHealing,
    int RestGold,
    int InitialUnlockedHeroCount)
{
    public ImmutableDictionary<string, CompiledStartingHeroEconomy> StartingHeroEconomy { get; init; } = ImmutableDictionary<string, CompiledStartingHeroEconomy>.Empty;
    public int InitialRunHealth { get; init; } = 100;
    public int MaximumRunHealth { get; init; } = 100;
    public int CombatDefeatHealthLoss { get; init; } = 20;
    public int EliteDefeatHealthLoss { get; init; } = 30;
    public int RestRunHealthRecovery { get; init; } = 25;
}

public sealed record CompiledProjectPresentation(
    SemanticIconCatalog SemanticIcons,
    PackedScene ChoiceCard,
    PackedScene UnitChoiceCard,
    PackedScene ItemChoiceCard);

public sealed record CompiledGameProject(
    string StableId,
    ContentCatalog Content,
    CompiledCampaign Campaign,
    CompiledRunRules RunRules,
    CompiledProjectPresentation Presentation,
    ImmutableDictionary<string, PackedScene> FloorRules)
{
    public CompiledMatrixStatusRules MatrixStatusRules { get; init; } = CompiledMatrixStatusRules.Default;
}

public sealed record GameProjectCompilationResult(
    CompiledGameProject? Project,
    ValidationReport Report);
