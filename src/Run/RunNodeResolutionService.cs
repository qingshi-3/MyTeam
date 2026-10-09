using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Battle;
using TowerAutobattler.Project;
using TowerAutobattler.Growth;

namespace TowerAutobattler.Run;

public sealed record RunBattleResolution
{
    internal RunBattleResolution(bool accepted, bool facadeReturnValue, ActiveRunDto? activeRun,
        BattleOutcome outcome, RunBattleResolutionFailure failure)
    {
        Accepted = accepted; FacadeReturnValue = facadeReturnValue; ActiveRun = activeRun;
        Outcome = outcome; Failure = failure;
    }
    public bool Accepted { get; }
    public bool FacadeReturnValue { get; }
    internal ActiveRunDto? ActiveRun { get; }
    public RunView? Run => ActiveRun is { } value ? RunView.For(value) : null;
    public BattleOutcome Outcome { get; }
    public RunBattleResolutionFailure Failure { get; }
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
    private readonly RunBattleResultProjector _battleResults;
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
        ArgumentNullException.ThrowIfNull(battlePreparation);
        _rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
        _decisions = decisions;
        _growth = growth;
        _battleResults = new RunBattleResultProjector(_project, battlePreparation, _rewards, growth);
    }

    public IReadOnlyList<TowerNodeOption> CurrentOptions(ActiveRunDto? run) =>
        run is null || run.OpeningRecruitment is not null || run.Growth?.PendingDiscovery is not null ? [] : _tower.Options(run);

    public EncounterPlan CurrentEncounter(ActiveRunDto? run) => run is null
        ? throw new InvalidOperationException("No active run")
        : _tower.Encounter(run, run.SelectedNode);

    public IReadOnlyList<OpponentOption> CurrentOpponentOptions(ActiveRunDto? run, TowerNodeType type) =>
        run is null || run.OpeningRecruitment is not null || run.PendingNode || run.PendingOffer is not null ||
        run.Growth?.PendingDiscovery is not null || !string.IsNullOrEmpty(run.TerminalCompletionId) ||
        !_tower.Options(run).Any(option => option.Type == type) ? [] : _tower.SelectableOpponentOptions(run, type);

    public bool SelectNode(ActiveRunDto? run, TowerNodeType type, string? opponentOptionId = null)
    {
        if (run is null || run.OpeningRecruitment is not null || run.PendingNode || run.PendingOffer is not null || run.Growth?.PendingDiscovery is not null || !string.IsNullOrEmpty(run.TerminalCompletionId) ||
            !_persistence.ValidateRun(run) || !_tower.Options(run).Any(option => option.Type == type))
            return false;
        var opponents = _tower.SelectableOpponentOptions(run, type);
        if (opponents.Count > 0 ? !opponents.Any(option => option.OptionId == opponentOptionId) :
            !string.IsNullOrEmpty(opponentOptionId)) return false;
        var working = _persistence.CloneRun(run);
        working.SelectedNode = type;
        working.PendingNode = true;
        working.SelectedOpponentOptionId = opponentOptionId ?? string.Empty;
        working.LegacyPendingOpponent = false;
        _rewards.RelicRules.EnterNode(working, type);
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
        if (active is null || !active.PendingNode || !_persistence.ValidateRun(active) ||
            !EncounterMatchesCurrent(active, encounter) || !BattleIdentityMatches(active, result, encounter) ||
            result.Outcome is not (BattleOutcome.PlayerVictory or BattleOutcome.PlayerDefeat or BattleOutcome.Timeout) ||
            result.GoldSpent < 0 || result.GoldSpent > active.Gold ||
            result.RelicTransition is not { } transition ||
            _appliedRelicTransitions.Contains(transition.TransitionId))
            return new RunBattleResolution(
                false, false, active, result.Outcome, RunBattleResolutionFailure.Rejected);

        var victory = result.Outcome == BattleOutcome.PlayerVictory;
        var finalVictory = victory && active.FloorIndex == _project.Campaign.TotalFloors - 1 && encounter.IsBoss;
        var ended = finalVictory || !victory && (encounter.IsBoss ||
            active.CurrentRunHealth <= RunHealthPolicy.DefeatLoss(_project.RunRules, encounter.NodeType));
        var working = _persistence.CloneRun(active);
        if (!_battleResults.TryApply(active, working, result, encounter, ended, out _))
            return new(false, false, active, result.Outcome, RunBattleResolutionFailure.Rejected);
        if (ended)
        {
            // Terminal battles validate their receipt above but intentionally grant no
            // reusable growth. Remove the consumed battle snapshot before validating
            // the post-battle counter, which has already advanced by one.
            if (working.Growth is not null) working.Growth.PendingNode = null;
            working.SelectedOpponentOptionId = string.Empty;
            working.LegacyPendingOpponent = false;
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
        working.SelectedOpponentOptionId = string.Empty;
        working.LegacyPendingOpponent = false;
        var rewardGroups = victory ? _decisions.CreateCombatRewardGroups(working) : [];
        working.PendingOffer = rewardGroups.IsEmpty ? null : rewardGroups[0];
        working.PendingRewardGroups = rewardGroups.IsEmpty ? [] : rewardGroups.RemoveAt(0);
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
        string.IsNullOrEmpty(identity.OriginKey) &&
        identity.FloorIndex == run.FloorIndex &&
        identity.BattleNumber == run.BattleNumber &&
        identity.CompositionId == encounter.CompositionId;
}
