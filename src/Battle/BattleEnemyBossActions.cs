using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Abilities;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Battle;

internal sealed partial class BattleEnemyActionScope
{
    private sealed record SeatGuard(int Team, float Multiplier, ImmutableArray<string> Targets);
    private sealed record ConeAfterglow(int Id, string OwnerId, int Team, CombatSourceRef Origin,
        Vector2 Start, Vector2 End, float Range, float Angle, float Damage, int NextTick, int Interval, int Remaining);

    private void RegisterSeatGuard(BattleUnitView owner, CompiledEnemySeatGuard rule)
    {
        if (_enemyActions.SeatGuards.ContainsKey(owner.RuntimeId)) return;
        var targets = read.Units.Where(unit => unit.RuntimeId != owner.RuntimeId && unit.Alive &&
            unit.Team == owner.Team && !unit.IsTemporary && read.IsInitialNative(unit.RuntimeId) &&
            rule.GuardCells.Contains(unit.Cell)).OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal)
            .Select(unit => unit.RuntimeId).ToImmutableArray();
        _enemyActions = _enemyActions with { SeatGuards = _enemyActions.SeatGuards.Add(owner.RuntimeId,
            new(owner.Team, rule.DamageMultiplier, targets)) };
        foreach (var target in targets) commands.SeatGuardMarker(owner.RuntimeId, target, true);
    }

    public float ModifyIncomingDamage(BattleCombatCalculationRequest request, float amount)
    {
        if (amount <= 0 || request.Kind != BattleCombatCalculationKind.Damage) return amount;
        var target = read.Units.FirstOrDefault(unit => unit.RuntimeId == request.TargetRuntimeId);
        if (target is not { Alive: true }) return amount;
        foreach (var pair in _enemyActions.SeatGuards.OrderBy(value => value.Key, StringComparer.Ordinal))
        {
            if (!pair.Value.Targets.Contains(target.RuntimeId) || target.Team != pair.Value.Team) continue;
            var source = read.Units.FirstOrDefault(unit => unit.RuntimeId == pair.Key);
            if (source is { Alive: true } && source.Team == pair.Value.Team) amount *= pair.Value.Multiplier;
        }
        return amount;
    }

    // Only formal defeat calls this. A cast cancellation or displacement does not unbind protection.
    public void ObserveDefeated(string id)
    {
        foreach (var pair in _enemyActions.SeatGuards.ToArray())
        {
            if (pair.Key == id)
            {
                _enemyActions = _enemyActions with { SeatGuards = _enemyActions.SeatGuards.Remove(pair.Key) };
                foreach (var target in pair.Value.Targets) commands.SeatGuardMarker(pair.Key, target, false);
            }
            else if (pair.Value.Targets.Contains(id))
            {
                _enemyActions = _enemyActions with { SeatGuards = _enemyActions.SeatGuards.SetItem(pair.Key,
                    pair.Value with { Targets = pair.Value.Targets.Remove(id) }) };
                commands.SeatGuardMarker(pair.Key, id, false);
            }
        }
    }

    private void AdvanceStoneQuake(BattleUnitView owner, EnemyCast cast, CompiledEnemyStoneQuake rule, int elapsed)
    {
        if (cast.Step < rule.PulseCount && elapsed >= rule.WindupTicks + cast.Step * rule.PulseIntervalTicks)
        {
            if (cast.Step == 0) EmitEnemyAction(cast, "stone_quake", "release",
                (rule.PulseCount - 1) * rule.PulseIntervalTicks + rule.RecoveryTicks, rule.Radius);
            var damage = owner.Damage * rule.AttackMultiplier + owner.Armor * rule.ArmorMultiplier;
            foreach (var target in read.Units.Where(unit => unit.Alive && unit.Team != cast.Team &&
                owner.Position.DistanceTo(unit.Position) <= rule.Radius && read.HasLineAccess(owner.Position, unit.Position))
                .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray())
                commands.Hit(owner.RuntimeId, target.RuntimeId, damage, cast.Origin, EffectDamageType.Normal, isAreaDamage: true);
            SetEnemyCast(cast with { Step = cast.Step + 1 });
        }
        if (elapsed >= rule.WindupTicks + (rule.PulseCount - 1) * rule.PulseIntervalTicks + rule.RecoveryTicks &&
            _enemyActions.Casts.TryGetValue(owner.RuntimeId, out var current) && current.Step == rule.PulseCount &&
            owner.Alive && !read.Controlled(owner.RuntimeId) && !read.Displacing(owner.RuntimeId))
        {
            commands.CompletionArmor(owner.RuntimeId, rule.CompletionArmor, rule.ArmorDurationTicks);
            Cancel(owner.RuntimeId);
        }
    }

    private void ReleaseAfterglow(BattleUnitView owner, EnemyCast cast, CompiledConeBreath rule)
    {
        var id = _enemyActions.Sequence + 1;
        var flame = new ConeAfterglow(id, owner.RuntimeId, cast.Team, cast.Origin, cast.Start, cast.End,
            rule.Range, rule.AngleDegrees, owner.Damage * rule.AfterglowAttackMultiplier,
            TickIndex + rule.AfterglowIntervalTicks, rule.AfterglowIntervalTicks,
            rule.AfterglowDurationTicks / rule.AfterglowIntervalTicks);
        _enemyActions = _enemyActions with { Sequence = id, Afterglows = _enemyActions.Afterglows.Add(id, flame) };
        // Distinct facts: never emit the cone breath release cue again.
        Emit("cone_afterglow_start", owner.RuntimeId, "", 0, cast.End, "", entityId: id, origin: cast.Start,
            enemyAction: new("cone_embers", "release", TickIndex, rule.AfterglowDurationTicks, rule.Range, rule.AngleDegrees));
    }

    private void AdvanceAfterglows()
    {
        foreach (var flame in _enemyActions.Afterglows.Values.OrderBy(value => value.Id).ToArray())
        {
            if (TickIndex < flame.NextTick) continue;
            var heading = (flame.End - flame.Start).Normalized();
            foreach (var target in read.Units.Where(unit => unit.Alive && unit.Team != flame.Team)
                .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray())
            {
                var offset = target.Position - flame.Start;
                var distance = offset.Length();
                var extraAngle = Mathf.Asin(Mathf.Clamp(target.BodyRadius / Math.Max(.001f, distance), 0, 1));
                if (distance - target.BodyRadius > flame.Range ||
                    (distance > target.BodyRadius && Math.Abs(heading.AngleTo(offset)) > Mathf.DegToRad(flame.Angle / 2) + extraAngle) ||
                    !read.HasLineAccess(flame.Start, target.Position)) continue;
                commands.PeriodicHit(flame.OwnerId, target.RuntimeId, flame.Damage, flame.Origin);
            }
            if (flame.Remaining == 1)
            {
                _enemyActions = _enemyActions with { Afterglows = _enemyActions.Afterglows.Remove(flame.Id) };
                Emit("cone_afterglow_end", flame.OwnerId, "", 0, flame.End, "", entityId: flame.Id, origin: flame.Start);
            }
            else _enemyActions = _enemyActions with { Afterglows = _enemyActions.Afterglows.SetItem(flame.Id,
                flame with { NextTick = flame.NextTick + flame.Interval, Remaining = flame.Remaining - 1 }) };
        }
    }
}
