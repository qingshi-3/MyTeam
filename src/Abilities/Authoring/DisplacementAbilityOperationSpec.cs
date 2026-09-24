using Godot;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Abilities;

public enum DisplacementKind { Charge = 0, Leap = 1, Blink = 2, Knockback = 3, Pull = 4, Gather = 5 }

[GlobalClass]
public partial class DisplacementAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public DisplacementKind Kind { get; set; }
    [Export] public EffectTargetQuerySpec? TargetQuery { get; set; }
    [Export] public float Distance { get; set; } = 4f;
    [Export] public float DurationSeconds { get; set; } = .4f;
    [Export] public float StopDistance { get; set; } = .1f;
    [Export] public bool BehindTarget { get; set; }
    [Export] public bool ExcludeBoss { get; set; }
    [Export] public float ImpactDamage { get; set; }
    [Export] public float AttackRatio { get; set; }
    [Export] public float ImpactRadius { get; set; }
    [Export] public EffectDamageType DamageType { get; set; }
    [Export] public StatusDefinition? ImpactStatus { get; set; }
    [Export] public float ArcHeight { get; set; } = .8f;
}
