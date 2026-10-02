using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Battle;

internal sealed record BattleBrokenShield(string Target, float Absorbed, CombatSourceRef Origin, string ChainId = "", int Depth = 0);

internal sealed class BattleSupportEffectScope(IBattleTraitQueries read, IBattleTraitCommands commands, BattleTraitBindings bindings)
{
    private sealed record TraitShield(string Source, string Target, int Expires, float Remaining);
    private sealed record TraitMana(string Source, string Target, float Amount);
    private sealed record State(ImmutableArray<TraitShield> Shields, ImmutableArray<TraitMana> Mana,
        ImmutableArray<BattleBrokenShield> Broken, ImmutableDictionary<string, float> Absorbed)
    {
        public static State Empty => new([], [], [], ImmutableDictionary<string, float>.Empty);
    }
    private State _state = State.Empty;
    private int TickIndex => read.Tick;
    private BattleUnitView? TraitUnit(string id) => read.Units.FirstOrDefault(unit => unit.RuntimeId == id);
    private float TraitCounter(string key) => _state.Absorbed.GetValueOrDefault(key);
    private void TraitWrite(string key, float value) => _state = _state with { Absorbed = _state.Absorbed.SetItem(key, value) };
    private IEnumerable<BattleTraitBinding> TraitFor(string owner, TraitMechanicKind kind) => bindings.For(owner, kind);
    public bool HasPending => !_state.Shields.IsEmpty || !_state.Mana.IsEmpty;
    public bool HasBroken => !_state.Broken.IsEmpty;
    public bool HasUnlinkedBreak(BattleCombatEvent fact) => _state.Broken.Any(broken =>
        broken.Target == fact.TargetRuntimeId && broken.Origin == fact.Source && broken.ChainId.Length == 0);
    public BattleBrokenShield TakeBroken()
    {
        var broken = _state.Broken[0];
        _state = _state with { Broken = _state.Broken.RemoveAt(0) };
        return broken;
    }
    public void Observe(BattleCombatEvent fact)
    {
        if (fact.Kind != BattleCombatEventKind.DamageResolved) return;
        for (var index = 0; index < _state.Broken.Length; index++)
            if (_state.Broken[index].Target == fact.TargetRuntimeId && _state.Broken[index].Origin == fact.Source &&
                _state.Broken[index].ChainId.Length == 0)
            {
                _state = _state with { Broken = _state.Broken.SetItem(index,
                    _state.Broken[index] with { ChainId = fact.ChainId, Depth = fact.Depth + 1 }) };
                break;
            }
    }
    internal sealed class Checkpoint
    {
        private readonly BattleSupportEffectScope _owner;
        private readonly State _state;
        internal Checkpoint(BattleSupportEffectScope owner) { _owner = owner; _state = owner._state; }
        internal void Restore(BattleSupportEffectScope owner)
        {
            if (!ReferenceEquals(owner, _owner)) throw new InvalidOperationException("Checkpoint belongs to another battle module.");
            owner._state = _state;
        }
    }
    public Checkpoint Capture() => new(this);
    public void Restore(Checkpoint checkpoint) => checkpoint.Restore(this);
    public void Clear() => _state = State.Empty;
    public float ApplyShield(string sourceId, string targetId, float amount, int durationTicks, CombatSourceRef origin = default)
    {
        var target = TraitUnit(targetId);
        if (target is not { Alive: true } || amount <= 0) return 0;
        var extra = TraitFor(sourceId, TraitMechanicKind.BarrierRetaliation).Any(b => b.Spec.Enabled) ? 20 : 0;
        if (target.Shield <= 0) TraitWrite($"shield_absorbed:{targetId}", 0);
        var granted = commands.Shield(sourceId, targetId, amount, origin);
        if (granted > 0) _state = _state with
        { Shields = _state.Shields.Add(new(sourceId, targetId, TickIndex + Math.Max(1, durationTicks + extra), granted)) };
        return granted;
    }

    public void ShieldConsumed(BattleUnitView target, float amount, bool enemyDamage, CombatSourceRef origin)
    {
        var remaining = amount;
        var shields = _state.Shields.ToBuilder();
        for (var i = 0; i < shields.Count && remaining > 0; i++)
        {
            if (shields[i].Target != target.RuntimeId) continue;
            var spent = Math.Min(remaining, shields[i].Remaining);
            remaining -= spent;
            shields[i] = shields[i] with { Remaining = shields[i].Remaining - spent };
        }
        _state = _state with { Shields = shields.Where(s => s.Remaining > 0).ToImmutableArray() };
        if (!bindings.ObserveBreaks) return;
        var key = $"shield_absorbed:{target.RuntimeId}";
        if (enemyDamage) TraitWrite(key, TraitCounter(key) + amount);
        if (target.Shield > 0) return;
        if (enemyDamage && amount > 0)
            _state = _state with { Broken = _state.Broken.Add(new(target.RuntimeId, TraitCounter(key), origin)) };
        TraitWrite(key, 0);
    }

    public void GrantMana(string sourceId, string targetId, float amount, bool deferWhileLocked = false)
    {
        var unit = TraitUnit(targetId);
        if (unit is not { Alive: true } || unit.MaxMana <= 0 || amount <= 0) return;
        if (deferWhileLocked && read.ManaLocked(unit.RuntimeId))
        {
            _state = _state with { Mana = _state.Mana.Add(new(sourceId, targetId, amount)) };
            return;
        }
        commands.GainMana(targetId, amount);
    }

    public void Advance()
    {
        foreach (var shield in _state.Shields.Where(s => s.Expires <= TickIndex).ToArray())
            if (TraitUnit(shield.Target) is { Alive: true } u)
            {
                commands.RemoveShield(u.RuntimeId, shield.Remaining);
                if (u.Shield == 0) TraitWrite($"shield_absorbed:{u.RuntimeId}", 0);
            }
        _state = _state with { Shields = _state.Shields.Where(s => s.Expires > TickIndex && TraitUnit(s.Target) is { Alive: true }).ToImmutableArray() };
        foreach (var pending in _state.Mana.ToArray())
        {
            var u = TraitUnit(pending.Target);
            if (u is { Alive: true } && read.ManaLocked(u.RuntimeId)) continue;
            _state = _state with { Mana = _state.Mana.Remove(pending) };
            if (u is { Alive: true }) commands.GainMana(u.RuntimeId, pending.Amount);
        }
    }
}
