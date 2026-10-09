using Godot;
using TowerAutobattler.Project;

namespace TowerAutobattler.Relics;

public enum RelicRunRuleKind
{
    ShopPriceMultiplier, NodeGoldUntilPurchase, ShopHealing, GoldGainHealing,
    EliteReward, RewardSkipMaximumHealth, VictoryInterest, GoldBattleHealth,
    EquipmentSkipGold, EliteStartingHealthMultiplier, CounterReward, VictoryNativeKillReward
}

// Static configuration only. Counters and the spent-bank flag live on the run instance.
[GlobalClass]
public partial class RelicRunRuleSpec : Resource
{
    [Export] public string RuleId { get; set; } = string.Empty;
    [Export] public RelicRunRuleKind Kind { get; set; }
    [Export] public float Amount { get; set; }
    [Export] public int Limit { get; set; }
    [Export] public string CounterId { get; set; } = string.Empty;
    [Export] public ContentPoolDefinition? RewardPool { get; set; }
}
