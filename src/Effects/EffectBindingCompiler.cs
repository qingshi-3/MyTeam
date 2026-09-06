using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using TowerAutobattler.Content;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Effects;

public sealed record EffectBindingCompilationResult(
    CompiledEffectBinding? Binding,
    ValidationReport Report);

public sealed record EffectBindingBatchCompilationResult(
    ImmutableArray<CompiledEffectBinding> Bindings,
    ValidationReport Report);

public static partial class EffectBindingCompiler
{
    private static readonly Regex StableIdPattern = StableIdRegex();

    public static EffectBindingCompilationResult Compile(
        EffectBindingSpec? authored,
        EffectProcessorRegistry? processors = null)
    {
        var report = new ValidationReport();
        var compiled = CompileInternal(authored, processors ?? EffectProcessorRegistry.CreateDefault(), report);
        return new EffectBindingCompilationResult(report.HasCoreErrors ? null : compiled, report);
    }

    public static EffectBindingBatchCompilationResult CompileBatch(
        IEnumerable<EffectBindingSpec?> authoredBindings,
        EffectProcessorRegistry? processors = null)
    {
        ArgumentNullException.ThrowIfNull(authoredBindings);
        var report = new ValidationReport();
        var registry = processors ?? EffectProcessorRegistry.CreateDefault();
        var compiled = new List<CompiledEffectBinding>();
        var stableIds = new HashSet<string>(StringComparer.Ordinal);

        var index = 0;
        foreach (var authored in authoredBindings)
        {
            var binding = CompileInternal(authored, registry, report, index++);
            if (binding is null) continue;
            if (!stableIds.Add(binding.StableId))
            {
                report.Error($"Duplicate effect binding stable id: {binding.StableId}");
                continue;
            }
            compiled.Add(binding);
        }

        if (!report.HasCoreErrors) ValidateDependencyCycles(compiled, report);
        return new EffectBindingBatchCompilationResult(
            report.HasCoreErrors ? [] : compiled.ToImmutableArray(),
            report);
    }

    private static CompiledEffectBinding? CompileInternal(
        EffectBindingSpec? authored,
        EffectProcessorRegistry processors,
        ValidationReport report,
        int? batchIndex = null)
    {
        var path = authored?.ResourcePath;
        var label = !string.IsNullOrWhiteSpace(path)
            ? path
            : batchIndex is null ? "effect binding" : $"effect binding[{batchIndex}]";
        if (authored is null)
        {
            report.Error($"{label}: binding resource is missing.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(authored.StableId))
            report.Error($"{label}: stable id is required.");
        else if (!StableIdPattern.IsMatch(authored.StableId))
            report.Error($"{label}: invalid stable id '{authored.StableId}'.");

        var trigger = CompileTrigger(authored.Trigger, label, report);
        var conditions = CompileConditions(authored.Conditions, label, report);
        var target = CompileTarget(authored.TargetQuery, label, report);
        var effects = CompileEffects(authored.Effects, trigger, processors, label, report);
        var limits = CompileLimits(authored.Limits, label, report);
        var presentation = authored.Presentation is null
            ? null
            : new CompiledEffectPresentation(
                authored.Presentation.DisplayName,
                authored.Presentation.ReportLabel,
                authored.Presentation.Cue.ToString());

        if (report.HasCoreErrors || trigger is null || target is null || limits is null)
            return null;
        return new CompiledEffectBinding(
            authored.StableId,
            authored.Priority,
            trigger,
            conditions,
            target,
            effects,
            limits,
            presentation);
    }

    private static CompiledEffectTrigger? CompileTrigger(
        EffectTriggerSpec? authored,
        string label,
        ValidationReport report)
    {
        if (authored is null)
        {
            report.Error($"{label}: trigger is required.");
            return null;
        }
        if (!Enum.IsDefined(authored.Kind))
            report.Error($"{label}: trigger kind is invalid.");
        if (!Enum.IsDefined(authored.EventKind))
            report.Error($"{label}: trigger event kind is invalid.");
        if (authored.Kind == EffectTriggerKind.Manual && authored.EventKind != EffectDomainEventKind.None)
            report.Error($"{label}: manual trigger cannot declare a domain event.");
        if (authored.Kind == EffectTriggerKind.DomainEvent && authored.EventKind == EffectDomainEventKind.None)
            report.Error($"{label}: domain-event trigger requires an event kind.");
        return new CompiledEffectTrigger(authored.Kind, authored.EventKind);
    }

    private static ImmutableArray<CompiledEffectCondition> CompileConditions(
        Godot.Collections.Array<EffectConditionSpec>? authored,
        string label,
        ValidationReport report)
    {
        if (authored is null)
        {
            report.Error($"{label}: condition collection is missing.");
            return [];
        }
        var conditions = ImmutableArray.CreateBuilder<CompiledEffectCondition>();
        for (var index = 0; index < authored.Count; index++)
        {
            switch (authored[index])
            {
                case EntityAliveConditionSpec alive when Enum.IsDefined(alive.Entity):
                    conditions.Add(new CompiledEntityAliveCondition(alive.Entity, alive.ExpectedAlive));
                    break;
                case EntityAliveConditionSpec:
                    report.Error($"{label}: condition[{index}] has an invalid entity reference.");
                    break;
                case HealthRatioConditionSpec health when Enum.IsDefined(health.Entity) && Enum.IsDefined(health.Comparison) &&
                    float.IsFinite(health.Ratio) && health.Ratio is >= 0 and <= 1:
                    conditions.Add(new CompiledHealthRatioCondition(health.Entity, health.Comparison, health.Ratio));
                    break;
                case HealthRatioConditionSpec:
                    report.Error($"{label}: condition[{index}] requires a valid entity/comparison and health ratio in [0,1].");
                    break;
                case EntityTagConditionSpec tag when Enum.IsDefined(tag.Entity) && !string.IsNullOrWhiteSpace(tag.Tag.ToString()):
                    conditions.Add(new CompiledEntityTagCondition(tag.Entity, tag.Tag.ToString(), tag.ExpectedPresent));
                    break;
                case EntityTagConditionSpec:
                    report.Error($"{label}: condition[{index}] requires a valid entity and nonempty tag.");
                    break;
                case null:
                    report.Error($"{label}: condition[{index}] is missing.");
                    break;
                default:
                    report.Error($"{label}: condition[{index}] uses unsupported type '{authored[index].GetType().Name}'.");
                    break;
            }
        }
        return conditions.ToImmutable();
    }

    public static CompiledEffectTargetQuery? CompileTarget(
        EffectTargetQuerySpec? authored,
        string label,
        ValidationReport report)
    {
        switch (authored)
        {
            case FilteredTargetQuerySpec filter when Enum.IsDefined(filter.Team) && Enum.IsDefined(filter.Anchor) &&
                Enum.IsDefined(filter.Order) && float.IsFinite(filter.Range) && (filter.Range == -1 || filter.Range >= 0) && filter.MaxTargets >= 0:
                return new CompiledFilteredTargetQuery(filter.Team, filter.Anchor, filter.IncludeDefeated, filter.IncludeAnchor,
                    filter.RequiredTag.ToString(), filter.Range, filter.MaxTargets, filter.Order);
            case FilteredTargetQuerySpec:
                report.Error($"{label}: filtered target requires valid relations/order, range -1 or >=0, and nonnegative target limit.");
                return null;
            case ExplicitTargetQuerySpec:
                return new CompiledExplicitTargetQuery();
            case SourceTargetQuerySpec:
                return new CompiledSourceTargetQuery();
            case OwnerTargetQuerySpec:
                return new CompiledOwnerTargetQuery();
            case RelativeTeamTargetQuerySpec relative when Enum.IsDefined(relative.Team):
                return new CompiledRelativeTeamTargetQuery(
                    relative.Team,
                    relative.IncludeDefeated,
                    relative.RequiredTag.ToString());
            case RelativeTeamTargetQuerySpec:
                report.Error($"{label}: relative-team target has an invalid team relation.");
                return null;
            case null:
                report.Error($"{label}: target query is required.");
                return null;
            default:
                report.Error($"{label}: unsupported target query '{authored.GetType().Name}'.");
                return null;
        }
    }

    private static ImmutableArray<CompiledEffectStep> CompileEffects(
        Godot.Collections.Array<EffectStepSpec>? authored,
        CompiledEffectTrigger? trigger,
        EffectProcessorRegistry processors,
        string label,
        ValidationReport report)
    {
        if (authored is null || authored.Count == 0)
        {
            report.Error($"{label}: at least one effect step is required.");
            return [];
        }
        var effects = ImmutableArray.CreateBuilder<CompiledEffectStep>();
        for (var index = 0; index < authored.Count; index++)
        {
            var step = authored[index];
            if (step is null)
            {
                report.Error($"{label}: effect[{index}] is missing.");
                continue;
            }
            var kind = step switch
            {
                DamageEffectSpec => EffectKind.Damage,
                HealEffectSpec => EffectKind.Heal,
                ShieldEffectSpec => EffectKind.Shield,
                _ => (EffectKind?)null
            };
            if (kind is null)
            {
                report.Error($"{label}: effect[{index}] uses unsupported type '{step.GetType().Name}'.");
                continue;
            }
            if (!processors.Contains(kind.Value))
                report.Error($"{label}: effect[{index}] has no processor for {kind.Value}.");
            if (!Enum.IsDefined(step.AmountSource))
                report.Error($"{label}: effect[{index}] has an invalid amount source.");
            if (!float.IsFinite(step.Amount) || step.Amount < 0)
                report.Error($"{label}: effect[{index}] amount must be finite and non-negative.");
            if (step.AmountSource == EffectAmountSource.EventEffectiveValue &&
                trigger?.Kind != EffectTriggerKind.DomainEvent)
                report.Error($"{label}: effect[{index}] reads event value from a non-event trigger.");
            CompiledAttributeMagnitude? magnitude = null;
            if (step.Magnitude is not null)
            {
                if (step.AmountSource != EffectAmountSource.Fixed || step.Amount != 1f)
                    report.Error($"{label}: effect[{index}] formula replaces AmountSource/Amount; leave them Fixed/1.");
                magnitude = AttributeDefinitionCompiler.CompileMagnitude(step.Magnitude, report);
                if (magnitude is not null)
                foreach (var leaf in AttributeMagnitudeSupport.Leaves(magnitude).OfType<CompiledContextValueMagnitude>())
                    if (leaf.Key != "InvocationValue" && (leaf.Key != "EventEffectiveValue" || trigger?.Kind != EffectTriggerKind.DomainEvent))
                        report.Error($"{label}: effect[{index}] context '{leaf.Key}' is not supplied by this trigger.");
            }
            var damageType = step is DamageEffectSpec damage ? damage.DamageType : EffectDamageType.Physical;
            if (!Enum.IsDefined(damageType)) report.Error($"{label}: effect[{index}] has an invalid damage type.");
            effects.Add(new CompiledEffectStep(kind.Value, step.AmountSource, step.Amount, magnitude, damageType));
        }
        return effects.ToImmutable();
    }

    private static CompiledEffectBindingLimits? CompileLimits(
        EffectBindingLimitsSpec? authored,
        string label,
        ValidationReport report)
    {
        if (authored is null)
        {
            report.Error($"{label}: execution limits are required.");
            return null;
        }
        if (authored.MaxUses < 0) report.Error($"{label}: max uses cannot be negative.");
        if (authored.MinimumIntervalTicks < 0) report.Error($"{label}: minimum interval cannot be negative.");
        if (authored.MaxDepth < 0) report.Error($"{label}: max depth cannot be negative.");
        if (authored.MaxRepeatedEdges < 0) report.Error($"{label}: repeated-edge limit cannot be negative.");
        return new CompiledEffectBindingLimits(
            authored.MaxUses,
            authored.MinimumIntervalTicks,
            authored.MaxDepth,
            authored.MaxRepeatedEdges);
    }

    private static void ValidateDependencyCycles(
        IReadOnlyList<CompiledEffectBinding> bindings,
        ValidationReport report)
    {
        var listeners = bindings
            .Where(binding => binding.Trigger.Kind == EffectTriggerKind.DomainEvent)
            .GroupBy(binding => binding.Trigger.EventKind)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var graph = bindings.ToDictionary(
            binding => binding.StableId,
            _ => new HashSet<string>(StringComparer.Ordinal),
            StringComparer.Ordinal);
        foreach (var producer in bindings)
        foreach (var producedEvent in producer.Effects.Select(step => EventKindFor(step.Kind)).Distinct())
        {
            if (!listeners.TryGetValue(producedEvent, out var consumers)) continue;
            foreach (var consumer in consumers) graph[producer.StableId].Add(consumer.StableId);
        }

        var state = new Dictionary<string, int>(StringComparer.Ordinal);
        var stack = new List<string>();
        foreach (var id in graph.Keys.OrderBy(value => value, StringComparer.Ordinal))
            if (!state.ContainsKey(id) && FindCycle(id, graph, state, stack, out var cycle))
            {
                report.Error($"Effect binding dependency cycle: {string.Join(" -> ", cycle)}");
                return;
            }
    }

    private static bool FindCycle(
        string id,
        IReadOnlyDictionary<string, HashSet<string>> graph,
        IDictionary<string, int> state,
        IList<string> stack,
        out IReadOnlyList<string> cycle)
    {
        state[id] = 1;
        stack.Add(id);
        foreach (var next in graph[id].OrderBy(value => value, StringComparer.Ordinal))
        {
            if (!state.TryGetValue(next, out var nextState))
            {
                if (FindCycle(next, graph, state, stack, out cycle)) return true;
            }
            else if (nextState == 1)
            {
                var start = stack.IndexOf(next);
                cycle = stack.Skip(start).Append(next).ToArray();
                return true;
            }
        }
        stack.RemoveAt(stack.Count - 1);
        state[id] = 2;
        cycle = [];
        return false;
    }

    internal static EffectDomainEventKind EventKindFor(EffectKind kind) => kind switch
    {
        EffectKind.Damage => EffectDomainEventKind.DamageResolved,
        EffectKind.Heal => EffectDomainEventKind.HealingResolved,
        EffectKind.Shield => EffectDomainEventKind.ShieldResolved,
        _ => EffectDomainEventKind.None
    };

    [GeneratedRegex("^[a-z0-9]+(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableIdRegex();
}
