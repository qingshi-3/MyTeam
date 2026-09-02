using System;
using Godot;

namespace TowerAutobattler.Presentation;

public enum BattleFloatingCueKind
{
    Damage,
    Healing,
    StatusActive,
    StatusStack,
    StatusRemoved
}

public partial class BattleFloatingCue : Label
{
    private const double TravelSeconds = .72;
    private const double FadeDelaySeconds = .42;
    private const double FadeSeconds = .3;

    private Tween? _tween;

    public event Action<BattleFloatingCue>? Finished;

    public BattleFloatingCueKind Kind { get; private set; }
    public string TargetRuntimeId { get; private set; } = string.Empty;
    public int Tick { get; private set; }
    public bool HasActiveTween => _tween is not null;

    public override void _ExitTree() => Stop();

    public void Play(
        BattleFloatingCueKind kind,
        string text,
        string targetRuntimeId,
        int tick,
        Vector2 anchor,
        int lane,
        int column)
    {
        Stop();
        Kind = kind;
        TargetRuntimeId = targetRuntimeId;
        Tick = tick;
        Text = text;
        ThemeTypeVariation = Variation(kind);
        ZIndex = Layer(kind);
        Modulate = Colors.White;
        SelfModulate = Colors.White;
        Position = anchor + new Vector2(-60 + column * 30, -34 - lane * 18);
        Visible = true;

        var tween = CreateTween().SetParallel();
        tween.SetTrans(Tween.TransitionType.Circ).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "position", Position + new Vector2(0, -34), TravelSeconds);
        tween.TweenProperty(this, "modulate:a", 0f, FadeSeconds).SetDelay(FadeDelaySeconds)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        tween.Chain().TweenCallback(Callable.From(() => Complete(tween)));
        _tween = tween;
    }

    public void Stop()
    {
        _tween?.Kill();
        _tween = null;
    }

    private void Complete(Tween tween)
    {
        if (!ReferenceEquals(_tween, tween)) return;
        _tween = null;
        Finished?.Invoke(this);
    }

    private static StringName Variation(BattleFloatingCueKind kind) => kind switch
    {
        BattleFloatingCueKind.Damage => "FloatingDamageLabel",
        BattleFloatingCueKind.Healing => "FloatingHealingLabel",
        BattleFloatingCueKind.StatusActive => "FloatingStatusActiveLabel",
        BattleFloatingCueKind.StatusStack => "FloatingStatusStackLabel",
        _ => "FloatingStatusRemovedLabel"
    };

    private static int Layer(BattleFloatingCueKind kind) => kind switch
    {
        BattleFloatingCueKind.StatusActive => 34,
        BattleFloatingCueKind.StatusStack => 33,
        BattleFloatingCueKind.Damage or BattleFloatingCueKind.Healing => 32,
        _ => 31
    };
}
