using System;
using Godot;

namespace TowerAutobattler.UI;

// Explicitly injected UI callbacks; the command owner remains responsible for persistence.
public partial class EquipmentDropZone : PanelContainer
{
    public Func<string, bool>? CanReceive { get; set; }
    public Action<string>? Receive { get; set; }
    public string DragScope { get; set; } = "run";
    public string DragContext { get; set; } = string.Empty;
    private string _normalStyle = "ContextSidebarPanel";
    public override void _Ready()
    {
        _normalStyle = ThemeTypeVariation;
        MouseExited += ClearHighlight;
    }
    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (!EquipmentSlotButton.TryEquipmentId(data, out var id)) return false;
        var allowed = EquipmentSlotButton.HasScope(data, DragScope) && EquipmentSlotButton.HasContext(data, DragContext) && CanReceive?.Invoke(id) == true;
        ThemeTypeVariation = allowed ? "EquipmentDropValid" : "EquipmentDropInvalid";
        return allowed;
    }
    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (EquipmentSlotButton.HasScope(data, DragScope) && EquipmentSlotButton.HasContext(data, DragContext) && EquipmentSlotButton.TryEquipmentId(data, out var id) && CanReceive?.Invoke(id) == true)
            Receive?.Invoke(id);
        ClearHighlight();
    }
    public override void _Notification(int what)
    {
        if (what == NotificationDragEnd) ClearHighlight();
    }
    private void ClearHighlight() => ThemeTypeVariation = _normalStyle;
}
