using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

// Authored overlay host: information windows block the background, tool windows
// leave the board reachable for native drag/drop. Neither participates in board layout.
public partial class ContextPopup : Control, IUiMotionHost
{
    [Export] public NodePath PanelPath { get; set; } = "Panel";
    [Export] public NodePath BackdropPath { get; set; } = "Backdrop";
    [Export] public NodePath CloseButtonPath { get; set; } = "Panel/Header/Close";
    [Export] public bool Blocking { get; set; } = true;
    [Export] public bool FullViewport { get; set; }
    public event Action? Closed;
    public Func<bool>? CancelActiveInteraction { get; set; }
    public bool IsOpen => IsVisibleInTree();
    public Func<bool> ReduceUiMotion { get; set; } = () => false;
    private UiPopupMotion? _motion;
    private Control _panel = null!;
    private Control _backdrop = null!;
    private Button _close = null!;
    private Control? _opener;
    private bool _opened;
    private bool _moving;
    private const string PopupGroup = "context_popup_windows";

    public override void _Ready()
    {
        _panel = GetNode<Control>(PanelPath);
        _backdrop = GetNode<Control>(BackdropPath);
        _close = GetNode<Button>(CloseButtonPath);
        _motion = new UiPopupMotion(_panel, _backdrop, () => ReduceUiMotion());
        MouseFilter = MouseFilterEnum.Ignore;
        _backdrop.MouseFilter = MouseFilterEnum.Stop;
        _close.Pressed += Close;
        _backdrop.GuiInput += BackdropInput;
        AddToGroup(PopupGroup);
        if (FullViewport)
        {
            // A page may be inset below the global resource header. This modal
            // owns viewport space and input above that header, not page space.
            TopLevel = true;
            SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
            GetViewport().SizeChanged += FitViewport;
            FitViewport();
        }
        Hide();
    }

    public void Open(Control? opener = null)
    {
        if (_opened && IsVisibleInTree())
        {
            _backdrop.Visible = Blocking;
            if (_motion?.IsClosing == true) { _motion.Open(); _close.GrabFocus(); }
            return;
        }
        foreach (var node in GetTree().GetNodesInGroup(PopupGroup))
            if (node is ContextPopup other && other != this && other.IsOpen) other.CloseImmediately();
        _opener = opener ?? GetViewport().GuiGetFocusOwner();
        BattleLabHoverHint.HideAll(true);
        _opened = true;
        _backdrop.Visible = Blocking;
        if (FullViewport) FitViewport();
        Show();
        _motion?.Open();
        _close.GrabFocus();
    }

    public void Close()
    {
        BattleLabHoverHint.HideAll(true);
        if (!_opened || !IsVisibleInTree()) { CloseImmediately(); return; }
        CancelActiveInteraction?.Invoke();
        _moving = false;
        _motion!.Close(CloseImmediately);
    }

    public void CloseImmediately()
    {
        _motion?.Reset();
        if (!_opened) { Hide(); return; }
        CancelActiveInteraction?.Invoke();
        _opened = false;
        _moving = false;
        Hide();
        BattleLabHoverHint.HideAll(true);
        if (_opener is not null && GodotObject.IsInstanceValid(_opener) && _opener.IsVisibleInTree()
            && _opener.FocusMode != FocusModeEnum.None) _opener.GrabFocus();
        _opener = null;
        Closed?.Invoke();
    }

    private void BackdropInput(InputEvent input)
    {
        if (input is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }) return;
        GetViewport().SetInputAsHandled();
        Close();
    }

    public override void _Input(InputEvent input)
    {
        if (!IsOpen) return;
        if (_motion?.IsClosing == true)
        {
            if (Blocking) GetViewport().SetInputAsHandled();
            return;
        }
        // Tool windows can be moved aside to expose any deployment cell. The title
        // band belongs to the window; item drags below it keep their native path.
        if (!Blocking && input is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouse)
        {
            if (!mouse.Pressed && _moving)
            {
                _moving = false;
                GetViewport().SetInputAsHandled();
                return;
            }
            var titleBand = new Rect2(_panel.GlobalPosition, new Vector2(_panel.Size.X, 48));
            if (mouse.Pressed && titleBand.HasPoint(mouse.Position) && !_close.GetGlobalRect().HasPoint(mouse.Position)
                && !GetViewport().GuiIsDragging())
            {
                _moving = true;
                GetViewport().SetInputAsHandled();
                return;
            }
        }
        if (_moving && input is InputEventMouseMotion motion)
        {
            var margin = new Vector2(8, 8);
            _panel.Position = (_panel.Position + motion.Relative).Clamp(margin, (Size - _panel.Size - margin).Max(margin));
            GetViewport().SetInputAsHandled();
            return;
        }
        if (input is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.Keycode == Key.Escape)
        {
            // First Esc cancels an in-flight item; the second closes the window.
            if (GetViewport().GuiIsDragging()) GetViewport().GuiCancelDrag();
            else if (CancelActiveInteraction?.Invoke() != true) Close();
            GetViewport().SetInputAsHandled();
        }
        else if (Blocking && key.Keycode == Key.Tab)
        {
            var focusable = Descendants(_panel).Prepend(_panel)
                .Where(control => control.IsVisibleInTree() && control.FocusMode == FocusModeEnum.All
                    && control is not BaseButton { Disabled: true }).ToArray();
            if (focusable.Length > 0)
            {
                var index = Array.IndexOf(focusable, GetViewport().GuiGetFocusOwner());
                index = (index + (key.ShiftPressed ? -1 : 1) + focusable.Length) % focusable.Length;
                focusable[index].GrabFocus();
            }
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged && _opened && !IsVisibleInTree()) CloseImmediately();
    }

    public override void _ExitTree()
    {
        _motion?.Dispose();
        if (FullViewport) GetViewport().SizeChanged -= FitViewport;
        _close.Pressed -= Close;
        _backdrop.GuiInput -= BackdropInput;
        _opener = null;
        _opened = false;
    }

    private void FitViewport()
    {
        GlobalPosition = GetViewport().GetVisibleRect().Position;
        Size = GetViewport().GetVisibleRect().Size;
    }

    private static IEnumerable<Control> Descendants(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is Control control) yield return control;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
}
