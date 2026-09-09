using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.Vfx;

// Each surface location owns its grow -> pop -> ripple cycle. The phase is
// sampled analytically, so a skipped frame cannot lose the pop or spawn it twice.
public partial class VfxPoolBubbleTrack : Node2D, IVfxTrack, IVfxPlaybackTrack
{
    [Export] public PackedScene ParticleScene { get; set; } = null!;
    [Export] public int Amount { get; set; } = 12;
    [Export] public int Seed { get; set; } = 812;
    [Export] public Vector2 Spread { get; set; } = new(98, 49);
    [Export] public Vector2 SizeRange { get; set; } = new(32, 58);
    [Export] public float CycleDuration { get; set; } = 1.85f;
    private VfxPlaybackState? _playback;
    private float? _stopAge;
    private float _lastAge;
    private readonly List<Bubble> _bubbles = [];
    private sealed record Bubble(Sprite2D Sprite, ShaderMaterial Material, Vector2 Origin,
        float Delay, float Size, float Period);
    public void ConfigurePlayback(VfxPlaybackState playback) => _playback = playback;
    public override void _Ready()
    {
        using var rng = new RandomNumberGenerator { Seed = (ulong)Seed };
        int count = Mathf.Clamp(Mathf.RoundToInt(Amount * (_playback?.DensityAtBirth ?? 1)), 1, 40);
        for (int i = 0; i < count; i++)
        {
            var sprite = ParticleScene.Instantiate<Sprite2D>();
            var material = (ShaderMaterial)sprite.Material.Duplicate();
            sprite.Material = material;
            AddChild(sprite);
            sprite.Visible = false;
            float angle = rng.RandfRange(0, Mathf.Tau);
            float radius = Mathf.Sqrt(rng.RandfRange(.08f, 1));
            var origin = Vector2.FromAngle(angle) * Spread * radius;
            _bubbles.Add(new(sprite, material, origin, .25f + i / (float)count * CycleDuration,
                rng.RandfRange(SizeRange.X, SizeRange.Y), CycleDuration * rng.RandfRange(.85f, 1.2f)));
        }
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        if (_playback?.Cancelled == true) _stopAge ??= _lastAge;
        _lastAge = age;
        foreach (var bubble in _bubbles)
        {
            float t = age - bubble.Delay;
            var sprite = bubble.Sprite;
            sprite.Visible = t >= 0 && release < 1 && (sustained || t < bubble.Period);
            if (_stopAge is { } stopped && (stopped < bubble.Delay ||
                Mathf.Floor(t / bubble.Period) > Mathf.Floor((stopped - bubble.Delay) / bubble.Period)))
                sprite.Visible = false;
            if (!sprite.Visible) continue;
            float phase = Mathf.PosMod(t, bubble.Period) / bubble.Period;
            sprite.Position = bubble.Origin;
            sprite.Scale = Vector2.One * bubble.Size / sprite.Texture.GetWidth() * (_playback?.ParticleScale ?? 1);
            sprite.Modulate = new Color(1, 1, 1, 1 - release);
            bubble.Material.SetShaderParameter("phase", reducedMotion ? .3f : phase);
            bubble.Material.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
        }
    }
}
