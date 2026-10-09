using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Presentation;
using TowerAutobattler.Attributes;
using TowerAutobattler.Domain;

namespace TowerAutobattler.UI;

public partial class BattleInspectorDock : Control
{
    [Export] public PackedScene StatRowScene { get; set; } = null!;
    public event Action? StatisticsRequested;
    public event Action<string>? UnitSelected;
    public event Action? ExportRequested;
    public SelectedUnitPanel Details { get; private set; } = null!;
    public bool WantsStatistics => _expanded && _statisticsPage;
    private ContextPopup _popup = null!;
    private Button _statisticsButton = null!;
    private Button _unitButton = null!;
    private Button _close = null!;
    private Label _heading = null!;
    private Control _unitPage = null!;
    private Control _emptySelection = null!;
    private Control _statistics = null!;
    private OptionButton _metric = null!;
    private Button _allies = null!;
    private Button _enemies = null!;
    private Label _total = null!;
    private VBoxContainer _rowContainer = null!;
    private Label _emptyStats = null!;
    private Control _expandedPanel = null!;
    private Control _statisticsDetail = null!;
    private RichTextLabel _detailText = null!;
    private Button _export = null!;
    private Button _inspectUnit = null!;
    private Label _exportFeedback = null!;
    private Label _clock = null!;
    private readonly Dictionary<string, BattleStatRow> _rows = new(StringComparer.Ordinal);
    private BattleUnitReportSnapshot[] _reports = [];
    private Func<string, UnitPortraitDefinition?>? _portraitFor;
    private bool _expanded;
    private bool _statisticsPage = true;
    private int _team;
    private string _selectedRuntimeId = "";
    private string _statisticsRuntimeId = "";
    private int _statisticsTicks;
    private bool _detailed;
    private Func<CombatSourceKind, string, string>? _sourceName;
    private Func<string, BattleScreenRuntimeUnitSnapshot?>? _runtimeUnit;

    public override void _Ready()
    {
        Details = GetNode<SelectedUnitPanel>("%SelectedUnitPanel");
        _popup = GetNode<ContextPopup>("%InspectorPopup");
        _statisticsButton = GetNode<Button>("%StatisticsToggle");
        _unitButton = GetNode<Button>("%UnitToggle");
        _close = GetNode<Button>("%CloseInspector");
        _heading = GetNode<Label>("%InspectorHeading");
        _unitPage = GetNode<Control>("%UnitDetailsPage");
        _emptySelection = GetNode<Control>("%EmptySelection");
        _statistics = GetNode<Control>("%StatisticsPage");
        _metric = GetNode<OptionButton>("%StatMetric");
        _allies = GetNode<Button>("%StatAllies");
        _enemies = GetNode<Button>("%StatEnemies");
        _total = GetNode<Label>("%StatTotal");
        _rowContainer = GetNode<VBoxContainer>("%StatRows");
        _emptyStats = GetNode<Label>("%EmptyStatistics");
        _expandedPanel = GetNode<Control>("%ExpandedInspector");
        _statisticsDetail = GetNode<Control>("%StatisticsDetail");
        _detailText = GetNode<RichTextLabel>("%StatisticsDetailText");
        _export = GetNode<Button>("%ExportStatistics");
        _inspectUnit = GetNode<Button>("%InspectStatisticsUnit");
        _exportFeedback = GetNode<Label>("%ExportFeedback");
        _clock = GetNode<Label>("%StatisticsClock");
        _export.Pressed += OnExportRequested;
        _inspectUnit.Pressed += InspectStatisticsUnit;
        _statisticsButton.Pressed += ToggleStatistics;
        _unitButton.Pressed += ToggleUnit;
        _popup.Closed += OnPopupClosed;
        _metric.ItemSelected += OnMetricChanged;
        _allies.Pressed += ShowAllies;
        _enemies.Pressed += ShowEnemies;
        BattleLabHoverHint.Bind(_close, new BattleLabTooltipInfo("关闭信息窗",
            Abilities: "当前选择与战斗状态保留，战场位置不变。", Hint: "Esc 或点击窗外关闭"));
        BattleLabHoverHint.Bind(_allies, new BattleLabTooltipInfo("我方统计", Abilities: "按单位初始阵营归属，包含召唤物与阵亡单位。魅惑后不会搬队。"));
        BattleLabHoverHint.Bind(_enemies, new BattleLabTooltipInfo("敌方统计", Abilities: "按单位初始阵营归属，包含召唤物与阵亡单位。魅惑后不会搬队。"));
        BattleLabHoverHint.Bind(_metric, new BattleLabTooltipInfo("统计指标",
            Abilities: "伤害与承伤为实际扣除生命与护盾吸收之和，不计溢出。治疗不计过量。受到硬控按自身受控时间去重；造成硬控按目标累计，同一目标的不同来源可能重叠。\n条长相对于本侧当前指标最高的单位。"));
        ResetSelection();
    }

    public void BindSelection(BattleScreenRuntimeUnitSnapshot state, UnitPortraitDefinition? portrait)
    {
        _selectedRuntimeId = state.RuntimeId;
        BattleLabHoverHint.Bind(_unitButton, new BattleLabTooltipInfo(state.DisplayName,
            Stats: $"生命 {state.Health:0}/{state.MaxHealth:0}" + (state.Shield > 0 ? $" · 护盾 {state.Shield:0}" : ""),
            Hint: "点击查看单位详情"));
        _emptySelection.Visible = false;
        Details.Bind(state, portrait);
        // Binding is intentionally independent of panel visibility; combat ticks never open it.
    }

    public void ShowUnitDetails()
    {
        _statisticsPage = false;
        SetExpanded(true, false);
    }

    public void ShowStatistics()
    {
        _statisticsPage = true;
        SetExpanded(true, false);
    }

    public void BindStatistics(IEnumerable<BattleUnitReportSnapshot> reports, Func<string, UnitPortraitDefinition?> portraitFor,
        int ticks = 0, BattleOutcome outcome = BattleOutcome.Running,
        Func<CombatSourceKind, string, string>? sourceName = null,
        Func<string, BattleScreenRuntimeUnitSnapshot?>? runtimeUnit = null, string context = "")
    {
        _reports = reports.ToArray();
        _portraitFor = portraitFor;
        _statisticsTicks = ticks;
        _sourceName = sourceName;
        _runtimeUnit = runtimeUnit;
        var detailed = _reports.Any(unit => unit.DetailedStatistics is not null);
        if (_detailed != detailed)
        {
            _detailed = detailed;
            _metric.ItemCount = detailed ? 7 : 3;
            if (detailed)
            {
                _metric.SetItemText(3, "造成硬控（目标秒）");
                _metric.SetItemText(4, "受到硬控（秒）");
                _metric.SetItemText(5, "护盾吸收");
                _metric.SetItemText(6, "法力施法");
            }
            if (_metric.Selected < 0) _metric.Select(0);
        }
        var result = outcome switch
        {
            BattleOutcome.Running => "进行中", BattleOutcome.PlayerVictory => "我方胜利",
            BattleOutcome.PlayerDefeat => "敌方胜利", BattleOutcome.Timeout => "战斗超时", _ => "战斗结束"
        };
        _clock.Text = $"{result}{(outcome == BattleOutcome.Running ? "" : " · 统计冻结")}　模拟时间 {ticks * BattleTiming.TickSeconds:0.0} 秒　{context}";
        _clock.Visible = detailed;
        _export.Visible = detailed;
        _exportFeedback.Visible = detailed && _exportFeedback.Text.Length > 0;
        _statisticsDetail.Visible = detailed;
        ResizePanel();
        RefreshStatistics();
    }

    public void ShowExportResult(string message)
    {
        _exportFeedback.Text = message;
        _exportFeedback.TooltipText = message;
        _exportFeedback.Visible = true;
    }

    public void ResetSelection()
    {
        BattleLabHoverHint.HideAll(true);
        _selectedRuntimeId = "";
        BattleLabHoverHint.Bind(_unitButton, new BattleLabTooltipInfo("单位详情", Abilities: "点选战场单位，查看生命、技能、装备和状态。"));
        Details.Visible = false;
        _emptySelection.Visible = true;
        _reports = [];
        _statisticsRuntimeId = "";
        _runtimeUnit = null;
        _sourceName = null;
        _exportFeedback.Text = "";
        _exportFeedback.Hide();
        _detailText.Text = "点击左侧单位查看明细。";
        foreach (var row in _rows.Values)
        {
            row.UnitSelected -= OnUnitSelected;
            _rowContainer.RemoveChild(row);
            row.QueueFree();
        }
        _rows.Clear();
        _emptyStats.Visible = true;
        _statisticsPage = true;
        _team = 0;
        _metric.Select(0);
        SetExpanded(false, false);
    }

    public void CollapsePanel() => SetExpanded(false, false);

    private void ToggleStatistics()
    {
        if (_expanded && _statisticsPage) { Collapse(); return; }
        _statisticsPage = true;
        SetExpanded(true, false);
    }

    private void ToggleUnit()
    {
        if (_expanded && !_statisticsPage) { Collapse(); return; }
        ShowUnitDetails();
    }

    private void Collapse() => SetExpanded(false, true);

    private void SetExpanded(bool expanded, bool returnFocus)
    {
        BattleLabHoverHint.HideAll(true);
        _expanded = expanded;
        _statistics.Visible = _statisticsPage;
        _unitPage.Visible = !_statisticsPage;
        _heading.Text = _statisticsPage ? "战斗统计" : "单位详情";
        ResizePanel();
        _statisticsButton.ThemeTypeVariation = expanded && _statisticsPage ? "SelectedButton" : "SecondaryButton";
        _unitButton.ThemeTypeVariation = expanded && !_statisticsPage ? "SelectedButton" : "SecondaryButton";
        BattleLabHoverHint.Bind(_statisticsButton, new BattleLabTooltipInfo("战斗统计",
            Abilities: "查看本场伤害、承伤、治疗与控制；能力实验中点击条目查看来源和时间明细。",
            Hint: "按需打开统计窗口，关闭后继续观察战场"));
        if (WantsStatistics) StatisticsRequested?.Invoke();
        if (expanded) _popup.Open(_statisticsPage ? _statisticsButton : _unitButton);
        else _popup.Close();
        if (returnFocus) (_statisticsPage ? _statisticsButton : _unitButton).GrabFocus();
    }

    private void OnPopupClosed() => SetExpanded(false, false);

    private void OnMetricChanged(long _) => RefreshStatistics();
    private void ShowAllies() { _team = 0; RefreshStatistics(); }
    private void ShowEnemies() { _team = 1; RefreshStatistics(); }

    private void RefreshStatistics()
    {
        var metric = _metric.Selected;
        var metricName = metric switch { 1 => "承伤", 2 => "有效治疗", 3 => "造成硬控", 4 => "受到硬控", 5 => "护盾吸收", 6 => "法力施法", _ => "伤害" };
        float Value(BattleUnitReportSnapshot unit) => metric switch
        {
            1 => unit.DamageTaken, 2 => unit.HealingDone,
            3 => (unit.DetailedStatistics?.HardControlTicksDealt ?? 0) * BattleTiming.TickSeconds,
            4 => (unit.DetailedStatistics?.HardControlTicksTaken ?? 0) * BattleTiming.TickSeconds,
            5 => unit.ShieldAbsorbed, 6 => unit.DetailedStatistics?.ManaCasts ?? 0, _ => unit.DamageDealt
        };
        var members = _reports.Where(unit => unit.Team == _team)
            .OrderByDescending(Value).ThenBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray();
        _allies.SetPressedNoSignal(_team == 0);
        _enemies.SetPressedNoSignal(_team == 1);
        var suffix = metric == 3 ? " 目标秒" : metric == 4 ? " 秒" : metric == 6 ? " 次" : "";
        _total.Text = $"{(_team == 0 ? "我方" : "敌方")} · 总{metricName} {members.Sum(Value):0.0}{suffix}";
        _emptyStats.Visible = members.Length == 0;
        var wanted = members.Select(unit => unit.RuntimeId).ToHashSet(StringComparer.Ordinal);
        foreach (var (id, row) in _rows) row.Visible = wanted.Contains(id);
        var maximum = members.Select(Value).DefaultIfEmpty(0).Max();
        for (var index = 0; index < members.Length; index++)
        {
            var unit = members[index];
            if (!_rows.TryGetValue(unit.RuntimeId, out var row))
            {
                row = StatRowScene.Instantiate<BattleStatRow>();
                _rowContainer.AddChild(row);
                row.UnitSelected += OnUnitSelected;
                _rows.Add(unit.RuntimeId, row);
            }
            row.Visible = true;
            _rowContainer.MoveChild(row, index);
            row.Bind(unit, _portraitFor?.Invoke(unit.ContentId), metricName, Value(unit), maximum, metric is 3 or 4);
        }
        if (!members.Any(unit => unit.RuntimeId == _statisticsRuntimeId))
            _statisticsRuntimeId = members.FirstOrDefault()?.RuntimeId ?? "";
        RefreshDetail();
    }

    private void OnUnitSelected(string runtimeId)
    {
        if (!_detailed) { UnitSelected?.Invoke(runtimeId); return; }
        _statisticsRuntimeId = runtimeId;
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        if (!_detailed) return;
        var selected = _reports.FirstOrDefault(unit => unit.RuntimeId == _statisticsRuntimeId);
        _inspectUnit.Disabled = selected is null;
        foreach (var (id, row) in _rows)
            row.ThemeTypeVariation = id == _statisticsRuntimeId ? "SelectedButton" : "SecondaryButton";
        if (selected is null) { _detailText.Text = "点击左侧单位查看明细。"; return; }
        string UnitName(string id)
        {
            var source = _reports.FirstOrDefault(unit => unit.RuntimeId == id);
            return source is null ? "未归属来源" : $"{(source.Team == 0 ? "我方" : "敌方")}·{source.DisplayName}" + (source.IsTemporary ? "（召唤物）" : "");
        }
        var text = BattleStatisticsText.Build(selected, _statisticsTicks, _runtimeUnit?.Invoke(selected.RuntimeId),
            _sourceName ?? ((_, id) => id), UnitName);
        if (_detailText.Text != text) _detailText.Text = text;
    }

    private void ResizePanel()
    {
        var wide = _statisticsPage && _detailed;
        _expandedPanel.AnchorLeft = wide ? .10f : .29f;
        _expandedPanel.AnchorRight = wide ? .90f : .71f;
    }
    private void OnExportRequested() => ExportRequested?.Invoke();
    private void InspectStatisticsUnit() => UnitSelected?.Invoke(_statisticsRuntimeId);

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (!_expanded || @event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape }) return;
        var focus = GetViewport().GuiGetFocusOwner();
        if (focus is null || !IsAncestorOf(focus)) return;
        Collapse();
        GetViewport().SetInputAsHandled();
    }

    public override void _ExitTree()
    {
        BattleLabHoverHint.HideAll(true);
        _statisticsButton.Pressed -= ToggleStatistics;
        _unitButton.Pressed -= ToggleUnit;
        _popup.Closed -= OnPopupClosed;
        _metric.ItemSelected -= OnMetricChanged;
        _allies.Pressed -= ShowAllies;
        _enemies.Pressed -= ShowEnemies;
        _export.Pressed -= OnExportRequested;
        _inspectUnit.Pressed -= InspectStatisticsUnit;
        foreach (var row in _rows.Values) row.UnitSelected -= OnUnitSelected;
    }
}
