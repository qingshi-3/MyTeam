using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TowerAutobattler.Effects;

/// <summary>Shared snapshot query for abilities and effect carriers. Selection has stable tie-breaking.</summary>
public static class EffectTargetResolver
{
    public static ImmutableArray<string> Resolve(CompiledEffectTargetQuery query, EffectWorldSnapshot snapshot,
        string sourceId, string ownerId, string explicitTargetId)
    {
        IEnumerable<EffectEntitySnapshot> targets;
        switch (query)
        {
            case CompiledExplicitTargetQuery: targets = Lookup(snapshot, explicitTargetId); break;
            case CompiledSourceTargetQuery: targets = Lookup(snapshot, sourceId); break;
            case CompiledOwnerTargetQuery: targets = Lookup(snapshot, ownerId); break;
            case CompiledRelativeTeamTargetQuery relative:
                return Resolve(new CompiledFilteredTargetQuery(relative.Team, EffectEntityReference.Owner,
                    relative.IncludeDefeated, true, relative.RequiredTag, -1, 0, EffectTargetOrder.RuntimeId),
                    snapshot, sourceId, ownerId, explicitTargetId);
            case CompiledFilteredTargetQuery filter:
                var anchorId = EntityId(filter.Anchor, sourceId, ownerId, explicitTargetId);
                if (filter.Anchor == EffectEntityReference.Owner && !snapshot.Entities.ContainsKey(anchorId)) anchorId = sourceId;
                if (!snapshot.Entities.TryGetValue(anchorId, out var anchor)) return [];
                var needsPosition = filter.Range >= 0 || filter.Order is EffectTargetOrder.Nearest or EffectTargetOrder.Farthest;
                if (needsPosition && anchor.Position is null)
                    throw new InvalidOperationException("Spatial target query requires anchor position.");
                targets = snapshot.Entities.Values.Where(entity =>
                    (filter.Team == EffectRelativeTeam.Allies ? entity.Team == anchor.Team : entity.Team != anchor.Team) &&
                    (filter.IncludeDefeated || entity.Alive) &&
                    (filter.IncludeAnchor || entity.RuntimeId != anchorId) &&
                    (string.IsNullOrEmpty(filter.RequiredTag) || HasTag(entity, filter.RequiredTag)));
                if (needsPosition && targets.Any(entity => entity.Position is null))
                    throw new InvalidOperationException("Spatial target query requires candidate positions.");
                if (filter.Range >= 0)
                    targets = targets.Where(entity => entity.Position!.Value.DistanceSquaredTo(anchor.Position!.Value) <= filter.Range * filter.Range);
                var ordered = filter.Order switch
                {
                    EffectTargetOrder.Nearest => targets.OrderBy(entity => entity.Position!.Value.DistanceSquaredTo(anchor.Position!.Value)),
                    EffectTargetOrder.Farthest => targets.OrderByDescending(entity => entity.Position!.Value.DistanceSquaredTo(anchor.Position!.Value)),
                    EffectTargetOrder.LowestHealthRatio => targets.OrderBy(HealthRatio),
                    EffectTargetOrder.HighestHealthRatio => targets.OrderByDescending(HealthRatio),
                    _ => targets.OrderBy(entity => entity.RuntimeId, StringComparer.Ordinal)
                };
                targets = ordered.ThenBy(entity => entity.RuntimeId, StringComparer.Ordinal);
                if (filter.MaxTargets > 0) targets = targets.Take(filter.MaxTargets);
                return targets.Select(entity => entity.RuntimeId).ToImmutableArray();
            default: throw new InvalidOperationException("Unsupported compiled target query.");
        }
        return targets.Select(entity => entity.RuntimeId).OrderBy(id => id, StringComparer.Ordinal).ToImmutableArray();
    }

    public static string EntityId(EffectEntityReference entity, string sourceId, string ownerId, string explicitTargetId) => entity switch
    {
        EffectEntityReference.Source => sourceId,
        EffectEntityReference.Owner => ownerId,
        EffectEntityReference.ExplicitTarget => explicitTargetId,
        _ => throw new InvalidOperationException("Invalid entity reference.")
    };

    public static float HealthRatio(EffectEntitySnapshot entity) => entity.MaxHealth > 0 ? entity.Health / entity.MaxHealth : 0;
    public static bool HasTag(EffectEntitySnapshot entity, string tag) => !entity.Tags.IsDefault && entity.Tags.Contains(tag, StringComparer.Ordinal);
    private static IEnumerable<EffectEntitySnapshot> Lookup(EffectWorldSnapshot snapshot, string id) =>
        snapshot.Entities.TryGetValue(id, out var entity) ? [entity] : [];
}
