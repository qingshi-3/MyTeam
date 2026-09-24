using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private sealed record GritTriggerLatch(bool BelowHalf = false, bool CriticalUsed = false);

    private static CompiledGritStorageOperation GritStorageFor(BattleUnitState owner, string key) =>
        owner.Definition.AbilityLoadout!.Abilities.SelectMany(a => a.Operations)
            .OfType<CompiledGritStorageOperation>().Single(s => s.CounterKey == key);

    private void RefreshGritActionRequests(BattleUnitState owner)
    {
        if (!owner.Alive || owner.Definition.AbilityLoadout is not { } loadout) return;
        foreach (var ability in loadout.Abilities.Where(a => a.Trigger == AbilityTriggerKind.ActionQueued))
        foreach (var punch in ability.Operations.OfType<CompiledGritPunchOperation>())
        {
            var key = CounterAddress(owner, punch.CounterKey, false);
            var latch = _techniques.GritTriggers.GetValueOrDefault(key, new GritTriggerLatch());
            var belowHalf = owner.Health < owner.MaxHealth * punch.LowHealthRatio;
            var reasons = UnitActionReason.None;
            if (belowHalf && !latch.BelowHalf) reasons |= UnitActionReason.LowHealth;
            if (!latch.CriticalUsed && owner.Health < owner.MaxHealth * punch.CriticalHealthRatio)
                reasons |= UnitActionReason.CriticalHealth;
            if (CurrentGrit(owner, punch.CounterKey) >= owner.MaxHealth * GritStorageFor(owner, punch.CounterKey).MaximumHealthRatio)
                reasons |= UnitActionReason.FullResource;
            else
                RemoveQueuedUnitActionReason(owner, ability.StableId, UnitActionReason.FullResource);
            if (latch.BelowHalf != belowHalf || !_techniques.GritTriggers.ContainsKey(key))
                _techniques = _techniques with
                {
                    GritTriggers = _techniques.GritTriggers.SetItem(key, latch with { BelowHalf = belowHalf })
                };
            QueueUnitAction(owner, ability.StableId, reasons);
        }
    }

    private void ConsumeGritActionRequest(BattleUnitState owner, CompiledAbilityDefinition ability, CompiledGritPunchOperation punch)
    {
        var reasons = TakeQueuedUnitAction(owner, ability.StableId);
        if (!reasons.HasFlag(UnitActionReason.CriticalHealth)) return;
        var key = CounterAddress(owner, punch.CounterKey, false);
        var latch = _techniques.GritTriggers.GetValueOrDefault(key, new GritTriggerLatch());
        // Death clears casts and pending requests, but not this spent entitlement.
        // Revival retains identity; a new battle creates a fresh TechniqueState.
        _techniques = _techniques with { GritTriggers = _techniques.GritTriggers.SetItem(key, latch with { CriticalUsed = true }) };
    }
}
