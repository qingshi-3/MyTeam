using System;
using Godot;
using TowerAutobattler.Domain;

namespace TowerAutobattler.Presentation;

public enum RangedVisualKind { Projectile, Beam, Impact }

public partial class RangedAttackVisual : Node2D
{
    [Export] public RangedVisualKind Kind { get; set; }
    [Export] public float Lifetime { get; set; } = .22f;
    private Vector2 _origin;
    private Vector2 _position;
    private Vector2 _start;
    private Vector2 _destination;
    private Vector2 _direction = Vector2.Right;
    private float _travel;
    private float _age;

    public void Bind(Vector2 origin, Vector2 position)
    {
        _origin = origin;
        _position = _start = _destination = position;
        if (Kind == RangedVisualKind.Projectile) _direction = (origin - position).Normalized();
    }

    public void MoveTo(Vector2 position, bool snap)
    {
        _start = _position;
        _destination = position;
        _travel = 0;
        if (snap) _position = _start = position;
    }

    public bool Advance(float seconds, BattleBoard board)
    {
        _age += seconds;
        var scale = board.CurrentProjection.UnitScale;
        var offset = Vector2.Up * 24f * scale;
        if (Kind == RangedVisualKind.Projectile)
        {
            _travel = Math.Min(BattleTiming.TickSeconds, _travel + seconds);
            _position = _start.Lerp(_destination, _travel / BattleTiming.TickSeconds);
            Position = board.LogicalToLocal(_position) + offset;
            var direction = board.LogicalToLocal(_position + _direction) - board.LogicalToLocal(_position);
            Rotation = direction.Angle();
            Scale = Vector2.One * scale;
            return true;
        }
        var progress = Mathf.Clamp(_age / Math.Max(.01f, Lifetime), 0, 1);
        Modulate = new Color(1, 1, 1, 1 - progress);
        if (Kind == RangedVisualKind.Beam)
        {
            Position = board.LogicalToLocal(_origin) + offset;
            GetNode<Line2D>("Line").SetPointPosition(1, board.LogicalToLocal(_position) + offset - Position);
        }
        else
        {
            Position = board.LogicalToLocal(_position) + offset;
            Scale = Vector2.One * scale * (1 + progress * 1.5f);
        }
        return progress < 1;
    }
}
