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

public sealed partial class BattleSimulation
{
    private sealed record ValueContribution(string SourceId, string TargetId, CombatAttribute Attribute,
        float Amount, AttributeModifierHandle Handle, bool Broadcast);
    private sealed record Revival(string OwnerId, int ReadyTick, float HealthRatio);
    private sealed record AllegianceLease(string OwnerId, string TargetId, int OriginalTeam, int EndTick, int BeginTick, bool Active, CompiledEffectTargetQuery TargetQuery);
    // One immutable value graph is captured by every world checkpoint. No content Resource owns a ledger.
    private sealed record MechanicState(
        ImmutableDictionary<string,float> Counters,
        ImmutableDictionary<string,ValueContribution> Contributions,
        ImmutableArray<Revival> Revivals,
        ImmutableArray<AllegianceLease> Allegiances,
        ImmutableHashSet<string> SpentCorpses)
    {
        public static MechanicState Empty => new(ImmutableDictionary<string,float>.Empty,
            ImmutableDictionary<string,ValueContribution>.Empty, [], [], ImmutableHashSet<string>.Empty);
    }
    private MechanicState _mechanics = MechanicState.Empty;
    private BattleCombatEvent? _abilityTriggerEvent;
    private bool _executingEcho;

    private static string CounterAddress(BattleUnitState unit, string key, bool shared) =>
        shared ? $"team:{unit.Team}:{key}" : $"unit:{unit.RuntimeId}:{key}";
    private float ReadCounter(BattleUnitState unit, string key, bool shared = false) =>
        _mechanics.Counters.GetValueOrDefault(CounterAddress(unit,key,shared));
    private void WriteCounter(BattleUnitState unit, string key, float value, bool shared = false)
    {
        if (!float.IsFinite(value)) throw new InvalidOperationException("Battle counter overflow.");
        _mechanics = _mechanics with { Counters = _mechanics.Counters.SetItem(CounterAddress(unit,key,shared), Math.Max(0,value)) };
    }

    private float ReadBattleValue(CompiledBattleValueOperation operation, BattleUnitState owner, BattleUnitState target)
    {
        double value = operation.Amount;
        foreach (var term in operation.Terms)
        {
            var subject = term.Subject switch {
                BattleValueSubject.Owner => owner,
                BattleValueSubject.Target => target,
                BattleValueSubject.EventSource => _units.FirstOrDefault(u => u.RuntimeId == _abilityTriggerEvent?.SourceRuntimeId),
                BattleValueSubject.EventTarget => _units.FirstOrDefault(u => u.RuntimeId == _abilityTriggerEvent?.TargetRuntimeId),
                _ => null };
            if (subject is null) throw new InvalidOperationException("Battle value lacks its declared event subject.");
            var read = term.Metric switch {
                BattleValueMetric.Attribute => subject.Attributes.GetValue(term.Attribute),
                BattleValueMetric.Health => subject.Health,
                BattleValueMetric.IsTemporary => subject.IsTemporary ? 1 : 0,
                BattleValueMetric.MissingHealth => Math.Max(0,subject.MaxHealth-subject.Health),
                BattleValueMetric.HealthRatio => subject.Health / subject.MaxHealth,
                BattleValueMetric.Shield => subject.Shield,
                BattleValueMetric.Counter => ReadCounter(subject,term.Key,term.TeamShared),
                BattleValueMetric.EventOrdinal => _abilityTriggerEvent?.CurrentStacks ?? throw new InvalidOperationException("Missing event ordinal."),
                BattleValueMetric.EventEffective => _abilityTriggerEvent?.EffectiveValue ?? throw new InvalidOperationException("Missing event value."),
                BattleValueMetric.EventOverheal => _abilityTriggerEvent is { } e ? Math.Max(0,e.AppliedValue-e.EffectiveValue) : throw new InvalidOperationException("Missing heal event."),
                BattleValueMetric.StatusStacks => subject.Statuses.Where(s => s.StableId == term.Key && (!term.OwnStatusOnly || s.SourceId == owner.RuntimeId)).Sum(s => s.Stacks),
                BattleValueMetric.HarmfulKinds => subject.Statuses.Where(s => s.Disposition == StatusDisposition.Harmful).Select(s => s.StableId).Distinct().Count(),
                BattleValueMetric.BattleSeconds => TickIndex * .1f,
                BattleValueMetric.AttackActions => _statistics[subject.RuntimeId].AttackActions,
                BattleValueMetric.OwnedSummons => _units.Count(u => u.Alive && u.SummonerRuntimeId == subject.RuntimeId && u.Team == subject.Team),
                BattleValueMetric.IntrinsicArmor => subject.Armor - _mechanics.Contributions.Values.Where(c => c.Broadcast && c.TargetId == subject.RuntimeId && c.Attribute == CombatAttribute.Armor).Sum(c => c.Amount),
                _ => throw new InvalidOperationException("Unknown battle value metric.") };
            value += read * term.Scale;
        }
        if (!double.IsFinite(value) || value > float.MaxValue || value < -float.MaxValue)
            throw new InvalidOperationException("Battle formula overflow.");
        return (float)value;
    }

    private bool TargetMatches(BattleTargetPolicy policy, BattleUnitState owner, BattleUnitState target) => policy switch {
        BattleTargetPolicy.Any => target.Alive,
        BattleTargetPolicy.Wounded => target.Alive && target.Health < target.MaxHealth,
        BattleTargetPolicy.Temporary => target.Alive && target.IsTemporary && target.Team == owner.Team,
        BattleTargetPolicy.Corpse => !target.Alive && target.Team != owner.Team && !target.Definition.IsBoss &&
            !_mechanics.SpentCorpses.Contains(target.RuntimeId) && !_mechanics.Revivals.Any(r => r.OwnerId == target.RuntimeId),
        BattleTargetPolicy.NonBossEnemy => target.Alive && target.Team != owner.Team && !target.Definition.IsBoss &&
            !_mechanics.Allegiances.Any(a => a.TargetId == target.RuntimeId),
        BattleTargetPolicy.BehindAlly => target.Alive && target != owner && target.Team == owner.Team &&
            (target.Position.X-owner.Position.X) * (owner.Team == 0 ? 1 : -1) < -.1f,
        BattleTargetPolicy.EchoableAlly => target.Alive && target != owner && target.Team == owner.Team &&
            target.Definition.AbilityLoadout?.Abilities.Any(a => a.Echoable) == true,
        _ => false };

    private ImmutableArray<string> ResolveBattleTargets(CompiledEffectTargetQuery query, BattleUnitState owner,
        string explicitTarget, BattleTargetPolicy policy = BattleTargetPolicy.Any)
    {
        // Apply semantic eligibility before MaxTargets so an ineligible nearest body cannot hide a legal target.
        var expanded = query is CompiledFilteredTargetQuery filtered ? filtered with { MaxTargets = 0 } : query;
        var ids = EffectTargetResolver.Resolve(expanded,CaptureEffectSnapshot(TickIndex),owner.RuntimeId,owner.RuntimeId,explicitTarget)
            .Where(id => _units.Any(u => u.RuntimeId == id && TargetMatches(policy,owner,u)));
        if (query is CompiledFilteredTargetQuery { MaxTargets: > 0 } bounded) ids = ids.Take(bounded.MaxTargets);
        return ids.ToImmutableArray();
    }

    private bool PrepareBattleOperation(CompiledAbilityOperation operation, BattleUnitState owner, string explicitTarget,
        out ImmutableArray<string> targets)
    {
        targets = [];
        switch (operation)
        {
            case CompiledBattleValueOperation value:
                targets = ResolveBattleTargets(value.TargetQuery,owner,explicitTarget,value.TargetPolicy);
                if (value.Action == BattleValueAction.Require)
                    targets = targets.Where(id => {
                        float n = ReadBattleValue(value,owner,_units.First(u => u.RuntimeId == id));
                        return n >= value.Minimum && n <= value.Maximum &&
                            (value.Every <= 1 || n > 0 && Math.Abs(n % value.Every) < .001f);
                    }).ToImmutableArray();
                if (value.Action == BattleValueAction.ConsumeShield) targets = targets.Where(id => _units.First(u => u.RuntimeId == id).Shield > 0).ToImmutableArray();
                return targets.Length > 0 || value.Action == BattleValueAction.SetAttributeContribution && value.Broadcast;
            case CompiledConsumeStatusOperation consume:
                targets = ResolveBattleTargets(consume.TargetQuery,owner,explicitTarget)
                    .Where(id => _statusScope.RemainingPeriodicDamage(id,consume.StatusId,TickIndex) > 0).ToImmutableArray();
                return targets.Length > 0;
            case CompiledEchoOperation echo:
                if (_executingEcho) return false;
                targets = _units.Where(u => TargetMatches(BattleTargetPolicy.EchoableAlly,owner,u) &&
                        u.Position.DistanceTo(owner.Position) <= echo.Range && (!echo.BehindOnly || TargetMatches(BattleTargetPolicy.BehindAlly,owner,u)))
                    .OrderBy(u => u.Position.DistanceSquaredTo(owner.Position)).ThenBy(u => u.RuntimeId,StringComparer.Ordinal)
                    .Take(1).Select(u => u.RuntimeId).ToImmutableArray();
                return targets.Length > 0;
            case CompiledLifecycleOperation lifecycle:
                return PrepareLifecycleOperation(lifecycle,owner,explicitTarget,out targets);
            default: return false;
        }
    }

    private void ExecuteBattleOperation(ResolvedAbilityOperation resolved, AbilityExecutionPlan plan, BattleUnitState owner)
    {
        var origin = AbilityOrigin(plan.Ability,owner.RuntimeId);
        switch (resolved.Operation)
        {
            case CompiledBattleValueOperation value:
                if (value.Broadcast)
                {
                    var prefix = $"{owner.RuntimeId}:{plan.Ability.StableId}:{resolved.OperationIndex}:";
                    foreach (var pair in _mechanics.Contributions.Where(p => p.Key.StartsWith(prefix,StringComparison.Ordinal) && !resolved.TargetIds.Contains(p.Value.TargetId)).ToArray())
                    {
                        _units.First(u => u.RuntimeId == pair.Value.TargetId).Attributes.Remove(pair.Value.Handle);
                        _mechanics = _mechanics with { Contributions = _mechanics.Contributions.Remove(pair.Key) };
                    }
                }
                foreach (var id in resolved.TargetIds)
                {
                    var target = _units.First(u => u.RuntimeId == id);
                    if (!target.Alive) continue;
                    var amount = Math.Clamp(ReadBattleValue(value,owner,target),value.Minimum,value.Maximum);
                    switch (value.Action)
                    {
                        case BattleValueAction.Damage:
                            ApplyDamage(owner.RuntimeId,owner,target,Math.Max(0,amount),origin,value.DamageType); break;
                        case BattleValueAction.Heal:
                            var healed = HealLiving(owner.RuntimeId,target,Math.Max(0,amount),origin);
                            if (healed > 0) Emit("heal",owner.RuntimeId,id,healed,target.Position,"heal");
                            break;
                        case BattleValueAction.Shield: ApplyShield(owner.RuntimeId,target,Math.Max(0,amount),origin); break;
                        case BattleValueAction.Mana: target.CurrentMana = Math.Clamp(target.CurrentMana+amount,0,target.MaxMana); break;
                        case BattleValueAction.AddCounter: WriteCounter(target,value.CounterKey,ReadCounter(target,value.CounterKey,value.TeamShared)+amount,value.TeamShared); break;
                        case BattleValueAction.SetCounter: WriteCounter(target,value.CounterKey,amount,value.TeamShared); break;
                        case BattleValueAction.SetAttributeContribution:
                            SetValueContribution($"{owner.RuntimeId}:{plan.Ability.StableId}:{resolved.OperationIndex}:{id}",owner,target,value.Attribute,amount,value.Broadcast,origin); break;
                        case BattleValueAction.Require: break;
                        case BattleValueAction.ConsumeShield:
                            var spent = Math.Min(target.Shield,Math.Max(0,amount));
                            target.Shield -= spent;
                            ConsumeTimedShields(target.RuntimeId,spent);
                            WriteCounter(owner,value.CounterKey,spent,value.TeamShared);
                            Emit("vfx",owner.RuntimeId,id,spent,target.Position,"",vfx:new BattleVfxCue(BattleVfxPhase.ShieldDepleted,"shield"));
                            break;
                    }
                }
                break;
            case CompiledConsumeStatusOperation consume:
                foreach (var id in resolved.TargetIds)
                {
                    var target = _units.First(u => u.RuntimeId == id);
                    if (!target.Alive) continue;
                    var stored = _statusScope.ConsumePeriodicDamage(id,consume.StatusId,TickIndex);
                    if (stored > 0) ApplyDamage(owner.RuntimeId,owner,target,stored*consume.DamageMultiplier,origin,consume.DamageType);
                }
                break;
            case CompiledEchoOperation:
                var donor = _units.First(u => u.RuntimeId == resolved.TargetIds[0]);
                foreach (var echo in donor.Definition.AbilityLoadout!.Abilities.Where(a => a.Echoable).OrderBy(a => a.StableId,StringComparer.Ordinal))
                    _pendingAbilityReactions.Add(new PendingAbilityReaction(donor.RuntimeId,AbilityTriggerKind.OwnerDefeated,
                        "",TickIndex,$"{_combatPipeline.ScopeId}:echo:{owner.RuntimeId}:{TickIndex}",1,Echo:echo));
                break;
            case CompiledLifecycleOperation lifecycle: ExecuteLifecycleOperation(lifecycle,resolved.TargetIds,owner,origin); break;
        }
    }

    private void SetValueContribution(string key, BattleUnitState source, BattleUnitState target, CombatAttribute attribute,
        float amount, bool broadcast, CombatSourceRef origin)
    {
        if (_mechanics.Contributions.TryGetValue(key,out var old))
        {
            if (Math.Abs(old.Amount-amount) < .00001f) return;
            target.Attributes.Remove(old.Handle);
        }
        var handle = target.Attributes.ApplyModifier(new CompiledAttributeModifier(attribute,AttributeModifierOperation.Add,
            new CompiledConstantMagnitude(amount),0,key),origin);
        _mechanics = _mechanics with { Contributions = _mechanics.Contributions.SetItem(key,new(source.RuntimeId,target.RuntimeId,attribute,amount,handle,broadcast)) };
    }

    private static string ResourceLabel(string key) => key switch {
        "grit" => "怒劲", "surge" => "全队共鸣", "shield_gifts" => "受盾次数", "wounds" => "蓄痛", "frost_history" => "历霜",
        "death_power" => "送葬", "eaten_attack" => "吞食攻击", "eaten_health" => "吞食生命",
        "raised_corpses" => "已用敌尸", "revivals" => "返场次数", "spent_shield" => "上次耗盾", _ => key };

    private void RecordBattleRevival(BattleUnitState unit)
    {
        var waiting = Math.Max(0,TickIndex-ReadCounter(unit,"_dead_since"));
        WriteCounter(unit,"_inactive_ticks",ReadCounter(unit,"_inactive_ticks")+waiting);
    }

    private int ActiveBattleTicks(BattleUnitState unit) => Math.Max(1,
        TickIndex-_statistics[unit.RuntimeId].JoinTick-(int)ReadCounter(unit,"_inactive_ticks")-
        (unit.Alive ? 0 : Math.Max(0,TickIndex-(int)ReadCounter(unit,"_dead_since"))));

    private void CancelAllegianceProjectiles(BattleUnitState unit)
    {
        foreach (var projectile in Projectiles.Where(p => p.SourceId == unit.RuntimeId))
        {
            _projectiles.Remove(projectile.Id);
            Emit("projectile_end",unit.RuntimeId,"",0,projectile.Position,"",projectile.Id);
        }
    }

    public string DescribeBattleResources(string runtimeId)
    {
        var unit = _units.FirstOrDefault(u => u.RuntimeId == runtimeId);
        if (unit is null) return "";
        var own = $"unit:{runtimeId}:";
        var team = $"team:{unit.Team}:";
        return string.Join("  ",_mechanics.Counters.Where(p => (p.Key.StartsWith(own,StringComparison.Ordinal) || p.Key.StartsWith(team,StringComparison.Ordinal)) && !p.Key[(p.Key.LastIndexOf(':')+1)..].StartsWith("_",StringComparison.Ordinal))
            .OrderBy(p => p.Key,StringComparer.Ordinal).Select(p => $"{ResourceLabel(p.Key[(p.Key.LastIndexOf(':')+1)..])} {p.Value:0.#}"));
    }
}
