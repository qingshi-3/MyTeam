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
    string VisualId = "projectile", bool ContinueAfterHitLimit = false);

public sealed partial class BattleSimulation
{
    private readonly Dictionary<int, BattleProjectileState> _projectiles = [];
    private int _projectileSequence;
    public ImmutableArray<BattleProjectileState> Projectiles =>
        _projectiles.Values.OrderBy(projectile => projectile.Id).ToImmutableArray();

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
        var projectile = new BattleProjectileState(++_projectileSequence, source.RuntimeId, source.Team,
            source.Position, direction * definition.ProjectileSpeed, definition.ProjectileRadius,
            damage, definition.ProjectileLifetime, TickIndex,
            !skillOrigin.IsSpecified && definition.Behavior.PiercingLine ? 2 : 1, [], skillOrigin, VisualId: source.ProjectileVisualId);
        _projectiles.Add(projectile.Id, projectile);
        Emit("projectile_spawn", source.RuntimeId, target.RuntimeId, damage, projectile.Position, "",
            projectile.Id, projectile.Position + direction, sourceVfx: source.ProjectileVisualId);
    }

    private void AdvanceProjectiles(IReadOnlyDictionary<string, Vector2> previousPositions)
    {
        foreach (var initial in Projectiles)
        {
            // A launch is visible for at least one fixed step and never deals launch-time damage.
            if (initial.LaunchTick == TickIndex || !_projectiles.ContainsKey(initial.Id)) continue;
            var projectile = initial;
            var seconds = Math.Min(BattleTiming.TickSeconds, projectile.RemainingSeconds);
            var delta = projectile.Velocity * seconds;
            var terrainFraction = BattlefieldSpace.FirstTerrainHitFraction(projectile.Position, delta,
                projectile.Radius, Width, Height, cell => _config.FloorRule.CanOccupy(cell));
            var candidates = new List<(BattleUnitState Unit, float Time)>();
            foreach (var target in _units.Where(unit => (projectile.HitIds.Length < projectile.MaximumHits || IsRampartBody(unit)) &&
                         unit.Alive && (unit.Team != projectile.Team || IsRampartBody(unit)) &&
                         !projectile.HitIds.Contains(unit.RuntimeId)))
            {
                var start = previousPositions.TryGetValue(target.RuntimeId, out var previous) ? previous : target.Position;
                var movement = (target.Position - start) * (seconds / BattleTiming.TickSeconds);
                var radius = projectile.Radius + target.BodyRadius;
                // Already overlapping counts as impact, including a target crossing into a stationary origin.
                if (projectile.Position.DistanceSquaredTo(start) <= radius * radius)
                    candidates.Add((target, 0));
                else if (BattlefieldSpace.TryMovingCircleTimeOfImpact(projectile.Position, delta,
                             projectile.Radius, start, movement, target.BodyRadius, out var time))
                    candidates.Add((target, time));
            }
            var consumed = false;
            foreach (var collision in candidates.OrderBy(hit => hit.Time)
                         .ThenBy(hit => hit.Unit.RuntimeId, StringComparer.Ordinal))
            {
                if (!collision.Unit.Alive || collision.Time > terrainFraction ||
                    (terrainFraction < 1f && collision.Time >= terrainFraction)) continue;
                var source = _units.First(unit => unit.RuntimeId == projectile.SourceId);
                var contact = projectile.Position + delta * collision.Time;
                var blockedByWall = IsRampartBody(collision.Unit);
                using var resolution = _combatPipeline.BeginAuthoritativeResolution();
                // A wall consumes even an inert piercing flight, without restoring its damage budget.
                if (projectile.HitIds.Length >= projectile.MaximumHits ||
                    (blockedByWall && collision.Unit.Team == projectile.Team)) { }
                else if (projectile.SkillOrigin.IsSpecified)
                {
                    ResolveLineSkillHit(source, collision.Unit,
                        projectile.Damage * Mathf.Pow(projectile.SubsequentHitMultiplier, projectile.HitIds.Length),
                        projectile.SkillOrigin, projectile.DamageType);
                }
                else if (projectile.HitIds.IsEmpty)
                    ResolveAttackHit(source, collision.Unit, projectile.Damage, instantPiercing: false);
                else
                    ApplyDamage(source.RuntimeId, source, collision.Unit, projectile.Damage * .35f);
                projectile = projectile with { HitIds = projectile.HitIds.Add(collision.Unit.RuntimeId) };
                Emit("projectile_impact", source.RuntimeId, collision.Unit.RuntimeId, 0, contact, "", projectile.Id);
                resolution.Commit();
                if (blockedByWall)
                {
                    _projectiles.Remove(projectile.Id);
                    Emit("projectile_end",source.RuntimeId,"",0,contact,"",projectile.Id);
                    consumed = true;
                    break;
                }
                if (projectile.HitIds.Length >= projectile.MaximumHits)
                {
                    // Damage budget and travel distance are independent for charged
                    // piercing shots. Once exhausted, the remaining flight is inert.
                    if (!projectile.ContinueAfterHitLimit)
                    {
                        _projectiles.Remove(projectile.Id);
                        Emit("projectile_end", source.RuntimeId, "", 0, contact, "", projectile.Id);
                        consumed = true;
                    }
                    break;
                }
            }
            if (consumed) continue;
            var end = projectile.Position + delta * terrainFraction;
            var remaining = projectile.RemainingSeconds - seconds;
            if (terrainFraction < 1f || remaining <= .00001f)
            {
                _projectiles.Remove(projectile.Id);
                Emit("projectile_end", projectile.SourceId, "", 0, end, "", projectile.Id);
            }
            else
            {
                _projectiles[projectile.Id] = projectile with { Position = end, RemainingSeconds = remaining };
                Emit("projectile_move", projectile.SourceId, "", 0, end, "", projectile.Id);
            }
        }
    }
}
