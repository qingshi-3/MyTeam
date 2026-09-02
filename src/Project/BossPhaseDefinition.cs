using Godot;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class BossPhaseDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export(PropertyHint.Range, "0,1,0.01")] public float StartHealthRatio { get; set; } = 1f;
    [Export] public AbilityLoadoutDefinition? AbilityLoadout { get; set; }
}
