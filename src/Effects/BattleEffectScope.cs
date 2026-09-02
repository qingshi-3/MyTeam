using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TowerAutobattler.Effects;

public sealed class BattleEffectScope : IDisposable
{
    private readonly string _scopeId;
    private readonly IEffectRuntimeWorld _world;
    private readonly EffectProcessorRegistry _processors;
    private readonly EffectExecutionLimits _limits;
    private readonly List<PendingInvocation> _pending = [];
    private readonly Dictionary<EffectDomainEventKind, List<ReactiveRegistration>> _subscriptions = [];
    private readonly Dictionary<long, ReactiveRegistration> _registrations = [];
    private readonly Dictionary<long, SubscriptionHandle> _subscriptionHandles = [];
    private readonly Dictionary<RuntimeKey, RuntimeState> _runtimeStates = [];
    private readonly Dictionary<RepeatedEdgeKey, int> _edgeCounts = [];
    private readonly List<EffectDomainEvent> _events = [];
    private readonly List<EffectTraceEntry> _trace = [];
    private long _invocationSequence;
    private long _chainSequence;
    private long _eventSequence;
    private long _traceSequence;
    private long _registrationSequence;
    private bool _isDraining;
    private BattleScopeTransitionResult? _transition;
    private int _lastTick;

    public BattleEffectScope(
        string scopeId,
        IEffectRuntimeWorld world,
        EffectProcessorRegistry? processors = null,
        EffectExecutionLimits? limits = null)
    {
        if (string.IsNullOrWhiteSpace(scopeId))
            throw new ArgumentException("Effect scope id is required.", nameof(scopeId));
        _scopeId = scopeId;
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _processors = processors ?? EffectProcessorRegistry.CreateDefault();
        _limits = limits ?? new EffectExecutionLimits();
        ValidateLimits(_limits);
    }

    public string ScopeId => _scopeId;
    public bool IsCompleted => _transition is not null;
    public int SubscriptionCount => _registrations.Count;
    public int PendingInvocationCount => _pending.Count;
    public int LiveRuntimeInstanceCount => _runtimeStates.Count;
    public IReadOnlyList<EffectDomainEvent> Events => _events;
    public IReadOnlyList<EffectTraceEntry> Trace => _trace;
    public BattleScopeTransitionResult? Transition => _transition;

    internal EffectStateCheckpoint CaptureState()
    {
        if (_transition is not null)
            throw new InvalidOperationException("Cannot checkpoint a completed Effect scope.");
        if (_isDraining)
            throw new InvalidOperationException("Cannot checkpoint an Effect scope during queue drain.");
        return new EffectStateCheckpoint(this);
    }

    internal void RestoreState(EffectStateCheckpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        checkpoint.Restore(this);
    }

    public IDisposable ActivateReactiveBinding(
        CompiledEffectBinding binding,
        string sourceId,
        string ownerId)
    {
        ArgumentNullException.ThrowIfNull(binding);
        EnsureAttribution(sourceId, ownerId);
        if (_transition is not null)
            throw new InvalidOperationException("Cannot activate a binding after the effect scope has completed.");
        if (binding.Trigger.Kind != EffectTriggerKind.DomainEvent ||
            binding.Trigger.EventKind == EffectDomainEventKind.None)
            throw new InvalidOperationException($"Binding '{binding.StableId}' is not reactive.");
        if (_registrations.Values.Any(registration => registration.Active &&
                registration.Binding.StableId == binding.StableId &&
                registration.SourceId == sourceId && registration.OwnerId == ownerId))
            throw new InvalidOperationException(
                $"Reactive binding '{binding.StableId}' is already active for source '{sourceId}' and owner '{ownerId}'.");

        var sequence = ++_registrationSequence;
        var registration = new ReactiveRegistration(sequence, binding, sourceId, ownerId);
        _registrations.Add(sequence, registration);
        if (!_subscriptions.TryGetValue(binding.Trigger.EventKind, out var listeners))
        {
            listeners = [];
            _subscriptions.Add(binding.Trigger.EventKind, listeners);
        }
        listeners.Add(registration);
        _runtimeStates.TryAdd(new RuntimeKey(binding.StableId, sourceId, ownerId), new RuntimeState());
        var handle = new SubscriptionHandle(this, sequence);
        _subscriptionHandles.Add(sequence, handle);
        return handle;
    }

    public EffectEnqueueResult EnqueueRoot(
        CompiledEffectBinding binding,
        string sourceId,
        string ownerId,
        string explicitTargetId,
        int tick,
        float invocationValue = 0)
    {
        ArgumentNullException.ThrowIfNull(binding);
        EnsureAttribution(sourceId, ownerId);
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        _lastTick = Math.Max(_lastTick, tick);
        if (_transition is not null)
            return Rejected(EffectInterruptionReason.ScopeCompleted, "Effect scope has completed.");
        if (binding.Trigger.Kind != EffectTriggerKind.Manual)
            return Rejected(EffectInterruptionReason.InvalidBinding, $"Binding '{binding.StableId}' is not a manual root.");
        var sequence = ++_invocationSequence;
        var context = new EffectInvocationContext(
            _scopeId,
            $"{_scopeId}_chain_{++_chainSequence}",
            sourceId,
            ownerId,
            tick,
            0,
            sequence);
        _pending.Add(new PendingInvocation(binding, context, explicitTargetId, invocationValue, 0, null, string.Empty));
        return new EffectEnqueueResult(true, sequence, EffectInterruptionReason.None, string.Empty);
    }

    public EffectQueueDrainResult ExecuteImmediate(
        CompiledEffectBinding binding,
        string sourceId,
        string ownerId,
        string explicitTargetId,
        int tick,
        float invocationValue = 0)
    {
        if (_isDraining)
            return ReentrantDrainResult(binding, sourceId, ownerId, explicitTargetId, tick);
        var enqueue = EnqueueRoot(binding, sourceId, ownerId, explicitTargetId, tick, invocationValue);
        if (!enqueue.Accepted)
            return RejectedDrainResult(binding, sourceId, ownerId, explicitTargetId, tick, enqueue.Interruption, enqueue.Message);
        return Drain();
    }

    /// <summary>
    /// Resolves conditions, targets, runtime limits, modifiers, and processor preparation without
    /// reserving runtime counters, enqueueing work, publishing events, or mutating the world.
    /// Ability transactions use this gate before any operation is committed.
    /// </summary>
    public EffectPreflightResult PreflightImmediate(
        CompiledEffectBinding binding,
        string sourceId,
        string ownerId,
        string explicitTargetId,
        int tick,
        float invocationValue = 0)
    {
        ArgumentNullException.ThrowIfNull(binding);
        EnsureAttribution(sourceId, ownerId);
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        if (_transition is not null)
            return EffectPreflightResult.Rejected(EffectInterruptionReason.ScopeCompleted, "Effect scope has completed.");
        if (_isDraining)
            return EffectPreflightResult.Rejected(EffectInterruptionReason.ReentrantExecution, "Effect scope is draining.");
        if (binding.Trigger.Kind != EffectTriggerKind.Manual)
            return EffectPreflightResult.Rejected(EffectInterruptionReason.InvalidBinding, $"Binding '{binding.StableId}' is not a manual root.");
        if (binding.Effects.Length > _limits.MaxStepsPerDrain)
            return EffectPreflightResult.Rejected(EffectInterruptionReason.StepBudget, "Effect step budget would be exhausted.");

        var context = new EffectInvocationContext(
            _scopeId,
            $"{_scopeId}_preflight",
            sourceId,
            ownerId,
            tick,
            0,
            0);
        var invocation = new PendingInvocation(
            binding,
            context,
            explicitTargetId,
            invocationValue,
            0,
            null,
            string.Empty);
        var depthReason = ValidateDepth(invocation);
        if (depthReason != EffectInterruptionReason.None)
            return EffectPreflightResult.Rejected(depthReason, "Invocation depth limit would be reached.");

        EffectWorldSnapshot snapshot;
        try { snapshot = _world.CaptureSnapshot(tick); }
        catch (Exception exception)
        {
            return EffectPreflightResult.Rejected(
                EffectInterruptionReason.ProcessorFailure,
                $"Snapshot capture failed: {exception.Message}");
        }
        if (!ConditionsPass(invocation, snapshot))
            return EffectPreflightResult.Rejected(EffectInterruptionReason.ConditionFailed, "Binding conditions were not met.");
        var targets = ResolveTargets(invocation, snapshot);
        if (targets.Length == 0)
            return EffectPreflightResult.Rejected(EffectInterruptionReason.TargetUnavailable, "Target query produced no entities.");
        var runtimeReason = RuntimeInterruption(invocation);
        if (runtimeReason != EffectInterruptionReason.None)
            return EffectPreflightResult.Rejected(
                runtimeReason,
                runtimeReason == EffectInterruptionReason.RateLimited
                    ? "Binding minimum interval has not elapsed."
                    : "Binding usage limit reached.");

        try
        {
            var preparedCount = 0;
            for (var stepIndex = 0; stepIndex < binding.Effects.Length; stepIndex++)
            foreach (var targetId in targets)
            {
                if (++preparedCount > _limits.MaxStepsPerDrain)
                    return EffectPreflightResult.Rejected(EffectInterruptionReason.StepBudget, "Effect step budget would be exhausted.");
                var step = binding.Effects[stepIndex];
                var ordering = InvocationOrdering(invocation) with { TargetId = targetId };
                _processors.Get(step.Kind).Prepare(
                    context,
                    binding.StableId,
                    stepIndex,
                    targetId,
                    ResolveAmount(step, invocation),
                    ordering,
                    snapshot,
                    _world);
            }
            return EffectPreflightResult.Success(preparedCount);
        }
        catch (Exception exception)
        {
            return EffectPreflightResult.Rejected(
                EffectInterruptionReason.ProcessorFailure,
                $"Effect preparation failed: {exception.Message}");
        }
    }

    public EffectQueueDrainResult Drain()
    {
        if (_isDraining)
            return new EffectQueueDrainResult(EffectExecutionStatus.Interrupted, EffectInterruptionReason.ReentrantExecution, []);
        if (_transition is not null)
            return new EffectQueueDrainResult(EffectExecutionStatus.Interrupted, EffectInterruptionReason.ScopeCompleted, []);

        _isDraining = true;
        var results = ImmutableArray.CreateBuilder<EffectInvocationResult>();
        var invocationsUsed = 0;
        var stepsUsed = 0;
        var eventsUsed = 0;
        try
        {
            while (_pending.Count > 0)
            {
                var tick = _pending.Min(invocation => invocation.Context.Tick);
                var wave = _pending.Where(invocation => invocation.Context.Tick == tick)
                    .OrderBy(invocation => InvocationOrdering(invocation), Comparer<EffectOrderingKey>.Default)
                    .ToArray();
                _pending.RemoveAll(invocation => invocation.Context.Tick == tick);

                EffectWorldSnapshot snapshot;
                try
                {
                    snapshot = _world.CaptureSnapshot(tick);
                }
                catch (Exception exception)
                {
                    foreach (var invocation in wave)
                        results.Add(CreateTerminalInvocation(invocation, EffectExecutionStatus.Failed,
                            EffectInterruptionReason.ProcessorFailure, $"Snapshot capture failed: {exception.Message}"));
                    AbortPendingInto(results, EffectInterruptionReason.QueueAborted, "Queue aborted after snapshot failure.");
                    break;
                }

                var work = new List<InvocationWork>(wave.Length);
                var prepared = new List<PreparedWork>();
                foreach (var invocation in wave)
                {
                    if (++invocationsUsed > _limits.MaxInvocationsPerDrain)
                    {
                        work.Add(InvocationWork.Terminal(invocation, EffectExecutionStatus.Interrupted,
                            EffectInterruptionReason.InvocationBudget, "Invocation budget exhausted."));
                        continue;
                    }
                    if (invocation.PreInterruption is { } preInterruption)
                    {
                        work.Add(InvocationWork.Terminal(invocation, EffectExecutionStatus.Interrupted,
                            preInterruption, invocation.PreInterruptionMessage));
                        continue;
                    }
                    var limitReason = ValidateDepth(invocation);
                    if (limitReason != EffectInterruptionReason.None)
                    {
                        work.Add(InvocationWork.Terminal(invocation, EffectExecutionStatus.Interrupted,
                            limitReason, "Invocation depth limit reached."));
                        continue;
                    }
                    if (!ConditionsPass(invocation, snapshot))
                    {
                        work.Add(InvocationWork.Terminal(invocation, EffectExecutionStatus.Skipped,
                            EffectInterruptionReason.ConditionFailed, "Binding conditions were not met."));
                        continue;
                    }
                    var targets = ResolveTargets(invocation, snapshot);
                    if (targets.Length == 0)
                    {
                        work.Add(InvocationWork.Terminal(invocation, EffectExecutionStatus.Skipped,
                            EffectInterruptionReason.TargetUnavailable, "Target query produced no entities."));
                        continue;
                    }
                    var runtimeReason = ReserveRuntime(invocation);
                    if (runtimeReason != EffectInterruptionReason.None)
                    {
                        work.Add(InvocationWork.Terminal(invocation, EffectExecutionStatus.Interrupted,
                            runtimeReason, runtimeReason == EffectInterruptionReason.RateLimited
                                ? "Binding minimum interval has not elapsed."
                                : "Binding usage limit reached."));
                        continue;
                    }

                    var invocationWork = new InvocationWork(invocation);
                    work.Add(invocationWork);
                    for (var stepIndex = 0; stepIndex < invocation.Binding.Effects.Length; stepIndex++)
                    foreach (var targetId in targets)
                    {
                        var step = invocation.Binding.Effects[stepIndex];
                        if (++stepsUsed > _limits.MaxStepsPerDrain)
                        {
                            invocationWork.AddStep(InterruptedStep(stepIndex, targetId, step.Kind,
                                EffectInterruptionReason.StepBudget, "Step budget exhausted."));
                            continue;
                        }
                        try
                        {
                            var amount = ResolveAmount(step, invocation);
                            var ordering = InvocationOrdering(invocation) with { TargetId = targetId };
                            var mutation = _processors.Get(step.Kind).Prepare(
                                invocation.Context,
                                invocation.Binding.StableId,
                                stepIndex,
                                targetId,
                                amount,
                                ordering,
                                snapshot,
                                _world);
                            prepared.Add(new PreparedWork(invocationWork, stepIndex, targetId, step.Kind, mutation));
                        }
                        catch (Exception exception)
                        {
                            invocationWork.AddStep(FailedStep(stepIndex, targetId, step.Kind,
                                $"Effect preparation failed: {exception.Message}"));
                        }
                    }
                }

                foreach (var preparedWork in prepared
                             .OrderBy(item => item.Mutation.Ordering, Comparer<EffectOrderingKey>.Default)
                             .ThenBy(item => item.StepIndex)
                             .ThenBy(item => item.TargetId, StringComparer.Ordinal))
                {
                    if (eventsUsed >= _limits.MaxEventsPerDrain)
                    {
                        preparedWork.Invocation.AddStep(InterruptedStep(
                            preparedWork.StepIndex,
                            preparedWork.TargetId,
                            preparedWork.Kind,
                            EffectInterruptionReason.EventBudget,
                            "Resolved-event budget exhausted."));
                        continue;
                    }
                    EffectCommitOutcome outcome;
                    try
                    {
                        outcome = _world.Commit(preparedWork.Mutation);
                    }
                    catch (Exception exception)
                    {
                        outcome = EffectCommitOutcome.Failed($"Effect commit failed: {exception.Message}");
                    }
                    outcome = ValidateCommitOutcome(preparedWork, outcome);

                    EffectDomainEvent? domainEvent = null;
                    if (outcome.Status == EffectExecutionStatus.Succeeded &&
                        outcome.EventKind != EffectDomainEventKind.None)
                    {
                        eventsUsed++;
                        domainEvent = new EffectDomainEvent(
                            ++_eventSequence,
                            outcome.EventKind,
                            preparedWork.Invocation.Pending.Context,
                            preparedWork.Invocation.Pending.Binding.StableId,
                            preparedWork.StepIndex,
                            preparedWork.TargetId,
                            outcome.AppliedAmount,
                            outcome.EffectiveAmount);
                        _events.Add(domainEvent);
                        EnqueueReactive(domainEvent);
                    }
                    preparedWork.Invocation.AddStep(new EffectStepResult(
                        preparedWork.StepIndex,
                        preparedWork.TargetId,
                        preparedWork.Kind,
                        outcome.Status,
                        outcome.Interruption,
                        outcome.AppliedAmount,
                        outcome.EffectiveAmount,
                        domainEvent,
                        outcome.Message));
                }

                foreach (var invocationWork in work)
                    results.Add(FinalizeInvocation(invocationWork));
            }
        }
        finally
        {
            _isDraining = false;
        }

        return FinalizeDrain(results.ToImmutable());
    }

    public BattleScopeTransitionResult Complete(BattleScopeCompletionReason reason, int finalTick)
    {
        if (_transition is not null) return _transition;
        if (reason == BattleScopeCompletionReason.None)
            throw new ArgumentOutOfRangeException(nameof(reason));
        if (finalTick < 0) throw new ArgumentOutOfRangeException(nameof(finalTick));
        if (_isDraining)
            throw new InvalidOperationException("Effect scope cannot complete during queue drain.");
        _lastTick = Math.Max(_lastTick, finalTick);

        var ignored = ImmutableArray.CreateBuilder<EffectInvocationResult>();
        AbortPendingInto(ignored, EffectInterruptionReason.QueueAborted, $"Scope completed: {reason}.");
        foreach (var pair in _subscriptionHandles.ToArray())
        {
            pair.Value.Invalidate(this);
            Unsubscribe(pair.Key, pair.Value);
        }
        _subscriptions.Clear();
        _registrations.Clear();
        _subscriptionHandles.Clear();
        _runtimeStates.Clear();
        _edgeCounts.Clear();
        _pending.Clear();
        _transition = new BattleScopeTransitionResult(
            _scopeId,
            reason,
            _lastTick,
            _events.ToImmutableArray(),
            _trace.ToImmutableArray(),
            SubscriptionCount,
            PendingInvocationCount,
            LiveRuntimeInstanceCount);
        return _transition;
    }

    public void Dispose()
    {
        if (_transition is null) Complete(BattleScopeCompletionReason.Disposal, _lastTick);
    }

    private EffectInterruptionReason ReserveRuntime(PendingInvocation invocation)
    {
        var key = new RuntimeKey(invocation.Binding.StableId, invocation.Context.SourceId, invocation.Context.OwnerId);
        if (!_runtimeStates.TryGetValue(key, out var state))
        {
            state = new RuntimeState();
            _runtimeStates.Add(key, state);
        }
        var interruption = RuntimeInterruption(invocation, state);
        if (interruption != EffectInterruptionReason.None) return interruption;
        state.Uses++;
        state.LastTick = invocation.Context.Tick;
        return EffectInterruptionReason.None;
    }

    private EffectInterruptionReason RuntimeInterruption(PendingInvocation invocation)
    {
        var key = new RuntimeKey(invocation.Binding.StableId, invocation.Context.SourceId, invocation.Context.OwnerId);
        return _runtimeStates.TryGetValue(key, out var state)
            ? RuntimeInterruption(invocation, state)
            : EffectInterruptionReason.None;
    }

    private static EffectInterruptionReason RuntimeInterruption(PendingInvocation invocation, RuntimeState state)
    {
        if (invocation.Binding.Limits.MaxUses > 0 && state.Uses >= invocation.Binding.Limits.MaxUses)
            return EffectInterruptionReason.UsageLimit;
        if (invocation.Binding.Limits.MinimumIntervalTicks > 0 &&
            state.LastTick is { } lastTick &&
            invocation.Context.Tick - lastTick < invocation.Binding.Limits.MinimumIntervalTicks)
            return EffectInterruptionReason.RateLimited;
        return EffectInterruptionReason.None;
    }

    private EffectInterruptionReason ValidateDepth(PendingInvocation invocation)
    {
        var maxDepth = _limits.MaxDepth;
        if (invocation.Binding.Limits.MaxDepth > 0)
            maxDepth = Math.Min(maxDepth, invocation.Binding.Limits.MaxDepth);
        return invocation.Context.Depth > maxDepth
            ? EffectInterruptionReason.DepthLimit
            : EffectInterruptionReason.None;
    }

    private bool ConditionsPass(PendingInvocation invocation, EffectWorldSnapshot snapshot)
    {
        foreach (var condition in invocation.Binding.Conditions)
        {
            if (condition is not CompiledEntityAliveCondition alive) return false;
            var id = ResolveEntityReference(invocation, alive.Entity);
            if (string.IsNullOrWhiteSpace(id) || !snapshot.Entities.TryGetValue(id, out var entity) ||
                entity.Alive != alive.ExpectedAlive)
                return false;
        }
        return true;
    }

    private static string[] ResolveTargets(PendingInvocation invocation, EffectWorldSnapshot snapshot)
    {
        IEnumerable<EffectEntitySnapshot> targets = invocation.Binding.TargetQuery switch
        {
            CompiledExplicitTargetQuery => Lookup(snapshot, invocation.ExplicitTargetId),
            CompiledSourceTargetQuery => Lookup(snapshot, invocation.Context.SourceId),
            CompiledOwnerTargetQuery => Lookup(snapshot, invocation.Context.OwnerId),
            CompiledRelativeTeamTargetQuery relative => ResolveRelative(snapshot, invocation, relative),
            _ => []
        };
        return targets.Select(entity => entity.RuntimeId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<EffectEntitySnapshot> Lookup(EffectWorldSnapshot snapshot, string id)
    {
        if (!string.IsNullOrWhiteSpace(id) && snapshot.Entities.TryGetValue(id, out var entity))
            yield return entity;
    }

    private static IEnumerable<EffectEntitySnapshot> ResolveRelative(
        EffectWorldSnapshot snapshot,
        PendingInvocation invocation,
        CompiledRelativeTeamTargetQuery relative)
    {
        var anchorId = snapshot.Entities.ContainsKey(invocation.Context.OwnerId)
            ? invocation.Context.OwnerId
            : invocation.Context.SourceId;
        if (!snapshot.Entities.TryGetValue(anchorId, out var anchor)) return [];
        var targetTeam = relative.Team == EffectRelativeTeam.Allies ? anchor.Team : 1 - anchor.Team;
        return snapshot.Entities.Values.Where(entity =>
            entity.Team == targetTeam &&
            (relative.IncludeDefeated || entity.Alive) &&
            (string.IsNullOrWhiteSpace(relative.RequiredTag) ||
             (!entity.Tags.IsDefault && entity.Tags.Contains(relative.RequiredTag, StringComparer.Ordinal))));
    }

    private static string ResolveEntityReference(PendingInvocation invocation, EffectEntityReference reference) => reference switch
    {
        EffectEntityReference.Source => invocation.Context.SourceId,
        EffectEntityReference.Owner => invocation.Context.OwnerId,
        EffectEntityReference.ExplicitTarget => invocation.ExplicitTargetId,
        _ => string.Empty
    };

    private static float ResolveAmount(CompiledEffectStep step, PendingInvocation invocation) => step.AmountSource switch
    {
        EffectAmountSource.Fixed => step.Amount,
        EffectAmountSource.InvocationValue => invocation.InvocationValue * step.Amount,
        EffectAmountSource.EventEffectiveValue => invocation.EventEffectiveValue * step.Amount,
        _ => throw new InvalidOperationException("Unsupported effect amount source.")
    };

    private void EnqueueReactive(EffectDomainEvent domainEvent)
    {
        if (!_subscriptions.TryGetValue(domainEvent.Kind, out var listeners)) return;
        foreach (var registration in listeners.Where(listener => listener.Active)
                     .OrderBy(listener => listener.Binding.Priority)
                     .ThenBy(listener => listener.SourceId, StringComparer.Ordinal)
                     .ThenBy(listener => listener.OwnerId, StringComparer.Ordinal)
                     .ThenBy(listener => listener.Binding.StableId, StringComparer.Ordinal)
                     .ThenBy(listener => listener.Sequence))
        {
            var sequence = ++_invocationSequence;
            var context = new EffectInvocationContext(
                _scopeId,
                domainEvent.Context.ChainId,
                registration.SourceId,
                registration.OwnerId,
                domainEvent.Context.Tick,
                domainEvent.Context.Depth + 1,
                sequence);
            var reason = ValidateReactiveEdge(domainEvent, registration, context);
            _pending.Add(new PendingInvocation(
                registration.Binding,
                context,
                domainEvent.TargetId,
                domainEvent.AppliedAmount,
                domainEvent.EffectiveAmount,
                reason == EffectInterruptionReason.None ? null : reason,
                reason == EffectInterruptionReason.RepeatedEdge
                    ? "Repeated reactive edge limit reached."
                    : reason == EffectInterruptionReason.DepthLimit
                        ? "Reactive depth limit reached."
                        : string.Empty));
        }
    }

    private EffectInterruptionReason ValidateReactiveEdge(
        EffectDomainEvent domainEvent,
        ReactiveRegistration registration,
        EffectInvocationContext context)
    {
        var depthLimit = _limits.MaxDepth;
        if (registration.Binding.Limits.MaxDepth > 0)
            depthLimit = Math.Min(depthLimit, registration.Binding.Limits.MaxDepth);
        if (context.Depth > depthLimit) return EffectInterruptionReason.DepthLimit;

        var key = new RepeatedEdgeKey(
            context.ChainId,
            domainEvent.BindingId,
            domainEvent.Kind,
            registration.Binding.StableId);
        var count = _edgeCounts.TryGetValue(key, out var current) ? current + 1 : 1;
        _edgeCounts[key] = count;
        var edgeLimit = _limits.MaxRepeatedEdgesPerChain;
        if (registration.Binding.Limits.MaxRepeatedEdges > 0)
            edgeLimit = Math.Min(edgeLimit, registration.Binding.Limits.MaxRepeatedEdges);
        return count > edgeLimit ? EffectInterruptionReason.RepeatedEdge : EffectInterruptionReason.None;
    }

    private EffectInvocationResult FinalizeInvocation(InvocationWork work)
    {
        if (work.TerminalStatus is { } terminalStatus)
        {
            var terminal = CreateTerminalInvocation(
                work.Pending,
                terminalStatus,
                work.TerminalInterruption,
                work.TerminalMessage);
            return terminal;
        }
        var steps = work.Steps
            .OrderBy(step => step.StepIndex)
            .ThenBy(step => step.TargetId, StringComparer.Ordinal)
            .ToImmutableArray();
        var (status, interruption, message) = SummarizeSteps(steps);
        var result = new EffectInvocationResult(
            work.Pending.Context.InvocationSequence,
            work.Pending.Context,
            work.Pending.Binding.StableId,
            status,
            interruption,
            steps,
            message);
        foreach (var step in steps) AddTrace(work.Pending, step);
        return result;
    }

    private EffectInvocationResult CreateTerminalInvocation(
        PendingInvocation pending,
        EffectExecutionStatus status,
        EffectInterruptionReason interruption,
        string message)
    {
        var result = new EffectInvocationResult(
            pending.Context.InvocationSequence,
            pending.Context,
            pending.Binding.StableId,
            status,
            interruption,
            [],
            message);
        AddTrace(pending, -1, string.Empty, null, status, interruption, 0, 0, message);
        return result;
    }

    private static (EffectExecutionStatus Status, EffectInterruptionReason Interruption, string Message) SummarizeSteps(
        ImmutableArray<EffectStepResult> steps)
    {
        if (steps.Length == 0)
            return (EffectExecutionStatus.Skipped, EffectInterruptionReason.TargetUnavailable, "No effect steps were prepared.");
        var failed = steps.FirstOrDefault(step => step.Status == EffectExecutionStatus.Failed);
        if (failed is not null) return (EffectExecutionStatus.Failed, failed.Interruption, failed.Message);
        var interrupted = steps.FirstOrDefault(step => step.Status == EffectExecutionStatus.Interrupted);
        if (interrupted is not null) return (EffectExecutionStatus.Interrupted, interrupted.Interruption, interrupted.Message);
        if (steps.Any(step => step.Status == EffectExecutionStatus.Succeeded))
            return (EffectExecutionStatus.Succeeded, EffectInterruptionReason.None, string.Empty);
        var skipped = steps[0];
        return (EffectExecutionStatus.Skipped, skipped.Interruption, skipped.Message);
    }

    private static EffectQueueDrainResult FinalizeDrain(ImmutableArray<EffectInvocationResult> invocations)
    {
        var failed = invocations.FirstOrDefault(result => result.Status == EffectExecutionStatus.Failed);
        if (failed is not null)
            return new EffectQueueDrainResult(EffectExecutionStatus.Failed, failed.Interruption, invocations);
        var interrupted = invocations.FirstOrDefault(result => result.Status == EffectExecutionStatus.Interrupted);
        if (interrupted is not null)
            return new EffectQueueDrainResult(EffectExecutionStatus.Interrupted, interrupted.Interruption, invocations);
        if (invocations.Any(result => result.Status == EffectExecutionStatus.Succeeded))
            return new EffectQueueDrainResult(EffectExecutionStatus.Succeeded, EffectInterruptionReason.None, invocations);
        return new EffectQueueDrainResult(EffectExecutionStatus.Skipped,
            invocations.FirstOrDefault()?.Interruption ?? EffectInterruptionReason.None, invocations);
    }

    private void AddTrace(PendingInvocation pending, EffectStepResult step) => AddTrace(
        pending,
        step.StepIndex,
        step.TargetId,
        step.Kind,
        step.Status,
        step.Interruption,
        step.AppliedAmount,
        step.EffectiveAmount,
        step.Message);

    private void AddTrace(
        PendingInvocation pending,
        int stepIndex,
        string targetId,
        EffectKind? kind,
        EffectExecutionStatus status,
        EffectInterruptionReason interruption,
        float appliedAmount,
        float effectiveAmount,
        string message)
    {
        if (_trace.Count >= _limits.MaxTraceEntries) return;
        var ordering = InvocationOrdering(pending) with { TargetId = targetId };
        _trace.Add(new EffectTraceEntry(
            ++_traceSequence,
            ordering,
            pending.Context,
            pending.Binding.StableId,
            stepIndex,
            targetId,
            kind,
            status,
            interruption,
            appliedAmount,
            effectiveAmount,
            message));
    }

    private void AbortPendingInto(
        ImmutableArray<EffectInvocationResult>.Builder results,
        EffectInterruptionReason reason,
        string message)
    {
        foreach (var pending in _pending.OrderBy(InvocationOrdering, Comparer<EffectOrderingKey>.Default))
            results.Add(CreateTerminalInvocation(pending, EffectExecutionStatus.Interrupted, reason, message));
        _pending.Clear();
    }

    private void Unsubscribe(long sequence, SubscriptionHandle handle)
    {
        if (!_subscriptionHandles.TryGetValue(sequence, out var current) || !ReferenceEquals(current, handle)) return;
        _subscriptionHandles.Remove(sequence);
        if (!_registrations.Remove(sequence, out var registration)) return;
        registration.Active = false;
        if (_subscriptions.TryGetValue(registration.Binding.Trigger.EventKind, out var listeners))
        {
            listeners.Remove(registration);
            if (listeners.Count == 0) _subscriptions.Remove(registration.Binding.Trigger.EventKind);
        }
        _runtimeStates.Remove(new RuntimeKey(registration.Binding.StableId, registration.SourceId, registration.OwnerId));
    }

    private EffectQueueDrainResult ReentrantDrainResult(
        CompiledEffectBinding binding,
        string sourceId,
        string ownerId,
        string explicitTargetId,
        int tick) => RejectedDrainResult(
        binding,
        sourceId,
        ownerId,
        explicitTargetId,
        tick,
        EffectInterruptionReason.ReentrantExecution,
        "Inline effect re-entry is forbidden; reactive work must enqueue for a later wave.");

    private EffectQueueDrainResult RejectedDrainResult(
        CompiledEffectBinding binding,
        string sourceId,
        string ownerId,
        string explicitTargetId,
        int tick,
        EffectInterruptionReason reason,
        string message)
    {
        var context = new EffectInvocationContext(
            _scopeId,
            $"{_scopeId}_rejected",
            sourceId,
            ownerId,
            Math.Max(0, tick),
            0,
            0);
        var result = new EffectInvocationResult(
            0,
            context,
            binding.StableId,
            EffectExecutionStatus.Interrupted,
            reason,
            [],
            message);
        if (_transition is null)
        {
            var pending = new PendingInvocation(binding, context, explicitTargetId, 0, 0, reason, message);
            AddTrace(pending, -1, explicitTargetId, null, EffectExecutionStatus.Interrupted, reason, 0, 0, message);
        }
        return new EffectQueueDrainResult(EffectExecutionStatus.Interrupted, reason, [result]);
    }

    private static EffectStepResult InterruptedStep(
        int stepIndex,
        string targetId,
        EffectKind kind,
        EffectInterruptionReason reason,
        string message) => new(
        stepIndex, targetId, kind, EffectExecutionStatus.Interrupted, reason, 0, 0, null, message);

    private static EffectStepResult FailedStep(
        int stepIndex,
        string targetId,
        EffectKind kind,
        string message) => new(
        stepIndex, targetId, kind, EffectExecutionStatus.Failed,
        EffectInterruptionReason.ProcessorFailure, 0, 0, null, message);

    private static EffectCommitOutcome ValidateCommitOutcome(
        PreparedWork work,
        EffectCommitOutcome outcome)
    {
        if (outcome.Status != EffectExecutionStatus.Succeeded) return outcome;
        if (!float.IsFinite(outcome.AppliedAmount) || outcome.AppliedAmount < 0 ||
            !float.IsFinite(outcome.EffectiveAmount) || outcome.EffectiveAmount < 0)
            return EffectCommitOutcome.Failed(
                $"Effect commit returned invalid amounts for {work.Invocation.Pending.Binding.StableId}[{work.StepIndex}].");
        var expectedEvent = EffectBindingCompiler.EventKindFor(work.Kind);
        if (outcome.EventKind != expectedEvent)
            return EffectCommitOutcome.Failed(
                $"Effect commit returned {outcome.EventKind} for {work.Kind}; expected {expectedEvent}.");
        return outcome;
    }

    private static EffectOrderingKey InvocationOrdering(PendingInvocation invocation) => new(
        invocation.Context.Tick,
        invocation.Binding.Priority,
        invocation.Context.SourceId,
        invocation.Context.OwnerId,
        invocation.Binding.StableId,
        invocation.ExplicitTargetId,
        invocation.Context.InvocationSequence);

    private static EffectEnqueueResult Rejected(EffectInterruptionReason reason, string message) =>
        new(false, 0, reason, message);

    private static void EnsureAttribution(string sourceId, string ownerId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("Effect source id is required.", nameof(sourceId));
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("Effect owner id is required.", nameof(ownerId));
    }

    private static void ValidateLimits(EffectExecutionLimits limits)
    {
        if (limits.MaxInvocationsPerDrain <= 0 || limits.MaxStepsPerDrain <= 0 ||
            limits.MaxEventsPerDrain <= 0 || limits.MaxDepth < 0 ||
            limits.MaxRepeatedEdgesPerChain <= 0 || limits.MaxTraceEntries <= 0)
            throw new ArgumentOutOfRangeException(nameof(limits), "Effect execution limits are invalid.");
    }

    private sealed record PendingInvocation(
        CompiledEffectBinding Binding,
        EffectInvocationContext Context,
        string ExplicitTargetId,
        float InvocationValue,
        float EventEffectiveValue,
        EffectInterruptionReason? PreInterruption,
        string PreInterruptionMessage);

    private sealed class InvocationWork
    {
        public PendingInvocation Pending { get; }
        public List<EffectStepResult> Steps { get; } = [];
        public EffectExecutionStatus? TerminalStatus { get; private init; }
        public EffectInterruptionReason TerminalInterruption { get; private init; }
        public string TerminalMessage { get; private init; } = string.Empty;

        public InvocationWork(PendingInvocation pending) => Pending = pending;

        public static InvocationWork Terminal(
            PendingInvocation pending,
            EffectExecutionStatus status,
            EffectInterruptionReason interruption,
            string message) => new(pending)
        {
            TerminalStatus = status,
            TerminalInterruption = interruption,
            TerminalMessage = message
        };

        public void AddStep(EffectStepResult step) => Steps.Add(step);
    }

    private sealed record PreparedWork(
        InvocationWork Invocation,
        int StepIndex,
        string TargetId,
        EffectKind Kind,
        PreparedEffectMutation Mutation);

    private sealed class ReactiveRegistration(
        long sequence,
        CompiledEffectBinding binding,
        string sourceId,
        string ownerId)
    {
        public long Sequence { get; } = sequence;
        public CompiledEffectBinding Binding { get; } = binding;
        public string SourceId { get; } = sourceId;
        public string OwnerId { get; } = ownerId;
        public bool Active { get; set; } = true;
    }

    private sealed class SubscriptionHandle : IDisposable
    {
        private readonly long _sequence;
        private BattleEffectScope? _owner;

        internal SubscriptionHandle(BattleEffectScope owner, long sequence)
        {
            _owner = owner;
            _sequence = sequence;
        }

        public void Dispose()
        {
            var owner = System.Threading.Interlocked.Exchange(ref _owner, null);
            owner?.Unsubscribe(_sequence, this);
        }

        internal void Rearm(BattleEffectScope owner)
        {
            var current = System.Threading.Volatile.Read(ref _owner);
            if (ReferenceEquals(current, owner)) return;
            if (current is not null)
                throw new InvalidOperationException("Effect subscription handle belongs to another scope.");
            var previous = System.Threading.Interlocked.CompareExchange(ref _owner, owner, null);
            if (previous is not null && !ReferenceEquals(previous, owner))
                throw new InvalidOperationException("Effect subscription handle could not be restored.");
        }

        internal void Invalidate(BattleEffectScope owner)
        {
            var current = System.Threading.Volatile.Read(ref _owner);
            if (current is null) return;
            if (!ReferenceEquals(current, owner))
                throw new InvalidOperationException("Effect subscription handle belongs to another scope.");
            System.Threading.Interlocked.CompareExchange(ref _owner, null, owner);
        }
    }

    private sealed class RuntimeState
    {
        public int Uses { get; set; }
        public int? LastTick { get; set; }
    }

    internal sealed class EffectStateCheckpoint
    {
        private readonly BattleEffectScope _owner;
        private readonly PendingInvocation[] _pending;
        private readonly Dictionary<EffectDomainEventKind, ReactiveRegistration[]> _subscriptions;
        private readonly Dictionary<long, ReactiveRegistration> _registrations;
        private readonly Dictionary<long, SubscriptionHandle> _subscriptionHandles;
        private readonly Dictionary<ReactiveRegistration, bool> _registrationActivity;
        private readonly Dictionary<RuntimeKey, (int Uses, int? LastTick)> _runtimeStates;
        private readonly Dictionary<RepeatedEdgeKey, int> _edgeCounts;
        private readonly int _eventCount;
        private readonly int _traceCount;
        private readonly long _invocationSequence;
        private readonly long _chainSequence;
        private readonly long _eventSequence;
        private readonly long _traceSequence;
        private readonly long _registrationSequence;
        private readonly int _lastTick;

        internal EffectStateCheckpoint(BattleEffectScope owner)
        {
            _owner = owner;
            _pending = owner._pending.ToArray();
            _subscriptions = owner._subscriptions.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToArray());
            _registrations = owner._registrations.ToDictionary(pair => pair.Key, pair => pair.Value);
            _subscriptionHandles = owner._subscriptionHandles.ToDictionary(pair => pair.Key, pair => pair.Value);
            _registrationActivity = owner._registrations.Values.Distinct()
                .ToDictionary(registration => registration, registration => registration.Active);
            _runtimeStates = owner._runtimeStates.ToDictionary(
                pair => pair.Key,
                pair => (pair.Value.Uses, pair.Value.LastTick));
            _edgeCounts = owner._edgeCounts.ToDictionary(pair => pair.Key, pair => pair.Value);
            // Events and trace are append-only while the scope is active. Retaining only
            // their boundaries avoids copying the complete Battle history per Ability.
            _eventCount = owner._events.Count;
            _traceCount = owner._trace.Count;
            _invocationSequence = owner._invocationSequence;
            _chainSequence = owner._chainSequence;
            _eventSequence = owner._eventSequence;
            _traceSequence = owner._traceSequence;
            _registrationSequence = owner._registrationSequence;
            _lastTick = owner._lastTick;
        }

        internal void Restore(BattleEffectScope owner)
        {
            if (!ReferenceEquals(owner, _owner))
                throw new InvalidOperationException("Effect checkpoint belongs to another scope.");
            if (owner._transition is not null || owner._isDraining)
                throw new InvalidOperationException("Cannot restore an unavailable Effect scope.");
            if (owner._events.Count < _eventCount || owner._trace.Count < _traceCount)
                throw new InvalidOperationException("Effect append-only history changed before rollback.");

            // Failed-transaction handles are invalidated before the registration sequence
            // can rewind. Only handles present in the checkpoint regain ownership.
            foreach (var handle in owner._subscriptionHandles.Values.ToArray()) handle.Invalidate(owner);
            foreach (var registration in owner._registrations.Values) registration.Active = false;

            owner._pending.Clear();
            owner._pending.AddRange(_pending);
            owner._subscriptions.Clear();
            foreach (var pair in _subscriptions) owner._subscriptions.Add(pair.Key, pair.Value.ToList());
            owner._registrations.Clear();
            foreach (var pair in _registrations) owner._registrations.Add(pair.Key, pair.Value);
            owner._subscriptionHandles.Clear();
            foreach (var pair in _subscriptionHandles) owner._subscriptionHandles.Add(pair.Key, pair.Value);
            foreach (var handle in owner._subscriptionHandles.Values) handle.Rearm(owner);
            foreach (var pair in _registrationActivity) pair.Key.Active = pair.Value;
            owner._runtimeStates.Clear();
            foreach (var pair in _runtimeStates)
                owner._runtimeStates.Add(pair.Key, new RuntimeState { Uses = pair.Value.Uses, LastTick = pair.Value.LastTick });
            owner._edgeCounts.Clear();
            foreach (var pair in _edgeCounts) owner._edgeCounts.Add(pair.Key, pair.Value);
            if (owner._events.Count > _eventCount)
                owner._events.RemoveRange(_eventCount, owner._events.Count - _eventCount);
            if (owner._trace.Count > _traceCount)
                owner._trace.RemoveRange(_traceCount, owner._trace.Count - _traceCount);
            owner._invocationSequence = _invocationSequence;
            owner._chainSequence = _chainSequence;
            owner._eventSequence = _eventSequence;
            owner._traceSequence = _traceSequence;
            owner._registrationSequence = _registrationSequence;
            owner._lastTick = _lastTick;
        }
    }

    private readonly record struct RuntimeKey(string BindingId, string SourceId, string OwnerId);
    private readonly record struct RepeatedEdgeKey(
        string ChainId,
        string ProducerBindingId,
        EffectDomainEventKind EventKind,
        string ConsumerBindingId);
}
