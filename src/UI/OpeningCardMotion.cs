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
    private bool _selected, _down, _hovered, _reduced, _entering;
    private double _delay;

    public override void _Ready()
    {
        _card = GetParent<HeroLibraryTile>();
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
        _delay = .045 * _card.GetIndex();
        _card.Modulate = new Color(1, 1, 1, 0);
        _card.OffsetTransformScale = Vector2.One * 1.1f;
        _card.OffsetTransformPosition = new Vector2(0, -22);
    }

    public override void _Process(double delta)
    {
        if (!_card.IsVisibleInTree()) return;
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
                _tween.TweenProperty(_card, "offset_transform_scale", Vector2.One, .36);
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
        var scale = _down ? .985f : _hovered ? 1.035f : 1f;
        var y = _down ? 0 : _hovered ? -9 : _selected ? -6 : 0;
        _tween = NewTween();
        _tween.TweenProperty(_card, "offset_transform_scale", Vector2.One * scale, _down ? .065 : .20);
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
    }

    public override void _ExitTree()
    {
        Settle();
        _card.ButtonDown -= Press;
        _card.ButtonUp -= Release;
        _card.VisibilityChanged -= Visibility;
    }
}
