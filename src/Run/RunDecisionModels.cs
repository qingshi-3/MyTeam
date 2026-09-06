using System.Collections.Immutable;
using TowerAutobattler.Project;

namespace TowerAutobattler.Run;

public sealed record CompiledStartingHeroEconomy(int BattleGoldBonus, int RecruitConversionGold);

public sealed record CompiledRunOperation(RunOperationKind Kind, int Amount = 0, float Ratio = 0,
    string ContentId = "", string SourceId = "", RunRosterTarget Target = RunRosterTarget.All, float MinimumHealthRatio = 0);
public sealed record CompiledRunCondition(RunConditionKind Kind, int Amount = 0, float Ratio = 0, string ContentId = "");
public sealed record CompiledRunChoice(string StableId, string DisplayName, string Description,
    ImmutableArray<CompiledRunCondition> Conditions, ImmutableArray<CompiledRunOperation> Costs,
    ImmutableArray<CompiledRunOperation> Operations, float SuccessChance = 1,
    ImmutableArray<CompiledRunOperation> FailureOperations = default, string ContentId = "");
public sealed record CompiledRunOffer(string StableId, RunOfferKind Kind, string DisplayName, bool AllowSkip,
    bool Repeatable, CompiledContentPool? Pool, RunPoolAction PoolAction, int PoolChoiceCount,
    ImmutableArray<CompiledRunChoice> Choices)
{
    public ImmutableArray<CompiledRunChoice> PoolChoices { get; init; } = [];
}

public sealed record PendingRunOffer(string OfferId, RunOfferKind Kind, string DisplayName,
    bool AllowSkip, bool Repeatable, int FloorIndex, int BattleNumber, ImmutableArray<CompiledRunChoice> Choices);
public enum RunDecisionFailure { None, NoOffer, StaleOffer, UnknownChoice, NotEligible, InsufficientResources, InvalidOperation, PersistenceFailed }
public enum RunChangeKind { Gold, Population, PopulationCap, HeroAdded, ItemAdded, HeroHealth }
public sealed record RunDecisionChange(RunChangeKind Kind, string SubjectId, string ContentId, double Before, double After);
public sealed record RunDecisionResult(bool Succeeded, RunDecisionFailure Failure, string Message, bool ChanceSucceeded = true)
{
    public ImmutableArray<RunDecisionChange> Changes { get; init; } = [];
    public static RunDecisionResult Reject(RunDecisionFailure failure, string message) => new(false, failure, message);
}
