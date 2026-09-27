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
    private VBoxContainer _list = null!;
    private RosterHeroCard _card = null!;
    private RosterHeroDetails _details = null!;
    private ScrollContainer _scroll = null!;
    private HFlowContainer _inventory = null!;
    private EquipmentDropZone _return = null!;
    private Label _feedback = null!;
    private readonly Dictionary<string, RosterHeroSelector> _selectors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, EquipmentSlotButton> _items = new(StringComparer.Ordinal);
    private readonly Dictionary<string, UnitSnapshot> _snapshots = new(StringComparer.Ordinal);
    private string _selectedItem = "";
    private PackedScene _cardScene = null!, _tileScene = null!;
    public override void _Ready()
    {
        _list = GetNode<VBoxContainer>("%RosterHeroList");
        _card = GetNode<RosterHeroCard>("%SelectedCard");
        _details = GetNode<RosterHeroDetails>("%HeroDetails");
        _scroll = GetNode<ScrollContainer>("%RosterHeroScroll");
        _inventory = GetNode<HFlowContainer>("%RosterInventory");
        _return = GetNode<EquipmentDropZone>("%RosterReturnZone");
        _feedback = GetNode<Label>("%RosterFeedback");
        _cardScene = GD.Load<PackedScene>("res://scenes/ui/components/RosterHeroSelector.tscn");
        _tileScene = GD.Load<PackedScene>("res://scenes/ui/components/EquipmentSlotButton.tscn");
        _return.CanReceive = CanReturn;
        _return.Receive = Remove;
        _card.Selected += SelectHero;
        _card.EquipmentDropped += Equip;
        _card.EquipmentInspected += Inspect;
        _card.KeyboardEquipment += Keyboard;
        _card.FactInspected += _details.ShowExplanation;
        _card.EvaluateDrop = data => RunEquipmentDropRules.Evaluate(_app, SelectedHeroId, data);
        _card.CanReceiveItem = itemId => _app?.EquipmentEditingLocked == false && Find(itemId) is not null;
        Resized += FitRosterList;
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
        foreach (var id in _selectors.Keys.Where(id => run.Roster.All(hero => hero.InstanceId != id)).ToArray())
        { var selector = _selectors[id]; _list.RemoveChild(selector); selector.QueueFree(); _selectors.Remove(id); }
        foreach (var hero in run.Roster)
        {
            if (!_app.Content.TryGet(hero.ContentId, out var entry) || entry.Definition is not UnitDefinition definition) continue;
            if (!_snapshots.TryGetValue(hero.ContentId, out var snapshot))
                _snapshots.Add(hero.ContentId, snapshot = BattleSetupFactory.Snapshot(entry, _app.Content));
            if (!_selectors.TryGetValue(hero.InstanceId, out var selector))
            {
                selector = _cardScene.Instantiate<RosterHeroSelector>();
                _list.AddChild(selector); _selectors.Add(hero.InstanceId, selector);
                selector.Selected += SelectHero;
                selector.EquipmentDropped += Equip;
                var id = hero.InstanceId;
                selector.EvaluateDrop = data => RunEquipmentDropRules.Evaluate(_app, id, data);
            }
            selector.Bind(hero.InstanceId, definition, hero.InstanceId == SelectedHeroId, run.Deployment.Contains(hero.InstanceId));
            if (hero.InstanceId == SelectedHeroId)
            {
                var model = new UnitInformation(hero.InstanceId, definition, snapshot,
                    _preparedFor?.Invoke(hero.InstanceId) ?? RosterEquipmentPreview.Build(_app.Content, hero, snapshot, _app.GrowthRules));
                _card.Bind(_app, hero, definition, model, _selectedItem);
                _details.Bind(model, hero.Rank);
            }
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
                tile.CustomMinimumSize = new Vector2(82, 82);
                tile.AddThemeConstantOverride("icon_max_width", 58);
                tile.InlinePresentation = true;
                tile.WorkbenchPresentation = true;
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
        GetNode<Label>("%RosterInventoryTitle").Text = $"背包装备 · {run.EquipmentInventory.Count} 件";
        GetNode<Label>("%RosterCount").Text = $"{run.Roster.Count} 名英雄" + (_app.EquipmentEditingLocked ? " · 战斗中仅查看" : "");
        GetNode<Control>("%RosterInventoryEmpty").Visible = run.EquipmentInventory.Count == 0;
        GetNode<Control>("%RosterEmpty").Visible = run.Roster.Count == 0;
        _card.Visible = _details.Visible = run.Roster.Count > 0;
        GetNode<Label>("%RosterItemName").Text = Find(_selectedItem) is { } selectedItem
            ? Item(selectedItem.ContentId).DisplayName : string.Empty;
        FitRosterList();
    }
    private void FitRosterList()
    {
        // The shared list frame follows short rosters; longer rosters scroll inside
        // a bounded sidebar instead of becoming another full-height backboard.
        GetNode<Control>("%ListPanel").CustomMinimumSize =
            new Vector2(0, Math.Clamp(_selectors.Count * 104 + 44, 240, Math.Max(240, Size.Y * .70f)));
    }
    public override void _ExitTree() => Resized -= FitRosterList;
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
        UiDragVisual.Committed(GetViewport(), itemId);
        Feedback($"已穿戴 {name}" + (old is null ? "" : $" · {Item(old.ContentId).DisplayName} 已回背包"), false);
    }
    private void Remove(string itemId)
    {
        if (Find(itemId) is not { OwnerHeroInstanceId.Length: > 0 } item || _app is null) return;
        if (!_app.RemoveEquipment(item.OwnerHeroInstanceId, item.SlotIndex))
        { Feedback(_app.EquipmentEditingLocked ? "战斗中不能更换装备。" : "保存失败，原装备保持不变。", true); return; }
        _selectedItem = ""; EquipmentChanged?.Invoke(); Refresh();
        UiDragVisual.Committed(GetViewport(), itemId);
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
