using Godot;

namespace TowerAutobattler.Attributes;

[GlobalClass]
public partial class SourceAttributeMagnitudeSpec : AttributeMagnitudeSpec
{
    [Export] public CombatAttribute Attribute { get; set; }
}
