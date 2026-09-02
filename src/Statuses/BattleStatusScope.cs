using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;

namespace TowerAutobattler.Statuses;

public sealed class BattleStatusScope : IDisposable
{
    private readonly string _scopeId;
    private readonly Action<string, ImmutableArray<StatusRuntimeSnapshot>> _ownerChanged;
    private readonly Func<string, BattleAttributeSet?> _attributeResolver;
    private readonly Func<StatusEffectInvocation, bool> _effectSink;
    private readonly Action<StatusLifecycleEvent> _lifecycleSink;
    private readonly Action<StatusPresentationCue> _cueSink;
    private readonly Func<StatusMagnitudeContextRequest, BattleAttributeMagnitudeContext> _magnitudeContextFactory;
    private readonly Func<StatusCombatReactiveSubscriptionRequest, IDisposable>? _combatReactiveRegistrar;
    private readonly Func<StatusEffectInvocation, bool> _reactiveEffectSink;
    private readonly Dictionary<StatusKey, RuntimeInstance> _instances = [];
    private readonly Dictionary<ProjectionKey, ModifierProjection> _modifierProjections = [];
    private readonly Dictionary<ReactiveSubscriptionKey, IDisposable> _reactiveSubscriptions = [];
    private StatusScopeTransitionResult? _transition;
    private long _applicationSequence;
    private int _lastTick;
    private bool _insideMutation;
    private bool _worldTransactionActive;

    public BattleStatusScope(
        string scopeId,
        Action<string, ImmutableArray<StatusRuntimeSnapshot>> ownerChanged,
        Func<StatusPeriodicInvocation, bool>? periodicSink = null,
        Action<StatusLifecycleEvent>? lifecycleSink = null)
        : this(
            scopeId,
            ownerChanged,
            _ => null,
            invocation => invocation.Kind != StatusEffectInvocationKind.Periodic ||
                          (periodicSink ?? (_ => true))(new StatusPeriodicInvocation(
                              invocation.Definition,
                              invocation.SourceId,
                              invocation.OwnerId,
                              invocation.Tick,
                              invocation.Binding)),
            lifecycleSink,
            null,
            null,
            null,
            null)
    {
    }

    public BattleStatusScope(
        string scopeId,
        Action<string, ImmutableArray<StatusRuntimeSnapshot>> ownerChanged,
        Func<string, BattleAttributeSet?> attributeResolver,
        Func<StatusEffectInvocation, bool>? effectSink,
        Action<StatusLifecycleEvent>? lifecycleSink,
        Action<StatusPresentationCue>? cueSink,
        Func<StatusMagnitudeContextRequest, BattleAttributeMagnitudeContext>? magnitudeContextFactory = null,
        Func<StatusCombatReactiveSubscriptionRequest, IDisposable>? combatReactiveRegistrar = null,
        Func<StatusEffectInvocation, bool>? reactiveEffectSink = null)
    {
        if (string.IsNullOrWhiteSpace(scopeId))
            throw new ArgumentException("Status scope id is required.", nameof(scopeId));
        _scopeId = scopeId;
        _ownerChanged = ownerChanged ?? throw new ArgumentNullException(nameof(ownerChanged));
        _attributeResolver = attributeResolver ?? throw new ArgumentNullException(nameof(attributeResolver));
        _effectSink = effectSink ?? (_ => true);
        _lifecycleSink = lifecycleSink ?? (_ => { });
        _cueSink = cueSink ?? (_ => { });
        _magnitudeContextFactory = magnitudeContextFactory ?? (request =>
            new BattleAttributeMagnitudeContext(request.SourceAttributes, request.TargetAttributes));
        _combatReactiveRegistrar = combatReactiveRegistrar;
        _reactiveEffectSink = reactiveEffectSink ?? _effectSink;
    }

    public string ScopeId => _scopeId;
    public bool IsCompleted => _transition is not null;
    public int LiveInstanceCount => _instances.Count;
    public int LiveModifierHandleCount => _modifierProjections.Count;
    public int ContributionCount => _instances.Values.Sum(instance => instance.Contributions.Count);
    public int LiveReactiveSubscriptionCount => _reactiveSubscriptions.Count;
    public StatusScopeTransitionResult? Transition => _transition;

    internal WorldStateCheckpoint BeginWorldTransaction()
    {
        EnsureNotMutating();
        if (_transition is not null)
            throw new InvalidOperationException("Cannot checkpoint a completed Status scope.");
        if (_worldTransactionActive)
            throw new InvalidOperationException("A Status world transaction is already active.");
        var checkpoint = new WorldStateCheckpoint(this);
        _worldTransactionActive = true;
        return checkpoint;
    }

    public StatusApplicationResult Apply(
        CompiledStatusDefinition definition,
        string sourceId,
        string ownerId,
        int tick)
    {
        ArgumentNullException.ThrowIfNull(definition);
        EnsureNotMutating();
        EnsureAttribution(sourceId, ownerId);
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        if (_transition is not null)
            return new StatusApplicationResult(false, "状态作用域已经结束。", null, 0);

        return ExecuteTransactional(batch =>
        {
            _lastTick = Math.Max(_lastTick, tick);
            return ApplyCore(definition, sourceId, ownerId, tick, batch, 0);
        });
    }

    public ImmutableArray<StatusApplicationResult> ApplyBatch(
        IEnumerable<StatusApplicationRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        EnsureNotMutating();
        var ordered = requests.ToArray();
        foreach (var request in ordered)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Definition);
            EnsureAttribution(request.SourceId, request.OwnerId);
            if (request.Tick < 0) throw new ArgumentOutOfRangeException(nameof(requests));
        }
        if (_transition is not null)
            return ordered.Select(_ => new StatusApplicationResult(
                false, "状态作用域已经结束。", null, 0)).ToImmutableArray();

        return ExecuteTransactional(batch =>
        {
            var results = ImmutableArray.CreateBuilder<StatusApplicationResult>(ordered.Length);
            foreach (var request in ordered)
            {
                _lastTick = Math.Max(_lastTick, request.Tick);
                results.Add(ApplyCore(
                    request.Definition,
                    request.SourceId,
                    request.OwnerId,
                    request.Tick,
                    batch,
                    0));
            }
            return results.ToImmutable();
        });
    }

    public bool HasTag(string ownerId, string tag)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("Status owner id is required.", nameof(ownerId));
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Status tag is required.", nameof(tag));
        return _transition is null && _instances.Values.Any(instance =>
            instance.OwnerId == ownerId && instance.Definition.GrantedTags.Contains(tag, StringComparer.Ordinal));
    }

    public StatusAdvanceResult AdvanceOwner(string ownerId, int tick)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("Status owner id is required.", nameof(ownerId));
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        EnsureNotMutating();
        if (_transition is not null) return new StatusAdvanceResult(false, [], 0, []);

        return ExecuteTransactional(batch =>
        {
            _lastTick = Math.Max(_lastTick, tick);
            var owned = Owned(ownerId);
            var tags = owned.SelectMany(pair => pair.Value.Definition.GrantedTags)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(tag => tag, StringComparer.Ordinal)
                .ToImmutableArray();
            var periodicCount = 0;
            foreach (var pair in owned)
            {
                var instance = pair.Value;
                while (instance.Definition.PeriodicEffect is not null &&
                       instance.Definition.PeriodicIntervalTicks > 0 &&
                       instance.NextPeriodTick > instance.AppliedTick &&
                       instance.NextPeriodTick <= tick)
                {
                    var dueTick = instance.NextPeriodTick;
                    instance.NextPeriodTick += instance.Definition.PeriodicIntervalTicks;
                    batch.Effects.Add(new StatusEffectInvocation(
                        StatusEffectInvocationKind.Periodic,
                        instance.Definition,
                        PrimarySource(instance),
                        instance.OwnerId,
                        instance.InstanceId,
                        instance.OwnerId,
                        dueTick,
                        instance.Definition.PeriodicEffect,
                        StatusRemovalReason.None));
                    AddCue(batch, instance.Definition, StatusPresentationCueLifecycle.WhileActive, Snapshot(instance),
                        StatusRemovalReason.None, dueTick);
                    periodicCount++;
                }
            }

            var expired = ImmutableArray.CreateBuilder<StatusRuntimeSnapshot>();
            foreach (var pair in owned)
            {
                var instance = pair.Value;
                if (instance.Definition.DurationKind != StatusDurationKind.TimedTicks) continue;
                if (instance.RemainingTicks > 0) instance.RemainingTicks--;
                if (instance.RemainingTicks > 0) continue;
                var snapshot = Snapshot(instance);
                expired.Add(snapshot);
                RemoveInstance(pair.Key, instance, StatusRemovalReason.Expired, tick, snapshot, batch);
            }
            batch.Owners.Add(ownerId);
            return new StatusAdvanceResult(
                tags.Contains(StatusDefinitionCompiler.ActionDisabledTag, StringComparer.Ordinal),
                expired.ToImmutable(),
                periodicCount,
                tags);
        });
    }

    public bool Dispel(
        string ownerId,
        string stableId,
        StatusDispelStrength strength = StatusDispelStrength.Ordinary,
        string? sourceId = null)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("Status owner id is required.", nameof(ownerId));
        if (string.IsNullOrWhiteSpace(stableId))
            throw new ArgumentException("Status stable id is required.", nameof(stableId));
        if (!Enum.IsDefined(strength)) throw new ArgumentOutOfRangeException(nameof(strength));
        EnsureNotMutating();
        if (_transition is not null) return false;

        return ExecuteTransactional(batch =>
        {
            var removalReason = strength == StatusDispelStrength.Strong
                ? StatusRemovalReason.StrongDispelled
                : StatusRemovalReason.OrdinaryDispelled;
            var changed = false;
            foreach (var pair in Owned(ownerId).Where(pair => pair.Value.Definition.StableId == stableId))
            {
                var instance = pair.Value;
                if (!CanDispel(instance.Definition.DispelCategory, strength)) continue;
                if (sourceId is not null && instance.Definition.AggregationPolicy == StatusAggregationPolicy.ByTarget)
                {
                    var contribution = instance.Contributions.FirstOrDefault(item => item.SourceId == sourceId);
                    if (contribution is null) continue;
                    var previous = instance.Stacks;
                    var removalSnapshot = Snapshot(instance, sourceId);
                    instance.Contributions.Remove(contribution);
                    instance.Stacks -= contribution.Stacks.Count;
                    changed = true;
                    batch.ModifierInstances.Add(instance.InstanceId);
                    batch.Owners.Add(ownerId);
                    if (instance.Stacks <= 0)
                    {
                        RemoveInstance(pair.Key, instance, removalReason, _lastTick, removalSnapshot, batch);
                    }
                    else
                    {
                        var snapshot = Snapshot(instance, sourceId);
                        AddLifecycle(batch, instance.Definition, StatusLifecycleKind.StackChanged, snapshot,
                            previous, instance.Stacks, StatusRemovalReason.None, _lastTick);
                    }
                    continue;
                }
                if (sourceId is not null && instance.Contributions.All(item => item.SourceId != sourceId)) continue;
                changed = true;
                RemoveInstance(pair.Key, instance, removalReason, _lastTick, Snapshot(instance, sourceId), batch);
            }
            return changed;
        });
    }

    public bool Dispel(string ownerId, string stableId, string? sourceId) =>
        Dispel(ownerId, stableId, StatusDispelStrength.Ordinary, sourceId);

    public int DispelOwner(
        string ownerId,
        StatusDispelStrength strength,
        StatusDisposition? disposition = null)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("Status owner id is required.", nameof(ownerId));
        if (!Enum.IsDefined(strength)) throw new ArgumentOutOfRangeException(nameof(strength));
        if (disposition is not null && !Enum.IsDefined(disposition.Value))
            throw new ArgumentOutOfRangeException(nameof(disposition));
        EnsureNotMutating();
        if (_transition is not null) return 0;

        return ExecuteTransactional(batch =>
        {
            var removalReason = strength == StatusDispelStrength.Strong
                ? StatusRemovalReason.StrongDispelled
                : StatusRemovalReason.OrdinaryDispelled;
            var removed = 0;
            foreach (var pair in Owned(ownerId))
            {
                var instance = pair.Value;
                if (disposition is not null && instance.Definition.Disposition != disposition.Value) continue;
                if (!CanDispel(instance.Definition.DispelCategory, strength)) continue;
                RemoveInstance(pair.Key, instance, removalReason, _lastTick, Snapshot(instance), batch);
                removed++;
            }
            return removed;
        });
    }

    public void HandleOwnerDeath(string ownerId) => RemoveOwnerCore(ownerId, deathOnly: true);

    public void RemoveOwner(string ownerId) => RemoveOwnerCore(ownerId, deathOnly: false);

    public ImmutableArray<StatusRuntimeSnapshot> SnapshotOwner(string ownerId) => _instances.Values
        .Where(instance => instance.OwnerId == ownerId)
        .OrderBy(instance => instance.Definition.StableId, StringComparer.Ordinal)
        .ThenBy(instance => instance.ApplicationSequence)
        .Select(instance => Snapshot(instance))
        .ToImmutableArray();

    public ImmutableArray<StatusModifierProjectionSnapshot> SnapshotModifierProjections(string ownerId)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("Status owner id is required.", nameof(ownerId));
        var instanceIds = _instances.Values.Where(instance => instance.OwnerId == ownerId)
            .Select(instance => instance.InstanceId).ToHashSet(StringComparer.Ordinal);
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

    public StatusScopeTransitionResult Complete(StatusScopeCompletionReason reason, int finalTick)
    {
        EnsureNotMutating();
        if (_transition is not null) return _transition;
        if (reason == StatusScopeCompletionReason.None) throw new ArgumentOutOfRangeException(nameof(reason));
        if (finalTick < 0) throw new ArgumentOutOfRangeException(nameof(finalTick));
        _lastTick = Math.Max(_lastTick, finalTick);

        _insideMutation = true;
        try
        {
            var batch = new MutationBatch();
            foreach (var pair in _instances.OrderBy(pair => pair.Value.OwnerId, StringComparer.Ordinal)
                         .ThenBy(pair => pair.Value.Definition.StableId, StringComparer.Ordinal)
                         .ThenBy(pair => pair.Value.ApplicationSequence))
                RemoveInstance(pair.Key, pair.Value, StatusRemovalReason.ScopeCompleted, _lastTick,
                    Snapshot(pair.Value), batch, removeFromDictionary: false, scheduleLifecycleEffects: false);
            var owners = _instances.Values.Select(instance => instance.OwnerId)
                .Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            RemoveAllModifiers();
            var failure = DisposeAllReactiveSubscriptions();
            _instances.Clear();
            _modifierProjections.Clear();

            foreach (var effect in batch.Effects)
                try
                {
                    if (!_effectSink(effect))
                        throw new InvalidOperationException($"Status effect '{effect.Binding.StableId}' failed to schedule.");
                }
                catch (Exception exception) { failure ??= exception; }
            foreach (var owner in owners)
                try { _ownerChanged(owner, []); }
                catch (Exception exception) { failure ??= exception; }
            foreach (var lifecycle in batch.Lifecycles)
                try { _lifecycleSink(lifecycle); }
                catch (Exception exception) { failure ??= exception; }
            foreach (var cue in batch.Cues)
                try { _cueSink(cue); }
                catch (Exception exception) { failure ??= exception; }
            _transition = new StatusScopeTransitionResult(
                _scopeId,
                failure is null ? reason : StatusScopeCompletionReason.Exception,
                _lastTick,
                LiveInstanceCount,
                LiveModifierHandleCount,
                ContributionCount,
                LiveReactiveSubscriptionCount);
            if (failure is not null) throw failure;
            return _transition;
        }
        finally
        {
            _insideMutation = false;
        }
    }

    public void Dispose()
    {
        if (_transition is null) Complete(StatusScopeCompletionReason.Disposal, _lastTick);
    }

    private StatusApplicationResult ApplyCore(
        CompiledStatusDefinition definition,
        string sourceId,
        string ownerId,
        int tick,
        MutationBatch batch,
        int depth)
    {
        if (depth > 64) throw new InvalidOperationException("Status transition depth exceeded the compiled acyclic limit.");
        var applicationSequence = ++_applicationSequence;
        var duration = StatusControlDuration.ResolveTicks(definition, _attributeResolver(ownerId));
        if (definition.DurationKind == StatusDurationKind.Instant)
        {
            var instant = new RuntimeInstance(definition, ownerId, applicationSequence, tick, duration);
            instant.Contributions.Add(CreateContribution(instant, sourceId, applicationSequence, tick));
            var snapshot = Snapshot(instant, sourceId);
            AddLifecycle(batch, definition, StatusLifecycleKind.Applied, snapshot, 0, 1,
                StatusRemovalReason.None, tick);
            AddCue(batch, definition, StatusPresentationCueLifecycle.Executed, snapshot, StatusRemovalReason.None, tick);
            AddLifecycle(batch, definition, StatusLifecycleKind.Removed, snapshot, 1, 0,
                StatusRemovalReason.InstantExecuted, tick);
            AddCue(batch, definition, StatusPresentationCueLifecycle.Removed, snapshot,
                StatusRemovalReason.InstantExecuted, tick);
            return new StatusApplicationResult(true, string.Empty, snapshot, 1);
        }

        var key = Key(definition, sourceId, ownerId, applicationSequence);
        var created = !_instances.TryGetValue(key, out var instance);
        if (created)
        {
            instance = new RuntimeInstance(definition, ownerId, applicationSequence, tick, duration);
            instance.Contributions.Add(CreateContribution(instance, sourceId, applicationSequence, tick));
            _instances.Add(key, instance);
        }
        else
        {
            instance!.LastAppliedTick = tick;
        }

        var previousStacks = created ? 0 : instance!.Stacks;
        var addedStacks = created ? 1 : 0;
        if (!created)
        {
            var canAdd = definition.StackLimit == 0 || instance!.Stacks < definition.StackLimit;
            if (canAdd)
            {
                instance.Stacks++;
                addedStacks = 1;
                var contribution = instance.Contributions.FirstOrDefault(item => item.SourceId == sourceId);
                if (contribution is null)
                    instance.Contributions.Add(CreateContribution(instance, sourceId, applicationSequence, tick));
                else
                    contribution.Stacks.Add(CaptureStack(instance, sourceId, applicationSequence, tick));
            }
            RefreshDuration(instance, duration, definition.DurationRefreshPolicy);
            if (!canAdd && definition.OverflowPolicy == StatusOverflowPolicy.RefreshDuration)
                instance.RemainingTicks = duration;
            if (definition.PeriodicResetPolicy == StatusPeriodicResetPolicy.ResetOnApplication &&
                definition.PeriodicIntervalTicks > 0)
                instance.NextPeriodTick = tick + definition.PeriodicIntervalTicks;
        }

        batch.Owners.Add(ownerId);
        if (created || addedStacks > 0) batch.ModifierInstances.Add(instance!.InstanceId);
        var appliedSnapshot = Snapshot(instance!, sourceId);
        AddLifecycle(batch, definition, StatusLifecycleKind.Applied, appliedSnapshot,
            previousStacks, instance!.Stacks, StatusRemovalReason.None, tick);
        if (!created && previousStacks != instance.Stacks)
            AddLifecycle(batch, definition, StatusLifecycleKind.StackChanged, appliedSnapshot,
                previousStacks, instance.Stacks, StatusRemovalReason.None, tick);
        AddCue(batch, definition, StatusPresentationCueLifecycle.Executed, appliedSnapshot, StatusRemovalReason.None, tick);
        if (created)
            AddCue(batch, definition, StatusPresentationCueLifecycle.OnActive, appliedSnapshot, StatusRemovalReason.None, tick);

        var overflowed = addedStacks > 0 && definition.StackLimit > 0 &&
                         instance.Stacks >= definition.StackLimit &&
                         definition.OverflowPolicy == StatusOverflowPolicy.ApplyStatusAndConsumeAtLimit &&
                         definition.OverflowTransition is not null;
        if (overflowed)
        {
            ConsumeOverflow(key, instance, sourceId, tick, definition.OverflowTransition!, batch);
            ApplyCore(definition.OverflowTransition!.Target, sourceId, ownerId, tick, batch, depth + 1);
        }
        return new StatusApplicationResult(true, string.Empty, appliedSnapshot, addedStacks, overflowed);
    }

    private void ConsumeOverflow(
        StatusKey key,
        RuntimeInstance instance,
        string sourceId,
        int tick,
        CompiledStatusTransition transition,
        MutationBatch batch)
    {
        var previous = instance.Stacks;
        var removalSnapshot = Snapshot(instance, sourceId);
        var consume = transition.ConsumeStacks == 0 ? previous : Math.Min(previous, transition.ConsumeStacks);
        foreach (var contribution in instance.Contributions.OrderBy(item => item.ApplicationSequence).ToArray())
        {
            if (consume == 0) break;
            var taken = Math.Min(consume, contribution.Stacks.Count);
            contribution.Stacks.RemoveRange(0, taken);
            instance.Stacks -= taken;
            consume -= taken;
            if (contribution.Stacks.Count == 0) instance.Contributions.Remove(contribution);
        }
        batch.ModifierInstances.Add(instance.InstanceId);
        batch.Owners.Add(instance.OwnerId);
        if (instance.Stacks == 0)
            RemoveInstance(key, instance, StatusRemovalReason.OverflowConsumed, tick,
                removalSnapshot, batch);
        else
            AddLifecycle(batch, instance.Definition, StatusLifecycleKind.StackChanged, Snapshot(instance, sourceId),
                previous, instance.Stacks, StatusRemovalReason.None, tick);
    }

    private void RemoveOwnerCore(string ownerId, bool deathOnly)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("Status owner id is required.", nameof(ownerId));
        if (_transition is not null) return;
        ExecuteTransactional(batch =>
        {
            var reason = deathOnly ? StatusRemovalReason.OwnerDied : StatusRemovalReason.OwnerRemoved;
            foreach (var pair in Owned(ownerId))
            {
                if (deathOnly && pair.Value.Definition.DeathPolicy == StatusDeathPolicy.Persist) continue;
                RemoveInstance(pair.Key, pair.Value, reason, _lastTick, Snapshot(pair.Value), batch);
            }
            batch.Owners.Add(ownerId);
            return true;
        });
    }

    private void RemoveInstance(
        StatusKey key,
        RuntimeInstance instance,
        StatusRemovalReason reason,
        int tick,
        StatusRuntimeSnapshot snapshot,
        MutationBatch batch,
        bool removeFromDictionary = true,
        bool scheduleLifecycleEffects = true)
    {
        if (removeFromDictionary) _instances.Remove(key);
        batch.ModifierInstances.Add(instance.InstanceId);
        batch.Owners.Add(instance.OwnerId);
        AddLifecycle(batch, instance.Definition, StatusLifecycleKind.Removed, snapshot, snapshot.Stacks, 0, reason, tick,
            scheduleLifecycleEffects);
        AddCue(batch, instance.Definition, StatusPresentationCueLifecycle.Removed, snapshot, reason, tick);
    }

    private T ExecuteTransactional<T>(Func<MutationBatch, T> mutation)
    {
        EnsureNotMutating();
        _insideMutation = true;
        var backup = CaptureState();
        var batch = new MutationBatch();
        ProjectionTransaction? projectionTransaction = null;
        ReactiveSubscriptionTransaction? reactiveTransaction = null;
        try
        {
            var result = mutation(batch);
            projectionTransaction = SynchronizeModifiers(batch.ModifierInstances);
            reactiveTransaction = SynchronizeReactiveSubscriptions(batch.ModifierInstances);
            Flush(batch);
            reactiveTransaction.Commit();
            projectionTransaction.Commit();
            return result;
        }
        catch
        {
            reactiveTransaction?.Rollback();
            projectionTransaction?.Rollback();
            RestoreState(backup);
            foreach (var owner in batch.Owners.OrderBy(id => id, StringComparer.Ordinal))
                try { _ownerChanged(owner, SnapshotOwner(owner)); }
                catch { }
            throw;
        }
        finally
        {
            _insideMutation = false;
        }
    }

    private void Flush(MutationBatch batch)
    {
        foreach (var effect in batch.Effects)
            if (!_effectSink(effect))
                throw new InvalidOperationException($"Status effect '{effect.Binding.StableId}' failed to schedule.");
        foreach (var owner in batch.Owners.OrderBy(id => id, StringComparer.Ordinal))
            _ownerChanged(owner, SnapshotOwner(owner));
        foreach (var lifecycle in batch.Lifecycles) _lifecycleSink(lifecycle);
        foreach (var cue in batch.Cues) _cueSink(cue);
    }

    private ScopeState CaptureState() => new(
        _instances.ToDictionary(pair => pair.Key, pair => pair.Value.Clone()),
        _applicationSequence,
        _lastTick);

    private void RestoreState(ScopeState backup)
    {
        _instances.Clear();
        foreach (var pair in backup.Instances) _instances.Add(pair.Key, pair.Value.Clone());
        _applicationSequence = backup.ApplicationSequence;
        _lastTick = backup.LastTick;
    }

    private void CommitWorldTransaction(WorldStateCheckpoint checkpoint)
    {
        if (!checkpoint.BelongsTo(this) || !_worldTransactionActive)
            throw new InvalidOperationException("Status world transaction is not active for this scope.");
        try
        {
            var desired = _instances.Values.SelectMany(instance =>
                    Enumerable.Range(0, instance.Definition.CombatReactiveBindings.Length)
                        .Select(index => new ReactiveSubscriptionKey(instance.InstanceId, index)))
                .ToHashSet();
            foreach (var key in _reactiveSubscriptions.Keys.Where(key => !desired.Contains(key))
                         .OrderBy(key => key.InstanceId, StringComparer.Ordinal)
                         .ThenBy(key => key.BindingIndex)
                         .ToArray())
            {
                if (!_reactiveSubscriptions.Remove(key, out var handle)) continue;
                try { handle.Dispose(); }
                catch { }
            }
        }
        finally
        {
            _worldTransactionActive = false;
        }
    }

    private void RestoreWorldTransaction(WorldStateCheckpoint checkpoint)
    {
        if (!checkpoint.BelongsTo(this) || !_worldTransactionActive)
            throw new InvalidOperationException("Status world transaction is not active for this scope.");
        try
        {
            foreach (var key in _reactiveSubscriptions.Keys.Where(key =>
                         !checkpoint.ContainsReactiveSubscription(key.InstanceId, key.BindingIndex))
                         .OrderBy(key => key.InstanceId, StringComparer.Ordinal)
                         .ThenBy(key => key.BindingIndex)
                         .Reverse()
                         .ToArray())
            {
                if (!_reactiveSubscriptions.Remove(key, out var handle)) continue;
                try { handle.Dispose(); }
                catch { }
            }
            checkpoint.RestoreCapturedState();
        }
        finally
        {
            _worldTransactionActive = false;
        }
    }

    private ProjectionTransaction SynchronizeModifiers(IReadOnlySet<string> instanceIds)
    {
        if (instanceIds.Count == 0) return ProjectionTransaction.Empty;
        var desired = new Dictionary<ProjectionKey, DesiredProjection>();
        foreach (var instance in _instances.Values.Where(item => instanceIds.Contains(item.InstanceId))
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
                        modifier.Magnitude is CompiledSourceAttributeMagnitude))
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

    private void RemoveModifierProjections(IEnumerable<string> instanceIds)
    {
        var ids = instanceIds.ToHashSet(StringComparer.Ordinal);
        foreach (var key in _modifierProjections.Keys.Where(key => ids.Contains(key.InstanceId)).ToArray())
        {
            var projection = _modifierProjections[key];
            projection.Attributes.Remove(projection.Handle);
            _modifierProjections.Remove(key);
        }
    }

    private void RemoveAllModifiers()
    {
        foreach (var projection in _modifierProjections.Values)
            projection.Attributes.Remove(projection.Handle);
        _modifierProjections.Clear();
    }

    private ReactiveSubscriptionTransaction SynchronizeReactiveSubscriptions(IReadOnlySet<string> instanceIds)
    {
        if (instanceIds.Count == 0) return ReactiveSubscriptionTransaction.Empty;
        var desired = new Dictionary<ReactiveSubscriptionKey, CompiledStatusCombatReactiveBinding>();
        foreach (var instance in _instances.Values.Where(item => instanceIds.Contains(item.InstanceId))
                     .OrderBy(item => item.ApplicationSequence))
        for (var bindingIndex = 0; bindingIndex < instance.Definition.CombatReactiveBindings.Length; bindingIndex++)
            desired.Add(
                new ReactiveSubscriptionKey(instance.InstanceId, bindingIndex),
                instance.Definition.CombatReactiveBindings[bindingIndex]);

        if (desired.Count > 0 && _combatReactiveRegistrar is null)
            throw new InvalidOperationException("Status combat reactive bindings require a Battle-local registrar.");
        var obsolete = _reactiveSubscriptions.Keys.Where(key =>
                instanceIds.Contains(key.InstanceId) && !desired.ContainsKey(key))
            .OrderBy(key => key.InstanceId, StringComparer.Ordinal)
            .ThenBy(key => key.BindingIndex)
            .ToArray();
        var added = new List<ReactiveSubscriptionKey>();
        var transaction = new ReactiveSubscriptionTransaction(this, added, obsolete);
        try
        {
            foreach (var pair in desired.Where(pair => !_reactiveSubscriptions.ContainsKey(pair.Key))
                         .OrderBy(pair => pair.Key.InstanceId, StringComparer.Ordinal)
                         .ThenBy(pair => pair.Key.BindingIndex))
            {
                var key = pair.Key;
                var binding = pair.Value;
                var instance = _instances.Values.Single(item => item.InstanceId == key.InstanceId);
                var subscriptionSource = CombatSourceRef.Status(
                    instance.Definition.StableId,
                    instance.OwnerId,
                    $"{instance.InstanceId}:reactive:{key.BindingIndex}");
                var handle = _combatReactiveRegistrar!(new StatusCombatReactiveSubscriptionRequest(
                    binding.EventKind,
                    subscriptionSource,
                    binding.Priority,
                    (combatEvent, reactionSink) =>
                        HandleReactiveEvent(key, binding, combatEvent, reactionSink)));
                if (handle is null)
                    throw new InvalidOperationException(
                        $"Status '{instance.Definition.StableId}' reactive registrar returned null.");
                _reactiveSubscriptions.Add(key, handle);
                added.Add(key);
            }
            return transaction;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private void HandleReactiveEvent(
        ReactiveSubscriptionKey key,
        CompiledStatusCombatReactiveBinding binding,
        BattleCombatEvent combatEvent,
        BattleCombatReactionSink reactionSink)
    {
        if (_transition is not null) return;
        var instance = _instances.Values.FirstOrDefault(item => item.InstanceId == key.InstanceId);
        if (instance is null) return;
        var matches = binding.OwnerRole switch
        {
            StatusReactiveOwnerRole.OwnerIsSource => combatEvent.SourceRuntimeId == instance.OwnerId,
            StatusReactiveOwnerRole.OwnerIsTarget => combatEvent.TargetRuntimeId == instance.OwnerId,
            _ => false
        };
        if (!matches) return;
        var sourceId = binding.EffectSourcePolicy switch
        {
            StatusReactiveEffectSourcePolicy.PrimaryContribution => PrimarySource(instance),
            _ => string.Empty
        };
        if (string.IsNullOrWhiteSpace(sourceId)) return;
        var explicitTargetId = binding.OwnerRole switch
        {
            StatusReactiveOwnerRole.OwnerIsSource => combatEvent.TargetRuntimeId,
            StatusReactiveOwnerRole.OwnerIsTarget => combatEvent.SourceRuntimeId,
            _ => string.Empty
        };
        var invocation = new StatusEffectInvocation(
            StatusEffectInvocationKind.Reactive,
            instance.Definition,
            sourceId,
            instance.OwnerId,
            instance.InstanceId,
            explicitTargetId,
            combatEvent.Tick,
            binding.Binding,
            StatusRemovalReason.None,
            combatEvent);
        var reactionSource = CombatSourceRef.Status(
            instance.Definition.StableId,
            instance.OwnerId,
            $"{instance.InstanceId}:reactive:{key.BindingIndex}");
        if (!reactionSink.Enqueue(reactionSource, binding.Priority, _ =>
            {
                if (!_reactiveEffectSink(invocation))
                    throw new InvalidOperationException(
                        $"Status reactive effect '{binding.Binding.StableId}' failed to execute.");
            }))
            throw new InvalidOperationException(
                $"Status reactive effect '{binding.Binding.StableId}' failed to enqueue.");
    }

    private Exception? DisposeAllReactiveSubscriptions()
    {
        Exception? failure = null;
        // Unsubscription is irreversible cleanup. Finish every detach, clear Status ownership,
        // and report the first failure only after no live handle remains; ordinary state rollback
        // must never recreate a subscription whose external detach already ran.
        foreach (var handle in _reactiveSubscriptions.OrderBy(pair => pair.Key.InstanceId, StringComparer.Ordinal)
                     .ThenBy(pair => pair.Key.BindingIndex).Select(pair => pair.Value).ToArray())
            try { handle.Dispose(); }
            catch (Exception exception) { failure ??= exception; }
        _reactiveSubscriptions.Clear();
        return failure;
    }

    private KeyValuePair<StatusKey, RuntimeInstance>[] Owned(string ownerId) => _instances
        .Where(pair => pair.Value.OwnerId == ownerId)
        .OrderBy(pair => pair.Value.Definition.StableId, StringComparer.Ordinal)
        .ThenBy(pair => pair.Value.ApplicationSequence)
        .ToArray();

    private static StatusKey Key(
        CompiledStatusDefinition definition,
        string sourceId,
        string ownerId,
        long applicationSequence) => definition.AggregationPolicy switch
    {
        StatusAggregationPolicy.BySource => new StatusKey(definition.StableId, ownerId, $"source:{sourceId}"),
        StatusAggregationPolicy.ByTarget => new StatusKey(definition.StableId, ownerId, "target"),
        StatusAggregationPolicy.Independent => new StatusKey(definition.StableId, ownerId, $"instance:{applicationSequence}"),
        _ => throw new InvalidOperationException("Unsupported Status aggregation policy.")
    };

    private SourceContribution CreateContribution(
        RuntimeInstance instance,
        string sourceId,
        long applicationSequence,
        int tick)
    {
        var contribution = new SourceContribution(sourceId);
        contribution.Stacks.Add(CaptureStack(instance, sourceId, applicationSequence, tick));
        return contribution;
    }

    private SourceStack CaptureStack(
        RuntimeInstance instance,
        string sourceId,
        long applicationSequence,
        int tick)
    {
        var stack = new SourceStack(applicationSequence, tick);
        if (!instance.Definition.AttributeModifiers.Any(modifier =>
                modifier.Magnitude.CaptureMode == AttributeCaptureMode.Snapshot))
            return stack;
        var target = _attributeResolver(instance.OwnerId) ?? throw new InvalidOperationException(
            $"Status '{instance.Definition.StableId}' cannot resolve owner AttributeSet '{instance.OwnerId}'.");
        var sourceAttributes = _attributeResolver(sourceId);
        if (sourceAttributes is null && instance.Definition.AttributeModifiers.Any(modifier =>
                modifier.Magnitude.CaptureMode == AttributeCaptureMode.Snapshot &&
                modifier.Magnitude is CompiledSourceAttributeMagnitude))
            throw new InvalidOperationException(
                $"Status '{instance.Definition.StableId}' requires source AttributeSet '{sourceId}'.");
        sourceAttributes ??= target;
        var context = CreateMagnitudeContext(
            instance,
            sourceId,
            applicationSequence,
            tick,
            sourceAttributes,
            target);
        for (var modifierIndex = 0; modifierIndex < instance.Definition.AttributeModifiers.Length; modifierIndex++)
        {
            var modifier = instance.Definition.AttributeModifiers[modifierIndex];
            if (modifier.Magnitude.CaptureMode != AttributeCaptureMode.Snapshot) continue;
            stack.CapturedMagnitudes.Add(modifierIndex, target.EvaluateMagnitude(modifier.Magnitude, context));
        }
        return stack;
    }

    private BattleAttributeMagnitudeContext CreateMagnitudeContext(
        RuntimeInstance instance,
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

    private static void RefreshDuration(
        RuntimeInstance instance,
        int duration,
        StatusDurationRefreshPolicy policy)
    {
        if (instance.Definition.DurationKind != StatusDurationKind.TimedTicks) return;
        instance.RemainingTicks = policy switch
        {
            StatusDurationRefreshPolicy.None => instance.RemainingTicks,
            StatusDurationRefreshPolicy.Reset => duration,
            StatusDurationRefreshPolicy.KeepLonger => Math.Max(instance.RemainingTicks, duration),
            StatusDurationRefreshPolicy.Extend => checked(instance.RemainingTicks + duration),
            _ => throw new InvalidOperationException("Unsupported Status duration refresh policy.")
        };
    }

    private static bool CanDispel(StatusDispelCategory category, StatusDispelStrength strength) => category switch
    {
        StatusDispelCategory.Ordinary => true,
        StatusDispelCategory.StrongOnly => strength == StatusDispelStrength.Strong,
        _ => false
    };

    private void AddLifecycle(
        MutationBatch batch,
        CompiledStatusDefinition definition,
        StatusLifecycleKind kind,
        StatusRuntimeSnapshot snapshot,
        int previousStacks,
        int currentStacks,
        StatusRemovalReason reason,
        int tick,
        bool scheduleBindings = true)
    {
        batch.Lifecycles.Add(new StatusLifecycleEvent(
            kind, snapshot, previousStacks, currentStacks, reason, tick));
        var trigger = kind switch
        {
            StatusLifecycleKind.Applied => StatusLifecycleTriggerKind.Applied,
            StatusLifecycleKind.StackChanged => StatusLifecycleTriggerKind.StackChanged,
            _ => StatusLifecycleTriggerKind.Removed
        };
        if (!scheduleBindings) return;
        foreach (var binding in definition.LifecycleBindings.Where(item => item.Trigger == trigger))
            batch.Effects.Add(new StatusEffectInvocation(
                kind switch
                {
                    StatusLifecycleKind.Applied => StatusEffectInvocationKind.Applied,
                    StatusLifecycleKind.StackChanged => StatusEffectInvocationKind.StackChanged,
                    _ => StatusEffectInvocationKind.Removed
                },
                definition,
                snapshot.SourceId,
                snapshot.OwnerId,
                snapshot.InstanceId,
                snapshot.OwnerId,
                tick,
                binding.Binding,
                reason));
    }

    private void AddCue(
        MutationBatch batch,
        CompiledStatusDefinition definition,
        StatusPresentationCueLifecycle lifecycle,
        StatusRuntimeSnapshot snapshot,
        StatusRemovalReason reason,
        int tick)
    {
        var presentation = definition.Presentation;
        if (presentation is null) return;
        var cue = lifecycle switch
        {
            StatusPresentationCueLifecycle.Executed => presentation.ExecutedCue,
            StatusPresentationCueLifecycle.OnActive => presentation.OnActiveCue,
            StatusPresentationCueLifecycle.WhileActive => presentation.WhileActiveCue,
            _ => presentation.RemovedCue
        };
        batch.Cues.Add(new StatusPresentationCue(lifecycle, cue, snapshot, reason, tick));
    }

    private StatusRuntimeSnapshot Snapshot(
        RuntimeInstance instance,
        string? attributionSource = null,
        int? stacksOverride = null)
    {
        var source = attributionSource ?? PrimarySource(instance);
        return new StatusRuntimeSnapshot(
            instance.Definition.StableId,
            instance.Definition.DisplayName,
            instance.Definition.Description,
            source,
            instance.OwnerId,
            instance.InstanceId,
            instance.ApplicationSequence,
            instance.AppliedTick,
            instance.LastAppliedTick,
            stacksOverride ?? instance.Stacks,
            instance.RemainingTicks,
            instance.Definition.DurationKind == StatusDurationKind.Permanent,
            instance.Definition.DispelCategory != StatusDispelCategory.NonDispellable,
            instance.Definition.DurationKind,
            instance.Definition.AggregationPolicy,
            instance.Definition.DispelCategory,
            instance.Definition.DeathPolicy,
            instance.Definition.Behavior,
            instance.Definition.Disposition,
            instance.Definition.Magnitude,
            instance.Definition.GrantedTags,
            instance.Contributions.OrderBy(item => item.ApplicationSequence)
                .Select(item => new StatusSourceContributionSnapshot(
                    item.SourceId, item.ApplicationSequence, item.AppliedTick, item.Stacks.Count))
                .ToImmutableArray(),
            instance.Contributions.OrderBy(item => item.ApplicationSequence)
                .SelectMany(contribution => contribution.Stacks.OrderBy(stack => stack.ApplicationSequence)
                    .SelectMany(stack => stack.CapturedMagnitudes.OrderBy(pair => pair.Key)
                        .Select(pair =>
                        {
                            var modifier = instance.Definition.AttributeModifiers[pair.Key];
                            return new StatusCapturedMagnitudeSnapshot(
                                contribution.SourceId,
                                stack.ApplicationSequence,
                                stack.AppliedTick,
                                pair.Key,
                                modifier.Attribute,
                                modifier.SlotId,
                                pair.Value);
                        })))
                .ToImmutableArray(),
            instance.Definition.Presentation?.SemanticIcon ?? string.Empty,
            instance.Definition.Presentation?.ReportLabel ?? string.Empty);
    }

    private static string PrimarySource(RuntimeInstance instance) => instance.Contributions
        .OrderBy(item => item.ApplicationSequence).Select(item => item.SourceId).FirstOrDefault() ?? string.Empty;

    private static void EnsureAttribution(string sourceId, string ownerId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("Status source id is required.", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("Status owner id is required.", nameof(ownerId));
    }

    private void EnsureNotMutating()
    {
        if (_insideMutation)
            throw new InvalidOperationException("Status scope cannot synchronously re-enter an active mutation.");
    }

    private readonly record struct StatusKey(string StableId, string OwnerId, string AggregationToken);
    private readonly record struct ProjectionKey(
        string InstanceId,
        string SourceId,
        long StackApplicationSequence,
        int ModifierIndex);
    private readonly record struct ReactiveSubscriptionKey(string InstanceId, int BindingIndex);
    private sealed record DesiredProjection(
        BattleAttributeSet Attributes,
        CompiledAttributeModifier Modifier,
        CombatSourceRef Source,
        BattleAttributeMagnitudeContext Context);
    private sealed record ModifierProjection(BattleAttributeSet Attributes, AttributeModifierHandle Handle);
    private sealed record ScopeState(
        Dictionary<StatusKey, RuntimeInstance> Instances,
        long ApplicationSequence,
        int LastTick);

    internal sealed class WorldStateCheckpoint
    {
        private bool _finished;

        internal WorldStateCheckpoint(BattleStatusScope owner)
        {
            Owner = owner;
            State = owner.CaptureState();
            ModifierProjections = owner._modifierProjections.ToDictionary(pair => pair.Key, pair => pair.Value);
            ReactiveSubscriptions = owner._reactiveSubscriptions.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private BattleStatusScope Owner { get; }
        private ScopeState State { get; }
        private Dictionary<ProjectionKey, ModifierProjection> ModifierProjections { get; }
        private Dictionary<ReactiveSubscriptionKey, IDisposable> ReactiveSubscriptions { get; }

        internal bool BelongsTo(BattleStatusScope owner) => ReferenceEquals(Owner, owner);

        internal bool ContainsReactiveSubscription(string instanceId, int bindingIndex) =>
            ReactiveSubscriptions.ContainsKey(new ReactiveSubscriptionKey(instanceId, bindingIndex));

        internal void RestoreCapturedState()
        {
            Owner.RestoreState(State);
            Owner._modifierProjections.Clear();
            foreach (var pair in ModifierProjections)
                Owner._modifierProjections.Add(pair.Key, pair.Value);
            Owner._reactiveSubscriptions.Clear();
            foreach (var pair in ReactiveSubscriptions)
                Owner._reactiveSubscriptions.Add(pair.Key, pair.Value);
        }

        internal void Commit()
        {
            if (_finished) return;
            Owner.CommitWorldTransaction(this);
            _finished = true;
        }

        internal void Rollback()
        {
            if (_finished) return;
            Owner.RestoreWorldTransaction(this);
            _finished = true;
        }
    }

    private sealed class ProjectionTransaction
    {
        public static ProjectionTransaction Empty { get; } = new();

        private readonly BattleStatusScope? _scope;
        private readonly Dictionary<ProjectionKey, ModifierProjection>? _previous;
        private readonly Dictionary<BattleAttributeSet, BattleAttributeSet.ModifierStateCheckpoint>? _checkpoints;
        private bool _finished;

        private ProjectionTransaction()
        {
        }

        public ProjectionTransaction(
            BattleStatusScope scope,
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

    private sealed class ReactiveSubscriptionTransaction
    {
        public static ReactiveSubscriptionTransaction Empty { get; } = new();

        private readonly BattleStatusScope? _scope;
        private readonly IReadOnlyList<ReactiveSubscriptionKey>? _added;
        private readonly IReadOnlyList<ReactiveSubscriptionKey>? _obsolete;
        private bool _finished;

        private ReactiveSubscriptionTransaction()
        {
        }

        public ReactiveSubscriptionTransaction(
            BattleStatusScope scope,
            IReadOnlyList<ReactiveSubscriptionKey> added,
            IReadOnlyList<ReactiveSubscriptionKey> obsolete)
        {
            _scope = scope;
            _added = added;
            _obsolete = obsolete;
        }

        public void Commit()
        {
            if (_finished || _scope is null || _obsolete is null) return;
            if (_scope._worldTransactionActive)
            {
                // The Battle transaction keeps obsolete listeners attached but logically dormant:
                // their Status instance is absent, so handlers no-op. Final detach happens only
                // after the complete Ability world commit succeeds.
                _finished = true;
                return;
            }
            foreach (var key in _obsolete)
            {
                if (!_scope._reactiveSubscriptions.Remove(key, out var handle)) continue;
                // External detach cannot be rolled back. Preserve the committed removal direction
                // even when a cleanup wrapper reports after it has already unsubscribed.
                try { handle.Dispose(); }
                catch { }
            }
            _finished = true;
        }

        public void Rollback()
        {
            if (_finished || _scope is null || _added is null) return;
            foreach (var key in _added.Reverse())
            {
                if (!_scope._reactiveSubscriptions.Remove(key, out var handle)) continue;
                // Failed application owns no subscription. Cleanup exceptions cannot convert that
                // rollback into a pseudo-commit or restore an externally detached listener.
                try { handle.Dispose(); }
                catch { }
            }
            _finished = true;
        }
    }

    private sealed class MutationBatch
    {
        public HashSet<string> Owners { get; } = new(StringComparer.Ordinal);
        public HashSet<string> ModifierInstances { get; } = new(StringComparer.Ordinal);
        public List<StatusEffectInvocation> Effects { get; } = [];
        public List<StatusLifecycleEvent> Lifecycles { get; } = [];
        public List<StatusPresentationCue> Cues { get; } = [];
    }

    private sealed class RuntimeInstance(
        CompiledStatusDefinition definition,
        string ownerId,
        long applicationSequence,
        int appliedTick,
        int durationTicks)
    {
        public CompiledStatusDefinition Definition { get; } = definition;
        public string OwnerId { get; } = ownerId;
        public string InstanceId { get; } = $"status:{definition.StableId}:{ownerId}:{applicationSequence}";
        public long ApplicationSequence { get; } = applicationSequence;
        public int AppliedTick { get; } = appliedTick;
        public int LastAppliedTick { get; set; } = appliedTick;
        public int Stacks { get; set; } = 1;
        public int RemainingTicks { get; set; } = durationTicks;
        public int NextPeriodTick { get; set; } = definition.PeriodicIntervalTicks > 0
            ? appliedTick + definition.PeriodicIntervalTicks
            : 0;
        public List<SourceContribution> Contributions { get; } = [];

        public RuntimeInstance Clone()
        {
            var clone = new RuntimeInstance(Definition, OwnerId, ApplicationSequence, AppliedTick, RemainingTicks)
            {
                LastAppliedTick = LastAppliedTick,
                Stacks = Stacks,
                RemainingTicks = RemainingTicks,
                NextPeriodTick = NextPeriodTick
            };
            clone.Contributions.AddRange(Contributions.Select(item => item.Clone()));
            return clone;
        }
    }

    private sealed class SourceContribution(string sourceId)
    {
        public string SourceId { get; } = sourceId;
        public List<SourceStack> Stacks { get; } = [];
        public long ApplicationSequence => Stacks.Count == 0 ? 0 : Stacks[0].ApplicationSequence;
        public int AppliedTick => Stacks.Count == 0 ? 0 : Stacks[0].AppliedTick;

        public SourceContribution Clone()
        {
            var clone = new SourceContribution(SourceId);
            clone.Stacks.AddRange(Stacks.Select(item => item.Clone()));
            return clone;
        }
    }

    private sealed class SourceStack(long applicationSequence, int appliedTick)
    {
        public long ApplicationSequence { get; } = applicationSequence;
        public int AppliedTick { get; } = appliedTick;
        public Dictionary<int, float> CapturedMagnitudes { get; } = [];

        public SourceStack Clone()
        {
            var clone = new SourceStack(ApplicationSequence, AppliedTick);
            foreach (var pair in CapturedMagnitudes) clone.CapturedMagnitudes.Add(pair.Key, pair.Value);
            return clone;
        }
    }
}
