using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Presentation;

namespace TowerAutobattler.UI;

public partial class BattleInspectorDock : Control
{
    [Export] public PackedScene StatRowScene { get; set; } = null!;
    public event Action? StatisticsRequested;
    public event Action<string>? UnitSelected;
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
    private readonly Dictionary<string, BattleStatRow> _rows = new(StringComparer.Ordinal);
    private BattleUnitReportSnapshot[] _reports = [];
    private Func<string, UnitPortraitDefinition?>? _portraitFor;
    private bool _expanded;
    private bool _statisticsPage = true;
    private int _team;
    private string _selectedRuntimeId = "";

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
            Abilities: "伤害与承伤为实际扣除生命与护盾吸收之和，不计溢出的伤害。治疗仅统计实际恢复生命，不计过量治疗。\n条长相对于本侧当前指标最高的单位。"));
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

    public void BindStatistics(IEnumerable<BattleUnitReportSnapshot> reports, Func<string, UnitPortraitDefinition?> portraitFor)
    {
        _reports = reports.ToArray();
        _portraitFor = portraitFor;
        RefreshStatistics();
    }

    public void ResetSelection()
    {
        BattleLabHoverHint.HideAll(true);
        _selectedRuntimeId = "";
        BattleLabHoverHint.Bind(_unitButton, new BattleLabTooltipInfo("单位详情", Abilities: "点选战场单位，查看生命、技能、装备和状态。"));
        Details.Visible = false;
        _emptySelection.Visible = true;
        _reports = [];
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
        _statisticsButton.ThemeTypeVariation = expanded && _statisticsPage ? "SelectedButton" : "SecondaryButton";
        _unitButton.ThemeTypeVariation = expanded && !_statisticsPage ? "SelectedButton" : "SecondaryButton";
        BattleLabHoverHint.Bind(_statisticsButton, new BattleLabTooltipInfo("战斗统计",
            Abilities: "查看本场伤害、承伤和有效治疗，点击条目查看单位。",
            Hint: "按需打开统计窗口，关闭后继续观察战场"));
        if (expanded) _popup.Open(_statisticsPage ? _statisticsButton : _unitButton);
        else _popup.Close();
        if (returnFocus) (_statisticsPage ? _statisticsButton : _unitButton).GrabFocus();
        if (WantsStatistics) StatisticsRequested?.Invoke();
    }

    private void OnPopupClosed() => SetExpanded(false, false);

    private void OnMetricChanged(long _) => RefreshStatistics();
    private void ShowAllies() { _team = 0; RefreshStatistics(); }
    private void ShowEnemies() { _team = 1; RefreshStatistics(); }

    private void RefreshStatistics()
    {
        var metric = _metric.Selected;
        var metricName = metric switch { 1 => "承伤", 2 => "有效治疗", _ => "伤害" };
        float Value(BattleUnitReportSnapshot unit) => metric switch
        { 1 => unit.DamageTaken, 2 => unit.HealingDone, _ => unit.DamageDealt };
        var members = _reports.Where(unit => unit.Team == _team)
            .OrderByDescending(Value).ThenBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray();
        _allies.SetPressedNoSignal(_team == 0);
        _enemies.SetPressedNoSignal(_team == 1);
        _total.Text = $"{(_team == 0 ? "我方" : "敌方")} · 总{metricName} {members.Sum(Value):0}";
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
            row.Bind(unit, _portraitFor?.Invoke(unit.ContentId), metricName, Value(unit), maximum);
        }
    }

    private void OnUnitSelected(string runtimeId) => UnitSelected?.Invoke(runtimeId);

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
        foreach (var row in _rows.Values) row.UnitSelected -= OnUnitSelected;
    }
}
