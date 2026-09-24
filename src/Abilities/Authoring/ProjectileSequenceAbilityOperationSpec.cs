using Godot;

namespace TowerAutobattler.Abilities;

// Sequential skill hits share the owner's projectile physics and current attack cadence.
[GlobalClass]
public partial class ProjectileSequenceAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public int ShotCount { get; set; } = 3;
    [Export] public int MaxTargets { get; set; } = 1;
    [Export] public float AttackIntervalRatio { get; set; } = .5f;
    [Export] public float AttackDamageMultiplier { get; set; } = .65f;
}
