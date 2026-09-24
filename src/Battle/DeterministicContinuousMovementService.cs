using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TowerAutobattler.Battle;

public interface IContinuousMovementService : IDisposable
{
    void BeginTick();
    BattleUnitState? SelectTarget(BattleUnitState mover, IReadOnlyList<BattleUnitState> orderedCandidates, float? centerRange = null);
    bool QueueMove(BattleUnitState mover);
    void ResolveIntents(Action<BattleUnitState, Vector2> moved);
    void ReleaseUnit(string runtimeId);
    void ReleaseTarget(string runtimeId);
    void ReleaseGoal(string runtimeId);
    void ClearTarget(string runtimeId);
    bool IsPositionReserved(Vector2 position, float radius, string ignoredRuntimeId = "");
}

/// <summary>
/// Deterministic fixed-tick movement authority for continuous logical battle space. Authored cells are
/// used only as coarse terrain route samples; committed positions and engagement goals are never quantized.
/// </summary>
public sealed class DeterministicContinuousMovementService : IContinuousMovementService
{
    internal const int GoalWaitLease = 6;
    internal const float AttackStoppingMargin = .08f;
    private const int EngagementSamples = 12;
    private const int StagingRings = 3;
    private const float GoalClearance = .04f;
    private const float MovementEpsilon = .0005f;
    private static readonly Vector2I[] RouteDirections =
    [
        Vector2I.Right, Vector2I.Left, Vector2I.Down, Vector2I.Up,
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
    ];

    private readonly int _width;
    private readonly int _height;
    private readonly Func<IReadOnlyList<BattleUnitState>> _units;
    private readonly Func<Vector2I, bool> _terrainAllows;
    private readonly Func<Vector2, Vector2, bool> _hasLineAccess;
    private readonly Func<BattleUnitState, bool> _isGrounded;
    private readonly Func<IReadOnlyList<DisplacementReservation>> _landingReservations;
    private readonly ulong _seed;
    private readonly Dictionary<string, string> _targetByUnit = new(StringComparer.Ordinal);
    // Skills can use center distance while basic attacks use body-edge reach.
    private readonly Dictionary<string, float> _centerRangeByUnit = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Vector2> _goalByUnit = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _retargetFromByUnit = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BattleUnitState> _snapshotById = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Vector2> _snapshotPositionById = new(StringComparer.Ordinal);
    private readonly List<Vector2I> _blockedTerrainCells = [];
    private readonly Dictionary<RouteOrigin, RouteSearch> _routeSearchByOrigin = [];
    private readonly List<MoveRequest> _requests = [];

    public int ActiveGoalCount => _goalByUnit.Count;
    public int PendingRequestCount => _requests.Count;
    internal int RetargetLeaseCount => _retargetFromByUnit.Count;
    internal int PlanningStateCount =>
        _targetByUnit.Count + _centerRangeByUnit.Count + _goalByUnit.Count + _requests.Count + _retargetFromByUnit.Count;
    internal bool HasPlanningState(string runtimeId) =>
        _targetByUnit.ContainsKey(runtimeId) || _targetByUnit.ContainsValue(runtimeId) ||
        _goalByUnit.ContainsKey(runtimeId) || _centerRangeByUnit.ContainsKey(runtimeId) ||
        _retargetFromByUnit.ContainsKey(runtimeId) || _retargetFromByUnit.ContainsValue(runtimeId) ||
        _requests.Any(request => request.Mover.RuntimeId == runtimeId || request.TargetRuntimeId == runtimeId) ||
        _snapshotById.ContainsKey(runtimeId);

    public DeterministicContinuousMovementService(
        int width,
        int height,
        Func<IReadOnlyList<BattleUnitState>> units,
        Func<Vector2I, bool> terrainAllows,
        Func<Vector2, Vector2, bool> hasLineAccess,
        ulong seed = 0,
        Func<BattleUnitState, bool>? isGrounded = null,
        Func<IReadOnlyList<DisplacementReservation>>? landingReservations = null)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        _width = width;
        _height = height;
        _units = units ?? throw new ArgumentNullException(nameof(units));
        _terrainAllows = terrainAllows ?? throw new ArgumentNullException(nameof(terrainAllows));
        _hasLineAccess = hasLineAccess ?? throw new ArgumentNullException(nameof(hasLineAccess));
        _seed = seed;
        _isGrounded = isGrounded ?? (_ => true);
        _landingReservations = landingReservations ?? (() => Array.Empty<DisplacementReservation>());
    }

    internal MovementStateCheckpoint CaptureState() => new(this);

    internal void RestoreState(MovementStateCheckpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        checkpoint.Restore(this);
    }

    public void BeginTick()
    {
        _requests.Clear();
        _snapshotById.Clear();
        _snapshotPositionById.Clear();
        _blockedTerrainCells.Clear();
        _routeSearchByOrigin.Clear();
        for (var y = 0; y < _height; y++)
        for (var x = 0; x < _width; x++)
        {
            var cell = new Vector2I(x, y);
            var allowed = _terrainAllows(cell);
            if (!allowed) _blockedTerrainCells.Add(cell);
        }
        foreach (var unit in _units().Where(unit => unit.Alive).OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal))
        {
            _snapshotById[unit.RuntimeId] = unit;
            _snapshotPositionById[unit.RuntimeId] = unit.Position;
        }

        foreach (var runtimeId in _targetByUnit.Keys.ToArray())
            if (!_snapshotById.ContainsKey(runtimeId) || !_snapshotById.ContainsKey(_targetByUnit[runtimeId]))
                ClearTarget(runtimeId);
        foreach (var runtimeId in _retargetFromByUnit.Keys.ToArray())
            if (!_snapshotById.ContainsKey(runtimeId) || !_snapshotById.ContainsKey(_retargetFromByUnit[runtimeId]))
                _retargetFromByUnit.Remove(runtimeId);
        foreach (var runtimeId in _goalByUnit.Keys.ToArray())
        {
            if (!_snapshotById.TryGetValue(runtimeId, out var mover) ||
                !_targetByUnit.TryGetValue(runtimeId, out var targetId) ||
                !_snapshotById.TryGetValue(targetId, out var target) ||
                mover.WaitingTicks >= GoalWaitLease ||
                !IsGoalStillUseful(mover, target, _goalByUnit[runtimeId]))
                ReleaseGoal(runtimeId);
        }
    }

    public BattleUnitState? SelectTarget(BattleUnitState mover, IReadOnlyList<BattleUnitState> orderedCandidates, float? centerRange = null)
    {
        if (centerRange is { } range && (!float.IsFinite(range) || range <= 0))
            throw new ArgumentOutOfRangeException(nameof(centerRange));
        float? previousRange = _centerRangeByUnit.TryGetValue(mover.RuntimeId, out var previous) ? previous : null;
        if (previousRange != centerRange)
        {
            ReleaseGoal(mover.RuntimeId);
            _requests.RemoveAll(request => request.Mover.RuntimeId == mover.RuntimeId);
            _retargetFromByUnit.Remove(mover.RuntimeId);
            mover.WaitingTicks = 0;
        }
        if (centerRange is { } actionRange) _centerRangeByUnit[mover.RuntimeId] = actionRange;
        else _centerRangeByUnit.Remove(mover.RuntimeId);
        var legalCandidates = orderedCandidates
            .Where(candidate => candidate.Alive && candidate.RuntimeId != mover.RuntimeId)
            .DistinctBy(candidate => candidate.RuntimeId)
            .ToArray();
        if (legalCandidates.Length == 0)
        {
            ClearTarget(mover.RuntimeId);
            return null;
        }

        var immediateTarget = legalCandidates.FirstOrDefault(candidate => CanActFrom(mover, candidate, mover.Position));
        if (_targetByUnit.TryGetValue(mover.RuntimeId, out var currentTargetId))
        {
            var stableTarget = legalCandidates.FirstOrDefault(candidate => candidate.RuntimeId == currentTargetId);
            if (stableTarget is not null && CanActFrom(mover, stableTarget, mover.Position))
                return AcceptTarget(mover, stableTarget, true);
            if (immediateTarget is not null)
                return AcceptTarget(mover, immediateTarget, true);
            if (stableTarget is not null &&
                (!_retargetFromByUnit.TryGetValue(mover.RuntimeId, out var blockedId) || blockedId != currentTargetId) &&
                ScoreTarget(mover, stableTarget, 0) is not null)
                return AcceptTarget(mover, stableTarget, false);
        }
        else if (immediateTarget is not null)
            return AcceptTarget(mover, immediateTarget, true);

        var candidates = legalCandidates
            .Select((candidate, authoredRank) => ScoreTarget(mover, candidate, authoredRank))
            .Where(score => score is not null)
            .Select(score => score!)
            .ToArray();
        if (candidates.Length == 0)
        {
            ClearTarget(mover.RuntimeId);
            return null;
        }

        var immediate = candidates.Where(score => score.ImmediatelyActionable).ToArray();
        IReadOnlyList<TargetScore> pool = immediate.Length > 0 ? immediate : candidates;
        if (immediate.Length == 0 &&
            _retargetFromByUnit.TryGetValue(mover.RuntimeId, out var blockedTargetId))
        {
            var alternatives = candidates.Where(score => score.Target.RuntimeId != blockedTargetId).ToArray();
            if (alternatives.Length > 0) pool = alternatives;
        }

        var best = pool.OrderBy(score => score.PathCost)
            .ThenBy(score => score.AuthoredRank)
            .ThenBy(score => score.TieBreak)
            .First();
        if (_targetByUnit.TryGetValue(mover.RuntimeId, out var stableId))
        {
            var stable = pool.FirstOrDefault(score => score.Target.RuntimeId == stableId);
            if (stable is not null && stable.PathCost <= best.PathCost + 1f) best = stable;
        }

        return AcceptTarget(mover, best.Target, best.ImmediatelyActionable);
    }

    private BattleUnitState AcceptTarget(BattleUnitState mover, BattleUnitState target, bool immediatelyActionable)
    {
        if (!_targetByUnit.TryGetValue(mover.RuntimeId, out var previous) || previous != target.RuntimeId)
        {
            ReleaseGoal(mover.RuntimeId);
            mover.WaitingTicks = 0;
        }
        _targetByUnit[mover.RuntimeId] = target.RuntimeId;
        if (immediatelyActionable ||
            _retargetFromByUnit.TryGetValue(mover.RuntimeId, out var avoided) && avoided != target.RuntimeId)
            _retargetFromByUnit.Remove(mover.RuntimeId);
        return target;
    }

    public bool QueueMove(BattleUnitState mover)
    {
        if (!mover.Alive || !_isGrounded(mover) || !_targetByUnit.TryGetValue(mover.RuntimeId, out var targetId))
        {
            MarkWaiting(mover);
            return false;
        }
        _requests.RemoveAll(request => request.Mover.RuntimeId == mover.RuntimeId);
        _requests.Add(new MoveRequest(mover, targetId));
        return true;
    }

    public void ResolveIntents(Action<BattleUnitState, Vector2> moved)
    {
        ArgumentNullException.ThrowIfNull(moved);
        // Summons may be committed after BeginTick. They do not move until the next tick, but they
        // become authoritative body obstacles immediately for this tick's goal and sweep resolution.
        foreach (var unit in _units().Where(unit => unit.Alive)
                     .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal))
        {
            if (_snapshotById.ContainsKey(unit.RuntimeId)) continue;
            _snapshotById.Add(unit.RuntimeId, unit);
            _snapshotPositionById.Add(unit.RuntimeId, unit.Position);
        }
        // Casts and summons may change grounded bodies or landing reservations after target selection.
        _routeSearchByOrigin.Clear();
        var liveRequests = _requests
            .Where(request => request.Mover.Alive && _isGrounded(request.Mover) &&
                              _targetByUnit.GetValueOrDefault(request.Mover.RuntimeId) == request.TargetRuntimeId &&
                              _snapshotById.ContainsKey(request.TargetRuntimeId))
            .OrderByDescending(request => request.Mover.WaitingTicks)
            .ThenBy(request => Initiative(request.Mover.RuntimeId))
            .ThenBy(request => request.Mover.RuntimeId, StringComparer.Ordinal)
            .ToArray();
        var requestIds = liveRequests.Select(request => request.Mover.RuntimeId).ToHashSet(StringComparer.Ordinal);
        var reservations = _goalByUnit
            .Where(pair => !requestIds.Contains(pair.Key) && _snapshotById.ContainsKey(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var retainedGoals = _goalByUnit
            .Where(pair => requestIds.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        foreach (var request in liveRequests) ReleaseGoal(request.Mover.RuntimeId);

        var goals = new Dictionary<string, Vector2>(StringComparer.Ordinal);
        foreach (var request in liveRequests)
        {
            var mover = request.Mover;
            var target = _snapshotById[request.TargetRuntimeId];
            var retainedGoal = retainedGoals.TryGetValue(mover.RuntimeId, out var existingGoal)
                ? existingGoal
                : (Vector2?)null;
            var preferred = SelectGoal(
                mover,
                target,
                reservations,
                retainedGoal);
            if (preferred is null)
            {
                MarkWaitingAndMaybeRetarget(mover, request.TargetRuntimeId);
                continue;
            }
            goals[mover.RuntimeId] = preferred.Value;
            reservations[mover.RuntimeId] = preferred.Value;
            _goalByUnit[mover.RuntimeId] = preferred.Value;
        }

        var deltas = new Dictionary<string, Vector2>(StringComparer.Ordinal);
        foreach (var request in liveRequests)
        {
            if (!goals.TryGetValue(request.Mover.RuntimeId, out var goal)) continue;
            var mover = request.Mover;
            var start = _snapshotPositionById[mover.RuntimeId];
            var route = FindTravelRoute(mover, start, goal);
            if (route is null)
            {
                MarkWaitingAndMaybeRetarget(mover, request.TargetRuntimeId);
                continue;
            }
            var waypoint = route.Points.Count > 0 ? route.Points[0] : goal;
            var desired = waypoint - start;
            if (desired.LengthSquared() <= MovementEpsilon * MovementEpsilon)
                desired = goal - start;
            if (desired.LengthSquared() <= MovementEpsilon * MovementEpsilon)
            {
                MarkWaitingAndMaybeRetarget(mover, request.TargetRuntimeId);
                continue;
            }
            // A body-clear route already chooses a side around the whole obstruction.
            // Local repulsion must not steer it back into that obstruction or a terrain corner.
            if (!route.AvoidsBodies) desired = AddLocalAvoidance(mover, desired);
            var maximumDistance = Math.Min(1f, 1f / Math.Max(1, mover.EffectiveMoveTicks));
            var delta = desired.Normalized() * Math.Min(maximumDistance, desired.Length());
            var terrainFraction = BattlefieldSpace.FirstTerrainHitFraction(
                start, delta, mover.BodyRadius, _width, _height, _blockedTerrainCells);
            var safeTerrainFraction = terrainFraction >= 1f ? 1f : Math.Max(0f, terrainFraction - .001f);
            deltas[mover.RuntimeId] = delta * safeTerrainFraction;
        }

        ResolveBodySweeps(deltas);
        foreach (var request in liveRequests)
        {
            var mover = request.Mover;
            if (!deltas.TryGetValue(mover.RuntimeId, out var delta) ||
                delta.LengthSquared() <= MovementEpsilon * MovementEpsilon)
            {
                if (goals.ContainsKey(mover.RuntimeId)) MarkWaitingAndMaybeRetarget(mover, request.TargetRuntimeId);
                continue;
            }
            mover.Position = _snapshotPositionById[mover.RuntimeId] + delta;
            mover.Mode = BattleUnitMode.Moving;
            mover.WaitingTicks = 0;
            _retargetFromByUnit.Remove(mover.RuntimeId);
        }
        foreach (var request in liveRequests)
            if (deltas.TryGetValue(request.Mover.RuntimeId, out var delta) &&
                delta.LengthSquared() > MovementEpsilon * MovementEpsilon)
                moved(request.Mover, request.Mover.Position);
        _requests.Clear();
    }

    public void ReleaseUnit(string runtimeId)
    {
        ClearTarget(runtimeId);
        ReleaseTarget(runtimeId);
        _retargetFromByUnit.Remove(runtimeId);
        _requests.RemoveAll(request => request.Mover.RuntimeId == runtimeId || request.TargetRuntimeId == runtimeId);
        _snapshotById.Remove(runtimeId);
        _snapshotPositionById.Remove(runtimeId);
        _routeSearchByOrigin.Clear();
    }

    public void ReleaseTarget(string runtimeId)
    {
        foreach (var moverId in _targetByUnit.Where(pair => pair.Value == runtimeId).Select(pair => pair.Key).ToArray())
            ClearTarget(moverId);
    }

    public void ReleaseGoal(string runtimeId) => _goalByUnit.Remove(runtimeId);

    public void ClearTarget(string runtimeId)
    {
        _targetByUnit.Remove(runtimeId);
        _centerRangeByUnit.Remove(runtimeId);
        _retargetFromByUnit.Remove(runtimeId);
        ReleaseGoal(runtimeId);
        _requests.RemoveAll(request => request.Mover.RuntimeId == runtimeId);
    }

    public bool IsPositionReserved(Vector2 position, float radius, string ignoredRuntimeId = "")
    {
        if (IsLandingReserved(position, radius, ignoredRuntimeId)) return true;
        foreach (var pair in _goalByUnit)
        {
            if (pair.Key == ignoredRuntimeId || !_snapshotById.TryGetValue(pair.Key, out var owner)) continue;
            if (position.DistanceTo(pair.Value) < radius + owner.BodyRadius + GoalClearance) return true;
        }
        return false;
    }

    private bool IsLandingReserved(Vector2 position, float radius, string ignoredRuntimeId) =>
        _landingReservations().Any(reservation => reservation.RuntimeId != ignoredRuntimeId &&
            position.DistanceTo(reservation.Position) < radius + reservation.Radius + GoalClearance);

    public void Dispose()
    {
        _requests.Clear();
        _targetByUnit.Clear();
        _centerRangeByUnit.Clear();
        _goalByUnit.Clear();
        _retargetFromByUnit.Clear();
        _snapshotById.Clear();
        _snapshotPositionById.Clear();
        _blockedTerrainCells.Clear();
        _routeSearchByOrigin.Clear();
    }

    private TargetScore? ScoreTarget(BattleUnitState mover, BattleUnitState target, int authoredRank)
    {
        if (CanActFrom(mover, target, mover.Position))
            return new TargetScore(target, 0f, authoredRank, StableHash(target.RuntimeId), true);
        var best = EngagementPositions(mover, target, 0)
            .Where(position => CanActFrom(mover, target, position))
            .Select(position => FindTravelRoute(mover, mover.Position, position)?.Cost ?? float.PositiveInfinity)
            .DefaultIfEmpty(float.PositiveInfinity)
            .Min();
        if (float.IsPositiveInfinity(best))
            best = FallbackEngagementPositions(mover, target)
                .Select(position => FindTravelRoute(mover, mover.Position, position)?.Cost ?? float.PositiveInfinity)
                .DefaultIfEmpty(float.PositiveInfinity)
                .Min();
        return float.IsPositiveInfinity(best)
            ? null
            : new TargetScore(target, best, authoredRank, StableHash(target.RuntimeId), false);
    }

    private Vector2? SelectGoal(
        BattleUnitState mover,
        BattleUnitState target,
        IReadOnlyDictionary<string, Vector2> reservations,
        Vector2? retainedGoal)
    {
        if (retainedGoal is { } retained &&
            IsGoalStillUseful(mover, target, retained) &&
            IsGoalAvailable(mover, target, retained, reservations) &&
            FindRoute(mover.Position, retained, mover.BodyRadius, mover.RuntimeId) is not null)
            return retained;

        var attackGoal = SelectAvailableGoal(mover, target, reservations,
            EngagementPositions(mover, target, 0).Where(position => CanActFrom(mover, target, position)));
        if (attackGoal is not null) return attackGoal;

        // A long attack radius can put every outer-ring sample outside the arena or behind a
        // wall, even though a closer firing position is reachable through a side lane. Search
        // the same fallback used by target scoring before settling for an out-of-range queue.
        attackGoal = SelectAvailableGoal(mover, target, reservations, FallbackEngagementPositions(mover, target));
        if (attackGoal is not null) return attackGoal;

        for (var ring = 1; ring <= StagingRings; ring++)
        {
            var stagingGoal = SelectAvailableGoal(mover, target, reservations, EngagementPositions(mover, target, ring));
            if (stagingGoal is not null) return stagingGoal;
        }
        return null;
    }

    private Vector2? SelectAvailableGoal(
        BattleUnitState mover,
        BattleUnitState target,
        IReadOnlyDictionary<string, Vector2> reservations,
        IEnumerable<Vector2> positions) =>
        positions
            .Where(position => IsGoalAvailable(mover, target, position, reservations))
            .Select(position => (Position: position, Route: FindTravelRoute(mover, mover.Position, position)))
            .Where(choice => choice.Route is not null)
            .OrderByDescending(choice => choice.Route!.AvoidsBodies)
            .ThenBy(choice => choice.Route!.Cost)
            .ThenBy(choice => PositionTieBreak(mover.RuntimeId, choice.Position))
            .Select(choice => (Vector2?)choice.Position)
            .FirstOrDefault();

    private IEnumerable<Vector2> FallbackEngagementPositions(BattleUnitState mover, BattleUnitState target)
    {
        // These are deterministic route samples, not a return to cell-quantized movement.
        // Geometry, body avoidance, reservations and continuous sweeps remain authoritative.
        for (var y = 0; y < _height; y++)
        for (var x = 0; x < _width; x++)
        {
            var position = BattlefieldSpace.CellCenter(new Vector2I(x, y));
            if (IsTerrainClear(position, mover.BodyRadius) && CanActFrom(mover, target, position))
                yield return position;
        }
    }

    private IEnumerable<Vector2> EngagementPositions(BattleUnitState mover, BattleUnitState target, int stagingRing)
    {
        var contactDistance = mover.BodyRadius + target.BodyRadius + BattlefieldSpace.BodyClearance;
        var maximumAttackDistance = Math.Max(contactDistance, ActionCenterRange(mover, target) - AttackStoppingMargin);
        var outerDistance = maximumAttackDistance + stagingRing * (mover.BodyRadius * 2f + GoalClearance);
        var phase = (StableHash($"{_seed}|{mover.RuntimeId}|{target.RuntimeId}") % EngagementSamples) /
                    (float)EngagementSamples * Mathf.Tau;
        var towardMover = mover.Position - target.Position;
        var directAngle = towardMover.LengthSquared() > MovementEpsilon * MovementEpsilon
            ? towardMover.Angle()
            : phase;
        var distances = stagingRing > 0
            ? new[] { outerDistance }
            : new[] { maximumAttackDistance, (maximumAttackDistance + contactDistance) * .5f, contactDistance };
        foreach (var centerDistance in distances.Distinct())
        {
            var direct = target.Position + Vector2.FromAngle(directAngle) * centerDistance;
            if (IsTerrainClear(direct, mover.BodyRadius)) yield return direct;
        }
        for (var index = 0; index < EngagementSamples; index++)
        {
            var angle = phase + index * Mathf.Tau / EngagementSamples;
            var position = target.Position + Vector2.FromAngle(angle) * outerDistance;
            if (IsTerrainClear(position, mover.BodyRadius)) yield return position;
        }
    }

    private bool IsGoalAvailable(
        BattleUnitState mover,
        BattleUnitState target,
        Vector2 position,
        IReadOnlyDictionary<string, Vector2> reservations)
    {
        if (!IsTerrainClear(position, mover.BodyRadius) || IsLandingReserved(position, mover.BodyRadius, mover.RuntimeId)) return false;
        foreach (var pair in reservations)
        {
            if (pair.Key == mover.RuntimeId || !_snapshotById.TryGetValue(pair.Key, out var owner)) continue;
            if (position.DistanceTo(pair.Value) < mover.BodyRadius + owner.BodyRadius + GoalClearance) return false;
        }
        foreach (var other in _snapshotById.Values)
        {
            if (!other.Alive || !_isGrounded(other) || other.RuntimeId == mover.RuntimeId || other.RuntimeId == target.RuntimeId) continue;
            var otherPosition = _snapshotPositionById[other.RuntimeId];
            if (position.DistanceTo(otherPosition) < mover.BodyRadius + other.BodyRadius + GoalClearance) return false;
        }
        return true;
    }

    private bool IsGoalStillUseful(BattleUnitState mover, BattleUnitState target, Vector2 goal)
    {
        if (!IsTerrainClear(goal, mover.BodyRadius) || IsLandingReserved(goal, mover.BodyRadius, mover.RuntimeId)) return false;
        return goal.DistanceTo(target.Position) <= ActionCenterRange(mover, target) + .001f && HasActionLineAccess(mover, target, goal);
    }

    private bool CanActFrom(BattleUnitState mover, BattleUnitState target, Vector2 position)
    {
        return position.DistanceTo(target.Position) <= ActionCenterRange(mover, target) + .0001f && HasActionLineAccess(mover, target, position);
    }

    private float ActionCenterRange(BattleUnitState mover, BattleUnitState target) =>
        _centerRangeByUnit.TryGetValue(mover.RuntimeId, out var range) ? range :
            mover.AttackRange + mover.BodyRadius + target.BodyRadius;

    private bool HasActionLineAccess(BattleUnitState mover, BattleUnitState target, Vector2 position)
    {
        if (!_hasLineAccess(position, target.Position)) return false;
        // A clear center ray is insufficient for a physical arrow at a wall corner. Match
        // hostile projectile execution, while preserving the supplied ray rule for healing
        // and instant attacks; a healer's projectile art does not change healing access.
        if (mover.Team == target.Team || mover.AttackDelivery != TowerAutobattler.Content.AttackDelivery.Projectile)
            return true;
        return BattlefieldSpace.IsSegmentTerrainClear(position, target.Position,
            Math.Max(0f, mover.Definition.ProjectileRadius), _width, _height, _blockedTerrainCells);
    }

    // Bodies are snapshot obstacles, not permanent terrain. If traffic seals every route,
    // retain the terrain plan so swept following/waiting can resume when the lane opens.
    private Route? FindTravelRoute(BattleUnitState mover, Vector2 start, Vector2 goal) =>
        FindRoute(start, goal, mover.BodyRadius, mover.RuntimeId) ?? FindRoute(start, goal, mover.BodyRadius);

    private Route? FindRoute(Vector2 start, Vector2 goal, float radius, string? moverId = null)
    {
        if (!IsTerrainClear(start, radius) || !IsRoutePositionClear(goal, radius, moverId)) return null;
        if (IsRouteSegmentClear(start, goal, radius, moverId))
            return new Route([goal], start.DistanceTo(goal), moverId is not null);
        var origin = new RouteOrigin(start, radius, moverId);
        if (!_routeSearchByOrigin.TryGetValue(origin, out var search))
        {
            search = BuildRouteSearch(start, radius, moverId);
            _routeSearchByOrigin.Add(origin, search);
        }
        var orderedNodes = search.Nodes
            .Where(cell => !float.IsPositiveInfinity(search.Distance[cell]))
            .OrderBy(cell => BattlefieldSpace.CellCenter(cell).DistanceSquaredTo(goal))
            .ThenBy(cell => CellTieBreak(cell))
            .ToArray();
        var reached = FindGoalVisibleNode(orderedNodes.Take(12), search, goal, radius, moverId);
        if (reached.X == int.MinValue)
            reached = FindGoalVisibleNode(orderedNodes.Skip(12), search, goal, radius, moverId);
        if (reached.X == int.MinValue) return null;
        var path = new List<Vector2> { BattlefieldSpace.CellCenter(reached) };
        while (search.Previous.TryGetValue(reached, out var parent))
        {
            reached = parent;
            path.Add(BattlefieldSpace.CellCenter(parent));
        }
        path.Reverse();
        path.Add(goal);
        for (var index = path.Count - 1; index > 0; index--)
            if (IsRouteSegmentClear(start, path[index], radius, moverId))
            {
                path.RemoveRange(0, index);
                break;
            }
        var cost = start.DistanceTo(path[0]);
        for (var index = 1; index < path.Count; index++) cost += path[index - 1].DistanceTo(path[index]);
        return new Route(path, cost, moverId is not null);
    }

    private Vector2I FindGoalVisibleNode(
        IEnumerable<Vector2I> candidates,
        RouteSearch search,
        Vector2 goal,
        float radius,
        string? moverId) =>
        candidates
            .Where(cell => IsRouteSegmentClear(BattlefieldSpace.CellCenter(cell), goal, radius, moverId))
            .OrderBy(cell => search.Distance[cell] + BattlefieldSpace.CellCenter(cell).DistanceTo(goal))
            .ThenBy(cell => CellTieBreak(cell))
            .FirstOrDefault(new Vector2I(int.MinValue, int.MinValue));

    private RouteSearch BuildRouteSearch(Vector2 start, float radius, string? moverId)
    {
        var nodes = new List<Vector2I>();
        for (var y = 0; y < _height; y++)
        for (var x = 0; x < _width; x++)
        {
            var cell = new Vector2I(x, y);
            if (IsRoutePositionClear(BattlefieldSpace.CellCenter(cell), radius, moverId)) nodes.Add(cell);
        }
        var distance = nodes.ToDictionary(cell => cell, _ => float.PositiveInfinity);
        var previous = new Dictionary<Vector2I, Vector2I>();
        var open = new HashSet<Vector2I>();
        foreach (var cell in nodes.Where(cell => IsRouteSegmentClear(
                     start, BattlefieldSpace.CellCenter(cell), radius, moverId))
                 .OrderBy(cell => start.DistanceSquaredTo(BattlefieldSpace.CellCenter(cell)))
                 .ThenBy(cell => CellTieBreak(cell))
                 .Take(8))
        {
            distance[cell] = start.DistanceTo(BattlefieldSpace.CellCenter(cell));
            open.Add(cell);
        }
        while (open.Count > 0)
        {
            var current = open.OrderBy(cell => distance[cell]).ThenBy(cell => CellTieBreak(cell)).First();
            open.Remove(current);
            foreach (var direction in RouteDirections)
            {
                var next = current + direction;
                if (!distance.ContainsKey(next)) continue;
                var currentPoint = BattlefieldSpace.CellCenter(current);
                var nextPoint = BattlefieldSpace.CellCenter(next);
                if (!IsRouteSegmentClear(currentPoint, nextPoint, radius, moverId)) continue;
                var candidate = distance[current] + currentPoint.DistanceTo(nextPoint);
                if (candidate + .0001f >= distance[next]) continue;
                distance[next] = candidate;
                previous[next] = current;
                open.Add(next);
            }
        }
        return new RouteSearch(nodes, distance, previous);
    }

    private bool IsRoutePositionClear(Vector2 position, float radius, string? moverId)
    {
        if (!IsTerrainClear(position, radius)) return false;
        if (moverId is null) return true;
        if (IsLandingReserved(position, radius, moverId)) return false;
        return !_snapshotById.Values.Any(other => other.Alive && _isGrounded(other) && other.RuntimeId != moverId &&
            position.DistanceTo(_snapshotPositionById[other.RuntimeId]) < radius + other.BodyRadius + BattlefieldSpace.BodyClearance);
    }

    private bool IsRouteSegmentClear(Vector2 start, Vector2 end, float radius, string? moverId)
    {
        if (!BattlefieldSpace.IsSegmentTerrainClear(start, end, radius, _width, _height, _blockedTerrainCells)) return false;
        if (moverId is null) return true;
        foreach (var other in _snapshotById.Values)
        {
            if (!other.Alive || !_isGrounded(other) || other.RuntimeId == moverId) continue;
            if (BattlefieldSpace.TryMovingCircleTimeOfImpact(start, end - start, radius,
                    _snapshotPositionById[other.RuntimeId], Vector2.Zero, other.BodyRadius, out _)) return false;
        }
        foreach (var reservation in _landingReservations())
            if (reservation.RuntimeId != moverId && BattlefieldSpace.TryMovingCircleTimeOfImpact(
                    start, end - start, radius, reservation.Position, Vector2.Zero, reservation.Radius, out _)) return false;
        return true;
    }

    private Vector2 AddLocalAvoidance(BattleUnitState mover, Vector2 desired)
    {
        var direction = desired.Normalized();
        var result = direction;
        var start = _snapshotPositionById[mover.RuntimeId];
        foreach (var other in _snapshotById.Values.OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal))
        {
            if (!other.Alive || !_isGrounded(other) || other.RuntimeId == mover.RuntimeId) continue;
            var offset = _snapshotPositionById[other.RuntimeId] - start;
            var distance = offset.Length();
            if (distance <= .0001f || distance > 1.75f) continue;
            var combined = mover.BodyRadius + other.BodyRadius + GoalClearance;
            var ahead = direction.Dot(offset / distance);
            if (distance < combined + .35f && ahead > -.2f)
            {
                var side = PairSide(mover.RuntimeId, other.RuntimeId);
                var tangent = new Vector2(-direction.Y, direction.X) * side;
                result += tangent * (.85f * (combined + .35f - distance) / .35f);
            }
            if (distance < combined + .12f)
                result -= offset.Normalized() * ((combined + .12f - distance) / .12f);
        }
        return result.LengthSquared() <= .000001f ? desired : result.Normalized() * desired.Length();
    }

    private void ResolveBodySweeps(Dictionary<string, Vector2> deltas)
    {
        // Commit discards sub-epsilon movement. Sweeps must use those same zero deltas,
        // otherwise a follower can consume clearance that its leader never actually frees.
        foreach (var runtimeId in deltas.Keys.ToArray()) deltas[runtimeId] = CommittableDelta(deltas[runtimeId]);
        var ordered = _snapshotById.Values.Where(unit => unit.Alive && _isGrounded(unit))
            .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray();
        // An airborne body does not obstruct ground travel. Its destination does, from cast start
        // until landing, so a leap cannot end inside a walking unit or a newly authored summon.
        var landingReservations = _landingReservations();
        foreach (var mover in ordered)
        {
            if (!deltas.TryGetValue(mover.RuntimeId, out var delta)) continue;
            foreach (var reservation in landingReservations)
            {
                if (reservation.RuntimeId == mover.RuntimeId) continue;
                if (BattlefieldSpace.TryMovingCircleTimeOfImpact(_snapshotPositionById[mover.RuntimeId], delta,
                        mover.BodyRadius, reservation.Position, Vector2.Zero, reservation.Radius, out var contact))
                    delta *= Math.Max(0f, contact - .001f);
            }
            deltas[mover.RuntimeId] = CommittableDelta(delta);
        }
        var maximumPasses = Math.Max(4, ordered.Length * ordered.Length);
        for (var pass = 0; pass < maximumPasses; pass++)
        {
            var changed = false;
            for (var firstIndex = 0; firstIndex < ordered.Length; firstIndex++)
            for (var secondIndex = firstIndex + 1; secondIndex < ordered.Length; secondIndex++)
            {
                var first = ordered[firstIndex];
                var second = ordered[secondIndex];
                var firstDelta = deltas.GetValueOrDefault(first.RuntimeId);
                var secondDelta = deltas.GetValueOrDefault(second.RuntimeId);
                if (!BattlefieldSpace.TryMovingCircleTimeOfImpact(
                        _snapshotPositionById[first.RuntimeId], firstDelta, first.BodyRadius,
                        _snapshotPositionById[second.RuntimeId], secondDelta, second.BodyRadius,
                        out var contact)) continue;
                var safeFraction = Math.Max(0f, contact - .001f);
                if (firstDelta.LengthSquared() > MovementEpsilon * MovementEpsilon)
                {
                    deltas[first.RuntimeId] = CommittableDelta(firstDelta * safeFraction);
                    changed = true;
                }
                if (secondDelta.LengthSquared() > MovementEpsilon * MovementEpsilon)
                {
                    deltas[second.RuntimeId] = CommittableDelta(secondDelta * safeFraction);
                    changed = true;
                }
            }
            if (!changed) return;
        }

        // Monotonic clipping converges for ordinary traffic. If a long dependency chain still has a
        // colliding pair at the bound, stop the involved movers for this tick and let wait/replan recover.
        var residualCollision = false;
        for (var firstIndex = 0; firstIndex < ordered.Length && !residualCollision; firstIndex++)
        for (var secondIndex = firstIndex + 1; secondIndex < ordered.Length; secondIndex++)
        {
            var first = ordered[firstIndex];
            var second = ordered[secondIndex];
            var firstDelta = deltas.GetValueOrDefault(first.RuntimeId);
            var secondDelta = deltas.GetValueOrDefault(second.RuntimeId);
            if (!BattlefieldSpace.TryMovingCircleTimeOfImpact(
                    _snapshotPositionById[first.RuntimeId], firstDelta, first.BodyRadius,
                    _snapshotPositionById[second.RuntimeId], secondDelta, second.BodyRadius,
                    out _)) continue;
            residualCollision = true;
            break;
        }
        if (residualCollision)
            foreach (var runtimeId in deltas.Keys.ToArray()) deltas[runtimeId] = Vector2.Zero;
    }

    private static Vector2 CommittableDelta(Vector2 delta) =>
        delta.LengthSquared() <= MovementEpsilon * MovementEpsilon ? Vector2.Zero : delta;

    private bool IsTerrainClear(Vector2 position, float radius) =>
        BattlefieldSpace.IsPositionTerrainClear(position, radius, _width, _height, _blockedTerrainCells);

    private ulong Initiative(string runtimeId) => StableHash($"{_seed}|{runtimeId}");
    private static ulong PositionTieBreak(string runtimeId, Vector2 position) =>
        StableHash($"{runtimeId}|{position.X:R}|{position.Y:R}");
    private static ulong CellTieBreak(Vector2I cell) => StableHash($"{cell.X}|{cell.Y}");

    private static float PairSide(string first, string second)
    {
        var low = string.CompareOrdinal(first, second) <= 0 ? first : second;
        var high = low == first ? second : first;
        var baseSide = (StableHash($"{low}|{high}") & 1UL) == 0 ? 1f : -1f;
        return first == low ? baseSide : -baseSide;
    }

    private static ulong StableHash(string value)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        var hash = offset;
        foreach (var character in value)
        {
            hash ^= character;
            hash *= prime;
        }
        return hash;
    }

    private void MarkWaitingAndMaybeRetarget(BattleUnitState mover, string targetRuntimeId)
    {
        MarkWaiting(mover);
        if (!mover.Alive || mover.WaitingTicks < GoalWaitLease) return;
        ReleaseGoal(mover.RuntimeId);
        _retargetFromByUnit[mover.RuntimeId] = targetRuntimeId;
    }

    private static void MarkWaiting(BattleUnitState mover)
    {
        if (!mover.Alive || mover.Mode == BattleUnitMode.Defeated) return;
        mover.Mode = BattleUnitMode.Waiting;
        mover.WaitingTicks++;
    }

    private sealed record TargetScore(
        BattleUnitState Target, float PathCost, int AuthoredRank, ulong TieBreak, bool ImmediatelyActionable);
    private sealed record MoveRequest(BattleUnitState Mover, string TargetRuntimeId);
    private sealed record Route(IReadOnlyList<Vector2> Points, float Cost, bool AvoidsBodies);
    private readonly record struct RouteOrigin(Vector2 Start, float Radius, string? MoverId);
    private sealed record RouteSearch(
        IReadOnlyList<Vector2I> Nodes,
        IReadOnlyDictionary<Vector2I, float> Distance,
        IReadOnlyDictionary<Vector2I, Vector2I> Previous);

    internal sealed class MovementStateCheckpoint
    {
        private readonly DeterministicContinuousMovementService _owner;
        private readonly Dictionary<string, string> _targetByUnit;
        private readonly Dictionary<string, float> _centerRangeByUnit;
        private readonly Dictionary<string, Vector2> _goalByUnit;
        private readonly Dictionary<string, string> _retargetFromByUnit;
        private readonly Dictionary<string, BattleUnitState> _snapshotById;
        private readonly Dictionary<string, Vector2> _snapshotPositionById;
        private readonly MoveRequest[] _requests;

        internal MovementStateCheckpoint(DeterministicContinuousMovementService owner)
        {
            _owner = owner;
            _targetByUnit = new Dictionary<string, string>(owner._targetByUnit, StringComparer.Ordinal);
            _centerRangeByUnit = new Dictionary<string, float>(owner._centerRangeByUnit, StringComparer.Ordinal);
            _goalByUnit = new Dictionary<string, Vector2>(owner._goalByUnit, StringComparer.Ordinal);
            _retargetFromByUnit = new Dictionary<string, string>(owner._retargetFromByUnit, StringComparer.Ordinal);
            _snapshotById = new Dictionary<string, BattleUnitState>(owner._snapshotById, StringComparer.Ordinal);
            _snapshotPositionById = new Dictionary<string, Vector2>(owner._snapshotPositionById, StringComparer.Ordinal);
            _requests = owner._requests.ToArray();
        }

        internal void Restore(DeterministicContinuousMovementService owner)
        {
            if (!ReferenceEquals(owner, _owner))
                throw new InvalidOperationException("Movement checkpoint belongs to another service.");
            Restore(owner._targetByUnit, _targetByUnit);
            Restore(owner._centerRangeByUnit, _centerRangeByUnit);
            Restore(owner._goalByUnit, _goalByUnit);
            Restore(owner._retargetFromByUnit, _retargetFromByUnit);
            Restore(owner._snapshotById, _snapshotById);
            Restore(owner._snapshotPositionById, _snapshotPositionById);
            owner._requests.Clear();
            owner._requests.AddRange(_requests);
            owner._routeSearchByOrigin.Clear();
        }

        private static void Restore<TKey, TValue>(
            IDictionary<TKey, TValue> target,
            IReadOnlyDictionary<TKey, TValue> source) where TKey : notnull
        {
            target.Clear();
            foreach (var pair in source) target.Add(pair.Key, pair.Value);
        }
    }
}
