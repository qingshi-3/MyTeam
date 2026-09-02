using System;
using System.Collections.Generic;
using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

[GlobalClass]
public partial class SemanticIconCatalog : Resource
{
    [Export] public Godot.Collections.Array<SemanticIconEntry> Entries { get; set; } = [];

    public bool TryResolve(StringName key, out SemanticIconEntry entry)
    {
        var requested = key.ToString();
        foreach (var candidate in Entries)
        {
            if (candidate is not null && string.Equals(candidate.Key.ToString(), requested, StringComparison.Ordinal))
            {
                entry = candidate;
                return true;
            }
        }

        entry = null!;
        return false;
    }

    public Texture2D? ResolveIcon(StringName key) => TryResolve(key, out var entry) ? entry.Icon : null;

    public ValidationReport Validate()
    {
        var report = new ValidationReport();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in Entries)
        {
            if (entry is null)
            {
                report.Error("semantic icon catalog contains a null entry.");
                continue;
            }

            var key = entry.Key.ToString();
            if (string.IsNullOrWhiteSpace(key)) report.Error("semantic icon catalog contains a blank key.");
            else if (!seen.Add(key)) report.Error($"semantic icon catalog contains duplicate key '{key}'.");
            if (entry.Icon is null) report.Error($"semantic icon '{key}' has no texture.");
        }
        foreach (var requiredKey in SemanticIconKeys.Required)
        {
            var key = requiredKey.ToString();
            if (!seen.Contains(key)) report.Error($"semantic icon catalog is missing required key '{key}'.");
        }

        return report;
    }
}

public static class SemanticIcons
{
    public const string CatalogPath = "res://content/ui/semantic_icon_catalog.tres";
    private static SemanticIconCatalog? _catalog;

    public static SemanticIconCatalog Catalog => _catalog ??= GD.Load<SemanticIconCatalog>(CatalogPath);

    public static void Configure(SemanticIconCatalog catalog) =>
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
}
