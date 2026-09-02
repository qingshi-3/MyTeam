using System.Collections.Immutable;

namespace TowerAutobattler.Effects;

public sealed record CompiledEffectTrigger(
    EffectTriggerKind Kind,
    EffectDomainEventKind EventKind);

public abstract record CompiledEffectCondition;

public sealed record CompiledEntityAliveCondition(
    EffectEntityReference Entity,
    bool ExpectedAlive) : CompiledEffectCondition;

public abstract record CompiledEffectTargetQuery;

public sealed record CompiledExplicitTargetQuery : CompiledEffectTargetQuery;
public sealed record CompiledSourceTargetQuery : CompiledEffectTargetQuery;
public sealed record CompiledOwnerTargetQuery : CompiledEffectTargetQuery;

public sealed record CompiledRelativeTeamTargetQuery(
    EffectRelativeTeam Team,
    bool IncludeDefeated,
    string RequiredTag = "") : CompiledEffectTargetQuery;

public sealed record CompiledEffectStep(
    EffectKind Kind,
    EffectAmountSource AmountSource,
    float Amount);

public sealed record CompiledEffectBindingLimits(
    int MaxUses,
    int MinimumIntervalTicks,
    int MaxDepth,
    int MaxRepeatedEdges);

public sealed record CompiledEffectPresentation(
    string DisplayName,
    string ReportLabel,
    string Cue);

public sealed record CompiledEffectBinding(
    string StableId,
    int Priority,
    CompiledEffectTrigger Trigger,
    ImmutableArray<CompiledEffectCondition> Conditions,
    CompiledEffectTargetQuery TargetQuery,
    ImmutableArray<CompiledEffectStep> Effects,
    CompiledEffectBindingLimits Limits,
    CompiledEffectPresentation? Presentation);
