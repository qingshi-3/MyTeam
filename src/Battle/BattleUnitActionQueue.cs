using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private readonly BattleUnitActionQueueScope _actionQueue;
    private bool UnitActionRecovering(BattleUnitState unit) => _actionQueue.UnitActionRecovering(unit);
    private void QueueUnitAction(BattleUnitState owner, string abilityId, UnitActionReason reasons) => _actionQueue.QueueUnitAction(owner, abilityId, reasons);
    private bool HasQueuedUnitAction(BattleUnitState owner, string abilityId) => _actionQueue.HasQueuedUnitAction(owner, abilityId);
    private UnitActionReason TakeQueuedUnitAction(BattleUnitState owner, string abilityId) => _actionQueue.TakeQueuedUnitAction(owner, abilityId);
    private void RemoveQueuedUnitActionReason(BattleUnitState owner, string abilityId, UnitActionReason reason) => _actionQueue.RemoveQueuedUnitActionReason(owner, abilityId, reason);
    private void RecoverUnitAction(BattleUnitState owner, int recoveryTicks) => _actionQueue.RecoverUnitAction(owner, recoveryTicks);
    private AbilityActivationResult? TryStartQueuedUnitAction(BattleUnitState owner) => _actionQueue.TryStartQueuedUnitAction(owner);
}
