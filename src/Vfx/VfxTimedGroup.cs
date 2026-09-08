using System.Linq;
using Godot;

namespace TowerAutobattler.Vfx;

// Authored sublayers share one local cue time and transform. No timers or
// deferred callbacks survive an owning effect's cancellation.
public partial class VfxTimedGroup : Node2D, IVfxTrack
{
    [Export] public float Delay { get; set; }
    private IVfxTrack[] _tracks = [];
    private bool _started;
    public override void _Ready() => _tracks = GetChildren().OfType<IVfxTrack>().ToArray();
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        // Cancellation may fade an already-started group, but must never
        // trigger a pending impact during the owner's release interval.
        Visible = age >= Delay && (_started || release <= 0);
        if (!Visible) return;
        _started = true;
        foreach (var track in _tracks)
            track.Sample(age - Delay, sustained, release, impact, reducedMotion);
    }
}
