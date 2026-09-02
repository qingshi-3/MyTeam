using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.Project;

namespace TowerAutobattler.Composition;

public sealed record CompiledGamePackage(
    ContentRegistry Content,
    CompiledGameProject Project,
    int PublicationVersion);

public sealed record GamePackagePublicationResult(
    CompiledGamePackage? Package,
    ValidationReport Report,
    int PublishedVersion);

// Application composition boundary: Content and Project remain one-way layers,
// while their complete authored graphs become visible in one atomic publication.
public static class GamePackagePublisher
{
    public static Task<GamePackagePublicationResult> CreateReadyAsync(
        Node treeOwner,
        GameProjectDefinition? project,
        IEnumerable<PackedScene>? additionalValidationScenes = null,
        IEnumerable<PackedScene>? additionalStructuralValidationScenes = null) =>
        PublishAsync(
            treeOwner,
            project,
            null,
            additionalValidationScenes,
            additionalStructuralValidationScenes);

    internal static Task<GamePackagePublicationResult> CreateAuthoredReadyAsync(
        Node treeOwner,
        AuthoredContentPackage package,
        IEnumerable<PackedScene>? additionalValidationScenes = null) =>
        PublishAsync(treeOwner, package.Project, package, additionalValidationScenes, null);

    private static async Task<GamePackagePublicationResult> PublishAsync(
        Node treeOwner,
        GameProjectDefinition? project,
        AuthoredContentPackage? authoredPackage,
        IEnumerable<PackedScene>? additionalValidationScenes,
        IEnumerable<PackedScene>? additionalStructuralValidationScenes)
    {
        var gate = await ContentRegistry.PublishReadyAsync(
            treeOwner,
            () => Prepare(project, authoredPackage, additionalStructuralValidationScenes),
            additionalValidationScenes);
        var package = gate.Registry is not null && gate.Payload is not null
            ? new CompiledGamePackage(gate.Registry, gate.Payload, gate.PublishedVersion)
            : null;
        return new GamePackagePublicationResult(package, gate.Report, gate.PublishedVersion);
    }

    private static ContentPublicationPreparation<CompiledGameProject> Prepare(
        GameProjectDefinition? project,
        AuthoredContentPackage? authoredPackage,
        IEnumerable<PackedScene>? additionalStructuralValidationScenes)
    {
        var report = new ValidationReport();
        if (project is null)
        {
            report.Error("Game project definition is missing.");
            return new ContentPublicationPreparation<CompiledGameProject>(null, null, null, report);
        }

        var catalog = authoredPackage?.Catalog ?? project.Content;
        if (catalog is null)
            report.Error("Game package content catalog is missing.");
        if (authoredPackage is not null &&
            !ReferenceEquals(project.Content, catalog) &&
            project.Content?.ResourcePath != catalog?.ResourcePath)
            report.Error("Authored package catalog does not match GameProjectDefinition.Content.");

        var projectLoadouts = GameProjectCompiler.CollectAbilityLoadoutReferences(project);
        var content = authoredPackage is null
            ? ContentValidator.CompileProductionGraph(
                catalog,
                projectLoadouts,
                additionalStructuralValidationScenes)
            : ContentValidator.CompileAuthoredGraph(
                catalog,
                authoredPackage.Loadouts,
                authoredPackage.Abilities,
                authoredPackage.Statuses,
                authoredPackage.Relics,
                authoredPackage.Equipment,
                authoredPackage.Traits,
                authoredPackage.TacticalCommands,
                authoredPackage.TacticalCommandScenes,
                projectLoadouts);
        report.Merge(content.Report);

        GameProjectCompilationResult? compiledProject = null;
        if (!report.HasCoreErrors && content.Graph is not null)
        {
            compiledProject = GameProjectCompiler.Compile(project, content.Graph);
            report.Merge(compiledProject.Report);
        }

        return new ContentPublicationPreparation<CompiledGameProject>(
            catalog,
            report.HasCoreErrors ? null : content.Graph,
            report.HasCoreErrors ? null : compiledProject?.Project,
            report);
    }
}
