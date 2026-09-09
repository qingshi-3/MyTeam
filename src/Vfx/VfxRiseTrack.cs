using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.Vfx;

// An impulse of independent upright plumes. Birth follows the cast; each
// plume's acceleration and tail use real local seconds after its own birth.
public partial class VfxRiseTrack : Node2D, IVfxTrack, IVfxPlaybackTrack
{
    [Export] public PackedScene ParticleScene { get; set; } = null!;
    [Export] public int Amount { get; set; } = 9;
    [Export] public int Seed { get; set; } = 320;
    [Export] public float Delay { get; set; } = .08f;
    [Export] public float EmitDuration { get; set; } = .32f;
    [Export] public float Lifetime { get; set; } = .65f;
    [Export] public Vector2 Spread { get; set; } = new(38, 8);
    [Export] public Vector2 Size { get; set; } = new(32, 110);
    [Export] public float InitialSpeed { get; set; } = 85;
    [Export] public float Acceleration { get; set; } = 390;
    [Export] public float Opacity { get; set; } = .9f;
    private VfxPlaybackState? _playback;
    private readonly List<Plume> _plumes = [];
    private sealed record Plume(Sprite2D Sprite, ShaderMaterial Material, Vector2 Origin,
        float Birth, float Size, float Speed);
    public void ConfigurePlayback(VfxPlaybackState playback) => _playback = playback;
    public override void _Ready()
    {
        using var rng = new RandomNumberGenerator { Seed = (ulong)Seed };
        int count = Mathf.Clamp(Mathf.RoundToInt(Amount * (_playback?.DensityAtBirth ?? 1)), 1, 48);
        for (int i = 0; i < count; i++)
        {
            var sprite = ParticleScene.Instantiate<Sprite2D>();
            var material = (ShaderMaterial)sprite.Material.Duplicate();
            sprite.Material = material;
            sprite.Offset = new Vector2(0, -sprite.Texture.GetHeight() * .5f);
            AddChild(sprite);
            sprite.Visible = false;
            // Alternating sides leaves the torso readable instead of filling it with white.
            var origin = new Vector2((i % 2 == 0 ? -1 : 1) * rng.RandfRange(.3f, 1) * Spread.X,
                rng.RandfRange(-Spread.Y, Spread.Y));
            float phase = (i + rng.RandfRange(.05f, .6f)) / count;
            _plumes.Add(new(sprite, material, origin, Delay + phase * EmitDuration,
                rng.RandfRange(.7f, 1.15f), rng.RandfRange(.75f, 1.2f)));
        }
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        foreach (var plume in _plumes)
        {
            float t = age - plume.Birth / (_playback?.Parameters.CastSpeed ?? 1);
            var sprite = plume.Sprite;
            if (_playback?.Cancelled == true && !sprite.Visible) continue;
            sprite.Visible = t >= 0 && t < Lifetime && release < 1;
            if (!sprite.Visible) continue;
            float p = Mathf.Clamp(t / Lifetime, 0, 1);
            float travel = reducedMotion ? 0 : InitialSpeed * plume.Speed * t + Acceleration * t * t * .5f;
            sprite.Position = plume.Origin + new Vector2(0, -travel);
            float stretch = reducedMotion ? .7f : Mathf.Lerp(.4f, 1.25f, Mathf.SmoothStep(0, .22f, t));
            sprite.Scale = Size * new Vector2(1 - p * .4f, stretch) * plume.Size
                / sprite.Texture.GetSize() * (_playback?.ParticleScale ?? 1);
            sprite.Modulate = new Color(1, 1, 1, Opacity * (1 - release) * (reducedMotion ? .55f : 1));
            plume.Material.SetShaderParameter("progress", p);
            plume.Material.SetShaderParameter("age", t);
            plume.Material.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
        }
    }
}
