using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private readonly BattleTargetingPolicy _targeting;
    private sealed class TargetingWorld(BattleSimulation owner) : IBattleTargetingWorld
    {
        public IReadOnlyList<BattleUnitState> Units => owner._units;
        public BattleUnitState? DuelOpponent(BattleUnitState unit) => owner.DuelOpponent(unit);
        public bool CanAimHook(BattleUnitState source, BattleUnitState target, CompiledHookOperation op) => owner.CanAimHook(source, target, op);
        public bool CanAimTrample(BattleUnitState source, BattleUnitState target, CompiledTrampleOperation op) => owner.CanAimTrample(source, target, op);
        public bool CanAimChargedLine(BattleUnitState source, BattleUnitState target, CompiledChargedLineOperation op) => owner.CanAimChargedLine(source, target, op);
        public string EnemyActionTarget(BattleUnitState source, CompiledEnemyAction op) => owner.SelectEnemyActionTarget(source, op);
        public bool HasLineAccess(BattleUnitState source, BattleUnitState target) => owner.HasLineAccess(source, target);
        public bool IsRampartBody(BattleUnitState unit) => owner.IsRampartBody(unit);
        public BattleUnitState? SelectMovementTarget(BattleUnitState source, IReadOnlyList<BattleUnitState> candidates) => owner._movement!.SelectTarget(source, candidates);
        public void ClearMovementTarget(string id) => owner._movement!.ClearTarget(id);
    }
}
