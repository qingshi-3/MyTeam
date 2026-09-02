using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.UI;

public partial class BattleReportDensityContractSmoke : Node
{
    private const float RectEpsilon = 0.75f;
    private const string OutputPath = "res://.godot/qa";

    public override async void _Ready()
    {
        var failures = new List<string>();
        VerifyAuthoredContract(failures);
        try
        {
            await VerifyStatisticalDensityAsync(failures);
        }
        catch (Exception exception)
        {
            failures.Add($"statistical density runtime could not complete: {exception.GetType().Name}: {exception.Message}");
        }

        if (failures.Count > 0)
        {
            GD.PrintErr("BATTLE_REPORT_DENSITY_CONTRACT_FAILED: " + string.Join(" | ", failures));
            GetTree().Quit(1);
            return;
        }

        GD.Print("BATTLE_REPORT_DENSITY_CONTRACT_OK overview=three-core-matchups roster=compact leaderboard=six-aligned-rows detail=single");
        GetTree().Quit();
    }

    private static void VerifyAuthoredContract(List<string> failures)
    {
        var scene = Read("res://scenes/ui/BattleReportScreen.tscn");
        Require(scene, "name=\"RosterStrips\" type=\"HBoxContainer\"", "overview lacks compact two-side roster strips", failures);
        Require(scene, "name=\"LeaderboardBody\" type=\"HFlowContainer\"", "leaderboard/detail cannot responsive-wrap", failures);
        var comparison = Read("res://scenes/ui/components/BattleReportComparison.tscn");
        Require(comparison, "name=\"CoreMatchups\" type=\"VBoxContainer\"", "overview lacks compact three-row core matchup stack", failures);
        if (scene.Contains("BattleReportUnitCard.tscn", StringComparison.Ordinal)) failures.Add("density contract still retains the card-wall resource");
    }

    private async Task VerifyStatisticalDensityAsync(List<string> failures)
    {
        var host = new Control { Size = new Vector2(1600, 900), ClipContents = true };
        var screen = GD.Load<PackedScene>("res://scenes/ui/BattleReportScreen.tscn").Instantiate<BattleReportScreen>();
        screen.Theme = GD.Load<Theme>("res://content/ui/RealmTheme.tres");
        host.AddChild(screen);
        screen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(host);
        await SettleAsync();
        screen.Bind(OrdinaryResult(), "第六层常规守卫战", null!);
        await SettleAsync();

        foreach (var (width, height) in new[] { (1600, 900), (1280, 720) })
        {
            host.Size = new Vector2(width, height);
            await SettleAsync();

            screen.GetNode<Button>("%PlayerTab").EmitSignal(BaseButton.SignalName.Pressed);
            screen.GetNode<Button>("%OverviewTab").EmitSignal(BaseButton.SignalName.Pressed);
            await SettleAsync();
            VerifyOverview(screen, width, height, failures);
            await CaptureAsync(host, width, height, "PlayerOverview");

            screen.GetNode<Button>("%OffenseTab").EmitSignal(BaseButton.SignalName.Pressed);
            await SettleAsync();
            VerifyLeaderboard(screen, width, height, failures);
            await CaptureAsync(host, width, height, "PlayerOffense");
            screen.GetNode<Button>("%SurvivalTab").EmitSignal(BaseButton.SignalName.Pressed);
            await SettleAsync();
            await CaptureAsync(host, width, height, "PlayerSurvival");
            screen.GetNode<Button>("%HealingTab").EmitSignal(BaseButton.SignalName.Pressed);
            await SettleAsync();
            await CaptureAsync(host, width, height, "PlayerHealing");

            screen.GetNode<Button>("%EnemyTab").EmitSignal(BaseButton.SignalName.Pressed);
            screen.GetNode<Button>("%OverviewTab").EmitSignal(BaseButton.SignalName.Pressed);
            await SettleAsync();
            VerifyOverview(screen, width, height, failures);
            await CaptureAsync(host, width, height, "EnemyOverview");
            screen.GetNode<Button>("%OffenseTab").EmitSignal(BaseButton.SignalName.Pressed);
            await SettleAsync();
            await CaptureAsync(host, width, height, "EnemyOffense");
            screen.GetNode<Button>("%SurvivalTab").EmitSignal(BaseButton.SignalName.Pressed);
            await SettleAsync();
            await CaptureAsync(host, width, height, "EnemySurvival");
            screen.GetNode<Button>("%HealingTab").EmitSignal(BaseButton.SignalName.Pressed);
            await SettleAsync();
            VerifyHealingEmptyState(screen, width, height, failures);
            await CaptureAsync(host, width, height, "EnemyHealingZero");

            screen.GetNode<Button>("%PlayerTab").EmitSignal(BaseButton.SignalName.Pressed);
            screen.GetNode<Button>("%OverviewTab").EmitSignal(BaseButton.SignalName.Pressed);
            await SettleAsync();

            screen.Bind(TiedZeroResult(), "并列核心与双零治疗", null!);
            await SettleAsync();
            VerifyTiedZeroOverview(screen, width, height, failures);
            await CaptureAsync(host, width, height, "TieBothZeroOverview");

            screen.Bind(OrdinaryResult(), "第六层常规守卫战", null!);
            await SettleAsync();
        }

        screen.QueueFree();
        host.QueueFree();
        await SettleAsync();
    }

    private static void VerifyOverview(BattleReportScreen screen, int width, int height, List<string> failures)
    {
        var scroll = screen.GetNode<ScrollContainer>("%ReportContentScroll");
        var scrollRect = scroll.GetGlobalRect();
        var matchups = screen.GetNode<BattleReportComparison>("%OverviewComparison")
            .GetNode<VBoxContainer>("%CoreMatchups");
        if (matchups.GetChildCount() != 3) failures.Add($"{width}x{height} overview expected three core matchup rows, got {matchups.GetChildCount()}");
        foreach (var child in matchups.GetChildren().OfType<Control>())
            if (!ContainsHorizontally(scrollRect, child.GetGlobalRect())) failures.Add($"{width}x{height} core matchup escapes content width");
        foreach (var path in new[] { "%OverviewComparison", "%PlayerRosterStrip", "%EnemyRosterStrip" })
        {
            var control = screen.GetNode<Control>(path);
            if (!ContainsHorizontally(scrollRect, control.GetGlobalRect())) failures.Add($"{width}x{height} overview surface {path} escapes content width");
        }
    }

    private static void VerifyLeaderboard(BattleReportScreen screen, int width, int height, List<string> failures)
    {
        var rows = screen.GetNode<VBoxContainer>("%LeaderboardList").GetChildren().OfType<BattleReportLeaderboardRow>().ToArray();
        if (rows.Length != 6)
        {
            failures.Add($"{width}x{height} expected six aligned leaderboard rows, got {rows.Length}");
            return;
        }
        var heights = rows.Select(row => row.Size.Y).ToArray();
        if (heights.Max() - heights.Min() > RectEpsilon || heights.Max() > 70)
            failures.Add($"{width}x{height} compact row heights drift or exceed 70px: [{string.Join(',', heights.Select(value => value.ToString("0.0")))}]");
        var maximumBar = rows.Max(row => row.GetNode<ProgressBar>("%ContributionBar").Value);
        if (Math.Abs(maximumBar - 100d) > 0.1) failures.Add($"{width}x{height} common-scale leader bar is {maximumBar:0.0}");
        if (string.IsNullOrWhiteSpace(screen.SelectedRuntimeId) || !screen.GetNode<Control>("%UnitDetail").Visible)
            failures.Add($"{width}x{height} leaderboard lacks stable selection/single detail");
        GD.Print($"BATTLE_REPORT_DENSITY_MEASURED size={width}x{height} rows=6 row-height={heights[0]:0.0}px common-max={maximumBar:0.0} detail=visible");
    }

    private static void VerifyHealingEmptyState(BattleReportScreen screen, int width, int height, List<string> failures)
    {
        var emptyState = screen.GetNode<Control>("%EmptyState");
        var rows = screen.GetNode<VBoxContainer>("%LeaderboardList");
        var detail = screen.GetNode<Control>("%UnitDetail");
        if (!emptyState.Visible || rows.Visible || detail.Visible)
            failures.Add($"{width}x{height} enemy zero-healing state is not deliberate or leaves stale leaderboard detail visible");
    }

    private static void VerifyTiedZeroOverview(BattleReportScreen screen, int width, int height, List<string> failures)
    {
        var comparison = screen.GetNode<BattleReportComparison>("%OverviewComparison");
        var output = comparison.GetNode<Control>("%OutputCoreMatchup");
        var healing = comparison.GetNode<Control>("%HealingCoreMatchup");
        var outputNames = output.GetNode<Label>("%PlayerLeaderNames").Text;
        var outputValueShare = output.GetNode<Label>("%PlayerLeaderValueShare").Text;
        var healingState = healing.GetNode<Label>("%BothZeroState");
        if (outputNames != "并列甲、并列乙" ||
            !outputValueShare.StartsWith("各 ", StringComparison.Ordinal) ||
            !outputValueShare.Contains("各占 ", StringComparison.Ordinal))
            failures.Add($"{width}x{height} tied core leaders lost deterministic joined identity/value/share wording");
        if (!healingState.Visible || healingState.Text != "治疗核心 · 双方均无有效治疗")
            failures.Add($"{width}x{height} both-zero healing lacks its compact explicit overview state");
    }

    private async Task CaptureAsync(Control host, int width, int height, string suffix)
    {
        if (DisplayServer.GetName() == "headless") return;
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(OutputPath));
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var viewportImage = GetViewport().GetTexture().GetImage();
        var image = viewportImage.GetRegion(new Rect2I(0, 0, Math.Min(width, viewportImage.GetWidth()), Math.Min(height, viewportImage.GetHeight())));
        var fileName = $"UI_{width}x{height}_BattleReportStatisticalOrdinary{suffix}.png";
        var error = image.SavePng($"{ProjectSettings.GlobalizePath(OutputPath)}/{fileName}");
        if (error != Error.Ok) throw new InvalidOperationException($"capture {fileName}: {error}");
    }

    private async Task SettleAsync()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static BattleResult OrdinaryResult()
    {
        var units = ImmutableArray.Create(
            Unit("ordinary-player-1", "王庭盾卫长", UnitRole.Vanguard, 0, true, 980, 760, 2450, 620, 120, 4, 28, 2),
            Unit("ordinary-player-2", "霜原重甲卫士", UnitRole.Vanguard, 0, false, 1250, 0, 1780, 850, 80, 2, 24, 1),
            Unit("ordinary-player-3", "林地战地医师", UnitRole.Support, 0, false, 720, 510, 640, 480, 2260, 0, 12, 18),
            Unit("ordinary-player-4", "赤沙弩手", UnitRole.Ranged, 0, false, 610, 430, 2120, 400, 0, 3, 31, 0),
            Unit("ordinary-player-5", "铁炉破阵兵", UnitRole.Fighter, 0, false, 860, 0, 1940, 650, 0, 2, 27, 0),
            Unit("ordinary-player-6", "星辉召唤师", UnitRole.Summoner, 0, false, 690, 390, 1560, 350, 420, 1, 20, 4),
            Unit("ordinary-enemy-1", "熔炉守卫", UnitRole.Vanguard, 1, false, 1100, 0, 1900, 6300, 0, 2, 25, 0),
            Unit("ordinary-enemy-2", "余烬术士", UnitRole.Ranged, 1, false, 760, 0, 1450, 4190, 0, 1, 22, 0));
        return new BattleResult(BattleOutcome.PlayerVictory, 240, new string('d', 64), units, 15, 2);
    }

    private static BattleResult TiedZeroResult()
    {
        var units = ImmutableArray.Create(
            Unit("tie-a", "并列甲", UnitRole.Fighter, 0, false, 100, 100, 500, 100, 0, 1, 4, 0),
            Unit("tie-b", "并列乙", UnitRole.Ranged, 0, false, 100, 80, 500, 200, 0, 1, 4, 0),
            Unit("tie-enemy", "对照敌军", UnitRole.Vanguard, 1, false, 100, 0, 300, 1000, 0, 0, 3, 0));
        return new BattleResult(BattleOutcome.PlayerVictory, 60, new string('a', 64), units, 0, 0);
    }

    private static BattleUnitReportSnapshot Unit(string runtimeId, string displayName, UnitRole role, int team, bool hero,
        float maxHealth, float finalHealth, float damage, float taken, float healing, int kills, int attacks, int healingEvents) => new(
        runtimeId, runtimeId, runtimeId, displayName, role, team, hero, false, finalHealth > 0,
        Vector2I.Zero, finalHealth, maxHealth, 0, 125, damage, taken, 180, healing, kills, 0, null, attacks, healingEvents);

    private static bool ContainsHorizontally(Rect2 outer, Rect2 inner) =>
        inner.Position.X >= outer.Position.X - RectEpsilon && inner.End.X <= outer.End.X + RectEpsilon;
    private static void Require(string source, string token, string failure, List<string> failures)
    {
        if (!source.Contains(token, StringComparison.Ordinal)) failures.Add(failure);
    }
    private static string Read(string path) => FileAccess.FileExists(path) ? FileAccess.GetFileAsString(path) : string.Empty;
}
