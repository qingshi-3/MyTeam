using System;
using Godot;

namespace TowerAutobattler.Presentation;

// The authored overlay and its one real-time tween share one cancellation lifetime.
internal sealed class BattleEndTransition(Control host, ColorRect overlay)
{
    private Tween? _tween;
    private bool _active;
    public bool Reported { get; private set; }
    public event Action? Completed;

    public void Begin(bool allowLabControls)
    {
        Reset();
        _active = true;
        overlay.Color = new Color(0, 0, 0, 0);
        overlay.Visible = true;
        overlay.MouseFilter = allowLabControls ? Control.MouseFilterEnum.Ignore : Control.MouseFilterEnum.Stop;
        _tween = host.CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        _tween.TweenInterval(1.1);
        _tween.TweenProperty(overlay, "color:a", 1f, .45)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        _tween.TweenCallback(Callable.From(Complete));
    }

    public void Complete()
    {
        if (!_active || Reported) return;
        Reported = true;
        _tween?.Kill();
        _tween = null;
        overlay.Color = Colors.Black;
        Completed?.Invoke();
    }

    public void Reset()
    {
        _tween?.Kill();
        _tween = null;
        _active = false;
        Reported = false;
        overlay.Visible = false;
        overlay.Color = new Color(0, 0, 0, 0);
    }
}
