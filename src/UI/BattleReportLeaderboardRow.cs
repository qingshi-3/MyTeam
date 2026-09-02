using System;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public partial class BattleReportLeaderboardRow : Button
{
    public event Action<BattleReportLeaderboardRow>? Selected;

    private Label _rank = null!;
    private UnitPortrait _portrait = null!;
    private Label _name = null!;
    private Label _identity = null!;
    private Label _primary = null!;
    private Label[] _secondary = [];
    private ProgressBar _contribution = null!;
    private bool _signalsConnected;

    public BattleReportUnitViewModel Model { get; private set; } = null!;
    public string RuntimeId => Model.Unit.RuntimeId;

    public override void _Ready()
    {
        _rank = GetNode<Label>("%Rank");
        _portrait = GetNode<UnitPortrait>("%UnitPortrait");
        _name = GetNode<Label>("%UnitName");
        _identity = GetNode<Label>("%UnitIdentity");
        _primary = GetNode<Label>("%PrimaryValue");
        _secondary =
        [
            GetNode<Label>("%SecondaryValue1"),
            GetNode<Label>("%SecondaryValue2"),
            GetNode<Label>("%SecondaryValue3"),
            GetNode<Label>("%SecondaryValue4")
        ];
        _contribution = GetNode<ProgressBar>("%ContributionBar");
        Pressed += NotifySelected;
        FocusEntered += NotifySelected;
        _signalsConnected = true;
    }

    public override void _ExitTree()
    {
        if (_signalsConnected)
        {
            Pressed -= NotifySelected;
            FocusEntered -= NotifySelected;
            _signalsConnected = false;
        }
        Selected = null;
    }

    public void Bind(
        int rank,
        BattleReportUnitViewModel model,
        BattleReportDimension dimension,
        float primaryMaximum,
        UnitPortraitDefinition? portrait,
        Texture2D fallback)
    {
        Model = model;
        var unit = model.Unit;
        _rank.Text = rank.ToString();
        _portrait.Bind(portrait, fallback);
        _name.Text = unit.DisplayName;
        _name.ThemeTypeVariation = unit.IsHero ? "HeroLabel" : unit.Team == 1 ? "EnemyLabel" : "ChoiceTitle";
        _identity.Text = $"{Identity(unit)} · {PlayerFacingText.DescribeUnitRole(unit.Role)} · {(unit.Alive ? "存活" : "阵亡")}";
        _identity.ThemeTypeVariation = unit.Alive ? "SecondaryLabel" : "DangerValue";
        TooltipText = $"第 {rank} 名 · {unit.DisplayName}";

        var primaryValue = dimension switch
        {
            BattleReportDimension.Offense => unit.DamageDealt,
            BattleReportDimension.Survival => unit.DamageTaken,
            BattleReportDimension.Healing => unit.HealingDone,
            _ => unit.FinalHealth
        };
        _primary.Text = primaryValue.ToString("0");
        _primary.ThemeTypeVariation = dimension switch
        {
            BattleReportDimension.Offense => "DamageValue",
            BattleReportDimension.Survival => "DangerValue",
            BattleReportDimension.Healing => "HealingValue",
            _ => "HealthValue"
        };
        _contribution.Value = primaryMaximum <= 0 ? 0 : primaryValue / primaryMaximum * 100;

        switch (dimension)
        {
            case BattleReportDimension.Offense:
                SetSecondary(model.DamageShare.ToString("P0"), model.DamagePerSecond.ToString("0.0"), unit.Kills.ToString(), unit.AttackActions.ToString());
                break;
            case BattleReportDimension.Survival:
                SetSecondary(unit.ShieldAbsorbed.ToString("0"), $"{unit.FinalHealth:0}/{unit.MaxHealth:0}", $"{model.ActiveLifetimeSeconds:0.0}秒", model.FinalHealthRatio.ToString("P0"));
                break;
            case BattleReportDimension.Healing:
                SetSecondary(model.HealingShare.ToString("P0"), model.HealingPerSecond.ToString("0.0"), unit.EffectiveHealingEvents.ToString(), null);
                break;
        }
    }

    public void SetSelected(bool selected) => SetPressedNoSignal(selected);

    private void SetSecondary(string first, string second, string third, string? fourth)
    {
        var values = new[] { first, second, third, fourth };
        for (var index = 0; index < _secondary.Length; index++)
        {
            _secondary[index].Text = values[index] ?? string.Empty;
            ((Control)_secondary[index].GetParent()).Visible = values[index] is not null;
        }
    }

    private void NotifySelected() => Selected?.Invoke(this);

    private static string Identity(BattleUnitReportSnapshot unit)
    {
        if (unit.IsTemporary) return "召唤物";
        if (unit.IsHero) return "英雄";
        if (unit.Role == UnitRole.Boss) return "首领";
        return unit.Team == 0 ? "士兵" : "敌方单位";
    }
}
