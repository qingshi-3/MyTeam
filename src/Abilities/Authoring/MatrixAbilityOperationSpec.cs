using Godot;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Abilities;

// Composable combat atoms. Content identities never select runtime behavior.
public enum MatrixOperationKind
{
    Damage, Heal, Shield, Mana, Chill, Poison, Ember, Counter, ClearCounter,
    PayHealth, ConsumeShield, ConsumePoison, TransferPoison, TimedAttribute,
    NextSkillBoost, NextAttackBoost, ExtendSummon, Summon, RegisterReaction,
    Delay, DashStrike, DeathGift, EchoDeathGift, Taunt, SkillVolley, LineDamage, ScalePoison, ExtraShot, Sequence,
    PermanentAttribute
}
public enum MatrixTargetKind
{
    Self, CurrentEnemy, NearestEnemies, LowestHealthEnemies, MostPoisonEnemies,
    MostEmberEnemies, DenseEnemies, LowestHealthAllies, NearestAllies,
    NearestCaster, OwnedSummons, TemporaryAllies, EventSource, EventTarget,
    EventNearbyEnemies, EventNearbyAllies, LowestManaAllies, FrontAlly, OtherEnemy, EventVictim,
    NearWoundedAllyEnemies, NearestSummoner, NearEventSourceAllies, FrontEnemies, NearestWoundedAlly, NearestBoostableCaster
}
public enum MatrixEventKind
{
    Tick, AttackHit, SkillHit, HealthLost, ShieldReceived, ShieldBroken, Healing,
    ManaCast, Death, Control, PoisonTransferred, EmberDetonated, SummonHit, PoisonApplied
}
public enum MatrixRelation { Any, Owner, Ally, OtherAlly, Enemy, OwnedSummon }
public enum MatrixCondition { None, Poisoned, Chilled, EmberMarked, Controlled, LowHealth, Shielded, Unshielded, Frozen }

[GlobalClass]
public partial class MatrixAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public MatrixOperationKind Kind { get; set; }
    [Export] public MatrixTargetingParameters? Targeting { get; set; }
    [Export] public MatrixMagnitudeParameters? Magnitude { get; set; }
    [Export] public MatrixExecutionParameters? Execution { get; set; }
    [Export] public MatrixReactionParameters? Reaction { get; set; }
    // CLR arrays avoid native Array<Self> script retention at engine shutdown.
    [Export] public MatrixAbilityOperationSpec[] Effects { get; set; } = [];

    // C# construction/compiler compatibility. Inspector and serialized resources use
    // focused parameter blocks; reading omitted blocks does not allocate Resources.
    public MatrixTargetKind Target { get => Targeting?.Target ?? default; set => (Targeting ??= new()).Target = value; }
    public float Range { get => Targeting?.Range ?? 20; set => (Targeting ??= new()).Range = value; }
    public float Radius { get => Targeting?.Radius ?? 0; set => (Targeting ??= new()).Radius = value; }
    public int MaxTargets { get => Targeting?.MaxTargets ?? 1; set => (Targeting ??= new()).MaxTargets = value; }
    public bool ExcludeSelf { get => Targeting?.ExcludeSelf ?? false; set => (Targeting ??= new()).ExcludeSelf = value; }
    public MatrixCondition TargetCondition { get => Targeting?.TargetCondition ?? default; set => (Targeting ??= new()).TargetCondition = value; }
    public MatrixCondition OwnerCondition { get => Targeting?.OwnerCondition ?? default; set => (Targeting ??= new()).OwnerCondition = value; }
    public float HealthThreshold { get => Targeting?.HealthThreshold ?? .6f; set => (Targeting ??= new()).HealthThreshold = value; }
    public float RadiusPerCounter { get => Targeting?.RadiusPerCounter ?? 0; set => (Targeting ??= new()).RadiusPerCounter = value; }
    public bool CounterOnTarget { get => Targeting?.CounterOnTarget ?? false; set => (Targeting ??= new()).CounterOnTarget = value; }
    public bool LandingSide { get => Targeting?.LandingSide ?? false; set => (Targeting ??= new()).LandingSide = value; }
    public float Amount { get => Magnitude?.Amount ?? 0; set => (Magnitude ??= new()).Amount = value; }
    public float AttackRatio { get => Magnitude?.AttackRatio ?? 0; set => (Magnitude ??= new()).AttackRatio = value; }
    public float OwnerHealthRatio { get => Magnitude?.OwnerHealthRatio ?? 0; set => (Magnitude ??= new()).OwnerHealthRatio = value; }
    public float TargetHealthRatio { get => Magnitude?.TargetHealthRatio ?? 0; set => (Magnitude ??= new()).TargetHealthRatio = value; }
    public float EventRatio { get => Magnitude?.EventRatio ?? 0; set => (Magnitude ??= new()).EventRatio = value; }
    public float EventTargetHealthRatio { get => Magnitude?.EventTargetHealthRatio ?? 0; set => (Magnitude ??= new()).EventTargetHealthRatio = value; }
    public float CounterRatio { get => Magnitude?.CounterRatio ?? 0; set => (Magnitude ??= new()).CounterRatio = value; }
    public float ShieldCap { get => Magnitude?.ShieldCap ?? 0; set => (Magnitude ??= new()).ShieldCap = value; }
    public float TemporaryScale { get => Magnitude?.TemporaryScale ?? 1; set => (Magnitude ??= new()).TemporaryScale = value; }
    public float Maximum { get => Magnitude?.Maximum ?? 100000; set => (Magnitude ??= new()).Maximum = value; }
    public string Key { get => Execution?.Key ?? ""; set => (Execution ??= new()).Key = value; }
    public string RequiredCounter { get => Execution?.RequiredCounter ?? ""; set => (Execution ??= new()).RequiredCounter = value; }
    public float CounterThreshold { get => Execution?.CounterThreshold ?? 1; set => (Execution ??= new()).CounterThreshold = value; }
    public bool ConsumeCounter { get => Execution?.ConsumeCounter ?? false; set => (Execution ??= new()).ConsumeCounter = value; }
    public float CounterConsumeRatio { get => Execution?.CounterConsumeRatio ?? 1; set => (Execution ??= new()).CounterConsumeRatio = value; }
    public bool ResetOnTargetChange { get => Execution?.ResetOnTargetChange ?? false; set => (Execution ??= new()).ResetOnTargetChange = value; }
    public int DurationTicks { get => Execution?.DurationTicks ?? 30; set => (Execution ??= new()).DurationTicks = value; }
    public int Count { get => Execution?.Count ?? 1; set => (Execution ??= new()).Count = value; }
    public int IntervalTicks { get => Execution?.IntervalTicks ?? 10; set => (Execution ??= new()).IntervalTicks = value; }
    public CombatAttribute Attribute { get => Execution?.Attribute ?? CombatAttribute.AttackDamage; set => (Execution ??= new()).Attribute = value; }
    public string ContentId { get => Execution?.ContentId ?? ""; set => (Execution ??= new()).ContentId = value; }
    public bool ClearOnDeath { get => Execution?.ClearOnDeath ?? false; set => (Execution ??= new()).ClearOnDeath = value; }
    public MatrixEventKind Event { get => Reaction?.Event ?? default; set => (Reaction ??= new()).Event = value; }
    public MatrixRelation SourceRelation { get => Reaction?.SourceRelation ?? default; set => (Reaction ??= new()).SourceRelation = value; }
    public MatrixRelation TargetRelation { get => Reaction?.TargetRelation ?? default; set => (Reaction ??= new()).TargetRelation = value; }
    public float MinimumEventValue { get => Reaction?.MinimumEventValue ?? 0; set => (Reaction ??= new()).MinimumEventValue = value; }
    public int CooldownTicks { get => Reaction?.CooldownTicks ?? 0; set => (Reaction ??= new()).CooldownTicks = value; }
    public int Every { get => Reaction?.Every ?? 1; set => (Reaction ??= new()).Every = value; }
    public bool PerTargetCooldown { get => Reaction?.PerTargetCooldown ?? false; set => (Reaction ??= new()).PerTargetCooldown = value; }
    public bool OncePerTarget { get => Reaction?.OncePerTarget ?? false; set => (Reaction ??= new()).OncePerTarget = value; }
    public bool OncePerSource { get => Reaction?.OncePerSource ?? false; set => (Reaction ??= new()).OncePerSource = value; }
    public bool OncePerAction { get => Reaction?.OncePerAction ?? false; set => (Reaction ??= new()).OncePerAction = value; }
    public bool OncePerBattle { get => Reaction?.OncePerBattle ?? false; set => (Reaction ??= new()).OncePerBattle = value; }
    public bool ActiveSkillOnly { get => Reaction?.ActiveSkillOnly ?? false; set => (Reaction ??= new()).ActiveSkillOnly = value; }
    public bool TemporaryOnly { get => Reaction?.TemporaryOnly ?? false; set => (Reaction ??= new()).TemporaryOnly = value; }
    public bool LivingOwnerRequired { get => Reaction?.LivingOwnerRequired ?? true; set => (Reaction ??= new()).LivingOwnerRequired = value; }
}
