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
using TowerAutobattler.Statuses;

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
        ability.AutomaticTarget,
        ability.Echoable,
        ability.AuthoredDescription,
        string.Join("/", ability.Operations.Select(Operation)),
        ability.Presentation is null
            ? string.Empty
            : $"{ability.Presentation.SemanticIcon}:{ability.Presentation.Cue}:{ability.Presentation.ReportLabel}:{ability.Presentation.DamageVfx}");

    private static string Operation(CompiledAbilityOperation operation) => operation switch
    {
        CompiledEffectAbilityOperation effect =>
            $"effect:{Binding(effect.Binding)}:{effect.InvocationValueSource}:" +
            effect.InvocationValueScale.ToString("R", CultureInfo.InvariantCulture),
        CompiledCooldownAbilityOperation cooldown =>
            $"cooldown:{Target(cooldown.TargetQuery)}:{cooldown.AttackAdjustment}:{cooldown.AttackValue}:" +
            $"{cooldown.MoveAdjustment}:{cooldown.MoveValue}",
        CompiledApplyStatusAbilityOperation status =>
            $"status:{StatusDefinitionFingerprint.Compute(status.Status)}:{Target(status.TargetQuery)}:{status.ApplicationVfx}",
        CompiledScaledStatusAbilityOperation scaled => string.Join(":", "scaled-status",
            StatusDefinitionFingerprint.Compute(scaled.Status), Target(scaled.TargetQuery), scaled.BaseStacks,
            scaled.StatusId, Number(scaled.ExistingStackRatio), Number(scaled.Chance),
            scaled.ChanceStatusId, Number(scaled.ChancePerStack), scaled.ApplicationVfx,
            scaled.ChanceTraitId, Number(scaled.MaximumChance),
            scaled.ChanceByTraitTier.IsDefaultOrEmpty ? "" : string.Join(",", scaled.ChanceByTraitTier.Select(Number))),
        CompiledSummonAbilityOperation summon =>
            $"summon:{summon.Profile}:{summon.Count}:" +
            $"{summon.HealthMultiplier.ToString("R", CultureInfo.InvariantCulture)}:" +
            $"{summon.DamageMultiplier.ToString("R", CultureInfo.InvariantCulture)}:" +
            $"{summon.MaximumLivingTemporaryUnits}:{summon.RequireAtLeastOne}:{summon.SummonContentId}:{summon.LimitPerOwner}",
        CompiledDuelOperation d => $"duel:{d.DurationTicks}:{Number(d.BreakDistance)}",
        CompiledCounterattackOperation c => $"counter:{c.HitsRequired}:{Number(c.AttackRatio)}:{Number(c.LifestealRatio)}",
        CompiledGritStorageOperation g => $"grit:{g.CounterKey}:{g.WindowTicks}:{Number(g.MaximumHealthRatio)}",
        CompiledGritPunchOperation p => string.Join(":", "punch", p.CounterKey,
            Number(p.LowHealthRatio),Number(p.CriticalHealthRatio),Number(p.Range),Number(p.Radius),
            p.ChargeTicks,p.RecoveryTicks,p.ShieldTicks,Number(p.AttackRatio),Number(p.GritDamageRatio)),
        CompiledHookOperation h => string.Join(":","hook",Number(h.Range),Number(h.Radius),Number(h.Speed),h.WindupTicks,h.ReturnTicks,Number(h.AttackRatio),h.ExcludeBoss),
        CompiledConeBreath x => string.Join(":","cone",Number(x.Range),Number(x.AngleDegrees),x.WindupTicks,x.PulseIntervalTicks,x.PulseCount,x.RecoveryTicks,Number(x.AttackMultiplier),x.Vfx),
        CompiledReturningBlade x => string.Join(":","return",Number(x.Range),Number(x.Radius),Number(x.Speed),Number(x.AttackMultiplier),x.Vfx),
        CompiledPositionSwap x => string.Join(":","swap",Number(x.Range),x.PrepareTicks,x.RecoveryTicks,x.Vfx),
        CompiledRampart x => string.Join(":","rampart",x.WallContentId,x.RaiseTicks,x.IntervalTicks,x.FissureWindupTicks,x.RecoveryTicks,x.WallLifetimeTicks,Number(x.Range),Number(x.FissureRadius),Number(x.AttackMultiplier),x.Vfx),
        CompiledBroodPhase x => string.Join(":","brood",x.EggContentId,x.LarvaContentId,Number(x.HealthThreshold),x.BreakTicks,Number(x.SmallRadius),Number(x.RetreatDistance),Number(x.RangedReach),x.HatchTicks,x.SpawnCycleTicks,x.MaximumLiving,x.MaximumBatches,Number(x.SwipeMultiplier),x.Vfx),
        CompiledTrampleOperation trample => string.Join(":", "trample", Number(trample.Range), Number(trample.Distance),
            Number(trample.Speed), trample.ChargeTicks, trample.RecoveryTicks, Number(trample.SideDistance),
            Number(trample.AttackMultiplier), trample.WarningVfx, trample.RushVfx),
        CompiledChargedLineOperation line => string.Join(":", "charged-line", line.Delivery, line.DamageType,
            Number(line.Range), Number(line.Radius), line.ChargeTicks, Number(line.AttackMultiplier), line.MaximumHits,
            Number(line.SubsequentHitMultiplier), Number(line.ProjectileSpeed), line.HoldPosition, line.ChargeVfx, line.ReleaseVfx),
        CompiledProjectileSequenceAbilityOperation sequence =>
            $"projectile-sequence:{sequence.ShotCount}:{Number(sequence.AttackIntervalRatio)}:{Number(sequence.AttackDamageMultiplier)}:{sequence.MaxTargets}",
        CompiledBattleValueOperation value => string.Join(":",
            "battle-value", value.Action, Target(value.TargetQuery), value.TargetPolicy, Number(value.Amount),
            string.Join(",", value.Terms.Select(term => string.Join(";", term.Metric, term.Subject, term.Attribute,
                term.Key, Number(term.Scale), term.TeamShared, term.OwnStatusOnly))),
            value.DamageType, value.Attribute, value.CounterKey, value.TeamShared,
            Number(value.Minimum), Number(value.Maximum), value.Every, value.Broadcast, value.Label),
        CompiledConsumeStatusOperation consume =>
            $"consume-status:{Target(consume.TargetQuery)}:{consume.StatusId}:{Number(consume.DamageMultiplier)}:{consume.DamageType}",
        CompiledEchoOperation echo => $"echo:{Number(echo.Range)}:{echo.BehindOnly}",
        CompiledDisplacementOperation displacement => string.Join(":", "displacement", displacement.Kind,
            Target(displacement.TargetQuery), Number(displacement.Distance), displacement.DurationTicks,
            Number(displacement.StopDistance), displacement.BehindTarget, displacement.ExcludeBoss,
            Number(displacement.ImpactDamage), Number(displacement.AttackRatio), Number(displacement.ImpactRadius),
            displacement.DamageType,
            displacement.ImpactStatus is { } impact ? StatusDefinitionFingerprint.Compute(impact) : "",
            Number(displacement.ArcHeight)),
        CompiledLifecycleOperation lifecycle => string.Join(":", "lifecycle", lifecycle.Kind,
            Target(lifecycle.TargetQuery), lifecycle.DelayTicks, lifecycle.DurationTicks,
            Number(lifecycle.HealthRatio), Number(lifecycle.AttackTransferRatio), lifecycle.SummonContentId,
            lifecycle.MaximumLivingSummons),
        _ => throw new InvalidOperationException(
            $"Unsupported tactical-command Ability operation: {operation.GetType().Name}")
    };

    private static string Binding(CompiledEffectBinding binding) => EffectModelText.BindingFingerprint(binding);

    private static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static string Target(CompiledEffectTargetQuery query) => EffectModelText.Target(query);

    [GeneratedRegex("^[a-z0-9]+(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableIdRegex();
}
