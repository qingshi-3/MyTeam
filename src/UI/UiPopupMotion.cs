using System;
using Godot;

namespace TowerAutobattler.UI;

public interface IUiMotionHost
{
    Func<bool> ReduceUiMotion { get; set; }
}

// A controller-owned transition over authored controls. The controller keeps its
// modal blocker and focus scope until the close callback; hiding an owner cancels
// the tween and restores its visual/input state immediately.
public sealed class UiPopupMotion : IDisposable
{
    private readonly Control _panel;
    private readonly Control _backdrop;
    private readonly Func<bool> _reduced;
    private readonly Color _panelColor, _backdropColor;
    private readonly Control.MouseBehaviorRecursiveEnum _mouse;
    private readonly Control.FocusBehaviorRecursiveEnum _focus;
    private Tween? _tween;
    private readonly SceneTree _tree;
    private Action? _pendingClose;
    private bool _watching;
    public bool IsClosing { get; private set; }

    public UiPopupMotion(Control panel, Control backdrop, Func<bool> reduced)
    {
        _panel = panel;
        _tree = panel.GetTree();
        _backdrop = backdrop;
        _reduced = reduced;
        _panelColor = panel.Modulate;
        _backdropColor = backdrop.Modulate;
        _mouse = panel.MouseBehaviorRecursive;
        _focus = panel.FocusBehaviorRecursive;
        panel.OffsetTransformEnabled = true;
        panel.OffsetTransformVisualOnly = false;
        panel.OffsetTransformPivotRatio = new Vector2(.5f, .5f);
    }

    public void Open()
    {
        var reversing = IsClosing;
        Stop();
        RestoreInput();
        if (_reduced()) { Reset(); return; }
        var large = _panel.Size.X >= _panel.GetViewportRect().Size.X * .85f;
        if (!reversing)
        {
            _panel.OffsetTransformScale = Vector2.One * (large ? .978f : .94f);
            _panel.OffsetTransformPosition = new Vector2(0, large ? 8 : 14);
            _panel.Modulate = new Color(_panelColor, 0);
            _backdrop.Modulate = new Color(_backdropColor, 0);
        }
        _tween = NewTween().SetParallel();
        _tween.TweenProperty(_panel, "offset_transform_scale", Vector2.One, .28)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        _tween.TweenProperty(_panel, "offset_transform_position", Vector2.Zero, .28)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        _tween.TweenProperty(_panel, "modulate", _panelColor, .10);
        _tween.TweenProperty(_backdrop, "modulate", _backdropColor, .14);
        _tween.Chain().TweenCallback(Callable.From(Reset));
    }

    public void Close(Action finished)
    {
        if (IsClosing) return;
        Stop();
        if (_reduced() || !_panel.IsVisibleInTree())
        {
            Reset();
            finished();
            return;
        }
        IsClosing = true;
        _pendingClose = finished;
        _panel.MouseBehaviorRecursive = Control.MouseBehaviorRecursiveEnum.Disabled;
        _panel.FocusBehaviorRecursive = Control.FocusBehaviorRecursiveEnum.Disabled;
        _tween = NewTween().SetParallel();
        _tween.TweenProperty(_panel, "offset_transform_scale", Vector2.One * .976f, .12)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        _tween.TweenProperty(_panel, "offset_transform_position", new Vector2(0, 6), .12);
        _tween.TweenProperty(_panel, "modulate:a", 0f, .12);
        _tween.TweenProperty(_backdrop, "modulate:a", 0f, .12);
        _tween.Chain().TweenCallback(Callable.From(() =>
        {
            Reset();
            finished();
        }));
    }

    private Tween NewTween()
    {
        _tree.ProcessFrame += CheckReducedMotion;
        _watching = true;
        return _panel.CreateTween().SetIgnoreTimeScale(true)
            .SetPauseMode(Tween.TweenPauseMode.Process).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
    }

    private void CheckReducedMotion()
    {
        if (!_reduced()) return;
        var close = _pendingClose;
        Reset();
        close?.Invoke();
    }

    private void Stop()
    {
        if (_watching) _tree.ProcessFrame -= CheckReducedMotion;
        _watching = false;
        _pendingClose = null;
        _tween?.Kill();
        _tween = null;
        IsClosing = false;
    }

    private void RestoreInput()
    {
        _panel.MouseBehaviorRecursive = _mouse;
        _panel.FocusBehaviorRecursive = _focus;
    }

    public void Reset()
    {
        Stop();
        if (GodotObject.IsInstanceValid(_panel))
        {
            _panel.OffsetTransformScale = Vector2.One;
            _panel.OffsetTransformPosition = Vector2.Zero;
            _panel.Modulate = _panelColor;
            RestoreInput();
        }
        if (GodotObject.IsInstanceValid(_backdrop)) _backdrop.Modulate = _backdropColor;
    }

    public void Dispose() => Reset();
}
