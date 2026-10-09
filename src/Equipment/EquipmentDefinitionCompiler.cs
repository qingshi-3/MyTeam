using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Equipment;

public sealed record EquipmentCompilationResult(
    CompiledEquipmentDefinition? Definition,
    ValidationReport Report);

public sealed record EquipmentBatchCompilationResult(
    ImmutableArray<CompiledEquipmentDefinition> Definitions,
    ValidationReport Report);

public static partial class EquipmentDefinitionCompiler
{
    private static readonly Regex StableIdPattern = StableIdRegex();

    public static EquipmentCompilationResult Compile(
        EquipmentDefinition? authored,
        Func<StatusDefinition?, CompiledStatusDefinition?>? resolveStatus = null)
    {
        var report = new ValidationReport();
        var definition = CompileInternal(authored, report, null, resolveStatus);
        return new EquipmentCompilationResult(report.HasCoreErrors ? null : definition, report);
    }

    public static EquipmentBatchCompilationResult CompileBatch(
        IEnumerable<EquipmentDefinition?> authored,
        Func<StatusDefinition?, CompiledStatusDefinition?>? resolveStatus = null)
    {
        ArgumentNullException.ThrowIfNull(authored);
        var report = new ValidationReport();
        var definitions = new List<CompiledEquipmentDefinition>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;
        foreach (var definition in authored)
        {
            var compiled = CompileInternal(definition, report, index++, resolveStatus);
            if (compiled is null) continue;
            if (!ids.Add(compiled.StableId))
                report.Error($"Duplicate equipment stable id: {compiled.StableId}");
            else
                definitions.Add(compiled);
        }

        return new EquipmentBatchCompilationResult(
            report.HasCoreErrors
                ? []
                : definitions.OrderBy(definition => definition.StableId, StringComparer.Ordinal).ToImmutableArray(),
            report);
    }

    private static CompiledEquipmentDefinition? CompileInternal(
        EquipmentDefinition? authored,
        ValidationReport report,
        int? index,
        Func<StatusDefinition?, CompiledStatusDefinition?>? resolveStatus)
    {
        var label = authored is not null && !string.IsNullOrWhiteSpace(authored.ResourcePath)
            ? authored.ResourcePath
            : index is null ? "equipment definition" : $"equipment definition[{index}]";
        if (authored is null)
        {
            report.Error($"{label}: definition is missing.");
            return null;
        }
        if (string.IsNullOrWhiteSpace(authored.StableId))
            report.Error($"{label}: stable id is required.");
        else if (!StableIdPattern.IsMatch(authored.StableId))
            report.Error($"{label}: invalid stable id '{authored.StableId}'.");

        if ((authored.AttributeModifiers is null || authored.AttributeModifiers.Length == 0) &&
            (authored.UniqueAttributeModifiers is null || authored.UniqueAttributeModifiers.Length == 0) &&
            (authored.ReactiveStatusBindings is null || authored.ReactiveStatusBindings.Length == 0) &&
            (authored.TraitContributions is null || authored.TraitContributions.Length == 0) &&
            (authored.GrantedStatuses is null || authored.GrantedStatuses.Length == 0) && authored.ControlGuard is null && authored.LifeSteal is null)
            report.Error($"{label}: equipment must declare an Attribute modifier, reactive Status binding, or Trait contribution.");

        var compiled = ImmutableArray.CreateBuilder<CompiledAttributeModifier>();
        var slots = new HashSet<(CombatAttribute Attribute, string SlotId)>();
        var modifiers = authored.AttributeModifiers ?? [];
        for (var modifierIndex = 0; modifierIndex < modifiers.Length; modifierIndex++)
        {
            var result = AttributeDefinitionCompiler.Compile(modifiers[modifierIndex]);
            foreach (var error in result.Report.CoreErrors)
                report.Error($"{label}: attribute modifier[{modifierIndex}]: {error}");
            foreach (var warning in result.Report.Warnings)
                report.Warn($"{label}: attribute modifier[{modifierIndex}]: {warning}");
            if (result.Modifier is null) continue;
            AttributeMagnitudeSupport.Validate(result.Modifier.Magnitude,
                AttributeContextCapabilities.SourceAttribute | AttributeContextCapabilities.TargetAttribute,
                report, $"{label}: attribute modifier[{modifierIndex}]");
            if (!slots.Add((result.Modifier.Attribute, result.Modifier.SlotId)))
                report.Error($"{label}: attribute modifier[{modifierIndex}] duplicates " +
                             $"({result.Modifier.Attribute}, {result.Modifier.SlotId}).");
            compiled.Add(result.Modifier);
        }

        var unique = ImmutableArray.CreateBuilder<CompiledAttributeModifier>();
        foreach (var authoredModifier in authored.UniqueAttributeModifiers ?? [])
        {
            var result = AttributeDefinitionCompiler.Compile(authoredModifier);
            report.Merge(result.Report);
            if (result.Modifier is not { } modifier) continue;
            if (modifier.Magnitude is not CompiledConstantMagnitude)
                report.Error($"{label}: unique equipment modifiers require constant magnitudes.");
            if (!slots.Add((modifier.Attribute, modifier.SlotId)))
                report.Error($"{label}: duplicate regular/unique attribute slot '{modifier.SlotId}'.");
            unique.Add(modifier);
        }

        var contributions = TraitDefinitionCompiler.CompileContributions(
            authored.TraitContributions ?? [],
            label);
        report.Merge(contributions.Report);
        var reactive = ImmutableArray.CreateBuilder<CompiledEquipmentReactiveStatusBinding>();
        var reactiveKeys = new HashSet<(BattleCombatEventKind EventKind, EquipmentReactiveStatusTarget Target,
            EquipmentReactiveStatusSource Source, string StatusId, StatusReactiveOwnerRole OwnerRole)>();
        var authoredReactive = authored.ReactiveStatusBindings ?? [];
        for (var bindingIndex = 0; bindingIndex < authoredReactive.Length; bindingIndex++)
        {
            var binding = authoredReactive[bindingIndex];
            var bindingLabel = $"{label}: reactive Status binding[{bindingIndex}]";
            if (binding is null)
            {
                report.Error($"{bindingLabel} is missing.");
                continue;
            }
            if (binding.UniqueRuleId is null || binding.UniqueRuleId.Length > 0 && !StableIdPattern.IsMatch(binding.UniqueRuleId))
                report.Error($"{bindingLabel}: unique rule id is invalid.");
            if (!Enum.IsDefined(binding.EventKind))
                report.Error($"{bindingLabel}: event kind is invalid.");
            else if (!IsReactiveEvent(binding.EventKind))
                report.Error($"{bindingLabel}: event '{binding.EventKind}' is not an active-combat hook.");
            if (!Enum.IsDefined(binding.Target))
                report.Error($"{bindingLabel}: target policy is invalid.");
            if (!Enum.IsDefined(binding.Source))
                report.Error($"{bindingLabel}: source policy is invalid.");
            if (!Enum.IsDefined(binding.OwnerRole)) report.Error($"{bindingLabel}: owner event role is invalid.");
            if (binding.Status is null)
            {
                report.Error($"{bindingLabel}: Status is required.");
                continue;
            }
            var status = resolveStatus?.Invoke(binding.Status);
            if (status is null)
            {
                report.Error($"{bindingLabel}: Status dependency is not registered: " +
                             (string.IsNullOrWhiteSpace(binding.Status.ResourcePath)
                                 ? binding.Status.StableId
                                 : binding.Status.ResourcePath));
                continue;
            }
            if (binding.Source == EquipmentReactiveStatusSource.EquipmentInstance &&
                TryFindSourceAttributeStatus(status, out var sourceAttributeStatusId))
            {
                report.Error($"{bindingLabel}: Equipment-instance source cannot resolve source Attribute " +
                             $"modifiers in the reachable Status graph at '{sourceAttributeStatusId}'; " +
                             "use an owner source or a source-independent magnitude.");
                continue;
            }
            var key = (binding.EventKind, binding.Target, binding.Source, status.StableId, binding.OwnerRole);
            if (!reactiveKeys.Add(key))
            {
                report.Error($"{bindingLabel}: duplicate reactive Status binding for '{status.StableId}'.");
                continue;
            }
            reactive.Add(new CompiledEquipmentReactiveStatusBinding(
                binding.EventKind,
                binding.Target,
                binding.Source,
                binding.Priority,
                status, binding.OwnerRole, binding.UniqueRuleId ?? ""));
        }
        CompiledEquipmentLifeSteal? lifeSteal = null;
        if (authored.LifeSteal is { } steal)
        {
            if (string.IsNullOrWhiteSpace(steal.RuleId) || !StableIdPattern.IsMatch(steal.RuleId) ||
                !float.IsFinite(steal.Ratio) || steal.Ratio is <= 0 or > 1 ||
                !float.IsFinite(steal.AreaEfficiency) || steal.AreaEfficiency is <= 0 or > 1)
                report.Error($"{label}: life steal needs a stable rule id and finite ratios in (0,1].");
            else lifeSteal = new(steal.RuleId, steal.Ratio, steal.AreaEfficiency);
        }
        CompiledEquipmentControlGuard? guard = null;
        if (authored.ControlGuard is { } control)
        {
            if (string.IsNullOrWhiteSpace(control.RuleId) || !StableIdPattern.IsMatch(control.RuleId) ||
                control.WindowTicks is < 1 or > 3600 || !float.IsFinite(control.SubsequentDurationMultiplier) ||
                control.SubsequentDurationMultiplier is <= 0 or > 1)
                report.Error($"{label}: control guard requires a stable rule id, bounded positive window and duration multiplier in (0,1].");
            else guard = new(control.RuleId, control.WindowTicks, control.SubsequentDurationMultiplier);
        }
        var grants = StatusGrantCompiler.Compile(authored.GrantedStatuses ?? [], resolveStatus, report, label);
        if (report.HasCoreErrors) return null;
        var ordered = compiled.ToImmutable();
        var orderedReactive = reactive.ToImmutable();
        return new CompiledEquipmentDefinition(
            authored.StableId,
            authored.ResourcePath ?? string.Empty,
            ordered,
            orderedReactive,
            contributions.Contributions,
            Fingerprint(authored.StableId, ordered, orderedReactive, contributions.Contributions, grants, unique, guard, lifeSteal), grants, unique.ToImmutable(), guard, lifeSteal);
    }

    private static string Fingerprint(
        string stableId,
        IEnumerable<CompiledAttributeModifier> modifiers,
        IEnumerable<CompiledEquipmentReactiveStatusBinding> reactive,
        IEnumerable<CompiledTraitContribution> contributions,
        ImmutableArray<CompiledStatusDefinition> grants,
        IEnumerable<CompiledAttributeModifier> unique,
        CompiledEquipmentControlGuard? guard, CompiledEquipmentLifeSteal? lifeSteal)
    {
        var canonical = stableId + "|" + string.Join("|", modifiers.Select(modifier =>
            $"{modifier.Attribute}:{modifier.Operation}:{Magnitude(modifier.Magnitude)}:" +
            $"{modifier.Priority}:{modifier.SlotId}")) + "|reactive=" +
            string.Join("|", reactive.Select(binding =>
                $"{binding.EventKind}:{binding.Target}:{binding.Source}:{binding.Priority}:{binding.OwnerRole}:{binding.UniqueRuleId}:" +
                StatusDefinitionFingerprint.Compute(binding.Status))) + "|traits=" +
            string.Join("|", contributions.Select(contribution =>
                $"{contribution.TraitId}:{contribution.Value}")) + "|grants=" + string.Join("|", grants.Select(StatusDefinitionFingerprint.Compute)) + "|unique=" + string.Join("|", unique.Select(modifier =>
                $"{modifier.Attribute}:{modifier.Operation}:{Magnitude(modifier.Magnitude)}:{modifier.Priority}:{modifier.SlotId}"));
        if (guard is not null) canonical += $"|control-guard={guard.RuleId}:{guard.WindowTicks}:" +
            guard.SubsequentDurationMultiplier.ToString("R", CultureInfo.InvariantCulture);
        if (lifeSteal is not null) canonical += $"|life-steal={lifeSteal.RuleId}:" +
            lifeSteal.Ratio.ToString("R", CultureInfo.InvariantCulture) + ":" + lifeSteal.AreaEfficiency.ToString("R", CultureInfo.InvariantCulture);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static string Magnitude(CompiledAttributeMagnitude magnitude) => AttributeMagnitudeSupport.Fingerprint(magnitude);

    private static bool IsReactiveEvent(BattleCombatEventKind kind) => kind is
        BattleCombatEventKind.AttackDeclared or
        BattleCombatEventKind.AttackLanded or
        BattleCombatEventKind.AbilityResolved or
        BattleCombatEventKind.DamageResolved or
        BattleCombatEventKind.HealingResolved or
        BattleCombatEventKind.ShieldResolved or
        BattleCombatEventKind.UnitDefeated or
        BattleCombatEventKind.UnitKilled;

    private static bool TryFindSourceAttributeStatus(
        CompiledStatusDefinition root,
        out string statusId)
    {
        var pending = new Stack<CompiledStatusDefinition>();
        var visited = new HashSet<CompiledStatusDefinition>(ReferenceEqualityComparer.Instance);
        pending.Push(root);
        while (pending.TryPop(out var current))
        {
            if (!visited.Add(current)) continue;
            if (current.AttributeModifiers.Any(modifier =>
                    AttributeMagnitudeSupport.Leaves(modifier.Magnitude).Any(item => item is CompiledSourceAttributeMagnitude)))
            {
                statusId = current.StableId;
                return true;
            }
            var effects = current.LifecycleBindings.Select(item => item.Binding)
                .Concat(current.CombatReactiveBindings.Select(item => item.Binding));
            if (current.PeriodicEffect is not null) effects = effects.Append(current.PeriodicEffect);
            if (effects.Any(binding =>
                    binding.TargetQuery is CompiledSourceTargetQuery or CompiledFilteredTargetQuery { Anchor: EffectEntityReference.Source } ||
                    binding.Conditions.Any(condition => condition is CompiledEntityAliveCondition { Entity: EffectEntityReference.Source }
                        or CompiledHealthRatioCondition { Entity: EffectEntityReference.Source }
                        or CompiledEntityTagCondition { Entity: EffectEntityReference.Source }) ||
                    binding.Effects.Any(step => step.Magnitude is not null &&
                        AttributeMagnitudeSupport.Leaves(step.Magnitude).Any(item => item is CompiledSourceAttributeMagnitude))))
            {
                statusId = current.StableId;
                return true;
            }
            if (current.OverflowTransition is not null)
                pending.Push(current.OverflowTransition.Target);
        }
        statusId = string.Empty;
        return false;
    }

    [GeneratedRegex("^[a-z0-9]+(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableIdRegex();
}
