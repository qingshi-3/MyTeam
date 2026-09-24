using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Domain;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private sealed record DuelLease(string Source, string Target, int SourceTeam, int TargetTeam, int EndTick, float BreakDistance);
    private sealed record GritSample(int Expires, float Amount);
    private sealed record GritLedger(string Owner, string Key, ImmutableArray<GritSample> Samples);
    private sealed record TimedShield(string Owner, int Expires, float Remaining);
    private sealed record PunchCast(string Owner, int Team, Vector2 Start, Vector2 End, int StartTick,
        float Damage, CompiledGritPunchOperation Skill, CombatSourceRef Origin);
    private sealed record HookCast(string Owner, int Team, Vector2 Start, Vector2 End, Vector2 Position,
        int StartTick, CompiledHookOperation Skill, CombatSourceRef Origin, int ReturningAt = -1, string Caught = "");
    // One immutable graph; the normal world transaction captures this together with health,
    // mana, effects, movement reservations, event queues and random state.
    private sealed record TechniqueState(ImmutableArray<DuelLease> Duels, ImmutableDictionary<string,GritLedger> Grit,
        ImmutableArray<TimedShield> Shields, ImmutableDictionary<string,PunchCast> Punches,
        ImmutableDictionary<string,HookCast> Hooks, ImmutableDictionary<string,GritTriggerLatch> GritTriggers)
    {
        public static TechniqueState Empty => new([], ImmutableDictionary<string,GritLedger>.Empty, [],
            ImmutableDictionary<string,PunchCast>.Empty, ImmutableDictionary<string,HookCast>.Empty,
            ImmutableDictionary<string,GritTriggerLatch>.Empty);
    }
    private TechniqueState _techniques = TechniqueState.Empty;
    private BattleUnitState? TechniqueUnit(string id) => _units.FirstOrDefault(u => u.RuntimeId == id);
    private bool TechniqueBusy(BattleUnitState owner) => _techniques.Hooks.ContainsKey(owner.RuntimeId) || _techniques.Punches.ContainsKey(owner.RuntimeId);
    private bool TechniqueControlled(BattleUnitState owner) => owner.DisabledTicks > 0 ||
        _statusScope.HasTag(owner.RuntimeId, StatusDefinitionCompiler.ActionDisabledTag);
    private bool DuelValid(DuelLease duel) => TickIndex < duel.EndTick &&
        TechniqueUnit(duel.Source) is { Alive: true } a && TechniqueUnit(duel.Target) is { Alive: true } b &&
        a.Team == duel.SourceTeam && b.Team == duel.TargetTeam && a.Team != b.Team && a.Position.DistanceTo(b.Position) <= duel.BreakDistance;
    private BattleUnitState? DuelOpponent(BattleUnitState unit)
    {
        var duel = _techniques.Duels.FirstOrDefault(d => (d.Source == unit.RuntimeId || d.Target == unit.RuntimeId) && DuelValid(d));
        return duel is null ? null : TechniqueUnit(duel.Source == unit.RuntimeId ? duel.Target : duel.Source);
    }
    private bool CanAimHook(BattleUnitState owner, BattleUnitState target, CompiledHookOperation skill) =>
        target.Alive && target.Team != owner.Team && owner.Position.DistanceTo(target.Position) <= skill.Range &&
        BattlefieldSpace.EdgeDistance(owner,target) > .35f &&
        BattlefieldSpace.FirstTerrainHitFraction(owner.Position,target.Position-owner.Position,skill.Radius,
            Width,Height,cell => _config.FloorRule.CanOccupy(cell)) >= 1;

    private bool PrepareTechnique(CompiledCombatTechniqueOperation op, BattleUnitState owner, string targetId,
        out ImmutableArray<string> targets)
    {
        targets = [owner.RuntimeId];
        var target = TechniqueUnit(targetId);
        switch (op)
        {
            case CompiledCounterattackOperation:
                return _abilityTriggerEvent is { Kind: BattleCombatEventKind.AttackLanded } e && e.TargetRuntimeId == owner.RuntimeId;
            case CompiledGritStorageOperation:
                return _abilityTriggerEvent is { Kind: BattleCombatEventKind.HealthLost, EffectiveValue: > 0 } lost && lost.TargetRuntimeId == owner.RuntimeId;
            case CompiledDuelOperation:
                if (target is not { Alive: true } || target.Team == owner.Team || DuelOpponent(owner) is not null ||
                    DuelOpponent(target) is not null || IsDisplacing(target) || TechniqueBusy(target) || EnemyActionBusy(target) ||
                    !BattlefieldSpace.IsWithinReach(owner,target,owner.AttackRange) || !HasLineAccess(owner,target)) return false;
                targets = [targetId]; return true;
            case CompiledHookOperation hook:
                if (target is null || !CanAimHook(owner,target,hook)) return false;
                targets = [targetId]; return true;
            case CompiledGritPunchOperation punch:
                if (target is not { Alive: true } || target.Team == owner.Team || IsDisplacing(owner) ||
                    !owner.Definition.AbilityLoadout!.Abilities.Any(a => a.Operations.Contains(punch) && HasQueuedUnitAction(owner, a.StableId)) ||
                    owner.Position.DistanceTo(target.Position) > punch.Range || !HasLineAccess(owner,target)) return false;
                targets = [targetId];
                return true;
            default: return false;
        }
    }

    private float CurrentGrit(BattleUnitState owner, string key) =>
        _techniques.Grit.TryGetValue(CounterAddress(owner,key,false),out var ledger)
            ? ledger.Samples.Where(s => s.Expires > TickIndex).Sum(s => s.Amount) : 0;

    private void ExecuteTechnique(CompiledCombatTechniqueOperation op, BattleUnitState owner,
        ImmutableArray<string> targets, CompiledAbilityDefinition ability)
    {
        var origin = AbilityOrigin(ability,owner.RuntimeId);
        switch (op)
        {
            case CompiledCounterattackOperation counter:
                var key = "_counter_" + ability.StableId;
                var hits = ReadCounter(owner,key)+1;
                // A ready counter is retained while there is no reachable opponent; it is
                // retried by the next received attack, never fired through range or control.
                var target = DuelOpponent(owner) ?? TechniqueUnit(owner.ActionTargetRuntimeId) ?? TechniqueUnit(_abilityTriggerEvent!.SourceRuntimeId);
                if (hits < counter.HitsRequired || TechniqueControlled(owner) || TechniqueBusy(owner) || IsDisplacing(owner) ||
                    target is not { Alive: true } || target.Team == owner.Team ||
                    !BattlefieldSpace.IsWithinReach(owner,target,owner.AttackRange) || !HasLineAccess(owner,target))
                { WriteCounter(owner,key,Math.Min(hits,counter.HitsRequired)); break; }
                WriteCounter(owner,key,0);
                var before = target.Health;
                ResolveLineSkillHit(owner,target,owner.Damage*counter.AttackRatio,origin,EffectDamageType.Normal);
                if (owner.Alive)
                {
                    var healed = HealLiving(owner.RuntimeId,owner,Math.Max(0,before-target.Health)*counter.LifestealRatio,origin);
                    if (healed > 0) Emit("heal",owner.RuntimeId,owner.RuntimeId,healed,owner.Position,"");
                }
                Emit("counter_strike",owner.RuntimeId,target.RuntimeId,before-target.Health,target.Position,"attack",origin:owner.Position);
                break;
            case CompiledGritStorageOperation storage:
                var address = CounterAddress(owner,storage.CounterKey,false);
                var samples = _techniques.Grit.TryGetValue(address,out var ledger)
                    ? ledger.Samples.Where(s=>s.Expires>TickIndex).ToImmutableArray() : [];
                var room = Math.Max(0,owner.MaxHealth*storage.MaximumHealthRatio-samples.Sum(s=>s.Amount));
                var added = Math.Min(room,_abilityTriggerEvent!.EffectiveValue);
                if (added > 0) samples=samples.Add(new(TickIndex+storage.WindowTicks,added));
                _techniques = _techniques with { Grit = _techniques.Grit.SetItem(address,new(owner.RuntimeId,storage.CounterKey,samples)) };
                WriteCounter(owner,storage.CounterKey,samples.Sum(s=>s.Amount));
                RefreshGritActionRequests(owner);
                break;
            case CompiledDuelOperation duel:
                var opponent = TechniqueUnit(targets[0])!;
                _techniques = _techniques with { Duels = _techniques.Duels.Add(new(owner.RuntimeId,opponent.RuntimeId,
                    owner.Team,opponent.Team,TickIndex+duel.DurationTicks,duel.BreakDistance)) };
                foreach (var participant in new[] { owner,opponent })
                {
                    CancelProjectileWindups(participant); participant.ProjectileSequence=null;
                    CancelChargedLine(participant); CancelTrample(participant);
                    InterruptBattleChannels(participant.RuntimeId); _movement?.ReleaseUnit(participant.RuntimeId);
                }
                SetActionTarget(owner,opponent); SetActionTarget(opponent,owner);
                Emit("duel_begin",owner.RuntimeId,opponent.RuntimeId,duel.DurationTicks,opponent.Position,"",origin:owner.Position);
                break;
            case CompiledGritPunchOperation punch:
                var victim = TechniqueUnit(targets[0])!;
                ConsumeGritActionRequest(owner, ability, punch);
                var stored = CurrentGrit(owner,punch.CounterKey);
                _techniques = _techniques with { Grit = _techniques.Grit.Remove(CounterAddress(owner,punch.CounterKey,false)) };
                WriteCounter(owner,punch.CounterKey,0);
                var shield = ApplyShield(owner.RuntimeId,owner,stored,origin);
                if (shield > 0)
                    _techniques = _techniques with { Shields = _techniques.Shields.Add(new(owner.RuntimeId,TickIndex+punch.ShieldTicks,shield)) };
                var direction = (victim.Position-owner.Position).Normalized();
                var travel = direction*punch.Range;
                var terrain = BattlefieldSpace.FirstTerrainHitFraction(owner.Position,travel,punch.Radius,Width,Height,c=>_config.FloorRule.CanOccupy(c));
                var cast = new PunchCast(owner.RuntimeId,owner.Team,owner.Position,owner.Position+travel*terrain,TickIndex,
                    owner.Damage*punch.AttackRatio+stored*punch.GritDamageRatio,punch,origin);
                _techniques = _techniques with { Punches = _techniques.Punches.SetItem(owner.RuntimeId,cast) };
                HoldTechnique(owner);
                EmitPunch("line_charge",cast);
                break;
            case CompiledHookOperation hook:
                var aim = TechniqueUnit(targets[0])!;
                var delta = (aim.Position-owner.Position).Normalized()*hook.Range;
                var fraction = BattlefieldSpace.FirstTerrainHitFraction(owner.Position,delta,hook.Radius,Width,Height,c=>_config.FloorRule.CanOccupy(c));
                var flight = new HookCast(owner.RuntimeId,owner.Team,owner.Position,owner.Position+delta*fraction,
                    owner.Position,TickIndex,hook,origin);
                _techniques = _techniques with { Hooks = _techniques.Hooks.SetItem(owner.RuntimeId,flight) };
                HoldTechnique(owner);
                Emit("hook_start",owner.RuntimeId,"",hook.Radius,owner.Position,"",origin:owner.Position);
                break;
        }
    }

    private void HoldTechnique(BattleUnitState unit)
    {
        unit.Mode=BattleUnitMode.Casting;
        _movement?.ReleaseUnit(unit.RuntimeId);
        CancelProjectileWindups(unit); unit.ProjectileSequence=null;
    }
    private void EmitPunch(string type,PunchCast cast) => Emit(type,cast.Owner,cast.Owner,cast.Damage,cast.End,"",origin:cast.Start,
        attackTiming:type=="line_charge" ? new BattleAttackTiming(
            (cast.Skill.ChargeTicks+cast.Skill.RecoveryTicks)*BattleTiming.TickSeconds,
            (float)cast.Skill.ChargeTicks/(cast.Skill.ChargeTicks+cast.Skill.RecoveryTicks)) : null,
        line:new("grit_charge","grit_punch",cast.StartTick,cast.Skill.ChargeTicks,cast.Skill.Radius,ChargedLineDelivery.Beam,
            (float)cast.Skill.ChargeTicks/(cast.Skill.ChargeTicks+cast.Skill.RecoveryTicks)));

    private void ConsumeTimedShields(string ownerId,float amount)
    {
        var shields=_techniques.Shields.ToBuilder();
        for(var i=0;i<shields.Count && amount>0;i++)
        {
            if(shields[i].Owner!=ownerId)continue;
            var consumed=Math.Min(shields[i].Remaining,amount);amount-=consumed;
            shields[i]=shields[i] with {Remaining=shields[i].Remaining-consumed};
        }
        _techniques=_techniques with {Shields=shields.Where(s=>s.Remaining>0).ToImmutableArray()};
    }

    private void AdvanceTechniques()
    {
        if(_techniques==TechniqueState.Empty)return;
        var checkpoint=new BattleWorldStateCheckpoint(this);
        try
        {
            using var resolution=_combatPipeline.BeginAuthoritativeResolution();
            foreach(var duel in _techniques.Duels.Where(d=>!DuelValid(d)).ToArray()) EndDuel(duel);
            foreach(var pair in _techniques.Grit.OrderBy(p=>p.Key,StringComparer.Ordinal).ToArray())
            {
                var owner=TechniqueUnit(pair.Value.Owner);
                var samples=pair.Value.Samples.Where(s=>s.Expires>TickIndex && owner is {Alive:true}).ToImmutableArray();
                _techniques=_techniques with {Grit=samples.IsEmpty?_techniques.Grit.Remove(pair.Key):
                    _techniques.Grit.SetItem(pair.Key,pair.Value with {Samples=samples})};
                if(owner is not null)WriteCounter(owner,pair.Value.Key,samples.Sum(s=>s.Amount));
            }
            foreach(var shield in _techniques.Shields.Where(s=>s.Expires<=TickIndex).ToArray())
            {
                if(TechniqueUnit(shield.Owner) is {Alive:true} unit)
                {
                    unit.Shield=Math.Max(0,unit.Shield-shield.Remaining);
                    Emit("vfx",unit.RuntimeId,unit.RuntimeId,0,unit.Position,"",vfx:new(
                        unit.Shield>0?BattleVfxPhase.ShieldActive:BattleVfxPhase.ShieldDepleted,"shield"));
                }
            }
            _techniques=_techniques with {Shields=_techniques.Shields.Where(s=>s.Expires>TickIndex && TechniqueUnit(s.Owner) is {Alive:true}).ToImmutableArray()};
            foreach(var cast in _techniques.Punches.Values.OrderBy(p=>p.Owner,StringComparer.Ordinal).ToArray())
            {
                var owner=TechniqueUnit(cast.Owner)!;
                if(!owner.Alive || owner.Team!=cast.Team || TechniqueControlled(owner) || IsDisplacing(owner) || !owner.Position.IsEqualApprox(cast.Start))
                {
                    EmitPunch("line_cancel",cast);
                    _techniques=_techniques with {Punches=_techniques.Punches.Remove(cast.Owner)};
                    if (owner.Alive) RecoverUnitAction(owner, cast.Skill.RecoveryTicks);
                    continue;
                }
                if(TickIndex<cast.StartTick+cast.Skill.ChargeTicks)continue;
                _techniques=_techniques with {Punches=_techniques.Punches.Remove(cast.Owner)};
                RecoverUnitAction(owner, cast.Skill.RecoveryTicks);
                EmitPunch("line_release",cast);
                foreach(var hit in _units.Where(u=>u.Alive && u.Team!=owner.Team)
                    .Select(u=>(Unit:u,Time:LineHitTime(cast.Start,cast.End-cast.Start,cast.Skill.Radius,u)))
                    .Where(h=>h.Time is not null).OrderBy(h=>h.Time).ThenBy(h=>h.Unit.RuntimeId,StringComparer.Ordinal).ToArray())
                {
                    ResolveLineSkillHit(owner,hit.Unit,cast.Damage,cast.Origin,EffectDamageType.Normal);
                    Emit("line_impact",owner.RuntimeId,hit.Unit.RuntimeId,0,hit.Unit.Position,"");
                }
                owner.AttackCooldown=Math.Max(owner.AttackCooldown,4);
            }
            // Cancellation precedes displacement advancement so an interrupted tether
            // never drags its victim for another tick.
            foreach(var hook in _techniques.Hooks.Values.OrderBy(h=>h.Owner,StringComparer.Ordinal).ToArray())
                if(HookInvalid(hook))EndHook(hook,true);
            resolution.Commit();checkpoint.Commit();
        }
        catch{checkpoint.Rollback();throw;}
    }

    private bool HookInvalid(HookCast hook) => TechniqueUnit(hook.Owner) is not {Alive:true} owner ||
        owner.Team!=hook.Team || TechniqueControlled(owner) || IsDisplacing(owner) || !owner.Position.IsEqualApprox(hook.Start);

    private void AdvanceHookFlights(IReadOnlyDictionary<string,Vector2> previous)
    {
        if(_techniques.Hooks.IsEmpty)return;
        var checkpoint=new BattleWorldStateCheckpoint(this);
        try
        {
            using var resolution=_combatPipeline.BeginAuthoritativeResolution();
            foreach(var hook in _techniques.Hooks.Values.OrderBy(h=>h.Owner,StringComparer.Ordinal).ToArray())AdvanceHook(hook,previous);
            resolution.Commit();checkpoint.Commit();
        }
        catch{checkpoint.Rollback();throw;}
    }

    private static float? HookHitTime(HookCast hook,Vector2 delta,BattleUnitState target,IReadOnlyDictionary<string,Vector2> previous)
    {
        var start=previous.GetValueOrDefault(target.RuntimeId,target.Position);
        var radius=hook.Skill.Radius+target.BodyRadius;
        if(hook.Position.DistanceSquaredTo(start)<=radius*radius)return 0;
        return BattlefieldSpace.TryMovingCircleTimeOfImpact(hook.Position,delta,hook.Skill.Radius,
            start,target.Position-start,target.BodyRadius,out var time)?time:null;
    }

    private void AdvanceHook(HookCast hook,IReadOnlyDictionary<string,Vector2> previous)
    {
        var owner=TechniqueUnit(hook.Owner)!;
        if(HookInvalid(hook))
        { EndHook(hook,true);return; }
        if(hook.ReturningAt>=0)
        {
            if(hook.Caught.Length>0)
            {
                var caught=TechniqueUnit(hook.Caught)!;
                if(!caught.Alive || caught.Team==owner.Team || !IsDisplacing(caught)) {EndHook(hook,false);return;}
                hook=hook with {Position=caught.Position};
            }
            else
            {
                var time=Math.Clamp((TickIndex-hook.ReturningAt)/(float)hook.Skill.ReturnTicks,0,1);
                hook=hook with {Position=hook.End.Lerp(hook.Start,time)};
                if(time>=1){EndHook(hook,false);return;}
            }
            _techniques=_techniques with {Hooks=_techniques.Hooks.SetItem(hook.Owner,hook)};
            Emit("hook_return",hook.Owner,hook.Caught,hook.Skill.Radius,hook.Position,"",origin:owner.Position);return;
        }
        if(TickIndex<=hook.StartTick+hook.Skill.WindupTicks)return;
        var remaining=hook.End-hook.Position;
        var delta=remaining.LimitLength(hook.Skill.Speed*BattleTiming.TickSeconds);
        var hit=_units.Where(u=>u.Alive && u.Team!=owner.Team)
            .Select(u=>(Unit:u,Time:HookHitTime(hook,delta,u,previous)))
            .Where(h=>h.Time is not null).OrderBy(h=>h.Time).ThenBy(h=>h.Unit.RuntimeId,StringComparer.Ordinal).FirstOrDefault();
        hook=hook with {Position=hook.Position+delta*(hit.Unit is null?1:hit.Time!.Value)};
        Emit("hook_move",hook.Owner,"",hook.Skill.Radius,hook.Position,"",origin:owner.Position);
        if(hit.Unit is { } target)
        {
            ResolveLineSkillHit(owner,target,owner.Damage*hook.Skill.AttackRatio,hook.Origin,EffectDamageType.Normal);
            var pull=new CompiledDisplacementOperation(DisplacementKind.Pull,new CompiledExplicitTargetQuery(),hook.Skill.Range,
                hook.Skill.ReturnTicks,.12f,false,hook.Skill.ExcludeBoss,0,0,0,EffectDamageType.Normal,null,0);
            bool canPull=target.Alive && IsDisplacementTargetEligible(pull,owner,target) && BuildDisplacementPlans(pull,owner,[target]).Count>0;
            if(canPull)ExecuteDisplacementOperation(pull,[target.RuntimeId],owner,hook.Origin);
            hook=hook with {ReturningAt=TickIndex,End=hook.Position,Caught=canPull?target.RuntimeId:""};
            Emit("hook_caught",hook.Owner,target.RuntimeId,0,hook.Position,"",origin:owner.Position);
        }
        else if(hook.Position.DistanceSquaredTo(hook.End)<.00001f)hook=hook with {ReturningAt=TickIndex};
        _techniques=_techniques with {Hooks=_techniques.Hooks.SetItem(hook.Owner,hook)};
    }
    private void EndHook(HookCast hook,bool cancel)
    {
        if(cancel && hook.Caught.Length>0 && _displacements.TryGetValue(hook.Caught,out var motion) && motion.SourceId==hook.Owner)
            CancelDisplacement(hook.Caught);
        _techniques=_techniques with {Hooks=_techniques.Hooks.Remove(hook.Owner)};
        Emit("hook_end",hook.Owner,hook.Caught,0,hook.Position,"",origin:hook.Start);
    }
    private void EndDuel(DuelLease duel)
    {
        _techniques=_techniques with {Duels=_techniques.Duels.Remove(duel)};
        Emit("duel_end",duel.Source,duel.Target,0,TechniqueUnit(duel.Target)?.Position??Vector2.Zero,"");
    }
    private void RemoveDefeatedTechniques(BattleUnitState unit)
    {
        _unitActionQueues = _unitActionQueues.Remove(unit.RuntimeId);
        foreach (var ledger in _techniques.Grit.Values.Where(g=>g.Owner==unit.RuntimeId))
            WriteCounter(unit,ledger.Key,0);
        foreach(var duel in _techniques.Duels.Where(d=>d.Source==unit.RuntimeId || d.Target==unit.RuntimeId).ToArray())EndDuel(duel);
        if(_techniques.Hooks.TryGetValue(unit.RuntimeId,out var hook))EndHook(hook,true);
        if(_techniques.Punches.TryGetValue(unit.RuntimeId,out var punch))EmitPunch("line_cancel",punch);
        _techniques=_techniques with {Punches=_techniques.Punches.Remove(unit.RuntimeId),
            Grit=_techniques.Grit.Where(p=>p.Value.Owner!=unit.RuntimeId).ToImmutableDictionary(),
            Shields=_techniques.Shields.Where(s=>s.Owner!=unit.RuntimeId).ToImmutableArray()};
    }
}
