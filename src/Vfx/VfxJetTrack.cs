using Godot;

namespace TowerAutobattler.Vfx;

// The visible front and the shutoff travel downstream; cast rate only changes supply time.
public partial class VfxJetTrack : Sprite2D, IVfxTrack, IVfxPlaybackTrack
{
    [Export] public float SupplyDuration { get; set; } = .5f;
    [Export] public float TransitDuration { get; set; } = .32f;
    private ShaderMaterial _shader = null!;
    private VfxPlaybackState? _playback;
    private bool _started;
    public void ConfigurePlayback(VfxPlaybackState playback) => _playback = playback;
    public override void _Ready() => Material = _shader = (ShaderMaterial)Material.Duplicate();
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        float supply = SupplyDuration / (_playback?.Parameters.CastSpeed ?? 1);
        Visible = age < supply + TransitDuration + .12f && release < 1
            && (_started || _playback?.Cancelled != true);
        if (!Visible) return;
        _started |= age > 0;
        _shader.SetShaderParameter("age", age);
        _shader.SetShaderParameter("supply_duration", supply);
        _shader.SetShaderParameter("transit_duration", TransitDuration);
        _shader.SetShaderParameter("release", release);
        _shader.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
    }
}
