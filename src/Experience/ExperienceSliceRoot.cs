using System;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;
using TowerAutobattler.Presentation;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

namespace TowerAutobattler.Experience;

// Separate composition root: no GameFlowCoordinator or production save service is created.
public partial class ExperienceSliceRoot : Control
{
    [Export] public GameProjectDefinition? ProjectDefinition { get; set; }
    [Export] public ExperienceSliceDefinition? SliceDefinition { get; set; }
    private ExperienceSlicePanel _panel = null!;
    private DeploymentScreenController _deployment = null!;
    private BattleScreenController _battle = null!;
    private BattleReportScreen _report = null!;
    private ConfirmationDialog _exitDialog = null!;
    private ScreenRouter _router = null!;
    private string _battleTitle = "";
    private bool _reportShown;
    public ExperienceSliceSession? Session { get; private set; }
    public string BootstrapFailure { get; private set; } = "";
    public event Action? ReturnRequested;

    public override async void _Ready()
    {
        _panel = GetNode<ExperienceSlicePanel>("Screens/ExperiencePanel");
        _deployment = GetNode<DeploymentScreenController>("Screens/DeploymentScreen");
        _battle = GetNode<BattleScreenController>("Screens/BattleScreen");
        _report = GetNode<BattleReportScreen>("Screens/BattleReportScreen");
        _exitDialog = GetNode<ConfirmationDialog>("ExitDialog");
        var army = GetNode<ArmyOverviewController>("ArmyOverview");
        army.BindModalFocusScope(GetNode<Control>("Screens"));
        _router = new ScreenRouter([_panel, _deployment, _battle, _report], [_panel, _deployment], army);
        _panel.ChoiceCommitted += Choose;
        _panel.SkipRequested += SkipRecruit;
        _panel.PrepareRequested += ShowDeployment;
        _panel.RetryRequested += Retry;
        _panel.EquipmentMoveRequested += MoveEquipment;
        _panel.ExitRequested += AskExit;
        _exitDialog.Confirmed += ReturnToMenu;
        _deployment.BackRequested += ShowPanel;
        _deployment.StartRequested += StartBattle;
        _deployment.MoveRequested += Move;
        _deployment.WithdrawRequested += Withdraw;
        _battle.Finished += FinishBattle;
        _battle.EndTransitionFinished += ShowReport;
        _battle.TacticalCommandAttempted += RecordCommand;
        _report.ContinueRequested += ContinueReport;
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this, ProjectDefinition);
            if (!IsInstanceValid(this) || !IsInsideTree()) return;
            var package = gate.Package ?? throw new InvalidOperationException(string.Join("\n", gate.Report.CoreErrors));
            SemanticIcons.Configure(package.Project.Presentation.SemanticIcons);
            Session = new ExperienceSliceSession(package, SliceDefinition ?? throw new InvalidOperationException("缺少试验定义。"));
            ShowPanel();
        }
        catch (Exception exception)
        {
            BootstrapFailure = exception.Message;
            _panel.ShowFeedback("试验未能启动，未读取或修改正式存档。\n" + exception.Message);
            GD.PushError("Experience slice: " + exception);
        }
    }

    public override void _ExitTree()
    {
        _panel.ChoiceCommitted -= Choose; _panel.SkipRequested -= SkipRecruit;
        _panel.PrepareRequested -= ShowDeployment; _panel.RetryRequested -= Retry;
        _panel.EquipmentMoveRequested -= MoveEquipment; _panel.ExitRequested -= AskExit;
        _exitDialog.Confirmed -= ReturnToMenu;
        _deployment.BackRequested -= ShowPanel; _deployment.StartRequested -= StartBattle;
        _deployment.MoveRequested -= Move; _deployment.WithdrawRequested -= Withdraw;
        _battle.Finished -= FinishBattle; _battle.EndTransitionFinished -= ShowReport;
        _battle.TacticalCommandAttempted -= RecordCommand;
        _report.ContinueRequested -= ContinueReport;
        _battle.StopBattle();
        Session = null;
    }

    private void Show(Control target)
    {
        if (Session is null) return;
        _router.Show(target, Session.Application.ActiveRun, Session.Application.Content, Session.Application.Rules);
    }
    private void ShowPanel()
    {
        if (Session is null) return;
        _panel.Bind(Session); Show(_panel);
    }
    private void Choose(string id)
    {
        if (Session is null) return;
        var accepted = Session.Stage switch
        {
            ExperienceStage.Relic => Session.ChooseRelic(id),
            ExperienceStage.Recruit => Session.ChooseRecruit(id),
            ExperienceStage.Equipment => Session.ChooseEquipment(id),
            _ => false
        };
        if (accepted) ShowPanel(); else _panel.ShowFeedback("选择已失效或条件不满足，没有重复领取。");
    }
    private void SkipRecruit() { if (Session?.ChooseRecruit(null) == true) ShowPanel(); }
    private void Retry(bool afterFirst)
    {
        if (Session?.Retry(afterFirst) != true) return;
        _reportShown = false;
        ShowPanel();
        _panel.ShowFeedback("已创建新尝试；之前的选择与战报摘要保留在下方记录中。仅在本次打开期间保留。");
    }
    private void MoveEquipment(string id, string? owner, int slot)
    {
        if (Session is null) return;
        var before = Session.PreparedStats();
        if (!Session.MoveEquipment(id, owner, slot)) { _panel.ShowFeedback("无法移动装备，原归属未变。"); return; }
        var after = Session.PreparedStats();
        ShowPanel();
        _panel.ShowFeedback("装备归属已更新。\n" + ExperienceSliceSession.DescribeChanges(before, after));
    }
    private void ShowDeployment()
    {
        if (Session?.IsPreparing != true) return;
        _deployment.Bind(Session.Application, Session.Encounter, equipmentEditing: false);
        Show(_deployment);
        _deployment.ShowMessage("玩法试验 · 返回可查看奖励／换装。布阵不会改变已领取的选择。", false);
    }
    private void Move(FormationMoveCommand command)
    {
        if (Session?.IsPreparing != true || _deployment.FloorRule is not { } floor) return;
        var evaluation = Session.Application.EvaluateFormationCommand(command, floor);
        if (!evaluation.IsValid) { _deployment.ShowMessage(evaluation.RejectionReason, true); return; }
        if (Session.Application.ApplyFormationCommand(command, floor)) ShowDeployment();
    }
    private void Withdraw(string id)
    {
        if (Session?.IsPreparing != true) return;
        if (Session.Application.WithdrawDeploymentUnit(id)) ShowDeployment();
        else _deployment.ShowMessage("当前无法撤回：请检查后备容量与部署状态。", true);
    }
    private void StartBattle()
    {
        if (Session?.IsPreparing != true) return;
        try
        {
            _battleTitle = Session.Encounter.Title;
            var config = Session.StartBattle();
            _reportShown = false;
            Show(_battle);
            _battle.StartBattle(Session.Application.Content, config, _battleTitle, 1f);
        }
        catch (Exception exception)
        {
            _battle.StopBattle(); Session.CancelFailedStart();
            ShowDeployment(); _deployment.ShowMessage(exception.Message, true);
        }
    }
    private void FinishBattle(BattleResult result)
    {
        if (Session?.AcceptResult(result) == true) return;
        _battle.StopBattle(); Session?.CancelFailedStart(); ShowPanel();
        _panel.ShowFeedback("战斗结果未通过结算校验，没有发放奖励。可重试当前准备。");
    }
    private void ShowReport()
    {
        if (Session?.LastResult is not { } result || _reportShown ||
            Session.Stage is not (ExperienceStage.ReportFirst or ExperienceStage.ReportSecond)) return;
        _reportShown = true;
        _battle.StopBattle(replacement: false);
        _report.Bind(result, _battleTitle, Session.Application.Content,
            Session.Stage == ExperienceStage.ReportFirst && result.Outcome == BattleOutcome.PlayerVictory ? "去选择装备 →" : "回顾这次尝试 →");
        Show(_report);
    }
    private void ContinueReport() { if (Session?.ContinueReport() == true) ShowPanel(); }
    private void RecordCommand(int tick, int slot, string target, bool succeeded, string reason) =>
        Session?.RecordCommand(tick, slot, target, succeeded, reason);
    private void AskExit() => _exitDialog.PopupCentered();
    private void ReturnToMenu()
    {
        if (ReturnRequested is not null) ReturnRequested.Invoke();
        else GetTree().ChangeSceneToFile("res://scenes/app/GameRoot.tscn");
    }
}
