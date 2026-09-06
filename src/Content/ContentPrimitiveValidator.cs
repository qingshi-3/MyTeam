using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;
using TowerAutobattler.Relics;

namespace TowerAutobattler.Content;

internal static class ContentPrimitiveValidator
{
    // Module compilation establishes local capabilities; publication resolves cross-product ids.
    public static void Validate(CompiledContentGraph graph, ValidationReport report)
    {
        var traits = graph.Traits.Select(trait => trait.StableId).ToHashSet(StringComparer.Ordinal);
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
}
