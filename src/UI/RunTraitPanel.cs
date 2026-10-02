using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

public partial class RunTraitPanel : PanelContainer
{
    private VBoxContainer _rows = null!;
    private PackedScene _rowScene = null!;
    private readonly Dictionary<string, RunTraitRow> _tiles = [];
    public override void _Ready()
    {
        _rows = GetNode<VBoxContainer>("%TraitRows");
        _rowScene = GD.Load<PackedScene>("res://scenes/ui/components/RunTraitRow.tscn");
    }

    public void Bind(IReadOnlyList<RunTraitViewModel> traits)
    {
        var retained = traits.Select(trait => trait.TraitId).ToHashSet();
        foreach (var id in _tiles.Keys.Where(id => !retained.Contains(id)).ToArray())
        {
            var tile = _tiles[id];
            _rows.RemoveChild(tile);
            tile.QueueFree();
            _tiles.Remove(id);
        }
        for (var index = 0; index < traits.Count; index++)
        {
            var trait = traits[index];
            if (!_tiles.TryGetValue(trait.TraitId, out var tile))
            {
                tile = _rowScene.Instantiate<RunTraitRow>();
                _rows.AddChild(tile);
                _tiles.Add(trait.TraitId, tile);
            }
            tile.Bind(trait);
            _rows.MoveChild(tile, index);
        }
        GetNode<Label>("%TraitSummary").Text = $"已激活 {traits.Count(trait => trait.ActiveIndex >= 0)} 项";
        GetNode<Label>("%EmptyTraits").Visible = traits.Count == 0;
    }
}
