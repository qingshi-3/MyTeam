using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TowerAutobattler.Attributes;

public sealed class BattleAttributeSet
{
    private readonly Dictionary<CombatAttribute, float> _baseOverrides = [];
    private readonly Dictionary<ModifierKey, RuntimeModifier> _modifiers = [];
    private readonly Dictionary<long, ModifierKey> _handleKeys = [];
    private readonly HashSet<CombatAttribute> _evaluationStack = [];
    private ImmutableDictionary<CombatAttribute, float> _completedBases = ImmutableDictionary<CombatAttribute, float>.Empty;
    private ImmutableDictionary<CombatAttribute, float> _completedValues = ImmutableDictionary<CombatAttribute, float>.Empty;
    private readonly Guid _scopeInstanceId;
    private long _handleSequence;
    private long _applicationSequence;

    internal BattleAttributeSet(
        string scopeId,
        string ownerRuntimeId,
        CompiledAttributeSetDefinition definition,
        Guid? scopeInstanceId = null)
    {
        if (string.IsNullOrWhiteSpace(scopeId)) throw new ArgumentException("Attribute scope id is required.", nameof(scopeId));
        if (string.IsNullOrWhiteSpace(ownerRuntimeId)) throw new ArgumentException("Attribute owner id is required.", nameof(ownerRuntimeId));
        ScopeId = scopeId;
        OwnerRuntimeId = ownerRuntimeId;
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _scopeInstanceId = scopeInstanceId ?? Guid.NewGuid();
    }

    public string ScopeId { get; }
    public string OwnerRuntimeId { get; }
    public CompiledAttributeSetDefinition Definition { get; }
    public bool IsCompleted { get; private set; }
    public int ModifierCount => _modifiers.Count;

    public float GetBaseValue(CombatAttribute attribute)
    {
        if (IsCompleted)
            return _completedBases.TryGetValue(attribute, out var completed)
                ? completed
                : throw new KeyNotFoundException($"Attribute '{attribute}' is not defined.");
        return _baseOverrides.TryGetValue(attribute, out var overridden)
            ? overridden
            : Definition.Find(attribute).BaseValue;
    }

    public float GetValue(CombatAttribute attribute)
    {
        if (IsCompleted)
            return _completedValues.TryGetValue(attribute, out var completed)
                ? completed
                : throw new KeyNotFoundException($"Attribute '{attribute}' is not defined.");
        if (!_evaluationStack.Add(attribute))
            throw new InvalidOperationException($"Cyclic live magnitude detected while evaluating '{OwnerRuntimeId}.{attribute}'.");
        try
        {
            var definition = Definition.Find(attribute);
            var ordered = _modifiers.Values.Where(item => item.Modifier.Attribute == attribute)
                .OrderBy(item => item.Modifier.Priority)
                .ThenBy(item => item.ApplicationSequence)
                .ThenBy(item => item.Source)
                .ThenBy(item => item.Modifier.SlotId, StringComparer.Ordinal)
                .ToArray();
            var running = GetBaseValue(attribute);
            foreach (var item in ordered.Where(item => item.Modifier.Operation == AttributeModifierOperation.Add))
                running = Finite(running + ResolveMagnitude(item), attribute);
            foreach (var item in ordered.Where(item => item.Modifier.Operation == AttributeModifierOperation.Multiply))
                running = Finite(running * ResolveMagnitude(item), attribute);
            var winner = ordered.Where(item => item.Modifier.Operation == AttributeModifierOperation.Override)
                .OrderBy(item => item.Modifier.Priority)
                .ThenBy(item => item.ApplicationSequence)
                .ThenBy(item => item.Source)
                .ThenBy(item => item.Modifier.SlotId, StringComparer.Ordinal)
                .LastOrDefault();
            if (winner is not null) running = ResolveMagnitude(winner);
            return Math.Clamp(Finite(running, attribute), definition.Minimum, definition.Maximum);
        }
        finally
        {
            _evaluationStack.Remove(attribute);
        }
    }

    public void SetBaseValue(CombatAttribute attribute, float value)
    {
        EnsureActive();
        Definition.Find(attribute);
        var hadPrevious = _baseOverrides.TryGetValue(attribute, out var previous);
        _baseOverrides[attribute] = Finite(value, attribute);
        try
        {
            _ = GetValue(attribute);
        }
        catch
        {
            if (hadPrevious) _baseOverrides[attribute] = previous;
            else _baseOverrides.Remove(attribute);
            throw;
        }
    }

    public AttributeModifierHandle ApplyModifier(
        CompiledAttributeModifier modifier,
        CombatSourceRef source,
        BattleAttributeMagnitudeContext? context = null)
    {
        EnsureActive();
        ArgumentNullException.ThrowIfNull(modifier);
        Definition.Find(modifier.Attribute);
        if (!source.IsSpecified) throw new ArgumentException("Attribute modifier source is required.", nameof(source));
        if (string.IsNullOrWhiteSpace(modifier.SlotId)) throw new ArgumentException("Attribute modifier slot is required.", nameof(modifier));
        context ??= new BattleAttributeMagnitudeContext(this, this);
        EnsureSameScope(context.Source);
        EnsureSameScope(context.Target);
        var key = new ModifierKey(modifier.Attribute, source, modifier.SlotId);
        var replacing = _modifiers.TryGetValue(key, out var previous);
        var previousHandleSequence = _handleSequence;
        var previousApplicationSequence = _applicationSequence;
        var handleSequence = replacing ? previous!.Handle.Sequence : ++_handleSequence;
        var handle = new AttributeModifierHandle(ScopeId, OwnerRuntimeId, handleSequence, _scopeInstanceId);
        try
        {
            float? captured = null;
            if (modifier.Magnitude.CaptureMode == AttributeCaptureMode.Snapshot)
                captured = EvaluateMagnitudeCore(modifier.Magnitude, context);
            var runtime = new RuntimeModifier(modifier, source, handle, ++_applicationSequence, context, captured);
            _modifiers[key] = runtime;
            _handleKeys[handle.Sequence] = key;
            _ = GetValue(modifier.Attribute);
            return handle;
        }
        catch
        {
            _handleSequence = previousHandleSequence;
            _applicationSequence = previousApplicationSequence;
            if (replacing)
            {
                _modifiers[key] = previous!;
                _handleKeys[previous!.Handle.Sequence] = key;
            }
            else
            {
                _modifiers.Remove(key);
                _handleKeys.Remove(handle.Sequence);
            }
            throw;
        }
    }

    public float EvaluateMagnitude(
        CompiledAttributeMagnitude magnitude,
        BattleAttributeMagnitudeContext context)
    {
        EnsureActive();
        ArgumentNullException.ThrowIfNull(magnitude);
        ArgumentNullException.ThrowIfNull(context);
        EnsureSameScope(context.Source);
        EnsureSameScope(context.Target);
        return EvaluateMagnitudeCore(magnitude, context);
    }

    internal ModifierStateCheckpoint CaptureModifierState() => new(this);

    internal void RestoreModifierState(ModifierStateCheckpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        checkpoint.Restore(this);
    }

    public bool Remove(AttributeModifierHandle handle)
    {
        if (IsCompleted || handle.ScopeInstanceId != _scopeInstanceId || handle.ScopeId != ScopeId ||
            handle.OwnerRuntimeId != OwnerRuntimeId ||
            !_handleKeys.Remove(handle.Sequence, out var key))
            return false;
        return _modifiers.Remove(key);
    }

    public int RemoveSource(CombatSourceRef source)
    {
        if (IsCompleted) return 0;
        var keys = _modifiers.Where(pair => pair.Value.Source == source).Select(pair => pair.Key).ToArray();
        foreach (var key in keys)
        {
            var handle = _modifiers[key].Handle;
            _modifiers.Remove(key);
            _handleKeys.Remove(handle.Sequence);
        }
        return keys.Length;
    }

    internal void Complete()
    {
        if (IsCompleted) return;
        Exception? failure = null;
        var bases = ImmutableDictionary.CreateBuilder<CombatAttribute, float>();
        var values = ImmutableDictionary.CreateBuilder<CombatAttribute, float>();
        foreach (var item in Definition.Attributes)
        {
            var baseValue = GetBaseValue(item.Attribute);
            bases[item.Attribute] = baseValue;
            try { values[item.Attribute] = GetValue(item.Attribute); }
            catch (Exception exception)
            {
                failure ??= exception;
                values[item.Attribute] = Math.Clamp(baseValue, item.Minimum, item.Maximum);
            }
        }
        _completedBases = bases.ToImmutable();
        _completedValues = values.ToImmutable();
        _modifiers.Clear();
        _handleKeys.Clear();
        _baseOverrides.Clear();
        _evaluationStack.Clear();
        IsCompleted = true;
        if (failure is not null) throw failure;
    }

    private float ResolveMagnitude(RuntimeModifier modifier) => modifier.CapturedMagnitude ??
        EvaluateMagnitudeCore(modifier.Modifier.Magnitude, modifier.Context);

    private static float EvaluateMagnitudeCore(
        CompiledAttributeMagnitude magnitude,
        BattleAttributeMagnitudeContext context)
    {
        var value = magnitude switch
        {
            CompiledConstantMagnitude constant => constant.Value,
            CompiledSourceAttributeMagnitude source => context.Source?.GetValue(source.Attribute) ??
                throw new InvalidOperationException("Source-attribute magnitude has no source AttributeSet."),
            CompiledTargetAttributeMagnitude target => context.Target?.GetValue(target.Attribute) ??
                throw new InvalidOperationException("Target-attribute magnitude has no target AttributeSet."),
            CompiledContextValueMagnitude invocation => context.ContextValue(invocation.Key),
            CompiledTeamCountMagnitude count => context.TeamCount(count.CountKind, count.Team),
            CompiledTraitValueMagnitude trait => context.TraitValue(trait.TraitId, trait.Team),
            _ => throw new InvalidOperationException($"Unsupported compiled magnitude: {magnitude.GetType().Name}.")
        };
        if (!float.IsFinite(value)) throw new InvalidOperationException("Attribute magnitude resolved to a non-finite value.");
        return value;
    }

    private void EnsureSameScope(BattleAttributeSet? other)
    {
        if (other is not null && other._scopeInstanceId != _scopeInstanceId)
            throw new InvalidOperationException("Live attribute magnitude cannot reference another Battle scope.");
    }

    private void EnsureActive()
    {
        if (IsCompleted) throw new InvalidOperationException("AttributeSet has completed.");
    }

    private static float Finite(float value, CombatAttribute attribute) => float.IsFinite(value)
        ? value
        : throw new InvalidOperationException($"Attribute '{attribute}' resolved to a non-finite value.");

    private readonly record struct ModifierKey(CombatAttribute Attribute, CombatSourceRef Source, string SlotId);

    internal sealed class ModifierStateCheckpoint
    {
        private readonly Dictionary<CombatAttribute, float> _baseOverrides;
        private readonly Dictionary<ModifierKey, RuntimeModifier> _modifiers;
        private readonly Dictionary<long, ModifierKey> _handleKeys;

        internal ModifierStateCheckpoint(BattleAttributeSet owner)
        {
            _baseOverrides = owner._baseOverrides.ToDictionary(pair => pair.Key, pair => pair.Value);
            _modifiers = owner._modifiers.ToDictionary(pair => pair.Key, pair => pair.Value);
            _handleKeys = owner._handleKeys.ToDictionary(pair => pair.Key, pair => pair.Value);
            HandleSequence = owner._handleSequence;
            ApplicationSequence = owner._applicationSequence;
            ScopeInstanceId = owner._scopeInstanceId;
        }

        internal void Restore(BattleAttributeSet owner)
        {
            if (ScopeInstanceId != owner._scopeInstanceId)
                throw new InvalidOperationException("Attribute modifier checkpoint belongs to another Battle scope.");
            owner._baseOverrides.Clear();
            foreach (var pair in _baseOverrides) owner._baseOverrides.Add(pair.Key, pair.Value);
            owner._modifiers.Clear();
            foreach (var pair in _modifiers) owner._modifiers.Add(pair.Key, pair.Value);
            owner._handleKeys.Clear();
            foreach (var pair in _handleKeys) owner._handleKeys.Add(pair.Key, pair.Value);
            owner._handleSequence = HandleSequence;
            owner._applicationSequence = ApplicationSequence;
            owner._evaluationStack.Clear();
        }

        internal long HandleSequence { get; }
        internal long ApplicationSequence { get; }
        internal Guid ScopeInstanceId { get; }
    }

    private sealed record RuntimeModifier(
        CompiledAttributeModifier Modifier,
        CombatSourceRef Source,
        AttributeModifierHandle Handle,
        long ApplicationSequence,
        BattleAttributeMagnitudeContext Context,
        float? CapturedMagnitude);
}

public sealed class BattleAttributeScope : IDisposable
{
    private readonly Dictionary<string, BattleAttributeSet> _sets = new(StringComparer.Ordinal);
    private readonly Guid _instanceId = Guid.NewGuid();
    private int _lastTick;

    public BattleAttributeScope(string scopeId)
    {
        if (string.IsNullOrWhiteSpace(scopeId)) throw new ArgumentException("Attribute scope id is required.", nameof(scopeId));
        ScopeId = scopeId;
    }

    public string ScopeId { get; }
    public bool IsCompleted => Transition is not null;
    public int LiveSetCount => _sets.Count;
    public int ModifierCount => _sets.Values.Sum(set => set.ModifierCount);
    public AttributeScopeTransitionResult? Transition { get; private set; }

    public BattleAttributeSet CreateSet(string ownerRuntimeId, CompiledAttributeSetDefinition definition)
    {
        if (IsCompleted) throw new InvalidOperationException("Attribute scope has completed.");
        if (string.IsNullOrWhiteSpace(ownerRuntimeId)) throw new ArgumentException("Attribute owner id is required.", nameof(ownerRuntimeId));
        if (_sets.ContainsKey(ownerRuntimeId)) throw new InvalidOperationException($"Attribute owner '{ownerRuntimeId}' is already registered.");
        var set = new BattleAttributeSet(ScopeId, ownerRuntimeId, definition, _instanceId);
        _sets.Add(ownerRuntimeId, set);
        return set;
    }

    internal ScopeStateCheckpoint CaptureState()
    {
        if (Transition is not null)
            throw new InvalidOperationException("Cannot checkpoint a completed Attribute scope.");
        return new ScopeStateCheckpoint(this);
    }

    internal void RestoreState(ScopeStateCheckpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        checkpoint.Restore(this);
    }

    public AttributeScopeTransitionResult Complete(AttributeScopeCompletionReason reason, int finalTick)
    {
        if (Transition is not null) return Transition;
        if (reason == AttributeScopeCompletionReason.None) throw new ArgumentOutOfRangeException(nameof(reason));
        if (finalTick < 0) throw new ArgumentOutOfRangeException(nameof(finalTick));
        _lastTick = Math.Max(_lastTick, finalTick);
        Exception? failure = null;
        foreach (var set in _sets.Values.OrderBy(set => set.OwnerRuntimeId, StringComparer.Ordinal))
            try { set.Complete(); }
            catch (Exception exception) { failure ??= exception; }
        _sets.Clear();
        Transition = new AttributeScopeTransitionResult(
            ScopeId,
            failure is null ? reason : AttributeScopeCompletionReason.Exception,
            _lastTick,
            LiveSetCount,
            ModifierCount);
        if (failure is not null) throw failure;
        return Transition;
    }

    public void Dispose()
    {
        if (Transition is null) Complete(AttributeScopeCompletionReason.Disposal, _lastTick);
    }

    internal sealed class ScopeStateCheckpoint
    {
        private readonly BattleAttributeScope _owner;
        private readonly Dictionary<string, BattleAttributeSet> _sets;
        private readonly Dictionary<BattleAttributeSet, BattleAttributeSet.ModifierStateCheckpoint> _states;
        private readonly int _lastTick;

        internal ScopeStateCheckpoint(BattleAttributeScope owner)
        {
            _owner = owner;
            _sets = owner._sets.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            _states = owner._sets.Values.ToDictionary(set => set, set => set.CaptureModifierState());
            _lastTick = owner._lastTick;
        }

        internal void Restore(BattleAttributeScope owner)
        {
            if (!ReferenceEquals(owner, _owner))
                throw new InvalidOperationException("Attribute scope checkpoint belongs to another scope.");
            if (owner.Transition is not null)
                throw new InvalidOperationException("Cannot restore a completed Attribute scope.");
            owner._sets.Clear();
            foreach (var pair in _sets) owner._sets.Add(pair.Key, pair.Value);
            foreach (var pair in _states) pair.Key.RestoreModifierState(pair.Value);
            owner._lastTick = _lastTick;
        }
    }
}
