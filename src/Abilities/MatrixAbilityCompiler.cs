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
    bool LivingOwnerRequired, bool ClearOnDeath, ImmutableArray<CompiledMatrixOperation> Effects, float SpellPowerRatio = 0) : CompiledAbilityOperation;

public static class MatrixAbilityCompiler
{
    public static CompiledMatrixOperation? Compile(MatrixAbilityOperationSpec authored, string label, ValidationReport report) =>
        CompileNode(authored, label, report, new HashSet<MatrixAbilityOperationSpec>(ReferenceEqualityComparer.Instance), 0);

    private static CompiledMatrixOperation? CompileNode(MatrixAbilityOperationSpec a, string label, ValidationReport report,
        HashSet<MatrixAbilityOperationSpec> visiting, int depth, MatrixOperationKind? parent = null)
    {
        if (depth > 5 || !visiting.Add(a)) { report.Error($"{label}: cyclic or excessively nested matrix operation."); return null; }
        if (!Enum.IsDefined(a.Kind) || !Enum.IsDefined(a.Target) || !Enum.IsDefined(a.Event) ||
            !Enum.IsDefined(a.SourceRelation) || !Enum.IsDefined(a.TargetRelation) || !Enum.IsDefined(a.TargetCondition) ||
            !Enum.IsDefined(a.OwnerCondition) || !Enum.IsDefined(a.Attribute) || a.Effects.Length > 12 ||
            new[]{a.SpellPowerRatio,a.Range,a.Radius,a.Amount,a.AttackRatio,a.OwnerHealthRatio,a.TargetHealthRatio,a.EventRatio,a.CounterRatio,
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
        if (a.Kind is MatrixOperationKind.RegisterReaction or MatrixOperationKind.AttackReaction or MatrixOperationKind.Windup or MatrixOperationKind.Delay or MatrixOperationKind.Sequence && a.Effects.Length == 0)
            report.Error($"{label}: reaction/delay requires effects.");
        if (depth > 0 && (a.Kind == MatrixOperationKind.RegisterReaction || a.Kind == MatrixOperationKind.AttackReaction && parent != MatrixOperationKind.Transform))
            report.Error($"{label}: reactions cannot register nested reactions.");
        if (a.Kind == MatrixOperationKind.AttackReaction && (a.Event != MatrixEventKind.AttackHit ||
            a.SourceRelation != MatrixRelation.Owner || a.DurationTicks <= 0))
            report.Error($"{label}: attack followups require an owner attack event and finite duration.");
        if (a.Kind == MatrixOperationKind.Windup && (a.Count != 1 || a.DurationTicks <= 0))
            report.Error($"{label}: windup requires a single delayed release and positive duration.");
        if (a.Kind == MatrixOperationKind.DeathGift && (a.Effects.Length == 0 || a.Effects.Any(e=>e is null || e.Kind != MatrixOperationKind.Shield || e.Effects.Length > 0)))
            report.Error($"{label}: death-gift echoes only support explicit shield effects.");
        if (a.Kind == MatrixOperationKind.PermanentAttribute &&
            (a.Target != MatrixTargetKind.Self || a.Attribute is not (CombatAttribute.AttackDamage or CombatAttribute.MaxHealth) ||
             a.Amount <= 0 || a.AttackRatio != 0 || a.OwnerHealthRatio != 0 || a.TargetHealthRatio != 0 ||
             a.SpellPowerRatio != 0 || a.EventRatio != 0 || a.EventTargetHealthRatio != 0 || a.CounterRatio != 0 || a.RadiusPerCounter != 0 ||
             a.Effects.Length != 0))
            report.Error($"{label}: permanent attribute requires Self, a positive fixed Amount, AttackDamage/MaxHealth, and no dynamic ratios or effects.");
        if (a.Kind == MatrixOperationKind.AttackDodgeAura &&
            (a.Target != MatrixTargetKind.Self || a.DurationTicks <= 0 || a.Radius <= 0 || a.Effects.Length != 0))
            report.Error($"{label}: attack dodge aura requires Self, positive finite radius/duration, and no nested effects.");
        if (a.Kind is MatrixOperationKind.OptionalMeleeStrike or MatrixOperationKind.AttackSpeedForOriginalAttacks or MatrixOperationKind.BlinkLineDisarm or MatrixOperationKind.FrontalAttackGuard or MatrixOperationKind.GroundStorm)
        {
            if (a.Target != MatrixTargetKind.Self || a.Effects.Length != 0)
                report.Error($"{label}: hero payoff requires Self and no nested effects.");
            if (a.Kind == MatrixOperationKind.FrontalAttackGuard &&
                (a.Amount is <=0 or >=1 || a.Radius is <=0 or >1.570797f || a.DurationTicks<=0))
                report.Error($"{label}: frontal guard requires reduction in (0,1), half-angle radians in (0,pi/2], and duration.");
            if (a.Kind == MatrixOperationKind.GroundStorm &&
                (a.Amount is <=0 or >=1 || a.Radius<=0 || a.DurationTicks<=0 || a.IntervalTicks<=0 || a.DurationTicks%a.IntervalTicks!=0 || a.SpellPowerRatio<=0))
                report.Error($"{label}: ground storm requires a finite radial field, fixed pulse schedule, slow in (0,1), and spell ratio.");
            if (a.Kind == MatrixOperationKind.OptionalMeleeStrike &&
                (a.SpellPowerRatio <= 0 || a.Amount < 0 || a.Amount > 4 || a.DurationTicks <= 0))
                report.Error($"{label}: optional melee strike requires spell ratio, nonnegative bounded knockback and stun duration.");
            if (a.Kind == MatrixOperationKind.AttackSpeedForOriginalAttacks &&
                (a.Amount <= 0 || a.Amount > 5 || a.Count < 1 || a.Count > 20 || a.DurationTicks <= 0 || string.IsNullOrWhiteSpace(a.Key)))
                report.Error($"{label}: original-attack speed requires a positive bounded amount, attack count, duration and stable key.");
            if (a.Kind == MatrixOperationKind.BlinkLineDisarm &&
                (a.Amount <= 0 || a.Amount > 4 || a.Range <= 0 || a.Radius <= 0 || a.AttackRatio <= 0 || a.DurationTicks <= 0))
                report.Error($"{label}: blink-line disarm requires bounded blink distance, line length/half-width, attack ratio and duration.");
        }
        if (a.Kind is MatrixOperationKind.RememberAllyFixedGains or MatrixOperationKind.BindOpeningAllies or MatrixOperationKind.GrantRememberableAttribute)
        {
            if (a.Target != MatrixTargetKind.Self || string.IsNullOrWhiteSpace(a.Key) ||
                !System.Text.RegularExpressions.Regex.IsMatch(a.Key, "^[A-Za-z0-9_.-]+$") || a.Effects.Length != 0 ||
                a.OwnerCondition != MatrixCondition.None || a.TargetCondition != MatrixCondition.None || a.RequiredCounter.Length != 0)
                report.Error($"{label}: remembered metadata/grant requires Self, a stable key and no children, conditions or counter prerequisite.");
            if (a.Kind == MatrixOperationKind.BindOpeningAllies && a.MaxTargets != 2)
                report.Error($"{label}: opening bindings require exactly two eligible allies at most.");
            if (a.Kind == MatrixOperationKind.GrantRememberableAttribute &&
                (parent != MatrixOperationKind.Transform || a.Attribute is not (CombatAttribute.AttackDamage or CombatAttribute.MaxHealth) ||
                 a.Amount <= 0 || a.Maximum < a.Amount || a.SpellPowerRatio != 0 || a.AttackRatio != 0 || a.OwnerHealthRatio != 0 ||
                 a.TargetHealthRatio != 0 || a.EventRatio != 0 || a.EventTargetHealthRatio != 0 || a.CounterRatio != 0 || a.RadiusPerCounter != 0 || a.ConsumeCounter))
                report.Error($"{label}: remembered grant requires a direct Transform child and a fixed positive AD/HP amount without ratios or consumption.");
        }
        if(a.Kind is MatrixOperationKind.RandomEnemyChannel or MatrixOperationKind.RetreatDualShot or MatrixOperationKind.HealingSeriesShield or MatrixOperationKind.KillRefundAttack)
        {
            if(a.Effects.Length!=0)report.Error($"{label}: completed hero action cannot contain nested effects.");
            if(a.Kind!=MatrixOperationKind.KillRefundAttack && a.Target!=MatrixTargetKind.Self)
                report.Error($"{label}: channel, dual shot and healing series require Self.");
            if(a.Kind==MatrixOperationKind.RandomEnemyChannel && (parent!=MatrixOperationKind.Windup||a.DurationTicks!=a.Count*a.IntervalTicks||a.SpellPowerRatio<=0||a.Amount<0||a.Amount>160))
                report.Error($"{label}: random enemy channel requires a complete fixed pulse schedule, positive spell ratio and bounded completion refund.");
            if(a.Kind==MatrixOperationKind.RetreatDualShot && (a.Amount is <=0 or >2||a.Range<=0||a.AttackRatio<=0||a.SpellPowerRatio<=0||a.EventRatio<0||a.EventRatio>1||a.EventRatio>0&&a.Radius<=0))
                report.Error($"{label}: dual shot requires bounded retreat, retained reach, attack/spell ratios and optional bounded bounce.");
            if(a.Kind==MatrixOperationKind.HealingSeriesShield && (a.Count<1||a.DurationTicks!=a.Count*a.IntervalTicks||a.MaxTargets!=2||a.SpellPowerRatio<=0||a.Amount is <=0 or >1))
                report.Error($"{label}: completed healing series requires two recipients at most, fixed full duration, spell healing and shield conversion in (0,1].");
            if(a.Kind==MatrixOperationKind.KillRefundAttack && (parent!=MatrixOperationKind.DashStrike||a.Target!=MatrixTargetKind.EventTarget||a.AttackRatio<=0||a.Amount<=0||a.Amount>160))
                report.Error($"{label}: direct kill refund requires the arrived EventTarget, attack ratio and bounded mana refund.");
        }
        var effects = a.Effects.Select((child,i)=>child is null ? null : CompileNode(child,$"{label}.Effects[{i}]",report,visiting,depth+1,a.Kind))
            .Where(child=>child is not null).Cast<CompiledMatrixOperation>().ToImmutableArray();
        if(effects.Length != a.Effects.Length) report.Error($"{label}: missing nested effect.");
        visiting.Remove(a);
        return new(a.Kind,a.Target,a.Range,a.Radius,a.MaxTargets,a.ExcludeSelf,a.Amount,a.AttackRatio,a.OwnerHealthRatio,
            a.TargetHealthRatio,a.EventRatio,a.CounterRatio,a.EventTargetHealthRatio,a.RadiusPerCounter,a.CounterOnTarget,a.ShieldCap,a.TemporaryScale,a.Maximum,a.Key,a.RequiredCounter,a.CounterThreshold,
            a.ConsumeCounter,a.CounterConsumeRatio,a.LandingSide,a.ResetOnTargetChange,a.DurationTicks,a.Count,a.IntervalTicks,a.Attribute,a.ContentId,a.Event,
            a.SourceRelation,a.TargetRelation,a.TargetCondition,a.OwnerCondition,a.HealthThreshold,a.MinimumEventValue,a.CooldownTicks,
            a.Every,a.PerTargetCooldown,a.OncePerTarget,a.OncePerSource,a.OncePerAction,a.OncePerBattle,a.ActiveSkillOnly,a.TemporaryOnly,a.LivingOwnerRequired,a.ClearOnDeath,effects,a.SpellPowerRatio);
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
