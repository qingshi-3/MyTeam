using Godot;

namespace TowerAutobattler.Vfx;

// Geometry is projected from authoritative endpoints/body radius; all motion uses the player clock.
public partial class VfxTrampleTrack : Node2D, IVfxTrack, IVfxPlaybackTrack, IVfxSpatialTrack
{
    [Export] public bool Rush { get; set; }
    [Export] public Sprite2D Surface { get; set; } = null!;
    private ShaderMaterial _material = null!;
    private VfxPlaybackState _playback = null!;
    public void ConfigurePlayback(VfxPlaybackState playback) => _playback = playback;
    public override void _Ready()
    {
        _material = (ShaderMaterial)Surface.Material.Duplicate();
        Surface.Material = _material;
        _material.SetShaderParameter("rush", Rush);
    }
    public void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D stageToInstance)
    {
        var direction = context.Direction ?? (context.Target - context.Source).Normalized();
        if (direction.IsZeroApprox()) direction = Vector2.Right;
        var normal = new Vector2(-direction.Y, direction.X);
        var radius = context.Radius > 0 ? context.Radius : .9f;
        var anchor = stage.Project(context.Source, true);
        var along = stage.Project(context.Source + direction, true) - anchor;
        var across = stage.Project(context.Source + normal, true) - anchor;
        var length = Rush ? radius * 4 : context.Source.DistanceTo(context.Target);
        var center = Rush ? (context.TargetDisplayPosition ?? anchor) - along * radius * .6f :
            (anchor + stage.Project(context.Target, true)) / 2;
        Surface.Transform = stageToInstance * new Transform2D(along * length / 256,
            across * radius * 2 / 256, center);
        _material.SetShaderParameter("length_ratio", length / (radius * 2));
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        _material.SetShaderParameter("age", reducedMotion ? 0 : age);
        _material.SetShaderParameter("progress", Mathf.Clamp(_playback.Age / Mathf.Max(.01f, _playback.CastDuration), 0, 1));
        Modulate = new Color(1, 1, 1, 1 - Mathf.Clamp(release * 5, 0, 1));
    }
}
