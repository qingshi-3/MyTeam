using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

internal interface IBattleValueWorld
{
    int Tick { get; }
    bool ExecutingEcho { get; }
    BattleCombatEvent? TriggerEvent { get; }
    IReadOnlyList<BattleUnitState> Units { get; }
    EffectWorldSnapshot Snapshot(int tick);
    CombatSourceRef AbilityOrigin(CompiledAbilityDefinition ability, string ownerId);
    float Damage(string source, BattleUnitState owner, BattleUnitState target, float amount, CombatSourceRef origin, EffectDamageType type, bool isAreaDamage = false);
    float Heal(string source, BattleUnitState target, float amount, CombatSourceRef origin);
    float Shield(string source, BattleUnitState target, float amount, CombatSourceRef origin);
    void ShieldConsumed(BattleUnitState target, float amount, bool enemyDamage, CombatSourceRef origin);
    void EnqueueEcho(string donor, CompiledAbilityDefinition echo, string owner);
    void Present(string type, string source, string target, float value, Vector2 position, string cue, BattleVfxCue? vfx = null);
}

internal sealed class BattleValueOperations(IBattleValueWorld world, BattleValueLedger values,
    BattleLifecycleScope lifecycles, BattleStatusScope statuses, BattleStatistics statistics)
{
    private static bool TargetsArea(CompiledEffectTargetQuery query) => query is CompiledRelativeTeamTargetQuery or
        CompiledFilteredTargetQuery { MaxTargets: not 1 };

    private float ReadBattleValue(CompiledBattleValueOperation operation, BattleUnitState owner, BattleUnitState target)
    {
        double value = operation.Amount;
        foreach (var term in operation.Terms)
        {
            var subject = term.Subject switch {
                BattleValueSubject.Owner => owner,
                BattleValueSubject.Target => target,
                BattleValueSubject.EventSource => world.Units.FirstOrDefault(u => u.RuntimeId == world.TriggerEvent?.SourceRuntimeId),
                BattleValueSubject.EventTarget => world.Units.FirstOrDefault(u => u.RuntimeId == world.TriggerEvent?.TargetRuntimeId),
                _ => null };
            if (subject is null) throw new InvalidOperationException("Battle value lacks its declared event subject.");
            var read = term.Metric switch {
                BattleValueMetric.Attribute => subject.Attributes.GetValue(term.Attribute),
                BattleValueMetric.Health => subject.Health,
                BattleValueMetric.IsTemporary => subject.IsTemporary ? 1 : 0,
                BattleValueMetric.MissingHealth => Math.Max(0,subject.MaxHealth-subject.Health),
                BattleValueMetric.HealthRatio => subject.Health / subject.MaxHealth,
                BattleValueMetric.Shield => subject.Shield,
                BattleValueMetric.Counter => values.ReadCounter(subject,term.Key,term.TeamShared),
                BattleValueMetric.EventOrdinal => world.TriggerEvent?.CurrentStacks ?? throw new InvalidOperationException("Missing event ordinal."),
                BattleValueMetric.EventEffective => world.TriggerEvent?.EffectiveValue ?? throw new InvalidOperationException("Missing event value."),
                BattleValueMetric.EventOverheal => world.TriggerEvent is { } e ? Math.Max(0,e.AppliedValue-e.EffectiveValue) : throw new InvalidOperationException("Missing heal event."),
                BattleValueMetric.StatusStacks => subject.Statuses.Where(s => s.StableId == term.Key && (!term.OwnStatusOnly || s.SourceId == owner.RuntimeId)).Sum(s => s.Stacks),
                BattleValueMetric.HarmfulKinds => subject.Statuses.Where(s => s.Disposition == StatusDisposition.Harmful).Select(s => s.StableId).Distinct().Count(),
                BattleValueMetric.BattleSeconds => world.Tick * .1f,
                BattleValueMetric.AttackActions => statistics.AttackActions(subject.RuntimeId),
                BattleValueMetric.OwnedSummons => world.Units.Count(u => u.Alive && u.SummonerRuntimeId == subject.RuntimeId && u.Team == subject.Team),
                BattleValueMetric.IntrinsicArmor => values.IntrinsicArmor(subject),
                _ => throw new InvalidOperationException("Unknown battle value metric.") };
            value += read * term.Scale;
        }
        if (!double.IsFinite(value) || value > float.MaxValue || value < -float.MaxValue)
            throw new InvalidOperationException("Battle formula overflow.");
        return (float)value;
    }

    public bool TargetMatches(BattleTargetPolicy policy, BattleUnitState owner, BattleUnitState target) => policy switch {
        BattleTargetPolicy.Any => target.Alive,
        BattleTargetPolicy.Wounded => target.Alive && target.Health < target.MaxHealth,
        BattleTargetPolicy.Temporary => target.Alive && target.IsTemporary && target.Team == owner.Team,
        BattleTargetPolicy.Corpse => !target.Alive && target.Team != owner.Team && !target.Definition.IsBoss &&
            !lifecycles.CorpseSpent(target.RuntimeId) && !lifecycles.RevivalPending(target.RuntimeId),
        BattleTargetPolicy.NonBossEnemy => target.Alive && target.Team != owner.Team && !target.Definition.IsBoss &&
            !lifecycles.IsLeased(target.RuntimeId),
        BattleTargetPolicy.BehindAlly => target.Alive && target != owner && target.Team == owner.Team &&
            (target.Position.X-owner.Position.X) * (owner.Team == 0 ? 1 : -1) < -.1f,
        BattleTargetPolicy.EchoableAlly => target.Alive && target != owner && target.Team == owner.Team &&
            target.AbilityLoadout?.Abilities.Any(a => a.Echoable) == true,
        _ => false };

    public ImmutableArray<string> ResolveBattleTargets(CompiledEffectTargetQuery query, BattleUnitState owner,
        string explicitTarget, BattleTargetPolicy policy = BattleTargetPolicy.Any)
    {
        // Apply semantic eligibility before MaxTargets so an ineligible nearest body cannot hide a legal target.
        var expanded = query is CompiledFilteredTargetQuery filtered ? filtered with { MaxTargets = 0 } : query;
        var ids = EffectTargetResolver.Resolve(expanded,world.Snapshot(world.Tick),owner.RuntimeId,owner.RuntimeId,explicitTarget)
            .Where(id => world.Units.Any(u => u.RuntimeId == id && TargetMatches(policy,owner,u)));
        if (query is CompiledFilteredTargetQuery { MaxTargets: > 0 } bounded) ids = ids.Take(bounded.MaxTargets);
        return ids.ToImmutableArray();
    }

    public bool PrepareBattleOperation(CompiledAbilityOperation operation, BattleUnitState owner, string explicitTarget,
        out ImmutableArray<string> targets)
    {
        targets = [];
        switch (operation)
        {
            case CompiledBattleValueOperation value:
                targets = ResolveBattleTargets(value.TargetQuery,owner,explicitTarget,value.TargetPolicy);
                if (value.Action == BattleValueAction.Require)
                    targets = targets.Where(id => {
                        float n = ReadBattleValue(value,owner,world.Units.First(u => u.RuntimeId == id));
                        return n >= value.Minimum && n <= value.Maximum &&
                            (value.Every <= 1 || n > 0 && Math.Abs(n % value.Every) < .001f);
                    }).ToImmutableArray();
                if (value.Action == BattleValueAction.ConsumeShield) targets = targets.Where(id => world.Units.First(u => u.RuntimeId == id).Shield > 0).ToImmutableArray();
                return targets.Length > 0 || value.Action == BattleValueAction.SetAttributeContribution && value.Broadcast;
            case CompiledConsumeStatusOperation consume:
                targets = ResolveBattleTargets(consume.TargetQuery,owner,explicitTarget)
                    .Where(id => statuses.RemainingPeriodicDamage(id,consume.StatusId,world.Tick) > 0).ToImmutableArray();
                return targets.Length > 0;
            case CompiledEchoOperation echo:
                if (world.ExecutingEcho) return false;
                targets = world.Units.Where(u => TargetMatches(BattleTargetPolicy.EchoableAlly,owner,u) &&
                        u.Position.DistanceTo(owner.Position) <= echo.Range && (!echo.BehindOnly || TargetMatches(BattleTargetPolicy.BehindAlly,owner,u)))
                    .OrderBy(u => u.Position.DistanceSquaredTo(owner.Position)).ThenBy(u => u.RuntimeId,StringComparer.Ordinal)
                    .Take(1).Select(u => u.RuntimeId).ToImmutableArray();
                return targets.Length > 0;
            case CompiledLifecycleOperation lifecycle:
                return lifecycles.PrepareLifecycleOperation(lifecycle,owner,explicitTarget,out targets);
            default: return false;
        }
    }

    public void ExecuteBattleOperation(ResolvedAbilityOperation resolved, AbilityExecutionPlan plan, BattleUnitState owner)
    {
        var origin = world.AbilityOrigin(plan.Ability,owner.RuntimeId);
        switch (resolved.Operation)
        {
            case CompiledBattleValueOperation value:
                if (value.Broadcast)
                {
                    var prefix = $"{owner.RuntimeId}:{plan.Ability.StableId}:{resolved.OperationIndex}:";
                    values.RemoveObsoleteBroadcast(prefix, resolved.TargetIds);
                }
                foreach (var id in resolved.TargetIds)
                {
                    var target = world.Units.First(u => u.RuntimeId == id);
                    if (!target.Alive) continue;
                    var amount = Math.Clamp(ReadBattleValue(value,owner,target),value.Minimum,value.Maximum);
                    switch (value.Action)
                    {
                        case BattleValueAction.Damage:
                            world.Damage(owner.RuntimeId,owner,target,Math.Max(0,amount),origin,value.DamageType,isAreaDamage:TargetsArea(value.TargetQuery)); break;
                        case BattleValueAction.Heal:
                            var healed = world.Heal(owner.RuntimeId,target,Math.Max(0,amount),origin);
                            if (healed > 0) world.Present("heal",owner.RuntimeId,id,healed,target.Position,"heal");
                            break;
                        case BattleValueAction.Shield: world.Shield(owner.RuntimeId,target,Math.Max(0,amount),origin); break;
                        case BattleValueAction.Mana:
                            if (amount > 0) BattleHeroMana.Gain(target, amount, world.Tick);
                            else target.CurrentMana = Math.Clamp(target.CurrentMana + amount, 0, target.MaxMana);
                            break;
                        case BattleValueAction.AddCounter: values.WriteCounter(target,value.CounterKey,values.ReadCounter(target,value.CounterKey,value.TeamShared)+amount,value.TeamShared); break;
                        case BattleValueAction.SetCounter: values.WriteCounter(target,value.CounterKey,amount,value.TeamShared); break;
                        case BattleValueAction.SetAttributeContribution:
                            values.SetValueContribution($"{owner.RuntimeId}:{plan.Ability.StableId}:{resolved.OperationIndex}:{id}",owner,target,value.Attribute,amount,value.Broadcast,origin); break;
                        case BattleValueAction.Require: break;
                        case BattleValueAction.ConsumeShield:
                            var spent = Math.Min(target.Shield,Math.Max(0,amount));
                            target.Shield -= spent;
                            world.ShieldConsumed(target,spent,false,origin);
                            values.WriteCounter(owner,value.CounterKey,spent,value.TeamShared);
                            world.Present("vfx",owner.RuntimeId,id,spent,target.Position,"",vfx:new BattleVfxCue(BattleVfxPhase.ShieldDepleted,"shield"));
                            break;
                    }
                }
                break;
            case CompiledConsumeStatusOperation consume:
                foreach (var id in resolved.TargetIds)
                {
                    var target = world.Units.First(u => u.RuntimeId == id);
                    if (!target.Alive) continue;
                    var stored = statuses.ConsumePeriodicDamage(id,consume.StatusId,world.Tick);
                    if (stored > 0) world.Damage(owner.RuntimeId,owner,target,stored*consume.DamageMultiplier,origin,consume.DamageType,isAreaDamage:TargetsArea(consume.TargetQuery));
                }
                break;
            case CompiledEchoOperation:
                var donor = world.Units.First(u => u.RuntimeId == resolved.TargetIds[0]);
                foreach (var echo in donor.AbilityLoadout!.Abilities.Where(a => a.Echoable).OrderBy(a => a.StableId,StringComparer.Ordinal))
                    world.EnqueueEcho(donor.RuntimeId, echo, owner.RuntimeId);
                break;
            case CompiledLifecycleOperation lifecycle: lifecycles.ExecuteLifecycleOperation(lifecycle,resolved.TargetIds,owner,origin); break;
        }
    }

}
