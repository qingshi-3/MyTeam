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

public sealed record MatrixPoisonTransferReceipt(string SourceId, string FromId, string ToId, int Layers);

internal sealed class BattleElementStatusScope(IBattleTraitQueries read, IBattleTraitCommands commands,
    BattleTraitBindings bindings, BattleStatusScope statuses, CompiledMatrixStatusRules rules)
{
    private sealed record TraitPoison(long Sequence, string Source, int SourceTeam, string Target, int Expires, int NextTick,
        float Damage, string StatusInstance);
    private sealed record TraitChill(string Source, int Stacks, int Expires, string StatusInstance);
    private sealed record TraitEmber(string Source, int Stacks, int Expires, string StatusInstance = "");
    private sealed record State(ImmutableArray<TraitPoison> Poison, ImmutableDictionary<string, TraitChill> Chill,
        ImmutableDictionary<string, TraitEmber> Ember, ImmutableDictionary<string, float> FrozenDeaths, long Sequence)
    {
        public static State Empty => new([], ImmutableDictionary<string, TraitChill>.Empty,
            ImmutableDictionary<string, TraitEmber>.Empty, ImmutableDictionary<string, float>.Empty, 0);
    }
    private State _state = State.Empty;
    private int TickIndex => read.Tick;
    private BattleUnitView? TraitUnit(string id) => read.Units.FirstOrDefault(unit => unit.RuntimeId == id);
    private float TraitCounter(string key) => _state.FrozenDeaths.GetValueOrDefault(key);
    private void TraitWrite(string key, float value) => _state = _state with { FrozenDeaths = _state.FrozenDeaths.SetItem(key, value) };
    private IEnumerable<BattleTraitBinding> TraitFor(string owner, TraitMechanicKind kind) => bindings.For(owner, kind);
    public bool HasPending => !_state.Poison.IsEmpty || !_state.Chill.IsEmpty || !_state.Ember.IsEmpty;
    public string? ChillInstance(string id) => _state.Chill.GetValueOrDefault(id)?.StatusInstance;
    public void ClearChill(string id, string instance)
    {
        statuses.MatrixRemoveInstance(instance, TickIndex);
        _state = _state with { Chill = _state.Chill.Remove(id) };
    }
    internal sealed class Checkpoint
    {
        private readonly BattleElementStatusScope _owner;
        private readonly State _state;
        internal Checkpoint(BattleElementStatusScope owner) { _owner = owner; _state = owner._state; }
        internal void Restore(BattleElementStatusScope owner)
        {
            if (!ReferenceEquals(owner, _owner)) throw new InvalidOperationException("Checkpoint belongs to another battle module.");
            owner._state = _state;
        }
    }
    public Checkpoint Capture() => new(this);
    public void Restore(Checkpoint checkpoint) => checkpoint.Restore(this);
    public void Clear() => _state = State.Empty;
    public int ReadPoison(string targetId, int? sourceTeam = null)
    {
        var deathKey = $"death_poison:{targetId}:{sourceTeam?.ToString() ?? "all"}";
        if (TraitUnit(targetId) is { Alive: false } && _state.FrozenDeaths.TryGetValue(deathKey, out var frozen)) return (int)frozen;
        var visible = TraitVisibleStatusIds(targetId);
        return _state.Poison.Count(p => p.Target == targetId && p.Expires > TickIndex && visible.Contains(p.StatusInstance) &&
            (sourceTeam is null || p.SourceTeam == sourceTeam));
    }

    private HashSet<string> TraitVisibleStatusIds(string ownerId) => statuses.SnapshotOwner(ownerId)
        .Select(s => s.InstanceId).ToHashSet(StringComparer.Ordinal);

    public void ApplyPoison(string sourceId, string targetId, int layers)
    {
        if (TraitUnit(sourceId) is not { } source || TraitUnit(targetId) is not { Alive: true }) return;
        var bonus = TraitFor(sourceId, TraitMechanicKind.PoisonOnAttack).Sum(b => b.Spec.Amount);
        for (var i = 0; i < Math.Clamp(layers, 0, 1000); i++)
        {
            var status = statuses.Apply(BattleMechanicStatus.Create("matrix_poison", "棘毒", rules.PoisonDurationTicks, harmful: true), sourceId, targetId, TickIndex);
            if (!status.Applied || status.Status is null) continue;
            var sequence = _state.Sequence + 1;
            _state = _state with { Sequence = sequence,
                Poison = _state.Poison.Add(new(sequence, sourceId, source.Team, targetId, TickIndex + rules.PoisonDurationTicks, TickIndex + rules.PoisonIntervalTicks,
                    rules.PoisonDamagePerLayer * (1 + bonus), status.Status.InstanceId)) };
        }
    }

    public int ConsumePoison(string targetId, int layers)
    {
        var visible = TraitVisibleStatusIds(targetId);
        var selected = _state.Poison.Where(p => p.Target == targetId && p.Expires > TickIndex && visible.Contains(p.StatusInstance))
            .OrderByDescending(p => p.Expires).ThenBy(p => p.Sequence).Take(Math.Max(0, layers)).ToArray();
        foreach (var p in selected) statuses.MatrixRemoveInstance(p.StatusInstance, TickIndex);
        var ids = selected.Select(p => p.Sequence).ToHashSet();
        _state = _state with { Poison = _state.Poison.Where(p => !ids.Contains(p.Sequence)).ToImmutableArray() };
        return selected.Length;
    }

    public ImmutableArray<MatrixPoisonTransferReceipt> TransferPoison(string fromId, string toId, int layers, int? sourceTeam = null)
    {
        if (fromId == toId || TraitUnit(toId) is not { Alive: true }) return [];
        var visible = TraitVisibleStatusIds(fromId);
        var selected = _state.Poison.Where(p => p.Target == fromId && p.Expires > TickIndex && visible.Contains(p.StatusInstance) &&
                (sourceTeam is null || p.SourceTeam == sourceTeam))
            .OrderByDescending(p => p.Expires).ThenBy(p => p.Sequence).Take(Math.Max(0, layers)).ToArray();
        foreach (var p in selected)
        {
            statuses.MatrixRemoveInstance(p.StatusInstance, TickIndex);
            var applied = statuses.Apply(BattleMechanicStatus.Create("matrix_poison", "棘毒", p.Expires - TickIndex, harmful: true),
                p.Source, toId, TickIndex);
            _state = _state with { Poison = _state.Poison.Remove(p) };
            if (applied.Applied && applied.Status is not null)
                _state = _state with { Poison = _state.Poison.Add(p with
                { Target = toId, StatusInstance = applied.Status.InstanceId }) };
        }
        var receipts = selected.GroupBy(p => p.Source).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new MatrixPoisonTransferReceipt(g.Key, fromId, toId, g.Count())).ToImmutableArray();
        foreach (var receipt in receipts)
            commands.Publish(new(BattleCombatEventKind.StatusStackChanged, CombatSourceRef.System("poison_transfer"),
                receipt.SourceId, toId, TickIndex, SubjectStableId: "matrix_poison_transfer", CurrentStacks: receipt.Layers));
        return receipts;
    }

    public bool ApplyChill(string sourceId, string targetId, int layers)
    {
        if (TraitUnit(targetId) is not { Alive: true } || layers <= 0) return false;
        var limit = TraitFor(sourceId, TraitMechanicKind.ChillOnAttack).Select(b => b.Spec.Count).DefaultIfEmpty(rules.ChillDefaultStackLimit).Max();
        _state.Chill.TryGetValue(targetId, out var previous);
        var oldStacks = ReadChill(targetId);
        var stacks = Math.Min(Math.Max(limit, oldStacks), oldStacks + layers);
        if (previous is not null) statuses.MatrixRemoveInstance(previous.StatusInstance, TickIndex);
        // Attribute projections are per stack. Their product yields the authored linear
        // total slow while the public Status model exposes the real number of layers.
        var perStackFactor = MathF.Pow(1 - rules.ChillSlowPerLayer * stacks, 1f / stacks);
        var modifiers = ImmutableArray.Create(
            new CompiledAttributeModifier(CombatAttribute.AttackSpeed, AttributeModifierOperation.Multiply, new CompiledConstantMagnitude(perStackFactor), 0, "chill_attack"),
            new CompiledAttributeModifier(CombatAttribute.MoveSpeed, AttributeModifierOperation.Multiply, new CompiledConstantMagnitude(perStackFactor), 0, "chill_move"));
        var definition = BattleMechanicStatus.Create("matrix_chill", "寒意", rules.ChillDurationTicks, harmful: true, modifiers: modifiers) with
        { AggregationPolicy = StatusAggregationPolicy.BySource, StackLimit = stacks };
        var applied = statuses.ApplyBatch(Enumerable.Range(0, stacks)
            .Select(_ => new StatusApplicationRequest(definition, sourceId, targetId, TickIndex)))[^1];
        if (!applied.Applied || applied.Status is null) return false;
        _state = _state with { Chill = _state.Chill.SetItem(targetId, new(sourceId, stacks, TickIndex + rules.ChillDurationTicks, applied.Status.InstanceId)) };
        return true;
    }

    public int ReadChill(string targetId) => TraitUnit(targetId) is { Alive: false }
        ? (int)TraitCounter($"death_chill:{targetId}")
        : _state.Chill.TryGetValue(targetId, out var chill) && chill.Expires > TickIndex &&
          TraitVisibleStatusIds(targetId).Contains(chill.StatusInstance) ? chill.Stacks : 0;

    public int ReadEmber(string targetId) => TraitUnit(targetId) is { Alive: false }
        ? (int)TraitCounter($"death_ember:{targetId}")
        : _state.Ember.TryGetValue(targetId, out var ember) && ember.Expires > TickIndex &&
          TraitVisibleStatusIds(targetId).Contains(ember.StatusInstance) ? ember.Stacks : 0;
    public void ApplyEmber(string sourceId, string targetId, int layers)
    {
        if (TraitUnit(targetId) is not { Alive: true }) return;
        _state = _state with { Ember = _state.Ember.SetItem(targetId,
            new(sourceId, Math.Clamp(ReadEmber(targetId) + layers, 0, rules.EmberStackLimit), TickIndex + rules.EmberDurationTicks)) };
        TraitRefreshEmberStatus(targetId);
    }
    public int ConsumeEmber(string targetId, int layers)
    {
        var old = ReadEmber(targetId);
        var consumed = Math.Min(old, Math.Max(0, layers));
        if (_state.Ember.TryGetValue(targetId, out var ember))
            _state = _state with { Ember = old == consumed ? _state.Ember.Remove(targetId) :
                _state.Ember.SetItem(targetId, ember with { Stacks = old - consumed }) };
        TraitRefreshEmberStatus(targetId);
        return consumed;
    }
    private void TraitRefreshEmberStatus(string targetId)
    {
        foreach (var status in statuses.SnapshotOwner(targetId).Where(s => s.StableId == "matrix_ember"))
            statuses.MatrixRemoveInstance(status.InstanceId, TickIndex);
        if (_state.Ember.TryGetValue(targetId, out var ember) && ember.Expires > TickIndex && ember.Stacks > 0)
        {
            var definition = BattleMechanicStatus.Create("matrix_ember", "余烬", ember.Expires - TickIndex, harmful: true) with
            { AggregationPolicy = StatusAggregationPolicy.BySource, StackLimit = ember.Stacks };
            var applied = statuses.ApplyBatch(Enumerable.Range(0, ember.Stacks)
                .Select(_ => new StatusApplicationRequest(definition, ember.Source, targetId, TickIndex)))[^1];
            _state = _state with { Ember = applied.Applied && applied.Status is not null
                ? _state.Ember.SetItem(targetId, ember with { StatusInstance = applied.Status.InstanceId })
                : _state.Ember.Remove(targetId) };
        }
    }

    public void FreezeDeath(string id)
    {
        var target = TraitUnit(id)!;
        // Death listeners execute after authoritative cleanup. Keep only immutable facts,
        // never the ticking layers, so killed-poisoned/frozen predicates remain truthful.
        var visible = TraitVisibleStatusIds(target.RuntimeId);
        TraitWrite($"death_poison:{target.RuntimeId}:all", _state.Poison.Count(p => p.Target == target.RuntimeId &&
            p.Expires > TickIndex && visible.Contains(p.StatusInstance)));
        for (var team = 0; team <= 1; team++)
            TraitWrite($"death_poison:{target.RuntimeId}:{team}", _state.Poison.Count(p => p.Target == target.RuntimeId &&
                p.Expires > TickIndex && p.SourceTeam == team && visible.Contains(p.StatusInstance)));
        TraitWrite($"death_chill:{target.RuntimeId}", _state.Chill.TryGetValue(target.RuntimeId, out var chill) &&
            chill.Expires > TickIndex && visible.Contains(chill.StatusInstance) ? chill.Stacks : 0);
        TraitWrite($"death_ember:{target.RuntimeId}", _state.Ember.TryGetValue(target.RuntimeId, out var ember) &&
            ember.Expires > TickIndex && visible.Contains(ember.StatusInstance) ? ember.Stacks : 0);
    }
    public void RemoveLive(string targetId) => _state = _state with
    { Poison = _state.Poison.Where(p => p.Target != targetId).ToImmutableArray(),
        Chill = _state.Chill.Remove(targetId), Ember = _state.Ember.Remove(targetId) };
    public void Advance()
    {
        foreach (var p in _state.Poison.ToArray())
        {
            // A previous layer may kill this target and transfer all remaining layers.
            if (!_state.Poison.Contains(p)) continue;
            var target = TraitUnit(p.Target);
            var visible = target is { Alive: true } && target.Statuses.Any(s => s.InstanceId == p.StatusInstance);
            if (!visible || p.Expires < TickIndex)
            { _state = _state with { Poison = _state.Poison.Remove(p) }; continue; }
            if (p.NextTick > TickIndex) continue;
            _state = _state with { Poison = _state.Poison.Replace(p, p with { NextTick = p.NextTick + rules.PoisonIntervalTicks }) };
            commands.Damage(p.Source, target!.RuntimeId, p.Damage,
                CombatSourceRef.Status("matrix_poison", p.Source, $"poison:{p.Sequence}"), EffectDamageType.True,
                damageClass: CombatDamageClass.Periodic);
        }
        foreach (var pair in _state.Chill.ToArray())
            if (pair.Value.Expires <= TickIndex || TraitUnit(pair.Key) is not { Alive: true } u ||
                !u.Statuses.Any(s => s.InstanceId == pair.Value.StatusInstance))
                _state = _state with { Chill = _state.Chill.Remove(pair.Key) };
        foreach (var pair in _state.Ember.ToArray())
            if (pair.Value.Expires <= TickIndex || TraitUnit(pair.Key) is not { Alive: true } u ||
                !u.Statuses.Any(s => s.InstanceId == pair.Value.StatusInstance))
                _state = _state with { Ember = _state.Ember.Remove(pair.Key) };
    }
}
