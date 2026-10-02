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

internal static class ContentPublicationPipeline
{
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
                ContentSceneValidation.ValidateStructuralProbe(scene, report);

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var scenes = new HashSet<string>(StringComparer.Ordinal);
        var definitions = new HashSet<string>(StringComparer.Ordinal);
        var portraits = new HashSet<string>(StringComparer.Ordinal);
        ContentCatalogValidation.ValidateCatalogEntries(catalog, report, ids, scenes, definitions, portraits, requireProductionDirectories: true);

        var diskScenes = ContentResourceDiscovery.DiscoverConcreteScenes(report);
        foreach (var scenePath in scenes)
            if (!diskScenes.Contains(scenePath)) report.Error($"Catalog scene does not exist in concrete directories: {scenePath}");
        foreach (var scenePath in diskScenes)
            if (!scenes.Contains(scenePath)) report.Error($"Concrete scene is missing from catalog: {scenePath}");

        var diskDefinitions = ContentResourceDiscovery.DiscoverFiles("res://content/definitions", ".tres", true, report);
        foreach (var definitionPath in definitions)
            if (!diskDefinitions.Contains(definitionPath)) report.Error($"Catalog definition does not exist: {definitionPath}");
        foreach (var definitionPath in diskDefinitions)
            if (!definitions.Contains(definitionPath)) report.Error($"Definition is missing from catalog: {definitionPath}");

        var diskPortraits = ContentResourceDiscovery.DiscoverFiles("res://content/portraits", ".tres", true, report);
        foreach (var portraitPath in portraits)
            if (!diskPortraits.Contains(portraitPath)) report.Error($"Catalog portrait does not exist: {portraitPath}");
        foreach (var portraitPath in diskPortraits)
            if (!portraits.Contains(portraitPath)) report.Error($"Portrait resource is missing from the production unit catalog: {portraitPath}");

        ContentCatalogValidation.ValidateFloorRules(catalog, report, requireProductionDirectories: true);

        var tacticalAuthored = ContentResourceDiscovery.LoadProductionTacticalCommandGraph(report);
        var traitAuthoring = ContentResourceDiscovery.LoadResources<TraitDefinition>(
            ContentResourceDiscovery.DiscoverFiles("res://content/traits/definitions", ".tres", false, report), "Trait definition", report);
        var equipmentStatusReferences = ContentReferenceCollector.CollectEquipmentStatusReferences(
            ContentReferenceCollector.CollectItemEquipmentReferences(catalog, report).Select(reference => reference.Definition))
            .Concat(ContentReferenceCollector.CollectCarrierStatusReferences(
                traitAuthoring,
                ContentReferenceCollector.CollectItemRelicReferences(catalog, report).Select(reference => reference.Definition))).ToArray();
        var abilityStatus = CompileProductionAbilityStatusGraph(
            catalog,
            additionalLoadoutReferences.Concat(tacticalAuthored.Definitions
                .Where(definition => definition is not null)
                .Select(definition => definition!.AbilityLoadout)).ToArray(),
            equipmentStatusReferences,
            report);
        var relics = CompileProductionRelicGraph(catalog, abilityStatus.Statuses.Definitions, report);
        var equipment = CompileProductionEquipmentGraph(catalog, abilityStatus.Statuses.Definitions, report);
        var traits = ContentTraitCompiler.CompileTraitGraph(traitAuthoring, catalog, equipment.Equipment.Definitions, abilityStatus.Statuses.Definitions);
        report.Merge(traits.Report);
        var tacticalCommands = ContentTacticalCompiler.CompileTacticalCommandGraph(
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
                traits.UnitContributions, traitAuthoring.OfType<Resource>());
        if (graph is not null) ContentPrimitiveValidator.Validate(graph, report);
        return new ContentGraphCompilationResult(report.HasCoreErrors ? null : graph, report);
    }

    internal static EquipmentGraphCompilationResult CompileProductionEquipmentGraph(
        ContentCatalog catalog,
        IReadOnlyList<CompiledStatusDefinition> statuses,
        ValidationReport report)
    {
        var authored = ContentResourceDiscovery.LoadResources<EquipmentDefinition>(
            ContentResourceDiscovery.DiscoverFiles("res://content/equipment/definitions", ".tres", false, report),
            "Equipment definition",
            report);
        var referenced = ContentReferenceCollector.CollectItemEquipmentReferences(catalog, report);
        foreach (var (itemId, definition) in referenced)
            if (definition is not null &&
                (string.IsNullOrWhiteSpace(definition.ResourcePath) ||
                 !definition.ResourcePath.StartsWith("res://content/equipment/definitions/", StringComparison.Ordinal)))
                report.Error($"{itemId}: Equipment definition is outside the production Equipment directory.");

        var compilation = ContentItemCompiler.CompileEquipmentGraph(authored, referenced, statuses);
        report.Merge(compilation.Report);
        return compilation;
    }

    internal static RelicGraphCompilationResult CompileProductionRelicGraph(
        ContentCatalog catalog,
        IReadOnlyList<CompiledStatusDefinition> statuses,
        ValidationReport report)
    {
        var authored = ContentResourceDiscovery.LoadResources<RelicDefinition>(
            ContentResourceDiscovery.DiscoverFiles("res://content/relics/definitions", ".tres", false, report),
            "relic definition",
            report);
        var referenced = ContentReferenceCollector.CollectItemRelicReferences(catalog, report);
        foreach (var (itemId, definition) in referenced)
            if (definition is not null &&
                (string.IsNullOrWhiteSpace(definition.ResourcePath) ||
                 !definition.ResourcePath.StartsWith("res://content/relics/definitions/", StringComparison.Ordinal)))
                report.Error($"{itemId}: relic definition is outside the production relic directory.");

        var compilation = ContentItemCompiler.CompileRelicGraph(
            authored,
            referenced,
            catalog.AllEntries().Select(entry => entry.StableId).ToHashSet(StringComparer.Ordinal), statuses);
        report.Merge(compilation.Report);
        ContentItemCompiler.ValidateRelicRewardPools(compilation.Relics.Definitions, catalog, report);
        return compilation;
    }

    internal static AbilityStatusGraphCompilationResult CompileProductionAbilityStatusGraph(
        ContentCatalog catalog,
        IReadOnlyList<AbilityLoadoutDefinition?> additionalLoadoutReferences,
        IReadOnlyList<StatusDefinition?> additionalStatusReferences,
        ValidationReport report)
    {
        var loadouts = ContentResourceDiscovery.LoadResources<AbilityLoadoutDefinition>(
            ContentResourceDiscovery.DiscoverFiles("res://content/abilities/loadouts", ".tres", false, report),
            "ability loadout",
            report);
        var commandAbilities = ContentResourceDiscovery.LoadResources<AbilityDefinition>(
            ContentResourceDiscovery.DiscoverFiles("res://content/abilities/commands", ".tres", false, report),
            "command ability",
            report);
        var automaticAbilities = ContentResourceDiscovery.LoadResources<AbilityDefinition>(
            ContentResourceDiscovery.DiscoverFiles("res://content/abilities/automatic", ".tres", false, report),
            "automatic ability",
            report);
        var triggeredAbilities = ContentResourceDiscovery.LoadOptionalAbilityDirectory("triggered", AbilityActivationKind.Triggered, report);
        var passiveAbilities = ContentResourceDiscovery.LoadOptionalAbilityDirectory("passive", AbilityActivationKind.Passive, report);
        var statuses = ContentResourceDiscovery.LoadResources<StatusDefinition>(
            ContentResourceDiscovery.DiscoverFiles("res://content/statuses", ".tres", false, report),
            "status definition",
            report);

        foreach (var ability in commandAbilities.OfType<AbilityDefinition>()
                     .Where(ability => ability.ActivationKind != AbilityActivationKind.ManualCommand))
            report.Error($"{ContentResourceDiscovery.ResourceLabel(ability)}: command ability must use the manual-command entry point.");
        foreach (var ability in automaticAbilities.OfType<AbilityDefinition>()
                     .Where(ability => ability.ActivationKind != AbilityActivationKind.Automatic))
            report.Error($"{ContentResourceDiscovery.ResourceLabel(ability)}: automatic ability must use the automatic entry point.");

        var activeLoadoutReferences = ContentReferenceCollector.CollectContentAbilityLoadouts(catalog, report)
            .Concat(additionalLoadoutReferences)
            .ToArray();
        ContentReferenceCollector.ValidateActiveUnitReferences(catalog, activeLoadoutReferences, report);
        var contentLoadoutReferences = activeLoadoutReferences
            .Concat(ContentReferenceCollector.CollectRetainedContentAbilityLoadouts(catalog, report)).ToArray();
        var graph = new AbilityStatusAuthoredGraph(
            loadouts,
            commandAbilities.Concat(automaticAbilities).Concat(triggeredAbilities).Concat(passiveAbilities).ToArray(),
            statuses,
            contentLoadoutReferences,
            ContentReferenceCollector.CatalogEntriesForValidation(catalog).Where(entry => entry is not null)
                .Select(entry => entry.StableId).ToHashSet(StringComparer.Ordinal))
        {
            AdditionalStatusReferences = additionalStatusReferences,
            ValidUnitContentIds = ContentReferenceCollector.CatalogEntriesForValidation(catalog).Where(entry => entry?.Definition is UnitDefinition)
                .Select(entry => entry.StableId).ToHashSet(StringComparer.Ordinal)
        };
        var compilation = ContentAbilityStatusCompiler.CompileAbilityStatusAuthoredGraph(graph);
        report.Merge(compilation.Report);
        return compilation;
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
        var report = ContentCatalogValidation.ValidateAuthoredEntries(catalog);
        if (catalog is null) return new ContentGraphCompilationResult(null, report);

        ContentCatalogValidation.ValidateFloorRules(catalog, report, requireProductionDirectories: false);
        var validContentIds = catalog.AllEntries()
            .Where(entry => entry is not null && !string.IsNullOrWhiteSpace(entry.StableId))
            .Select(entry => entry.StableId)
            .ToHashSet(StringComparer.Ordinal);

        var activeLoadoutReferences = ContentReferenceCollector.CollectContentAbilityLoadouts(catalog, report)
            .Concat(additionalLoadoutReferences)
            .Concat(tacticalCommands.Where(definition => definition is not null)
                .Select(definition => definition!.AbilityLoadout)).ToArray();
        ContentReferenceCollector.ValidateActiveUnitReferences(catalog, activeLoadoutReferences, report);

        var abilityStatus = ContentAbilityStatusCompiler.CompileAbilityStatusAuthoredGraph(new AbilityStatusAuthoredGraph(
            loadouts,
            abilities,
            statuses,
            activeLoadoutReferences.Concat(ContentReferenceCollector.CollectRetainedContentAbilityLoadouts(catalog, report)).ToArray(),
            ContentReferenceCollector.CatalogEntriesForValidation(catalog).Where(entry => entry is not null)
                .Select(entry => entry.StableId).ToHashSet(StringComparer.Ordinal))
        {
            AdditionalStatusReferences = ContentReferenceCollector.CollectEquipmentStatusReferences(equipment).Concat(ContentReferenceCollector.CollectCarrierStatusReferences(traits, relics)).ToArray(),
            ValidUnitContentIds = ContentReferenceCollector.CatalogEntriesForValidation(catalog).Where(entry => entry?.Definition is UnitDefinition)
                .Select(entry => entry.StableId).ToHashSet(StringComparer.Ordinal)
        });
        report.Merge(abilityStatus.Report);
        var relicCompilation = ContentItemCompiler.CompileRelicGraph(
            relics,
            ContentReferenceCollector.CollectItemRelicReferences(catalog, report),
            validContentIds, abilityStatus.Statuses.Definitions);
        report.Merge(relicCompilation.Report);
        ContentItemCompiler.ValidateRelicRewardPools(relicCompilation.Relics.Definitions, catalog, report);
        var equipmentCompilation = ContentItemCompiler.CompileEquipmentGraph(
            equipment,
            ContentReferenceCollector.CollectItemEquipmentReferences(catalog, report),
            abilityStatus.Statuses.Definitions);
        report.Merge(equipmentCompilation.Report);
        var traitCompilation = ContentTraitCompiler.CompileTraitGraph(
            traits,
            catalog,
            equipmentCompilation.Equipment.Definitions, abilityStatus.Statuses.Definitions);
        report.Merge(traitCompilation.Report);
        var tacticalCompilation = ContentTacticalCompiler.CompileTacticalCommandGraph(
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
                traitCompilation.UnitContributions, traits.OfType<Resource>());
        if (graph is not null) ContentPrimitiveValidator.Validate(graph, report);
        return new ContentGraphCompilationResult(report.HasCoreErrors ? null : graph, report);
    }

}
