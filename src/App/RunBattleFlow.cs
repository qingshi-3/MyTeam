using System;
using TowerAutobattler.Battle;
using TowerAutobattler.Presentation;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

namespace TowerAutobattler.App;

internal sealed record RunBattleNavigation(Action<AppScreenId> ShowScreen, Action ShowTower,
    Action ShowReward, Action<string, string> ShowResult, Action OpenGrowthWorkbench);

// Owns deployment and the campaign battle/report/retry lifecycle. Lab sessions have
// no access to these pending settlement inputs or their exactly-once guards.
internal sealed class RunBattleFlow : IDisposable
{
    private readonly Func<RunApplication?> _application;
    private readonly DeploymentScreenController _deployment;
    private readonly BattleScreenController _battle;
    private readonly BattleReportScreen _report;
    private readonly RunBattleNavigation _navigation;
    private EncounterPlan? _encounter;
    private BattleResult? _pendingBattleResult;
    private PostBattleRoute _postBattleRoute;
    private string _pendingResultTitle = string.Empty;
    private string _pendingResultSummary = string.Empty;
    private string _pendingEncounterTitle = string.Empty;
    private string _pendingSettlementMessage = string.Empty;
    private RunBattleResolution? _pendingBattleResolution;
    private bool _pendingFinalBoss;
    private bool _pendingExpedition;
    private bool _battleResolutionCommitted;
    private bool _battleReportShown;
    private bool _battleReportContinued;

    internal RunBattleFlow(Func<RunApplication?> application, DeploymentScreenController deployment,
        BattleScreenController battle, BattleReportScreen report, RunBattleNavigation navigation)
    {
        _application = application;
        _deployment = deployment;
        _battle = battle;
        _report = report;
        _navigation = navigation;
    }
    private RunApplication App => _application() ?? throw new InvalidOperationException("Run is not ready.");
    internal void SetEncounter(EncounterPlan encounter) => _encounter = encounter;
    internal bool BattleResolutionCommitted => _battleResolutionCommitted;
    internal RunBattleResolution? PendingBattleResolution => _pendingBattleResolution;
    internal void RefreshDeployment()
    {
        if (_deployment.Visible && _encounter is not null) _deployment.Bind(App, _encounter);
    }
    public void Dispose() => _application()?.SetEquipmentEditingLocked(false);

    internal void ShowDeployment()
    {
        if (_encounter is null) return;
        App.SetEquipmentEditingLocked(false);
        _deployment.Bind(App, _encounter);
        _navigation.ShowScreen(AppScreenId.Deployment);
    }

    internal void MoveDeploymentUnit(FormationMoveCommand command)
    {
        if (_deployment.FloorRule is not { } floorRule) return;
        var evaluation = App.EvaluateFormationCommand(command, floorRule);
        if (!evaluation.IsValid)
        {
            _deployment.ShowCellResult(command.TargetCell, false);
            _deployment.ShowMessage(evaluation.RejectionReason, true);
        }
        else if (App.ApplyFormationCommand(command, floorRule))
        {
            ShowDeployment();
            _deployment.ShowCellResult(command.TargetCell, true);
            _deployment.ShowMessage("阵型已更新。", false);
        }
        else
        {
            _deployment.ShowCellResult(command.TargetCell, false);
            _deployment.ShowMessage("阵型保存失败，已恢复原阵型。", true);
        }
    }

    internal void WithdrawDeploymentUnit(string instanceId)
    {
        if (App.WithdrawDeploymentUnit(instanceId))
        {
            ShowDeployment();
            _deployment.ShowMessage("已撤回候命。", false);
        }
        else
            _deployment.ShowMessage(
                $"只有已部署单位可以撤回，且后备区不能超过 {App.Rules.ReserveCapacity} 人。", true);
    }

    internal void StartBattle()
    {
        if (_encounter is null) return;
        if (App.Run?.Growth?.PendingDiscovery is not null)
        {
            _navigation.OpenGrowthWorkbench();
            return;
        }
        ResetPendingBattleFlow();
        try
        {
            // The facade validates the complete configuration before paying for a preset.
            var begin = App.TryBeginGrowthBattle(_encounter);
            if (!begin.Succeeded)
            {
                _deployment.ShowMessage(begin.Message, true);
                return;
            }
            var config = App.BuildBattleConfig(_encounter);
            App.SetEquipmentEditingLocked(true);
            _navigation.ShowScreen(AppScreenId.Battle);
            _battle.StartBattle(App.Content, config, _encounter.Title, App.Settings.DefaultBattleSpeed);
            _battle.BindRunHealth(App.Run!.CurrentRunHealth, App.Run.MaximumRunHealth);
        }
        catch (Exception exception)
        {
            _battle.StopBattle();
            App.SetEquipmentEditingLocked(false);
            ShowDeployment();
            _deployment.ShowMessage("战斗未能启动，可重试；已保存的法术不会重复消耗。" + exception.Message, true);
        }
    }

    internal void AcceptBattleResult(BattleResult result)
    {
        if (_battleResolutionCommitted || _encounter is null || _pendingBattleResult is not null) return;
        _pendingBattleResult = result;
        _pendingEncounterTitle = _encounter.Title;
        _pendingExpedition = App.IsExpedition;
        _pendingFinalBoss = _encounter.IsBoss && (App.IsExpedition
            ? App.IsFinalExpeditionStage
            : App.Run?.FloorIndex == App.Project.Campaign.TotalFloors - 1);
        TryResolvePendingBattle();
    }

    private bool TryResolvePendingBattle()
    {
        if (_battleResolutionCommitted || _encounter is null || _pendingBattleResult is null)
            return _battleResolutionCommitted;

        var resolution = App.ResolveBattle(_pendingBattleResult, _encounter);
        _pendingBattleResolution = resolution;
        if (!resolution.Accepted)
        {
            _pendingSettlementMessage = resolution.Failure == RunBattleResolutionFailure.PersistenceFailed
                ? !string.IsNullOrEmpty(App.Run?.TerminalCompletionId)
                    ? "终局结果已暂存，征程归档尚未完成。请重试结算，不会重复扣血或发奖。"
                    : "战斗结果暂未保存，军团状态没有改变。请重试结算。"
                : "战斗结算暂未完成，军团状态没有改变。请重试结算。";
            if (_battleReportShown)
                _report.ShowSettlementRetry(_pendingSettlementMessage);
            return false;
        }

        _battleResolutionCommitted = true;
        App.SetEquipmentEditingLocked(false);
        _pendingSettlementMessage = string.Empty;
        if (resolution.Outcome != BattleOutcome.PlayerVictory)
        {
            _postBattleRoute = resolution.Run is not null ? PostBattleRoute.Continue : PostBattleRoute.Failure;
            _pendingResultTitle = "征程结束";
            _pendingResultSummary = resolution.Consequence is { } consequence
                ? RunHealthText.Consequence(consequence) : "本次征程已结束。";
        }
        else if (_pendingFinalBoss)
        {
            _postBattleRoute = PostBattleRoute.Success;
            _pendingResultTitle = _pendingExpedition ? "征程胜利" : "登塔成功";
            _pendingResultSummary = (_pendingExpedition
                ? "最终首领已被击败，军团完成了这次大陆征程。\n本局战斗摘要："
                : "塔顶主宰已被击败。新的英雄与更高难度正在等待下一次征程。\n本局战斗摘要：") + _pendingBattleResult.Digest[..12];
        }
        else
            _postBattleRoute = PostBattleRoute.Reward;
        return true;
    }

    internal void ShowBattleReport()
    {
        if (_battleReportShown || _pendingBattleResult is null) return;
        _battleReportShown = true;
        _navigation.ShowScreen(AppScreenId.BattleReport);
        _report.Bind(_pendingBattleResult, _pendingEncounterTitle, App.Content);
        if (_battleResolutionCommitted) _report.BindRunConsequence(_pendingBattleResolution?.Consequence);
        if (!_battleResolutionCommitted)
            _report.ShowSettlementRetry(_pendingSettlementMessage);
    }

    internal void ContinueAfterBattleReport()
    {
        if (!_battleReportShown || _battleReportContinued) return;
        if (!_battleResolutionCommitted && !TryResolvePendingBattle()) return;
        _battleReportContinued = true;
        if (_postBattleRoute == PostBattleRoute.Reward) _navigation.ShowReward();
        else if (_postBattleRoute == PostBattleRoute.Continue) _navigation.ShowTower();
        else _navigation.ShowResult(_pendingResultTitle, _pendingResultSummary);
    }

    internal void ResetPendingBattleFlow()
    {
        _pendingBattleResult = null;
        _pendingBattleResolution = null;
        _postBattleRoute = PostBattleRoute.None;
        _pendingResultTitle = string.Empty;
        _pendingResultSummary = string.Empty;
        _pendingEncounterTitle = string.Empty;
        _pendingSettlementMessage = string.Empty;
        _pendingFinalBoss = false;
        _pendingExpedition = false;
        _battleResolutionCommitted = false;
        _battleReportShown = false;
        _battleReportContinued = false;
    }

    private enum PostBattleRoute { None, Reward, Continue, Success, Failure }
}
