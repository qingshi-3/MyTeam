using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Battle;

public enum BattleOutcome { Running, PlayerVictory, PlayerDefeat, Timeout }
public enum BattleUnitMode { Seeking, Moving, Waiting, Attacking, Casting, Recovering, Disabled, Defeated }
public enum BattleActionKind { None, Attack, Heal }

public sealed record UnitSnapshot(
    string ContentId, string DisplayName, UnitRole Role, bool IsHero, bool IsBoss,
    float MaxHealth, float Damage, float Range, int AttackTicks, int MoveTicks,
    float Armor, float HealPower, float SplashRadius, float LifeSteal,
    IReadOnlyList<string> Tags, UnitBehaviorSnapshot Behavior,
    CompiledAbilityLoadout? AbilityLoadout = null,
    CompiledAttributeSetDefinition? AttributeDefinition = null,
    ImmutableArray<CompiledTraitContribution> TraitContributions = default);

public sealed record UnitBehaviorSnapshot(
    int SlowOnHitTicks = 0, float AdjacentArmorAura = 0, float AdjacentDamageAura = 0,
    float ExecuteHealthThreshold = 0, float LowHealthDamageBonus = 0,
    float OnDeathDamage = 0, bool PiercingLine = false,
    int PeriodicShieldTicks = 0, float PeriodicShieldAmount = 0,
    int PeriodicSummonTicks = 0, int PeriodicSummonLimit = 0, bool PreferBacklineTargets = false,
    string SummonContentId = "");

public sealed record HeroRuleSnapshot(
    float SoldierHealthMultiplier, float SoldierDamageMultiplier,
    float HeroDamageMultiplier, float EmptySlotHeroBonus, float EmptySlotHeroDefense, float EmptySlotStartShield, bool PreferBossTargets,
    string RequiredSoldierTag, float TaggedSoldierHealthMultiplier, float TaggedSoldierDamageMultiplier,
    float FormationArmorBonus, float FormationDamageBonus, float KillGrowth, float HeroLifeStealBonus,
    bool SummonOnAllyDeath, bool AddBattleConstruct, int BattleGoldBonus, int RecruitConversionGold,
    string SummonContentId);

public sealed record ModifierSnapshot(
    float ArmyHealthMultiplier = 1f, float ArmyDamageMultiplier = 1f,
    float HeroHealthMultiplier = 1f, float HeroDamageMultiplier = 1f,
    float ArmyLifeStealBonus = 0f, float HeroLifeStealBonus = 0f, int StartShield = 0,
    int EmptySlotPower = 0, bool SummonToken = false, int GoldPerBattle = 0,
    float FormationAdjacentArmor = 0, float FormationAdjacentDamageMultiplier = 1f, string SummonContentId = "");

public sealed record SummonProfiles(
    UnitSnapshot? DeathSummon = null, UnitSnapshot? HeroConstruct = null,
    UnitSnapshot? Mercenary = null, UnitSnapshot? ItemToken = null);

public sealed record BattleSpawn(
    UnitSnapshot Unit, int Team, Vector2I Cell, string InstanceId,
    float HealthRatio = 1f, bool IsTemporary = false, UnitSnapshot? BehaviorSummon = null,
    bool? IsPersistentRosterHero = null);

public sealed record BattleIdentity(
    string EncounterId,
    TowerNodeType NodeType,
    ulong RunSeed,
    int FloorIndex,
    int BattleNumber);

public sealed record BossPhaseSnapshot(
    string StableId,
    string DisplayName,
    float StartHealthRatio,
    CompiledAbilityLoadout? AbilityLoadout);

public sealed record BossTimelineSnapshot(
    string StableId,
    string BossContentId,
    ImmutableArray<BossPhaseSnapshot> Phases);

public sealed class BattleConfig
{
    public ulong Seed { get; init; }
    public BattleIdentity? Identity { get; init; }
    public required IBattleFloorRuleRuntime FloorRule { get; init; }
    public List<BattleSpawn> Spawns { get; init; } = [];
    public required HeroRuleSnapshot HeroRule { get; init; }
    public ModifierSnapshot Modifiers { get; init; } = new();
    public SummonProfiles Summons { get; init; } = new();
    public int EmptyDeploymentSlots { get; init; }
    public int StartingGold { get; init; }
    public RelicBattlePreparation? Relics { get; init; }
    public IReadOnlyDictionary<string, UnitSnapshot> RelicSummons { get; init; } =
        ImmutableDictionary<string, UnitSnapshot>.Empty;
    public EquipmentBattlePreparation Equipment { get; init; } = EquipmentBattlePreparation.Empty;
    public TraitBattlePreparation Traits { get; init; } = TraitBattlePreparation.Empty;
    public TacticalCommandBattlePreparation? TacticalCommands { get; init; }
    public IReadOnlyDictionary<string, UnitSnapshot> TacticalSummons { get; init; } =
        ImmutableDictionary<string, UnitSnapshot>.Empty;
    public BossTimelineSnapshot? BossTimeline { get; init; }
    public Action<BattleCombatBindingRegistry>? ConfigureCombatBindings { get; init; }
}

public sealed record BattleEvent(
    int Tick, string Type, string SourceRuntimeId, string TargetRuntimeId,
    float Value, Vector2I Cell, string Cue);

public sealed class BattleUnitState
{
    public required string RuntimeId { get; init; }
    public required string SourceInstanceId { get; init; }
    public required UnitSnapshot Definition { get; init; }
    public required BattleAttributeSet Attributes { get; init; }
    public required int Team { get; init; }
    public required Vector2I Cell { get; set; }
    public float Health { get; set; }
    public float MaxHealth
    {
        get => Attributes.GetValue(CombatAttribute.MaxHealth);
        set => Attributes.SetBaseValue(CombatAttribute.MaxHealth, value);
    }
    public float Damage
    {
        get => Attributes.GetValue(CombatAttribute.AttackDamage);
        set => Attributes.SetBaseValue(CombatAttribute.AttackDamage, value);
    }
    public float LifeSteal
    {
        get => Attributes.GetValue(CombatAttribute.LifeSteal);
        set => Attributes.SetBaseValue(CombatAttribute.LifeSteal, value);
    }
    public float Armor => Attributes.GetValue(CombatAttribute.Armor);
    public float AttackRange => Attributes.GetValue(CombatAttribute.AttackRange);
    public float HealingPower => Attributes.GetValue(CombatAttribute.HealingPower);
    public int EffectiveAttackTicks => Math.Max(1, Mathf.RoundToInt(
        Definition.AttackTicks / Attributes.GetValue(CombatAttribute.AttackSpeed)));
    public int EffectiveMoveTicks => Math.Max(1, Mathf.RoundToInt(
        Definition.MoveTicks / Attributes.GetValue(CombatAttribute.MoveSpeed)));
    public float Shield { get; set; }
    public int AttackCooldown { get; set; }
    public int MoveCooldown { get; set; }
    public int DisabledTicks { get; set; }
    public int WaitingTicks { get; set; }
    public bool IsTemporary { get; init; }
    public bool IsPersistentRosterHero { get; init; }
    public UnitSnapshot? BehaviorSummon { get; init; }
    public ImmutableArray<StatusRuntimeSnapshot> Statuses { get; set; } = [];
    public string BossPhaseId { get; set; } = string.Empty;
    public BattleUnitMode Mode { get; set; } = BattleUnitMode.Seeking;
    public BattleActionKind LastActionKind { get; set; }
    public string ActionTargetRuntimeId { get; set; } = string.Empty;
    public string ActionTargetName { get; set; } = string.Empty;
    public bool Alive => Health > 0;
}

public sealed record BattleUnitReportSnapshot(
    string RuntimeId,
    string SourceInstanceId,
    string ContentId,
    string DisplayName,
    UnitRole Role,
    int Team,
    bool IsHero,
    bool IsTemporary,
    bool Alive,
    Vector2I FinalCell,
    float FinalHealth,
    float MaxHealth,
    float FinalShield,
    float FinalDamage,
    float DamageDealt,
    float DamageTaken,
    float ShieldAbsorbed,
    float HealingDone,
    int Kills,
    int JoinTick = 0,
    int? DefeatTick = null,
    int AttackActions = 0,
    int EffectiveHealingEvents = 0);

public sealed record BattleResult(
    BattleOutcome Outcome,
    int Ticks,
    string Digest,
    ImmutableArray<BattleUnitReportSnapshot> Units,
    int GoldSpent,
    int SuccessfulTacticalCommandUses = 0,
    RelicBattleTransitionResult? RelicTransition = null,
    BattleIdentity? Identity = null);
