using System.Linq;
using Godot;

namespace TowerAutobattler.Vfx;

// The scene owns the metal claw, cable and joints; combat supplies both endpoints.
public partial class VfxHookTrack : Node2D, IVfxTrack, IVfxSpatialTrack
{
    [Export] public Line2D Cable { get; set; } = null!;
    [Export] public Line2D Shadow { get; set; } = null!;
    [Export] public Node2D Head { get; set; } = null!;
    [Export] public Node2D Joints { get; set; } = null!;
    private Node2D[] _joints = [];
    public override void _Ready() => _joints = Joints.GetChildren().OfType<Node2D>().ToArray();
    public void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D stageToInstance)
    {
        var source = stage.Project(context.Source, false);
        var target = context.TargetDisplayPosition ?? stage.Project(context.Target, false);
        var delta = target - source;
        var direction = delta.IsZeroApprox() ? Vector2.Right : delta.Normalized();
        Cable.Transform = Shadow.Transform = stageToInstance;
        Cable.Points = Shadow.Points = [source, target];
        Cable.Width = 2.5f * stage.UnitScale; Shadow.Width = 6 * stage.UnitScale;
        Head.Transform = stageToInstance * new Transform2D(direction.Angle(), Vector2.One * stage.UnitScale, 0, target);
        for (var i = 0; i < _joints.Length; i++)
        {
            var distance = (i + 1) * 17 * stage.UnitScale;
            _joints[i].Visible = distance < delta.Length() - 12 * stage.UnitScale;
            _joints[i].Transform = stageToInstance * new Transform2D(direction.Angle(), Vector2.One * stage.UnitScale, 0, source + direction * distance);
        }
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion) =>
        Modulate = new Color(1, 1, 1, 1 - release);
}
