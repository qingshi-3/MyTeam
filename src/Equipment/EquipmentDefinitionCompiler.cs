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
            (authored.ReactiveStatusBindings is null || authored.ReactiveStatusBindings.Length == 0) &&
            (authored.TraitContributions is null || authored.TraitContributions.Length == 0))
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
            if (!slots.Add((result.Modifier.Attribute, result.Modifier.SlotId)))
                report.Error($"{label}: attribute modifier[{modifierIndex}] duplicates " +
                             $"({result.Modifier.Attribute}, {result.Modifier.SlotId}).");
            compiled.Add(result.Modifier);
        }

        var contributions = TraitDefinitionCompiler.CompileContributions(
            authored.TraitContributions ?? [],
            label);
        report.Merge(contributions.Report);
        var reactive = ImmutableArray.CreateBuilder<CompiledEquipmentReactiveStatusBinding>();
        var reactiveKeys = new HashSet<(BattleCombatEventKind EventKind, EquipmentReactiveStatusTarget Target,
            EquipmentReactiveStatusSource Source, string StatusId)>();
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
            if (!Enum.IsDefined(binding.EventKind))
                report.Error($"{bindingLabel}: event kind is invalid.");
            else if (!IsReactiveEvent(binding.EventKind))
                report.Error($"{bindingLabel}: event '{binding.EventKind}' is not an active-combat hook.");
            if (!Enum.IsDefined(binding.Target))
                report.Error($"{bindingLabel}: target policy is invalid.");
            if (!Enum.IsDefined(binding.Source))
                report.Error($"{bindingLabel}: source policy is invalid.");
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
            var key = (binding.EventKind, binding.Target, binding.Source, status.StableId);
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
                status));
        }
        if (report.HasCoreErrors) return null;
        var ordered = compiled.ToImmutable();
        var orderedReactive = reactive.ToImmutable();
        return new CompiledEquipmentDefinition(
            authored.StableId,
            authored.ResourcePath ?? string.Empty,
            ordered,
            orderedReactive,
            contributions.Contributions,
            Fingerprint(authored.StableId, ordered, orderedReactive, contributions.Contributions));
    }

    private static string Fingerprint(
        string stableId,
        IEnumerable<CompiledAttributeModifier> modifiers,
        IEnumerable<CompiledEquipmentReactiveStatusBinding> reactive,
        IEnumerable<CompiledTraitContribution> contributions)
    {
        var canonical = stableId + "|" + string.Join("|", modifiers.Select(modifier =>
            $"{modifier.Attribute}:{modifier.Operation}:{Magnitude(modifier.Magnitude)}:" +
            $"{modifier.Priority}:{modifier.SlotId}")) + "|reactive=" +
            string.Join("|", reactive.Select(binding =>
                $"{binding.EventKind}:{binding.Target}:{binding.Source}:{binding.Priority}:" +
                $"{binding.Status.StableId}:{binding.Status.ResourcePath}")) + "|traits=" +
            string.Join("|", contributions.Select(contribution =>
                $"{contribution.TraitId}:{contribution.Value}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static string Magnitude(CompiledAttributeMagnitude magnitude) => magnitude switch
    {
        CompiledConstantMagnitude constant =>
            $"constant:{constant.Value.ToString("R", CultureInfo.InvariantCulture)}:{constant.CaptureMode}",
        CompiledSourceAttributeMagnitude source => $"source:{source.Attribute}:{source.CaptureMode}",
        CompiledTargetAttributeMagnitude target => $"target:{target.Attribute}:{target.CaptureMode}",
        CompiledContextValueMagnitude context => $"context:{context.Key}:{context.CaptureMode}",
        CompiledTeamCountMagnitude count => $"count:{count.CountKind}:{count.Team}:{count.CaptureMode}",
        CompiledTraitValueMagnitude trait => $"trait:{trait.TraitId}:{trait.Team}:{trait.CaptureMode}",
        _ => throw new InvalidOperationException($"Unsupported Equipment magnitude: {magnitude.GetType().Name}")
    };

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
                    modifier.Magnitude is CompiledSourceAttributeMagnitude))
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
