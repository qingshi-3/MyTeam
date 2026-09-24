using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.Content;

public static partial class ContentValidator
{
    private static IEnumerable<CatalogEntry> CatalogEntriesForValidation(ContentCatalog catalog)
    {
        foreach (var entry in catalog.AllEntries()) yield return entry;
        if (catalog.RetainedContent is not { } retained) yield break;
        foreach (var entry in retained.Heroes) yield return entry;
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
                report.Error($"Duplicate retained ability loadout: {ResourceLabel(loadout)}");
            if (requireProductionDirectories &&
                (!hasPath || !loadout.ResourcePath.StartsWith("res://content/abilities/loadouts/", StringComparison.Ordinal)))
                report.Error($"Retained ability loadout is outside the production loadout directory: {ResourceLabel(loadout)}");
        }
    }

    private static IEnumerable<AbilityLoadoutDefinition?> CollectRetainedContentAbilityLoadouts(
        ContentCatalog catalog, ValidationReport report)
    {
        if (catalog.RetainedContent is not { } retained) return [];
        return CollectContentAbilityLoadouts(retained.Heroes, report).Concat(retained.AbilityLoadouts);
    }

    private static void ValidateActiveUnitReferences(
        ContentCatalog catalog, IEnumerable<AbilityLoadoutDefinition?> activeLoadouts, ValidationReport report)
    {
        if (catalog.RetainedContent is null) return;
        var activeUnitIds = catalog.AllEntries().Where(entry => entry?.Definition is UnitDefinition)
            .Select(entry => entry.StableId).ToHashSet(StringComparer.Ordinal);
        // The complete authoring graph can read retained identities, but playable suppliers
        // must still resolve their summoned units through the active runtime registry.
        foreach (var ability in activeLoadouts.OfType<AbilityLoadoutDefinition>()
                     .SelectMany(loadout => loadout.Abilities).OfType<AbilityDefinition>()
                     .Distinct<AbilityDefinition>(ReferenceEqualityComparer.Instance))
        foreach (var operation in ability.Operations)
        {
            foreach (var dependency in AbilityDefinitionCompiler.EnemyActionDependencies(operation))
                if (!activeUnitIds.Contains(dependency)) report.Error($"{ResourceLabel(ability)}: unavailable enemy action product: {dependency}");
            var contentId = operation switch
            {
                SummonAbilityOperationSpec summon => summon.SummonContentId,
                LifecycleAbilityOperationSpec { Kind: LifecycleAbilityKind.RaiseCorpse } corpse => corpse.SummonContentId,
                _ => string.Empty
            };
            if (!string.IsNullOrWhiteSpace(contentId) && !activeUnitIds.Contains(contentId))
                report.Error($"{ResourceLabel(ability)}: active ability references an unavailable runtime unit content id: {contentId}");
        }
    }
}
