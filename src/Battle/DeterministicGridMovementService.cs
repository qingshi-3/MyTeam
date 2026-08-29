using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TowerAutobattler.Battle;

public interface IGridMovementService : IDisposable
{
    void BeginTick();
    BattleUnitState? SelectTarget(BattleUnitState mover, IReadOnlyList<BattleUnitState> orderedCandidates);
    bool QueueMove(BattleUnitState mover);
    void ResolveIntents(Action<BattleUnitState, Vector2I> moved);
    void ReleaseUnit(string runtimeId);
    void ReleaseTarget(string runtimeId);
    void ReleaseGoal(string runtimeId);
    void ClearTarget(string runtimeId);
    bool IsReserved(Vector2I cell);
}

public sealed class DeterministicGridMovementService : IGridMovementService
{
    internal const int GoalWaitLease = 4;
    private static readonly Vector2I[] Directions = [Vector2I.Right, Vector2I.Left, Vector2I.Down, Vector2I.Up];

    private readonly int _width;
    private readonly int _height;
    private readonly Func<IReadOnlyList<BattleUnitState>> _units;
    private readonly Func<Vector2I, bool> _terrainAllows;
    private readonly Func<Vector2I, UnitSnapshot, Vector2I, bool> _hasLineAccess;
    private readonly ulong _seed;
    private readonly Dictionary<string, string> _targetByUnit = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Vector2I> _goalByUnit = new(StringComparer.Ordinal);
    private readonly Dictionary<Vector2I, string> _goalOwnerByCell = [];
    private readonly Dictionary<string, string> _retargetFromByUnit = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BattleUnitState> _snapshotById = new(StringComparer.Ordinal);
    private readonly Dictionary<Vector2I, BattleUnitState> _snapshotByCell = [];
    private readonly Dictionary<Vector2I, bool> _terrainSnapshot = [];
    private readonly List<MoveRequest> _requests = [];

    public int ActiveGoalCount => _goalByUnit.Count;
    public int PendingRequestCount => _requests.Count;
    internal int RetargetLeaseCount => _retargetFromByUnit.Count;
    internal int PlanningStateCount => _targetByUnit.Count + _goalByUnit.Count + _requests.Count + _retargetFromByUnit.Count;
    internal bool HasPlanningState(string runtimeId) =>
        _targetByUnit.ContainsKey(runtimeId) || _targetByUnit.ContainsValue(runtimeId) ||
        _goalByUnit.ContainsKey(runtimeId) || _goalOwnerByCell.ContainsValue(runtimeId) ||
        _retargetFromByUnit.ContainsKey(runtimeId) || _retargetFromByUnit.ContainsValue(runtimeId) ||
        _requests.Any(request => request.Mover.RuntimeId == runtimeId || request.TargetRuntimeId == runtimeId) ||
        _snapshotById.ContainsKey(runtimeId) || _snapshotByCell.Values.Any(unit => unit.RuntimeId == runtimeId);

    public DeterministicGridMovementService(
        int width,
        int height,
        Func<IReadOnlyList<BattleUnitState>> units,
        Func<Vector2I, bool> terrainAllows,
        Func<Vector2I, UnitSnapshot, Vector2I, bool> hasLineAccess,
        ulong seed = 0)
    {
        _width = width;
        _height = height;
        _units = units;
        _terrainAllows = terrainAllows;
        _hasLineAccess = hasLineAccess;
        _seed = seed;
    }

    public void BeginTick()
    {
        _requests.Clear();
        _snapshotById.Clear();
        _snapshotByCell.Clear();
        _terrainSnapshot.Clear();
        for (var y = 0; y < _height; y++)
        for (var x = 0; x < _width; x++)
        {
            var cell = new Vector2I(x, y);
            _terrainSnapshot[cell] = _terrainAllows(cell);
        }
        foreach (var unit in _units().Where(unit => unit.Alive))
        {
            _snapshotById[unit.RuntimeId] = unit;
            _snapshotByCell[unit.Cell] = unit;
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
                mover.WaitingTicks >= GoalWaitLease || !IsEngagementCell(mover, target, _goalByUnit[runtimeId]))
                ReleaseGoal(runtimeId);
        }
    }

    public BattleUnitState? SelectTarget(BattleUnitState mover, IReadOnlyList<BattleUnitState> orderedCandidates)
    {
        var candidates = orderedCandidates
            .Where(candidate => candidate.Alive && candidate.RuntimeId != mover.RuntimeId)
            .DistinctBy(candidate => candidate.RuntimeId)
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
        if (immediate.Length == 0 && _retargetFromByUnit.TryGetValue(mover.RuntimeId, out var blockedTargetId))
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
            if (stable is not null && stable.PathCost <= best.PathCost + 1)
                best = stable;
        }

        if (!_targetByUnit.TryGetValue(mover.RuntimeId, out var previous) || previous != best.Target.RuntimeId)
        {
            ReleaseGoal(mover.RuntimeId);
            mover.WaitingTicks = 0;
        }
        _targetByUnit[mover.RuntimeId] = best.Target.RuntimeId;
        if (best.ImmediatelyActionable ||
            _retargetFromByUnit.TryGetValue(mover.RuntimeId, out var avoided) && avoided != best.Target.RuntimeId)
            _retargetFromByUnit.Remove(mover.RuntimeId);
        return best.Target;
    }

    public bool QueueMove(BattleUnitState mover)
    {
        if (!mover.Alive || !_targetByUnit.TryGetValue(mover.RuntimeId, out var targetId))
        {
            MarkWaiting(mover);
            return false;
        }
        _requests.RemoveAll(request => request.Mover.RuntimeId == mover.RuntimeId);
        Vector2I? preferredGoal = _goalByUnit.TryGetValue(mover.RuntimeId, out var retainedGoal) ? retainedGoal : null;
        _requests.Add(new MoveRequest(mover, targetId, preferredGoal));
        return true;
    }

    public void ResolveIntents(Action<BattleUnitState, Vector2I> moved)
    {
        var liveRequests = _requests
            .Where(request => request.Mover.Alive &&
                              _targetByUnit.GetValueOrDefault(request.Mover.RuntimeId) == request.TargetRuntimeId &&
                              _snapshotById.ContainsKey(request.TargetRuntimeId))
            .OrderByDescending(request => request.Mover.WaitingTicks)
            .ThenBy(request => Initiative(request.Mover))
            .ToArray();
        var requestIds = liveRequests.Select(request => request.Mover.RuntimeId).ToHashSet(StringComparer.Ordinal);

        // Every request finishes candidate generation from the tick snapshot before shared goals can change.
        var plans = liveRequests.Select(request => GenerateRequestPlan(request, requestIds)).ToArray();
        var externalGoalOwners = _goalByUnit
            .Where(pair => !requestIds.Contains(pair.Key))
            .ToDictionary(pair => pair.Value, pair => pair.Key);
        foreach (var request in liveRequests) ReleaseGoal(request.Mover.RuntimeId);

        var proposals = Arbitrate(plans, externalGoalOwners);
        var acceptedIds = proposals.Keys.ToHashSet(StringComparer.Ordinal);
        foreach (var request in liveRequests.Where(request => !acceptedIds.Contains(request.Mover.RuntimeId)))
            MarkWaitingAndMaybeRetarget(request.Mover, request.TargetRuntimeId);

        foreach (var proposal in proposals.Values.Where(proposal => proposal.Candidate.Goal is not null))
        {
            var goal = proposal.Candidate.Goal!.Value;
            _goalByUnit[proposal.Mover.RuntimeId] = goal;
            _goalOwnerByCell[goal] = proposal.Mover.RuntimeId;
        }

        var currentOccupancy = _units().Where(unit => unit.Alive).ToDictionary(unit => unit.Cell, unit => unit);
        var commitOrder = BuildCommitOrder(proposals, currentOccupancy);
        foreach (var proposal in commitOrder)
        {
            proposal.Mover.Cell = proposal.Candidate.To;
            proposal.Mover.MoveCooldown = proposal.Mover.Definition.MoveTicks;
            proposal.Mover.Mode = BattleUnitMode.Moving;
            proposal.Mover.WaitingTicks = 0;
            _retargetFromByUnit.Remove(proposal.Mover.RuntimeId);
        }
        foreach (var proposal in commitOrder)
            moved(proposal.Mover, proposal.Candidate.To);
        _requests.Clear();
    }

    public void ReleaseUnit(string runtimeId)
    {
        ClearTarget(runtimeId);
        ReleaseTarget(runtimeId);
        _retargetFromByUnit.Remove(runtimeId);
        _requests.RemoveAll(request => request.Mover.RuntimeId == runtimeId || request.TargetRuntimeId == runtimeId);
        if (_snapshotById.Remove(runtimeId, out var unit) &&
            _snapshotByCell.TryGetValue(unit.Cell, out var occupant) && occupant.RuntimeId == runtimeId)
            _snapshotByCell.Remove(unit.Cell);
    }

    public void ReleaseTarget(string runtimeId)
    {
        foreach (var moverId in _targetByUnit.Where(pair => pair.Value == runtimeId).Select(pair => pair.Key).ToArray())
            ClearTarget(moverId);
    }

    public void ReleaseGoal(string runtimeId)
    {
        if (!_goalByUnit.Remove(runtimeId, out var cell)) return;
        if (_goalOwnerByCell.TryGetValue(cell, out var owner) && owner == runtimeId)
            _goalOwnerByCell.Remove(cell);
    }

    public void ClearTarget(string runtimeId)
    {
        _targetByUnit.Remove(runtimeId);
        _retargetFromByUnit.Remove(runtimeId);
        ReleaseGoal(runtimeId);
        _requests.RemoveAll(request => request.Mover.RuntimeId == runtimeId);
    }

    public bool IsReserved(Vector2I cell) => _goalOwnerByCell.ContainsKey(cell);

    public void Dispose()
    {
        _requests.Clear();
        _targetByUnit.Clear();
        _goalByUnit.Clear();
        _goalOwnerByCell.Clear();
        _retargetFromByUnit.Clear();
        _snapshotById.Clear();
        _snapshotByCell.Clear();
        _terrainSnapshot.Clear();
    }

    private TargetScore? ScoreTarget(BattleUnitState mover, BattleUnitState target, int authoredRank)
    {
        if (CanActFrom(mover, target, mover.Cell))
            return new TargetScore(target, 0, authoredRank, StableHash(target.RuntimeId), true);
        var distances = BuildDistanceMap(mover);
        var cost = EngagementCells(mover, target, requireOpen: true)
            .Where(distances.ContainsKey)
            .Select(cell => distances[cell])
            .DefaultIfEmpty(int.MaxValue)
            .Min();
        return cost == int.MaxValue ? null : new TargetScore(target, cost, authoredRank, StableHash(target.RuntimeId), false);
    }

    private RequestPlan GenerateRequestPlan(MoveRequest request, IReadOnlySet<string> requestingIds)
    {
        if (!request.Mover.Alive || !_snapshotById.TryGetValue(request.TargetRuntimeId, out var target))
            return new RequestPlan(request, [], [], []);

        var relaxedDistances = BuildDistanceMap(request.Mover);
        var engagementCells = EngagementCells(request.Mover, target, requireOpen: false)
            .Where(relaxedDistances.ContainsKey)
            .OrderBy(cell => relaxedDistances[cell])
            .ThenBy(cell => CellTieBreak(request.Mover, cell))
            .ToArray();
        var goalCandidates = new List<PlanCandidate>();
        var stagingCandidates = new List<PlanCandidate>();
        foreach (var destination in engagementCells)
        {
            var isOpenGoal = !_snapshotByCell.TryGetValue(destination, out var occupant) ||
                             occupant.RuntimeId == request.Mover.RuntimeId;
            if (isOpenGoal)
                goalCandidates.AddRange(BuildPlanCandidates(request.Mover, destination, destination, requestingIds));
            stagingCandidates.AddRange(BuildPlanCandidates(request.Mover, destination, null, requestingIds));
        }

        var orderedGoalCandidates = OrderAndDeduplicate(goalCandidates, includeGoal: true);
        var orderedStagingCandidates = OrderAndDeduplicate(stagingCandidates, includeGoal: false);
        var goalOptions = orderedGoalCandidates
            .GroupBy(candidate => candidate.Goal!.Value)
            .Select(group => new GoalOption(group.Key, group.Min(candidate => candidate.RouteCost),
                request.PreferredGoal == group.Key, CellTieBreak(request.Mover, group.Key)))
            .OrderByDescending(option => option.Retained)
            .ThenBy(option => option.RouteCost)
            .ThenBy(option => option.TieBreak)
            .ToArray();
        return new RequestPlan(request, orderedGoalCandidates, orderedStagingCandidates, goalOptions);
    }

    private IReadOnlyList<PlanCandidate> BuildPlanCandidates(
        BattleUnitState mover,
        Vector2I destination,
        Vector2I? goal,
        IReadOnlySet<string> requestingIds)
    {
        var result = new List<PlanCandidate>();
        foreach (var next in Directions.Select(direction => mover.Cell + direction)
                     .OrderBy(cell => CellTieBreak(mover, cell)))
        {
            if (!InBounds(next) || !TerrainAllowsSnapshot(next)) continue;
            var occupiedByFriend = _snapshotByCell.TryGetValue(next, out var occupant) &&
                                   occupant.RuntimeId != mover.RuntimeId && occupant.Team == mover.Team;
            var friendId = occupiedByFriend ? occupant!.RuntimeId : string.Empty;
            if (_snapshotByCell.TryGetValue(next, out occupant) &&
                occupant.RuntimeId != mover.RuntimeId && occupant.Team != mover.Team)
                continue;

            var baseline = ShortestPathLength(mover.Cell, destination, mover, requestingIds, friendId, null);
            var remaining = ShortestPathLength(next, destination, mover, requestingIds, friendId, mover.Cell);
            if (baseline == int.MaxValue || remaining == int.MaxValue || remaining >= baseline) continue;
            result.Add(new PlanCandidate(mover, mover.Cell, next, destination, goal,
                1 + remaining, remaining, CellTieBreak(mover, next)));
        }
        return result;
    }

    private static IReadOnlyList<PlanCandidate> OrderAndDeduplicate(IEnumerable<PlanCandidate> candidates, bool includeGoal)
    {
        return candidates
            .OrderBy(candidate => candidate.RouteCost)
            .ThenBy(candidate => candidate.RemainingCost)
            .ThenBy(candidate => candidate.TieBreak)
            .GroupBy(candidate => includeGoal ? (candidate.Goal, candidate.To) : (null, candidate.To))
            .Select(group => group.First())
            .ToArray();
    }

    private Dictionary<string, MoveProposal> Arbitrate(
        IReadOnlyList<RequestPlan> plans,
        IReadOnlyDictionary<Vector2I, string> externalGoalOwners)
    {
        var disabledGoals = new HashSet<GoalKey>();
        var bannedCandidates = new HashSet<CandidateKey>();
        var maximumAttempts = Math.Max(1, plans.Sum(plan => plan.GoalCandidates.Count + plan.StagingCandidates.Count + plan.GoalOptions.Count) + 1);
        for (var attempt = 0; attempt < maximumAttempts; attempt++)
        {
            var goals = AssignGoals(plans, externalGoalOwners, disabledGoals, bannedCandidates);
            var candidatesByRequest = new Dictionary<string, IReadOnlyList<PlanCandidate>>(StringComparer.Ordinal);
            foreach (var plan in plans)
            {
                var runtimeId = plan.Request.Mover.RuntimeId;
                var candidates = goals.TryGetValue(runtimeId, out var goal)
                    ? plan.GoalCandidates.Where(candidate => candidate.Goal == goal &&
                        !bannedCandidates.Contains(CandidateKey.From(candidate))).ToArray()
                    : plan.StagingCandidates.Where(candidate =>
                        !bannedCandidates.Contains(CandidateKey.From(candidate))).ToArray();
                candidatesByRequest[runtimeId] = candidates;
            }

            var selected = AssignDestinations(plans, candidatesByRequest);
            var assignedWithoutStep = goals.FirstOrDefault(pair => !selected.ContainsKey(pair.Key));
            if (!string.IsNullOrEmpty(assignedWithoutStep.Key))
            {
                if (!disabledGoals.Add(new GoalKey(assignedWithoutStep.Key, assignedWithoutStep.Value))) break;
                continue;
            }

            var nonProgressingChase = FindNonProgressingReciprocalChase(selected, plans);
            if (nonProgressingChase is not null)
            {
                if (!bannedCandidates.Add(CandidateKey.From(selected[nonProgressingChase].Candidate))) break;
                continue;
            }

            var blocker = FindDependencyBlocker(selected, plans);
            if (blocker is not null)
            {
                if (!bannedCandidates.Add(CandidateKey.From(selected[blocker].Candidate))) break;
                continue;
            }
            return selected;
        }
        return [];
    }

    private static Dictionary<string, Vector2I> AssignGoals(
        IReadOnlyList<RequestPlan> plans,
        IReadOnlyDictionary<Vector2I, string> externalGoalOwners,
        IReadOnlySet<GoalKey> disabledGoals,
        IReadOnlySet<CandidateKey> bannedCandidates)
    {
        var planById = plans.ToDictionary(plan => plan.Request.Mover.RuntimeId, StringComparer.Ordinal);
        var ownerByGoal = new Dictionary<Vector2I, string>(externalGoalOwners);
        var goalByRequest = new Dictionary<string, Vector2I>(StringComparer.Ordinal);

        bool TryAssign(string runtimeId, ISet<Vector2I> visited)
        {
            var plan = planById[runtimeId];
            foreach (var option in plan.GoalOptions)
            {
                var key = new GoalKey(runtimeId, option.Goal);
                if (disabledGoals.Contains(key) || !visited.Add(option.Goal) ||
                    !plan.GoalCandidates.Any(candidate => candidate.Goal == option.Goal &&
                        !bannedCandidates.Contains(CandidateKey.From(candidate))))
                    continue;
                if (!ownerByGoal.TryGetValue(option.Goal, out var owner) ||
                    planById.ContainsKey(owner) && TryAssign(owner, visited))
                {
                    ownerByGoal[option.Goal] = runtimeId;
                    goalByRequest[runtimeId] = option.Goal;
                    return true;
                }
            }
            return false;
        }

        foreach (var plan in plans)
            TryAssign(plan.Request.Mover.RuntimeId, new HashSet<Vector2I>());
        return goalByRequest;
    }

    private static Dictionary<string, MoveProposal> AssignDestinations(
        IReadOnlyList<RequestPlan> plans,
        IReadOnlyDictionary<string, IReadOnlyList<PlanCandidate>> candidatesByRequest)
    {
        var planById = plans.ToDictionary(plan => plan.Request.Mover.RuntimeId, StringComparer.Ordinal);
        var ownerByDestination = new Dictionary<Vector2I, string>();
        var selected = new Dictionary<string, MoveProposal>(StringComparer.Ordinal);

        bool TryAssign(string runtimeId, ISet<Vector2I> visited)
        {
            foreach (var candidate in candidatesByRequest[runtimeId])
            {
                if (!visited.Add(candidate.To)) continue;
                if (!ownerByDestination.TryGetValue(candidate.To, out var owner) ||
                    planById.ContainsKey(owner) && TryAssign(owner, visited))
                {
                    ownerByDestination[candidate.To] = runtimeId;
                    selected[runtimeId] = new MoveProposal(planById[runtimeId].Request.Mover, candidate);
                    return true;
                }
            }
            return false;
        }

        foreach (var plan in plans)
            TryAssign(plan.Request.Mover.RuntimeId, new HashSet<Vector2I>());
        return selected;
    }

    private string? FindNonProgressingReciprocalChase(
        IReadOnlyDictionary<string, MoveProposal> proposals,
        IReadOnlyList<RequestPlan> plans)
    {
        var planById = plans.ToDictionary(plan => plan.Request.Mover.RuntimeId, StringComparer.Ordinal);
        var priority = plans.Select((plan, index) => (plan.Request.Mover.RuntimeId, index))
            .ToDictionary(pair => pair.RuntimeId, pair => pair.index, StringComparer.Ordinal);
        foreach (var proposal in proposals.Values.OrderBy(proposal => priority[proposal.Mover.RuntimeId]))
        {
            var targetId = planById[proposal.Mover.RuntimeId].Request.TargetRuntimeId;
            if (!proposals.TryGetValue(targetId, out var targetProposal) ||
                !planById.TryGetValue(targetId, out var targetPlan) ||
                targetPlan.Request.TargetRuntimeId != proposal.Mover.RuntimeId)
                continue;
            var currentDistance = proposal.Mover.Cell.DistanceTo(targetProposal.Mover.Cell);
            var finalDistance = proposal.Candidate.To.DistanceTo(targetProposal.Candidate.To);
            var finalCanAct = finalDistance <= proposal.Mover.Definition.Range &&
                              _hasLineAccess(proposal.Candidate.To, proposal.Mover.Definition, targetProposal.Candidate.To);
            var lineImproved = !_hasLineAccess(proposal.Mover.Cell, proposal.Mover.Definition, targetProposal.Mover.Cell) &&
                               _hasLineAccess(proposal.Candidate.To, proposal.Mover.Definition, targetProposal.Candidate.To);
            if (finalCanAct || lineImproved || finalDistance + .001f < currentDistance) continue;
            return priority[proposal.Mover.RuntimeId] >= priority[targetId]
                ? proposal.Mover.RuntimeId
                : targetId;
        }
        return null;
    }

    private string? FindDependencyBlocker(
        IReadOnlyDictionary<string, MoveProposal> proposals,
        IReadOnlyList<RequestPlan> plans)
    {
        var occupancy = _units().Where(unit => unit.Alive).ToDictionary(unit => unit.Cell, unit => unit);
        var priority = plans.Select((plan, index) => (plan.Request.Mover.RuntimeId, index))
            .ToDictionary(pair => pair.RuntimeId, pair => pair.index, StringComparer.Ordinal);
        var accepted = new HashSet<string>(StringComparer.Ordinal);
        var visiting = new List<string>();

        string? Visit(string runtimeId)
        {
            if (accepted.Contains(runtimeId)) return null;
            var cycleStart = visiting.IndexOf(runtimeId);
            if (cycleStart >= 0)
                return visiting.Skip(cycleStart).OrderByDescending(id => priority[id]).First();
            var proposal = proposals[runtimeId];
            if (!proposal.Mover.Alive || proposal.Candidate.From != proposal.Mover.Cell ||
                !_terrainAllows(proposal.Candidate.To))
                return runtimeId;
            visiting.Add(runtimeId);
            if (occupancy.TryGetValue(proposal.Candidate.To, out var occupant) && occupant.RuntimeId != runtimeId)
            {
                if (occupant.Team != proposal.Mover.Team || !proposals.ContainsKey(occupant.RuntimeId))
                {
                    visiting.RemoveAt(visiting.Count - 1);
                    return runtimeId;
                }
                var blocker = Visit(occupant.RuntimeId);
                if (blocker is not null)
                {
                    visiting.RemoveAt(visiting.Count - 1);
                    return blocker;
                }
            }
            visiting.RemoveAt(visiting.Count - 1);
            accepted.Add(runtimeId);
            return null;
        }

        foreach (var runtimeId in proposals.Keys.OrderBy(id => priority[id]))
        {
            var blocker = Visit(runtimeId);
            if (blocker is not null) return blocker;
        }
        return null;
    }

    private static IReadOnlyList<MoveProposal> BuildCommitOrder(
        IReadOnlyDictionary<string, MoveProposal> proposals,
        IReadOnlyDictionary<Vector2I, BattleUnitState> occupancy)
    {
        var committed = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<MoveProposal>();
        void Visit(MoveProposal proposal)
        {
            if (!committed.Add(proposal.Mover.RuntimeId)) return;
            if (occupancy.TryGetValue(proposal.Candidate.To, out var occupant) &&
                proposals.TryGetValue(occupant.RuntimeId, out var dependency))
                Visit(dependency);
            result.Add(proposal);
        }
        foreach (var proposal in proposals.Values) Visit(proposal);
        return result;
    }

    private IEnumerable<Vector2I> EngagementCells(BattleUnitState mover, BattleUnitState target, bool requireOpen)
    {
        for (var y = 0; y < _height; y++)
        for (var x = 0; x < _width; x++)
        {
            var cell = new Vector2I(x, y);
            if (!IsEngagementCell(mover, target, cell)) continue;
            if (requireOpen && _snapshotByCell.TryGetValue(cell, out var occupant) && occupant.RuntimeId != mover.RuntimeId) continue;
            yield return cell;
        }
    }

    private bool IsEngagementCell(BattleUnitState mover, BattleUnitState target, Vector2I cell) =>
        InBounds(cell) && TerrainAllowsSnapshot(cell) && cell != target.Cell &&
        cell.DistanceTo(target.Cell) <= mover.Definition.Range && _hasLineAccess(cell, mover.Definition, target.Cell);

    private bool CanActFrom(BattleUnitState mover, BattleUnitState target, Vector2I cell) =>
        cell.DistanceTo(target.Cell) <= mover.Definition.Range && _hasLineAccess(cell, mover.Definition, target.Cell);

    private Dictionary<Vector2I, int> BuildDistanceMap(BattleUnitState mover)
    {
        var distances = new Dictionary<Vector2I, int> { [mover.Cell] = 0 };
        var frontier = new Queue<Vector2I>();
        frontier.Enqueue(mover.Cell);
        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            foreach (var next in Directions.Select(direction => current + direction)
                         .OrderBy(cell => CellTieBreak(mover, cell)))
            {
                if (distances.ContainsKey(next) || !CanTraverseRelaxed(next, mover)) continue;
                distances[next] = distances[current] + 1;
                frontier.Enqueue(next);
            }
        }
        return distances;
    }

    private int ShortestPathLength(
        Vector2I origin,
        Vector2I destination,
        BattleUnitState mover,
        IReadOnlySet<string> traversableFriendIds,
        string extraTraversableFriendId,
        Vector2I? forbidden)
    {
        if (origin == destination) return 0;
        var distances = new Dictionary<Vector2I, int> { [origin] = 0 };
        var frontier = new Queue<Vector2I>();
        frontier.Enqueue(origin);
        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            foreach (var next in Directions.Select(direction => current + direction)
                         .OrderBy(cell => CellTieBreak(mover, cell)))
            {
                if (next == forbidden || distances.ContainsKey(next) ||
                    !CanTraverse(next, mover, traversableFriendIds, extraTraversableFriendId, forbidden)) continue;
                var distance = distances[current] + 1;
                if (next == destination) return distance;
                distances[next] = distance;
                frontier.Enqueue(next);
            }
        }
        return int.MaxValue;
    }

    private bool CanTraverse(
        Vector2I cell,
        BattleUnitState mover,
        IReadOnlySet<string> traversableFriendIds,
        string extraTraversableFriendId,
        Vector2I? forbidden)
    {
        if (cell == forbidden || !InBounds(cell) || !TerrainAllowsSnapshot(cell)) return false;
        if (!_snapshotByCell.TryGetValue(cell, out var occupant) || occupant.RuntimeId == mover.RuntimeId) return true;
        return occupant.Team == mover.Team &&
               (traversableFriendIds.Contains(occupant.RuntimeId) || occupant.RuntimeId == extraTraversableFriendId);
    }

    private bool CanTraverseRelaxed(Vector2I cell, BattleUnitState mover)
    {
        if (!InBounds(cell) || !TerrainAllowsSnapshot(cell)) return false;
        return !_snapshotByCell.TryGetValue(cell, out var occupant) ||
               occupant.RuntimeId == mover.RuntimeId || occupant.Team == mover.Team;
    }

    private bool TerrainAllowsSnapshot(Vector2I cell) =>
        _terrainSnapshot.TryGetValue(cell, out var allowed) ? allowed : _terrainAllows(cell);

    private bool InBounds(Vector2I cell) => cell.X >= 0 && cell.X < _width && cell.Y >= 0 && cell.Y < _height;

    private ulong Initiative(BattleUnitState mover) => StableHash($"{_seed}|{mover.RuntimeId}|{mover.Cell.X}|{mover.Cell.Y}");

    private static ulong CellTieBreak(BattleUnitState mover, Vector2I cell) =>
        StableHash($"{mover.RuntimeId}|{cell.X}|{cell.Y}");

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
        mover.MoveCooldown = Math.Max(1, mover.MoveCooldown);
    }

    private sealed record TargetScore(
        BattleUnitState Target, int PathCost, int AuthoredRank, ulong TieBreak, bool ImmediatelyActionable);

    private sealed record MoveRequest(BattleUnitState Mover, string TargetRuntimeId, Vector2I? PreferredGoal);
    private sealed record RequestPlan(
        MoveRequest Request,
        IReadOnlyList<PlanCandidate> GoalCandidates,
        IReadOnlyList<PlanCandidate> StagingCandidates,
        IReadOnlyList<GoalOption> GoalOptions);
    private sealed record GoalOption(Vector2I Goal, int RouteCost, bool Retained, ulong TieBreak);
    private sealed record PlanCandidate(
        BattleUnitState Mover,
        Vector2I From,
        Vector2I To,
        Vector2I RouteDestination,
        Vector2I? Goal,
        int RouteCost,
        int RemainingCost,
        ulong TieBreak);
    private sealed record MoveProposal(BattleUnitState Mover, PlanCandidate Candidate);
    private readonly record struct GoalKey(string RuntimeId, Vector2I Goal);
    private readonly record struct CandidateKey(string RuntimeId, Vector2I? Goal, Vector2I Destination)
    {
        public static CandidateKey From(PlanCandidate candidate) =>
            new(candidate.Mover.RuntimeId, candidate.Goal, candidate.To);
    }
}
