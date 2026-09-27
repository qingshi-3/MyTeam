using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Attributes;
using TowerAutobattler.Content;

namespace TowerAutobattler.Abilities;

public sealed record CompiledMatrixOperation(
    MatrixOperationKind Kind, MatrixTargetKind Target, float Range, float Radius, int MaxTargets, bool ExcludeSelf,
    float Amount, float AttackRatio, float OwnerHealthRatio, float TargetHealthRatio, float EventRatio, float CounterRatio,
    float EventTargetHealthRatio, float RadiusPerCounter, bool CounterOnTarget, float ShieldCap, float TemporaryScale, float Maximum, string Key, string RequiredCounter, float CounterThreshold, bool ConsumeCounter,
    float CounterConsumeRatio, bool LandingSide, bool ResetOnTargetChange, int DurationTicks, int Count, int IntervalTicks, CombatAttribute Attribute, string ContentId,
    MatrixEventKind Event, MatrixRelation SourceRelation, MatrixRelation TargetRelation, MatrixCondition TargetCondition,
    MatrixCondition OwnerCondition, float HealthThreshold, float MinimumEventValue, int CooldownTicks, int Every,
    bool PerTargetCooldown, bool OncePerTarget, bool OncePerSource, bool OncePerAction, bool OncePerBattle, bool ActiveSkillOnly, bool TemporaryOnly,
    bool LivingOwnerRequired, bool ClearOnDeath, ImmutableArray<CompiledMatrixOperation> Effects) : CompiledAbilityOperation;

public static class MatrixAbilityCompiler
{
    public static CompiledMatrixOperation? Compile(MatrixAbilityOperationSpec authored, string label, ValidationReport report) =>
        CompileNode(authored, label, report, new HashSet<MatrixAbilityOperationSpec>(ReferenceEqualityComparer.Instance), 0);

    private static CompiledMatrixOperation? CompileNode(MatrixAbilityOperationSpec a, string label, ValidationReport report,
        HashSet<MatrixAbilityOperationSpec> visiting, int depth)
    {
        if (depth > 5 || !visiting.Add(a)) { report.Error($"{label}: cyclic or excessively nested matrix operation."); return null; }
        if (!Enum.IsDefined(a.Kind) || !Enum.IsDefined(a.Target) || !Enum.IsDefined(a.Event) ||
            !Enum.IsDefined(a.SourceRelation) || !Enum.IsDefined(a.TargetRelation) || !Enum.IsDefined(a.TargetCondition) ||
            !Enum.IsDefined(a.OwnerCondition) || !Enum.IsDefined(a.Attribute) || a.Effects.Count > 12 ||
            new[]{a.Range,a.Radius,a.Amount,a.AttackRatio,a.OwnerHealthRatio,a.TargetHealthRatio,a.EventRatio,a.CounterRatio,
                a.EventTargetHealthRatio,a.ShieldCap,a.RadiusPerCounter,a.TemporaryScale,a.Maximum,a.CounterThreshold,a.HealthThreshold,a.MinimumEventValue,a.CounterConsumeRatio}.Any(v=>!float.IsFinite(v)) ||
            a.Range < 0 || a.Range > 30 || a.Radius < 0 || a.Radius > 15 || a.MaxTargets < 1 || a.MaxTargets > 64 ||
            a.Count < 1 || a.Count > 16 || a.IntervalTicks < 1 || a.DurationTicks < 0 || a.DurationTicks > 3600 ||
            a.CooldownTicks < 0 || a.Every < 1 || a.Maximum < 0 || a.TemporaryScale < 0 ||
            a.HealthThreshold is < 0 or > 1 || a.CounterConsumeRatio is < 0 or > 1)
            report.Error($"{label}: invalid bounded matrix operation parameters.");
        if (a.Kind is MatrixOperationKind.Counter or MatrixOperationKind.ClearCounter && string.IsNullOrWhiteSpace(a.Key))
            report.Error($"{label}: counter operation requires a key.");
        if (a.Kind == MatrixOperationKind.Summon && string.IsNullOrWhiteSpace(a.ContentId))
            report.Error($"{label}: summon requires explicit content identity.");
        if (a.Kind is MatrixOperationKind.RegisterReaction or MatrixOperationKind.Delay or MatrixOperationKind.Sequence && a.Effects.Count == 0)
            report.Error($"{label}: reaction/delay requires effects.");
        if (depth > 0 && a.Kind == MatrixOperationKind.RegisterReaction)
            report.Error($"{label}: reactions cannot register nested reactions.");
        if (a.Kind == MatrixOperationKind.DeathGift && (a.Effects.Count == 0 || a.Effects.Any(e=>e is null || e.Kind != MatrixOperationKind.Shield || e.Effects.Count > 0)))
            report.Error($"{label}: death-gift echoes only support explicit shield effects.");
        if (a.Kind == MatrixOperationKind.PermanentAttribute &&
            (a.Target != MatrixTargetKind.Self || a.Attribute is not (CombatAttribute.AttackDamage or CombatAttribute.MaxHealth) ||
             a.Amount <= 0 || a.AttackRatio != 0 || a.OwnerHealthRatio != 0 || a.TargetHealthRatio != 0 ||
             a.EventRatio != 0 || a.EventTargetHealthRatio != 0 || a.CounterRatio != 0 || a.RadiusPerCounter != 0 ||
             a.Effects.Count != 0))
            report.Error($"{label}: permanent attribute requires Self, a positive fixed Amount, AttackDamage/MaxHealth, and no dynamic ratios or effects.");
        var effects = a.Effects.Select((child,i)=>child is null ? null : CompileNode(child,$"{label}.Effects[{i}]",report,visiting,depth+1))
            .Where(child=>child is not null).Cast<CompiledMatrixOperation>().ToImmutableArray();
        if(effects.Length != a.Effects.Count) report.Error($"{label}: missing nested effect.");
        visiting.Remove(a);
        return new(a.Kind,a.Target,a.Range,a.Radius,a.MaxTargets,a.ExcludeSelf,a.Amount,a.AttackRatio,a.OwnerHealthRatio,
            a.TargetHealthRatio,a.EventRatio,a.CounterRatio,a.EventTargetHealthRatio,a.RadiusPerCounter,a.CounterOnTarget,a.ShieldCap,a.TemporaryScale,a.Maximum,a.Key,a.RequiredCounter,a.CounterThreshold,
            a.ConsumeCounter,a.CounterConsumeRatio,a.LandingSide,a.ResetOnTargetChange,a.DurationTicks,a.Count,a.IntervalTicks,a.Attribute,a.ContentId,a.Event,
            a.SourceRelation,a.TargetRelation,a.TargetCondition,a.OwnerCondition,a.HealthThreshold,a.MinimumEventValue,a.CooldownTicks,
            a.Every,a.PerTargetCooldown,a.OncePerTarget,a.OncePerSource,a.OncePerAction,a.OncePerBattle,a.ActiveSkillOnly,a.TemporaryOnly,a.LivingOwnerRequired,a.ClearOnDeath,effects);
    }

    public static IEnumerable<CompiledMatrixOperation> Flatten(CompiledMatrixOperation operation)
    {
        yield return operation;
        foreach(var child in operation.Effects)
        foreach(var descendant in Flatten(child)) yield return descendant;
    }

    public static IEnumerable<MatrixAbilityOperationSpec> Flatten(MatrixAbilityOperationSpec operation)
    {
        var visited=new HashSet<MatrixAbilityOperationSpec>(ReferenceEqualityComparer.Instance);
        var pending=new Stack<(MatrixAbilityOperationSpec Item,int Depth)>();
        pending.Push((operation,0));
        while(pending.Count>0)
        {
            var (item,depth)=pending.Pop();
            if(depth>5||!visited.Add(item))continue;
            yield return item;
            foreach(var child in item.Effects.AsEnumerable().Reverse())if(child is not null)pending.Push((child,depth+1));
        }
    }

    public static string Fingerprint(CompiledMatrixOperation operation) => System.Text.Json.JsonSerializer.Serialize(operation);
}
