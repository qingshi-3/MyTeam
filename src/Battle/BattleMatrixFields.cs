using System;
using System.Linq;
using System.Collections.Immutable;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Battle;

public sealed record BattleFrontalGuardSnapshot(string OwnerRuntimeId,Vector2 Direction,int ExpiresTick,float HalfAngleRadians,float Reduction);
public sealed record BattleGroundStormSnapshot(string StableId,string OwnerRuntimeId,int Team,Vector2 Center,float Radius,int StartedTick,int ExpiresTick);

internal sealed partial class BattleMatrixAbilityScope
{
    private sealed record MatrixGuard(string Owner,Vector2 Direction,int Expires,float HalfAngle,float Reduction);
    private sealed record MatrixStorm(string Id,string Owner,int Team,Vector2 Center,float Radius,int Started,int Expires,
        int NextPulse,int Interval,float SpellRatio,float Slow,CombatSourceRef Origin);
    private sealed record MatrixStormSlow(string Target,float Reduction,int Expires,AttributeModifierHandle Handle,string Source,bool WasInside = true);
    public ImmutableArray<BattleFrontalGuardSnapshot> FrontalGuards=>_matrixAbilities.Guards.Where(g=>g.Expires>world.Tick&&MatrixUnit(g.Owner)?.Alive==true)
        .Select(g=>new BattleFrontalGuardSnapshot(g.Owner,g.Direction,g.Expires,g.HalfAngle,g.Reduction)).ToImmutableArray();
    public ImmutableArray<BattleGroundStormSnapshot> GroundStorms=>_matrixAbilities.Storms
        .Select(s=>new BattleGroundStormSnapshot(s.Id,s.Owner,s.Team,s.Center,s.Radius,s.Started,s.Expires)).ToImmutableArray();
    public bool HoldsAction(string id)=>MatrixUnit(id)?.Alive==true&&
        (_matrixAbilities.Guards.Any(g=>g.Owner==id&&g.Expires>world.Tick)||_matrixAbilities.Channels.Any(c=>c.Owner==id)||
         _matrixAbilities.Jobs.Any(j=>j.Owner==id&&j.Interruptible&&j.Operation.Kind==MatrixOperationKind.RandomEnemyChannel));
    private void PruneGuards()=>_matrixAbilities=_matrixAbilities with {Guards=_matrixAbilities.Guards.Where(g=>g.Expires>world.Tick&&MatrixUnit(g.Owner)?.Alive==true).ToImmutableArray()};
    private float ApplyFrontalGuard(BattleCombatCalculationRequest request,float amount)
    {
        if(request.Kind!=BattleCombatCalculationKind.Damage||request.DamageClass!=CombatDamageClass.BasicAttack)return amount;
        var guard=_matrixAbilities.Guards.FirstOrDefault(g=>g.Owner==request.TargetRuntimeId&&g.Expires>world.Tick);
        var target=MatrixUnit(request.TargetRuntimeId);
        if(guard is null||target?.Alive!=true)return amount;
        var bearing=request.AttackIncomingDirection is {} point?new Vector2(point.X,point.Y):
            (MatrixUnit(request.SourceRuntimeId)?.Position??target.Position)-target.Position;
        if(bearing.IsZeroApprox())return amount;
        return guard.Direction.Dot(bearing.Normalized())+ .0001f>=Mathf.Cos(guard.HalfAngle)?amount*(1-guard.Reduction):amount;
    }
    private void CreateStorm(CompiledMatrixOperation op,BattleUnitState owner,string targetId,CombatSourceRef origin,Vector2? center)
    {
        if(_matrixAbilities.Storms.Length>=128)throw new InvalidOperationException("Ground storm budget exceeded.");
        var anchor=center??MatrixUnit(targetId)?.Position??owner.Position;
        var sequence=_matrixAbilities.ZoneSequence+1;
        _matrixAbilities=_matrixAbilities with {ZoneSequence=sequence,Storms=_matrixAbilities.Storms.Add(new(
            "ground-storm:"+sequence,owner.RuntimeId,owner.Team,anchor,op.Radius,world.Tick,world.Tick+op.DurationTicks,
            world.Tick+op.IntervalTicks,op.IntervalTicks,op.SpellPowerRatio,op.Amount,origin))};
        world.Present("ground_storm_start",owner.RuntimeId,targetId,op.Radius,anchor,"");
    }
    private void AdvanceStorms()
    {
        // Fixed tick, bounded active fields and one modifier per target. Overlapping
        // fields select the strongest slow rather than stacking separate modifiers.
        foreach(var storm in _matrixAbilities.Storms.ToArray())
        {
            if(world.Tick>=storm.NextPulse&&world.Tick<=storm.Expires&&MatrixUnit(storm.Owner) is {} owner)
            {
                foreach(var target in world.Units.Where(u=>u.Alive&&u.Team!=storm.Team&&
                    u.Position.DistanceSquaredTo(storm.Center)<=storm.Radius*storm.Radius+.0001f).OrderBy(u=>u.RuntimeId,StringComparer.Ordinal).ToArray())
                    MatrixHit(owner,target,owner.Attributes.GetValue(CombatAttribute.SpellPower)*storm.SpellRatio,storm.Origin,isAreaDamage:true);
                _matrixAbilities=_matrixAbilities with {Storms=_matrixAbilities.Storms.Replace(storm,storm with {NextPulse=storm.NextPulse+storm.Interval})};
            }
        }
        foreach(var target in world.Units.Where(u=>u.Alive).OrderBy(u=>u.RuntimeId,StringComparer.Ordinal))
        {
            var inside=_matrixAbilities.Storms.Where(s=>s.Expires>world.Tick&&target.Team!=s.Team&&
                target.Position.DistanceSquaredTo(s.Center)<=s.Radius*s.Radius+.0001f).OrderByDescending(s=>s.Slow).ThenBy(s=>s.Id,StringComparer.Ordinal).FirstOrDefault();
            if(inside is null)
            {
                var leaving=_matrixAbilities.StormSlows.FirstOrDefault(s=>s.Target==target.RuntimeId&&s.WasInside);
                if(leaving is not null)
                {
                    _matrixAbilities=_matrixAbilities with {StormSlows=_matrixAbilities.StormSlows.Replace(leaving,leaving with {Expires=world.Tick+5,WasInside=false})};
                    world.ApplyMatrixMarker(leaving.Source,target.RuntimeId,"ground_storm_slow","霜暴减速",5,leaving.Reduction,harmful:true);
                }
                continue;
            }
            var prior=_matrixAbilities.StormSlows.FirstOrDefault(s=>s.Target==target.RuntimeId);
            var reduction=Math.Max(inside.Slow,prior is not null&&prior.Expires>world.Tick?prior.Reduction:0);
            var expires=inside.Slow>=reduction?world.Tick+5:prior!.Expires;
            var handle=prior?.Handle??default;
            if(prior is null||Math.Abs(prior.Reduction-reduction)>.0001f)
            {
                if(prior is not null)target.Attributes.Remove(prior.Handle);
                handle=target.Attributes.ApplyModifier(new(CombatAttribute.AttackSpeed,AttributeModifierOperation.Multiply,
                    new CompiledConstantMagnitude(1-reduction),0,"ground_storm_slow"),inside.Origin);
            }
            _matrixAbilities=_matrixAbilities with {StormSlows=_matrixAbilities.StormSlows.Where(s=>s.Target!=target.RuntimeId).ToImmutableArray()
                .Add(new(target.RuntimeId,reduction,expires,handle,inside.Owner))};
            world.ApplyMatrixMarker(inside.Owner,target.RuntimeId,"ground_storm_slow","霜暴减速",Math.Max(1,expires-world.Tick),reduction,harmful:true);
        }
        foreach(var slow in _matrixAbilities.StormSlows.Where(s=>s.Expires<=world.Tick||MatrixUnit(s.Target)?.Alive!=true).ToArray())
        {
            MatrixUnit(slow.Target)?.Attributes.Remove(slow.Handle);
            _matrixAbilities=_matrixAbilities with {StormSlows=_matrixAbilities.StormSlows.Remove(slow)};
        }
        foreach(var expired in _matrixAbilities.Storms.Where(s=>s.Expires<=world.Tick))
            world.Present("ground_storm_end",expired.Owner,"",expired.Radius,expired.Center,"");
        _matrixAbilities=_matrixAbilities with {Storms=_matrixAbilities.Storms.Where(s=>s.Expires>world.Tick).ToImmutableArray()};
    }
}
