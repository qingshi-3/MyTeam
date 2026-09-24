using Godot;

namespace TowerAutobattler.Vfx;

// Rock translates through a fixed ground clipping plane. It does not squash
// vertically like a crystal growing, and each rock keeps its size as area changes.
public partial class VfxRockSpikeTrack : Sprite2D, IVfxTrack, IVfxPlaybackTrack
{
    [Export] public Vector2 PixelSize { get; set; } = new(72, 144);
    [Export] public float Delay { get; set; } = .12f;
    [Export] public float RiseDuration { get; set; } = .14f;
    [Export] public float HoldDuration { get; set; } = .42f;
    [Export] public float FallDuration { get; set; } = .5f;
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
        float t = age - Delay / speed;
        float riseTime = RiseDuration / speed;
        float ending = Mathf.Clamp((t - riseTime - HoldDuration) / FallDuration, 0, 1);
        Visible = t >= 0 && ending < 1 && release < 1 && (_started || _playback?.Cancelled != true);
        if (!Visible) return;
        _started = true;
        float rise = 1 - Mathf.Pow(1 - Mathf.Clamp(t / riseTime, 0, 1), 3);
        float sink = ending * ending;
        Scale = PixelSize / Texture.GetSize() * (_playback?.ParticleScale ?? 1);
        _shader.SetShaderParameter("lift", reducedMotion ? 1f : rise * (1 - sink));
        _shader.SetShaderParameter("arrival", Mathf.Exp(-Mathf.Max(0, t - riseTime) * 10) * rise);
        _shader.SetShaderParameter("fade", reducedMotion ? ending : Mathf.SmoothStep(.55f, 1, ending));
        _shader.SetShaderParameter("release", release);
    }
}
