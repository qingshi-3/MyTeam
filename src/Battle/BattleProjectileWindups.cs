using System;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Attributes;
using TowerAutobattler.Domain;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private void BeginProjectileWindup(BattleUnitState owner, BattleUnitState target,
        CombatSourceRef skillOrigin = default, float multiplier = 1, float intervalRatio = 1)
    {
        var definition = owner.Definition;
        var baseWindup = Math.Min(definition.ProjectileWindupSeconds,
            definition.AttackTicks * BattleTiming.TickSeconds * definition.AttackReleaseProgress);
        var seconds = baseWindup * intervalRatio / owner.Attributes.GetValue(CombatAttribute.AttackSpeed);
        // The action and its release use the same quantized simulation duration. At extreme
        // speed several independent windups may mature together; there is no one-shot cap.
        var ticks = Math.Max(1, (int)Math.Ceiling(seconds / BattleTiming.TickSeconds - .00001));
        owner.ProjectileWindups = owner.ProjectileWindups.Add(new(target.RuntimeId, TickIndex + ticks, multiplier, skillOrigin));
        SelectAttackChainTarget(owner, target);
        SetActionTarget(owner, target);
        Emit("attack_windup", owner.RuntimeId, target.RuntimeId, 0, target.Position, "",
            attackTiming: new(ticks * BattleTiming.TickSeconds / definition.AttackReleaseProgress, definition.AttackReleaseProgress));
    }

    private bool AdvanceProjectileWindups(BattleUnitState owner)
    {
        if (owner.ProjectileWindups.IsEmpty) return false;
        var ready = owner.ProjectileWindups.Where(shot => shot.ReleaseTick <= TickIndex).ToArray();
        owner.ProjectileWindups = owner.ProjectileWindups.Where(shot => shot.ReleaseTick > TickIndex).ToImmutableArray();
        foreach (var shot in ready)
        {
            var target = _units.FirstOrDefault(unit => unit.RuntimeId == shot.TargetId && unit.Alive && unit.Team != owner.Team);
            if (!owner.Alive || target is null || !BattlefieldSpace.IsWithinReach(owner, target, owner.AttackRange) || !HasLineAccess(owner, target))
            {
                Emit("attack_cancel", owner.RuntimeId, shot.TargetId, 0, owner.Position, "");
                continue;
            }
            if (shot.SkillOrigin.IsSpecified)
            {
                var damage = EffectiveDamage(owner) * shot.DamageMultiplier;
                LaunchProjectile(owner, target, damage, shot.SkillOrigin);
                Emit("skill_arrow", owner.RuntimeId, target.RuntimeId, damage, target.Position, "");
                owner.ManaLockedUntilTick = Math.Max(owner.ManaLockedUntilTick, TickIndex + 1);
            }
            else ResolveAttack(owner, target, timed: true);
        }
        return ready.Length > 0;
    }

    private void CancelProjectileWindups(BattleUnitState owner)
    {
        CancelChargedLine(owner);
        if (owner.ProjectileWindups.IsEmpty) return;
        owner.ProjectileWindups = [];
        Emit("attack_cancel", owner.RuntimeId, "", 0, owner.Position, "");
    }
}
