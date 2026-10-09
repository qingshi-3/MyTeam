using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TowerAutobattler.Expedition;

// These operations touch only a caller-owned working copy; the Run transaction
// decides whether a command is allowed and publishes it after a successful save.
public static class ExpeditionNavigation
{
    public static bool IsAdjacent(ExpeditionStateDto state, int first, int second) =>
        InBounds(state, first) && InBounds(state, second) &&
        Math.Abs(first % state.Size - second % state.Size) +
        Math.Abs(first / state.Size - second / state.Size) == 1;

    public static bool IsBlocked(ExpeditionStateDto state, int index)
    {
        if (!InBounds(state, index) || !state.Cells[index].Walkable) return true;
        var site = SiteAt(state, index);
        return site is not null && !string.IsNullOrEmpty(site.EncounterProfileId) && !site.Cleared;
    }

    public static ExpeditionSiteDto? SiteAt(ExpeditionStateDto state, int index)
    {
        if (!InBounds(state, index)) return null;
        var id = state.Cells[index].SiteId;
        return string.IsNullOrEmpty(id) ? null : state.Sites.FirstOrDefault(site => site.SiteId == id);
    }

    public static bool CanStep(ExpeditionStateDto state, int target) =>
        IsAdjacent(state, state.PositionIndex, target) && state.Cells[target].Revealed && !IsBlocked(state, target);

    public static bool TryStep(ExpeditionStateDto state, int target, out string reason)
    {
        if (!IsAdjacent(state, state.PositionIndex, target)) { reason = "只能移动到相邻格。"; return false; }
        if (!state.Cells[target].Revealed) { reason = "需要先发现通路。"; return false; }
        if (!state.Cells[target].Walkable) { reason = "此处地形无法通行。"; return false; }
        if (IsBlocked(state, target)) { reason = "敌人挡住了通路；可以迎战或寻找其他道路。"; return false; }
        state.PositionIndex = target;
        RevealAround(state, target);
        reason = string.Empty;
        return true;
    }

    public static void RevealAround(ExpeditionStateDto state, int index)
    {
        if (!InBounds(state, index)) throw new ArgumentOutOfRangeException(nameof(index));
        var x = index % state.Size;
        var y = index / state.Size;
        for (var dy = -1; dy <= 1; dy++)
        for (var dx = -1; dx <= 1; dx++)
        {
            var nx = x + dx;
            var ny = y + dy;
            if (nx >= 0 && ny >= 0 && nx < state.Size && ny < state.Size)
                state.Cells[ny * state.Size + nx].Revealed = true;
        }
    }

    // Captures a path through the currently known graph. The UI executes one
    // saved step at a time and must not recompute onward through newly seen fog.
    public static ImmutableArray<int> FindKnownPath(ExpeditionStateDto state, int target)
    {
        if (!InBounds(state, target) || !state.Cells[target].Revealed || IsBlocked(state, target)) return [];
        var previous = Enumerable.Repeat(-1, state.Cells.Count).ToArray();
        var queue = new Queue<int>();
        previous[state.PositionIndex] = state.PositionIndex;
        queue.Enqueue(state.PositionIndex);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == target) break;
            foreach (var next in Neighbors(state.Size, current))
            {
                if (previous[next] >= 0 || !state.Cells[next].Revealed || IsBlocked(state, next)) continue;
                previous[next] = current;
                queue.Enqueue(next);
            }
        }
        if (previous[target] < 0) return [];
        var path = new List<int>();
        for (var current = target; current != state.PositionIndex; current = previous[current]) path.Add(current);
        path.Reverse();
        var frontier = path.FindIndex(index => RevealsNewCells(state, index));
        if (frontier >= 0 && frontier + 1 < path.Count) path.RemoveRange(frontier + 1, path.Count - frontier - 1);
        return [.. path];
    }

    public static bool RevealsNewCells(ExpeditionStateDto state, int index)
    {
        if (!InBounds(state, index)) return false;
        for (var dy = -1; dy <= 1; dy++)
        for (var dx = -1; dx <= 1; dx++)
        {
            var x = index % state.Size + dx;
            var y = index / state.Size + dy;
            if (x >= 0 && y >= 0 && x < state.Size && y < state.Size && !state.Cells[y * state.Size + x].Revealed)
                return true;
        }
        return false;
    }

    public static bool CanInspectSite(ExpeditionStateDto state, string siteId) =>
        state.Sites.Any(site => site.SiteId == siteId && InBounds(state, site.CellIndex) && state.Cells[site.CellIndex].Revealed);

    public static bool CanInteractWithSite(ExpeditionStateDto state, string siteId)
    {
        var site = state.Sites.FirstOrDefault(candidate => candidate.SiteId == siteId);
        return site is not null && state.Cells[site.CellIndex].Revealed &&
            (state.PositionIndex == site.CellIndex || IsAdjacent(state, state.PositionIndex, site.CellIndex));
    }

    internal static bool InBounds(ExpeditionStateDto state, int index) =>
        index >= 0 && index < state.Cells.Count && state.Size > 0 && state.Cells.Count == state.Size * state.Size;

    internal static IEnumerable<int> Neighbors(int size, int index)
    {
        var x = index % size;
        var y = index / size;
        // Stable ordering is part of generation/pathfinding determinism.
        if (y > 0) yield return index - size;
        if (x > 0) yield return index - 1;
        if (x + 1 < size) yield return index + 1;
        if (y + 1 < size) yield return index + size;
    }
}

public sealed record ExpeditionCellView(int Index, bool Revealed, bool Walkable, int BiomeIndex,
    string SiteId, string DefinitionId, string EncounterProfileId, bool Cleared, bool Handled, bool Blocked);
public sealed record ExpeditionMapView(int Size, int PositionIndex, int StageIndex, int RemainingActions,
    ImmutableArray<ExpeditionCellView> Cells);

public static class ExpeditionMapProjection
{
    public static ExpeditionMapView Create(ExpeditionStateDto state)
    {
        var sites = state.Sites.ToDictionary(site => site.SiteId, StringComparer.Ordinal);
        return new(state.Size, state.PositionIndex, state.StageIndex, state.RemainingActions,
            state.Cells.Select(cell =>
            {
                // Fog projection deliberately carries neither terrain nor site
                // identity, content seed, stock, or future encounter information.
                if (!cell.Revealed) return new ExpeditionCellView(cell.Index, false, false, -1, "", "", "", false, false, false);
                sites.TryGetValue(cell.SiteId, out var site);
                return new ExpeditionCellView(cell.Index, true, cell.Walkable, cell.BiomeIndex, cell.SiteId,
                    site?.DefinitionId ?? "", site?.EncounterProfileId ?? "", site?.Cleared ?? false,
                    site?.Handled ?? false, !cell.Walkable || (site is { Cleared: false } && site.EncounterProfileId.Length > 0));
            }).ToImmutableArray());
    }
}
