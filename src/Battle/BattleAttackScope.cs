using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Domain;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

// Authoritative attack rules may mutate a body's action state. They cannot obtain the simulation.
internal interface IBattleAttackWorld
{
    int Tick { get; }
    IReadOnlyList<BattleUnitState> Units { get; }
    CombatSourceRef Source(string id, BattleUnitState? unit = null);
    float DamageValue(BattleUnitState unit);
    float Damage(string sourceRuntimeId, BattleUnitState? source, BattleUnitState target, float raw,
        CombatSourceRef origin = default, EffectDamageType damageType = EffectDamageType.Normal,
        CombatDamageClass damageClass = CombatDamageClass.Other, string actionId = "");
    float Heal(string source, BattleUnitState target, float amount);
    float ReadCounter(BattleUnitState unit, string key);
    void WriteCounter(BattleUnitState unit, string key, float value);
    bool HasLineAccess(BattleUnitState source, BattleUnitState target);
    void LaunchProjectile(BattleUnitState source, BattleUnitState target, float damage, CombatSourceRef origin = default);
    void CancelChargedLine(BattleUnitState unit);
    void ReleaseGoal(string id);
    void Publish(BattleCombatEventDraft fact);
    void Present(string type, string source, string target, float value, Vector2 position, string cue,
        Vector2 origin = default, BattleVfxCue? vfx = null, BattleAttackTiming? attackTiming = null, string sourceVfx = "");
}

internal sealed class BattleAttackScope(IBattleAttackWorld world, BattleCombatEventPipeline pipeline,
    BattleStatistics statistics, BattleStatusScope statuses, Func<float> nextFloat)
{
    private const int Width = BattlefieldLayout.Width, Height = BattlefieldLayout.Height;
    private IEnumerable<BattleUnitState> Allies(int team) => world.Units.Where(unit => unit.Team == team && unit.Alive);
    private static CombatCell ToCombatCell(Vector2I cell) => new(cell.X, cell.Y);
    private static CombatPoint ToCombatPoint(Vector2 position) => new(position.X, position.Y);
    private static void SetActionTarget(BattleUnitState owner, BattleUnitState target)
    { owner.ActionTargetRuntimeId = target.RuntimeId; owner.ActionTargetName = target.Definition.DisplayName; }
    public void Attack(BattleUnitState attacker, BattleUnitState target)
    {
        attacker.AttackCooldown = attacker.Definition.AttackHitGrowth is null ? attacker.EffectiveAttackTicks : 0;
        if (attacker.AttackDelivery == Content.AttackDelivery.Projectile && attacker.Definition.ProjectileWindupSeconds > 0)
        {
            attacker.Mode = BattleUnitMode.Attacking;
            attacker.LastActionKind = BattleActionKind.Attack;
            attacker.WaitingTicks = 0;
            BeginProjectileWindup(attacker, target);
            return;
        }
        ResolveAttack(attacker, target);
    }

    public void ResolveAttack(BattleUnitState attacker, BattleUnitState target, bool timed = false)
    {
        using var resolution = pipeline.BeginAuthoritativeResolution();
        statistics.Attack(attacker.RuntimeId);
        SelectAttackChainTarget(attacker, target);
        attacker.Mode = BattleUnitMode.Attacking;
        attacker.LastActionKind = BattleActionKind.Attack;
        attacker.WaitingTicks = 0;
        SetActionTarget(attacker, target);
        var source = world.Source(attacker.RuntimeId, attacker);
        world.Publish(new BattleCombatEventDraft(
            BattleCombatEventKind.AttackDeclared,
            source,
            attacker.RuntimeId,
            target.RuntimeId,
            world.Tick,
            Cell: ToCombatCell(target.Cell),
            Position: ToCombatPoint(target.Position)));
        var rawDamage = world.DamageValue(attacker);
        if (attacker.Definition.Behavior.LowHealthDamageBonus > 0 && attacker.Health / attacker.MaxHealth <= .4f)
            rawDamage *= 1f + attacker.Definition.Behavior.LowHealthDamageBonus;
        BattleHeroMana.OnAttack(attacker, world.Tick);
        world.Present("attack", attacker.RuntimeId, target.RuntimeId, rawDamage, target.Position, timed ? "" : "attack");
        if (attacker.AttackDelivery == TowerAutobattler.Content.AttackDelivery.Projectile)
            world.LaunchProjectile(attacker, target, rawDamage);
        else
        {
            if (attacker.AttackDelivery == TowerAutobattler.Content.AttackDelivery.Beam)
                world.Present("beam", attacker.RuntimeId, target.RuntimeId, rawDamage, target.Position, "",
                    origin: attacker.Position, sourceVfx: attacker.ProjectileVisualId);
            ResolveAttackHit(attacker, target, rawDamage, instantPiercing: true);
        }
        resolution.Commit();
    }

    // Shared impact rules execute only after an instant attack or an authoritative projectile collision.
    public void ResolveAttackHit(BattleUnitState attacker, BattleUnitState target, float rawDamage, bool instantPiercing, string actionId = "")
    {
        using var resolution = pipeline.BeginAuthoritativeResolution();
        var source = world.Source(attacker.RuntimeId, attacker);
        if (!target.Alive) { resolution.Commit(); return; }
        var dodge = target.Attributes.GetValue(CombatAttribute.DodgeChance);
        if (dodge > 0 && target.DisabledTicks <= 0 && !statuses.HasTag(target.RuntimeId,StatusDefinitionCompiler.ActionDisabledTag) && nextFloat() < dodge)
        {
            world.Publish(new BattleCombatEventDraft(BattleCombatEventKind.AttackDodged,source,attacker.RuntimeId,target.RuntimeId,world.Tick));
            world.Present("dodge",attacker.RuntimeId,target.RuntimeId,0,target.Position,"hit");
            resolution.Commit(); return;
        }
        var chance = attacker.Attributes.GetValue(CombatAttribute.CriticalChance);
        var critical = chance > 0 && nextFloat() < chance;
        if (critical) rawDamage *= attacker.Attributes.GetValue(CombatAttribute.CriticalDamage);
        if (string.IsNullOrEmpty(actionId)) actionId = $"attack:{attacker.RuntimeId}:{statistics.AttackActions(attacker.RuntimeId)}";
        var damage = world.Damage(attacker.RuntimeId, attacker, target, rawDamage,
            damageClass: CombatDamageClass.BasicAttack, actionId: actionId);
        if (critical)
            world.Publish(new BattleCombatEventDraft(BattleCombatEventKind.CriticalHit,source,attacker.RuntimeId,target.RuntimeId,
                world.Tick,RequestedValue:rawDamage,EffectiveValue:damage));
        RegisterGrowingHit(attacker, target);
        if (attacker.Alive && attacker.LifeSteal > 0)
            world.Heal(attacker.RuntimeId, attacker, damage * attacker.LifeSteal);
        world.WriteCounter(attacker,"_basic_hits",world.ReadCounter(attacker,"_basic_hits")+1);
        world.Publish(new BattleCombatEventDraft(
            BattleCombatEventKind.AttackLanded,
            source,
            attacker.RuntimeId,
            target.RuntimeId,
            world.Tick,
            RequestedValue: rawDamage,
            AppliedValue: damage,
            EffectiveValue: damage,
            Cell: ToCombatCell(target.Cell),
            Position: ToCombatPoint(target.Position),
            CurrentStacks: (int)Math.Min(int.MaxValue,world.ReadCounter(attacker,"_basic_hits")),
            DamageClass:CombatDamageClass.BasicAttack, ActionId:actionId));
        if (attacker.Definition.SplashRadius > 0)
            world.Present("vfx", attacker.RuntimeId, target.RuntimeId, 0, target.Position, "",
                vfx: new BattleVfxCue(BattleVfxPhase.Burst, "burst", attacker.Definition.SplashRadius));
        if (attacker.Definition.SplashRadius > 0)
            foreach (var splash in Allies(target.Team).Where(other =>
                         other != target &&
                         Math.Max(0f, other.Position.DistanceTo(target.Position) - other.BodyRadius) <=
                         attacker.Definition.SplashRadius + .0001f).ToArray())
                world.Damage(attacker.RuntimeId, attacker, splash, rawDamage * .45f);
        if (instantPiercing && attacker.Definition.Behavior.PiercingLine)
        {
            var attackDelta = target.Position - attacker.Position;
            var direction = attackDelta.LengthSquared() <= .000001f ? Vector2.Right : attackDelta.Normalized();
            var targetProjection = attackDelta.Length();
            var rayEnd = attacker.Position + direction * MathF.Sqrt(Width * Width + Height * Height);
            var behind = Allies(target.Team)
                .Where(other => other.RuntimeId != target.RuntimeId)
                .Select(other => new
                {
                    Unit = other,
                    Projection = (other.Position - attacker.Position).Dot(direction)
                })
                .Where(candidate => candidate.Projection > targetProjection + .0001f &&
                                    BattlefieldSpace.PointSegmentDistance(
                                        candidate.Unit.Position, target.Position, rayEnd) <=
                                    candidate.Unit.BodyRadius + .0001f)
                .GroupBy(candidate => candidate.Unit.RuntimeId, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(candidate => candidate.Projection)
                .ThenBy(candidate => candidate.Unit.RuntimeId, StringComparer.Ordinal)
                .Select(candidate => candidate.Unit)
                .FirstOrDefault();
            if (behind is not null) world.Damage(attacker.RuntimeId, attacker, behind, rawDamage * .35f);
        }
        if (attacker.Definition.Behavior.SlowOnHitTicks > 0 && target.Alive)
        {
            target.AttackCooldown += attacker.Definition.Behavior.SlowOnHitTicks / 2;
            target.MoveCooldown += attacker.Definition.Behavior.SlowOnHitTicks;
        }
        resolution.Commit();
    }

    public void SelectAttackChainTarget(BattleUnitState owner, BattleUnitState target)
    {
        if (owner.Definition.AttackHitGrowth is not { } growth) return;
        if (growth.ResetOnTargetChange && owner.AttackChainTargetId != target.RuntimeId)
        {
            owner.AttackHitStacks = 0;
            ProjectAttackGrowth(owner);
        }
        owner.AttackChainTargetId = target.RuntimeId;
    }

    public void RegisterGrowingHit(BattleUnitState owner, BattleUnitState target)
    {
        if (!owner.Alive || owner.Definition.AttackHitGrowth is not { } growth) return;
        // Old arrows already in flight may still land after a switch. They cannot restart
        // or contaminate the base form's new target chain; the retention upgrade keeps them.
        if (growth.ResetOnTargetChange && owner.AttackChainTargetId != target.RuntimeId) return;
        owner.AttackHitStacks = checked(owner.AttackHitStacks + 1);
        ProjectAttackGrowth(owner);
        world.Present("growth", owner.RuntimeId, target.RuntimeId, owner.AttackHitStacks, owner.Position, "");
    }

    private static void ProjectAttackGrowth(BattleUnitState owner)
    {
        var growth = owner.Definition.AttackHitGrowth!;
        owner.Attributes.ApplyModifier(new CompiledAttributeModifier(CombatAttribute.AttackSpeed,
            AttributeModifierOperation.Add,
            new CompiledConstantMagnitude(owner.AttackHitStacks * growth.AttackSpeedPerHit), 0, "attack-chain"),
            CombatSourceRef.Unit(owner.Definition.ContentId, owner.RuntimeId, owner.SourceInstanceId));
    }

    public void AdvanceGrowingAttacks(BattleUnitState owner, BattleUnitState target)
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

    public void AdvanceProjectileSequence(BattleUnitState owner)
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
        world.ReleaseGoal(owner.RuntimeId);
    }

    public void FireSequenceShot(BattleUnitState owner)
    {
        var sequence = owner.ProjectileSequence!;
        // A multi-target volley selects its group once. Later arrows skip unavailable
        // members, without silently increasing the number of targets promised by the skill.
        var candidates = world.Units.Where(unit => unit.Alive && unit.Team != owner.Team &&
                (sequence.TargetGroup.IsDefault || sequence.TargetGroup.Contains(unit.RuntimeId)) &&
                BattlefieldSpace.IsWithinReach(owner, unit, owner.AttackRange) && world.HasLineAccess(owner, unit))
            .OrderBy(unit => BattlefieldSpace.EdgeDistance(owner, unit))
            .ThenBy(unit => unit.RuntimeId, StringComparer.Ordinal).Take(sequence.Operation.MaxTargets).ToArray();
        if (sequence.Operation.MaxTargets > 1 && sequence.TargetGroup.IsDefault)
        {
            sequence = sequence with { TargetGroup = candidates.Select(unit => unit.RuntimeId).ToImmutableArray() };
            owner.ProjectileSequence = sequence;
        }
        var target = sequence.Operation.MaxTargets > 1 && candidates.Length > 0
            ? candidates[(sequence.Operation.ShotCount - sequence.RemainingShots) % candidates.Length]
            : world.Units.FirstOrDefault(unit => unit.RuntimeId == sequence.TargetId && unit.Alive && unit.Team != owner.Team &&
                BattlefieldSpace.IsWithinReach(owner, unit, owner.AttackRange) && world.HasLineAccess(owner, unit));
        if (sequence.Operation.MaxTargets == 1) target ??= world.Units.Where(unit => unit.Alive && unit.Team != owner.Team &&
                BattlefieldSpace.IsWithinReach(owner, unit, owner.AttackRange) && world.HasLineAccess(owner, unit))
            .OrderBy(unit => BattlefieldSpace.EdgeDistance(owner, unit)).ThenBy(unit => unit.RuntimeId, StringComparer.Ordinal)
            .FirstOrDefault();
        if (target is null) { owner.ProjectileSequence = null; return; }
        SelectAttackChainTarget(owner, target);
        SetActionTarget(owner, target);
        var priorShots = target.RuntimeId == sequence.TargetId ? sequence.ConsecutiveTargetShots : 0;
        var multiplier = sequence.Operation.AttackDamageMultiplier + priorShots * sequence.Operation.PerShotDamageIncrease;
        var damage = world.DamageValue(owner) * multiplier;
        if (owner.Definition.ProjectileWindupSeconds > 0)
            BeginProjectileWindup(owner, target, sequence.Origin, multiplier, sequence.Operation.AttackIntervalRatio);
        else
        {
            world.LaunchProjectile(owner, target, damage, sequence.Origin);
            world.Present("skill_arrow", owner.RuntimeId, target.RuntimeId, damage, target.Position, "attack");
        }
        owner.ProjectileSequence = sequence.RemainingShots == 1 ? null : sequence with
        {
            RemainingShots = sequence.RemainingShots - 1, TargetId = target.RuntimeId,
            ConsecutiveTargetShots = priorShots + 1
        };
        owner.ManaLockedUntilTick = Math.Max(owner.ManaLockedUntilTick, world.Tick + 1);
    }
    public void BeginProjectileWindup(BattleUnitState owner, BattleUnitState target,
        CombatSourceRef skillOrigin = default, float multiplier = 1, float intervalRatio = 1)
    {
        var definition = owner.Definition;
        var baseWindup = Math.Min(definition.ProjectileWindupSeconds,
            definition.AttackTicks * BattleTiming.TickSeconds * definition.AttackReleaseProgress);
        var seconds = baseWindup * intervalRatio / owner.Attributes.GetValue(CombatAttribute.AttackSpeed);
        // The action and its release use the same quantized simulation duration. At extreme
        // speed several independent windups may mature together; there is no one-shot cap.
        var ticks = Math.Max(1, (int)Math.Ceiling(seconds / BattleTiming.TickSeconds - .00001));
        owner.ProjectileWindups = owner.ProjectileWindups.Add(new(target.RuntimeId, world.Tick + ticks, multiplier, skillOrigin));
        SelectAttackChainTarget(owner, target);
        SetActionTarget(owner, target);
        world.Present("attack_windup", owner.RuntimeId, target.RuntimeId, 0, target.Position, "",
            attackTiming: new(ticks * BattleTiming.TickSeconds / definition.AttackReleaseProgress, definition.AttackReleaseProgress));
    }

    public bool AdvanceProjectileWindups(BattleUnitState owner)
    {
        if (owner.ProjectileWindups.IsEmpty) return false;
        var ready = owner.ProjectileWindups.Where(shot => shot.ReleaseTick <= world.Tick).ToArray();
        owner.ProjectileWindups = owner.ProjectileWindups.Where(shot => shot.ReleaseTick > world.Tick).ToImmutableArray();
        foreach (var shot in ready)
        {
            var target = world.Units.FirstOrDefault(unit => unit.RuntimeId == shot.TargetId && unit.Alive && unit.Team != owner.Team);
            if (!owner.Alive || target is null || !BattlefieldSpace.IsWithinReach(owner, target, owner.AttackRange) || !world.HasLineAccess(owner, target))
            {
                world.Present("attack_cancel", owner.RuntimeId, shot.TargetId, 0, owner.Position, "");
                continue;
            }
            if (shot.SkillOrigin.IsSpecified)
            {
                var damage = world.DamageValue(owner) * shot.DamageMultiplier;
                world.LaunchProjectile(owner, target, damage, shot.SkillOrigin);
                world.Present("skill_arrow", owner.RuntimeId, target.RuntimeId, damage, target.Position, "");
                owner.ManaLockedUntilTick = Math.Max(owner.ManaLockedUntilTick, world.Tick + 1);
            }
            else ResolveAttack(owner, target, timed: true);
        }
        return ready.Length > 0;
    }

    public void CancelProjectileWindups(BattleUnitState owner)
    {
        world.CancelChargedLine(owner);
        if (owner.ProjectileWindups.IsEmpty) return;
        owner.ProjectileWindups = [];
        world.Present("attack_cancel", owner.RuntimeId, "", 0, owner.Position, "");
    }
}
