using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Domain;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Battle;

// Battle-owned immutable state. Scene nodes are projections; physics callbacks never apply damage.
public sealed record BattleProjectileState(
    int Id, string SourceId, int Team, Vector2 Position, Vector2 Velocity,
    float Radius, float Damage, float RemainingSeconds, int LaunchTick,
    int MaximumHits, ImmutableArray<string> HitIds, CombatSourceRef SkillOrigin = default,
    EffectDamageType DamageType = EffectDamageType.Normal, float SubsequentHitMultiplier = 1,
    string VisualId = "projectile", bool ContinueAfterHitLimit = false, string ActionId = "");

public sealed partial class BattleSimulation
{
    private readonly BattleProjectileScope _projectileScope;
    public ImmutableArray<BattleProjectileState> Projectiles => _projectileScope.ReadSnapshot();

    private void LaunchProjectile(BattleUnitState source, BattleUnitState target, float damage,
        CombatSourceRef skillOrigin = default)
    {
        var definition = source.Definition;
        if (!float.IsFinite(definition.ProjectileSpeed) || definition.ProjectileSpeed is <= 0 or > 100 ||
            !float.IsFinite(definition.ProjectileRadius) || definition.ProjectileRadius is <= 0 or > .5f ||
            !float.IsFinite(definition.ProjectileLifetime) || definition.ProjectileLifetime is <= 0 or > 10)
            throw new InvalidOperationException("Invalid projectile parameters.");
        var direction = (target.Position - source.Position).Normalized();
        if (direction == Vector2.Zero) direction = source.Team == 0 ? Vector2.Right : Vector2.Left;
        var projectile = _projectileScope.Spawn(new BattleProjectileState(0, source.RuntimeId, source.Team,
            source.Position, direction * definition.ProjectileSpeed, definition.ProjectileRadius,
            damage, definition.ProjectileLifetime, TickIndex,
            !skillOrigin.IsSpecified && definition.Behavior.PiercingLine ? 2 : 1, [], skillOrigin,
            VisualId: source.ProjectileVisualId, ActionId: skillOrigin.IsSpecified ? skillOrigin.InstanceId : $"attack:{source.RuntimeId}:{_statistics.AttackActions(source.RuntimeId)}"));
        Emit("projectile_spawn", source.RuntimeId, target.RuntimeId, damage, projectile.Position, "",
            projectile.Id, projectile.Position + direction, sourceVfx: source.ProjectileVisualId);
    }

    private void AdvanceProjectiles(IReadOnlyDictionary<string, Vector2> previousPositions) =>
        _projectileScope.Advance(previousPositions);

    private sealed class ProjectileWorld(BattleSimulation owner) : IBattleProjectileWorld
    {
        public int Tick => owner.TickIndex;
        public bool CanOccupy(Vector2I cell) => owner._config.FloorRule.CanOccupy(cell);
        public bool IsAlive(string id) => owner._units.Any(unit => unit.RuntimeId == id && unit.Alive);
        public IEnumerable<ProjectileTarget> ReadTargets()
        {
            foreach (var unit in owner._units)
                yield return new(unit.RuntimeId, unit.Team, unit.Position, unit.BodyRadius, unit.Alive, owner.IsRampartBody(unit));
        }

        public void ResolveImpact(BattleProjectileState projectile, string targetId, Vector2 contact, bool blockedByWall)
        {
            var source = owner._units.First(unit => unit.RuntimeId == projectile.SourceId);
            var target = owner._units.First(unit => unit.RuntimeId == targetId);
            using var resolution = owner._combatPipeline.BeginAuthoritativeResolution();
            if (projectile.HitIds.Length >= projectile.MaximumHits || (blockedByWall && target.Team == projectile.Team)) { }
            else if (projectile.SkillOrigin.IsSpecified)
                owner.ResolveLineSkillHit(source, target,
                    projectile.Damage * Mathf.Pow(projectile.SubsequentHitMultiplier, projectile.HitIds.Length),
                    projectile.SkillOrigin, projectile.DamageType);
            else if (projectile.HitIds.IsEmpty)
                owner.ResolveAttackHit(source, target, projectile.Damage, instantPiercing: false, actionId: projectile.ActionId);
            else owner.ApplyDamage(source.RuntimeId, source, target, projectile.Damage * .35f);
            owner.Emit("projectile_impact", source.RuntimeId, target.RuntimeId, 0, contact, "", projectile.Id);
            resolution.Commit();
        }

        public void Publish(ProjectileFlightFact fact) => owner.Emit(
            fact.Kind == ProjectileFlightKind.Move ? "projectile_move" : "projectile_end",
            fact.Projectile.SourceId, "", 0, fact.Position, "", fact.Projectile.Id);
    }
}
