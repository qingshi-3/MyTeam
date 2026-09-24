using System;
using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

// One authored icon tile serves both an equipment instance and a hero slot.
public partial class EquipmentSlotButton : Button
{
    public event Action<EquipmentSlotButton>? Chosen;
    public event Action<string, int>? EquipmentDropped;
    public string InstanceId { get; private set; } = string.Empty;
    public int SlotIndex { get; private set; } = -1;
    public bool AcceptEquipment { get; set; }
    private bool _dragEnabled;
    private string _normalStyle = "SecondaryButton";
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
        InstanceId = instanceId;
        SlotIndex = slotIndex;
        SizeFlagsHorizontal = slotIndex >= 0 ? SizeFlags.ExpandFill : SizeFlags.ShrinkBegin;
        _dragEnabled = editable && !string.IsNullOrEmpty(instanceId);
        AcceptEquipment = editable && slotIndex >= 0;
        Icon = item?.Icon;
        Text = item is null ? $"＋\n槽位 {slotIndex + 1}" : item.DisplayName;
        TooltipText = item is null ? "拖入装备。已有装备会回到背包。" :
            $"{item.DisplayName}\n{item.Description}\n{(editable ? "拖动装备 · 点击查看 · Ctrl+回车装入空槽 · Delete卸下" : "当前仅查看。")}";
        _normalStyle = selected ? "SelectedButton" : "SecondaryButton";
        ThemeTypeVariation = _normalStyle;
        Disabled = !editable && item is null;
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (!_dragEnabled) return default;
        SetDragPreview(CreatePreview());
        return DragPayload();
    }

    public Control CreatePreview()
    {
        var preview = GD.Load<PackedScene>("res://scenes/ui/components/EquipmentDragPreview.tscn").Instantiate<Control>();
        preview.GetNode<TextureRect>("Layout/Icon").Texture = Icon;
        preview.GetNode<Label>("Layout/Name").Text = Text;
        return preview;
    }
    public Variant DragPayload() => new Godot.Collections.Dictionary
        { ["equipment-instance-id"] = InstanceId, ["equipment-scope"] = DragScope, ["equipment-context"] = DragContext };

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (!TryEquipmentId(data, out var id)) return false;
        var allowed = HasScope(data, DragScope) && HasContext(data, DragContext) && AcceptEquipment && id != InstanceId && (CanReceive?.Invoke(id) ?? true);
        ThemeTypeVariation = allowed ? "EquipmentSlotValid" : "EquipmentSlotInvalid";
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
