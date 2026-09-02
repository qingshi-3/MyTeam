using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public partial class HeroSelectScreen : Control
{
    [Signal] public delegate void HeroChosenEventHandler(string stableId);
    public event Action? BackRequested;
    [Export] public PackedScene HeroLibraryTileScene { get; set; } = null!;

    private GridContainer _library = null!;
    private HeroDetailPanel _detail = null!;
    private Button _back = null!;
    private readonly Dictionary<string, HeroSelectionViewModel> _models = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HeroLibraryTile> _tiles = new(StringComparer.Ordinal);
    private string _previewId = string.Empty;

    public string PreviewStableId => _previewId;

    public override void _Ready()
    {
        CacheNodes();
        Resized += UpdateResponsiveColumns;
        _detail.DeployRequested += OnDeployRequested;
        _back.Pressed += OnBack;
        UpdateResponsiveColumns();
    }

    public override void _ExitTree()
    {
        Resized -= UpdateResponsiveColumns;
        _detail.DeployRequested -= OnDeployRequested;
        _back.Pressed -= OnBack;
    }

    public void Bind(ContentRegistry content, MetaProgressDto meta)
    {
        var heroes = new List<HeroSelectionViewModel>();
        foreach (var entry in content.Catalog.Heroes)
        {
            var definition = (UnitDefinition)entry.Definition;
            var root = entry.Scene.Instantiate<UnitContentRoot>();
            try
            {
                var rule = root.HeroRule;
                heroes.Add(new HeroSelectionViewModel(
                    entry.StableId,
                    definition,
                    meta.UnlockedHeroIds.Contains(entry.StableId),
                    rule?.RuleTitle ?? "军团规则",
                    rule?.RuleDescription ?? definition.Description));
            }
            finally { root.Free(); }
        }
        Bind(heroes);
    }

    public void Bind(IEnumerable<HeroSelectionViewModel> heroes)
    {
        CacheNodes();
        foreach (var child in _library.GetChildren())
        {
            if (child is HeroLibraryTile existing) existing.SelectionRequested -= Preview;
            _library.RemoveChild(child);
            child.Free();
        }
        _models.Clear();
        _tiles.Clear();
        _previewId = string.Empty;
        foreach (var model in heroes)
        {
            _models.Add(model.StableId, model);
            var tile = HeroLibraryTileScene.Instantiate<HeroLibraryTile>();
            _library.AddChild(tile);
            tile.Bind(model);
            tile.SelectionRequested += Preview;
            _tiles.Add(model.StableId, tile);
        }
        var initial = _models.Values.FirstOrDefault(model => model.Unlocked) ?? _models.Values.FirstOrDefault();
        if (initial is not null)
        {
            Preview(initial.StableId);
            _tiles[initial.StableId].CallDeferred(Control.MethodName.GrabFocus);
        }
    }

    public void Preview(string stableId)
    {
        if (!_models.TryGetValue(stableId, out var model)) return;
        _previewId = stableId;
        foreach (var pair in _tiles) pair.Value.SetPreviewed(pair.Key == stableId);
        _detail.Bind(model);
    }

    private void OnDeployRequested(string stableId)
    {
        if (_models.TryGetValue(stableId, out var model) && model.Unlocked)
            EmitSignal(SignalName.HeroChosen, stableId);
    }

    private void OnBack() => BackRequested?.Invoke();

    private void UpdateResponsiveColumns()
    {
        if (_library is not null) _library.Columns = Size.X < 1400f ? 2 : 3;
    }

    private void CacheNodes()
    {
        _library ??= GetNode<GridContainer>("%HeroLibrary");
        _detail ??= GetNode<HeroDetailPanel>("%HeroDetailPanel");
        _back ??= GetNode<Button>("Margin/Layout/BackButton");
    }
}
