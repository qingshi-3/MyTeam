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
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Relics;

public sealed record RelicCompilationResult(
    CompiledRelicDefinition? Definition,
    ValidationReport Report);

public sealed record RelicBatchCompilationResult(
    ImmutableArray<CompiledRelicDefinition> Definitions,
    ValidationReport Report);

public static partial class RelicDefinitionCompiler
{
    private static readonly Regex StableIdPattern = StableIdRegex();

    public static RelicCompilationResult Compile(
        RelicDefinition? authored,
        IReadOnlySet<string>? validContentIds = null,
        Func<StatusDefinition?, CompiledStatusDefinition?>? resolveStatus = null)
    {
        var report = new ValidationReport();
        var definition = CompileInternal(authored, validContentIds, report, null, resolveStatus);
        return new RelicCompilationResult(report.HasCoreErrors ? null : definition, report);
    }

    public static RelicBatchCompilationResult CompileBatch(
        IEnumerable<RelicDefinition?> authored,
        IReadOnlySet<string>? validContentIds = null,
        Func<StatusDefinition?, CompiledStatusDefinition?>? resolveStatus = null)
    {
        ArgumentNullException.ThrowIfNull(authored);
        var report = new ValidationReport();
        var definitions = new List<CompiledRelicDefinition>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;
        foreach (var definition in authored)
        {
            var compiled = CompileInternal(definition, validContentIds, report, index++, resolveStatus);
            if (compiled is null) continue;
            if (!ids.Add(compiled.StableId))
                report.Error($"Duplicate relic stable id: {compiled.StableId}");
            else definitions.Add(compiled);
        }
        return new RelicBatchCompilationResult(
            report.HasCoreErrors
                ? []
                : definitions.OrderBy(definition => definition.StableId, StringComparer.Ordinal).ToImmutableArray(),
            report);
    }

    private static CompiledRelicDefinition? CompileInternal(
        RelicDefinition? authored,
        IReadOnlySet<string>? validContentIds,
        ValidationReport report,
        int? index,
        Func<StatusDefinition?, CompiledStatusDefinition?>? resolveStatus)
    {
        var label = authored is not null && !string.IsNullOrWhiteSpace(authored.ResourcePath)
            ? authored.ResourcePath
            : index is null ? "relic definition" : $"relic definition[{index}]";
        if (authored is null)
        {
            report.Error($"{label}: definition is missing.");
            return null;
        }
        if (string.IsNullOrWhiteSpace(authored.StableId))
            report.Error($"{label}: stable id is required.");
        else if (!StableIdPattern.IsMatch(authored.StableId))
            report.Error($"{label}: invalid stable id '{authored.StableId}'.");

        var bindingIds = new HashSet<string>(StringComparer.Ordinal);
        var attributeBindings = CompileAttributeBindings(authored.AttributeBindings, bindingIds, label, report);
        var battleStartEffects = CompileBattleStartEffects(
            authored.BattleStartEffects,
            bindingIds,
            validContentIds,
            label,
            report);
        var counters = CompileReactiveCounters(authored.ReactiveCounters, bindingIds, label, report);
        var statusGrants = ImmutableArray.CreateBuilder<CompiledRelicStatusGrant>();
        foreach (var grant in authored.StatusGrants ?? [])
        {
            if (grant is null) { report.Error($"{label}: missing Status grant."); continue; }
            if (!StableIdPattern.IsMatch(grant.BindingId) || !bindingIds.Add(grant.BindingId))
                report.Error($"{label}: invalid or duplicate Status grant binding id '{grant.BindingId}'.");
            var target = CompileUnitTarget(grant.Target);
            if (target is null) report.Error($"{label}: missing or unsupported Status grant target.");
            var statuses = StatusGrantCompiler.Compile([grant.Status], resolveStatus, report, label);
            if (target is not null && statuses.Length == 1)
                statusGrants.Add(new CompiledRelicStatusGrant(grant.BindingId, target, statuses[0]));
        }
        var battleModifiers = authored.BattleModifiers;
        if (battleModifiers is null)
        {
            report.Error($"{label}: Battle modifier collection is missing.");
            battleModifiers = [];
        }
        var victoryOutcomesAuthored = authored.VictoryOutcomes;
        if (victoryOutcomesAuthored is null)
        {
            report.Error($"{label}: Victory outcome collection is missing.");
            victoryOutcomesAuthored = [];
        }

        if (battleModifiers.Length > 0 &&
            (attributeBindings.Length > 0 || battleStartEffects.Length > 0))
            report.Error($"{label}: legacy battle modifiers cannot be mixed with typed Relic bindings.");

        var modifiers = ImmutableArray.CreateBuilder<CompiledRelicBattleModifier>();
        for (var modifierIndex = 0; modifierIndex < battleModifiers.Length; modifierIndex++)
        {
            var modifier = battleModifiers[modifierIndex];
            var modifierLabel = $"{label}: battle modifier[{modifierIndex}]";
            if (modifier is null)
            {
                report.Error($"{modifierLabel} is missing.");
                continue;
            }
            if (!Enum.IsDefined(modifier.Kind))
                report.Error($"{modifierLabel}: kind is invalid.");
            if (!float.IsFinite(modifier.Amount))
                report.Error($"{modifierLabel}: amount must be finite.");

            switch (modifier.Kind)
            {
                case RelicBattleModifierKind.ArmyHealthMultiplier:
                case RelicBattleModifierKind.ArmyDamageMultiplier:
                case RelicBattleModifierKind.HeroHealthMultiplier:
                case RelicBattleModifierKind.HeroDamageMultiplier:
                case RelicBattleModifierKind.FormationAdjacentDamageMultiplier:
                    if (modifier.Amount <= 0) report.Error($"{modifierLabel}: multiplier must be positive.");
                    if (!string.IsNullOrWhiteSpace(modifier.ContentId))
                        report.Error($"{modifierLabel}: multiplier cannot declare a content id.");
                    break;
                case RelicBattleModifierKind.SummonToken:
                    if (Math.Abs(modifier.Amount - 1f) > .0001f)
                        report.Error($"{modifierLabel}: summon token amount must be one.");
                    if (string.IsNullOrWhiteSpace(modifier.ContentId))
                        report.Error($"{modifierLabel}: summon content id is required.");
                    else if (validContentIds is not null && !validContentIds.Contains(modifier.ContentId))
                        report.Error($"{modifierLabel}: unknown summon content id '{modifier.ContentId}'.");
                    break;
                case RelicBattleModifierKind.StartBattleShield:
                case RelicBattleModifierKind.EmptySlotPower:
                    if (modifier.Amount < 0 || Math.Abs(modifier.Amount - MathF.Round(modifier.Amount)) > .0001f)
                        report.Error($"{modifierLabel}: amount must be a non-negative integer.");
                    if (!string.IsNullOrWhiteSpace(modifier.ContentId))
                        report.Error($"{modifierLabel}: numeric modifier cannot declare a content id.");
                    break;
                default:
                    if (modifier.Amount < 0) report.Error($"{modifierLabel}: amount cannot be negative.");
                    if (!string.IsNullOrWhiteSpace(modifier.ContentId))
                        report.Error($"{modifierLabel}: numeric modifier cannot declare a content id.");
                    break;
            }
            modifiers.Add(new CompiledRelicBattleModifier(modifier.Kind, modifier.Amount, modifier.ContentId));
        }

        var outcomes = ImmutableArray.CreateBuilder<CompiledRelicRunOutcome>();
        for (var outcomeIndex = 0; outcomeIndex < victoryOutcomesAuthored.Length; outcomeIndex++)
        {
            var outcome = victoryOutcomesAuthored[outcomeIndex];
            var outcomeLabel = $"{label}: victory outcome[{outcomeIndex}]";
            if (outcome is null)
            {
                report.Error($"{outcomeLabel} is missing.");
                continue;
            }
            if (!Enum.IsDefined(outcome.Kind)) report.Error($"{outcomeLabel}: kind is invalid.");
            if (outcome.Amount < 0) report.Error($"{outcomeLabel}: amount cannot be negative.");
            outcomes.Add(new CompiledRelicRunOutcome(outcome.Kind, outcome.Amount));
        }

        if (battleModifiers.Length == 0 && attributeBindings.Length == 0 &&
            battleStartEffects.Length == 0 && counters.Length == 0 && victoryOutcomesAuthored.Length == 0 && statusGrants.Count == 0)
            report.Error($"{label}: relic must declare at least one battle modifier or victory outcome.");
        if (report.HasCoreErrors) return null;
        var legacy = modifiers.ToImmutable();
        var victoryOutcomes = outcomes.ToImmutable();
        return new CompiledRelicDefinition(
            authored.StableId,
            authored.ResourcePath ?? string.Empty,
            attributeBindings,
            battleStartEffects,
            legacy,
            counters,
            victoryOutcomes,
            Fingerprint(authored.StableId, attributeBindings, battleStartEffects, legacy, counters, victoryOutcomes,
                statusGrants.ToImmutable()), statusGrants.ToImmutable());
    }

    private static ImmutableArray<CompiledRelicAttributeBinding> CompileAttributeBindings(
        RelicAttributeBindingSpec[]? authored,
        ISet<string> bindingIds,
        string label,
        ValidationReport report)
    {
        if (authored is null)
        {
            report.Error($"{label}: Attribute binding collection is missing.");
            return [];
        }
        var compiled = ImmutableArray.CreateBuilder<CompiledRelicAttributeBinding>();
        for (var index = 0; index < authored.Length; index++)
        {
            var binding = authored[index];
            var bindingLabel = $"{label}: Attribute binding[{index}]";
            if (binding is null)
            {
                report.Error($"{bindingLabel} is missing.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(binding.BindingId))
                report.Error($"{bindingLabel}: binding id is required.");
            else if (!StableIdPattern.IsMatch(binding.BindingId))
                report.Error($"{bindingLabel}: invalid binding id '{binding.BindingId}'.");
            else if (!bindingIds.Add(binding.BindingId))
                report.Error($"{bindingLabel}: duplicate binding id '{binding.BindingId}'.");
            if (!Enum.IsDefined(binding.StackPolicy))
                report.Error($"{bindingLabel}: stack policy is invalid.");

            var target = CompileUnitTarget(binding.Target);
            if (target is null)
                report.Error($"{bindingLabel}: unsupported or missing Relic unit target.");

            var modifier = AttributeDefinitionCompiler.Compile(binding.Modifier);
            foreach (var error in modifier.Report.CoreErrors)
                report.Error($"{bindingLabel}: {error}");
            foreach (var warning in modifier.Report.Warnings)
                report.Warn($"{bindingLabel}: {warning}");
            if (modifier.Modifier is not null)
                AttributeMagnitudeSupport.Validate(modifier.Modifier.Magnitude,
                    AttributeContextCapabilities.SourceAttribute | AttributeContextCapabilities.TargetAttribute | AttributeContextCapabilities.TeamCount,
                    report, bindingLabel);
            if (target is CompiledRelicPlayerEmptySlotHeroesTarget && modifier.Modifier is { } emptyModifier &&
                (emptyModifier.Magnitude is not CompiledConstantMagnitude ||
                 emptyModifier.Operation == AttributeModifierOperation.Override))
                report.Error($"{bindingLabel}: empty-slot target requires a constant additive or multiplicative modifier.");
            if (target is CompiledRelicPlayerEmptySlotHeroesTarget &&
                binding.StackPolicy != RelicAttributeStackPolicy.LinearAcrossStacksAndInstances)
                report.Error($"{bindingLabel}: empty-slot target requires linear stack/instance aggregation.");
            if (target is not CompiledRelicPlayerEmptySlotHeroesTarget &&
                binding.StackPolicy != RelicAttributeStackPolicy.PerStack)
                report.Error($"{bindingLabel}: linear stack/instance aggregation is only supported for empty-slot targets.");
            if (target is not null && modifier.Modifier is not null)
                compiled.Add(new CompiledRelicAttributeBinding(
                    binding.BindingId,
                    target,
                    binding.StackPolicy,
                    modifier.Modifier));
        }
        return compiled.ToImmutable();
    }

    private static ImmutableArray<CompiledRelicBattleStartEffect> CompileBattleStartEffects(
        RelicBattleStartEffectSpec[]? authored,
        ISet<string> bindingIds,
        IReadOnlySet<string>? validContentIds,
        string label,
        ValidationReport report)
    {
        if (authored is null)
        {
            report.Error($"{label}: Battle-start effect collection is missing.");
            return [];
        }
        var compiled = ImmutableArray.CreateBuilder<CompiledRelicBattleStartEffect>();
        for (var index = 0; index < authored.Length; index++)
        {
            var effect = authored[index];
            var effectLabel = $"{label}: Battle-start effect[{index}]";
            if (effect is null)
            {
                report.Error($"{effectLabel} is missing.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(effect.BindingId))
                report.Error($"{effectLabel}: binding id is required.");
            else if (!StableIdPattern.IsMatch(effect.BindingId))
                report.Error($"{effectLabel}: invalid binding id '{effect.BindingId}'.");
            else if (!bindingIds.Add(effect.BindingId))
                report.Error($"{effectLabel}: duplicate binding id '{effect.BindingId}'.");
            if (!Enum.IsDefined(effect.RepeatPolicy))
                report.Error($"{effectLabel}: repeat policy is invalid.");

            switch (effect)
            {
                case RelicBattleStartShieldSpec shield:
                    if (shield.Amount <= 0) report.Error($"{effectLabel}: shield amount must be positive.");
                    compiled.Add(new CompiledRelicBattleStartShield(
                        shield.BindingId,
                        shield.RepeatPolicy,
                        shield.Amount,
                        ManualShieldBinding(shield.BindingId, shield.Amount)));
                    break;
                case RelicBattleStartSummonSpec summon:
                    if (string.IsNullOrWhiteSpace(summon.ContentId))
                        report.Error($"{effectLabel}: summon content id is required.");
                    else if (validContentIds is not null && !validContentIds.Contains(summon.ContentId))
                        report.Error($"{effectLabel}: unknown summon content id '{summon.ContentId}'.");
                    if (!float.IsFinite(summon.HealthMultiplier) || summon.HealthMultiplier <= 0 ||
                        !float.IsFinite(summon.DamageMultiplier) || summon.DamageMultiplier <= 0)
                        report.Error($"{effectLabel}: summon multipliers must be finite and positive.");
                    compiled.Add(new CompiledRelicBattleStartSummon(
                        summon.BindingId,
                        summon.RepeatPolicy,
                        summon.ContentId,
                        summon.HealthMultiplier,
                        summon.DamageMultiplier));
                    break;
                default:
                    report.Error($"{effectLabel}: unsupported Battle-start effect '{effect.GetType().Name}'.");
                    break;
            }
        }
        return compiled.ToImmutable();
    }

    private static ImmutableArray<CompiledRelicReactiveCounter> CompileReactiveCounters(
        RelicReactiveCounterSpec[]? authored,
        ISet<string> bindingIds,
        string label,
        ValidationReport report)
    {
        if (authored is null)
        {
            report.Error($"{label}: Reactive counter collection is missing.");
            return [];
        }
        var compiled = ImmutableArray.CreateBuilder<CompiledRelicReactiveCounter>();
        for (var index = 0; index < authored.Length; index++)
        {
            var counter = authored[index];
            var counterLabel = $"{label}: Reactive counter[{index}]";
            if (counter is null)
            {
                report.Error($"{counterLabel} is missing.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(counter.CounterId))
                report.Error($"{counterLabel}: counter id is required.");
            else if (!StableIdPattern.IsMatch(counter.CounterId))
                report.Error($"{counterLabel}: invalid counter id '{counter.CounterId}'.");
            else if (!bindingIds.Add(counter.CounterId))
                report.Error($"{counterLabel}: counter id '{counter.CounterId}' conflicts with another Relic binding id.");
            if (!Enum.IsDefined(counter.Scope)) report.Error($"{counterLabel}: counter scope is invalid.");
            if (!Enum.IsDefined(counter.ResetPolicy)) report.Error($"{counterLabel}: reset policy is invalid.");
            if (!Enum.IsDefined(counter.Source)) report.Error($"{counterLabel}: counter source is invalid.");
            if (!Enum.IsDefined(counter.Target)) report.Error($"{counterLabel}: threshold target is invalid.");
            if (counter.Team is < 0 or > 1) report.Error($"{counterLabel}: source team must be zero or one.");
            if (counter.TargetTeam is < 0 or > 1) report.Error($"{counterLabel}: target team must be zero or one.");
            if (counter.Threshold <= 0) report.Error($"{counterLabel}: threshold must be positive.");
            if (counter.Consumption <= 0 || counter.Consumption > counter.Threshold)
                report.Error($"{counterLabel}: consumption must be positive and cannot exceed the threshold.");
            if (counter.Scope == RelicCounterScope.Battle && counter.ResetPolicy != RelicCounterResetPolicy.BattleEnd)
                report.Error($"{counterLabel}: Battle counter must reset at Battle end.");
            if (counter.Scope == RelicCounterScope.Run && counter.ResetPolicy != RelicCounterResetPolicy.RunEnd)
                report.Error($"{counterLabel}: Run counter must reset at Run end.");
            if ((counter.Source is RelicCounterSourceKind.Population or RelicCounterSourceKind.Alive) &&
                counter.Target != RelicThresholdTargetKind.FirstAliveTeamUnit)
                report.Error($"{counterLabel}: population/alive counters require a first-alive-team target.");

            var binding = EffectBindingCompiler.Compile(counter.ThresholdEffect);
            foreach (var error in binding.Report.CoreErrors)
                report.Error($"{counterLabel}: threshold effect: {error}");
            foreach (var warning in binding.Report.Warnings)
                report.Warn($"{counterLabel}: threshold effect: {warning}");
            if (binding.Binding is { } thresholdEffect &&
                (thresholdEffect.Trigger.Kind != EffectTriggerKind.Manual ||
                 thresholdEffect.TargetQuery is not CompiledExplicitTargetQuery))
                report.Error($"{counterLabel}: threshold effect must be manual with an explicit target query.");
            if (binding.Binding is null) continue;
            foreach (var step in binding.Binding.Effects)
                if (step.Magnitude is not null && AttributeMagnitudeSupport.Leaves(step.Magnitude).Any(item => item is CompiledSourceAttributeMagnitude))
                    report.Error($"{counterLabel}: Relic counters have no source AttributeSet; use target attributes or event magnitude.");
            if (binding.Binding.Conditions.Any(condition => condition is
                    CompiledEntityAliveCondition { Entity: EffectEntityReference.Source or EffectEntityReference.Owner } or
                    CompiledHealthRatioCondition { Entity: EffectEntityReference.Source or EffectEntityReference.Owner } or
                    CompiledEntityTagCondition { Entity: EffectEntityReference.Source or EffectEntityReference.Owner }))
                report.Error($"{counterLabel}: Relic counter source/owner is not a combat entity; conditions must inspect the explicit target.");
            var eventKind = Enum.IsDefined(counter.Source)
                ? EventKind(counter.Source)
                : BattleCombatEventKind.BattleStarted;
            compiled.Add(new CompiledRelicReactiveCounter(
                counter.CounterId,
                counter.Scope,
                counter.ResetPolicy,
                counter.Source,
                eventKind,
                counter.Team,
                counter.IncludeTemporary,
                counter.Threshold,
                counter.Consumption,
                counter.Priority,
                counter.Target,
                counter.TargetTeam,
                binding.Binding));
        }
        return compiled.ToImmutable();
    }

    private static BattleCombatEventKind EventKind(RelicCounterSourceKind source) => source switch
    {
        RelicCounterSourceKind.Population or RelicCounterSourceKind.Alive => BattleCombatEventKind.BattleStarted,
        RelicCounterSourceKind.Attack => BattleCombatEventKind.AttackLanded,
        RelicCounterSourceKind.Death => BattleCombatEventKind.UnitDefeated,
        _ => throw new ArgumentOutOfRangeException(nameof(source))
    };

    private static CompiledEffectBinding ManualShieldBinding(string bindingId, int amount) => new(
        bindingId,
        0,
        new CompiledEffectTrigger(EffectTriggerKind.Manual, EffectDomainEventKind.None),
        [],
        new CompiledExplicitTargetQuery(),
        [new CompiledEffectStep(EffectKind.Shield, EffectAmountSource.Fixed, amount)],
        new CompiledEffectBindingLimits(0, 0, 0, 0),
        null);

    private static string Fingerprint(
        string stableId,
        IEnumerable<CompiledRelicAttributeBinding> attributes,
        IEnumerable<CompiledRelicBattleStartEffect> starts,
        IEnumerable<CompiledRelicBattleModifier> legacy,
        IEnumerable<CompiledRelicReactiveCounter> counters,
        IEnumerable<CompiledRelicRunOutcome> outcomes,
        ImmutableArray<CompiledRelicStatusGrant> grants)
    {
        var canonical = new StringBuilder(stableId);
        foreach (var grant in grants)
            canonical.Append("|grant:").Append(grant.BindingId).Append(':').Append(grant.Target.GetType().Name)
                .Append(':').Append(StatusDefinitionFingerprint.Compute(grant.Status));
        foreach (var binding in attributes)
            canonical.Append("|attribute:").Append(binding.BindingId).Append(':')
                .Append(binding.Target.GetType().Name).Append(':').Append(binding.StackPolicy).Append(':')
                .Append(binding.Modifier.Attribute).Append(':')
                .Append(binding.Modifier.Operation).Append(':').Append(Magnitude(binding.Modifier.Magnitude)).Append(':')
                .Append(binding.Modifier.Priority).Append(':').Append(binding.Modifier.SlotId);
        foreach (var start in starts)
            canonical.Append(start switch
            {
                CompiledRelicBattleStartShield shield =>
                    $"|start-shield:{shield.BindingId}:{shield.RepeatPolicy}:{shield.Amount}",
                CompiledRelicBattleStartSummon summon =>
                    $"|start-summon:{summon.BindingId}:{summon.RepeatPolicy}:{summon.ContentId}:" +
                    $"{summon.HealthMultiplier.ToString("R", CultureInfo.InvariantCulture)}:" +
                    summon.DamageMultiplier.ToString("R", CultureInfo.InvariantCulture),
                _ => throw new InvalidOperationException($"Unsupported Relic Battle-start binding: {start.GetType().Name}")
            });
        foreach (var modifier in legacy)
            canonical.Append("|legacy:").Append(modifier.Kind).Append(':')
                .Append(modifier.Amount.ToString("R", CultureInfo.InvariantCulture)).Append(':').Append(modifier.ContentId);
        foreach (var counter in counters)
            canonical.Append("|counter:").Append(counter.CounterId).Append(':').Append(counter.Scope).Append(':')
                .Append(counter.ResetPolicy).Append(':').Append(counter.Source).Append(':').Append(counter.EventKind)
                .Append(':').Append(counter.Team).Append(':').Append(counter.IncludeTemporary).Append(':')
                .Append(counter.Threshold).Append(':').Append(counter.Consumption).Append(':').Append(counter.Priority)
                .Append(':').Append(counter.Target).Append(':').Append(counter.TargetTeam).Append(':')
                .Append(EffectModelText.BindingFingerprint(counter.ThresholdEffect));
        foreach (var outcome in outcomes)
            canonical.Append("|outcome:").Append(outcome.Kind).Append(':').Append(outcome.Amount);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
    }

    private static string Magnitude(CompiledAttributeMagnitude magnitude) => AttributeMagnitudeSupport.Fingerprint(magnitude);

    private static CompiledRelicUnitTarget? CompileUnitTarget(RelicUnitTargetSpec? target) => target switch
    {
        RelicPlayerArmyTargetSpec => new CompiledRelicPlayerArmyTarget(),
        RelicPlayerSummonsTargetSpec => new CompiledRelicPlayerSummonsTarget(),
        RelicPlayerHeroesTargetSpec => new CompiledRelicPlayerHeroesTarget(),
        RelicPlayerFormationAdjacentTargetSpec => new CompiledRelicPlayerFormationAdjacentTarget(),
        RelicPlayerEmptySlotHeroesTargetSpec => new CompiledRelicPlayerEmptySlotHeroesTarget(),
        _ => null
    };


    [GeneratedRegex("^[a-z0-9]+(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableIdRegex();
}
