using System.Linq;
using Godot;

namespace TowerAutobattler.Vfx;

// Authored sublayers share one local cue time and transform. No timers or
// deferred callbacks survive an owning effect's cancellation.
public partial class VfxTimedGroup : Node2D, IVfxTrack, IVfxPlaybackTrack, IVfxSourceTrack
{
    [Export] public float Delay { get; set; }
    [Export] public bool DelayFollowsCast { get; set; }
    [Export] public VfxStartCue StartCue { get; set; }
    [Export] public VfxClockDomain Clock { get; set; }
    [Export] public bool UntilFired { get; set; }
    [Export] public bool AtSource { get; set; }
    [Export] public Vector2 SourceOffset { get; set; }
    private IVfxTrack[] _tracks = [];
    private VfxPlaybackState? _playback;
    private Vector2 _authoredScale;
    private bool _started;
    public override void _Ready()
    {
        _tracks = GetChildren().OfType<IVfxTrack>().ToArray();
        _authoredScale = Scale;
    }
    public void ConfigurePlayback(VfxPlaybackState playback)
    {
        _playback = playback;
        foreach (var track in GetChildren().OfType<IVfxPlaybackTrack>()) track.ConfigurePlayback(playback);
    }
    public void SetSource(Vector2 localSource, float localUnitScale)
    {
        if (AtSource) { Position = localSource + SourceOffset * localUnitScale; Scale = _authoredScale * localUnitScale; }
        foreach (var track in _tracks.OfType<IVfxSourceTrack>())
            track.SetSource(Transform.AffineInverse() * localSource, localUnitScale / Mathf.Max(.001f, Scale.X));
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        // Cancellation may fade an already-started group, but must never
        // trigger a pending impact during the owner's release interval.
        if (_playback is { } playback)
        {
            age = playback.Since(StartCue);
            if (age >= 0) age *= Clock == VfxClockDomain.Cast ? playback.Parameters.CastSpeed
                : Clock == VfxClockDomain.Motion ? playback.Parameters.MotionSpeed : 1;
            if (UntilFired && Clock == VfxClockDomain.Cast && playback.FiredAt is null && playback.CastDuration > 0)
                age = Mathf.Min(age, playback.CastDuration * playback.Parameters.CastSpeed - .0001f);
        }
        float delay = Delay / (DelayFollowsCast && Clock != VfxClockDomain.Cast ? _playback?.Parameters.CastSpeed ?? 1 : 1);
        Visible = age >= delay && (_started || release <= 0 && _playback?.Cancelled != true) && !(UntilFired && _playback?.FiredAt is not null);
        if (!Visible) return;
        _started = true;
        foreach (var track in _tracks)
            track.Sample(age - delay, sustained, release, impact, reducedMotion);
    }
}
