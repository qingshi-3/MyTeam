using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public partial class BattleReportScreen : Control
{
    public event Action? ContinueRequested;

    [Export] public PackedScene MetricScene { get; set; } = null!;
    [Export] public PackedScene UnitRowScene { get; set; } = null!;

    private Label _outcome = null!;
    private Label _encounter = null!;
    private IconText _duration = null!;
    private HBoxContainer _metrics = null!;
    private Button _playerTab = null!;
    private Button _enemyTab = null!;
    private VBoxContainer _rows = null!;
    private Button _continue = null!;
    private BattleResult? _result;
    private ContentRegistry? _content;
    private int _team;
    private bool _continueReported;

    public override void _Ready()
    {
        _outcome = GetNode<Label>("%ReportOutcome");
        _encounter = GetNode<Label>("%ReportEncounter");
        _duration = GetNode<IconText>("%ReportDuration");
        _metrics = GetNode<HBoxContainer>("%ReportMetrics");
        _playerTab = GetNode<Button>("%PlayerTab");
        _enemyTab = GetNode<Button>("%EnemyTab");
        _rows = GetNode<VBoxContainer>("%ReportRows");
        _continue = GetNode<Button>("%ReportContinue");
        _playerTab.Pressed += ShowPlayerTeam;
        _enemyTab.Pressed += ShowEnemyTeam;
        _continue.Pressed += RequestContinue;
    }

    public override void _ExitTree()
    {
        _playerTab.Pressed -= ShowPlayerTeam;
        _enemyTab.Pressed -= ShowEnemyTeam;
        _continue.Pressed -= RequestContinue;
        ContinueRequested = null;
    }

    public void Bind(BattleResult result, string encounterName, ContentRegistry content)
    {
        _result = result;
        _content = content;
        _team = 0;
        _continueReported = false;
        _continue.Disabled = false;
        _outcome.Text = PlayerFacingText.DescribeBattleOutcome(result.Outcome);
        _outcome.ThemeTypeVariation = result.Outcome switch
        {
            BattleOutcome.PlayerVictory => "HealingTitleLabel",
            BattleOutcome.Timeout => "WarningTitleLabel",
            _ => "EnemyTitleLabel"
        };
        _encounter.Text = encounterName;
        _duration.Bind(Icon(SemanticIconKeys.Time), $"模拟时长 {result.Ticks * BattleSimulation.TickSeconds:0.0} 秒", "SecondaryLabel");
        BindMetrics();
        BindRows();
        _continue.GrabFocus();
    }

    private void BindMetrics()
    {
        Clear(_metrics);
        var result = _result!;
        var player = result.Units.Where(unit => unit.Team == 0).ToArray();
        AddMetric(SemanticIconKeys.Health, "存活", player.Count(unit => unit.Alive).ToString(), "HealthValue");
        AddMetric(SemanticIconKeys.Deaths, "伤亡", player.Count(unit => !unit.Alive).ToString(), "DangerValue");
        AddMetric(SemanticIconKeys.Damage, "有效伤害", player.Sum(unit => unit.DamageDealt).ToString("0"), "DamageValue");
        AddMetric(SemanticIconKeys.Healing, "有效治疗", player.Sum(unit => unit.HealingDone).ToString("0"), "HealingValue");
        AddMetric(SemanticIconKeys.Gold, "指令金币", result.GoldSpent.ToString(), "GoldValue");
    }

    private void AddMetric(StringName iconKey, string label, string value, StringName variation)
    {
        var displayValue = string.IsNullOrWhiteSpace(value) ? "0" : value;
        var metric = MetricScene.Instantiate<BattleReportMetric>();
        _metrics.AddChild(metric);
        metric.Bind(Icon(iconKey), label, displayValue, variation);
    }

    private void BindRows()
    {
        Clear(_rows);
        var units = _result!.Units.Where(unit => unit.Team == _team).ToArray();
        var maxDamage = units.Select(unit => unit.DamageDealt).DefaultIfEmpty().Max();
        var maxTaken = units.Select(unit => unit.DamageTaken).DefaultIfEmpty().Max();
        var maxHealing = units.Select(unit => unit.HealingDone).DefaultIfEmpty().Max();
        foreach (var unit in units.OrderByDescending(unit => unit.IsHero).ThenBy(unit => unit.IsTemporary).ThenBy(unit => unit.RuntimeId, StringComparer.Ordinal))
        {
            var row = UnitRowScene.Instantiate<BattleReportUnitRow>();
            _rows.AddChild(row);
            row.Bind(
                unit,
                PortraitDefinition(unit),
                FallbackPortrait(unit),
                unit.DamageDealt == maxDamage,
                unit.DamageTaken == maxTaken,
                unit.HealingDone == maxHealing);
        }
        _playerTab.ButtonPressed = _team == 0;
        _enemyTab.ButtonPressed = _team == 1;
    }

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
    private void ShowPlayerTeam() { _team = 0; BindRows(); }
    private void ShowEnemyTeam() { _team = 1; BindRows(); }

    private void RequestContinue()
    {
        if (_continueReported) return;
        _continueReported = true;
        _continue.Disabled = true;
        ContinueRequested?.Invoke();
    }

    private static void Clear(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            parent.RemoveChild(child);
            child.Free();
        }
    }
}
