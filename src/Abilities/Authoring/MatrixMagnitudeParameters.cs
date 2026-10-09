using Godot;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class MatrixMagnitudeParameters : Resource
{
    [Export] public float Amount { get; set; }
    [Export] public float SpellPowerRatio { get; set; }
    [Export] public float AttackRatio { get; set; }
    [Export] public float OwnerHealthRatio { get; set; }
    [Export] public float TargetHealthRatio { get; set; }
    [Export] public float EventRatio { get; set; }
    [Export] public float EventTargetHealthRatio { get; set; }
    [Export] public float CounterRatio { get; set; }
    [Export] public float ShieldCap { get; set; }
    [Export] public float TemporaryScale { get; set; } = 1;
    [Export] public float Maximum { get; set; } = 100000;
}
