using Godot;

namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class AbilityPresentationSpec : Resource
{
    [Export] public StringName SemanticIcon { get; set; } = new();
    [Export] public StringName Cue { get; set; } = new();
    [Export] public string ReportLabel { get; set; } = string.Empty;
}
