using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.BattleLab;

namespace TowerAutobattler.UI;

public partial class BattleLabEquipmentPanel : VBoxContainer
{
    public event Action<string, int>? EquipRequested;
    public event Action<string>? RemoveRequested;
    public event Action<string>? InspectRequested;
    private readonly List<EquipmentSlotButton> _slots = [];
    private readonly Dictionary<string, EquipmentSlotButton> _library = new();
    private BattleLabSession? _session;
    private string _ownerId = string.Empty;
    private PackedScene _tile = null!;
    private HBoxContainer _slotHost = null!;
    private HFlowContainer _libraryHost = null!;
    private EquipmentDropZone _return = null!;
    public override void _Ready()
    {
        _tile = GD.Load<PackedScene>("res://scenes/ui/components/EquipmentSlotButton.tscn");
        _slotHost = GetNode<HBoxContainer>("Slots");
        _libraryHost = GetNode<HFlowContainer>("Library/Layout/Items");
        _return = GetNode<EquipmentDropZone>("Library");
        _return.DragScope = "lab";
        _return.CanReceive = id => _session?.Units.Any(owner => owner.Equipment.Any(item => item.InstanceId == id)) == true;
        _return.Receive = id => RemoveRequested?.Invoke(id);
    }
    public void Bind(BattleLabSession session, string ownerId, string dragContext)
    {
        _session = session; _ownerId = ownerId;
        _return.DragContext = dragContext;
        var owner = session.TryGet(ownerId, out var unit) ? unit : null;
        var editable = owner is not null;
        while (_slots.Count < session.Content.Rules.EquipmentSlotCapacity)
        {
            var button = _tile.Instantiate<EquipmentSlotButton>(); _slotHost.AddChild(button);
            button.DragScope = "lab";
            button.EquipmentDropped += (id, slot) => EquipRequested?.Invoke(id, slot);
            button.Chosen += chosen => Inspect(chosen.InstanceId);
            button.KeyboardActionRequested += KeyboardEquipment;
            button.CanReceive = CanEquip;
            _slots.Add(button);
        }
        for (var slot = 0; slot < _slots.Count; slot++)
        {
            var item = owner?.Equipment.FirstOrDefault(value => value.SlotIndex == slot);
            var definition = item is null ? null : session.Content.Equipment.First(value => value.StableId == item.ContentId);
            _slots[slot].Bind(item?.InstanceId ?? string.Empty, slot, definition?.Definition, false, editable);
            _slots[slot].DragContext = dragContext;
            if (definition is not null) _slots[slot].TooltipText = BattleLabTooltipFormatter.Item(definition, session.Content).PlainText + "\n拖回装备库移除配置。";
        }
        foreach (var item in session.Content.Equipment)
        {
            var id = "catalog:" + item.StableId;
            if (!_library.TryGetValue(id, out var button))
            {
                button = _tile.Instantiate<EquipmentSlotButton>(); _libraryHost.AddChild(button); _library.Add(id, button);
                button.DragScope = "lab"; button.Chosen += chosen => Inspect(chosen.InstanceId);
                button.KeyboardActionRequested += KeyboardEquipment;
                button.EquipmentDropped += (instanceId, _) => RemoveRequested?.Invoke(instanceId);
            }
            button.Bind(id, -1, item.Definition, false, editable);
            button.DragContext = dragContext;
            button.AcceptEquipment = editable;
            button.CanReceive = instanceId => _session?.Units.Any(unit =>
                unit.Equipment.Any(equipment => equipment.InstanceId == instanceId)) == true;
            button.TooltipText = BattleLabTooltipFormatter.Item(item, session.Content).PlainText + "\n拖到槽位配置 · 实验室可重复取用";
        }
    }
    private bool CanEquip(string id) => _session is not null && _session.TryGet(_ownerId, out var owner) &&
        (id.StartsWith("catalog:", StringComparison.Ordinal)
            ? _session.Content.Equipment.Any(item => "catalog:" + item.StableId == id)
            : _session.Units.Any(unit => unit.Equipment.Any(item => item.InstanceId == id)));
    private void KeyboardEquipment(EquipmentSlotButton tile, bool remove)
    {
        if (remove) { if (tile.SlotIndex >= 0) RemoveRequested?.Invoke(tile.InstanceId); return; }
        if (_session?.TryGet(_ownerId, out var owner) != true) return;
        var slot = Enumerable.Range(0, _session.Content.Rules.EquipmentSlotCapacity)
            .FirstOrDefault(index => owner.Equipment.All(item => item.SlotIndex != index), -1);
        if (slot >= 0) EquipRequested?.Invoke(tile.InstanceId, slot);
    }
    private void Inspect(string id)
    {
        var contentId = id.StartsWith("catalog:", StringComparison.Ordinal) ? id[8..] :
            _session?.Units.SelectMany(unit => unit.Equipment).FirstOrDefault(item => item.InstanceId == id)?.ContentId;
        if (contentId is not null) InspectRequested?.Invoke(contentId);
    }
}
