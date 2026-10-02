using Godot;
namespace TowerAutobattler.Statuses;
public enum StatusDamageCondition { TargetHealthAbove, WithinRange, BossTarget, HarmfulStatusCount, MeleeAttack }
[GlobalClass]
public partial class StatusDamageRuleSpec : Resource
{
    [Export] public StatusDamageCondition Condition { get; set; }
    [Export] public float Threshold { get; set; }
    [Export] public float Multiplier { get; set; } = 1;
}
