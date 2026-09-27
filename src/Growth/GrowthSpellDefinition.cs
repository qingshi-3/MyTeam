using Godot;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.Growth;

[GlobalClass]
public partial class GrowthSpellDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
    [Export] public int ResearchCost { get; set; } = 4;
    [Export] public AbilityLoadoutDefinition? BattleLoadout { get; set; }
}
