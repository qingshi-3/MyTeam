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

    public override void _Ready() => Pressed += OnPressed;
    public override void _ExitTree() => Pressed -= OnPressed;

    public void Bind(string instanceId, int slotIndex, ItemDefinition? item, bool selected, bool editable)
    {
        InstanceId = instanceId;
        SlotIndex = slotIndex;
        _dragEnabled = editable && !string.IsNullOrEmpty(instanceId);
        AcceptEquipment = editable && slotIndex >= 0;
        Icon = item?.Icon;
        Text = item is null ? $"＋\n槽位 {slotIndex + 1}" : item.DisplayName;
        TooltipText = item is null ? "拖入装备，或先点背包装备再点此槽位。" :
            $"{item.DisplayName}\n{item.Description}\n{(editable ? "可拖动转交；替换时旧装备回背包。" : "当前仅查看。")}";
        ThemeTypeVariation = selected ? "SelectedButton" : "SecondaryButton";
        Disabled = !editable && item is null;
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (!_dragEnabled) return default;
        var preview = (Control)Duplicate();
        preview.MouseFilter = MouseFilterEnum.Ignore;
        SetDragPreview(preview);
        return new Godot.Collections.Dictionary { ["equipment_instance_id"] = InstanceId };
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) =>
        AcceptEquipment && TryEquipmentId(data, out var id) && id != InstanceId;

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (_CanDropData(atPosition, data) && TryEquipmentId(data, out var id))
            EquipmentDropped?.Invoke(id, SlotIndex);
    }

    public static bool TryEquipmentId(Variant data, out string id)
    {
        id = string.Empty;
        if (data.VariantType != Variant.Type.Dictionary) return false;
        var dictionary = data.AsGodotDictionary();
        if (!dictionary.TryGetValue("equipment_instance_id", out var value) || value.VariantType != Variant.Type.String)
            return false;
        id = value.AsString();
        return !string.IsNullOrWhiteSpace(id);
    }

    private void OnPressed() => Chosen?.Invoke(this);
}
