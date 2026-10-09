using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.Battle;

// Query capability only: implementations cannot provide mutable unit state.
internal interface IBattleMatrixReadWorld
{
    IReadOnlyList<BattleUnitView> Units { get; }
    BattleUnitView? FindUnit(string id);
    float Counter(string ownerId, string key);
    int Poison(string id);
    int Chill(string id);
    int Ember(string id);
    bool ActionDisabled(string id);
}

// Owns authored target ordering, spatial selection and condition semantics.
internal sealed class BattleMatrixQueries(IBattleMatrixReadWorld world)
{
    private readonly IBattleMatrixReadWorld _world = world;
    internal IEnumerable<BattleUnitView> Select(CompiledMatrixOperation op, BattleUnitView owner, string explicitTarget,
        BattleCombatEvent? fact, Vector2? center = null)
    {
        var eventTarget = _world.FindUnit(fact?.TargetRuntimeId ?? explicitTarget);
        var eventSource = _world.FindUnit(fact?.SourceRuntimeId ?? "");
        if (center is { } fixedCenter && op.Radius > 0)
            return _world.Units.Where(u => u.Alive && u.Team != owner.Team && u.Position.DistanceTo(fixedCenter) <= op.Radius +
                (op.Key.Length > 0 ? _world.Counter(owner.RuntimeId, op.Key) * op.RadiusPerCounter : 0) && MatchesCondition(op.TargetCondition, u, op.HealthThreshold))
                .OrderBy(u => u.Position.DistanceSquaredTo(fixedCenter)).ThenBy(u => u.RuntimeId, StringComparer.Ordinal).Take(op.MaxTargets).ToArray();
        var origin = op.Target == MatrixTargetKind.OtherEnemiesNearTarget ? eventTarget?.Position ?? owner.Position : center ?? (op.Target is MatrixTargetKind.EventNearbyAllies or MatrixTargetKind.EventNearbyEnemies
            ? eventTarget?.Position ?? owner.Position : owner.Position);
        if (op.Target == MatrixTargetKind.NearWoundedAllyEnemies) origin = _world.Units.Where(u => u.Alive && u.Team == owner.Team)
            .OrderBy(u => u.Health / u.MaxHealth).ThenBy(u => u.RuntimeId, StringComparer.Ordinal).FirstOrDefault()?.Position ?? owner.Position;
        if (op.Target == MatrixTargetKind.NearEventSourceAllies) origin = eventSource?.Position ?? owner.Position;
        IEnumerable<BattleUnitView> candidates = op.Target switch
        {
            MatrixTargetKind.Self => [owner],
            MatrixTargetKind.EventSource => eventSource is null ? [] : [eventSource],
            MatrixTargetKind.EventTarget => eventTarget is null ? [] : [eventTarget],
            MatrixTargetKind.EventVictim => _world.FindUnit(eventTarget?.ActionTargetRuntimeId ?? "") is { } victim ? [victim] : [],
            MatrixTargetKind.CurrentEnemy => _world.Units.Where(u => u.RuntimeId == explicitTarget && u.Team != owner.Team),
            MatrixTargetKind.CurrentOrNearestEnemy => _world.Units.Where(u => u.Team != owner.Team),
            MatrixTargetKind.OwnedSummons => _world.Units.Where(u => u.Team == owner.Team && u.SummonerRuntimeId == owner.RuntimeId),
            MatrixTargetKind.TemporaryAllies => _world.Units.Where(u => u.Team == owner.Team && u.IsTemporary),
            MatrixTargetKind.NearestCaster => _world.Units.Where(u => u.Team == owner.Team && u.HasManaSkill),
            MatrixTargetKind.NearestBoostableCaster => _world.Units.Where(u => u.Team == owner.Team && u.HasManaSkill &&
                u.AbilityLoadout?.Abilities.Any(a => a.IsActiveSkill && a.Operations.Any(HasBoostableValue)) == true),
            MatrixTargetKind.NearestWoundedAlly or MatrixTargetKind.LowestWoundedHealthAllies => _world.Units.Where(u => u.Team == owner.Team && u.Health < u.MaxHealth),
            MatrixTargetKind.NearestSummoner => _world.Units.Where(u => u.Team == owner.Team && u.Definition.Tags.Contains("summoner")),
            MatrixTargetKind.LowestHealthAllies or MatrixTargetKind.NearestAllies or MatrixTargetKind.LowestManaAllies or MatrixTargetKind.FrontAlly or MatrixTargetKind.EventNearbyAllies or MatrixTargetKind.NearEventSourceAllies => _world.Units.Where(u => u.Team == owner.Team),
            _ => _world.Units.Where(u => u.Team != owner.Team)
        };
        candidates = candidates.Where(u => u.Alive && (!op.ExcludeSelf || u != owner) &&
            (op.Target is not (MatrixTargetKind.OtherEnemy or MatrixTargetKind.OtherEnemiesNearTarget) || u.RuntimeId != explicitTarget) && u.Position.DistanceTo(origin) <= op.Range &&
            MatchesCondition(op.TargetCondition, u, op.HealthThreshold));
        if (op.Target == MatrixTargetKind.LowestManaAllies) candidates = candidates.Where(u => u.MaxMana > 0 && u.CurrentMana < u.MaxMana);
        if (op.Target == MatrixTargetKind.FrontEnemies)
        {
            var aim = _world.FindUnit(explicitTarget)?.Position - owner.Position ?? new Vector2(owner.Team == 0 ? 1 : -1, 0);
            var direction = aim.Normalized(); candidates = candidates.Where(u => (u.Position - owner.Position).Normalized().Dot(direction) >= .3f);
            var extra = op.Key.Length > 0 ? _world.Counter(owner.RuntimeId, op.Key) * op.RadiusPerCounter : 0;
            candidates = _world.Units.Where(u => u.Alive && u.Team != owner.Team && u.Position.DistanceTo(owner.Position) <= op.Range + extra &&
                (u.Position - owner.Position).Normalized().Dot(direction) >= .3f);
        }
        candidates = op.Target switch
        {
            MatrixTargetKind.CurrentOrNearestEnemy => candidates.OrderBy(u => u.RuntimeId == explicitTarget ||
                u.RuntimeId == owner.ActionTargetRuntimeId ? 0 : 1).ThenBy(u => u.Position.DistanceSquaredTo(origin))
                .ThenBy(u => u.RuntimeId, StringComparer.Ordinal),
            MatrixTargetKind.LowestHealthAllies or MatrixTargetKind.LowestHealthEnemies or MatrixTargetKind.LowestWoundedHealthAllies => candidates.OrderBy(u => u.Health / u.MaxHealth).ThenBy(u => u.RuntimeId, StringComparer.Ordinal),
            MatrixTargetKind.BacklineEnemy => candidates.OrderByDescending(u => owner.Team == 0 ? u.Position.X : -u.Position.X).ThenBy(u => u.RuntimeId, StringComparer.Ordinal),
            MatrixTargetKind.LowestAbsoluteHealthEnemy => candidates.OrderBy(u => u.Health).ThenBy(u => u.RuntimeId, StringComparer.Ordinal),
            MatrixTargetKind.LowestManaAllies => candidates.OrderBy(u => u.CurrentMana / u.MaxMana).ThenBy(u => u.RuntimeId, StringComparer.Ordinal),
            MatrixTargetKind.MostPoisonEnemies => candidates.OrderByDescending(u => _world.Poison(u.RuntimeId)).ThenBy(u => u.RuntimeId, StringComparer.Ordinal),
            MatrixTargetKind.MostEmberEnemies => candidates.OrderByDescending(u => _world.Ember(u.RuntimeId)).ThenBy(u => u.RuntimeId, StringComparer.Ordinal),
            MatrixTargetKind.DenseEnemies => candidates.OrderByDescending(u => _world.Units.Count(v => v.Alive && v.Team != owner.Team && v.Position.DistanceTo(u.Position) <= Math.Max(.5f, op.Radius))).ThenBy(u => u.RuntimeId, StringComparer.Ordinal),
            MatrixTargetKind.FrontAlly => candidates.OrderByDescending(u => u.Team == 0 ? u.Position.X : -u.Position.X).ThenBy(u => u.RuntimeId, StringComparer.Ordinal),
            _ => candidates.OrderBy(u => u.Position.DistanceSquaredTo(origin)).ThenBy(u => u.RuntimeId, StringComparer.Ordinal)
        };
        var selected = candidates.Take(op.MaxTargets).ToArray();
        if (op.Radius > 0 && selected.Length > 0 && op.Target is MatrixTargetKind.DenseEnemies or MatrixTargetKind.CurrentEnemy)
        {
            var at = center ?? selected[0].Position;
            var radius = op.Radius + (op.Key.Length > 0 ? _world.Counter(owner.RuntimeId, op.Key) * op.RadiusPerCounter : 0);
            return _world.Units.Where(u => u.Alive && u.Team != owner.Team && u.Position.DistanceTo(at) <= radius)
                .OrderBy(u => u.Position.DistanceSquaredTo(at)).ThenBy(u => u.RuntimeId, StringComparer.Ordinal).Take(op.MaxTargets).ToArray();
        }
        return selected;
    }

    internal bool MatchesCondition(MatrixCondition condition, BattleUnitView? unit, float threshold) => condition switch
    {
        MatrixCondition.Isolated => unit is not null && !_world.Units.Any(u => u.Alive && u.Team == unit.Team && u.RuntimeId != unit.RuntimeId && u.Position.DistanceTo(unit.Position) <= 1.5f),
        MatrixCondition.None => true,
        MatrixCondition.Poisoned => unit is not null && _world.Poison(unit.RuntimeId) > 0,
        MatrixCondition.EmberMarked => unit is not null && _world.Ember(unit.RuntimeId) > 0,
        MatrixCondition.Chilled => unit is not null && _world.Chill(unit.RuntimeId) > 0,
        MatrixCondition.Controlled => unit is not null && (unit.DisabledTicks > 0 || _world.ActionDisabled(unit.RuntimeId)),
        MatrixCondition.LowHealth => unit is not null && unit.Health / unit.MaxHealth < threshold,
        MatrixCondition.Shielded => unit is not null && unit.Shield > 0,
        MatrixCondition.Unshielded => unit is not null && unit.Shield <= 0,
        MatrixCondition.Frozen => unit is not null && unit.Statuses.Any(s => s.StableId == "matrix_freeze"),
        _ => false
    };

    // A next-skill numeric lease must have an actual effect to modify; pure summons and status-only
    // casts are not legal recipients. Inspect compiled atoms rather than maintaining hero ids.
    private static bool HasBoostableValue(CompiledAbilityOperation operation) => operation switch
    {
        CompiledMatrixOperation matrix => MatrixAbilityCompiler.Flatten(matrix).Any(o =>
            o.Kind is MatrixOperationKind.Damage or MatrixOperationKind.Heal or MatrixOperationKind.Shield or
                MatrixOperationKind.Mana or MatrixOperationKind.DashStrike or MatrixOperationKind.SkillVolley or MatrixOperationKind.LineDamage or
                MatrixOperationKind.OptionalMeleeStrike or MatrixOperationKind.BlinkLineDisarm or MatrixOperationKind.GroundStorm or
                MatrixOperationKind.RandomEnemyChannel or MatrixOperationKind.RetreatDualShot or MatrixOperationKind.HealingSeriesShield or MatrixOperationKind.KillRefundAttack),
        CompiledEffectAbilityOperation effect => !effect.Binding.Effects.IsEmpty,
        CompiledProjectileSequenceAbilityOperation => true,
        CompiledDisplacementOperation motion => motion.ImpactDamage > 0 || motion.AttackRatio > 0,
        CompiledBattleValueOperation value => value.Action is BattleValueAction.Damage or BattleValueAction.Heal or BattleValueAction.Shield,
        CompiledConsumeStatusOperation consume => consume.DamageMultiplier > 0,
        CompiledChargedLineOperation => true,
        _ => false
    };

}
