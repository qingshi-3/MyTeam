using Godot;

namespace TowerAutobattler.Relics;

public enum RelicCombatRuleKind
{
    DamageAboveHealthRatio,
    DamageWithinBodyDistance,
    DamageAgainstBoss,
    HealOnAttackCritical,
    HealMissingHealthAtTick
}

[GlobalClass]
public partial class RelicCombatRuleSpec : Resource
{
    [Export] public string RuleId { get; set; } = string.Empty;
    [Export] public RelicCombatRuleKind Kind { get; set; }
    [Export] public float Amount { get; set; }
    [Export] public float Threshold { get; set; }
    [Export] public int Tick { get; set; }
}
