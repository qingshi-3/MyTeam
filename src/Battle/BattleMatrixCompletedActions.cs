using System;
using System.Linq;
using System.Collections.Immutable;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Battle;

internal sealed partial class BattleMatrixAbilityScope
{
    private sealed record MatrixChannel(string Owner,int Team,int NextTick,int Remaining,int LockUntil,
        CompiledMatrixOperation Operation,CombatSourceRef Origin);
    private sealed record MatrixHealingSeries(string Owner,int Started,int NextTick,int Completed,
        ImmutableArray<string> Recipients,ImmutableDictionary<string,float> EffectiveHealing,
        CompiledMatrixOperation Operation,CombatSourceRef Origin);
    private sealed record MatrixManaRefund(string Owner,string Action,int ReadyTick,float Amount);
    private sealed record MatrixDualShot(string Owner,string Target,CompiledMatrixOperation Operation,CombatSourceRef Origin);

    private void StartCompletedHeroAction(CompiledMatrixOperation operation,BattleUnitState owner,string targetId,CombatSourceRef origin)
    {
        if(!owner.Alive)return;
        if(operation.Kind==MatrixOperationKind.RandomEnemyChannel)
        {
            if(_matrixAbilities.Channels.Any(c=>c.Owner==owner.RuntimeId))return;
            var until=world.Tick+operation.DurationTicks;
            owner.ManaLockedUntilTick=Math.Max(owner.ManaLockedUntilTick,until);
            _matrixAbilities=_matrixAbilities with {Channels=_matrixAbilities.Channels.Add(new(owner.RuntimeId,owner.Team,
                world.Tick+operation.IntervalTicks,operation.Count,until,operation,origin))};
            world.ApplyMatrixMarker(owner.RuntimeId,owner.RuntimeId,"storm_channel","雷雨引导",operation.DurationTicks,operation.Count);
            world.Present("storm_channel_start",owner.RuntimeId,targetId,operation.Count,owner.Position,"skill_cast");
            return;
        }
        if(operation.Kind==MatrixOperationKind.HealingSeriesShield)
        {
            var wounded=world.Units.Where(u=>u.Alive&&u.Team==owner.Team&&u.RuntimeId!=owner.RuntimeId&&u.Health<u.MaxHealth&&
                u.Position.DistanceTo(owner.Position)<=operation.Range).OrderBy(u=>u.Health/u.MaxHealth)
                .ThenBy(u=>u.RuntimeId,StringComparer.Ordinal).FirstOrDefault();
            var recipients=wounded is null?ImmutableArray.Create(owner.RuntimeId):ImmutableArray.Create(owner.RuntimeId,wounded.RuntimeId);
            var series=new MatrixHealingSeries(owner.RuntimeId,world.Tick,world.Tick,0,recipients,
                ImmutableDictionary<string,float>.Empty,operation,origin);
            _matrixAbilities=_matrixAbilities with {HealingSeries=_matrixAbilities.HealingSeries.Add(series)};
            AdvanceHealingSeries(series);
            return;
        }
        var target=MatrixUnit(targetId);
        if(target is not {Alive:true}||target.Team==owner.Team)return;
        if(displacements.TryRetreatMaintainingReach(owner,target,operation.Amount,operation.Range,origin))
        {
            owner.ManaLockedUntilTick=Math.Max(owner.ManaLockedUntilTick,world.Tick+2);
            _matrixAbilities=_matrixAbilities with {DualShots=_matrixAbilities.DualShots.Add(new(owner.RuntimeId,target.RuntimeId,operation,origin))};
        }
        else FireDualShot(new(owner.RuntimeId,target.RuntimeId,operation,origin));
    }

    private void FireDualShot(MatrixDualShot cast)
    {
        var owner=MatrixUnit(cast.Owner);var target=MatrixUnit(cast.Target);
        if(owner is not {Alive:true}||target is not {Alive:true}||target.Team==owner.Team||
            !BattlefieldSpace.IsWithinReach(owner,target,cast.Operation.Range)||!world.HasLineAccess(owner,target))return;
        world.AdditionalAttack(owner,target,owner.Damage*cast.Operation.AttackRatio,$"derived:{cast.Origin.InstanceId}:dual-shot",
            new(cast.Origin,CombatDamageClass.ActiveSkill));
        // Both shots are aimed at the same locked victim. A dead first victim cannot
        // receive a second hit, and there is no fabricated bounce from that absence.
        if(!target.Alive)return;
        world.LaunchProjectile(owner,target,owner.Attributes.GetValue(CombatAttribute.SpellPower)*cast.Operation.SpellPowerRatio,
            cast.Origin,cast.Operation.Radius,cast.Operation.EventRatio);
        world.Present("skill_arrow",owner.RuntimeId,target.RuntimeId,cast.Operation.SpellPowerRatio,target.Position,"");
    }
    private void CompleteDualShotMovement(CombatSourceRef origin,bool completed)
    {
        foreach(var cast in _matrixAbilities.DualShots.Where(c=>c.Origin.InstanceId==origin.InstanceId).ToArray())
        {
            _matrixAbilities=_matrixAbilities with {DualShots=_matrixAbilities.DualShots.Remove(cast)};
            if(completed)FireDualShot(cast);
        }
    }
    public void ResolveDualShotBounce(BattleProjectileState projectile,BattleUnitState victim)
    {
        if(projectile.BounceSpellRatio<=0||projectile.BounceRange<=0||MatrixUnit(projectile.SourceId) is not {} owner)return;
        var next=world.Units.Where(u=>u.Alive&&u.Team!=projectile.Team&&u.RuntimeId!=victim.RuntimeId&&
            u.Position.DistanceTo(victim.Position)<=projectile.BounceRange+.0001f&&world.HasLineAccess(victim.Position,u.Position))
            .OrderBy(u=>u.Position.DistanceSquaredTo(victim.Position)).ThenBy(u=>u.RuntimeId,StringComparer.Ordinal).FirstOrDefault();
        if(next is null)return;
        world.LaunchProjectile(owner,next,owner.Attributes.GetValue(CombatAttribute.SpellPower)*projectile.BounceSpellRatio,
            projectile.SkillOrigin,launchPosition:victim.Position,excludedTargetId:victim.RuntimeId);
        world.Present("dual_shot_bounce",owner.RuntimeId,next.RuntimeId,projectile.BounceSpellRatio,victim.Position,"");
    }

    public bool HasInterruptibleAction(string id)=>HasWindup(id)||
        _matrixAbilities.Channels.Any(c=>c.Owner==id)||_matrixAbilities.DualShots.Any(c=>c.Owner==id);
    public void CancelCompletedHeroActions(string id)
    {
        foreach(var channel in _matrixAbilities.Channels.Where(c=>c.Owner==id).ToArray())EndChannel(channel,false);
        _matrixAbilities=_matrixAbilities with {DualShots=_matrixAbilities.DualShots.Where(c=>c.Owner!=id).ToImmutableArray(),
            Jobs=_matrixAbilities.Jobs.Where(j=>j.Owner!=id||!j.Interruptible||j.Operation.Kind!=MatrixOperationKind.RandomEnemyChannel).ToImmutableArray()};
        // Released U11 healing keeps its original non-interruptible semantics.
    }
    private void EndChannel(MatrixChannel channel,bool completed)
    {
        _matrixAbilities=_matrixAbilities with {Channels=_matrixAbilities.Channels.Remove(channel)};
        if(MatrixUnit(channel.Owner) is {} owner)
        {
            world.ClearMatrixMarker(owner.RuntimeId,owner.RuntimeId,"storm_channel");
            if(owner.ManaLockedUntilTick==channel.LockUntil)owner.ManaLockedUntilTick=world.Tick;
            if(completed&&owner.Alive&&channel.Operation.Amount>0)support.GrantMana(owner.RuntimeId,owner.RuntimeId,channel.Operation.Amount);
            world.Present(completed?"storm_channel_complete":"storm_channel_cancel",owner.RuntimeId,owner.RuntimeId,0,owner.Position,"");
        }
    }
    private void AdvanceHealingSeries(MatrixHealingSeries series)
    {
        var owner=MatrixUnit(series.Owner);
        if(owner is not {Alive:true})
        {_matrixAbilities=_matrixAbilities with {HealingSeries=_matrixAbilities.HealingSeries.Remove(series)};return;}
        if(series.Completed<series.Operation.Count&&world.Tick>=series.NextTick)
        {
            var healed=series.EffectiveHealing;
            foreach(var id in series.Recipients)
                if(MatrixUnit(id) is {Alive:true} target)
                {
                    var amount=world.Heal(owner.RuntimeId,target,owner.Attributes.GetValue(CombatAttribute.SpellPower)*series.Operation.SpellPowerRatio,series.Origin);
                    healed=healed.SetItem(id,healed.GetValueOrDefault(id)+amount);
                    if(amount>0)world.Present("heal",owner.RuntimeId,id,amount,target.Position,"heal");
                }
            var updated=series with {Completed=series.Completed+1,NextTick=series.NextTick+series.Operation.IntervalTicks,EffectiveHealing=healed};
            _matrixAbilities=_matrixAbilities with {HealingSeries=_matrixAbilities.HealingSeries.Replace(series,updated)};
            series=updated;
        }
        if(!owner.Alive){_matrixAbilities=_matrixAbilities with {HealingSeries=_matrixAbilities.HealingSeries.Remove(series)};return;}
        if(world.Tick<series.Started+series.Operation.DurationTicks)return;
        _matrixAbilities=_matrixAbilities with {HealingSeries=_matrixAbilities.HealingSeries.Remove(series)};
        if(series.Completed!=series.Operation.Count)return;
        foreach(var id in series.Recipients)
            if(MatrixUnit(id) is {Alive:true} target&&series.EffectiveHealing.GetValueOrDefault(id)>0)
                support.ApplyShield(owner.RuntimeId,id,series.EffectiveHealing[id]*series.Operation.Amount,40,series.Origin);
    }
    private void AdvanceCompletedHeroActions()
    {
        foreach(var channel in _matrixAbilities.Channels.ToArray())
        {
            var owner=MatrixUnit(channel.Owner);
            if(owner is not {Alive:true}||owner.Team!=channel.Team){EndChannel(channel,false);continue;}
            if(world.Tick<channel.NextTick)continue;
            var candidates=world.Units.Where(u=>u.Alive&&u.Team!=channel.Team&&
                owner.Position.DistanceTo(u.Position)<=channel.Operation.Range&&world.HasLineAccess(owner,u))
                .OrderBy(u=>u.RuntimeId,StringComparer.Ordinal).ToArray();
            if(candidates.Length==0){EndChannel(channel,false);continue;}
            var target=candidates[world.NextIndex(candidates.Length)];
            MatrixHit(owner,target,owner.Attributes.GetValue(CombatAttribute.SpellPower)*channel.Operation.SpellPowerRatio,channel.Origin);
            world.Present("storm_channel_hit",owner.RuntimeId,target.RuntimeId,channel.Remaining,target.Position,"hit");
            if(!_matrixAbilities.Channels.Contains(channel))continue;
            if(!owner.Alive){EndChannel(channel,false);continue;}
            if(channel.Remaining==1)EndChannel(channel,true);
            else _matrixAbilities=_matrixAbilities with {Channels=_matrixAbilities.Channels.Replace(channel,
                channel with {NextTick=channel.NextTick+channel.Operation.IntervalTicks,Remaining=channel.Remaining-1})};
        }
        foreach(var series in _matrixAbilities.HealingSeries.ToArray())AdvanceHealingSeries(series);
        foreach(var refund in _matrixAbilities.ManaRefunds.ToArray())
        {
            var owner=MatrixUnit(refund.Owner);
            if(owner is not {Alive:true}){_matrixAbilities=_matrixAbilities with {ManaRefunds=_matrixAbilities.ManaRefunds.Remove(refund)};continue;}
            if(world.Tick<refund.ReadyTick||world.Tick<owner.ManaLockedUntilTick)continue;
            _matrixAbilities=_matrixAbilities with {ManaRefunds=_matrixAbilities.ManaRefunds.Remove(refund)};
            support.GrantMana(owner.RuntimeId,owner.RuntimeId,refund.Amount);
            world.Present("kill_mana_refund",owner.RuntimeId,owner.RuntimeId,refund.Amount,owner.Position,"");
        }
    }
}
