using Godot;

namespace TowerAutobattler.UI;

// A small lifecycle component. The popup itself is an authored scene, shared one-at-a-time.
public partial class BattleLabHoverHint : Node
{
    private static PackedScene? _hintScene;
    private static PackedScene? _tooltipScene;
    private static BattleLabHoverHint? _active;
    private Control? _target;
    private BattleLabTooltipInfo? _info;
    private BattleLabTooltip? _tooltip;
    private Timer _delay = null!;
    private bool _hovered, _keyboardFocus, _suppressed;

    public static void Bind(Control target, BattleLabTooltipInfo? info)
    {
        var hint = target.GetNodeOrNull<BattleLabHoverHint>("BattleLabHoverHint");
        if (hint is null)
        {
            if (info is null) return;
            _hintScene ??= GD.Load<PackedScene>("res://scenes/ui/components/BattleLabHoverHint.tscn");
            hint = _hintScene.Instantiate<BattleLabHoverHint>();
            hint.Name = "BattleLabHoverHint";
            target.AddChild(hint);
        }
        hint.Attach(target);
        hint.SetInfo(info);
    }

    public static void HideAll(bool suppressUntilExit = false)
    {
        if (_active is null || !GodotObject.IsInstanceValid(_active)) { _active = null; return; }
        _active._suppressed |= suppressUntilExit;
        _active.Dismiss();
    }

    public override void _Ready()
    {
        _delay = GetNode<Timer>("Delay");
        _delay.Timeout += ShowHint;
        SetProcess(false);
        SetProcessInput(false);
    }

    private void Attach(Control target)
    {
        if (_target == target) return;
        _target = target;
        _keyboardFocus = target.HasFocus() && !Input.IsMouseButtonPressed(MouseButton.Left);
        target.MouseEntered += OnMouseEntered;
        target.MouseExited += OnMouseExited;
        target.FocusEntered += OnFocusEntered;
        target.FocusExited += OnFocusExited;
        target.VisibilityChanged += OnVisibilityChanged;
        target.GuiInput += OnTargetInput;
    }

    private void SetInfo(BattleLabTooltipInfo? info)
    {
        if (_target is not null && info is not null) _target.TooltipText = string.Empty;
        if (_info == info) return;
        _info = info;
        if (info is null) Dismiss();
        else if (_active == this && _tooltip is { Visible: true } && _target is not null)
            _tooltip.Present(info, _target, _keyboardFocus);
        // Live battle facts can refresh faster than the delay. Keep the first hover deadline.
        else if (_active == this && _delay is not null && !_delay.IsStopped()) return;
        else if (_hovered || _keyboardFocus) Schedule();
    }

    private void OnMouseEntered() { _hovered = true; _suppressed = false; _keyboardFocus = false; Schedule(); }
    private void OnMouseExited() { _hovered = false; _suppressed = false; if (!_keyboardFocus) Dismiss(); }
    private void OnFocusEntered()
    {
        _keyboardFocus = !Input.IsMouseButtonPressed(MouseButton.Left);
        _suppressed = false;
        if (_keyboardFocus) Schedule();
    }
    private void OnFocusExited() { _keyboardFocus = false; if (!_hovered) Dismiss(); }
    private void OnVisibilityChanged() { if (_target?.IsVisibleInTree() != true) Dismiss(); }
    private void OnTargetInput(InputEvent input)
    {
        if (input is InputEventMouseButton { Pressed: true } ||
            (input is InputEventKey { Pressed: true } key && key.Keycode is Key.Enter or Key.Space or Key.Escape))
        { _suppressed = true; Dismiss(); }
    }

    private void Schedule()
    {
        if (!CanShow()) return;
        _delay ??= GetNode<Timer>("Delay");
        // Children may already be exiting when a cell clears its drag state.
        if (!_delay.IsInsideTree()) return;
        HideAll();
        _active = this;
        _delay.Start();
        SetProcess(true);
    }

    private bool CanShow() => IsInsideTree() && _info is not null && !_suppressed && _target?.IsVisibleInTree() == true &&
        (_hovered || _keyboardFocus && _target.HasFocus()) && !Input.IsMouseButtonPressed(MouseButton.Left);

    private void ShowHint()
    {
        if (!CanShow() || _active != this) { Dismiss(); return; }
        if (_tooltip is null || !GodotObject.IsInstanceValid(_tooltip))
        {
            _tooltipScene ??= GD.Load<PackedScene>("res://scenes/ui/components/BattleLabTooltip.tscn");
            _tooltip = _tooltipScene.Instantiate<BattleLabTooltip>();
            AddChild(_tooltip);
        }
        _tooltip.Present(_info!, _target!, _keyboardFocus);
        SetProcessInput(true);
    }

    public override void _Process(double delta)
    {
        if (!CanShow()) Dismiss();
    }

    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouseButton { Pressed: true } || input.IsActionPressed("ui_cancel"))
        { _suppressed = true; Dismiss(); }
    }

    private void Dismiss()
    {
        _delay?.Stop();
        if (_tooltip is not null && GodotObject.IsInstanceValid(_tooltip)) _tooltip.Visible = false;
        if (_active == this) _active = null;
        SetProcess(false);
        SetProcessInput(false);
    }

    public override void _ExitTree()
    {
        Dismiss();
        if (_target is not null && GodotObject.IsInstanceValid(_target))
        {
            _target.MouseEntered -= OnMouseEntered;
            _target.MouseExited -= OnMouseExited;
            _target.FocusEntered -= OnFocusEntered;
            _target.FocusExited -= OnFocusExited;
            _target.VisibilityChanged -= OnVisibilityChanged;
            _target.GuiInput -= OnTargetInput;
        }
        if (_delay is not null) _delay.Timeout -= ShowHint;
        _target = null;
        _info = null;
    }
}
