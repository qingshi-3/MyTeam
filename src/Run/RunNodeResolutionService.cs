using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Battle;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;
using TowerAutobattler.Growth;

namespace TowerAutobattler.Run;

public sealed record RunBattleResolution(
    bool Accepted,
    bool FacadeReturnValue,
    ActiveRunDto? ActiveRun,
    BattleOutcome Outcome,
    RunBattleResolutionFailure Failure)
{
    public RunBattleConsequence? Consequence { get; init; }
}

public enum RunBattleResolutionFailure
{
    None,
    Rejected,
    PersistenceFailed
}

// Owns node identity, exactly-once battle transition coordination, and floor progression.
public sealed class RunNodeResolutionService
{
    private readonly CompiledGameProject _project;
    private readonly TowerGenerator _tower;
    private readonly RunProgressionPersistenceService _persistence;
    private readonly RunBattlePreparationService _battlePreparation;
    private readonly RunRewardEconomyService _rewards;
    private readonly RunDecisionService _decisions;
    private readonly GrowthRunService? _growth;
    private readonly HashSet<string> _appliedRelicTransitions = new(StringComparer.Ordinal);

    public RunNodeResolutionService(
        CompiledGameProject project,
        TowerGenerator tower,
        RunProgressionPersistenceService persistence,
        RunBattlePreparationService battlePreparation,
        RunRewardEconomyService rewards,
        RunDecisionService decisions,
        GrowthRunService? growth = null)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _tower = tower ?? throw new ArgumentNullException(nameof(tower));
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        _battlePreparation = battlePreparation ?? throw new ArgumentNullException(nameof(battlePreparation));
        _rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
        _decisions = decisions;
        _growth = growth;
    }

    public IReadOnlyList<TowerNodeOption> CurrentOptions(ActiveRunDto? run) =>
        run is null || run.OpeningRecruitment is not null || run.Growth?.PendingDiscovery is not null ? [] : _tower.Options(run);

    public EncounterPlan CurrentEncounter(ActiveRunDto? run) => run is null
        ? throw new InvalidOperationException("No active run")
        : _tower.Encounter(run, run.SelectedNode);

    public bool SelectNode(ActiveRunDto? run, TowerNodeType type)
    {
        if (run is null || run.OpeningRecruitment is not null || run.PendingNode || run.PendingOffer is not null || run.Growth?.PendingDiscovery is not null || !string.IsNullOrEmpty(run.TerminalCompletionId) ||
            !_persistence.ValidateRun(run) || !_tower.Options(run).Any(option => option.Type == type))
            return false;
        var working = _persistence.CloneRun(run);
        working.SelectedNode = type;
        working.PendingNode = true;
        if (RunDecisionService.KindFor(type) is { } kind)
        {
            if (_growth is not null && !_growth.FreezeNonCombat(working)) return false;
            working.PendingOffer = _decisions.CreateOffer(working, kind);
        }
        return _persistence.ValidateRun(working) && _persistence.TryPublish(working, run);
    }

    public void FinishNonCombatNode(ActiveRunDto? run)
    {
        if (run?.PendingOffer is { } offer) _decisions.Resolve(run, offer.OfferId, null);
    }

    public void ResetRunLifecycle() => _appliedRelicTransitions.Clear();

    public RunBattleResolution CompleteBattle(
        ActiveRunDto? active,
        BattleResult result,
        EncounterPlan encounter)
    {
        if (active is not null && !string.IsNullOrEmpty(active.TerminalCompletionId))
            return _persistence.TryCompleteTerminal(active)
                ? new(true, active.TerminalVictory, null, active.LastBattleConsequence?.Outcome ?? result.Outcome, RunBattleResolutionFailure.None)
                    { Consequence = active.LastBattleConsequence }
                : new(false, false, active, result.Outcome, RunBattleResolutionFailure.PersistenceFailed);
        if (active is null || !active.PendingNode || !EncounterMatchesCurrent(active, encounter) ||
            !_persistence.ValidateRun(active) || !BattleIdentityMatches(active, result, encounter) ||
            result.Outcome is not (BattleOutcome.PlayerVictory or BattleOutcome.PlayerDefeat or BattleOutcome.Timeout) ||
            result.GoldSpent < 0 || result.GoldSpent > active.Gold ||
            result.RelicTransition is not { } transition ||
            _appliedRelicTransitions.Contains(transition.TransitionId))
            return new RunBattleResolution(
                false, false, active, result.Outcome, RunBattleResolutionFailure.Rejected);

        var expectedReason = result.Outcome switch
        {
            BattleOutcome.PlayerVictory => RelicBattleCompletionReason.PlayerVictory,
            BattleOutcome.PlayerDefeat => RelicBattleCompletionReason.PlayerDefeat,
            BattleOutcome.Timeout => RelicBattleCompletionReason.Timeout,
            _ => RelicBattleCompletionReason.None
        };
        var validation = _battlePreparation.ValidateTransition(active, transition, expectedReason);
        if (!validation.Succeeded)
            return new RunBattleResolution(
                false, false, active, result.Outcome, RunBattleResolutionFailure.Rejected);

        var working = _persistence.CloneRun(active);
        var relicApply = _battlePreparation.ApplyTransition(working, transition, expectedReason);
        if (!relicApply.Succeeded)
            return new RunBattleResolution(
                false, false, active, result.Outcome, RunBattleResolutionFailure.Rejected);
        foreach (var projected in relicApply.ProjectedInstances)
        {
            var item = working.Items.First(instance => instance.InstanceId == projected.InstanceId);
            item.Stacks = projected.Stacks;
            item.Charges = projected.Charges;
            item.Roll = projected.Roll;
            item.Counters = projected.Counters.Select(counter => new RelicCounterStateDto
            {
                CounterId = counter.CounterId,
                Value = counter.Value
            }).ToList();
        }
        var victory = result.Outcome == BattleOutcome.PlayerVictory;
        if (victory)
            _rewards.ApplyBattleVictory(working, result, encounter, relicApply.GoldDelta);
        else
        {
            // A loss preserves the prepared formation. A fresh battle restores hero health;
            // only the Run resource is spent, and no victory rewards are created.
            foreach (var hero in working.Roster) hero.HealthRatio = 1f;
            working.Gold -= result.GoldSpent;
            working.BattleNumber = checked(working.BattleNumber + 1);
            working.CurrentRunHealth = Math.Max(0, working.CurrentRunHealth -
                RunHealthPolicy.DefeatLoss(_project.RunRules, encounter.NodeType));
        }

        var finalVictory = victory && working.FloorIndex == _project.Campaign.TotalFloors - 1 && encounter.IsBoss;
        var ended = finalVictory || !victory && (encounter.IsBoss || working.CurrentRunHealth == 0);
        working.LastBattleConsequence = new(result.Outcome, active.CurrentRunHealth, working.CurrentRunHealth,
            working.MaximumRunHealth, encounter.IsBoss, ended);
        if (_growth is not null && !_growth.ApplyBattlePermanentGains(working, result.PermanentGains, !ended))
            return new(false, false, active, result.Outcome, RunBattleResolutionFailure.Rejected);
        if (ended)
        {
            // Terminal battles validate their receipt above but intentionally grant no
            // reusable growth. Remove the consumed battle snapshot before validating
            // the post-battle counter, which has already advanced by one.
            if (working.Growth is not null) working.Growth.PendingNode = null;
            working.TerminalCompletionId = Guid.NewGuid().ToString("N");
            working.TerminalVictory = finalVictory;
            if (!_persistence.ValidateRun(working))
                return new(false, false, active, result.Outcome, RunBattleResolutionFailure.Rejected);
            if (!_persistence.TryPublish(working, active) || !_persistence.TryCompleteTerminal(active))
                return new(false, false, active, result.Outcome, RunBattleResolutionFailure.PersistenceFailed);
            ResetRunLifecycle();
            return new RunBattleResolution(
                true, finalVictory, null, result.Outcome, RunBattleResolutionFailure.None)
                { Consequence = working.LastBattleConsequence };
        }

        if (_growth is not null && !_growth.SettleNode(working))
            return new(false, false, active, result.Outcome, RunBattleResolutionFailure.Rejected);

        working.FloorIndex++;
        working.PendingNode = false;
        working.PendingOffer = victory ? _decisions.CreateOffer(working, Project.RunOfferKind.CombatReward) : null;
        if (!_persistence.ValidateRun(working))
            return new(false, false, active, result.Outcome, RunBattleResolutionFailure.Rejected);
        if (!_persistence.TryPublish(working, active))
            return new RunBattleResolution(
                false, false, active, result.Outcome, RunBattleResolutionFailure.PersistenceFailed);
        _appliedRelicTransitions.Add(transition.TransitionId);
        return new RunBattleResolution(
            true, victory, active, result.Outcome, RunBattleResolutionFailure.None)
            { Consequence = working.LastBattleConsequence };
    }

    private bool EncounterMatchesCurrent(ActiveRunDto run, EncounterPlan encounter)
    {
        if (encounter.NodeType != run.SelectedNode) return false;
        var expected = _tower.Encounter(run, run.SelectedNode);
        return encounter.EncounterId == expected.EncounterId && encounter.Title == expected.Title &&
               encounter.FloorRuleId == expected.FloorRuleId && encounter.IsBoss == expected.IsBoss &&
               encounter.IsElite == expected.IsElite && encounter.EnemyIds.SequenceEqual(expected.EnemyIds) &&
               encounter.CompositionId == expected.CompositionId && encounter.EnemyCells.SequenceEqual(expected.EnemyCells);
    }

    private static bool BattleIdentityMatches(
        ActiveRunDto run,
        BattleResult result,
        EncounterPlan encounter) => result.Identity is { } identity &&
        identity.EncounterId == encounter.EncounterId &&
        identity.NodeType == encounter.NodeType &&
        identity.RunSeed == run.Seed &&
        identity.FloorIndex == run.FloorIndex &&
        identity.BattleNumber == run.BattleNumber;
}
