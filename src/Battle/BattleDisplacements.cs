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

public sealed record DisplacementReservation(string RuntimeId, Vector2 Position, float Radius);

public sealed partial class BattleSimulation
{
    // Immutable battle-owned trajectories are included in the enclosing world checkpoint. A leap's
    // ground projection is still targetable, but its reserved landing circle is the ground obstacle.
    private sealed record DisplacementMotion(string SourceId, string MoverId, string TargetId,
        int SourceTeam, int MoverTeam, Vector2 Start, Vector2 End, Vector2 EffectCenter, int StartTick,
        CompiledDisplacementOperation Operation, CombatSourceRef Origin, bool Cancelled = false);
    private sealed record DisplacementPlan(BattleUnitState Mover, BattleUnitState Target, Vector2 End);
    private ImmutableDictionary<string, DisplacementMotion> _displacements =
        ImmutableDictionary<string, DisplacementMotion>.Empty.WithComparers(StringComparer.Ordinal);

    private bool IsDisplacing(BattleUnitState unit) => _displacements.ContainsKey(unit.RuntimeId);
    private bool IsAirborne(BattleUnitState unit) => _displacements.TryGetValue(unit.RuntimeId, out var motion) &&
        motion.Operation.Kind == DisplacementKind.Leap;

    private IReadOnlyList<DisplacementReservation> CaptureDisplacementReservations() =>
        _displacements.Values.OrderBy(motion => motion.MoverId, StringComparer.Ordinal)
            .Select(motion => new DisplacementReservation(motion.MoverId, motion.End,
                _units.First(unit => unit.RuntimeId == motion.MoverId).BodyRadius)).ToArray();

    private bool PrepareDisplacementOperation(CompiledDisplacementOperation operation, BattleUnitState owner,
        string explicitTarget, out ImmutableArray<string> targets)
    {
        targets = [];
        if (!owner.Alive || IsDisplacing(owner)) return false;
        // Exclude ineligible bodies before applying the authored target count. A nearby boss or an
        // already-moving target must not hide the next legal candidate and consume the cast.
        var expanded = operation.TargetQuery is CompiledFilteredTargetQuery filtered
            ? filtered with { MaxTargets = 0 } : operation.TargetQuery;
        var candidates = ResolveBattleTargets(expanded, owner, explicitTarget)
            .Select(id => _units.First(unit => unit.RuntimeId == id))
            .Where(target => IsDisplacementTargetEligible(operation, owner, target));
        var plans = BuildDisplacementPlans(operation, owner, candidates);
        var limit = operation.TargetQuery is CompiledFilteredTargetQuery query ? query.MaxTargets : 0;
        if (MovesCaster(operation.Kind)) limit = 1;
        targets = (limit > 0 ? plans.Take(limit) : plans).Select(plan => plan.Target.RuntimeId).ToImmutableArray();
        return !targets.IsEmpty;
    }

    private void ExecuteDisplacementOperation(CompiledDisplacementOperation operation, ImmutableArray<string> targets,
        BattleUnitState owner, CombatSourceRef origin)
    {
        var candidates = targets.Select(id => _units.FirstOrDefault(unit => unit.RuntimeId == id))
            .Where(unit => unit is not null).Select(unit => unit!);
        var plans = BuildDisplacementPlans(operation, owner, candidates);
        if (plans.Count == 0)
            throw new InvalidOperationException("Prepared displacement no longer has a legal moving body and destination.");
        foreach (var plan in plans)
        {
            var mover = plan.Mover;
            var motion = new DisplacementMotion(owner.RuntimeId, mover.RuntimeId, plan.Target.RuntimeId,
                owner.Team, mover.Team, mover.Position, plan.End, owner.Position, TickIndex, operation, origin);
            _displacements = _displacements.Add(mover.RuntimeId, motion);
            _movement?.ReleaseUnit(mover.RuntimeId);
            CancelProjectileWindups(mover);
            mover.ProjectileSequence = null;
            InterruptBattleChannels(mover.RuntimeId);
            mover.WaitingTicks = 0;
            mover.Mode = BattleUnitMode.Moving;
            if (mover == owner) SetActionTarget(owner, plan.Target);
            EmitDisplacement(motion, mover, 0, false);
        }
    }

    private static bool MovesCaster(DisplacementKind kind) =>
        kind is DisplacementKind.Charge or DisplacementKind.Leap or DisplacementKind.Blink;

    private bool IsDisplacementTargetEligible(CompiledDisplacementOperation operation,
        BattleUnitState owner, BattleUnitState target) =>
        owner.Alive && target.Alive && target != owner && target.Team != owner.Team &&
        (!operation.ExcludeBoss || !target.Definition.IsBoss) && !IsDisplacing(target) && !IsDisplacing(owner);

    private List<DisplacementPlan> BuildDisplacementPlans(CompiledDisplacementOperation operation,
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
        var preferred = operation.BehindTarget ? direction : -direction;
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
                cell => _config.FloorRule.CanOccupy(cell))) return false;
        if (_units.Any(unit => unit.Alive && unit != mover && !IsAirborne(unit) &&
                unit.Position.DistanceTo(position) < mover.BodyRadius + unit.BodyRadius + BattlefieldSpace.BodyClearance))
            return false;
        if (CaptureDisplacementReservations().Any(reservation => reservation.RuntimeId != mover.RuntimeId &&
                reservation.Position.DistanceTo(position) < mover.BodyRadius + reservation.Radius + BattlefieldSpace.BodyClearance))
            return false;
        return additional is null || !additional.Any(reservation => reservation.RuntimeId != mover.RuntimeId &&
            reservation.Position.DistanceTo(position) < mover.BodyRadius + reservation.Radius + BattlefieldSpace.BodyClearance);
    }

    private Vector2 ClipDisplacementTravel(BattleUnitState mover, Vector2 start, Vector2 destination,
        IReadOnlyList<DisplacementReservation>? additional = null)
    {
        var delta = destination - start;
        var fraction = BattlefieldSpace.FirstTerrainHitFraction(start, delta, mover.BodyRadius, Width, Height,
            cell => _config.FloorRule.CanOccupy(cell));
        foreach (var other in _units.Where(unit => unit.Alive && unit != mover && !IsAirborne(unit)))
            if (BattlefieldSpace.TryMovingCircleTimeOfImpact(start, delta, mover.BodyRadius,
                    other.Position, Vector2.Zero, other.BodyRadius, out var contact)) fraction = Math.Min(fraction, contact);
        foreach (var reservation in CaptureDisplacementReservations().Concat(additional ?? []))
            if (reservation.RuntimeId != mover.RuntimeId &&
                BattlefieldSpace.TryMovingCircleTimeOfImpact(start, delta, mover.BodyRadius,
                    reservation.Position, Vector2.Zero, reservation.Radius, out var contact)) fraction = Math.Min(fraction, contact);
        return start + delta * (fraction >= 1f ? 1f : Math.Max(0f, fraction - .001f));
    }

    private void AdvanceDisplacements(Dictionary<string, Vector2> previousPositions)
    {
        if (_displacements.IsEmpty) return;
        var checkpoint = new BattleWorldStateCheckpoint(this);
        try
        {
            using var resolution = _combatPipeline.BeginAuthoritativeResolution();
            foreach (var entry in _displacements.Values.OrderBy(motion => motion.MoverId, StringComparer.Ordinal).ToArray())
            {
                if (!_displacements.TryGetValue(entry.MoverId, out var motion) || motion.StartTick >= TickIndex) continue;
                var mover = _units.First(unit => unit.RuntimeId == motion.MoverId);
                var source = _units.First(unit => unit.RuntimeId == motion.SourceId);
                if (!mover.Alive)
                {
                    FinishDisplacement(motion with { Cancelled = true }, mover, 0);
                    continue;
                }
                var sourceInvalid = !source.Alive || source.Team != motion.SourceTeam || mover.Team != motion.MoverTeam;
                var selfControlled = MovesCaster(motion.Operation.Kind) &&
                    (mover.DisabledTicks > 0 || _statusScope.HasTag(mover.RuntimeId, StatusDefinitionCompiler.ActionDisabledTag));
                if (sourceInvalid || selfControlled)
                {
                    CancelDisplacement(mover.RuntimeId);
                    if (!_displacements.TryGetValue(mover.RuntimeId, out motion)) continue;
                }
                var progress = Mathf.Clamp((TickIndex - motion.StartTick) / (float)motion.Operation.DurationTicks, 0, 1);
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
                PublishCombat(new BattleCombatEventDraft(BattleCombatEventKind.UnitMoved, motion.Origin,
                    mover.RuntimeId, string.Empty, TickIndex, Cell: ToCombatCell(mover.Cell), Position: ToCombatPoint(next)));
                if (complete) FinishDisplacement(motion, mover, 1);
                else EmitDisplacement(motion, mover, progress, false);
            }
            resolution.Commit();
            checkpoint.Commit();
        }
        catch { checkpoint.Rollback(); throw; }
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

    private void CancelDisplacement(string runtimeId)
    {
        foreach (var motion in _displacements.Values.Where(motion => motion.MoverId == runtimeId ||
                     motion.SourceId == runtimeId).ToArray())
        {
            if (!_displacements.ContainsKey(motion.MoverId)) continue;
            var mover = _units.First(unit => unit.RuntimeId == motion.MoverId);
            if (motion.Operation.Kind == DisplacementKind.Leap && mover.Alive)
            {
                // Removing an airborne record would instantly turn its projection into a ground
                // collider. Cancel its payoff now, finish its safe reserved descent on the same clock.
                _displacements = _displacements.SetItem(mover.RuntimeId, motion with { Cancelled = true });
            }
            else FinishDisplacement(motion with { Cancelled = true }, mover,
                Mathf.Clamp((TickIndex - motion.StartTick) / (float)motion.Operation.DurationTicks, 0, 1));
        }
    }

    private void FinishDisplacement(DisplacementMotion motion, BattleUnitState mover, float progress)
    {
        _displacements = _displacements.Remove(mover.RuntimeId);
        _movement?.ReleaseUnit(mover.RuntimeId);
        if (mover.Alive) mover.Mode = BattleUnitMode.Seeking;
        EmitDisplacement(motion, mover, progress, true);
        if (motion.Cancelled || !mover.Alive) return;
        var source = _units.First(unit => unit.RuntimeId == motion.SourceId);
        if (!source.Alive || source.Team != motion.SourceTeam || mover.Team != motion.MoverTeam) return;
        var operation = motion.Operation;
        var targets = _units.Where(unit => unit.Alive && unit.Team != source.Team &&
            (operation.ImpactRadius > 0
                ? unit.Position.DistanceTo(mover.Position) <= operation.ImpactRadius + .0001f
                : unit.RuntimeId == motion.TargetId &&
                  unit.Position.DistanceTo(mover.Position) <= unit.BodyRadius + mover.BodyRadius + operation.StopDistance + .2f))
            .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray();
        foreach (var target in targets)
        {
            var damage = Math.Max(0, operation.ImpactDamage + source.Damage * operation.AttackRatio);
            if (damage > 0) ApplyDamage(source.RuntimeId, source, target, damage, motion.Origin, operation.DamageType);
            if (target.Alive && operation.ImpactStatus is { } status)
                _statusScope.Apply(status, source.RuntimeId, target.RuntimeId, TickIndex);
        }
    }

    private void EmitDisplacement(DisplacementMotion motion, BattleUnitState mover, float progress, bool finished) =>
        Emit("displacement", motion.SourceId, mover.RuntimeId, 0, mover.Position, "", origin: motion.Start,
            displacement: new BattleDisplacementCue(motion.Operation.Kind, motion.Start, motion.End,
                motion.StartTick, motion.Operation.DurationTicks, progress, finished, motion.Cancelled, motion.Operation.ArcHeight,
                motion.EffectCenter, motion.Operation.Kind == DisplacementKind.Gather &&
                    motion.Operation.TargetQuery is CompiledFilteredTargetQuery area ? area.Range : motion.Operation.ImpactRadius));
}
