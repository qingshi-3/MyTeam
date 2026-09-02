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

public static class ContentValidator
{
    private static readonly string[] ConcreteDirectories =
    [
        "res://content/heroes", "res://content/soldiers", "res://content/enemies", "res://content/items"
    ];
    private static readonly Regex StableIdPattern = new("^[a-z0-9]+(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant);

    public static ValidationReport Validate(
        ContentCatalog? catalog,
        IEnumerable<PackedScene>? additionalStructuralValidationScenes = null) =>
        CompileProductionGraph(catalog, [], additionalStructuralValidationScenes).Report;

    internal static ContentGraphCompilationResult CompileProductionGraph(
        ContentCatalog? catalog,
        IReadOnlyList<AbilityLoadoutDefinition?> additionalLoadoutReferences,
        IEnumerable<PackedScene>? additionalStructuralValidationScenes = null)
    {
        var report = new ValidationReport();
        if (catalog is null)
        {
            report.Error("Content catalog failed to load.");
            return new ContentGraphCompilationResult(null, report);
        }

        if (additionalStructuralValidationScenes is not null)
            foreach (var scene in additionalStructuralValidationScenes)
                ValidateStructuralProbe(scene, report);

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var scenes = new HashSet<string>(StringComparer.Ordinal);
        var definitions = new HashSet<string>(StringComparer.Ordinal);
        var portraits = new HashSet<string>(StringComparer.Ordinal);
        ValidateCatalogEntries(catalog, report, ids, scenes, definitions, portraits, requireProductionDirectories: true);

        var diskScenes = DiscoverConcreteScenes(report);
        foreach (var scenePath in scenes)
            if (!diskScenes.Contains(scenePath)) report.Error($"Catalog scene does not exist in concrete directories: {scenePath}");
        foreach (var scenePath in diskScenes)
            if (!scenes.Contains(scenePath)) report.Error($"Concrete scene is missing from catalog: {scenePath}");

        var diskDefinitions = DiscoverFiles("res://content/definitions", ".tres", true, report);
        foreach (var definitionPath in definitions)
            if (!diskDefinitions.Contains(definitionPath)) report.Error($"Catalog definition does not exist: {definitionPath}");
        foreach (var definitionPath in diskDefinitions)
            if (!definitions.Contains(definitionPath)) report.Error($"Definition is missing from catalog: {definitionPath}");

        var diskPortraits = DiscoverFiles("res://content/portraits", ".tres", true, report);
        foreach (var portraitPath in portraits)
            if (!diskPortraits.Contains(portraitPath)) report.Error($"Catalog portrait does not exist: {portraitPath}");
        foreach (var portraitPath in diskPortraits)
            if (!portraits.Contains(portraitPath)) report.Error($"Portrait resource is missing from the production unit catalog: {portraitPath}");

        ValidateFloorRules(catalog, report, requireProductionDirectories: true);

        var tacticalAuthored = LoadProductionTacticalCommandGraph(report);
        var equipmentStatusReferences = CollectEquipmentStatusReferences(
            CollectItemEquipmentReferences(catalog, report).Select(reference => reference.Definition));
        var abilityStatus = CompileProductionAbilityStatusGraph(
            catalog,
            additionalLoadoutReferences.Concat(tacticalAuthored.Definitions
                .Where(definition => definition is not null)
                .Select(definition => definition!.AbilityLoadout)).ToArray(),
            equipmentStatusReferences,
            report);
        var relics = CompileProductionRelicGraph(catalog, report);
        var equipment = CompileProductionEquipmentGraph(catalog, abilityStatus.Statuses.Definitions, report);
        var traits = CompileProductionTraitGraph(catalog, equipment.Equipment.Definitions, report);
        var tacticalCommands = CompileTacticalCommandGraph(
            tacticalAuthored,
            abilityStatus.Abilities,
            report,
            requireProductionDirectories: true);
        var graph = report.HasCoreErrors
            ? null
            : new CompiledContentGraph(
                abilityStatus.Abilities.Loadouts,
                abilityStatus.Abilities.Abilities,
                abilityStatus.Statuses.Definitions,
                relics.Relics.Definitions,
                equipment.Equipment.Definitions,
                tacticalCommands.Definitions,
                tacticalAuthored.Definitions.Where(definition => definition is not null)
                    .Cast<TacticalCommandDefinition>(),
                traits.Definitions,
                traits.UnitContributions);
        return new ContentGraphCompilationResult(graph, report);
    }

    private static TraitGraphCompilationResult CompileProductionTraitGraph(
        ContentCatalog catalog,
        IReadOnlyList<CompiledEquipmentDefinition> equipment,
        ValidationReport report)
    {
        var authored = LoadResources<TraitDefinition>(
            DiscoverFiles("res://content/traits/definitions", ".tres", false, report),
            "Trait definition",
            report);
        var compilation = CompileTraitGraph(authored, catalog, equipment);
        report.Merge(compilation.Report);
        return compilation;
    }

    private static EquipmentGraphCompilationResult CompileProductionEquipmentGraph(
        ContentCatalog catalog,
        IReadOnlyList<CompiledStatusDefinition> statuses,
        ValidationReport report)
    {
        var authored = LoadResources<EquipmentDefinition>(
            DiscoverFiles("res://content/equipment/definitions", ".tres", false, report),
            "Equipment definition",
            report);
        var referenced = CollectItemEquipmentReferences(catalog, report);
        foreach (var (itemId, definition) in referenced)
            if (definition is not null &&
                (string.IsNullOrWhiteSpace(definition.ResourcePath) ||
                 !definition.ResourcePath.StartsWith("res://content/equipment/definitions/", StringComparison.Ordinal)))
                report.Error($"{itemId}: Equipment definition is outside the production Equipment directory.");

        var compilation = CompileEquipmentGraph(authored, referenced, statuses);
        report.Merge(compilation.Report);
        return compilation;
    }

    private static RelicGraphCompilationResult CompileProductionRelicGraph(
        ContentCatalog catalog,
        ValidationReport report)
    {
        var authored = LoadResources<RelicDefinition>(
            DiscoverFiles("res://content/relics/definitions", ".tres", false, report),
            "relic definition",
            report);
        var referenced = CollectItemRelicReferences(catalog, report);
        foreach (var (itemId, definition) in referenced)
            if (definition is not null &&
                (string.IsNullOrWhiteSpace(definition.ResourcePath) ||
                 !definition.ResourcePath.StartsWith("res://content/relics/definitions/", StringComparison.Ordinal)))
                report.Error($"{itemId}: relic definition is outside the production relic directory.");

        var compilation = CompileRelicGraph(
            authored,
            referenced,
            catalog.AllEntries().Select(entry => entry.StableId).ToHashSet(StringComparer.Ordinal));
        report.Merge(compilation.Report);
        return compilation;
    }

    internal static ValidationReport ValidateRelicGraph(
        IEnumerable<RelicDefinition?> authoredDefinitions,
        IEnumerable<(string ItemId, RelicDefinition? Definition)> itemReferences,
        IReadOnlySet<string> validContentIds) =>
        CompileRelicGraph(authoredDefinitions, itemReferences, validContentIds).Report;

    internal static ValidationReport ValidateEquipmentGraph(
        IEnumerable<EquipmentDefinition?> authoredDefinitions,
        IEnumerable<(string ItemId, EquipmentDefinition? Definition)> itemReferences,
        IEnumerable<CompiledStatusDefinition>? statuses = null) =>
        CompileEquipmentGraph(authoredDefinitions, itemReferences, statuses).Report;

    private static EquipmentGraphCompilationResult CompileEquipmentGraph(
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
            report.Error($"Orphan Equipment definition is not referenced by concrete item content: {ResourceLabel(definition)}");
        foreach (var definition in referencedSet.Where(definition => !authoredSet.Contains(definition)))
            report.Error($"Concrete item content references an unregistered Equipment definition: {ResourceLabel(definition)}");

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

    private static RelicGraphCompilationResult CompileRelicGraph(
        IEnumerable<RelicDefinition?> authoredDefinitions,
        IEnumerable<(string ItemId, RelicDefinition? Definition)> itemReferences,
        IReadOnlySet<string> validContentIds)
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
            report.Error($"Orphan relic definition is not referenced by concrete item content: {ResourceLabel(definition)}");
        foreach (var definition in referencedSet.Where(definition => !authoredSet.Contains(definition)))
            report.Error($"Concrete item content references an unregistered relic definition: {ResourceLabel(definition)}");

        var batch = RelicDefinitionCompiler.CompileBatch(
            authored,
            validContentIds);
        report.Merge(batch.Report);
        if (!report.HasCoreErrors && batch.Definitions.Length != referenced.Length)
            report.Error("Relic publication did not compile exactly one definition for every production item scene.");
        return new RelicGraphCompilationResult(batch, report);
    }

    private static AbilityStatusGraphCompilationResult CompileProductionAbilityStatusGraph(
        ContentCatalog catalog,
        IReadOnlyList<AbilityLoadoutDefinition?> additionalLoadoutReferences,
        IReadOnlyList<StatusDefinition?> additionalStatusReferences,
        ValidationReport report)
    {
        var loadouts = LoadResources<AbilityLoadoutDefinition>(
            DiscoverFiles("res://content/abilities/loadouts", ".tres", false, report),
            "ability loadout",
            report);
        var commandAbilities = LoadResources<AbilityDefinition>(
            DiscoverFiles("res://content/abilities/commands", ".tres", false, report),
            "command ability",
            report);
        var automaticAbilities = LoadResources<AbilityDefinition>(
            DiscoverFiles("res://content/abilities/automatic", ".tres", false, report),
            "automatic ability",
            report);
        var statuses = LoadResources<StatusDefinition>(
            DiscoverFiles("res://content/statuses", ".tres", false, report),
            "status definition",
            report);

        foreach (var ability in commandAbilities.OfType<AbilityDefinition>()
                     .Where(ability => ability.ActivationKind != AbilityActivationKind.ManualCommand))
            report.Error($"{ResourceLabel(ability)}: command ability must use the manual-command entry point.");
        foreach (var ability in automaticAbilities.OfType<AbilityDefinition>()
                     .Where(ability => ability.ActivationKind != AbilityActivationKind.Automatic))
            report.Error($"{ResourceLabel(ability)}: automatic ability must use the automatic entry point.");

        var contentLoadoutReferences = CollectContentAbilityLoadouts(catalog, report)
            .Concat(additionalLoadoutReferences)
            .ToArray();
        var graph = new AbilityStatusAuthoredGraph(
            loadouts,
            commandAbilities.Concat(automaticAbilities).ToArray(),
            statuses,
            contentLoadoutReferences,
            catalog.AllEntries().Select(entry => entry.StableId).ToHashSet(StringComparer.Ordinal))
        {
            AdditionalStatusReferences = additionalStatusReferences
        };
        var compilation = CompileAbilityStatusAuthoredGraph(graph);
        report.Merge(compilation.Report);
        return compilation;
    }

    internal static ValidationReport ValidateAbilityStatusAuthoredGraph(AbilityStatusAuthoredGraph graph)
        => CompileAbilityStatusAuthoredGraph(graph).Report;

    private static AbilityStatusGraphCompilationResult CompileAbilityStatusAuthoredGraph(AbilityStatusAuthoredGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var report = new ValidationReport();
        var loadouts = graph.Loadouts.Where(loadout => loadout is not null).Cast<AbilityLoadoutDefinition>().ToArray();
        var abilities = graph.Abilities.Where(ability => ability is not null).Cast<AbilityDefinition>().ToArray();
        var statuses = graph.Statuses.Where(status => status is not null).Cast<StatusDefinition>().ToArray();

        var statusBatch = StatusDefinitionCompiler.CompileBatch(graph.Statuses);
        report.Merge(statusBatch.Report);
        var statusesByResource = new Dictionary<StatusDefinition, CompiledStatusDefinition>(
            ReferenceEqualityComparer.Instance);
        foreach (var publication in statusBatch.Publications)
            statusesByResource.Add(publication.Authored, publication.Compiled);
        var statusesByPath = statusBatch.Publications
            .Where(publication => !string.IsNullOrWhiteSpace(publication.Authored.ResourcePath))
            .ToDictionary(
                publication => publication.Authored.ResourcePath,
                publication => publication.Compiled,
                StringComparer.Ordinal);
        CompiledStatusDefinition? ResolveStatus(StatusDefinition? authored)
        {
            if (authored is null) return null;
            if (statusesByResource.TryGetValue(authored, out var byResource)) return byResource;
            return !string.IsNullOrWhiteSpace(authored.ResourcePath) &&
                   statusesByPath.TryGetValue(authored.ResourcePath, out var byPath)
                ? byPath
                : null;
        }
        var abilityBatch = AbilityDefinitionCompiler.CompileBatch(graph.Loadouts, ResolveStatus);
        report.Merge(abilityBatch.Report);

        var authoredLoadouts = loadouts.ToHashSet<AbilityLoadoutDefinition>(ReferenceEqualityComparer.Instance);
        var referencedLoadouts = graph.ContentLoadoutReferences
            .Where(loadout => loadout is not null)
            .Cast<AbilityLoadoutDefinition>()
            .ToHashSet<AbilityLoadoutDefinition>(ReferenceEqualityComparer.Instance);
        foreach (var loadout in loadouts.Where(loadout => !referencedLoadouts.Contains(loadout)))
            report.Error($"Orphan ability loadout is not referenced by concrete content: {ResourceLabel(loadout)}");
        foreach (var loadout in referencedLoadouts.Where(loadout => !authoredLoadouts.Contains(loadout)))
            report.Error($"Concrete content references an unregistered ability loadout: {ResourceLabel(loadout)}");

        var authoredAbilities = abilities.ToHashSet<AbilityDefinition>(ReferenceEqualityComparer.Instance);
        var referencedAbilities = loadouts.SelectMany(loadout => loadout.Abilities)
            .OfType<AbilityDefinition>()
            .ToHashSet<AbilityDefinition>(ReferenceEqualityComparer.Instance);
        foreach (var ability in abilities.Where(ability => !referencedAbilities.Contains(ability)))
            report.Error($"Orphan ability definition is not referenced by a loadout: {ResourceLabel(ability)}");
        foreach (var ability in referencedAbilities.Where(ability => !authoredAbilities.Contains(ability)))
            report.Error($"Ability loadout references an unregistered definition: {ResourceLabel(ability)}");

        var authoredStatuses = statuses.ToHashSet<StatusDefinition>(ReferenceEqualityComparer.Instance);
        var authoredStatusesByPath = statuses
            .Where(status => !string.IsNullOrWhiteSpace(status.ResourcePath))
            .GroupBy(status => status.ResourcePath, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        StatusDefinition? ResolveAuthoredStatus(StatusDefinition? status)
        {
            if (status is null) return null;
            if (authoredStatuses.Contains(status)) return status;
            return !string.IsNullOrWhiteSpace(status.ResourcePath) &&
                   authoredStatusesByPath.TryGetValue(status.ResourcePath, out var byPath)
                ? byPath
                : null;
        }

        var referencedStatusRoots = abilities.SelectMany(ability => ability.Operations)
            .OfType<ApplyStatusAbilityOperationSpec>()
            .Select(operation => operation.Status)
            .OfType<StatusDefinition>()
            .Concat(graph.AdditionalStatusReferences.Where(status => status is not null)
                .Cast<StatusDefinition>())
            .ToHashSet<StatusDefinition>(ReferenceEqualityComparer.Instance);
        var reachableStatuses = new HashSet<StatusDefinition>(ReferenceEqualityComparer.Instance);
        void VisitStatus(StatusDefinition status)
        {
            var canonical = ResolveAuthoredStatus(status);
            if (canonical is null)
            {
                report.Error($"Status dependency is not registered: {ResourceLabel(status)}");
                return;
            }
            if (!reachableStatuses.Add(canonical)) return;
            if (canonical.OverflowStatus is not null) VisitStatus(canonical.OverflowStatus);
        }
        foreach (var status in referencedStatusRoots) VisitStatus(status);

        foreach (var status in statuses.Where(status => !reachableStatuses.Contains(status) &&
                                                        (string.IsNullOrWhiteSpace(status.ResourcePath) ||
                                                         !reachableStatuses.Any(reachable =>
                                                             reachable.ResourcePath == status.ResourcePath))))
            report.Error($"Orphan status definition is not referenced by an Ability or product binding: {ResourceLabel(status)}");
        foreach (var status in referencedStatusRoots.Where(status => ResolveAuthoredStatus(status) is null))
            report.Error($"Ability references an unregistered status definition: {ResourceLabel(status)}");

        foreach (var summon in abilities.SelectMany(ability => ability.Operations).OfType<SummonAbilityOperationSpec>())
            if (!string.IsNullOrWhiteSpace(summon.SummonContentId) && !graph.ValidContentIds.Contains(summon.SummonContentId))
                report.Error($"Summon ability references an unknown content id: {summon.SummonContentId}");

        var canonicalStatusCount = statuses.Select(status => string.IsNullOrWhiteSpace(status.ResourcePath)
                ? $"instance:{System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(status)}"
                : $"path:{status.ResourcePath}")
            .Distinct(StringComparer.Ordinal)
            .Count();
        if (!report.HasCoreErrors &&
            (abilityBatch.Abilities.Length != abilities.Length || statusBatch.Definitions.Length != canonicalStatusCount))
            report.Error("Ability/status publication did not compile the complete authored graph.");
        return new AbilityStatusGraphCompilationResult(abilityBatch, statusBatch, report);
    }

    private static IReadOnlyList<AbilityLoadoutDefinition?> CollectContentAbilityLoadouts(
        ContentCatalog catalog,
        ValidationReport report)
    {
        var result = new List<AbilityLoadoutDefinition?>();
        foreach (var entry in catalog.AllEntries())
        {
            if (entry.Scene is null) continue;
            Node? instance = null;
            try
            {
                instance = entry.Scene.Instantiate();
                if (instance is not UnitContentRoot unit) continue;
                if (unit.AbilityLoadout?.Loadout is not null)
                    result.Add(unit.AbilityLoadout.Loadout);
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

    private static IReadOnlyList<T?> LoadResources<T>(
        IEnumerable<string> paths,
        string label,
        ValidationReport report) where T : Resource
    {
        var resources = new List<T?>();
        foreach (var path in paths.OrderBy(path => path, StringComparer.Ordinal))
        {
            try
            {
                var resource = GD.Load<T>(path);
                if (resource is null) report.Error($"Failed to load {label}: {path}");
                resources.Add(resource);
            }
            catch (Exception exception)
            {
                report.Error($"Failed to load {label} {path}: {exception.Message}");
                resources.Add(null);
            }
        }
        return resources;
    }

    private static string ResourceLabel(Resource resource) =>
        string.IsNullOrWhiteSpace(resource.ResourcePath) ? $"<{resource.GetType().Name}>" : resource.ResourcePath;

    // Validates the authored entry graph without assuming production directory roots or sample totals.
    // Registry publication still uses Validate(), which adds bidirectional disk-completeness gates.
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

    internal static ContentGraphCompilationResult CompileAuthoredGraph(
        ContentCatalog? catalog,
        IReadOnlyList<AbilityLoadoutDefinition?> loadouts,
        IReadOnlyList<AbilityDefinition?> abilities,
        IReadOnlyList<StatusDefinition?> statuses,
        IReadOnlyList<RelicDefinition?> relics,
        IReadOnlyList<EquipmentDefinition?> equipment,
        IReadOnlyList<TraitDefinition?> traits,
        IReadOnlyList<TacticalCommandDefinition?> tacticalCommands,
        IReadOnlyList<PackedScene?> tacticalCommandScenes,
        IReadOnlyList<AbilityLoadoutDefinition?> additionalLoadoutReferences)
    {
        var report = ValidateAuthoredEntries(catalog);
        if (catalog is null) return new ContentGraphCompilationResult(null, report);

        ValidateFloorRules(catalog, report, requireProductionDirectories: false);
        var validContentIds = catalog.AllEntries()
            .Where(entry => entry is not null && !string.IsNullOrWhiteSpace(entry.StableId))
            .Select(entry => entry.StableId)
            .ToHashSet(StringComparer.Ordinal);

        var abilityStatus = CompileAbilityStatusAuthoredGraph(new AbilityStatusAuthoredGraph(
            loadouts,
            abilities,
            statuses,
            CollectContentAbilityLoadouts(catalog, report)
                .Concat(additionalLoadoutReferences)
                .Concat(tacticalCommands.Where(definition => definition is not null)
                    .Select(definition => definition!.AbilityLoadout))
                .ToArray(),
            validContentIds)
        {
            AdditionalStatusReferences = CollectEquipmentStatusReferences(equipment)
        });
        report.Merge(abilityStatus.Report);
        var relicCompilation = CompileRelicGraph(
            relics,
            CollectItemRelicReferences(catalog, report),
            validContentIds);
        report.Merge(relicCompilation.Report);
        var equipmentCompilation = CompileEquipmentGraph(
            equipment,
            CollectItemEquipmentReferences(catalog, report),
            abilityStatus.Statuses.Definitions);
        report.Merge(equipmentCompilation.Report);
        var traitCompilation = CompileTraitGraph(
            traits,
            catalog,
            equipmentCompilation.Equipment.Definitions);
        report.Merge(traitCompilation.Report);
        var tacticalCompilation = CompileTacticalCommandGraph(
            new TacticalCommandAuthoredGraph(tacticalCommands, tacticalCommandScenes),
            abilityStatus.Abilities,
            report,
            requireProductionDirectories: false);
        var graph = report.HasCoreErrors
            ? null
            : new CompiledContentGraph(
                abilityStatus.Abilities.Loadouts,
                abilityStatus.Abilities.Abilities,
                abilityStatus.Statuses.Definitions,
                relicCompilation.Relics.Definitions,
                equipmentCompilation.Equipment.Definitions,
                tacticalCompilation.Definitions,
                tacticalCommands.Where(definition => definition is not null)
                    .Cast<TacticalCommandDefinition>(),
                traitCompilation.Definitions,
                traitCompilation.UnitContributions);
        return new ContentGraphCompilationResult(graph, report);
    }

    private static TraitGraphCompilationResult CompileTraitGraph(
        IEnumerable<TraitDefinition?> authoredDefinitions,
        ContentCatalog catalog,
        IReadOnlyList<CompiledEquipmentDefinition> equipment)
    {
        ArgumentNullException.ThrowIfNull(authoredDefinitions);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(equipment);
        var report = new ValidationReport();
        var compilation = TraitDefinitionCompiler.CompileBatch(authoredDefinitions);
        report.Merge(compilation.Report);
        var validIds = compilation.Definitions.Select(definition => definition.StableId)
            .ToHashSet(StringComparer.Ordinal);
        var byUnit = new Dictionary<string, System.Collections.Immutable.ImmutableArray<CompiledTraitContribution>>(
            StringComparer.Ordinal);
        foreach (var entry in catalog.AllEntries().Where(entry => entry?.Definition is UnitDefinition))
        {
            var unit = (UnitDefinition)entry.Definition;
            var contributions = TraitDefinitionCompiler.CompileContributions(
                unit.TraitContributions ?? [],
                string.IsNullOrWhiteSpace(unit.ResourcePath) ? unit.Id : unit.ResourcePath);
            report.Merge(contributions.Report);
            ValidateTraitContributionDependencies(contributions.Contributions, validIds, unit.Id, report);
            if (!contributions.Contributions.IsEmpty)
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

    private static void ValidateTraitContributionDependencies(
        IEnumerable<CompiledTraitContribution> contributions,
        IReadOnlySet<string> validIds,
        string ownerLabel,
        ValidationReport report)
    {
        foreach (var contribution in contributions)
            if (!validIds.Contains(contribution.TraitId))
                report.Error($"{ownerLabel}: Trait contribution references missing Trait '{contribution.TraitId}'.");
    }

    private static TacticalCommandAuthoredGraph LoadProductionTacticalCommandGraph(
        ValidationReport report)
    {
        var definitions = LoadResources<TacticalCommandDefinition>(
            DiscoverFiles("res://content/tactical-commands/definitions", ".tres", false, report),
            "tactical-command definition",
            report);
        var scenePaths = DiscoverFiles(
            "res://content/tactical-commands/commands",
            ".tscn",
            false,
            report);
        var scenes = new List<PackedScene?>();
        foreach (var path in scenePaths.OrderBy(path => path, StringComparer.Ordinal))
        {
            try
            {
                var scene = GD.Load<PackedScene>(path);
                if (scene is null) report.Error($"Failed to load tactical-command scene: {path}");
                scenes.Add(scene);
            }
            catch (Exception exception)
            {
                report.Error($"Failed to load tactical-command scene {path}: {exception.Message}");
                scenes.Add(null);
            }
        }
        return new TacticalCommandAuthoredGraph(definitions, scenes);
    }

    private static TacticalCommandBatchCompilationResult CompileTacticalCommandGraph(
        TacticalCommandAuthoredGraph graph,
        AbilityBatchCompilationResult abilities,
        ValidationReport report,
        bool requireProductionDirectories)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var authored = graph.Definitions.ToArray();
        var referenced = new List<TacticalCommandDefinition?>();
        var scenePaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var scene in graph.Scenes)
        {
            if (scene is null)
            {
                report.Error("Tactical-command scene is missing.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(scene.ResourcePath) || !scenePaths.Add(scene.ResourcePath))
                report.Error($"Duplicate or pathless tactical-command scene: {scene.ResourcePath}");
            TacticalCommandContentRoot? root = null;
            try
            {
                root = scene.Instantiate<TacticalCommandContentRoot>();
                report.Merge(root.ValidateAuthoring());
                if (requireProductionDirectories && root.Definition is not null &&
                    !string.IsNullOrWhiteSpace(root.Definition.ResourcePath) &&
                    !root.Definition.ResourcePath.StartsWith(
                        "res://content/tactical-commands/definitions/",
                        StringComparison.Ordinal))
                    report.Error($"{scene.ResourcePath}: tactical-command definition is outside the production directory.");
                referenced.Add(root.Definition);
            }
            catch (Exception exception)
            {
                report.Error($"Tactical-command scene validation failed for {scene.ResourcePath}: {exception.Message}");
            }
            finally
            {
                if (GodotObject.IsInstanceValid(root)) root?.Free();
            }
        }

        var authoredByPath = authored.Where(definition => definition is not null)
            .Cast<TacticalCommandDefinition>()
            .GroupBy(ResourceLabel, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var referencedPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var definition in referenced)
        {
            if (definition is null)
            {
                report.Error("Tactical-command scene has no definition.");
                continue;
            }
            var label = ResourceLabel(definition);
            if (!referencedPaths.Add(label))
                report.Error($"Tactical-command definition is referenced by multiple scenes: {label}");
            if (!authoredByPath.ContainsKey(label))
                report.Error($"Tactical-command scene references an unregistered definition: {label}");
        }
        foreach (var definition in authored.Where(definition => definition is not null).Cast<TacticalCommandDefinition>())
            if (!referencedPaths.Contains(ResourceLabel(definition)))
                report.Error($"Orphan tactical-command definition is not referenced by a concrete scene: {ResourceLabel(definition)}");

        CompiledAbilityLoadout? ResolveLoadout(AbilityLoadoutDefinition? loadout)
        {
            if (loadout is null) return null;
            foreach (var publication in abilities.Loadouts)
                if (ReferenceEquals(publication.Authored, loadout) ||
                    !string.IsNullOrWhiteSpace(loadout.ResourcePath) &&
                    publication.Authored.ResourcePath == loadout.ResourcePath)
                    return publication.Compiled;
            return null;
        }

        var compilation = TacticalCommandDefinitionCompiler.CompileBatch(authored, ResolveLoadout);
        report.Merge(compilation.Report);
        if (!report.HasCoreErrors &&
            (compilation.Definitions.Length != authored.Length ||
             compilation.Definitions.Length != referenced.Count))
            report.Error("Tactical-command publication did not compile exactly one definition per concrete scene.");
        return report.HasCoreErrors
            ? new TacticalCommandBatchCompilationResult([], report)
            : compilation;
    }

    private static IReadOnlyList<(string ItemId, RelicDefinition? Definition)> CollectItemRelicReferences(
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

    private static IReadOnlyList<(string ItemId, EquipmentDefinition? Definition)> CollectItemEquipmentReferences(
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

    private static IReadOnlyList<StatusDefinition?> CollectEquipmentStatusReferences(
        IEnumerable<EquipmentDefinition?> equipment)
    {
        ArgumentNullException.ThrowIfNull(equipment);
        return equipment.Where(definition => definition is not null)
            .SelectMany(definition => definition!.ReactiveStatusBindings ?? [])
            .Where(binding => binding is not null)
            .Select(binding => binding.Status)
            .ToArray();
    }

    private static void ValidateFloorRules(
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
        var diskFloorScenes = DiscoverFiles("res://content/floor-rules", ".tscn", false, report);
        foreach (var scenePath in floorScenePaths)
            if (!diskFloorScenes.Contains(scenePath)) report.Error($"Catalog floor-rule scene does not exist: {scenePath}");
        foreach (var scenePath in diskFloorScenes)
            if (!floorScenePaths.Contains(scenePath)) report.Error($"Floor-rule scene is missing from catalog: {scenePath}");
    }

    public static async Task ValidateReadyFrameAsync(
        Node treeOwner, ContentCatalog catalog, ValidationReport report,
        IEnumerable<PackedScene>? additionalValidationScenes = null)
    {
        if (!treeOwner.IsInsideTree())
        {
            report.Error("Ready-frame validation requires a node inside the active scene tree.");
            return;
        }

        var host = new Node2D
        {
            Name = "ContentReadyValidationHost",
            Visible = false
        };
        var attached = false;
        try
        {
            try
            {
                foreach (var entry in catalog.AllEntries())
                    TryAddValidationInstance(host, entry.Scene, entry.Scene.ResourcePath, report);
                foreach (var scene in catalog.FloorRules)
                    TryAddValidationInstance(host, scene, scene?.ResourcePath ?? "<empty floor rule>", report);
                if (additionalValidationScenes is not null)
                    foreach (var scene in additionalValidationScenes)
                        TryAddValidationInstance(host, scene, scene?.ResourcePath ?? "<additional validation scene>", report);
            }
            catch (Exception exception)
            {
                report.Error($"Content ready-frame staging failed: {exception.Message}");
            }

            if (report.HasCoreErrors) return;
            try
            {
                treeOwner.AddChild(host);
                attached = true;
            }
            catch (Exception exception)
            {
                report.Error($"Content ready-frame attach failed: {exception.Message}");
            }

            if (!attached) return;
            try
            {
                await treeOwner.ToSignal(treeOwner.GetTree(), SceneTree.SignalName.ProcessFrame);
                // ProcessFrame is emitted before node _Process callbacks. The second signal proves
                // that every enabled validation node completed at least one actual process cycle.
                await treeOwner.ToSignal(treeOwner.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            catch (Exception exception)
            {
                report.Error($"Content ready-frame process failed: {exception.Message}");
            }
        }
        finally
        {
            try
            {
                if (attached && GodotObject.IsInstanceValid(treeOwner) && GodotObject.IsInstanceValid(host))
                    treeOwner.RemoveChild(host);
            }
            catch (Exception exception) { report.Error($"Content ready-frame detach failed: {exception.Message}"); }
            try
            {
                if (GodotObject.IsInstanceValid(host)) host.Free();
            }
            catch (Exception exception) { report.Error($"Content ready-frame free failed: {exception.Message}"); }
        }
    }

    private static void ValidateStructuralProbe(PackedScene? scene, ValidationReport report)
    {
        if (scene is null)
        {
            report.Error("Structural validation probe scene is empty.");
            return;
        }

        Node? instance = null;
        try { instance = scene.Instantiate(); }
        catch (Exception exception) { report.Error($"Structural validation probe instantiate failed: {exception.Message}"); }
        finally
        {
            try
            {
                if (GodotObject.IsInstanceValid(instance)) instance?.Free();
            }
            catch (Exception exception) { report.Error($"Structural validation probe free failed: {exception.Message}"); }
        }
    }

    private static void TryAddValidationInstance(Node host, PackedScene? scene, string label, ValidationReport report)
    {
        if (scene is null)
        {
            report.Error($"Ready-frame validation scene is empty: {label}");
            return;
        }
        Node? instance;
        try { instance = scene.Instantiate(); }
        catch (Exception exception)
        {
            report.Error($"Ready-frame instantiate failed for {label}: {exception.Message}");
            return;
        }

        try { host.AddChild(instance); }
        catch (Exception exception)
        {
            report.Error($"Ready-frame validation-host attach failed for {label}: {exception.Message}");
            try
            {
                if (GodotObject.IsInstanceValid(instance)) instance.Free();
            }
            catch (Exception freeException) { report.Error($"Ready-frame failed-instance free failed for {label}: {freeException.Message}"); }
        }
    }

    private static void ValidateGroup(
        Godot.Collections.Array<CatalogEntry> entries, ContentCategory category, ValidationReport report,
        HashSet<string> ids, HashSet<string> scenes, HashSet<string> definitions, HashSet<string> portraits,
        bool requireProductionDirectories)
    {
        foreach (var entry in entries)
            ValidateEntry(entry, category, report, ids, scenes, definitions, portraits, requireProductionDirectories);
    }

    private static void ValidateCatalogEntries(
        ContentCatalog catalog, ValidationReport report,
        HashSet<string> ids, HashSet<string> scenes, HashSet<string> definitions, HashSet<string> portraits,
        bool requireProductionDirectories)
    {
        ValidateGroup(catalog.Heroes, ContentCategory.Hero, report, ids, scenes, definitions, portraits, requireProductionDirectories);
        ValidateGroup(catalog.Soldiers, ContentCategory.Soldier, report, ids, scenes, definitions, portraits, requireProductionDirectories);
        ValidateGroup(catalog.Enemies, ContentCategory.Enemy, report, ids, scenes, definitions, portraits, requireProductionDirectories);
        ValidateGroup(catalog.Items, ContentCategory.Item, report, ids, scenes, definitions, portraits, requireProductionDirectories);
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
        if (!float.IsFinite(definition.BaseControlResistance) ||
            definition.BaseControlResistance is < 0 or > 1)
            report.Error($"{definition.Id}: BaseControlResistance must be finite and within [0,1].");
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

    private static HashSet<string> DiscoverConcreteScenes(ValidationReport report)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var directoryPath in ConcreteDirectories)
        {
            using var directory = DirAccess.Open(directoryPath);
            if (directory is null) { report.Error($"Missing concrete content directory: {directoryPath}"); continue; }
            directory.ListDirBegin();
            for (var name = directory.GetNext(); !string.IsNullOrEmpty(name); name = directory.GetNext())
            {
                if (!directory.CurrentIsDir() && name.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
                    result.Add($"{directoryPath}/{name}");
            }
            directory.ListDirEnd();
        }
        return result;
    }

    private static HashSet<string> DiscoverFiles(string rootPath, string extension, bool recursive, ValidationReport report)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        DiscoverInto(rootPath, extension, recursive, report, result);
        return result;
    }

    private static void DiscoverInto(string path, string extension, bool recursive, ValidationReport report, HashSet<string> result)
    {
        using var directory = DirAccess.Open(path);
        if (directory is null) { report.Error($"Missing content directory: {path}"); return; }
        directory.ListDirBegin();
        for (var name = directory.GetNext(); !string.IsNullOrEmpty(name); name = directory.GetNext())
        {
            if (directory.CurrentIsDir())
            {
                if (recursive && name is not "." and not "..") DiscoverInto($"{path}/{name}", extension, true, report, result);
            }
            else if (name.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) result.Add($"{path}/{name}");
        }
        directory.ListDirEnd();
    }

    private enum ContentCategory { Hero, Soldier, Enemy, Item }
}

internal sealed record AbilityStatusGraphCompilationResult(
    AbilityBatchCompilationResult Abilities,
    StatusBatchCompilationResult Statuses,
    ValidationReport Report);

internal sealed record RelicGraphCompilationResult(
    RelicBatchCompilationResult Relics,
    ValidationReport Report);

internal sealed record EquipmentGraphCompilationResult(
    EquipmentBatchCompilationResult Equipment,
    ValidationReport Report);

internal sealed record TraitGraphCompilationResult(
    System.Collections.Immutable.ImmutableArray<CompiledTraitDefinition> Definitions,
    IReadOnlyDictionary<string, System.Collections.Immutable.ImmutableArray<CompiledTraitContribution>> UnitContributions,
    ValidationReport Report);

internal sealed record TacticalCommandAuthoredGraph(
    IReadOnlyList<TacticalCommandDefinition?> Definitions,
    IReadOnlyList<PackedScene?> Scenes);

internal sealed record AbilityStatusAuthoredGraph(
    IReadOnlyList<AbilityLoadoutDefinition?> Loadouts,
    IReadOnlyList<AbilityDefinition?> Abilities,
    IReadOnlyList<StatusDefinition?> Statuses,
    IReadOnlyList<AbilityLoadoutDefinition?> ContentLoadoutReferences,
    IReadOnlySet<string> ValidContentIds)
{
    public IReadOnlyList<StatusDefinition?> AdditionalStatusReferences { get; init; } = [];
}

internal sealed partial class ContentReadyGateLogger : Logger
{
    private readonly object _lock = new();
    private readonly List<string> _errors = [];
    public IReadOnlyList<string> Errors
    {
        get { lock (_lock) return _errors.ToArray(); }
    }

    public override void _LogError(
        string function, string file, int line, string code, string rationale,
        bool editorNotify, int errorType, Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
    {
        if (errorType == (int)ErrorType.Warning) return;
        var message = string.IsNullOrWhiteSpace(rationale) ? code : rationale;
        lock (_lock) _errors.Add($"{message} ({file}:{line} {function})");
    }
}
