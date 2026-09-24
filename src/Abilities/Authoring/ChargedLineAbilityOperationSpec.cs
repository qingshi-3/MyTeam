using Godot;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Abilities;

public enum ChargedLineDelivery { Projectile, Beam }

// Shared authoring only. Locked trajectory, countdown and hits belong to the battle.
[GlobalClass]
public partial class ChargedLineAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public ChargedLineDelivery Delivery { get; set; }
    [Export] public EffectDamageType DamageType { get; set; }
    [Export] public float Range { get; set; } = 16;
    [Export] public float Radius { get; set; } = .18f;
    [Export] public int ChargeTicks { get; set; } = 12;
    [Export] public float AttackMultiplier { get; set; } = 1.2f;
    [Export] public int MaximumHits { get; set; } = 2;
    [Export] public float SubsequentHitMultiplier { get; set; } = 2f / 3f;
    [Export] public float ProjectileSpeed { get; set; } = 12;
    [Export] public bool HoldPosition { get; set; } = true;
    [Export] public string ChargeVfx { get; set; } = "piercing_arrow_charge";
    [Export] public string ReleaseVfx { get; set; } = "piercing_arrow";
}
