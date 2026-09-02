using System;
using System.Collections.Generic;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Project;

namespace TowerAutobattler.Run;

public sealed class RunFormationService
{
    private static readonly IBattleFloorRuleRuntime ClearFloorRule =
        new ClearFloorRuleRuntime("formation-clear", "常规", "formation command compatibility");
    private readonly CompiledRunRules _rules;
    private readonly IRunFormationPersistence _persistence;

    public RunFormationService(CompiledRunRules rules, IRunFormationPersistence persistence)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
    }

    public bool MoveDeploymentUnit(ActiveRunDto? run, string instanceId, int slot)
    {
        if (run is null || slot < 0 || slot >= _rules.PhysicalDeploymentCeiling)
            return false;
        return Apply(run, FormationMoveCommand.RosterHero(
            instanceId,
            BattlefieldLayout.PlayerDeploymentCells[slot]), ClearFloorRule);
    }

    public bool Apply(ActiveRunDto? run, FormationMoveCommand command, IBattleFloorRuleRuntime floorRule)
    {
        var evaluation = Evaluate(run, command, floorRule);
        if (!evaluation.IsValid || run is null) return false;
        return evaluation.Operation switch
        {
            FormationOperation.Deploy or FormationOperation.Replace => Commit(run, () =>
                run.Deployment[evaluation.TargetSlot] = command.InstanceId),
            FormationOperation.Move => Commit(run, () =>
            {
                run.Deployment[evaluation.SourceSlot] = string.Empty;
                run.Deployment[evaluation.TargetSlot] = command.InstanceId;
            }),
            FormationOperation.Swap => Commit(run, () =>
            {
                (run.Deployment[evaluation.SourceSlot], run.Deployment[evaluation.TargetSlot]) =
                    (run.Deployment[evaluation.TargetSlot], run.Deployment[evaluation.SourceSlot]);
            }),
            _ => false
        };
    }

    public FormationEvaluation Evaluate(
        ActiveRunDto? run,
        FormationMoveCommand command,
        IBattleFloorRuleRuntime floorRule)
    {
        if (run is null) return FormationEvaluation.Reject("当前没有进行中的征程。");
        if (floorRule is null) return FormationEvaluation.Reject("当前楼层规则不可用。");
        if (run.Deployment is null || run.Deployment.Count != _rules.PhysicalDeploymentCeiling ||
            !RunPopulationPolicy.Validate(run, _rules))
            return FormationEvaluation.Reject("当前阵型数据不完整。");
        var population = RunPopulationPolicy.Evaluate(run, _rules);
        var targetSlot = BattlefieldLayout.PlayerDeploymentSlot(command.TargetCell);
        if (targetSlot < 0)
            return FormationEvaluation.Reject("只能部署在我方区域。", population);
        if (!floorRule.CanOccupy(command.TargetCell))
            return FormationEvaluation.Reject("该格受楼层规则阻挡。", population);

        if (command.PieceKind != FormationPieceKind.RosterHero || string.IsNullOrWhiteSpace(command.InstanceId) ||
            run.Roster.TrueForAll(unit => unit.InstanceId != command.InstanceId))
            return FormationEvaluation.Reject("找不到该名册英雄。", population);

        var sourceSlot = run.Deployment.IndexOf(command.InstanceId);
        var occupied = run.Deployment[targetSlot];
        if (sourceSlot == targetSlot)
            return FormationEvaluation.Reject("该英雄已在此格。", population);
        if (sourceSlot >= 0)
            return string.IsNullOrEmpty(occupied)
                ? FormationEvaluation.Accept(FormationOperation.Move, sourceSlot, targetSlot, population)
                : FormationEvaluation.Accept(FormationOperation.Swap, sourceSlot, targetSlot, population);
        if (!string.IsNullOrEmpty(occupied))
            return FormationEvaluation.Accept(FormationOperation.Replace, targetSlot: targetSlot, population: population);
        return population.DeployedPersistentHeroes >= population.AvailableDeploymentPopulation
            ? FormationEvaluation.Reject(
                $"当前人口已满（{population.DeployedPersistentHeroes}/{population.AvailableDeploymentPopulation}）。",
                population)
            : FormationEvaluation.Accept(FormationOperation.Deploy, targetSlot: targetSlot, population: population);
    }

    public bool Withdraw(ActiveRunDto? run, string instanceId)
    {
        if (run is null) return false;
        var slot = run.Deployment.IndexOf(instanceId);
        if (slot < 0 || ReserveCount(run) >= _rules.ReserveCapacity) return false;
        return Commit(run, () => run.Deployment[slot] = string.Empty);
    }

    public void ClearSlot(ActiveRunDto? run, int slot)
    {
        if (run is null || slot < 0 || slot >= _rules.PhysicalDeploymentCeiling ||
            string.IsNullOrEmpty(run.Deployment[slot]))
            return;
        Withdraw(run, run.Deployment[slot]);
    }

    private bool Commit(ActiveRunDto run, Action mutation) =>
        _persistence.TryCommitFormation(run, mutation);

    private static int ReserveCount(ActiveRunDto run) =>
        run.Roster.Count - run.Deployment.FindAll(id => !string.IsNullOrEmpty(id)).Count;
}
