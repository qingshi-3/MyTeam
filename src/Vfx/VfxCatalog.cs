using System;
using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.Vfx;

[GlobalClass]
public partial class VfxCatalog : Resource
{
    [Export] public Godot.Collections.Array<VfxDefinition> Effects { get; set; } = [];
    public VfxDefinition Find(string id)
    {
        foreach (var item in Effects) if (item.StableId == id) return item;
        throw new InvalidOperationException($"Unknown VFX '{id}' in {ResourcePath}.");
    }
    public void Validate()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in Effects)
            if (item is null || string.IsNullOrWhiteSpace(item.StableId) || !ids.Add(item.StableId) ||
                item.Scene is null || item.Duration <= 0 || item.Size <= 0)
                throw new InvalidOperationException($"Invalid VFX catalog: {ResourcePath}");
    }
}
