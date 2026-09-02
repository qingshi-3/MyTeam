using Godot;

namespace TowerAutobattler.Effects;

[GlobalClass]
public partial class EffectPresentationSpec : Resource
{
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public string ReportLabel { get; set; } = string.Empty;
    [Export] public StringName Cue { get; set; } = new();
}
