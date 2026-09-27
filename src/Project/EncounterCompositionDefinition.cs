using Godot;

namespace TowerAutobattler.Project;

// A complete authored formation. Randomness selects between formations, never arbitrary roles.
[GlobalClass]
public partial class EncounterCompositionDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public int MinLocalFloor { get; set; }
    [Export] public int MaxLocalFloor { get; set; } = 4;
    [Export] public string[] EnemyIds { get; set; } = [];
    [Export] public Godot.Collections.Array<Vector2I> Cells { get; set; } = [];
}
