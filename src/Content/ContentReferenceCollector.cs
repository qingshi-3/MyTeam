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

internal static class ContentReferenceCollector
{
    internal static IReadOnlyList<AbilityLoadoutDefinition?> CollectContentAbilityLoadouts(
        ContentCatalog catalog,
        ValidationReport report) => CollectContentAbilityLoadouts(catalog.AllEntries(), report);

    internal static IReadOnlyList<AbilityLoadoutDefinition?> CollectContentAbilityLoadouts(
        IEnumerable<CatalogEntry> entries,
        ValidationReport report)
    {
        var result = new List<AbilityLoadoutDefinition?>();
        foreach (var entry in entries)
        {
            if (entry?.Scene is null) continue;
            Node? instance = null;
            try
            {
                instance = entry.Scene.Instantiate();
                if (instance is not UnitContentRoot unit) continue;
                if (unit.AbilityLoadout?.Loadout is not null)
                {
                    result.Add(unit.AbilityLoadout.Loadout);
                    var manaSkills = unit.AbilityLoadout.Loadout.Abilities
                        .Where(ability => ability is not null && ability.Trigger == AbilityTriggerKind.ManaFull).ToArray();
                    if (manaSkills.Any(ability => unit.Definition.MaxMana <= 0 ||
                            Math.Abs(unit.Definition.MaxMana - ability.ManaCost) > .001f))
                        report.Error($"{entry.Scene.ResourcePath}: mana-full skill cost must match the unit's positive authored MaxMana.");
                }
            }
            catch (Exception exception)
            {
                report.Error($"Ability-loadout reference scan failed for {entry.Scene.ResourcePath}: {exception.Message}");
            }
            finally
            {
                if (GodotObject.IsInstanceValid(instance)) instance?.Free();
            }
        }
        return result;
    }

    internal static IReadOnlyList<(string ItemId, RelicDefinition? Definition)> CollectItemRelicReferences(
        ContentCatalog catalog,
        ValidationReport report)
    {
        var referenced = new List<(string ItemId, RelicDefinition? Definition)>();
        foreach (var entry in catalog.Items)
        {
            if (entry?.Scene is null) continue;
            ItemContentRoot? root = null;
            try
            {
                root = entry.Scene.Instantiate<ItemContentRoot>();
                if (root.Definition?.ProductKind == ItemProductKind.Relic)
                    referenced.Add((entry.StableId, root.Relic));
            }
            catch (Exception exception)
            {
                report.Error($"Relic reference scan failed for {entry.Scene.ResourcePath}: {exception.Message}");
            }
            finally
            {
                if (GodotObject.IsInstanceValid(root)) root?.Free();
            }
        }
        return referenced;
    }

    internal static IReadOnlyList<(string ItemId, EquipmentDefinition? Definition)> CollectItemEquipmentReferences(
        ContentCatalog catalog,
        ValidationReport report)
    {
        var referenced = new List<(string ItemId, EquipmentDefinition? Definition)>();
        foreach (var entry in catalog.Items)
        {
            if (entry?.Scene is null) continue;
            ItemContentRoot? root = null;
            try
            {
                root = entry.Scene.Instantiate<ItemContentRoot>();
                if (root.Definition?.ProductKind == ItemProductKind.Equipment)
                    referenced.Add((entry.StableId, root.Equipment));
            }
            catch (Exception exception)
            {
                report.Error($"Equipment reference scan failed for {entry.Scene.ResourcePath}: {exception.Message}");
            }
            finally
            {
                if (GodotObject.IsInstanceValid(root)) root?.Free();
            }
        }
        return referenced;
    }

    internal static IReadOnlyList<StatusDefinition?> CollectEquipmentStatusReferences(
        IEnumerable<EquipmentDefinition?> equipment)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        return equipment.Where(definition => definition is not null)
            .SelectMany(definition => definition!.ReactiveStatusBindings ?? [])
            .Where(binding => binding is not null)
            .Select(binding => binding.Status)
            .Concat(equipment.Where(definition => definition is not null).SelectMany(definition => definition!.GrantedStatuses ?? []))
            .ToArray();
    }

    internal static IEnumerable<StatusDefinition?> CollectCarrierStatusReferences(
        IEnumerable<TraitDefinition?> traits, IEnumerable<RelicDefinition?> relics) =>
        traits.OfType<TraitDefinition>().SelectMany(trait => trait.Breakpoints ?? [])
            .Where(tier => tier is not null).SelectMany(tier => tier.GrantedStatuses ?? [])
            .Concat(relics.OfType<RelicDefinition>().SelectMany(relic => relic.StatusGrants ?? [])
                .Where(grant => grant is not null).Select(grant => grant.Status));

    internal static IEnumerable<CatalogEntry> CatalogEntriesForValidation(ContentCatalog catalog)
    {
        foreach (var entry in catalog.AllEntries()) yield return entry;
        if (catalog.RetainedContent is not { } retained) yield break;
        foreach (var entry in retained.Heroes) yield return entry;
    }

    internal static IEnumerable<AbilityLoadoutDefinition?> CollectRetainedContentAbilityLoadouts(
        ContentCatalog catalog, ValidationReport report)
    {
        if (catalog.RetainedContent is not { } retained) return [];
        return CollectContentAbilityLoadouts(retained.Heroes, report).Concat(retained.AbilityLoadouts);
    }

    internal static void ValidateActiveUnitReferences(
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
                if (!activeUnitIds.Contains(dependency)) report.Error($"{ContentResourceDiscovery.ResourceLabel(ability)}: unavailable enemy action product: {dependency}");
            if (operation is MatrixAbilityOperationSpec matrix)
                foreach (var node in MatrixAbilityCompiler.Flatten(matrix).Where(node => node.Kind == MatrixOperationKind.Summon))
                    if (!activeUnitIds.Contains(node.ContentId))
                        report.Error($"{ContentResourceDiscovery.ResourceLabel(ability)}: unavailable matrix summon product: {node.ContentId}");
            var contentId = operation switch
            {
                SummonAbilityOperationSpec summon => summon.SummonContentId,
                LifecycleAbilityOperationSpec { Kind: LifecycleAbilityKind.RaiseCorpse } corpse => corpse.SummonContentId,
                _ => string.Empty
            };
            if (!string.IsNullOrWhiteSpace(contentId) && !activeUnitIds.Contains(contentId))
                report.Error($"{ContentResourceDiscovery.ResourceLabel(ability)}: active ability references an unavailable runtime unit content id: {contentId}");
        }
    }

}
