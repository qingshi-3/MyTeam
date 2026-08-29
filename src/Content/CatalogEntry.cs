using Godot;

namespace TowerAutobattler.Content;

[GlobalClass]
public partial class CatalogEntry : Resource
{
    [Export] public PackedScene Scene { get; set; } = null!;
    [Export] public Resource Definition { get; set; } = null!;

    public string StableId => Definition switch
    {
        UnitDefinition unit => unit.Id,
        ItemDefinition item => item.Id,
        _ => string.Empty
    };
}
