using System;
using System.Linq;
using System.Collections.Immutable;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Battle;

internal sealed partial class BattleMatrixAbilityScope
{
    private void ExecuteHeroAction(CompiledMatrixOperation operation, BattleUnitState owner, string targetId, CombatSourceRef origin, Vector2? center = null)
    {
        if (!owner.Alive) return;
        if (operation.Kind == MatrixOperationKind.AttackSpeedForOriginalAttacks)
        {
            var key = owner.RuntimeId + ":" + operation.Key;
            foreach (var previous in _matrixAbilities.AttackSpeedLeases.Where(lease => lease.Key == key))
                owner.Attributes.Remove(previous.Handle);
            var handle = owner.Attributes.ApplyModifier(new(CombatAttribute.AttackSpeed,
                AttributeModifierOperation.Add, new CompiledConstantMagnitude(operation.Amount), 0, operation.Key), origin);
            _matrixAbilities = _matrixAbilities with { AttackSpeedLeases = _matrixAbilities.AttackSpeedLeases
                .Where(lease => lease.Key != key).ToImmutableArray().Add(new(owner.RuntimeId, key,
                    operation.Count, world.Tick + operation.DurationTicks, handle)) };
            world.Present("original_attack_speed", owner.RuntimeId, owner.RuntimeId, operation.Count, owner.Position, "");
            return;
        }
        if(operation.Kind==MatrixOperationKind.GroundStorm)
        {
            CreateStorm(operation,owner,targetId,origin,center);
            return;
        }
        var target = MatrixUnit(targetId);
        if (target is null || !target.Alive || target.Team == owner.Team) return;
        if(operation.Kind==MatrixOperationKind.FrontalAttackGuard)
        {
            var direction=(target.Position-owner.Position).Normalized();
            if(direction.IsZeroApprox())direction=owner.Team==0?Vector2.Right:Vector2.Left;
            _matrixAbilities=_matrixAbilities with {Guards=_matrixAbilities.Guards.Where(g=>g.Owner!=owner.RuntimeId).ToImmutableArray()
                .Add(new(owner.RuntimeId,direction,world.Tick+operation.DurationTicks,operation.Radius,operation.Amount))};
            world.ApplyMatrixMarker(owner.RuntimeId,owner.RuntimeId,"frontal_attack_guard","方向盾",operation.DurationTicks,operation.Amount);
            world.Present("frontal_guard_start",owner.RuntimeId,target.RuntimeId,operation.Radius,owner.Position,"");
            return;
        }
        if (operation.Kind == MatrixOperationKind.OptionalMeleeStrike)
        {
            if (!BattlefieldSpace.IsWithinReach(owner, target, .35f) || !world.HasLineAccess(owner, target)) return;
            MatrixHit(owner, target, owner.Attributes.GetValue(CombatAttribute.SpellPower) * operation.SpellPowerRatio, origin);
            if (!target.Alive) return;
            world.Control(owner.RuntimeId, target.RuntimeId, operation.DurationTicks);
            if (operation.Amount <= 0) return;
            var push = new CompiledDisplacementOperation(DisplacementKind.Knockback, new CompiledExplicitTargetQuery(),
                operation.Amount, 3, 0, false, false, 0, 0, 0, EffectDamageType.Normal, null, 0);
            if (displacements.PrepareDisplacementOperation(push, owner, target.RuntimeId, out var legal))
                displacements.ExecuteDisplacementOperation(push, legal, owner, origin);
            return;
        }
        // Legal nearby landing uses the shared body/terrain/reservation checks.
        // Failure leaves the caster in place and retains the same locked target direction.
        var start = owner.Position;
        var heading = target.Position - start;
        if (heading.LengthSquared() <= .000001f) heading = owner.Team == 0 ? Vector2.Right : Vector2.Left;
        displacements.TryBlinkBeside(owner, target, operation.Amount, origin);
        var after = target.Position - owner.Position;
        if (after.LengthSquared() > .000001f) heading = after;
        heading = heading.Normalized();
        var travel = heading * operation.Range;
        var fraction = BattlefieldSpace.FirstTerrainHitFraction(owner.Position, travel, operation.Radius,
            BattlefieldLayout.Width, BattlefieldLayout.Height, world.CanOccupyTerrain);
        var end = owner.Position + travel * fraction;
        world.Present("line_slash_start", owner.RuntimeId, target.RuntimeId, operation.Radius, owner.Position, "skill_cast");
        world.Present("line_slash_end", owner.RuntimeId, target.RuntimeId, operation.Radius, end, "");
        foreach (var hit in world.Units.Where(unit => unit.Alive && unit.Team != owner.Team &&
                     BattlefieldSpace.PointSegmentDistance(unit.Position, owner.Position, end) <= operation.Radius + unit.BodyRadius)
                 .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray())
        {
            MatrixHit(owner, hit, owner.Damage * operation.AttackRatio, origin, isAreaDamage:true);
            if (hit.Alive) world.ApplyDisarm(owner.RuntimeId, hit.RuntimeId, operation.DurationTicks);
        }
    }

    public bool WantsOriginalAttack(BattleCombatEvent fact) => fact.Kind == BattleCombatEventKind.AttackDeclared &&
        _matrixAbilities.AttackSpeedLeases.Any(lease => lease.Owner == fact.SourceRuntimeId);

    public void ObserveOriginalAttack(BattleCombatEvent fact)
    {
        if (!WantsOriginalAttack(fact) || string.IsNullOrEmpty(fact.ActionId)) return;
        foreach (var lease in _matrixAbilities.AttackSpeedLeases.Where(lease => lease.Owner == fact.SourceRuntimeId).ToArray())
        {
            if (lease.LastAction == fact.ActionId) continue;
            _matrixAbilities = _matrixAbilities with { AttackSpeedLeases = _matrixAbilities.AttackSpeedLeases.Remove(lease) };
            if (lease.Remaining <= 1)
                MatrixUnit(lease.Owner)?.Attributes.Remove(lease.Handle);
            else
                _matrixAbilities = _matrixAbilities with { AttackSpeedLeases = _matrixAbilities.AttackSpeedLeases
                    .Add(lease with { Remaining = lease.Remaining - 1, LastAction = fact.ActionId }) };
        }
        // The declaration occurs after this action's cadence has already been chosen.
        // Removing the final lease must not rewrite that third action's cooldown.
    }

    private void PruneOriginalAttackSpeedLeases()
    {
        foreach (var lease in _matrixAbilities.AttackSpeedLeases.Where(lease => lease.Expires <= world.Tick || MatrixUnit(lease.Owner)?.Alive != true).ToArray())
        {
            MatrixUnit(lease.Owner)?.Attributes.Remove(lease.Handle);
            _matrixAbilities = _matrixAbilities with { AttackSpeedLeases = _matrixAbilities.AttackSpeedLeases.Remove(lease) };
        }
    }
}
