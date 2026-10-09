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

internal sealed partial class BattlePermanentGrowthScope
{
    public ImmutableArray<BattlePermanentGain> Gains => _permanentGains;
    internal sealed class Checkpoint
    {
        private readonly BattlePermanentGrowthScope _owner;
        private readonly ImmutableArray<BattlePermanentGain> _gains;
        private readonly ImmutableHashSet<string> _keys;
        private readonly RememberedState _remembered;
        internal Checkpoint(BattlePermanentGrowthScope owner)
        { _owner = owner; _gains = owner._permanentGains; _keys = owner._permanentGainKeys; _remembered = owner._rememberedState; }
        internal void Restore(BattlePermanentGrowthScope owner)
        {
            if (!ReferenceEquals(owner, _owner)) throw new InvalidOperationException("Checkpoint belongs to another permanent growth scope.");
            owner._permanentGains = _gains; owner._permanentGainKeys = _keys;
            owner._rememberedState = _remembered;
        }
    }
    public Checkpoint Capture() => new(this);
    public void Restore(Checkpoint checkpoint) => checkpoint.Restore(this);
    private ImmutableArray<BattlePermanentGain> _permanentGains = [];
    private ImmutableHashSet<string> _permanentGainKeys = ImmutableHashSet.Create<string>(StringComparer.Ordinal);

    public void ApplyPermanentAttribute(BattleUnitState owner, BattleUnitState target,
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
        owner.Attributes.AddBaseValue(operation.Attribute, amount);
        if (operation.Attribute == CombatAttribute.MaxHealth)
            owner.Health = Math.Min(owner.MaxHealth, owner.Health + Math.Max(0, owner.MaxHealth - previousMaximum));
        _permanentGainKeys = _permanentGainKeys.Add(key);
        _permanentGains = _permanentGains.Add(new(owner.SourceInstanceId, target.SourceInstanceId,
            origin.StableId, authoredSlot, operation.Attribute, amount));
    }
}
