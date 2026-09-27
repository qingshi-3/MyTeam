using System;
using TowerAutobattler.Battle;
using TowerAutobattler.Project;

namespace TowerAutobattler.Run;

// Immutable settlement facts survive terminal-publication retries and reloads.
public sealed record RunBattleConsequence(BattleOutcome Outcome, int HealthBefore, int HealthAfter,
    int MaximumHealth, bool IsBoss, bool RunEnded)
{
    public int HealthLost => HealthBefore - HealthAfter;
}

public static class RunHealthPolicy
{
    public static int DefeatLoss(CompiledRunRules rules, TowerNodeType type) => type switch
    {
        TowerNodeType.Combat => rules.CombatDefeatHealthLoss,
        TowerNodeType.Elite => rules.EliteDefeatHealthLoss,
        _ => 0
    };

    public static bool ValidConsequence(RunBattleConsequence? value) => value is null ||
        (value.Outcome is BattleOutcome.PlayerVictory or BattleOutcome.PlayerDefeat or BattleOutcome.Timeout) &&
        value.MaximumHealth > 0 && value.HealthBefore > 0 && value.HealthBefore <= value.MaximumHealth &&
        value.HealthAfter >= 0 && value.HealthAfter <= value.HealthBefore &&
        (value.Outcome != BattleOutcome.PlayerVictory || value.HealthLost == 0) &&
        (!value.IsBoss || value.HealthLost == 0) &&
        (value.Outcome == BattleOutcome.PlayerVictory || value.RunEnded == (value.IsBoss || value.HealthAfter == 0));

    public static int Recover(ActiveRunDto run, int amount) =>
        (int)Math.Min(run.MaximumRunHealth, (long)run.CurrentRunHealth + amount);
}
