using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Battle;

public enum BattleCombatEventKind
{
    BattleStarted, BattleCompleted, AttackDeclared, AttackLanded, AbilityResolved,
    DamageResolved, HealingResolved, ShieldResolved,
    StatusApplied, StatusStackChanged, StatusRemoved,
    UnitSummoned, UnitMoved, UnitDefeated, UnitKilled
}

public enum BattleCombatCalculationKind { Damage, Healing, Shield }
public enum BattleCombatPublishRejection { None, PipelineCompleted, SynchronousReentry, EventBudget, DepthLimit }
public enum BattleCombatCompletionReason { None, PlayerVictory, PlayerDefeat, Timeout, Abort, Replacement, Exception, Disposal }

public readonly record struct CombatCell(int X, int Y);

public sealed record BattleCombatEventDraft(
    BattleCombatEventKind Kind, CombatSourceRef Source, string SourceRuntimeId, string TargetRuntimeId, int Tick,
    float RequestedValue = 0, float AppliedValue = 0, float EffectiveValue = 0, CombatCell Cell = default,
    string SubjectStableId = "", int PreviousStacks = 0, int CurrentStacks = 0, string Reason = "");

public sealed record BattleCombatEvent(
    long Sequence, string ScopeId, BattleIdentity? Identity, string ChainId, int Depth, BattleCombatEventKind Kind,
    CombatSourceRef Source, string SourceRuntimeId, string TargetRuntimeId, int Tick,
    float RequestedValue, float AppliedValue, float EffectiveValue, CombatCell Cell,
    string SubjectStableId, int PreviousStacks, int CurrentStacks, string Reason);

public sealed record BattleCombatPublishResult(
    bool Accepted, BattleCombatPublishRejection Rejection, BattleCombatEvent? Event, string Message);

public sealed record BattleCombatCalculationRequest(
    BattleCombatCalculationKind Kind, CombatSourceRef Source, string SourceRuntimeId,
    string TargetRuntimeId, int Tick, float RequestedAmount);

public sealed record BattleCombatCalculationContribution(
    CombatSourceRef Source, int Priority, float Before, float After);

public sealed record BattleCombatCalculationResult(
    float RequestedAmount, float ResolvedAmount, ImmutableArray<BattleCombatCalculationContribution> Contributions);

public sealed record BattleCombatPipelineLimits(
    int MaxEvents = 65_536, int MaxReactions = 16_384, int MaxDepth = 16, int MaxTraceEntries = 65_536);

public sealed record BattleCombatTraceEntry(
    long Sequence, string ChainId, int Depth, string Kind, CombatSourceRef Source, string Detail);

public sealed record BattleCombatTransitionResult(
    string ScopeId, BattleCombatCompletionReason Reason, int FinalTick,
    ImmutableArray<BattleCombatEvent> Events, ImmutableArray<BattleCombatTraceEntry> Trace,
    int RemainingSubscriptions, int RemainingReactions, int RemainingRuntimeEntries);

internal enum BattleCombatSubscriptionChannel { Event, Calculation }

internal sealed record BattleCombatSubscriptionSnapshot(
    long Sequence,
    BattleCombatSubscriptionChannel Channel,
    BattleCombatEventKind? EventKind,
    BattleCombatCalculationKind? CalculationKind,
    CombatSourceRef Source,
    int Priority);

public sealed class BattleCombatReactionContext
{
    private readonly BattleCombatEventPipeline _pipeline;
    internal BattleCombatReactionContext(BattleCombatEventPipeline pipeline, string chainId, int depth)
    {
        _pipeline = pipeline;
        ChainId = chainId;
        Depth = depth;
    }
    public string ChainId { get; }
    public int Depth { get; }
    public BattleCombatPublishResult Publish(BattleCombatEventDraft draft) => _pipeline.PublishReaction(draft, ChainId, Depth);
}

public sealed class BattleCombatReactionSink
{
    private readonly BattleCombatEventPipeline _pipeline;
    private readonly BattleCombatEvent _event;
    internal BattleCombatReactionSink(BattleCombatEventPipeline pipeline, BattleCombatEvent combatEvent)
    {
        _pipeline = pipeline;
        _event = combatEvent;
    }
    public bool Enqueue(CombatSourceRef source, int priority, Action<BattleCombatReactionContext> reaction) =>
        _pipeline.EnqueueReaction(_event, source, priority, reaction);
}

public sealed class BattleCombatBindingRegistry
{
    private readonly BattleCombatEventPipeline _pipeline;
    private bool _registrationOpen = true;

    internal BattleCombatBindingRegistry(BattleCombatEventPipeline pipeline) =>
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));

    public IDisposable Subscribe(
        BattleCombatEventKind kind, CombatSourceRef source, int priority,
        Action<BattleCombatEvent, BattleCombatReactionSink> listener)
    {
        EnsureRegistrationOpen();
        return _pipeline.Subscribe(kind, source, priority, listener);
    }

    public IDisposable SubscribeCalculation(
        BattleCombatCalculationKind kind, CombatSourceRef source, int priority,
        Func<BattleCombatCalculationRequest, float, float> resolver)
    {
        EnsureRegistrationOpen();
        return _pipeline.SubscribeCalculation(kind, source, priority, resolver);
    }

    internal void CloseRegistration() => _registrationOpen = false;

    private void EnsureRegistrationOpen()
    {
        if (!_registrationOpen)
            throw new InvalidOperationException("Combat bindings may only be registered while the Battle is being constructed.");
    }
}

public sealed class BattleCombatEventPipeline : IDisposable
{
    private readonly Dictionary<BattleCombatEventKind, List<EventRegistration>> _eventSubscriptions = [];
    private readonly Dictionary<BattleCombatCalculationKind, List<CalculationRegistration>> _calculationSubscriptions = [];
    private readonly Dictionary<BattleCombatEventKind, EventRegistration[]> _eventSnapshots = [];
    private readonly Dictionary<BattleCombatCalculationKind, CalculationRegistration[]> _calculationSnapshots = [];
    private readonly Dictionary<long, Action> _unsubscribers = [];
    private readonly Dictionary<long, SubscriptionHandle> _subscriptionHandles = [];
    private readonly List<PendingReaction> _pending = [];
    private readonly List<BattleCombatEvent> _events = [];
    private readonly List<BattleCombatTraceEntry> _trace = [];
    private readonly ReadOnlyCollection<BattleCombatEvent> _eventView;
    private readonly ReadOnlyCollection<BattleCombatTraceEntry> _traceView;
    private long _registrationSequence;
    private long _eventSequence;
    private long _reactionSequence;
    private long _chainSequence;
    private long _traceSequence;
    private int _executedReactions;
    private int _resolutionDepth;
    private readonly Stack<ResolutionFrame> _resolutionFrames = [];
    private long _resolutionSequence;
    private int _lastTick;
    private bool _insideSubscriber;
    private bool _isDraining;

    public BattleCombatEventPipeline(
        string scopeId,
        BattleCombatPipelineLimits? limits = null,
        BattleIdentity? identity = null)
    {
        if (string.IsNullOrWhiteSpace(scopeId)) throw new ArgumentException("Combat pipeline scope id is required.", nameof(scopeId));
        ScopeId = scopeId;
        Identity = identity;
        Limits = limits ?? new BattleCombatPipelineLimits();
        _eventView = _events.AsReadOnly();
        _traceView = _trace.AsReadOnly();
        if (Limits.MaxEvents <= 0 || Limits.MaxReactions <= 0 || Limits.MaxDepth <= 0 || Limits.MaxTraceEntries <= 0)
            throw new ArgumentOutOfRangeException(nameof(limits), "Combat pipeline limits must be positive.");
    }

    public string ScopeId { get; }
    public BattleIdentity? Identity { get; }
    public BattleCombatPipelineLimits Limits { get; }
    public bool IsCompleted => Transition is not null;
    public int SubscriptionCount => _unsubscribers.Count;
    public int PendingReactionCount => _pending.Count;
    public IReadOnlyList<BattleCombatEvent> Events => _eventView;
    public IReadOnlyList<BattleCombatTraceEntry> Trace => _traceView;
    public BattleCombatTransitionResult? Transition { get; private set; }

    internal ImmutableArray<BattleCombatSubscriptionSnapshot> CaptureSubscriptionSnapshot() =>
        _eventSubscriptions
            .SelectMany(pair => pair.Value.Select(registration => new BattleCombatSubscriptionSnapshot(
                registration.Sequence,
                BattleCombatSubscriptionChannel.Event,
                pair.Key,
                null,
                registration.Source,
                registration.Priority)))
            .Concat(_calculationSubscriptions.SelectMany(pair => pair.Value.Select(registration =>
                new BattleCombatSubscriptionSnapshot(
                    registration.Sequence,
                    BattleCombatSubscriptionChannel.Calculation,
                    null,
                    pair.Key,
                    registration.Source,
                    registration.Priority))))
            .OrderBy(subscription => subscription.Sequence)
            .ToImmutableArray();

    internal CombatStateCheckpoint CaptureState()
    {
        if (Transition is not null)
            throw new InvalidOperationException("Cannot checkpoint a completed combat pipeline.");
        if (_insideSubscriber || _isDraining)
            throw new InvalidOperationException("Cannot checkpoint the combat pipeline during subscriber execution or reaction drain.");
        return new CombatStateCheckpoint(this);
    }

    internal void RestoreState(CombatStateCheckpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        checkpoint.Restore(this);
    }

    public IDisposable Subscribe(
        BattleCombatEventKind kind, CombatSourceRef source, int priority,
        Action<BattleCombatEvent, BattleCombatReactionSink> listener)
    {
        EnsureCanSubscribe(source);
        ArgumentNullException.ThrowIfNull(listener);
        var sequence = ++_registrationSequence;
        var registration = new EventRegistration(sequence, source, priority, listener);
        if (!_eventSubscriptions.TryGetValue(kind, out var listeners)) _eventSubscriptions[kind] = listeners = [];
        listeners.Add(registration);
        RefreshEventSnapshot(kind, listeners);
        _unsubscribers[sequence] = () =>
        {
            listeners.Remove(registration);
            if (listeners.Count == 0)
            {
                _eventSubscriptions.Remove(kind);
                _eventSnapshots.Remove(kind);
            }
            else RefreshEventSnapshot(kind, listeners);
        };
        var handle = new SubscriptionHandle(this, sequence);
        _subscriptionHandles.Add(sequence, handle);
        return handle;
    }

    public IDisposable SubscribeCalculation(
        BattleCombatCalculationKind kind, CombatSourceRef source, int priority,
        Func<BattleCombatCalculationRequest, float, float> resolver)
    {
        EnsureCanSubscribe(source);
        ArgumentNullException.ThrowIfNull(resolver);
        var sequence = ++_registrationSequence;
        var registration = new CalculationRegistration(sequence, source, priority, resolver);
        if (!_calculationSubscriptions.TryGetValue(kind, out var listeners)) _calculationSubscriptions[kind] = listeners = [];
        listeners.Add(registration);
        RefreshCalculationSnapshot(kind, listeners);
        _unsubscribers[sequence] = () =>
        {
            listeners.Remove(registration);
            if (listeners.Count == 0)
            {
                _calculationSubscriptions.Remove(kind);
                _calculationSnapshots.Remove(kind);
            }
            else RefreshCalculationSnapshot(kind, listeners);
        };
        var handle = new SubscriptionHandle(this, sequence);
        _subscriptionHandles.Add(sequence, handle);
        return handle;
    }

    public BattleCombatCalculationResult Resolve(BattleCombatCalculationRequest request)
    {
        if (IsCompleted) throw new InvalidOperationException("Combat pipeline has completed.");
        if (_insideSubscriber) throw new InvalidOperationException("Combat calculations cannot synchronously re-enter the pipeline.");
        if (request.Tick < 0) throw new ArgumentOutOfRangeException(nameof(request));
        ValidateAmount(request.RequestedAmount, nameof(request.RequestedAmount));
        _lastTick = Math.Max(_lastTick, request.Tick);
        var contributions = ImmutableArray.CreateBuilder<BattleCombatCalculationContribution>();
        var current = request.RequestedAmount;
        var listeners = _calculationSnapshots.GetValueOrDefault(request.Kind) ?? [];
        _insideSubscriber = true;
        try
        {
            foreach (var listener in listeners)
            {
                var before = current;
                current = listener.Resolver(request, current);
                ValidateAmount(current, "resolved calculation amount");
                contributions.Add(new BattleCombatCalculationContribution(listener.Source, listener.Priority, before, current));
                AddTrace(string.Empty, 0, "calculation", listener.Source, $"{request.Kind}:{before:R}->{current:R}");
            }
        }
        finally { _insideSubscriber = false; }
        return new BattleCombatCalculationResult(request.RequestedAmount, current, contributions.ToImmutable());
    }

    public BattleCombatPublishResult Publish(BattleCombatEventDraft draft)
    {
        if (_insideSubscriber)
            return Rejected(BattleCombatPublishRejection.SynchronousReentry, "Combat listeners cannot synchronously publish.");
        return PublishCore(draft, $"{ScopeId}:chain:{++_chainSequence}", 0, false);
    }

    public BattleCombatResolution BeginAuthoritativeResolution()
    {
        if (IsCompleted) throw new InvalidOperationException("Combat pipeline has completed.");
        if (_insideSubscriber)
            throw new InvalidOperationException("Combat listeners cannot synchronously open an authoritative resolution.");
        var frame = new ResolutionFrame(++_resolutionSequence, _pending.Count);
        _resolutionFrames.Push(frame);
        _resolutionDepth++;
        return new BattleCombatResolution(this, frame.Sequence);
    }

    internal BattleCombatPublishResult PublishReaction(BattleCombatEventDraft draft, string chainId, int depth)
    {
        if (_insideSubscriber)
            return Rejected(BattleCombatPublishRejection.SynchronousReentry, "Combat listeners cannot synchronously publish.");
        return PublishCore(draft, chainId, depth, false);
    }

    internal bool EnqueueReaction(
        BattleCombatEvent combatEvent, CombatSourceRef source, int priority,
        Action<BattleCombatReactionContext> reaction)
    {
        if (IsCompleted || !_insideSubscriber || !source.IsSpecified || reaction is null ||
            _pending.Count + _executedReactions >= Limits.MaxReactions || combatEvent.Depth + 1 > Limits.MaxDepth)
            return false;
        _pending.Add(new PendingReaction(
            combatEvent.Sequence, combatEvent.ChainId, combatEvent.Depth + 1, source, priority, ++_reactionSequence, reaction));
        AddTrace(combatEvent.ChainId, combatEvent.Depth + 1, "reaction-enqueued", source, combatEvent.Kind.ToString());
        return true;
    }

    // Product scopes use this only while an authoritative mutation is open. The
    // work joins the same bounded deterministic queue and is discarded when the
    // enclosing resolution rolls back; it never runs inside the product mutation.
    public bool EnqueuePostResolution(
        CombatSourceRef source,
        int priority,
        Action<BattleCombatReactionContext> reaction)
    {
        if (IsCompleted || _insideSubscriber || _resolutionDepth <= 0 || !source.IsSpecified || reaction is null ||
            _pending.Count + _executedReactions >= Limits.MaxReactions || Limits.MaxDepth < 1)
            return false;
        var chainId = $"{ScopeId}:deferred:{++_chainSequence}";
        _pending.Add(new PendingReaction(
            _eventSequence, chainId, 1, source, priority, ++_reactionSequence, reaction));
        AddTrace(chainId, 1, "reaction-enqueued", source, "post-resolution");
        return true;
    }

    public BattleCombatTransitionResult Complete(BattleCombatCompletionReason reason, int finalTick)
    {
        if (Transition is not null) return Transition;
        if (reason == BattleCombatCompletionReason.None) throw new ArgumentOutOfRangeException(nameof(reason));
        if (finalTick < 0) throw new ArgumentOutOfRangeException(nameof(finalTick));
        _lastTick = Math.Max(_lastTick, finalTick);
        Exception? failure = null;
        try
        {
            var completion = new BattleCombatEventDraft(
                BattleCombatEventKind.BattleCompleted, CombatSourceRef.System(ScopeId), string.Empty, string.Empty,
                _lastTick, Reason: reason.ToString());
            var published = PublishCore(completion, $"{ScopeId}:chain:{++_chainSequence}", 0, true);
            if (!published.Accepted) throw new InvalidOperationException(published.Message);
        }
        catch (Exception exception) { failure = exception; }
        finally
        {
            foreach (var handle in _subscriptionHandles.Values.ToArray()) handle.Invalidate(this);
            _pending.Clear();
            _eventSubscriptions.Clear();
            _calculationSubscriptions.Clear();
            _eventSnapshots.Clear();
            _calculationSnapshots.Clear();
            _unsubscribers.Clear();
            _subscriptionHandles.Clear();
            _resolutionDepth = 0;
            _resolutionFrames.Clear();
            Transition = new BattleCombatTransitionResult(
                ScopeId, failure is null ? reason : BattleCombatCompletionReason.Exception, _lastTick,
                _events.ToImmutableArray(), _trace.ToImmutableArray(), SubscriptionCount, PendingReactionCount, 0);
        }
        if (failure is not null) throw failure;
        return Transition;
    }

    public void Dispose()
    {
        if (Transition is null) Complete(BattleCombatCompletionReason.Disposal, _lastTick);
    }

    private BattleCombatPublishResult PublishCore(
        BattleCombatEventDraft draft, string chainId, int depth, bool completion)
    {
        if (IsCompleted) return Rejected(BattleCombatPublishRejection.PipelineCompleted, "Combat pipeline has completed.");
        if (draft.Tick < 0) throw new ArgumentOutOfRangeException(nameof(draft));
        ValidateAmount(draft.RequestedValue, nameof(draft.RequestedValue));
        ValidateAmount(draft.AppliedValue, nameof(draft.AppliedValue));
        ValidateAmount(draft.EffectiveValue, nameof(draft.EffectiveValue));
        if (depth > Limits.MaxDepth) return Rejected(BattleCombatPublishRejection.DepthLimit, "Combat event depth limit reached.");
        if (!completion && _events.Count >= Limits.MaxEvents)
            return Rejected(BattleCombatPublishRejection.EventBudget, "Combat event budget reached.");
        _lastTick = Math.Max(_lastTick, draft.Tick);
        var combatEvent = new BattleCombatEvent(
            ++_eventSequence, ScopeId, Identity, chainId, depth, draft.Kind, draft.Source,
            draft.SourceRuntimeId, draft.TargetRuntimeId, draft.Tick,
            draft.RequestedValue, draft.AppliedValue, draft.EffectiveValue, draft.Cell,
            draft.SubjectStableId, draft.PreviousStacks, draft.CurrentStacks, draft.Reason);
        _events.Add(combatEvent);
        AddTrace(chainId, depth, "event", draft.Source, draft.Kind.ToString());
        var listeners = _eventSnapshots.GetValueOrDefault(draft.Kind) ?? [];
        var pendingCheckpoint = _pending.Count;
        _insideSubscriber = true;
        try
        {
            var sink = new BattleCombatReactionSink(this, combatEvent);
            foreach (var listener in listeners) listener.Listener(combatEvent, sink);
        }
        catch
        {
            DiscardPendingSince(pendingCheckpoint);
            throw;
        }
        finally { _insideSubscriber = false; }
        if (!_isDraining && _resolutionDepth == 0) DrainReactions();
        return new BattleCombatPublishResult(true, BattleCombatPublishRejection.None, combatEvent, string.Empty);
    }

    private void DrainReactions()
    {
        _isDraining = true;
        try
        {
            while (_pending.Count > 0)
            {
                var wave = _pending.OrderBy(item => item.ParentEventSequence)
                    .ThenBy(item => item.Priority).ThenBy(item => item.Source).ThenBy(item => item.EnqueueSequence).ToArray();
                _pending.Clear();
                foreach (var reaction in wave)
                {
                    if (++_executedReactions > Limits.MaxReactions)
                        throw new InvalidOperationException("Combat reaction budget exceeded.");
                    AddTrace(reaction.ChainId, reaction.Depth, "reaction-executed", reaction.Source, string.Empty);
                    reaction.Reaction(new BattleCombatReactionContext(this, reaction.ChainId, reaction.Depth));
                }
            }
        }
        catch
        {
            _pending.Clear();
            throw;
        }
        finally { _isDraining = false; }
    }

    private void EnsureCanSubscribe(CombatSourceRef source)
    {
        if (IsCompleted) throw new InvalidOperationException("Combat pipeline has completed.");
        if (!source.IsSpecified) throw new ArgumentException("Combat subscription source is required.", nameof(source));
    }

    private void RefreshEventSnapshot(BattleCombatEventKind kind, List<EventRegistration> listeners) =>
        _eventSnapshots[kind] = listeners.OrderBy(item => item.Priority)
            .ThenBy(item => item.Source)
            .ThenBy(item => item.Sequence)
            .ToArray();

    private void RefreshCalculationSnapshot(
        BattleCombatCalculationKind kind,
        List<CalculationRegistration> listeners) =>
        _calculationSnapshots[kind] = listeners.OrderBy(item => item.Priority)
            .ThenBy(item => item.Source)
            .ThenBy(item => item.Sequence)
            .ToArray();

    private void Unsubscribe(long sequence, SubscriptionHandle handle)
    {
        if (!_subscriptionHandles.TryGetValue(sequence, out var current) || !ReferenceEquals(current, handle)) return;
        _subscriptionHandles.Remove(sequence);
        if (!_unsubscribers.Remove(sequence, out var unsubscribe)) return;
        unsubscribe();
    }

    internal void EndAuthoritativeResolution(long sequence, bool committed)
    {
        if (_resolutionDepth <= 0 || _resolutionFrames.Count == 0) return;
        var frame = _resolutionFrames.Peek();
        if (frame.Sequence != sequence)
            throw new InvalidOperationException("Authoritative resolutions must complete in LIFO order.");
        _resolutionFrames.Pop();
        if (!committed && _pending.Count > frame.PendingCheckpoint)
            _pending.RemoveRange(frame.PendingCheckpoint, _pending.Count - frame.PendingCheckpoint);
        _resolutionDepth--;
        if (committed && _resolutionDepth == 0 && !IsCompleted && !_isDraining) DrainReactions();
    }

    private void AddTrace(string chainId, int depth, string kind, CombatSourceRef source, string detail)
    {
        if (_trace.Count >= Limits.MaxTraceEntries) return;
        _trace.Add(new BattleCombatTraceEntry(++_traceSequence, chainId, depth, kind, source, detail));
    }

    private void DiscardPendingSince(int checkpoint)
    {
        if (_pending.Count > checkpoint)
            _pending.RemoveRange(checkpoint, _pending.Count - checkpoint);
    }

    private static BattleCombatPublishResult Rejected(BattleCombatPublishRejection rejection, string message) =>
        new(false, rejection, null, message);

    private static void ValidateAmount(float value, string parameter)
    {
        if (!float.IsFinite(value) || value < 0)
            throw new ArgumentOutOfRangeException(parameter, "Combat amounts must be finite and non-negative.");
    }

    private sealed record EventRegistration(
        long Sequence, CombatSourceRef Source, int Priority, Action<BattleCombatEvent, BattleCombatReactionSink> Listener);
    private sealed record CalculationRegistration(
        long Sequence, CombatSourceRef Source, int Priority, Func<BattleCombatCalculationRequest, float, float> Resolver);
    private sealed record PendingReaction(
        long ParentEventSequence, string ChainId, int Depth, CombatSourceRef Source, int Priority,
        long EnqueueSequence, Action<BattleCombatReactionContext> Reaction);
    private readonly record struct ResolutionFrame(long Sequence, int PendingCheckpoint);

    internal sealed class CombatStateCheckpoint
    {
        private readonly BattleCombatEventPipeline _owner;
        private readonly Dictionary<BattleCombatEventKind,
            (List<EventRegistration> List, EventRegistration[] Entries)> _eventSubscriptions;
        private readonly Dictionary<BattleCombatCalculationKind,
            (List<CalculationRegistration> List, CalculationRegistration[] Entries)> _calculationSubscriptions;
        private readonly Dictionary<BattleCombatEventKind, EventRegistration[]> _eventSnapshots;
        private readonly Dictionary<BattleCombatCalculationKind, CalculationRegistration[]> _calculationSnapshots;
        private readonly Dictionary<long, Action> _unsubscribers;
        private readonly Dictionary<long, SubscriptionHandle> _subscriptionHandles;
        private readonly PendingReaction[] _pending;
        private readonly int _eventCount;
        private readonly int _traceCount;
        private readonly ResolutionFrame[] _resolutionFrames;
        private readonly long _registrationSequence;
        private readonly long _eventSequence;
        private readonly long _reactionSequence;
        private readonly long _chainSequence;
        private readonly long _traceSequence;
        private readonly long _resolutionSequence;
        private readonly int _executedReactions;
        private readonly int _resolutionDepth;
        private readonly int _lastTick;
        private readonly bool _insideSubscriber;
        private readonly bool _isDraining;
        private readonly BattleCombatTransitionResult? _transition;

        internal CombatStateCheckpoint(BattleCombatEventPipeline owner)
        {
            _owner = owner;
            _eventSubscriptions = owner._eventSubscriptions.ToDictionary(
                pair => pair.Key,
                pair => (pair.Value, pair.Value.ToArray()));
            _calculationSubscriptions = owner._calculationSubscriptions.ToDictionary(
                pair => pair.Key,
                pair => (pair.Value, pair.Value.ToArray()));
            _eventSnapshots = owner._eventSnapshots.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            _calculationSnapshots = owner._calculationSnapshots.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            _unsubscribers = owner._unsubscribers.ToDictionary(pair => pair.Key, pair => pair.Value);
            _subscriptionHandles = owner._subscriptionHandles.ToDictionary(pair => pair.Key, pair => pair.Value);
            _pending = owner._pending.ToArray();
            // Published facts and trace entries are append-only until completion. A
            // boundary is sufficient to restore a failed authoritative transaction.
            _eventCount = owner._events.Count;
            _traceCount = owner._trace.Count;
            _resolutionFrames = owner._resolutionFrames.ToArray();
            _registrationSequence = owner._registrationSequence;
            _eventSequence = owner._eventSequence;
            _reactionSequence = owner._reactionSequence;
            _chainSequence = owner._chainSequence;
            _traceSequence = owner._traceSequence;
            _resolutionSequence = owner._resolutionSequence;
            _executedReactions = owner._executedReactions;
            _resolutionDepth = owner._resolutionDepth;
            _lastTick = owner._lastTick;
            _insideSubscriber = owner._insideSubscriber;
            _isDraining = owner._isDraining;
            _transition = owner.Transition;
        }

        internal void Restore(BattleCombatEventPipeline owner)
        {
            if (!ReferenceEquals(owner, _owner))
                throw new InvalidOperationException("Combat checkpoint belongs to another pipeline.");
            if (owner._insideSubscriber || owner._isDraining)
                throw new InvalidOperationException("Cannot restore the combat pipeline during active execution.");
            if (owner._events.Count < _eventCount || owner._trace.Count < _traceCount)
                throw new InvalidOperationException("Combat append-only history changed before rollback.");

            // Handles created by the failed transaction must never be able to act on a
            // later registration that reuses their rolled-back sequence. Existing handles
            // are rearmed only after their exact checkpoint registrations are restored.
            foreach (var handle in owner._subscriptionHandles.Values.ToArray()) handle.Invalidate(owner);

            owner._eventSubscriptions.Clear();
            foreach (var pair in _eventSubscriptions)
            {
                pair.Value.List.Clear();
                pair.Value.List.AddRange(pair.Value.Entries);
                owner._eventSubscriptions.Add(pair.Key, pair.Value.List);
            }
            owner._calculationSubscriptions.Clear();
            foreach (var pair in _calculationSubscriptions)
            {
                pair.Value.List.Clear();
                pair.Value.List.AddRange(pair.Value.Entries);
                owner._calculationSubscriptions.Add(pair.Key, pair.Value.List);
            }
            owner._eventSnapshots.Clear();
            foreach (var pair in _eventSnapshots) owner._eventSnapshots.Add(pair.Key, pair.Value.ToArray());
            owner._calculationSnapshots.Clear();
            foreach (var pair in _calculationSnapshots)
                owner._calculationSnapshots.Add(pair.Key, pair.Value.ToArray());
            owner._unsubscribers.Clear();
            foreach (var pair in _unsubscribers) owner._unsubscribers.Add(pair.Key, pair.Value);
            owner._subscriptionHandles.Clear();
            foreach (var pair in _subscriptionHandles) owner._subscriptionHandles.Add(pair.Key, pair.Value);
            foreach (var handle in owner._subscriptionHandles.Values) handle.Rearm(owner);
            owner._pending.Clear();
            owner._pending.AddRange(_pending);
            if (owner._events.Count > _eventCount)
                owner._events.RemoveRange(_eventCount, owner._events.Count - _eventCount);
            if (owner._trace.Count > _traceCount)
                owner._trace.RemoveRange(_traceCount, owner._trace.Count - _traceCount);
            owner._resolutionFrames.Clear();
            foreach (var frame in _resolutionFrames.Reverse()) owner._resolutionFrames.Push(frame);
            owner._registrationSequence = _registrationSequence;
            owner._eventSequence = _eventSequence;
            owner._reactionSequence = _reactionSequence;
            owner._chainSequence = _chainSequence;
            owner._traceSequence = _traceSequence;
            owner._resolutionSequence = _resolutionSequence;
            owner._executedReactions = _executedReactions;
            owner._resolutionDepth = _resolutionDepth;
            owner._lastTick = _lastTick;
            owner._insideSubscriber = _insideSubscriber;
            owner._isDraining = _isDraining;
            owner.Transition = _transition;
        }
    }

    private sealed class SubscriptionHandle : IDisposable
    {
        private readonly long _sequence;
        private BattleCombatEventPipeline? _owner;

        internal SubscriptionHandle(BattleCombatEventPipeline owner, long sequence)
        {
            _owner = owner;
            _sequence = sequence;
        }

        public void Dispose()
        {
            var owner = System.Threading.Interlocked.Exchange(ref _owner, null);
            owner?.Unsubscribe(_sequence, this);
        }

        internal void Rearm(BattleCombatEventPipeline owner)
        {
            var current = System.Threading.Volatile.Read(ref _owner);
            if (ReferenceEquals(current, owner)) return;
            if (current is not null)
                throw new InvalidOperationException("Combat subscription handle belongs to another pipeline.");
            var previous = System.Threading.Interlocked.CompareExchange(ref _owner, owner, null);
            if (previous is not null && !ReferenceEquals(previous, owner))
                throw new InvalidOperationException("Combat subscription handle could not be restored.");
        }

        internal void Invalidate(BattleCombatEventPipeline owner)
        {
            var current = System.Threading.Volatile.Read(ref _owner);
            if (current is null) return;
            if (!ReferenceEquals(current, owner))
                throw new InvalidOperationException("Combat subscription handle belongs to another pipeline.");
            System.Threading.Interlocked.CompareExchange(ref _owner, null, owner);
        }
    }

}

public sealed class BattleCombatResolution : IDisposable
{
    private BattleCombatEventPipeline? _owner;
    private readonly long _sequence;
    private bool _committed;

    internal BattleCombatResolution(BattleCombatEventPipeline owner, long sequence)
    {
        _owner = owner;
        _sequence = sequence;
    }

    public void Commit() => _committed = true;

    public void Dispose()
    {
        var owner = _owner;
        if (owner is null) return;
        owner.EndAuthoritativeResolution(_sequence, _committed);
        System.Threading.Interlocked.CompareExchange(ref _owner, null, owner);
    }
}
