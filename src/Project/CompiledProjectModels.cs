using System.Collections.Immutable;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;
using TowerAutobattler.UI;

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
    CompiledBossTimeline? BossTimeline)
{
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
    int FloorsPerRegion,
    ImmutableArray<CompiledTowerRegion> Regions,
    CompiledTowerNodeTable NodeTable,
    CompiledContentPool StarterPool,
    CompiledContentPool RecruitmentPool,
    CompiledContentPool ItemRewardPool,
    CompiledContentPool ShopPool)
{
    public int TotalFloors => FloorsPerRegion * Regions.Length;
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
    int InitialUnlockedHeroCount);

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
    ImmutableDictionary<string, PackedScene> FloorRules);

public sealed record GameProjectCompilationResult(
    CompiledGameProject? Project,
    ValidationReport Report);
