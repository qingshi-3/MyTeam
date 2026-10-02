using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;

namespace TowerAutobattler.Run;

// Called only on detached run projections. The outer node / choice transaction owns publication.
public sealed class RunRelicRules(ContentRegistry content)
{
    public IEnumerable<(ItemInstanceDto Item, CompiledRelicRunRule Rule)> Rules(ActiveRunDto run) =>
        run.Items.ToArray().SelectMany(item =>
        {
            var rules = content.Graph.ResolveRelic(item.ContentId).RunRules;
            return (rules.IsDefault ? [] : rules).Select(rule => (item, rule));
        });

    public void GainGold(ActiveRunDto run, int amount)
    {
        if (amount <= 0) return;
        run.Gold = checked(run.Gold + amount);
        foreach (var (item, rule) in Rules(run).Where(x => x.Rule.Kind == RelicRunRuleKind.GoldGainHealing))
            run.CurrentRunHealth = RunHealthPolicy.Recover(run, checked((int)rule.Amount * item.Stacks));
    }

    public void EnterNode(ActiveRunDto run, TowerNodeType type)
    {
        foreach (var (item, rule) in Rules(run).ToArray())
        {
            if (rule.Kind == RelicRunRuleKind.NodeGoldUntilPurchase && item.Charges == 0)
                GainGold(run, checked((int)rule.Amount * item.Stacks));
            if (rule.Kind == RelicRunRuleKind.ShopHealing && type == TowerNodeType.Shop)
                run.CurrentRunHealth = RunHealthPolicy.Recover(run, checked((int)rule.Amount * item.Stacks));
        }
    }

    public void PaidShopPurchase(ActiveRunDto run)
    {
        foreach (var (item, _) in Rules(run).Where(x => x.Rule.Kind == RelicRunRuleKind.NodeGoldUntilPurchase))
            item.Charges = 1;
    }

    public PendingRunOffer PresentOffer(ActiveRunDto run, PendingRunOffer offer)
    {
        if (offer.Kind != RunOfferKind.Shop) return offer;
        var factor = Rules(run).Where(x => x.Rule.Kind == RelicRunRuleKind.ShopPriceMultiplier)
            .Aggregate(1d, (value, x) => value * Math.Pow(x.Rule.Amount, x.Item.Stacks));
        if (factor == 1d) return offer;
        return offer with { Choices = offer.Choices.Select(choice => choice with
        {
            Costs = choice.Costs.Select(cost => cost.Kind == RunOperationKind.SpendGold
                ? cost with { Amount = cost.Amount == 0 ? 0 : Math.Max(1, (int)Math.Ceiling(cost.Amount * factor)) } : cost).ToImmutableArray()
        }).ToImmutableArray() };
    }

    public string SkipText(ActiveRunDto run, PendingRunOffer offer)
    {
        var rewards = new List<string>();
        foreach (var (item, rule) in Rules(run))
        {
            if (offer.Kind == RunOfferKind.CombatReward && rule.Kind == RelicRunRuleKind.RewardSkipMaximumHealth)
                rewards.Add($"最大征程生命 +{(int)rule.Amount * item.Stacks}");
            if (offer.Kind == RunOfferKind.CombatReward && offer.RewardCategory == RunRewardCategory.Equipment && rule.Kind == RelicRunRuleKind.EquipmentSkipGold)
                rewards.Add($"回收 +{(int)rule.Amount * item.Stacks} 金币");
        }
        return rewards.Count == 0 ? "跳过" : "放弃本组奖励：" + string.Join("，", rewards);
    }

    public void SkipReward(ActiveRunDto run, PendingRunOffer offer)
    {
        if (offer.Kind != RunOfferKind.CombatReward) return;
        foreach (var (item, rule) in Rules(run).ToArray())
        {
            if (rule.Kind == RelicRunRuleKind.RewardSkipMaximumHealth)
            {
                var amount = checked((int)rule.Amount * item.Stacks);
                run.MaximumRunHealth = checked(run.MaximumRunHealth + amount);
                run.CurrentRunHealth = checked(run.CurrentRunHealth + amount);
            }
            if (rule.Kind == RelicRunRuleKind.EquipmentSkipGold && offer.RewardCategory == RunRewardCategory.Equipment)
                GainGold(run, checked((int)rule.Amount * item.Stacks));
        }
    }

    public bool SettleBattle(ActiveRunDto run, BattleResult result, EncounterPlan encounter, int heldGold,
        Func<ActiveRunDto, string, bool> grant)
    {
        // Freeze the source set: a newly awarded relic cannot reward its own acquisition battle.
        foreach (var (item, rule) in Rules(run).ToArray())
        {
            if (rule.Kind == RelicRunRuleKind.VictoryInterest && result.Outcome == BattleOutcome.PlayerVictory && run.BattleNumber <= rule.Limit)
                GainGold(run, checked((int)Math.Floor(Math.Max(0, heldGold - result.GoldSpent) * (double)rule.Amount * item.Stacks)));
            var draws = rule.Kind switch
            {
                RelicRunRuleKind.EliteReward when encounter.IsElite && result.Outcome == BattleOutcome.PlayerVictory => (int)rule.Amount * item.Stacks,
                RelicRunRuleKind.CounterReward => checked((result.RelicTransition?.CounterTransitions
                    .Where(c => c.InstanceId == item.InstanceId && c.CounterId == rule.CounterId).Sum(c => c.ThresholdExecutions) ?? 0) * (int)rule.Amount * item.Stacks),
                _ => 0
            };
            for (var draw = 0; draw < draws; draw++)
            {
                var eligible = rule.ContentIds.Where(id => RunItemSupplyPolicy.CanAcquire(run, id, content)).Order(StringComparer.Ordinal).ToArray();
                if (eligible.Length == 0) continue; // Exhausted unique-relic pools do not mint substitute currency.
                var key = $"{run.Seed:x16}:{run.FloorIndex}:{run.BattleNumber}:{item.InstanceId}:{rule.RuleId}:{rule.PoolId}:{draw}";
                var roll = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
                if (!grant(run, eligible[roll % eligible.Length])) return false;
            }
        }
        return true;
    }

    public float EliteHealthRatio(ActiveRunDto run) => (float)Rules(run)
        .Where(x => x.Rule.Kind == RelicRunRuleKind.EliteStartingHealthMultiplier)
        .Aggregate(1d, (ratio, x) => ratio * Math.Pow(x.Rule.Amount, x.Item.Stacks));
}
