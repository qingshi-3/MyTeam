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

// Queries contain live read-only views and immutable definitions; commands cannot expose a unit.
internal interface IBattleEnemyQueries
{
    int Tick { get; }
    IReadOnlyList<BattleUnitView> Units { get; }
    IReadOnlyDictionary<string, UnitSnapshot> Profiles { get; }
    IReadOnlyList<DisplacementReservation> Reservations { get; }
    bool CanOccupyTerrain(Vector2I cell);
    bool Controlled(string id);
    bool Displacing(string id);
    bool Airborne(string id);
    bool HasAttackInFlight(string id);
    CompiledBroodPhase? Brood(string id);
    bool HasLineAccess(Vector2 from, Vector2 to);
    bool CanOccupy(Vector2 position, float radius);
}
internal interface IBattleEnemyCommands
{
    void BeginCast(string ownerId, string targetId, string displayName);
    void ContinueCast(string ownerId);
    void Hit(string sourceId, string targetId, float damage, CombatSourceRef origin, EffectDamageType type);
    string? Spawn(UnitSnapshot profile, int team, Vector2 position, string ownerId);
    void Swap(string firstId, string secondId);
    void InterruptForForm(string ownerId);
    void TransformBrood(string ownerId, float radius, float reach);
    void Retreat(string ownerId, string targetId, Vector2 start, Vector2 end, float distance);
    void Retire(string unitId, string ownerId, string reason);
    void Present(BattleEvent fact);
}
internal interface IBattleEnemyTransactions
{
    void Resolve(Action work);
    void CheckpointOnly(Action work);
    void ResolveHit(Action work);
}

internal sealed record BattleBroodProgress(int BreakStart, bool Transformed, int NextSpawn, int Batches);

internal sealed class BattleEnemyActionScope(IBattleEnemyQueries read, IBattleEnemyCommands commands, IBattleEnemyTransactions transactions)
{
    private const int Width = BattlefieldLayout.Width;
    private const int Height = BattlefieldLayout.Height;
    private int TickIndex => read.Tick;
    // All records are immutable so an ability/world rollback restores one complete state root.
    private sealed record EnemyCast(string OwnerId, string TargetId, int Team, CompiledEnemyAction Operation,
        CombatSourceRef Origin, Vector2 Start, Vector2 End, int StartTick, int Step = 0);
    private sealed record BladeFlight(int Id, string OwnerId, int Team, CompiledReturningBlade Operation,
        CombatSourceRef Origin, Vector2 Start, Vector2 End, Vector2 Position, float Damage,
        bool Returning, ImmutableArray<string> HitIds);
    private sealed record EnemyProduct(string UnitId, string OwnerId, int ExpireTick, string HatchContentId = "");
    private sealed record EnemyActionState(ImmutableDictionary<string, EnemyCast> Casts,
        ImmutableDictionary<int, BladeFlight> Blades, ImmutableDictionary<string, BattleBroodProgress> Broods,
        ImmutableArray<EnemyProduct> Products, int Sequence = 0);
    private static EnemyActionState Empty => new(ImmutableDictionary<string, EnemyCast>.Empty,
        ImmutableDictionary<int, BladeFlight>.Empty, ImmutableDictionary<string, BattleBroodProgress>.Empty, []);
    private EnemyActionState _enemyActions = Empty;
    public bool IsBusy(string id) => _enemyActions.Casts.ContainsKey(id) || _enemyActions.Blades.Values.Any(blade => blade.OwnerId == id) ||
        (_enemyActions.Broods.TryGetValue(id, out var brood) && !brood.Transformed);
    public bool IsRampart(string id) => _enemyActions.Products.Any(product => product.UnitId == id && product.HatchContentId == "");
    public BattleBroodProgress? ReadBrood(string id) => _enemyActions.Broods.GetValueOrDefault(id);
    public bool IsExecuting(string id, string abilityId) =>
        (_enemyActions.Casts.TryGetValue(id, out var cast) && cast.Origin.StableId == abilityId) ||
        _enemyActions.Blades.Values.Any(blade => blade.OwnerId == id && blade.Origin.StableId == abilityId);
    public void Clear() => _enemyActions = Empty;
    public Checkpoint Capture() => new(this);
    public void Restore(Checkpoint checkpoint) => checkpoint.Restore(this);
    internal sealed class Checkpoint
    {
        private readonly BattleEnemyActionScope _owner;
        private readonly EnemyActionState _state;
        internal Checkpoint(BattleEnemyActionScope owner) { _owner = owner; _state = owner._enemyActions; }
        internal void Restore(BattleEnemyActionScope owner)
        {
            if (!ReferenceEquals(owner, _owner)) throw new InvalidOperationException("Enemy action checkpoint belongs to another battle.");
            owner._enemyActions = _state;
        }
    }
    public string SelectTarget(string ownerId, CompiledEnemyAction operation) => SelectEnemyActionTarget(Unit(ownerId), operation);
    public bool Prepare(string ownerId, CompiledEnemyAction operation, string targetId) => PrepareEnemyAction(Unit(ownerId), operation, targetId);
    public void Begin(string ownerId, string targetId, CompiledEnemyAction operation, string displayName, CombatSourceRef origin) =>
        BeginEnemyAction(Unit(ownerId), Unit(targetId), operation, displayName, origin);
    private BattleUnitView Unit(string id) => read.Units.First(unit => unit.RuntimeId == id);

    private string SelectEnemyActionTarget(BattleUnitView owner, CompiledEnemyAction operation)
    {
        var range = operation switch { CompiledConeBreath x => x.Range, CompiledReturningBlade x => x.Range,
            CompiledPositionSwap x => x.Range, CompiledRampart x => x.Range, _ => owner.BodyRadius + 2 };
        var targets = read.Units.Where(u => u.Alive && u.Team != owner.Team && !IsRampart(u.RuntimeId) &&
            owner.Position.DistanceTo(u.Position) <= range + (operation is CompiledConeBreath ? u.BodyRadius : 0) &&
            (operation is CompiledPositionSwap ? CanSwapBodies(owner, u) : read.HasLineAccess(owner.Position,u.Position)));
        return (operation is CompiledPositionSwap ? targets.OrderByDescending(u => owner.Position.DistanceSquaredTo(u.Position)) :
            targets.OrderBy(u => owner.Position.DistanceSquaredTo(u.Position)))
            .ThenBy(u => u.RuntimeId,StringComparer.Ordinal).Select(u => u.RuntimeId).FirstOrDefault() ?? "";
    }

    private bool PrepareEnemyAction(BattleUnitView owner, CompiledEnemyAction operation, string targetId)
    {
        if (IsBusy(owner.RuntimeId) || read.Displacing(owner.RuntimeId) || read.HasAttackInFlight(owner.RuntimeId)) return false;
        if (operation is CompiledBroodPhase && _enemyActions.Broods.ContainsKey(owner.RuntimeId)) return false;
        if (operation.ContentDependencies.Any(id => !read.Profiles.ContainsKey(id))) return false;
        return targetId.Length > 0 && targetId == SelectEnemyActionTarget(owner,operation);
    }

    private void BeginEnemyAction(BattleUnitView owner, BattleUnitView target, CompiledEnemyAction operation, string displayName, CombatSourceRef origin)
    {
        var direction = (target.Position-owner.Position).Normalized();
        if (direction.IsZeroApprox()) direction = owner.Team == 0 ? Vector2.Right : Vector2.Left;
        var range = operation switch { CompiledConeBreath x => x.Range, CompiledReturningBlade x => x.Range,
            CompiledRampart x => x.Range, _ => owner.Position.DistanceTo(target.Position) };
        var cast = new EnemyCast(owner.RuntimeId,target.RuntimeId,owner.Team,operation,origin,
            owner.Position,owner.Position+direction*range,TickIndex);
        if (operation is CompiledRampart rampart)
            cast = cast with { End = cast.Start+direction*range*BattlefieldSpace.FirstTerrainHitFraction(
                cast.Start,direction*range,rampart.FissureRadius,Width,Height,read.CanOccupyTerrain) };
        _enemyActions = _enemyActions with { Casts = _enemyActions.Casts.SetItem(owner.RuntimeId,cast) };
        commands.BeginCast(owner.RuntimeId, target.RuntimeId, displayName);
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

    public void Advance()
    {
        if (_enemyActions.Casts.IsEmpty && _enemyActions.Products.IsEmpty &&
            !read.Units.Any(unit => unit.Alive && read.Brood(unit.RuntimeId) is not null)) return;
        transactions.Resolve(() =>
        {
            AdvanceBroodPhases();
            AdvanceEnemyProducts();
            foreach (var cast in _enemyActions.Casts.Values.OrderBy(c => c.OwnerId,StringComparer.Ordinal).ToArray())
            {
                if (!_enemyActions.Casts.ContainsKey(cast.OwnerId)) continue;
                var owner = read.Units.First(u => u.RuntimeId == cast.OwnerId);
                if (!owner.Alive || owner.Team != cast.Team || read.Controlled(owner.RuntimeId) || read.Displacing(owner.RuntimeId) ||
                    !owner.Position.IsEqualApprox(cast.Start)) { Cancel(owner.RuntimeId); continue; }
                commands.ContinueCast(owner.RuntimeId);
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
                        if (elapsed >= x.WindupTicks+x.PulseCount*x.PulseIntervalTicks+x.RecoveryTicks) Cancel(owner.RuntimeId);
                        break;
                    case CompiledReturningBlade x:
                        if (cast.Step == 0 && elapsed >= Math.Max(1,Mathf.CeilToInt(owner.Definition.ProjectileWindupSeconds/BattleTiming.TickSeconds)))
                        {
                            var delta = cast.End-cast.Start;
                            var end = cast.Start+delta*BattlefieldSpace.FirstTerrainHitFraction(cast.Start,delta,x.Radius,Width,Height,read.CanOccupyTerrain);
                            var id = _enemyActions.Sequence+1;
                            var blade = new BladeFlight(id,owner.RuntimeId,owner.Team,x,cast.Origin,cast.Start,end,cast.Start,
                                owner.Damage*x.AttackMultiplier,false,[]);
                            _enemyActions = _enemyActions with { Sequence = id, Blades = _enemyActions.Blades.Add(id,blade) };
                            SetEnemyCast(cast with { Step = id });
                            Emit("blade_release",owner.RuntimeId,cast.TargetId,0,cast.End,"",entityId:id,origin:cast.Start,
                                enemyAction:new(x.Vfx,"flight",TickIndex,1,x.Radius));
                        }
                        else if (cast.Step > 0 && !_enemyActions.Blades.ContainsKey(cast.Step)) Cancel(owner.RuntimeId);
                        break;
                    case CompiledPositionSwap x:
                        var target = read.Units.FirstOrDefault(u => u.RuntimeId == cast.TargetId);
                        if (cast.Step == 0 && (target is null || !target.Alive || target.Team == owner.Team || read.Displacing(target.RuntimeId)))
                        { Cancel(owner.RuntimeId); break; }
                        if (cast.Step == 0 && elapsed >= x.PrepareTicks)
                        {
                            if (target is null || !CanSwapBodies(owner,target)) { Cancel(owner.RuntimeId); break; }
                            commands.Swap(owner.RuntimeId,target.RuntimeId);
                            SetEnemyCast(cast with { Start = owner.Position, End = target.Position, Step = 1 });
                            EmitEnemyAction(cast,x.Vfx,"release",3);
                        }
                        else if (cast.Step == 0 && target is not null)
                            EmitEnemyAction(cast with { End = target.Position },x.Vfx,"update",x.PrepareTicks);
                        if (elapsed >= x.PrepareTicks+x.RecoveryTicks) Cancel(owner.RuntimeId);
                        break;
                    case CompiledRampart x:
                        if (cast.Step == 0 && elapsed >= x.RaiseTicks) { RaiseRampart(owner,cast,x); SetEnemyCast(cast with { Step = 1 }); }
                        if (cast.Step == 1 && elapsed >= x.RaiseTicks+x.IntervalTicks)
                        { EmitEnemyAction(cast,x.Vfx,"prepare",x.FissureWindupTicks,x.FissureRadius); SetEnemyCast(cast with { Step = 2 }); }
                        if (cast.Step == 2 && elapsed >= x.RaiseTicks+x.IntervalTicks+x.FissureWindupTicks)
                        {
                            foreach (var hit in read.Units.Where(u => u.Alive && u.Team != owner.Team &&
                                LineHitTime(cast.Start,cast.End-cast.Start,x.FissureRadius,u) is not null).OrderBy(u => u.RuntimeId,StringComparer.Ordinal).ToArray())
                                commands.Hit(owner.RuntimeId,hit.RuntimeId,owner.Damage*x.AttackMultiplier,cast.Origin,EffectDamageType.Normal);
                            EmitEnemyAction(cast,x.Vfx,"release",5,x.FissureRadius);
                            RemoveProducts(owner.RuntimeId, wallsOnly:true);
                            SetEnemyCast(cast with { Step = 3 });
                        }
                        if (elapsed >= x.RaiseTicks+x.IntervalTicks+x.FissureWindupTicks+x.RecoveryTicks) Cancel(owner.RuntimeId);
                        break;
                    case CompiledBroodPhase:
                        if (elapsed >= 5) Cancel(owner.RuntimeId);
                        break;
                }
            }
        });
    }

    private void SetEnemyCast(EnemyCast cast)
    {
        // Reactions may have killed or interrupted the caster while resolving this stage.
        if (_enemyActions.Casts.ContainsKey(cast.OwnerId))
            _enemyActions = _enemyActions with { Casts = _enemyActions.Casts.SetItem(cast.OwnerId,cast) };
    }
    public void Cancel(string ownerId)
    {
        if (!_enemyActions.Casts.TryGetValue(ownerId,out var cast)) return;
        _enemyActions = _enemyActions with { Casts = _enemyActions.Casts.Remove(ownerId) };
        var vfx = cast.Operation switch { CompiledConeBreath x => x.Vfx, CompiledPositionSwap x => x.Vfx,
            CompiledRampart x => x.Vfx, _ => "" };
        if (vfx != "") EmitEnemyAction(cast,vfx,"end",1);
        if (cast.Operation is CompiledRampart) RemoveProducts(ownerId,wallsOnly:true);
        // Already released blades deliberately survive control, displacement and caster death.
        Emit("attack_cancel",ownerId,ownerId,0,cast.Start,"");
    }

    private void HitCone(BattleUnitView owner, EnemyCast cast, float range, float angle, float multiplier, EffectDamageType damageType)
    {
        var heading = (cast.End-cast.Start).Normalized();
        foreach (var target in read.Units.Where(u => u.Alive && u.Team != cast.Team).OrderBy(u => u.RuntimeId,StringComparer.Ordinal).ToArray())
        {
            var offset = target.Position-cast.Start;
            var distance = offset.Length();
            var extraAngle = Mathf.Asin(Mathf.Clamp(target.BodyRadius/Math.Max(.001f,distance),0,1));
            if (distance-target.BodyRadius > range ||
                (distance > target.BodyRadius && Math.Abs(heading.AngleTo(offset)) > Mathf.DegToRad(angle/2)+extraAngle) ||
                !read.HasLineAccess(cast.Start,target.Position)) continue;
            commands.Hit(owner.RuntimeId,target.RuntimeId,owner.Damage*multiplier,cast.Origin,damageType);
            Emit("line_impact",owner.RuntimeId,target.RuntimeId,0,target.Position,"");
        }
    }

    private bool CanSwapBodies(BattleUnitView first, BattleUnitView second)
    {
        if (!first.Alive || !second.Alive || first == second || first.Team == second.Team ||
            read.Displacing(first.RuntimeId) || read.Displacing(second.RuntimeId) || IsRampart(second.RuntimeId)) return false;
        bool Fits(BattleUnitView body, Vector2 at) =>
            BattlefieldSpace.IsPositionTerrainClear(at,body.BodyRadius,Width,Height,read.CanOccupyTerrain) &&
            read.Units.All(u => !u.Alive || u == first || u == second || read.Airborne(u.RuntimeId) ||
                u.Position.DistanceTo(at) >= u.BodyRadius+body.BodyRadius+BattlefieldSpace.BodyClearance) &&
            read.Reservations.All(r => r.RuntimeId == first.RuntimeId || r.RuntimeId == second.RuntimeId ||
                r.Position.DistanceTo(at) >= r.Radius+body.BodyRadius+BattlefieldSpace.BodyClearance);
        return first.Position.DistanceTo(second.Position) >= first.BodyRadius+second.BodyRadius+BattlefieldSpace.BodyClearance &&
            Fits(first,second.Position) && Fits(second,first.Position);
    }

    public void AdvanceBlades(System.Collections.Generic.IReadOnlyDictionary<string,Vector2> previousPositions)
    {
        if (_enemyActions.Blades.IsEmpty) return;
        transactions.CheckpointOnly(() => AdvanceReturningBladesCore(previousPositions));
    }

    private void AdvanceReturningBladesCore(System.Collections.Generic.IReadOnlyDictionary<string,Vector2> previousPositions)
    {
        foreach (var original in _enemyActions.Blades.Values.OrderBy(b => b.Id).ToArray())
        {
            var blade = original;
            var destination = blade.Returning ? blade.Start : blade.End;
            var delta = (destination-blade.Position).LimitLength(blade.Operation.Speed*BattleTiming.TickSeconds);
            var source = read.Units.First(u => u.RuntimeId == blade.OwnerId);
            var stopped = false;
            var hits = read.Units.Where(u => u.Alive && (u.Team != blade.Team || IsRampart(u.RuntimeId)) && !blade.HitIds.Contains(u.RuntimeId))
                .Select(u => (Unit:u, Time:BladeHitTime(blade,delta,u,previousPositions)))
                .Where(h => h.Time is not null).OrderBy(h => h.Time).ThenBy(h => h.Unit.RuntimeId,StringComparer.Ordinal).ToArray();
            foreach (var hit in hits)
            {
                if (!hit.Unit.Alive) continue;
                var wall = IsRampart(hit.Unit.RuntimeId);
                transactions.ResolveHit(() =>
                {
                    if (hit.Unit.Team != blade.Team) commands.Hit(source.RuntimeId,hit.Unit.RuntimeId,blade.Damage,blade.Origin,EffectDamageType.Normal);
                    blade = blade with { HitIds = blade.HitIds.Add(hit.Unit.RuntimeId) };
                    Emit("line_impact",source.RuntimeId,hit.Unit.RuntimeId,0,hit.Unit.Position,"");
                });
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

    private static float? BladeHitTime(BladeFlight blade, Vector2 delta, BattleUnitView unit,
        System.Collections.Generic.IReadOnlyDictionary<string,Vector2> previous)
    {
        var start = previous.TryGetValue(unit.RuntimeId,out var p) ? p : unit.Position;
        var radius = blade.Operation.Radius+unit.BodyRadius;
        if (blade.Position.DistanceSquaredTo(start) <= radius*radius) return 0;
        return BattlefieldSpace.TryMovingCircleTimeOfImpact(blade.Position,delta,blade.Operation.Radius,
            start,unit.Position-start,unit.BodyRadius,out var t) ? t : null;
    }

    private void RaiseRampart(BattleUnitView owner, EnemyCast cast, CompiledRampart operation)
    {
        RemoveProducts(owner.RuntimeId,wallsOnly:true);
        var profile = read.Profiles[operation.WallContentId];
        var forward = (cast.End-cast.Start).Normalized();
        var normal = new Vector2(-forward.Y,forward.X);
        var spacing = profile.BodyRadius*2+BattlefieldSpace.BodyClearance+.005f;
        // A short three-body barrier; all segments reserve together before any is created.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var center = owner.Position+forward*(owner.BodyRadius+profile.BodyRadius+.15f+attempt*.35f);
            var positions = new[] { center-normal*spacing,center,center+normal*spacing };
            if (positions.Any(p => !read.CanOccupy(p,profile.BodyRadius))) continue;
            foreach (var position in positions)
            {
                if (!owner.Alive) { RemoveProducts(owner.RuntimeId,wallsOnly:true); return; }
                var productId = commands.Spawn(profile,owner.Team,position,owner.RuntimeId);
                if (productId is null)
                    throw new InvalidOperationException("Reserved wall body became unavailable.");
                var unit = read.Units.First(u => u.RuntimeId == productId);
                _enemyActions = _enemyActions with { Products = _enemyActions.Products.Add(new(unit.RuntimeId,owner.RuntimeId,TickIndex+operation.WallLifetimeTicks)) };
                if (!owner.Alive) { RemoveProducts(owner.RuntimeId,wallsOnly:true); return; }
                Emit("wall_raised",owner.RuntimeId,unit.RuntimeId,0,position,"",enemyAction:new("rock_raise","release",TickIndex,5,profile.BodyRadius));
            }
            return;
        }
        Emit("wall_skipped",owner.RuntimeId,"",0,owner.Position,"");
    }

    private void AdvanceBroodPhases()
    {
        foreach (var owner in read.Units.Where(u => u.Alive).OrderBy(u => u.RuntimeId,StringComparer.Ordinal).ToArray())
        {
            var operation = read.Brood(owner.RuntimeId);
            if (operation is null) continue;
            if (!_enemyActions.Broods.TryGetValue(owner.RuntimeId,out var state))
            {
                if (owner.Health/owner.MaxHealth > operation.HealthThreshold) continue;
                commands.InterruptForForm(owner.RuntimeId);
                state = new(TickIndex,false,TickIndex+operation.BreakTicks,0);
                _enemyActions = _enemyActions with { Broods = _enemyActions.Broods.Add(owner.RuntimeId,state) };
                Emit("shell_break",owner.RuntimeId,owner.RuntimeId,0,owner.Position,"",origin:owner.Position,
                    enemyAction:new(operation.Vfx,"prepare",TickIndex,operation.BreakTicks,owner.BodyRadius));
            }
            if (!state.Transformed && TickIndex >= state.BreakStart+operation.BreakTicks)
            {
                // Preserve the entity, health, shield, mana and status scope. Only the form changes.
                commands.TransformBrood(owner.RuntimeId, operation.SmallRadius, operation.RangedReach);
                var nearest = read.Units.Where(u => u.Alive && u.Team != owner.Team).OrderBy(u => u.Position.DistanceSquaredTo(owner.Position))
                    .ThenBy(u => u.RuntimeId,StringComparer.Ordinal).FirstOrDefault();
                if (nearest is not null && !read.Controlled(owner.RuntimeId) && !read.Displacing(owner.RuntimeId)) RetreatBrood(owner,nearest,operation.RetreatDistance);
                state = state with { Transformed = true };
                _enemyActions = _enemyActions with { Broods = _enemyActions.Broods.SetItem(owner.RuntimeId,state) };
                Emit("form_changed",owner.RuntimeId,owner.RuntimeId,operation.SmallRadius,owner.Position,"",origin:owner.Position,
                    enemyAction:new(operation.Vfx,"release",state.BreakStart,5,operation.SmallRadius));
            }
            if (!state.Transformed || TickIndex < state.NextSpawn || state.Batches >= operation.MaximumBatches || read.Controlled(owner.RuntimeId) || read.Displacing(owner.RuntimeId)) continue;
            var living = read.Units.Count(u => u.Alive && u.SummonerRuntimeId == owner.RuntimeId &&
                (u.Definition.ContentId == operation.EggContentId || u.Definition.ContentId == operation.LarvaContentId));
            var count = Math.Min(2,Math.Max(0,operation.MaximumLiving-living));
            var made = 0;
            if (read.Profiles.TryGetValue(operation.EggContentId,out var egg))
                for (var i = 0; i < count; i++)
                {
                    Vector2? position = null;
                    // Eggs stay beside the mother, never fall back to arbitrary faraway cells.
                    for (var sample = 0; sample < 12 && position is null; sample++)
                    {
                        var p = owner.Position+Vector2.FromAngle(sample*Mathf.Tau/12)*(owner.BodyRadius+egg.BodyRadius+.18f);
                        if (read.CanOccupy(p,egg.BodyRadius)) position = p;
                    }
                    if (position is null) break;
                    var eggId = commands.Spawn(egg,owner.Team,position.Value,owner.RuntimeId);
                    if (eggId is null) break;
                    made++;
                    _enemyActions = _enemyActions with { Products = _enemyActions.Products.Add(new(eggId,owner.RuntimeId,TickIndex+operation.HatchTicks,operation.LarvaContentId)) };
                    if (!owner.Alive) { RemoveProducts(owner.RuntimeId); break; }
                    Emit("egg_spawn",owner.RuntimeId,eggId,0,position.Value,"");
                }
            state = state with { NextSpawn = TickIndex+operation.SpawnCycleTicks, Batches = state.Batches+(made > 0 ? 1 : 0) };
            _enemyActions = _enemyActions with { Broods = _enemyActions.Broods.SetItem(owner.RuntimeId,state) };
        }
    }

    private void RetreatBrood(BattleUnitView owner, BattleUnitView target, float distance)
    {
        var start = owner.Position;
        var direction = (start-target.Position).Normalized();
        var end = start;
        for (var travelled = .1f; travelled <= distance+.001f; travelled += .1f)
        {
            var at = start+direction*travelled;
            if (!BattlefieldSpace.IsSegmentTerrainClear(end,at,owner.BodyRadius,Width,Height,read.CanOccupyTerrain) ||
                read.Units.Any(u => u != owner && u.Alive && !read.Airborne(u.RuntimeId) &&
                    u.Position.DistanceTo(at) < u.BodyRadius+owner.BodyRadius+BattlefieldSpace.BodyClearance) ||
                read.Reservations.Any(r => r.RuntimeId != owner.RuntimeId &&
                    r.Position.DistanceTo(at) < r.Radius+owner.BodyRadius+BattlefieldSpace.BodyClearance)) break;
            end = at;
        }
        if (end == start) return;
        commands.Retreat(owner.RuntimeId, target.RuntimeId, start, end, distance);
    }

    private void AdvanceEnemyProducts()
    {
        foreach (var product in _enemyActions.Products.ToArray())
        {
            if (!_enemyActions.Products.Contains(product)) continue;
            var unit = read.Units.First(u => u.RuntimeId == product.UnitId);
            var owner = read.Units.First(u => u.RuntimeId == product.OwnerId);
            if (unit.Alive && owner.Alive && TickIndex < product.ExpireTick) continue;
            _enemyActions = _enemyActions with { Products = _enemyActions.Products.Remove(product) };
            if (!unit.Alive) continue;
            var hatch = owner.Alive && product.HatchContentId.Length > 0;
            commands.Retire(unit.RuntimeId,owner.RuntimeId,hatch ? "egg_hatch" : "product_expired");
            if (hatch && read.Profiles.TryGetValue(product.HatchContentId,out var larva) &&
                read.CanOccupy(unit.Position,larva.BodyRadius))
                commands.Spawn(larva,owner.Team,unit.Position,owner.RuntimeId);
        }
    }

    public void RemoveProducts(string ownerId, bool wallsOnly = false)
    {
        foreach (var product in _enemyActions.Products.Where(p => p.OwnerId == ownerId && (!wallsOnly || p.HatchContentId == "")).ToArray())
        {
            _enemyActions = _enemyActions with { Products = _enemyActions.Products.Remove(product) };
            var unit = read.Units.First(u => u.RuntimeId == product.UnitId);
            if (unit.Alive) commands.Retire(unit.RuntimeId,ownerId,"product_expired");
        }
    }

    private static float? LineHitTime(Vector2 start, Vector2 delta, float radius, BattleUnitView target)
    {
        var combined = radius + target.BodyRadius;
        if (start.DistanceSquaredTo(target.Position) <= combined * combined) return 0;
        return BattlefieldSpace.TryMovingCircleTimeOfImpact(start, delta, radius, target.Position,
            Vector2.Zero, target.BodyRadius, out var time) ? time : null;
    }

    private void Emit(string type, string source, string target, float value, Vector2 position, string cue,
        int entityId = 0, Vector2 origin = default, BattleAttackTiming? attackTiming = null, BattleEnemyActionCue? enemyAction = null) =>
        commands.Present(new BattleEvent(TickIndex, type, source, target, value, BattlefieldSpace.PositionToCell(position),
            cue, position, entityId, origin, AttackTiming: attackTiming, EnemyAction: enemyAction));
}
