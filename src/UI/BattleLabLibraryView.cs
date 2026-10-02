using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.BattleLab;

namespace TowerAutobattler.UI;

// Owns authored catalog cards and their signal lifetime; the page receives selection/drag commands.
internal sealed class BattleLabLibraryView : IDisposable
{
    private readonly PackedScene _scene;
    private readonly Control _unitLibrary;
    private readonly Label _libraryEmpty;
    private readonly LineEdit _unitSearch;
    private readonly OptionButton _unitFilter, _placementTeam;
    private readonly BattleLabSelection _selection;
    private readonly Action<string, BattleLabSide> _selected, _drag;
    private readonly List<BattleLabLibraryCard> _cards = [];
    private BattleLabEditorSession? _editor;
    private BattleLabContentIndex? _content => _editor?.Content;
    public IReadOnlyList<BattleLabLibraryCard> Cards => _cards;
    public BattleLabLibraryView(PackedScene scene, Control container, Label empty, LineEdit search,
        OptionButton filter, OptionButton team, BattleLabSelection selection,
        Action<string, BattleLabSide> selected, Action<string, BattleLabSide> drag)
    {
        _scene = scene; _unitLibrary = container; _libraryEmpty = empty; _unitSearch = search;
        _unitFilter = filter; _placementTeam = team; _selection = selection; _selected = selected; _drag = drag;
        _unitSearch.TextChanged += OnSearchChanged; _unitFilter.ItemSelected += OnUnitFilterSelected;
    }
    public void Bind(BattleLabEditorSession editor) => _editor = editor;
    public void RefreshSelection()
    { foreach (var card in _cards) card.SetSelected(card.ContentId == _selection.Prototype && card.Side == _selection.Side); }
    private void OnSelected(string id, BattleLabSide side) => _selected(id, side);
    private void OnDrag(string id, BattleLabSide side) => _drag(id, side);
    public void Dispose()
    {
        if (GodotObject.IsInstanceValid(_unitSearch)) _unitSearch.TextChanged -= OnSearchChanged;
        if (GodotObject.IsInstanceValid(_unitFilter)) _unitFilter.ItemSelected -= OnUnitFilterSelected;
        foreach (var card in _cards)
            if (GodotObject.IsInstanceValid(card)) { card.Selected -= OnSelected; card.DragRequested -= OnDrag; }
        _cards.Clear(); _editor = null;
    }
    public void RebuildLibraries()
    {
        foreach (var card in _cards)
        { card.Selected -= OnSelected; card.DragRequested -= OnDrag; card.QueueFree(); }
        _cards.Clear();
        if (_content is null) return;
        var units = _content.Units.Where(MatchesLibrarySearch).ToArray();
        var side = (BattleLabSide)_placementTeam.GetSelectedId();
        foreach (var unit in units) AddCard(_unitLibrary, unit, side, DescribeClassification(unit.Classification));
        _libraryEmpty.Visible = units.Length == 0;
    }
    private void AddCard(Control owner, BattleLabPublishedUnit unit, BattleLabSide side, string classification)
    {
        var card = _scene.Instantiate<BattleLabLibraryCard>();
        card.Bind(unit.StableId, unit.DisplayName, side, classification, unit.Definition.Portrait);
        card.SetInspection(BattleLabTooltipFormatter.Prototype(unit, _editor!.UnitSnapshotFor(unit)));
        card.Selected += OnSelected; card.DragRequested += OnDrag;
        owner.AddChild(card); card.SetSelected(_selection.Prototype == unit.StableId && _selection.Side == side); _cards.Add(card);
    }
    private void OnSearchChanged(string _) => RebuildLibraries();
    private void OnUnitFilterSelected(long _) => RebuildLibraries();
    private bool MatchesLibrarySearch(BattleLabPublishedUnit unit)
    {
        var filter = _unitFilter.Selected < 0 ? 0 : _unitFilter.GetItemId(_unitFilter.Selected);
        return (filter == 0 || unit.Classification.HasFlag((BattleLabUnitClassification)filter)) && Matches(unit, _unitSearch.Text);
    }
    private static bool Matches(BattleLabPublishedUnit unit, string query) => string.IsNullOrWhiteSpace(query) ||
        unit.DisplayName.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase) ||
        unit.StableId.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase) ||
        unit.Definition.Description.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);
    private static string DescribeClassification(BattleLabUnitClassification classification)
    {
        var labels = new List<string>();
        if (classification.HasFlag(BattleLabUnitClassification.PlayerHero)) labels.Add("英雄");
        if (classification.HasFlag(BattleLabUnitClassification.TestDummy)) labels.Add("测试假人");
        if (classification.HasFlag(BattleLabUnitClassification.PveNormal)) labels.Add("基础 / 普通");
        if (classification.HasFlag(BattleLabUnitClassification.PveElite)) labels.Add("精英");
        if (classification.HasFlag(BattleLabUnitClassification.PveBoss)) labels.Add("首领");
        if (classification.HasFlag(BattleLabUnitClassification.PublishedSummon)) labels.Add("召唤物");
        return labels.Count == 0 ? "单位" : string.Join(" / ", labels);
    }
}
