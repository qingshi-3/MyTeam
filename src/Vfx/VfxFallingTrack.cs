using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TowerAutobattler.Vfx;

// A presentation-only lob. Trail particles stay at their historical birth
// positions rather than being dragged with the projectile. No damage is applied.
public partial class VfxFallingTrack : Node2D, IVfxTrack, IVfxSourceTrack
{
    [Export] public float TravelDuration { get; set; } = .78f;
    [Export] public float ArcHeight { get; set; } = 72;
    [Export] public Vector2 LaunchOffset { get; set; } = new(16, -12);
    [Export] public PackedScene TrailParticle { get; set; } = null!;
    [Export] public int Seed { get; set; }
    private Node2D _projectile = null!;
    private ShaderMaterial[] _materials = [];
    private readonly List<Spark> _sparks = [];
    private Vector2 _start = new(-180, -35);
    private float _unitScale = 1;
    private sealed record Spark(Sprite2D Sprite, float Birth, float Lifetime, float Size, Vector2 Drift);
    public void SetSource(Vector2 localSource, float localUnitScale)
    {
        _unitScale = localUnitScale;
        float facing = localSource.X > 0 ? -1 : 1;
        _start = localSource + LaunchOffset * new Vector2(facing, 1) * _unitScale;
    }
    private Vector2 PositionAt(float t) => _start * (1 - t) + new Vector2(0, -4 * ArcHeight * _unitScale * t * (1 - t));
    private Vector2 TangentAt(float t) => -_start + new Vector2(0, -4 * ArcHeight * _unitScale * (1 - 2 * t));
    public override void _Ready()
    {
        _projectile = GetNode<Node2D>("Projectile");
        _materials = _projectile.GetChildren().OfType<Sprite2D>().Select(sprite =>
        {
            var material = (ShaderMaterial)sprite.Material.Duplicate();
            sprite.Material = material;
            return material;
        }).ToArray();
        var trail = GetNode<Node2D>("Trail");
        using var rng = new RandomNumberGenerator();
        if (Seed == 0) rng.Randomize(); else rng.Seed = (ulong)Seed;
        for (int i = 0; i < 24; i++)
        {
            var sprite = TrailParticle.Instantiate<Sprite2D>();
            trail.AddChild(sprite);
            sprite.Visible = false;
            _sparks.Add(new(sprite, TravelDuration * (i + .5f) / 24,
                rng.RandfRange(.18f, .28f), rng.RandfRange(24, 33), new(rng.RandfRange(-6, 6), -12)));
        }
    }
    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        float p = Mathf.Clamp(age / Mathf.Max(.01f, TravelDuration), 0, 1);
        _projectile.Visible = age < TravelDuration && release < 1;
        _projectile.Position = reducedMotion ? new Vector2(0, -25) * _unitScale : PositionAt(p);
        _projectile.Rotation = reducedMotion ? 0 : TangentAt(p).Angle();
        _projectile.Scale = Vector2.One * _unitScale;
        _projectile.Modulate = new Color(1, 1, 1, Mathf.Min(age / .045f, 1) * (1 - release));
        foreach (var material in _materials)
        {
            material.SetShaderParameter("age", age);
            material.SetShaderParameter("progress", p);
            material.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
        }
        foreach (var spark in _sparks)
        {
            float t = age - spark.Birth;
            spark.Sprite.Visible = !reducedMotion && t >= 0 && t < spark.Lifetime;
            if (!spark.Sprite.Visible) continue;
            float life = t / spark.Lifetime;
            spark.Sprite.Position = PositionAt(spark.Birth / TravelDuration) + spark.Drift * t * _unitScale;
            spark.Sprite.Rotation = TangentAt(spark.Birth / TravelDuration).Angle();
            spark.Sprite.Scale = new Vector2(1, .85f) * _unitScale * spark.Size * (1 - .6f * life) / spark.Sprite.Texture.GetWidth();
            spark.Sprite.Modulate = new Color(1, 1 - .5f * life, 1 - .8f * life,
                .75f * Mathf.Min(t / .025f, 1) * (1 - life) * (1 - release));
        }
    }
}
