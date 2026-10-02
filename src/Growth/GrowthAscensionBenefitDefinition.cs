using Godot;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.Growth;

// Optional battle benefit. Leaving this resource unassigned preserves the base loadout.
[GlobalClass]
public partial class GrowthAscensionBenefitDefinition : Resource
{
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
    [Export] public AbilityLoadoutDefinition? BattleLoadout { get; set; }
}
