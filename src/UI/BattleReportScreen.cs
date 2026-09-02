using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;

namespace TowerAutobattler.UI;

public partial class BattleReportScreen : Control
{
    public event Action? ContinueRequested;

    [Export] public PackedScene LeaderboardRowScene { get; set; } = null!;

    private Label _outcome = null!;
    private Label _encounter = null!;
    private IconText _duration = null!;
    private SemanticChip _commandMeta = null!;
    private BattleReportTeamSummary _playerSummary = null!;
    private BattleReportTeamSummary _enemySummary = null!;
    private Button _overviewTab = null!;
    private Button _offenseTab = null!;
    private Button _survivalTab = null!;
    private Button _healingTab = null!;
    private Button _playerTab = null!;
    private Button _enemyTab = null!;
    private Label _rosterContext = null!;
    private ScrollContainer _reportContentScroll = null!;
    private Control _overviewPage = null!;
    private BattleReportComparison _overviewComparison = null!;
    private BattleReportRosterStrip _playerRosterStrip = null!;
    private BattleReportRosterStrip _enemyRosterStrip = null!;
    private Control _leaderboardPage = null!;
    private Label _leaderboardTitle = null!;
    private BattleReportLeaderboardHeader _leaderboardHeader = null!;
    private VBoxContainer _leaderboardList = null!;
    private Control _emptyState = null!;
    private Label _emptyStateText = null!;
    private BattleReportUnitDetail _unitDetail = null!;
    private Label _settlementMessage = null!;
    private Button _continue = null!;
    private readonly List<BattleReportLeaderboardRow> _rows = [];
    private BattleResult? _result;
    private ContentRegistry? _content;
    private int _team;
    private BattleReportDimension _dimension;
    private bool _continueReported;

    public int SelectedTeam => _team;
    public BattleReportDimension SelectedDimension => _dimension;
    public string? SelectedRuntimeId { get; private set; }

    public override void _Ready()
    {
        _outcome = GetNode<Label>("%ReportOutcome");
        _encounter = GetNode<Label>("%ReportEncounter");
        _duration = GetNode<IconText>("%ReportDuration");
        _commandMeta = GetNode<SemanticChip>("%CommandMeta");
        _playerSummary = GetNode<BattleReportTeamSummary>("%PlayerSummary");
        _enemySummary = GetNode<BattleReportTeamSummary>("%EnemySummary");
        _overviewTab = GetNode<Button>("%OverviewTab");
        _offenseTab = GetNode<Button>("%OffenseTab");
        _survivalTab = GetNode<Button>("%SurvivalTab");
        _healingTab = GetNode<Button>("%HealingTab");
        _playerTab = GetNode<Button>("%PlayerTab");
        _enemyTab = GetNode<Button>("%EnemyTab");
        _rosterContext = GetNode<Label>("%RosterContext");
        _reportContentScroll = GetNode<ScrollContainer>("%ReportContentScroll");
        _overviewPage = GetNode<Control>("%OverviewPage");
        _overviewComparison = GetNode<BattleReportComparison>("%OverviewComparison");
        _playerRosterStrip = GetNode<BattleReportRosterStrip>("%PlayerRosterStrip");
        _enemyRosterStrip = GetNode<BattleReportRosterStrip>("%EnemyRosterStrip");
        _leaderboardPage = GetNode<Control>("%LeaderboardPage");
        _leaderboardTitle = GetNode<Label>("%LeaderboardTitle");
        _leaderboardHeader = GetNode<BattleReportLeaderboardHeader>("%LeaderboardHeader");
        _leaderboardList = GetNode<VBoxContainer>("%LeaderboardList");
        _emptyState = GetNode<Control>("%EmptyState");
        _emptyStateText = GetNode<Label>("%EmptyStateText");
        _unitDetail = GetNode<BattleReportUnitDetail>("%UnitDetail");
        _settlementMessage = GetNode<Label>("%SettlementMessage");
        _continue = GetNode<Button>("%ReportContinue");

        _overviewTab.Pressed += ShowOverview;
        _offenseTab.Pressed += ShowOffense;
        _survivalTab.Pressed += ShowSurvival;
        _healingTab.Pressed += ShowHealing;
        _playerTab.Pressed += ShowPlayerTeam;
        _enemyTab.Pressed += ShowEnemyTeam;
        _continue.Pressed += RequestContinue;
    }

    public override void _ExitTree()
    {
        _overviewTab.Pressed -= ShowOverview;
        _offenseTab.Pressed -= ShowOffense;
        _survivalTab.Pressed -= ShowSurvival;
        _healingTab.Pressed -= ShowHealing;
        _playerTab.Pressed -= ShowPlayerTeam;
        _enemyTab.Pressed -= ShowEnemyTeam;
        _continue.Pressed -= RequestContinue;
        ClearLeaderboard();
        ContinueRequested = null;
    }

    public void Bind(BattleResult result, string encounterName, ContentRegistry content, string continueText = "继续")
    {
        _result = result;
        _content = content;
        _team = 0;
        _dimension = BattleReportDimension.Overview;
        SelectedRuntimeId = null;
        _continueReported = false;
        _continue.Disabled = false;
        _continue.Text = continueText;
        _settlementMessage.Visible = false;
        _settlementMessage.Text = string.Empty;
        _outcome.Text = PlayerFacingText.DescribeBattleOutcome(result.Outcome);
        _outcome.ThemeTypeVariation = result.Outcome switch
        {
            BattleOutcome.PlayerVictory => "HealingTitleLabel",
            BattleOutcome.Timeout => "WarningTitleLabel",
            _ => "EnemyTitleLabel"
        };
        _encounter.Text = encounterName;
        _duration.Bind(Icon(SemanticIconKeys.Time), $"模拟时长 {result.Ticks * BattleTiming.TickSeconds:0.0} 秒", "SecondaryLabel");
        _commandMeta.Bind(
            SemanticIconKeys.Gold,
            $"战术指令 {result.SuccessfulTacticalCommandUses} 次 · 指令金币 {result.GoldSpent}",
            "GoldValue");
        BindComparison();
        BindPage();
        _overviewTab.GrabFocus();
    }

    public void ShowSettlementRetry(string message)
    {
        _settlementMessage.Text = string.IsNullOrWhiteSpace(message)
            ? "战斗结算暂未完成，请重试。"
            : message;
        _settlementMessage.Visible = true;
        _continue.Text = "重试结算";
        _continue.Disabled = false;
        _continueReported = false;
        _continue.GrabFocus();
    }

    private void BindComparison()
    {
        var model = BattleReportViewModels.Build(_result!, _team, _dimension);
        _playerSummary.Bind(model.PlayerTeam);
        _enemySummary.Bind(model.EnemyTeam);
    }

    private void BindPage()
    {
        var model = BattleReportViewModels.Build(_result!, _team, _dimension);
        var overview = _dimension == BattleReportDimension.Overview;
        _overviewPage.Visible = overview;
        _leaderboardPage.Visible = !overview;
        _rosterContext.Text = $"{SideName(_team)} · {DimensionName(_dimension)}";
        _reportContentScroll.ScrollVertical = 0;

        _overviewTab.SetPressedNoSignal(overview);
        _offenseTab.SetPressedNoSignal(_dimension == BattleReportDimension.Offense);
        _survivalTab.SetPressedNoSignal(_dimension == BattleReportDimension.Survival);
        _healingTab.SetPressedNoSignal(_dimension == BattleReportDimension.Healing);
        _playerTab.SetPressedNoSignal(_team == 0);
        _enemyTab.SetPressedNoSignal(_team == 1);

        if (overview) BindOverview(model);
        else BindLeaderboard(model);
    }

    private void BindOverview(BattleReportViewModel model)
    {
        ClearLeaderboard();
        _emptyState.Visible = false;
        _overviewComparison.Bind(
            model.PlayerTeam,
            model.EnemyTeam,
            BattleReportViewModels.BuildCoreMatchups(_result!));
        _playerRosterStrip.Bind("我方阵容", RosterModels(model.PlayerRoster));
        _enemyRosterStrip.Bind("敌方阵容", RosterModels(model.EnemyRoster));
    }

    private void BindLeaderboard(BattleReportViewModel model)
    {
        ClearLeaderboard();
        _leaderboardHeader.Bind(_dimension);
        _leaderboardTitle.Text = $"{SideName(_team)} · {DimensionName(_dimension)}排行榜";
        _emptyState.Visible = model.ShowHealingEmptyState;
        _leaderboardHeader.Visible = !model.ShowHealingEmptyState;
        _leaderboardList.Visible = !model.ShowHealingEmptyState;
        _unitDetail.Visible = !model.ShowHealingEmptyState;
        if (model.ShowHealingEmptyState)
        {
            SelectedRuntimeId = null;
            _emptyStateText.Text = $"{SideName(_team)}没有产生有效治疗\n切换到“战局总览”仍可查看双方结论与完整阵容。";
            return;
        }

        for (var index = 0; index < model.Units.Count; index++)
        {
            var unit = model.Units[index];
            var row = LeaderboardRowScene.Instantiate<BattleReportLeaderboardRow>();
            row.Name = $"Row_{unit.Unit.RuntimeId}";
            _leaderboardList.AddChild(row);
            row.Bind(
                index + 1,
                unit,
                _dimension,
                model.PrimaryMaximum,
                PortraitDefinition(unit.Unit),
                FallbackPortrait(unit.Unit));
            row.Selected += SelectRow;
            _rows.Add(row);
        }

        var selected = _rows.FirstOrDefault(row => row.RuntimeId == SelectedRuntimeId) ?? _rows[0];
        SelectRow(selected);
    }

    private void SelectRow(BattleReportLeaderboardRow row)
    {
        if (!_rows.Contains(row)) return;
        SelectedRuntimeId = row.RuntimeId;
        foreach (var candidate in _rows) candidate.SetSelected(candidate == row);
        _unitDetail.Bind(row.Model, PortraitDefinition(row.Model.Unit), FallbackPortrait(row.Model.Unit));
        _unitDetail.Visible = true;
    }

    private void ClearLeaderboard()
    {
        foreach (var row in _rows)
        {
            row.Selected -= SelectRow;
            if (row.GetParent() == _leaderboardList) _leaderboardList.RemoveChild(row);
            row.QueueFree();
        }
        _rows.Clear();
    }

    private BattleReportRosterPortraitModel[] RosterModels(IReadOnlyList<BattleUnitReportSnapshot> units) => units
        .Select(unit => new BattleReportRosterPortraitModel(unit, PortraitDefinition(unit), FallbackPortrait(unit)))
        .ToArray();

    private UnitPortraitDefinition? PortraitDefinition(BattleUnitReportSnapshot unit)
    {
        return _content?.TryGet(unit.ContentId, out var entry) == true && entry.Definition is UnitDefinition definition
            ? definition.Portrait
            : null;
    }

    private static Texture2D FallbackPortrait(BattleUnitReportSnapshot unit) => Icon(
        unit.IsHero ? SemanticIconKeys.Hero : unit.Role is UnitRole.Ranged or UnitRole.Artillery ? SemanticIconKeys.Ranged : SemanticIconKeys.Melee);

    private static Texture2D Icon(StringName key) => SemanticIcons.Catalog.ResolveIcon(key)
        ?? throw new InvalidOperationException($"Missing semantic icon '{key}'.");

    private void ShowOverview() => SetDimension(BattleReportDimension.Overview, _overviewTab);
    private void ShowOffense() => SetDimension(BattleReportDimension.Offense, _offenseTab);
    private void ShowSurvival() => SetDimension(BattleReportDimension.Survival, _survivalTab);
    private void ShowHealing() => SetDimension(BattleReportDimension.Healing, _healingTab);

    private void SetDimension(BattleReportDimension dimension, Button focusTarget)
    {
        _dimension = dimension;
        BindPage();
        focusTarget.GrabFocus();
    }

    private void ShowPlayerTeam() => SetTeam(0, _playerTab);
    private void ShowEnemyTeam() => SetTeam(1, _enemyTab);

    private void SetTeam(int team, Button focusTarget)
    {
        _team = team;
        BindPage();
        focusTarget.GrabFocus();
    }

    private void RequestContinue()
    {
        if (_continueReported) return;
        _continueReported = true;
        _continue.Disabled = true;
        ContinueRequested?.Invoke();
    }

    private static string SideName(int team) => team == 0 ? "我方" : "敌方";

    private static string DimensionName(BattleReportDimension dimension) => dimension switch
    {
        BattleReportDimension.Offense => "输出",
        BattleReportDimension.Survival => "生存",
        BattleReportDimension.Healing => "治疗",
        _ => "战局总览"
    };
}
