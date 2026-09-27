using System;
using Godot;

namespace TowerAutobattler.UI;

// Transient view only. Native drag/drop and the existing command owners still
// decide legality and persistence. A rebound never submits or repeats a command.
public partial class UiDragVisual : Control
{
    private const string PayloadKey = "ui-drag-visual";
    private Control _source = null!;
    private Control? _destination;
    private Control? _aimTarget;
    private Control _face = null!, _shadow = null!;
    private Label _mark = null!;
    private Func<bool> _reduced = () => false;
    private Color _sourceColor;
    private Vector2 _lastPointer, _lag;
    private float _tilt, _lift;
    private Tween? _tween;
    private Rect2 _aimBounds;
    private bool _hasAim, _finishing, _restored, _keepTargetVisible;
    public bool IsReturning { get; private set; }
    public bool IsFinishing => _finishing;
    public bool HasCommittedDestination => _destination is not null;

    public static Variant Begin(Control source, Control content, Godot.Collections.Dictionary payload, Func<bool> reduced,
        bool keepTargetVisible = false)
    {
        var visual = Lift(source, content, reduced, keepTargetVisible);
        var anchor = GD.Load<PackedScene>("res://scenes/ui/components/UiDragAnchor.tscn").Instantiate<UiDragAnchor>();
        anchor.Visual = visual;
        payload[PayloadKey] = visual;
        source.SetDragPreview(anchor);
        return payload;
    }

    // The lab already owns a pointer-drag lifecycle. It supplies that lifecycle
    // explicitly instead of starting a second native drag over it.
    public static UiDragVisual Lift(Control source, Control content, Func<bool> reduced, bool keepTargetVisible = false)
    {
        BattleLabHoverHint.HideAll(true);
        var visual = GD.Load<PackedScene>("res://scenes/ui/components/UiDragVisual.tscn").Instantiate<UiDragVisual>();
        visual._source = source;
        visual._sourceColor = source.Modulate;
        visual._reduced = reduced;
        visual._keepTargetVisible = keepTargetVisible;
        source.GetViewport().AddChild(visual);
        visual._face.AddChild(content);
        content.Position = Vector2.Zero;
        content.Size = content.GetCombinedMinimumSize();
        visual.Size = content.Size;
        visual._face.Size = visual._shadow.Size = visual.Size;
        visual._face.PivotOffset = visual._shadow.PivotOffset = visual.Size * .5f;
        visual._lastPointer = source.GetViewport().GetMousePosition();
        visual.Position = keepTargetVisible
            ? visual.ResolveDragPosition(visual._lastPointer, Vector2.Zero)
            : visual._lastPointer - visual.Size * .5f + new Vector2(0, -24);
        source.Modulate = new Color(visual._sourceColor, visual._sourceColor.A * .38f);
        visual._mark.Position = new Vector2(visual.Size.X - 20, visual.Size.Y - 8);
        if (reduced()) visual._lift = 1;
        else
        {
            visual._tween = visual.CreateTween().SetIgnoreTimeScale(true).SetPauseMode(Tween.TweenPauseMode.Process);
            visual._tween.TweenMethod(Callable.From<float>(value => visual._lift = value), 0f, 1f, .18)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }
        return visual;
    }

    public override void _Ready()
    {
        MouseBehaviorRecursive = MouseBehaviorRecursiveEnum.Disabled;
        FocusBehaviorRecursive = FocusBehaviorRecursiveEnum.Disabled;
        _face = GetNode<Control>("Face");
        _shadow = GetNode<Control>("Shadow");
        _mark = GetNode<Label>("Mark");
    }

    public override void _Process(double delta)
    {
        if (_finishing)
        {
            var target = IsReturning ? _source : _destination;
            if (_reduced() || !IsInstanceValid(target) || !target!.IsVisibleInTree()) Finish();
            return;
        }
        if (!IsInstanceValid(_source) || !_source.IsVisibleInTree())
        {
            // A successful command may remove the inventory/bench source before
            // the native preview is freed. The bound destination owns that flight.
            if (IsInstanceValid(_destination)) Release();
            else Finish();
            return;
        }
        var pointer = GetViewport().GetMousePosition();
        var movement = pointer - _lastPointer;
        _lastPointer = pointer;
        var dt = Mathf.Max((float)delta, .001f);
        var blend = 1f - Mathf.Exp(-18 * dt);
        var reduced = _reduced();
        var velocity = movement / dt;
        _lag = _lag.Lerp(reduced ? Vector2.Zero : (-velocity * .012f).LimitLength(12), blend);
        _tilt = Mathf.Lerp(_tilt, reduced ? 0 : Mathf.Clamp(velocity.X * .000055f, -.075f, .075f), blend);
        if (reduced) { _tween?.Kill(); _lift = 1; _lag = Vector2.Zero; _tilt = 0; }
        Position = _keepTargetVisible
            ? ResolveDragPosition(pointer, _lag)
            : pointer - Size * .5f + new Vector2(0, reduced ? -24 : -12 - 12 * _lift) + _lag;
        _face.Scale = Vector2.One * (reduced ? 1 : .96f + .12f * _lift);
        _face.Rotation = _tilt;
        _shadow.Scale = Vector2.One * (reduced ? 1 : 1 + .03f * _lift);
        _shadow.Position = new Vector2(4, reduced ? 9 : 6 + 10 * _lift);
        _shadow.Rotation = _tilt * .5f;
        _mark.Visible = _hasAim && _aimBounds.HasPoint(pointer);
    }

    private Vector2 ResolveDragPosition(Vector2 pointer, Vector2 lag)
    {
        var viewport = GetViewport().GetVisibleRect();
        var scale = 1.08f;
        var tilt = .075f;
        var cosine = Mathf.Cos(tilt);
        var sine = Mathf.Sin(tilt);
        var rotated = new Vector2(
            (Size.X * cosine + Size.Y * sine) * scale,
            (Size.X * sine + Size.Y * cosine) * scale);
        // Includes full lift/tilt, 12 px lag, the lowered shadow and a small clear edge.
        var overflow = new Vector2(
            Mathf.Max((rotated.X - Size.X) * .5f, 4 + Size.X * .015f) + 18,
            Mathf.Max((rotated.Y - Size.Y) * .5f, 16 + Size.Y * .015f) + 18);
        const float gap = 8;

        Vector2[] candidates;
        var currentAim = _hasAim && _aimBounds.HasPoint(pointer);
        if (currentAim)
        {
            var end = _aimBounds.Position + _aimBounds.Size;
            candidates =
            [
                new(pointer.X - Size.X * .5f, _aimBounds.Position.Y - gap - Size.Y - overflow.Y),
                new(pointer.X - Size.X * .5f, end.Y + gap + overflow.Y),
                new(_aimBounds.Position.X - gap - Size.X - overflow.X, pointer.Y - Size.Y * .5f),
                new(end.X + gap + overflow.X, pointer.Y - Size.Y * .5f)
            ];
        }
        else
        {
            // Keep the thumbnail clear of the pointer even when no receiver is active.
            candidates =
            [
                new(pointer.X + gap + overflow.X, pointer.Y - gap - Size.Y - overflow.Y),
                new(pointer.X - gap - Size.X - overflow.X, pointer.Y - gap - Size.Y - overflow.Y),
                new(pointer.X + gap + overflow.X, pointer.Y + gap + overflow.Y),
                new(pointer.X - gap - Size.X - overflow.X, pointer.Y + gap + overflow.Y)
            ];
        }

        foreach (var candidate in candidates)
        {
            var position = candidate + lag;
            var footprint = ExpandedRect(position, overflow);
            if (Contains(viewport, footprint) && (!currentAim || !footprint.Intersects(_aimBounds))) return position;
        }

        foreach (var candidate in candidates)
        {
            var position = ClampToViewport(candidate + lag, viewport, overflow);
            if (!currentAim || !ExpandedRect(position, overflow).Intersects(_aimBounds)) return position;
        }
        return ClampToViewport(candidates[0] + lag, viewport, overflow);
    }

    private Rect2 ExpandedRect(Vector2 position, Vector2 overflow) =>
        new(position - overflow, Size + overflow * 2);

    private Vector2 ClampToViewport(Vector2 position, Rect2 viewport, Vector2 overflow)
    {
        var minimum = viewport.Position + overflow;
        var maximum = viewport.Position + viewport.Size - Size - overflow;
        return new Vector2(
            Mathf.Clamp(position.X, minimum.X, Mathf.Max(minimum.X, maximum.X)),
            Mathf.Clamp(position.Y, minimum.Y, Mathf.Max(minimum.Y, maximum.Y)));
    }

    private static bool Contains(Rect2 outer, Rect2 inner)
    {
        var outerEnd = outer.Position + outer.Size;
        var innerEnd = inner.Position + inner.Size;
        return inner.Position.X >= outer.Position.X && inner.Position.Y >= outer.Position.Y &&
               innerEnd.X <= outerEnd.X && innerEnd.Y <= outerEnd.Y;
    }

    // Receivers provide their current domain evaluation, never a second UI rule.
    public static void Aim(Variant data, Control target, bool allowed, bool swap = false)
    {
        if (From(data) is not { } visual || visual._finishing) return;
        visual.AimAt(target, allowed, swap);
    }

    public void ClearAim() { _hasAim = false; _aimTarget = null; }
    public void AimAt(Control target, bool allowed, bool swap = false)
    {
        _aimBounds = target.GetGlobalRect();
        _hasAim = true;
        _aimTarget = allowed ? target : null;
        _mark.Text = allowed ? swap ? "⇄" : "＋" : "×";
        _mark.Modulate = allowed ? new Color("c9e4d4") : new Color("e9b0a4");
    }
    public void CommitTo(Control target) { if (!_finishing) _destination = target; }

    public static void Committed(Viewport viewport, string identity)
    {
        var data = viewport.GuiGetDragData();
        if (From(data) is not { } visual || visual._finishing ||
            !EquipmentSlotButton.TryEquipmentId(data, out var dragged) || dragged != identity) return;
        // Bound() may already have supplied the precise refreshed equipment slot.
        visual._destination ??= visual._aimTarget;
    }

    // Called only by a view binding a changed identity AFTER the command's
    // authoritative result. Engine drag-success alone does not mean save-success.
    public static void Bound(Control target, string identity, string identityKey)
    {
        if (string.IsNullOrEmpty(identity)) return;
        var data = target.GetViewport().GuiGetDragData();
        if (From(data) is not { } visual || visual._finishing || target == visual._source) return;
        var payload = data.AsGodotDictionary();
        if (payload.TryGetValue(identityKey, out var value) && value.AsString() == identity)
            visual._destination = target;
    }

    private static UiDragVisual? From(Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary ||
            !data.AsGodotDictionary().TryGetValue(PayloadKey, out var value) || value.VariantType != Variant.Type.Object) return null;
        var visual = value.AsGodotObject() as UiDragVisual;
        return IsInstanceValid(visual) ? visual : null;
    }

    public void Release()
    {
        if (_finishing || !IsInsideTree()) return;
        _finishing = true;
        RestoreSource();
        _tween?.Kill();
        _mark.Hide();
        IsReturning = !IsInstanceValid(_destination);
        var target = IsReturning ? _source : _destination!;
        if (_reduced() || !IsInstanceValid(target) || !target.IsVisibleInTree()) { Finish(); return; }
        var rect = target.GetGlobalRect();
        var scale = Mathf.Clamp(Mathf.Min(rect.Size.X / Size.X, rect.Size.Y / Size.Y), .4f, 1);
        _tween = CreateTween().SetIgnoreTimeScale(true).SetPauseMode(Tween.TweenPauseMode.Process).SetParallel();
        var duration = IsReturning ? .22 : .16;
        _tween.TweenProperty(this, "position", rect.GetCenter() - Size * .5f, duration)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _tween.TweenProperty(_face, "rotation", 0f, duration);
        _tween.TweenProperty(_face, "scale", Vector2.One * scale, duration)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.In);
        _tween.TweenProperty(_shadow, "modulate:a", 0f, duration);
        _tween.TweenProperty(this, "modulate:a", 0f, .07).SetDelay(duration - .07);
        _tween.Chain().TweenCallback(Callable.From(Finish));
    }

    private void RestoreSource()
    {
        if (_restored) return;
        _restored = true;
        if (IsInstanceValid(_source)) _source.Modulate = _sourceColor;
    }
    private void Finish() { RestoreSource(); QueueFree(); }
    public override void _ExitTree() { _tween?.Kill(); RestoreSource(); }
}
