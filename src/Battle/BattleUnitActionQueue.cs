using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    [Flags]
    private enum UnitActionReason { None = 0, FullResource = 1, LowHealth = 2, CriticalHealth = 4 }
    private sealed record PendingUnitAction(string AbilityId, int Team, UnitActionReason Reasons);
    private sealed record UnitActionQueue(ImmutableArray<PendingUnitAction> Pending, int RecoveryUntilTick)
    {
        public static UnitActionQueue Empty => new([], 0);
    }

    // Requests belong to one concrete unit, never to a shared skill resource. Keep only
    // one pending entry per skill; a second condition adds a reason, not a duplicate cast.
    private ImmutableDictionary<string, UnitActionQueue> _unitActionQueues =
        ImmutableDictionary<string, UnitActionQueue>.Empty;

    private UnitActionQueue ActionsFor(BattleUnitState unit) =>
        _unitActionQueues.GetValueOrDefault(unit.RuntimeId, UnitActionQueue.Empty);

    private bool UnitActionRecovering(BattleUnitState unit) => TickIndex < ActionsFor(unit).RecoveryUntilTick;

    private void QueueUnitAction(BattleUnitState owner, string abilityId, UnitActionReason reasons)
    {
        if (reasons == UnitActionReason.None || !owner.Alive) return;
        var queue = ActionsFor(owner);
        var pending = queue.Pending.Where(p => p.Team == owner.Team).ToImmutableArray();
        var index = -1;
        for (var i = 0; i < pending.Length; i++)
            if (pending[i].AbilityId == abilityId) { index = i; break; }
        pending = index < 0 ? pending.Add(new(abilityId, owner.Team, reasons)) :
            pending.SetItem(index, pending[index] with { Reasons = pending[index].Reasons | reasons });
        _unitActionQueues = _unitActionQueues.SetItem(owner.RuntimeId, queue with { Pending = pending });
    }

    private bool HasQueuedUnitAction(BattleUnitState owner, string abilityId) =>
        ActionsFor(owner).Pending.Any(p => p.AbilityId == abilityId && p.Team == owner.Team);

    private UnitActionReason TakeQueuedUnitAction(BattleUnitState owner, string abilityId)
    {
        var queue = ActionsFor(owner);
        var request = queue.Pending.First(p => p.AbilityId == abilityId && p.Team == owner.Team);
        _unitActionQueues = _unitActionQueues.SetItem(owner.RuntimeId,
            queue with { Pending = queue.Pending.Remove(request) });
        return request.Reasons;
    }

    private void RemoveQueuedUnitActionReason(BattleUnitState owner, string abilityId, UnitActionReason reason)
    {
        var queue = ActionsFor(owner);
        if (!queue.Pending.Any(p => p.AbilityId == abilityId && p.Reasons.HasFlag(reason))) return;
        var pending = queue.Pending.Select(p => p.AbilityId == abilityId ? p with { Reasons = p.Reasons & ~reason } : p)
            .Where(p => p.Reasons != UnitActionReason.None).ToImmutableArray();
        _unitActionQueues = _unitActionQueues.SetItem(owner.RuntimeId, queue with { Pending = pending });
    }

    private void RecoverUnitAction(BattleUnitState owner, int recoveryTicks)
    {
        var queue = ActionsFor(owner);
        _unitActionQueues = _unitActionQueues.SetItem(owner.RuntimeId,
            queue with { RecoveryUntilTick = Math.Max(queue.RecoveryUntilTick, TickIndex + recoveryTicks) });
    }

    private AbilityActivationResult? TryStartQueuedUnitAction(BattleUnitState owner)
    {
        if (_abilityScope is null || UnitActionRecovering(owner)) return null;
        var queue = ActionsFor(owner);
        var pending = queue.Pending.Where(p => p.Team == owner.Team &&
            _abilityScope.Find(owner.RuntimeId, p.AbilityId)?.Trigger == AbilityTriggerKind.ActionQueued).ToImmutableArray();
        if (pending.Length != queue.Pending.Length)
            _unitActionQueues = _unitActionQueues.SetItem(owner.RuntimeId, queue with { Pending = pending });
        if (pending.IsEmpty) return null;
        // Resolve target and spend resource only when this unit can start. A failed
        // commit keeps the request and once-per-battle entitlement in its checkpoint.
        return _abilityScope.TryActivateQueuedAutomatic(owner.RuntimeId, pending[0].AbilityId, TickIndex);
    }
}
