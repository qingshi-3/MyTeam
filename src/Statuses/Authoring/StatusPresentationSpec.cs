using Godot;

namespace TowerAutobattler.Statuses;

[GlobalClass]
public partial class StatusPresentationSpec : Resource
{
    [Export] public StringName SemanticIcon { get; set; } = new();
    [Export] public StringName ExecutedCue { get; set; } = new();
    [Export] public StringName OnActiveCue { get; set; } = new();
    [Export] public StringName WhileActiveCue { get; set; } = new();
    [Export] public StringName RemovedCue { get; set; } = new();
    [Export] public string ReportLabel { get; set; } = string.Empty;
}
