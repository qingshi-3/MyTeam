using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Vfx;

namespace TowerAutobattler.Presentation;

public partial class RangedAttackLayer
{
    private readonly Dictionary<string,float> _enemyVfxExpiry = [];
    private readonly Dictionary<int,ProjectileFlight> _returnBlades = [];
    private void PresentEnemyAction(BattleEvent fact, bool snap)
    {
        if (fact.EnemyAction is { } action && fact.Type != "blade_release")
        {
            var key=$"enemy:{fact.SourceRuntimeId}:{action.StartTick}:{action.Vfx}";
            if (fact.Type == "wall_raised") key += ":"+fact.TargetRuntimeId;
            var context=new VfxContext(fact.Origin,fact.Position,action.Radius,TravelProgress:action.Stage=="release"?1:0,ConeAngleDegrees:action.AngleDegrees);
            if (action.Stage=="end") { _player.End(key,VfxEndReason.ScopeEnded);_enemyVfxExpiry.Remove(key); }
            else if (action.Stage=="update") _player.UpdateContext(key,context);
            else
            {
                // End the preparation instance before releasing its independently timed body.
                if (action.Stage=="release")
                {
                    _player.End(key,VfxEndReason.ScopeEnded); _enemyVfxExpiry.Remove(key);
                    key+=":released";
                }
                _player.Play(action.Vfx,context,key);
                _enemyVfxExpiry[key]=action.DurationTicks*BattleTiming.TickSeconds;
            }
            if(action.Stage=="end")
            {
                _player.End(key+":released",VfxEndReason.ScopeEnded);
                _enemyVfxExpiry.Remove(key+":released");
            }
        }
        var bladeKey="blade:"+fact.EntityId;
        switch(fact.Type)
        {
            case "blade_release":
                _returnBlades[fact.EntityId]=new(fact.Origin,(fact.Position-fact.Origin).Normalized(),true);
                _player.Play(fact.EnemyAction!.Vfx,new(fact.Origin,fact.Origin),bladeKey);
                break;
            case "blade_move":
            case "blade_end":
                if(_returnBlades.TryGetValue(fact.EntityId,out var flight))
                {
                    flight.From=flight.Rendered;flight.To=fact.Position;flight.Elapsed=snap?BattleTiming.TickSeconds:0;
                    flight.Ending=fact.Type=="blade_end";
                }
                break;
        }
    }
    private void AdvanceEnemyVisuals(float seconds)
    {
        foreach(var (key,time) in _enemyVfxExpiry.ToArray())
        {
            var remaining=time-seconds;
            if(remaining>0) _enemyVfxExpiry[key]=remaining;
            else { _player.End(key,VfxEndReason.Completed);_enemyVfxExpiry.Remove(key); }
        }
        foreach(var (id,flight) in _returnBlades.ToArray())
        {
            if(flight.Ending && flight.Elapsed>=BattleTiming.TickSeconds && seconds>0)
            { _player.End("blade:"+id,VfxEndReason.Completed);_returnBlades.Remove(id);continue; }
            flight.Elapsed=Math.Min(BattleTiming.TickSeconds,flight.Elapsed+seconds);
            flight.Rendered=flight.From.Lerp(flight.To,flight.Elapsed/BattleTiming.TickSeconds);
            _player.UpdateContext("blade:"+id,new(flight.From,flight.Rendered));
        }
    }
    private void ClearEnemyVisuals() { _enemyVfxExpiry.Clear();_returnBlades.Clear(); }
}
