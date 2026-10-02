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

internal static class ContentAbilityStatusCompiler
{
    internal static AbilityStatusGraphCompilationResult CompileAbilityStatusAuthoredGraph(AbilityStatusAuthoredGraph graph)
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
            report.Error($"Orphan ability loadout is not referenced by concrete content: {ContentResourceDiscovery.ResourceLabel(loadout)}");
        foreach (var loadout in referencedLoadouts.Where(loadout => !authoredLoadouts.Contains(loadout)))
            report.Error($"Concrete content references an unregistered ability loadout: {ContentResourceDiscovery.ResourceLabel(loadout)}");

        var authoredAbilities = abilities.ToHashSet<AbilityDefinition>(ReferenceEqualityComparer.Instance);
        var referencedAbilities = loadouts.SelectMany(loadout => loadout.Abilities)
            .OfType<AbilityDefinition>()
            .ToHashSet<AbilityDefinition>(ReferenceEqualityComparer.Instance);
        foreach (var ability in abilities.Where(ability => !referencedAbilities.Contains(ability)))
            report.Error($"Orphan ability definition is not referenced by a loadout: {ContentResourceDiscovery.ResourceLabel(ability)}");
        foreach (var ability in referencedAbilities.Where(ability => !authoredAbilities.Contains(ability)))
            report.Error($"Ability loadout references an unregistered definition: {ContentResourceDiscovery.ResourceLabel(ability)}");

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
            .Select(operation => operation switch
            {
                ApplyStatusAbilityOperationSpec status => status.Status,
                DisplacementAbilityOperationSpec displacement => displacement.ImpactStatus,
                _ => null
            })
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
                report.Error($"Status dependency is not registered: {ContentResourceDiscovery.ResourceLabel(status)}");
                return;
            }
            if (!reachableStatuses.Add(canonical)) return;
            if (canonical.OverflowStatus is not null) VisitStatus(canonical.OverflowStatus);
            foreach (var reaction in canonical.Reactions ?? [])
                if (reaction?.Status is not null) VisitStatus(reaction.Status);
        }
        foreach (var status in referencedStatusRoots) VisitStatus(status);

        // String-based reads and consumption are dependencies too, even if the supplier
        // belongs to another hero or an item rather than this ability's own operations.
        var statusesById = statuses.GroupBy(status => status.StableId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        foreach (var ability in abilities)
        foreach (var operation in ability.Operations)
        {
            var referencedIds = operation switch
            {
                ScaledStatusAbilityOperationSpec scaled => new[] { scaled.StatusId, scaled.ChanceStatusId }
                    .Where(id => !string.IsNullOrWhiteSpace(id)),
                ConsumeStatusAbilityOperationSpec consume => new[] { consume.StatusId },
                BattleValueAbilityOperationSpec value => value.Terms.OfType<BattleValueTermSpec>()
                    .Where(term => term.Metric == BattleValueMetric.StatusStacks).Select(term => term.Key),
                _ => Enumerable.Empty<string>()
            };
            foreach (var statusId in referencedIds)
                if (statusesById.TryGetValue(statusId, out var referencedStatus)) VisitStatus(referencedStatus);
                else report.Error($"{ContentResourceDiscovery.ResourceLabel(ability)}: unknown status reference '{statusId}'.");
        }

        foreach (var status in statuses.Where(status => !reachableStatuses.Contains(status) &&
                                                        (string.IsNullOrWhiteSpace(status.ResourcePath) ||
                                                         !reachableStatuses.Any(reachable =>
                                                             reachable.ResourcePath == status.ResourcePath))))
            report.Error($"Orphan status definition is not referenced by an Ability or product binding: {ContentResourceDiscovery.ResourceLabel(status)}");
        foreach (var status in referencedStatusRoots.Where(status => ResolveAuthoredStatus(status) is null))
            report.Error($"Ability references an unregistered status definition: {ContentResourceDiscovery.ResourceLabel(status)}");

        foreach (var ability in abilities)
        foreach (var operation in ability.Operations)
        {
            foreach (var dependency in AbilityDefinitionCompiler.EnemyActionDependencies(operation))
                if (!(graph.ValidUnitContentIds ?? graph.ValidContentIds).Contains(dependency)) report.Error($"{ContentResourceDiscovery.ResourceLabel(ability)}: unavailable enemy action product: {dependency}");
            if (operation is MatrixAbilityOperationSpec matrix)
                foreach (var dependency in MatrixAbilityCompiler.Flatten(matrix)
                             .Where(node => node.Kind == MatrixOperationKind.Summon).Select(node => node.ContentId))
                    if (!(graph.ValidUnitContentIds ?? graph.ValidContentIds).Contains(dependency))
                        report.Error($"{ContentResourceDiscovery.ResourceLabel(ability)}: unavailable matrix summon product: {dependency}");
            var contentId = operation switch
            {
                SummonAbilityOperationSpec summon => summon.SummonContentId,
                LifecycleAbilityOperationSpec { Kind: LifecycleAbilityKind.RaiseCorpse } corpse => corpse.SummonContentId,
                _ => string.Empty
            };
            if (!string.IsNullOrWhiteSpace(contentId) &&
                !(graph.ValidUnitContentIds ?? graph.ValidContentIds).Contains(contentId))
                report.Error($"{ContentResourceDiscovery.ResourceLabel(ability)}: summon references an unavailable unit content id: {contentId}");
        }
        ContentPrimitiveValidator.ValidateAbilityStatusReferences(abilityBatch.Abilities,
            statusBatch.Definitions.ToDictionary(status => status.StableId, StringComparer.Ordinal), report);

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

}
