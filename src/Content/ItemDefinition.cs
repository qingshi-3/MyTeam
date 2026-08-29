using Godot;

namespace TowerAutobattler.Content;

public enum ItemRarity { Common, Uncommon, Rare, Legendary }

[GlobalClass]
public partial class ItemDefinition : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
    [Export] public Texture2D? Icon { get; set; }
    [Export] public ItemRarity Rarity { get; set; } = ItemRarity.Common;
    [Export] public int Price { get; set; } = 10;
    [Export] public StringName[] Tags { get; set; } = [];
}
