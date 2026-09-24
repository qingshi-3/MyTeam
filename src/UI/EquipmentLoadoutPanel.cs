using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public partial class EquipmentLoadoutPanel : PanelContainer
{
    public event Action? EquipmentChanged;
    public event Action<string>? HeroSelected;
    private RunApplication? _app;
    private string _heroId = string.Empty;
    private string _selectedEquipmentId = string.Empty;
    private Label _heroName = null!;
    private UnitPortrait _portrait = null!;
    private Label _details = null!;
    private Label _inventoryTitle = null!;
    private Label _feedback = null!;
    private Button _previous = null!;
    private Button _next = null!;
    private HFlowContainer _inventory = null!;
    private HBoxContainer _slotsContainer = null!;
    private PackedScene _tileScene = null!;
    private readonly List<EquipmentSlotButton> _slots = [];
    private readonly Dictionary<string, EquipmentSlotButton> _items = new(StringComparer.Ordinal);
    private readonly Dictionary<string, EquipmentDropZone> _heroTargets = new(StringComparer.Ordinal);
    private HFlowContainer _heroes = null!;
    private EquipmentDropZone _returnZone = null!;

    public override void _Ready()
    {
        _heroName = GetNode<Label>("%EquipmentHeroName");
        _portrait = GetNode<UnitPortrait>("%EquipmentHeroPortrait");
        _details = GetNode<Label>("%EquipmentDetails");
        _inventoryTitle = GetNode<Label>("%EquipmentInventoryTitle");
        _feedback = GetNode<Label>("%EquipmentFeedback");
        _previous = GetNode<Button>("%EquipmentPreviousHero");
        _next = GetNode<Button>("%EquipmentNextHero");
        _inventory = GetNode<HFlowContainer>("%EquipmentInventory");
        _slotsContainer = GetNode<HBoxContainer>("%EquipmentSlots");
        _tileScene = GD.Load<PackedScene>("res://scenes/ui/components/EquipmentSlotButton.tscn");
        _heroes = GetNode<HFlowContainer>("%EquipmentHeroes");
        _returnZone = GetNode<EquipmentDropZone>("%EquipmentReturnZone");
        _returnZone.CanReceive = id => _app?.EquipmentEditingLocked == false &&
            FindEquipment(id) is { OwnerHeroInstanceId.Length: > 0 };
        _returnZone.Receive = Remove;
        _previous.Pressed += PreviousHero;
        _next.Pressed += NextHero;
    }

    public override void _ExitTree()
    {
        _previous.Pressed -= PreviousHero;
        _next.Pressed -= NextHero;
    }

    public void Bind(RunApplication app, string heroId = "")
    {
        _app = app;
        if (!string.IsNullOrEmpty(heroId)) _heroId = heroId;
        var run = app.ActiveRun;
        Visible = run is { Roster.Count: > 0 };
        if (!Visible) return;
        if (!run!.Roster.Any(hero => hero.InstanceId == _heroId)) _heroId = run.Roster[0].InstanceId;
        Refresh();
    }

    private void Refresh()
    {
        if (_app?.ActiveRun is not { } run) return;
        var hero = run.Roster.First(unit => unit.InstanceId == _heroId);
        var definition = (UnitDefinition)Required(hero.ContentId).Definition;
        _heroName.Text = definition.DisplayName;
        if (_portrait.Definition != definition.Portrait) _portrait.Bind(definition.Portrait);
        _previous.Disabled = _next.Disabled = run.Roster.Count < 2;
        var editable = !_app.EquipmentEditingLocked;
        var dragContext = RunEquipmentDropRules.ContextFor(run);
        _returnZone.DragContext = dragContext;
        foreach (var staleId in _heroTargets.Keys.Where(id => run.Roster.All(owner => owner.InstanceId != id)).ToArray())
        {
            var stale = _heroTargets[staleId]; _heroes.RemoveChild(stale); stale.QueueFree(); _heroTargets.Remove(staleId);
        }
        foreach (var owner in run.Roster)
        {
            var ownerId = owner.InstanceId;
            if (!_heroTargets.TryGetValue(ownerId, out var target))
            {
                target = GD.Load<PackedScene>("res://scenes/ui/components/EquipmentHeroTarget.tscn").Instantiate<EquipmentDropZone>();
                _heroes.AddChild(target); _heroTargets.Add(ownerId, target);
                var select = target.GetNode<Button>("Layout/Select");
                select.Pressed += () => SelectHero(ownerId);
                // The name remains a real inspect button while sharing the whole
                // card's validated drop target, including on its text pixels.
                select.SetDragForwarding(Callable.From<Vector2, Variant>(_ => default),
                    Callable.From<Vector2, Variant, bool>((position, data) => target._CanDropData(position, data)),
                    Callable.From<Vector2, Variant>((position, data) => target._DropData(position, data)));
                target.CanReceive = id => _app?.EquipmentEditingLocked == false && FindEquipment(id) is not null && FirstEmptySlot(ownerId) >= 0;
                target.Receive = id => { var slot = FirstEmptySlot(ownerId); if (slot >= 0) { SelectHero(ownerId); Equip(id, slot); } };
            }
            var ownerDefinition = (UnitDefinition)Required(owner.ContentId).Definition;
            target.DragContext = dragContext;
            target.GetNode<UnitPortrait>("Layout/Portrait").Bind(ownerDefinition.Portrait);
            target.GetNode<Button>("Layout/Select").Text = ownerDefinition.DisplayName;
            target.GetNode<Label>("Layout/Capacity").Text = $"{owner.Equipment.Count}/{_app.Rules.EquipmentSlotCapacity} · {(FirstEmptySlot(ownerId) < 0 ? "已满" : "可拖入")}";
            target.TooltipText = FirstEmptySlot(ownerId) < 0 ? "装备已满；选择英雄后拖到具体槽位替换。" : "把装备拖到此英雄，放入第一个空槽。点击名字查看槽位。";
        }
        while (_slots.Count < _app.Rules.EquipmentSlotCapacity)
        {
            var tile = _tileScene.Instantiate<EquipmentSlotButton>();
            _slotsContainer.AddChild(tile);
            tile.Chosen += ChooseSlot;
            tile.EquipmentDropped += Equip;
            tile.KeyboardActionRequested += KeyboardEquipment;
            _slots.Add(tile);
        }
        for (var slot = 0; slot < _slots.Count; slot++)
        {
            var item = hero.Equipment.SingleOrDefault(equipment => equipment.SlotIndex == slot);
            _slots[slot].Bind(item?.InstanceId ?? string.Empty, slot,
                item is null ? null : ItemDefinition(item.ContentId), item?.InstanceId == _selectedEquipmentId, editable);
            _slots[slot].CanReceive = id => FindEquipment(id) is not null;
            _slots[slot].DragContext = dragContext;
        }
        var ids = run.EquipmentInventory.Select(item => item.InstanceId).ToHashSet(StringComparer.Ordinal);
        foreach (var staleId in _items.Keys.Where(id => !ids.Contains(id)).ToArray())
        {
            var stale = _items[staleId];
            _inventory.RemoveChild(stale);
            stale.QueueFree();
            _items.Remove(staleId);
        }
        foreach (var item in run.EquipmentInventory)
        {
            if (!_items.TryGetValue(item.InstanceId, out var tile))
            {
                tile = _tileScene.Instantiate<EquipmentSlotButton>();
                _inventory.AddChild(tile);
                tile.Chosen += ChooseInventory;
                tile.KeyboardActionRequested += KeyboardEquipment;
                // Inventory buttons are hit-test stops; route drops on their artwork
                // through the same unload command as the surrounding empty bag area.
                tile.EquipmentDropped += (id, _) => Remove(id);
                _items.Add(item.InstanceId, tile);
            }
            tile.Bind(item.InstanceId, -1, ItemDefinition(item.ContentId), item.InstanceId == _selectedEquipmentId, editable);
            tile.DragContext = dragContext;
            tile.AcceptEquipment = editable;
            tile.CanReceive = id => _app?.EquipmentEditingLocked == false &&
                FindEquipment(id) is { OwnerHeroInstanceId.Length: > 0 };
        }
        _inventoryTitle.Text = run.EquipmentInventory.Count == 0 ? "背包暂无装备 · 战利品与商店可获得" :
            $"装备背包 · {run.EquipmentInventory.Count} 件";
        var selected = FindEquipment(_selectedEquipmentId);
        if (selected is null) _selectedEquipmentId = string.Empty;
        var selectedOwner = selected is null ? null : run.Roster.FirstOrDefault(owner => owner.InstanceId == selected.OwnerHeroInstanceId);
        var origin = selectedOwner is null ? "背包" : ((UnitDefinition)Required(selectedOwner.ContentId).Definition).DisplayName;
        _details.Text = selected is null ? "拖到英雄或槽位穿戴 · 拖回背包卸下\n点击只查看；替换装备自动回包。" :
            $"{ItemDefinition(selected.ContentId).DisplayName} · 来自{origin}\n{ItemDefinition(selected.ContentId).Description}";
    }

    private void ChooseInventory(EquipmentSlotButton tile)
    {
        _selectedEquipmentId = tile.InstanceId;
        Refresh();
        SetFeedback("拖动可穿戴；点击只查看效果。", false);
    }
    private void KeyboardEquipment(EquipmentSlotButton tile, bool remove)
    {
        if (remove) { Remove(tile.InstanceId); return; }
        var slot = FirstEmptySlot(_heroId);
        if (slot < 0) { SetFeedback("装备已满；先聚焦已装备槽位按 Delete 卸下。", true); return; }
        Equip(tile.InstanceId, slot);
    }

    private void ChooseSlot(EquipmentSlotButton tile)
    {
        _selectedEquipmentId = tile.InstanceId;
        Refresh();
        SetFeedback(string.IsNullOrEmpty(tile.InstanceId) ? "把装备拖到这个槽位。" : "拖到其他英雄转交，或拖回背包卸下。", false);
    }

    private void Equip(string instanceId, int slot)
    {
        if (_app?.ActiveRun is not { } run || FindEquipment(instanceId) is not { } item) return;
        var hero = run.Roster.First(unit => unit.InstanceId == _heroId);
        var previous = hero.Equipment.SingleOrDefault(equipment => equipment.SlotIndex == slot);
        var label = ItemDefinition(item.ContentId).DisplayName;
        if (!_app.EquipOwnedItem(instanceId, _heroId, slot))
        {
            SetFeedback(_app.EquipmentEditingLocked ? "战斗中不能更换装备。" : "未能保存更换，原装备保持不变。", true);
            return;
        }
        _selectedEquipmentId = string.Empty;
        Refresh();
        EquipmentChanged?.Invoke();
        SetFeedback($"已穿戴 {label}" + (previous is null ? "" : $" · {ItemDefinition(previous.ContentId).DisplayName} 已回背包"), false);
        _slots[slot].GrabFocus();
    }

    private void Remove(string instanceId)
    {
        var item = FindEquipment(instanceId);
        if (item is null || string.IsNullOrEmpty(item.OwnerHeroInstanceId) || _app is null) return;
        if (!_app.RemoveEquipment(item.OwnerHeroInstanceId, item.SlotIndex))
        {
            SetFeedback("未能卸下，原装备保持不变。", true);
            return;
        }
        _selectedEquipmentId = string.Empty;
        Refresh();
        EquipmentChanged?.Invoke();
        SetFeedback($"{ItemDefinition(item.ContentId).DisplayName} 已回背包", false);
    }

    private EquipmentInstanceState? FindEquipment(string id) => _app?.ActiveRun is { } run
        ? run.EquipmentInventory.Concat(run.Roster.SelectMany(hero => hero.Equipment)).FirstOrDefault(item => item.InstanceId == id)
        : null;
    private CatalogEntry Required(string id) => _app!.Content.TryGet(id, out var entry) ? entry :
        throw new InvalidOperationException("Missing equipment presentation: " + id);
    private ItemDefinition ItemDefinition(string id) => (ItemDefinition)Required(id).Definition;
    private void PreviousHero() => ChangeHero(-1);
    private int FirstEmptySlot(string ownerId)
    {
        var owner = _app?.ActiveRun?.Roster.FirstOrDefault(hero => hero.InstanceId == ownerId);
        return owner is null ? -1 : Enumerable.Range(0, _app!.Rules.EquipmentSlotCapacity)
            .FirstOrDefault(slot => owner.Equipment.All(item => item.SlotIndex != slot), -1);
    }
    private void SelectHero(string ownerId)
    {
        _heroId = ownerId; Refresh(); HeroSelected?.Invoke(ownerId);
    }
    private void NextHero() => ChangeHero(1);
    private void ChangeHero(int direction)
    {
        if (_app?.ActiveRun is not { } run) return;
        var index = run.Roster.FindIndex(hero => hero.InstanceId == _heroId);
        _heroId = run.Roster[(index + direction + run.Roster.Count) % run.Roster.Count].InstanceId;
        Refresh();
        HeroSelected?.Invoke(_heroId);
    }
    private void SetFeedback(string message, bool error)
    {
        _feedback.Text = message;
        _feedback.ThemeTypeVariation = error ? "FeedbackFailure" : "FeedbackSuccess";
    }
}
