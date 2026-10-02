using System;
using TowerAutobattler.Battle;
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
    private readonly Action<string>? _feedback;
    private readonly CompiledProjectPresentation _presentation;
    private readonly RunBattleFlow _runBattle;
    private readonly BattleLabFlow _battleLab;
    private bool _connected;

    public GameFlowCoordinator(
        Func<RunApplication?> application,
        AppScreenHost screens,
        CompiledProjectPresentation presentation,
        Action quit,
        Action<string>? feedback = null)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
        _screens = screens ?? throw new ArgumentNullException(nameof(screens));
        _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        _quit = quit ?? throw new ArgumentNullException(nameof(quit));
        _feedback = feedback;
        _runBattle = new(application, screens.Deployment, screens.Battle, screens.BattleReport,
            new(Show, ShowTower, ShowCombatReward, ShowResult, screens.OpenGrowthWorkbench));
        _battleLab = new(application, screens.BattleLab, screens.Battle, screens.BattleReport,
            Show, ShowMainMenu, ShowResult);
    }

    private RunApplication App => _application() ??
        throw new InvalidOperationException("Run application has not completed bootstrap.");

    public void Start()
    {
        if (_connected) return;
        _screens.MainMenu.NewRunRequested += ShowHeroSelection;
        _screens.GrowthChanged += RefreshGrowth;
        _screens.MainMenu.ContinueRequested += ContinueRun;
        _screens.MainMenu.BattleLabRequested += ShowBattleLab;
        _screens.MainMenu.SettingsRequested += ShowSettings;
        _screens.MainMenu.QuitRequested += _quit;
        _screens.HeroSelection.OpeningHeroToggled += ToggleOpeningHero;
        _screens.HeroSelection.OpeningConfirmed += ConfirmOpening;
        _screens.HeroSelection.BackRequested += ShowMainMenu;
        _screens.Tower.NodeSelected += SelectNode;
        _screens.Tower.OpponentSelected += SelectOpponent;
        _screens.Tower.AbandonRequested += AbandonRun;
        _screens.Deployment.BackRequested += ShowTower;
        _screens.Deployment.StartRequested += StartBattle;
        _screens.Deployment.GrowthWorkbenchRequested += _screens.OpenGrowthWorkbench;
        _screens.Deployment.MoveRequested += MoveDeploymentUnit;
        _screens.Deployment.WithdrawRequested += WithdrawDeploymentUnit;
        _screens.Reward.ChoiceRequested += ClaimItem;
        _screens.Reward.ContinueRequested += SkipRunOffer;
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
        _runBattle.Dispose();
        if (!_connected) return;
        _screens.MainMenu.NewRunRequested -= ShowHeroSelection;
        _screens.GrowthChanged -= RefreshGrowth;
        _screens.MainMenu.ContinueRequested -= ContinueRun;
        _screens.MainMenu.BattleLabRequested -= ShowBattleLab;
        _screens.MainMenu.SettingsRequested -= ShowSettings;
        _screens.MainMenu.QuitRequested -= _quit;
        _screens.HeroSelection.OpeningHeroToggled -= ToggleOpeningHero;
        _screens.HeroSelection.OpeningConfirmed -= ConfirmOpening;
        _screens.HeroSelection.BackRequested -= ShowMainMenu;
        _screens.Tower.NodeSelected -= SelectNode;
        _screens.Tower.OpponentSelected -= SelectOpponent;
        _screens.Tower.AbandonRequested -= AbandonRun;
        _screens.Deployment.BackRequested -= ShowTower;
        _screens.Deployment.StartRequested -= StartBattle;
        _screens.Deployment.GrowthWorkbenchRequested -= _screens.OpenGrowthWorkbench;
        _screens.Deployment.MoveRequested -= MoveDeploymentUnit;
        _screens.Deployment.WithdrawRequested -= WithdrawDeploymentUnit;
        _screens.Reward.ChoiceRequested -= ClaimItem;
        _screens.Reward.ContinueRequested -= SkipRunOffer;
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
        _battleLab.Dispose();
        _connected = false;
    }

    internal void ShowMainMenu()
    {
        _screens.MainMenu.Bind(App.Run is not null);
        Show(AppScreenId.MainMenu);
    }

    internal void ShowHeroSelection()
    {
        if (!App.BeginOpeningRecruitment((ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))
        {
            ShowResult("开局未能保存", "原征程已保留。无法写入新存档或备份旧档，请检查存档目录的访问权限和可用空间后重试。");
            return;
        }
        ShowOpeningDraft();
    }

    private void ShowOpeningDraft()
    {
        _screens.HeroSelection.BindOpening(App);
        Show(AppScreenId.HeroSelection);
    }

    private void ToggleOpeningHero(string heroId)
    {
        if (App.ToggleOpeningHero(heroId)) _screens.HeroSelection.RefreshOpening(App.Run!.OpeningRecruitment!);
        else _screens.HeroSelection.ShowOpeningError("未能保存选择，原选择已保留，请重试。");
    }

    private void ConfirmOpening()
    {
        if (App.ConfirmOpeningRecruitment()) ShowTower();
        else _screens.HeroSelection.ShowOpeningError("确认未成功，候选和原选择已保留，请重试。");
    }

    private void ContinueRun()
    {
        if (App.Run is null) { ShowHeroSelection(); return; }
        ShowTower();
    }

    internal void ShowTower()
    {
        if (App.Run is not { } run) { ShowMainMenu(); return; }
        if (run.OpeningRecruitment is not null) { ShowOpeningDraft(); return; }
        if (!string.IsNullOrEmpty(run.TerminalCompletionId))
        {
            var victory = run.TerminalVictory;
            var summary = run.LastBattleConsequence is { } consequence ? RunHealthText.Consequence(consequence) : "终局记录已安全保存。";
            if (App.ResumeTerminalCompletion()) ShowResult(victory ? "登塔成功" : "征程结束", summary);
            else
            {
                _screens.Reward.ShowTerminalFailure("终局记录尚未保存，原征程已保留；修复存储问题后可重试，不会重复发放胜利奖励。");
                Show(AppScreenId.Reward);
            }
            return;
        }
        if (run.PendingOffer is not null) { ShowPendingOffer(); return; }
        if (run.PendingNode) { OpenSelectedNode(); return; }
        _screens.Tower.Bind(App, _presentation.ChoiceCard, _presentation.SemanticIcons);
        Show(AppScreenId.Tower);
        if (run.Growth?.PendingDiscovery is not null) _screens.OpenGrowthWorkbench();
    }

    private void SelectNode(TowerNodeType type)
    {
        if (App.Run?.Growth?.PendingDiscovery is not null)
        {
            _screens.OpenGrowthWorkbench();
            return;
        }
        if (App.SelectNode(type)) OpenSelectedNode();
    }

    private void SelectOpponent(TowerNodeType type, string opponentOptionId)
    {
        if (App.Run?.Growth?.PendingDiscovery is not null)
        {
            _screens.OpenGrowthWorkbench();
            return;
        }
        if (App.SelectNode(type, opponentOptionId)) OpenSelectedNode();
        else _screens.Tower.ShowMessage("未能确认对手，原候选和征程状态已保留，请重试。");
    }

    internal void OpenSelectedNode()
    {
        if (App.Run is not { } run) { ShowMainMenu(); return; }
        if (run.PendingOffer is not null) { ShowPendingOffer(); return; }
        switch (run.SelectedNode)
        {
            case TowerNodeType.Combat or TowerNodeType.Elite or TowerNodeType.Boss:
                _runBattle.SetEncounter(App.CurrentEncounter());
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
                _screens.Rest.Bind(App.Rules, run.CurrentRunHealth, run.MaximumRunHealth);
                Show(AppScreenId.Rest);
                break;
        }
    }

    private void RefreshGrowth()
    {
        // Rebind the underlying page without dismissing the workbench/focus scope.
        _runBattle.RefreshDeployment();
        if (_screens.Tower.Visible) _screens.Tower.Bind(App, _presentation.ChoiceCard, _presentation.SemanticIcons);
        _screens.BindEquipmentManagement(App);
    }

    internal void ShowRecruitment()
    {
        _screens.Recruitment.BindOffer(App, _presentation.ChoiceCard, _presentation.ItemChoiceCard, _presentation.SemanticIcons);
        Show(AppScreenId.Recruitment);
    }

    internal void ShowCombatReward()
    {
        if (App.PendingOffer is null) { ShowTower(); return; }
        _screens.Reward.BindOffer(
            App,
            _presentation.ChoiceCard,
            _presentation.ItemChoiceCard,
            _presentation.SemanticIcons);
        Show(AppScreenId.Reward);
    }

    private void ClaimRecruit(string stableId)
    {
        ResolveRunChoice(stableId);
    }

    private void ClaimItem(string stableId)
    {
        ResolveRunChoice(stableId);
    }

    private void ConvertRecruit()
    {
        App.ConvertRecruitToGold();
        App.FinishNonCombatNode();
        ShowTower();
    }

    private void SkipRecruitment()
    {
        SkipRunOffer();
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
        if (App.PendingOffer is not { } offer) return;
        var result = App.ResolveOffer(offer.OfferId, stableId);
        _feedback?.Invoke(result.Succeeded ? "purchase" : "ui_error");
        if (App.PendingOffer is null) ShowTower(); else ShowShop();
        _screens.Shop.ShowDecisionResult(result);
    }

    private void LeaveShop()
    {
        SkipRunOffer();
    }

    private void ResolveEvent(bool risky)
    {
        ResolveRunChoice(risky ? "risky" : "safe");
    }

    private void ResolveRest(bool takeGold)
    {
        ResolveRunChoice(takeGold ? "gold" : "recover_run_health");
    }

    private void ShowPendingOffer()
    {
        if (App.PendingOffer is not { } offer) return;
        if (offer.Kind == RunOfferKind.Shop) { ShowShop(); return; }
        if (offer.Kind == RunOfferKind.Recruitment) { ShowRecruitment(); return; }
        ShowCombatReward();
    }

    private void ResolveRunChoice(string choiceId)
    {
        if (App.PendingOffer is not { } offer) return;
        var result = App.ResolveOffer(offer.OfferId, choiceId);
        if (!result.Succeeded)
        {
            _feedback?.Invoke("ui_error");
            if (offer.Kind == RunOfferKind.Recruitment) _screens.Recruitment.ShowDecisionMessage(result.Message);
            else _screens.Reward.ShowDecisionMessage(result.Message);
            return;
        }
        ShowTower();
    }

    private void SkipRunOffer()
    {
        if (App.PendingOffer is not { } offer) { ShowTower(); return; }
        var result = App.ResolveOffer(offer.OfferId, null);
        if (result.Succeeded) ShowTower();
        else if (offer.Kind == RunOfferKind.Shop) _screens.Shop.ShowDecisionResult(result);
        else if (offer.Kind == RunOfferKind.Recruitment) _screens.Recruitment.ShowDecisionMessage(result.Message);
        else _screens.Reward.ShowDecisionMessage(result.Message);
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
        App.Settings.ReduceUiMotion = intent.ReduceUiMotion;
        App.SaveSettings();
        ShowMainMenu();
    }

    internal void ShowResult(string title, string summary)
    {
        _screens.Result.Bind(title, summary);
        Show(AppScreenId.Result);
    }

    internal void Show(AppScreenId id)
    {
        _screens.BindEquipmentManagement(App);
        var displayedRun = _battleLab.BattleActive && id is AppScreenId.Battle or AppScreenId.BattleReport
            ? null : App.Run;
        _screens.Show(id, displayedRun, App.Content, App.Rules);
    }

    internal void SetEncounterForTesting(EncounterPlan encounter) => _runBattle.SetEncounter(encounter);

    internal bool BattleResolutionCommitted => _runBattle.BattleResolutionCommitted;
    internal RunBattleResolution? PendingBattleResolution => _runBattle.PendingBattleResolution;

    internal void ShowDeployment() => _runBattle.ShowDeployment();
    internal void ShowBattleLab() => _battleLab.ShowBattleLab();
    private void StartBattle() => _runBattle.StartBattle();
    private void MoveDeploymentUnit(FormationMoveCommand command) => _runBattle.MoveDeploymentUnit(command);
    private void WithdrawDeploymentUnit(string id) => _runBattle.WithdrawDeploymentUnit(id);
    private void StartBattleLabBattle() => _battleLab.StartBattleLabBattle();
    private void ResetBattleLabBattle() => _battleLab.ResetBattleLabBattle();
    private void ReturnToBattleLabConfiguration() => _battleLab.ReturnToBattleLabConfiguration();
    internal void ResetPendingBattleFlow() => _runBattle.ResetPendingBattleFlow();
    internal void AcceptBattleResult(BattleResult result)
    {
        if (_battleLab.BattleActive) _battleLab.AcceptBattleResult(result);
        else _runBattle.AcceptBattleResult(result);
    }
    internal void ShowBattleReport()
    {
        if (_battleLab.BattleActive) _battleLab.ShowBattleReport();
        else _runBattle.ShowBattleReport();
    }
    private void ContinueAfterBattleReport()
    {
        if (_battleLab.BattleActive) _battleLab.ReturnToBattleLabConfiguration();
        else _runBattle.ContinueAfterBattleReport();
    }
}
