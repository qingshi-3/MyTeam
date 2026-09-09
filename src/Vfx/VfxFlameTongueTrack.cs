using Godot;

namespace TowerAutobattler.Vfx;

// A fixed fire root with continuously advected outer flame; no particle-cycle reset.
public partial class VfxFlameTongueTrack : Sprite2D, IVfxTrack, IVfxPlaybackTrack
{
    [Export] public Vector2 PixelSize { get; set; } = new(48, 100);
    [Export] public float Phase { get; set; }
    [Export] public float FlowSpeed { get; set; } = 1;
    private ShaderMaterial _shader = null!;
    private VfxPlaybackState? _playback;
    private bool _started;
    public void ConfigurePlayback(VfxPlaybackState playback) => _playback = playback;
    public override void _Ready()
    {
        Offset = new(0, -Texture.GetHeight() * .5f);
        Material = _shader = (ShaderMaterial)Material.Duplicate();
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        Visible = release < 1 && (_started || _playback?.Cancelled != true);
        if (!Visible) return;
        _started |= age > 0;
        float time = reducedMotion ? Phase : age * FlowSpeed + Phase;
        float height = reducedMotion ? 1 : .92f + .10f * Mathf.Sin(time * 4.7f) + .06f * Mathf.Sin(time * 7.1f);
        Scale = PixelSize / Texture.GetSize() * new Vector2(1, height) * (_playback?.ParticleScale ?? 1);
        _shader.SetShaderParameter("age", time);
        _shader.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
        _shader.SetShaderParameter("visibility", Mathf.Clamp(age / .13f, 0, 1) * (1 - release));
    }
}
