using Godot;

namespace TowerAutobattler.Components;

[GlobalClass]
public partial class UnitBehaviorComponent : Node
{
    [Export] public int SlowOnHitTicks { get; set; }
    [Export] public float AdjacentArmorAura { get; set; }
    [Export] public float AdjacentDamageAura { get; set; }
    [Export] public float ExecuteHealthThreshold { get; set; }
    [Export] public float LowHealthDamageBonus { get; set; }
    [Export] public float OnDeathDamage { get; set; }
    [Export] public bool PiercingLine { get; set; }
    [Export] public int PeriodicShieldTicks { get; set; }
    [Export] public float PeriodicShieldAmount { get; set; }
    [Export] public int PeriodicSummonTicks { get; set; }
    [Export] public int PeriodicSummonLimit { get; set; }
    [Export] public bool PreferBacklineTargets { get; set; }
    [Export] public string SummonContentId { get; set; } = string.Empty;
}
