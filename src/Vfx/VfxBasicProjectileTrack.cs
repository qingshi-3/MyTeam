using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TowerAutobattler.Vfx;

// Only the body artwork and already travelled trail are owned here. The stage
// supplies the real projectile position; this component never invents a path.
public partial class VfxBasicProjectileTrack : Node2D, IVfxTrack, IVfxSpatialTrack
{
    [Export] public Sprite2D Core { get; set; } = null!;
    [Export] public Node2D Trail { get; set; } = null!;
    [Export] public float Spin { get; set; }
    [Export] public float TrailLifetime { get; set; } = .16f;
    [Export] public Vector2 TrailSize { get; set; } = new(28, 15);
    [Export] public Color TrailTint { get; set; } = new(.3f, .7f, 1, .5f);
    private Sprite2D[] _trail = [];
    private readonly List<(Vector2 Point, float Birth)> _history = [];
    private Transform2D _toLocal;
    private IVfxStage _stage = null!;
    private Vector2 _point;
    private float _unitScale = 1;
    private float _lastBirth = -1;
    private float _rotation;
    private bool _hit;

    public override void _Ready()
    {
        _trail = Trail.GetChildren().OfType<Sprite2D>().ToArray();
        _rotation = Core.Rotation;
        if (Core.Material is not null) Core.Material = (Material)Core.Material.Duplicate();
        foreach (var sprite in _trail) sprite.Visible = false;
    }

    public void SetSpatialContext(VfxContext context, IVfxStage stage, Transform2D stageToInstance)
    {
        _point = context.Target;
        _stage = stage;
        _toLocal = stageToInstance;
        _unitScale = stage.UnitScale;
    }

    public void Sample(float age, bool sustained, float release, float impact, bool reducedMotion)
    {
        _hit |= impact > 0;
        Core.Visible = release <= 0 && !_hit;
        Core.Rotation = _rotation + (reducedMotion ? 0 : age * Spin);
        if (Core.Material is ShaderMaterial shader)
        {
            shader.SetShaderParameter("age", reducedMotion ? 0 : age);
            shader.SetShaderParameter("motion_amount", reducedMotion ? 0f : 1f);
        }
        _history.RemoveAll(sample => age - sample.Birth >= TrailLifetime);
        if (!reducedMotion && !_hit && release <= 0 && age - _lastBirth >= .018f &&
            (_history.Count == 0 || _history[^1].Point.DistanceSquaredTo(_point) > .000001f))
        {
            if (_history.Count == _trail.Length) _history.RemoveAt(0);
            _history.Add((_point, age));
            _lastBirth = age;
        }
        for (var index = 0; index < _trail.Length; index++)
        {
            var sprite = _trail[index];
            sprite.Visible = !reducedMotion && index < _history.Count;
            if (!sprite.Visible) continue;
            var sample = _history[index];
            var fade = Mathf.Clamp(1 - (age - sample.Birth) / Math.Max(.01f, TrailLifetime), 0, 1);
            var worldSize = TrailSize * _unitScale * (.25f + .75f * fade);
            // Full transform compensation also keeps historic wisps upright when
            // a projectile faces left or the stage is resized during playback.
            var origin = _toLocal * _stage.Project(sample.Point, false);
            sprite.Transform = new Transform2D(
                _toLocal.BasisXform(Vector2.Right * worldSize.X / sprite.Texture.GetWidth()),
                _toLocal.BasisXform(Vector2.Down * worldSize.Y / sprite.Texture.GetHeight()), origin);
            sprite.Modulate = TrailTint with { A = TrailTint.A * fade * fade * (1 - release) };
        }
    }
}
