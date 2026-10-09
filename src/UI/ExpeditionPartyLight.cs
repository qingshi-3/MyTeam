using System;
using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

// MapCanvas supplies the actual marker center. Only the lamp follows that center;
// emitted motes remain in the canvas coordinates at which the marker passed.
public partial class ExpeditionPartyLight : Control
{
    private sealed class Mote(Sprite2D sprite)
    {
        public readonly Sprite2D Sprite = sprite;
        public Vector2 Origin, Velocity;
        public float Born, Life, Size, Spin;
        public bool Alive;
    }

    private Mote[] _motes = [];
    private Sprite2D _lamp = null!, _core = null!;
    private readonly RandomNumberGenerator _random = new() { Seed = 194271 };
    private Vector2 _previous;
    private float _time, _cellSize, _distance, _settledAt = -1;
    private int _next;
    private bool _ready, _tracked, _moving, _reduced, _clearing;

    public Func<bool> ReduceUiMotion { get; set; } = () => false;

    public override void _Ready()
    {
        _lamp = GetNode<Sprite2D>("LampGlow");
        _core = GetNode<Sprite2D>("LampCore");
        _motes = GetNode("Motes").GetChildren().Cast<Sprite2D>()
            .Select(sprite => new Mote(sprite)).ToArray();
        _ready = true;
        VisibilityChanged += OnVisibilityChanged;
        Clear();
    }

    public void Track(Vector2 center, float cellSize, bool moving, bool snap = false)
    {
        if (!_ready || !IsInsideTree()) return;
        if (GetParent() is CanvasItem parent && !parent.IsVisibleInTree())
        {
            Clear();
            return;
        }

        var size = Mathf.Max(1f, cellSize);
        var reduced = ReduceUiMotion();
        // Zoom, map replacement and unexpected jumps cannot leave a light path
        // connecting two unrelated layouts. The caller can explicitly snap too.
        var reset = snap || !_tracked || !Mathf.IsEqualApprox(size, _cellSize)
            || center.DistanceTo(_previous) > size * 1.5f;
        if (reset || reduced != _reduced)
        {
            ClearMotes();
            _distance = 0;
            _settledAt = -1;
        }
        _cellSize = size;
        _reduced = reduced;

        if (!reset && !reduced && (moving || _moving)) EmitPath(_previous, center);
        if (!reset && !reduced && _moving && !moving) _settledAt = _time;
        if (moving) _settledAt = -1;
        _moving = moving;
        _previous = center;
        _tracked = true;
        _lamp.Position = _core.Position = center;
        ApplyLamp();
        Show();
        SetProcess(true);
    }

    private void EmitPath(Vector2 from, Vector2 to)
    {
        var segment = to - from;
        var length = segment.Length();
        if (length < .01f) return;
        var tangent = segment / length;
        var normal = new Vector2(-tangent.Y, tangent.X);
        var spacing = _cellSize * .28f;
        for (var distance = spacing - _distance; distance <= length; distance += spacing)
        {
            var mote = _motes[_next++ % _motes.Length];
            mote.Origin = from + tangent * distance + normal * _random.RandfRange(-.07f, .07f) * _cellSize;
            mote.Velocity = (-tangent * _random.RandfRange(.03f, .08f)
                + normal * _random.RandfRange(-.12f, .12f)) * _cellSize;
            mote.Born = _time;
            mote.Life = _random.RandfRange(.24f, .34f);
            mote.Size = _cellSize * _random.RandfRange(.20f, .28f) / 256f;
            mote.Spin = _random.RandfRange(-.7f, .7f);
            mote.Alive = true;
            mote.Sprite.Position = mote.Origin;
            mote.Sprite.Scale = Vector2.One * mote.Size;
            mote.Sprite.Rotation = 0;
            mote.Sprite.Modulate = new Color(1f, .79f, .41f, .48f);
            mote.Sprite.Show();
        }
        _distance = (_distance + length) % spacing;
    }

    public override void _Process(double delta)
    {
        if (!_tracked || !IsVisibleInTree()) { Clear(); return; }
        var reduced = ReduceUiMotion();
        if (reduced != _reduced)
        {
            _reduced = reduced;
            ClearMotes();
            _distance = 0;
            _settledAt = -1;
        }
        if (!reduced) _time += (float)delta;
        ApplyLamp();
        foreach (var mote in _motes)
        {
            if (!mote.Alive) continue;
            var elapsed = _time - mote.Born;
            var age = elapsed / mote.Life;
            if (age >= 1f) { mote.Alive = false; mote.Sprite.Hide(); continue; }
            mote.Sprite.Position = mote.Origin + mote.Velocity * elapsed * (1f - .35f * age);
            mote.Sprite.Rotation = mote.Spin * age;
            mote.Sprite.Scale = Vector2.One * mote.Size * Mathf.Lerp(1f, .45f, age);
            mote.Sprite.Modulate = new Color(1f, .79f, .41f, .48f * (1f - age) * (1f - age));
        }
    }

    private void ApplyLamp()
    {
        // A quiet, uneven lamp rather than a spreading ring. All shader
        // materials stay immutable; per-instance state belongs to the sprites.
        var flicker = _reduced ? 0f : Mathf.Sin(_time * 2.1f) * .025f
            + Mathf.Sin(_time * 3.7f + .8f) * .012f;
        var contraction = 0f;
        if (!_reduced && _settledAt >= 0)
        {
            var age = (_time - _settledAt) / .22f;
            if (age < 1f) contraction = Mathf.Sin(age * Mathf.Pi) * .07f;
            else _settledAt = -1;
        }
        _lamp.Scale = Vector2.One * (_cellSize * 1.45f / 256f * (1f + flicker - contraction));
        _core.Scale = Vector2.One * (_cellSize * .86f / 256f * (1f + flicker * .5f - contraction));
        _lamp.Modulate = new Color(1f, .72f, .30f, .32f * (1f + flicker));
        _core.Modulate = new Color(1f, .84f, .48f, .23f);
        _lamp.Show();
        _core.Show();
    }

    private void ClearMotes()
    {
        _next = 0;
        foreach (var mote in _motes) { mote.Alive = false; mote.Sprite.Hide(); }
    }

    public void Clear()
    {
        if (_clearing) return;
        _clearing = true;
        _tracked = _moving = false;
        _time = _distance = 0;
        _settledAt = -1;
        ClearMotes();
        _lamp?.Hide();
        _core?.Hide();
        SetProcess(false);
        Hide();
        _clearing = false;
    }

    private void OnVisibilityChanged()
    {
        if (_ready && !IsVisibleInTree()) Clear();
    }

    public override void _ExitTree()
    {
        VisibilityChanged -= OnVisibilityChanged;
        Clear();
        _ready = false;
    }
}
