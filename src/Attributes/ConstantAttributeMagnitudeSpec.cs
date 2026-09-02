using Godot;

namespace TowerAutobattler.Attributes;

[GlobalClass]
public partial class ConstantAttributeMagnitudeSpec : AttributeMagnitudeSpec
{
    [Export] public float Value { get; set; }
}
