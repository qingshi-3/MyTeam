using TowerAutobattler.Battle;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public static class RunHealthText
{
    public static string Risk(CompiledRunRules rules, TowerNodeType type) => type switch
    {
        TowerNodeType.Boss => "首领战 · 战败或超时即结束征程，不受剩余生命影响。",
        TowerNodeType.Combat or TowerNodeType.Elite => $"战败 / 超时：全局生命 −{RunHealthPolicy.DefeatLoss(rules, type)} · 无战利品；生命尚有剩余可继续。",
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
