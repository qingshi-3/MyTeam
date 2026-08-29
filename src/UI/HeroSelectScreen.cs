using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

public partial class HeroSelectScreen : Control
{
    [Signal] public delegate void HeroChosenEventHandler(string stableId);
    [Export] public PackedScene HeroLibraryTileScene { get; set; } = null!;

    private GridContainer _library = null!;
    private HeroDetailPanel _detail = null!;
    private readonly Dictionary<string, HeroSelectionViewModel> _models = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HeroLibraryTile> _tiles = new(StringComparer.Ordinal);
    private string _previewId = string.Empty;

    public string PreviewStableId => _previewId;

    public override void _Ready()
    {
        CacheNodes();
        Resized += UpdateResponsiveColumns;
        _detail.DeployRequested += OnDeployRequested;
        UpdateResponsiveColumns();
    }

    public override void _ExitTree()
    {
        Resized -= UpdateResponsiveColumns;
        _detail.DeployRequested -= OnDeployRequested;
    }

    public void Bind(IEnumerable<HeroSelectionViewModel> heroes)
    {
        CacheNodes();
        foreach (var child in _library.GetChildren())
        {
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
            tile.PreviewRequested += Preview;
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

    private void UpdateResponsiveColumns()
    {
        if (_library is not null) _library.Columns = Size.X < 1400f ? 2 : 3;
    }

    private void CacheNodes()
    {
        _library ??= GetNode<GridContainer>("%HeroLibrary");
        _detail ??= GetNode<HeroDetailPanel>("%HeroDetailPanel");
    }
}
