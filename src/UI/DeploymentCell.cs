using System;
using Godot;

namespace TowerAutobattler.UI;

public partial class DeploymentCell : Button
{
    public event Action<int>? SlotSelected;
    public event Action<string, int>? UnitDropped;
    public int Slot { get; private set; } = -1;
    public string InstanceId { get; private set; } = string.Empty;

    public override void _Ready() => Pressed += OnPressed;
    public override void _ExitTree() => Pressed -= OnPressed;

    public void Bind(int slot, string instanceId, string displayName, bool selected)
    {
        Slot = slot;
        InstanceId = instanceId;
        Text = string.IsNullOrEmpty(instanceId) ? $"{slot + 1}\n空位" : $"{slot + 1}\n{displayName}";
        TooltipText = string.IsNullOrEmpty(instanceId) ? "部署到此锚点" : "点击选择，或拖动以移动/交换";
        Modulate = selected ? new Color(1f, .84f, .42f) : Colors.White;
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (string.IsNullOrWhiteSpace(InstanceId)) return default;
        var preview = (Control)Duplicate();
        preview.MouseFilter = MouseFilterEnum.Ignore;
        SetDragPreview(preview);
        return new Godot.Collections.Dictionary { ["unit_id"] = InstanceId };
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (Slot < 0 || data.VariantType != Variant.Type.Dictionary) return false;
        var dictionary = data.AsGodotDictionary();
        return dictionary.ContainsKey("unit_id") && !string.IsNullOrWhiteSpace(dictionary["unit_id"].AsString());
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (!_CanDropData(atPosition, data)) return;
        UnitDropped?.Invoke(data.AsGodotDictionary()["unit_id"].AsString(), Slot);
    }

    private void OnPressed() => SlotSelected?.Invoke(Slot);
}
