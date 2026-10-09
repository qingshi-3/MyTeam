using System;
using Godot;

namespace TowerAutobattler.UI;

// Bound from the saved draft. A successful toggle supplies the source card's
// position; the portrait travels to its permanent seat without duplicating data.
public partial class OpeningHeroSeat : Button, IUiMotionHost
{
    public Func<bool> ReduceUiMotion { get; set; } = () => false;
    public string StableId { get; private set; } = string.Empty;
    public Control Portrait => _portrait;
    private UnitPortrait _portrait = null!;
    private Tween? _arrival;
    private Control _glow = null!;
    private OpeningSelectionLight _selectionLight = null!;

    public override void _Ready()
    {
        _portrait = GetNode<UnitPortrait>("Portrait");
        _glow = GetNode<Control>("ArrivalGlow");
        _selectionLight = GetNode<OpeningSelectionLight>("SelectionLight");
        _portrait.OffsetTransformEnabled = true;
        _portrait.OffsetTransformVisualOnly = true;
        _portrait.OffsetTransformPivotRatio = new Vector2(.5f, .5f);
        VisibilityChanged += Visibility;
    }

    public void Bind(HeroSelectionViewModel? model)
    {
        var changed = StableId != (model?.StableId ?? string.Empty);
        if (changed) CancelArrival();
        StableId = model?.StableId ?? string.Empty;
        Disabled = model is null;
        GetNode<Control>("EmptyRing").Visible = model is null;
        GetNode<Control>("Ordinal").Visible = model is null;
        _portrait.Visible = model is not null;
        GetNode<Label>("Name").Text = model?.Definition.DisplayName ?? string.Empty;
        AccessibilityName = model is null ? "空英雄席位" : model.Definition.DisplayName + "，取消选择";
        if (model is not null) _portrait.Bind(model.Definition.Portrait, model.Definition.Icon);
    }

    public void ArriveFrom(Vector2 sourceCenter)
    {
        CancelArrival();
        if (ReduceUiMotion() || !IsVisibleInTree() || StableId.Length == 0) return;
        var start = _portrait.GetGlobalTransformWithCanvas().AffineInverse() * sourceCenter - _portrait.Size * .5f;
        var bend = new Vector2(start.X * .48f, Mathf.Min(start.Y - 24, -48));
        _portrait.ZIndex = 45;
        _portrait.OffsetTransformPosition = start;
        _portrait.OffsetTransformScale = Vector2.One * 1.12f;
        _selectionLight.Begin(LightCenter(start));
        _arrival = CreateTween().SetIgnoreTimeScale(true).SetPauseMode(Tween.TweenPauseMode.Process);
        _arrival.TweenMethod(Callable.From<float>(progress =>
        {
            var remaining = 1 - progress;
            _portrait.OffsetTransformPosition = remaining * remaining * start + 2 * remaining * progress * bend;
            _portrait.OffsetTransformScale = Vector2.One * Mathf.Lerp(1.12f, .94f, progress);
            _selectionLight.Track(LightCenter(_portrait.OffsetTransformPosition));
        }), 0f, 1f, .38).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _arrival.TweenCallback(Callable.From(() =>
        {
            _glow.Modulate = Colors.White;
            _selectionLight.Land(LightCenter(Vector2.Zero));
        }));
        _arrival.TweenProperty(_portrait, "offset_transform_scale", Vector2.One, .18)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        _arrival.Parallel().TweenProperty(_glow, "modulate:a", 0f, .34);
        _arrival.TweenCallback(Callable.From(CancelArrival));
    }

    public override void _Process(double delta)
    {
        if (_arrival is not null && ReduceUiMotion()) CancelArrival();
    }

    public void CancelArrival()
    {
        _arrival?.Kill();
        _arrival = null;
        _selectionLight?.Clear();
        if (_portrait is null) return;
        _portrait.OffsetTransformPosition = Vector2.Zero;
        _portrait.OffsetTransformScale = Vector2.One;
        _portrait.Modulate = Colors.White;
        _portrait.ZIndex = 0;
        _glow.Modulate = new Color(1, 1, 1, 0);
    }

    // OffsetTransformVisualOnly deliberately keeps hit-test bounds at the seat.
    // Derive the visible center from the very same offset as the portrait Tween.
    private Vector2 LightCenter(Vector2 visualOffset) =>
        _selectionLight.GetGlobalTransformWithCanvas().AffineInverse() *
        (_portrait.GetGlobalTransformWithCanvas() * (_portrait.Size * .5f + visualOffset));

    private void Visibility() { if (!IsVisibleInTree()) CancelArrival(); }
    public override void _ExitTree() { VisibilityChanged -= Visibility; CancelArrival(); }
}
