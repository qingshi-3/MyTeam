using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

namespace TowerAutobattler.Traits;

public static class TraitSnapshotBuilder
{
    public static TraitSnapshot Build(
        IEnumerable<CompiledTraitDefinition> definitions,
        IEnumerable<TraitContributionInput> inputs)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(inputs);
        var orderedDefinitions = definitions.OrderBy(definition => definition.StableId, StringComparer.Ordinal).ToArray();
        if (orderedDefinitions.Select(definition => definition.StableId).Distinct(StringComparer.Ordinal).Count() !=
            orderedDefinitions.Length)
            throw new ArgumentException("Trait snapshot definitions contain duplicate stable ids.", nameof(definitions));
        var byId = orderedDefinitions.ToDictionary(definition => definition.StableId, StringComparer.Ordinal);
        var orderedInputs = inputs.Select(ValidateInput)
            .OrderBy(input => input.Team)
            .ThenBy(input => input.TraitId, StringComparer.Ordinal)
            .ThenBy(input => input.SourceKind)
            .ThenBy(input => input.ContentIdentity, StringComparer.Ordinal)
            .ThenBy(input => input.SourceInstanceId, StringComparer.Ordinal)
            .ThenBy(input => input.OwnerRuntimeId, StringComparer.Ordinal)
            .ToArray();
        foreach (var input in orderedInputs)
            if (!byId.ContainsKey(input.TraitId))
                throw new ArgumentException($"Trait contribution references missing Trait '{input.TraitId}'.", nameof(inputs));

        var counted = ImmutableArray.CreateBuilder<TraitContributionSnapshot>();
        var values = ImmutableArray.CreateBuilder<TraitValueSnapshot>();
        foreach (var definition in orderedDefinitions)
        for (var team = 0; team <= 1; team++)
        {
            var eligible = orderedInputs.Where(input => input.Team == team && input.TraitId == definition.StableId)
                .Where(input => Eligible(definition.CountingPolicy, input))
                .ToArray();
            if (definition.CountingPolicy.DuplicateContentPolicy == TraitDuplicateContentPolicy.UniqueContent)
                eligible = eligible.GroupBy(
                        input => (input.SourceKind, input.ContentIdentity),
                        EqualityComparer<(TraitContributionSourceKind, string)>.Default)
                    .Select(group => group.First())
                    .ToArray();
            var snapshots = eligible.Select(ToSnapshot).ToImmutableArray();
            counted.AddRange(snapshots);
            var value = snapshots.Sum(contribution => contribution.Value);
            var breakpoint = definition.Breakpoints.SingleOrDefault(candidate =>
                value >= candidate.MinValue && value <= candidate.MaxValue);
            var text = breakpoint is null
                ? $"{definition.DisplayName} {value}"
                : $"{definition.DisplayName} {value}（{breakpoint.MinValue}–{breakpoint.MaxValue}）";
            var presentation = new TraitPresentationSnapshot(
                definition.StableId,
                definition.DisplayName,
                definition.SemanticIconKey,
                team,
                value,
                breakpoint?.MinValue,
                breakpoint?.MaxValue,
                breakpoint?.DisplayStyle ?? "TraitInactive",
                text);
            values.Add(new TraitValueSnapshot(
                definition.StableId,
                team,
                value,
                breakpoint,
                snapshots,
                presentation));
        }
        var immutableValues = values.ToImmutable();
        var immutableContributions = counted.ToImmutable();
        return new TraitSnapshot(Fingerprint(immutableValues, immutableContributions), immutableValues, immutableContributions);
    }

    private static TraitContributionInput ValidateInput(TraitContributionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.TraitId) || input.Value <= 0 || input.Team is < 0 or > 1 ||
            !Enum.IsDefined(input.SourceKind) || string.IsNullOrWhiteSpace(input.SourceInstanceId) ||
            string.IsNullOrWhiteSpace(input.ContentIdentity))
            throw new ArgumentException("Trait contribution input is invalid.", nameof(input));
        if (input.IsPersistent && input.IsTemporary)
            throw new ArgumentException("Trait contribution cannot be both persistent and temporary.", nameof(input));
        if (input.SourceKind == TraitContributionSourceKind.Equipment &&
            string.IsNullOrWhiteSpace(input.OwnerRuntimeId))
            throw new ArgumentException("Equipment Trait contribution requires an owner.", nameof(input));
        return input;
    }

    private static bool Eligible(CompiledTraitCountingPolicy policy, TraitContributionInput input)
    {
        if (input.SourceKind == TraitContributionSourceKind.Equipment && !policy.CountEquipment) return false;
        if (input.SourceKind == TraitContributionSourceKind.ExplicitExtra) return policy.CountExplicitExtra;
        if (input.IsTemporary && policy.TemporaryUnitPolicy == TraitTemporaryUnitPolicy.Exclude) return false;
        return policy.DeploymentPolicy != TraitDeploymentPolicy.DeployedOnly || input.IsDeployed;
    }

    private static TraitContributionSnapshot ToSnapshot(TraitContributionInput input) => new(
        input.TraitId,
        input.Value,
        input.Team,
        input.SourceKind,
        input.SourceInstanceId,
        input.OwnerRuntimeId,
        input.ContentIdentity,
        input.IsPersistent,
        input.IsTemporary,
        input.IsDeployed);

    private static string Fingerprint(
        IEnumerable<TraitValueSnapshot> values,
        IEnumerable<TraitContributionSnapshot> contributions)
    {
        var canonical = string.Join("|", values.Select(value =>
            $"{value.Team}:{value.TraitId}:{value.Value}:{value.ActiveBreakpoint?.Fingerprint}")) + "||" +
            string.Join("|", contributions.Select(contribution =>
                $"{contribution.Team}:{contribution.TraitId}:{contribution.Value}:{contribution.SourceKind}:" +
                $"{contribution.SourceInstanceId}:{contribution.OwnerRuntimeId}:{contribution.ContentIdentity}:" +
                $"{contribution.IsPersistent}:{contribution.IsTemporary}:{contribution.IsDeployed}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}

public static class RunTraitSnapshotBuilder
{
    public static TraitSnapshot Build(
        ActiveRunDto run,
        CompiledContentGraph graph,
        IEnumerable<TraitExplicitContribution>? explicitExtras = null) =>
        TraitSnapshotBuilder.Build(graph.Traits, CollectInputs(run, graph, explicitExtras));

    public static ImmutableArray<TraitContributionInput> CollectInputs(
        ActiveRunDto run,
        CompiledContentGraph graph,
        IEnumerable<TraitExplicitContribution>? explicitExtras = null)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(graph);
        if (run.Roster is null || run.Deployment is null)
            throw new ArgumentException("Run roster or deployment is missing.", nameof(run));
        var deployed = run.Deployment.Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        var inputs = ImmutableArray.CreateBuilder<TraitContributionInput>();
        foreach (var hero in run.Roster.Where(hero => hero is not null)
                     .OrderBy(hero => hero.InstanceId, StringComparer.Ordinal))
        {
            var isDeployed = deployed.Contains(hero.InstanceId);
            foreach (var contribution in graph.ResolveUnitTraitContributions(hero.ContentId))
                inputs.Add(new TraitContributionInput(
                    contribution.TraitId,
                    contribution.Value,
                    0,
                    TraitContributionSourceKind.Hero,
                    hero.InstanceId,
                    hero.InstanceId,
                    hero.ContentId,
                    true,
                    false,
                    isDeployed));
            if (hero.Equipment is null)
                throw new ArgumentException("Run hero Equipment state is missing.", nameof(run));
            foreach (var equipment in hero.Equipment.Where(item => item is not null)
                         .OrderBy(item => item.SlotIndex))
            foreach (var contribution in graph.ResolveEquipment(equipment.ContentId).TraitContributions)
                inputs.Add(new TraitContributionInput(
                    contribution.TraitId,
                    contribution.Value,
                    0,
                    TraitContributionSourceKind.Equipment,
                    equipment.InstanceId,
                    hero.InstanceId,
                    equipment.ContentId,
                    true,
                    false,
                    isDeployed));
        }
        foreach (var extra in explicitExtras ?? [])
        {
            if (extra is null) throw new ArgumentException("Explicit Trait contribution is missing.", nameof(explicitExtras));
            inputs.Add(new TraitContributionInput(
                extra.TraitId,
                extra.Value,
                extra.Team,
                TraitContributionSourceKind.ExplicitExtra,
                extra.SourceInstanceId,
                string.Empty,
                extra.ContentIdentity,
                false,
                false,
                false));
        }
        return inputs.ToImmutable();
    }
}

public static class TraitBattlePreparationBuilder
{
    public static TraitBattlePreparation Build(
        IEnumerable<CompiledTraitDefinition> definitions,
        IEnumerable<TraitContributionInput> contributions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(contributions);
        var orderedDefinitions = definitions.OrderBy(definition => definition.StableId, StringComparer.Ordinal)
            .ToImmutableArray();
        var orderedContributions = contributions.OrderBy(input => input.Team)
            .ThenBy(input => input.TraitId, StringComparer.Ordinal)
            .ThenBy(input => input.SourceKind)
            .ThenBy(input => input.SourceInstanceId, StringComparer.Ordinal)
            .ToImmutableArray();
        var snapshot = TraitSnapshotBuilder.Build(orderedDefinitions, orderedContributions);
        var source = string.Join("|", orderedDefinitions.Select(definition => definition.Fingerprint)) + "||" +
                     snapshot.Fingerprint;
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();
        return new TraitBattlePreparation(fingerprint, orderedDefinitions, orderedContributions);
    }

    public static bool HasValidFingerprint(TraitBattlePreparation preparation) =>
        preparation.SourceFingerprint == Build(preparation.Definitions, preparation.Contributions).SourceFingerprint;
}
