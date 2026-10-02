using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Domain;

namespace TowerAutobattler.Battle;

internal readonly record struct ProjectileTarget(string RuntimeId, int Team, Vector2 Position,
    float BodyRadius, bool Alive, bool IsWall);
internal enum ProjectileFlightKind { Move, End }
internal readonly record struct ProjectileFlightFact(ProjectileFlightKind Kind, BattleProjectileState Projectile, Vector2 Position);

// Read-only body queries and explicit commands keep flight independent of mutable combat units.
internal interface IBattleProjectileWorld
{
    int Tick { get; }
    IEnumerable<ProjectileTarget> ReadTargets();
    bool IsAlive(string runtimeId);
    bool CanOccupy(Vector2I cell);
    void ResolveImpact(BattleProjectileState projectile, string targetId, Vector2 contact, bool blockedByWall);
    void Publish(ProjectileFlightFact fact);
}

// Owns flight state, identities, swept travel, checkpoints and cleanup. Damage stays in the battle authority.
internal sealed class BattleProjectileScope(IBattleProjectileWorld world)
{
    private readonly IBattleProjectileWorld _world = world;
    private readonly Dictionary<int, BattleProjectileState> _projectiles = [];
    private int _sequence;
    private ImmutableArray<BattleProjectileState> _snapshot;

    internal ImmutableArray<BattleProjectileState> ReadSnapshot()
    {
        if (_snapshot.IsDefault)
            _snapshot = _projectiles.Values.OrderBy(projectile => projectile.Id).ToImmutableArray();
        return _snapshot;
    }

    internal BattleProjectileState Spawn(BattleProjectileState template)
    {
        var projectile = template with { Id = ++_sequence };
        _projectiles.Add(projectile.Id, projectile);
        _snapshot = default;
        return projectile;
    }

    private void Write(BattleProjectileState projectile)
    {
        _projectiles[projectile.Id] = projectile;
        _snapshot = default;
    }

    private void Remove(int id)
    {
        _projectiles.Remove(id);
        _snapshot = default;
    }

    private void Publish(ProjectileFlightKind kind, BattleProjectileState projectile, Vector2 position) =>
        _world.Publish(new(kind, projectile, position));

    internal void CancelOwner(string runtimeId)
    {
        foreach (var projectile in ReadSnapshot())
        {
            if (projectile.SourceId != runtimeId) continue;
            Remove(projectile.Id);
            Publish(ProjectileFlightKind.End, projectile, projectile.Position);
        }
    }

    internal void Clear()
    {
        _projectiles.Clear();
        _snapshot = [];
    }

    internal sealed record Checkpoint(BattleProjectileScope Owner, int Sequence, ImmutableArray<BattleProjectileState> Projectiles);
    internal Checkpoint Capture() => new(this, _sequence, ReadSnapshot());
    internal void Restore(Checkpoint checkpoint)
    {
        if (!ReferenceEquals(checkpoint.Owner, this)) throw new InvalidOperationException("Projectile checkpoint belongs to another battle.");
        _projectiles.Clear();
        foreach (var projectile in checkpoint.Projectiles) _projectiles.Add(projectile.Id, projectile);
        _sequence = checkpoint.Sequence;
        _snapshot = checkpoint.Projectiles;
    }

    internal void Advance(IReadOnlyDictionary<string, Vector2> previousPositions)
    {
        foreach (var initial in ReadSnapshot())
        {
            // A launch is visible for at least one fixed step and never deals launch-time damage.
            if (initial.LaunchTick == _world.Tick || !_projectiles.ContainsKey(initial.Id)) continue;
            var projectile = initial;
            var seconds = Math.Min(BattleTiming.TickSeconds, projectile.RemainingSeconds);
            var delta = projectile.Velocity * seconds;
            var terrainFraction = BattlefieldSpace.FirstTerrainHitFraction(projectile.Position, delta,
                projectile.Radius, BattlefieldLayout.Width, BattlefieldLayout.Height, _world.CanOccupy);
            var candidates = new List<(ProjectileTarget Unit, float Time)>();
            foreach (var target in _world.ReadTargets().Where(unit => (projectile.HitIds.Length < projectile.MaximumHits || unit.IsWall) &&
                         unit.Alive && (unit.Team != projectile.Team || unit.IsWall) &&
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
                if (!_world.IsAlive(collision.Unit.RuntimeId) || collision.Time > terrainFraction ||
                    (terrainFraction < 1f && collision.Time >= terrainFraction)) continue;
                var contact = projectile.Position + delta * collision.Time;
                var blockedByWall = collision.Unit.IsWall;
                _world.ResolveImpact(projectile, collision.Unit.RuntimeId, contact, blockedByWall);
                // A committed reaction can change the source's allegiance and cancel
                // its flights. Do not recreate that flight from this local copy.
                if (!_projectiles.ContainsKey(projectile.Id))
                {
                    consumed = true;
                    break;
                }
                projectile = projectile with { HitIds = projectile.HitIds.Add(collision.Unit.RuntimeId) };
                if (blockedByWall)
                {
                    Remove(projectile.Id);
                    Publish(ProjectileFlightKind.End, projectile, contact);
                    consumed = true;
                    break;
                }
                if (projectile.HitIds.Length >= projectile.MaximumHits)
                {
                    // Damage budget and travel distance are independent for charged
                    // piercing shots. Once exhausted, the remaining flight is inert.
                    if (!projectile.ContinueAfterHitLimit)
                    {
                        Remove(projectile.Id);
                        Publish(ProjectileFlightKind.End, projectile, contact);
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
                Remove(projectile.Id);
                Publish(ProjectileFlightKind.End, projectile, end);
            }
            else
            {
                Write(projectile with { Position = end, RemainingSeconds = remaining });
                Publish(ProjectileFlightKind.Move, projectile, end);
            }
        }
    }
}
