using System.Linq;
using Godot;

namespace TowerAutobattler.Vfx;

// A source-centered world plane. Rotate in logical space before projection,
// so an isometric stage never rotates an already-flattened ellipse.
public partial class VfxSweepPlane : Node2D, IVfxTrack, IVfxPlaybackTrack, IVfxSpatialTrack
{
    [Export] public float DefaultRadius { get; set; } = 1.5f;
    // Shape the authored sweep before stage projection; forward reach stays Radius.
    [Export(PropertyHint.Range, "0.1,1,0.01")] public float SideRadiusRatio { get; set; } = 1f;
    private IVfxTrack[] _tracks = [];
    private Vector2? _direction;
    public override void _Ready() => _tracks = GetChildren().OfType<IVfxTrack>().ToArray();
    public void ConfigurePlayback(VfxPlaybackState playback)
    {
        foreach (var track in GetChildren().OfType<IVfxPlaybackTrack>()) track.ConfigurePlayback(playback);
    }
    public void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D stageToInstance)
    {
        var requested = context.Direction ?? context.Target - context.Source;
        _direction ??= requested.LengthSquared() > .000001f ? requested.Normalized() : Vector2.Right;
        var forward = _direction.Value;
        var side = new Vector2(-forward.Y, forward.X);
        float radius = context.Radius > 0 ? context.Radius : DefaultRadius;
        var center = stage.Project(context.Source, false);
        var x = (stage.Project(context.Source + forward * radius, false) - center) / 128;
        var y = (stage.Project(context.Source + side * radius * SideRadiusRatio, false) - center) / 128;
        Transform = stageToInstance * new Transform2D(x, y, center);
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        foreach (var track in _tracks) track.Sample(age, sustained, release, impact, reducedMotion);
    }
}
