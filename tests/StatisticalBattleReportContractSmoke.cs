using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.UI;

public partial class StatisticalBattleReportContractSmoke : Node
{
    private static readonly string[] RequiredComponents =
    [
        "res://scenes/ui/components/BattleReportComparison.tscn",
        "res://scenes/ui/components/BattleReportCoreMatchupRow.tscn",
        "res://scenes/ui/components/BattleReportRosterStrip.tscn",
        "res://scenes/ui/components/BattleReportLeaderboardHeader.tscn",
        "res://scenes/ui/components/BattleReportLeaderboardRow.tscn",
        "res://scenes/ui/components/BattleReportUnitDetail.tscn"
    ];

    public override async void _Ready()
    {
        var failures = new List<string>();
        VerifyDerivation(failures);
        var authoredReady = VerifyAuthoredContract(failures);
        if (authoredReady)
        {
            try
            {
                await VerifyRuntimeContractAsync(failures);
            }
            catch (Exception exception)
            {
                failures.Add($"statistical report runtime could not complete: {exception.GetType().Name}: {exception.Message}");
            }
        }

        if (failures.Count > 0)
        {
            GD.PrintErr("STATISTICAL_BATTLE_REPORT_CONTRACT_FAILED: " + string.Join(" | ", failures));
            GetTree().Quit(1);
            return;
        }

        GD.Print("STATISTICAL_BATTLE_REPORT_CONTRACT_OK overview=three-core-matchups values=authoritative shares=team-zero-safe ties=retained leaderboard=fixed-columns selection=stable detail=single continue=once");
        GetTree().Quit();
    }

    private static void VerifyDerivation(List<string> failures)
    {
        var offense = BattleReportViewModels.Build(Result(), 0, BattleReportDimension.Offense);
        if (offense.Units.Count != 6 || offense.Units[0].Unit.RuntimeId != "player-1" ||
            Math.Abs(offense.PrimaryMaximum - 9000f) > 0.01f)
            failures.Add("offense derivation lost deterministic order or selected-side common maximum");
        if (offense.OutputLeaders.Select(unit => unit.Unit.RuntimeId).SequenceEqual(new[] { "player-1" }) is false ||
            offense.DamageTakenLeaders.Select(unit => unit.Unit.RuntimeId).SequenceEqual(new[] { "player-2" }) is false ||
            offense.HealingLeaders.Select(unit => unit.Unit.RuntimeId).SequenceEqual(new[] { "player-3" }) is false)
            failures.Add("overview leader derivation does not match positive authoritative facts");

        var tie = BattleReportViewModels.Build(TieAndZeroResult(), 0, BattleReportDimension.Healing);
        if (!tie.OutputLeaders.Select(unit => unit.Unit.RuntimeId).SequenceEqual(new[] { "tie-a", "tie-b" }))
            failures.Add("tied positive leaders are not retained deterministically");
        if (tie.HealingLeaders.Count != 0 || !tie.ShowHealingEmptyState || tie.PrimaryMaximum != 0)
            failures.Add("all-zero healing category invented a leader or lost its deliberate empty state");

        VerifyCoreMatchupDerivation(failures);
    }

    private static void VerifyCoreMatchupDerivation(List<string> failures)
    {
        var builder = typeof(BattleReportViewModels).GetMethod(
            "BuildCoreMatchups",
            BindingFlags.Public | BindingFlags.Static);
        if (builder is null)
        {
            failures.Add("report derivation lacks fixed two-team core matchups");
            return;
        }

        VerifyCoreMatchupSet(builder.Invoke(null, [Result()]), false, false, failures);
        VerifyCoreMatchupSet(builder.Invoke(null, [TieAndZeroResult()]), true, false, failures);
        VerifyCoreMatchupSet(builder.Invoke(null, [OneSidedHealingResult()]), false, true, failures);
    }

    private static void VerifyCoreMatchupSet(
        object? value,
        bool expectTiedOutputAndBothZeroHealing,
        bool expectOneSidedZeroHealing,
        List<string> failures)
    {
        if (value is not System.Collections.IEnumerable enumerable)
        {
            failures.Add("core matchup builder did not return an enumerable authored-row model set");
            return;
        }

        var matchups = enumerable.Cast<object>().ToArray();
        if (matchups.Length != 3)
        {
            failures.Add($"core matchup derivation expected exactly three rows, got {matchups.Length}");
            return;
        }

        var dimensions = matchups.Select(item => ReadProperty(item, "Dimension")?.ToString()).ToArray();
        if (!dimensions.SequenceEqual(new[] { "Offense", "Survival", "Healing" }))
            failures.Add("core matchup rows lost fixed output/survival/healing order");

        var output = matchups[0];
        var healing = matchups[2];
        var playerOutput = ReadEnumerableProperty(output, "PlayerLeaders");
        var enemyOutput = ReadEnumerableProperty(output, "EnemyLeaders");
        if (playerOutput.Length == 0 || enemyOutput.Length == 0)
            failures.Add("positive output matchup lost one side's authoritative leader");

        if (expectTiedOutputAndBothZeroHealing)
        {
            var ids = playerOutput.Select(item => ReadProperty(ReadProperty(item, "Unit")!, "RuntimeId")?.ToString()).ToArray();
            if (!ids.SequenceEqual(new[] { "tie-a", "tie-b" }))
                failures.Add("core matchup did not retain deterministic tied player leaders");
            if (!(bool)(ReadProperty(healing, "BothSidesZero") ?? false))
                failures.Add("both-zero healing matchup lacks its compact explicit state");
        }

        if (expectOneSidedZeroHealing)
        {
            var playerHealing = ReadEnumerableProperty(healing, "PlayerLeaders");
            var enemyHealing = ReadEnumerableProperty(healing, "EnemyLeaders");
            if (playerHealing.Length != 0 || enemyHealing.Length != 1 ||
                (bool)(ReadProperty(healing, "BothSidesZero") ?? true))
                failures.Add("one-sided zero healing did not retain the positive enemy leader without inventing a player leader");
        }
    }

    private static bool VerifyAuthoredContract(List<string> failures)
    {
        var ready = true;
        foreach (var path in RequiredComponents)
        {
            if (FileAccess.FileExists(path)) continue;
            failures.Add($"missing authored statistical component {path}");
            ready = false;
        }

        var screenScene = Read("res://scenes/ui/BattleReportScreen.tscn");
        foreach (var token in new[]
                 {
                     "name=\"ReportContentScroll\"", "name=\"OverviewPage\"", "name=\"LeaderboardPage\"",
                     "name=\"LeaderboardList\"", "name=\"UnitDetail\"", "name=\"OverviewComparison\""
                 })
            Require(screenScene, token, $"screen lacks authored statistical surface {token}", failures, ref ready);
        if (screenScene.Contains("BattleReportUnitCard.tscn", StringComparison.Ordinal) ||
            screenScene.Contains("name=\"ReportCards\"", StringComparison.Ordinal))
        {
            failures.Add("card wall remains the report's primary authored surface");
            ready = false;
        }
        foreach (var obsolete in new[]
                 {
                     "BattleReportLeaderSummary.tscn", "name=\"LeaderSummaries\"", "name=\"OutcomeFacts\"",
                     "name=\"PlayerHealthBar\"", "name=\"EnemyHealthBar\"", "name=\"PlayerDamageBar\"",
                     "name=\"EnemyDamageBar\"", "name=\"PlayerHealingBar\"", "name=\"EnemyHealingBar\""
                 })
            Reject(screenScene + Read("res://scenes/ui/components/BattleReportComparison.tscn"), obsolete,
                $"overview retains obsolete team-total/separate-leader surface {obsolete}", failures, ref ready);

        var comparisonScene = Read("res://scenes/ui/components/BattleReportComparison.tscn");
        foreach (var token in new[]
                 {
                     "name=\"CoreMatchups\"", "name=\"OutputCoreMatchup\"",
                     "name=\"SurvivalCoreMatchup\"", "name=\"HealingCoreMatchup\"",
                     "name=\"EnvironmentDamage\""
                 })
            Require(comparisonScene, token, $"core comparison lacks authored surface {token}", failures, ref ready);

        var matchupScene = Read("res://scenes/ui/components/BattleReportCoreMatchupRow.tscn");
        foreach (var token in new[]
                 {
                     "name=\"PlayerLeaderNames\"", "name=\"PlayerLeaderValueShare\"",
                     "name=\"Category\"", "name=\"EnemyLeaderNames\"",
                     "name=\"EnemyLeaderValueShare\"", "name=\"BothZeroState\""
                 })
            Require(matchupScene, token, $"core matchup row lacks authored field {token}", failures, ref ready);

        var rowScene = Read("res://scenes/ui/components/BattleReportLeaderboardRow.tscn");
        foreach (var token in new[]
                 {
                     "type=\"Button\"", "name=\"Rank\"", "name=\"UnitIdentity\"", "name=\"PrimaryValue\"",
                     "name=\"SecondaryValue1\"", "name=\"SecondaryValue2\"", "name=\"SecondaryValue3\"",
                     "name=\"SecondaryValue4\"", "name=\"ContributionBar\""
                 })
            Require(rowScene, token, $"leaderboard row lacks {token}", failures, ref ready);

        var headerScene = Read("res://scenes/ui/components/BattleReportLeaderboardHeader.tscn");
        foreach (var token in new[]
                 {
                     "name=\"RankHeader\"", "name=\"UnitHeader\"", "name=\"PrimaryHeader\"",
                     "name=\"SecondaryHeader1\"", "name=\"SecondaryHeader2\"", "name=\"SecondaryHeader3\"",
                     "name=\"SecondaryHeader4\""
                 })
            Require(headerScene, token, $"leaderboard header lacks fixed column {token}", failures, ref ready);

        var controller = Read("res://src/UI/BattleReportScreen.cs");
        foreach (var token in new[] { "SelectedRuntimeId", "BindOverview", "BuildCoreMatchups", "BindLeaderboard", "SelectRow" })
            Require(controller, token, $"screen controller lacks {token}", failures, ref ready);
        var models = Read("res://src/UI/BattleReportModels.cs");
        foreach (var token in new[] { "PrimaryMaximum", "OutputLeaders", "DamageTakenLeaders", "HealingLeaders", "BattleReportCoreMatchupViewModel", "BuildCoreMatchups" })
            Require(models, token, $"report derivation lacks {token}", failures, ref ready);

        return ready;
    }

    private async Task VerifyRuntimeContractAsync(List<string> failures)
    {
        var host = new Control { Size = new Vector2(1280, 720), ClipContents = true };
        var screen = GD.Load<PackedScene>("res://scenes/ui/BattleReportScreen.tscn").Instantiate<BattleReportScreen>();
        screen.Theme = GD.Load<Theme>("res://content/ui/RealmTheme.tres");
        host.AddChild(screen);
        screen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(host);
        await SettleAsync();

        screen.Bind(Result(), "统计契约遭遇战", null!);
        await SettleAsync();
        if (!screen.GetNode<Control>("%OverviewPage").Visible)
            failures.Add("default report does not expose the statistical overview");
        if (screen.GetNodeOrNull("%ReportCards") is not null)
            failures.Add("runtime still instantiates the obsolete card wall");
        var coreMatchups = screen.GetNode<BattleReportComparison>("%OverviewComparison")
            .GetNode<Container>("%CoreMatchups");
        if (coreMatchups.GetChildCount() != 3)
            failures.Add("overview does not expose exactly three authored core matchup rows");
        VerifyPositiveMatchup(coreMatchups.GetNode<Control>("OutputCoreMatchup"), "王庭盾卫长", "余烬术士", failures);

        screen.Bind(TieAndZeroResult(), "并列与双零治疗", null!);
        await SettleAsync();
        var healingRow = screen.GetNode<BattleReportComparison>("%OverviewComparison")
            .GetNode<Control>("%HealingCoreMatchup");
        if (!healingRow.GetNode<Label>("%BothZeroState").Visible ||
            healingRow.GetNode<Label>("%BothZeroState").Text != "治疗核心 · 双方均无有效治疗" ||
            healingRow.GetNode<Control>("%MatchupContent").Visible)
            failures.Add("both-zero healing did not collapse to the compact explicit Chinese state");

        screen.Bind(OneSidedHealingResult(), "单侧零治疗", null!);
        await SettleAsync();
        healingRow = screen.GetNode<BattleReportComparison>("%OverviewComparison")
            .GetNode<Control>("%HealingCoreMatchup");
        if (healingRow.GetNode<Label>("%BothZeroState").Visible ||
            healingRow.GetNode<Label>("%PlayerLeaderNames").Text != "无有效治疗" ||
            healingRow.GetNode<Label>("%EnemyLeaderNames").Text != "敌方医师" ||
            !healingRow.GetNode<Label>("%EnemyLeaderValueShare").Text.Contains('%'))
            failures.Add("one-sided zero healing lost its explicit zero side or positive identity/value/share side");

        screen.Bind(Result(), "统计契约遭遇战", null!);
        await SettleAsync();

        screen.GetNode<Button>("%OffenseTab").EmitSignal(BaseButton.SignalName.Pressed);
        await SettleAsync();
        VerifyLeaderboard(screen, BattleReportDimension.Offense, 6, failures);

        var rows = screen.GetNode<Container>("%LeaderboardList").GetChildren().OfType<Button>().ToArray();
        if (rows.Length > 1)
        {
            rows[1].EmitSignal(BaseButton.SignalName.Pressed);
            await SettleAsync();
            var selected = ReadSelectedRuntimeId(screen);
            if (selected != "player-4") failures.Add($"row selection expected player-4, got '{selected}'");
            if (!screen.GetNode<Control>("%UnitDetail").Visible)
                failures.Add("selected row does not reveal the single authored detail panel");
        }

        screen.GetNode<Button>("%SurvivalTab").EmitSignal(BaseButton.SignalName.Pressed);
        await SettleAsync();
        VerifyLeaderboard(screen, BattleReportDimension.Survival, 6, failures);
        if (string.IsNullOrWhiteSpace(ReadSelectedRuntimeId(screen)))
            failures.Add("dimension refresh did not preserve or recover a valid stable selection");

        var continueCount = 0;
        screen.ContinueRequested += () => continueCount++;
        var continueButton = screen.GetNode<Button>("%ReportContinue");
        continueButton.EmitSignal(BaseButton.SignalName.Pressed);
        continueButton.EmitSignal(BaseButton.SignalName.Pressed);
        if (continueCount != 1) failures.Add($"continue emitted {continueCount} times instead of once");

        screen.QueueFree();
        host.QueueFree();
        await SettleAsync();
    }

    private static void VerifyLeaderboard(
        BattleReportScreen screen,
        BattleReportDimension dimension,
        int expectedRows,
        List<string> failures)
    {
        if (screen.SelectedDimension != dimension)
            failures.Add($"dimension switch expected {dimension}, got {screen.SelectedDimension}");
        var rows = screen.GetNode<Container>("%LeaderboardList").GetChildren().OfType<Button>().ToArray();
        if (rows.Length != expectedRows)
            failures.Add($"{dimension} expected {expectedRows} leaderboard rows, got {rows.Length}");
        var values = rows.Select(row => row.GetNode<ProgressBar>("%ContributionBar").Value).ToArray();
        if (values.Length > 0 && Math.Abs(values.Max() - 100d) > 0.1)
            failures.Add($"{dimension} common-scale leader bar is {values.Max():0.0} instead of 100");
    }

    private static string? ReadSelectedRuntimeId(BattleReportScreen screen)
    {
        var property = screen.GetType().GetProperty("SelectedRuntimeId", BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(screen) as string;
    }

    private async Task SettleAsync()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static BattleResult Result()
    {
        var units = ImmutableArray.Create(
            Unit("player-1", "王庭盾卫长", UnitRole.Vanguard, 0, true, 1000, 700, 9000, 4000, 1200, 4, 22, 3),
            Unit("player-2", "霜原重甲卫士", UnitRole.Vanguard, 0, false, 1200, 0, 6500, 7600, 0, 2, 18, 0),
            Unit("player-3", "林地战地医师", UnitRole.Support, 0, false, 760, 540, 800, 1800, 8800, 0, 10, 11),
            Unit("player-4", "赤沙弩手", UnitRole.Ranged, 0, false, 620, 410, 7200, 1600, 0, 3, 28, 0),
            Unit("player-5", "铁炉破阵兵", UnitRole.Fighter, 0, false, 880, 0, 5100, 4200, 0, 2, 20, 0),
            Unit("player-6", "星辉召唤师", UnitRole.Summoner, 0, false, 700, 390, 3200, 2300, 600, 1, 16, 2),
            Unit("enemy-1", "熔炉守卫", UnitRole.Vanguard, 1, false, 1200, 0, 5400, 18000, 0, 1, 18, 0),
            Unit("enemy-2", "余烬术士", UnitRole.Ranged, 1, false, 800, 0, 8800, 13800, 0, 2, 24, 0));
        return new BattleResult(BattleOutcome.PlayerVictory, 240, new string('e', 64), units, 15, 2);
    }

    private static BattleResult TieAndZeroResult()
    {
        var units = ImmutableArray.Create(
            Unit("tie-a", "并列甲", UnitRole.Fighter, 0, false, 100, 100, 500, 100, 0, 1, 4, 0),
            Unit("tie-b", "并列乙", UnitRole.Ranged, 0, false, 100, 80, 500, 200, 0, 1, 4, 0),
            Unit("tie-enemy", "对照敌军", UnitRole.Vanguard, 1, false, 100, 0, 300, 1000, 0, 0, 3, 0));
        return new BattleResult(BattleOutcome.PlayerVictory, 60, new string('a', 64), units, 0, 0);
    }

    private static BattleResult OneSidedHealingResult()
    {
        var units = ImmutableArray.Create(
            Unit("dry-player", "无治疗前卫", UnitRole.Vanguard, 0, false, 100, 50, 200, 300, 0, 0, 3, 0),
            Unit("healing-enemy", "敌方医师", UnitRole.Support, 1, false, 100, 80, 100, 200, 240, 0, 2, 3));
        return new BattleResult(BattleOutcome.PlayerDefeat, 60, new string('b', 64), units, 0, 0);
    }

    private static void VerifyPositiveMatchup(Control row, string playerName, string enemyName, List<string> failures)
    {
        var playerNames = row.GetNode<Label>("%PlayerLeaderNames").Text;
        var enemyNames = row.GetNode<Label>("%EnemyLeaderNames").Text;
        var playerValueShare = row.GetNode<Label>("%PlayerLeaderValueShare").Text;
        var enemyValueShare = row.GetNode<Label>("%EnemyLeaderValueShare").Text;
        if (!playerNames.Contains(playerName, StringComparison.Ordinal) ||
            !enemyNames.Contains(enemyName, StringComparison.Ordinal) ||
            !playerValueShare.Contains('%') || !enemyValueShare.Contains('%'))
            failures.Add("positive core matchup does not expose both identities, absolute values, and team shares");
    }

    private static object[] ReadEnumerableProperty(object item, string propertyName) =>
        ReadProperty(item, propertyName) is System.Collections.IEnumerable values
            ? values.Cast<object>().ToArray()
            : [];

    private static object? ReadProperty(object item, string propertyName) =>
        item.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(item);

    private static BattleUnitReportSnapshot Unit(
        string runtimeId,
        string displayName,
        UnitRole role,
        int team,
        bool hero,
        float maxHealth,
        float finalHealth,
        float damage,
        float taken,
        float healing,
        int kills,
        int attacks,
        int healingEvents) => new(
        runtimeId, runtimeId, runtimeId, displayName, role, team, hero, false, finalHealth > 0,
        Vector2I.Zero, finalHealth, maxHealth, 120, 125, damage, taken, 180,
        healing, kills, 0, null, attacks, healingEvents);

    private static void Require(string source, string token, string failure, List<string> failures, ref bool ready)
    {
        if (source.Contains(token, StringComparison.Ordinal)) return;
        failures.Add(failure);
        ready = false;
    }

    private static void Reject(string source, string token, string failure, List<string> failures, ref bool ready)
    {
        if (!source.Contains(token, StringComparison.Ordinal)) return;
        failures.Add(failure);
        ready = false;
    }

    private static string Read(string path) => FileAccess.FileExists(path) ? FileAccess.GetFileAsString(path) : string.Empty;
}
