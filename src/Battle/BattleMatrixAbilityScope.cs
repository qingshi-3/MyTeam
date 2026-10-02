using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Battle;

internal interface IBattleMatrixOperationWorld
{
    int Tick { get; }
    IReadOnlyList<BattleUnitState> Units { get; }
    IReadOnlyDictionary<string, UnitSnapshot> SummonProfiles { get; }
    CombatSourceRef AbilityOrigin(CompiledAbilityDefinition ability, string ownerId);
    bool HasLineAccess(BattleUnitState source, BattleUnitState target);
    Vector2? FindOpenNear(Vector2 origin, int team, float radius);
    bool SpawnNear(UnitSnapshot profile, int team, Vector2 origin, float health, float damage, string source);
    bool ExtraSummon(string id);
    void FireSequenceShot(BattleUnitState unit);
    void LaunchProjectile(BattleUnitState owner, BattleUnitState target, float damage, CombatSourceRef origin);
    float Damage(string sourceId, BattleUnitState owner, BattleUnitState target, float amount, CombatSourceRef origin,
        EffectDamageType type, CombatDamageClass damageClass, string actionId);
    float Heal(string source, BattleUnitState target, float amount, CombatSourceRef origin);
    void Retire(BattleUnitState unit, string ownerId, string reason);
    void Resolve(Action work);
    void Publish(BattleCombatEventDraft fact);
    void Present(string type, string source, string target, float value, Vector2 position, string cue);
}

internal sealed class BattleMatrixAbilityScope(IBattleMatrixOperationWorld world, BattleMatrixQueries queries,
    BattleMatrixReactionScope reactions, BattleDisplacementScope displacements, BattleElementStatusScope elements,
    BattleSupportEffectScope support, BattleStatistics statistics, BattlePermanentGrowthScope growth)
{
    private static void SetActionTarget(BattleUnitState owner, BattleUnitState target)
    { owner.ActionTargetRuntimeId = target.RuntimeId; owner.ActionTargetName = target.Definition.DisplayName; }
    internal sealed class Checkpoint
    {
        private readonly BattleMatrixAbilityScope _owner;
        private readonly MatrixAbilityState _state;
        internal Checkpoint(BattleMatrixAbilityScope owner) { _owner = owner; _state = owner._matrixAbilities; }
        internal void Restore(BattleMatrixAbilityScope owner)
        {
            if (!ReferenceEquals(owner, _owner)) throw new InvalidOperationException("Checkpoint belongs to another matrix operation scope.");
            owner._matrixAbilities = _state;
        }
    }
    public Checkpoint Capture() => new(this);
    public void Restore(Checkpoint checkpoint) => checkpoint.Restore(this);
    private void ClearState() => _matrixAbilities = MatrixAbilityState.Empty;
    private sealed record MatrixJob(string Owner, string Target, int ReadyTick, CompiledMatrixOperation Operation,
        CombatSourceRef Origin, BattleCombatEvent? Event, Vector2? Center);
    private sealed record MatrixBuff(string Owner, string Source, string Key, int Expires, AttributeModifierHandle Handle,
        MatrixCondition OwnerCondition,MatrixCondition TargetCondition,float HealthThreshold);
    private sealed record MatrixBoost(string Owner, string Key, bool Skill, float Amount, int Expires);
    private sealed record MatrixProduct(string Unit, string Owner, int Expires, int Extensions);
    private sealed record MatrixArrival(string Owner,CompiledMatrixOperation Operation,CombatSourceRef Origin);
    private sealed record MatrixAbilityState(ImmutableArray<MatrixJob> Jobs,
        ImmutableArray<MatrixBuff> Buffs, ImmutableArray<MatrixBoost> Boosts, ImmutableArray<MatrixProduct> Products,
        ImmutableDictionary<string,float> Counters, ImmutableDictionary<string,string> Targets,
        ImmutableDictionary<string,float> ActionFactors, ImmutableDictionary<string,MatrixArrival> Arrivals)
    {
        public static MatrixAbilityState Empty => new([],[],[],[],ImmutableDictionary<string,float>.Empty,
            ImmutableDictionary<string,string>.Empty,ImmutableDictionary<string,float>.Empty,
            ImmutableDictionary<string,MatrixArrival>.Empty);
    }
    private MatrixAbilityState _matrixAbilities = MatrixAbilityState.Empty;
    public float Counter(string key) => _matrixAbilities.Counters.GetValueOrDefault(key);
    public void Write(string key, float value) => _matrixAbilities = _matrixAbilities with { Counters = _matrixAbilities.Counters.SetItem(key, Math.Max(0, value)) };
    private BattleUnitState? MatrixUnit(string id) => world.Units.FirstOrDefault(u=>u.RuntimeId==id);
    private string MatrixKey(BattleUnitState owner,string key) => $"{owner.RuntimeId}:{key}";
    private float MatrixCounter(BattleUnitState owner,string key) => Counter(MatrixKey(owner,key));
    private void MatrixWrite(BattleUnitState owner,string key,float amount) =>
        Write(MatrixKey(owner,key), amount);

    public bool PrepareMatrixOperation(CompiledMatrixOperation operation,BattleUnitState owner,string target,
        out ImmutableArray<string> targets)
    {
        targets = (operation.Kind is MatrixOperationKind.RegisterReaction or MatrixOperationKind.DeathGift)
            ? ImmutableArray.Create(owner.RuntimeId) : MatrixTargets(operation,owner,target,null).Select(u=>u.RuntimeId).ToImmutableArray();
        if(operation.Kind==MatrixOperationKind.Summon && (!world.SummonProfiles.ContainsKey(operation.ContentId) ||
            world.Units.Count(u=>u.Alive && u.SummonerRuntimeId==owner.RuntimeId && u.Definition.ContentId==operation.ContentId)>=operation.Maximum)) return false;
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
        foreach(var candidate in MatrixTargets(operation with{MaxTargets=Math.Max(1,world.Units.Count)},owner,explicitTarget,null))
        {
            if(MatrixCanStrikeInPlace(motion,owner,candidate))
            {targets=ImmutableArray.Create(candidate.RuntimeId);return true;}
            if(!displacements.PrepareDisplacementOperation(motion,owner,candidate.RuntimeId,out var legal))continue;
            var plan=displacements.BuildDisplacementPlans(motion,owner,[candidate]).FirstOrDefault();
            // A clipped charge ending before contact is movement, not a reachable strike.
            if(plan is null||plan.End.DistanceTo(candidate.Position)>
                owner.BodyRadius+candidate.BodyRadius+motion.StopDistance+.2f)continue;
            targets=legal;return true;
        }
        targets=[];
        return false;
    }

    private bool MatrixCanStrikeInPlace(CompiledDisplacementOperation motion,BattleUnitState owner,BattleUnitState target) =>
        motion.Kind==DisplacementKind.Charge&&displacements.IsDisplacementTargetEligible(motion,owner,target)&&
        owner.Position.DistanceTo(target.Position)<=motion.Distance&&
        owner.Position.DistanceTo(target.Position)<=owner.BodyRadius+target.BodyRadius+motion.StopDistance+.2f&&
        world.HasLineAccess(owner,target);

    public void ExecuteMatrixOperation(ResolvedAbilityOperation resolved,AbilityExecutionPlan plan,BattleUnitState owner)
    {
        var op=(CompiledMatrixOperation)resolved.Operation;
        var origin=world.AbilityOrigin(plan.Ability,owner.RuntimeId);
        if(op.Kind==MatrixOperationKind.TransferPoison)
        {
            if(resolved.TargetIds.Length>1)MatrixExecute(op,owner,resolved.TargetIds[0],origin,null,null,
                $"{plan.Ability.StableId}:{resolved.OperationIndex}",resolved.TargetIds.RemoveAt(0));
            return;
        }
        MatrixExecute(op,owner,resolved.TargetIds.FirstOrDefault()??"",origin,null,null,
            $"{plan.Ability.StableId}:{resolved.OperationIndex}",resolved.TargetIds);
    }

    private IEnumerable<BattleUnitState> MatrixTargets(CompiledMatrixOperation operation, BattleUnitState owner,
        string target, BattleCombatEvent? fact, Vector2? center = null) =>
        queries.Select(operation, BattleUnitView.For(owner), target, fact, center)
            .Select(unit => MatrixUnit(unit.RuntimeId)!);
    private bool MatrixConditionMatches(MatrixCondition condition, BattleUnitState? unit, float threshold) =>
        queries.MatchesCondition(condition, unit is null ? null : BattleUnitView.For(unit), threshold);

    private float MatrixAmount(CompiledMatrixOperation op,BattleUnitState owner,BattleUnitState target,BattleCombatEvent? fact)
    {
        var amount=op.Amount+owner.Damage*op.AttackRatio+owner.MaxHealth*op.OwnerHealthRatio+
            target.MaxHealth*op.TargetHealthRatio+(fact?.EffectiveValue??0)*op.EventRatio+
            (MatrixUnit(fact?.TargetRuntimeId??"")?.MaxHealth??0)*op.EventTargetHealthRatio+
            MatrixCounter(op.CounterOnTarget?target:owner,op.Key)*op.CounterRatio;
        if(fact is not null && MatrixUnit(fact.TargetRuntimeId)?.IsTemporary==true) amount*=op.TemporaryScale;
        return Math.Clamp(amount,op.Kind is MatrixOperationKind.Counter or MatrixOperationKind.TimedAttribute?-op.Maximum:0,op.Maximum);
    }

    public void MatrixExecute(CompiledMatrixOperation op,BattleUnitState owner,string explicitTarget,CombatSourceRef origin,
        BattleCombatEvent? fact,Vector2? center,string slot,ImmutableArray<string> prepared=default)
    {
        if(op.RequiredCounter.Length>0&&MatrixCounter(owner,op.RequiredCounter)<op.CounterThreshold) return;
        if(!MatrixConditionMatches(op.OwnerCondition,owner,op.HealthThreshold)) return;
        var targets=prepared.IsDefault?MatrixTargets(op,owner,explicitTarget,fact,center).ToArray():
            prepared.Select(MatrixUnit).Where(u=>u is not null).Cast<BattleUnitState>().ToArray();
        if(op.Kind==MatrixOperationKind.RegisterReaction)
        {
            var key=$"{owner.RuntimeId}:{slot}";
            reactions.Register(owner.RuntimeId, key, op, origin);
            return;
        }
        if(op.Kind==MatrixOperationKind.DeathGift)
        {
            var rule=op with {Kind=MatrixOperationKind.RegisterReaction,Event=MatrixEventKind.Death,TargetRelation=MatrixRelation.Owner,
                LivingOwnerRequired=false,OncePerBattle=true,Effects=op.Effects};
            var key=$"{owner.RuntimeId}:death-gift:{slot}";
            reactions.Register(owner.RuntimeId, key, rule, origin);
            return;
        }
        if(op.Kind==MatrixOperationKind.Delay)
        {
            var anchor=targets.FirstOrDefault()?.Position??center??owner.Position;
            for(var pulse=0;pulse<op.Count;pulse++)
            foreach(var child in op.Effects)
                _matrixAbilities=_matrixAbilities with {Jobs=_matrixAbilities.Jobs.Add(new(owner.RuntimeId,explicitTarget,
                    world.Tick+op.DurationTicks+pulse*op.IntervalTicks,child,origin,fact,anchor))};
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
            if(!world.SummonProfiles.TryGetValue(op.ContentId,out var product)) throw new InvalidOperationException($"Missing summon {op.ContentId}");
            var spawned=false;
            for(var i=0;i<op.Count&&world.Units.Count(u=>u.Alive&&u.SummonerRuntimeId==owner.RuntimeId&&u.Definition.ContentId==op.ContentId)<op.Maximum;i++)
            {
                var before=world.Units.Count;
                if(!world.SpawnNear(product,owner.Team,owner.Position,1,1,owner.RuntimeId)) break;
                spawned=true;
                foreach(var unit in world.Units.Skip(before)) _matrixAbilities=_matrixAbilities with {Products=_matrixAbilities.Products.Add(new(unit.RuntimeId,owner.RuntimeId,world.Tick+op.DurationTicks,0))};
            }
            if(spawned&&world.ExtraSummon(owner.RuntimeId)&&
                world.Units.Count(u=>u.Alive&&u.SummonerRuntimeId==owner.RuntimeId&&u.Definition.ContentId==op.ContentId)<op.Maximum&&
                world.FindOpenNear(owner.Position,owner.Team,product.BodyRadius) is not null)
            {
                var before=world.Units.Count;
                if(world.SpawnNear(product,owner.Team,owner.Position,1,1,owner.RuntimeId))
                    foreach(var unit in world.Units.Skip(before))_matrixAbilities=_matrixAbilities with{Products=_matrixAbilities.Products.Add(new(unit.RuntimeId,owner.RuntimeId,world.Tick+op.DurationTicks,0))};
            }
            return;
        }
        if(op.Kind==MatrixOperationKind.EchoDeathGift)
        {
            reactions.EchoDeathGift(targets.Select(target => target.RuntimeId), explicitTarget);
            return;
        }
        if(op.Kind==MatrixOperationKind.SkillVolley)
        {
            if(targets.Length==0||owner.ProjectileSequence is not null) return;
            var multiplier=op.AttackRatio+MatrixCounter(owner,op.Key)*op.CounterRatio;
            owner.ProjectileSequence=new(new CompiledProjectileSequenceAbilityOperation(op.Count,Math.Max(.1f,op.Amount),multiplier,op.MaxTargets,op.EventRatio),origin,targets[0].RuntimeId,op.Count);
            world.FireSequenceShot(owner);
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
            else if(targets.Length>0&&displacements.PrepareDisplacementOperation(motion,owner,targets[0].RuntimeId,out var legal))
            {
                if(!op.Effects.IsEmpty)_matrixAbilities=_matrixAbilities with{Arrivals=_matrixAbilities.Arrivals.SetItem(origin.InstanceId,new(owner.RuntimeId,op,origin))};
                displacements.ExecuteDisplacementOperation(motion,legal,owner,origin);
            }
        }
        else if(op.Kind==MatrixOperationKind.LineDamage)
        {
            if(targets.Length==0)return;
            var direction=(targets[0].Position-owner.Position).Normalized();
            foreach(var target in world.Units.Where(u=>u.Alive&&u.Team!=owner.Team).OrderBy(u=>u.RuntimeId,StringComparer.Ordinal).ToArray())
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
                case MatrixOperationKind.Heal: world.Heal(owner.RuntimeId,target,amount,origin); break;
                case MatrixOperationKind.Shield: support.ApplyShield(owner.RuntimeId,target.RuntimeId,
                    op.ShieldCap>0?Math.Min(amount,Math.Max(0,op.ShieldCap-target.Shield)):amount,op.DurationTicks,origin); break;
                case MatrixOperationKind.Mana:
                    var manaFactor=origin.Kind==CombatSourceKind.Ability&&owner.Definition.AbilityLoadout?.Find(origin.StableId)?.IsActiveSkill==true
                        ? MatrixActionFactor(owner,true,origin.InstanceId):1;
                    support.GrantMana(owner.RuntimeId,target.RuntimeId,amount*manaFactor,true);break;
                case MatrixOperationKind.Chill: elements.ApplyChill(owner.RuntimeId,target.RuntimeId,Math.Max(1,(int)amount)); break;
                case MatrixOperationKind.Poison: elements.ApplyPoison(owner.RuntimeId,target.RuntimeId,Math.Max(1,(int)amount)); break;
                case MatrixOperationKind.ScalePoison: elements.ApplyPoison(owner.RuntimeId,target.RuntimeId,Math.Max(1,(int)(elements.ReadPoison(target.RuntimeId)*op.Amount))); break;
                case MatrixOperationKind.Ember: elements.ApplyEmber(owner.RuntimeId,target.RuntimeId,Math.Max(1,(int)amount)); break;
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
                    world.Present("matrix_cost",owner.RuntimeId,target.RuntimeId,paid,target.Position,"skill_cast"); break;
                case MatrixOperationKind.ConsumeShield:
                    var spent=Math.Min(target.Shield,target.Shield*op.Amount);
                    target.Shield-=spent; support.ShieldConsumed(BattleUnitView.For(target),spent,false,origin);
                    MatrixWrite(owner,op.Key,spent); break;
                case MatrixOperationKind.ConsumePoison:
                    var consumed=elements.ConsumePoison(target.RuntimeId,Math.Min(op.Count,elements.ReadPoison(target.RuntimeId)));
                    MatrixWrite(owner,op.Key,consumed); break;
                case MatrixOperationKind.TransferPoison:
                    var from=MatrixUnit(explicitTarget);
                    if(from is not null&&from!=target)elements.TransferPoison(from.RuntimeId,target.RuntimeId,Math.Min(op.Count,elements.ReadPoison(from.RuntimeId))); break;
                case MatrixOperationKind.TimedAttribute: MatrixAttribute(target,op,amount,origin,slot); break;
                case MatrixOperationKind.PermanentAttribute: growth.ApplyPermanentAttribute(owner,target,op,origin,slot); break;
                case MatrixOperationKind.NextSkillBoost or MatrixOperationKind.NextAttackBoost:
                    var boostKey=$"{target.RuntimeId}:{op.Key}:{op.Kind}";
                    _matrixAbilities=_matrixAbilities with {Boosts=_matrixAbilities.Boosts.Where(b=>b.Key!=boostKey).ToImmutableArray().Add(
                        new(target.RuntimeId,boostKey,op.Kind==MatrixOperationKind.NextSkillBoost,amount,world.Tick+op.DurationTicks))}; break;
                case MatrixOperationKind.ExtendSummon:
                    var product=_matrixAbilities.Products.FirstOrDefault(p=>p.Unit==target.RuntimeId&&p.Owner==owner.RuntimeId);
                    if(product is not null&&product.Extensions<op.Count)
                        _matrixAbilities=_matrixAbilities with {Products=_matrixAbilities.Products.Replace(product,product with{Expires=product.Expires+op.DurationTicks,Extensions=product.Extensions+1})}; break;
                case MatrixOperationKind.Taunt:
                    SetActionTarget(target,owner);
                    if(op.DurationTicks>1)_matrixAbilities=_matrixAbilities with {Jobs=_matrixAbilities.Jobs.Add(new(owner.RuntimeId,target.RuntimeId,world.Tick+1,op with{Target=MatrixTargetKind.EventTarget,DurationTicks=op.DurationTicks-1},origin,null,null))}; break;
                case MatrixOperationKind.ExtraShot:
                    world.LaunchProjectile(owner,target,amount,origin);break;
            }
        }
        if(op.ConsumeCounter&&op.Key.Length>0) MatrixWrite(owner,op.Key,MatrixCounter(owner,op.Key)*(1-op.CounterConsumeRatio));
        if(op.ConsumeCounter&&op.RequiredCounter.Length>0) MatrixWrite(owner,op.RequiredCounter,MatrixCounter(owner,op.RequiredCounter)*(1-op.CounterConsumeRatio));
    }

    private CompiledDisplacementOperation MatrixDisplacement(CompiledMatrixOperation op,BattleUnitState owner) => new(op.LandingSide?DisplacementKind.Leap:DisplacementKind.Charge,
        new CompiledExplicitTargetQuery(),
        op.Range,Math.Max(1,op.DurationTicks),.1f,false,false,op.Amount+MatrixCounter(owner,op.Key)*op.CounterRatio,op.AttackRatio,op.Radius,EffectDamageType.Normal,null,0,
        LandingAngleRadians:op.LandingSide?Mathf.Pi/2:0);

    public void MatrixDisplacementArrived(BattleUnitState source,BattleUnitState target,CombatSourceRef origin)
    {
        if(string.IsNullOrEmpty(origin.InstanceId)||!_matrixAbilities.Arrivals.TryGetValue(origin.InstanceId,out var pending))return;
        foreach(var child in pending.Operation.Effects)MatrixExecute(child,source,target.RuntimeId,origin,null,null,origin.StableId);
    }
    public void MatrixDisplacementFinished(CombatSourceRef origin)
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
        world.Damage(owner.RuntimeId,owner,target,amount,origin,EffectDamageType.Normal,
            active?CombatDamageClass.ActiveSkill:CombatDamageClass.Derived,origin.InstanceId);
        var damage=Math.Max(0,vitality-target.Health-target.Shield);
        if(active)world.Publish(new BattleCombatEventDraft(BattleCombatEventKind.SkillHitLanded,origin,owner.RuntimeId,target.RuntimeId,
            world.Tick,amount,damage,damage,SubjectStableId:origin.StableId,DamageClass:CombatDamageClass.ActiveSkill,ActionId:origin.InstanceId));
    }

    private void MatrixAttribute(BattleUnitState target,CompiledMatrixOperation op,float amount,CombatSourceRef origin,string slot)
    {
        var key=$"{origin.OwnerRuntimeId}:{slot}:{target.RuntimeId}:{op.Attribute}";
        foreach(var previous in _matrixAbilities.Buffs.Where(b=>b.Key==key))target.Attributes.Remove(previous.Handle);
        var handle=target.Attributes.ApplyModifier(new CompiledAttributeModifier(op.Attribute,AttributeModifierOperation.Add,
            new CompiledConstantMagnitude(amount),0,key),origin);
        _matrixAbilities=_matrixAbilities with {Buffs=_matrixAbilities.Buffs.Where(b=>b.Key!=key).ToImmutableArray().Add(new(target.RuntimeId,origin.OwnerRuntimeId,key,world.Tick+op.DurationTicks,handle,op.OwnerCondition,op.TargetCondition,op.HealthThreshold))};
    }

    public void AdvanceMatrixAbilities()
    {
        if(_matrixAbilities.Buffs.IsEmpty&&_matrixAbilities.Boosts.IsEmpty&&_matrixAbilities.Products.IsEmpty&&
            _matrixAbilities.Jobs.IsEmpty&&!reactions.HasPendingOrPeriodic)return;
        world.Resolve(() =>
        {
        MatrixPruneBuffs();
        _matrixAbilities=_matrixAbilities with{Boosts=_matrixAbilities.Boosts.Where(b=>b.Expires>world.Tick&&MatrixUnit(b.Owner)?.Alive==true).ToImmutableArray()};
        foreach(var product in _matrixAbilities.Products.ToArray())
        {
            var unit=MatrixUnit(product.Unit);
            if(unit?.Alive==true&&product.Expires>world.Tick)continue;
            _matrixAbilities=_matrixAbilities with{Products=_matrixAbilities.Products.Remove(product)};
            if(unit?.Alive==true)world.Retire(unit,product.Owner,"matrix_product_expired");
        }
        foreach(var job in _matrixAbilities.Jobs.Where(j=>j.ReadyTick<=world.Tick).ToArray())
        {
            _matrixAbilities=_matrixAbilities with{Jobs=_matrixAbilities.Jobs.Remove(job)};
            if(MatrixUnit(job.Owner) is not {Alive:true} owner)continue;
            if(job.Operation.Kind==MatrixOperationKind.Taunt&&job.Operation.DurationTicks<=0)continue;
            MatrixExecute(job.Operation,owner,job.Target,job.Origin,job.Event,job.Center,job.Origin.StableId);
        }
        });
        reactions.AdvancePeriodic();
        reactions.Drain();
    }

    public void MatrixPruneBuffs()
    {
        foreach(var buff in _matrixAbilities.Buffs.Where(b=>b.Expires<=world.Tick||MatrixUnit(b.Owner)?.Alive!=true||
            !MatrixConditionMatches(b.OwnerCondition,MatrixUnit(b.Source),b.HealthThreshold)||
            !MatrixConditionMatches(b.TargetCondition,MatrixUnit(b.Owner),b.HealthThreshold)).ToArray())
        {MatrixUnit(buff.Owner)?.Attributes.Remove(buff.Handle);_matrixAbilities=_matrixAbilities with{Buffs=_matrixAbilities.Buffs.Remove(buff)};}
    }

    // Register as a calculation subscriber. One cast token shares its lease across every arrow/target.
    public float MatrixAbilityCalculation(BattleCombatCalculationRequest request,float amount)
    {
        var source=MatrixUnit(request.SourceRuntimeId);if(source is null)return amount;
        var skill=request.DamageClass==CombatDamageClass.ActiveSkill || request.Kind!=BattleCombatCalculationKind.Damage&&
            source.Definition.AbilityLoadout?.Find(request.Source.StableId)?.IsActiveSkill==true;
        var basic=request.Kind==BattleCombatCalculationKind.Damage&&request.DamageClass==CombatDamageClass.BasicAttack;
        if(!skill&&!basic)return amount;
        var fallback=skill?request.Source.InstanceId:$"{request.Tick}:{statistics.AttackActions(source.RuntimeId)}";
        return amount*MatrixActionFactor(source,skill,request.ActionId.Length>0?request.ActionId:fallback);
    }

    private float MatrixActionFactor(BattleUnitState source,bool skill,string token)
    {
        var action=$"{source.RuntimeId}:{(skill?"skill":"attack")}:{token}";
        if(!_matrixAbilities.ActionFactors.TryGetValue(action,out var factor))
        {
            var leases=_matrixAbilities.Boosts.Where(b=>b.Owner==source.RuntimeId&&b.Skill==skill&&b.Expires>world.Tick).ToArray();
            factor=1+leases.Sum(b=>b.Amount);
            _matrixAbilities=_matrixAbilities with{Boosts=_matrixAbilities.Boosts.Except(leases).ToImmutableArray(),ActionFactors=_matrixAbilities.ActionFactors.SetItem(action,factor)};
        }
        return factor;
    }

    public void ClearMatrixAbilities()
    {
        foreach(var buff in _matrixAbilities.Buffs)MatrixUnit(buff.Owner)?.Attributes.Remove(buff.Handle);
        ClearState();
        reactions.Clear();
    }
}
