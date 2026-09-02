using System;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Domain;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

namespace TowerAutobattler.App;

// Coordinates Run use cases and navigation. Screen-local controllers own all
// node lookup, view-model construction, card reconciliation, and typed input.
public sealed class GameFlowCoordinator : IDisposable
{
    private readonly Func<RunApplication?> _application;
    private readonly AppScreenHost _screens;
    private readonly Action _quit;
    private readonly CompiledProjectPresentation _presentation;
    private EncounterPlan? _encounter;
    private BattleResult? _pendingBattleResult;
    private PostBattleRoute _postBattleRoute;
    private string _pendingResultTitle = string.Empty;
    private string _pendingResultSummary = string.Empty;
    private string _pendingEncounterTitle = string.Empty;
    private string _pendingSettlementMessage = string.Empty;
    private RunBattleResolution? _pendingBattleResolution;
    private bool _pendingFinalBoss;
    private bool _battleResolutionCommitted;
    private bool _battleReportShown;
    private bool _battleReportContinued;
    private BattleLabContentIndex? _battleLabIndex;
    private BattleLabPresetStore? _battleLabPresets;
    private BattleLabSession? _battleLabSession;
    private BattleLabStartSnapshot? _battleLabStartSnapshot;
    private BattleResult? _battleLabResult;
    private bool _battleLabBattleActive;
    private bool _battleLabReportShown;
    private bool _connected;

    public GameFlowCoordinator(
        Func<RunApplication?> application,
        AppScreenHost screens,
        CompiledProjectPresentation presentation,
        Action quit)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
        _screens = screens ?? throw new ArgumentNullException(nameof(screens));
        _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        _quit = quit ?? throw new ArgumentNullException(nameof(quit));
    }

    private RunApplication App => _application() ??
        throw new InvalidOperationException("Run application has not completed bootstrap.");

    public void Start()
    {
        if (_connected) return;
        _screens.MainMenu.NewRunRequested += ShowHeroSelection;
        _screens.MainMenu.ContinueRequested += ContinueRun;
        _screens.MainMenu.BattleLabRequested += ShowBattleLab;
        _screens.MainMenu.SettingsRequested += ShowSettings;
        _screens.MainMenu.QuitRequested += _quit;
        _screens.HeroSelection.HeroChosen += StartNewRun;
        _screens.HeroSelection.BackRequested += ShowMainMenu;
        _screens.Tower.NodeSelected += SelectNode;
        _screens.Tower.AbandonRequested += AbandonRun;
        _screens.Deployment.BackRequested += ShowTower;
        _screens.Deployment.StartRequested += StartBattle;
        _screens.Deployment.MoveRequested += MoveDeploymentUnit;
        _screens.Deployment.WithdrawRequested += WithdrawDeploymentUnit;
        _screens.Reward.ChoiceRequested += ClaimItem;
        _screens.Reward.ContinueRequested += ShowTower;
        _screens.Recruitment.ChoiceRequested += ClaimRecruit;
        _screens.Recruitment.ConvertRequested += ConvertRecruit;
        _screens.Recruitment.ContinueRequested += SkipRecruitment;
        _screens.Shop.PurchaseRequested += BuyItem;
        _screens.Shop.LeaveRequested += LeaveShop;
        _screens.Event.ChoiceRequested += ResolveEvent;
        _screens.Rest.ChoiceRequested += ResolveRest;
        _screens.Result.NewRunRequested += ShowHeroSelection;
        _screens.Result.MenuRequested += ShowMainMenu;
        _screens.Settings.SaveRequested += SaveSettings;
        _screens.Battle.Finished += AcceptBattleResult;
        _screens.Battle.EndTransitionFinished += ShowBattleReport;
        _screens.Battle.ResetRequested += ResetBattleLabBattle;
        _screens.Battle.ReturnToConfigurationRequested += ReturnToBattleLabConfiguration;
        _screens.BattleReport.ContinueRequested += ContinueAfterBattleReport;
        _screens.BattleLab.BackRequested += ShowMainMenu;
        _screens.BattleLab.StartRequested += StartBattleLabBattle;
        _connected = true;
        ShowMainMenu();
    }

    public void Dispose()
    {
        if (!_connected) return;
        _screens.MainMenu.NewRunRequested -= ShowHeroSelection;
        _screens.MainMenu.ContinueRequested -= ContinueRun;
        _screens.MainMenu.BattleLabRequested -= ShowBattleLab;
        _screens.MainMenu.SettingsRequested -= ShowSettings;
        _screens.MainMenu.QuitRequested -= _quit;
        _screens.HeroSelection.HeroChosen -= StartNewRun;
        _screens.HeroSelection.BackRequested -= ShowMainMenu;
        _screens.Tower.NodeSelected -= SelectNode;
        _screens.Tower.AbandonRequested -= AbandonRun;
        _screens.Deployment.BackRequested -= ShowTower;
        _screens.Deployment.StartRequested -= StartBattle;
        _screens.Deployment.MoveRequested -= MoveDeploymentUnit;
        _screens.Deployment.WithdrawRequested -= WithdrawDeploymentUnit;
        _screens.Reward.ChoiceRequested -= ClaimItem;
        _screens.Reward.ContinueRequested -= ShowTower;
        _screens.Recruitment.ChoiceRequested -= ClaimRecruit;
        _screens.Recruitment.ConvertRequested -= ConvertRecruit;
        _screens.Recruitment.ContinueRequested -= SkipRecruitment;
        _screens.Shop.PurchaseRequested -= BuyItem;
        _screens.Shop.LeaveRequested -= LeaveShop;
        _screens.Event.ChoiceRequested -= ResolveEvent;
        _screens.Rest.ChoiceRequested -= ResolveRest;
        _screens.Result.NewRunRequested -= ShowHeroSelection;
        _screens.Result.MenuRequested -= ShowMainMenu;
        _screens.Settings.SaveRequested -= SaveSettings;
        _screens.Battle.Finished -= AcceptBattleResult;
        _screens.Battle.EndTransitionFinished -= ShowBattleReport;
        _screens.Battle.ResetRequested -= ResetBattleLabBattle;
        _screens.Battle.ReturnToConfigurationRequested -= ReturnToBattleLabConfiguration;
        _screens.BattleReport.ContinueRequested -= ContinueAfterBattleReport;
        _screens.BattleLab.BackRequested -= ShowMainMenu;
        _screens.BattleLab.StartRequested -= StartBattleLabBattle;
        if (_battleLabBattleActive) _screens.Battle.StopBattle();
        _battleLabBattleActive = false;
        _connected = false;
    }

    internal void ShowMainMenu()
    {
        _screens.MainMenu.Bind(App.ActiveRun is not null);
        Show(AppScreenId.MainMenu);
    }

    internal void ShowBattleLab()
    {
        try
        {
            EnsureBattleLabSession();
            _screens.BattleLab.Bind(_battleLabIndex!, _battleLabSession!, _battleLabPresets);
            Show(AppScreenId.BattleLab);
        }
        catch (Exception exception)
        {
            _screens.Result.Bind("战斗实验室不可用", exception.Message);
            Show(AppScreenId.Result);
        }
    }

    private void EnsureBattleLabSession()
    {
        if (_battleLabSession is not null) return;
        _battleLabIndex = new BattleLabContentIndex(new CompiledGamePackage(
            App.Content,
            App.Project,
            App.Content.PublicationVersion));
        _battleLabPresets = new BattleLabPresetStore(_screens.BattleLab.PresetCatalog);
        if (!_battleLabPresets.TryLoad(_battleLabPresets.DefaultPresetName, out var preset))
            throw new InvalidOperationException("默认战斗实验室预设无法读取。");
        var snapshot = BattleLabPresetStore.ToSnapshot(preset);
        _battleLabSession = new BattleLabSession(
            _battleLabIndex,
            snapshot.CurrentPopulation,
            snapshot.Seed,
            snapshot.Mode,
            snapshot.FloorRuleId);
        _battleLabSession.Restore(snapshot);
    }

    private void StartBattleLabBattle()
    {
        if (_battleLabIndex is null || _battleLabSession is null) return;
        try
        {
            _battleLabStartSnapshot = _battleLabSession.Freeze();
            _battleLabResult = null;
            _battleLabReportShown = false;
            _battleLabBattleActive = true;
            var config = new BattleLabPreparationAdapter(_battleLabIndex).Build(_battleLabStartSnapshot);
            Show(AppScreenId.Battle);
            _screens.Battle.StartBattle(App.Content, config, "战斗实验室", 1f);
            _screens.Battle.SetLabControlsVisible(true);
        }
        catch (Exception exception)
        {
            _screens.Battle.StopBattle();
            _battleLabBattleActive = false;
            _screens.BattleLab.ShowFeedback("无法开始实验：" + exception.Message, false);
            Show(AppScreenId.BattleLab);
        }
    }

    private void ResetBattleLabBattle()
    {
        if (!_battleLabBattleActive || _battleLabIndex is null || _battleLabStartSnapshot is null) return;
        try
        {
            _battleLabResult = null;
            _battleLabReportShown = false;
            var config = new BattleLabPreparationAdapter(_battleLabIndex).Build(_battleLabStartSnapshot);
            _screens.Battle.StartBattle(App.Content, config, "战斗实验室", 1f);
            _screens.Battle.SetLabControlsVisible(true);
        }
        catch (Exception exception)
        {
            _screens.Battle.StopBattle();
            ReturnToBattleLabConfiguration();
            _screens.BattleLab.ShowFeedback("重置失败：" + exception.Message, false);
        }
    }

    private void ReturnToBattleLabConfiguration()
    {
        if (!_battleLabBattleActive && _battleLabSession is null) return;
        _screens.Battle.StopBattle();
        _battleLabBattleActive = false;
        _battleLabResult = null;
        _battleLabReportShown = false;
        _battleLabStartSnapshot = null;
        if (_battleLabIndex is null || _battleLabSession is null) { ShowMainMenu(); return; }
        _screens.BattleLab.Bind(_battleLabIndex, _battleLabSession, _battleLabPresets);
        Show(AppScreenId.BattleLab);
    }

    internal void ShowHeroSelection()
    {
        _screens.HeroSelection.Bind(App.Content, App.Meta);
        Show(AppScreenId.HeroSelection);
    }

    private void StartNewRun(string heroId)
    {
        if (App.StartNewRun(heroId, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())) ShowTower();
    }

    private void ContinueRun()
    {
        if (App.ActiveRun is null) { ShowHeroSelection(); return; }
        if (App.ActiveRun.PendingNode) OpenSelectedNode(); else ShowTower();
    }

    internal void ShowTower()
    {
        if (App.ActiveRun is not { } run) { ShowMainMenu(); return; }
        if (run.PendingNode) { OpenSelectedNode(); return; }
        _screens.Tower.Bind(App, _presentation.ChoiceCard, _presentation.SemanticIcons);
        Show(AppScreenId.Tower);
    }

    private void SelectNode(TowerNodeType type)
    {
        if (App.SelectNode(type)) OpenSelectedNode();
    }

    internal void OpenSelectedNode()
    {
        if (App.ActiveRun is not { } run) { ShowMainMenu(); return; }
        switch (run.SelectedNode)
        {
            case TowerNodeType.Combat or TowerNodeType.Elite or TowerNodeType.Boss:
                _encounter = App.CurrentEncounter();
                ShowDeployment();
                break;
            case TowerNodeType.Recruitment:
                ShowRecruitment();
                break;
            case TowerNodeType.Shop:
                ShowShop();
                break;
            case TowerNodeType.Event:
                _screens.Event.Bind(App.Rules);
                Show(AppScreenId.Event);
                break;
            case TowerNodeType.Rest:
                _screens.Rest.Bind(App.Rules);
                Show(AppScreenId.Rest);
                break;
        }
    }

    internal void ShowDeployment()
    {
        if (_encounter is null) return;
        _screens.Deployment.Bind(App, _encounter);
        Show(AppScreenId.Deployment);
    }

    private void MoveDeploymentUnit(FormationMoveCommand command)
    {
        if (_screens.Deployment.FloorRule is not { } floorRule) return;
        var evaluation = App.EvaluateFormationCommand(command, floorRule);
        if (!evaluation.IsValid)
        {
            _screens.Deployment.ShowCellResult(command.TargetCell, false);
            _screens.Deployment.ShowMessage(evaluation.RejectionReason, true);
        }
        else if (App.ApplyFormationCommand(command, floorRule))
        {
            ShowDeployment();
            _screens.Deployment.ShowCellResult(command.TargetCell, true);
            _screens.Deployment.ShowMessage("阵型已更新。", false);
        }
        else
        {
            _screens.Deployment.ShowCellResult(command.TargetCell, false);
            _screens.Deployment.ShowMessage("阵型保存失败，已恢复原阵型。", true);
        }
    }

    private void WithdrawDeploymentUnit(string instanceId)
    {
        if (App.WithdrawDeploymentUnit(instanceId))
        {
            ShowDeployment();
            _screens.Deployment.ShowMessage("已撤回候命。", false);
        }
        else
            _screens.Deployment.ShowMessage(
                $"只有已部署单位可以撤回，且后备区不能超过 {App.Rules.ReserveCapacity} 人。", true);
    }

    private void StartBattle()
    {
        if (_encounter is null) return;
        ResetPendingBattleFlow();
        var config = App.BuildBattleConfig(_encounter);
        Show(AppScreenId.Battle);
        _screens.Battle.StartBattle(App.Content, config, _encounter.Title, App.Settings.DefaultBattleSpeed);
    }

    internal void AcceptBattleResult(BattleResult result)
    {
        if (_battleLabBattleActive)
        {
            _battleLabResult ??= result;
            return;
        }
        if (_battleResolutionCommitted || _encounter is null || _pendingBattleResult is not null) return;
        _pendingBattleResult = result;
        _pendingEncounterTitle = _encounter.Title;
        _pendingFinalBoss = App.ActiveRun?.FloorIndex == App.Project.Campaign.TotalFloors - 1 && _encounter.IsBoss;
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
                ? "战斗结果暂未保存，军团状态没有改变。请重试结算。"
                : "战斗结算暂未完成，军团状态没有改变。请重试结算。";
            if (_battleReportShown)
                _screens.BattleReport.ShowSettlementRetry(_pendingSettlementMessage);
            return false;
        }

        _battleResolutionCommitted = true;
        _pendingSettlementMessage = string.Empty;
        if (resolution.Outcome != BattleOutcome.PlayerVictory)
        {
            _postBattleRoute = PostBattleRoute.Failure;
            _pendingResultTitle = "征程失败";
            _pendingResultSummary = $"军团止步于第 {Math.Max(1, App.Meta.HighestRegion)} 区。\n战斗摘要：{PlayerFacingText.DescribeBattleOutcome(resolution.Outcome)}，耗时 {_pendingBattleResult.Ticks * BattleTiming.TickSeconds:0.0} 秒。";
        }
        else if (_pendingFinalBoss)
        {
            _postBattleRoute = PostBattleRoute.Success;
            _pendingResultTitle = "登塔成功";
            _pendingResultSummary = "塔顶主宰已被击败。新的英雄与更高难度正在等待下一次征程。\n本局战斗摘要：" + _pendingBattleResult.Digest[..12];
        }
        else
            _postBattleRoute = PostBattleRoute.Reward;
        return true;
    }

    internal void ShowBattleReport()
    {
        if (_battleLabBattleActive)
        {
            if (_battleLabReportShown || _battleLabResult is null) return;
            _battleLabReportShown = true;
            _screens.Battle.StopBattle(replacement: false);
            Show(AppScreenId.BattleReport);
            _screens.BattleReport.Bind(_battleLabResult, "战斗实验室", App.Content, "返回原配置");
            return;
        }
        if (_battleReportShown || _pendingBattleResult is null) return;
        _battleReportShown = true;
        Show(AppScreenId.BattleReport);
        _screens.BattleReport.Bind(_pendingBattleResult, _pendingEncounterTitle, App.Content);
        if (!_battleResolutionCommitted)
            _screens.BattleReport.ShowSettlementRetry(_pendingSettlementMessage);
    }

    private void ContinueAfterBattleReport()
    {
        if (_battleLabBattleActive)
        {
            ReturnToBattleLabConfiguration();
            return;
        }
        if (!_battleReportShown || _battleReportContinued) return;
        if (!_battleResolutionCommitted && !TryResolvePendingBattle()) return;
        _battleReportContinued = true;
        if (_postBattleRoute == PostBattleRoute.Reward) ShowCombatReward();
        else ShowResult(_pendingResultTitle, _pendingResultSummary);
    }

    internal void ShowRecruitment()
    {
        _screens.Recruitment.BindRecruitment(App, _presentation.UnitChoiceCard, _presentation.SemanticIcons);
        Show(AppScreenId.Recruitment);
    }

    internal void ShowCombatReward()
    {
        _screens.Reward.BindCombatReward(
            App,
            _presentation.ChoiceCard,
            _presentation.ItemChoiceCard,
            _presentation.SemanticIcons);
        Show(AppScreenId.Reward);
    }

    private void ClaimRecruit(string stableId)
    {
        if (!App.Recruit(stableId)) return;
        App.FinishNonCombatNode();
        ShowTower();
    }

    private void ClaimItem(string stableId)
    {
        if (App.GrantItem(stableId)) ShowTower();
    }

    private void ConvertRecruit()
    {
        App.ConvertRecruitToGold();
        App.FinishNonCombatNode();
        ShowTower();
    }

    private void SkipRecruitment()
    {
        App.FinishNonCombatNode();
        ShowTower();
    }

    internal void ShowShop()
    {
        _screens.Shop.Bind(
            App,
            _presentation.ChoiceCard,
            _presentation.ItemChoiceCard,
            _presentation.SemanticIcons);
        Show(AppScreenId.Shop);
    }

    private void BuyItem(string stableId)
    {
        var success = App.BuyItem(stableId);
        ShowShop();
        _screens.Shop.ShowPurchaseResult(success);
    }

    private void LeaveShop()
    {
        App.FinishNonCombatNode();
        ShowTower();
    }

    private void ResolveEvent(bool risky)
    {
        App.ResolveEvent(risky);
        App.FinishNonCombatNode();
        ShowTower();
    }

    private void ResolveRest(bool takeGold)
    {
        App.Rest(takeGold);
        App.FinishNonCombatNode();
        ShowTower();
    }

    private void AbandonRun()
    {
        App.AbandonRun();
        ShowResult("征程已放弃", "本次军团已解散，已解锁的英雄与历史进度仍会保留。可随时再次登塔。");
    }

    internal void ShowSettings()
    {
        _screens.Settings.Bind(App.Settings);
        Show(AppScreenId.Settings);
    }

    private void SaveSettings(SettingsIntent intent)
    {
        App.Settings.MasterVolume = intent.MasterVolume;
        App.Settings.DefaultBattleSpeed = intent.DefaultBattleSpeed;
        App.SaveSettings();
        ShowMainMenu();
    }

    internal void ShowResult(string title, string summary)
    {
        _screens.Result.Bind(title, summary);
        Show(AppScreenId.Result);
    }

    internal void Show(AppScreenId id) =>
        _screens.Show(id, App.ActiveRun, App.Content, App.Rules);

    internal void SetEncounterForTesting(EncounterPlan encounter) => _encounter = encounter;

    internal bool BattleResolutionCommitted => _battleResolutionCommitted;
    internal RunBattleResolution? PendingBattleResolution => _pendingBattleResolution;

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
        _battleResolutionCommitted = false;
        _battleReportShown = false;
        _battleReportContinued = false;
    }

    private enum PostBattleRoute { None, Reward, Success, Failure }
}
