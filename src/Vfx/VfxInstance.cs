using System.Linq;
using Godot;

namespace TowerAutobattler.Vfx;

public partial class VfxInstance : Node2D
{
    private VfxDefinition _definition = null!;
    private IVfxTrack[] _tracks = [];
    private float _age;
    private float _release = -1;
    private float _impact;
    public VfxContext Context { get; set; }
    public void Bind(VfxDefinition definition, VfxContext context)
    {
        _definition = definition;
        Context = context;
        _tracks = GetChildren().OfType<IVfxTrack>().ToArray();
    }
    public void Impact() => _impact = 1;
    public void End(VfxEndReason reason)
    {
        if (_release >= 0) return;
        _release = 0;
        if (reason == VfxEndReason.Depleted) Impact();
    }
    public bool Advance(float seconds, IVfxStage stage, bool reducedMotion)
    {
        _age += seconds;
        _impact = Mathf.Max(0, _impact - seconds * 6);
        if (_release >= 0) _release += seconds / .25f;
        Position = stage.Project(Context.Target, _definition.Ground);
        var size = Context.Radius > 0 ? stage.RadiusPixels(Context.Radius) * 2 : _definition.Size * stage.UnitScale;
        Scale = Vector2.One * size / 256;
        if (_definition.Ground && _definition.FlattenGround) Scale *= new Vector2(1, .6f);
        if (_definition.StretchBetween)
        {
            Position = stage.Project(Context.Source, false);
            var direction = stage.Project(Context.Target, false) - Position;
            Rotation = direction.Angle();
            Scale = new Vector2(direction.Length() / 256, _definition.Size * stage.UnitScale / 256);
        }
        foreach (var track in _tracks)
        {
            if (track is IVfxSourceTrack sourceTrack)
                sourceTrack.SetSource(Transform.AffineInverse() * stage.Project(Context.Source, false),
                    stage.UnitScale / Mathf.Max(.001f, Scale.X));
            track.Sample(_age, _definition.Persistent, Mathf.Max(0, _release), _impact, reducedMotion);
        }
        return _release < 1 && (_definition.Persistent || _age < _definition.Duration);
    }
}
