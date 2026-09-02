using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TowerAutobattler.Abilities;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;

namespace TowerAutobattler.TacticalCommands;

public sealed record TacticalCommandCompilationResult(
    CompiledTacticalCommandDefinition? Definition,
    ValidationReport Report);

public sealed record TacticalCommandBatchCompilationResult(
    ImmutableArray<CompiledTacticalCommandDefinition> Definitions,
    ValidationReport Report);

public static partial class TacticalCommandDefinitionCompiler
{
    private static readonly Regex StableIdPattern = StableIdRegex();

    public static TacticalCommandCompilationResult Compile(
        TacticalCommandDefinition? authored,
        Func<AbilityLoadoutDefinition?, CompiledAbilityLoadout?> resolveLoadout)
    {
        ArgumentNullException.ThrowIfNull(resolveLoadout);
        var report = new ValidationReport();
        var definition = CompileInternal(authored, resolveLoadout, report, null);
        return new TacticalCommandCompilationResult(report.HasCoreErrors ? null : definition, report);
    }

    public static TacticalCommandBatchCompilationResult CompileBatch(
        IEnumerable<TacticalCommandDefinition?> authored,
        Func<AbilityLoadoutDefinition?, CompiledAbilityLoadout?> resolveLoadout)
    {
        ArgumentNullException.ThrowIfNull(authored);
        ArgumentNullException.ThrowIfNull(resolveLoadout);
        var report = new ValidationReport();
        var definitions = new List<CompiledTacticalCommandDefinition>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;
        foreach (var definition in authored)
        {
            var compiled = CompileInternal(definition, resolveLoadout, report, index++);
            if (compiled is null) continue;
            if (!ids.Add(compiled.StableId))
                report.Error($"Duplicate tactical-command stable id: {compiled.StableId}");
            else
                definitions.Add(compiled);
        }

        return new TacticalCommandBatchCompilationResult(
            report.HasCoreErrors
                ? []
                : definitions.OrderBy(definition => definition.StableId, StringComparer.Ordinal).ToImmutableArray(),
            report);
    }

    private static CompiledTacticalCommandDefinition? CompileInternal(
        TacticalCommandDefinition? authored,
        Func<AbilityLoadoutDefinition?, CompiledAbilityLoadout?> resolveLoadout,
        ValidationReport report,
        int? index)
    {
        var label = authored is not null && !string.IsNullOrWhiteSpace(authored.ResourcePath)
            ? authored.ResourcePath
            : index is null ? "tactical-command definition" : $"tactical-command definition[{index}]";
        if (authored is null)
        {
            report.Error($"{label}: definition is missing.");
            return null;
        }
        if (string.IsNullOrWhiteSpace(authored.StableId))
            report.Error($"{label}: stable id is required.");
        else if (!StableIdPattern.IsMatch(authored.StableId))
            report.Error($"{label}: invalid stable id '{authored.StableId}'.");
        if (string.IsNullOrWhiteSpace(authored.DisplayName))
            report.Error($"{label}: display name is required.");
        if (authored.TacticalPointCost is < 1 or > 3)
            report.Error($"{label}: tactical-point cost must be within 1..3.");
        if (authored.AbilityLoadout is null)
            report.Error($"{label}: ability loadout is required.");
        if (string.IsNullOrWhiteSpace(authored.PrimaryAbilityId))
            report.Error($"{label}: primary ability id is required.");

        var loadout = authored.AbilityLoadout is null ? null : resolveLoadout(authored.AbilityLoadout);
        if (authored.AbilityLoadout is not null && loadout is null)
            report.Error($"{label}: ability loadout is not part of the canonical compiled graph.");
        var ability = loadout?.Find(authored.PrimaryAbilityId);
        if (loadout is not null && ability is null)
            report.Error($"{label}: primary ability '{authored.PrimaryAbilityId}' is missing from the compiled loadout.");
        else if (ability is not null &&
                 (ability.ActivationKind != AbilityActivationKind.ManualCommand ||
                  ability.Trigger != AbilityTriggerKind.None))
            report.Error($"{label}: tactical command must reference a manual, untriggered Ability.");

        if (report.HasCoreErrors || ability is null) return null;
        return new CompiledTacticalCommandDefinition(
            authored.StableId,
            authored.ResourcePath ?? string.Empty,
            authored.DisplayName,
            ability.Description,
            authored.TacticalPointCost,
            ability,
            Fingerprint(authored, ability));
    }

    private static string Fingerprint(
        TacticalCommandDefinition authored,
        CompiledAbilityDefinition ability)
    {
        var canonical = string.Join("|",
            authored.StableId,
            authored.DisplayName,
            authored.TacticalPointCost,
            authored.PrimaryAbilityId,
            Ability(ability));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static string Ability(CompiledAbilityDefinition ability) => string.Join(";",
        ability.StableId,
        ability.DisplayName,
        ability.Description,
        ability.ActivationKind,
        ability.Trigger,
        ability.ManaCost,
        ability.GoldCost,
        ability.CooldownTicks,
        ability.MaxUses,
        ability.IntervalTicks,
        string.Join("/", ability.Operations.Select(Operation)),
        ability.Presentation is null
            ? string.Empty
            : $"{ability.Presentation.SemanticIcon}:{ability.Presentation.Cue}:{ability.Presentation.ReportLabel}");

    private static string Operation(CompiledAbilityOperation operation) => operation switch
    {
        CompiledEffectAbilityOperation effect =>
            $"effect:{Binding(effect.Binding)}:{effect.InvocationValueSource}:" +
            effect.InvocationValueScale.ToString("R", CultureInfo.InvariantCulture),
        CompiledCooldownAbilityOperation cooldown =>
            $"cooldown:{Target(cooldown.TargetQuery)}:{cooldown.AttackAdjustment}:{cooldown.AttackValue}:" +
            $"{cooldown.MoveAdjustment}:{cooldown.MoveValue}",
        CompiledApplyStatusAbilityOperation status =>
            $"status:{status.Status.StableId}:{status.Status.ResourcePath}:{status.Status.Description}:" +
            $"{status.Status.DurationKind}:{status.Status.DurationTicks}:{status.Status.StackLimit}:" +
            $"{status.Status.Magnitude.ToString("R", CultureInfo.InvariantCulture)}:{Target(status.TargetQuery)}",
        CompiledSummonAbilityOperation summon =>
            $"summon:{summon.Profile}:{summon.Count}:" +
            $"{summon.HealthMultiplier.ToString("R", CultureInfo.InvariantCulture)}:" +
            $"{summon.DamageMultiplier.ToString("R", CultureInfo.InvariantCulture)}:" +
            $"{summon.MaximumLivingTemporaryUnits}:{summon.RequireAtLeastOne}:{summon.SummonContentId}",
        _ => throw new InvalidOperationException(
            $"Unsupported tactical-command Ability operation: {operation.GetType().Name}")
    };

    private static string Binding(CompiledEffectBinding binding) => string.Join(":",
        binding.StableId,
        binding.Priority,
        binding.Trigger.Kind,
        binding.Trigger.EventKind,
        string.Join(",", binding.Conditions.Select(condition => condition.ToString())),
        Target(binding.TargetQuery),
        string.Join(",", binding.Effects.Select(effect =>
            $"{effect.Kind}-{effect.AmountSource}-{effect.Amount.ToString("R", CultureInfo.InvariantCulture)}")),
        binding.Limits.MaxUses,
        binding.Limits.MinimumIntervalTicks,
        binding.Limits.MaxDepth,
        binding.Limits.MaxRepeatedEdges,
        binding.Presentation?.ToString() ?? string.Empty);

    private static string Target(CompiledEffectTargetQuery query) => query switch
    {
        CompiledExplicitTargetQuery => "explicit",
        CompiledSourceTargetQuery => "source",
        CompiledOwnerTargetQuery => "owner",
        CompiledRelativeTeamTargetQuery relative =>
            $"relative:{relative.Team}:{relative.IncludeDefeated}:{relative.RequiredTag}",
        _ => throw new InvalidOperationException(
            $"Unsupported tactical-command target query: {query.GetType().Name}")
    };

    [GeneratedRegex("^[a-z0-9]+(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableIdRegex();
}
