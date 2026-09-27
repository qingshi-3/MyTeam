using Godot;

namespace TowerAutobattler.Growth;

[GlobalClass]
public partial class GrowthRulesDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public GrowthHeroDefinition[] Heroes { get; set; } = [];
    [Export] public string[] FirstDiscoveryPool { get; set; } = [];
    [Export] public string[] AdvancedDiscoveryPool { get; set; } = [];
    [Export] public GrowthSpellDefinition[] Spells { get; set; } = [];
    [Export] public int MaterialsPerNode { get; set; } = 1;
    [Export] public int AscensionCost { get; set; } = 4;
}
