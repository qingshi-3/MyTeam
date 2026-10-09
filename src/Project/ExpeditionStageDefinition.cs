using Godot;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class ExpeditionStageDefinition : Resource
{
    [Export] public string StableId { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public int Actions { get; set; } = 6;
    [Export] public bool AdvancedDiscovery { get; set; }
    [Export] public ExpeditionEncounterDefinition? Boss { get; set; }
}
