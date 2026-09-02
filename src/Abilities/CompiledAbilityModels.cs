using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Abilities;

public sealed record CompiledAbilityPresentation(
    string SemanticIcon,
    string Cue,
    string ReportLabel);

public abstract record CompiledAbilityOperation;

public sealed record CompiledEffectAbilityOperation(
    CompiledEffectBinding Binding,
    AbilityInvocationValueSource InvocationValueSource,
    float InvocationValueScale) : CompiledAbilityOperation;

public sealed record CompiledCooldownAbilityOperation(
    CompiledEffectTargetQuery TargetQuery,
    CooldownAdjustmentKind AttackAdjustment,
    int AttackValue,
    CooldownAdjustmentKind MoveAdjustment,
    int MoveValue) : CompiledAbilityOperation;

public sealed record CompiledApplyStatusAbilityOperation(
    CompiledStatusDefinition Status,
    CompiledEffectTargetQuery TargetQuery) : CompiledAbilityOperation;

public sealed record CompiledSummonAbilityOperation(
    AbilitySummonProfile Profile,
    int Count,
    float HealthMultiplier,
    float DamageMultiplier,
    int MaximumLivingTemporaryUnits,
    bool RequireAtLeastOne,
    string SummonContentId) : CompiledAbilityOperation;

public sealed record CompiledAbilityDefinition(
    string StableId,
    string DisplayName,
    string Description,
    AbilityActivationKind ActivationKind,
    AbilityTriggerKind Trigger,
    int ManaCost,
    int GoldCost,
    int CooldownTicks,
    int MaxUses,
    int IntervalTicks,
    ImmutableArray<CompiledAbilityOperation> Operations,
    CompiledAbilityPresentation? Presentation);

public sealed record CompiledAbilityLoadout(
    ImmutableArray<CompiledAbilityDefinition> Abilities)
{
    public CompiledAbilityDefinition? Find(string stableId) =>
        Abilities.FirstOrDefault(ability => ability.StableId == stableId);
}

public sealed record AbilityEntitySnapshot(
    string RuntimeId,
    int Team,
    bool Alive,
    float MaxHealth,
    ImmutableArray<string> Tags);

public sealed record AbilityWorldSnapshot(
    int Tick,
    ImmutableDictionary<string, AbilityEntitySnapshot> Entities);

public sealed record ResolvedAbilityOperation(
    int OperationIndex,
    CompiledAbilityOperation Operation,
    ImmutableArray<string> TargetIds,
    float InvocationValue);

public sealed record AbilityExecutionPlan(
    CompiledAbilityDefinition Ability,
    string SourceId,
    string OwnerId,
    int Tick,
    ImmutableArray<ResolvedAbilityOperation> Operations,
    ImmutableArray<AbilitySummonReservation> Summons,
    int GoldCost);

public sealed record AbilitySummonReservation(
    int OperationIndex,
    AbilitySummonProfile Profile,
    int Sequence,
    int CellX,
    int CellY,
    float HealthMultiplier,
    float DamageMultiplier);

public sealed record AbilityPreparationResult(
    bool Succeeded,
    AbilityActivationFailure Failure,
    string FailureReason,
    AbilityExecutionPlan? Plan);

public sealed record AbilityCommitResult(
    bool Succeeded,
    AbilityActivationFailure Failure,
    string FailureReason,
    ImmutableArray<string> ResolvedFacts);

public enum AbilityActivationFailure
{
    None,
    ScopeCompleted,
    MissingAbility,
    WrongEntryPoint,
    SourceUnavailable,
    InsufficientMana,
    InsufficientGold,
    Cooldown,
    UsageLimit,
    ConditionsUnmet,
    CommitFailed
}

public sealed record AbilityActivationResult(
    bool Succeeded,
    AbilityActivationFailure Failure,
    string FailureReason,
    string AbilityId,
    int ManaSpent,
    int GoldSpent,
    ImmutableArray<string> ResolvedFacts);

public enum AbilityScopeCompletionReason
{
    None,
    BattleCompleted,
    Abort,
    Replacement,
    Exception,
    Disposal
}

public sealed record AbilityScopeTransitionResult(
    string ScopeId,
    AbilityScopeCompletionReason Reason,
    int FinalTick,
    int RemainingRuntimeInstances);
