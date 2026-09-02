using System.Collections.Immutable;
using TowerAutobattler.Abilities;

namespace TowerAutobattler.TacticalCommands;

public sealed record CompiledTacticalCommandDefinition(
    string StableId,
    string ResourcePath,
    string DisplayName,
    string Description,
    int TacticalPointCost,
    CompiledAbilityDefinition Ability,
    string Fingerprint)
{
    public int GoldCost => Ability.GoldCost;
    public int CooldownTicks => Ability.CooldownTicks;
    public int MaxUses => Ability.MaxUses;
}

public sealed record TacticalCommandBattlePreparation(
    string SourceFingerprint,
    ImmutableArray<CompiledTacticalCommandDefinition> Commands);

public enum TacticalCommandActivationFailure
{
    None,
    ScopeCompleted,
    InvalidSlot,
    InsufficientTacticalPoints,
    Cooldown,
    UsageLimit,
    SourceUnavailable,
    InsufficientGold,
    PreflightFailed,
    CommitFailed
}

public sealed record TacticalCommandActivationResult(
    bool Succeeded,
    TacticalCommandActivationFailure Failure,
    string FailureReason,
    int SlotIndex,
    string CommandId,
    int TacticalPointsSpent,
    int GoldSpent,
    ImmutableArray<string> ResolvedFacts);

public sealed record TacticalCommandSlotSnapshot(
    int SlotIndex,
    string StableId,
    string DisplayName,
    string Description,
    int TacticalPointCost,
    int GoldCost,
    int CooldownTicks,
    int CooldownRemainingTicks,
    int MaxUses,
    int Uses,
    bool CanAttempt);

public sealed record BattleTacticalCommandSnapshot(
    int CurrentPoints,
    int MaximumPoints,
    ImmutableArray<TacticalCommandSlotSnapshot> Slots);

public enum TacticalCommandScopeCompletionReason
{
    None,
    BattleCompleted,
    Abort,
    Replacement,
    Exception,
    Disposal
}

public sealed record TacticalCommandScopeTransitionResult(
    string ScopeId,
    string SourceFingerprint,
    TacticalCommandScopeCompletionReason Reason,
    int FinalTick,
    int RemainingPoints,
    int RemainingRuntimeInstances);
