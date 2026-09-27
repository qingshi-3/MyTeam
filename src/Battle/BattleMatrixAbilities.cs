using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private sealed record MatrixRule(string Owner, string Key, CompiledMatrixOperation Operation, CombatSourceRef Origin);
    private sealed record MatrixJob(string Owner, string Target, int ReadyTick, CompiledMatrixOperation Operation,
        CombatSourceRef Origin, BattleCombatEvent? Event, Vector2? Center);
    private sealed record MatrixBuff(string Owner, string Source, string Key, int Expires, AttributeModifierHandle Handle,
        MatrixCondition OwnerCondition,MatrixCondition TargetCondition,float HealthThreshold);
    private sealed record MatrixBoost(string Owner, string Key, bool Skill, float Amount, int Expires);
    private sealed record MatrixProduct(string Unit, string Owner, int Expires, int Extensions);
    private sealed record MatrixArrival(string Owner,CompiledMatrixOperation Operation,CombatSourceRef Origin);
    private sealed record MatrixAbilityState(ImmutableArray<MatrixRule> Rules, ImmutableArray<MatrixJob> Jobs,
        ImmutableArray<MatrixBuff> Buffs, ImmutableArray<MatrixBoost> Boosts, ImmutableArray<MatrixProduct> Products,
        ImmutableArray<BattleCombatEvent> Pending, ImmutableDictionary<string,float> Counters,
        ImmutableDictionary<string,int> Ready, ImmutableHashSet<string> Used, ImmutableDictionary<string,string> Targets,
        ImmutableDictionary<string,float> ActionFactors, ImmutableDictionary<string,ImmutableHashSet<MatrixCondition>> DeathConditions,
        ImmutableDictionary<string,MatrixArrival> Arrivals)
    {
        public static MatrixAbilityState Empty => new([],[],[],[],[],[],ImmutableDictionary<string,float>.Empty,
            ImmutableDictionary<string,int>.Empty,ImmutableHashSet<string>.Empty,ImmutableDictionary<string,string>.Empty,
            ImmutableDictionary<string,float>.Empty,ImmutableDictionary<string,ImmutableHashSet<MatrixCondition>>.Empty,
            ImmutableDictionary<string,MatrixArrival>.Empty);
    }
    private MatrixAbilityState _matrixAbilities = MatrixAbilityState.Empty;
    private bool _drainingMatrixAbilities;
    private BattleUnitState? MatrixUnit(string id) => _units.FirstOrDefault(u=>u.RuntimeId==id);
    private string MatrixKey(BattleUnitState owner,string key) => $"{owner.RuntimeId}:{key}";
    private float MatrixCounter(BattleUnitState owner,string key) => _matrixAbilities.Counters.GetValueOrDefault(MatrixKey(owner,key));
    private void MatrixWrite(BattleUnitState owner,string key,float amount) =>
        _matrixAbilities = _matrixAbilities with { Counters=_matrixAbilities.Counters.SetItem(MatrixKey(owner,key),Math.Max(0,amount)) };

    // Eligibility snapshot only; no status or periodic work survives death through this record.
    private void MatrixBeforeDeath(BattleUnitState unit) => _matrixAbilities=_matrixAbilities with
    {
        DeathConditions=_matrixAbilities.DeathConditions.SetItem(unit.RuntimeId,Enum.GetValues<MatrixCondition>()
            .Where(condition=>MatrixConditionMatches(condition,unit,.6f)).ToImmutableHashSet())
    };

    // Subscribers only append immutable facts. No combat mutation is performed under event dispatch.
    private void ObserveMatrixAbilityEvent(BattleCombatEvent fact)
    {
        if (_matrixAbilities.Rules.IsEmpty) return;
        if (_matrixAbilities.Pending.Length >= _combatPipeline.Limits.MaxReactions)
            throw new InvalidOperationException("Matrix reaction queue exceeded its bounded budget.");
        _matrixAbilities = _matrixAbilities with { Pending=_matrixAbilities.Pending.Add(fact) };
        if(fact.Kind==BattleCombatEventKind.ControlApplied)
            _matrixAbilities=_matrixAbilities with{Counters=_matrixAbilities.Counters.SetItem($"control-window:{fact.TargetRuntimeId}",fact.Sequence)};
    }

    private bool PrepareMatrixOperation(CompiledMatrixOperation operation,BattleUnitState owner,string target,
        out ImmutableArray<string> targets)
    {
        targets = (operation.Kind is MatrixOperationKind.RegisterReaction or MatrixOperationKind.DeathGift)
            ? ImmutableArray.Create(owner.RuntimeId) : MatrixTargets(operation,owner,target,null).Select(u=>u.RuntimeId).ToImmutableArray();
        if(operation.Kind==MatrixOperationKind.Summon && (!_config.TacticalSummons.ContainsKey(operation.ContentId) ||
            _units.Count(u=>u.Alive && u.SummonerRuntimeId==owner.RuntimeId && u.Definition.ContentId==operation.ContentId)>=operation.Maximum)) return false;
        if(operation.Kind==MatrixOperationKind.SkillVolley && owner.ProjectileSequence is not null) return false;
        if(operation.Kind==MatrixOperationKind.DashStrike)
            return PrepareMatrixDisplacement(operation,owner,target,out targets);
        if(operation.Kind==MatrixOperationKind.TransferPoison&&targets.Length>0)targets=targets.Insert(0,target);
        // Optional transfer/echo/resource effects do not invalidate a useful cast's other effects.
        return targets.Length>0 || operation.TargetCondition!=MatrixCondition.None || operation.Kind is MatrixOperationKind.EchoDeathGift or MatrixOperationKind.TransferPoison;
    }

    private bool PrepareMatrixDisplacement(CompiledMatrixOperation operation,BattleUnitState owner,string explicitTarget,
        out ImmutableArray<string> targets)
    {
        // Check reachability before truncating the authored priority order. A blocked high-poison
        // target must not hide the next reachable one, and the shared motion query must not re-sort.
        var motion=MatrixDisplacement(operation,owner);
        foreach(var candidate in MatrixTargets(operation with{MaxTargets=Math.Max(1,_units.Count)},owner,explicitTarget,null))
        {
            if(MatrixCanStrikeInPlace(motion,owner,candidate))
            {targets=ImmutableArray.Create(candidate.RuntimeId);return true;}
            if(!PrepareDisplacementOperation(motion,owner,candidate.RuntimeId,out var legal))continue;
            var plan=BuildDisplacementPlans(motion,owner,[candidate]).FirstOrDefault();
            // A clipped charge ending before contact is movement, not a reachable strike.
            if(plan is null||plan.End.DistanceTo(candidate.Position)>
                owner.BodyRadius+candidate.BodyRadius+motion.StopDistance+.2f)continue;
            targets=legal;return true;
        }
        targets=[];
        return false;
    }

    private bool MatrixCanStrikeInPlace(CompiledDisplacementOperation motion,BattleUnitState owner,BattleUnitState target) =>
        motion.Kind==DisplacementKind.Charge&&IsDisplacementTargetEligible(motion,owner,target)&&
        owner.Position.DistanceTo(target.Position)<=motion.Distance&&
        owner.Position.DistanceTo(target.Position)<=owner.BodyRadius+target.BodyRadius+motion.StopDistance+.2f&&
        HasLineAccess(owner,target);

    private void ExecuteMatrixOperation(ResolvedAbilityOperation resolved,AbilityExecutionPlan plan,BattleUnitState owner)
    {
        var op=(CompiledMatrixOperation)resolved.Operation;
        var origin=AbilityOrigin(plan.Ability,owner.RuntimeId);
        if(op.Kind==MatrixOperationKind.TransferPoison)
        {
            if(resolved.TargetIds.Length>1)MatrixExecute(op,owner,resolved.TargetIds[0],origin,null,null,
                $"{plan.Ability.StableId}:{resolved.OperationIndex}",resolved.TargetIds.RemoveAt(0));
            return;
        }
        MatrixExecute(op,owner,resolved.TargetIds.FirstOrDefault()??"",origin,null,null,
            $"{plan.Ability.StableId}:{resolved.OperationIndex}",resolved.TargetIds);
    }

    private IEnumerable<BattleUnitState> MatrixTargets(CompiledMatrixOperation op,BattleUnitState owner,string explicitTarget,
        BattleCombatEvent? fact,Vector2? center=null)
    {
        var eventTarget=MatrixUnit(fact?.TargetRuntimeId??explicitTarget);
        var eventSource=MatrixUnit(fact?.SourceRuntimeId??"");
        if(center is { } fixedCenter&&op.Radius>0)
            return _units.Where(u=>u.Alive&&u.Team!=owner.Team&&u.Position.DistanceTo(fixedCenter)<=op.Radius+
                (op.Key.Length>0?MatrixCounter(owner,op.Key)*op.RadiusPerCounter:0)&&MatrixConditionMatches(op.TargetCondition,u,op.HealthThreshold))
                .OrderBy(u=>u.Position.DistanceSquaredTo(fixedCenter)).ThenBy(u=>u.RuntimeId,StringComparer.Ordinal).Take(op.MaxTargets).ToArray();
        var origin=center??(op.Target is MatrixTargetKind.EventNearbyAllies or MatrixTargetKind.EventNearbyEnemies
            ? eventTarget?.Position??owner.Position : owner.Position);
        if(op.Target==MatrixTargetKind.NearWoundedAllyEnemies)origin=_units.Where(u=>u.Alive&&u.Team==owner.Team)
            .OrderBy(u=>u.Health/u.MaxHealth).ThenBy(u=>u.RuntimeId,StringComparer.Ordinal).FirstOrDefault()?.Position??owner.Position;
        if(op.Target==MatrixTargetKind.NearEventSourceAllies)origin=eventSource?.Position??owner.Position;
        IEnumerable<BattleUnitState> candidates=op.Target switch
        {
            MatrixTargetKind.Self => [owner],
            MatrixTargetKind.EventSource => eventSource is null?[]:[eventSource],
            MatrixTargetKind.EventTarget => eventTarget is null?[]:[eventTarget],
            MatrixTargetKind.EventVictim => MatrixUnit(eventTarget?.ActionTargetRuntimeId??"") is { } victim?[victim]:[],
            MatrixTargetKind.CurrentEnemy => _units.Where(u=>u.RuntimeId==explicitTarget && u.Team!=owner.Team),
            MatrixTargetKind.OwnedSummons => _units.Where(u=>u.Team==owner.Team && u.SummonerRuntimeId==owner.RuntimeId),
            MatrixTargetKind.TemporaryAllies => _units.Where(u=>u.Team==owner.Team && u.IsTemporary),
            MatrixTargetKind.NearestCaster => _units.Where(u=>u.Team==owner.Team && u.HasManaSkill),
            MatrixTargetKind.NearestBoostableCaster => _units.Where(u=>u.Team==owner.Team && u.HasManaSkill &&
                u.Definition.AbilityLoadout?.Abilities.Any(a=>a.IsActiveSkill&&a.Operations.Any(MatrixHasBoostableValue))==true),
            MatrixTargetKind.NearestWoundedAlly => _units.Where(u=>u.Team==owner.Team && u.Health<u.MaxHealth),
            MatrixTargetKind.NearestSummoner => _units.Where(u=>u.Team==owner.Team&&u.Definition.Tags.Contains("summoner")),
            MatrixTargetKind.LowestHealthAllies or MatrixTargetKind.NearestAllies or MatrixTargetKind.LowestManaAllies or MatrixTargetKind.FrontAlly or MatrixTargetKind.EventNearbyAllies or MatrixTargetKind.NearEventSourceAllies => _units.Where(u=>u.Team==owner.Team),
            _ => _units.Where(u=>u.Team!=owner.Team)
        };
        candidates=candidates.Where(u=>u.Alive && (!op.ExcludeSelf||u!=owner) &&
            (op.Target!=MatrixTargetKind.OtherEnemy||u.RuntimeId!=explicitTarget) && u.Position.DistanceTo(origin)<=op.Range &&
            MatrixConditionMatches(op.TargetCondition,u,op.HealthThreshold));
        if(op.Target==MatrixTargetKind.LowestManaAllies) candidates=candidates.Where(u=>u.MaxMana>0 && u.CurrentMana<u.MaxMana);
        if(op.Target==MatrixTargetKind.FrontEnemies)
        {
            var aim=MatrixUnit(explicitTarget)?.Position-owner.Position??new Vector2(owner.Team==0?1:-1,0);
            var direction=aim.Normalized();candidates=candidates.Where(u=>(u.Position-owner.Position).Normalized().Dot(direction)>=.3f);
            var extra=op.Key.Length>0?MatrixCounter(owner,op.Key)*op.RadiusPerCounter:0;
            candidates=_units.Where(u=>u.Alive&&u.Team!=owner.Team&&u.Position.DistanceTo(owner.Position)<=op.Range+extra&&
                (u.Position-owner.Position).Normalized().Dot(direction)>=.3f);
        }
        candidates=op.Target switch
        {
            MatrixTargetKind.LowestHealthAllies or MatrixTargetKind.LowestHealthEnemies => candidates.OrderBy(u=>u.Health/u.MaxHealth).ThenBy(u=>u.RuntimeId,StringComparer.Ordinal),
            MatrixTargetKind.LowestManaAllies => candidates.OrderBy(u=>u.CurrentMana/u.MaxMana).ThenBy(u=>u.RuntimeId,StringComparer.Ordinal),
            MatrixTargetKind.MostPoisonEnemies => candidates.OrderByDescending(u=>MatrixReadPoison(u.RuntimeId)).ThenBy(u=>u.RuntimeId,StringComparer.Ordinal),
            MatrixTargetKind.MostEmberEnemies => candidates.OrderByDescending(u=>MatrixReadEmber(u.RuntimeId)).ThenBy(u=>u.RuntimeId,StringComparer.Ordinal),
            MatrixTargetKind.DenseEnemies => candidates.OrderByDescending(u=>_units.Count(v=>v.Alive&&v.Team!=owner.Team&&v.Position.DistanceTo(u.Position)<=Math.Max(.5f,op.Radius))).ThenBy(u=>u.RuntimeId,StringComparer.Ordinal),
            MatrixTargetKind.FrontAlly => candidates.OrderByDescending(u=>u.Team==0?u.Position.X:-u.Position.X).ThenBy(u=>u.RuntimeId,StringComparer.Ordinal),
            _=>candidates.OrderBy(u=>u.Position.DistanceSquaredTo(origin)).ThenBy(u=>u.RuntimeId,StringComparer.Ordinal)
        };
        var selected=candidates.Take(op.MaxTargets).ToArray();
        if(op.Radius>0 && selected.Length>0 && op.Target is MatrixTargetKind.DenseEnemies or MatrixTargetKind.CurrentEnemy)
        {
            var at=center??selected[0].Position;
            var radius=op.Radius+(op.Key.Length>0?MatrixCounter(owner,op.Key)*op.RadiusPerCounter:0);
            return _units.Where(u=>u.Alive&&u.Team!=owner.Team&&u.Position.DistanceTo(at)<=radius)
                .OrderBy(u=>u.Position.DistanceSquaredTo(at)).ThenBy(u=>u.RuntimeId,StringComparer.Ordinal).Take(op.MaxTargets).ToArray();
        }
        return selected;
    }

    private bool MatrixConditionMatches(MatrixCondition condition,BattleUnitState? unit,float threshold) => condition switch
    {
        MatrixCondition.None=>true,
        MatrixCondition.Poisoned=>unit is not null&&MatrixReadPoison(unit.RuntimeId)>0,
        MatrixCondition.EmberMarked=>unit is not null&&MatrixReadEmber(unit.RuntimeId)>0,
        MatrixCondition.Chilled=>unit is not null&&MatrixReadChill(unit.RuntimeId)>0,
        MatrixCondition.Controlled=>unit is not null&&(unit.DisabledTicks>0||_statusScope.HasTag(unit.RuntimeId,Statuses.StatusDefinitionCompiler.ActionDisabledTag)),
        MatrixCondition.LowHealth=>unit is not null&&unit.Health/unit.MaxHealth<threshold,
        MatrixCondition.Shielded=>unit is not null&&unit.Shield>0,
        MatrixCondition.Unshielded=>unit is not null&&unit.Shield<=0,
        MatrixCondition.Frozen=>unit is not null&&unit.Statuses.Any(s=>s.StableId=="matrix_freeze"),
        _=>false
    };

    // A next-skill numeric lease must have an actual effect to modify; pure summons and status-only
    // casts are not legal recipients. Inspect compiled atoms rather than maintaining hero ids.
    private static bool MatrixHasBoostableValue(CompiledAbilityOperation operation) => operation switch
    {
        CompiledMatrixOperation matrix => MatrixAbilityCompiler.Flatten(matrix).Any(o=>
            o.Kind is MatrixOperationKind.Damage or MatrixOperationKind.Heal or MatrixOperationKind.Shield or
                MatrixOperationKind.Mana or MatrixOperationKind.DashStrike or MatrixOperationKind.SkillVolley or MatrixOperationKind.LineDamage),
        CompiledEffectAbilityOperation effect => !effect.Binding.Effects.IsEmpty,
        CompiledProjectileSequenceAbilityOperation => true,
        CompiledDisplacementOperation motion => motion.ImpactDamage>0||motion.AttackRatio>0,
        CompiledBattleValueOperation value => value.Action is BattleValueAction.Damage or BattleValueAction.Heal or BattleValueAction.Shield,
        CompiledConsumeStatusOperation consume => consume.DamageMultiplier>0,
        CompiledChargedLineOperation => true,
        _=>false
    };

    private float MatrixAmount(CompiledMatrixOperation op,BattleUnitState owner,BattleUnitState target,BattleCombatEvent? fact)
    {
        var amount=op.Amount+owner.Damage*op.AttackRatio+owner.MaxHealth*op.OwnerHealthRatio+
            target.MaxHealth*op.TargetHealthRatio+(fact?.EffectiveValue??0)*op.EventRatio+
            (MatrixUnit(fact?.TargetRuntimeId??"")?.MaxHealth??0)*op.EventTargetHealthRatio+
            MatrixCounter(op.CounterOnTarget?target:owner,op.Key)*op.CounterRatio;
        if(fact is not null && MatrixUnit(fact.TargetRuntimeId)?.IsTemporary==true) amount*=op.TemporaryScale;
        return Math.Clamp(amount,op.Kind is MatrixOperationKind.Counter or MatrixOperationKind.TimedAttribute?-op.Maximum:0,op.Maximum);
    }

    private void MatrixExecute(CompiledMatrixOperation op,BattleUnitState owner,string explicitTarget,CombatSourceRef origin,
        BattleCombatEvent? fact,Vector2? center,string slot,ImmutableArray<string> prepared=default)
    {
        if(op.RequiredCounter.Length>0&&MatrixCounter(owner,op.RequiredCounter)<op.CounterThreshold) return;
        if(!MatrixConditionMatches(op.OwnerCondition,owner,op.HealthThreshold)) return;
        var targets=prepared.IsDefault?MatrixTargets(op,owner,explicitTarget,fact,center).ToArray():
            prepared.Select(MatrixUnit).Where(u=>u is not null).Cast<BattleUnitState>().ToArray();
        if(op.Kind==MatrixOperationKind.RegisterReaction)
        {
            var key=$"{owner.RuntimeId}:{slot}";
            if(!_matrixAbilities.Rules.Any(r=>r.Key==key))
                _matrixAbilities=_matrixAbilities with {Rules=_matrixAbilities.Rules.Add(new(owner.RuntimeId,key,op,origin))};
            return;
        }
        if(op.Kind==MatrixOperationKind.DeathGift)
        {
            var rule=op with {Kind=MatrixOperationKind.RegisterReaction,Event=MatrixEventKind.Death,TargetRelation=MatrixRelation.Owner,
                LivingOwnerRequired=false,OncePerBattle=true,Effects=op.Effects};
            var key=$"{owner.RuntimeId}:death-gift:{slot}";
            if(!_matrixAbilities.Rules.Any(r=>r.Key==key)) _matrixAbilities=_matrixAbilities with {Rules=_matrixAbilities.Rules.Add(new(owner.RuntimeId,key,rule,origin))};
            return;
        }
        if(op.Kind==MatrixOperationKind.Delay)
        {
            var anchor=targets.FirstOrDefault()?.Position??center??owner.Position;
            for(var pulse=0;pulse<op.Count;pulse++)
            foreach(var child in op.Effects)
                _matrixAbilities=_matrixAbilities with {Jobs=_matrixAbilities.Jobs.Add(new(owner.RuntimeId,explicitTarget,
                    TickIndex+op.DurationTicks+pulse*op.IntervalTicks,child,origin,fact,anchor))};
            return;
        }
        if(op.Kind==MatrixOperationKind.Sequence)
        {
            // One prepared anchor owns every component, including a final single-target operation.
            var anchor=targets.FirstOrDefault();
            if(anchor is null)return;
            foreach(var child in op.Effects)MatrixExecute(child,owner,anchor.RuntimeId,origin,fact,anchor.Position,slot);
            return;
        }
        if(op.Kind==MatrixOperationKind.Summon)
        {
            if(!_config.TacticalSummons.TryGetValue(op.ContentId,out var product)) throw new InvalidOperationException($"Missing summon {op.ContentId}");
            var spawned=false;
            for(var i=0;i<op.Count&&_units.Count(u=>u.Alive&&u.SummonerRuntimeId==owner.RuntimeId&&u.Definition.ContentId==op.ContentId)<op.Maximum;i++)
            {
                var before=_units.Count;
                if(!SpawnTemporaryNear(product,owner.Team,owner.Position,1,1,owner.RuntimeId)) break;
                spawned=true;
                foreach(var unit in _units.Skip(before)) _matrixAbilities=_matrixAbilities with {Products=_matrixAbilities.Products.Add(new(unit.RuntimeId,owner.RuntimeId,TickIndex+op.DurationTicks,0))};
            }
            if(spawned&&TraitMechanicsExtraSummon(owner.RuntimeId)&&
                _units.Count(u=>u.Alive&&u.SummonerRuntimeId==owner.RuntimeId&&u.Definition.ContentId==op.ContentId)<op.Maximum&&
                FindOpenNear(owner.Position,owner.Team,product.BodyRadius) is not null)
            {
                var before=_units.Count;
                if(SpawnTemporaryNear(product,owner.Team,owner.Position,1,1,owner.RuntimeId))
                    foreach(var unit in _units.Skip(before))_matrixAbilities=_matrixAbilities with{Products=_matrixAbilities.Products.Add(new(unit.RuntimeId,owner.RuntimeId,TickIndex+op.DurationTicks,0))};
            }
            return;
        }
        if(op.Kind==MatrixOperationKind.EchoDeathGift)
        {
            // Only explicit DeathGift registrations qualify. No death event or ordinary ability echo is emitted.
            var donor=targets.FirstOrDefault(t=>_matrixAbilities.Rules.Any(r=>r.Owner==t.RuntimeId&&r.Key.Contains(":death-gift:",StringComparison.Ordinal))&&
                !_matrixAbilities.Used.Contains($"death-gift-copy:{t.RuntimeId}"));
            if(donor is null) return;
            _matrixAbilities=_matrixAbilities with {Used=_matrixAbilities.Used.Add($"death-gift-copy:{donor.RuntimeId}")};
            foreach(var gift in _matrixAbilities.Rules.Where(r=>r.Owner==donor.RuntimeId&&r.Key.Contains(":death-gift:",StringComparison.Ordinal)).ToArray())
            foreach(var child in gift.Operation.Effects) MatrixExecute(child,donor,explicitTarget,gift.Origin,null,null,gift.Key);
            return;
        }
        if(op.Kind==MatrixOperationKind.SkillVolley)
        {
            if(targets.Length==0||owner.ProjectileSequence is not null) return;
            var multiplier=op.AttackRatio+MatrixCounter(owner,op.Key)*op.CounterRatio;
            owner.ProjectileSequence=new(new CompiledProjectileSequenceAbilityOperation(op.Count,Math.Max(.1f,op.Amount),multiplier,op.MaxTargets,op.EventRatio),origin,targets[0].RuntimeId,op.Count);
            FireSequenceShot(owner);
        }
        else if(op.Kind==MatrixOperationKind.DashStrike)
        {
            var motion=MatrixDisplacement(op,owner);
            if(targets.Length>0&&MatrixCanStrikeInPlace(motion,owner,targets[0]))
            {
                // Already in legal contact: perform this cast's payoff without manufacturing a
                // trajectory. The enclosing ability checkpoint owns damage, children and payment.
                var target=targets[0];
                try
                {
                    MatrixHit(owner,target,Math.Max(0,motion.ImpactDamage+owner.Damage*motion.AttackRatio),origin);
                    if(target.Alive)
                        foreach(var child in op.Effects)MatrixExecute(child,owner,target.RuntimeId,origin,null,null,origin.StableId);
                }
                finally {MatrixDisplacementFinished(origin);}
            }
            else if(targets.Length>0&&PrepareDisplacementOperation(motion,owner,targets[0].RuntimeId,out var legal))
            {
                if(!op.Effects.IsEmpty)_matrixAbilities=_matrixAbilities with{Arrivals=_matrixAbilities.Arrivals.SetItem(origin.InstanceId,new(owner.RuntimeId,op,origin))};
                ExecuteDisplacementOperation(motion,legal,owner,origin);
            }
        }
        else if(op.Kind==MatrixOperationKind.LineDamage)
        {
            if(targets.Length==0)return;
            var direction=(targets[0].Position-owner.Position).Normalized();
            foreach(var target in _units.Where(u=>u.Alive&&u.Team!=owner.Team).OrderBy(u=>u.RuntimeId,StringComparer.Ordinal).ToArray())
            {
                var along=(target.Position-owner.Position).Dot(direction);
                var across=Math.Abs((target.Position-owner.Position).Cross(direction));
                if(along<0||along>op.Range||across>op.Radius+target.BodyRadius)continue;
                MatrixHit(owner,target,MatrixAmount(op,owner,target,fact),origin);
            }
        }
        else foreach(var target in targets)
        {
            if(!target.Alive)continue;
            var amount=MatrixAmount(op,owner,target,fact);
            switch(op.Kind)
            {
                case MatrixOperationKind.Damage: MatrixHit(owner,target,amount,origin); break;
                case MatrixOperationKind.Heal: HealLiving(owner.RuntimeId,target,amount,origin); break;
                case MatrixOperationKind.Shield: MatrixApplyTimedShield(owner.RuntimeId,target.RuntimeId,
                    op.ShieldCap>0?Math.Min(amount,Math.Max(0,op.ShieldCap-target.Shield)):amount,op.DurationTicks,origin); break;
                case MatrixOperationKind.Mana:
                    var manaFactor=origin.Kind==CombatSourceKind.Ability&&owner.Definition.AbilityLoadout?.Find(origin.StableId)?.IsActiveSkill==true
                        ? MatrixActionFactor(owner,true,origin.InstanceId):1;
                    MatrixGrantMana(owner.RuntimeId,target.RuntimeId,amount*manaFactor,true);break;
                case MatrixOperationKind.Chill: MatrixApplyChill(owner.RuntimeId,target.RuntimeId,Math.Max(1,(int)amount)); break;
                case MatrixOperationKind.Poison: MatrixApplyPoison(owner.RuntimeId,target.RuntimeId,Math.Max(1,(int)amount)); break;
                case MatrixOperationKind.ScalePoison: MatrixApplyPoison(owner.RuntimeId,target.RuntimeId,Math.Max(1,(int)(MatrixReadPoison(target.RuntimeId)*op.Amount))); break;
                case MatrixOperationKind.Ember: MatrixApplyEmber(owner.RuntimeId,target.RuntimeId,Math.Max(1,(int)amount)); break;
                case MatrixOperationKind.Counter:
                    if(op.ResetOnTargetChange&&_matrixAbilities.Targets.GetValueOrDefault(MatrixKey(owner,op.Key))!=explicitTarget)
                    {
                        MatrixWrite(owner,op.Key,0);
                        _matrixAbilities=_matrixAbilities with {Targets=_matrixAbilities.Targets.SetItem(MatrixKey(owner,op.Key),explicitTarget)};
                    }
                    MatrixWrite(target,op.Key,Math.Min(op.Maximum,MatrixCounter(target,op.Key)+amount)); break;
                case MatrixOperationKind.ClearCounter: MatrixWrite(target,op.Key,0); break;
                case MatrixOperationKind.PayHealth:
                    var paid=Math.Min(Math.Max(0,target.Health-1),target.Health*op.Amount);
                    target.Health-=paid; MatrixWrite(owner,op.Key.Length>0?op.Key:"health-paid",paid);
                    Emit("matrix_cost",owner.RuntimeId,target.RuntimeId,paid,target.Position,"skill_cast"); break;
                case MatrixOperationKind.ConsumeShield:
                    var spent=Math.Min(target.Shield,target.Shield*op.Amount);
                    target.Shield-=spent; TraitMechanicsShieldConsumed(target,spent,false,origin);
                    MatrixWrite(owner,op.Key,spent); break;
                case MatrixOperationKind.ConsumePoison:
                    var consumed=MatrixConsumePoison(target.RuntimeId,Math.Min(op.Count,MatrixReadPoison(target.RuntimeId)));
                    MatrixWrite(owner,op.Key,consumed); break;
                case MatrixOperationKind.TransferPoison:
                    var from=MatrixUnit(explicitTarget);
                    if(from is not null&&from!=target)MatrixTransferPoison(from.RuntimeId,target.RuntimeId,Math.Min(op.Count,MatrixReadPoison(from.RuntimeId))); break;
                case MatrixOperationKind.TimedAttribute: MatrixAttribute(target,op,amount,origin,slot); break;
                case MatrixOperationKind.PermanentAttribute: ApplyPermanentAttribute(owner,target,op,origin,slot); break;
                case MatrixOperationKind.NextSkillBoost or MatrixOperationKind.NextAttackBoost:
                    var boostKey=$"{target.RuntimeId}:{op.Key}:{op.Kind}";
                    _matrixAbilities=_matrixAbilities with {Boosts=_matrixAbilities.Boosts.Where(b=>b.Key!=boostKey).ToImmutableArray().Add(
                        new(target.RuntimeId,boostKey,op.Kind==MatrixOperationKind.NextSkillBoost,amount,TickIndex+op.DurationTicks))}; break;
                case MatrixOperationKind.ExtendSummon:
                    var product=_matrixAbilities.Products.FirstOrDefault(p=>p.Unit==target.RuntimeId&&p.Owner==owner.RuntimeId);
                    if(product is not null&&product.Extensions<op.Count)
                        _matrixAbilities=_matrixAbilities with {Products=_matrixAbilities.Products.Replace(product,product with{Expires=product.Expires+op.DurationTicks,Extensions=product.Extensions+1})}; break;
                case MatrixOperationKind.Taunt:
                    SetActionTarget(target,owner);
                    if(op.DurationTicks>1)_matrixAbilities=_matrixAbilities with {Jobs=_matrixAbilities.Jobs.Add(new(owner.RuntimeId,target.RuntimeId,TickIndex+1,op with{Target=MatrixTargetKind.EventTarget,DurationTicks=op.DurationTicks-1},origin,null,null))}; break;
                case MatrixOperationKind.ExtraShot:
                    LaunchProjectile(owner,target,amount,origin);break;
            }
        }
        if(op.ConsumeCounter&&op.Key.Length>0) MatrixWrite(owner,op.Key,MatrixCounter(owner,op.Key)*(1-op.CounterConsumeRatio));
        if(op.ConsumeCounter&&op.RequiredCounter.Length>0) MatrixWrite(owner,op.RequiredCounter,MatrixCounter(owner,op.RequiredCounter)*(1-op.CounterConsumeRatio));
    }

    private CompiledDisplacementOperation MatrixDisplacement(CompiledMatrixOperation op,BattleUnitState owner) => new(op.LandingSide?DisplacementKind.Leap:DisplacementKind.Charge,
        new CompiledExplicitTargetQuery(),
        op.Range,Math.Max(1,op.DurationTicks),.1f,false,false,op.Amount+MatrixCounter(owner,op.Key)*op.CounterRatio,op.AttackRatio,op.Radius,EffectDamageType.Normal,null,0,
        LandingAngleRadians:op.LandingSide?Mathf.Pi/2:0);

    private void MatrixDisplacementArrived(BattleUnitState source,BattleUnitState target,CombatSourceRef origin)
    {
        if(string.IsNullOrEmpty(origin.InstanceId)||!_matrixAbilities.Arrivals.TryGetValue(origin.InstanceId,out var pending))return;
        foreach(var child in pending.Operation.Effects)MatrixExecute(child,source,target.RuntimeId,origin,null,null,origin.StableId);
    }
    private void MatrixDisplacementFinished(CombatSourceRef origin)
    {
        // Non-ability displacements also share this lifecycle and may carry a default origin.
        if(string.IsNullOrEmpty(origin.InstanceId))return;
        _matrixAbilities=_matrixAbilities with{Arrivals=_matrixAbilities.Arrivals.Remove(origin.InstanceId)};
    }

    private void MatrixHit(BattleUnitState owner,BattleUnitState target,float amount,CombatSourceRef origin)
    {
        if(!target.Alive)return;
        var active=owner.Definition.AbilityLoadout?.Find(origin.StableId)?.IsActiveSkill==true;
        var vitality=target.Health+target.Shield;
        ApplyDamage(owner.RuntimeId,owner,target,amount,origin,EffectDamageType.Normal,
            active?CombatDamageClass.ActiveSkill:CombatDamageClass.Derived,origin.InstanceId);
        var damage=Math.Max(0,vitality-target.Health-target.Shield);
        if(active)PublishCombat(new BattleCombatEventDraft(BattleCombatEventKind.SkillHitLanded,origin,owner.RuntimeId,target.RuntimeId,
            TickIndex,amount,damage,damage,SubjectStableId:origin.StableId,DamageClass:CombatDamageClass.ActiveSkill,ActionId:origin.InstanceId));
    }

    private void MatrixAttribute(BattleUnitState target,CompiledMatrixOperation op,float amount,CombatSourceRef origin,string slot)
    {
        var key=$"{origin.OwnerRuntimeId}:{slot}:{target.RuntimeId}:{op.Attribute}";
        foreach(var previous in _matrixAbilities.Buffs.Where(b=>b.Key==key))target.Attributes.Remove(previous.Handle);
        var handle=target.Attributes.ApplyModifier(new CompiledAttributeModifier(op.Attribute,AttributeModifierOperation.Add,
            new CompiledConstantMagnitude(amount),0,key),origin);
        _matrixAbilities=_matrixAbilities with {Buffs=_matrixAbilities.Buffs.Where(b=>b.Key!=key).ToImmutableArray().Add(new(target.RuntimeId,origin.OwnerRuntimeId,key,TickIndex+op.DurationTicks,handle,op.OwnerCondition,op.TargetCondition,op.HealthThreshold))};
    }

    private bool MatrixRelationMatches(MatrixRelation relation,BattleUnitState owner,BattleUnitState? other)=>relation switch
    {
        MatrixRelation.Any=>true,MatrixRelation.Owner=>other==owner,MatrixRelation.Ally=>other?.Team==owner.Team,
        MatrixRelation.OtherAlly=>other is not null&&other!=owner&&other.Team==owner.Team,
        MatrixRelation.Enemy=>other is not null&&other.Team!=owner.Team,
        MatrixRelation.OwnedSummon=>other is not null&&other.Team==owner.Team&&other.SummonerRuntimeId==owner.RuntimeId,_=>false
    };

    private bool MatrixEventMatches(MatrixEventKind kind,BattleCombatEvent e)=>kind switch
    {
        MatrixEventKind.AttackHit=>e.Kind==BattleCombatEventKind.AttackLanded&&e.DamageClass is CombatDamageClass.BasicAttack or CombatDamageClass.Other,
        MatrixEventKind.SkillHit=>e.Kind==BattleCombatEventKind.SkillHitLanded&&e.DamageClass==CombatDamageClass.ActiveSkill,
        MatrixEventKind.HealthLost=>e.Kind==BattleCombatEventKind.HealthLost,
        MatrixEventKind.ShieldReceived=>e.Kind==BattleCombatEventKind.ShieldResolved,
        MatrixEventKind.ShieldBroken=>e.Kind==BattleCombatEventKind.StatusStackChanged&&e.SubjectStableId=="matrix_shield_broken",
        MatrixEventKind.Healing=>e.Kind==BattleCombatEventKind.HealingResolved,
        MatrixEventKind.ManaCast=>e.Kind==BattleCombatEventKind.ManaSkillResolved&&e.EffectiveValue>0,
        MatrixEventKind.Death=>e.Kind==BattleCombatEventKind.UnitDefeated&&e.Reason is "" or "Consumed"&&!MatrixExcludedDeath(e.TargetRuntimeId),
        MatrixEventKind.Control=>e.Kind==BattleCombatEventKind.ControlApplied,
        MatrixEventKind.PoisonTransferred=>e.Kind==BattleCombatEventKind.StatusStackChanged&&e.SubjectStableId=="matrix_poison_transfer",
        MatrixEventKind.EmberDetonated=>e.Kind==BattleCombatEventKind.StatusStackChanged&&e.SubjectStableId=="matrix_ember_detonation",
        MatrixEventKind.SummonHit=>e.Kind==BattleCombatEventKind.AttackLanded,
        MatrixEventKind.PoisonApplied=>e.Kind==BattleCombatEventKind.StatusApplied&&e.SubjectStableId=="matrix_poison",
        _=>false
    };

    private void DrainMatrixAbilityEvents()
    {
        if(_drainingMatrixAbilities)return;
        _drainingMatrixAbilities=true;
        try
        {
            var budget=0;
            while(!_matrixAbilities.Pending.IsEmpty)
            {
                if(++budget>_combatPipeline.Limits.MaxReactions)throw new InvalidOperationException("Matrix reaction budget exceeded.");
                var fact=_matrixAbilities.Pending[0];
                _matrixAbilities=_matrixAbilities with {Pending=_matrixAbilities.Pending.RemoveAt(0)};
                var pruneCheckpoint=new BattleWorldStateCheckpoint(this);
                try{using var pruneResolution=_combatPipeline.BeginAuthoritativeResolution();MatrixPruneBuffs();pruneResolution.Commit();pruneCheckpoint.Commit();}
                catch{pruneCheckpoint.Rollback();throw;}
                foreach(var rule in _matrixAbilities.Rules.ToArray())MatrixRunRule(rule,fact);
                if(fact.Kind==BattleCombatEventKind.UnitDefeated)
                {
                    var dead=MatrixUnit(fact.TargetRuntimeId);
                    if(dead is not null)
                        foreach(var rule in _matrixAbilities.Rules.Where(r=>r.Owner==dead.RuntimeId))
                        foreach(var operation in MatrixAbilityCompiler.Flatten(rule.Operation).Where(o=>o.ClearOnDeath&&o.Key.Length>0))MatrixWrite(dead,operation.Key,0);
                }
            }
        }
        finally{_drainingMatrixAbilities=false;}
    }

    private void MatrixRunRule(MatrixRule rule,BattleCombatEvent? fact)
    {
        var op=rule.Operation;var owner=MatrixUnit(rule.Owner);
        if(owner is null||(op.LivingOwnerRequired&&!owner.Alive)||!MatrixConditionMatches(op.OwnerCondition,owner,op.HealthThreshold))return;
        if(op.RequiredCounter.Length>0&&MatrixCounter(owner,op.RequiredCounter)<op.CounterThreshold)return;
        if(fact is null && op.Event!=MatrixEventKind.Tick)return;
        var source=fact is null?owner:MatrixUnit(fact.SourceRuntimeId);
        var target=fact is null?owner:MatrixUnit(fact.TargetRuntimeId);
        if(fact is not null&&(!MatrixEventMatches(op.Event,fact)||!MatrixRelationMatches(op.SourceRelation,owner,source)||
            !MatrixRelationMatches(op.TargetRelation,owner,target)||fact.EffectiveValue<op.MinimumEventValue))return;
        if(op.ActiveSkillOnly&&(fact is null||fact.Source.Kind!=CombatSourceKind.Ability||
            source?.Definition.AbilityLoadout?.Find(fact.Source.StableId)?.IsActiveSkill!=true))return;
        if(op.Event==MatrixEventKind.PoisonApplied&&target is not null&&MatrixReadPoison(target.RuntimeId)<op.CounterThreshold)return;
        var conditionMatches=fact?.Kind==BattleCombatEventKind.UnitDefeated&&target is not null&&
            _matrixAbilities.DeathConditions.TryGetValue(target.RuntimeId,out var prior)
            ? prior.Contains(op.TargetCondition) : MatrixConditionMatches(op.TargetCondition,target,op.HealthThreshold);
        if(fact?.Kind==BattleCombatEventKind.HealingResolved&&op.TargetCondition==MatrixCondition.LowHealth&&target is not null)
            conditionMatches=(fact.TargetHealthRatio>=0?fact.TargetHealthRatio:(target.Health-fact.EffectiveValue)/target.MaxHealth)<op.HealthThreshold;
        if(!conditionMatches||op.TemporaryOnly&&target?.IsTemporary!=true)return;
        var counterpart=target==owner?source:target;
        if(counterpart is not null&&counterpart.Position.DistanceTo(owner.Position)>op.Range)return;
        var key=rule.Key+(op.PerTargetCooldown?":"+counterpart?.RuntimeId:"");
        if(_matrixAbilities.Ready.GetValueOrDefault(key)>TickIndex)return;
        var window=op.TargetCondition==MatrixCondition.Frozen?_matrixAbilities.Counters.GetValueOrDefault($"control-window:{target?.RuntimeId}"):0;
        var unique=op.OncePerBattle?rule.Key:op.OncePerSource?$"{rule.Key}:source:{source?.RuntimeId}":op.OncePerTarget?$"{rule.Key}:target:{counterpart?.RuntimeId}:{window}":
            op.OncePerAction?$"{rule.Key}:action:{fact?.ActionId}":"";
        if(unique.Length>0&&_matrixAbilities.Used.Contains(unique))return;
        if(op.OncePerAction&&string.IsNullOrEmpty(fact?.ActionId))return;
        using var continuation=fact is null?null:_combatPipeline.ContinueReaction(fact.ChainId,fact.Depth+1);
        using var resolution=_combatPipeline.BeginAuthoritativeResolution();
        var checkpoint=new BattleWorldStateCheckpoint(this);
        try
        {
            var countKey=op.OncePerAction?$"{rule.Key}:count:{fact!.ActionId}":$"{rule.Key}:count";
            if(op.OncePerAction)
            {
                var hitKey=$"{countKey}:{target?.RuntimeId}";
                if(_matrixAbilities.Used.Contains(hitKey)){resolution.Commit();checkpoint.Commit();return;}
                _matrixAbilities=_matrixAbilities with {Used=_matrixAbilities.Used.Add(hitKey)};
            }
            var count=_matrixAbilities.Counters.GetValueOrDefault(countKey)+1;
            _matrixAbilities=_matrixAbilities with {Counters=_matrixAbilities.Counters.SetItem(countKey,count)};
            if((int)count%op.Every!=0){resolution.Commit();checkpoint.Commit();return;}
            _matrixAbilities=_matrixAbilities with {Ready=_matrixAbilities.Ready.SetItem(key,TickIndex+Math.Max(op.CooldownTicks,op.Event==MatrixEventKind.Tick?op.IntervalTicks:0))};
            if(unique.Length>0)_matrixAbilities=_matrixAbilities with {Used=_matrixAbilities.Used.Add(unique)};
            var effectOrigin=rule.Origin with {InstanceId=$"{rule.Origin.InstanceId}:reaction:{fact?.Sequence??TickIndex}"};
            MatrixPruneBuffs();
            for(var i=0;i<op.Effects.Length;i++)
            {
                var child=op.Effects[i];
                if(op.Event==MatrixEventKind.Tick&&child.Kind==MatrixOperationKind.TimedAttribute)
                    child=child with{OwnerCondition=op.OwnerCondition,TargetCondition=op.TargetCondition,HealthThreshold=op.HealthThreshold};
                MatrixExecute(child,owner,counterpart?.RuntimeId??"",effectOrigin,fact,null,$"{rule.Key}:{i}");
            }
            resolution.Commit();checkpoint.Commit();
        }
        catch{checkpoint.Rollback();throw;}
    }

    private void AdvanceMatrixAbilities()
    {
        if(_matrixAbilities.Buffs.IsEmpty&&_matrixAbilities.Boosts.IsEmpty&&_matrixAbilities.Products.IsEmpty&&
            _matrixAbilities.Jobs.IsEmpty&&_matrixAbilities.Pending.IsEmpty&&
            !_matrixAbilities.Rules.Any(r=>r.Operation.Event==MatrixEventKind.Tick))return;
        var checkpoint=new BattleWorldStateCheckpoint(this);
        try
        {
        using var resolution=_combatPipeline.BeginAuthoritativeResolution();
        MatrixPruneBuffs();
        _matrixAbilities=_matrixAbilities with{Boosts=_matrixAbilities.Boosts.Where(b=>b.Expires>TickIndex&&MatrixUnit(b.Owner)?.Alive==true).ToImmutableArray()};
        foreach(var product in _matrixAbilities.Products.ToArray())
        {
            var unit=MatrixUnit(product.Unit);
            if(unit?.Alive==true&&product.Expires>TickIndex)continue;
            _matrixAbilities=_matrixAbilities with{Products=_matrixAbilities.Products.Remove(product)};
            if(unit?.Alive==true)RetireEnemyProduct(unit,product.Owner,"matrix_product_expired");
        }
        foreach(var job in _matrixAbilities.Jobs.Where(j=>j.ReadyTick<=TickIndex).ToArray())
        {
            _matrixAbilities=_matrixAbilities with{Jobs=_matrixAbilities.Jobs.Remove(job)};
            if(MatrixUnit(job.Owner) is not {Alive:true} owner)continue;
            if(job.Operation.Kind==MatrixOperationKind.Taunt&&job.Operation.DurationTicks<=0)continue;
            MatrixExecute(job.Operation,owner,job.Target,job.Origin,job.Event,job.Center,job.Origin.StableId);
        }
        resolution.Commit();checkpoint.Commit();
        }
        catch{checkpoint.Rollback();throw;}
        foreach(var rule in _matrixAbilities.Rules.Where(r=>r.Operation.Event==MatrixEventKind.Tick).ToArray())MatrixRunRule(rule,null);
        DrainMatrixAbilityEvents();
    }

    private void MatrixPruneBuffs()
    {
        foreach(var buff in _matrixAbilities.Buffs.Where(b=>b.Expires<=TickIndex||MatrixUnit(b.Owner)?.Alive!=true||
            !MatrixConditionMatches(b.OwnerCondition,MatrixUnit(b.Source),b.HealthThreshold)||
            !MatrixConditionMatches(b.TargetCondition,MatrixUnit(b.Owner),b.HealthThreshold)).ToArray())
        {MatrixUnit(buff.Owner)?.Attributes.Remove(buff.Handle);_matrixAbilities=_matrixAbilities with{Buffs=_matrixAbilities.Buffs.Remove(buff)};}
    }

    // Register as a calculation subscriber. One cast token shares its lease across every arrow/target.
    private float MatrixAbilityCalculation(BattleCombatCalculationRequest request,float amount)
    {
        var source=MatrixUnit(request.SourceRuntimeId);if(source is null)return amount;
        var skill=request.DamageClass==CombatDamageClass.ActiveSkill || request.Kind!=BattleCombatCalculationKind.Damage&&
            source.Definition.AbilityLoadout?.Find(request.Source.StableId)?.IsActiveSkill==true;
        var basic=request.Kind==BattleCombatCalculationKind.Damage&&request.DamageClass==CombatDamageClass.BasicAttack;
        if(!skill&&!basic)return amount;
        var fallback=skill?request.Source.InstanceId:$"{request.Tick}:{_statistics[source.RuntimeId].AttackActions}";
        return amount*MatrixActionFactor(source,skill,request.ActionId.Length>0?request.ActionId:fallback);
    }

    private float MatrixActionFactor(BattleUnitState source,bool skill,string token)
    {
        var action=$"{source.RuntimeId}:{(skill?"skill":"attack")}:{token}";
        if(!_matrixAbilities.ActionFactors.TryGetValue(action,out var factor))
        {
            var leases=_matrixAbilities.Boosts.Where(b=>b.Owner==source.RuntimeId&&b.Skill==skill&&b.Expires>TickIndex).ToArray();
            factor=1+leases.Sum(b=>b.Amount);
            _matrixAbilities=_matrixAbilities with{Boosts=_matrixAbilities.Boosts.Except(leases).ToImmutableArray(),ActionFactors=_matrixAbilities.ActionFactors.SetItem(action,factor)};
        }
        return factor;
    }

    private void ClearMatrixAbilities()
    {
        foreach(var buff in _matrixAbilities.Buffs)MatrixUnit(buff.Owner)?.Attributes.Remove(buff.Handle);
        _matrixAbilities=MatrixAbilityState.Empty;
    }
}
