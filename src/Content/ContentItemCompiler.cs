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

internal static class ContentItemCompiler
{
    internal static void ValidateRelicRewardPools(IEnumerable<CompiledRelicDefinition> relics, ContentCatalog catalog, ValidationReport report)
    {
        foreach (var relic in relics)
        foreach (var rule in relic.RunRules.IsDefault ? [] : relic.RunRules)
        foreach (var id in rule.ContentIds)
        {
            var expectedKind = rule.Kind is RelicRunRuleKind.CounterReward or RelicRunRuleKind.VictoryNativeKillReward
                ? ItemProductKind.Equipment : ItemProductKind.Relic;
            if (!ContentReferenceCollector.CatalogEntriesForValidation(catalog).Any(entry => entry?.StableId == id &&
                entry.Definition is ItemDefinition item && item.ProductKind == expectedKind))
                report.Error($"{relic.StableId}: reward pool '{rule.PoolId}' must contain only {expectedKind} items: {id}");
        }

    }

    internal static EquipmentGraphCompilationResult CompileEquipmentGraph(
        IEnumerable<EquipmentDefinition?> authoredDefinitions,
        IEnumerable<(string ItemId, EquipmentDefinition? Definition)> itemReferences,
        IEnumerable<CompiledStatusDefinition>? statuses = null)
    {
        ArgumentNullException.ThrowIfNull(authoredDefinitions);
        ArgumentNullException.ThrowIfNull(itemReferences);
        var report = new ValidationReport();
        var authored = authoredDefinitions.ToArray();
        var referenced = itemReferences.ToArray();
        foreach (var (itemId, definition) in referenced)
        {
            if (definition is null)
                report.Error($"{itemId}: production Equipment scene has no Equipment definition.");
            else if (definition.StableId != itemId)
                report.Error($"{itemId}: Equipment definition stable id does not match its catalog entry.");
        }

        var authoredSet = authored.Where(definition => definition is not null)
            .Cast<EquipmentDefinition>()
            .ToHashSet<EquipmentDefinition>(ReferenceEqualityComparer.Instance);
        var referencedSet = referenced.Select(reference => reference.Definition)
            .Where(definition => definition is not null)
            .Cast<EquipmentDefinition>()
            .ToHashSet<EquipmentDefinition>(ReferenceEqualityComparer.Instance);
        foreach (var definition in authoredSet.Where(definition => !referencedSet.Contains(definition)))
            report.Error($"Orphan Equipment definition is not referenced by concrete item content: {ContentResourceDiscovery.ResourceLabel(definition)}");
        foreach (var definition in referencedSet.Where(definition => !authoredSet.Contains(definition)))
            report.Error($"Concrete item content references an unregistered Equipment definition: {ContentResourceDiscovery.ResourceLabel(definition)}");

        var compiledStatuses = (statuses ?? []).ToArray();
        var statusesByPath = compiledStatuses.Where(status => !string.IsNullOrWhiteSpace(status.ResourcePath))
            .ToDictionary(status => status.ResourcePath, StringComparer.Ordinal);
        var statusesById = compiledStatuses.ToDictionary(status => status.StableId, StringComparer.Ordinal);
        CompiledStatusDefinition? ResolveStatus(StatusDefinition? status)
        {
            if (status is null) return null;
            if (!string.IsNullOrWhiteSpace(status.ResourcePath) &&
                statusesByPath.TryGetValue(status.ResourcePath, out var byPath))
                return byPath;
            return statusesById.GetValueOrDefault(status.StableId);
        }
        var compilation = EquipmentDefinitionCompiler.CompileBatch(authored, ResolveStatus);
        report.Merge(compilation.Report);
        return new EquipmentGraphCompilationResult(
            report.HasCoreErrors
                ? new EquipmentBatchCompilationResult([], report)
                : compilation,
            report);
    }

    internal static RelicGraphCompilationResult CompileRelicGraph(
        IEnumerable<RelicDefinition?> authoredDefinitions,
        IEnumerable<(string ItemId, RelicDefinition? Definition)> itemReferences,
        IReadOnlySet<string> validContentIds,
        IEnumerable<CompiledStatusDefinition>? statuses = null)
    {
        ArgumentNullException.ThrowIfNull(authoredDefinitions);
        ArgumentNullException.ThrowIfNull(itemReferences);
        ArgumentNullException.ThrowIfNull(validContentIds);
        var report = new ValidationReport();
        var authored = authoredDefinitions.ToArray();
        var referenced = itemReferences.ToArray();
        foreach (var (itemId, definition) in referenced)
        {
            if (definition is null)
                report.Error($"{itemId}: production item scene has no relic definition.");
            else if (definition.StableId != itemId)
                report.Error($"{itemId}: relic definition stable id does not match its catalog entry.");
        }

        var authoredSet = authored.Where(definition => definition is not null)
            .Cast<RelicDefinition>()
            .ToHashSet<RelicDefinition>(ReferenceEqualityComparer.Instance);
        var referencedSet = referenced.Select(reference => reference.Definition).Where(definition => definition is not null)
            .Cast<RelicDefinition>()
            .ToHashSet<RelicDefinition>(ReferenceEqualityComparer.Instance);
        foreach (var definition in authoredSet.Where(definition => !referencedSet.Contains(definition)))
            report.Error($"Orphan relic definition is not referenced by concrete item content: {ContentResourceDiscovery.ResourceLabel(definition)}");
        foreach (var definition in referencedSet.Where(definition => !authoredSet.Contains(definition)))
            report.Error($"Concrete item content references an unregistered relic definition: {ContentResourceDiscovery.ResourceLabel(definition)}");

        var batch = RelicDefinitionCompiler.CompileBatch(
            authored,
            validContentIds, StatusResolver(statuses));
        report.Merge(batch.Report);
        if (!report.HasCoreErrors && batch.Definitions.Length != referenced.Length)
            report.Error("Relic publication did not compile exactly one definition for every production item scene.");
        return new RelicGraphCompilationResult(batch, report);
    }

    internal static Func<StatusDefinition?, CompiledStatusDefinition?> StatusResolver(IEnumerable<CompiledStatusDefinition>? statuses)
    {
        var definitions = (statuses ?? []).ToArray();
        var byId = definitions.ToDictionary(status => status.StableId, StringComparer.Ordinal);
        var byPath = definitions.Where(status => !string.IsNullOrEmpty(status.ResourcePath))
            .ToDictionary(status => status.ResourcePath, StringComparer.Ordinal);
        return authored => authored is null ? null :
            !string.IsNullOrEmpty(authored.ResourcePath) && byPath.TryGetValue(authored.ResourcePath, out var byResource)
                ? byResource : byId.GetValueOrDefault(authored.StableId);
    }

}
