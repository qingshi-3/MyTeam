using System.Collections.Immutable;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Relics;

public sealed record CompiledRelicBattleModifier(
    RelicBattleModifierKind Kind,
    float Amount,
    string ContentId);

public sealed record CompiledRelicRunOutcome(
    RelicRunOutcomeKind Kind,
    int Amount);

public abstract record CompiledRelicUnitTarget;
public sealed record CompiledRelicPlayerArmyTarget : CompiledRelicUnitTarget;
public sealed record CompiledRelicPlayerHeroesTarget : CompiledRelicUnitTarget;
public sealed record CompiledRelicPlayerFormationAdjacentTarget : CompiledRelicUnitTarget;
public sealed record CompiledRelicPlayerEmptySlotHeroesTarget : CompiledRelicUnitTarget;

public sealed record CompiledRelicAttributeBinding(
    string BindingId,
    CompiledRelicUnitTarget Target,
    RelicAttributeStackPolicy StackPolicy,
    CompiledAttributeModifier Modifier);

public abstract record CompiledRelicBattleStartEffect(
    string BindingId,
    RelicBattleStartRepeatPolicy RepeatPolicy);

public sealed record CompiledRelicBattleStartShield(
    string BindingId,
    RelicBattleStartRepeatPolicy RepeatPolicy,
    int Amount,
    CompiledEffectBinding Effect) : CompiledRelicBattleStartEffect(BindingId, RepeatPolicy);

public sealed record CompiledRelicBattleStartSummon(
    string BindingId,
    RelicBattleStartRepeatPolicy RepeatPolicy,
    string ContentId,
    float HealthMultiplier,
    float DamageMultiplier) : CompiledRelicBattleStartEffect(BindingId, RepeatPolicy);

public sealed record CompiledRelicReactiveCounter(
    string CounterId,
    RelicCounterScope Scope,
    RelicCounterResetPolicy ResetPolicy,
    RelicCounterSourceKind Source,
    BattleCombatEventKind EventKind,
    int Team,
    bool IncludeTemporary,
    int Threshold,
    int Consumption,
    int Priority,
    RelicThresholdTargetKind Target,
    int TargetTeam,
    CompiledEffectBinding ThresholdEffect);

public sealed record CompiledRelicDefinition(
    string StableId,
    string ResourcePath,
    ImmutableArray<CompiledRelicAttributeBinding> AttributeBindings,
    ImmutableArray<CompiledRelicBattleStartEffect> BattleStartEffects,
    ImmutableArray<CompiledRelicBattleModifier> BattleModifiers,
    ImmutableArray<CompiledRelicReactiveCounter> ReactiveCounters,
    ImmutableArray<CompiledRelicRunOutcome> VictoryOutcomes,
    string Fingerprint);

public sealed record RelicCounterStateSnapshot(string CounterId, int Value);

public sealed record RelicRunKey(
    ulong Seed,
    string HeroId,
    int FloorIndex,
    int BattleNumber);

public sealed record RelicRunInstanceSnapshot(
    string InstanceId,
    string ContentId,
    int Stacks,
    int Charges,
    int Roll,
    ImmutableArray<RelicCounterStateSnapshot> Counters = default);

public sealed record RelicBattleInstanceSnapshot(
    string InstanceId,
    string ContentId,
    int Stacks,
    int Charges,
    int Roll,
    ImmutableArray<RelicCounterStateSnapshot> Counters,
    CompiledRelicDefinition Definition);

public sealed record RelicBattleModifierSnapshot(
    float ArmyHealthMultiplier = 1f,
    float ArmyDamageMultiplier = 1f,
    float HeroHealthMultiplier = 1f,
    float HeroDamageMultiplier = 1f,
    float ArmyLifeStealBonus = 0f,
    float HeroLifeStealBonus = 0f,
    int StartBattleShield = 0,
    int EmptySlotPower = 0,
    bool SummonToken = false,
    float FormationAdjacentArmor = 0f,
    float FormationAdjacentDamageMultiplier = 1f,
    string SummonContentId = "");

public sealed record RelicBattlePreparation(
    string TransitionId,
    RelicRunKey RunKey,
    string SourceFingerprint,
    ImmutableArray<RelicBattleInstanceSnapshot> Instances,
    RelicBattleModifierSnapshot Modifiers);

public sealed record RelicCounterTransitionSnapshot(
    long Sequence,
    string InstanceId,
    string CounterId,
    BattleCombatEventKind EventKind,
    long EventSequence,
    int PreviousValue,
    int Increment,
    int ThresholdExecutions,
    int CurrentValue);

public enum RelicBattleCompletionReason
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

public sealed record RelicRunOutcomeContribution(
    string InstanceId,
    string ContentId,
    RelicRunOutcomeKind Kind,
    int Amount);

public sealed record RelicBattleTransitionResult(
    string TransitionId,
    RelicRunKey RunKey,
    string SourceFingerprint,
    RelicBattleCompletionReason Reason,
    ImmutableArray<RelicRunInstanceSnapshot> ProjectedInstances,
    ImmutableArray<RelicCounterTransitionSnapshot> CounterTransitions,
    ImmutableArray<RelicRunOutcomeContribution> Contributions,
    int GoldDelta,
    int RemainingBattleInstances,
    int RemainingCounters,
    int RemainingSubscriptions,
    int RemainingModifierHandles);

public sealed record RelicRunApplyResult(
    bool Succeeded,
    string FailureReason,
    string TransitionId,
    int GoldDelta,
    ImmutableArray<RelicRunInstanceSnapshot> ProjectedInstances)
{
    public static RelicRunApplyResult Failed(string transitionId, string reason) =>
        new(false, reason, transitionId, 0, []);
}

public enum RelicRunCompletionReason
{
    None,
    BattlePrepared,
    TransitionApplied,
    Abort,
    Replacement,
    Exception,
    Disposal
}

public sealed record RelicRunScopeTransitionResult(
    RelicRunKey RunKey,
    RelicRunCompletionReason Reason,
    int RemainingRunInstances);
