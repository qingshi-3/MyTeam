using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

public sealed record BattleEnemyActionCue(string Vfx, string Stage, int StartTick, int DurationTicks,
    float Radius = 0, float AngleDegrees = 0);

public sealed partial class BattleSimulation
{
    // All records are immutable so an ability/world rollback restores one complete state root.
    private sealed record EnemyCast(string OwnerId, string TargetId, int Team, CompiledEnemyAction Operation,
        CombatSourceRef Origin, Vector2 Start, Vector2 End, int StartTick, int Step = 0);
    private sealed record BladeFlight(int Id, string OwnerId, int Team, CompiledReturningBlade Operation,
        CombatSourceRef Origin, Vector2 Start, Vector2 End, Vector2 Position, float Damage,
        bool Returning, ImmutableArray<string> HitIds);
    private sealed record BroodState(int BreakStart, bool Transformed, int NextSpawn, int Batches);
    private sealed record EnemyProduct(string UnitId, string OwnerId, int ExpireTick, string HatchContentId = "");
    private sealed record EnemyActionState(ImmutableDictionary<string, EnemyCast> Casts,
        ImmutableDictionary<int, BladeFlight> Blades, ImmutableDictionary<string, BroodState> Broods,
        ImmutableArray<EnemyProduct> Products, int Sequence = 0);
    private EnemyActionState _enemyActions = new(ImmutableDictionary<string, EnemyCast>.Empty,
        ImmutableDictionary<int, BladeFlight>.Empty, ImmutableDictionary<string, BroodState>.Empty, []);

    private bool EnemyActionBusy(BattleUnitState owner) => _enemyActions.Casts.ContainsKey(owner.RuntimeId) ||
        _enemyActions.Blades.Values.Any(blade => blade.OwnerId == owner.RuntimeId) ||
        (_enemyActions.Broods.TryGetValue(owner.RuntimeId, out var brood) && !brood.Transformed);
    private bool EnemyControlled(BattleUnitState owner) => owner.DisabledTicks > 0 ||
        _statusScope.HasTag(owner.RuntimeId, StatusDefinitionCompiler.ActionDisabledTag);
    private bool IsRampartBody(BattleUnitState unit) => _enemyActions.Products.Any(p => p.UnitId == unit.RuntimeId && p.HatchContentId == "");

    private string SelectEnemyActionTarget(BattleUnitState owner, CompiledEnemyAction operation)
    {
        var range = operation switch { CompiledConeBreath x => x.Range, CompiledReturningBlade x => x.Range,
            CompiledPositionSwap x => x.Range, CompiledRampart x => x.Range, _ => owner.BodyRadius + 2 };
        var targets = _units.Where(u => u.Alive && u.Team != owner.Team && !IsRampartBody(u) &&
            owner.Position.DistanceTo(u.Position) <= range + (operation is CompiledConeBreath ? u.BodyRadius : 0) &&
            (operation is CompiledPositionSwap ? CanSwapBodies(owner, u) : HasLineAccess(owner.Position,u.Position)));
        return (operation is CompiledPositionSwap ? targets.OrderByDescending(u => owner.Position.DistanceSquaredTo(u.Position)) :
            targets.OrderBy(u => owner.Position.DistanceSquaredTo(u.Position)))
            .ThenBy(u => u.RuntimeId,StringComparer.Ordinal).Select(u => u.RuntimeId).FirstOrDefault() ?? "";
    }

    private bool PrepareEnemyAction(BattleUnitState owner, CompiledEnemyAction operation, string targetId)
    {
        if (EnemyActionBusy(owner) || IsDisplacing(owner) || owner.ProjectileSequence is not null || !owner.ProjectileWindups.IsEmpty) return false;
        if (operation is CompiledBroodPhase && _enemyActions.Broods.ContainsKey(owner.RuntimeId)) return false;
        if (operation.ContentDependencies.Any(id => !_config.TacticalSummons.ContainsKey(id))) return false;
        return targetId.Length > 0 && targetId == SelectEnemyActionTarget(owner,operation);
    }

    private void BeginEnemyAction(BattleUnitState owner, BattleUnitState target, CompiledEnemyAction operation, CompiledAbilityDefinition ability)
    {
        var direction = (target.Position-owner.Position).Normalized();
        if (direction.IsZeroApprox()) direction = owner.Team == 0 ? Vector2.Right : Vector2.Left;
        var range = operation switch { CompiledConeBreath x => x.Range, CompiledReturningBlade x => x.Range,
            CompiledRampart x => x.Range, _ => owner.Position.DistanceTo(target.Position) };
        var cast = new EnemyCast(owner.RuntimeId,target.RuntimeId,owner.Team,operation,AbilityOrigin(ability,owner.RuntimeId),
            owner.Position,owner.Position+direction*range,TickIndex);
        if (operation is CompiledRampart rampart)
            cast = cast with { End = cast.Start+direction*range*BattlefieldSpace.FirstTerrainHitFraction(
                cast.Start,direction*range,rampart.FissureRadius,Width,Height,_config.FloorRule.CanOccupy) };
        _enemyActions = _enemyActions with { Casts = _enemyActions.Casts.SetItem(owner.RuntimeId,cast) };
        owner.LastAbilityName = ability.DisplayName;
        owner.LastActionKind = BattleActionKind.Ability;
        owner.Mode = BattleUnitMode.Casting;
        SetActionTarget(owner,target);
        _movement?.ReleaseUnit(owner.RuntimeId);
        switch (operation)
        {
            case CompiledConeBreath x: EmitEnemyAction(cast,x.Vfx,"prepare",x.WindupTicks,x.Range,x.AngleDegrees); break;
            case CompiledReturningBlade:
                // Authored hand-release timing, with no extra charge stage or warning line.
                var seconds = Math.Max(BattleTiming.TickSeconds,owner.Definition.ProjectileWindupSeconds);
                Emit("blade_throw",owner.RuntimeId,target.RuntimeId,0,target.Position,"attack",attackTiming:
                    new BattleAttackTiming(seconds / owner.Definition.AttackReleaseProgress,owner.Definition.AttackReleaseProgress));
                break;
            case CompiledPositionSwap x:
                cast = cast with { End = target.Position };
                _enemyActions = _enemyActions with { Casts = _enemyActions.Casts.SetItem(owner.RuntimeId,cast) };
                EmitEnemyAction(cast,x.Vfx,"prepare",x.PrepareTicks); break;
            case CompiledRampart x: EmitEnemyAction(cast,"rock_raise","prepare",x.RaiseTicks); break;
            case CompiledBroodPhase x:
                HitCone(owner,cast,owner.BodyRadius+1.2f,110,x.SwipeMultiplier,EffectDamageType.Normal);
                EmitEnemyAction(cast,"claw_swipe","release",4,owner.BodyRadius+1.2f,110);
                break;
        }
    }

    private void EmitEnemyAction(EnemyCast cast, string vfx, string stage, int ticks, float radius = 0, float angle = 0) =>
        Emit("enemy_action",cast.OwnerId,cast.TargetId,0,cast.End,stage is "prepare" or "release" ? "skill_cast" : "",origin:cast.Start,
            enemyAction:new(vfx,stage,cast.StartTick,ticks,radius,angle));

    private void AdvanceEnemyActions()
    {
        if (_enemyActions.Casts.IsEmpty && _enemyActions.Products.IsEmpty &&
            !_units.Any(unit => unit.Alive && _abilityScope?.FindOperation<CompiledBroodPhase>(unit.RuntimeId) is not null)) return;
        var checkpoint = new BattleWorldStateCheckpoint(this);
        try
        {
            using var resolution = _combatPipeline.BeginAuthoritativeResolution();
            AdvanceBroodPhases();
            AdvanceEnemyProducts();
            foreach (var cast in _enemyActions.Casts.Values.OrderBy(c => c.OwnerId,StringComparer.Ordinal).ToArray())
            {
                if (!_enemyActions.Casts.ContainsKey(cast.OwnerId)) continue;
                var owner = _units.First(u => u.RuntimeId == cast.OwnerId);
                if (!owner.Alive || owner.Team != cast.Team || EnemyControlled(owner) || IsDisplacing(owner) ||
                    !owner.Position.IsEqualApprox(cast.Start)) { CancelEnemyAction(owner.RuntimeId); continue; }
                owner.Mode = BattleUnitMode.Casting;
                var elapsed = TickIndex-cast.StartTick;
                switch (cast.Operation)
                {
                    case CompiledConeBreath x:
                        if (elapsed >= x.WindupTicks && cast.Step < x.PulseCount &&
                            elapsed >= x.WindupTicks + cast.Step*x.PulseIntervalTicks)
                        {
                            if (cast.Step == 0) EmitEnemyAction(cast,x.Vfx,"release",x.PulseCount*x.PulseIntervalTicks,x.Range,x.AngleDegrees);
                            HitCone(owner,cast,x.Range,x.AngleDegrees,x.AttackMultiplier,EffectDamageType.Normal);
                            SetEnemyCast(cast with { Step = cast.Step+1 });
                        }
                        if (elapsed >= x.WindupTicks+x.PulseCount*x.PulseIntervalTicks+x.RecoveryTicks) CancelEnemyAction(owner.RuntimeId);
                        break;
                    case CompiledReturningBlade x:
                        if (cast.Step == 0 && elapsed >= Math.Max(1,Mathf.CeilToInt(owner.Definition.ProjectileWindupSeconds/BattleTiming.TickSeconds)))
                        {
                            var delta = cast.End-cast.Start;
                            var end = cast.Start+delta*BattlefieldSpace.FirstTerrainHitFraction(cast.Start,delta,x.Radius,Width,Height,_config.FloorRule.CanOccupy);
                            var id = _enemyActions.Sequence+1;
                            var blade = new BladeFlight(id,owner.RuntimeId,owner.Team,x,cast.Origin,cast.Start,end,cast.Start,
                                owner.Damage*x.AttackMultiplier,false,[]);
                            _enemyActions = _enemyActions with { Sequence = id, Blades = _enemyActions.Blades.Add(id,blade) };
                            SetEnemyCast(cast with { Step = id });
                            Emit("blade_release",owner.RuntimeId,cast.TargetId,0,cast.End,"",entityId:id,origin:cast.Start,
                                enemyAction:new(x.Vfx,"flight",TickIndex,1,x.Radius));
                        }
                        else if (cast.Step > 0 && !_enemyActions.Blades.ContainsKey(cast.Step)) CancelEnemyAction(owner.RuntimeId);
                        break;
                    case CompiledPositionSwap x:
                        var target = _units.FirstOrDefault(u => u.RuntimeId == cast.TargetId);
                        if (cast.Step == 0 && (target is null || !target.Alive || target.Team == owner.Team || IsDisplacing(target)))
                        { CancelEnemyAction(owner.RuntimeId); break; }
                        if (cast.Step == 0 && elapsed >= x.PrepareTicks)
                        {
                            if (target is null || !CanSwapBodies(owner,target)) { CancelEnemyAction(owner.RuntimeId); break; }
                            SwapBodies(owner,target);
                            SetEnemyCast(cast with { Start = owner.Position, End = target.Position, Step = 1 });
                            EmitEnemyAction(cast,x.Vfx,"release",3);
                        }
                        else if (cast.Step == 0 && target is not null)
                            EmitEnemyAction(cast with { End = target.Position },x.Vfx,"update",x.PrepareTicks);
                        if (elapsed >= x.PrepareTicks+x.RecoveryTicks) CancelEnemyAction(owner.RuntimeId);
                        break;
                    case CompiledRampart x:
                        if (cast.Step == 0 && elapsed >= x.RaiseTicks) { RaiseRampart(owner,cast,x); SetEnemyCast(cast with { Step = 1 }); }
                        if (cast.Step == 1 && elapsed >= x.RaiseTicks+x.IntervalTicks)
                        { EmitEnemyAction(cast,x.Vfx,"prepare",x.FissureWindupTicks,x.FissureRadius); SetEnemyCast(cast with { Step = 2 }); }
                        if (cast.Step == 2 && elapsed >= x.RaiseTicks+x.IntervalTicks+x.FissureWindupTicks)
                        {
                            foreach (var hit in _units.Where(u => u.Alive && u.Team != owner.Team &&
                                LineHitTime(cast.Start,cast.End-cast.Start,x.FissureRadius,u) is not null).OrderBy(u => u.RuntimeId,StringComparer.Ordinal).ToArray())
                                ResolveLineSkillHit(owner,hit,owner.Damage*x.AttackMultiplier,cast.Origin,EffectDamageType.Normal);
                            EmitEnemyAction(cast,x.Vfx,"release",5,x.FissureRadius);
                            RemoveEnemyProducts(owner.RuntimeId, wallsOnly:true);
                            SetEnemyCast(cast with { Step = 3 });
                        }
                        if (elapsed >= x.RaiseTicks+x.IntervalTicks+x.FissureWindupTicks+x.RecoveryTicks) CancelEnemyAction(owner.RuntimeId);
                        break;
                    case CompiledBroodPhase:
                        if (elapsed >= 5) CancelEnemyAction(owner.RuntimeId);
                        break;
                }
            }
            resolution.Commit();
            checkpoint.Commit();
        }
        catch { checkpoint.Rollback(); throw; }
    }

    private void SetEnemyCast(EnemyCast cast)
    {
        // Reactions may have killed or interrupted the caster while resolving this stage.
        if (_enemyActions.Casts.ContainsKey(cast.OwnerId))
            _enemyActions = _enemyActions with { Casts = _enemyActions.Casts.SetItem(cast.OwnerId,cast) };
    }
    private void CancelEnemyAction(string ownerId)
    {
        if (!_enemyActions.Casts.TryGetValue(ownerId,out var cast)) return;
        _enemyActions = _enemyActions with { Casts = _enemyActions.Casts.Remove(ownerId) };
        var vfx = cast.Operation switch { CompiledConeBreath x => x.Vfx, CompiledPositionSwap x => x.Vfx,
            CompiledRampart x => x.Vfx, _ => "" };
        if (vfx != "") EmitEnemyAction(cast,vfx,"end",1);
        if (cast.Operation is CompiledRampart) RemoveEnemyProducts(ownerId,wallsOnly:true);
        // Already released blades deliberately survive control, displacement and caster death.
        Emit("attack_cancel",ownerId,ownerId,0,cast.Start,"");
    }

    private void HitCone(BattleUnitState owner, EnemyCast cast, float range, float angle, float multiplier, EffectDamageType damageType)
    {
        var heading = (cast.End-cast.Start).Normalized();
        foreach (var target in _units.Where(u => u.Alive && u.Team != cast.Team).OrderBy(u => u.RuntimeId,StringComparer.Ordinal).ToArray())
        {
            var offset = target.Position-cast.Start;
            var distance = offset.Length();
            var extraAngle = Mathf.Asin(Mathf.Clamp(target.BodyRadius/Math.Max(.001f,distance),0,1));
            if (distance-target.BodyRadius > range ||
                (distance > target.BodyRadius && Math.Abs(heading.AngleTo(offset)) > Mathf.DegToRad(angle/2)+extraAngle) ||
                !HasLineAccess(cast.Start,target.Position)) continue;
            ResolveLineSkillHit(owner,target,owner.Damage*multiplier,cast.Origin,damageType);
            Emit("line_impact",owner.RuntimeId,target.RuntimeId,0,target.Position,"");
        }
    }

    private bool CanSwapBodies(BattleUnitState first, BattleUnitState second)
    {
        if (!first.Alive || !second.Alive || first == second || first.Team == second.Team ||
            IsDisplacing(first) || IsDisplacing(second) || IsRampartBody(second)) return false;
        bool Fits(BattleUnitState body, Vector2 at) =>
            BattlefieldSpace.IsPositionTerrainClear(at,body.BodyRadius,Width,Height,_config.FloorRule.CanOccupy) &&
            _units.All(u => !u.Alive || u == first || u == second || IsAirborne(u) ||
                u.Position.DistanceTo(at) >= u.BodyRadius+body.BodyRadius+BattlefieldSpace.BodyClearance) &&
            CaptureDisplacementReservations().All(r => r.RuntimeId == first.RuntimeId || r.RuntimeId == second.RuntimeId ||
                r.Position.DistanceTo(at) >= r.Radius+body.BodyRadius+BattlefieldSpace.BodyClearance);
        return first.Position.DistanceTo(second.Position) >= first.BodyRadius+second.BodyRadius+BattlefieldSpace.BodyClearance &&
            Fits(first,second.Position) && Fits(second,first.Position);
    }

    private void SwapBodies(BattleUnitState first, BattleUnitState second)
    {
        var a = first.Position; var b = second.Position;
        CancelEnemyAction(second.RuntimeId); CancelChargedLine(second); CancelTrample(second);
        CancelProjectileWindups(second); second.ProjectileSequence = null;
        InterruptBattleChannels(second.RuntimeId);
        _movement?.ReleaseUnit(first.RuntimeId); _movement?.ReleaseUnit(second.RuntimeId);
        first.Position = b; second.Position = a;
        ClearActionTarget(first); ClearActionTarget(second);
        foreach (var (unit,start,end) in new[] { (first,a,b), (second,b,a) })
            Emit("displacement",first.RuntimeId,unit.RuntimeId,0,end,"",origin:start,
                displacement:new(DisplacementKind.Blink,start,end,TickIndex,1,1,true,false,0,start,0));
        Emit("swap_complete",first.RuntimeId,second.RuntimeId,0,b,"",origin:a);
    }

    private void AdvanceReturningBlades(System.Collections.Generic.IReadOnlyDictionary<string,Vector2> previousPositions)
    {
        if (_enemyActions.Blades.IsEmpty) return;
        var checkpoint = new BattleWorldStateCheckpoint(this);
        try { AdvanceReturningBladesCore(previousPositions); checkpoint.Commit(); }
        catch { checkpoint.Rollback(); throw; }
    }

    private void AdvanceReturningBladesCore(System.Collections.Generic.IReadOnlyDictionary<string,Vector2> previousPositions)
    {
        foreach (var original in _enemyActions.Blades.Values.OrderBy(b => b.Id).ToArray())
        {
            var blade = original;
            var destination = blade.Returning ? blade.Start : blade.End;
            var delta = (destination-blade.Position).LimitLength(blade.Operation.Speed*BattleTiming.TickSeconds);
            var source = _units.First(u => u.RuntimeId == blade.OwnerId);
            var stopped = false;
            var hits = _units.Where(u => u.Alive && (u.Team != blade.Team || IsRampartBody(u)) && !blade.HitIds.Contains(u.RuntimeId))
                .Select(u => (Unit:u, Time:BladeHitTime(blade,delta,u,previousPositions)))
                .Where(h => h.Time is not null).OrderBy(h => h.Time).ThenBy(h => h.Unit.RuntimeId,StringComparer.Ordinal).ToArray();
            foreach (var hit in hits)
            {
                if (!hit.Unit.Alive) continue;
                var wall = IsRampartBody(hit.Unit);
                using var resolution = _combatPipeline.BeginAuthoritativeResolution();
                if (hit.Unit.Team != blade.Team) ResolveLineSkillHit(source,hit.Unit,blade.Damage,blade.Origin,EffectDamageType.Normal);
                blade = blade with { HitIds = blade.HitIds.Add(hit.Unit.RuntimeId) };
                Emit("line_impact",source.RuntimeId,hit.Unit.RuntimeId,0,hit.Unit.Position,"");
                resolution.Commit();
                if (!wall) continue;
                delta *= hit.Time!.Value;
                stopped = true;
                break;
            }
            var end = blade.Position+delta;
            var reached = stopped || end.DistanceSquaredTo(destination) <= .000001f;
            if (reached && blade.Returning)
            {
                _enemyActions = _enemyActions with { Blades = _enemyActions.Blades.Remove(blade.Id) };
                Emit("blade_end",blade.OwnerId,"",0,end,"",entityId:blade.Id,origin:blade.Position);
                continue;
            }
            var next = blade with { Position = end };
            if (reached) next = next with { Returning = true, End = end, HitIds = [] };
            _enemyActions = _enemyActions with { Blades = _enemyActions.Blades.SetItem(blade.Id,next) };
            Emit("blade_move",blade.OwnerId,"",0,end,"",entityId:blade.Id,origin:blade.Position);
        }
    }

    private static float? BladeHitTime(BladeFlight blade, Vector2 delta, BattleUnitState unit,
        System.Collections.Generic.IReadOnlyDictionary<string,Vector2> previous)
    {
        var start = previous.TryGetValue(unit.RuntimeId,out var p) ? p : unit.Position;
        var radius = blade.Operation.Radius+unit.BodyRadius;
        if (blade.Position.DistanceSquaredTo(start) <= radius*radius) return 0;
        return BattlefieldSpace.TryMovingCircleTimeOfImpact(blade.Position,delta,blade.Operation.Radius,
            start,unit.Position-start,unit.BodyRadius,out var t) ? t : null;
    }

    private void RaiseRampart(BattleUnitState owner, EnemyCast cast, CompiledRampart operation)
    {
        RemoveEnemyProducts(owner.RuntimeId,wallsOnly:true);
        var profile = _config.TacticalSummons[operation.WallContentId];
        var forward = (cast.End-cast.Start).Normalized();
        var normal = new Vector2(-forward.Y,forward.X);
        var spacing = profile.BodyRadius*2+BattlefieldSpace.BodyClearance+.005f;
        // A short three-body barrier; all segments reserve together before any is created.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var center = owner.Position+forward*(owner.BodyRadius+profile.BodyRadius+.15f+attempt*.35f);
            var positions = new[] { center-normal*spacing,center,center+normal*spacing };
            if (positions.Any(p => !CanOccupyPosition(p,profile.BodyRadius))) continue;
            foreach (var position in positions)
            {
                if (!owner.Alive) { RemoveEnemyProducts(owner.RuntimeId,wallsOnly:true); return; }
                var productId = $"s-{owner.Team}-{_summonCounter}";
                if (!SpawnTemporary(profile,owner.Team,position,1,1,owner.RuntimeId))
                    throw new InvalidOperationException("Reserved wall body became unavailable.");
                var unit = _units.First(u => u.RuntimeId == productId);
                _enemyActions = _enemyActions with { Products = _enemyActions.Products.Add(new(unit.RuntimeId,owner.RuntimeId,TickIndex+operation.WallLifetimeTicks)) };
                if (!owner.Alive) { RemoveEnemyProducts(owner.RuntimeId,wallsOnly:true); return; }
                Emit("wall_raised",owner.RuntimeId,unit.RuntimeId,0,position,"",enemyAction:new("rock_raise","release",TickIndex,5,profile.BodyRadius));
            }
            return;
        }
        Emit("wall_skipped",owner.RuntimeId,"",0,owner.Position,"");
    }

    private void AdvanceBroodPhases()
    {
        foreach (var owner in _units.Where(u => u.Alive).OrderBy(u => u.RuntimeId,StringComparer.Ordinal).ToArray())
        {
            var operation = _abilityScope?.FindOperation<CompiledBroodPhase>(owner.RuntimeId);
            if (operation is null) continue;
            if (!_enemyActions.Broods.TryGetValue(owner.RuntimeId,out var state))
            {
                if (owner.Health/owner.MaxHealth > operation.HealthThreshold) continue;
                CancelEnemyAction(owner.RuntimeId); CancelChargedLine(owner); CancelTrample(owner);
                CancelProjectileWindups(owner); owner.ProjectileSequence = null;
                _movement?.ReleaseUnit(owner.RuntimeId);
                state = new(TickIndex,false,TickIndex+operation.BreakTicks,0);
                _enemyActions = _enemyActions with { Broods = _enemyActions.Broods.Add(owner.RuntimeId,state) };
                Emit("shell_break",owner.RuntimeId,owner.RuntimeId,0,owner.Position,"",origin:owner.Position,
                    enemyAction:new(operation.Vfx,"prepare",TickIndex,operation.BreakTicks,owner.BodyRadius));
            }
            if (!state.Transformed && TickIndex >= state.BreakStart+operation.BreakTicks)
            {
                // Preserve the entity, health, shield, mana and status scope. Only the form changes.
                owner.BodyRadiusOverride = operation.SmallRadius;
                owner.AttackDeliveryOverride = AttackDelivery.Projectile;
                owner.ProjectileVisualId = "acid_spit";
                owner.Attributes.SetBaseValue(CombatAttribute.AttackRange,operation.RangedReach);
                owner.BossPhaseId = "exposed";
                _movement?.ReleaseUnit(owner.RuntimeId);
                var nearest = _units.Where(u => u.Alive && u.Team != owner.Team).OrderBy(u => u.Position.DistanceSquaredTo(owner.Position))
                    .ThenBy(u => u.RuntimeId,StringComparer.Ordinal).FirstOrDefault();
                if (nearest is not null && !EnemyControlled(owner) && !IsDisplacing(owner)) RetreatBrood(owner,nearest,operation.RetreatDistance);
                state = state with { Transformed = true };
                _enemyActions = _enemyActions with { Broods = _enemyActions.Broods.SetItem(owner.RuntimeId,state) };
                Emit("form_changed",owner.RuntimeId,owner.RuntimeId,operation.SmallRadius,owner.Position,"",origin:owner.Position,
                    enemyAction:new(operation.Vfx,"release",state.BreakStart,5,operation.SmallRadius));
            }
            if (!state.Transformed || TickIndex < state.NextSpawn || state.Batches >= operation.MaximumBatches || EnemyControlled(owner) || IsDisplacing(owner)) continue;
            var living = _units.Count(u => u.Alive && u.SummonerRuntimeId == owner.RuntimeId &&
                (u.Definition.ContentId == operation.EggContentId || u.Definition.ContentId == operation.LarvaContentId));
            var count = Math.Min(2,Math.Max(0,operation.MaximumLiving-living));
            var made = 0;
            if (_config.TacticalSummons.TryGetValue(operation.EggContentId,out var egg))
                for (var i = 0; i < count; i++)
                {
                    Vector2? position = null;
                    // Eggs stay beside the mother, never fall back to arbitrary faraway cells.
                    for (var sample = 0; sample < 12 && position is null; sample++)
                    {
                        var p = owner.Position+Vector2.FromAngle(sample*Mathf.Tau/12)*(owner.BodyRadius+egg.BodyRadius+.18f);
                        if (CanOccupyPosition(p,egg.BodyRadius)) position = p;
                    }
                    var eggId = $"s-{owner.Team}-{_summonCounter}";
                    if (position is null || !SpawnTemporary(egg,owner.Team,position.Value,1,1,owner.RuntimeId)) break;
                    made++;
                    _enemyActions = _enemyActions with { Products = _enemyActions.Products.Add(new(eggId,owner.RuntimeId,TickIndex+operation.HatchTicks,operation.LarvaContentId)) };
                    if (!owner.Alive) { RemoveEnemyProducts(owner.RuntimeId); break; }
                    Emit("egg_spawn",owner.RuntimeId,eggId,0,position.Value,"");
                }
            state = state with { NextSpawn = TickIndex+operation.SpawnCycleTicks, Batches = state.Batches+(made > 0 ? 1 : 0) };
            _enemyActions = _enemyActions with { Broods = _enemyActions.Broods.SetItem(owner.RuntimeId,state) };
        }
    }

    private void RetreatBrood(BattleUnitState owner, BattleUnitState target, float distance)
    {
        var start = owner.Position;
        var direction = (start-target.Position).Normalized();
        var end = start;
        for (var travelled = .1f; travelled <= distance+.001f; travelled += .1f)
        {
            var at = start+direction*travelled;
            if (!BattlefieldSpace.IsSegmentTerrainClear(end,at,owner.BodyRadius,Width,Height,_config.FloorRule.CanOccupy) ||
                _units.Any(u => u != owner && u.Alive && !IsAirborne(u) &&
                    u.Position.DistanceTo(at) < u.BodyRadius+owner.BodyRadius+BattlefieldSpace.BodyClearance) ||
                CaptureDisplacementReservations().Any(r => r.RuntimeId != owner.RuntimeId &&
                    r.Position.DistanceTo(at) < r.Radius+owner.BodyRadius+BattlefieldSpace.BodyClearance)) break;
            end = at;
        }
        if (end == start) return;
        var motion = new CompiledDisplacementOperation(DisplacementKind.Knockback, new CompiledExplicitTargetQuery(), distance, 5, 0, false, false, 0, 0, 0, EffectDamageType.Normal, null, 0);
        _displacements = _displacements.SetItem(owner.RuntimeId,new(owner.RuntimeId,owner.RuntimeId,target.RuntimeId,
            owner.Team,owner.Team,start,end,start,TickIndex,motion,default));
        EmitDisplacement(_displacements[owner.RuntimeId],owner,0,false);
    }

    private void AdvanceEnemyProducts()
    {
        foreach (var product in _enemyActions.Products.ToArray())
        {
            if (!_enemyActions.Products.Contains(product)) continue;
            var unit = _units.First(u => u.RuntimeId == product.UnitId);
            var owner = _units.First(u => u.RuntimeId == product.OwnerId);
            if (unit.Alive && owner.Alive && TickIndex < product.ExpireTick) continue;
            _enemyActions = _enemyActions with { Products = _enemyActions.Products.Remove(product) };
            if (!unit.Alive) continue;
            var hatch = owner.Alive && product.HatchContentId.Length > 0;
            RetireEnemyProduct(unit,owner.RuntimeId,hatch ? "egg_hatch" : "product_expired");
            if (hatch && _config.TacticalSummons.TryGetValue(product.HatchContentId,out var larva) &&
                CanOccupyPosition(unit.Position,larva.BodyRadius))
                SpawnTemporary(larva,owner.Team,unit.Position,1,1,owner.RuntimeId);
        }
    }

    private void RemoveEnemyProducts(string ownerId, bool wallsOnly = false)
    {
        foreach (var product in _enemyActions.Products.Where(p => p.OwnerId == ownerId && (!wallsOnly || p.HatchContentId == "")).ToArray())
        {
            _enemyActions = _enemyActions with { Products = _enemyActions.Products.Remove(product) };
            var unit = _units.First(u => u.RuntimeId == product.UnitId);
            if (unit.Alive) RetireEnemyProduct(unit,ownerId,"product_expired");
        }
    }

    private void RetireEnemyProduct(BattleUnitState unit, string ownerId, string reason)
    {
        // Expiry/hatching is removal, not combat damage or a credited kill.
        unit.Health = 0; unit.Mode = BattleUnitMode.Defeated;
        _statistics[unit.RuntimeId].DefeatTick ??= TickIndex;
        _mechanics = _mechanics with { SpentCorpses = _mechanics.SpentCorpses.Add(unit.RuntimeId) };
        _movement?.ReleaseUnit(unit.RuntimeId);
        HandleDeath(null,unit);
        Emit(reason,ownerId,unit.RuntimeId,0,unit.Position,"defeated");
    }
}
