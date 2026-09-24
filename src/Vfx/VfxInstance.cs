using System.Linq;
using Godot;

namespace TowerAutobattler.Vfx;

public partial class VfxInstance : Node2D
{
    private VfxDefinition _definition = null!;
    private IVfxTrack[] _tracks = [];
    private float _age;
    public VfxPlaybackState Playback { get; private set; } = null!;
    private float _release = -1;
    private float _impact;
    public VfxContext Context { get; set; }
    public void Bind(VfxDefinition definition, VfxContext context)
    {
        _definition = definition;
        Context = context;
        Playback = new(definition, context);
        _tracks = GetChildren().OfType<IVfxTrack>().ToArray();
        foreach (var track in _tracks.OfType<IVfxPlaybackTrack>()) track.ConfigurePlayback(Playback);
    }
    public void Impact() => _impact = 1;
    public void Signal(VfxStartCue cue) => Playback.Signal(cue);
    public void End(VfxEndReason reason)
    {
        if (_release >= 0) return;
        _release = 0;
        Playback.Cancel();
        if (reason == VfxEndReason.Depleted) Impact();
    }
    public bool Advance(float seconds, IVfxStage stage, bool reducedMotion)
    {
        _age += seconds;
        Playback.UpdateContext(Context);
        Playback.Advance(seconds);
        _impact = Mathf.Max(0, _impact - seconds * 6);
        if (_release >= 0) _release += seconds / .25f;
        Position = Context.TargetDisplayPosition ?? stage.Project(Context.Target, _definition.Ground);
        var size = Context.Radius > 0 ? stage.RadiusPixels(Context.Radius) * 2 : _definition.Size * stage.UnitScale;
        Scale = Vector2.One * size / 256;
        if (_definition.Ground && _definition.FlattenGround) Scale *= new Vector2(1, .6f);
        if (_definition.FaceDirection)
        {
            var heading = Context.Direction ?? (Context.Target - Context.Source);
            if (heading.IsZeroApprox()) heading = Vector2.Right;
            // Project the heading through the same board transform as the arrow position.
            Rotation = (stage.Project(Context.Target + heading, _definition.Ground) - Position).Angle();
        }
        if (_definition.StretchBetween)
        {
            Position = stage.Project(Context.Source, false);
            var direction = stage.Project(Context.Target, false) - Position;
            Rotation = direction.Angle();
            Scale = new Vector2(direction.Length() / 256, _definition.Size * stage.UnitScale / 256 * Playback.Parameters.WidthScale);
        }
        foreach (var track in _tracks)
        {
            if (track is IVfxSpatialTrack spatialTrack)
                spatialTrack.SetSpatialContext(Context, stage, Transform.AffineInverse());
            if (track is IVfxSourceTrack sourceTrack)
                sourceTrack.SetSource(Transform.AffineInverse() * stage.Project(Context.Source, false),
                    stage.UnitScale / Mathf.Max(.001f, Scale.X));
            track.Sample(_definition.Persistent ? _age * Playback.Parameters.MotionSpeed : _age,
                _definition.Persistent, Mathf.Clamp(_release, 0, 1), _impact, reducedMotion);
        }
        return _release < 1 && !Playback.Finished;
    }
}
