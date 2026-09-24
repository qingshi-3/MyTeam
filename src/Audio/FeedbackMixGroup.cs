using Godot;

namespace TowerAutobattler.Audio;

// Shared immutable mix policy. Live admission and envelope state belongs to each mixer.
[GlobalClass]
public partial class FeedbackMixGroup : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public float MinimumInterval { get; set; }
    [Export] public int MaximumVoices { get; set; } = 2;
    [Export] public bool DuckUnderHighlights { get; set; }
    [Export] public bool IsHighlight { get; set; }
}
