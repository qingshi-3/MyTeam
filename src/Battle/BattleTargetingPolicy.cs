using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

internal interface IBattleTargetingWorld
{
    IReadOnlyList<BattleUnitState> Units { get; }
    BattleUnitState? DuelOpponent(BattleUnitState unit);
    bool CanAimHook(BattleUnitState owner, BattleUnitState target, CompiledHookOperation op);
    bool CanAimTrample(BattleUnitState owner, BattleUnitState target, CompiledTrampleOperation op);
    bool CanAimChargedLine(BattleUnitState owner, BattleUnitState target, CompiledChargedLineOperation op);
    string EnemyActionTarget(BattleUnitState owner, CompiledEnemyAction op);
    bool HasLineAccess(BattleUnitState owner, BattleUnitState target);
    bool IsRampartBody(BattleUnitState unit);
    BattleUnitState? SelectMovementTarget(BattleUnitState owner, IReadOnlyList<BattleUnitState> candidates);
    void ClearMovementTarget(string id);
}

internal sealed class BattleTargetingPolicy(IBattleTargetingWorld world, bool preferBoss)
{
    private IEnumerable<BattleUnitState> Allies(int team) => world.Units.Where(unit => unit.Team == team && unit.Alive);
    public string SelectAutomaticTarget(BattleUnitState owner, CompiledAbilityDefinition ability)
    {
        if (ability.Operations.OfType<CompiledHookOperation>().FirstOrDefault() is { } hook)
            return world.Units.Where(target => world.CanAimHook(owner,target,hook))
                .OrderByDescending(target => owner.Position.DistanceSquaredTo(target.Position))
                .ThenBy(target => target.RuntimeId,StringComparer.Ordinal).Select(target => target.RuntimeId).FirstOrDefault() ?? "";
        if (ability.Operations.OfType<CompiledGritPunchOperation>().FirstOrDefault() is { } punch)
            return world.Units.Where(target => target.Alive && target.Team != owner.Team &&
                    owner.Position.DistanceTo(target.Position) <= punch.Range && world.HasLineAccess(owner,target))
                .OrderBy(target => target.RuntimeId == owner.ActionTargetRuntimeId ? 0 : 1)
                .ThenBy(target => owner.Position.DistanceSquaredTo(target.Position))
                .ThenBy(target => target.RuntimeId,StringComparer.Ordinal).Select(target => target.RuntimeId).FirstOrDefault() ?? "";
        if (ability.Operations.OfType<CompiledEnemyAction>().FirstOrDefault() is { } enemyAction)
            return world.EnemyActionTarget(owner,enemyAction);
        if (ability.Operations.OfType<CompiledTrampleOperation>().FirstOrDefault() is { } trample)
            return world.Units.Where(target => world.CanAimTrample(owner, target, trample))
                .OrderByDescending(target => owner.Position.DistanceSquaredTo(target.Position))
                .ThenBy(target => target.RuntimeId, StringComparer.Ordinal)
                .Select(target => target.RuntimeId).FirstOrDefault() ?? string.Empty;
        if (ability.Operations.OfType<CompiledChargedLineOperation>().FirstOrDefault() is { } line)
            return world.Units.Where(target => world.CanAimChargedLine(owner, target, line))
                .OrderBy(target => owner.Position.DistanceSquaredTo(target.Position))
                .ThenBy(target => target.RuntimeId, StringComparer.Ordinal)
                .Select(target => target.RuntimeId).FirstOrDefault() ?? string.Empty;
        var candidates = world.Units.Where(target => target.Alive &&
            BattlefieldSpace.IsWithinReach(owner, target, owner.AttackRange) && world.HasLineAccess(owner, target));
        if (ability.AutomaticTarget == AbilityAutomaticTargetKind.WoundedAlly)
            return candidates.Where(target => target.Team == owner.Team && target.Health < target.MaxHealth)
                .OrderBy(target => target.Health / target.MaxHealth)
                .ThenBy(target => target.RuntimeId, StringComparer.Ordinal)
                .Select(target => target.RuntimeId).FirstOrDefault() ?? string.Empty;
        return candidates.Where(target => target.Team != owner.Team)
            .OrderBy(target => target.RuntimeId == owner.ActionTargetRuntimeId ? 0 : 1)
            .ThenBy(target => BattlefieldSpace.EdgeDistance(owner, target))
            .ThenBy(target => target.RuntimeId, StringComparer.Ordinal)
            .Select(target => target.RuntimeId).FirstOrDefault() ?? string.Empty;
    }

    public BattleUnitState? SelectTarget(BattleUnitState unit)
    {
        if (world.DuelOpponent(unit) is { } opponent) return world.SelectMovementTarget(unit, [opponent]);
        var taunter = unit.Statuses.Where(status => status.Behavior == StatusBehaviorKind.Taunt)
            .OrderByDescending(status => status.LastAppliedTick).ThenByDescending(status => status.ApplicationSequence)
            .Select(status => world.Units.FirstOrDefault(candidate => candidate.RuntimeId == status.SourceId &&
                candidate.Alive && candidate.Team != unit.Team)).FirstOrDefault(candidate => candidate is not null);
        if (taunter is not null) return world.SelectMovementTarget(unit, [taunter]);
        var enemies = Allies(1 - unit.Team).ToList();
        if (enemies.Count == 0)
        {
            world.ClearMovementTarget(unit.RuntimeId);
            return null;
        }
        IEnumerable<BattleUnitState> ordered;
        if (unit.Team == 0 && unit.Definition.IsHero && preferBoss)
        {
            ordered = enemies.OrderByDescending(enemy => enemy.Definition.IsBoss && BattlefieldSpace.EdgeDistance(unit, enemy) <= 3f)
                .ThenBy(enemy => BattlefieldSpace.EdgeDistance(unit, enemy)).ThenBy(enemy => enemy.RuntimeId, StringComparer.Ordinal);
        }
        else if (unit.Definition.Behavior.PreferBacklineTargets)
            ordered = enemies.OrderByDescending(enemy => enemy.AttackRange + enemy.HealingPower)
                .ThenBy(enemy => BattlefieldSpace.EdgeDistance(unit, enemy)).ThenBy(enemy => enemy.RuntimeId, StringComparer.Ordinal);
        else if (unit.Definition.Role == Content.UnitRole.Assassin)
            ordered = enemies.OrderByDescending(enemy => enemy.AttackRange).ThenBy(enemy => BattlefieldSpace.EdgeDistance(unit, enemy))
                .ThenBy(enemy => enemy.RuntimeId, StringComparer.Ordinal);
        else
            ordered = enemies.OrderBy(enemy => BattlefieldSpace.EdgeDistance(unit, enemy)).ThenBy(enemy => enemy.RuntimeId, StringComparer.Ordinal);
        var desired = ordered.First();
        var wall = enemies.Where(candidate => world.IsRampartBody(candidate) &&
                BattleCollisionQueries.LineHitTime(unit.Position, desired.Position-unit.Position, unit.BodyRadius*.5f, candidate) is not null)
            .OrderBy(candidate => unit.Position.DistanceSquaredTo(candidate.Position)).ThenBy(candidate => candidate.RuntimeId,StringComparer.Ordinal).FirstOrDefault();
        return world.SelectMovementTarget(unit, wall is null ? ordered.ToArray() : [wall]);
    }

}
