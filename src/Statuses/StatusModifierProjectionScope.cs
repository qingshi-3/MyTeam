using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;

namespace TowerAutobattler.Statuses;

internal sealed class StatusModifierProjectionScope(
    Func<string, BattleAttributeSet?> _attributeResolver,
    Func<StatusMagnitudeContextRequest, BattleAttributeMagnitudeContext> _magnitudeContextFactory)
{
    private readonly Dictionary<ProjectionKey, ModifierProjection> _modifierProjections = [];
    public int Count => _modifierProjections.Count;
    internal sealed class Checkpoint
    {
        private readonly StatusModifierProjectionScope _owner;
        private readonly Dictionary<ProjectionKey, ModifierProjection> _state;
        internal Checkpoint(StatusModifierProjectionScope owner)
        { _owner = owner; _state = new(owner._modifierProjections); }
        internal void Restore(StatusModifierProjectionScope owner)
        {
            if (!ReferenceEquals(owner, _owner)) throw new InvalidOperationException("Modifier checkpoint belongs to another Status scope.");
            owner._modifierProjections.Clear();
            foreach (var pair in _state) owner._modifierProjections.Add(pair.Key, pair.Value);
        }
    }
    public Checkpoint CaptureIndex() => new(this);
    // AttributeSet checkpoints are restored by the containing world transaction.
    public void RestoreIndex(Checkpoint checkpoint) => checkpoint.Restore(this);
    public ImmutableArray<StatusModifierProjectionSnapshot> Snapshot(IReadOnlySet<string> instanceIds)
    {
        return _modifierProjections.Where(pair => instanceIds.Contains(pair.Key.InstanceId))
            .OrderBy(pair => pair.Key.StackApplicationSequence)
            .ThenBy(pair => pair.Key.ModifierIndex)
            .Select(pair => new StatusModifierProjectionSnapshot(
                pair.Key.InstanceId,
                pair.Key.SourceId,
                pair.Key.StackApplicationSequence,
                pair.Key.ModifierIndex,
                pair.Value.Handle))
            .ToImmutableArray();
    }
    internal readonly record struct ProjectionKey(
        string InstanceId,
        string SourceId,
        long StackApplicationSequence,
        int ModifierIndex);
    internal sealed record DesiredProjection(
        BattleAttributeSet Attributes,
        CompiledAttributeModifier Modifier,
        CombatSourceRef Source,
        BattleAttributeMagnitudeContext Context);
    internal sealed record ModifierProjection(BattleAttributeSet Attributes, AttributeModifierHandle Handle);
    public ProjectionTransaction Synchronize(IEnumerable<StatusRuntimeInstance> instances, IReadOnlySet<string> instanceIds)
    {
        if (instanceIds.Count == 0) return ProjectionTransaction.Empty;
        var desired = new Dictionary<ProjectionKey, DesiredProjection>();
        foreach (var instance in instances.Where(item => instanceIds.Contains(item.InstanceId))
                     .OrderBy(item => item.ApplicationSequence))
        {
            if (instance.Definition.AttributeModifiers.IsEmpty) continue;
            var target = _attributeResolver(instance.OwnerId) ?? throw new InvalidOperationException(
                $"Status '{instance.Definition.StableId}' cannot resolve owner AttributeSet '{instance.OwnerId}'.");
            foreach (var contribution in instance.Contributions.OrderBy(item => item.ApplicationSequence))
            foreach (var stack in contribution.Stacks.OrderBy(item => item.ApplicationSequence))
            {
                var sourceAttributes = _attributeResolver(contribution.SourceId);
                if (sourceAttributes is null && instance.Definition.AttributeModifiers.Any(modifier =>
                    AttributeMagnitudeSupport.Leaves(modifier.Magnitude).Any(item => item is CompiledSourceAttributeMagnitude)))
                    throw new InvalidOperationException(
                        $"Status '{instance.Definition.StableId}' requires source AttributeSet '{contribution.SourceId}'.");
                sourceAttributes ??= target;
                var context = CreateMagnitudeContext(
                    instance,
                    contribution.SourceId,
                    stack.ApplicationSequence,
                    stack.AppliedTick,
                    sourceAttributes,
                    target);
                var source = CombatSourceRef.Status(
                    instance.Definition.StableId,
                    instance.OwnerId,
                    $"{instance.InstanceId}:{stack.ApplicationSequence}");
                for (var modifierIndex = 0; modifierIndex < instance.Definition.AttributeModifiers.Length; modifierIndex++)
                {
                    var modifier = instance.Definition.AttributeModifiers[modifierIndex];
                    if (stack.CapturedMagnitudes.TryGetValue(modifierIndex, out var captured))
                        modifier = modifier with { Magnitude = new CompiledConstantMagnitude(captured) };
                    var key = new ProjectionKey(
                        instance.InstanceId,
                        contribution.SourceId,
                        stack.ApplicationSequence,
                        modifierIndex);
                    desired.Add(key, new DesiredProjection(target, modifier, source, context));
                }
            }
        }

        var previous = _modifierProjections.ToDictionary(pair => pair.Key, pair => pair.Value);
        var touched = _modifierProjections.Where(pair => instanceIds.Contains(pair.Key.InstanceId))
            .Select(pair => pair.Value.Attributes)
            .Concat(desired.Values.Select(item => item.Attributes))
            .Distinct()
            .ToDictionary(attributes => attributes, attributes => attributes.CaptureModifierState());
        var transaction = new ProjectionTransaction(this, previous, touched);
        try
        {
            foreach (var key in _modifierProjections.Keys.Where(key =>
                         instanceIds.Contains(key.InstanceId) && !desired.ContainsKey(key))
                     .OrderBy(key => key.StackApplicationSequence)
                     .ThenBy(key => key.ModifierIndex)
                     .ToArray())
            {
                var projection = _modifierProjections[key];
                projection.Attributes.Remove(projection.Handle);
                _modifierProjections.Remove(key);
            }
            foreach (var pair in desired.Where(pair => !_modifierProjections.ContainsKey(pair.Key))
                         .OrderBy(pair => pair.Key.StackApplicationSequence)
                         .ThenBy(pair => pair.Key.ModifierIndex))
            {
                var desiredProjection = pair.Value;
                var handle = desiredProjection.Attributes.ApplyModifier(
                    desiredProjection.Modifier,
                    desiredProjection.Source,
                    desiredProjection.Context);
                _modifierProjections.Add(pair.Key, new ModifierProjection(desiredProjection.Attributes, handle));
            }
            return transaction;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void Clear()
    {
        foreach (var projection in _modifierProjections.Values)
            projection.Attributes.Remove(projection.Handle);
        _modifierProjections.Clear();
    }

    public BattleAttributeMagnitudeContext CreateMagnitudeContext(
        StatusRuntimeInstance instance,
        string sourceId,
        long stackApplicationSequence,
        int stackAppliedTick,
        BattleAttributeSet sourceAttributes,
        BattleAttributeSet targetAttributes)
    {
        var context = _magnitudeContextFactory(new StatusMagnitudeContextRequest(
            instance.Definition,
            sourceId,
            instance.OwnerId,
            stackApplicationSequence,
            stackAppliedTick,
            sourceAttributes,
            targetAttributes));
        return context ?? throw new InvalidOperationException(
            $"Status '{instance.Definition.StableId}' magnitude context factory returned null.");
    }

    internal sealed class ProjectionTransaction
    {
        public static ProjectionTransaction Empty { get; } = new();

        private readonly StatusModifierProjectionScope? _scope;
        private readonly Dictionary<ProjectionKey, ModifierProjection>? _previous;
        private readonly Dictionary<BattleAttributeSet, BattleAttributeSet.ModifierStateCheckpoint>? _checkpoints;
        private bool _finished;

        private ProjectionTransaction()
        {
        }

        public ProjectionTransaction(
            StatusModifierProjectionScope scope,
            Dictionary<ProjectionKey, ModifierProjection> previous,
            Dictionary<BattleAttributeSet, BattleAttributeSet.ModifierStateCheckpoint> checkpoints)
        {
            _scope = scope;
            _previous = previous;
            _checkpoints = checkpoints;
        }

        public void Commit() => _finished = true;

        public void Rollback()
        {
            if (_finished || _scope is null || _previous is null || _checkpoints is null) return;
            foreach (var pair in _checkpoints) pair.Key.RestoreModifierState(pair.Value);
            _scope._modifierProjections.Clear();
            foreach (var pair in _previous) _scope._modifierProjections.Add(pair.Key, pair.Value);
            _finished = true;
        }
    }

}
