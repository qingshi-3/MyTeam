using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;

namespace TowerAutobattler.Battle;

public sealed record BattleSpatialUnit(string RuntimeId, string DisplayName, int Team,
    Vector2 Position, float BodyRadius, float AttackReach, bool Grounded,
    string TargetId, bool TargetWithinReach, bool TargetLineClear);

public sealed record BattleSpatialSnapshot(int Tick, ImmutableArray<BattleSpatialUnit> Units,
    ImmutableArray<Vector2I> BlockedCells, ImmutableArray<DisplacementReservation> Reservations);

public sealed partial class BattleSimulation
{
    // Read the same geometry and line-access predicates as attacks/navigation. Diagnostics
    // must not advance the world, consume randomness or retain mutable unit objects.
    public BattleSpatialSnapshot ReadSpatialSnapshot() => new(TickIndex,
        _units.Where(unit => unit.Alive).OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal)
            .Select(unit =>
            {
                var target = _units.FirstOrDefault(candidate => candidate.Alive && candidate.RuntimeId == unit.ActionTargetRuntimeId);
                return new BattleSpatialUnit(unit.RuntimeId, unit.Definition.DisplayName, unit.Team,
                    unit.Position, unit.BodyRadius, unit.AttackRange, !IsAirborne(unit),
                    target?.RuntimeId ?? "", target is not null && BattlefieldSpace.IsWithinReach(unit, target, unit.AttackRange),
                    target is not null && HasLineAccess(unit, target));
            }).ToImmutableArray(),
        Enumerable.Range(0, Height).SelectMany(y => Enumerable.Range(0, Width).Select(x => new Vector2I(x, y)))
            .Where(cell => !_config.FloorRule.CanOccupy(cell)).ToImmutableArray(),
        CaptureDisplacementReservations().ToImmutableArray());
}
