using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Battle;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;

namespace TowerAutobattler.Run;

public sealed record RunBattleResolution(
    bool Accepted,
    bool FacadeReturnValue,
    ActiveRunDto? ActiveRun,
    BattleOutcome Outcome,
    RunBattleResolutionFailure Failure);

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
    private readonly HashSet<string> _appliedRelicTransitions = new(StringComparer.Ordinal);

    public RunNodeResolutionService(
        CompiledGameProject project,
        TowerGenerator tower,
        RunProgressionPersistenceService persistence,
        RunBattlePreparationService battlePreparation,
        RunRewardEconomyService rewards)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _tower = tower ?? throw new ArgumentNullException(nameof(tower));
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        _battlePreparation = battlePreparation ?? throw new ArgumentNullException(nameof(battlePreparation));
        _rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
    }

    public IReadOnlyList<TowerNodeOption> CurrentOptions(ActiveRunDto? run) =>
        run is null ? [] : _tower.Options(run);

    public EncounterPlan CurrentEncounter(ActiveRunDto? run) => run is null
        ? throw new InvalidOperationException("No active run")
        : _tower.Encounter(run, run.SelectedNode);

    public bool SelectNode(ActiveRunDto? run, TowerNodeType type)
    {
        if (run is null || run.PendingNode || !_tower.Options(run).Any(option => option.Type == type))
            return false;
        run.SelectedNode = type;
        run.PendingNode = true;
        return _persistence.SaveActiveRun(run);
    }

    public void FinishNonCombatNode(ActiveRunDto? run)
    {
        if (run is not null) _persistence.AdvanceFloor(run);
    }

    public void ResetRunLifecycle() => _appliedRelicTransitions.Clear();

    public RunBattleResolution CompleteBattle(
        ActiveRunDto? active,
        BattleResult result,
        EncounterPlan encounter)
    {
        if (active is null || !active.PendingNode || !EncounterMatchesCurrent(active, encounter) ||
            !BattleIdentityMatches(active, result, encounter) || result.Outcome == BattleOutcome.Running ||
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

        // A terminal Run is removed only after both encounter identity and the immutable
        // Battle→Run transition have been authenticated in full.
        if (result.Outcome != BattleOutcome.PlayerVictory)
        {
            ResetRunLifecycle();
            _persistence.EndRun();
            return new RunBattleResolution(
                true, false, null, result.Outcome, RunBattleResolutionFailure.None);
        }

        var working = _persistence.CloneRun(active);
        var relicApply = _battlePreparation.ApplyTransition(working, transition);
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
        _rewards.ApplyBattleVictory(working, result, encounter, relicApply.GoldDelta);

        var finalVictory = working.FloorIndex == _project.Campaign.TotalFloors - 1 && encounter.IsBoss;
        if (finalVictory)
        {
            _persistence.CompleteFinalVictory();
            ResetRunLifecycle();
            return new RunBattleResolution(
                true, true, null, result.Outcome, RunBattleResolutionFailure.None);
        }

        working.FloorIndex++;
        working.PendingNode = false;
        if (!_persistence.TryPublish(working, active))
            return new RunBattleResolution(
                false, false, active, result.Outcome, RunBattleResolutionFailure.PersistenceFailed);
        _appliedRelicTransitions.Add(transition.TransitionId);
        return new RunBattleResolution(
            true, true, active, result.Outcome, RunBattleResolutionFailure.None);
    }

    private bool EncounterMatchesCurrent(ActiveRunDto run, EncounterPlan encounter)
    {
        if (encounter.NodeType != run.SelectedNode) return false;
        var expected = _tower.Encounter(run, run.SelectedNode);
        return encounter.EncounterId == expected.EncounterId && encounter.Title == expected.Title &&
               encounter.FloorRuleId == expected.FloorRuleId && encounter.IsBoss == expected.IsBoss &&
               encounter.IsElite == expected.IsElite && encounter.EnemyIds.SequenceEqual(expected.EnemyIds);
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
