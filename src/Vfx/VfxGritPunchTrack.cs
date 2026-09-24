using Godot;

namespace TowerAutobattler.Vfx;

public partial class VfxGritPunchTrack : Node2D, IVfxTrack, IVfxPlaybackTrack, IVfxSpatialTrack
{
    [Export] public Sprite2D GroundStrip { get; set; } = null!;
    [Export] public Sprite2D BodyStrip { get; set; } = null!;
    [Export] public bool Released { get; set; }
    private VfxPlaybackState _clock = null!;
    private ShaderMaterial _ground = null!, _body = null!;
    public void ConfigurePlayback(VfxPlaybackState playback) => _clock = playback;
    public override void _Ready()
    {
        _ground = (ShaderMaterial)GroundStrip.Material.Duplicate(); GroundStrip.Material = _ground;
        _body = (ShaderMaterial)BodyStrip.Material.Duplicate(); BodyStrip.Material = _body;
        _ground.SetShaderParameter("released", Released); _body.SetShaderParameter("released", Released);
        _body.SetShaderParameter("body_layer", true);
    }
    public void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D toLocal)
    {
        var direction = (context.Target - context.Source).Normalized();
        if (direction.IsZeroApprox()) direction = Vector2.Right;
        var normal = new Vector2(-direction.Y, direction.X);
        var radius = context.Radius > 0 ? context.Radius : .8f;
        foreach (var strip in new[] { GroundStrip, BodyStrip })
        {
            var ground = strip == GroundStrip;
            var start = stage.Project(context.Source, ground); var end = stage.Project(context.Target, ground);
            var width = (stage.Project(context.Source + normal * radius, ground) - start) * 2;
            strip.Transform = toLocal * new Transform2D((end - start) / 256, width / 256, (start + end) / 2);
        }
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        Visible = Released || release <= 0;
        var progress = Released ? Mathf.Clamp(age / .48f, 0, 1) : Mathf.Clamp(_clock.Age / _clock.CastDuration, 0, 1);
        foreach (var material in new[] { _ground, _body })
        {
            material.SetShaderParameter("progress", progress);
            material.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
        }
        Modulate = new Color(1, 1, 1, 1 - release);
    }
}
