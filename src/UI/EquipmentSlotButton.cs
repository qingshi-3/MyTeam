using System;
using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

// One authored icon tile serves both an equipment instance and a hero slot.
public partial class EquipmentSlotButton : Button, IUiMotionHost
{
    public Func<bool> ReduceUiMotion { get; set; } = () => false;
    [Export] public bool InlinePresentation { get; set; }
    [Export] public bool WorkbenchPresentation { get; set; }
    public event Action<EquipmentSlotButton>? Chosen;
    public event Action<string, int>? EquipmentDropped;
    public string InstanceId { get; private set; } = string.Empty;
    public int SlotIndex { get; private set; } = -1;
    public bool AcceptEquipment { get; set; }
    private bool _dragEnabled;
    private string _itemName = string.Empty;
    private string _normalStyle = "EquipmentSocket";
    public Func<string, bool>? CanReceive { get; set; }
    public string DragScope { get; set; } = "run";
    public string DragContext { get; set; } = string.Empty;
    public event Action<EquipmentSlotButton, bool>? KeyboardActionRequested;

    public override void _Ready()
    {
        Pressed += OnPressed;
        MouseExited += ClearHighlight;
    }
    public override void _ExitTree() => Pressed -= OnPressed;

    public void Bind(string instanceId, int slotIndex, ItemDefinition? item, bool selected, bool editable)
    {
        var previousIdentity = InstanceId;
        InstanceId = instanceId;
        SlotIndex = slotIndex;
        SizeFlagsHorizontal = WorkbenchPresentation ? SizeFlags.ShrinkCenter : InlinePresentation ? SizeFlags.ExpandFill :
            slotIndex >= 0 ? SizeFlags.ShrinkCenter : SizeFlags.ShrinkBegin;
        _dragEnabled = editable && !string.IsNullOrEmpty(instanceId);
        AcceptEquipment = editable && slotIndex >= 0;
        Icon = item?.Icon;
        _itemName = item?.DisplayName ?? string.Empty;
        AccessibilityName = slotIndex >= 0
            ? $"装备槽 {slotIndex + 1} · {item?.DisplayName ?? "空槽"}"
            : _itemName;
        Text = slotIndex >= 0 ? (item is null ? (InlinePresentation ? "＋" : "空槽") : string.Empty)
            : WorkbenchPresentation ? string.Empty : item?.DisplayName ?? "空槽";
        VerticalIconAlignment = slotIndex >= 0 || WorkbenchPresentation ? VerticalAlignment.Center : VerticalAlignment.Top;
        var ordinal = GetNode<Label>("SlotOrdinal");
        ordinal.Visible = slotIndex >= 0 && !InlinePresentation;
        ordinal.Text = (slotIndex + 1).ToString();
        TooltipText = item is null ? "拖入装备。已有装备会回到背包。" :
            $"{item.DisplayName}\n{item.Description}\n{(editable ? "拖动装备 · 点击查看 · Ctrl+回车装入空槽 · Delete卸下" : "当前仅查看。")}";
        var style = WorkbenchPresentation ? "WorkbenchEquipment" : InlinePresentation ? "CardEquipment" : "EquipmentSocket";
        _normalStyle = selected ? style + "Selected" : style;
        ThemeTypeVariation = _normalStyle;
        Disabled = !editable && item is null;
        if (previousIdentity != InstanceId) UiDragVisual.Bound(this, InstanceId, "equipment-instance-id");
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (!_dragEnabled) return default;
        return UiDragVisual.Begin(this, CreatePreview(), DragPayload().AsGodotDictionary(), () => ReduceUiMotion());
    }

    public Control CreatePreview()
    {
        var preview = GD.Load<PackedScene>("res://scenes/ui/components/EquipmentDragPreview.tscn").Instantiate<Control>();
        preview.GetNode<TextureRect>("Layout/Icon").Texture = Icon;
        // Card sockets omit names, and custom hover hints clear native tooltip text.
        // The drag preview still needs the bound item's identity.
        preview.GetNode<Label>("Layout/Name").Text = _itemName;
        return preview;
    }
    public Variant DragPayload() => new Godot.Collections.Dictionary
        { ["equipment-instance-id"] = InstanceId, ["equipment-scope"] = DragScope, ["equipment-context"] = DragContext };

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (!TryEquipmentId(data, out var id)) return false;
        var allowed = HasScope(data, DragScope) && HasContext(data, DragContext) && AcceptEquipment && id != InstanceId && (CanReceive?.Invoke(id) ?? true);
        var style = WorkbenchPresentation ? "WorkbenchEquipment" : InlinePresentation ? "CardEquipment" : "EquipmentSocket";
        ThemeTypeVariation = style + (allowed ? "Valid" : "Invalid");
        UiDragVisual.Aim(data, this, allowed, allowed && !string.IsNullOrEmpty(InstanceId));
        return allowed;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (_CanDropData(atPosition, data) && TryEquipmentId(data, out var id))
            EquipmentDropped?.Invoke(id, SlotIndex);
        ClearHighlight();
    }
    public override void _Notification(int what)
    {
        if (what == NotificationDragEnd) ClearHighlight();
    }
    public override void _GuiInput(InputEvent input)
    {
        if (input is InputEventKey { Pressed: true, Echo: false } key && _dragEnabled &&
            (key.Keycode == Key.Delete || key is { CtrlPressed: true, Keycode: Key.Enter }))
        {
            KeyboardActionRequested?.Invoke(this, key.Keycode == Key.Delete);
            AcceptEvent();
        }
    }
    private void ClearHighlight() => ThemeTypeVariation = _normalStyle;

    public static bool TryEquipmentId(Variant data, out string id)
    {
        id = string.Empty;
        if (data.VariantType != Variant.Type.Dictionary) return false;
        var dictionary = data.AsGodotDictionary();
        if (!dictionary.TryGetValue("equipment-instance-id", out var value) || value.VariantType != Variant.Type.String)
            return false;
        id = value.AsString();
        return !string.IsNullOrWhiteSpace(id);
    }
    public static bool HasScope(Variant data, string scope) => data.VariantType == Variant.Type.Dictionary &&
        data.AsGodotDictionary().TryGetValue("equipment-scope", out var value) &&
        value.VariantType == Variant.Type.String && value.AsString() == scope;
    public static bool HasContext(Variant data, string context) => string.IsNullOrEmpty(context) ||
        (data.VariantType == Variant.Type.Dictionary && data.AsGodotDictionary().TryGetValue("equipment-context", out var value) &&
        value.VariantType == Variant.Type.String && value.AsString() == context);

    private void OnPressed() => Chosen?.Invoke(this);
}
