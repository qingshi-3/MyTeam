using Godot;

namespace TowerAutobattler.Traits;

// Behavior identities are reusable processors, never a dispatch on a concrete trait id.
public enum TraitMechanicKind
{
    HealthThresholdRescue, DamageTakenRamp, RepeatedTargetAttack, WoundedTargetReward,
    ActiveDamageAndRefund, SupportProtection, OwnedSummonBonus, ChillOnAttack,
    PoisonOnAttack, ActiveHitMarks, DeathResource, BarrierRetaliation,
    LowHealthLeech, SharedCastResource
}

[GlobalClass]
public partial class TraitMechanicSpec : Resource
{
    [Export] public TraitMechanicKind Kind { get; set; }
    [Export] public float Amount { get; set; }
    [Export] public float Secondary { get; set; }
    [Export] public float Threshold { get; set; }
    [Export] public float Extra { get; set; }
    [Export] public int Count { get; set; }
    [Export] public int Limit { get; set; }
    [Export] public int DurationTicks { get; set; }
    [Export] public int CooldownTicks { get; set; }
    [Export] public bool Enabled { get; set; }
    [Export] public string SummonContentId { get; set; } = "";
}
