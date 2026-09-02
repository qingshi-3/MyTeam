using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TowerAutobattler.Effects;

public enum EffectKind { Damage, Heal, Shield }
public enum EffectAmountSource { Fixed, InvocationValue, EventEffectiveValue }
public enum EffectTriggerKind { Manual, DomainEvent }
public enum EffectDomainEventKind { None, DamageResolved, HealingResolved, ShieldResolved }
public enum EffectEntityReference { Source, Owner, ExplicitTarget }
public enum EffectRelativeTeam { Allies, Enemies }
public enum EffectExecutionStatus { Succeeded, Skipped, Failed, Interrupted }

public enum EffectInterruptionReason
{
    None,
    ScopeInactive,
    ScopeCompleted,
    ReentrantExecution,
    InvalidBinding,
    ConditionFailed,
    TargetUnavailable,
    RateLimited,
    UsageLimit,
    DepthLimit,
    InvocationBudget,
    StepBudget,
    EventBudget,
    RepeatedEdge,
    QueueAborted,
    ProcessorFailure
}

public enum BattleScopeCompletionReason
{
    None,
    PlayerVictory,
    PlayerDefeat,
    Timeout,
    Abort,
    Replacement,
    Exception,
    Disposal
}

public readonly record struct EffectOrderingKey(
    int Tick,
    int Priority,
    string SourceId,
    string OwnerId,
    string BindingId,
    string TargetId,
    long EnqueueSequence) : IComparable<EffectOrderingKey>
{
    public int CompareTo(EffectOrderingKey other)
    {
        var result = Tick.CompareTo(other.Tick);
        if (result != 0) return result;
        result = Priority.CompareTo(other.Priority);
        if (result != 0) return result;
        result = string.Compare(SourceId, other.SourceId, StringComparison.Ordinal);
        if (result != 0) return result;
        result = string.Compare(OwnerId, other.OwnerId, StringComparison.Ordinal);
        if (result != 0) return result;
        result = string.Compare(BindingId, other.BindingId, StringComparison.Ordinal);
        if (result != 0) return result;
        result = string.Compare(TargetId, other.TargetId, StringComparison.Ordinal);
        return result != 0 ? result : EnqueueSequence.CompareTo(other.EnqueueSequence);
    }
}

public sealed record EffectInvocationContext(
    string ScopeId,
    string ChainId,
    string SourceId,
    string OwnerId,
    int Tick,
    int Depth,
    long InvocationSequence);

public sealed record EffectExecutionLimits(
    int MaxInvocationsPerDrain = 256,
    int MaxStepsPerDrain = 1024,
    int MaxEventsPerDrain = 1024,
    int MaxDepth = 16,
    int MaxRepeatedEdgesPerChain = 8,
    int MaxTraceEntries = 4096);

public sealed record EffectEntitySnapshot(
    string RuntimeId,
    int Team,
    bool Alive,
    float Health,
    float MaxHealth,
    float Shield,
    ImmutableArray<string> Tags = default);

public sealed record EffectWorldSnapshot(
    int Tick,
    ImmutableDictionary<string, EffectEntitySnapshot> Entities)
{
    public static EffectWorldSnapshot Create(int tick, IEnumerable<EffectEntitySnapshot> entities) =>
        new(tick, entities.ToImmutableDictionary(entity => entity.RuntimeId, StringComparer.Ordinal));
}

public sealed record EffectModifierRequest(
    EffectInvocationContext Context,
    string BindingId,
    int StepIndex,
    EffectKind Kind,
    string TargetId,
    float RequestedAmount);

public sealed record EffectModifierContribution(string StableId, float Before, float After);

public sealed record EffectModifierResult(
    float RequestedAmount,
    float ResolvedAmount,
    ImmutableArray<EffectModifierContribution> Contributions)
{
    public static EffectModifierResult Identity(float amount) => new(amount, amount, []);
}

public sealed record PreparedEffectMutation(
    EffectModifierRequest Request,
    EffectModifierResult Modifiers,
    EffectOrderingKey Ordering);

public sealed record EffectCommitOutcome(
    EffectExecutionStatus Status,
    EffectInterruptionReason Interruption,
    float AppliedAmount,
    float EffectiveAmount,
    EffectDomainEventKind EventKind,
    string Message)
{
    public static EffectCommitOutcome Succeeded(
        float appliedAmount, float effectiveAmount, EffectDomainEventKind eventKind) =>
        new(EffectExecutionStatus.Succeeded, EffectInterruptionReason.None, appliedAmount, effectiveAmount, eventKind, string.Empty);

    public static EffectCommitOutcome Skipped(EffectInterruptionReason reason, string message) =>
        new(EffectExecutionStatus.Skipped, reason, 0, 0, EffectDomainEventKind.None, message);

    public static EffectCommitOutcome Failed(string message) =>
        new(EffectExecutionStatus.Failed, EffectInterruptionReason.ProcessorFailure, 0, 0, EffectDomainEventKind.None, message);
}

public interface IEffectRuntimeWorld
{
    EffectWorldSnapshot CaptureSnapshot(int tick);
    EffectModifierResult ResolveModifiers(EffectModifierRequest request, EffectWorldSnapshot snapshot);
    EffectCommitOutcome Commit(PreparedEffectMutation mutation);
}

public sealed record EffectDomainEvent(
    long Sequence,
    EffectDomainEventKind Kind,
    EffectInvocationContext Context,
    string BindingId,
    int StepIndex,
    string TargetId,
    float AppliedAmount,
    float EffectiveAmount);

public sealed record EffectStepResult(
    int StepIndex,
    string TargetId,
    EffectKind Kind,
    EffectExecutionStatus Status,
    EffectInterruptionReason Interruption,
    float AppliedAmount,
    float EffectiveAmount,
    EffectDomainEvent? DomainEvent,
    string Message);

public sealed record EffectInvocationResult(
    long InvocationSequence,
    EffectInvocationContext Context,
    string BindingId,
    EffectExecutionStatus Status,
    EffectInterruptionReason Interruption,
    ImmutableArray<EffectStepResult> Steps,
    string Message)
{
    public float FirstAppliedAmount => Steps.FirstOrDefault(step => step.Status == EffectExecutionStatus.Succeeded)?.AppliedAmount ?? 0;
}

public sealed record EffectEnqueueResult(
    bool Accepted,
    long InvocationSequence,
    EffectInterruptionReason Interruption,
    string Message);

public sealed record EffectQueueDrainResult(
    EffectExecutionStatus Status,
    EffectInterruptionReason Interruption,
    ImmutableArray<EffectInvocationResult> Invocations)
{
    public EffectInvocationResult? Find(long invocationSequence) =>
        Invocations.FirstOrDefault(result => result.InvocationSequence == invocationSequence);
}

public sealed record EffectPreflightResult(
    bool Succeeded,
    EffectInterruptionReason Interruption,
    string Message,
    int PreparedStepCount)
{
    public static EffectPreflightResult Success(int preparedStepCount) =>
        new(true, EffectInterruptionReason.None, string.Empty, preparedStepCount);

    public static EffectPreflightResult Rejected(EffectInterruptionReason reason, string message) =>
        new(false, reason, message, 0);
}

public sealed record EffectTraceEntry(
    long TraceSequence,
    EffectOrderingKey Ordering,
    EffectInvocationContext Context,
    string BindingId,
    int StepIndex,
    string TargetId,
    EffectKind? Kind,
    EffectExecutionStatus Status,
    EffectInterruptionReason Interruption,
    float AppliedAmount,
    float EffectiveAmount,
    string Message);

public sealed record EffectTransitionValidation(bool IsValid, ImmutableArray<string> Errors);

public sealed record BattleScopeTransitionResult(
    string ScopeId,
    BattleScopeCompletionReason Reason,
    int FinalTick,
    ImmutableArray<EffectDomainEvent> Events,
    ImmutableArray<EffectTraceEntry> Trace,
    int RemainingSubscriptions,
    int RemainingInvocations,
    int RemainingRuntimeInstances)
{
    public EffectTransitionValidation Validate()
    {
        var errors = ImmutableArray.CreateBuilder<string>();
        if (string.IsNullOrWhiteSpace(ScopeId)) errors.Add("Transition scope id is required.");
        if (Reason == BattleScopeCompletionReason.None) errors.Add("Transition completion reason is required.");
        if (FinalTick < 0) errors.Add("Transition final tick cannot be negative.");
        if (RemainingSubscriptions != 0) errors.Add("Transition retained subscriptions.");
        if (RemainingInvocations != 0) errors.Add("Transition retained pending invocations.");
        if (RemainingRuntimeInstances != 0) errors.Add("Transition retained mutable runtime instances.");
        if (Events.Any(effectEvent => effectEvent.Context.ScopeId != ScopeId))
            errors.Add("Transition contains an event from another scope.");
        if (Trace.Any(entry => entry.Context.ScopeId != ScopeId))
            errors.Add("Transition contains trace evidence from another scope.");
        if (Events.Where((effectEvent, index) => index > 0 && effectEvent.Sequence <= Events[index - 1].Sequence).Any())
            errors.Add("Transition event sequence is not strictly increasing.");
        if (Trace.Where((entry, index) => index > 0 && entry.TraceSequence <= Trace[index - 1].TraceSequence).Any())
            errors.Add("Transition trace sequence is not strictly increasing.");
        if (Events.Any(effectEvent => !float.IsFinite(effectEvent.AppliedAmount) || effectEvent.AppliedAmount < 0 ||
                                      !float.IsFinite(effectEvent.EffectiveAmount) || effectEvent.EffectiveAmount < 0))
            errors.Add("Transition contains invalid resolved event amounts.");
        return new EffectTransitionValidation(errors.Count == 0, errors.ToImmutable());
    }
}
