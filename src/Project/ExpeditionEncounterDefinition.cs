using Godot;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class ExpeditionEncounterDefinition : Resource
{
    [Export] public string StableId { get; set; } = "";
    [Export] public EncounterDefinition? Encounter { get; set; }
    [Export] public string CompositionId { get; set; } = "";
    [Export] public float FinalHealthMultiplier { get; set; } = 1;
    [Export] public float FinalDamageMultiplier { get; set; } = 1;
    [Export] public bool OpeningCandidate { get; set; }
    [Export] public RunRewardRecipeDefinition? Reward { get; set; }
}
