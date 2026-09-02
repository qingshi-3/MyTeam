using Godot;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.TacticalCommands;

[GlobalClass]
public partial class TacticalCommandDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export(PropertyHint.Range, "1,3,1")] public int TacticalPointCost { get; set; } = 1;
    [Export] public AbilityLoadoutDefinition AbilityLoadout { get; set; } = null!;
    [Export] public string PrimaryAbilityId { get; set; } = string.Empty;
}
