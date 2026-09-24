using System.Linq;
using Godot;

namespace TowerAutobattler.Vfx;

// A single real source-to-target connection. Endpoint flashes keep world scale
// when the connection length changes; there are no invented chained recipients.
public partial class VfxArcLinkTrack : Node2D, IVfxTrack, IVfxPlaybackTrack, IVfxSpatialTrack
{
    private VfxPlaybackState? _playback;
    private Node2D _link = null!;
    private Node2D _source = null!;
    private Node2D _target = null!;
    private IVfxTrack[] _linkTracks = [];
    private IVfxTrack[] _endpointTracks = [];
    private bool _hasLength;
    public void ConfigurePlayback(VfxPlaybackState playback)
    {
        _playback = playback;
        foreach (var child in GetChildren())
        {
            if (child is IVfxPlaybackTrack track) track.ConfigurePlayback(playback);
            if (child.Name == "Link")
                foreach (var link in child.GetChildren().OfType<IVfxPlaybackTrack>()) link.ConfigurePlayback(playback);
        }
    }
    public override void _Ready()
    {
        _link = GetNode<Node2D>("Link");
        _source = GetNode<Node2D>("SourceContact");
        _target = GetNode<Node2D>("TargetContact");
        _linkTracks = _link.GetChildren().OfType<IVfxTrack>().ToArray();
        _endpointTracks = GetChildren().OfType<IVfxTrack>().ToArray();
    }
    public void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D stageToInstance)
    {
        var from = stageToInstance * stage.Project(context.Source, false);
        var to = stageToInstance * (context.TargetDisplayPosition ?? stage.Project(context.Target, false));
        var direction = to - from;
        _hasLength = direction.LengthSquared() > .0001f;
        float unit = stageToInstance.BasisXform(Vector2.Right * stage.UnitScale).Length();
        float width = _playback?.Parameters.WidthScale ?? 1;
        var tangent = _hasLength ? direction.Normalized() : Vector2.Right;
        _link.Transform = new Transform2D(direction / 256,
            new Vector2(-tangent.Y, tangent.X) * unit * width * .55f, from);
        _source.Position = from;
        _target.Position = to;
        _source.Scale = _target.Scale = Vector2.One * unit;
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        _link.Visible = _hasLength;
        foreach (var track in _linkTracks) track.Sample(age, sustained, release, impact, reducedMotion);
        foreach (var track in _endpointTracks) track.Sample(age, sustained, release, impact, reducedMotion);
    }
}
