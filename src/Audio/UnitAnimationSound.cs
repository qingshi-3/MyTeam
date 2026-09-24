using Godot;

namespace TowerAutobattler.Audio;

/// <summary>A presentation cue on a one-based animation frame, independent of damage timing.</summary>
[GlobalClass]
public partial class UnitAnimationSound : Resource
{
    [Export] public string Animation { get; set; } = "attack";
    [Export(PropertyHint.Range, "1,120,1,or_greater")] public int Frame { get; set; } = 1;
    [Export] public FeedbackSound Sound { get; set; } = null!;
}
