using Godot;

namespace TowerAutobattler.Attributes;

[GlobalClass]
public partial class TargetAttributeMagnitudeSpec : AttributeMagnitudeSpec
{
    [Export] public CombatAttribute Attribute { get; set; }
}
