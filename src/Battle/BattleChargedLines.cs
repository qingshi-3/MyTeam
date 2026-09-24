using System;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Domain;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Battle;

public sealed record ChargedLineState(CompiledChargedLineOperation Operation, CombatSourceRef Origin,
    Vector2 Start, Vector2 End, int StartTick, int Team);
public sealed record BattleLineCue(string ChargeVfx, string ReleaseVfx, int StartTick, int ChargeTicks,
    float Radius, ChargedLineDelivery Delivery, float ReleaseProgress = .8f);

public sealed partial class BattleSimulation
{
    private bool CanAimChargedLine(BattleUnitState owner, BattleUnitState target, CompiledChargedLineOperation line) =>
        target.Alive && target.Team != owner.Team && owner.Position.DistanceTo(target.Position) <= line.Range &&
        BattlefieldSpace.FirstTerrainHitFraction(owner.Position, target.Position - owner.Position,
            line.Radius, Width, Height, cell => _config.FloorRule.CanOccupy(cell)) >= 1;

    private void BeginChargedLine(BattleUnitState owner, BattleUnitState target,
        CompiledChargedLineOperation operation, CompiledAbilityDefinition ability)
    {
        if (owner.ChargedLine is not null || !CanAimChargedLine(owner, target, operation) ||
            IsDisplacing(owner) || owner.DisabledTicks > 0)
            throw new InvalidOperationException("Prepared charged line no longer has a legal caster or target.");
        var heading = (target.Position - owner.Position).Normalized();
        if (heading.IsZeroApprox()) heading = owner.Team == 0 ? Vector2.Right : Vector2.Left;
        var travel = heading * operation.Range;
        var fraction = BattlefieldSpace.FirstTerrainHitFraction(owner.Position, travel, operation.Radius,
            Width, Height, cell => _config.FloorRule.CanOccupy(cell));
        owner.ChargedLine = new(operation, AbilityOrigin(ability, owner.RuntimeId), owner.Position,
            owner.Position + travel * fraction, TickIndex, owner.Team);
        owner.Mode = BattleUnitMode.Casting;
        owner.LastActionKind = BattleActionKind.Ability;
        owner.LastAbilityName = ability.DisplayName;
        SetActionTarget(owner, target);
        _movement?.ReleaseGoal(owner.RuntimeId);
        EmitChargedLine("line_charge", owner, owner.ChargedLine, "skill_cast");
    }

    private bool AdvanceChargedLine(BattleUnitState owner)
    {
        if (owner.ChargedLine is not { } cast) return false;
        _movement?.ReleaseGoal(owner.RuntimeId);
        owner.Mode = BattleUnitMode.Casting;
        // Source displacement or allegiance change invalidates the already shown line.
        if (!owner.Alive || owner.Team != cast.Team || !owner.Position.IsEqualApprox(cast.Start))
        {
            CancelChargedLine(owner);
            return true;
        }
        if (TickIndex < cast.StartTick + cast.Operation.ChargeTicks) return true;
        owner.ChargedLine = null;
        using var resolution = _combatPipeline.BeginAuthoritativeResolution();
        EmitChargedLine("line_release", owner, cast, "");
        var operation = cast.Operation;
        var damage = owner.Damage * operation.AttackMultiplier;
        if (operation.Delivery == ChargedLineDelivery.Projectile)
        {
            var distance = cast.Start.DistanceTo(cast.End);
            if (distance > .0001f)
            {
                var heading = (cast.End - cast.Start).Normalized();
                var projectile = new BattleProjectileState(++_projectileSequence, owner.RuntimeId, owner.Team,
                    cast.Start, heading * operation.ProjectileSpeed, operation.Radius, damage,
                    distance / operation.ProjectileSpeed, TickIndex, operation.MaximumHits, [], cast.Origin,
                    operation.DamageType, operation.SubsequentHitMultiplier, operation.ReleaseVfx,
                    ContinueAfterHitLimit: true);
                _projectiles.Add(projectile.Id, projectile);
                Emit("projectile_spawn", owner.RuntimeId, "", damage, cast.Start, "", projectile.Id,
                    cast.Start + heading, line: Cue(cast));
            }
        }
        else
        {
            var delta = cast.End - cast.Start;
            var hits = _units.Where(target => target.Alive && target.Team != owner.Team)
                .Select(target => (Target: target, Time: LineHitTime(cast.Start, delta, operation.Radius, target)))
                .Where(hit => hit.Time is not null).OrderBy(hit => hit.Time)
                .ThenBy(hit => hit.Target.RuntimeId, StringComparer.Ordinal).ToArray();
            var count = 0;
            foreach (var hit in hits)
            {
                if (!hit.Target.Alive) continue;
                ResolveLineSkillHit(owner, hit.Target, damage * Mathf.Pow(operation.SubsequentHitMultiplier, count),
                    cast.Origin, operation.DamageType);
                Emit("line_impact", owner.RuntimeId, hit.Target.RuntimeId, 0, hit.Target.Position, "");
                if (++count >= operation.MaximumHits) break;
            }
        }
        owner.AttackCooldown = Math.Max(owner.AttackCooldown, owner.EffectiveAttackTicks);
        resolution.Commit();
        return true;
    }

    private static float? LineHitTime(Vector2 start, Vector2 delta, float radius, BattleUnitState target)
    {
        var combined = radius + target.BodyRadius;
        if (start.DistanceSquaredTo(target.Position) <= combined * combined) return 0;
        return BattlefieldSpace.TryMovingCircleTimeOfImpact(start, delta, radius, target.Position,
            Vector2.Zero, target.BodyRadius, out var time) ? time : null;
    }

    private void ResolveLineSkillHit(BattleUnitState source, BattleUnitState target, float damage,
        CombatSourceRef origin, Effects.EffectDamageType damageType)
    {
        var applied = ApplyDamage(source.RuntimeId, source, target, damage, origin, damageType);
        RegisterGrowingHit(source, target);
        PublishCombat(new BattleCombatEventDraft(BattleCombatEventKind.SkillHitLanded, origin,
            source.RuntimeId, target.RuntimeId, TickIndex, RequestedValue: damage, AppliedValue: applied,
            EffectiveValue: applied, DamageType: damageType));
    }

    private void CancelChargedLine(BattleUnitState owner)
    {
        if (owner.ChargedLine is not { } cast) return;
        owner.ChargedLine = null;
        EmitChargedLine("line_cancel", owner, cast, "");
    }

    private bool ShouldHoldForChargedLine(BattleUnitState owner) =>
        owner.Definition.AbilityLoadout is { } loadout &&
        loadout.Abilities.SelectMany(ability => ability.Operations).OfType<CompiledChargedLineOperation>()
            .Any(line => line.HoldPosition && _units.Any(target => CanAimChargedLine(owner, target, line))) &&
        !_units.Any(target => target.Alive && target.Team != owner.Team &&
            BattlefieldSpace.IsWithinReach(owner, target, owner.AttackRange) && HasLineAccess(owner, target));

    private static BattleLineCue Cue(ChargedLineState cast) => new(cast.Operation.ChargeVfx,
        cast.Operation.ReleaseVfx, cast.StartTick, cast.Operation.ChargeTicks, cast.Operation.Radius, cast.Operation.Delivery);
    private void EmitChargedLine(string type, BattleUnitState owner, ChargedLineState cast, string animationCue) =>
        Emit(type, owner.RuntimeId, owner.RuntimeId, 0, cast.End, animationCue, origin: cast.Start, line: Cue(cast),
            attackTiming: type == "line_charge" ? new BattleAttackTiming(cast.Operation.ChargeTicks * BattleTiming.TickSeconds / .8f, .8f) : null);
}
