using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

public partial class RunRelicShelf : PanelContainer
{
    private HBoxContainer _icons = null!;
    private PackedScene _iconScene = null!;
    private readonly Dictionary<string, RunRelicIcon> _tiles = [];

    public override void _Ready()
    {
        _icons = GetNode<HBoxContainer>("%RelicIcons");
        _iconScene = GD.Load<PackedScene>("res://scenes/ui/components/RunRelicIcon.tscn");
    }

    public void Bind(IReadOnlyList<RunRelicViewModel> relics)
    {
        var retained = relics.Select(relic => relic.ContentId).ToHashSet();
        foreach (var id in _tiles.Keys.Where(id => !retained.Contains(id)).ToArray())
        {
            var tile = _tiles[id];
            _icons.RemoveChild(tile);
            tile.QueueFree();
            _tiles.Remove(id);
        }
        for (var index = 0; index < relics.Count; index++)
        {
            var relic = relics[index];
            if (!_tiles.TryGetValue(relic.ContentId, out var tile))
            {
                tile = _iconScene.Instantiate<RunRelicIcon>();
                _icons.AddChild(tile);
                _tiles.Add(relic.ContentId, tile);
            }
            tile.Bind(relic);
            tile.HintAnchor = this;
            _icons.MoveChild(tile, index);
        }
        GetNode<Label>("%RelicCount").Text = relics.Count == 0 ? "遗物" : $"遗物 {relics.Count}";
        GetNode<Label>("%EmptyRelics").Visible = relics.Count == 0;
        CustomMinimumSize = new Vector2(Mathf.Clamp(82 + relics.Count * 52, 180, 544), 64);
        if (GetParent() is not Container) Size = new Vector2(CustomMinimumSize.X, 64);
    }
}
