using Godot;

namespace TowerAutobattler.Content;

[GlobalClass]
public partial class ContentCatalog : Resource
{
    [Export] public Godot.Collections.Array<CatalogEntry> Heroes { get; set; } = [];
    [Export] public Godot.Collections.Array<CatalogEntry> Soldiers { get; set; } = [];
    [Export] public Godot.Collections.Array<CatalogEntry> Enemies { get; set; } = [];
    [Export] public Godot.Collections.Array<CatalogEntry> Items { get; set; } = [];
    [Export] public Godot.Collections.Array<PackedScene> FloorRules { get; set; } = [];

    public Godot.Collections.Array<CatalogEntry> AllEntries()
    {
        var result = new Godot.Collections.Array<CatalogEntry>();
        foreach (var entry in Heroes) result.Add(entry);
        foreach (var entry in Soldiers) result.Add(entry);
        foreach (var entry in Enemies) result.Add(entry);
        foreach (var entry in Items) result.Add(entry);
        return result;
    }
}
