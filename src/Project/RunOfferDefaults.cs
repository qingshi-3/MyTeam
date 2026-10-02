using System;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

namespace TowerAutobattler.Project;

// Compatibility recipes freeze the current product rules into the same grammar
// as authored offers. Runtime never dispatches a rest/event-specific mutation.
public static class RunOfferDefaults
{
    // Each category owns its draw and claim slot. Recipes outside an ordinary
    // one-item reward remain an explicit separate opportunity rather than being
    // silently duplicated or decomposed into invented entitlements.
    public static ImmutableArray<CompiledRunOffer> ItemRewardGroups(
        CompiledRunOffer definition, Func<string, CatalogEntry> entry)
    {
        if (definition.Repeatable) return [definition];
        RunRewardCategory Category(CompiledRunChoice choice)
        {
            if (choice.SuccessChance != 1 || !choice.FailureOperations.IsDefaultOrEmpty ||
                choice.Operations.Length != 1 ||
                choice.Operations[0] is not { Kind: RunOperationKind.GrantItem, Amount: 1 } operation ||
                entry(operation.ContentId).Definition is not ItemDefinition item)
                return RunRewardCategory.None;
            return item.ProductKind == ItemProductKind.Equipment ? RunRewardCategory.Equipment : RunRewardCategory.Relic;
        }
        var groups = ImmutableArray.CreateBuilder<CompiledRunOffer>();
        foreach (var category in new[] { RunRewardCategory.Equipment, RunRewardCategory.Relic, RunRewardCategory.None })
        {
            var fixedChoices = definition.Choices.Where(choice => Category(choice) == category).ToImmutableArray();
            var poolChoices = definition.PoolChoices.Where(choice => Category(choice) == category).ToImmutableArray();
            if (fixedChoices.IsEmpty && poolChoices.IsEmpty) continue;
            var suffix = category.ToString().ToLowerInvariant();
            groups.Add(definition with
            {
                StableId = definition.StableId + "_" + suffix,
                DisplayName = category switch
                {
                    RunRewardCategory.Equipment => "战斗胜利 · 装备奖励",
                    RunRewardCategory.Relic => "战斗胜利 · 遗物奖励",
                    _ => definition.DisplayName
                },
                AllowSkip = category == RunRewardCategory.None ? definition.AllowSkip : true,
                Choices = fixedChoices,
                PoolChoices = poolChoices,
                RewardCategory = category
            });
        }
        return groups.ToImmutable();
    }

    // Keep frozen opportunities and rewards. Retire hero-wound choices and extend an
    // unclaimed legacy camp with the distinct Run-health choice, without save-on-read.
    public static PendingRunOffer RefreshHealthChoices(PendingRunOffer offer, CompiledRunRules? rules = null)
    {
        if (offer.Kind == RunOfferKind.Rest && offer.OfferId.EndsWith(":default_rest", StringComparison.Ordinal))
        {
            var choices = offer.Choices.Where(choice => choice.StableId != "recover").ToImmutableArray();
            if (rules is not null && choices.All(choice => choice.StableId != "recover_run_health"))
                choices = choices.Add(RunHealthRecovery(rules));
            return offer with { Choices = choices };
        }
        if (offer.Kind != RunOfferKind.Event || !offer.OfferId.EndsWith(":default_event", StringComparison.Ordinal))
            return offer;
        return offer with
        {
            Choices = offer.Choices.Select(choice => choice.StableId == "risky" &&
                choice.FailureOperations.Any(operation => operation.Kind == RunOperationKind.RecoverRoster)
                ? choice with
                {
                    Description = $"{choice.SuccessChance:P0} 概率获得 {choice.Operations.Where(operation => operation.Kind == RunOperationKind.GainGold).Sum(operation => operation.Amount)} 金币；失败无收益。",
                    FailureOperations = choice.FailureOperations.Where(operation => operation.Kind != RunOperationKind.RecoverRoster).ToImmutableArray()
                } : choice).ToImmutableArray()
        };
    }

    private static CompiledRunChoice RunHealthRecovery(CompiledRunRules rules) =>
        new("recover_run_health", "休养整顿", $"恢复 {rules.RestRunHealthRecovery} 全局生命，与领取军费二选一。",
            [new(RunConditionKind.RunHealthBelowMaximum)], [],
            [new(RunOperationKind.RecoverRunHealth, rules.RestRunHealthRecovery)], FailureOperations: []);

    public static CompiledCampaign WithDefaults(CompiledCampaign campaign, CompiledRunRules rules, Func<string, CatalogEntry> entry)
    {
        var offers = campaign.RunOffers.ToBuilder();
        void AddPool(RunOfferKind kind, string title, CompiledContentPool pool, RunPoolAction action, int count, bool repeatable = false)
        {
            if (offers.ContainsKey(kind)) return;
            var choices = pool.ContentIds.Select(id =>
            {
                var definition = entry(id).Definition;
                var name = definition is UnitDefinition unit ? unit.DisplayName : ((ItemDefinition)definition).DisplayName;
                var description = definition is UnitDefinition unitDefinition ? unitDefinition.Description : ((ItemDefinition)definition).Description;
                return new CompiledRunChoice(id, name, description, [], action == RunPoolAction.BuyItem
                        ? [new CompiledRunOperation(RunOperationKind.SpendGold, ((ItemDefinition)definition).Price)] : [],
                    [new CompiledRunOperation(action == RunPoolAction.Recruit ? RunOperationKind.Recruit : RunOperationKind.GrantItem, 1, ContentId: id)],
                    FailureOperations: [], ContentId: id);
            }).ToImmutableArray();
            offers.Add(kind, new CompiledRunOffer("default_" + kind.ToString().ToLowerInvariant(), kind, title, true, repeatable,
                pool, action, count, []) { PoolChoices = choices });
        }
        AddPool(RunOfferKind.Recruitment, "英雄征募", campaign.RecruitmentPool, RunPoolAction.Recruit, rules.RecruitmentChoiceCount);
        if (!campaign.RunOffers.ContainsKey(RunOfferKind.Recruitment))
            offers[RunOfferKind.Recruitment] = offers[RunOfferKind.Recruitment] with
            {
                Choices = rules.StartingHeroEconomy.Where(pair => pair.Value.RecruitConversionGold > 0).Select(pair =>
                    new CompiledRunChoice("conversion_" + pair.Key, "将征募机会换为金币", $"放弃此次征募，获得 {pair.Value.RecruitConversionGold} 金币。",
                        [new(RunConditionKind.StartingHeroIs, ContentId: pair.Key)], [],
                        [new(RunOperationKind.GainGold, pair.Value.RecruitConversionGold)], FailureOperations: [])).ToImmutableArray()
            };
        AddPool(RunOfferKind.Shop, "旅途商店", campaign.ShopPool, RunPoolAction.BuyItem, rules.ItemChoiceCount, true);
        AddPool(RunOfferKind.CombatReward, "战斗胜利 · 战利品", campaign.ItemRewardPool, RunPoolAction.GrantItem, rules.ItemChoiceCount);
        if (!offers.ContainsKey(RunOfferKind.Event))
            offers.Add(RunOfferKind.Event, new CompiledRunOffer("default_event", RunOfferKind.Event, "旅途抉择", true, false, null, default, 0,
            [
                new("risky", "冒险开启", $"{rules.RiskyEventSuccessChance:P0} 概率获得 {rules.RiskyEventSuccessGold} 金币；失败无收益。", [], [],
                    [new(RunOperationKind.GainGold, rules.RiskyEventSuccessGold)], rules.RiskyEventSuccessChance,
                    []),
                new("safe", "谨慎绕行", $"获得 {rules.SafeEventGold} 金币。", [], [], [new(RunOperationKind.GainGold, rules.SafeEventGold)], FailureOperations: [])
            ]));
        if (!offers.ContainsKey(RunOfferKind.Rest))
            offers.Add(RunOfferKind.Rest, new CompiledRunOffer("default_rest", RunOfferKind.Rest, "队伍休整", true, false, null, default, 0,
            [
                RunHealthRecovery(rules),
                new("gold", "整理战利品", $"获得 {rules.RestGold} 金币。", [], [], [new(RunOperationKind.GainGold, rules.RestGold)], FailureOperations: [])
            ]));
        return campaign with { RunOffers = offers.ToImmutable() };
    }
}
