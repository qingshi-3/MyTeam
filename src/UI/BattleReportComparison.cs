using System;
using System.Collections.Generic;
using Godot;

namespace TowerAutobattler.UI;

public partial class BattleReportComparison : PanelContainer
{
    private BattleReportCoreMatchupRow _output = null!;
    private BattleReportCoreMatchupRow _survival = null!;
    private BattleReportCoreMatchupRow _healing = null!;
    private Label _environment = null!;

    public override void _Ready()
    {
        _output = GetNode<BattleReportCoreMatchupRow>("%OutputCoreMatchup");
        _survival = GetNode<BattleReportCoreMatchupRow>("%SurvivalCoreMatchup");
        _healing = GetNode<BattleReportCoreMatchupRow>("%HealingCoreMatchup");
        _environment = GetNode<Label>("%EnvironmentDamage");
    }

    public void Bind(
        BattleReportTeamViewModel player,
        BattleReportTeamViewModel enemy,
        IReadOnlyList<BattleReportCoreMatchupViewModel> matchups)
    {
        if (matchups.Count != 3)
            throw new ArgumentException("Battle report overview requires exactly three core matchups.", nameof(matchups));

        BindRow(BattleReportDimension.Offense, _output, matchups);
        BindRow(BattleReportDimension.Survival, _survival, matchups);
        BindRow(BattleReportDimension.Healing, _healing, matchups);

        var environmentParts = new List<string>(2);
        if (player.EnvironmentDamage > 0) environmentParts.Add($"我方环境承伤 {player.EnvironmentDamage:0}");
        if (enemy.EnvironmentDamage > 0) environmentParts.Add($"敌方环境承伤 {enemy.EnvironmentDamage:0}");
        _environment.Text = string.Join("　", environmentParts);
        _environment.Visible = environmentParts.Count > 0;
    }

    private static void BindRow(
        BattleReportDimension dimension,
        BattleReportCoreMatchupRow row,
        IReadOnlyList<BattleReportCoreMatchupViewModel> matchups)
    {
        foreach (var matchup in matchups)
        {
            if (matchup.Dimension != dimension) continue;
            row.Bind(matchup);
            return;
        }

        throw new ArgumentException($"Missing core matchup for {dimension}.", nameof(matchups));
    }
}
