using Godot;

namespace TowerAutobattler.Project;

// A complete authored formation. Randomness selects between formations, never arbitrary roles.
[GlobalClass]
public partial class EncounterCompositionDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string PressureSummary { get; set; } = string.Empty;
    [Export] public int RewardGold { get; set; }
    [Export] public int RewardResearch { get; set; }
    [Export] public int RewardMaterials { get; set; }
    // Additional choices never alter a previously committed legacy random encounter.
    [Export] public bool OpponentChoiceOnly { get; set; }
    [Export] public int MinLocalFloor { get; set; }
    [Export] public int MaxLocalFloor { get; set; } = 4;
    [Export] public string[] EnemyIds { get; set; } = [];
    [Export] public Godot.Collections.Array<Vector2I> Cells { get; set; } = [];
}
