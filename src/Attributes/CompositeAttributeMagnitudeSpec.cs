using Godot;

namespace TowerAutobattler.Attributes;

/// <summary>A bounded expression tree, not a script. The root owns capture timing for the entire expression.</summary>
[GlobalClass]
public partial class CompositeAttributeMagnitudeSpec : AttributeMagnitudeSpec
{
    [Export] public AttributeMagnitudeOperation Operation { get; set; }
    [Export] public Godot.Collections.Array<AttributeMagnitudeSpec> Operands { get; set; } = [];
}
