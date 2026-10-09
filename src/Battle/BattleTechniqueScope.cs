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

internal interface IBattleTechniqueWorld
{
    int Tick { get; }
    IReadOnlyList<BattleUnitState> Units { get; }
    BattleCombatEvent? TriggerEvent { get; }
    bool CanOccupy(Vector2I cell);
    bool IsDisplacing(BattleUnitState unit);
    bool EnemyBusy(BattleUnitState unit);
    bool HasLineAccess(BattleUnitState source, BattleUnitState target);
    CombatSourceRef AbilityOrigin(CompiledAbilityDefinition ability, string ownerId);
    float ReadCounter(BattleUnitState unit, string key);
    void WriteCounter(BattleUnitState unit, string key, float value);
    void Hit(BattleUnitState source, BattleUnitState target, float amount, CombatSourceRef origin, EffectDamageType type, bool isAreaDamage = false);
    float Heal(string source, BattleUnitState target, float amount, CombatSourceRef origin);
    void TimedShield(string source, string target, float amount, int duration, CombatSourceRef origin);
    void ReleaseUnit(string id);
    void CancelProjectileWindups(BattleUnitState unit);
    void CancelChargedLine(BattleUnitState unit);
    void CancelTrample(BattleUnitState unit);
    void InterruptChannels(string id);
    float? LineHitTime(Vector2 start, Vector2 delta, float radius, BattleUnitState target);
    bool CanDisplace(CompiledDisplacementOperation op, BattleUnitState owner, BattleUnitState target);
    void Displace(CompiledDisplacementOperation op, ImmutableArray<string> targets, BattleUnitState owner, CombatSourceRef origin);
    bool IsDisplacedBy(string id, string source);
    void CancelDisplacement(string id);
    void Resolve(Action work);
    void Present(string type, string source, string target, float value, Vector2 position, string cue,
        Vector2 origin = default, BattleVfxCue? vfx = null, BattleAttackTiming? attackTiming = null, BattleLineCue? line = null);
}

internal sealed class BattleTechniqueScope(IBattleTechniqueWorld world, BattleStatusScope statuses, BattleUnitActionQueueScope actions)
{
    private const int Width = BattlefieldLayout.Width, Height = BattlefieldLayout.Height;
    private static string CounterAddress(BattleUnitState owner, string key, bool shared) =>
        shared ? $"team:{owner.Team}:{key}" : $"unit:{owner.RuntimeId}:{key}";
    private static void SetActionTarget(BattleUnitState owner, BattleUnitState target)
    { owner.ActionTargetRuntimeId = target.RuntimeId; owner.ActionTargetName = target.Definition.DisplayName; }
    internal sealed class Checkpoint
    {
        private readonly BattleTechniqueScope _owner;
        private readonly TechniqueState _state;
        internal Checkpoint(BattleTechniqueScope owner) { _owner = owner; _state = owner._techniques; }
        internal void Restore(BattleTechniqueScope owner)
        {
            if (!ReferenceEquals(owner, _owner)) throw new InvalidOperationException("Checkpoint belongs to another technique scope.");
            owner._techniques = _state;
        }
    }
    public Checkpoint Capture() => new(this);
    public void Restore(Checkpoint checkpoint) => checkpoint.Restore(this);
    public void Clear() => _techniques = TechniqueState.Empty;
    public bool CriticalUsed(BattleUnitState owner, string key) =>
        _techniques.GritTriggers.GetValueOrDefault(CounterAddress(owner, key, false), new GritTriggerLatch()).CriticalUsed;
    public bool IsExecuting(string owner, string ability) =>
        (_techniques.Punches.TryGetValue(owner, out var punch) && punch.Origin.StableId == ability) ||
        (_techniques.Hooks.TryGetValue(owner, out var hook) && hook.Origin.StableId == ability);
    private sealed record DuelLease(string Source, string Target, int SourceTeam, int TargetTeam, int EndTick, float BreakDistance);
    private sealed record GritSample(int Expires, float Amount)
    {
        public bool ActiveAt(int tick) => Expires == 0 || Expires > tick;
    }
    private sealed record GritLedger(string Owner, string Key, ImmutableArray<GritSample> Samples);
    private sealed record TimedShield(string Owner, int Expires, float Remaining);
    private sealed record PunchCast(string Owner, int Team, Vector2 Start, Vector2 End, int StartTick,
        float Damage, CompiledGritPunchOperation Skill, CombatSourceRef Origin, float ConsumedGrit = 0);
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
    private BattleUnitState? TechniqueUnit(string id) => world.Units.FirstOrDefault(u => u.RuntimeId == id);
    public bool TechniqueBusy(BattleUnitState owner) => _techniques.Hooks.ContainsKey(owner.RuntimeId) || _techniques.Punches.ContainsKey(owner.RuntimeId);
    private bool TechniqueControlled(BattleUnitState owner) => owner.DisabledTicks > 0 ||
        statuses.HasTag(owner.RuntimeId, StatusDefinitionCompiler.ActionDisabledTag);
    private bool DuelValid(DuelLease duel) => world.Tick < duel.EndTick &&
        TechniqueUnit(duel.Source) is { Alive: true } a && TechniqueUnit(duel.Target) is { Alive: true } b &&
        a.Team == duel.SourceTeam && b.Team == duel.TargetTeam && a.Team != b.Team && a.Position.DistanceTo(b.Position) <= duel.BreakDistance;
    public BattleUnitState? DuelOpponent(BattleUnitState unit)
    {
        var duel = _techniques.Duels.FirstOrDefault(d => (d.Source == unit.RuntimeId || d.Target == unit.RuntimeId) && DuelValid(d));
        return duel is null ? null : TechniqueUnit(duel.Source == unit.RuntimeId ? duel.Target : duel.Source);
    }
    public bool CanAimHook(BattleUnitState owner, BattleUnitState target, CompiledHookOperation skill) =>
        target.Alive && target.Team != owner.Team && owner.Position.DistanceTo(target.Position) <= skill.Range &&
        BattlefieldSpace.EdgeDistance(owner,target) > .35f &&
        BattlefieldSpace.FirstTerrainHitFraction(owner.Position,target.Position-owner.Position,skill.Radius,
            Width,Height,cell => world.CanOccupy(cell)) >= 1;

    public bool PrepareTechnique(CompiledCombatTechniqueOperation op, BattleUnitState owner, string targetId,
        out ImmutableArray<string> targets)
    {
        targets = [owner.RuntimeId];
        var target = TechniqueUnit(targetId);
        switch (op)
        {
            case CompiledCounterattackOperation:
                return world.TriggerEvent is { Kind: BattleCombatEventKind.AttackLanded } e && e.TargetRuntimeId == owner.RuntimeId;
            case CompiledGritStorageOperation:
                return world.TriggerEvent is { Kind: BattleCombatEventKind.HealthLost, EffectiveValue: > 0 } lost && lost.TargetRuntimeId == owner.RuntimeId;
            case CompiledDuelOperation:
                if (target is not { Alive: true } || target.Team == owner.Team || DuelOpponent(owner) is not null ||
                    DuelOpponent(target) is not null || world.IsDisplacing(target) || TechniqueBusy(target) || world.EnemyBusy(target) ||
                    !BattlefieldSpace.IsWithinReach(owner,target,owner.AttackRange) || !world.HasLineAccess(owner,target)) return false;
                targets = [targetId]; return true;
            case CompiledHookOperation hook:
                if (target is null || !CanAimHook(owner,target,hook)) return false;
                targets = [targetId]; return true;
            case CompiledGritPunchOperation punch:
                if (target is not { Alive: true } || target.Team == owner.Team || world.IsDisplacing(owner) ||
                    !owner.AbilityLoadout!.Abilities.Any(a => a.Operations.Contains(punch) && actions.HasQueuedUnitAction(owner, a.StableId)) ||
                    owner.Position.DistanceTo(target.Position) > punch.Range || !world.HasLineAccess(owner,target)) return false;
                targets = [targetId];
                return true;
            default: return false;
        }
    }

    public float CurrentGrit(BattleUnitState owner, string key) =>
        _techniques.Grit.TryGetValue(CounterAddress(owner,key,false),out var ledger)
            ? ledger.Samples.Where(s => s.ActiveAt(world.Tick)).Sum(s => s.Amount) : 0;

    public void ExecuteTechnique(CompiledCombatTechniqueOperation op, BattleUnitState owner,
        ImmutableArray<string> targets, CompiledAbilityDefinition ability)
    {
        var origin = world.AbilityOrigin(ability,owner.RuntimeId);
        switch (op)
        {
            case CompiledCounterattackOperation counter:
                var key = "_counter_" + ability.StableId;
                var hits = world.ReadCounter(owner,key)+1;
                // A ready counter is retained while there is no reachable opponent; it is
                // retried by the next received attack, never fired through range or control.
                var target = DuelOpponent(owner) ?? TechniqueUnit(owner.ActionTargetRuntimeId) ?? TechniqueUnit(world.TriggerEvent!.SourceRuntimeId);
                if (hits < counter.HitsRequired || TechniqueControlled(owner) || TechniqueBusy(owner) || world.IsDisplacing(owner) ||
                    target is not { Alive: true } || target.Team == owner.Team ||
                    !BattlefieldSpace.IsWithinReach(owner,target,owner.AttackRange) || !world.HasLineAccess(owner,target))
                { world.WriteCounter(owner,key,Math.Min(hits,counter.HitsRequired)); break; }
                world.WriteCounter(owner,key,0);
                var before = target.Health;
                world.Hit(owner,target,owner.Damage*counter.AttackRatio,origin,EffectDamageType.Normal);
                if (owner.Alive)
                {
                    var healed = world.Heal(owner.RuntimeId,owner,Math.Max(0,before-target.Health)*counter.LifestealRatio,origin);
                    if (healed > 0) world.Present("heal",owner.RuntimeId,owner.RuntimeId,healed,owner.Position,"");
                }
                world.Present("counter_strike",owner.RuntimeId,target.RuntimeId,before-target.Health,target.Position,"attack",origin:owner.Position);
                break;
            case CompiledGritStorageOperation storage:
                var address = CounterAddress(owner,storage.CounterKey,false);
                var samples = _techniques.Grit.TryGetValue(address,out var ledger)
                    ? ledger.Samples.Where(s=>s.ActiveAt(world.Tick)).ToImmutableArray() : [];
                var room = Math.Max(0,owner.MaxHealth*storage.MaximumHealthRatio-samples.Sum(s=>s.Amount));
                var added = Math.Min(room,world.TriggerEvent!.EffectiveValue);
                if (added > 0) samples=samples.Add(new(storage.WindowTicks == 0 ? 0 : world.Tick+storage.WindowTicks,added));
                _techniques = _techniques with { Grit = _techniques.Grit.SetItem(address,new(owner.RuntimeId,storage.CounterKey,samples)) };
                world.WriteCounter(owner,storage.CounterKey,samples.Sum(s=>s.Amount));
                RefreshGritActionRequests(owner);
                break;
            case CompiledDuelOperation duel:
                var opponent = TechniqueUnit(targets[0])!;
                _techniques = _techniques with { Duels = _techniques.Duels.Add(new(owner.RuntimeId,opponent.RuntimeId,
                    owner.Team,opponent.Team,world.Tick+duel.DurationTicks,duel.BreakDistance)) };
                foreach (var participant in new[] { owner,opponent })
                {
                    world.CancelProjectileWindups(participant); participant.ProjectileSequence=null;
                    world.CancelChargedLine(participant); world.CancelTrample(participant);
                    world.InterruptChannels(participant.RuntimeId); world.ReleaseUnit(participant.RuntimeId);
                }
                SetActionTarget(owner,opponent); SetActionTarget(opponent,owner);
                world.Present("duel_begin",owner.RuntimeId,opponent.RuntimeId,duel.DurationTicks,opponent.Position,"",origin:owner.Position);
                break;
            case CompiledGritPunchOperation punch:
                var victim = TechniqueUnit(targets[0])!;
                ConsumeGritActionRequest(owner, ability, punch);
                var stored = CurrentGrit(owner,punch.CounterKey);
                _techniques = _techniques with { Grit = _techniques.Grit.Remove(CounterAddress(owner,punch.CounterKey,false)) };
                world.WriteCounter(owner,punch.CounterKey,0);
                world.TimedShield(owner.RuntimeId, owner.RuntimeId, stored, punch.ShieldTicks, origin);
                var direction = (victim.Position-owner.Position).Normalized();
                var travel = direction*punch.Range;
                var terrain = BattlefieldSpace.FirstTerrainHitFraction(owner.Position,travel,punch.Radius,Width,Height,c=>world.CanOccupy(c));
                var cast = new PunchCast(owner.RuntimeId,owner.Team,owner.Position,owner.Position+travel*terrain,world.Tick,
                    owner.Damage*punch.AttackRatio+stored*punch.GritDamageRatio,punch,origin,stored);
                _techniques = _techniques with { Punches = _techniques.Punches.SetItem(owner.RuntimeId,cast) };
                HoldTechnique(owner);
                EmitPunch("line_charge",cast);
                break;
            case CompiledHookOperation hook:
                var aim = TechniqueUnit(targets[0])!;
                var delta = (aim.Position-owner.Position).Normalized()*hook.Range;
                var fraction = BattlefieldSpace.FirstTerrainHitFraction(owner.Position,delta,hook.Radius,Width,Height,c=>world.CanOccupy(c));
                var flight = new HookCast(owner.RuntimeId,owner.Team,owner.Position,owner.Position+delta*fraction,
                    owner.Position,world.Tick,hook,origin);
                _techniques = _techniques with { Hooks = _techniques.Hooks.SetItem(owner.RuntimeId,flight) };
                HoldTechnique(owner);
                world.Present("hook_start",owner.RuntimeId,"",hook.Radius,owner.Position,"",origin:owner.Position);
                break;
        }
    }

    private void HoldTechnique(BattleUnitState unit)
    {
        unit.Mode=BattleUnitMode.Casting;
        world.ReleaseUnit(unit.RuntimeId);
        world.CancelProjectileWindups(unit); unit.ProjectileSequence=null;
    }
    private void EmitPunch(string type,PunchCast cast) => world.Present(type,cast.Owner,cast.Owner,cast.Damage,cast.End,"",origin:cast.Start,
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

    public void AdvanceTechniques()
    {
        if(_techniques==TechniqueState.Empty)return;
        world.Resolve(() =>
        {
            foreach(var duel in _techniques.Duels.Where(d=>!DuelValid(d)).ToArray()) EndDuel(duel);
            foreach(var pair in _techniques.Grit.OrderBy(p=>p.Key,StringComparer.Ordinal).ToArray())
            {
                var owner=TechniqueUnit(pair.Value.Owner);
                var samples=pair.Value.Samples.Where(s=>s.ActiveAt(world.Tick) && owner is {Alive:true}).ToImmutableArray();
                _techniques=_techniques with {Grit=samples.IsEmpty?_techniques.Grit.Remove(pair.Key):
                    _techniques.Grit.SetItem(pair.Key,pair.Value with {Samples=samples})};
                if(owner is not null)world.WriteCounter(owner,pair.Value.Key,samples.Sum(s=>s.Amount));
            }
            foreach(var shield in _techniques.Shields.Where(s=>s.Expires<=world.Tick).ToArray())
            {
                if(TechniqueUnit(shield.Owner) is {Alive:true} unit)
                {
                    unit.Shield=Math.Max(0,unit.Shield-shield.Remaining);
                    world.Present("vfx",unit.RuntimeId,unit.RuntimeId,0,unit.Position,"",vfx:new(
                        unit.Shield>0?BattleVfxPhase.ShieldActive:BattleVfxPhase.ShieldDepleted,"shield"));
                }
            }
            _techniques=_techniques with {Shields=_techniques.Shields.Where(s=>s.Expires>world.Tick && TechniqueUnit(s.Owner) is {Alive:true}).ToImmutableArray()};
            foreach(var cast in _techniques.Punches.Values.OrderBy(p=>p.Owner,StringComparer.Ordinal).ToArray())
            {
                var owner=TechniqueUnit(cast.Owner)!;
                if(!owner.Alive || owner.Team!=cast.Team || TechniqueControlled(owner) || world.IsDisplacing(owner) || !owner.Position.IsEqualApprox(cast.Start))
                {
                    EmitPunch("line_cancel",cast);
                    _techniques=_techniques with {Punches=_techniques.Punches.Remove(cast.Owner)};
                    if (owner.Alive) actions.RecoverUnitAction(owner, cast.Skill.RecoveryTicks);
                    continue;
                }
                if(world.Tick<cast.StartTick+cast.Skill.ChargeTicks)continue;
                _techniques=_techniques with {Punches=_techniques.Punches.Remove(cast.Owner)};
                actions.RecoverUnitAction(owner, cast.Skill.RecoveryTicks);
                EmitPunch("line_release",cast);
                foreach(var hit in world.Units.Where(u=>u.Alive && u.Team!=owner.Team)
                    .Select(u=>(Unit:u,Time:world.LineHitTime(cast.Start,cast.End-cast.Start,cast.Skill.Radius,u)))
                    .Where(h=>h.Time is not null).OrderBy(h=>h.Time).ThenBy(h=>h.Unit.RuntimeId,StringComparer.Ordinal).ToArray())
                {
                    world.Hit(owner,hit.Unit,cast.Damage,cast.Origin,EffectDamageType.Normal,isAreaDamage:true);
                    world.Present("line_impact",owner.RuntimeId,hit.Unit.RuntimeId,0,hit.Unit.Position,"");
                }
                if(cast.Skill.SuccessHealGritRatio>0 && owner.Alive)
                {
                    var healed=world.Heal(owner.RuntimeId,owner,cast.ConsumedGrit*cast.Skill.SuccessHealGritRatio,cast.Origin);
                    if(healed>0)world.Present("heal",owner.RuntimeId,owner.RuntimeId,healed,owner.Position,"heal");
                }
                owner.AttackCooldown=Math.Max(owner.AttackCooldown,4);
            }
            // Cancellation precedes displacement advancement so an interrupted tether
            // never drags its victim for another tick.
            foreach(var hook in _techniques.Hooks.Values.OrderBy(h=>h.Owner,StringComparer.Ordinal).ToArray())
                if(HookInvalid(hook))EndHook(hook,true);
        });
    }

    private bool HookInvalid(HookCast hook) => TechniqueUnit(hook.Owner) is not {Alive:true} owner ||
        owner.Team!=hook.Team || TechniqueControlled(owner) || world.IsDisplacing(owner) || !owner.Position.IsEqualApprox(hook.Start);

    public void AdvanceHookFlights(IReadOnlyDictionary<string,Vector2> previous)
    {
        if(_techniques.Hooks.IsEmpty)return;
        world.Resolve(() =>
        {
            foreach(var hook in _techniques.Hooks.Values.OrderBy(h=>h.Owner,StringComparer.Ordinal).ToArray())AdvanceHook(hook,previous);
        });
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
                if(!caught.Alive || caught.Team==owner.Team || !world.IsDisplacing(caught)) {EndHook(hook,false);return;}
                hook=hook with {Position=caught.Position};
            }
            else
            {
                var time=Math.Clamp((world.Tick-hook.ReturningAt)/(float)hook.Skill.ReturnTicks,0,1);
                hook=hook with {Position=hook.End.Lerp(hook.Start,time)};
                if(time>=1){EndHook(hook,false);return;}
            }
            _techniques=_techniques with {Hooks=_techniques.Hooks.SetItem(hook.Owner,hook)};
            world.Present("hook_return",hook.Owner,hook.Caught,hook.Skill.Radius,hook.Position,"",origin:owner.Position);return;
        }
        if(world.Tick<=hook.StartTick+hook.Skill.WindupTicks)return;
        var remaining=hook.End-hook.Position;
        var delta=remaining.LimitLength(hook.Skill.Speed*BattleTiming.TickSeconds);
        var hit=world.Units.Where(u=>u.Alive && u.Team!=owner.Team)
            .Select(u=>(Unit:u,Time:HookHitTime(hook,delta,u,previous)))
            .Where(h=>h.Time is not null).OrderBy(h=>h.Time).ThenBy(h=>h.Unit.RuntimeId,StringComparer.Ordinal).FirstOrDefault();
        hook=hook with {Position=hook.Position+delta*(hit.Unit is null?1:hit.Time!.Value)};
        world.Present("hook_move",hook.Owner,"",hook.Skill.Radius,hook.Position,"",origin:owner.Position);
        if(hit.Unit is { } target)
        {
            world.Hit(owner,target,owner.Damage*hook.Skill.AttackRatio,hook.Origin,EffectDamageType.Normal);
            var pull=new CompiledDisplacementOperation(DisplacementKind.Pull,new CompiledExplicitTargetQuery(),hook.Skill.Range,
                hook.Skill.ReturnTicks,.12f,false,hook.Skill.ExcludeBoss,0,0,0,EffectDamageType.Normal,null,0);
            bool canPull=target.Alive && world.CanDisplace(pull, owner, target);
            if(canPull)world.Displace(pull,[target.RuntimeId],owner,hook.Origin);
            hook=hook with {ReturningAt=world.Tick,End=hook.Position,Caught=canPull?target.RuntimeId:""};
            world.Present("hook_caught",hook.Owner,target.RuntimeId,0,hook.Position,"",origin:owner.Position);
        }
        else if(hook.Position.DistanceSquaredTo(hook.End)<.00001f)hook=hook with {ReturningAt=world.Tick};
        _techniques=_techniques with {Hooks=_techniques.Hooks.SetItem(hook.Owner,hook)};
    }
    private void EndHook(HookCast hook,bool cancel)
    {
        if(cancel && hook.Caught.Length>0 && world.IsDisplacedBy(hook.Caught, hook.Owner))
            world.CancelDisplacement(hook.Caught);
        _techniques=_techniques with {Hooks=_techniques.Hooks.Remove(hook.Owner)};
        world.Present("hook_end",hook.Owner,hook.Caught,0,hook.Position,"",origin:hook.Start);
    }
    private void EndDuel(DuelLease duel)
    {
        _techniques=_techniques with {Duels=_techniques.Duels.Remove(duel)};
        world.Present("duel_end",duel.Source,duel.Target,0,TechniqueUnit(duel.Target)?.Position??Vector2.Zero,"");
    }
    public void RemoveDefeatedTechniques(BattleUnitState unit)
    {
        actions.Remove(unit.RuntimeId);
        foreach (var ledger in _techniques.Grit.Values.Where(g=>g.Owner==unit.RuntimeId))
            world.WriteCounter(unit,ledger.Key,0);
        foreach(var duel in _techniques.Duels.Where(d=>d.Source==unit.RuntimeId || d.Target==unit.RuntimeId).ToArray())EndDuel(duel);
        if(_techniques.Hooks.TryGetValue(unit.RuntimeId,out var hook))EndHook(hook,true);
        if(_techniques.Punches.TryGetValue(unit.RuntimeId,out var punch))EmitPunch("line_cancel",punch);
        _techniques=_techniques with {Punches=_techniques.Punches.Remove(unit.RuntimeId),
            Grit=_techniques.Grit.Where(p=>p.Value.Owner!=unit.RuntimeId).ToImmutableDictionary(),
            Shields=_techniques.Shields.Where(s=>s.Owner!=unit.RuntimeId).ToImmutableArray()};
    }
    private sealed record GritTriggerLatch(bool BelowHalf = false, bool CriticalUsed = false);

    private static CompiledGritStorageOperation GritStorageFor(BattleUnitState owner, string key) =>
        owner.AbilityLoadout!.Abilities.SelectMany(a => a.Operations)
            .OfType<CompiledGritStorageOperation>().Single(s => s.CounterKey == key);

    public void RefreshGritActionRequests(BattleUnitState owner)
    {
        if (!owner.Alive || owner.AbilityLoadout is not { } loadout) return;
        foreach (var ability in loadout.Abilities.Where(a => a.Trigger == AbilityTriggerKind.ActionQueued))
        foreach (var punch in ability.Operations.OfType<CompiledGritPunchOperation>())
        {
            var key = CounterAddress(owner, punch.CounterKey, false);
            var latch = _techniques.GritTriggers.GetValueOrDefault(key, new GritTriggerLatch());
            var belowHalf = owner.Health < owner.MaxHealth * punch.LowHealthRatio;
            var reasons = UnitActionReason.None;
            if (belowHalf && !latch.BelowHalf) reasons |= UnitActionReason.LowHealth;
            if (!latch.CriticalUsed && owner.Health < owner.MaxHealth * punch.CriticalHealthRatio)
                reasons |= UnitActionReason.CriticalHealth;
            if (CurrentGrit(owner, punch.CounterKey) >= owner.MaxHealth * GritStorageFor(owner, punch.CounterKey).MaximumHealthRatio)
                reasons |= UnitActionReason.FullResource;
            else
                actions.RemoveQueuedUnitActionReason(owner, ability.StableId, UnitActionReason.FullResource);
            if (latch.BelowHalf != belowHalf || !_techniques.GritTriggers.ContainsKey(key))
                _techniques = _techniques with
                {
                    GritTriggers = _techniques.GritTriggers.SetItem(key, latch with { BelowHalf = belowHalf })
                };
            actions.QueueUnitAction(owner, ability.StableId, reasons);
        }
    }

    private void ConsumeGritActionRequest(BattleUnitState owner, CompiledAbilityDefinition ability, CompiledGritPunchOperation punch)
    {
        var reasons = actions.TakeQueuedUnitAction(owner, ability.StableId);
        if (!reasons.HasFlag(UnitActionReason.CriticalHealth)) return;
        var key = CounterAddress(owner, punch.CounterKey, false);
        var latch = _techniques.GritTriggers.GetValueOrDefault(key, new GritTriggerLatch());
        // Death clears casts and pending requests, but not this spent entitlement.
        // Revival retains identity; a new battle creates a fresh TechniqueState.
        _techniques = _techniques with { GritTriggers = _techniques.GritTriggers.SetItem(key, latch with { CriticalUsed = true }) };
    }
}
