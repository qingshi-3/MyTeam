using System;
namespace TowerAutobattler.Relics;
public static class RelicRuleText
{
    public static string Describe(CompiledRelicRunRule rule) => rule.Kind switch
    {
        RelicRunRuleKind.ShopPriceMultiplier => $"商店价格 ×{rule.Amount:0.##}，向上取整，付费商品最低 1 金币。",
        RelicRunRuleKind.NodeGoldUntilPurchase => $"进入节点获得 {rule.Amount:0} 金币；首次在商店花费金币后永久失效。",
        RelicRunRuleKind.ShopHealing => $"每次到访商店恢复 {rule.Amount:0} 征程生命。",
        RelicRunRuleKind.GoldGainHealing => $"每次获得金币恢复 {rule.Amount:0} 征程生命。",
        RelicRunRuleKind.EliteReward => $"精英胜利后从独立遗物池抽取 {rule.Amount:0} 件未拥有的遗物；池耗尽不补发。",
        RelicRunRuleKind.RewardSkipMaximumHealth => $"跳过一组战后奖励：最大与当前征程生命各增加 {rule.Amount:0}。",
        RelicRunRuleKind.VictoryInterest => $"前 {rule.Limit} 场战斗胜利时，按扣除战斗支出后的持有金币获得 {rule.Amount:P0} 利息，向下取整；不计本场新奖励。",
        RelicRunRuleKind.GoldBattleHealth => $"每持有 1 金币，我方单位获得 {rule.Amount:0.##} 临时最大生命；金币减少时收益同步减少。",
        RelicRunRuleKind.EquipmentSkipGold => $"跳过一组装备奖励获得 {rule.Amount:0} 金币；不提供已有装备出售权限。",
        RelicRunRuleKind.EliteStartingHealthMultiplier => $"精英战初始敌人以 {rule.Amount:P0} 生命入场；最大生命不变，后续召唤不受影响。",
        RelicRunRuleKind.CounterReward => $"每次计数达标，战后从独立装备池抽取 {rule.Amount:0} 件装备；胜负均结算。",
        _ => throw new ArgumentOutOfRangeException(nameof(rule))
    };
}
