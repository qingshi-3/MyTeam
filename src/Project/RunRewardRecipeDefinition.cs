using Godot;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class RunRewardRecipeDefinition : Resource
{
    [Export] public string StableId { get; set; } = "";
    [Export] public int Gold { get; set; }
    [Export] public RunOfferDefinition[] Groups { get; set; } = [];
}
