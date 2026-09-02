using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Traits;

public sealed class BattleTraitScope : IDisposable
{
    private readonly Dictionary<string, TraitOwnerBinding> _owners = new(StringComparer.Ordinal);
    private readonly Dictionary<TierKey, ActiveTierState> _activeTiers = [];
    private readonly ImmutableArray<CompiledTraitDefinition> _definitions;
    private ImmutableArray<TraitContributionInput> _contributions;
    private TraitSnapshot _snapshot;
    private TraitSnapshot? _projectionSnapshot;
    private TraitBattleTransitionResult? _transition;

    public BattleTraitScope(
        string scopeId,
        TraitBattlePreparation preparation,
        IEnumerable<TraitOwnerBinding> owners)
    {
        if (string.IsNullOrWhiteSpace(scopeId))
            throw new ArgumentException("Trait Battle scope id is required.", nameof(scopeId));
        ArgumentNullException.ThrowIfNull(preparation);
        ArgumentNullException.ThrowIfNull(owners);
        if (!TraitBattlePreparationBuilder.HasValidFingerprint(preparation))
            throw new ArgumentException("Trait Battle preparation fingerprint is invalid.", nameof(preparation));
        ScopeId = scopeId;
        SourceFingerprint = preparation.SourceFingerprint;
        _definitions = preparation.Definitions;
        _contributions = preparation.Contributions;
        _snapshot = TraitSnapshotBuilder.Build(_definitions, _contributions);
        foreach (var owner in owners.OrderBy(owner => owner.RuntimeId, StringComparer.Ordinal))
            ValidateAndAddOwner(owner);
        try { ApplySnapshot(_snapshot); }
        catch
        {
            RemoveAll(bestEffort: true);
            throw;
        }
    }

    public string ScopeId { get; }
    public string SourceFingerprint { get; }
    public TraitSnapshot Snapshot => _snapshot;
    public bool IsCompleted => _transition is not null;
    public int LiveTierCount => _activeTiers.Count;
    public int LiveModifierHandleCount => _activeTiers.Values.Sum(state =>
        state.Handles.Values.Sum(handles => handles.Length));
    public TraitBattleTransitionResult? Transition => _transition;

    public void Refresh(IEnumerable<TraitContributionInput> contributions)
    {
        EnsureActive();
        ArgumentNullException.ThrowIfNull(contributions);
        ExecuteTransaction(() =>
        {
            _contributions = contributions.ToImmutableArray();
            var next = TraitSnapshotBuilder.Build(_definitions, _contributions);
            ApplySnapshot(next);
            _snapshot = next;
        });
    }

    public void AddOwnerAndContributions(
        TraitOwnerBinding owner,
        IEnumerable<TraitContributionInput> contributions)
    {
        EnsureActive();
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(contributions);
        ExecuteTransaction(() =>
        {
            ValidateAndAddOwner(owner);
            _contributions = _contributions.AddRange(contributions);
            var next = TraitSnapshotBuilder.Build(_definitions, _contributions);
            ApplySnapshot(next);
            _snapshot = next;
        }, owner.Attributes);
    }

    internal TraitStateCheckpoint CaptureState()
    {
        EnsureActive();
        return new TraitStateCheckpoint(this);
    }

    internal void RestoreState(TraitStateCheckpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        checkpoint.Restore(this);
    }

    public TraitBattleTransitionResult Complete(TraitBattleCompletionReason reason)
    {
        if (_transition is not null) return _transition;
        if (reason == TraitBattleCompletionReason.None) throw new ArgumentOutOfRangeException(nameof(reason));
        Exception? failure = null;
        try { RemoveAll(bestEffort: false); }
        catch (Exception exception) { failure = exception; }
        _owners.Clear();
        _contributions = [];
        _transition = new TraitBattleTransitionResult(
            SourceFingerprint,
            failure is null ? reason : TraitBattleCompletionReason.Exception,
            LiveTierCount,
            LiveModifierHandleCount);
        if (failure is not null) throw failure;
        return _transition;
    }

    public void Dispose()
    {
        if (_transition is null) Complete(TraitBattleCompletionReason.Disposal);
    }

    private void ExecuteTransaction(Action mutation, BattleAttributeSet? additionalSet = null)
    {
        var state = new TraitStateCheckpoint(this);
        var sets = new HashSet<BattleAttributeSet>(ReferenceEqualityComparer.Instance);
        foreach (var owner in _owners.Values) sets.Add(owner.Attributes);
        if (additionalSet is not null) sets.Add(additionalSet);
        var attributeStates = new Dictionary<
            BattleAttributeSet,
            BattleAttributeSet.ModifierStateCheckpoint>(ReferenceEqualityComparer.Instance);
        foreach (var set in sets) attributeStates.Add(set, set.CaptureModifierState());
        try { mutation(); }
        catch
        {
            foreach (var pair in attributeStates) pair.Key.RestoreModifierState(pair.Value);
            state.Restore(this);
            throw;
        }
    }

    private void ApplySnapshot(TraitSnapshot next)
    {
        _projectionSnapshot = next;
        try
        {
            var nextValues = next.Values.ToDictionary(value => new TierKey(value.Team, value.TraitId));
            foreach (var key in _activeTiers.Keys.Union(nextValues.Keys).OrderBy(key => key.Team)
                         .ThenBy(key => key.TraitId, StringComparer.Ordinal).ToArray())
            {
                _activeTiers.TryGetValue(key, out var current);
                nextValues.TryGetValue(key, out var nextValue);
                var nextBreakpoint = nextValue?.ActiveBreakpoint;
                if (current is not null &&
                    (nextBreakpoint is null || current.Breakpoint.Fingerprint != nextBreakpoint.Fingerprint))
                {
                    Remove(current, bestEffort: false);
                    _activeTiers.Remove(key);
                    current = null;
                }
                if (nextBreakpoint is null) continue;
                if (current is null)
                {
                    var definition = _definitions.Single(definition => definition.StableId == key.TraitId);
                    current = new ActiveTierState(definition, nextBreakpoint);
                    _activeTiers.Add(key, current);
                }
                foreach (var owner in _owners.Values.Where(owner => owner.Team == key.Team)
                             .OrderBy(owner => owner.RuntimeId, StringComparer.Ordinal))
                {
                    current.Owners[owner.RuntimeId] = owner;
                    if (!current.Handles.ContainsKey(owner.RuntimeId))
                        current.Handles.Add(owner.RuntimeId, Apply(current, owner));
                }
            }
        }
        finally { _projectionSnapshot = null; }
    }

    private ImmutableArray<AttributeModifierHandle> Apply(
        ActiveTierState tier,
        TraitOwnerBinding owner)
    {
        var handles = ImmutableArray.CreateBuilder<AttributeModifierHandle>();
        var source = new CombatSourceRef(
            CombatSourceKind.Trait,
            tier.Definition.StableId,
            owner.RuntimeId,
            $"{ScopeId}:{owner.Team}:{tier.Definition.StableId}:{tier.Breakpoint.Index}:{owner.RuntimeId}");
        try
        {
            foreach (var modifier in tier.Breakpoint.AttributeModifiers)
                handles.Add(owner.Attributes.ApplyModifier(
                    modifier,
                    source,
                    new BattleAttributeMagnitudeContext(
                        owner.Attributes,
                        owner.Attributes,
                        traitValue: (traitId, team) => (_projectionSnapshot ?? _snapshot).Value(traitId, team))));
            return handles.ToImmutable();
        }
        catch
        {
            foreach (var handle in handles.ToImmutable().Reverse()) owner.Attributes.Remove(handle);
            throw;
        }
    }

    private static void Remove(ActiveTierState tier, bool bestEffort)
    {
        Exception? failure = null;
        foreach (var pair in tier.Handles.OrderByDescending(pair => pair.Key, StringComparer.Ordinal))
        foreach (var handle in pair.Value.Reverse())
            if (!tier.Owners[pair.Key].Attributes.Remove(handle) && !bestEffort)
                failure ??= new InvalidOperationException(
                    $"Trait modifier handle could not be removed: {tier.Definition.StableId}:{pair.Key}");
        tier.Handles.Clear();
        tier.Owners.Clear();
        if (!bestEffort && failure is not null) throw failure;
    }

    private void RemoveAll(bool bestEffort)
    {
        Exception? failure = null;
        foreach (var state in _activeTiers.OrderByDescending(pair => pair.Key.Team)
                     .ThenByDescending(pair => pair.Key.TraitId, StringComparer.Ordinal).Select(pair => pair.Value))
            try { Remove(state, bestEffort); }
            catch (Exception exception) { failure ??= exception; }
        _activeTiers.Clear();
        if (!bestEffort && failure is not null) throw failure;
    }

    private void ValidateAndAddOwner(TraitOwnerBinding owner)
    {
        if (string.IsNullOrWhiteSpace(owner.RuntimeId) || owner.Team is < 0 or > 1 ||
            owner.Attributes is null || owner.Attributes.OwnerRuntimeId != owner.RuntimeId)
            throw new ArgumentException("Trait owner binding is invalid.", nameof(owner));
        if (!_owners.TryAdd(owner.RuntimeId, owner))
            throw new ArgumentException($"Duplicate Trait owner runtime id: {owner.RuntimeId}", nameof(owner));
        foreach (var tier in _activeTiers.Values) tier.Owners[owner.RuntimeId] = owner;
    }

    private void EnsureActive()
    {
        if (_transition is not null) throw new InvalidOperationException("Trait Battle scope has completed.");
    }

    private readonly record struct TierKey(int Team, string TraitId);

    private sealed class ActiveTierState(
        CompiledTraitDefinition definition,
        CompiledTraitBreakpoint breakpoint)
    {
        public CompiledTraitDefinition Definition { get; } = definition;
        public CompiledTraitBreakpoint Breakpoint { get; } = breakpoint;
        public Dictionary<string, ImmutableArray<AttributeModifierHandle>> Handles { get; } =
            new(StringComparer.Ordinal);
        public Dictionary<string, TraitOwnerBinding> Owners { get; } = new(StringComparer.Ordinal);
    }

    internal sealed class TraitStateCheckpoint
    {
        private readonly string _scopeId;
        private readonly TraitSnapshot _snapshot;
        private readonly ImmutableArray<TraitContributionInput> _contributions;
        private readonly Dictionary<string, TraitOwnerBinding> _owners;
        private readonly Dictionary<TierKey, ActiveTierCheckpoint> _tiers;

        internal TraitStateCheckpoint(BattleTraitScope owner)
        {
            _scopeId = owner.ScopeId;
            _snapshot = owner._snapshot;
            _contributions = owner._contributions;
            _owners = new Dictionary<string, TraitOwnerBinding>(owner._owners, StringComparer.Ordinal);
            _tiers = owner._activeTiers.ToDictionary(
                pair => pair.Key,
                pair => new ActiveTierCheckpoint(
                    pair.Value.Definition,
                    pair.Value.Breakpoint,
                    pair.Value.Handles.ToDictionary(
                        handles => handles.Key,
                        handles => handles.Value,
                        StringComparer.Ordinal),
                    pair.Value.Owners.ToDictionary(
                        binding => binding.Key,
                        binding => binding.Value,
                        StringComparer.Ordinal)));
        }

        internal void Restore(BattleTraitScope owner)
        {
            if (owner.ScopeId != _scopeId || owner._transition is not null)
                throw new InvalidOperationException("Trait checkpoint belongs to another or completed scope.");
            owner._snapshot = _snapshot;
            owner._contributions = _contributions;
            owner._owners.Clear();
            foreach (var pair in _owners) owner._owners.Add(pair.Key, pair.Value);
            owner._activeTiers.Clear();
            foreach (var pair in _tiers)
            {
                var state = new ActiveTierState(pair.Value.Definition, pair.Value.Breakpoint);
                foreach (var handles in pair.Value.Handles) state.Handles.Add(handles.Key, handles.Value);
                foreach (var binding in pair.Value.Owners) state.Owners.Add(binding.Key, binding.Value);
                owner._activeTiers.Add(pair.Key, state);
            }
        }
    }

    private sealed record ActiveTierCheckpoint(
        CompiledTraitDefinition Definition,
        CompiledTraitBreakpoint Breakpoint,
        Dictionary<string, ImmutableArray<AttributeModifierHandle>> Handles,
        Dictionary<string, TraitOwnerBinding> Owners);
}
