using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Statuses;

/// <summary>Exact content identity, independent of rounded player-facing descriptions.</summary>
public static class StatusDefinitionFingerprint
{
    private static string Pack(params string[] parts) => string.Concat(parts.Select(part => $"{part.Length}:{part}"));
    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    public static string Compute(CompiledStatusDefinition status)
    {
        ArgumentNullException.ThrowIfNull(status);
        var canonical = Pack(
            "status-v1", status.StableId, status.ResourcePath, status.DisplayName,
            status.Behavior.ToString(), status.Disposition.ToString(), status.DurationKind.ToString(), Number(status.DurationTicks),
            status.AggregationPolicy.ToString(), Number(status.StackLimit), status.OverflowPolicy.ToString(),
            status.DurationRefreshPolicy.ToString(), status.PeriodicResetPolicy.ToString(), status.DispelCategory.ToString(),
            status.DeathPolicy.ToString(), status.ControlDurationRule.ToString(),
            Pack(status.GrantedTags.ToArray()),
            Pack(status.AttributeModifiers.Select(modifier => Pack(modifier.Attribute.ToString(), modifier.Operation.ToString(),
                AttributeMagnitudeSupport.Fingerprint(modifier.Magnitude), Number(modifier.Priority), modifier.SlotId)).ToArray()),
            status.Magnitude.ToString("R", CultureInfo.InvariantCulture), Number(status.PeriodicIntervalTicks),
            status.PeriodicEffect is null ? "none" : Pack("effect", EffectModelText.BindingFingerprint(status.PeriodicEffect)),
            Pack(status.LifecycleBindings.Select(binding => Pack(binding.Trigger.ToString(),
                EffectModelText.BindingFingerprint(binding.Binding))).ToArray()),
            Pack(status.CombatReactiveBindings.Select(binding => Pack(binding.EventKind.ToString(), binding.OwnerRole.ToString(),
                binding.EffectSourcePolicy.ToString(), Number(binding.Priority), EffectModelText.BindingFingerprint(binding.Binding),
                binding.SourceKind.ToString(), binding.FilterDamageType.ToString(), binding.DamageType.ToString())).ToArray()),
            status.OverflowTransition is null ? "none" : Pack("transition", Number(status.OverflowTransition.ConsumeStacks),
                Compute(status.OverflowTransition.Target)),
            status.Presentation is null ? "none" : Pack("presentation", status.Presentation.SemanticIcon,
                status.Presentation.ExecutedCue, status.Presentation.OnActiveCue, status.Presentation.WhileActiveCue,
                status.Presentation.RemovedCue, status.Presentation.ReportLabel, status.Presentation.PersistentVfx));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
