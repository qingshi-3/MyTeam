using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TowerAutobattler.Attributes;
using TowerAutobattler.Content;

namespace TowerAutobattler.Traits;

public static partial class TraitDefinitionCompiler
{
    private static readonly Regex StableIdPattern = StableIdRegex();

    public static TraitCompilationResult Compile(TraitDefinition? authored)
    {
        var report = new ValidationReport();
        var definition = CompileInternal(authored, report, null);
        return new TraitCompilationResult(report.HasCoreErrors ? null : definition, report);
    }

    public static TraitBatchCompilationResult CompileBatch(IEnumerable<TraitDefinition?> authored)
    {
        ArgumentNullException.ThrowIfNull(authored);
        var report = new ValidationReport();
        var definitions = new List<CompiledTraitDefinition>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;
        foreach (var definition in authored)
        {
            var compiled = CompileInternal(definition, report, index++);
            if (compiled is null) continue;
            if (!ids.Add(compiled.StableId))
                report.Error($"Duplicate Trait stable id: {compiled.StableId}");
            else
                definitions.Add(compiled);
        }

        var validIds = definitions.Select(definition => definition.StableId).ToHashSet(StringComparer.Ordinal);
        foreach (var definition in definitions)
        foreach (var breakpoint in definition.Breakpoints)
        foreach (var dependency in breakpoint.AttributeModifiers.Select(modifier => modifier.Magnitude)
                     .OfType<CompiledTraitValueMagnitude>())
            if (!validIds.Contains(dependency.TraitId))
                report.Error($"{definition.ResourcePathOrLabel()}: breakpoint[{breakpoint.Index}] references " +
                             $"missing Trait dependency '{dependency.TraitId}'.");

        return new TraitBatchCompilationResult(
            report.HasCoreErrors
                ? []
                : definitions.OrderBy(definition => definition.StableId, StringComparer.Ordinal).ToImmutableArray(),
            report);
    }

    public static TraitContributionCompilationResult CompileContributions(
        IEnumerable<TraitContributionSpec?> authored,
        string ownerLabel)
    {
        ArgumentNullException.ThrowIfNull(authored);
        if (string.IsNullOrWhiteSpace(ownerLabel)) ownerLabel = "Trait contribution owner";
        var report = new ValidationReport();
        var contributions = ImmutableArray.CreateBuilder<CompiledTraitContribution>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;
        foreach (var contribution in authored)
        {
            var label = $"{ownerLabel}: Trait contribution[{index++}]";
            if (contribution is null)
            {
                report.Error($"{label} is missing.");
                continue;
            }
            var valid = true;
            if (string.IsNullOrWhiteSpace(contribution.TraitId))
            {
                report.Error($"{label} id is required.");
                valid = false;
            }
            else if (!StableIdPattern.IsMatch(contribution.TraitId))
            {
                report.Error($"{label} has invalid id '{contribution.TraitId}'.");
                valid = false;
            }
            else if (!ids.Add(contribution.TraitId))
            {
                report.Error($"{label} duplicates Trait '{contribution.TraitId}'.");
                valid = false;
            }
            if (contribution.Value <= 0)
            {
                report.Error($"{label} value must be positive.");
                valid = false;
            }
            if (valid)
                contributions.Add(new CompiledTraitContribution(contribution.TraitId, contribution.Value));
        }
        return new TraitContributionCompilationResult(
            report.HasCoreErrors ? [] : contributions.ToImmutable(),
            report);
    }

    private static CompiledTraitDefinition? CompileInternal(
        TraitDefinition? authored,
        ValidationReport report,
        int? index)
    {
        var label = authored is not null && !string.IsNullOrWhiteSpace(authored.ResourcePath)
            ? authored.ResourcePath
            : index is null ? "Trait definition" : $"Trait definition[{index}]";
        if (authored is null)
        {
            report.Error($"{label} is missing.");
            return null;
        }
        if (string.IsNullOrWhiteSpace(authored.StableId))
            report.Error($"{label}: stable id is required.");
        else if (!StableIdPattern.IsMatch(authored.StableId))
            report.Error($"{label}: invalid stable id '{authored.StableId}'.");
        if (string.IsNullOrWhiteSpace(authored.DisplayName))
            report.Error($"{label}: player-facing display name is required.");
        if (string.IsNullOrWhiteSpace(authored.SemanticIconKey))
            report.Error($"{label}: semantic icon key is required.");
        if (authored.CountingPolicy is null)
            report.Error($"{label}: counting policy is required.");
        else
        {
            if (!Enum.IsDefined(authored.CountingPolicy.DeploymentPolicy))
                report.Error($"{label}: deployment policy is invalid.");
            if (!Enum.IsDefined(authored.CountingPolicy.TemporaryUnitPolicy))
                report.Error($"{label}: temporary-unit policy is invalid.");
            if (!Enum.IsDefined(authored.CountingPolicy.DuplicateContentPolicy))
                report.Error($"{label}: duplicate-content policy is invalid.");
        }

        var breakpoints = ImmutableArray.CreateBuilder<CompiledTraitBreakpoint>();
        var previousMax = -1;
        var authoredBreakpoints = authored.Breakpoints ?? [];
        if (authoredBreakpoints.Length == 0)
            report.Error($"{label}: a concrete Trait definition must declare at least one breakpoint.");
        for (var breakpointIndex = 0; breakpointIndex < authoredBreakpoints.Length; breakpointIndex++)
        {
            var breakpoint = authoredBreakpoints[breakpointIndex];
            var breakpointLabel = $"{label}: breakpoint[{breakpointIndex}]";
            if (breakpoint is null)
            {
                report.Error($"{breakpointLabel} is missing.");
                continue;
            }
            if (breakpoint.MinValue < 0 || breakpoint.MaxValue < breakpoint.MinValue)
                report.Error($"{breakpointLabel} has an invalid inclusive range " +
                             $"{breakpoint.MinValue}..{breakpoint.MaxValue}.");
            if (breakpointIndex > 0 && breakpoint.MinValue <= previousMax)
                report.Error($"{breakpointLabel} overlaps or is out of order with the previous breakpoint.");
            previousMax = Math.Max(previousMax, breakpoint.MaxValue);
            if (string.IsNullOrWhiteSpace(breakpoint.DisplayStyle))
                report.Error($"{breakpointLabel} display style is required.");

            var modifiers = ImmutableArray.CreateBuilder<CompiledAttributeModifier>();
            var slots = new HashSet<(CombatAttribute Attribute, string SlotId)>();
            var authoredModifiers = breakpoint.AttributeModifiers ?? [];
            if (authoredModifiers.Length == 0)
                report.Error($"{breakpointLabel} must declare at least one Attribute modifier.");
            for (var modifierIndex = 0; modifierIndex < authoredModifiers.Length; modifierIndex++)
            {
                var result = AttributeDefinitionCompiler.Compile(authoredModifiers[modifierIndex]);
                foreach (var error in result.Report.CoreErrors)
                    report.Error($"{breakpointLabel}: modifier[{modifierIndex}]: {error}");
                foreach (var warning in result.Report.Warnings)
                    report.Warn($"{breakpointLabel}: modifier[{modifierIndex}]: {warning}");
                if (result.Modifier is null) continue;
                if (!slots.Add((result.Modifier.Attribute, result.Modifier.SlotId)))
                    report.Error($"{breakpointLabel}: modifier[{modifierIndex}] duplicates " +
                                 $"({result.Modifier.Attribute}, {result.Modifier.SlotId}).");
                modifiers.Add(result.Modifier);
            }
            var compiledModifiers = modifiers.ToImmutable();
            breakpoints.Add(new CompiledTraitBreakpoint(
                breakpointIndex,
                breakpoint.MinValue,
                breakpoint.MaxValue,
                breakpoint.DisplayStyle,
                compiledModifiers,
                BreakpointFingerprint(
                    breakpointIndex,
                    breakpoint.MinValue,
                    breakpoint.MaxValue,
                    breakpoint.DisplayStyle,
                    compiledModifiers)));
        }

        if (report.HasCoreErrors || authored.CountingPolicy is null) return null;
        var policy = new CompiledTraitCountingPolicy(
            authored.CountingPolicy.DeploymentPolicy,
            authored.CountingPolicy.TemporaryUnitPolicy,
            authored.CountingPolicy.DuplicateContentPolicy,
            authored.CountingPolicy.CountEquipment,
            authored.CountingPolicy.CountExplicitExtra);
        var compiledBreakpoints = breakpoints.ToImmutable();
        return new CompiledTraitDefinition(
            authored.StableId,
            authored.ResourcePath ?? string.Empty,
            authored.DisplayName,
            authored.SemanticIconKey,
            policy,
            compiledBreakpoints,
            DefinitionFingerprint(
                authored.StableId,
                authored.DisplayName,
                authored.SemanticIconKey,
                policy,
                compiledBreakpoints));
    }

    private static string DefinitionFingerprint(
        string stableId,
        string displayName,
        string semanticIconKey,
        CompiledTraitCountingPolicy policy,
        IEnumerable<CompiledTraitBreakpoint> breakpoints) => Hash(string.Join("|",
        stableId,
        displayName,
        semanticIconKey,
        policy.DeploymentPolicy,
        policy.TemporaryUnitPolicy,
        policy.DuplicateContentPolicy,
        policy.CountEquipment,
        policy.CountExplicitExtra,
        string.Join(";", breakpoints.Select(breakpoint => breakpoint.Fingerprint))));

    private static string BreakpointFingerprint(
        int index,
        int minValue,
        int maxValue,
        string displayStyle,
        IEnumerable<CompiledAttributeModifier> modifiers) => Hash(string.Join("|",
        index,
        minValue,
        maxValue,
        displayStyle,
        string.Join(";", modifiers.Select(modifier =>
            $"{modifier.Attribute}:{modifier.Operation}:{Magnitude(modifier.Magnitude)}:" +
            $"{modifier.Priority}:{modifier.SlotId}"))));

    private static string Magnitude(CompiledAttributeMagnitude magnitude) => magnitude switch
    {
        CompiledConstantMagnitude constant =>
            $"constant:{constant.Value.ToString("R", CultureInfo.InvariantCulture)}:{constant.CaptureMode}",
        CompiledSourceAttributeMagnitude source => $"source:{source.Attribute}:{source.CaptureMode}",
        CompiledTargetAttributeMagnitude target => $"target:{target.Attribute}:{target.CaptureMode}",
        CompiledContextValueMagnitude context => $"context:{context.Key}:{context.CaptureMode}",
        CompiledTeamCountMagnitude count => $"count:{count.CountKind}:{count.Team}:{count.CaptureMode}",
        CompiledTraitValueMagnitude trait => $"trait:{trait.TraitId}:{trait.Team}:{trait.CaptureMode}",
        _ => throw new InvalidOperationException($"Unsupported Trait magnitude: {magnitude.GetType().Name}")
    };

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string ResourcePathOrLabel(this CompiledTraitDefinition definition) =>
        string.IsNullOrWhiteSpace(definition.ResourcePath)
            ? $"Trait '{definition.StableId}'"
            : definition.ResourcePath;

    [GeneratedRegex("^[a-z0-9]+(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableIdRegex();
}
