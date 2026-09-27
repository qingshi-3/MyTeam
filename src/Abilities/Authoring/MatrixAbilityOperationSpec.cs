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
    [Export] public MatrixTargetKind Target { get; set; }
    [Export] public float Range { get; set; } = 20;
    [Export] public float Radius { get; set; }
    [Export] public int MaxTargets { get; set; } = 1;
    [Export] public bool ExcludeSelf { get; set; }
    [Export] public float Amount { get; set; }
    [Export] public float AttackRatio { get; set; }
    [Export] public float OwnerHealthRatio { get; set; }
    [Export] public float TargetHealthRatio { get; set; }
    [Export] public float EventRatio { get; set; }
    [Export] public float EventTargetHealthRatio { get; set; }
    [Export] public float CounterRatio { get; set; }
    [Export] public float RadiusPerCounter { get; set; }
    [Export] public bool CounterOnTarget { get; set; }
    [Export] public float ShieldCap { get; set; }
    [Export] public float TemporaryScale { get; set; } = 1;
    [Export] public float Maximum { get; set; } = 100000;
    [Export] public string Key { get; set; } = "";
    [Export] public string RequiredCounter { get; set; } = "";
    [Export] public float CounterThreshold { get; set; } = 1;
    [Export] public bool ConsumeCounter { get; set; }
    [Export] public float CounterConsumeRatio { get; set; } = 1;
    [Export] public bool LandingSide { get; set; }
    [Export] public bool ResetOnTargetChange { get; set; }
    [Export] public int DurationTicks { get; set; } = 30;
    [Export] public int Count { get; set; } = 1;
    [Export] public int IntervalTicks { get; set; } = 10;
    [Export] public CombatAttribute Attribute { get; set; } = CombatAttribute.AttackDamage;
    [Export] public string ContentId { get; set; } = "";
    [Export] public MatrixEventKind Event { get; set; }
    [Export] public MatrixRelation SourceRelation { get; set; }
    [Export] public MatrixRelation TargetRelation { get; set; }
    [Export] public MatrixCondition TargetCondition { get; set; }
    [Export] public MatrixCondition OwnerCondition { get; set; }
    [Export] public float HealthThreshold { get; set; } = .6f;
    [Export] public float MinimumEventValue { get; set; }
    [Export] public int CooldownTicks { get; set; }
    [Export] public int Every { get; set; } = 1;
    [Export] public bool PerTargetCooldown { get; set; }
    [Export] public bool OncePerTarget { get; set; }
    [Export] public bool OncePerSource { get; set; }
    [Export] public bool OncePerAction { get; set; }
    [Export] public bool OncePerBattle { get; set; }
    [Export] public bool ActiveSkillOnly { get; set; }
    [Export] public bool TemporaryOnly { get; set; }
    [Export] public bool LivingOwnerRequired { get; set; } = true;
    [Export] public bool ClearOnDeath { get; set; }
    [Export] public Godot.Collections.Array<MatrixAbilityOperationSpec> Effects { get; set; } = [];
}
