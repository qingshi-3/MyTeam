using Godot;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.Content;

// An explicit authoring inventory, separate from the catalog's active roster and pools.
[GlobalClass]
public partial class RetainedContentLibrary : Resource
{
    [Export] public Godot.Collections.Array<CatalogEntry> Heroes { get; set; } = [];
    [Export] public Godot.Collections.Array<AbilityLoadoutDefinition> AbilityLoadouts { get; set; } = [];
}
