using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using TowerAutobattler.Content;

namespace TowerAutobattler.Statuses;

public sealed class StatusGrantRuntimeContext
{
    public required Func<int> Tick { get; init; }
    public required Func<string, bool> CanReceive { get; init; }
    public required Action<ImmutableArray<string>, ImmutableArray<StatusApplicationRequest>> Replace { get; init; }
}

// A grant is a revocable, source-isolated passive, not an alternative status lifetime engine.
public static class StatusGrantCompiler
{
    public static ImmutableArray<CompiledStatusDefinition> Compile(
        IEnumerable<StatusDefinition?> authored,
        Func<StatusDefinition?, CompiledStatusDefinition?>? resolve,
        ValidationReport report, string label)
    {
        var result = ImmutableArray.CreateBuilder<CompiledStatusDefinition>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in authored)
        {
            var compiled = item is null ? null : resolve?.Invoke(item);
            if (compiled is null) { report.Error($"{label}: granted Status is missing or unpublished."); continue; }
            if (!seen.Add(compiled.StableId)) { report.Error($"{label}: duplicate granted Status '{compiled.StableId}'."); continue; }
            if (!IsSupported(compiled))
                report.Error($"{label}: granted Status '{compiled.StableId}' must be permanent, BySource, one stack, non-dispellable, removed on owner death, and have no overflow transition.");
            result.Add(compiled);
        }
        return result.ToImmutable();
    }

    public static bool IsSupported(CompiledStatusDefinition definition) =>
        definition.DurationKind == StatusDurationKind.Permanent &&
        definition.AggregationPolicy == StatusAggregationPolicy.BySource && definition.StackLimit == 1 &&
        definition.DispelCategory == StatusDispelCategory.NonDispellable && definition.DeathPolicy == StatusDeathPolicy.Remove &&
        definition.OverflowTransition is null;
}
