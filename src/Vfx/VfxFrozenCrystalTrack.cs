using Godot;

namespace TowerAutobattler.Vfx;

// An authored prism grows from a fixed base, holds while the status exists,
// and fractures only on release. It does not reuse the burst's expiry timer.
public partial class VfxFrozenCrystalTrack : Sprite2D, IVfxTrack, IVfxPlaybackTrack
{
    [Export] public Vector2 PixelSize { get; set; } = new(60, 110);
    [Export] public float Delay { get; set; }
    [Export] public float GrowthDuration { get; set; } = .24f;
    [Export] public float GlintPhase { get; set; }
    private VfxPlaybackState? _playback;
    private ShaderMaterial _shader = null!;
    private bool _started;
    public void ConfigurePlayback(VfxPlaybackState playback) => _playback = playback;
    public override void _Ready()
    {
        Offset = new(0, -Texture.GetHeight() * .5f);
        Material = _shader = (ShaderMaterial)Material.Duplicate();
        Visible = false;
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        float speed = _playback?.Parameters.CastSpeed ?? 1;
        float t = (_playback?.Age ?? age) - Delay / speed;
        Visible = t >= 0 && release < 1 && (_started || _playback?.Cancelled != true);
        if (!Visible) return;
        _started = true;
        float growth = Mathf.SmoothStep(0, GrowthDuration / speed, t);
        Scale = PixelSize / Texture.GetSize() * (_playback?.ParticleScale ?? 1)
            * (reducedMotion ? Vector2.One : new Vector2(.7f + .3f * growth, Mathf.Max(.01f, growth)));
        _shader.SetShaderParameter("growth", growth);
        _shader.SetShaderParameter("age", age + GlintPhase);
        _shader.SetShaderParameter("release", release);
        _shader.SetShaderParameter("impact", impact);
        _shader.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
    }
}
