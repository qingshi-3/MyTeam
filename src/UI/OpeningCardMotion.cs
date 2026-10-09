using System;
using Godot;

namespace TowerAutobattler.UI;

// The authored opening card is one object: frame, portrait and readable entries
// share this transform. Containers own layout; entry hit areas follow the art.
public partial class OpeningCardMotion : Node, IUiMotionHost
{
    public Func<bool> ReduceUiMotion { get; set; } = () => false;
    private HeroLibraryTile _card = null!;
    private Tween? _tween;
    private Control _glow = null!;
    private float _glowEnergy;
    private double _lightPhase;
    private bool _selected, _down, _hovered, _reduced, _entering;
    private double _delay;

    public override void _Ready()
    {
        _card = GetParent<HeroLibraryTile>();
        _glow = _card.GetNode<Control>("SelectionGlow");
        _card.OffsetTransformEnabled = true;
        _card.OffsetTransformVisualOnly = false;
        _card.OffsetTransformPivotRatio = new Vector2(.5f, .5f);
        _card.ButtonDown += Press;
        _card.ButtonUp += Release;
        _card.VisibilityChanged += Visibility;
        Enter();
    }

    public void SetSelected(bool selected)
    {
        if (_selected == selected) return;
        _selected = selected;
        if (!_entering) Refresh();
    }

    private void Enter()
    {
        Settle();
        if (!_card.IsVisibleInTree() || ReduceUiMotion()) return;
        _entering = true;
        _delay = .075 * (_card.GetIndex() % 3) + .04 * (_card.GetIndex() / 3);
        _card.Modulate = new Color(1, 1, 1, 0);
        _card.OffsetTransformScale = Vector2.One * .94f;
        _card.OffsetTransformPosition = new Vector2(0, 30);
    }

    public override void _Process(double delta)
    {
        if (!_card.IsVisibleInTree()) return;
        _lightPhase += delta;
        var lightTarget = _selected ? .72f : _hovered ? .16f : 0;
        _glowEnergy = Mathf.MoveToward(_glowEnergy, lightTarget, (float)delta * 4.5f);
        var breath = _selected && !ReduceUiMotion() ? 1f + .07f * Mathf.Sin((float)_lightPhase * 2f) : 1f;
        _glow.Modulate = new Color(1, 1, 1, _glowEnergy * breath);
        var reduced = ReduceUiMotion();
        if (reduced != _reduced)
        {
            _reduced = reduced;
            if (reduced) Settle();
            else Refresh();
        }
        if (_entering && _delay >= 0)
        {
            _delay -= delta;
            if (_delay < 0)
            {
                _tween = NewTween();
                _tween.TweenProperty(_card, "offset_transform_scale", Vector2.One, .36)
                    .SetTrans(Tween.TransitionType.Back);
                _tween.TweenProperty(_card, "offset_transform_position", Vector2.Zero, .36);
                _tween.TweenProperty(_card, "modulate:a", 1f, .13);
                _tween.Chain().TweenCallback(Callable.From(() => { _entering = false; Refresh(); }));
            }
        }
        var hovered = GetViewport().GuiGetHoveredControl();
        var inside = hovered == _card || hovered is not null && _card.IsAncestorOf(hovered);
        // The original per-entry tooltip owns its reading bridge and reveal.
        inside |= _card.Disclosure.IsOpen;
        if (inside == _hovered) return;
        _hovered = inside;
        if (!_entering) Refresh();
    }

    private void Press() { _down = true; _entering = false; Refresh(); }
    private void Release() { _down = false; Refresh(); }
    private void Visibility() { if (_card.IsVisibleInTree()) Enter(); else Settle(); }

    private void Refresh()
    {
        _tween?.Kill();
        _card.Modulate = Colors.White;
        _card.ZIndex = _hovered ? 20 : _selected ? 2 : 0;
        if (ReduceUiMotion() || !_card.IsVisibleInTree()) { Settle(); return; }
        var scale = _down ? .974f : _hovered ? 1.025f : 1f;
        var y = _down ? 1 : _hovered ? -5 : _selected ? -3 : 0;
        _tween = NewTween();
        _tween.TweenProperty(_card, "offset_transform_scale", Vector2.One * scale, _down ? .065 : .24)
            .SetTrans(_down ? Tween.TransitionType.Cubic : Tween.TransitionType.Back);
        _tween.TweenProperty(_card, "offset_transform_position", new Vector2(0, y), _down ? .065 : .20);
    }

    private Tween NewTween() => CreateTween().SetIgnoreTimeScale(true).SetPauseMode(Tween.TweenPauseMode.Process)
        .SetParallel().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

    private void Settle()
    {
        _tween?.Kill(); _tween = null;
        _entering = _down = false;
        _card.OffsetTransformScale = Vector2.One;
        _card.OffsetTransformPosition = Vector2.Zero;
        _card.Modulate = Colors.White;
        _card.ZIndex = 0;
        _glowEnergy = _selected ? .72f : 0;
        _glow.Modulate = new Color(1, 1, 1, _glowEnergy);
    }

    public override void _ExitTree()
    {
        Settle();
        _card.ButtonDown -= Press;
        _card.ButtonUp -= Release;
        _card.VisibilityChanged -= Visibility;
    }
}
