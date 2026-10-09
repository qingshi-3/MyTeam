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

internal interface IBattleDisplacementWorld
{
    int Tick { get; }
    IReadOnlyList<BattleUnitState> Units { get; }
    bool CanOccupy(Vector2I cell);
    ImmutableArray<string> ResolveTargets(CompiledEffectTargetQuery query, BattleUnitState owner, string target);
    void ReleaseUnit(string id);
    void CancelProjectileWindups(BattleUnitState unit);
    void InterruptChannels(string id);
    float Damage(string source, BattleUnitState owner, BattleUnitState target, float raw, CombatSourceRef origin, EffectDamageType type);
    CombatDamageClass DamageClass(CombatSourceRef origin);
    void Arrived(BattleUnitState owner, BattleUnitState target, CombatSourceRef origin);
    void Finished(CombatSourceRef origin,bool completed);
    void Resolve(Action work);
    void Publish(BattleCombatEventDraft fact);
    void Present(string type, string source, string target, float value, Vector2 position, string cue,
        Vector2 origin = default, BattleDisplacementCue? displacement = null);
}

internal sealed class BattleDisplacementScope(IBattleDisplacementWorld world, BattleStatusScope statuses)
{
    private const int Width = BattlefieldLayout.Width, Height = BattlefieldLayout.Height;
    private static CombatCell ToCombatCell(Vector2I cell) => new(cell.X, cell.Y);
    private static CombatPoint ToCombatPoint(Vector2 position) => new(position.X, position.Y);
    private static void SetActionTarget(BattleUnitState owner, BattleUnitState target)
    { owner.ActionTargetRuntimeId = target.RuntimeId; owner.ActionTargetName = target.Definition.DisplayName; }
    internal sealed class Checkpoint
    {
        private readonly BattleDisplacementScope _owner;
        private readonly ImmutableDictionary<string, DisplacementMotion> _state;
        internal Checkpoint(BattleDisplacementScope owner) { _owner = owner; _state = owner._displacements; }
        internal void Restore(BattleDisplacementScope owner)
        {
            if (!ReferenceEquals(owner, _owner)) throw new InvalidOperationException("Checkpoint belongs to another displacement scope.");
            owner._displacements = _state;
        }
    }
    public Checkpoint Capture() => new(this);
    public void Restore(Checkpoint checkpoint) => checkpoint.Restore(this);
    public void Clear() => _displacements = _displacements.Clear();
    public bool IsDisplacedBy(string id, string source) => _displacements.TryGetValue(id, out var motion) && motion.SourceId == source;
    public void Retreat(BattleUnitState owner, string targetId, Vector2 start, Vector2 end, float distance)
    {
        var motion = new CompiledDisplacementOperation(DisplacementKind.Knockback, new CompiledExplicitTargetQuery(), distance, 5, 0, false, false, 0, 0, 0, EffectDamageType.Normal, null, 0);
        _displacements = _displacements.SetItem(owner.RuntimeId, new(owner.RuntimeId, owner.RuntimeId, targetId,
            owner.Team, owner.Team, start, end, start, world.Tick, motion, default));
        EmitDisplacement(_displacements[owner.RuntimeId], owner, 0, false);
    }
    // Immutable battle-owned trajectories are included in the enclosing world checkpoint. A leap's
    // ground projection is still targetable, but its reserved landing circle is the ground obstacle.
    private sealed record DisplacementMotion(string SourceId, string MoverId, string TargetId,
        int SourceTeam, int MoverTeam, Vector2 Start, Vector2 End, Vector2 EffectCenter, int StartTick,
        CompiledDisplacementOperation Operation, CombatSourceRef Origin, bool Cancelled = false);
    internal sealed record DisplacementPlan(BattleUnitState Mover, BattleUnitState Target, Vector2 End);
    private ImmutableDictionary<string, DisplacementMotion> _displacements =
        ImmutableDictionary<string, DisplacementMotion>.Empty.WithComparers(StringComparer.Ordinal);

    public bool IsDisplacing(BattleUnitState unit) => _displacements.ContainsKey(unit.RuntimeId);
    public bool IsAirborne(BattleUnitState unit) => _displacements.TryGetValue(unit.RuntimeId, out var motion) &&
        motion.Operation.Kind == DisplacementKind.Leap;

    public bool TryBlinkBeside(BattleUnitState owner, BattleUnitState target, float maximumDistance, CombatSourceRef origin)
    {
        if (!owner.Alive || !target.Alive || owner.Team == target.Team || IsDisplacing(owner)) return false;
        var operation = new CompiledDisplacementOperation(DisplacementKind.Blink, new CompiledExplicitTargetQuery(),
            maximumDistance, 1, .05f, false, false, 0, 0, 0, EffectDamageType.Normal, null, 0);
        var plan = BuildDisplacementPlans(operation, owner, [target]).FirstOrDefault();
        if (plan is null) return false;
        var start = owner.Position;
        var motion = new DisplacementMotion(owner.RuntimeId, owner.RuntimeId, target.RuntimeId,
            owner.Team, owner.Team, start, plan.End, start, world.Tick, operation, origin);
        EmitDisplacement(motion, owner, 0, false);
        owner.Position = plan.End;
        world.ReleaseUnit(owner.RuntimeId);
        SetActionTarget(owner, target);
        EmitDisplacement(motion, owner, 1, true);
        world.Publish(new(BattleCombatEventKind.UnitMoved, origin, owner.RuntimeId, "", world.Tick,
            Cell: ToCombatCell(owner.Cell), Position: ToCombatPoint(owner.Position), ActionId: origin.InstanceId));
        return true;
    }

    public bool TryRetreatMaintainingReach(BattleUnitState owner,BattleUnitState target,float distance,float reach,CombatSourceRef origin)
    {
        if(!owner.Alive||!target.Alive||IsDisplacing(owner))return false;
        var direction=(owner.Position-target.Position).Normalized();
        if(direction.IsZeroApprox())return false;
        var end=ClipDisplacementTravel(owner,owner.Position,owner.Position+direction*distance);
        if(owner.Position.DistanceTo(end)<.04f||!CanLandDisplacement(owner,end)||
            end.DistanceTo(target.Position)>owner.BodyRadius+target.BodyRadius+reach+.0001f)return false;
        var operation=new CompiledDisplacementOperation(DisplacementKind.Knockback,new CompiledExplicitTargetQuery(),
            distance,2,0,false,false,0,0,0,EffectDamageType.Normal,null,0);
        var motion=new DisplacementMotion(owner.RuntimeId,owner.RuntimeId,target.RuntimeId,owner.Team,owner.Team,
            owner.Position,end,owner.Position,world.Tick,operation,origin);
        _displacements=_displacements.SetItem(owner.RuntimeId,motion);
        world.ReleaseUnit(owner.RuntimeId);
        EmitDisplacement(motion,owner,0,false);
        return true;
    }

    public IReadOnlyList<DisplacementReservation> CaptureDisplacementReservations() =>
        _displacements.Values.OrderBy(motion => motion.MoverId, StringComparer.Ordinal)
            .Select(motion => new DisplacementReservation(motion.MoverId, motion.End,
                world.Units.First(unit => unit.RuntimeId == motion.MoverId).BodyRadius)).ToArray();

    public bool PrepareDisplacementOperation(CompiledDisplacementOperation operation, BattleUnitState owner,
        string explicitTarget, out ImmutableArray<string> targets)
    {
        targets = [];
        if (!owner.Alive || IsDisplacing(owner)) return false;
        // Exclude ineligible bodies before applying the authored target count. A nearby boss or an
        // already-moving target must not hide the next legal candidate and consume the cast.
        var expanded = operation.TargetQuery is CompiledFilteredTargetQuery filtered
            ? filtered with { MaxTargets = 0 } : operation.TargetQuery;
        var candidates = world.ResolveTargets(expanded, owner, explicitTarget)
            .Select(id => world.Units.First(unit => unit.RuntimeId == id))
            .Where(target => IsDisplacementTargetEligible(operation, owner, target));
        var plans = BuildDisplacementPlans(operation, owner, candidates);
        var limit = operation.TargetQuery is CompiledFilteredTargetQuery query ? query.MaxTargets : 0;
        if (MovesCaster(operation.Kind)) limit = 1;
        targets = (limit > 0 ? plans.Take(limit) : plans).Select(plan => plan.Target.RuntimeId).ToImmutableArray();
        return !targets.IsEmpty;
    }

    public void ExecuteDisplacementOperation(CompiledDisplacementOperation operation, ImmutableArray<string> targets,
        BattleUnitState owner, CombatSourceRef origin)
    {
        var candidates = targets.Select(id => world.Units.FirstOrDefault(unit => unit.RuntimeId == id))
            .Where(unit => unit is not null).Select(unit => unit!);
        var plans = BuildDisplacementPlans(operation, owner, candidates);
        if (plans.Count == 0)
            throw new InvalidOperationException("Prepared displacement no longer has a legal moving body and destination.");
        foreach (var plan in plans)
        {
            var mover = plan.Mover;
            var motion = new DisplacementMotion(owner.RuntimeId, mover.RuntimeId, plan.Target.RuntimeId,
                owner.Team, mover.Team, mover.Position, plan.End, owner.Position, world.Tick, operation, origin);
            _displacements = _displacements.Add(mover.RuntimeId, motion);
            world.ReleaseUnit(mover.RuntimeId);
            world.CancelProjectileWindups(mover);
            mover.ProjectileSequence = null;
            world.InterruptChannels(mover.RuntimeId);
            mover.WaitingTicks = 0;
            mover.Mode = BattleUnitMode.Moving;
            if (mover == owner) SetActionTarget(owner, plan.Target);
            EmitDisplacement(motion, mover, 0, false);
        }
    }

    private static bool MovesCaster(DisplacementKind kind) =>
        kind is DisplacementKind.Charge or DisplacementKind.Leap or DisplacementKind.Blink;

    public bool IsDisplacementTargetEligible(CompiledDisplacementOperation operation,
        BattleUnitState owner, BattleUnitState target) =>
        owner.Alive && target.Alive && target != owner && target.Team != owner.Team &&
        (!operation.ExcludeBoss || !target.Definition.IsBoss) && !IsDisplacing(target) && !IsDisplacing(owner);

    public List<DisplacementPlan> BuildDisplacementPlans(CompiledDisplacementOperation operation,
        BattleUnitState owner, IEnumerable<BattleUnitState> candidates)
    {
        var result = new List<DisplacementPlan>();
        var reserved = new List<DisplacementReservation>();
        foreach (var target in candidates)
        {
            if (!IsDisplacementTargetEligible(operation, owner, target)) continue;
            var mover = MovesCaster(operation.Kind) ? owner : target;
            Vector2? destination;
            if (operation.Kind is DisplacementKind.Leap or DisplacementKind.Blink)
                destination = FindDisplacementLanding(operation, owner, target, reserved);
            else
            {
                var delta = target.Position - owner.Position;
                var direction = delta.LengthSquared() > .000001f ? delta.Normalized() :
                    owner.Team == 0 ? Vector2.Right : Vector2.Left;
                var contact = owner.BodyRadius + target.BodyRadius + BattlefieldSpace.BodyClearance + operation.StopDistance;
                if (operation.Kind == DisplacementKind.Charge && delta.Length() <= contact + .04f) continue;
                var desired = operation.Kind switch
                {
                    DisplacementKind.Charge => target.Position - direction * contact,
                    DisplacementKind.Knockback => target.Position + direction * operation.Distance,
                    _ => owner.Position + direction * contact
                };
                // A pull never pushes someone who is already inside its stopping circle outward.
                if ((operation.Kind is DisplacementKind.Pull or DisplacementKind.Gather) && delta.Length() <= contact + .02f)
                    continue;
                var travel = desired - mover.Position;
                if (travel.Length() > operation.Distance) desired = mover.Position + travel.Normalized() * operation.Distance;
                destination = ClipDisplacementTravel(mover, mover.Position, desired, reserved);
            }
            if (destination is not { } end || mover.Position.DistanceTo(end) < .04f ||
                !CanLandDisplacement(mover, end, reserved)) continue;
            result.Add(new DisplacementPlan(mover, target, end));
            reserved.Add(new DisplacementReservation(mover.RuntimeId, end, mover.BodyRadius));
            if (MovesCaster(operation.Kind)) break;
        }
        return result;
    }

    private Vector2? FindDisplacementLanding(CompiledDisplacementOperation operation,
        BattleUnitState owner, BattleUnitState target, IReadOnlyList<DisplacementReservation> reserved)
    {
        var towardTarget = target.Position - owner.Position;
        var direction = towardTarget.LengthSquared() > .000001f ? towardTarget.Normalized() :
            owner.Team == 0 ? Vector2.Right : Vector2.Left;
        var preferred = (operation.BehindTarget ? direction : -direction).Rotated(operation.LandingAngleRadians);
        var separation = owner.BodyRadius + target.BodyRadius + BattlefieldSpace.BodyClearance + operation.StopDistance;
        // Prefer the requested side, then nearby angular alternatives. Do not teleport to an arbitrary
        // free cell elsewhere on the board when the target has no legal nearby landing circle.
        foreach (var ringOffset in new[] { 0f, .2f, .4f })
        foreach (var sample in new[] { 0, 1, -1, 2, -2, 3, -3, 4, -4, 5, -5, 6 })
        {
            var offset = preferred.Rotated(sample * Mathf.Pi / 6f) * (separation + ringOffset);
            if (operation.BehindTarget && offset.Dot(direction) < -.001f) continue;
            var candidate = target.Position + offset;
            if (owner.Position.DistanceTo(candidate) > operation.Distance + .0001f) continue;
            if (CanLandDisplacement(owner, candidate, reserved)) return candidate;
        }
        return null;
    }

    private bool CanLandDisplacement(BattleUnitState mover, Vector2 position,
        IReadOnlyList<DisplacementReservation>? additional = null)
    {
        if (!BattlefieldSpace.IsPositionTerrainClear(position, mover.BodyRadius, Width, Height,
                cell => world.CanOccupy(cell))) return false;
        if (world.Units.Any(unit => unit.Alive && unit != mover && !IsAirborne(unit) &&
                unit.Position.DistanceTo(position) < mover.BodyRadius + unit.BodyRadius + BattlefieldSpace.BodyClearance))
            return false;
        if (CaptureDisplacementReservations().Any(reservation => reservation.RuntimeId != mover.RuntimeId &&
                reservation.Position.DistanceTo(position) < mover.BodyRadius + reservation.Radius + BattlefieldSpace.BodyClearance))
            return false;
        return additional is null || !additional.Any(reservation => reservation.RuntimeId != mover.RuntimeId &&
            reservation.Position.DistanceTo(position) < mover.BodyRadius + reservation.Radius + BattlefieldSpace.BodyClearance);
    }

    public Vector2 ClipDisplacementTravel(BattleUnitState mover, Vector2 start, Vector2 destination,
        IReadOnlyList<DisplacementReservation>? additional = null)
    {
        var delta = destination - start;
        var fraction = BattlefieldSpace.FirstTerrainHitFraction(start, delta, mover.BodyRadius, Width, Height,
            cell => world.CanOccupy(cell));
        foreach (var other in world.Units.Where(unit => unit.Alive && unit != mover && !IsAirborne(unit)))
            if (BattlefieldSpace.TryMovingCircleTimeOfImpact(start, delta, mover.BodyRadius,
                    other.Position, Vector2.Zero, other.BodyRadius, out var contact)) fraction = Math.Min(fraction, contact);
        foreach (var reservation in CaptureDisplacementReservations().Concat(additional ?? []))
            if (reservation.RuntimeId != mover.RuntimeId &&
                BattlefieldSpace.TryMovingCircleTimeOfImpact(start, delta, mover.BodyRadius,
                    reservation.Position, Vector2.Zero, reservation.Radius, out var contact)) fraction = Math.Min(fraction, contact);
        return start + delta * (fraction >= 1f ? 1f : Math.Max(0f, fraction - .001f));
    }

    public void AdvanceDisplacements(Dictionary<string, Vector2> previousPositions)
    {
        if (_displacements.IsEmpty) return;
        world.Resolve(() =>
        {
            foreach (var entry in _displacements.Values.OrderBy(motion => motion.MoverId, StringComparer.Ordinal).ToArray())
            {
                if (!_displacements.TryGetValue(entry.MoverId, out var motion) || motion.StartTick >= world.Tick) continue;
                var mover = world.Units.First(unit => unit.RuntimeId == motion.MoverId);
                var source = world.Units.First(unit => unit.RuntimeId == motion.SourceId);
                if (!mover.Alive)
                {
                    FinishDisplacement(motion with { Cancelled = true }, mover, 0);
                    continue;
                }
                var sourceInvalid = !source.Alive || source.Team != motion.SourceTeam || mover.Team != motion.MoverTeam;
                var selfControlled = MovesCaster(motion.Operation.Kind) &&
                    (mover.DisabledTicks > 0 || statuses.HasTag(mover.RuntimeId, StatusDefinitionCompiler.ActionDisabledTag));
                if (sourceInvalid || selfControlled)
                {
                    CancelDisplacement(mover.RuntimeId);
                    if (!_displacements.TryGetValue(mover.RuntimeId, out motion)) continue;
                }
                var progress = Mathf.Clamp((world.Tick - motion.StartTick) / (float)motion.Operation.DurationTicks, 0, 1);
                var next = motion.Operation.Kind == DisplacementKind.Blink ? motion.End : motion.Start.Lerp(motion.End, progress);
                var complete = progress >= 1 || motion.Operation.Kind == DisplacementKind.Blink;
                if (motion.Operation.Kind is not (DisplacementKind.Leap or DisplacementKind.Blink))
                {
                    var clipped = ClipDisplacementTravel(mover, mover.Position, next);
                    if (clipped.DistanceSquaredTo(next) > .000001f) complete = true;
                    next = clipped;
                }
                else if (complete && !CanLandDisplacement(mover, next))
                {
                    // Normal navigation, summons and other displacement sweeps honor the reservation.
                    // If an external placement still conflicts, retain the airborne identity until a
                    // legal nearby landing exists instead of creating overlapping grounded bodies.
                    var fallback = FindEmergencyLanding(mover, motion.End);
                    if (fallback is null) { EmitDisplacement(motion, mover, progress, false); continue; }
                    next = fallback.Value;
                    motion = motion with { End = next, Cancelled = true };
                    _displacements = _displacements.SetItem(mover.RuntimeId, motion);
                }
                mover.Position = next;
                mover.Mode = BattleUnitMode.Moving;
                mover.WaitingTicks = 0;
                if (motion.Operation.Kind == DisplacementKind.Blink) previousPositions[mover.RuntimeId] = next;
                world.Publish(new BattleCombatEventDraft(BattleCombatEventKind.UnitMoved, motion.Origin,
                    mover.RuntimeId, string.Empty, world.Tick, Cell: ToCombatCell(mover.Cell), Position: ToCombatPoint(next)));
                if (complete) FinishDisplacement(motion, mover, 1);
                else EmitDisplacement(motion, mover, progress, false);
            }
        });
    }

    private Vector2? FindEmergencyLanding(BattleUnitState mover, Vector2 preferred)
    {
        if (CanLandDisplacement(mover, preferred)) return preferred;
        // This is only a failure recovery for external placement; the ordinary endpoint was reserved.
        for (var ring = 1; ring <= 10; ring++)
        for (var sample = 0; sample < 16; sample++)
        {
            var candidate = preferred + Vector2.FromAngle(sample * Mathf.Tau / 16f) * ring * .2f;
            if (CanLandDisplacement(mover, candidate)) return candidate;
        }
        return null;
    }

    public void CancelDisplacement(string runtimeId)
    {
        foreach (var motion in _displacements.Values.Where(motion => motion.MoverId == runtimeId ||
                     motion.SourceId == runtimeId).ToArray())
        {
            if (!_displacements.ContainsKey(motion.MoverId)) continue;
            var mover = world.Units.First(unit => unit.RuntimeId == motion.MoverId);
            if (motion.Operation.Kind == DisplacementKind.Leap && mover.Alive)
            {
                // Removing an airborne record would instantly turn its projection into a ground
                // collider. Cancel its payoff now, finish its safe reserved descent on the same clock.
                _displacements = _displacements.SetItem(mover.RuntimeId, motion with { Cancelled = true });
            }
            else FinishDisplacement(motion with { Cancelled = true }, mover,
                Mathf.Clamp((world.Tick - motion.StartTick) / (float)motion.Operation.DurationTicks, 0, 1));
        }
    }

    private void FinishDisplacement(DisplacementMotion motion, BattleUnitState mover, float progress)
    {
        _displacements = _displacements.Remove(mover.RuntimeId);
        world.ReleaseUnit(mover.RuntimeId);
        if (mover.Alive) mover.Mode = BattleUnitMode.Seeking;
        EmitDisplacement(motion, mover, progress, true);
        var completed=false;
        try
        {
        if (motion.Cancelled || !mover.Alive) return;
        var source = world.Units.First(unit => unit.RuntimeId == motion.SourceId);
        if (!source.Alive || source.Team != motion.SourceTeam || mover.Team != motion.MoverTeam) return;
        completed=true;
        var operation = motion.Operation;
        var targets = world.Units.Where(unit => unit.Alive && unit.Team != source.Team &&
            (operation.ImpactRadius > 0
                ? unit.Position.DistanceTo(mover.Position) <= operation.ImpactRadius + .0001f
                : unit.RuntimeId == motion.TargetId &&
                  unit.Position.DistanceTo(mover.Position) <= unit.BodyRadius + mover.BodyRadius + operation.StopDistance + .2f))
            .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray();
        foreach (var target in targets)
        {
            var damage = Math.Max(0, operation.ImpactDamage + source.Damage * operation.AttackRatio);
            if (damage > 0)
            {
                var vitality = target.Health + target.Shield;
                world.Damage(source.RuntimeId, source, target, damage, motion.Origin, operation.DamageType);
                var damageClass = world.DamageClass(motion.Origin);
                if (damageClass == CombatDamageClass.ActiveSkill)
                    world.Publish(new BattleCombatEventDraft(BattleCombatEventKind.SkillHitLanded,
                        motion.Origin, source.RuntimeId, target.RuntimeId, world.Tick,
                        EffectiveValue: Math.Max(0, vitality - target.Health - target.Shield),
                        Cell: ToCombatCell(target.Cell), Position: ToCombatPoint(target.Position),
                        DamageType: operation.DamageType, DamageClass: damageClass,
                        ActionId: motion.Origin.InstanceId));
            }
            if (target.Alive && operation.ImpactStatus is { } status)
                statuses.Apply(status, source.RuntimeId, target.RuntimeId, world.Tick);
            if (target.Alive) world.Arrived(source, target, motion.Origin);
        }
        }
        finally { world.Finished(motion.Origin,completed); }
    }

    private void EmitDisplacement(DisplacementMotion motion, BattleUnitState mover, float progress, bool finished) =>
        world.Present("displacement", motion.SourceId, mover.RuntimeId, 0, mover.Position, "", origin: motion.Start,
            displacement: new BattleDisplacementCue(motion.Operation.Kind, motion.Start, motion.End,
                motion.StartTick, motion.Operation.DurationTicks, progress, finished, motion.Cancelled, motion.Operation.ArcHeight,
                motion.EffectCenter, motion.Operation.Kind == DisplacementKind.Gather &&
                    motion.Operation.TargetQuery is CompiledFilteredTargetQuery area ? area.Range : motion.Operation.ImpactRadius));
}
