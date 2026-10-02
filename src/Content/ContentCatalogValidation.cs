using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Battle;
using TowerAutobattler.Equipment;
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Content;

internal static class ContentCatalogValidation
{
    private static readonly Regex StableIdPattern = new("^[a-z0-9]+(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant);
    internal static ValidationReport ValidateAuthoredEntries(ContentCatalog? catalog)
    {
        var report = new ValidationReport();
        if (catalog is null)
        {
            report.Error("Content catalog failed to load.");
            return report;
        }

        ValidateCatalogEntries(
            catalog, report,
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            requireProductionDirectories: false);
        return report;
    }

    internal static void ValidateFloorRules(
        ContentCatalog catalog,
        ValidationReport report,
        bool requireProductionDirectories)
    {
        var floorScenePaths = new HashSet<string>(StringComparer.Ordinal);
        var floorIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var floorRule in catalog.FloorRules)
        {
            if (floorRule is null || string.IsNullOrWhiteSpace(floorRule.ResourcePath))
            {
                report.Error("Floor-rule catalog contains an empty scene.");
                continue;
            }
            if (!floorScenePaths.Add(floorRule.ResourcePath))
            {
                report.Error($"Duplicate floor-rule scene: {floorRule.ResourcePath}");
                continue;
            }
            try
            {
                var root = floorRule.Instantiate<FloorRuleContentRoot>();
                try
                {
                    report.Merge(root.ValidateAuthoring());
                    if (!StableIdPattern.IsMatch(root.Id)) report.Error($"Invalid floor-rule id: {root.Id}");
                    if (!floorIds.Add(root.Id)) report.Error($"Duplicate floor-rule id: {root.Id}");
                }
                finally { root.Free(); }
            }
            catch (Exception exception)
            {
                report.Error($"Floor-rule instantiate failed for {floorRule.ResourcePath}: {exception.Message}");
            }
        }

        if (!requireProductionDirectories) return;
        var diskFloorScenes = ContentResourceDiscovery.DiscoverFiles("res://content/floor-rules", ".tscn", false, report);
        foreach (var scenePath in floorScenePaths)
            if (!diskFloorScenes.Contains(scenePath)) report.Error($"Catalog floor-rule scene does not exist: {scenePath}");
        foreach (var scenePath in diskFloorScenes)
            if (!floorScenePaths.Contains(scenePath)) report.Error($"Floor-rule scene is missing from catalog: {scenePath}");
    }

    private static void ValidateGroup(
        Godot.Collections.Array<CatalogEntry> entries, ContentCategory category, ValidationReport report,
        HashSet<string> ids, HashSet<string> scenes, HashSet<string> definitions, HashSet<string> portraits,
        bool requireProductionDirectories)
    {
        foreach (var entry in entries)
            ValidateEntry(entry, category, report, ids, scenes, definitions, portraits, requireProductionDirectories);
    }

    internal static void ValidateCatalogEntries(
        ContentCatalog catalog, ValidationReport report,
        HashSet<string> ids, HashSet<string> scenes, HashSet<string> definitions, HashSet<string> portraits,
        bool requireProductionDirectories)
    {
        ValidateGroup(catalog.Heroes, ContentCategory.Hero, report, ids, scenes, definitions, portraits, requireProductionDirectories);
        ValidateGroup(catalog.Soldiers, ContentCategory.Soldier, report, ids, scenes, definitions, portraits, requireProductionDirectories);
        ValidateGroup(catalog.Enemies, ContentCategory.Enemy, report, ids, scenes, definitions, portraits, requireProductionDirectories);
        ValidateGroup(catalog.Items, ContentCategory.Item, report, ids, scenes, definitions, portraits, requireProductionDirectories);
        ValidateRetainedContent(catalog, report, ids, scenes, definitions, portraits, requireProductionDirectories);
    }

    private static void ValidateEntry(
        CatalogEntry? entry,
        ContentCategory category,
        ValidationReport report,
        HashSet<string> ids,
        HashSet<string> scenes,
        HashSet<string> definitions,
        HashSet<string> portraits,
        bool requireProductionDirectories)
    {
        if (entry is null) { report.Error("Catalog contains a null entry."); return; }
        if (entry.Scene is null) { report.Error("Catalog entry has no PackedScene."); return; }
        if (entry.Definition is null) { report.Error($"{entry.Scene.ResourcePath}: catalog entry has no definition."); return; }
        if (string.IsNullOrWhiteSpace(entry.StableId)) report.Error($"{entry.Scene.ResourcePath}: definition has no stable id.");
        else if (!StableIdPattern.IsMatch(entry.StableId)) report.Error($"Invalid stable id: {entry.StableId}");
        else if (!ids.Add(entry.StableId)) report.Error($"Duplicate content id: {entry.StableId}");
        if (!scenes.Add(entry.Scene.ResourcePath)) report.Error($"Duplicate scene reference: {entry.Scene.ResourcePath}");
        if (string.IsNullOrWhiteSpace(entry.Definition.ResourcePath)) report.Error($"{entry.StableId}: definition is not an external resource.");
        else if (!definitions.Add(entry.Definition.ResourcePath)) report.Error($"Duplicate definition reference: {entry.Definition.ResourcePath}");

        try
        {
            var instance = entry.Scene.Instantiate();
            try
            {
                switch (instance)
                {
                    case UnitContentRoot unit:
                        report.Merge(unit.ValidateAuthoring());
                        if (!ReferenceEquals(unit.Definition, entry.Definition))
                            report.Error($"{entry.StableId}: scene root and catalog do not reference the same definition.");
                        ValidateUnitCategory(
                            unit.Definition, category, entry.Scene.ResourcePath, report, portraits,
                            requireProductionDirectories);
                        break;
                    case ItemContentRoot item:
                        report.Merge(item.ValidateAuthoring());
                        if (!ReferenceEquals(item.Definition, entry.Definition))
                            report.Error($"{entry.StableId}: scene root and catalog do not reference the same definition.");
                        if (category != ContentCategory.Item) report.Error($"{entry.StableId}: item appears in {category} catalog.");
                        break;
                    default:
                        report.Error($"{entry.Scene.ResourcePath}: root must be UnitContentRoot or ItemContentRoot.");
                        break;
                }
            }
            finally { instance.Free(); }
        }
        catch (Exception exception)
        {
            report.Error($"Scene instantiate failed for {entry.Scene.ResourcePath}: {exception.Message}");
        }
    }

    private static void ValidateUnitCategory(
        UnitDefinition definition, ContentCategory category, string scenePath, ValidationReport report,
        HashSet<string> portraits, bool requireProductionDirectories)
    {
        var valid = category switch
        {
            ContentCategory.Hero => definition.IsHero && !definition.IsEnemy,
            ContentCategory.Soldier => !definition.IsHero && !definition.IsEnemy,
            ContentCategory.Enemy => !definition.IsHero && definition.IsEnemy,
            _ => false
        };
        if (!valid) report.Error($"{definition.Id}: category flags do not match {category}.");
        if (!Enum.IsDefined(definition.AttackDelivery) ||
            !float.IsFinite(definition.ProjectileSpeed) || definition.ProjectileSpeed is <= 0 or > 100 ||
            !float.IsFinite(definition.ProjectileRadius) || definition.ProjectileRadius is <= 0 or > .5f ||
            !float.IsFinite(definition.ProjectileLifetime) || definition.ProjectileLifetime is <= 0 or > 10)
            report.Error($"{definition.Id}: invalid attack delivery or projectile speed/radius/lifetime.");
        if (definition.AttackVisual is { } visual &&
            (string.IsNullOrWhiteSpace(visual.StableId) || visual.Scene is null ||
             (definition.AttackDelivery == AttackDelivery.Projectile && (!visual.Persistent || !visual.FaceDirection)) ||
             (definition.AttackDelivery == AttackDelivery.Beam && (visual.Persistent || !visual.StretchBetween))))
            report.Error($"{definition.Id}: attack visual does not match its delivery.");
        if (!float.IsFinite(definition.ProjectileWindupSeconds) || definition.ProjectileWindupSeconds is < 0 or > 5 ||
            !float.IsFinite(definition.AttackReleaseProgress) || definition.AttackReleaseProgress is < .05f or > 1)
            report.Error($"{definition.Id}: invalid projectile windup or attack release progress.");
        if (!float.IsFinite(definition.BaseControlResistance) ||
            definition.BaseControlResistance is < 0 or > 1)
            report.Error($"{definition.Id}: BaseControlResistance must be finite and within [0,1].");
        if (!float.IsFinite(definition.SpellPower) || definition.SpellPower < 0)
            report.Error($"{definition.Id}: SpellPower must be finite and nonnegative.");
        if (new[] { definition.MaxMana, definition.StartingMana, definition.ManaPerSecond,
                definition.ManaPerAttack, definition.ManaPerDamageRatio, definition.ManaPerHitCap }
            .Any(value => !float.IsFinite(value) || value < 0 || value > 1_000_000) ||
            definition.StartingMana > definition.MaxMana)
            report.Error($"{definition.Id}: mana values must be finite, nonnegative and StartingMana cannot exceed MaxMana.");
        if (!float.IsFinite(definition.BodyRadius) ||
            definition.BodyRadius is < TowerAutobattler.Battle.BattlefieldSpace.MinimumBodyRadius or > TowerAutobattler.Battle.BattlefieldSpace.MaximumBodyRadius)
            report.Error($"{definition.Id}: BodyRadius must be finite and within [0.1,1.5].");
        var expectedFolder = category switch
        {
            ContentCategory.Hero => "heroes",
            ContentCategory.Soldier => "soldiers",
            ContentCategory.Enemy => "enemies",
            _ => "items"
        };
        if (requireProductionDirectories && !scenePath.StartsWith($"res://content/{expectedFolder}/", StringComparison.Ordinal))
            report.Error($"{definition.Id}: scene is outside its {category} directory.");
        var requiredTag = category.ToString().ToLowerInvariant();
        if (!definition.Tags.Contains(requiredTag)) report.Error($"{definition.Id}: missing required tag '{requiredTag}'.");
        if (definition.Portrait is null)
        {
            report.Error($"{definition.Id}: missing production portrait resource.");
            return;
        }
        report.Merge(definition.Portrait.Validate(definition.Id));
        if (string.IsNullOrWhiteSpace(definition.Portrait.ResourcePath))
        {
            report.Error($"{definition.Id}: portrait is not an external resource.");
            return;
        }
        if (!portraits.Add(definition.Portrait.ResourcePath))
            report.Error($"{definition.Id}: duplicate portrait resource reference {definition.Portrait.ResourcePath}.");
        if (requireProductionDirectories && !definition.Portrait.ResourcePath.StartsWith($"res://content/portraits/{expectedFolder}/", StringComparison.Ordinal))
            report.Error($"{definition.Id}: portrait is outside its {category} directory.");
    }

    private static void ValidateRetainedContent(
        ContentCatalog catalog, ValidationReport report,
        HashSet<string> ids, HashSet<string> scenes, HashSet<string> definitions, HashSet<string> portraits,
        bool requireProductionDirectories)
    {
        if (catalog.RetainedContent is not { } retained) return;
        if (requireProductionDirectories &&
            !retained.ResourcePath.StartsWith("res://content/catalogs/", StringComparison.Ordinal))
            report.Error("Retained content library must be an external resource under the production catalogs directory.");

        // Share all identity and path sets with active content: retention never waives collisions,
        // independent scene authoring, category flags, portraits, or reference consistency.
        ValidateGroup(retained.Heroes, ContentCategory.Hero, report,
            ids, scenes, definitions, portraits, requireProductionDirectories);
        var paths = new HashSet<string>(StringComparer.Ordinal);
        var resources = new HashSet<AbilityLoadoutDefinition>(ReferenceEqualityComparer.Instance);
        foreach (var loadout in retained.AbilityLoadouts)
        {
            if (loadout is null)
            {
                report.Error("Retained content library contains a null ability loadout.");
                continue;
            }
            var hasPath = !string.IsNullOrWhiteSpace(loadout.ResourcePath);
            if (!resources.Add(loadout) || hasPath && !paths.Add(loadout.ResourcePath))
                report.Error($"Duplicate retained ability loadout: {ContentResourceDiscovery.ResourceLabel(loadout)}");
            if (requireProductionDirectories &&
                (!hasPath || !loadout.ResourcePath.StartsWith("res://content/abilities/loadouts/", StringComparison.Ordinal)))
                report.Error($"Retained ability loadout is outside the production loadout directory: {ContentResourceDiscovery.ResourceLabel(loadout)}");
        }
    }

    private enum ContentCategory { Hero, Soldier, Enemy, Item }
}
