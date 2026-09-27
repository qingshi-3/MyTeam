using TowerAutobattler.Battle;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public static class RunHealthText
{
    public static string Risk(CompiledRunRules rules, TowerNodeType type) => type switch
    {
        TowerNodeType.Boss => "Boss 战失败或超时，直接结束征程（无论剩余生命）。",
        TowerNodeType.Combat or TowerNodeType.Elite => $"失败或超时扣 {RunHealthPolicy.DefeatLoss(rules, type)} 全局生命；有余血可继续，无战利品。",
        _ => string.Empty
    };

    public static string Consequence(RunBattleConsequence value)
    {
        var health = $"全局生命 {value.HealthBefore} → {value.HealthAfter}/{value.MaximumHealth}";
        if (value.Outcome == BattleOutcome.PlayerVictory) return health + " · 本战未扣血";
        if (value.IsBoss) return $"Boss 战失败，征程结束。剩余全局生命 {value.HealthAfter}/{value.MaximumHealth} 无法抵消此次失败。";
        return health + $"（−{value.HealthLost}）\n" + (value.RunEnded
            ? "全局生命已耗尽，征程结束。"
            : "本战无战利品。保留阵型，下一战英雄满血出战。可继续前进并寻找恢复机会。");
    }
}
