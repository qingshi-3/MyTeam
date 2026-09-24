using Godot;

namespace TowerAutobattler.Audio;

[GlobalClass]
public partial class FeedbackSound : Resource
{
    [Export] public string Cue { get; set; } = "";
    [Export] public AudioStream? Stream { get; set; }
    [Export] public float VolumeDb { get; set; } = -4;
    [Export] public float MinimumInterval { get; set; } = .12f;
    [Export] public int MaximumVoices { get; set; } = 2;
    [Export] public int Priority { get; set; } = 1;
    [Export] public bool VaryPitch { get; set; } = true;
    [Export] public FeedbackMixGroup? MixGroup { get; set; }
    // Zero preserves the source duration. Capped tails fade instead of hard-cutting.
    [Export] public float MaximumDuration { get; set; }
}
