using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Abilities;

public sealed record AbilityCompilationResult(
    CompiledAbilityDefinition? Ability,
    ValidationReport Report);

public sealed record AbilityLoadoutCompilationResult(
    CompiledAbilityLoadout? Loadout,
    ValidationReport Report);

public sealed record AbilityBatchCompilationResult(
    ImmutableArray<CompiledAbilityDefinition> Abilities,
    ValidationReport Report)
{
    internal ImmutableArray<CompiledAbilityLoadoutPublication> Loadouts { get; init; } = [];
}

internal sealed record CompiledAbilityLoadoutPublication(
    AbilityLoadoutDefinition Authored,
    CompiledAbilityLoadout Compiled);

public static partial class AbilityDefinitionCompiler
{
    private static readonly Regex StableIdPattern = StableIdRegex();

    public static AbilityCompilationResult Compile(AbilityDefinition? authored)
    {
        var report = new ValidationReport();
        var ability = CompileInternal(authored, report, null, null);
        return new AbilityCompilationResult(report.HasCoreErrors ? null : ability, report);
    }

    public static AbilityLoadoutCompilationResult CompileLoadout(
        AbilityLoadoutDefinition? authored,
        Func<StatusDefinition?, CompiledStatusDefinition?>? resolveStatus = null)
    {
        var report = new ValidationReport();
        if (authored is null)
        {
            report.Error("Ability loadout is missing.");
            return new AbilityLoadoutCompilationResult(null, report);
        }
        var abilities = new List<CompiledAbilityDefinition>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < authored.Abilities.Count; index++)
        {
            var ability = CompileInternal(authored.Abilities[index], report, index, resolveStatus);
            if (ability is null) continue;
            if (!ids.Add(ability.StableId))
            {
                report.Error($"Duplicate ability stable id in loadout: {ability.StableId}");
                continue;
            }
            abilities.Add(ability);
        }
        if (abilities.Count == 0) report.Error("Ability loadout must contain at least one ability.");
        return new AbilityLoadoutCompilationResult(
            report.HasCoreErrors
                ? null
                : new CompiledAbilityLoadout(abilities.OrderBy(ability => ability.StableId, StringComparer.Ordinal).ToImmutableArray()),
            report);
    }

    public static AbilityBatchCompilationResult CompileBatch(
        IEnumerable<AbilityLoadoutDefinition?> authoredLoadouts,
        Func<StatusDefinition?, CompiledStatusDefinition?>? resolveStatus = null)
    {
        ArgumentNullException.ThrowIfNull(authoredLoadouts);
        var report = new ValidationReport();
        var abilities = new List<CompiledAbilityDefinition>();
        var loadouts = new List<CompiledAbilityLoadoutPublication>();
        var compiledByResource = new Dictionary<AbilityDefinition, CompiledAbilityDefinition>(
            ReferenceEqualityComparer.Instance);
        var stableIdOwners = new Dictionary<string, AbilityDefinition>(StringComparer.Ordinal);
        var publishedLoadouts = new HashSet<AbilityLoadoutDefinition>(ReferenceEqualityComparer.Instance);
        foreach (var loadout in authoredLoadouts)
        {
            if (loadout is null)
            {
                report.Error("Ability loadout is missing.");
                continue;
            }
            if (!publishedLoadouts.Add(loadout)) continue;

            var loadoutReport = new ValidationReport();
            var loadoutAbilities = new List<CompiledAbilityDefinition>();
            var loadoutIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < loadout.Abilities.Count; index++)
            {
                var authoredAbility = loadout.Abilities[index];
                if (authoredAbility is null)
                {
                    loadoutReport.Error($"ability[{index}]: definition is missing.");
                    continue;
                }

                if (!compiledByResource.TryGetValue(authoredAbility, out var compiled))
                {
                    var abilityReport = new ValidationReport();
                    compiled = CompileInternal(authoredAbility, abilityReport, index, resolveStatus);
                    loadoutReport.Merge(abilityReport);
                    if (compiled is null) continue;
                    compiledByResource.Add(authoredAbility, compiled);
                }

                if (!loadoutIds.Add(compiled.StableId))
                {
                    loadoutReport.Error($"Duplicate ability stable id in loadout: {compiled.StableId}");
                    continue;
                }

                if (stableIdOwners.TryGetValue(compiled.StableId, out var owner) &&
                    !ReferenceEquals(owner, authoredAbility))
                {
                    loadoutReport.Error($"Duplicate ability stable id across distinct resources: {compiled.StableId}");
                    continue;
                }

                if (!stableIdOwners.ContainsKey(compiled.StableId))
                {
                    stableIdOwners.Add(compiled.StableId, authoredAbility);
                    abilities.Add(compiled);
                }
                loadoutAbilities.Add(compiled);
            }

            if (loadoutAbilities.Count == 0)
                loadoutReport.Error("Ability loadout must contain at least one ability.");
            if (!loadoutReport.HasCoreErrors)
                loadouts.Add(new CompiledAbilityLoadoutPublication(
                    loadout,
                    new CompiledAbilityLoadout(loadoutAbilities
                        .OrderBy(ability => ability.StableId, StringComparer.Ordinal)
                        .ToImmutableArray())));
            report.Merge(loadoutReport);
        }
        return new AbilityBatchCompilationResult(
            report.HasCoreErrors ? [] : abilities.OrderBy(ability => ability.StableId, StringComparer.Ordinal).ToImmutableArray(),
            report)
        {
            Loadouts = report.HasCoreErrors ? [] : loadouts.ToImmutableArray()
        };
    }

    private static CompiledAbilityDefinition? CompileInternal(
        AbilityDefinition? authored,
        ValidationReport report,
        int? index,
        Func<StatusDefinition?, CompiledStatusDefinition?>? resolveStatus)
    {
        var label = authored is not null && !string.IsNullOrWhiteSpace(authored.ResourcePath)
            ? authored.ResourcePath
            : index is null ? "ability definition" : $"ability[{index}]";
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
        if (!Enum.IsDefined(authored.ActivationKind))
            report.Error($"{label}: activation kind is invalid.");
        if (!Enum.IsDefined(authored.Trigger))
            report.Error($"{label}: trigger kind is invalid.");
        if (authored.ManaCost < 0 || authored.GoldCost < 0 || authored.CooldownTicks < 0 || authored.MaxUses < 0)
            report.Error($"{label}: costs, cooldown, and maximum uses cannot be negative.");
        ValidateEntryContract(authored, label, report);

        var operations = ImmutableArray.CreateBuilder<CompiledAbilityOperation>();
        if (authored.Operations is null || authored.Operations.Count == 0)
            report.Error($"{label}: at least one operation is required.");
        else
            for (var operationIndex = 0; operationIndex < authored.Operations.Count; operationIndex++)
            {
                var operation = CompileOperation(
                    authored.Operations[operationIndex], label, operationIndex, report, resolveStatus);
                if (operation is not null) operations.Add(operation);
            }
        ValidateAtomicOperationShape(operations, label, report);

        if (report.HasCoreErrors) return null;
        var presentation = authored.Presentation is null
            ? null
            : new CompiledAbilityPresentation(
                authored.Presentation.SemanticIcon.ToString(),
                authored.Presentation.Cue.ToString(),
                authored.Presentation.ReportLabel);
        var provisional = new CompiledAbilityDefinition(
            authored.StableId,
            authored.DisplayName,
            string.Empty,
            authored.ActivationKind,
            authored.Trigger,
            authored.ManaCost,
            authored.GoldCost,
            authored.CooldownTicks,
            authored.MaxUses,
            authored.IntervalTicks,
            operations.ToImmutable(),
            presentation);
        return provisional with { Description = AbilityDescriptionRenderer.Describe(provisional) };
    }

    private static void ValidateEntryContract(AbilityDefinition authored, string label, ValidationReport report)
    {
        switch (authored.ActivationKind)
        {
            case AbilityActivationKind.ManualCommand:
                if (authored.Trigger != AbilityTriggerKind.None)
                    report.Error($"{label}: manual ability cannot declare a trigger.");
                if (authored.ManaCost <= 0)
                    report.Error($"{label}: manual command mana cost must be positive.");
                if (authored.IntervalTicks != 0)
                    report.Error($"{label}: manual ability cannot declare an interval.");
                break;
            case AbilityActivationKind.Automatic:
                if (authored.Trigger is not (AbilityTriggerKind.BattleStarted or AbilityTriggerKind.PeriodicTick))
                    report.Error($"{label}: automatic ability requires battle-start or periodic trigger.");
                if (authored.Trigger == AbilityTriggerKind.PeriodicTick && authored.IntervalTicks <= 0)
                    report.Error($"{label}: periodic automatic ability requires a positive interval.");
                if (authored.Trigger == AbilityTriggerKind.BattleStarted && authored.IntervalTicks != 0)
                    report.Error($"{label}: battle-start automatic ability cannot declare an interval.");
                RejectNonManualCosts(authored, label, report);
                break;
            case AbilityActivationKind.Triggered:
                if (authored.Trigger is AbilityTriggerKind.None or AbilityTriggerKind.PeriodicTick or AbilityTriggerKind.BattleStarted)
                    report.Error($"{label}: triggered ability requires a supported domain trigger.");
                if (authored.IntervalTicks != 0)
                    report.Error($"{label}: triggered ability cannot declare an interval.");
                RejectNonManualCosts(authored, label, report);
                break;
            case AbilityActivationKind.Passive:
                if (authored.Trigger != AbilityTriggerKind.None || authored.IntervalTicks != 0)
                    report.Error($"{label}: passive ability cannot declare a trigger or interval.");
                RejectNonManualCosts(authored, label, report);
                break;
        }
    }

    private static void RejectNonManualCosts(AbilityDefinition authored, string label, ValidationReport report)
    {
        if (authored.ManaCost != 0 || authored.GoldCost != 0)
            report.Error($"{label}: non-manual ability cannot consume command resources.");
    }

    private static void ValidateAtomicOperationShape(
        ImmutableArray<CompiledAbilityOperation>.Builder operations,
        string label,
        ValidationReport report)
    {
        var effects = operations.Select((operation, index) => (operation, index))
            .Where(item => item.operation is CompiledEffectAbilityOperation)
            .ToArray();
        if (effects.Length > 1)
            report.Error($"{label}: one activation cannot contain multiple effect-kernel operations until batch commit is transactional.");
        if (effects.FirstOrDefault().operation is CompiledEffectAbilityOperation effect)
        {
            if (effects[0].index != 0)
                report.Error($"{label}: the effect-kernel operation must be first so all fallible work is resolved before other mutations.");
            if (effect.Binding.Effects.Length != 1)
                report.Error($"{label}: an ability effect binding must contain exactly one atomic effect step.");
            else if (effect.Binding.Effects[0].Kind == EffectKind.Damage)
                report.Error($"{label}: damage effects cannot enter an atomic ability transaction until death-chain rollback is authoritative.");
        }
    }

    private static CompiledAbilityOperation? CompileOperation(
        AbilityOperationSpec? authored,
        string label,
        int index,
        ValidationReport report,
        Func<StatusDefinition?, CompiledStatusDefinition?>? resolveStatus)
    {
        var operationLabel = $"{label}: operation[{index}]";
        switch (authored)
        {
            case EffectAbilityOperationSpec effect:
            {
                var compiled = EffectBindingCompiler.Compile(effect.Binding);
                report.Merge(compiled.Report);
                if (!Enum.IsDefined(effect.InvocationValueSource))
                    report.Error($"{operationLabel}: invocation value source is invalid.");
                if (!float.IsFinite(effect.InvocationValueScale) || effect.InvocationValueScale < 0)
                    report.Error($"{operationLabel}: invocation value scale must be finite and non-negative.");
                if (compiled.Binding is not null && compiled.Binding.Trigger.Kind != EffectTriggerKind.Manual)
                    report.Error($"{operationLabel}: ability effect binding must use a manual trigger.");
                return compiled.Binding is null
                    ? null
                    : new CompiledEffectAbilityOperation(compiled.Binding, effect.InvocationValueSource, effect.InvocationValueScale);
            }
            case CooldownAbilityOperationSpec cooldown:
            {
                var target = CompileTarget(cooldown.TargetQuery, operationLabel, report);
                ValidateCooldown(cooldown.AttackAdjustment, cooldown.AttackValue, "attack", operationLabel, report);
                ValidateCooldown(cooldown.MoveAdjustment, cooldown.MoveValue, "move", operationLabel, report);
                if (cooldown.AttackAdjustment == CooldownAdjustmentKind.None && cooldown.MoveAdjustment == CooldownAdjustmentKind.None)
                    report.Error($"{operationLabel}: at least one cooldown adjustment is required.");
                return target is null ? null : new CompiledCooldownAbilityOperation(
                    target,
                    cooldown.AttackAdjustment,
                    cooldown.AttackValue,
                    cooldown.MoveAdjustment,
                    cooldown.MoveValue);
            }
            case ApplyStatusAbilityOperationSpec status:
            {
                CompiledStatusDefinition? compiledStatus;
                if (resolveStatus is null)
                {
                    var compilation = StatusDefinitionCompiler.Compile(status.Status);
                    report.Merge(compilation.Report);
                    compiledStatus = compilation.Definition;
                }
                else
                {
                    compiledStatus = resolveStatus(status.Status);
                    if (compiledStatus is null)
                        report.Error($"{operationLabel}: status is not part of the compiled publication graph.");
                }
                var target = CompileTarget(status.TargetQuery, operationLabel, report);
                return compiledStatus is null || target is null
                    ? null
                    : new CompiledApplyStatusAbilityOperation(compiledStatus, target);
            }
            case SummonAbilityOperationSpec summon:
                if (!Enum.IsDefined(summon.Profile)) report.Error($"{operationLabel}: summon profile is invalid.");
                if (summon.Count <= 0) report.Error($"{operationLabel}: summon count must be positive.");
                if (!float.IsFinite(summon.HealthMultiplier) || summon.HealthMultiplier <= 0 ||
                    !float.IsFinite(summon.DamageMultiplier) || summon.DamageMultiplier <= 0)
                    report.Error($"{operationLabel}: summon multipliers must be finite and positive.");
                if (summon.MaximumLivingTemporaryUnits < 0)
                    report.Error($"{operationLabel}: living summon limit cannot be negative.");
                if (summon.Profile == AbilitySummonProfile.BehaviorSummon && string.IsNullOrWhiteSpace(summon.SummonContentId))
                    report.Error($"{operationLabel}: behavior summon content id is required.");
                return new CompiledSummonAbilityOperation(
                    summon.Profile,
                    summon.Count,
                    summon.HealthMultiplier,
                    summon.DamageMultiplier,
                    summon.MaximumLivingTemporaryUnits,
                    summon.RequireAtLeastOne,
                    summon.SummonContentId);
            case null:
                report.Error($"{operationLabel}: operation is missing.");
                return null;
            default:
                report.Error($"{operationLabel}: unsupported operation type '{authored.GetType().Name}'.");
                return null;
        }
    }

    private static void ValidateCooldown(
        CooldownAdjustmentKind kind,
        int value,
        string channel,
        string label,
        ValidationReport report)
    {
        if (!Enum.IsDefined(kind))
        {
            report.Error($"{label}: {channel} cooldown adjustment is invalid.");
            return;
        }
        if (kind is CooldownAdjustmentKind.Add or CooldownAdjustmentKind.Cap or CooldownAdjustmentKind.Divide)
        {
            if (value <= 0) report.Error($"{label}: {channel} cooldown adjustment value must be positive.");
        }
        else if (value != 0)
            report.Error($"{label}: {channel} cooldown value must be zero for {kind}.");
    }

    internal static CompiledEffectTargetQuery? CompileTarget(
        EffectTargetQuerySpec? authored,
        string label,
        ValidationReport report) => authored switch
    {
        ExplicitTargetQuerySpec => new CompiledExplicitTargetQuery(),
        SourceTargetQuerySpec => new CompiledSourceTargetQuery(),
        OwnerTargetQuerySpec => new CompiledOwnerTargetQuery(),
        RelativeTeamTargetQuerySpec relative when Enum.IsDefined(relative.Team) =>
            new CompiledRelativeTeamTargetQuery(relative.Team, relative.IncludeDefeated, relative.RequiredTag.ToString()),
        RelativeTeamTargetQuerySpec => InvalidTarget(label, "relative-team target has an invalid team relation", report),
        null => InvalidTarget(label, "target query is required", report),
        _ => InvalidTarget(label, $"unsupported target query '{authored.GetType().Name}'", report)
    };

    private static CompiledEffectTargetQuery? InvalidTarget(string label, string message, ValidationReport report)
    {
        report.Error($"{label}: {message}.");
        return null;
    }

    [GeneratedRegex("^[a-z0-9]+(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableIdRegex();
}

public static class AbilityDescriptionRenderer
{
    public static string Describe(CompiledAbilityDefinition ability)
    {
        var operations = ability.Operations;
        if (operations.Length == 1 && operations[0] is CompiledSummonAbilityOperation summon)
            return DescribeSummon(summon);
        if (operations.Length == 1 && operations[0] is CompiledEffectAbilityOperation effect)
            return DescribeEffect(effect);
        if (operations.Length == 1 && operations[0] is CompiledCooldownAbilityOperation cooldown)
            return DescribeCooldown(cooldown);

        var parts = operations.Select(operation => operation switch
        {
            CompiledEffectAbilityOperation effect => DescribeEffect(effect).TrimEnd('。'),
            CompiledCooldownAbilityOperation cooldown => DescribeCooldown(cooldown).TrimEnd('。'),
            CompiledApplyStatusAbilityOperation status => DescribeStatus(status).TrimEnd('。'),
            CompiledSummonAbilityOperation summon => DescribeSummon(summon).TrimEnd('。'),
            _ => string.Empty
        }).Where(part => !string.IsNullOrWhiteSpace(part)).ToArray();
        return parts.Length == 0 ? ability.DisplayName : string.Join("，", parts) + "。";
    }

    private static string DescribeEffect(CompiledEffectAbilityOperation operation)
    {
        var step = operation.Binding.Effects.First();
        var target = operation.Binding.TargetQuery;
        var amount = step.Amount;
        if (operation.InvocationValueSource == AbilityInvocationValueSource.OwnerMaxHealth)
            amount *= operation.InvocationValueScale;
        return (step.Kind, target, operation.InvocationValueSource) switch
        {
            (EffectKind.Shield, CompiledRelativeTeamTargetQuery { Team: EffectRelativeTeam.Allies }, _) =>
                $"全体友军获得 {amount:0.##} 点护盾。",
            (EffectKind.Heal, CompiledOwnerTargetQuery, AbilityInvocationValueSource.OwnerMaxHealth) =>
                $"英雄恢复最大生命的 {amount * 100f:0.#}%。",
            (EffectKind.Shield, CompiledOwnerTargetQuery, AbilityInvocationValueSource.OwnerMaxHealth) =>
                $"英雄获得相当于最大生命 {amount * 100f:0.#}% 的护盾。",
            _ => $"施放 {operation.Binding.Presentation?.DisplayName ?? operation.Binding.StableId}。"
        };
    }

    private static string DescribeCooldown(CompiledCooldownAbilityOperation operation)
    {
        if (operation.TargetQuery is CompiledRelativeTeamTargetQuery { Team: EffectRelativeTeam.Allies } &&
            operation.AttackAdjustment == CooldownAdjustmentKind.Reset &&
            operation.MoveAdjustment == CooldownAdjustmentKind.Reset)
            return "清零全体友军的攻击与移动等待。";
        if (operation.TargetQuery is CompiledRelativeTeamTargetQuery { Team: EffectRelativeTeam.Allies } &&
            operation.AttackAdjustment == CooldownAdjustmentKind.Cap)
            return $"攻击等待最多缩短至 {operation.AttackValue * BattleTiming.TickSeconds:0.##} 秒。";
        if (operation.TargetQuery is CompiledRelativeTeamTargetQuery { Team: EffectRelativeTeam.Allies } &&
            operation.AttackAdjustment == CooldownAdjustmentKind.Divide)
            return $"友军攻击等待缩短为原来的 1/{operation.AttackValue}。";
        if (operation.TargetQuery is CompiledOwnerTargetQuery &&
            operation.AttackAdjustment == CooldownAdjustmentKind.Reset)
            return "清零攻击等待。";
        return "调整行动等待。";
    }

    private static string DescribeStatus(CompiledApplyStatusAbilityOperation operation)
    {
        if (operation.Status.Behavior == StatusBehaviorKind.DisableActions &&
            operation.TargetQuery is CompiledRelativeTeamTargetQuery { Team: EffectRelativeTeam.Enemies })
            return $"敌军禁用 {operation.Status.DurationTicks * BattleTiming.TickSeconds:0.##} 秒。";
        if (operation.Status.Behavior == StatusBehaviorKind.DamageMultiplier)
        {
            var increase = (operation.Status.Magnitude - 1f) * 100f;
            if (operation.TargetQuery is CompiledRelativeTeamTargetQuery { Team: EffectRelativeTeam.Allies } relative)
                return $"所有{DescribeTag(relative.RequiredTag)}友军在本场战斗中伤害提高 {increase:0.#}%。";
            if (operation.TargetQuery is CompiledOwnerTargetQuery)
                return $"伤害提高 {increase:0.#}%。";
        }
        return operation.Status.Description;
    }

    private static string DescribeSummon(CompiledSummonAbilityOperation operation) => operation.Profile switch
    {
        AbilitySummonProfile.DeathSummon =>
            $"在英雄附近最多召唤 {operation.Count} 名临时骸骨，生命为 {operation.HealthMultiplier * 100f:0.#}%，伤害为 {operation.DamageMultiplier * 100f:0.#}%。",
        AbilitySummonProfile.Mercenary =>
            $"在英雄附近召唤一名临时雇佣兵，生命为 {operation.HealthMultiplier * 100f:0.#}%，伤害为 {operation.DamageMultiplier * 100f:0.#}%。",
        _ => $"周期召唤 {operation.Count} 名临时单位。"
    };

    private static string DescribeTag(string tag) => tag switch
    {
        "beast" => "野兽",
        "machine" => "机械",
        "undead" => "亡灵",
        "frost" => "霜寒",
        "desert" => "沙海",
        "order" => "秩序",
        "" => string.Empty,
        _ => $"具备「{tag}」标签的"
    };
}
