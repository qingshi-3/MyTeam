using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Content;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;

namespace TowerAutobattler.Composition;

public sealed record GamePackagePublicationResult(
    CompiledGamePackage? Package,
    ValidationReport Report,
    int PublishedVersion);

// All profiles share one structural/Ready gate. No registry or partial profile set
// escapes when even a non-default compatibility profile fails compilation.
public static class GamePackagePublisher
{
    public static Task<GamePackagePublicationResult> CreateReadyAsync(
        Node treeOwner,
        GameProjectDefinition? project,
        IEnumerable<PackedScene>? additionalValidationScenes = null,
        IEnumerable<PackedScene>? additionalStructuralValidationScenes = null) =>
        PublishAsync(treeOwner, project, null, additionalValidationScenes, additionalStructuralValidationScenes);

    internal static Task<GamePackagePublicationResult> CreateAuthoredReadyAsync(
        Node treeOwner,
        AuthoredContentPackage package,
        IEnumerable<PackedScene>? additionalValidationScenes = null,
        IEnumerable<PackedScene>? additionalStructuralValidationScenes = null) =>
        PublishAsync(treeOwner, package.Project, package, additionalValidationScenes, additionalStructuralValidationScenes);

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
            ? new CompiledGamePackage(gate.Registry, gate.Payload.ProfilesById,
                gate.Payload.DefaultNewRunProfileId, gate.PublishedVersion)
                { AuthoringOwner = (object?)authoredPackage ?? project }
            : null;
        return new GamePackagePublicationResult(package, gate.Report, gate.PublishedVersion);
    }

    private sealed record PreparedRules(
        ImmutableDictionary<string, CompiledRunProfile> ProfilesById,
        string DefaultNewRunProfileId);

    private static ContentPublicationPreparation<PreparedRules> Prepare(
        GameProjectDefinition? project,
        AuthoredContentPackage? authoredPackage,
        IEnumerable<PackedScene>? additionalStructuralValidationScenes)
    {
        var report = new ValidationReport();
        if (project is null)
        {
            report.Error("Game project definition is missing.");
            return new ContentPublicationPreparation<PreparedRules>(null, null, null, report);
        }

        IReadOnlyList<AuthoredRunProfile> profiles = authoredPackage is { Profiles.Count: > 0 }
            ? authoredPackage.Profiles
            : [new(RunProfileIds.SingleProject(project.StableId), project, authoredPackage?.GrowthRules)];
        var requestedDefault = authoredPackage?.DefaultNewRunProfileId;
        var defaultId = string.IsNullOrEmpty(requestedDefault) ? profiles[0].ProfileId : requestedDefault;
        var catalog = authoredPackage?.Catalog ?? project.Content;
        if (catalog is null) report.Error("Game package content catalog is missing.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var profile in profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.ProfileId) ||
                profile.ProfileId.Any(character => character is not (>= 'a' and <= 'z') and not (>= '0' and <= '9') and not '_') ||
                !ids.Add(profile.ProfileId))
                report.Error($"Invalid or duplicate run profile id: '{profile.ProfileId}'.");
            if (profile.CompatibilityVersion < 1 || profile.ProgressionProtocolVersion < 1 ||
                profile.RewardProtocolVersion < 1 || profile.IdentityProtocolVersion < 1)
                report.Error($"Run profile '{profile.ProfileId}' has an invalid protocol version.");
            if (!ReferenceEquals(profile.Project.Content, catalog))
                report.Error($"Run profile '{profile.ProfileId}' does not reference the shared package catalog.");
        }
        if (!ids.Contains(defaultId)) report.Error($"Default run profile '{defaultId}' is not registered.");
        else if (profiles.First(profile => profile.ProfileId == defaultId) is { } defaultProfile &&
                 (!ReferenceEquals(defaultProfile.Project, project) ||
                  !ReferenceEquals(defaultProfile.GrowthRules, authoredPackage?.GrowthRules)))
            report.Error("Authored package compatibility roots must reference its default profile.");
        if (report.HasCoreErrors)
            return new ContentPublicationPreparation<PreparedRules>(catalog, null, null, report);

        var allLoadoutReferences = profiles.SelectMany(profile =>
                GameProjectCompiler.CollectAbilityLoadoutReferences(profile.Project)
                    .Concat(GrowthLoadoutReferences(profile.GrowthRules)))
            .Concat(authoredPackage?.AdditionalLoadoutReferences ?? [])
            .ToArray();
        if (authoredPackage is not null && additionalStructuralValidationScenes is not null)
            foreach (var scene in additionalStructuralValidationScenes)
                ContentValidator.ValidateStructuralProbe(scene, report);
        var content = authoredPackage is null
            ? ContentValidator.CompileProductionGraph(catalog, allLoadoutReferences, additionalStructuralValidationScenes)
            : ContentValidator.CompileAuthoredGraph(
                catalog, authoredPackage.Loadouts, authoredPackage.Abilities, authoredPackage.Statuses,
                authoredPackage.Relics, authoredPackage.Equipment, authoredPackage.Traits,
                authoredPackage.TacticalCommands, authoredPackage.TacticalCommandScenes, allLoadoutReferences);
        report.Merge(content.Report);

        var compiled = ImmutableDictionary.CreateBuilder<string, CompiledRunProfile>(StringComparer.Ordinal);
        if (!report.HasCoreErrors && content.Graph is { } graph)
        {
            foreach (var profile in profiles)
            {
                var compiledProject = GameProjectCompiler.Compile(profile.Project, graph);
                report.Merge(compiledProject.Report);
                if (compiledProject.Project is not { } readyProject) continue;
                try
                {
                    var growth = profile.GrowthRules is { } rules
                        ? GrowthRulesCompiler.Compile(rules, catalog!, graph) : null;
                    var fingerprint = RunProfileContentFingerprint.Compute(profile.ProfileId,
                        profile.CompatibilityVersion, profile.ProgressionProtocolVersion,
                        profile.RewardProtocolVersion, profile.IdentityProtocolVersion, readyProject, growth, graph);
                    compiled.Add(profile.ProfileId, new CompiledRunProfile(profile.ProfileId,
                        profile.CompatibilityVersion, fingerprint, readyProject, growth,
                        profile.ProgressionProtocolVersion, profile.RewardProtocolVersion, profile.IdentityProtocolVersion));
                }
                catch (Exception exception)
                {
                    report.Error($"Run profile '{profile.ProfileId}' prepublication failed: {exception.Message}");
                }
            }
        }

        return new ContentPublicationPreparation<PreparedRules>(catalog,
            report.HasCoreErrors ? null : content.Graph,
            report.HasCoreErrors || compiled.Count != profiles.Count ? null : new PreparedRules(compiled.ToImmutable(), defaultId),
            report);
    }

    private static IEnumerable<AbilityLoadoutDefinition?> GrowthLoadoutReferences(GrowthRulesDefinition? rules) =>
        rules is null ? [] : rules.Heroes.SelectMany(hero => new[] { hero?.BaseLoadout, hero?.AscensionBenefit?.BattleLoadout })
            .Concat(rules.Spells.Select(spell => spell?.BattleLoadout)).Where(loadout => loadout is not null);
}
