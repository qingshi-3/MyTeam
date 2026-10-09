using System;
using System.Linq;
using Godot;
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
    private int _expeditionSelection = -1;
    private int _expeditionKeyboardTarget = -1;
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
        _screens.Expedition.CellSelected += SelectExpeditionCell;
        _screens.Expedition.MoveRequested += MoveExpedition;
        _screens.Expedition.StepRequested += StepExpedition;
        _screens.Expedition.InteractRequested += InteractExpedition;
        _screens.Expedition.InspectRequested += InspectExpeditionSite;
        _screens.Expedition.BossRequested += PreviewExpeditionBoss;
        _screens.Expedition.ArmyRequested += _screens.OpenArmyOverview;
        _screens.Expedition.MenuRequested += ShowMainMenu;
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
        _screens.Expedition.CellSelected -= SelectExpeditionCell;
        _screens.Expedition.MoveRequested -= MoveExpedition;
        _screens.Expedition.StepRequested -= StepExpedition;
        _screens.Expedition.InteractRequested -= InteractExpedition;
        _screens.Expedition.InspectRequested -= InspectExpeditionSite;
        _screens.Expedition.BossRequested -= PreviewExpeditionBoss;
        _screens.Expedition.ArmyRequested -= _screens.OpenArmyOverview;
        _screens.Expedition.MenuRequested -= ShowMainMenu;
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
        _expeditionSelection = -1;
        _expeditionKeyboardTarget = -1;
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
            var victoryTitle = App.IsExpedition ? "征程胜利" : "登塔成功";
            if (App.ResumeTerminalCompletion()) ShowResult(victory ? victoryTitle : "征程结束", summary);
            else
            {
                _screens.Reward.ShowTerminalFailure("终局记录尚未保存，原征程已保留；修复存储问题后可重试，不会重复发放胜利奖励。");
                Show(AppScreenId.Reward);
            }
            return;
        }
        if (App.IsExpedition && App.ExpeditionBattleCommitted)
        {
            _runBattle.SetEncounter(App.CurrentEncounter());
            _runBattle.StartBattle();
            return;
        }
        if (run.PendingOffer is not null) { ShowPendingOffer(); return; }
        if (App.IsExpedition)
        {
            ShowExpedition();
            if (run.Growth?.PendingDiscovery is not null) _screens.OpenGrowthWorkbench();
            return;
        }
        if (run.PendingNode) { OpenSelectedNode(); return; }
        _screens.Tower.Bind(App, _presentation.ChoiceCard, _presentation.SemanticIcons);
        Show(AppScreenId.Tower);
        if (run.Growth?.PendingDiscovery is not null) _screens.OpenGrowthWorkbench();
    }

    private void ShowExpedition()
    {
        _screens.Expedition.Bind(ExpeditionScreenPresenter.Build(App, _presentation.SemanticIcons, _expeditionSelection));
        Show(AppScreenId.Expedition);
    }

    private void SelectExpeditionCell(int cellIndex)
    {
        _expeditionSelection = cellIndex;
        _screens.Expedition.Bind(ExpeditionScreenPresenter.Build(App, _presentation.SemanticIcons, cellIndex));
    }

    private void MoveExpedition(int cellIndex)
    {
        var moved = App.MoveExpedition(cellIndex);
        if (App.Expedition is { } map)
        {
            _expeditionSelection = map.PositionIndex;
            _expeditionKeyboardTarget = map.PositionIndex;
        }
        ShowExpedition();
        if (!moved) _screens.Expedition.ShowMessage("未能前往此处。请沿已知道路移动，或先处理挡路的守军；原位置已保留。");
    }

    private void StepExpedition(Vector2I direction)
    {
        if (Math.Abs(direction.X) + Math.Abs(direction.Y) != 1 || App.Expedition is not { } map) return;
        if (App.ExpeditionInteractionLocked || map.RemainingActions <= 0)
        {
            _screens.Expedition.ShowMessage("当前不能探索，请先完成待处理事项或准备首领战。");
            return;
        }
        var column = map.PositionIndex % map.Size + direction.X;
        var row = map.PositionIndex / map.Size + direction.Y;
        if (column < 0 || row < 0 || column >= map.Size || row >= map.Size)
        {
            _expeditionKeyboardTarget = map.PositionIndex;
            _expeditionSelection = map.PositionIndex;
            ShowExpedition();
            _screens.Expedition.ShowMessage("已到达大陆边界，请换个方向。");
            return;
        }
        var target = row * map.Size + column;
        _expeditionKeyboardTarget = target;
        _expeditionSelection = target;
        if (App.CanMoveExpedition(target)) { MoveExpedition(target); return; }
        ShowExpedition();
        _screens.Expedition.ShowMessage(map.Cells[target].Walkable && map.Cells[target].Blocked
            ? "前方守军挡路：按回车查看敌情，或换个方向绕行。"
            : "前方地形无法通行，请换个方向。");
    }

    private void InteractExpedition()
    {
        if (App.Expedition is not { } map) return;
        var target = _expeditionKeyboardTarget;
        var adjacent = target >= 0 && target < map.Cells.Length &&
            Math.Abs(target % map.Size - map.PositionIndex % map.Size) +
            Math.Abs(target / map.Size - map.PositionIndex / map.Size) <= 1;
        if (!adjacent || !map.Cells[target].Revealed || !App.CanInteractExpeditionSite(map.Cells[target].SiteId))
            target = map.PositionIndex;
        var cell = map.Cells[target];
        if (cell.Revealed && !string.IsNullOrEmpty(cell.SiteId) && App.CanInteractExpeditionSite(cell.SiteId))
        {
            _expeditionSelection = target;
            InspectExpeditionSite(cell.SiteId);
        }
        else _screens.Expedition.ShowMessage(map.RemainingActions <= 0
            ? "本阶段行动已用尽，请整备军团并选择迎战首领。"
            : "此处没有可交互的地点。走到地点上，或面向相邻守军，再按回车。");
    }

    private void InspectExpeditionSite(string siteId)
    {
        if (App.Run?.Growth?.PendingDiscovery is not null) { _screens.OpenGrowthWorkbench(); return; }
        try
        {
            if (!App.InspectExpeditionSite(siteId))
            {
                ShowExpedition();
                _screens.Expedition.ShowMessage("暂时无法处理此处。请靠近地点并检查剩余行动；原状态已保留。");
                return;
            }
            // Keyboard interaction may address the party's site while the
            // pointer inspects another cell. Present the accepted source before
            // handing the page over; this does not change the interaction target.
            if (App.Expedition is { } map)
            {
                var source = map.Cells.FirstOrDefault(cell => cell.SiteId == siteId);
                if (source is not null) SelectExpeditionCell(source.Index);
            }
            if (App.PendingOffer is not null) { ShowPendingOffer(); return; }
            _runBattle.SetEncounter(App.PreviewExpeditionEncounter(siteId));
            ShowDeployment();
        }
        catch (Exception exception)
        {
            ShowExpedition();
            _screens.Expedition.ShowMessage("无法打开地点详情：" + exception.Message);
        }
    }

    private void PreviewExpeditionBoss()
    {
        if (App.Run?.Growth?.PendingDiscovery is not null) { _screens.OpenGrowthWorkbench(); return; }
        try
        {
            _runBattle.SetEncounter(App.PreviewExpeditionBoss());
            ShowDeployment();
        }
        catch (Exception exception)
        {
            ShowExpedition();
            _screens.Expedition.ShowMessage("无法准备首领战：" + exception.Message);
        }
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
        if (_screens.Expedition.Visible)
            _screens.Expedition.Bind(ExpeditionScreenPresenter.Build(App, _presentation.SemanticIcons, _expeditionSelection));
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
        if (_screens.Recruitment.IsRecruitmentReceiptPlaying ||
            App.PendingOffer is not { Kind: RunOfferKind.Recruitment } offer) return;
        var application = App;
        var result = application.ResolveOffer(offer.OfferId, stableId);
        if (!result.Succeeded)
        {
            _feedback?.Invoke("ui_error");
            _screens.Recruitment.ShowDecisionMessage(result.Message);
            return;
        }
        // Persistence and roster ownership are settled before any animation.
        // Closing/replacing this screen may discard the receipt, never the hero.
        _screens.RefreshRunOverview(application);
        _screens.Recruitment.PresentRecruitmentAccepted(stableId, application, () =>
        {
            if (_connected && ReferenceEquals(_application(), application) &&
                _screens.Recruitment.IsVisibleInTree()) ShowTower();
        });
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
        if (_screens.Recruitment.IsRecruitmentReceiptPlaying) return;
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
