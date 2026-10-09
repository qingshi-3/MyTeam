using System;
using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

// Presentation only. The seat supplies its actual flight samples; this component
// keeps already-emitted motes at those positions, with its own bounded sprite pool.
public partial class OpeningSelectionLight : Control
{
    private sealed class Mote(Sprite2D sprite)
    {
        public readonly Sprite2D Sprite = sprite;
        public Vector2 Origin, Velocity;
        public float Born, Life, Size, Spin;
        public bool Alive;
    }

    private Mote[] _motes = [];
    private Sprite2D _halo = null!, _start = null!, _landing = null!;
    private Tween? _clock;
    private readonly RandomNumberGenerator _random = new() { Seed = 731927 };
    private Vector2 _previous;
    private float _time, _distance, _landedAt = -1;
    private int _next;
    public bool IsPlaying => _clock is not null;

    public override void _Ready()
    {
        _halo = GetNode<Sprite2D>("TravelHalo");
        _start = GetNode<Sprite2D>("StartGlint");
        _landing = GetNode<Sprite2D>("LandingGlow");
        _motes = GetNode("Motes").GetChildren().Cast<Sprite2D>().Select(sprite => new Mote(sprite)).ToArray();
        Clear();
    }

    public void Begin(Vector2 localCenter)
    {
        Clear(); Show();
        _previous = localCenter;
        _halo.Position = _start.Position = localCenter;
        _halo.Show(); _start.Show();
        // The same UI clock policy as OpeningHeroSeat's accepted motion. No
        // process delta from the battle or gameplay clock enters this effect.
        _clock = CreateTween().SetIgnoreTimeScale(true).SetPauseMode(Tween.TweenPauseMode.Process);
        _clock.TweenMethod(Callable.From<float>(Advance), 0f, .72f, .72)
            .SetTrans(Tween.TransitionType.Linear);
        _clock.TweenCallback(Callable.From(Clear));
        Advance(0);
    }

    public void Track(Vector2 localCenter)
    {
        if (_clock is null || _landedAt >= 0) return;
        var segment = localCenter - _previous;
        var length = segment.Length();
        if (length > .01f)
        {
            var tangent = segment / length;
            var normal = new Vector2(-tangent.Y, tangent.X);
            // Distance-based emission also fills a longer render step, rather
            // than leaving a gap or moving the old tail with the new head.
            var first = 10f - _distance;
            for (var distance = first; distance <= length; distance += 10f)
            {
                var position = _previous + tangent * distance + normal * _random.RandfRange(-13, 13);
                Emit(position, -tangent * _random.RandfRange(9, 24) + normal * _random.RandfRange(-20, 20),
                    _random.RandfRange(.25f, .32f), _random.RandfRange(.18f, .32f));
            }
            _distance = (_distance + length) % 10f;
        }
        _previous = localCenter;
        _halo.Position = localCenter;
    }

    public void Land(Vector2 localCenter)
    {
        if (_clock is null) return;
        Track(localCenter);
        _landedAt = _time;
        _landing.Position = localCenter;
        _landing.Show();
        _halo.Hide();
        for (var index = 0; index < 10; index++)
        {
            var angle = index * Mathf.Tau / 10f + _random.RandfRange(-.16f, .16f);
            var outward = Vector2.FromAngle(angle);
            Emit(localCenter + outward * _random.RandfRange(31, 39), outward * _random.RandfRange(65, 105),
                _random.RandfRange(.24f, .30f), _random.RandfRange(.26f, .40f));
        }
        Advance(_time);
    }

    private void Emit(Vector2 position, Vector2 velocity, float life, float size)
    {
        var mote = _motes[_next++ % _motes.Length];
        mote.Origin = position; mote.Velocity = velocity;
        mote.Born = _time; mote.Life = life; mote.Size = size;
        mote.Spin = _random.RandfRange(-2, 2); mote.Alive = true;
        mote.Sprite.Position = position;
        mote.Sprite.Scale = Vector2.One * size;
        mote.Sprite.Modulate = new Color(1, _random.RandfRange(.72f, .9f), .42f, .85f);
        mote.Sprite.Show();
    }

    private void Advance(float time)
    {
        _time = time;
        _start.Scale = Vector2.One * Mathf.Lerp(.65f, 1.05f, Mathf.Min(time / .16f, 1));
        _start.Modulate = new Color(1, .86f, .55f, Mathf.Max(0, 1 - time / .16f));
        _halo.Scale = Vector2.One * (1.70f - Mathf.Min(time / .38f, 1) * .25f);
        _halo.Modulate = new Color(1, .70f, .28f, .72f);
        if (_landedAt >= 0)
        {
            var age = Mathf.Clamp((time - _landedAt) / .31f, 0, 1);
            _landing.Scale = Vector2.One * Mathf.Lerp(1.35f, 2.10f, 1 - (1 - age) * (1 - age));
            _landing.Modulate = new Color(1, .77f, .37f, (1 - age) * (1 - age) * .9f);
        }
        foreach (var mote in _motes)
        {
            if (!mote.Alive) continue;
            var age = (time - mote.Born) / mote.Life;
            if (age >= 1) { mote.Alive = false; mote.Sprite.Hide(); continue; }
            mote.Sprite.Position = mote.Origin + mote.Velocity * (time - mote.Born) * (1 - .38f * age);
            mote.Sprite.Rotation = mote.Spin * age;
            mote.Sprite.Scale = Vector2.One * mote.Size * Mathf.Lerp(1, .25f, age);
            var color = mote.Sprite.Modulate;
            color.A = 1.1f * (1 - age) * (1 - age);
            mote.Sprite.Modulate = color;
        }
    }

    public void Clear()
    {
        _clock?.Kill(); _clock = null;
        _time = _distance = 0; _landedAt = -1; _next = 0;
        foreach (var mote in _motes) { mote.Alive = false; mote.Sprite.Hide(); }
        Hide();
        _halo?.Hide(); _start?.Hide(); _landing?.Hide();
    }

    public override void _ExitTree() => Clear();
}
