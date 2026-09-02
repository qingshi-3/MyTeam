using Godot;

namespace TowerAutobattler.Attributes;

[GlobalClass]
public partial class AttributeModifierSpec : Resource
{
    [Export] public CombatAttribute Attribute { get; set; }
    [Export] public AttributeModifierOperation Operation { get; set; }
    [Export] public AttributeMagnitudeSpec Magnitude { get; set; } = null!;
    [Export] public int Priority { get; set; }
    [Export] public string SlotId { get; set; } = "default";
}
