using System;
using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.Vfx;

// A projected ground footprint with upright, independently cycling healing
// signs. Surface projection never flattens the signs or their upward travel.
public partial class VfxHealingFieldTrack : Node2D, IVfxTrack, IVfxPlaybackTrack, IVfxSpatialTrack
{
    [Export] public PackedScene PlusParticle { get; set; } = null!;
    [Export] public PackedScene DustParticle { get; set; } = null!;
    [Export] public int Seed { get; set; } = 1041;
    [Export] public float ReferenceRadius { get; set; } = 1.5f;
    private VfxPlaybackState? _playback;
    private IVfxStage? _stage;
    private VfxContext _context;
    private Sprite2D _surface = null!;
    private ShaderMaterial _material = null!;
    private float? _cancelAge;
    private readonly List<Particle> _particles = [];
    private sealed record Particle(Sprite2D Sprite, Vector2 Origin, float Phase, float Lifetime,
        float Size, float Speed, float Drift, bool Dust);

    public void ConfigurePlayback(VfxPlaybackState playback) => _playback = playback;
    public void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D stageToInstance)
    {
        _context = context;
        _stage = stage;
        Transform = stageToInstance;
    }
    public override void _Ready()
    {
        _surface = GetNode<Sprite2D>("Surface");
        _surface.Material = _material = (ShaderMaterial)_surface.Material.Duplicate();
        using var random = new RandomNumberGenerator { Seed = (ulong)Math.Max(1, Seed) };
        float density = _playback?.DensityAtBirth ?? 1;
        AddParticles(PlusParticle, Mathf.Clamp(Mathf.RoundToInt(12 * density), 4, 24), false, random);
        AddParticles(DustParticle, Mathf.Clamp(Mathf.RoundToInt(18 * density), 4, 32), true, random);
    }
    private void AddParticles(PackedScene scene, int count, bool dust, RandomNumberGenerator random)
    {
        for (int i = 0; i < count; i++)
        {
            var sprite = scene.Instantiate<Sprite2D>();
            AddChild(sprite);
            sprite.Visible = false;
            float angle = random.RandfRange(0, Mathf.Tau);
            float distance = Mathf.Sqrt(random.Randf()) * .88f;
            _particles.Add(new(sprite, Vector2.FromAngle(angle) * distance,
                i == 0 ? 0 : (i + random.RandfRange(.1f, .8f)) / count,
                random.RandfRange(1.3f, 1.85f), random.RandfRange(dust ? 8 : 13, dust ? 15 : 23),
                random.RandfRange(dust ? 22 : 28, dust ? 43 : 47), random.RandfRange(-5, 5), dust));
        }
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        var stage = _stage ?? throw new InvalidOperationException("Healing field stage was not injected.");
        float radius = _context.Radius > 0 ? _context.Radius : ReferenceRadius;
        var center = stage.Project(_context.Target, true);
        var x = (stage.Project(_context.Target + Vector2.Right * radius, true) - center) / 128;
        var y = (stage.Project(_context.Target + Vector2.Down * radius, true) - center) / 128;
        _surface.Transform = new Transform2D(x, y, center);
        _material.SetShaderParameter("age", age);
        _material.SetShaderParameter("release", release);
        _material.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
        if (_playback?.Cancelled == true || release > 0)
            // Recover the cancellation boundary from the owning 0.25 s release
            // envelope, so a large next step cannot birth another particle cycle.
            _cancelAge ??= age - release * .25f * (_playback?.Parameters.MotionSpeed ?? 1);
        float particleSize = (_playback?.Parameters.ParticleScale ?? 1) * stage.UnitScale;
        foreach (var particle in _particles)
        {
            float elapsed = age - particle.Phase * particle.Lifetime;
            float cycle = Mathf.Floor(elapsed / particle.Lifetime);
            bool cancelledBirth = _cancelAge is { } cancelled &&
                (cancelled < particle.Phase * particle.Lifetime ||
                    cycle > Mathf.Floor((cancelled - particle.Phase * particle.Lifetime) / particle.Lifetime));
            var sprite = particle.Sprite;
            sprite.Visible = elapsed >= 0 && release < 1 && !cancelledBirth;
            if (!sprite.Visible) continue;
            float t = Mathf.PosMod(elapsed, particle.Lifetime);
            float p = t / particle.Lifetime;
            var origin = stage.Project(_context.Target + particle.Origin * radius, true);
            // Birth positions are logical; motion after birth is an upright
            // optical overlay in stage pixels, not another world-plane vector.
            var drift = reducedMotion ? Vector2.Zero : new Vector2(particle.Drift * t, -particle.Speed * t) * stage.UnitScale;
            sprite.Position = origin + drift;
            sprite.Scale = Vector2.One * particle.Size * particleSize / sprite.Texture.GetWidth();
            float envelope = Mathf.SmoothStep(0, .12f, p) * (1 - Mathf.SmoothStep(.48f, 1, p));
            var tint = particle.Dust ? new Color(.26f, .86f, .46f) : Colors.White;
            sprite.Modulate = new Color(tint.R, tint.G, tint.B, envelope * (particle.Dust ? .65f : .92f) * (1 - release));
        }
    }
}
