using System;
using System.Linq;
using TowerAutobattler.Project;

namespace TowerAutobattler.Run;

public static class RunDecisionValidation
{
    public static bool ValidCondition(CompiledRunCondition? condition, Func<string, bool> hasContent) => condition is not null &&
        condition.ContentId is not null &&
        Enum.IsDefined(condition.Kind) && condition.Amount >= 0 && float.IsFinite(condition.Ratio) && condition.Ratio is >= 0 and <= 1 &&
        (condition.Kind is not (RunConditionKind.HasContent or RunConditionKind.StartingHeroIs) || hasContent(condition.ContentId));

    public static bool ValidOperation(CompiledRunOperation? operation, Func<string, bool> recruitable,
        Func<string, bool> item, bool cost = false)
    {
        if (operation is null || !Enum.IsDefined(operation.Kind) || !Enum.IsDefined(operation.Target) ||
            operation.ContentId is null || operation.SourceId is null ||
            operation.Amount < 0 || !float.IsFinite(operation.Ratio) || operation.Ratio is < -1 or > 1 ||
            !float.IsFinite(operation.MinimumHealthRatio) || operation.MinimumHealthRatio is < 0 or > 1) return false;
        if (cost && operation.Kind is not (RunOperationKind.SpendGold or RunOperationKind.SpendRunHealth) && !(operation.Kind == RunOperationKind.RecoverRoster && operation.Ratio < 0)) return false;
        return operation.Kind switch
        {
            RunOperationKind.Recruit => operation.Amount is > 0 and <= 64 && recruitable(operation.ContentId),
            RunOperationKind.GrantItem => operation.Amount is > 0 and <= 64 && item(operation.ContentId),
            RunOperationKind.GrantPopulation => operation.Amount > 0,
            RunOperationKind.RecoverRunHealth => operation.Amount > 0,
            RunOperationKind.SpendRunHealth => cost && operation.Amount > 0,
            RunOperationKind.RevealSites => !cost && operation.Amount is > 0 and <= 64,
            RunOperationKind.IncreasePopulationCap => operation.Amount > 0 && !string.IsNullOrWhiteSpace(operation.SourceId),
            _ => true
        };
    }

    public static bool ValidOffer(PendingRunOffer? offer, ActiveRunDto run,
        Func<string, bool> hasContent, Func<string, bool> recruitable, Func<string, bool> item)
    {
        if (offer is null) return true;
        if (run.Expedition is not null)
        {
            if (run.Action?.Source is not { } source || offer.Source != source || offer.FloorIndex != -1) return false;
        }
        else if (offer.Source is not null || offer.FloorIndex != run.FloorIndex) return false;
        return offer.BattleNumber == run.BattleNumber && ValidPayload(offer, hasContent, recruitable, item);
    }

    // A site's authored stock exists before an action and before any battle.
    // Activating it creates a separate immutable offer carrying that action's source.
    public static bool ValidStockOffer(PendingRunOffer? offer,
        Func<string, bool> hasContent, Func<string, bool> recruitable, Func<string, bool> item) =>
        offer is not null && offer.Source is null && offer.FloorIndex == -1 && offer.BattleNumber == 0 &&
        ValidPayload(offer, hasContent, recruitable, item);

    private static bool ValidPayload(PendingRunOffer offer,
        Func<string, bool> hasContent, Func<string, bool> recruitable, Func<string, bool> item)
    {
        if (offer.RecipeId is null || string.IsNullOrWhiteSpace(offer.OfferId) || string.IsNullOrWhiteSpace(offer.DisplayName) || !Enum.IsDefined(offer.Kind) ||
            !Enum.IsDefined(offer.RewardCategory) ||
            offer.RewardCategory != RunRewardCategory.None && offer.Kind != RunOfferKind.CombatReward ||
            offer.Choices.IsDefault ||
            offer.Choices.IsEmpty && !offer.AllowSkip ||
            offer.Repeatable && offer.Kind != RunOfferKind.Shop || offer.Kind == RunOfferKind.Shop && !offer.AllowSkip ||
            offer.Choices.Any(choice => choice is null) || offer.Choices.Select(choice => choice.StableId).Distinct().Count() != offer.Choices.Length)
            return false;
        return offer.Choices.All(choice => !string.IsNullOrWhiteSpace(choice.StableId) && !string.IsNullOrWhiteSpace(choice.DisplayName) &&
            choice.ContentId is not null && choice.Description is not null &&
            float.IsFinite(choice.SuccessChance) && choice.SuccessChance is >= 0 and <= 1 &&
            !choice.Conditions.IsDefault && !choice.Costs.IsDefault && !choice.Operations.IsDefault && !choice.FailureOperations.IsDefault &&
            choice.Conditions.All(condition => ValidCondition(condition, hasContent)) &&
            choice.Costs.All(operation => ValidOperation(operation, recruitable, item, true)) &&
            choice.Operations.All(operation => ValidOperation(operation, recruitable, item)) &&
            choice.FailureOperations.All(operation => ValidOperation(operation, recruitable, item)));
    }
}
