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
        ValidateTechniqueLoadout(abilities, report);
        if (abilities.Count(ability => ability.Trigger == AbilityTriggerKind.ManaFull) > 1)
            report.Error("Ability loadout may contain only one mana-full skill.");
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
            ValidateTechniqueLoadout(loadoutAbilities, loadoutReport);
            if (loadoutAbilities.Count(ability => ability.Trigger == AbilityTriggerKind.ManaFull) > 1)
                loadoutReport.Error("Ability loadout may contain only one mana-full skill.");
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
        if (!Enum.IsDefined(authored.AutomaticTarget))
            report.Error($"{label}: automatic target kind is invalid.");
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
        foreach (var value in operations.OfType<CompiledBattleValueOperation>())
            if (authored.ActivationKind != AbilityActivationKind.Triggered && value.Terms.Any(t =>
                t.Subject is BattleValueSubject.EventSource or BattleValueSubject.EventTarget ||
                t.Metric is BattleValueMetric.EventEffective or BattleValueMetric.EventOverheal or BattleValueMetric.EventOrdinal))
                report.Error($"{label}: event-based values require an event-triggered ability.");
        ValidateAtomicOperationShape(operations, label, report);
        foreach (var technique in operations.OfType<CompiledCombatTechniqueOperation>())
        {
            var expected = technique switch {
                CompiledCounterattackOperation => AbilityTriggerKind.ReceivedAttack,
                CompiledGritStorageOperation => AbilityTriggerKind.HealthDamaged,
                CompiledGritPunchOperation => AbilityTriggerKind.ActionQueued,
                _ => AbilityTriggerKind.ManaFull };
            if (operations.Count != 1 || authored.Echoable || authored.Trigger != expected ||
                authored.ActivationKind != (expected is AbilityTriggerKind.ManaFull or AbilityTriggerKind.ActionQueued ? AbilityActivationKind.Automatic : AbilityActivationKind.Triggered))
                report.Error($"{label}: combat technique requires its single matching automatic/event operation and cannot be echoed.");
        }
        if (authored.Trigger == AbilityTriggerKind.ActionQueued && !operations.Any(o => o is CompiledGritPunchOperation))
            report.Error($"{label}: queued automatic actions require a supported condition producer.");
        if (operations.Any(operation => operation is CompiledChargedLineOperation or CompiledEnemyAction) &&
            (operations.Count != 1 || authored.ActivationKind != AbilityActivationKind.Automatic ||
             authored.Trigger != AbilityTriggerKind.PeriodicTick || authored.AutomaticTarget != AbilityAutomaticTargetKind.CurrentEnemy))
            report.Error($"{label}: a charged line must be the sole operation of an automatic periodic enemy-targeting ability.");
        if (operations.Any(operation => operation is CompiledTrampleOperation) &&
            (operations.Count != 1 || authored.ActivationKind != AbilityActivationKind.Automatic ||
             authored.Trigger is not (AbilityTriggerKind.PeriodicTick or AbilityTriggerKind.ManaFull) ||
             authored.AutomaticTarget != AbilityAutomaticTargetKind.CurrentEnemy))
            report.Error($"{label}: trample must be the sole operation of an automatic periodic or mana-full enemy-targeting ability.");
        if (operations.Any(operation => operation is CompiledDisplacementOperation) &&
            (authored.ActivationKind != AbilityActivationKind.Automatic || authored.Trigger != AbilityTriggerKind.ManaFull))
            report.Error($"{label}: displacement is supported only by an automatic mana-full skill.");
        if (authored.Echoable && (authored.Trigger != AbilityTriggerKind.OwnerDefeated ||
            operations.Any(o => o is CompiledLifecycleOperation or CompiledEchoOperation or CompiledDisplacementOperation ||
                o is CompiledBattleValueOperation { Action: BattleValueAction.AddCounter or BattleValueAction.SetCounter })))
            report.Error($"{label}: echoable death effects cannot copy lifecycle, echo, displacement or counter operations.");
        if (authored.ActivationKind == AbilityActivationKind.Passive)
        {
            foreach (var operation in operations)
                if (operation is not CompiledApplyStatusAbilityOperation passive || !StatusGrantCompiler.IsSupported(passive.Status))
                    report.Error($"{label}: passive abilities grant permanent, single-stack, source-isolated, non-dispellable statuses without overflow; one-shot effects use BattleStarted.");
        }

        if (report.HasCoreErrors) return null;
        var presentation = authored.Presentation is null
            ? null
            : new CompiledAbilityPresentation(
                authored.Presentation.SemanticIcon.ToString(),
                authored.Presentation.Cue.ToString(),
                authored.Presentation.ReportLabel,
                authored.Presentation.DamageVfx,
                authored.Presentation.CastVfx);
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
            presentation,
            authored.AutomaticTarget, authored.Echoable, authored.Description);
        return provisional with { Description = string.IsNullOrWhiteSpace(authored.Description) ? AbilityDescriptionRenderer.Describe(provisional) : authored.Description };
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
                if (authored.Trigger == AbilityTriggerKind.ActionQueued)
                {
                    RejectNonManualCosts(authored, label, report);
                    if (authored.IntervalTicks != 0)
                        report.Error($"{label}: queued actions cannot declare a periodic interval.");
                    break;
                }
                if (authored.Trigger == AbilityTriggerKind.ManaFull)
                {
                    if (authored.ManaCost <= 0 || authored.GoldCost != 0 || authored.IntervalTicks != 0)
                        report.Error($"{label}: mana-full skill requires positive mana, zero gold and no periodic interval.");
                    break;
                }
                if (authored.Trigger is not (AbilityTriggerKind.BattleStarted or AbilityTriggerKind.PeriodicTick))
                    report.Error($"{label}: automatic ability requires battle-start, periodic or mana-full trigger.");
                if (authored.Trigger == AbilityTriggerKind.PeriodicTick && authored.IntervalTicks <= 0)
                    report.Error($"{label}: periodic automatic ability requires a positive interval.");
                if (authored.Trigger == AbilityTriggerKind.BattleStarted && authored.IntervalTicks != 0)
                    report.Error($"{label}: battle-start automatic ability cannot declare an interval.");
                RejectNonManualCosts(authored, label, report);
                break;
            case AbilityActivationKind.Triggered:
                if (authored.Trigger is AbilityTriggerKind.None or AbilityTriggerKind.PeriodicTick or AbilityTriggerKind.BattleStarted or AbilityTriggerKind.ManaFull or AbilityTriggerKind.ActionQueued)
                    report.Error($"{label}: triggered ability requires a supported domain trigger.");
                if (authored.IntervalTicks != 0)
                    report.Error($"{label}: triggered ability cannot declare an interval.");
                RejectNonManualCosts(authored, label, report);
                break;
            case AbilityActivationKind.Passive:
                if (authored.Trigger != AbilityTriggerKind.None || authored.IntervalTicks != 0)
                    report.Error($"{label}: passive ability cannot declare a trigger or interval.");
                if (authored.CooldownTicks != 0 || authored.MaxUses != 0)
                    report.Error($"{label}: passive grants cannot declare cooldown or usage limits.");
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
        // Ability commits now enclose every operation and its death/reaction chain in one
        // BattleWorldStateCheckpoint. Keep one kernel step per binding so target eligibility
        // can be re-evaluated between authored operations after an earlier lethal effect.
        foreach (var effect in operations.OfType<CompiledEffectAbilityOperation>())
            if (effect.Binding.Effects.Length != 1)
                report.Error($"{label}: an ability effect binding must contain exactly one atomic effect step.");
        if (operations.OfType<CompiledProjectileSequenceAbilityOperation>().Count() > 1)
            report.Error($"{label}: an ability may schedule only one projectile sequence.");
        if (operations.OfType<CompiledDisplacementOperation>().Count() > 1)
            report.Error($"{label}: an ability may schedule only one displacement operation.");
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
            case TrampleAbilityOperationSpec trample:
                if (!float.IsFinite(trample.Range) || trample.Range is <= 0 or > 64 ||
                    !float.IsFinite(trample.Distance) || trample.Distance is <= 0 or > 32 ||
                    !float.IsFinite(trample.Speed) || trample.Speed is <= 0 or > 20 ||
                    trample.ChargeTicks is < 1 or > 100 || trample.RecoveryTicks is < 1 or > 100 ||
                    !float.IsFinite(trample.SideDistance) || trample.SideDistance is <= 0 or > 4 ||
                    !float.IsFinite(trample.AttackMultiplier) || trample.AttackMultiplier < 0 ||
                    string.IsNullOrWhiteSpace(trample.WarningVfx) || string.IsNullOrWhiteSpace(trample.RushVfx))
                    report.Error($"{operationLabel}: trample parameters are invalid.");
                return new CompiledTrampleOperation(trample.Range, trample.Distance, trample.Speed,
                    trample.ChargeTicks, trample.RecoveryTicks, trample.SideDistance, trample.AttackMultiplier,
                    trample.WarningVfx, trample.RushVfx);
            case ConeBreathAbilityOperationSpec or ReturningBladeAbilityOperationSpec or PositionSwapAbilityOperationSpec or RampartAbilityOperationSpec or BroodPhaseAbilityOperationSpec:
                return CompileEnemyAction(authored, operationLabel, report);
            case ChargedLineAbilityOperationSpec line:
                if (!Enum.IsDefined(line.Delivery) || !Enum.IsDefined(line.DamageType) ||
                    !float.IsFinite(line.Range) || line.Range <= 0 || line.Range > 64 ||
                    !float.IsFinite(line.Radius) || line.Radius <= 0 || line.Radius > 1 ||
                    line.ChargeTicks < 1 || line.ChargeTicks > 100 || line.MaximumHits < 1 || line.MaximumHits > 16 ||
                    !float.IsFinite(line.AttackMultiplier) || line.AttackMultiplier <= 0 ||
                    !float.IsFinite(line.SubsequentHitMultiplier) || line.SubsequentHitMultiplier is < 0 or > 1 ||
                    !float.IsFinite(line.ProjectileSpeed) || line.ProjectileSpeed is <= 0 or > 100 ||
                    string.IsNullOrWhiteSpace(line.ChargeVfx) || string.IsNullOrWhiteSpace(line.ReleaseVfx))
                    report.Error($"{operationLabel}: charged line parameters are invalid.");
                return new CompiledChargedLineOperation(line.Delivery, line.DamageType, line.Range, line.Radius,
                    line.ChargeTicks, line.AttackMultiplier, line.MaximumHits, line.SubsequentHitMultiplier,
                    line.ProjectileSpeed, line.HoldPosition, line.ChargeVfx, line.ReleaseVfx);
            case DuelAbilityOperationSpec or CounterattackAbilityOperationSpec or GritStorageAbilityOperationSpec or GritPunchAbilityOperationSpec or HookAbilityOperationSpec:
                return CompileTechnique(authored, operationLabel, report);
            case BattleValueAbilityOperationSpec or ConsumeStatusAbilityOperationSpec or EchoAbilityOperationSpec or LifecycleAbilityOperationSpec or DisplacementAbilityOperationSpec:
                return CompileBattleOperation(authored, operationLabel, report, resolveStatus);
            case ProjectileSequenceAbilityOperationSpec sequence:
                if (sequence.ShotCount <= 0 || sequence.ShotCount > 64 || sequence.MaxTargets < 1 || sequence.MaxTargets > 8 ||
                    !float.IsFinite(sequence.AttackIntervalRatio) || sequence.AttackIntervalRatio <= 0 ||
                    !float.IsFinite(sequence.AttackDamageMultiplier) || sequence.AttackDamageMultiplier <= 0)
                    report.Error($"{operationLabel}: projectile sequence parameters are invalid.");
                return new CompiledProjectileSequenceAbilityOperation(sequence.ShotCount,
                    sequence.AttackIntervalRatio, sequence.AttackDamageMultiplier, sequence.MaxTargets);
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
            case ScaledStatusAbilityOperationSpec scaled:
            {
                if (scaled.BaseStacks < 0 || !float.IsFinite(scaled.ExistingStackRatio) || scaled.ExistingStackRatio < 0 ||
                    !float.IsFinite(scaled.Chance) || scaled.Chance < 0 || scaled.Chance > 1 ||
                    !float.IsFinite(scaled.ChancePerStack) || scaled.ChancePerStack < 0)
                    report.Error($"{operationLabel}: scaled status stacks and ratios must be nonnegative and finite; base chance must be within 0..1.");
                if (scaled.BaseStacks == 0 && scaled.ExistingStackRatio == 0)
                    report.Error($"{operationLabel}: scaled status requires a positive base or existing-stack ratio.");
                if (scaled.ExistingStackRatio > 0 && string.IsNullOrWhiteSpace(scaled.StatusId))
                    report.Error($"{operationLabel}: existing-stack scaling requires a status id.");
                if (scaled.ChancePerStack > 0 && string.IsNullOrWhiteSpace(scaled.ChanceStatusId))
                    report.Error($"{operationLabel}: per-stack chance requires a status id.");
                var tierChances = scaled.ChanceByTraitTier ?? [];
                if (!float.IsFinite(scaled.MaximumChance) || scaled.MaximumChance < 0 || scaled.MaximumChance > 1 ||
                    scaled.Chance > scaled.MaximumChance || tierChances.Any(chance =>
                        !float.IsFinite(chance) || chance < 0 || chance > scaled.MaximumChance))
                    report.Error($"{operationLabel}: base and tier chances must be within 0..MaximumChance, with a finite cap within 0..1.");
                if (scaled.ChanceTraitId is null ||
                    (scaled.ChanceTraitId.Length > 0 && !StableIdPattern.IsMatch(scaled.ChanceTraitId)) ||
                    (string.IsNullOrEmpty(scaled.ChanceTraitId) != (tierChances.Length == 0)))
                    report.Error($"{operationLabel}: trait chance requires a valid trait id and one chance per tier.");
                foreach (var id in new[] { scaled.StatusId, scaled.ChanceStatusId })
                    if (id is null || (id.Length > 0 && !StableIdPattern.IsMatch(id)))
                        report.Error($"{operationLabel}: invalid scaled status reference '{id}'.");
                var compiledApplication = CompileStatusOperation(scaled, operationLabel, report, resolveStatus);
                return compiledApplication is null ? null : new CompiledScaledStatusAbilityOperation(
                    compiledApplication.Status, compiledApplication.TargetQuery, scaled.BaseStacks, scaled.StatusId, scaled.ExistingStackRatio,
                    scaled.Chance, scaled.ChanceStatusId, scaled.ChancePerStack, compiledApplication.ApplicationVfx,
                    scaled.ChanceTraitId ?? string.Empty, tierChances.ToImmutableArray(), scaled.MaximumChance);
            }
            case ApplyStatusAbilityOperationSpec status:
                return CompileStatusOperation(status, operationLabel, report, resolveStatus);
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
                    summon.SummonContentId, summon.LimitPerOwner);
            case null:
                report.Error($"{operationLabel}: operation is missing.");
                return null;
            default:
                report.Error($"{operationLabel}: unsupported operation type '{authored.GetType().Name}'.");
                return null;
        }
    }

    private static CompiledApplyStatusAbilityOperation? CompileStatusOperation(
        ApplyStatusAbilityOperationSpec status, string label, ValidationReport report,
        Func<StatusDefinition?, CompiledStatusDefinition?>? resolveStatus)
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
                report.Error($"{label}: status is not part of the compiled publication graph.");
        }
        var target = CompileTarget(status.TargetQuery, label, report);
        if (!string.IsNullOrWhiteSpace(status.ApplicationVfx) &&
            (target is not CompiledFilteredTargetQuery area || area.Range <= 0 ||
             area.Anchor is not (EffectEntityReference.Source or EffectEntityReference.Owner)))
            report.Error($"{label}: area VFX requires a finite positive source/owner-centered range query.");
        return compiledStatus is null || target is null
            ? null
            : new CompiledApplyStatusAbilityOperation(compiledStatus, target, status.ApplicationVfx);
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
        ValidationReport report) => EffectBindingCompiler.CompileTarget(authored, label, report);

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
            return DescribeEffect(effect, ability.AutomaticTarget);
        if (operations.Length == 1 && operations[0] is CompiledCooldownAbilityOperation cooldown)
            return DescribeCooldown(cooldown);

        var parts = operations.Select(operation => operation switch
        {
            CompiledTrampleOperation trample =>
                $"蓄力 {trample.ChargeTicks * BattleTiming.TickSeconds:0.#} 秒后直线冲锋，将沿途单位撞向两侧；每个敌人受到一次 {trample.AttackMultiplier * 100:0}% 攻击力伤害，友军仅被推开。",
            CompiledChargedLineOperation line =>
                $"锁定方向蓄力 {line.ChargeTicks * BattleTiming.TickSeconds:0.#} 秒，直线贯穿最多 {line.MaximumHits} 个敌人，造成 {line.AttackMultiplier * 100:0}% 攻击力的{EffectModelText.DamageTypeName(line.DamageType)}伤害，后续目标倍率 {line.SubsequentHitMultiplier * 100:0}%。",
            CompiledProjectileSequenceAbilityOperation sequence =>
                $"连续发射 {sequence.ShotCount} 箭" + (sequence.MaxTargets > 1 ? $"，在最多 {sequence.MaxTargets} 名敌人间轮流射击" : string.Empty) +
                $"，每箭造成攻击力 {sequence.AttackDamageMultiplier * 100:0.#}% 的伤害；间隔为当前普攻的 {sequence.AttackIntervalRatio:0.##} 倍",
            CompiledEffectAbilityOperation effect => DescribeEffect(effect, ability.AutomaticTarget).TrimEnd('。'),
            CompiledCooldownAbilityOperation cooldown => DescribeCooldown(cooldown).TrimEnd('。'),
            CompiledApplyStatusAbilityOperation status => DescribeStatus(status).TrimEnd('。'),
            CompiledScaledStatusAbilityOperation scaled => DescribeScaledStatus(scaled),
            CompiledSummonAbilityOperation summon => DescribeSummon(summon).TrimEnd('。'),
            CompiledDisplacementOperation displacement => DescribeDisplacement(displacement),
            _ => string.Empty
        }).Where(part => !string.IsNullOrWhiteSpace(part)).ToArray();
        return parts.Length == 0 ? ability.DisplayName : string.Join("，", parts) + "。";
    }

    private static string DescribeDisplacement(CompiledDisplacementOperation operation)
    {
        var movement = operation.Kind switch
        {
            DisplacementKind.Charge => "向敌人冲锋",
            DisplacementKind.Leap => operation.BehindTarget ? "跃至敌人身后" : "跃至敌人附近",
            DisplacementKind.Blink => operation.BehindTarget ? "闪现至敌人身后" : "闪现至敌人附近",
            DisplacementKind.Knockback => "击退敌人",
            DisplacementKind.Pull => "将敌人拉向自己",
            _ => "将范围内敌人向自己聚拢"
        };
        var result = $"{movement}，最大位移 {operation.Distance:0.##} 格";
        if (operation.Kind != DisplacementKind.Blink)
            result += $"，持续 {operation.DurationTicks * BattleTiming.TickSeconds:0.##} 秒";
        if (operation.ImpactDamage > 0 || operation.AttackRatio > 0)
        {
            var damage = EffectModelText.DamageTypeName(operation.DamageType);
            result += $"，抵达后造成 {operation.ImpactDamage:0.#}＋{operation.AttackRatio * 100:0.#}% 攻击力的{damage}伤害";
        }
        if (operation.ImpactRadius > 0) result += $"，命中范围 {operation.ImpactRadius:0.##} 格";
        if (operation.ImpactStatus is { } status) result += $"，施加「{status.DisplayName}」";
        return result;
    }

    private static string DescribeEffect(CompiledEffectAbilityOperation operation, AbilityAutomaticTargetKind automaticTarget)
    {
        if (operation.Binding.TargetQuery is CompiledFilteredTargetQuery ||
            operation.Binding.Conditions.Length > 0 || operation.Binding.Effects.Any(step =>
                step.Magnitude is not null || step.DamageType != EffectDamageType.Normal))
            return EffectModelText.DescribeBinding(operation.Binding);
        var target = operation.Binding.TargetQuery switch
        {
            CompiledOwnerTargetQuery or CompiledSourceTargetQuery => "自身",
            CompiledExplicitTargetQuery when automaticTarget == AbilityAutomaticTargetKind.WoundedAlly => "范围内生命比例最低的受伤友军",
            CompiledExplicitTargetQuery => "目标敌人",
            CompiledRelativeTeamTargetQuery { Team: EffectRelativeTeam.Allies } relative => $"全体{DescribeTag(relative.RequiredTag)}友军",
            CompiledRelativeTeamTargetQuery relative => $"全体{DescribeTag(relative.RequiredTag)}敌人",
            _ => "目标"
        };
        var facts = operation.Binding.Effects.Select(step =>
        {
            var invocation = step.AmountSource == EffectAmountSource.InvocationValue;
            var percent = invocation && operation.InvocationValueSource == AbilityInvocationValueSource.OwnerMaxHealth;
            var amount = invocation ? step.Amount * operation.InvocationValueScale : step.Amount;
            var value = percent ? $"施法者最大生命的 {amount * 100:0.#}%" : $"{amount:0.##} 点";
            return step.Kind switch
            {
                EffectKind.Damage => $"对{target}造成{value}伤害",
                EffectKind.Heal => $"为{target}恢复{value}生命",
                EffectKind.Shield => $"为{target}提供{value}护盾",
                _ => $"对{target}施加效果"
            };
        });
        return string.Join("，", facts) + "。";
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

    private static string DescribeScaledStatus(CompiledScaledStatusAbilityOperation operation)
    {
        var stacks = $"{operation.BaseStacks}" + (operation.ExistingStackRatio > 0
            ? $"＋目标「{operation.StatusId}」当前层数×{operation.ExistingStackRatio:0.###}" : string.Empty);
        var chance = operation.ChancePerStack > 0
            ? $"以 {operation.Chance:P0}＋目标「{operation.ChanceStatusId}」每层 {operation.ChancePerStack:P0} 的概率（最高 {operation.MaximumChance:P0}）"
            : operation.Chance < 1 ? $"以 {operation.Chance:P0} 概率" : string.Empty;
        if (!operation.ChanceByTraitTier.IsDefaultOrEmpty)
            chance += $"（作为羁绊成员时，随己方档位提高至 {string.Join("／", operation.ChanceByTraitTier.Select(value => value.ToString("P0")))}，上限 {operation.MaximumChance:P0}）";
        return $"对{EffectModelText.DescribeTarget(operation.TargetQuery)}{chance}施加「{operation.Status.DisplayName}」{stacks} 层" +
            (operation.ExistingStackRatio > 0 ? "（向下取整）" : string.Empty);
    }

    private static string DescribeStatus(CompiledApplyStatusAbilityOperation operation)
    {
        if (operation.TargetQuery is CompiledFilteredTargetQuery)
            return $"为{EffectModelText.DescribeTarget(operation.TargetQuery)}施加「{operation.Status.DisplayName}」。";
        if (operation.TargetQuery is CompiledExplicitTargetQuery)
            return $"对目标施加「{operation.Status.DisplayName}」" +
                (operation.Status.DurationKind == StatusDurationKind.TimedTicks
                    ? $" {operation.Status.DurationTicks * BattleTiming.TickSeconds:0.##} 秒" : string.Empty) +
                (operation.Status.ControlDurationRule != StatusControlDurationRule.None ? "（受控制抗性影响）" : string.Empty) + "。";
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
