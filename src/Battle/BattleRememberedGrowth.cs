using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Battle;

internal sealed partial class BattlePermanentGrowthScope
{
    private sealed record RememberMarker(string AbilityId, string Key);
    private sealed record OpeningBinding(ImmutableArray<BattleUnitState> Targets);
    private sealed record RememberedState(ImmutableDictionary<string, RememberMarker> Receivers,
        ImmutableDictionary<string, OpeningBinding> Bindings, ImmutableHashSet<string> GrantedKeys,
        ImmutableArray<BattleOpeningAllyLink> Links, ImmutableArray<BattleRememberedGain> Gains)
    {
        public static RememberedState Empty => new(ImmutableDictionary<string, RememberMarker>.Empty,
            ImmutableDictionary<string, OpeningBinding>.Empty, ImmutableHashSet<string>.Empty, [], []);
    }
    private RememberedState _rememberedState = RememberedState.Empty;
    public ImmutableArray<BattleOpeningAllyLink> OpeningLinks => _rememberedState.Links;
    public ImmutableArray<BattleRememberedGain> RememberedGains => _rememberedState.Gains;

    public void RegisterRememberer(BattleUnitState owner, CompiledMatrixOperation operation, CombatSourceRef origin)
    {
        if (!owner.IsPersistentRosterHero || owner.IsTemporary || !owner.Definition.IsHero) return;
        if (_rememberedState.Receivers.ContainsKey(owner.RuntimeId)) return;
        _rememberedState = _rememberedState with { Receivers = _rememberedState.Receivers.SetItem(owner.RuntimeId,
            new(origin.StableId, operation.Key)) };
    }

    public ImmutableArray<BattleOpeningAllyLink> BindOpeningAllies(BattleUnitState owner,
        CompiledMatrixOperation operation, IReadOnlyList<BattleUnitState> units)
    {
        var bindingKey = owner.RuntimeId + ":" + operation.Key;
        if (_rememberedState.Bindings.ContainsKey(bindingKey)) return [];
        var candidates = units.Where(unit => unit.Alive && unit.Team == owner.Team &&
            unit.IsPersistentRosterHero && unit.Definition.IsHero && !unit.IsTemporary && unit != owner).ToArray();
        var selected = BattleOpeningAllySelector.Select(owner.SourceInstanceId, owner.Position,
            candidates.Select(unit => new BattleOpeningAllyCandidate(unit.SourceInstanceId, unit.Position)), operation.MaxTargets);
        var targets = selected.Select(id => candidates.Single(unit => unit.SourceInstanceId == id)).ToImmutableArray();
        var links = targets.Select(target => new BattleOpeningAllyLink(owner.RuntimeId, owner.SourceInstanceId,
            target.RuntimeId, target.SourceInstanceId, operation.Key)).ToImmutableArray();
        _rememberedState = _rememberedState with
        {
            Bindings = _rememberedState.Bindings.Add(bindingKey, new(targets)),
            Links = _rememberedState.Links.AddRange(links)
        };
        return links;
    }

    public ImmutableArray<BattleUnitState> ApplyRememberableAttribute(BattleUnitState owner,
        CompiledMatrixOperation operation, CombatSourceRef origin, int tick)
    {
        if (!owner.Alive || !_rememberedState.Bindings.TryGetValue(owner.RuntimeId + ":" + operation.Key, out var binding)) return [];
        var applied = ImmutableArray.CreateBuilder<BattleUnitState>();
        foreach (var target in binding.Targets)
        {
            if (!target.Alive || target.Team != owner.Team || target.IsTemporary || !target.IsPersistentRosterHero || target == owner) continue;
            var key = $"{owner.RuntimeId}:{origin.StableId}:{operation.Key}:{target.RuntimeId}:{operation.Attribute}";
            if (_rememberedState.GrantedKeys.Contains(key)) continue;
            target.Attributes.ApplyModifier(new CompiledAttributeModifier(operation.Attribute,
                AttributeModifierOperation.PostMultiplyAdd, new CompiledConstantMagnitude(operation.Amount), 0,
                "rememberable:" + operation.Key + ":" + operation.Attribute), origin);
            if (operation.Attribute == CombatAttribute.MaxHealth)
                target.Health = Math.Min(target.MaxHealth, target.Health + operation.Amount);
            _rememberedState = _rememberedState with { GrantedKeys = _rememberedState.GrantedKeys.Add(key) };
            if (owner.Team == 0 && owner.Definition.IsHero && owner.IsPersistentRosterHero && !owner.IsTemporary &&
                _rememberedState.Receivers.TryGetValue(target.RuntimeId, out var remember))
                _rememberedState = _rememberedState with { Gains = _rememberedState.Gains.Add(new(
                    owner.SourceInstanceId, target.SourceInstanceId, origin.StableId, operation.Key,
                    remember.AbilityId, remember.Key, operation.Attribute, operation.Amount, 1, tick)) };
            applied.Add(target);
        }
        return applied.ToImmutable();
    }
}
