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
                item.Scene is null || !float.IsFinite(item.Duration) || item.Duration <= 0 ||
                !float.IsFinite(item.Size) || item.Size <= 0 || !float.IsFinite(item.ReferenceRadius) || item.ReferenceRadius <= 0 ||
                !float.IsFinite(item.CastDuration) || item.CastDuration < 0 || !float.IsFinite(item.FlightDuration) || item.FlightDuration < 0 ||
                !float.IsFinite(item.ActionSpan) || item.ActionSpan < 0 || item.Duration <= item.CastDuration + item.FlightDuration + item.ActionSpan)
                throw new InvalidOperationException($"Invalid VFX catalog: {ResourcePath}");
    }
}
