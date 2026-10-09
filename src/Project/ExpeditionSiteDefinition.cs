using Godot;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class ExpeditionSiteDefinition : Resource
{
    [Export] public string StableId { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public TowerNodeType Kind { get; set; }
    [Export] public int CountPerBiome { get; set; } = 1;
    [Export] public int ActionCost { get; set; } = 1;
    [Export] public RunOfferDefinition? Offer { get; set; }
    [Export] public RunOfferDefinition[] OfferRecipes { get; set; } = [];
}
