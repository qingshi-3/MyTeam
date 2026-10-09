using System;
using System.Linq;
using TowerAutobattler.Battle;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;

namespace TowerAutobattler.Run;

// Applies the common battle receipt to a detached Run projection. Progression
// policies own terminal facts, reward queues, growth settlement and the single
// save/publication; neither tower nor expedition has a second result pipeline.
public sealed class RunBattleResultProjector(
    CompiledGameProject project,
    RunBattlePreparationService preparation,
    RunRewardEconomyService rewards,
    GrowthRunService? growth = null)
{
    public bool TryApply(ActiveRunDto active, ActiveRunDto working, BattleResult result,
        EncounterPlan encounter, bool terminal, out RunBattleConsequence consequence)
    {
        consequence = null!;
        if (ReferenceEquals(active, working) ||
            result.Outcome is not (BattleOutcome.PlayerVictory or BattleOutcome.PlayerDefeat or BattleOutcome.Timeout) ||
            result.GoldSpent < 0 || result.GoldSpent > active.Gold || result.RelicTransition is not { } transition)
            return false;
        var expectedReason = result.Outcome switch
        {
            BattleOutcome.PlayerVictory => RelicBattleCompletionReason.PlayerVictory,
            BattleOutcome.PlayerDefeat => RelicBattleCompletionReason.PlayerDefeat,
            BattleOutcome.Timeout => RelicBattleCompletionReason.Timeout,
            _ => RelicBattleCompletionReason.None
        };
        if (!preparation.ValidateTransition(active, transition, expectedReason).Succeeded) return false;
        BattlePreparationSnapshot? opening = null;
        if (!result.RememberedGains.IsDefaultOrEmpty)
        {
            if (growth is null) return false;
            try { opening = BattlePreparationProjection.Build(preparation.Build(active, encounter, requireLegalFormation: true)); }
            catch (InvalidOperationException) { return false; }
        }
        var relicApply = preparation.ApplyTransition(working, transition, expectedReason);
        if (!relicApply.Succeeded) return false;
        foreach (var projected in relicApply.ProjectedInstances)
        {
            var item = working.Items.First(instance => instance.InstanceId == projected.InstanceId);
            item.Stacks = projected.Stacks;
            item.Charges = projected.Charges;
            item.Roll = projected.Roll;
            item.Counters = projected.Counters.Select(counter => new RelicCounterStateDto
            {
                CounterId = counter.CounterId, Value = counter.Value
            }).ToList();
        }
        var victory = result.Outcome == BattleOutcome.PlayerVictory;
        if (victory) rewards.ApplyBattleVictory(working, result, encounter, relicApply.GoldDelta);
        else
        {
            // Defeat preserves the formation. Only global run health is lost;
            // all persistent members start the next battle at full health.
            foreach (var hero in working.Roster) hero.HealthRatio = 1f;
            working.Gold -= result.GoldSpent;
            working.BattleNumber = checked(working.BattleNumber + 1);
            working.CurrentRunHealth = Math.Max(0, working.CurrentRunHealth -
                RunHealthPolicy.DefeatLoss(project.RunRules, encounter.NodeType));
        }
        working.LastBattleConsequence = new(result.Outcome, active.CurrentRunHealth,
            victory ? active.CurrentRunHealth : working.CurrentRunHealth, working.MaximumRunHealth,
            encounter.IsBoss, terminal);
        // The active state is already the committed, post-entry state. Its gold
        // is the same frozen baseline used by the simulator and spend checks.
        if (!rewards.RelicRules.SettleBattle(working, result, encounter, active.Gold, rewards.GrantToProjection)) return false;
        if (growth is not null && !growth.ApplyBattlePermanentGains(working, result.PermanentGains, !terminal)) return false;
        if (growth is not null && !growth.ApplyBattleRememberedGains(active, working, result, opening, victory && !terminal)) return false;
        consequence = working.LastBattleConsequence;
        return true;
    }
}
