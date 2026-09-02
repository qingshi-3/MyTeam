using System;
using System.Collections.Immutable;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Statuses;

public sealed record CompiledStatusPresentation(
    string SemanticIcon,
    string ExecutedCue,
    string OnActiveCue,
    string WhileActiveCue,
    string RemovedCue,
    string ReportLabel);

public sealed record CompiledStatusLifecycleBinding(
    StatusLifecycleTriggerKind Trigger,
    CompiledEffectBinding Binding);

public sealed record CompiledStatusCombatReactiveBinding(
    BattleCombatEventKind EventKind,
    StatusReactiveOwnerRole OwnerRole,
    StatusReactiveEffectSourcePolicy EffectSourcePolicy,
    int Priority,
    CompiledEffectBinding Binding);

public sealed record CompiledStatusTransition(
    CompiledStatusDefinition Target,
    int ConsumeStacks);

public sealed record CompiledStatusDefinition(
    string StableId,
    string ResourcePath,
    string DisplayName,
    string Description,
    StatusBehaviorKind Behavior,
    StatusDisposition Disposition,
    StatusDurationKind DurationKind,
    int DurationTicks,
    StatusAggregationPolicy AggregationPolicy,
    int StackLimit,
    StatusOverflowPolicy OverflowPolicy,
    StatusDurationRefreshPolicy DurationRefreshPolicy,
    StatusPeriodicResetPolicy PeriodicResetPolicy,
    StatusDispelCategory DispelCategory,
    StatusDeathPolicy DeathPolicy,
    StatusControlDurationRule ControlDurationRule,
    ImmutableArray<string> GrantedTags,
    ImmutableArray<CompiledAttributeModifier> AttributeModifiers,
    float Magnitude,
    int PeriodicIntervalTicks,
    CompiledEffectBinding? PeriodicEffect,
    ImmutableArray<CompiledStatusLifecycleBinding> LifecycleBindings,
    ImmutableArray<CompiledStatusCombatReactiveBinding> CombatReactiveBindings,
    CompiledStatusTransition? OverflowTransition,
    CompiledStatusPresentation? Presentation);

public sealed record StatusSourceContributionSnapshot(
    string SourceId,
    long ApplicationSequence,
    int AppliedTick,
    int Stacks);

public sealed record StatusCapturedMagnitudeSnapshot(
    string SourceId,
    long StackApplicationSequence,
    int StackAppliedTick,
    int ModifierIndex,
    CombatAttribute Attribute,
    string SlotId,
    float Value);

public sealed record StatusRuntimeSnapshot(
    string StableId,
    string DisplayName,
    string Description,
    string SourceId,
    string OwnerId,
    string InstanceId,
    long ApplicationSequence,
    int AppliedTick,
    int LastAppliedTick,
    int Stacks,
    int RemainingTicks,
    bool Permanent,
    bool Dispellable,
    StatusDurationKind DurationKind,
    StatusAggregationPolicy AggregationPolicy,
    StatusDispelCategory DispelCategory,
    StatusDeathPolicy DeathPolicy,
    StatusBehaviorKind Behavior,
    StatusDisposition Disposition,
    float Magnitude,
    ImmutableArray<string> GrantedTags,
    ImmutableArray<StatusSourceContributionSnapshot> SourceContributions,
    ImmutableArray<StatusCapturedMagnitudeSnapshot> CapturedMagnitudes,
    string SemanticIcon,
    string ReportLabel);

public sealed record StatusModifierProjectionSnapshot(
    string StatusInstanceId,
    string SourceId,
    long StackApplicationSequence,
    int ModifierIndex,
    AttributeModifierHandle Handle);

public sealed record StatusMagnitudeContextRequest(
    CompiledStatusDefinition Definition,
    string SourceId,
    string OwnerId,
    long StackApplicationSequence,
    int StackAppliedTick,
    BattleAttributeSet SourceAttributes,
    BattleAttributeSet TargetAttributes);

public sealed record StatusApplicationResult(
    bool Applied,
    string FailureReason,
    StatusRuntimeSnapshot? Status,
    int AddedStacks,
    bool OverflowTriggered = false);

public sealed record StatusApplicationRequest(
    CompiledStatusDefinition Definition,
    string SourceId,
    string OwnerId,
    int Tick);

public enum StatusEffectInvocationKind { Periodic, Applied, StackChanged, Removed, Reactive }

public sealed record StatusEffectInvocation(
    StatusEffectInvocationKind Kind,
    CompiledStatusDefinition Definition,
    string SourceId,
    string OwnerId,
    string InstanceId,
    string ExplicitTargetId,
    int Tick,
    CompiledEffectBinding Binding,
    StatusRemovalReason RemovalReason,
    BattleCombatEvent? CombatEvent = null);

public sealed record StatusCombatReactiveSubscriptionRequest(
    BattleCombatEventKind EventKind,
    CombatSourceRef Source,
    int Priority,
    Action<BattleCombatEvent, BattleCombatReactionSink> Listener);

public sealed record StatusPeriodicInvocation(
    CompiledStatusDefinition Definition,
    string SourceId,
    string OwnerId,
    int Tick,
    CompiledEffectBinding Binding);

public sealed record StatusAdvanceResult(
    bool ActionsDisabled,
    ImmutableArray<StatusRuntimeSnapshot> Expired,
    int PeriodicInvocations,
    ImmutableArray<string> ActiveTags);

public enum StatusLifecycleKind { Applied, StackChanged, Removed }
public enum StatusRemovalReason
{
    None,
    InstantExecuted,
    Expired,
    OrdinaryDispelled,
    StrongDispelled,
    OverflowConsumed,
    OwnerDied,
    OwnerRemoved,
    ScopeCompleted
}

public enum StatusDispelStrength { Ordinary, Strong }

public sealed record StatusLifecycleEvent(
    StatusLifecycleKind Kind,
    StatusRuntimeSnapshot Status,
    int PreviousStacks,
    int CurrentStacks,
    StatusRemovalReason RemovalReason,
    int Tick);

public enum StatusPresentationCueLifecycle { Executed, OnActive, WhileActive, Removed }

public sealed record StatusPresentationCue(
    StatusPresentationCueLifecycle Lifecycle,
    string Cue,
    StatusRuntimeSnapshot Status,
    StatusRemovalReason RemovalReason,
    int Tick);

public static class StatusControlDuration
{
    // Deterministic control rule: ceil(authored ticks * (1 - clamped resistance)),
    // with a one-tick floor so finite authored control is never rounded to zero.
    public static int ResolveTicks(
        CompiledStatusDefinition definition,
        BattleAttributeSet? targetAttributes)
    {
        if (definition.DurationKind != StatusDurationKind.TimedTicks ||
            definition.ControlDurationRule == StatusControlDurationRule.None)
            return definition.DurationTicks;
        var resistance = targetAttributes?.GetValue(CombatAttribute.ControlResistance) ?? 0;
        resistance = Math.Clamp(resistance, 0, 1);
        return Math.Max(1, (int)Math.Ceiling(definition.DurationTicks * (1d - resistance)));
    }
}

public enum StatusScopeCompletionReason
{
    None,
    BattleCompleted,
    Abort,
    Replacement,
    Exception,
    Disposal
}

public sealed record StatusScopeTransitionResult(
    string ScopeId,
    StatusScopeCompletionReason Reason,
    int FinalTick,
    int RemainingInstances,
    int RemainingModifierHandles = 0,
    int RemainingContributions = 0,
    int RemainingReactiveSubscriptions = 0);
