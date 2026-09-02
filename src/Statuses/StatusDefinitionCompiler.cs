using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Statuses;

public sealed record StatusCompilationResult(
    CompiledStatusDefinition? Definition,
    ValidationReport Report);

public sealed record StatusBatchCompilationResult(
    ImmutableArray<CompiledStatusDefinition> Definitions,
    ValidationReport Report)
{
    internal ImmutableArray<CompiledStatusPublication> Publications { get; init; } = [];
}

internal sealed record CompiledStatusPublication(
    StatusDefinition Authored,
    CompiledStatusDefinition Compiled);

public static partial class StatusDefinitionCompiler
{
    public const string ActionDisabledTag = "state.action_disabled";

    private static readonly Regex StableIdPattern = StableIdRegex();
    private static readonly Regex TagPattern = TagRegex();

    public static StatusCompilationResult Compile(StatusDefinition? authored)
    {
        var report = new ValidationReport();
        if (authored is null)
        {
            report.Error("status definition: definition is missing.");
            return new StatusCompilationResult(null, report);
        }

        var compiler = new StatusGraphCompiler([authored], report, allowImplicitDependencies: true);
        var compiled = compiler.Compile(authored);
        return new StatusCompilationResult(report.HasCoreErrors ? null : compiled, report);
    }

    public static StatusBatchCompilationResult CompileBatch(IEnumerable<StatusDefinition?> authored)
    {
        ArgumentNullException.ThrowIfNull(authored);
        var entries = authored.ToArray();
        var report = new ValidationReport();
        for (var index = 0; index < entries.Length; index++)
            if (entries[index] is null)
                report.Error($"status definition[{index}]: definition is missing.");

        var nonNull = entries.OfType<StatusDefinition>().ToArray();
        var compiler = new StatusGraphCompiler(nonNull, report, allowImplicitDependencies: false);
        foreach (var definition in compiler.CanonicalDefinitions)
            compiler.Compile(definition);

        if (report.HasCoreErrors)
            return new StatusBatchCompilationResult([], report);

        var definitions = compiler.CanonicalDefinitions
            .Select(definition => compiler.ResolveCompiled(definition))
            .OrderBy(definition => definition.StableId, StringComparer.Ordinal)
            .ToImmutableArray();
        var publications = nonNull.Select(definition =>
                new CompiledStatusPublication(definition, compiler.ResolveCompiled(definition)))
            .ToImmutableArray();
        return new StatusBatchCompilationResult(definitions, report) { Publications = publications };
    }

    private static CompiledStatusDefinition? CompileDefinition(
        StatusDefinition authored,
        CompiledStatusDefinition? overflowTarget,
        ValidationReport report)
    {
        var label = ResourceLabel(authored);
        var errorCount = report.CoreErrors.Count;
        if (string.IsNullOrWhiteSpace(authored.StableId))
            report.Error($"{label}: stable id is required.");
        else if (!StableIdPattern.IsMatch(authored.StableId))
            report.Error($"{label}: invalid stable id '{authored.StableId}'.");
        if (string.IsNullOrWhiteSpace(authored.DisplayName))
            report.Error($"{label}: display name is required.");
        ValidateEnum(authored.Behavior, label, "behavior kind", report);
        ValidateEnum(authored.Disposition, label, "disposition", report);
        ValidateEnum(authored.DurationKind, label, "duration kind", report);
        ValidateEnum(authored.AggregationPolicy, label, "aggregation policy", report);
        ValidateEnum(authored.OverflowPolicy, label, "overflow policy", report);
        ValidateEnum(authored.DurationRefreshPolicy, label, "duration refresh policy", report);
        ValidateEnum(authored.PeriodicResetPolicy, label, "periodic reset policy", report);
        ValidateEnum(authored.DispelCategory, label, "dispel category", report);
        ValidateEnum(authored.DeathPolicy, label, "death policy", report);
        ValidateEnum(authored.ControlDurationRule, label, "control-duration rule", report);

        switch (authored.DurationKind)
        {
            case StatusDurationKind.TimedTicks when authored.DurationTicks <= 0:
                report.Error($"{label}: timed status duration must be positive.");
                break;
            case StatusDurationKind.Permanent or StatusDurationKind.Instant when authored.DurationTicks != 0:
                report.Error($"{label}: permanent/instant status cannot declare duration ticks.");
                break;
        }

        if (authored.StackLimit < 0)
            report.Error($"{label}: stack limit cannot be negative; zero means unlimited.");
        if (authored.AggregationPolicy == StatusAggregationPolicy.Independent && authored.StackLimit != 1)
            report.Error($"{label}: independent Status instances require a stack limit of one.");
        if (authored.OverflowPolicy == StatusOverflowPolicy.ApplyStatusAndConsumeAtLimit)
        {
            if (authored.StackLimit <= 0)
                report.Error($"{label}: transition overflow requires a finite positive stack limit.");
            if (overflowTarget is null)
                report.Error($"{label}: transition overflow requires a registered target Status.");
            if (authored.OverflowConsumeStacks < 0 || authored.OverflowConsumeStacks > authored.StackLimit)
                report.Error($"{label}: overflow consume stacks must be zero (all) or within the stack limit.");
        }
        else if (authored.OverflowStatus is not null || authored.OverflowConsumeStacks != 0)
            report.Error($"{label}: overflow target/consume values require transition overflow.");
        if (authored.OverflowPolicy == StatusOverflowPolicy.RefreshDuration &&
            authored.DurationKind != StatusDurationKind.TimedTicks)
            report.Error($"{label}: refresh-duration overflow requires a timed Status.");

        if (authored.DurationKind != StatusDurationKind.TimedTicks &&
            authored.DurationRefreshPolicy != StatusDurationRefreshPolicy.None)
            report.Error($"{label}: only timed Statuses may declare duration refresh behavior.");
        if (authored.DurationKind == StatusDurationKind.Instant &&
            (authored.PeriodicIntervalTicks != 0 || authored.PeriodicEffect is not null))
            report.Error($"{label}: instant Statuses cannot declare periodic execution.");
        if (authored.PeriodicIntervalTicks < 0)
            report.Error($"{label}: periodic interval cannot be negative.");
        if ((authored.PeriodicIntervalTicks == 0) != (authored.PeriodicEffect is null))
            report.Error($"{label}: periodic interval and periodic effect must be authored together.");
        if (authored.PeriodicEffect is null &&
            authored.PeriodicResetPolicy != StatusPeriodicResetPolicy.KeepSchedule)
            report.Error($"{label}: periodic reset behavior requires a periodic effect.");

        if (!float.IsFinite(authored.Magnitude) || authored.Magnitude <= 0)
            report.Error($"{label}: magnitude must be finite and positive.");
        if (authored.Behavior == StatusBehaviorKind.DisableActions &&
            Math.Abs(authored.Magnitude - 1f) > .0001f)
            report.Error($"{label}: action-disable status magnitude must remain one.");

        var tags = ImmutableArray.CreateBuilder<string>();
        var tagSet = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < authored.GrantedTags.Count; index++)
        {
            var tag = authored.GrantedTags[index].ToString();
            if (string.IsNullOrWhiteSpace(tag) || !TagPattern.IsMatch(tag))
                report.Error($"{label}: granted tag[{index}] is invalid.");
            else if (!tagSet.Add(tag))
                report.Error($"{label}: duplicate granted tag '{tag}'.");
            else
                tags.Add(tag);
        }
        if (authored.Behavior == StatusBehaviorKind.DisableActions &&
            !tagSet.Contains(ActionDisabledTag))
            report.Error($"{label}: action-disable Status must grant typed tag '{ActionDisabledTag}'.");
        if (authored.ControlDurationRule != StatusControlDurationRule.None)
        {
            if (authored.DurationKind != StatusDurationKind.TimedTicks)
                report.Error($"{label}: control resistance can modify only a timed Status.");
            if (!tags.Any(tag => tag.StartsWith("state.", StringComparison.Ordinal)))
                report.Error($"{label}: control resistance requires at least one granted state tag.");
        }

        var modifiers = ImmutableArray.CreateBuilder<CompiledAttributeModifier>();
        var modifierKeys = new HashSet<(CombatAttribute Attribute, string SlotId)>();
        for (var index = 0; index < authored.AttributeModifiers.Count; index++)
        {
            var result = AttributeDefinitionCompiler.Compile(authored.AttributeModifiers[index]);
            foreach (var error in result.Report.CoreErrors)
                report.Error($"{label}: attribute modifier[{index}]: {error}");
            foreach (var warning in result.Report.Warnings)
                report.Warn($"{label}: attribute modifier[{index}]: {warning}");
            if (result.Modifier is not null)
            {
                var key = (result.Modifier.Attribute, result.Modifier.SlotId);
                if (!modifierKeys.Add(key))
                    report.Error($"{label}: duplicate attribute modifier projection '{key.Attribute}:{key.SlotId}'.");
                else
                    modifiers.Add(result.Modifier);
            }
        }
        if (authored.DurationKind == StatusDurationKind.Instant && modifiers.Count > 0)
            report.Error($"{label}: instant Statuses cannot retain Attribute modifiers.");
        if (authored.Behavior == StatusBehaviorKind.DamageMultiplier &&
            !modifiers.Any(modifier => modifier.Attribute == CombatAttribute.AttackDamage &&
                                       modifier.Operation == AttributeModifierOperation.Multiply))
            report.Error($"{label}: damage-multiplier Status must author an AttackDamage multiply modifier.");

        CompiledEffectBinding? periodic = null;
        if (authored.PeriodicEffect is not null)
        {
            var result = EffectBindingCompiler.Compile(authored.PeriodicEffect);
            MergePrefixed(report, result.Report, $"{label}: periodic effect: ");
            periodic = result.Binding;
            if (periodic is not null && periodic.Trigger.Kind != EffectTriggerKind.Manual)
                report.Error($"{label}: periodic Status effect must use a manual trigger.");
        }

        var lifecycle = ImmutableArray.CreateBuilder<CompiledStatusLifecycleBinding>();
        for (var index = 0; index < authored.LifecycleBindings.Count; index++)
        {
            var item = authored.LifecycleBindings[index];
            if (item is null)
            {
                report.Error($"{label}: lifecycle binding[{index}] is missing.");
                continue;
            }
            if (!Enum.IsDefined(item.Trigger))
                report.Error($"{label}: lifecycle binding[{index}] trigger is invalid.");
            var result = EffectBindingCompiler.Compile(item.Binding);
            MergePrefixed(report, result.Report, $"{label}: lifecycle binding[{index}]: ");
            if (result.Binding is not null)
            {
                if (result.Binding.Trigger.Kind != EffectTriggerKind.Manual)
                    report.Error($"{label}: lifecycle binding[{index}] must use a manual effect trigger.");
                lifecycle.Add(new CompiledStatusLifecycleBinding(item.Trigger, result.Binding));
            }
        }

        var reactive = ImmutableArray.CreateBuilder<CompiledStatusCombatReactiveBinding>();
        for (var index = 0; index < authored.CombatReactiveBindings.Count; index++)
        {
            var item = authored.CombatReactiveBindings[index];
            if (item is null)
            {
                report.Error($"{label}: combat reactive binding[{index}] is missing.");
                continue;
            }
            ValidateEnum(item.EventKind, label, $"combat reactive binding[{index}] event kind", report);
            ValidateEnum(item.OwnerRole, label, $"combat reactive binding[{index}] owner role", report);
            ValidateEnum(item.EffectSourcePolicy, label,
                $"combat reactive binding[{index}] effect-source policy", report);
            if (!IsReactiveEventKind(item.EventKind))
                report.Error($"{label}: combat reactive binding[{index}] event '{item.EventKind}' is not an active-combat hook.");
            var result = EffectBindingCompiler.Compile(item.Binding);
            MergePrefixed(report, result.Report, $"{label}: combat reactive binding[{index}]: ");
            if (result.Binding is not null)
            {
                if (result.Binding.Trigger.Kind != EffectTriggerKind.Manual)
                    report.Error($"{label}: combat reactive binding[{index}] must use a manual effect trigger.");
                reactive.Add(new CompiledStatusCombatReactiveBinding(
                    item.EventKind,
                    item.OwnerRole,
                    item.EffectSourcePolicy,
                    item.Priority,
                    result.Binding));
            }
        }
        if (authored.DurationKind == StatusDurationKind.Instant && reactive.Count > 0)
            report.Error($"{label}: instant Statuses cannot retain combat reactive bindings.");

        if (report.CoreErrors.Count != errorCount) return null;
        var presentation = authored.Presentation is null
            ? null
            : new CompiledStatusPresentation(
                authored.Presentation.SemanticIcon.ToString(),
                authored.Presentation.ExecutedCue.ToString(),
                authored.Presentation.OnActiveCue.ToString(),
                authored.Presentation.WhileActiveCue.ToString(),
                authored.Presentation.RemovedCue.ToString(),
                authored.Presentation.ReportLabel);
        return new CompiledStatusDefinition(
            authored.StableId,
            authored.ResourcePath,
            authored.DisplayName,
            Describe(authored),
            authored.Behavior,
            authored.Disposition,
            authored.DurationKind,
            authored.DurationTicks,
            authored.AggregationPolicy,
            authored.StackLimit,
            authored.OverflowPolicy,
            authored.DurationRefreshPolicy,
            authored.PeriodicResetPolicy,
            authored.DispelCategory,
            authored.DeathPolicy,
            authored.ControlDurationRule,
            tags.ToImmutable(),
            modifiers.ToImmutable(),
            authored.Magnitude,
            authored.PeriodicIntervalTicks,
            periodic,
            lifecycle.ToImmutable(),
            reactive.ToImmutable(),
            overflowTarget is null ? null : new CompiledStatusTransition(overflowTarget, authored.OverflowConsumeStacks),
            presentation);
    }

    private static bool IsReactiveEventKind(BattleCombatEventKind kind) => kind is
        BattleCombatEventKind.AttackDeclared or
        BattleCombatEventKind.AttackLanded or
        BattleCombatEventKind.AbilityResolved or
        BattleCombatEventKind.DamageResolved or
        BattleCombatEventKind.HealingResolved or
        BattleCombatEventKind.ShieldResolved or
        BattleCombatEventKind.UnitDefeated or
        BattleCombatEventKind.UnitKilled;

    private static string Describe(StatusDefinition definition) => definition.Behavior switch
    {
        StatusBehaviorKind.DisableActions when definition.DurationKind == StatusDurationKind.TimedTicks =>
            $"无法行动，持续 {definition.DurationTicks * BattleTiming.TickSeconds:0.##} 秒。",
        StatusBehaviorKind.DamageMultiplier =>
            $"伤害提高 {(definition.Magnitude - 1f) * 100f:0.#}%{(definition.DurationKind == StatusDurationKind.Permanent ? "，持续至本场战斗结束。" : $"，持续 {definition.DurationTicks * BattleTiming.TickSeconds:0.##} 秒。")}",
        _ => definition.DisplayName
    };

    private static void ValidateEnum<T>(T value, string label, string name, ValidationReport report) where T : struct, Enum
    {
        if (!Enum.IsDefined(value)) report.Error($"{label}: {name} is invalid.");
    }

    private static void MergePrefixed(ValidationReport target, ValidationReport source, string prefix)
    {
        foreach (var error in source.CoreErrors) target.Error(prefix + error);
        foreach (var warning in source.Warnings) target.Warn(prefix + warning);
    }

    private static string ResourceLabel(StatusDefinition definition) =>
        string.IsNullOrWhiteSpace(definition.ResourcePath) ? $"status '{definition.StableId}'" : definition.ResourcePath;

    [GeneratedRegex("^[a-z0-9]+(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableIdRegex();

    [GeneratedRegex("^[a-z0-9]+(?:[._][a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex TagRegex();

    private sealed class StatusGraphCompiler
    {
        private readonly ValidationReport _report;
        private readonly bool _allowImplicitDependencies;
        private readonly Dictionary<StatusDefinition, StatusDefinition> _canonicalByResource =
            new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<string, StatusDefinition> _canonicalByPath = new(StringComparer.Ordinal);
        private readonly Dictionary<string, StatusDefinition> _canonicalByStableId = new(StringComparer.Ordinal);
        private readonly Dictionary<StatusDefinition, int> _states = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<StatusDefinition, CompiledStatusDefinition> _compiled =
            new(ReferenceEqualityComparer.Instance);
        private readonly List<StatusDefinition> _stack = [];
        private readonly List<StatusDefinition> _canonicalDefinitions = [];

        public StatusGraphCompiler(
            IEnumerable<StatusDefinition> authored,
            ValidationReport report,
            bool allowImplicitDependencies)
        {
            _report = report;
            _allowImplicitDependencies = allowImplicitDependencies;
            foreach (var definition in authored) Register(definition);
        }

        public IReadOnlyList<StatusDefinition> CanonicalDefinitions => _canonicalDefinitions;

        public CompiledStatusDefinition? Compile(StatusDefinition authored)
        {
            var canonical = ResolveCanonical(authored, allowRegistration: _allowImplicitDependencies);
            if (canonical is null)
            {
                _report.Error($"{ResourceLabel(authored)}: Status dependency is not registered in the authored graph.");
                return null;
            }
            if (_compiled.TryGetValue(canonical, out var existing)) return existing;
            if (_states.TryGetValue(canonical, out var state) && state == 1)
            {
                var start = _stack.IndexOf(canonical);
                var cycle = _stack.Skip(Math.Max(0, start)).Append(canonical).Select(ResourceLabel);
                _report.Error($"Status dependency cycle: {string.Join(" -> ", cycle)}");
                return null;
            }
            if (state == 2) return null;

            _states[canonical] = 1;
            _stack.Add(canonical);
            CompiledStatusDefinition? overflowTarget = null;
            if (canonical.OverflowStatus is not null)
            {
                var target = ResolveCanonical(canonical.OverflowStatus, allowRegistration: _allowImplicitDependencies);
                if (target is null)
                    _report.Error($"{ResourceLabel(canonical)}: overflow Status dependency is not registered: " +
                                  ResourceLabel(canonical.OverflowStatus));
                else
                    overflowTarget = Compile(target);
            }
            var compiled = CompileDefinition(canonical, overflowTarget, _report);
            _stack.RemoveAt(_stack.Count - 1);
            _states[canonical] = 2;
            if (compiled is not null) _compiled.Add(canonical, compiled);
            return compiled;
        }

        public CompiledStatusDefinition ResolveCompiled(StatusDefinition authored)
        {
            var canonical = ResolveCanonical(authored, allowRegistration: false) ??
                            throw new InvalidOperationException("Status definition is not registered.");
            return _compiled.TryGetValue(canonical, out var compiled)
                ? compiled
                : throw new InvalidOperationException("Status definition did not compile.");
        }

        private void Register(StatusDefinition authored)
        {
            if (_canonicalByResource.ContainsKey(authored)) return;
            StatusDefinition canonical;
            if (!string.IsNullOrWhiteSpace(authored.ResourcePath) &&
                _canonicalByPath.TryGetValue(authored.ResourcePath, out var byPath))
            {
                canonical = byPath;
                if (!string.Equals(canonical.StableId, authored.StableId, StringComparison.Ordinal))
                    _report.Error($"{authored.ResourcePath}: one resource path exposes conflicting Status stable ids.");
            }
            else
            {
                canonical = authored;
                _canonicalDefinitions.Add(canonical);
                if (!string.IsNullOrWhiteSpace(authored.ResourcePath))
                    _canonicalByPath.Add(authored.ResourcePath, canonical);
                if (!string.IsNullOrWhiteSpace(authored.StableId))
                {
                    if (_canonicalByStableId.TryGetValue(authored.StableId, out var collision) &&
                        !ReferenceEquals(collision, canonical))
                        _report.Error($"Duplicate status stable id across distinct resources: {authored.StableId} " +
                                      $"({ResourceLabel(collision)} | {ResourceLabel(authored)}).");
                    else
                        _canonicalByStableId[authored.StableId] = canonical;
                }
            }
            _canonicalByResource.Add(authored, canonical);
        }

        private StatusDefinition? ResolveCanonical(StatusDefinition authored, bool allowRegistration)
        {
            if (_canonicalByResource.TryGetValue(authored, out var byResource)) return byResource;
            if (!string.IsNullOrWhiteSpace(authored.ResourcePath) &&
                _canonicalByPath.TryGetValue(authored.ResourcePath, out var byPath))
            {
                _canonicalByResource[authored] = byPath;
                return byPath;
            }
            if (!allowRegistration) return null;
            Register(authored);
            return _canonicalByResource[authored];
        }
    }
}
