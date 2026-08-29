using System.Collections.Generic;
using System.Collections.Immutable;
using Godot;
using TowerAutobattler.Components;
using TowerAutobattler.Content;

namespace TowerAutobattler.Battle;

public enum BattleOutcome { Running, PlayerVictory, PlayerDefeat, Timeout }
public enum BattleUnitMode { Seeking, Moving, Waiting, Attacking, Casting, Recovering, Disabled, Defeated }
public enum BattleActionKind { None, Attack, Heal }

public sealed record UnitSnapshot(
    string ContentId, string DisplayName, UnitRole Role, bool IsHero, bool IsBoss,
    float MaxHealth, float Damage, float Range, int AttackTicks, int MoveTicks,
    float Armor, float HealPower, float SplashRadius, float LifeSteal,
    IReadOnlyList<string> Tags, UnitBehaviorSnapshot Behavior);

public sealed record UnitBehaviorSnapshot(
    int SlowOnHitTicks = 0, float AdjacentArmorAura = 0, float AdjacentDamageAura = 0,
    float ExecuteHealthThreshold = 0, float LowHealthDamageBonus = 0,
    float OnDeathDamage = 0, bool PiercingLine = false,
    int PeriodicShieldTicks = 0, float PeriodicShieldAmount = 0,
    int PeriodicSummonTicks = 0, int PeriodicSummonLimit = 0, bool PreferBacklineTargets = false,
    string SummonContentId = "");

public sealed record HeroRuleSnapshot(
    string CommandName, string CommandDescription, int MaxMana, int CommandManaCost, int CommandGoldCost, IHeroCommandRuntime Command,
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
    float HealthRatio = 1f, bool IsTemporary = false, UnitSnapshot? BehaviorSummon = null);

public sealed class BattleConfig
{
    public ulong Seed { get; init; }
    public required IBattleFloorRuleRuntime FloorRule { get; init; }
    public List<BattleSpawn> Spawns { get; init; } = [];
    public required HeroRuleSnapshot HeroRule { get; init; }
    public ModifierSnapshot Modifiers { get; init; } = new();
    public SummonProfiles Summons { get; init; } = new();
    public int EmptyDeploymentSlots { get; init; }
    public int StartingGold { get; init; }
}

public sealed record BattleEvent(
    int Tick, string Type, string SourceRuntimeId, string TargetRuntimeId,
    float Value, Vector2I Cell, string Cue);

public sealed class BattleUnitState
{
    public required string RuntimeId { get; init; }
    public required string SourceInstanceId { get; init; }
    public required UnitSnapshot Definition { get; init; }
    public required int Team { get; init; }
    public required Vector2I Cell { get; set; }
    public float Health { get; set; }
    public float MaxHealth { get; set; }
    public float Damage { get; set; }
    public float LifeSteal { get; set; }
    public float Shield { get; set; }
    public int AttackCooldown { get; set; }
    public int MoveCooldown { get; set; }
    public int DisabledTicks { get; set; }
    public int WaitingTicks { get; set; }
    public bool IsTemporary { get; init; }
    public UnitSnapshot? BehaviorSummon { get; init; }
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
    int SuccessfulHeroCommandUses = 0);

public sealed record HeroCommandUseResult(bool Succeeded, string FailureReason)
{
    public static readonly HeroCommandUseResult Success = new(true, string.Empty);
}
