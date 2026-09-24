using System;
using System.Linq;
using System.Collections.Immutable;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;
namespace TowerAutobattler.Abilities;

public static partial class AbilityDefinitionCompiler
{
    private static CompiledAbilityOperation? CompileBattleOperation(AbilityOperationSpec authored, string label,
        ValidationReport report, Func<StatusDefinition?, CompiledStatusDefinition?>? resolveStatus)
    {
        switch (authored)
        {
            case DisplacementAbilityOperationSpec displacement:
            {
                var target = CompileTarget(displacement.TargetQuery, label, report);
                if (!Enum.IsDefined(displacement.Kind) || !Enum.IsDefined(displacement.DamageType) ||
                    !float.IsFinite(displacement.Distance) || displacement.Distance is <= 0 or > 20 ||
                    !float.IsFinite(displacement.DurationSeconds) || displacement.DurationSeconds is <= 0 or > 5 ||
                    !float.IsFinite(displacement.StopDistance) || displacement.StopDistance is < 0 or > 5 ||
                    !float.IsFinite(displacement.ImpactDamage) || displacement.ImpactDamage < 0 ||
                    !float.IsFinite(displacement.AttackRatio) || displacement.AttackRatio < 0 ||
                    !float.IsFinite(displacement.ImpactRadius) || displacement.ImpactRadius is < 0 or > 20 ||
                    !float.IsFinite(displacement.ArcHeight) || displacement.ArcHeight is < 0 or > 5)
                    report.Error($"{label}: displacement requires bounded positive distance/duration and finite non-negative impact values.");
                if (target is not CompiledFilteredTargetQuery
                    { Team: EffectRelativeTeam.Enemies, IncludeDefeated: false,
                        Anchor: EffectEntityReference.Owner or EffectEntityReference.Source })
                    report.Error($"{label}: displacement requires an owner/source-centered living-enemy filter.");
                if (target is CompiledFilteredTargetQuery area && (!float.IsFinite(area.Range) || area.Range is <= 0 or > 20))
                    report.Error($"{label}: displacement requires a finite positive targeting radius no greater than 20.");
                CompiledStatusDefinition? impactStatus = null;
                if (displacement.ImpactStatus is { } status)
                {
                    if (resolveStatus is null)
                    {
                        var compilation = StatusDefinitionCompiler.Compile(status);
                        report.Merge(compilation.Report);
                        impactStatus = compilation.Definition;
                    }
                    else
                    {
                        impactStatus = resolveStatus(status);
                        if (impactStatus is null)
                            report.Error($"{label}: displacement impact status is not part of the compiled publication graph.");
                    }
                }
                if (report.HasCoreErrors || target is null) return null;
                var ticks = Math.Max(1, (int)Math.Ceiling(displacement.DurationSeconds / BattleTiming.TickSeconds - .00001f));
                return new CompiledDisplacementOperation(displacement.Kind, target, displacement.Distance, ticks,
                    displacement.StopDistance, displacement.BehindTarget, displacement.ExcludeBoss,
                    displacement.ImpactDamage, displacement.AttackRatio, displacement.ImpactRadius,
                    displacement.DamageType, impactStatus, displacement.ArcHeight);
            }
            case BattleValueAbilityOperationSpec value:
            {
                var target = CompileTarget(value.TargetQuery, label, report);
                if (!Enum.IsDefined(value.Action) || !Enum.IsDefined(value.TargetPolicy) || !Enum.IsDefined(value.Attribute) ||
                    !Enum.IsDefined(value.DamageType) || !float.IsFinite(value.Amount) || !float.IsFinite(value.Minimum) ||
                    !float.IsFinite(value.Maximum) || value.Minimum > value.Maximum || value.Every < 1 || value.Terms.Count > 16)
                    report.Error($"{label}: invalid battle-value operation.");
                if ((value.Action is BattleValueAction.AddCounter or BattleValueAction.SetCounter) && string.IsNullOrWhiteSpace(value.CounterKey))
                    report.Error($"{label}: counter key is required.");
                if (value.Action == BattleValueAction.ConsumeShield && string.IsNullOrWhiteSpace(value.CounterKey))
                    report.Error($"{label}: shield consumption requires a receipt counter.");
                foreach (var term in value.Terms)
                    if (term is null || !Enum.IsDefined(term.Metric) || !Enum.IsDefined(term.Subject) || !Enum.IsDefined(term.Attribute) ||
                        !float.IsFinite(term.Scale) || ((term.Metric is BattleValueMetric.Counter or BattleValueMetric.StatusStacks) && string.IsNullOrWhiteSpace(term.Key)))
                        report.Error($"{label}: invalid battle-value term.");
                if (report.HasCoreErrors || target is null) return null;
                return new CompiledBattleValueOperation(value.Action, target, value.TargetPolicy, value.Amount,
                    value.Terms.Select(t => new CompiledBattleValueTerm(t.Metric,t.Subject,t.Attribute,t.Key,t.Scale,t.TeamShared,t.OwnStatusOnly)).ToImmutableArray(),
                    value.DamageType,value.Attribute,value.CounterKey,value.TeamShared,value.Minimum,value.Maximum,value.Every,value.Broadcast,value.Label);
            }
            case ConsumeStatusAbilityOperationSpec consume:
            {
                var target = CompileTarget(consume.TargetQuery, label, report);
                if (string.IsNullOrWhiteSpace(consume.StatusId) || !float.IsFinite(consume.DamageMultiplier) || consume.DamageMultiplier <= 0 || !Enum.IsDefined(consume.DamageType))
                    report.Error($"{label}: invalid status consumption.");
                return target is null ? null : new CompiledConsumeStatusOperation(target,consume.StatusId,consume.DamageMultiplier,consume.DamageType);
            }
            case EchoAbilityOperationSpec echo:
                if (!float.IsFinite(echo.Range) || echo.Range <= 0) report.Error($"{label}: echo range must be positive.");
                return new CompiledEchoOperation(echo.Range,echo.BehindOnly);
            case LifecycleAbilityOperationSpec lifecycle:
            {
                var target = CompileTarget(lifecycle.TargetQuery,label,report);
                if (!Enum.IsDefined(lifecycle.Kind) || lifecycle.DelayTicks < 1 || lifecycle.DurationTicks < 1 ||
                    !float.IsFinite(lifecycle.HealthRatio) || lifecycle.HealthRatio <= 0 || lifecycle.HealthRatio > 1 ||
                    !float.IsFinite(lifecycle.AttackTransferRatio) || lifecycle.AttackTransferRatio < 0 || lifecycle.MaximumLivingSummons < 1)
                    report.Error($"{label}: invalid lifecycle operation.");
                if (lifecycle.Kind == LifecycleAbilityKind.RaiseCorpse && string.IsNullOrWhiteSpace(lifecycle.SummonContentId))
                    report.Error($"{label}: corpse product content id is required.");
                return target is null ? null : new CompiledLifecycleOperation(lifecycle.Kind,target,lifecycle.DelayTicks,lifecycle.DurationTicks,
                    lifecycle.HealthRatio,lifecycle.AttackTransferRatio,lifecycle.SummonContentId,lifecycle.MaximumLivingSummons);
            }
            default: throw new InvalidOperationException("Unknown battle operation authoring type.");
        }
    }
}
