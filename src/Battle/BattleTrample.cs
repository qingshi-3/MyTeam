using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

public sealed record TrampleState(CompiledTrampleOperation Operation, CombatSourceRef Origin,
    Vector2 Start, Vector2 End, Vector2 Position, int StartTick, int Team,
    ImmutableHashSet<string> HitIds, int RecoveryUntil = 0);
public sealed record BattleTrampleCue(string WarningVfx, string RushVfx, int StartTick,
    int ChargeTicks, float Radius, Vector2 Start, Vector2 End);

public sealed partial class BattleSimulation
{
    private readonly BattleTrampleScope _tramples;
    private bool CanAimTrample(BattleUnitState owner, BattleUnitState target, CompiledTrampleOperation op) => _tramples.CanAimTrample(owner, target, op);
    private void BeginTrample(BattleUnitState owner, BattleUnitState target, CompiledTrampleOperation op, CompiledAbilityDefinition ability) => _tramples.BeginTrample(owner, target, op, ability);
    private void AdvanceTramples() => _tramples.AdvanceTramples();
    private void CancelTrample(BattleUnitState owner) => _tramples.CancelTrample(owner);
}
