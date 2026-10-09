using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Equipment;

namespace TowerAutobattler.Statuses;

public sealed record StatusControlGuardTriggered(string OwnerId, string SourceId, string RuleId, int Tick);

public sealed class BattleStatusScope : IDisposable
{
    private readonly string _scopeId;
    private readonly Action<string, ImmutableArray<StatusRuntimeSnapshot>> _ownerChanged;
    private readonly Func<string, BattleAttributeSet?> _attributeResolver;
    private readonly Func<StatusEffectInvocation, bool> _effectSink;
    private readonly Action<StatusLifecycleEvent> _lifecycleSink;
    private readonly Action<StatusPresentationCue> _cueSink;
    private readonly Func<StatusMagnitudeContextRequest, BattleAttributeMagnitudeContext> _magnitudeContextFactory;
    private readonly Dictionary<StatusKey, StatusRuntimeInstance> _instances = [];
    private readonly StatusModifierProjectionScope _projections;
    private readonly StatusReactiveSubscriptionScope _subscriptions;
    private readonly StatusPeriodicReceiptLedger _periodicReceipts = new();
    private readonly bool _deferredPeriodicEffects;
    private StatusScopeTransitionResult? _transition;
    private long _applicationSequence;
    private int _lastTick;
    private bool _insideMutation;
    private bool _worldTransactionActive;
    private readonly Func<string, CompiledEquipmentControlGuard?> _controlGuardResolver;
    private readonly Action<StatusControlGuardTriggered> _controlGuardSink;
    private ImmutableDictionary<(string Owner, string Rule), int> _controlGuardWindows =
        ImmutableDictionary<(string Owner, string Rule), int>.Empty;

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
        _deferredPeriodicEffects = false;
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
        Func<StatusEffectInvocation, bool>? reactiveEffectSink = null,
        Func<string, CompiledEquipmentControlGuard?>? controlGuardResolver = null,
        Action<StatusControlGuardTriggered>? controlGuardSink = null)
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
        _projections = new StatusModifierProjectionScope(_attributeResolver, _magnitudeContextFactory);
        _subscriptions = new StatusReactiveSubscriptionScope(() => _instances.Values, () => _transition is not null,
            combatReactiveRegistrar, reactiveEffectSink ?? _effectSink);
        _deferredPeriodicEffects = true;
        _controlGuardResolver = controlGuardResolver ?? (_ => null);
        _controlGuardSink = controlGuardSink ?? (_ => { });
    }

    public string ScopeId => _scopeId;
    public bool IsCompleted => _transition is not null;
    public int LiveInstanceCount => _instances.Count;
    public int LiveModifierHandleCount => _projections.Count;
    public int ContributionCount => _instances.Values.Sum(instance => instance.Contributions.Count);
    public int LiveReactiveSubscriptionCount => _subscriptions.Count;
    public StatusScopeTransitionResult? Transition => _transition;

    public ImmutableArray<CompiledStatusDefinition> ActiveDefinitions(string ownerId) => _instances.Values
        .Where(instance => instance.OwnerId == ownerId).OrderBy(instance => instance.InstanceId, StringComparer.Ordinal)
        .Select(instance => instance.Definition).ToImmutableArray();

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
            if (request.GrantId is null || (request.GrantId.Length > 0 && string.IsNullOrWhiteSpace(request.GrantId)))
                throw new ArgumentException("Grant id must be empty or nonblank.", nameof(requests));
            if (!string.IsNullOrWhiteSpace(request.GrantId) && !StatusGrantCompiler.IsSupported(request.Definition))
                throw new ArgumentException("Unsupported passive Status grant.", nameof(requests));
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
                    0,
                    request.GrantId));
            }
            return results.ToImmutable();
        });
    }

    public void ReplaceGrants(IEnumerable<string> revokeGrantIds, IEnumerable<StatusApplicationRequest> grants)
    {
        ArgumentNullException.ThrowIfNull(revokeGrantIds);
        ArgumentNullException.ThrowIfNull(grants);
        EnsureNotMutating();
        var remove = revokeGrantIds.ToHashSet(StringComparer.Ordinal);
        if (remove.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Only nonblank grant ids can be revoked.", nameof(revokeGrantIds));
        var requests = grants.ToArray();
        foreach (var request in requests)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Definition);
            EnsureAttribution(request.SourceId, request.OwnerId);
            if (string.IsNullOrWhiteSpace(request.GrantId) || request.Tick < 0 || !StatusGrantCompiler.IsSupported(request.Definition))
                throw new ArgumentException("Invalid passive Status grant.", nameof(grants));
        }
        if (remove.Count == 0 && requests.Length == 0) return;
        if (_transition is not null)
        {
            if (requests.Length > 0) throw new InvalidOperationException("Status scope has completed.");
            return;
        }
        ExecuteTransactional(batch =>
        {
            foreach (var pair in _instances.Where(pair => remove.Contains(pair.Value.GrantId)).ToArray())
            {
                batch.Owners.Add(pair.Value.OwnerId);
                RemoveInstance(pair.Key, pair.Value, StatusRemovalReason.GrantRevoked, _lastTick, Snapshot(pair.Value), batch);
            }
            foreach (var request in requests)
            {
                _lastTick = Math.Max(_lastTick, request.Tick);
                ApplyCore(request.Definition, request.SourceId, request.OwnerId, request.Tick, batch, 0, request.GrantId);
            }
            return true;
        });
    }

    public bool RevokeGrant(string grantId)
    {
        if (string.IsNullOrWhiteSpace(grantId)) throw new ArgumentException("Grant id is required.", nameof(grantId));
        var exists = _instances.Values.Any(instance => instance.GrantId == grantId);
        ReplaceGrants([grantId], []);
        return exists;
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
                       instance.NextPeriodTick <= tick &&
                       (instance.Definition.DurationKind != StatusDurationKind.TimedTicks ||
                        instance.NextPeriodTick <= instance.ExpirationTick))
                {
                    var dueTick = instance.NextPeriodTick;
                    instance.NextPeriodTick = checked(instance.NextPeriodTick + instance.Definition.PeriodicIntervalTicks);
                    var periodicValue = instance.Stacks * instance.Definition.Magnitude;
                    var receiptId = $"{_scopeId}:{instance.InstanceId}:period:{dueTick}";
                    _periodicReceipts.Schedule(receiptId, instance.InstanceId,
                        instance.OwnerId, instance.Definition.StableId, dueTick, periodicValue);
                    batch.Effects.Add(new StatusEffectInvocation(
                        StatusEffectInvocationKind.Periodic,
                        instance.Definition,
                        PrimarySource(instance),
                        instance.OwnerId,
                        instance.InstanceId,
                        instance.OwnerId,
                        dueTick,
                        instance.Definition.PeriodicEffect,
                        StatusRemovalReason.None, PeriodicValue: periodicValue, PeriodicReceiptId: receiptId));
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
                instance.RemainingTicks = Math.Max(0, instance.ExpirationTick - tick);
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

    // Consumable definitions are compiled as one periodic owner-damage operation whose
    // InvocationValue is stacks * Magnitude. NextPeriodTick excludes scheduled receipts;
    // the receipt ledger retains their captured values until actual execution or cash-out.
    public float RemainingPeriodicDamage(string ownerId, string stableId, int tick)
    {
        ValidatePeriodicSelection(ownerId, stableId, tick);
        if (_transition is not null) return 0;
        double value = _periodicReceipts.PendingValue(ownerId, stableId);
        foreach (var pair in Owned(ownerId).Where(pair => CanConsumePeriodic(pair.Value, stableId)))
        {
            var instance = pair.Value;
            // A due but not-yet-advanced owner still owes its current scheduled tick.
            // Do not replace NextPeriodTick with the caller's tick or shift expiration.
            var count = instance.NextPeriodTick > instance.ExpirationTick ? 0L :
                1L + ((long)instance.ExpirationTick - instance.NextPeriodTick) / instance.Definition.PeriodicIntervalTicks;
            value += count * (double)instance.Stacks * instance.Definition.Magnitude;
        }
        if (!double.IsFinite(value) || value > float.MaxValue || value < 0)
            throw new InvalidOperationException("Remaining periodic damage is not a finite nonnegative value.");
        return (float)value;
    }

    public float ConsumePeriodicDamage(string ownerId, string stableId, int tick)
    {
        ValidatePeriodicSelection(ownerId, stableId, tick);
        EnsureNotMutating();
        if (_transition is not null) return 0;
        return ExecuteTransactional(batch =>
        {
            _lastTick = Math.Max(_lastTick, tick);
            var value = RemainingPeriodicDamage(ownerId,stableId,tick);
            _periodicReceipts.Consume(ownerId, stableId);
            foreach (var pair in Owned(ownerId).Where(pair => CanConsumePeriodic(pair.Value, stableId)))
                RemoveInstance(pair.Key,pair.Value,StatusRemovalReason.OverflowConsumed,tick,Snapshot(pair.Value),batch);
            return value;
        });
    }

    public bool TryClaimPeriodicInvocation(StatusEffectInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        if (invocation.Kind != StatusEffectInvocationKind.Periodic || string.IsNullOrWhiteSpace(invocation.PeriodicReceiptId))
            return true; // Compatibility invocations predate the authoritative receipt ledger.
        return _transition is null && _periodicReceipts.TryClaim(invocation);
    }

    private static bool CanConsumePeriodic(StatusRuntimeInstance instance, string stableId) =>
        instance.Definition.StableId == stableId && instance.Definition.PeriodicEffect is not null &&
        instance.Definition.DurationKind == StatusDurationKind.TimedTicks && instance.Definition.PeriodicIntervalTicks > 0;

    private static void ValidatePeriodicSelection(string ownerId, string stableId, int tick)
    {
        if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Status owner id is required.", nameof(ownerId));
        if (string.IsNullOrWhiteSpace(stableId)) throw new ArgumentException("Status stable id is required.", nameof(stableId));
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
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

    // Precise removal for a transferred/consumed independently attributed layer. Other
    // sources and timers remain untouched; the world transaction still owns rollback.
    internal bool MatrixRemoveInstance(string instanceId, int tick)
    {
        EnsureNotMutating();
        if (_transition is not null) return false;
        return ExecuteTransactional(batch =>
        {
            var found = _instances.FirstOrDefault(pair => pair.Value.InstanceId == instanceId);
            if (found.Value is null) return false;
            _lastTick = Math.Max(_lastTick, tick);
            batch.Owners.Add(found.Value.OwnerId);
            RemoveInstance(found.Key, found.Value, StatusRemovalReason.OverflowConsumed, tick,
                Snapshot(found.Value), batch);
            return true;
        });
    }

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
        return _projections.Snapshot(instanceIds);
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
            _projections.Clear();
            var failure = _subscriptions.Clear();
            _instances.Clear();
            _periodicReceipts.Clear();
            _controlGuardWindows = _controlGuardWindows.Clear();

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
        int depth,
        string grantId = "")
    {
        if (depth > 64) throw new InvalidOperationException("Status transition depth exceeded the compiled acyclic limit.");
        var applicationSequence = ++_applicationSequence;
        var duration = StatusControlDuration.ResolveTicks(definition, _attributeResolver(ownerId));
        // Reject the first hard-control contribution before tags, modifiers or control
        // facts exist. A successful interception must not fail the source's ability.
        if (string.IsNullOrEmpty(grantId) && definition.Behavior == StatusBehaviorKind.DisableActions &&
            definition.DurationKind == StatusDurationKind.TimedTicks && duration > 0 &&
            _controlGuardResolver(ownerId) is { } guard)
        {
            var guardKey = (ownerId, guard.RuleId);
            if (!_controlGuardWindows.TryGetValue(guardKey, out var until))
            {
                _controlGuardWindows = _controlGuardWindows.Add(guardKey, checked(tick + guard.WindowTicks));
                batch.ControlGuards.Add(new(ownerId, sourceId, guard.RuleId, tick));
                return new StatusApplicationResult(true, string.Empty, null, 0);
            }
            if (tick < until)
                duration = Math.Max(1, (int)Math.Ceiling(duration * (double)guard.SubsequentDurationMultiplier));
        }
        if (definition.DurationKind == StatusDurationKind.Instant)
        {
            var instant = new StatusRuntimeInstance(definition, ownerId, applicationSequence, tick, duration);
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

        var key = string.IsNullOrWhiteSpace(grantId)
            ? Key(definition, sourceId, ownerId, applicationSequence)
            : new StatusKey(definition.StableId, ownerId, "grant:" + grantId);
        var created = !_instances.TryGetValue(key, out var instance);
        if (!created && !string.IsNullOrWhiteSpace(grantId))
        {
            if (PrimarySource(instance!) != sourceId || instance!.Definition != definition)
                throw new InvalidOperationException("A grant cannot change source or definition without revocation.");
            return new StatusApplicationResult(true, string.Empty, Snapshot(instance!), 0);
        }
        if (created)
        {
            instance = new StatusRuntimeInstance(definition, ownerId, applicationSequence, tick, duration) { GrantId = grantId };
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
            RefreshDuration(instance, duration, definition.DurationRefreshPolicy, tick);
            if (!canAdd && definition.OverflowPolicy == StatusOverflowPolicy.RefreshDuration &&
                definition.DurationKind == StatusDurationKind.TimedTicks)
            {
                instance.ExpirationTick = checked(tick + duration);
                instance.RemainingTicks = duration;
            }
            if (definition.PeriodicResetPolicy == StatusPeriodicResetPolicy.ResetOnApplication &&
                definition.PeriodicIntervalTicks > 0)
                instance.NextPeriodTick = checked(tick + definition.PeriodicIntervalTicks);
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
        StatusRuntimeInstance instance,
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
        StatusRuntimeInstance instance,
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
        StatusModifierProjectionScope.ProjectionTransaction? projectionTransaction = null;
        StatusReactiveSubscriptionScope.ReactiveSubscriptionTransaction? reactiveTransaction = null;
        try
        {
            var result = mutation(batch);
            projectionTransaction = _projections.Synchronize(_instances.Values, batch.ModifierInstances);
            reactiveTransaction = _subscriptions.Synchronize(batch.ModifierInstances, _worldTransactionActive);
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
        {
            if (!_effectSink(effect))
                throw new InvalidOperationException($"Status effect '{effect.Binding.StableId}' failed to schedule.");
            if (!_deferredPeriodicEffects && !string.IsNullOrWhiteSpace(effect.PeriodicReceiptId))
                _periodicReceipts.Remove(effect.PeriodicReceiptId);
        }
        foreach (var owner in batch.Owners.OrderBy(id => id, StringComparer.Ordinal))
            _ownerChanged(owner, SnapshotOwner(owner));
        foreach (var lifecycle in batch.Lifecycles) _lifecycleSink(lifecycle);
        foreach (var cue in batch.Cues) _cueSink(cue);
        foreach (var guard in batch.ControlGuards) _controlGuardSink(guard);
    }

    private ScopeState CaptureState() => new(
        _instances.ToDictionary(pair => pair.Key, pair => pair.Value.Clone()),
        _periodicReceipts.Capture(),
        _applicationSequence,
        _lastTick,
        _controlGuardWindows);

    private void RestoreState(ScopeState backup)
    {
        _instances.Clear();
        foreach (var pair in backup.Instances) _instances.Add(pair.Key, pair.Value.Clone());
        _periodicReceipts.Restore(backup.PeriodicReceipts);
        _applicationSequence = backup.ApplicationSequence;
        _lastTick = backup.LastTick;
        _controlGuardWindows = backup.ControlGuardWindows;
    }

    private void CommitWorldTransaction(WorldStateCheckpoint checkpoint)
    {
        if (!checkpoint.BelongsTo(this) || !_worldTransactionActive)
            throw new InvalidOperationException("Status world transaction is not active for this scope.");
        try
        {
            _subscriptions.CommitWorld();
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
            checkpoint.RestoreCapturedState();
        }
        finally
        {
            _worldTransactionActive = false;
        }
    }

    private KeyValuePair<StatusKey, StatusRuntimeInstance>[] Owned(string ownerId) => _instances
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

    private StatusSourceContribution CreateContribution(
        StatusRuntimeInstance instance,
        string sourceId,
        long applicationSequence,
        int tick)
    {
        var contribution = new StatusSourceContribution(sourceId);
        contribution.Stacks.Add(CaptureStack(instance, sourceId, applicationSequence, tick));
        return contribution;
    }

    private StatusSourceStack CaptureStack(
        StatusRuntimeInstance instance,
        string sourceId,
        long applicationSequence,
        int tick)
    {
        var stack = new StatusSourceStack(applicationSequence, tick);
        if (!instance.Definition.AttributeModifiers.Any(modifier =>
                modifier.Magnitude.CaptureMode == AttributeCaptureMode.Snapshot))
            return stack;
        var target = _attributeResolver(instance.OwnerId) ?? throw new InvalidOperationException(
            $"Status '{instance.Definition.StableId}' cannot resolve owner AttributeSet '{instance.OwnerId}'.");
        var sourceAttributes = _attributeResolver(sourceId);
        if (sourceAttributes is null && instance.Definition.AttributeModifiers.Any(modifier =>
                modifier.Magnitude.CaptureMode == AttributeCaptureMode.Snapshot &&
                AttributeMagnitudeSupport.Leaves(modifier.Magnitude).Any(item => item is CompiledSourceAttributeMagnitude)))
            throw new InvalidOperationException(
                $"Status '{instance.Definition.StableId}' requires source AttributeSet '{sourceId}'.");
        sourceAttributes ??= target;
        var context = _projections.CreateMagnitudeContext(
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

    private static void RefreshDuration(
        StatusRuntimeInstance instance,
        int duration,
        StatusDurationRefreshPolicy policy,
        int tick)
    {
        if (instance.Definition.DurationKind != StatusDurationKind.TimedTicks) return;
        var refreshedExpiration = checked(tick + duration);
        instance.ExpirationTick = policy switch
        {
            StatusDurationRefreshPolicy.None => instance.ExpirationTick,
            StatusDurationRefreshPolicy.Reset => refreshedExpiration,
            StatusDurationRefreshPolicy.KeepLonger => Math.Max(instance.ExpirationTick, refreshedExpiration),
            StatusDurationRefreshPolicy.Extend => checked(Math.Max(tick, instance.ExpirationTick) + duration),
            _ => throw new InvalidOperationException("Unsupported Status duration refresh policy.")
        };
        instance.RemainingTicks = Math.Max(0, instance.ExpirationTick - tick);
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
        StatusRuntimeInstance instance,
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
            instance.Definition.Presentation?.ReportLabel ?? string.Empty,
            instance.Definition.Presentation?.PersistentVfx ?? string.Empty,
            instance.ExpirationTick,
            instance.NextPeriodTick);
    }

    private static string PrimarySource(StatusRuntimeInstance instance) => instance.Contributions
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
    private sealed record ScopeState(
        Dictionary<StatusKey, StatusRuntimeInstance> Instances,
        StatusPeriodicReceiptLedger.Checkpoint PeriodicReceipts,
        long ApplicationSequence,
        int LastTick,
        ImmutableDictionary<(string Owner, string Rule), int> ControlGuardWindows);

    internal sealed class WorldStateCheckpoint
    {
        private bool _finished;

        internal WorldStateCheckpoint(BattleStatusScope owner)
        {
            Owner = owner;
            State = owner.CaptureState();
            ModifierProjections = owner._projections.CaptureIndex();
            ReactiveSubscriptions = owner._subscriptions.Capture();
        }

        private BattleStatusScope Owner { get; }
        private ScopeState State { get; }
        private StatusModifierProjectionScope.Checkpoint ModifierProjections { get; }
        private StatusReactiveSubscriptionScope.Checkpoint ReactiveSubscriptions { get; }

        internal bool BelongsTo(BattleStatusScope owner) => ReferenceEquals(Owner, owner);

        internal void RestoreCapturedState()
        {
            Owner._subscriptions.Rollback(ReactiveSubscriptions);
            Owner.RestoreState(State);
            Owner._projections.RestoreIndex(ModifierProjections);
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

    private sealed class MutationBatch
    {
        public HashSet<string> Owners { get; } = new(StringComparer.Ordinal);
        public HashSet<string> ModifierInstances { get; } = new(StringComparer.Ordinal);
        public List<StatusEffectInvocation> Effects { get; } = [];
        public List<StatusLifecycleEvent> Lifecycles { get; } = [];
        public List<StatusPresentationCue> Cues { get; } = [];
        public List<StatusControlGuardTriggered> ControlGuards { get; } = [];
    }

}
