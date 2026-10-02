using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Domain;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

internal static class BattleCollisionQueries
{
    public static float? LineHitTime(Vector2 start, Vector2 delta, float radius, BattleUnitState target)
    {
        var combined = radius + target.BodyRadius;
        if (start.DistanceSquaredTo(target.Position) <= combined * combined) return 0;
        return BattlefieldSpace.TryMovingCircleTimeOfImpact(start, delta, radius, target.Position,
            Vector2.Zero, target.BodyRadius, out var time) ? time : null;
    }

}
