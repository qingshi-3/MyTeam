using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.Vfx;

// A small, bounded emitter sampled analytically so pause/step/slow motion need
// no second particle clock or frame-dependent physics. Each particle is a
// separate authored sprite instance; randomness is presentation-local.
public partial class VfxParticleTrack : Node2D, IVfxTrack
{
    [Export] public PackedScene ParticleScene { get; set; } = null!;
    [Export] public int Amount { get; set; } = 10;
    [Export] public Vector2 SpawnHalfExtents { get; set; } = new(65, 30);
    [Export] public float EmitWindow { get; set; } = .42f;
    [Export] public Vector2 LifetimeRange { get; set; } = new(.7f, 1.05f);
    [Export] public Vector2 SizeRange { get; set; } = new(14, 25);
    [Export] public Vector2 RiseSpeedRange { get; set; } = new(72, 115);
    [Export] public int Seed { get; set; }
    private readonly List<Particle> _particles = [];
    private sealed record Particle(Sprite2D Sprite, Vector2 Origin, float Delay,
        float Lifetime, float Size, float RiseSpeed, float Drift);

    public override void _Ready()
    {
        var random = new RandomNumberGenerator();
        if (Seed == 0) random.Randomize();
        else random.Seed = (ulong)Seed;
        for (int i = 0; i < Mathf.Clamp(Amount, 1, 32); i++)
        {
            var sprite = ParticleScene.Instantiate<Sprite2D>();
            AddChild(sprite);
            sprite.Visible = false;
            _particles.Add(new(sprite,
                new(random.RandfRange(-SpawnHalfExtents.X, SpawnHalfExtents.X),
                    random.RandfRange(-SpawnHalfExtents.Y, SpawnHalfExtents.Y)),
                i == 0 ? 0 : random.RandfRange(0, EmitWindow),
                random.RandfRange(LifetimeRange.X, LifetimeRange.Y),
                random.RandfRange(SizeRange.X, SizeRange.Y),
                random.RandfRange(RiseSpeedRange.X, RiseSpeedRange.Y),
                random.RandfRange(-18, 18)));
        }
        random.Dispose();
    }

    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        foreach (var particle in _particles)
        {
            var elapsed = age - particle.Delay;
            var t = elapsed / particle.Lifetime;
            particle.Sprite.Visible = t >= 0 && t < 1;
            if (!particle.Sprite.Visible) continue;
            var fadeIn = Mathf.SmoothStep(0, 1, Mathf.Clamp(elapsed / .09f, 0, 1));
            var fadeOut = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp((t - .5f) / .5f, 0, 1));
            particle.Sprite.Position = particle.Origin + (reducedMotion ? Vector2.Zero :
                new Vector2(particle.Drift * elapsed, -particle.RiseSpeed * elapsed));
            particle.Sprite.Scale = Vector2.One * particle.Size / particle.Sprite.Texture.GetWidth();
            particle.Sprite.Modulate = new Color(1, 1, 1, fadeIn * fadeOut * (1 - release));
        }
    }
}
