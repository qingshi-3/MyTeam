using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

public sealed record TrampleState(CompiledTrampleOperation Operation, CombatSourceRef Origin,
    Vector2 Start, Vector2 End, Vector2 Position, int StartTick, int Team,
    ImmutableHashSet<string> HitIds, int RecoveryUntil = 0);
public sealed record BattleTrampleCue(string WarningVfx, string RushVfx, int StartTick,
    int ChargeTicks, float Radius, Vector2 Start, Vector2 End);

public sealed partial class BattleSimulation
{
    private bool CanAimTrample(BattleUnitState owner, BattleUnitState target, CompiledTrampleOperation skill)
    {
        var distance = owner.Position.DistanceTo(target.Position);
        return target.Alive && target.Team != owner.Team && distance <= skill.Range && distance > 1.5f &&
            BattlefieldSpace.FirstTerrainHitFraction(owner.Position,
                (target.Position - owner.Position).Normalized() * Math.Min(distance, skill.Distance),
                owner.BodyRadius, Width, Height, cell => _config.FloorRule.CanOccupy(cell)) * Math.Min(distance, skill.Distance) > 1.5f;
    }

    private void BeginTrample(BattleUnitState owner, BattleUnitState target,
        CompiledTrampleOperation skill, CompiledAbilityDefinition ability)
    {
        var delta = (target.Position - owner.Position).Normalized() * skill.Distance;
        var fraction = BattlefieldSpace.FirstTerrainHitFraction(owner.Position, delta, owner.BodyRadius,
            Width, Height, cell => _config.FloorRule.CanOccupy(cell));
        owner.Trample = new(skill, AbilityOrigin(ability, owner.RuntimeId), owner.Position,
            owner.Position + delta * Math.Max(0, fraction - .001f), owner.Position, TickIndex, owner.Team,
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal));
        owner.Mode = BattleUnitMode.Casting;
        owner.LastActionKind = BattleActionKind.Ability;
        owner.LastAbilityName = ability.DisplayName;
        SetActionTarget(owner, target);
        _movement?.ReleaseUnit(owner.RuntimeId);
        EmitTrample("trample_charge", owner, owner.Trample);
    }

    private void AdvanceTramples()
    {
        foreach (var owner in _units.Where(unit => unit.Trample is not null)
                     .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray())
        {
            var checkpoint = new BattleWorldStateCheckpoint(this);
            try
            {
                using var resolution = _combatPipeline.BeginAuthoritativeResolution();
                AdvanceTrample(owner);
                resolution.Commit();
                checkpoint.Commit();
            }
            catch { checkpoint.Rollback(); throw; }
        }
    }

    private void AdvanceTrample(BattleUnitState owner)
    {
        if (owner.Trample is not { } cast) return;
        _movement?.ReleaseUnit(owner.RuntimeId);
        if (!owner.Alive || owner.Team != cast.Team || owner.DisabledTicks > 0 ||
            _statusScope.HasTag(owner.RuntimeId, StatusDefinitionCompiler.ActionDisabledTag) ||
            IsDisplacing(owner) || !owner.Position.IsEqualApprox(cast.Position))
        { CancelTrample(owner); return; }
        if (cast.RecoveryUntil > 0)
        {
            owner.Mode = BattleUnitMode.Recovering;
            if (TickIndex >= cast.RecoveryUntil) owner.Trample = null;
            return;
        }
        var rushTick = cast.StartTick + cast.Operation.ChargeTicks;
        if (TickIndex < rushTick) { owner.Mode = BattleUnitMode.Casting; return; }
        if (TickIndex == rushTick) EmitTrample("trample_rush", owner, cast);
        var direction = (cast.End - cast.Start).Normalized();
        var normal = new Vector2(-direction.Y, direction.X);
        var before = owner.Position;
        var distance = Math.Min(cast.Operation.Speed * BattleTiming.TickSeconds, before.DistanceTo(cast.End));
        var delta = direction * distance;
        // Resolve contacts in sweep order. Each side movement must itself sweep through free space;
        // if the corridor cannot be cleared, the charger stops before the obstructing body.
        var contacts = _units.Where(unit => unit.Alive && unit != owner && !IsAirborne(unit))
            .Select(unit => (Unit: unit, Time: LineHitTime(before, delta,
                owner.BodyRadius + BattlefieldSpace.BodyClearance, unit)))
            .Where(hit => hit.Time is not null).OrderBy(hit => hit.Time)
            .ThenBy(hit => hit.Unit.RuntimeId, StringComparer.Ordinal).ToArray();
        foreach (var (target, _) in contacts)
        {
            if (!target.Alive) continue;
            var offset = (target.Position - cast.Start).Dot(normal);
            var side = Math.Abs(offset) > .02f ? Math.Sign(offset) : cast.HitIds.Count % 2 == 0 ? 1 : -1;
            var clearance = owner.BodyRadius + target.BodyRadius + BattlefieldSpace.BodyClearance + .08f;
            var push = Math.Min(cast.Operation.SideDistance, Math.Max(.2f, clearance - Math.Abs(offset)));
            var from = target.Position;
            var destination = ClipDisplacementTravel(target, from, from + normal * side * push);
            if (IsDisplacing(target)) destination = from;
            if (destination.DistanceSquaredTo(from) > .0001f)
            {
                CancelTrample(target);
                CancelChargedLine(target);
                CancelProjectileWindups(target);
                target.ProjectileSequence = null;
                InterruptBattleChannels(target.RuntimeId);
                _movement?.ReleaseUnit(target.RuntimeId);
                target.Position = destination;
                target.DisabledTicks = Math.Max(target.DisabledTicks, 2);
                Emit("displacement", owner.RuntimeId, target.RuntimeId, 0, destination, "",
                    origin: from, displacement: new(DisplacementKind.Knockback, from, destination, TickIndex,
                        1, 1, true, false, 0, from, 0));
                PublishCombat(new BattleCombatEventDraft(BattleCombatEventKind.UnitMoved, cast.Origin,
                    owner.RuntimeId, target.RuntimeId, TickIndex, Cell: ToCombatCell(target.Cell), Position: ToCombatPoint(destination)));
            }
            if (!cast.HitIds.Contains(target.RuntimeId))
            {
                cast = cast with { HitIds = cast.HitIds.Add(target.RuntimeId) };
                owner.Trample = cast;
                if (target.Team != owner.Team)
                    ResolveLineSkillHit(owner, target, owner.Damage * cast.Operation.AttackMultiplier,
                        cast.Origin, EffectDamageType.Normal);
                Emit("trample_impact", owner.RuntimeId, target.RuntimeId, 0, target.Position, "");
            }
            if (!owner.Alive || owner.Trample is null) return;
            // No further hits beyond a body which could not actually be moved out of the path.
            if (target.Alive && LineHitTime(before, delta, owner.BodyRadius + BattlefieldSpace.BodyClearance, target) is not null)
                break;
        }
        var next = ClipDisplacementTravel(owner, before, before + delta);
        owner.Position = next;
        var finished = next.DistanceTo(cast.End) < .02f || next.DistanceTo(before + delta) > .002f;
        cast = cast with { Position = next, RecoveryUntil = finished ? TickIndex + cast.Operation.RecoveryTicks : 0 };
        owner.Trample = cast;
        owner.Mode = finished ? BattleUnitMode.Recovering : BattleUnitMode.Moving;
        var duration = Math.Max(1, (int)Math.Ceiling(cast.Start.DistanceTo(cast.End) / cast.Operation.Speed / BattleTiming.TickSeconds));
        Emit("displacement", owner.RuntimeId, owner.RuntimeId, 0, next, "",
            origin: cast.Start, displacement: new(DisplacementKind.Charge, cast.Start, cast.End,
                rushTick, duration, Math.Min(1, (TickIndex - rushTick + 1f) / duration), finished, false,
                0, cast.Start, 0));
        if (next.DistanceSquaredTo(before) > .000001f)
            PublishCombat(new BattleCombatEventDraft(BattleCombatEventKind.UnitMoved, cast.Origin,
                owner.RuntimeId, owner.RuntimeId, TickIndex, Cell: ToCombatCell(owner.Cell), Position: ToCombatPoint(next)));
        if (finished) EmitTrample("trample_end", owner, cast);
    }

    private void CancelTrample(BattleUnitState owner)
    {
        if (owner.Trample is not { } cast) return;
        owner.Trample = null;
        EmitTrample("trample_end", owner, cast);
        Emit("displacement", owner.RuntimeId, owner.RuntimeId, 0, owner.Position, "",
            origin: owner.Position, displacement: new(DisplacementKind.Charge, owner.Position, owner.Position,
                TickIndex, 1, 1, true, true, 0, owner.Position, 0));
    }

    private void EmitTrample(string type, BattleUnitState owner, TrampleState cast) =>
        Emit(type, owner.RuntimeId, owner.RuntimeId, 0, owner.Position, "", origin: cast.Start,
            trample: new(cast.Operation.WarningVfx, cast.Operation.RushVfx, cast.StartTick,
                cast.Operation.ChargeTicks, owner.BodyRadius, cast.Start, cast.End));
}
