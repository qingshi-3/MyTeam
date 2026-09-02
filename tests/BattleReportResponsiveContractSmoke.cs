using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.UI;

public partial class BattleReportResponsiveContractSmoke : Node
{
    private const float RectEpsilon = 0.75f;
    private const string OutputPath = "res://.godot/qa";

    public override async void _Ready()
    {
        var failures = new List<string>();
        VerifyAuthoredContract(failures);
        try
        {
            await VerifyRuntimeStressAsync(failures);
        }
        catch (Exception exception)
        {
            failures.Add($"statistical stress runtime could not complete: {exception.GetType().Name}: {exception.Message}");
        }

        if (failures.Count > 0)
        {
            GD.PrintErr("BATTLE_REPORT_RESPONSIVE_CONTRACT_FAILED: " + string.Join(" | ", failures));
            GetTree().Quit(1);
            return;
        }

        GD.Print("BATTLE_REPORT_RESPONSIVE_CONTRACT_OK pages=core-matchup-leaderboard columns=contained-fixed detail=contained scroll=single resize=reversible selection=valid");
        GetTree().Quit();
    }

    private static void VerifyAuthoredContract(List<string> failures)
    {
        var screenScene = Read("res://scenes/ui/BattleReportScreen.tscn");
        foreach (var token in new[] { "ReportContentScroll", "HFlowContainer", "LeaderboardHeader", "LeaderboardList", "UnitDetail" })
            Require(screenScene, token, $"screen lacks responsive statistical surface {token}", failures);
        var rowScene = Read("res://scenes/ui/components/BattleReportLeaderboardRow.tscn");
        Require(rowScene, "text_overrun_behavior = 3", "long leaderboard identity lacks authored truncation", failures);
        Require(rowScene, "mouse_filter = 2", "row descendants may intercept the focusable row input surface", failures);
        var matchupScene = Read("res://scenes/ui/components/BattleReportCoreMatchupRow.tscn");
        Require(matchupScene, "text_overrun_behavior = 3", "long core-matchup identity/value lacks authored truncation", failures);
        if ((screenScene + rowScene + matchupScene + Read("res://src/UI/BattleReportScreen.cs")).Contains("content_scale_factor", StringComparison.OrdinalIgnoreCase) ||
            rowScene.Contains("scale =", StringComparison.Ordinal) || matchupScene.Contains("scale =", StringComparison.Ordinal))
            failures.Add("statistical report uses forbidden global or transform scaling");
    }

    private async Task VerifyRuntimeStressAsync(List<string> failures)
    {
        var host = new Control { Size = new Vector2(1600, 900), ClipContents = true };
        var screen = GD.Load<PackedScene>("res://scenes/ui/BattleReportScreen.tscn").Instantiate<BattleReportScreen>();
        screen.Theme = GD.Load<Theme>("res://content/ui/RealmTheme.tres");
        host.AddChild(screen);
        screen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(host);
        await SettleAsync();

        screen.Bind(StressResult(), "超长名称与大数值统计压力遭遇", null!);
        await SettleAsync();
        await CaptureStressDimensionsAsync(host, screen, 1600, 900, failures);
        await CaptureStressDimensionsAsync(host, screen, 1280, 720, failures);

        screen.GetNode<Button>("%OffenseTab").EmitSignal(BaseButton.SignalName.Pressed);
        await SettleAsync();
        screen.GetNode<BattleReportLeaderboardRow>("%LeaderboardList/Row_stress-player-2").GrabFocus();
        var selected = screen.SelectedRuntimeId;

        foreach (var (width, height) in new[] { (1600, 900), (1280, 720), (1000, 720), (1600, 900), (1280, 720) })
        {
            host.Size = new Vector2(width, height);
            await SettleAsync();
            VerifyFixedRegions(host, screen, width, height, failures);
            VerifyLeaderboard(screen, width, failures);
            if (screen.SelectedRuntimeId != selected || screen.SelectedDimension != BattleReportDimension.Offense || screen.SelectedTeam != 0)
                failures.Add($"{width}x{height} resize lost dimension/allegiance/stable selection");
        }

        screen.QueueFree();
        host.QueueFree();
        await SettleAsync();
    }

    private async Task CaptureStressDimensionsAsync(
        Control host,
        BattleReportScreen screen,
        int width,
        int height,
        List<string> failures)
    {
        if (DisplayServer.GetName() == "headless") return;
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(OutputPath));
        host.Size = new Vector2(width, height);
        foreach (var (tabName, suffix) in new[]
                 {
                     ("%OverviewTab", "Overview"), ("%OffenseTab", "Offense"),
                     ("%SurvivalTab", "Survival"), ("%HealingTab", "Healing")
                 })
        {
            screen.GetNode<Button>(tabName).EmitSignal(BaseButton.SignalName.Pressed);
            await SettleAsync();
            if (tabName == "%OverviewTab") VerifyCoreMatchups(screen, width, failures);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var viewportImage = GetViewport().GetTexture().GetImage();
            var image = viewportImage.GetRegion(new Rect2I(0, 0, Math.Min(width, viewportImage.GetWidth()), Math.Min(height, viewportImage.GetHeight())));
            var fileName = $"UI_{width}x{height}_BattleReportStatisticalStress{suffix}.png";
            var error = image.SavePng($"{ProjectSettings.GlobalizePath(OutputPath)}/{fileName}");
            if (error != Error.Ok) throw new InvalidOperationException($"capture {fileName}: {error}");
        }
    }

    private static void VerifyCoreMatchups(BattleReportScreen screen, int width, List<string> failures)
    {
        var scrollRect = screen.GetNode<ScrollContainer>("%ReportContentScroll").GetGlobalRect();
        var rows = screen.GetNode<BattleReportComparison>("%OverviewComparison")
            .GetNode<VBoxContainer>("%CoreMatchups").GetChildren().OfType<Control>().ToArray();
        if (rows.Length != 3)
        {
            failures.Add($"{width}px overview expected three core matchup rows, got {rows.Length}");
            return;
        }

        foreach (var row in rows)
        {
            if (!ContainsHorizontally(scrollRect, row.GetGlobalRect()))
                failures.Add($"{width}px core matchup {row.Name} escapes content width");
            if (row.Scale != Vector2.One) failures.Add($"{width}px core matchup {row.Name} uses transform scale");
            foreach (var nodeName in new[]
                     {
                         "%PlayerLeaderNames", "%PlayerLeaderValueShare", "%Category",
                         "%EnemyLeaderNames", "%EnemyLeaderValueShare"
                     })
            {
                var control = row.GetNode<Control>(nodeName);
                if (!Contains(row.GetGlobalRect(), control.GetGlobalRect()))
                    failures.Add($"{width}px core matchup {row.Name} descendant {nodeName} escapes row");
            }
        }
    }

    private static void VerifyFixedRegions(Control host, BattleReportScreen screen, int width, int height, List<string> failures)
    {
        var hostRect = host.GetGlobalRect();
        foreach (var path in new[]
                 {
                     "Margin/Panel/Layout/OutcomeBanner", "Margin/Panel/Layout/TeamComparison",
                     "Margin/Panel/Layout/Controls", "%ReportContentScroll", "%ReportContinue"
                 })
        {
            var control = screen.GetNode<Control>(path);
            if (!Contains(hostRect, control.GetGlobalRect())) failures.Add($"{width}x{height} fixed region {path} escapes host");
        }
    }

    private static void VerifyLeaderboard(BattleReportScreen screen, int width, List<string> failures)
    {
        var rows = screen.GetNode<VBoxContainer>("%LeaderboardList").GetChildren().OfType<BattleReportLeaderboardRow>().ToArray();
        if (rows.Length != 4)
        {
            failures.Add($"{width}px expected four stress rows, got {rows.Length}");
            return;
        }
        var scrollRect = screen.GetNode<ScrollContainer>("%ReportContentScroll").GetGlobalRect();
        foreach (var row in rows)
        {
            if (!ContainsHorizontally(scrollRect, row.GetGlobalRect())) failures.Add($"{width}px row {row.Name} escapes content width");
            if (row.Scale != Vector2.One) failures.Add($"{width}px row {row.Name} uses transform scale");
            foreach (var nodeName in new[] { "%Rank", "%UnitPortrait", "%UnitName", "%UnitIdentity", "%PrimaryValue", "%ContributionBar" })
            {
                var control = row.GetNode<Control>(nodeName);
                if (!Contains(row.GetGlobalRect(), control.GetGlobalRect())) failures.Add($"{width}px {row.Name} descendant {nodeName} escapes row");
            }
        }

        var header = screen.GetNode<BattleReportLeaderboardHeader>("%LeaderboardHeader");
        foreach (var (headerName, rowName) in new[]
                 {
                     ("%RankHeader", "%Rank"), ("%UnitColumn", "%UnitCell"), ("%PrimaryHeader", "%PrimaryValue"),
                     ("%SecondaryHeader1", "%SecondaryValue1"), ("%SecondaryHeader2", "%SecondaryValue2"),
                     ("%SecondaryHeader3", "%SecondaryValue3"), ("%SecondaryHeader4", "%SecondaryValue4")
                 })
        {
            var headerNode = header.GetNode<Control>(headerName);
            var rowNode = rows[0].GetNode<Control>(rowName);
            var headerCell = headerName == "%UnitColumn" ? headerNode : (Control)headerNode.GetParent();
            var rowCell = rowName == "%UnitCell" ? rowNode : (Control)rowNode.GetParent();
            if (Math.Abs(headerCell.Size.X - rowCell.Size.X) > RectEpsilon)
                failures.Add($"{width}px fixed column {headerName}/{rowName} width drift {headerCell.Size.X:0.0}/{rowCell.Size.X:0.0}");
        }
    }

    private async Task SettleAsync()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static BattleResult StressResult()
    {
        var units = ImmutableArray.Create(
            Unit("stress-player-1", "深渊终焉机械近卫军团首席战场调度官", UnitRole.Artillery, 0, true, 9_876_543, 8_765_432, 9_654_321, 8_543_210, 7_432_109, 123_456, 98_765, 87_654),
            Unit("stress-player-2", "北境王庭远征军不屈荣耀守门人", UnitRole.Vanguard, 0, false, 8_765_432, 7_654_321, 8_543_210, 7_432_109, 6_321_098, 98_765, 87_654, 76_543),
            Unit("stress-player-3", "远古星辉召唤议会最高执行官", UnitRole.Summoner, 0, false, 7_654_321, 6_543_210, 7_432_109, 6_321_098, 5_210_987, 87_654, 76_543, 65_432),
            Unit("stress-player-4", "皇家战地生命维护与应急支援专家", UnitRole.Support, 0, false, 6_543_210, 5_432_109, 6_321_098, 5_210_987, 4_109_876, 76_543, 65_432, 54_321),
            Unit("stress-enemy-1", "终焉王座永夜军团统御者", UnitRole.Boss, 1, false, 5_432_109, 4_321_098, 5_210_987, 4_109_876, 3_098_765, 65_432, 54_321, 43_210));
        return new BattleResult(BattleOutcome.PlayerVictory, 123_456, new string('f', 64), units, 987_654, 12_345);
    }

    private static BattleUnitReportSnapshot Unit(string runtimeId, string displayName, UnitRole role, int team, bool hero,
        float maxHealth, float finalHealth, float damage, float taken, float healing, int kills, int attacks, int healingEvents) => new(
        runtimeId, runtimeId, runtimeId, displayName, role, team, hero, false, finalHealth > 0,
        Vector2I.Zero, finalHealth, maxHealth, 765_432, 123_456, damage, taken, 654_321,
        healing, kills, 0, null, attacks, healingEvents);

    private static bool Contains(Rect2 outer, Rect2 inner) =>
        inner.Position.X >= outer.Position.X - RectEpsilon && inner.Position.Y >= outer.Position.Y - RectEpsilon &&
        inner.End.X <= outer.End.X + RectEpsilon && inner.End.Y <= outer.End.Y + RectEpsilon;
    private static bool ContainsHorizontally(Rect2 outer, Rect2 inner) =>
        inner.Position.X >= outer.Position.X - RectEpsilon && inner.End.X <= outer.End.X + RectEpsilon;
    private static void Require(string source, string token, string failure, List<string> failures)
    {
        if (!source.Contains(token, StringComparison.Ordinal)) failures.Add(failure);
    }
    private static string Read(string path) => FileAccess.FileExists(path) ? FileAccess.GetFileAsString(path) : string.Empty;
}
