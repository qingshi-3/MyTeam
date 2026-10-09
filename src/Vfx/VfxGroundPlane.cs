using System.Linq;
using Godot;

namespace TowerAutobattler.Vfx;

// Authored 256-wide layers cover the exact logical disk, including non-square board projection.
public partial class VfxGroundPlane : Node2D, IVfxTrack, IVfxSpatialTrack
{
    [Export] public bool FollowDisplayPosition { get; set; }
    [Export] public bool UseLogicalDirection { get; set; }
    [Export] public bool UseConeAngle { get; set; }
    [Export] public float DefaultRadius { get; set; } = 2.5f;
    private IVfxTrack[] _tracks = [];
    public override void _Ready() => _tracks = GetChildren().OfType<IVfxTrack>().ToArray();
    public void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D stageToInstance)
    {
        var logicalCenter = stage.Project(context.Source, true);
        var center = FollowDisplayPosition ? context.TargetDisplayPosition ?? logicalCenter : logicalCenter;
        var radius = context.Radius > 0 ? context.Radius : DefaultRadius;
        var direction=UseLogicalDirection?context.Direction??Vector2.Right:Vector2.Right;
        if(direction.IsZeroApprox())direction=Vector2.Right;
        direction=direction.Normalized();
        var x = (stage.Project(context.Source + direction * radius, true) - logicalCenter) / 128;
        var y = (stage.Project(context.Source + direction.Rotated(Mathf.Pi/2) * radius, true) - logicalCenter) / 128;
        Transform = stageToInstance * new Transform2D(x, y, center);
        if (UseConeAngle)
            foreach (var item in GetChildren().OfType<CanvasItem>())
                if (item.Material is ShaderMaterial material)
                    material.SetShaderParameter("half_angle", Mathf.DegToRad(context.ConeAngleDegrees * .5f));
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        foreach (var track in _tracks) track.Sample(age, sustained, release, impact, reducedMotion);
    }
}
