using Godot;

namespace TowerAutobattler.Attributes;

[GlobalClass]
public partial class AttributeDefinitionSpec : Resource
{
    [Export] public CombatAttribute Attribute { get; set; }
    [Export] public float BaseValue { get; set; }
    [Export] public float Minimum { get; set; }
    [Export] public float Maximum { get; set; } = float.MaxValue;
}
