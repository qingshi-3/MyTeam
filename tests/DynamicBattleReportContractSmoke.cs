using System;
using System.Collections.Generic;
using Godot;

public partial class DynamicBattleReportContractSmoke : Node
{
    public override void _Ready()
    {
        var failures = new List<string>();
        foreach (var path in new[]
                 {
                     "res://src/UI/BattleReportModels.cs",
                     "res://src/UI/BattleReportScreen.cs",
                     "res://scenes/ui/components/BattleReportComparison.tscn",
                     "res://scenes/ui/components/BattleReportCoreMatchupRow.tscn",
                     "res://scenes/ui/components/BattleReportRosterStrip.tscn",
                     "res://scenes/ui/components/BattleReportLeaderboardHeader.tscn",
                     "res://scenes/ui/components/BattleReportLeaderboardRow.tscn",
                     "res://scenes/ui/components/BattleReportUnitDetail.tscn"
                 })
            Expect(path, failures);

        var battleModels = Read("res://src/Battle/BattleModels.cs");
        foreach (var token in new[] { "JoinTick", "DefeatTick", "AttackActions", "EffectiveHealingEvents", "SuccessfulTacticalCommandUses" })
            Require(battleModels, token, $"immutable battle result lacks {token}", failures);

        var reportModels = Read("res://src/UI/BattleReportModels.cs");
        foreach (var token in new[]
                 {
                     "BattleReportDimension", "Overview", "Offense", "Survival", "Healing",
                     "ActiveLifetimeSeconds", "DamageShare", "HealingShare", "EnvironmentDamage",
                     "OutputLeaders", "DamageTakenLeaders", "HealingLeaders", "PrimaryMaximum",
                     "BattleReportCoreMatchupViewModel", "BuildCoreMatchups", "BothSidesZero", "StringComparer.Ordinal"
                 })
            Require(reportModels, token, $"typed report derivation lacks {token}", failures);

        var screenScene = Read("res://scenes/ui/BattleReportScreen.tscn");
        foreach (var token in new[]
                 {
                     "BattleReportComparison.tscn", "BattleReportRosterStrip.tscn",
                     "BattleReportLeaderboardHeader.tscn", "BattleReportLeaderboardRow.tscn", "BattleReportUnitDetail.tscn",
                     "DimensionTabs", "OverviewTab", "OffenseTab", "SurvivalTab", "HealingTab", "AllegianceTabs",
                     "ReportContentScroll", "OverviewPage", "LeaderboardPage", "LeaderboardList", "EmptyState", "ReportContinue"
                 })
            Require(screenScene, token, $"authored statistical report composition lacks {token}", failures);
        var comparisonScene = Read("res://scenes/ui/components/BattleReportComparison.tscn");
        foreach (var token in new[] { "CoreMatchups", "OutputCoreMatchup", "SurvivalCoreMatchup", "HealingCoreMatchup" })
            Require(comparisonScene, token, $"authored core comparison lacks {token}", failures);
        if (screenScene.Contains("BattleReportUnitCard.tscn", StringComparison.Ordinal) || screenScene.Contains("ReportCards", StringComparison.Ordinal))
            failures.Add("statistical report still authors the obsolete card wall");

        var screen = Read("res://src/UI/BattleReportScreen.cs");
        foreach (var token in new[] { "BattleReportDimension", "BindOverview", "BuildCoreMatchups", "BindLeaderboard", "SelectedRuntimeId", "SelectRow", "GrabFocus", "_continueReported" })
            Require(screen, token, $"report controller lacks {token}", failures);

        var row = Read("res://scenes/ui/components/BattleReportLeaderboardRow.tscn");
        foreach (var token in new[] { "type=\"Button\"", "UnitPortrait", "UnitIdentity", "PrimaryValue", "SecondaryValue4", "ContributionBar" })
            Require(row, token, $"authored leaderboard row lacks {token}", failures);

        if (failures.Count > 0)
        {
            GD.PrintErr("DYNAMIC_BATTLE_REPORT_CONTRACT_FAILED: " + string.Join(" | ", failures));
            GetTree().Quit(1);
            return;
        }

        GD.Print("DYNAMIC_BATTLE_REPORT_CONTRACT_OK facts=immutable dimensions=4 allegiance=2 overview=statistical leaderboard=fixed-columns detail=single derivation=ranked-zero-safe");
        GetTree().Quit();
    }

    private static void Expect(string path, List<string> failures)
    {
        if (!ResourceLoader.Exists(path) && !FileAccess.FileExists(path)) failures.Add("missing " + path);
    }

    private static void Require(string source, string token, string failure, List<string> failures)
    {
        if (!source.Contains(token, StringComparison.Ordinal)) failures.Add(failure);
    }

    private static string Read(string path) => FileAccess.FileExists(path) ? FileAccess.GetFileAsString(path) : string.Empty;
}
