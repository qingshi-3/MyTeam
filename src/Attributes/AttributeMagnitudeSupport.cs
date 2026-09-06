using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TowerAutobattler.Content;

namespace TowerAutobattler.Attributes;

[Flags]
public enum AttributeContextCapabilities
{
    None = 0, SourceAttribute = 1, TargetAttribute = 2, ContextValue = 4, TeamCount = 8, TraitValue = 16,
    All = SourceAttribute | TargetAttribute | ContextValue | TeamCount | TraitValue
}

/// <summary>One contract for validation, execution, and identity across all content carriers.</summary>
public static class AttributeMagnitudeSupport
{
    public static IEnumerable<CompiledAttributeMagnitude> Leaves(CompiledAttributeMagnitude magnitude) =>
        magnitude is CompiledCompositeMagnitude composite
            ? composite.Operands.SelectMany(Leaves)
            : [magnitude];

    public static void Validate(CompiledAttributeMagnitude magnitude, AttributeContextCapabilities capabilities,
        ValidationReport report, string label)
    {
        foreach (var leaf in Leaves(magnitude))
        {
            var required = leaf switch
            {
                CompiledConstantMagnitude => AttributeContextCapabilities.None,
                CompiledSourceAttributeMagnitude => AttributeContextCapabilities.SourceAttribute,
                CompiledTargetAttributeMagnitude => AttributeContextCapabilities.TargetAttribute,
                CompiledContextValueMagnitude => AttributeContextCapabilities.ContextValue,
                CompiledTeamCountMagnitude => AttributeContextCapabilities.TeamCount,
                CompiledTraitValueMagnitude => AttributeContextCapabilities.TraitValue,
                _ => (AttributeContextCapabilities)(-1)
            };
            if ((capabilities & required) != required)
                report.Error($"{label}: magnitude requires unavailable context {required}.");
        }
    }

    public static float Evaluate(CompiledAttributeMagnitude magnitude, BattleAttributeMagnitudeContext context)
    {
        var value = magnitude switch
        {
            CompiledConstantMagnitude constant => constant.Value,
            CompiledSourceAttributeMagnitude source => context.SourceValue(source.Attribute),
            CompiledTargetAttributeMagnitude target => context.TargetValue(target.Attribute),
            CompiledContextValueMagnitude invocation => context.ContextValue(invocation.Key),
            CompiledTeamCountMagnitude count => context.TeamCount(count.CountKind, count.Team),
            CompiledTraitValueMagnitude trait => context.TraitValue(trait.TraitId, trait.Team),
            CompiledCompositeMagnitude composite => EvaluateComposite(composite, context),
            _ => throw new InvalidOperationException($"Unsupported compiled magnitude: {magnitude.GetType().Name}.")
        };
        if (!float.IsFinite(value)) throw new InvalidOperationException("Magnitude resolved to a non-finite value.");
        return value;
    }

    private static float EvaluateComposite(CompiledCompositeMagnitude composite, BattleAttributeMagnitudeContext context)
    {
        var values = composite.Operands.Select(child => Evaluate(child, context));
        return composite.Operation switch
        {
            AttributeMagnitudeOperation.Add => values.Sum(),
            AttributeMagnitudeOperation.Multiply => values.Aggregate(1f, (a, b) => a * b),
            AttributeMagnitudeOperation.Minimum => values.Min(),
            AttributeMagnitudeOperation.Maximum => values.Max(),
            _ => throw new InvalidOperationException("Unsupported magnitude operation.")
        };
    }

    // Length-prefixed strings avoid collisions when an authored key contains separators.
    public static string Fingerprint(CompiledAttributeMagnitude magnitude) => $"{magnitude.CaptureMode}:" + (magnitude switch
    {
        CompiledConstantMagnitude c => $"constant:{c.Value.ToString("R", CultureInfo.InvariantCulture)}",
        CompiledSourceAttributeMagnitude s => $"source:{s.Attribute}",
        CompiledTargetAttributeMagnitude t => $"target:{t.Attribute}",
        CompiledContextValueMagnitude c => $"context:{c.Key.Length}:{c.Key}",
        CompiledTeamCountMagnitude c => $"count:{c.CountKind}:{c.Team}",
        CompiledTraitValueMagnitude t => $"trait:{t.Team}:{t.TraitId.Length}:{t.TraitId}",
        CompiledCompositeMagnitude c => $"{c.Operation}[{string.Join(";", c.Operands.Select(Fingerprint))}]",
        _ => throw new InvalidOperationException("Unsupported magnitude fingerprint.")
    });
}
