using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

// Reveals independent entries inward from the card edges, within its own frame.
public partial class UiCardDisclosure : Node, IUiMotionHost
{
    public Func<bool> ReduceUiMotion { get; set; } = () => false;
    public bool IsOpen { get; private set; }
    public bool IsPinned { get; private set; }
    public IReadOnlyList<Control> Groups => _groups;
    private Control? _owner;
    private Control? _edgeShade;
    private UiPopupMotion? _shadeMotion;
    private Button? _toggle;
    private string _description = "属性与技能";
    private Control[] _groups = [];
    private readonly Dictionary<Control, UiPopupMotion> _motions = [];
    private bool _dismissed, _closing;
    private bool _keyboardNavigation;
    private bool _alwaysVisible;
    private double _hover, _leave;
    private static readonly StringName Group = "unit_card_disclosures";
    private static readonly Vector2[] From = [new(-44, 0), new(44, 0), new(0, -32), new(0, -32)];
    public override void _Ready() { AddToGroup(Group); SetProcess(false); }
    public void Configure(Control owner, Control card, Button? toggle, Control[] groups, Control? edgeShade = null,
        string description = "属性与技能", bool alwaysVisible = false)
    {
        _owner = owner; _toggle = toggle; _groups = groups;
        _description = description;
        _alwaysVisible = alwaysVisible;
        _edgeShade = edgeShade;
        if (edgeShade is not null) _shadeMotion = new UiPopupMotion(edgeShade, null, () => ReduceUiMotion(), fadeOnly: true);
        if (toggle is not null) toggle.Pressed += Toggle;
        owner.VisibilityChanged += OwnerVisibility;
        RefreshItems(); Reset(); SetProcess(!alwaysVisible);
    }
    public void RefreshItems()
    {
        if (_alwaysVisible) { Reset(); return; }
        var items = _groups.SelectMany(group => group.GetChildren().OfType<Control>())
            .Where(IsAvailable).ToArray();
        foreach (var item in _motions.Keys.Where(item => !items.Contains(item)).ToArray())
        { _motions[item].Dispose(); _motions.Remove(item); }
        foreach (var item in items)
            if (!_motions.ContainsKey(item)) _motions.Add(item, new UiPopupMotion(item, null, () => ReduceUiMotion()));
    }
    public void Pin() { if (_alwaysVisible || _owner?.IsVisibleInTree() != true) return; IsPinned = true; Open(); }
    private void Toggle() { if (IsPinned) Dismiss(); else Pin(); }
    public override void _Process(double delta)
    {
        if (_owner?.IsVisibleInTree() != true || _toggle is not null && !_toggle.IsVisibleInTree())
        { if (IsOpen) Reset(); return; }
        var pointer = _owner.GetGlobalMousePosition();
        var hovered = GetViewport().GuiGetHoveredControl();
        var inside = _owner.GetGlobalRect().HasPoint(pointer) && hovered is not null &&
            (hovered == _owner || _owner.IsAncestorOf(hovered));
        var focused = GetViewport().GuiGetFocusOwner();
        var focusInside = focused is not null && (focused == _toggle ||
            _toggle is null && _keyboardNavigation && (focused == _owner || _owner.IsAncestorOf(focused))) || IsOpen && focused is not null &&
            _groups.Any(group => group.IsAncestorOf(focused));
        if (!inside && !focusInside) _dismissed = false;
        if (GetViewport().GuiIsDragging()) { if (inside) Open(); _leave = 0; return; }
        if (!_dismissed && (inside || focusInside))
        { _leave = 0; _hover += delta; if (_hover >= .16 && (!IsOpen || _closing)) Open(); }
        else if (!IsPinned)
        {
            _hover = 0; _leave += delta;
            // The item's pointer bridge already owns its grace period. Do not
            // start a second leave delay after that tooltip has dismissed.
            if (_leave >= .24 && IsOpen && !_closing && !BattleLabHoverHint.HasActiveEntryFor(_owner)) Close();
        }
    }
    public override void _Input(InputEvent input)
    {
        if (_alwaysVisible) return;
        if (input is InputEventKey { Pressed: true }) _keyboardNavigation = true;
        else if (input is InputEventMouse) _keyboardNavigation = false;
        if (IsOpen && input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        { Dismiss(); GetViewport().SetInputAsHandled(); }
    }
    private void Open()
    {
        if (IsOpen && !_closing) { UpdateToggle(); return; }
        if (!IsPinned && GetTree().GetNodesInGroup(Group).OfType<UiCardDisclosure>()
            .Any(other => other != this && other.GetViewport() == GetViewport() && other.IsPinned)) return;
        foreach (var other in GetTree().GetNodesInGroup(Group).OfType<UiCardDisclosure>())
            if (other != this && other.GetViewport() == GetViewport()) other.Reset(true);
        IsOpen = true; _closing = false;
        _edgeShade?.Show();
        _shadeMotion?.Open(Vector2.Zero);
        for (var i = 0; i < _groups.Length; i++)
        {
            _groups[i].Show();
            foreach (var item in _groups[i].GetChildren().OfType<Control>().Where(IsAvailable))
                _motions[item].Open(From[i]);
        }
        UpdateToggle();
    }
    public void Dismiss() { if (_alwaysVisible) return; _dismissed = true; IsPinned = false; BattleLabHoverHint.HideAll(true); Close(); }
    private void Close()
    {
        if (_closing) return;
        IsPinned = false; _closing = true; UpdateToggle();
        _shadeMotion?.Close(() => _edgeShade?.Hide(), Vector2.Zero);
        var remaining = _motions.Count;
        for (var i = 0; i < _groups.Length; i++)
            foreach (var item in _groups[i].GetChildren().OfType<Control>().Where(IsAvailable))
                _motions[item].Close(() =>
                {
                    if (--remaining != 0) return;
                    IsOpen = false; _closing = false;
                    foreach (var group in _groups) group.Hide();
                }, From[i] * .45f);
    }
    public void Reset(bool suppressReopen = false)
    {
        _shadeMotion?.Reset();
        var visible = _alwaysVisible && _owner?.IsVisibleInTree() == true;
        if (_edgeShade is not null) _edgeShade.Visible = visible;
        foreach (var motion in _motions.Values) motion.Reset();
        foreach (var group in _groups) group.Visible = visible;
        IsOpen = visible; IsPinned = _closing = false; _dismissed = suppressReopen; _hover = _leave = 0; UpdateToggle();
    }
    private void UpdateToggle()
    {
        if (_toggle is null) return;
        _toggle.SetPressedNoSignal(IsPinned); _toggle.Text = IsPinned ? "收起" : "详情";
        _toggle.AccessibilityName = (IsPinned ? "收起" : "固定展开") + _description;
    }
    private void OwnerVisibility() { if (_alwaysVisible || _owner?.IsVisibleInTree() != true) Reset(); }
    private static bool IsAvailable(Control item) => !item.IsQueuedForDeletion() &&
        (!item.HasMeta("card_entry_available") || item.GetMeta("card_entry_available").AsBool());
    public override void _ExitTree()
    {
        _shadeMotion?.Dispose();
        foreach (var motion in _motions.Values) motion.Dispose();
        if (IsInstanceValid(_owner)) _owner!.VisibilityChanged -= OwnerVisibility;
        if (IsInstanceValid(_toggle)) _toggle!.Pressed -= Toggle;
    }
}
