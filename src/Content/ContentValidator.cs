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

// Compatibility entry points. Stage owners enforce the complete publication contract.
public static class ContentValidator
{
    public static ValidationReport Validate(
        ContentCatalog? catalog,
        IEnumerable<PackedScene>? additionalStructuralValidationScenes = null) =>
        ContentPublicationPipeline.CompileProductionGraph(catalog, [], additionalStructuralValidationScenes).Report;

    internal static ValidationReport ValidateRelicGraph(
        IEnumerable<RelicDefinition?> authoredDefinitions,
        IEnumerable<(string ItemId, RelicDefinition? Definition)> itemReferences,
        IReadOnlySet<string> validContentIds) =>
        ContentItemCompiler.CompileRelicGraph(authoredDefinitions, itemReferences, validContentIds).Report;

    internal static ValidationReport ValidateEquipmentGraph(
        IEnumerable<EquipmentDefinition?> authoredDefinitions,
        IEnumerable<(string ItemId, EquipmentDefinition? Definition)> itemReferences,
        IEnumerable<CompiledStatusDefinition>? statuses = null) =>
        ContentItemCompiler.CompileEquipmentGraph(authoredDefinitions, itemReferences, statuses).Report;

    internal static ValidationReport ValidateAbilityStatusAuthoredGraph(AbilityStatusAuthoredGraph graph)
        => ContentAbilityStatusCompiler.CompileAbilityStatusAuthoredGraph(graph).Report;

    internal static ContentGraphCompilationResult CompileProductionGraph(
        ContentCatalog? catalog,
        IReadOnlyList<AbilityLoadoutDefinition?> additionalLoadoutReferences,
        IEnumerable<PackedScene>? additionalStructuralValidationScenes = null) =>
        ContentPublicationPipeline.CompileProductionGraph(catalog, additionalLoadoutReferences, additionalStructuralValidationScenes);

    internal static ValidationReport ValidateAuthoredEntries(ContentCatalog? catalog) =>
        ContentCatalogValidation.ValidateAuthoredEntries(catalog);

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
        IReadOnlyList<AbilityLoadoutDefinition?> additionalLoadoutReferences) =>
        ContentPublicationPipeline.CompileAuthoredGraph(catalog, loadouts, abilities, statuses, relics, equipment, traits, tacticalCommands, tacticalCommandScenes, additionalLoadoutReferences);

    public static Task ValidateReadyFrameAsync(
        Node treeOwner, ContentCatalog catalog, ValidationReport report,
        IEnumerable<PackedScene>? additionalValidationScenes = null) =>
        ContentSceneValidation.ValidateReadyFrameAsync(treeOwner, catalog, report, additionalValidationScenes);

    internal static void ValidateStructuralProbe(PackedScene? scene, ValidationReport report) =>
        ContentSceneValidation.ValidateStructuralProbe(scene, report);

}
