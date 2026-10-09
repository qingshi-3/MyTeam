using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;

namespace TowerAutobattler.Battle;

public readonly record struct BattleOpeningAllyCandidate(string InstanceId, Vector2 Position);

// Callers supply living, same-team, persistent, non-temporary candidates. Positions
// come from authoritative prepared bodies, not the requested spawn cells.
public static class BattleOpeningAllySelector
{
    public static ImmutableArray<string> Select(string sourceInstanceId, Vector2 sourcePosition,
        IEnumerable<BattleOpeningAllyCandidate> eligible, int count = 2) => eligible
        .Where(candidate => candidate.InstanceId != sourceInstanceId)
        .OrderBy(candidate => candidate.Position.DistanceSquaredTo(sourcePosition))
        .ThenBy(candidate => candidate.InstanceId, StringComparer.Ordinal)
        .Take(Math.Clamp(count, 0, 2)).Select(candidate => candidate.InstanceId).ToImmutableArray();
}
