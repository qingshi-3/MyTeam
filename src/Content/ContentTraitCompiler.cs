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

internal static class ContentTraitCompiler
{
    internal static TraitGraphCompilationResult CompileTraitGraph(
        IEnumerable<TraitDefinition?> authoredDefinitions,
        ContentCatalog catalog,
        IReadOnlyList<CompiledEquipmentDefinition> equipment,
        IEnumerable<CompiledStatusDefinition>? statuses = null)
    {
        ArgumentNullException.ThrowIfNull(authoredDefinitions);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(equipment);
        var report = new ValidationReport();
        var compilation = TraitDefinitionCompiler.CompileBatch(authoredDefinitions, ContentItemCompiler.StatusResolver(statuses));
        report.Merge(compilation.Report);
        var validIds = compilation.Definitions.Select(definition => definition.StableId)
            .ToHashSet(StringComparer.Ordinal);
        var byUnit = new Dictionary<string, System.Collections.Immutable.ImmutableArray<CompiledTraitContribution>>(
            StringComparer.Ordinal);
        var activeUnitIds = catalog.AllEntries().Where(entry => entry?.Definition is UnitDefinition)
            .Select(entry => entry.StableId).ToHashSet(StringComparer.Ordinal);
        foreach (var trait in compilation.Definitions)
        foreach (var tier in trait.Breakpoints)
        foreach (var mechanic in tier.Mechanics.IsDefault ? [] : tier.Mechanics)
            if (!string.IsNullOrEmpty(mechanic.SummonContentId) && !activeUnitIds.Contains(mechanic.SummonContentId))
                report.Error($"{trait.StableId}: unavailable trait summon product: {mechanic.SummonContentId}");
        foreach (var entry in ContentReferenceCollector.CatalogEntriesForValidation(catalog).Where(entry => entry?.Definition is UnitDefinition))
        {
            var unit = (UnitDefinition)entry.Definition;
            var contributions = TraitDefinitionCompiler.CompileContributions(
                unit.TraitContributions ?? [],
                string.IsNullOrWhiteSpace(unit.ResourcePath) ? unit.Id : unit.ResourcePath);
            report.Merge(contributions.Report);
            ValidateTraitContributionDependencies(contributions.Contributions, validIds, unit.Id, report);
            if (!contributions.Contributions.IsEmpty && activeUnitIds.Contains(unit.Id))
                byUnit[unit.Id] = contributions.Contributions;
        }
        foreach (var definition in equipment)
            ValidateTraitContributionDependencies(
                definition.TraitContributions,
                validIds,
                string.IsNullOrWhiteSpace(definition.ResourcePath) ? definition.StableId : definition.ResourcePath,
                report);
        return new TraitGraphCompilationResult(
            report.HasCoreErrors ? [] : compilation.Definitions,
            report.HasCoreErrors
                ? new Dictionary<string, System.Collections.Immutable.ImmutableArray<CompiledTraitContribution>>(
                    StringComparer.Ordinal)
                : byUnit,
            report);
    }

    internal static void ValidateTraitContributionDependencies(
        IEnumerable<CompiledTraitContribution> contributions,
        IReadOnlySet<string> validIds,
        string ownerLabel,
        ValidationReport report)
    {
        foreach (var contribution in contributions)
            if (!validIds.Contains(contribution.TraitId))
                report.Error($"{ownerLabel}: Trait contribution references missing Trait '{contribution.TraitId}'.");
    }

}
