using System.Linq;
using Godot;

namespace TowerAutobattler.Vfx;

// Authored 256-wide layers cover the exact logical disk, including non-square board projection.
public partial class VfxGroundPlane : Node2D, IVfxTrack, IVfxSpatialTrack
{
    [Export] public float DefaultRadius { get; set; } = 2.5f;
    private IVfxTrack[] _tracks = [];
    public override void _Ready() => _tracks = GetChildren().OfType<IVfxTrack>().ToArray();
    public void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D stageToInstance)
    {
        var center = stage.Project(context.Source, true);
        var radius = context.Radius > 0 ? context.Radius : DefaultRadius;
        var x = (stage.Project(context.Source + Vector2.Right * radius, true) - center) / 128;
        var y = (stage.Project(context.Source + Vector2.Down * radius, true) - center) / 128;
        Transform = stageToInstance * new Transform2D(x, y, center);
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        foreach (var track in _tracks) track.Sample(age, sustained, release, impact, reducedMotion);
    }
}
