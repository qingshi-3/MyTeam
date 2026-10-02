using Godot;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class MatrixTargetingParameters : Resource
{
    [Export] public MatrixTargetKind Target { get; set; }
    [Export] public float Range { get; set; } = 20;
    [Export] public float Radius { get; set; }
    [Export] public int MaxTargets { get; set; } = 1;
    [Export] public bool ExcludeSelf { get; set; }
    [Export] public MatrixCondition TargetCondition { get; set; }
    [Export] public MatrixCondition OwnerCondition { get; set; }
    [Export] public float HealthThreshold { get; set; } = .6f;
    [Export] public float RadiusPerCounter { get; set; }
    [Export] public bool CounterOnTarget { get; set; }
    [Export] public bool LandingSide { get; set; }
}
