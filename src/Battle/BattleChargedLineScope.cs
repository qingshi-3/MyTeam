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

internal sealed class BattleChargedLineScope(IBattleDirectedActionWorld world, BattleCombatEventPipeline pipeline, BattleProjectileScope projectiles)
{
    private const int Width = BattlefieldLayout.Width, Height = BattlefieldLayout.Height;
    private static CombatCell ToCombatCell(Vector2I cell) => new(cell.X, cell.Y);
    private static CombatPoint ToCombatPoint(Vector2 position) => new(position.X, position.Y);
    private static void SetActionTarget(BattleUnitState owner, BattleUnitState target)
    { owner.ActionTargetRuntimeId = target.RuntimeId; owner.ActionTargetName = target.Definition.DisplayName; }
    private static float? LineHitTime(Vector2 start, Vector2 delta, float radius, BattleUnitState target) => BattleCollisionQueries.LineHitTime(start, delta, radius, target);
    public bool CanAimChargedLine(BattleUnitState owner, BattleUnitState target, CompiledChargedLineOperation line) =>
        target.Alive && target.Team != owner.Team && owner.Position.DistanceTo(target.Position) <= line.Range &&
        BattlefieldSpace.FirstTerrainHitFraction(owner.Position, target.Position - owner.Position,
            line.Radius, Width, Height, cell => world.CanOccupy(cell)) >= 1;

    public void BeginChargedLine(BattleUnitState owner, BattleUnitState target,
        CompiledChargedLineOperation operation, CompiledAbilityDefinition ability)
    {
        if (owner.ChargedLine is not null || !CanAimChargedLine(owner, target, operation) ||
            world.IsDisplacing(owner) || owner.DisabledTicks > 0)
            throw new InvalidOperationException("Prepared charged line no longer has a legal caster or target.");
        var heading = (target.Position - owner.Position).Normalized();
        if (heading.IsZeroApprox()) heading = owner.Team == 0 ? Vector2.Right : Vector2.Left;
        var travel = heading * operation.Range;
        var fraction = BattlefieldSpace.FirstTerrainHitFraction(owner.Position, travel, operation.Radius,
            Width, Height, cell => world.CanOccupy(cell));
        owner.ChargedLine = new(operation, world.AbilityOrigin(ability, owner.RuntimeId), owner.Position,
            owner.Position + travel * fraction, world.Tick, owner.Team);
        owner.Mode = BattleUnitMode.Casting;
        owner.LastActionKind = BattleActionKind.Ability;
        owner.LastAbilityName = ability.DisplayName;
        SetActionTarget(owner, target);
        world.ReleaseGoal(owner.RuntimeId);
        EmitChargedLine("line_charge", owner, owner.ChargedLine, "skill_cast");
    }

    public bool AdvanceChargedLine(BattleUnitState owner)
    {
        if (owner.ChargedLine is not { } cast) return false;
        world.ReleaseGoal(owner.RuntimeId);
        owner.Mode = BattleUnitMode.Casting;
        // Source displacement or allegiance change invalidates the already shown line.
        if (!owner.Alive || owner.Team != cast.Team || !owner.Position.IsEqualApprox(cast.Start))
        {
            CancelChargedLine(owner);
            return true;
        }
        if (world.Tick < cast.StartTick + cast.Operation.ChargeTicks) return true;
        owner.ChargedLine = null;
        using var resolution = pipeline.BeginAuthoritativeResolution();
        EmitChargedLine("line_release", owner, cast, "");
        var operation = cast.Operation;
        var damage = owner.Damage * operation.AttackMultiplier + owner.Attributes.GetValue(CombatAttribute.SpellPower) * operation.SpellPowerMultiplier;
        if (operation.Delivery == ChargedLineDelivery.Projectile)
        {
            var distance = cast.Start.DistanceTo(cast.End);
            if (distance > .0001f)
            {
                var heading = (cast.End - cast.Start).Normalized();
                var projectile = projectiles.Spawn(new BattleProjectileState(0, owner.RuntimeId, owner.Team,
                    cast.Start, heading * operation.ProjectileSpeed, operation.Radius, damage,
                    distance / operation.ProjectileSpeed, world.Tick, operation.MaximumHits, [], cast.Origin,
                    operation.DamageType, operation.SubsequentHitMultiplier, operation.ReleaseVfx,
                    ContinueAfterHitLimit: true));
                world.Present("projectile_spawn", owner.RuntimeId, "", damage, cast.Start, "", projectile.Id,
                    cast.Start + heading, line: Cue(cast));
            }
        }
        else
        {
            var delta = cast.End - cast.Start;
            var hits = world.Units.Where(target => target.Alive && target.Team != owner.Team)
                .Select(target => (Target: target, Time: LineHitTime(cast.Start, delta, operation.Radius, target)))
                .Where(hit => hit.Time is not null).OrderBy(hit => hit.Time)
                .ThenBy(hit => hit.Target.RuntimeId, StringComparer.Ordinal).ToArray();
            var count = 0;
            foreach (var hit in hits)
            {
                if (!hit.Target.Alive) continue;
                world.Hit(owner, hit.Target, damage * Mathf.Pow(operation.SubsequentHitMultiplier, count),
                    cast.Origin, operation.DamageType, isAreaDamage:operation.MaximumHits > 1);
                world.Present("line_impact", owner.RuntimeId, hit.Target.RuntimeId, 0, hit.Target.Position, "");
                if (++count >= operation.MaximumHits) break;
            }
        }
        owner.AttackCooldown = Math.Max(owner.AttackCooldown, owner.EffectiveAttackTicks);
        resolution.Commit();
        return true;
    }

    public void CancelChargedLine(BattleUnitState owner)
    {
        if (owner.ChargedLine is not { } cast) return;
        owner.ChargedLine = null;
        EmitChargedLine("line_cancel", owner, cast, "");
    }

    public bool ShouldHoldForChargedLine(BattleUnitState owner) =>
        owner.AbilityLoadout is { } loadout &&
        loadout.Abilities.SelectMany(ability => ability.Operations).OfType<CompiledChargedLineOperation>()
            .Any(line => line.HoldPosition && world.Units.Any(target => CanAimChargedLine(owner, target, line))) &&
        !world.Units.Any(target => target.Alive && target.Team != owner.Team &&
            BattlefieldSpace.IsWithinReach(owner, target, owner.AttackRange) && world.HasLineAccess(owner, target));

    private static BattleLineCue Cue(ChargedLineState cast) => new(cast.Operation.ChargeVfx,
        cast.Operation.ReleaseVfx, cast.StartTick, cast.Operation.ChargeTicks, cast.Operation.Radius, cast.Operation.Delivery);
    private void EmitChargedLine(string type, BattleUnitState owner, ChargedLineState cast, string animationCue) =>
        world.Present(type, owner.RuntimeId, owner.RuntimeId, 0, cast.End, animationCue, origin: cast.Start, line: Cue(cast),
            attackTiming: type == "line_charge" ? new BattleAttackTiming(cast.Operation.ChargeTicks * BattleTiming.TickSeconds / .8f, .8f) : null);
}
