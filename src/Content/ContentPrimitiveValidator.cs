using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Content;

internal static class ContentPrimitiveValidator
{
    // Module compilation establishes local capabilities; publication resolves cross-product ids.
    public static void Validate(CompiledContentGraph graph, ValidationReport report)
    {
        ValidateAbilityStatusReferences(graph.Abilities,
            graph.Statuses.ToDictionary(status => status.StableId, StringComparer.Ordinal), report);
        var traits = graph.Traits.Select(trait => trait.StableId).ToHashSet(StringComparer.Ordinal);
        foreach (var ability in graph.Abilities)
        foreach (var operation in ability.Operations.OfType<CompiledScaledStatusAbilityOperation>()
                     .Where(operation => !string.IsNullOrEmpty(operation.ChanceTraitId)))
        {
            var trait = graph.Traits.FirstOrDefault(trait => trait.StableId == operation.ChanceTraitId);
            if (trait is null)
                report.Error($"{ability.StableId}: unknown probability Trait reference '{operation.ChanceTraitId}'.");
            else if (trait.Breakpoints.Length != operation.ChanceByTraitTier.Length)
                report.Error($"{ability.StableId}: probability table must cover every tier of '{operation.ChanceTraitId}'.");
        }
        void Magnitude(CompiledAttributeMagnitude magnitude, string label)
        {
            foreach (var leaf in AttributeMagnitudeSupport.Leaves(magnitude).OfType<CompiledTraitValueMagnitude>())
                if (!traits.Contains(leaf.TraitId)) report.Error($"{label}: unknown Trait magnitude reference '{leaf.TraitId}'.");
        }
        void Effect(CompiledEffectBinding binding, string label)
        {
            for (var index = 0; index < binding.Effects.Length; index++)
                if (binding.Effects[index].Magnitude is { } magnitude) Magnitude(magnitude, $"{label}/{binding.StableId}/effect[{index}]");
        }
        foreach (var ability in graph.Abilities)
        foreach (var effect in ability.Operations.OfType<CompiledEffectAbilityOperation>()) Effect(effect.Binding, ability.StableId);
        foreach (var status in graph.Statuses)
        {
            foreach (var modifier in status.AttributeModifiers) Magnitude(modifier.Magnitude, status.StableId);
            if (status.PeriodicEffect is not null) Effect(status.PeriodicEffect, status.StableId);
            foreach (var binding in status.LifecycleBindings) Effect(binding.Binding, status.StableId);
            foreach (var binding in status.CombatReactiveBindings) Effect(binding.Binding, status.StableId);
        }
        foreach (var equipment in graph.Equipment)
        foreach (var modifier in equipment.AttributeModifiers) Magnitude(modifier.Magnitude, equipment.StableId);
        foreach (var trait in graph.Traits)
        foreach (var tier in trait.Breakpoints)
        foreach (var modifier in tier.AttributeModifiers) Magnitude(modifier.Magnitude, trait.StableId);
        foreach (var relic in graph.Relics)
        {
            foreach (var binding in relic.AttributeBindings) Magnitude(binding.Modifier.Magnitude, relic.StableId);
            foreach (var binding in relic.BattleStartEffects.OfType<CompiledRelicBattleStartShield>()) Effect(binding.Effect, relic.StableId);
            foreach (var counter in relic.ReactiveCounters) Effect(counter.ThresholdEffect, relic.StableId);
        }
    }

    internal static void ValidateAbilityStatusReferences(
        IEnumerable<CompiledAbilityDefinition> abilities,
        IReadOnlyDictionary<string, CompiledStatusDefinition> statuses,
        ValidationReport report)
    {
        foreach (var ability in abilities)
        foreach (var operation in ability.Operations)
        {
            switch (operation)
            {
                case CompiledScaledStatusAbilityOperation scaled:
                    if (!statuses.ContainsKey(scaled.Status.StableId))
                        report.Error($"{ability.StableId}: unknown applied status '{scaled.Status.StableId}'.");
                    foreach (var id in new[] { scaled.StatusId, scaled.ChanceStatusId }.Where(id => !string.IsNullOrWhiteSpace(id)))
                        if (!statuses.ContainsKey(id))
                            report.Error($"{ability.StableId}: unknown scaled status reference '{id}'.");
                    break;
                case CompiledBattleValueOperation value:
                    foreach (var term in value.Terms.Where(term => term.Metric == BattleValueMetric.StatusStacks))
                        if (!statuses.ContainsKey(term.Key))
                            report.Error($"{ability.StableId}: unknown status-stack reference '{term.Key}'.");
                    break;
                case CompiledConsumeStatusOperation consume:
                    if (!statuses.TryGetValue(consume.StatusId, out var status))
                        report.Error($"{ability.StableId}: unknown consumed status '{consume.StatusId}'.");
                    else if (!SupportsPeriodicDamageCashOut(status))
                        report.Error($"{ability.StableId}: consumed status '{consume.StatusId}' must contain one unconditional, owner-targeted periodic damage step using its stack magnitude.");
                    break;
                case CompiledDisplacementOperation displacement:
                    if (displacement.ImpactStatus is { } impact && !statuses.ContainsKey(impact.StableId))
                        report.Error($"{ability.StableId}: unknown displacement impact status '{impact.StableId}'.");
                    break;
                case CompiledEffectAbilityOperation:
                case CompiledCooldownAbilityOperation:
                case CompiledApplyStatusAbilityOperation:
                case CompiledSummonAbilityOperation:
                case CompiledProjectileSequenceAbilityOperation:
                case CompiledChargedLineOperation:
                case CompiledEnemyAction:
                case CompiledTrampleOperation:
                case CompiledCombatTechniqueOperation:
                case CompiledEchoOperation:
                case CompiledLifecycleOperation:
                    break;
                default:
                    report.Error($"{ability.StableId}: unsupported published Ability operation '{operation.GetType().Name}'.");
                    break;
            }
        }
    }

    // The runtime receipt counts remaining scheduled ticks times stacks times Magnitude.
    // Reject broader effects whose actual output cannot be represented by that receipt.
    private static bool SupportsPeriodicDamageCashOut(CompiledStatusDefinition status)
    {
        if (status.DurationKind != StatusDurationKind.TimedTicks || status.PeriodicIntervalTicks <= 0 ||
            !float.IsFinite(status.Magnitude) || status.Magnitude <= 0 || status.PeriodicEffect is not { } periodic)
            return false;
        return periodic.Trigger.Kind == EffectTriggerKind.Manual && periodic.Conditions.IsEmpty &&
            periodic.TargetQuery is CompiledOwnerTargetQuery && periodic.Effects.Length == 1 &&
            periodic.Effects[0] is { Kind: EffectKind.Damage, AmountSource: EffectAmountSource.InvocationValue,
                Amount: 1f, Magnitude: null } &&
            periodic.Limits.MaxUses == 0 && periodic.Limits.MinimumIntervalTicks == 0 &&
            periodic.Limits.MaxDepth == 0 && periodic.Limits.MaxRepeatedEdges == 0;
    }
}
