using Godot;

namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class AbilityLoadoutDefinition : Resource
{
    [Export] public Godot.Collections.Array<AbilityDefinition> Abilities { get; set; } = [];
}
