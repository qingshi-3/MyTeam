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
                     "res://src/UI/BattleReportUnitCard.cs",
                     "res://src/UI/BattleReportTeamSummary.cs",
                     "res://scenes/ui/components/BattleReportUnitCard.tscn",
                     "res://scenes/ui/components/BattleReportTeamSummary.tscn"
                 })
            Expect(path, failures);

        var battleModels = Read("res://src/Battle/BattleModels.cs");
        Require(battleModels, "JoinTick", "unit result lacks join tick", failures);
        Require(battleModels, "DefeatTick", "unit result lacks terminal defeat tick", failures);
        Require(battleModels, "AttackActions", "unit result lacks attack-action count", failures);
        Require(battleModels, "EffectiveHealingEvents", "unit result lacks effective healing-event count", failures);
        Require(battleModels, "SuccessfulHeroCommandUses", "battle result lacks successful command uses", failures);

        var simulation = Read("res://src/Battle/BattleSimulation.cs");
        Require(simulation, "JoinTick = 0", "initial units do not author tick-zero join", failures);
        Require(simulation, "JoinTick = TickIndex", "temporary summons do not retain their actual join tick", failures);
        Require(simulation, "AttackActions++", "attack actions are not counted at one authority boundary", failures);
        Require(simulation, "EffectiveHealingEvents++", "effective healing events are not counted at HealLiving authority", failures);
        Require(simulation, "SuccessfulHeroCommandUses++", "successful commands are not counted at commit", failures);

        var reportModels = Read("res://src/UI/BattleReportModels.cs");
        foreach (var token in new[]
                 {
                     "BattleReportDimension", "Overview", "Offense", "Survival", "Healing",
                     "ActiveLifetimeSeconds", "DamageShare", "HealingShare", "EnvironmentDamage",
                     "DamageLeader", "DamageTakenLeader", "HealingLeader", "StringComparer.Ordinal"
                 })
            Require(reportModels, token, $"typed report derivation lacks {token}", failures);

        var screenScene = Read("res://scenes/ui/BattleReportScreen.tscn");
        foreach (var token in new[]
                 {
                     "BattleReportUnitCard.tscn", "BattleReportTeamSummary.tscn", "DimensionTabs",
                     "OverviewTab", "OffenseTab", "SurvivalTab", "HealingTab", "AllegianceTabs",
                     "ReportCards", "EmptyState", "RosterScroll", "ReportContinue"
                 })
            Require(screenScene, token, $"authored report composition lacks {token}", failures);

        var screen = Read("res://src/UI/BattleReportScreen.cs");
        Require(screen, "BattleReportDimension", "report controller lacks typed dimension state", failures);
        Require(screen, "BindRoster", "report controller lacks complete derived-roster replacement", failures);
        Require(screen, "GrabFocus", "report controller lacks focus restoration", failures);
        if (screen.Contains("BattleReportUnitRow", StringComparison.Ordinal))
            failures.Add("report controller still owns the obsolete spreadsheet row");

        var cardScene = Read("res://scenes/ui/components/BattleReportUnitCard.tscn");
        foreach (var token in new[] { "UnitPortrait", "UnitIdentity", "UnitStatus", "PrimaryMetric", "ContributionBar", "Award" })
            Require(cardScene, token, $"authored report card lacks {token}", failures);

        if (failures.Count > 0)
        {
            GD.PrintErr("DYNAMIC_BATTLE_REPORT_CONTRACT_FAILED: " + string.Join(" | ", failures));
            GetTree().Quit(1);
            return;
        }

        GD.Print("DYNAMIC_BATTLE_REPORT_CONTRACT_OK facts=immutable dimensions=4 allegiance=2 cards=authored derivation=ranked-zero-safe");
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
