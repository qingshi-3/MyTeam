using System;
using System.Collections.Immutable;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Battle;

public sealed record BattlePermanentGain(
    string SourceInstanceId,
    string TargetInstanceId,
    string AbilityId,
    string Slot,
    CombatAttribute Attribute,
    float Amount);

public sealed partial class BattleSimulation
{
    private ImmutableArray<BattlePermanentGain> _permanentGains = [];
    private ImmutableHashSet<string> _permanentGainKeys = ImmutableHashSet.Create<string>(StringComparer.Ordinal);

    private void ApplyPermanentAttribute(BattleUnitState owner, BattleUnitState target,
        CompiledMatrixOperation operation, CombatSourceRef origin, string slot)
    {
        if (target != owner || owner.Team != 0 || owner.IsTemporary || !owner.IsPersistentRosterHero ||
            operation.Amount <= 0 || operation.Attribute is not (CombatAttribute.AttackDamage or CombatAttribute.MaxHealth))
            return;
        var authoredSlot = slot.StartsWith(owner.RuntimeId + ":", StringComparison.Ordinal)
            ? slot[(owner.RuntimeId.Length + 1)..]
            : slot;
        var key = $"{owner.RuntimeId}:{origin.StableId}:{authoredSlot}";
        if (_permanentGainKeys.Contains(key)) return;

        var amount = operation.Amount;
        var previousMaximum = owner.MaxHealth;
        owner.Attributes.SetBaseValue(operation.Attribute, owner.Attributes.GetBaseValue(operation.Attribute) + amount);
        if (operation.Attribute == CombatAttribute.MaxHealth)
            owner.Health = Math.Min(owner.MaxHealth, owner.Health + Math.Max(0, owner.MaxHealth - previousMaximum));
        _permanentGainKeys = _permanentGainKeys.Add(key);
        _permanentGains = _permanentGains.Add(new(owner.SourceInstanceId, target.SourceInstanceId,
            origin.StableId, authoredSlot, operation.Attribute, amount));
    }
}
