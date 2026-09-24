using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public partial class RosterLoadoutView : Control
{
    public event Action? EquipmentChanged;
    public event Action<string>? HeroSelected;
    public string SelectedHeroId { get; private set; } = "";
    private RunApplication? _app;
    private Func<string, PreparedUnitDetails?>? _preparedFor;
    private GridContainer _grid = null!;
    private ScrollContainer _scroll = null!;
    private HFlowContainer _inventory = null!;
    private EquipmentDropZone _return = null!;
    private Label _feedback = null!;
    private readonly Dictionary<string, RosterHeroCard> _cards = new(StringComparer.Ordinal);
    private readonly Dictionary<string, EquipmentSlotButton> _items = new(StringComparer.Ordinal);
    private readonly Dictionary<string, UnitSnapshot> _snapshots = new(StringComparer.Ordinal);
    private string _selectedItem = "";
    private PackedScene _cardScene = null!, _tileScene = null!;
    public override void _Ready()
    {
        _grid = GetNode<GridContainer>("%RosterHeroGrid");
        _scroll = GetNode<ScrollContainer>("%RosterHeroScroll");
        _inventory = GetNode<HFlowContainer>("%RosterInventory");
        _return = GetNode<EquipmentDropZone>("%RosterReturnZone");
        _feedback = GetNode<Label>("%RosterFeedback");
        _cardScene = GD.Load<PackedScene>("res://scenes/ui/components/RosterHeroCard.tscn");
        _tileScene = GD.Load<PackedScene>("res://scenes/ui/components/EquipmentSlotButton.tscn");
        _return.CanReceive = CanReturn;
        _return.Receive = Remove;
        _scroll.Resized += AdjustColumns;
        AdjustColumns();
    }
    public void Bind(RunApplication app, string heroId = "", Func<string, PreparedUnitDetails?>? preparedFor = null)
    {
        if (_app != app) _snapshots.Clear();
        _app = app;
        _preparedFor = preparedFor;
        if (heroId.Length > 0) SelectedHeroId = heroId;
        Refresh();
    }
    private void Refresh()
    {
        if (_app?.ActiveRun is not { } run) return;
        if (run.Roster.All(hero => hero.InstanceId != SelectedHeroId)) SelectedHeroId = run.Roster.FirstOrDefault()?.InstanceId ?? "";
        foreach (var id in _cards.Keys.Where(id => run.Roster.All(hero => hero.InstanceId != id)).ToArray())
        { var card = _cards[id]; _grid.RemoveChild(card); card.QueueFree(); _cards.Remove(id); }
        foreach (var hero in run.Roster)
        {
            if (!_app.Content.TryGet(hero.ContentId, out var entry) || entry.Definition is not UnitDefinition definition) continue;
            if (!_snapshots.TryGetValue(hero.ContentId, out var snapshot))
                _snapshots.Add(hero.ContentId, snapshot = BattleSetupFactory.Snapshot(entry, _app.Content));
            if (!_cards.TryGetValue(hero.InstanceId, out var card))
            {
                card = _cardScene.Instantiate<RosterHeroCard>();
                _grid.AddChild(card); _cards.Add(hero.InstanceId, card);
                card.Selected += SelectHero;
                card.EquipmentDropped += Equip;
                card.EquipmentInspected += Inspect;
                card.KeyboardEquipment += Keyboard;
                var id = hero.InstanceId;
                card.EvaluateDrop = data => RunEquipmentDropRules.Evaluate(_app, id, data);
                card.CanReceiveItem = itemId => _app?.EquipmentEditingLocked == false && Find(itemId) is not null;
            }
            card.Bind(_app, hero, definition, snapshot, _preparedFor?.Invoke(hero.InstanceId), hero.InstanceId == SelectedHeroId, _selectedItem);
        }
        foreach (var id in _items.Keys.Where(id => run.EquipmentInventory.All(item => item.InstanceId != id)).ToArray())
        { var tile = _items[id]; _inventory.RemoveChild(tile); tile.QueueFree(); _items.Remove(id); }
        var context = RunEquipmentDropRules.ContextFor(run);
        _return.DragContext = context;
        foreach (var item in run.EquipmentInventory)
        {
            if (!_items.TryGetValue(item.InstanceId, out var tile))
            {
                tile = _tileScene.Instantiate<EquipmentSlotButton>();
                _inventory.AddChild(tile); _items.Add(item.InstanceId, tile);
                tile.Chosen += selected => Inspect(selected, SelectedHeroId);
                tile.KeyboardActionRequested += (selected, remove) => Keyboard(selected, remove, SelectedHeroId);
                tile.EquipmentDropped += (id, _) => Remove(id);
            }
            var definition = Item(item.ContentId);
            tile.Bind(item.InstanceId, -1, definition, item.InstanceId == _selectedItem, !_app.EquipmentEditingLocked);
            tile.DragContext = context;
            tile.AcceptEquipment = !_app.EquipmentEditingLocked;
            tile.CanReceive = CanReturn;
            BattleLabHoverHint.Bind(tile, EquipmentHint(_app, definition, "背包装备",
                "拖到英雄空槽穿戴；拖到已有装备上替换，旧装备回包。Ctrl+回车装给选中英雄。"));
        }
        GetNode<Label>("%RosterInventoryTitle").Text = $"装备背包  {run.EquipmentInventory.Count}";
        GetNode<Label>("%RosterCount").Text = $"{run.Roster.Count} 名英雄 · {_app.EquipmentEditingLocked switch { true => "战斗中仅查看", false => "拖动装备自由配装" }}";
        GetNode<Control>("%RosterInventoryEmpty").Visible = run.EquipmentInventory.Count == 0;
        GetNode<Control>("%RosterEmpty").Visible = run.Roster.Count == 0;
        AdjustColumns();
    }
    private void AdjustColumns()
    {
        if (_grid is null) return;
        var columns = Math.Max(1, (int)((_scroll.Size.X - 16 + 16) / (274 + 16)));
        if (_grid.Columns != columns) _grid.Columns = columns;
    }
    private void SelectHero(string id)
    {
        SelectedHeroId = id; Refresh(); HeroSelected?.Invoke(id);
    }
    private void Inspect(EquipmentSlotButton tile, string heroId)
    {
        _selectedItem = tile.InstanceId;
        SelectedHeroId = heroId;
        Refresh(); HeroSelected?.Invoke(heroId);
        Feedback(tile.InstanceId.Length == 0 ? "把装备拖到此槽位穿戴。" : "已选中装备 · 拖动穿戴、转交或卸下", false);
    }
    private void Keyboard(EquipmentSlotButton tile, bool remove, string heroId)
    {
        if (remove) { Remove(tile.InstanceId); return; }
        var hero = _app?.ActiveRun?.Roster.FirstOrDefault(unit => unit.InstanceId == heroId);
        var slot = hero is null ? -1 : Enumerable.Range(0, _app!.Rules.EquipmentSlotCapacity)
            .FirstOrDefault(at => hero.Equipment.All(item => item.SlotIndex != at), -1);
        if (slot < 0) { Feedback("该英雄装备已满，请拖到具体槽位替换。", true); return; }
        Equip(heroId, slot, tile.InstanceId);
    }
    private void Equip(string heroId, int slot, string itemId)
    {
        if (_app?.ActiveRun is not { } run || Find(itemId) is not { } item) return;
        var owner = run.Roster.FirstOrDefault(hero => hero.InstanceId == heroId);
        if (owner is null) return;
        var old = owner.Equipment.FirstOrDefault(candidate => candidate.SlotIndex == slot);
        var name = Item(item.ContentId).DisplayName;
        if (!_app.EquipOwnedItem(itemId, heroId, slot))
        { Feedback(_app.EquipmentEditingLocked ? "战斗中不能更换装备。" : "保存失败，原装备保持不变。", true); return; }
        SelectedHeroId = heroId; _selectedItem = "";
        // The host rebuilds its preparation projection before we ask it for card values.
        EquipmentChanged?.Invoke(); Refresh(); HeroSelected?.Invoke(heroId);
        Feedback($"已穿戴 {name}" + (old is null ? "" : $" · {Item(old.ContentId).DisplayName} 已回背包"), false);
    }
    private void Remove(string itemId)
    {
        if (Find(itemId) is not { OwnerHeroInstanceId.Length: > 0 } item || _app is null) return;
        if (!_app.RemoveEquipment(item.OwnerHeroInstanceId, item.SlotIndex))
        { Feedback(_app.EquipmentEditingLocked ? "战斗中不能更换装备。" : "保存失败，原装备保持不变。", true); return; }
        _selectedItem = ""; EquipmentChanged?.Invoke(); Refresh();
        Feedback($"{Item(item.ContentId).DisplayName} 已回背包", false);
    }
    private bool CanReturn(string id) => _app?.EquipmentEditingLocked == false && Find(id) is { OwnerHeroInstanceId.Length: > 0 };
    private EquipmentInstanceState? Find(string id) => _app?.ActiveRun is { } run ?
        run.EquipmentInventory.Concat(run.Roster.SelectMany(hero => hero.Equipment)).FirstOrDefault(item => item.InstanceId == id) : null;
    private ItemDefinition Item(string id) => _app!.Content.TryGet(id, out var entry) ? (ItemDefinition)entry.Definition :
        throw new InvalidOperationException("Missing item " + id);
    public static BattleLabTooltipInfo EquipmentHint(RunApplication app, ItemDefinition item, string state, string hint)
    {
        // The existing offer formatter projects the published equipment graph, including
        // granted statuses, reactive rules and trait contributions absent from the blurb.
        var rules = RunOfferDetailText.Card(app, item.Id, "", "", "", "", "", false).Details;
        return new BattleLabTooltipInfo(item.DisplayName, state, Abilities: rules, Hint: hint);
    }
    private void Feedback(string message, bool error)
    { _feedback.Text = message; _feedback.Modulate = error ? new Color("ed9b83") : new Color("b7dbc6"); }
}
