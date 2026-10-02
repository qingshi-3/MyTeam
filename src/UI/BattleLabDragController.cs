using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Content;
using TowerAutobattler.Traits;

namespace TowerAutobattler.UI;

internal sealed class BattleLabSelection
{
    public string Prototype { get; set; } = "";
    public string InstanceId { get; set; } = "";
    public BattleLabSide Side { get; set; }
}

// Owns one pointer drag, its authored visual and confirmed placement/recall feedback.
internal sealed class BattleLabDragController(BattleLabEditorSession _editor, BattleLabSelection _selection,
    Control _host, Control _libraryPane, Label _placementHint, DeploymentFootprintLayer _footprints,
    IReadOnlyDictionary<Vector2I, BattleLabBoardCell> _cells, IReadOnlyList<BattleLabLibraryCard> _cards,
    Func<Vector2, bool> _toolContains, Func<Vector2, bool> _originContains,
    Func<bool> _reduceMotion, Action<string, bool> _feedback)
{
    private UiDragVisual? _dragMotion;
    private string _dragContentId = "", _dragInstanceId = "";
    private BattleLabSide _dragSide;
    private bool _dragging;
    public bool IsDragging => _dragging;
    private BattleLabSession _session => _editor.Session;
    private BattleLabContentIndex _content => _editor.Content;
    private static string SideName(BattleLabSide side) => side == BattleLabSide.Player ? "A 队" : "B 队";
    private string UnitName(string id) => _content.TryGetUnit(id, out var unit) ? unit.DisplayName : id;
    public void BeginLibraryDrag(string contentId, BattleLabSide side)
    {
        BattleLabHoverHint.HideAll(true);
        _selection.Prototype = string.Empty; _selection.InstanceId = string.Empty;
        _dragContentId = contentId; _dragInstanceId = string.Empty; _dragSide = side; _dragging = true;
        foreach (var card in _cards) card.SetSelected(false);
        BeginDragVisual(_cards.FirstOrDefault(card => card.ContentId == contentId && card.Side == side), contentId);
        _placementHint.Text = $"拖入{SideName(side)} {UnitName(contentId)} · 释放到合法空格";
        RefreshDropStates(_host.GetViewport().GetMousePosition());
    }
    public void BeginInstanceDrag(string instanceId)
    {
        BattleLabHoverHint.HideAll(true);
        if (_session?.TryGet(instanceId, out var unit) != true) return;
        _selection.Prototype = string.Empty; _selection.InstanceId = instanceId;
        _dragContentId = unit.ContentId; _dragInstanceId = instanceId; _dragSide = unit.Side; _dragging = true;
        BeginDragVisual(_cells.Values.FirstOrDefault(cell => cell.InstanceId == instanceId), unit.ContentId);
        _placementHint.Text = $"移动{SideName(unit.Side)} {UnitName(unit.ContentId)} · 可交换；拖回单位库可移除";
        RefreshDropStates(_host.GetViewport().GetMousePosition());
    }
    public void CompleteDrag(Vector2 position)
    {
        if (_session is null) { ResetDrag(); return; }
        var target = _cells.FirstOrDefault(pair => !_toolContains(position) && pair.Value.GetGlobalRect().HasPoint(position));
        var before = _session.Freeze();
        BattleLabPlacementResult? result = null;
        var instanceId = _dragInstanceId;
        if (target.Value is not null)
        {
            if (!string.IsNullOrEmpty(instanceId) && _session.TryGet(instanceId, out var existing) && existing.Cell == target.Key)
            { ResetDrag(); _feedback("位置未改变。", true); return; }
            result = string.IsNullOrEmpty(instanceId)
                ? _session.AddAndPlace(_dragContentId, _dragSide, target.Key) : _session.Move(instanceId, target.Key);
            if (result.Succeeded)
            {
                _selection.InstanceId = result.InstanceId;
                if (GodotObject.IsInstanceValid(_dragMotion)) _dragMotion!.CommitTo(target.Value);
            }
        }
        else if (!string.IsNullOrEmpty(instanceId) && _originContains(position))
        {
            var removed = _session.Recall(instanceId);
            if (removed && GodotObject.IsInstanceValid(_dragMotion)) _dragMotion!.CommitTo(_libraryPane);
            ResetDrag(); _editor.RecordChange(before, "移除单位");
            _feedback(removed ? "单位已移除，可撤销。" : "单位已经不在战场。", removed); return;
        }
        ResetDrag(); _editor.RecordChange(before, string.IsNullOrEmpty(instanceId) ? "布置单位" : "移动单位");
        _feedback(result is null ? "拖动已取消。" : result.Succeeded
            ? result.SwappedInstanceId is null ? "放置完成。" : "位置已交换。" : result.RejectionReason,
            result?.Succeeded ?? true);
    }
    public void RefreshDropStates(Vector2 mousePosition)
    {
        if (_session is null) return;
        _footprints.ShowDrag(null);
        if (GodotObject.IsInstanceValid(_dragMotion)) _dragMotion!.ClearAim();
        foreach (var (coordinate, cell) in _cells)
        {
            var prototype = new BattleLabUnitConfiguration(string.IsNullOrEmpty(_dragInstanceId) ? "lab-preview" : _dragInstanceId,
                _dragContentId, _dragSide, coordinate, []);
            if (!string.IsNullOrEmpty(_dragInstanceId) && _session.TryGet(_dragInstanceId, out var existing)) prototype = existing;
            var evaluation = BattleLabPlacementPolicy.Evaluate(_session, prototype, coordinate, !string.IsNullOrEmpty(_dragInstanceId));
            cell.ShowDropState(evaluation.Succeeded, evaluation.SwappedInstanceId is not null);
            if (!_toolContains(mousePosition) && cell.GetGlobalRect().HasPoint(mousePosition) &&
                _content?.TryGetUnit(_dragContentId, out var content) == true)
            {
                _footprints.ShowDrag(new DeploymentBodyPreview(_dragInstanceId, coordinate,
                    content.Definition.BodyRadius, (int)_dragSide), evaluation.Succeeded);
                if (GodotObject.IsInstanceValid(_dragMotion)) _dragMotion!.AimAt(cell, evaluation.Succeeded, evaluation.SwappedInstanceId is not null);
            }
        }
        if (_originContains(mousePosition) && GodotObject.IsInstanceValid(_dragMotion))
            _dragMotion!.AimAt(_libraryPane, !string.IsNullOrEmpty(_dragInstanceId));
        _host.MouseDefaultCursorShape = (!_toolContains(mousePosition) && _cells.Values.Any(cell => cell.GetGlobalRect().HasPoint(mousePosition))) ||
            _originContains(mousePosition) ? Control.CursorShape.PointingHand : Control.CursorShape.Forbidden;
    }
    public void ResetDrag()
    {
        if (GodotObject.IsInstanceValid(_dragMotion)) _dragMotion!.Release();
        _dragMotion = null;
        if (GodotObject.IsInstanceValid(_footprints)) _footprints.ShowDrag(null);
        _dragging = false; _dragContentId = string.Empty; _dragInstanceId = string.Empty;
        _host.MouseDefaultCursorShape = Control.CursorShape.Arrow;
        foreach (var cell in _cells.Values) if (GodotObject.IsInstanceValid(cell)) cell.ClearDropState();
    }

    public bool CancelPointerDrag()
    {
        if (!_dragging) return false;
        ResetDrag();
        _feedback("", true);
        return true;
    }

    private void BeginDragVisual(Control? source, string contentId)
    {
        if (source is null || _content?.TryGetUnit(contentId, out var unit) != true) return;
        if (GodotObject.IsInstanceValid(_dragMotion)) _dragMotion!.Release();
        var preview = GD.Load<PackedScene>("res://scenes/ui/components/HeroDragPreview.tscn").Instantiate<Control>();
        preview.GetNode<UnitPortrait>("Layout/Portrait").Bind(unit.Definition.Portrait, unit.Definition.Icon);
        preview.GetNode<Label>("Layout/Name").Text = unit.DisplayName;
        _dragMotion = UiDragVisual.Lift(source, preview, () => _reduceMotion(), keepTargetVisible: true);
    }
}
