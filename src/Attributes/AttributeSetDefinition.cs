using Godot;

namespace TowerAutobattler.Attributes;

[GlobalClass]
public partial class AttributeSetDefinition : Resource
{
    [Export] public Godot.Collections.Array<AttributeDefinitionSpec> Attributes { get; set; } = [];
}
