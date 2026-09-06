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
                new("risky", "冒险开启", $"{rules.RiskyEventSuccessChance:P0} 概率获得 {rules.RiskyEventSuccessGold} 金币；失败损失 {rules.RiskyEventHealthLoss:P0} 生命比例。", [], [],
                    [new(RunOperationKind.GainGold, rules.RiskyEventSuccessGold)], rules.RiskyEventSuccessChance,
                    [new(RunOperationKind.RecoverRoster, Ratio: -rules.RiskyEventHealthLoss, MinimumHealthRatio: rules.RiskyEventMinimumHealth)]),
                new("safe", "谨慎绕行", $"获得 {rules.SafeEventGold} 金币。", [], [], [new(RunOperationKind.GainGold, rules.SafeEventGold)], FailureOperations: [])
            ]));
        if (!offers.ContainsKey(RunOfferKind.Rest))
            offers.Add(RunOfferKind.Rest, new CompiledRunOffer("default_rest", RunOfferKind.Rest, "队伍休整", true, false, null, default, 0,
            [
                new("recover", "全军休整", $"起始英雄恢复 {rules.RestHeroHealing:P0}，其余英雄恢复 {rules.RestSoldierHealing:P0}。", [], [],
                    [new(RunOperationKind.RecoverRoster, Ratio: rules.RestHeroHealing, Target: RunRosterTarget.StartingHero),
                     new(RunOperationKind.RecoverRoster, Ratio: rules.RestSoldierHealing, Target: RunRosterTarget.OtherHeroes)], FailureOperations: []),
                new("gold", "整理战利品", $"获得 {rules.RestGold} 金币。", [], [], [new(RunOperationKind.GainGold, rules.RestGold)], FailureOperations: [])
            ]));
        return campaign with { RunOffers = offers.ToImmutable() };
    }
}
