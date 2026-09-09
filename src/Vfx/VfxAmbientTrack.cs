using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.Vfx;

// Bounded decorative particles, analytically sampled from the owning clock.
// Each particle wraps while invisible, never restarting the whole emitter.
public partial class VfxAmbientTrack : Node2D, IVfxTrack, IVfxPlaybackTrack
{
    public enum MotionKind { Drift, Radial, Cone, Orbit }
    [Export] public PackedScene ParticleScene { get; set; } = null!;
    [Export] public MotionKind Motion { get; set; }
    [Export] public int Amount { get; set; } = 12;
    [Export] public float Lifetime { get; set; } = 1.2f;
    [Export] public float EmitDuration { get; set; } = .45f;
    [Export] public Vector2 Spread { get; set; } = new(50, 12);
    [Export] public Vector2 Velocity { get; set; } = new(0, -70);
    [Export] public Vector2 SizeRange { get; set; } = new(15, 25);
    [Export] public Vector2 EndScale { get; set; } = new(.3f, .3f);
    [Export] public float AngularSpeed { get; set; } = 2;
    [Export] public float OrbitPhase { get; set; }
    [Export] public float OrbitDepthScale { get; set; }
    [Export] public float OrbitTilt { get; set; }
    [Export] public Color StartTint { get; set; } = Colors.White;
    [Export] public Color EndTint { get; set; } = Colors.White;
    [Export] public float Opacity { get; set; } = .8f;
    [Export] public bool FaceVelocity { get; set; }
    [Export] public int Seed { get; set; }
    [Export] public bool AnimateMaterial { get; set; }
    [Export] public bool FollowCastEmission { get; set; }
    [Export] public float RotationOffset { get; set; }
    [Export(PropertyHint.Range, "0,0.9")] public float FadeStart { get; set; }
    [Export(PropertyHint.Range, "0,8")] public float Drag { get; set; }
    // Screen-space buoyancy stays upward even when the effect aims left or diagonally.
    [Export] public float ScreenRiseAcceleration { get; set; }
    private VfxPlaybackState? _playback;
    public void ConfigurePlayback(VfxPlaybackState playback) => _playback = playback;
    private readonly List<Particle> _particles = [];
    private sealed record Particle(Sprite2D Sprite, float Phase, float Size, Vector2 Origin, Vector2 Velocity,
        ShaderMaterial? Material);

    public override void _Ready()
    {
        using var rng = new RandomNumberGenerator();
        if (Seed == 0) rng.Randomize(); else rng.Seed = (ulong)Seed;
        int count = Mathf.Clamp(Mathf.RoundToInt(Amount * (Motion == MotionKind.Orbit ? 1 : _playback?.DensityAtBirth ?? 1)), 1, 64);
        for (int i = 0; i < count; i++)
        {
            var sprite = ParticleScene.Instantiate<Sprite2D>();
            ShaderMaterial? material = null;
            if (AnimateMaterial && sprite.Material is ShaderMaterial source)
                sprite.Material = material = (ShaderMaterial)source.Duplicate();
            AddChild(sprite);
            sprite.Visible = false;
            float phase = (i + rng.RandfRange(.05f, .8f)) / count;
            if (Motion == MotionKind.Orbit) phase = i / (float)count;
            Vector2 origin = new(rng.RandfRange(-Spread.X, Spread.X), rng.RandfRange(-Spread.Y, Spread.Y));
            Vector2 velocity = Velocity * rng.RandfRange(.8f, 1.2f);
            if (Motion == MotionKind.Radial)
            {
                var radial = Vector2.FromAngle(phase * Mathf.Tau);
                origin = radial * Spread;
                velocity = radial * Velocity.Length();
            }
            if (Motion == MotionKind.Cone)
            {
                origin = Vector2.Zero;
                velocity.Y += rng.RandfRange(-Spread.Y, Spread.Y);
            }
            _particles.Add(new(sprite, phase, rng.RandfRange(SizeRange.X, SizeRange.Y), origin, velocity, material));
        }
    }

    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        float life = Mathf.Max(.05f, Lifetime);
        var buoyancy = Vector2.Zero;
        if (ScreenRiseAcceleration != 0 && Mathf.Abs(GlobalTransform.Determinant()) > .000001f)
            buoyancy = GlobalTransform.AffineInverse().BasisXform(new Vector2(0, -ScreenRiseAcceleration));
        foreach (var particle in _particles)
        {
            var sprite = particle.Sprite;
            float birth = particle.Phase * (sustained ? life : EmitDuration /
                (FollowCastEmission ? _playback?.Parameters.CastSpeed ?? 1 : 1));
            float t = age - birth;
            if (sustained && t >= 0) t = Mathf.PosMod(t, life);
            float p = Mathf.Clamp(t / life, 0, 1);
            if (_playback?.Cancelled == true && !sprite.Visible) continue;
            sprite.Visible = t >= 0 && t < life && release < 1;
            if (Motion == MotionKind.Orbit)
            {
                sprite.Visible = release < 1;
                float angle = particle.Phase * Mathf.Tau + OrbitPhase + (reducedMotion ? 0 : age * AngularSpeed);
                sprite.Position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Spread;
                // Continuous near/far emphasis; never switch opacity or size at a half-orbit seam.
                sprite.Scale = Vector2.One * particle.Size / sprite.Texture.GetWidth() * (_playback?.ParticleScale ?? 1)
                    * (1 + OrbitDepthScale * Mathf.Sin(angle));
                sprite.Rotation = RotationOffset + (reducedMotion ? 0 : OrbitTilt * Mathf.Sin(angle + .6f));
                sprite.Modulate = new(StartTint.R, StartTint.G, StartTint.B,
                    Mathf.Min(age / .15f, 1) * Opacity * (1 - release) * (.75f + .25f * Mathf.Sin(angle)));
                continue;
            }
            if (!sprite.Visible) continue;
            // Reduced motion retains a static cone's reach, rather than
            // collapsing the entire breath into a dot at the caster.
            float drag = Mathf.Max(0, Drag);
            float travelTime = drag < .001f ? t : (1 - Mathf.Exp(-drag * t)) / drag;
            var travel = reducedMotion ? (Motion == MotionKind.Cone ? particle.Velocity * life * particle.Phase : Vector2.Zero)
                : particle.Velocity * travelTime + buoyancy * (.5f * t * t);
            sprite.Position = particle.Origin + travel;
            var facing = reducedMotion ? particle.Velocity : particle.Velocity * Mathf.Exp(-drag * t) + buoyancy * t;
            sprite.Rotation = (FaceVelocity ? facing.Angle() : 0) + RotationOffset;
            sprite.Scale = Vector2.One.Lerp(EndScale, p) * particle.Size / sprite.Texture.GetWidth() * (_playback?.ParticleScale ?? 1);
            float envelope = Mathf.SmoothStep(0, 1, Mathf.Min(p / .12f, 1))
                * (1 - Mathf.SmoothStep(Mathf.Clamp(FadeStart, 0, .9f), 1, p));
            var tint = StartTint.Lerp(EndTint, p);
            sprite.Modulate = new(tint.R, tint.G, tint.B, envelope * Opacity * (1 - release));
            if (particle.Material is { } shader)
            {
                shader.SetShaderParameter("age", reducedMotion ? particle.Phase : t + particle.Phase * 3);
                shader.SetShaderParameter("particle_progress", p);
                shader.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
            }
        }
    }
}
