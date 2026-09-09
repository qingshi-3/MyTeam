using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.Vfx;

// Decorative fragments only. Analytic drag keeps stepping and normal playback
// identical; these particles never participate in projectile damage.
public partial class VfxBallisticTrack : Node2D, IVfxTrack, IVfxPlaybackTrack
{
    [Export] public PackedScene ParticleScene { get; set; } = null!;
    [Export] public int Amount { get; set; } = 5;
    [Export] public float Delay { get; set; } = .06f;
    [Export] public float Direction { get; set; } = .6f;
    [Export] public float Spread { get; set; } = .75f;
    [Export] public Vector2 SpeedRange { get; set; } = new(160, 330);
    [Export] public Vector2 LifetimeRange { get; set; } = new(.22f, .4f);
    [Export] public Vector2 SizeRange { get; set; } = new(17, 34);
    [Export] public float HeightScale { get; set; } = .6f;
    [Export] public Vector2 EndSizeFactor { get; set; } = new(.45f, 1);
    [Export] public bool Radial { get; set; }
    [Export] public float ArcDegrees { get; set; } = 360;
    [Export] public float EmitDuration { get; set; }
    [Export] public float VelocityAngleOffset { get; set; }
    [Export] public float OriginRadius { get; set; }
    [Export] public float Gravity { get; set; } = 45;
    [Export] public float LiftSpeed { get; set; }
    [Export] public float Spin { get; set; }
    [Export] public bool FaceVelocity { get; set; } = true;
    [Export] public Color EndTint { get; set; } = new(1, .5f, .6f);
    [Export] public int Seed { get; set; }
    [Export] public bool FollowCastDelay { get; set; }
    private VfxPlaybackState? _playback;
    public void ConfigurePlayback(VfxPlaybackState playback) => _playback = playback;
    private readonly List<Fragment> _fragments = [];
    private sealed record Fragment(Sprite2D Sprite, Vector2 Origin, Vector2 Velocity,
        float Birth, float Lifetime, float Size);

    public override void _Ready()
    {
        using var rng = new RandomNumberGenerator();
        if (Seed == 0) rng.Randomize(); else rng.Seed = (ulong)Seed;
        int count = Mathf.Clamp(Mathf.RoundToInt(Amount * (_playback?.DensityAtBirth ?? 1)), 1, 48);
        for (int i = 0; i < count; i++)
        {
            var sprite = ParticleScene.Instantiate<Sprite2D>();
            AddChild(sprite);
            sprite.Visible = false;
            float phase = Radial || EmitDuration > 0 ? (i + rng.RandfRange(.1f, .9f)) / count : 0;
            var angle = Radial ? Direction + Mathf.DegToRad(ArcDegrees) * phase
                : Direction + rng.RandfRange(-Spread, Spread);
            var velocity = Vector2.FromAngle(angle + VelocityAngleOffset)
                * rng.RandfRange(SpeedRange.X, SpeedRange.Y);
            velocity.Y -= LiftSpeed;
            var origin = Radial ? Vector2.FromAngle(angle) * OriginRadius
                : new Vector2(rng.RandfRange(-9, 9), rng.RandfRange(-9, 9));
            _fragments.Add(new(sprite, origin,
                velocity, Delay + EmitDuration * (1 - Mathf.Sqrt(1 - phase)) + rng.RandfRange(0, .025f),
                rng.RandfRange(LifetimeRange.X, LifetimeRange.Y), rng.RandfRange(SizeRange.X, SizeRange.Y)));
        }
    }

    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        foreach (var fragment in _fragments)
        {
            float t = age - fragment.Birth / (FollowCastDelay ? _playback?.Parameters.CastSpeed ?? 1 : 1);
            var sprite = fragment.Sprite;
            if (_playback?.Cancelled == true && !sprite.Visible) continue;
            sprite.Visible = t >= 0 && t < fragment.Lifetime && release < 1;
            if (!sprite.Visible) continue;
            float p = t / fragment.Lifetime;
            float drag = Mathf.Exp(-3 * t);
            sprite.Position = fragment.Origin + (reducedMotion ? Vector2.Zero :
                fragment.Velocity * ((1 - drag) / 3) + new Vector2(0, Gravity * t * t));
            sprite.Rotation = (FaceVelocity ? fragment.Velocity.Angle() : 0) + (reducedMotion ? 0 : Spin * t);
            sprite.Scale = new Vector2(fragment.Size, fragment.Size * HeightScale) * Vector2.One.Lerp(EndSizeFactor, p)
                / sprite.Texture.GetWidth() * (_playback?.ParticleScale ?? 1);
            float fade = Mathf.Min(t / .015f, 1) * Mathf.Pow(1 - p, 1.4f);
            var tint = Colors.White.Lerp(EndTint, p);
            sprite.Modulate = new Color(tint.R, tint.G, tint.B, fade * (1 - release));
        }
    }
}
