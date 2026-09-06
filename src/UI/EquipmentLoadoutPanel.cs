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
    private Button _remove = null!;
    private Button _cancel = null!;
    private HFlowContainer _inventory = null!;
    private HBoxContainer _slotsContainer = null!;
    private PackedScene _tileScene = null!;
    private readonly List<EquipmentSlotButton> _slots = [];
    private readonly Dictionary<string, EquipmentSlotButton> _items = new(StringComparer.Ordinal);

    public override void _Ready()
    {
        _heroName = GetNode<Label>("%EquipmentHeroName");
        _portrait = GetNode<UnitPortrait>("%EquipmentHeroPortrait");
        _details = GetNode<Label>("%EquipmentDetails");
        _inventoryTitle = GetNode<Label>("%EquipmentInventoryTitle");
        _feedback = GetNode<Label>("%EquipmentFeedback");
        _previous = GetNode<Button>("%EquipmentPreviousHero");
        _next = GetNode<Button>("%EquipmentNextHero");
        _remove = GetNode<Button>("%EquipmentRemove");
        _cancel = GetNode<Button>("%EquipmentCancel");
        _inventory = GetNode<HFlowContainer>("%EquipmentInventory");
        _slotsContainer = GetNode<HBoxContainer>("%EquipmentSlots");
        _tileScene = GD.Load<PackedScene>("res://scenes/ui/components/EquipmentSlotButton.tscn");
        _previous.Pressed += PreviousHero;
        _next.Pressed += NextHero;
        _remove.Pressed += RemoveSelected;
        _cancel.Pressed += CancelSelection;
    }

    public override void _ExitTree()
    {
        _previous.Pressed -= PreviousHero;
        _next.Pressed -= NextHero;
        _remove.Pressed -= RemoveSelected;
        _cancel.Pressed -= CancelSelection;
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
        while (_slots.Count < _app.Rules.EquipmentSlotCapacity)
        {
            var tile = _tileScene.Instantiate<EquipmentSlotButton>();
            _slotsContainer.AddChild(tile);
            tile.Chosen += ChooseSlot;
            tile.EquipmentDropped += Equip;
            _slots.Add(tile);
        }
        for (var slot = 0; slot < _slots.Count; slot++)
        {
            var item = hero.Equipment.SingleOrDefault(equipment => equipment.SlotIndex == slot);
            _slots[slot].Bind(item?.InstanceId ?? string.Empty, slot,
                item is null ? null : ItemDefinition(item.ContentId), item?.InstanceId == _selectedEquipmentId, editable);
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
                _items.Add(item.InstanceId, tile);
            }
            tile.Bind(item.InstanceId, -1, ItemDefinition(item.ContentId), item.InstanceId == _selectedEquipmentId, editable);
        }
        _inventoryTitle.Text = run.EquipmentInventory.Count == 0 ? "背包暂无装备 · 战利品与商店可获得" :
            $"装备背包 · {run.EquipmentInventory.Count} 件";
        var selected = FindEquipment(_selectedEquipmentId);
        if (selected is null) _selectedEquipmentId = string.Empty;
        var selectedOwner = selected is null ? null : run.Roster.FirstOrDefault(owner => owner.InstanceId == selected.OwnerHeroInstanceId);
        var origin = selectedOwner is null ? "背包" : ((UnitDefinition)Required(selectedOwner.ContentId).Definition).DisplayName;
        _details.Text = selected is null ? "拖装备到槽位，或先点装备再点槽位。替换的旧装备自动回包。" :
            $"{ItemDefinition(selected.ContentId).DisplayName} · 来自{origin}\n{ItemDefinition(selected.ContentId).Description}";
        _remove.Disabled = !editable || selected is null || string.IsNullOrEmpty(selected.OwnerHeroInstanceId);
        _cancel.Disabled = selected is null;
    }

    private void ChooseInventory(EquipmentSlotButton tile)
    {
        _selectedEquipmentId = tile.InstanceId;
        Refresh();
        SetFeedback("已拿起装备 · 点击目标槽位放入", false);
    }

    private void ChooseSlot(EquipmentSlotButton tile)
    {
        if (!string.IsNullOrEmpty(_selectedEquipmentId) && _selectedEquipmentId != tile.InstanceId)
            Equip(_selectedEquipmentId, tile.SlotIndex);
        else
        {
            _selectedEquipmentId = tile.InstanceId;
            Refresh();
            SetFeedback(string.IsNullOrEmpty(tile.InstanceId) ? "先从背包选择装备。" : "可拖动转交、选择另一槽位，或卸下回包。", false);
        }
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

    private void RemoveSelected()
    {
        var item = FindEquipment(_selectedEquipmentId);
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
    private void NextHero() => ChangeHero(1);
    private void ChangeHero(int direction)
    {
        if (_app?.ActiveRun is not { } run) return;
        var index = run.Roster.FindIndex(hero => hero.InstanceId == _heroId);
        _heroId = run.Roster[(index + direction + run.Roster.Count) % run.Roster.Count].InstanceId;
        Refresh();
        HeroSelected?.Invoke(_heroId);
    }
    private void CancelSelection() { _selectedEquipmentId = string.Empty; Refresh(); SetFeedback("", false); }
    private void SetFeedback(string message, bool error)
    {
        _feedback.Text = message;
        _feedback.ThemeTypeVariation = error ? "FeedbackFailure" : "FeedbackSuccess";
    }
}
