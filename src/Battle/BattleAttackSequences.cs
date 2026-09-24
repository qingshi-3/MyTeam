using System;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private void SelectAttackChainTarget(BattleUnitState owner, BattleUnitState target)
    {
        if (owner.Definition.AttackHitGrowth is not { } growth) return;
        if (growth.ResetOnTargetChange && owner.AttackChainTargetId != target.RuntimeId)
        {
            owner.AttackHitStacks = 0;
            ProjectAttackGrowth(owner);
        }
        owner.AttackChainTargetId = target.RuntimeId;
    }

    private void RegisterGrowingHit(BattleUnitState owner, BattleUnitState target)
    {
        if (!owner.Alive || owner.Definition.AttackHitGrowth is not { } growth) return;
        // Old arrows already in flight may still land after a switch. They cannot restart
        // or contaminate the base form's new target chain; the retention upgrade keeps them.
        if (growth.ResetOnTargetChange && owner.AttackChainTargetId != target.RuntimeId) return;
        owner.AttackHitStacks = checked(owner.AttackHitStacks + 1);
        ProjectAttackGrowth(owner);
        Emit("growth", owner.RuntimeId, target.RuntimeId, owner.AttackHitStacks, owner.Position, "");
    }

    private static void ProjectAttackGrowth(BattleUnitState owner)
    {
        var growth = owner.Definition.AttackHitGrowth!;
        owner.Attributes.ApplyModifier(new CompiledAttributeModifier(CombatAttribute.AttackSpeed,
            AttributeModifierOperation.Add,
            new CompiledConstantMagnitude(owner.AttackHitStacks * growth.AttackSpeedPerHit), 0, "attack-chain"),
            CombatSourceRef.Unit(owner.Definition.ContentId, owner.RuntimeId, owner.SourceInstanceId));
    }

    private void AdvanceGrowingAttacks(BattleUnitState owner, BattleUnitState target)
    {
        if (owner.AttackCooldown > 0) { owner.Mode = BattleUnitMode.Recovering; return; }
        SelectAttackChainTarget(owner, target);
        // Keep fractional progress and permit multiple launches in one fixed step. Rounding
        // each cooldown to one tick would silently cap this hero at ten attacks per second.
        while (owner.AttackCycleProgress >= 1 && owner.Alive && target.Alive)
        {
            owner.AttackCycleProgress -= 1;
            Attack(owner, target);
        }
        owner.AttackCycleProgress += 1 / owner.PreciseAttackTicks;
    }

    private void AdvanceProjectileSequence(BattleUnitState owner)
    {
        var sequence = owner.ProjectileSequence!;
        var progress = sequence.IntervalProgress + (float)(1 / (owner.PreciseAttackTicks * sequence.Operation.AttackIntervalRatio));
        owner.ProjectileSequence = sequence with { IntervalProgress = progress };
        while (owner.ProjectileSequence is { IntervalProgress: >= 1 } current && owner.Alive)
        {
            owner.ProjectileSequence = current with { IntervalProgress = current.IntervalProgress - 1 };
            FireSequenceShot(owner);
        }
        owner.Mode = BattleUnitMode.Casting;
        owner.LastActionKind = BattleActionKind.Ability;
        _movement!.ReleaseGoal(owner.RuntimeId);
    }

    private void FireSequenceShot(BattleUnitState owner)
    {
        var sequence = owner.ProjectileSequence!;
        // A multi-target volley selects its group once. Later arrows skip unavailable
        // members, without silently increasing the number of targets promised by the skill.
        var candidates = _units.Where(unit => unit.Alive && unit.Team != owner.Team &&
                (sequence.TargetGroup.IsDefault || sequence.TargetGroup.Contains(unit.RuntimeId)) &&
                BattlefieldSpace.IsWithinReach(owner, unit, owner.AttackRange) && HasLineAccess(owner, unit))
            .OrderBy(unit => BattlefieldSpace.EdgeDistance(owner, unit))
            .ThenBy(unit => unit.RuntimeId, StringComparer.Ordinal).Take(sequence.Operation.MaxTargets).ToArray();
        if (sequence.Operation.MaxTargets > 1 && sequence.TargetGroup.IsDefault)
        {
            sequence = sequence with { TargetGroup = candidates.Select(unit => unit.RuntimeId).ToImmutableArray() };
            owner.ProjectileSequence = sequence;
        }
        var target = sequence.Operation.MaxTargets > 1 && candidates.Length > 0
            ? candidates[(sequence.Operation.ShotCount - sequence.RemainingShots) % candidates.Length]
            : _units.FirstOrDefault(unit => unit.RuntimeId == sequence.TargetId && unit.Alive && unit.Team != owner.Team &&
                BattlefieldSpace.IsWithinReach(owner, unit, owner.AttackRange) && HasLineAccess(owner, unit));
        if (sequence.Operation.MaxTargets == 1) target ??= _units.Where(unit => unit.Alive && unit.Team != owner.Team &&
                BattlefieldSpace.IsWithinReach(owner, unit, owner.AttackRange) && HasLineAccess(owner, unit))
            .OrderBy(unit => BattlefieldSpace.EdgeDistance(owner, unit)).ThenBy(unit => unit.RuntimeId, StringComparer.Ordinal)
            .FirstOrDefault();
        if (target is null) { owner.ProjectileSequence = null; return; }
        SelectAttackChainTarget(owner, target);
        SetActionTarget(owner, target);
        var damage = EffectiveDamage(owner) * sequence.Operation.AttackDamageMultiplier;
        if (owner.Definition.ProjectileWindupSeconds > 0)
            BeginProjectileWindup(owner, target, sequence.Origin, sequence.Operation.AttackDamageMultiplier, sequence.Operation.AttackIntervalRatio);
        else
        {
            LaunchProjectile(owner, target, damage, sequence.Origin);
            Emit("skill_arrow", owner.RuntimeId, target.RuntimeId, damage, target.Position, "attack");
        }
        owner.ProjectileSequence = sequence.RemainingShots == 1 ? null : sequence with
        {
            RemainingShots = sequence.RemainingShots - 1, TargetId = target.RuntimeId
        };
        owner.ManaLockedUntilTick = Math.Max(owner.ManaLockedUntilTick, TickIndex + 1);
    }
}
