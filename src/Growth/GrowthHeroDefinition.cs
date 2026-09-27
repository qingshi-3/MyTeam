using Godot;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.Growth;

[GlobalClass]
public partial class GrowthHeroDefinition : Resource
{
    [Export] public string ContentId { get; set; } = string.Empty;
    // Godot exports numeric arrays; the compiler validates and freezes typed modes.
    [Export] public int[] ProductionModes { get; set; } = [];
    [Export] public float GrowthRate { get; set; } = .04f;
    [Export] public int ResearchYield { get; set; }
    [Export] public string AscensionId { get; set; } = string.Empty;
    [Export] public string AscensionName { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string AscensionDescription { get; set; } = string.Empty;
    [Export] public AbilityLoadoutDefinition? AscendedLoadout { get; set; }
    [Export] public AbilityLoadoutDefinition? BaseLoadout { get; set; }
    [Export] public string MaterialCategory { get; set; } = string.Empty;
}
