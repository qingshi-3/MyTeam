using Godot;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class ExpeditionBiomeDefinition : Resource
{
    [Export] public string StableId { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public Color Color { get; set; } = Colors.ForestGreen;
    [Export] public ExpeditionEncounterDefinition[] Encounters { get; set; } = [];
    [Export] public int[] RecruitmentWeights { get; set; } = [];
}
