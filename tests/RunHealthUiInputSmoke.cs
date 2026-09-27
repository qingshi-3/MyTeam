using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Isolated fixtures feed real production screens; all confirmation actions use GUI input.
public partial class RunHealthUiInputSmoke : Node
{
    private UiInputStage _stage = null!;
    private const string Output = "res://.godot/run-health-review";
    public override async void _Ready()
    {
        var code = 0;
        GameRoot? game = null;
        try
        {
            Require(DisplayServer.GetName() != "headless", "rendered driver required");
            _stage = new UiInputStage(this, true);
            GetWindow().Position = new Vector2I(-4000, -4000);
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(Output));
            game = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
            game.SaveNamespace = $"tests/run-health-ui/{Guid.NewGuid():N}";
            _stage.AddChild(game);
            for (var i = 0; i < 300 && game.Content is null; i++) await Frames(1);
            Require(game.Content is not null, "production bootstrap");
            var content = game.Content!;
            var compiled = GameProjectCompiler.Compile(game.ProjectDefinition!, content.Graph);
            var package = new CompiledGamePackage(content, compiled.Project!, content.PublicationVersion);
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            var (app, save) = RunHealthFixture.New(package);
            SetApp(game, app);
            RunHealthFixture.BattleNode(app, TowerNodeType.Combat);
            var encounter = app.CurrentEncounter();
            game.Flow.SetEncounterForTesting(encounter);
            game.Flow.ShowDeployment();
            await Frames(6);
            Require(screens.Deployment.GetNode<Label>("%RunRisk").Text.Contains("20"), "visible normal defeat cost");
            CheckHealth(game, 100);
            await Capture("deployment");

            var result = RunHealthFixture.Result(app.BuildBattleConfig(encounter, false), BattleOutcome.PlayerDefeat);
            game.Flow.AcceptBattleResult(result);
            game.Flow.ShowBattleReport();
            await Frames(5);
            Require(screens.BattleReport.GetNode<Label>("%SettlementMessage").Text.Contains("100 → 80"), "report shows actual loss");
            await Capture("defeat-report");
            await Click(screens.BattleReport.GetNode<Button>("%ReportContinue"));
            Require(screens.Tower.IsVisibleInTree() && app.ActiveRun?.CurrentRunHealth == 80, "defeat continue click routes to tower");
            CheckHealth(game, 80);
            await Capture("continued-route");
            var combat = Descendants<ChoiceCard>(screens.Tower).FirstOrDefault(card => card.StableId is "Combat" or "Elite");
            Require(combat is not null, "fixture next route has a battle");
            await Click(combat!);
            Require(screens.Deployment.IsVisibleInTree(), "next node opens deployment by click");
            await Click(screens.Deployment.GetNode<Button>("%StartBattleButton"));
            Require(screens.Battle.IsVisibleInTree() && screens.Battle.HasActiveBattle, "next battle starts by click");
            screens.Battle.SetPaused(true);
            Require(screens.Battle.GetNode<RunHealthDisplay>("%RunHealth").GetNode<SemanticChip>("Health").DisplayText.Contains("80/100"), "battle HP snapshot");
            Require(!screens.Battle.GetNode<RunHealthDisplay>("%RunHealth").GetGlobalRect().Intersects(
                screens.Battle.GetNode<Control>("%BattleInspectorDock").GetNode<Control>("Rail").GetGlobalRect()), "battle health does not overlap inspector controls");
            await Capture("next-battle");
            screens.Battle.StopBattle();

            // Separate camp fixture verifies the production generic-offer route, including save error and keyboard action.
            (app, save) = RunHealthFixture.New(package);
            SetApp(game, app);
            app.ActiveRun!.CurrentRunHealth = 20;
            RunHealthFixture.Camp(app, save);
            game.Flow.ShowTower();
            await Frames(6);
            Require(screens.Reward.IsVisibleInTree(), "camp uses actual generic offer screen");
            var recovery = Descendants<RunOfferChoiceCard>(screens.Reward).Single(card => card.StableId == "recover_run_health");
            Require(!recovery.ConfirmButton.Disabled, "damaged Run recovery is actionable");
            await Capture("camp-low-health");
            save.FailWrites = true;
            await Click(recovery.ConfirmButton);
            Require(screens.Reward.IsVisibleInTree() && app.ActiveRun.CurrentRunHealth == 20 && app.PendingOffer is not null, "camp save error stays actionable without gain");
            save.FailWrites = false;
            await Click(recovery.ConfirmButton);
            Require(screens.Tower.IsVisibleInTree() && app.ActiveRun.CurrentRunHealth == 45, "recovery click commits +25 and leaves camp");
            CheckHealth(game, 45);

            app.ActiveRun.CurrentRunHealth = 100;
            RunHealthFixture.Camp(app, save);
            game.Flow.ShowTower();
            await Frames(6);
            recovery = Descendants<RunOfferChoiceCard>(screens.Reward).Single(card => card.StableId == "recover_run_health");
            Require(recovery.ConfirmButton.Disabled, "full health recovery button disabled");
            await Click(recovery.ConfirmButton);
            Require(app.PendingOffer is not null, "disabled recovery does not consume opportunity");
            await Capture("camp-full-health");
            var goldCard = Descendants<RunOfferChoiceCard>(screens.Reward).Single(card => card.StableId == "gold");
            goldCard.ConfirmButton.GrabFocus();
            var gold = app.ActiveRun.Gold;
            await KeyPress(Key.Enter);
            Require(screens.Tower.IsVisibleInTree() && app.ActiveRun.Gold == gold + app.Rules.RestGold && app.ActiveRun.CurrentRunHealth == 100,
                "keyboard gold selection consumes camp without recovery");

            RunHealthFixture.BattleNode(app, TowerNodeType.Boss);
            encounter = app.CurrentEncounter();
            game.Flow.SetEncounterForTesting(encounter);
            game.Flow.ShowDeployment();
            await Frames(6);
            Require(screens.Deployment.GetNode<Label>("%RunRisk").Text.Contains("无论剩余生命"), "Boss hard-failure warning");
            await Capture("boss-warning");
            game.Flow.ResetPendingBattleFlow();
            game.Flow.AcceptBattleResult(RunHealthFixture.Result(app.BuildBattleConfig(encounter, false), BattleOutcome.PlayerDefeat));
            game.Flow.ShowBattleReport();
            await Frames(5);
            Require(app.ActiveRun is null && screens.BattleReport.GetNode<Label>("%SettlementMessage").Text.Contains("100/100"), "Boss report explains full-health ending");
            await Capture("boss-report");
            await Click(screens.BattleReport.GetNode<Button>("%ReportContinue"));
            Require(screens.Result.IsVisibleInTree(), "Boss report continue opens result");

            // Persistence retry remains a real report button path for a continuing defeat.
            (app, save) = RunHealthFixture.New(package);
            SetApp(game, app);
            RunHealthFixture.BattleNode(app, TowerNodeType.Combat);
            encounter = app.CurrentEncounter();
            game.Flow.SetEncounterForTesting(encounter);
            save.FailWrites = true;
            game.Flow.AcceptBattleResult(RunHealthFixture.Result(app.BuildBattleConfig(encounter, false), BattleOutcome.PlayerDefeat));
            game.Flow.ShowBattleReport();
            await Frames(4);
            Require(screens.BattleReport.GetNode<Button>("%ReportContinue").Text == "重试结算" && app.ActiveRun!.CurrentRunHealth == 100, "failed settlement has no visible committed loss");
            save.FailWrites = false;
            await Click(screens.BattleReport.GetNode<Button>("%ReportContinue"));
            Require(screens.Tower.IsVisibleInTree() && app.ActiveRun!.CurrentRunHealth == 80, "retry click charges once and continues");

            game.Flow.ShowBattleLab();
            await Frames(3);
            Require(!game.GetNode<ArmyOverviewController>("ArmyOverview").Visible, "laboratory hides Run resources");
            GD.Print("RUN_HEALTH_UI_INPUT_OK defeat-continue route-start battle-hud camp-retry camp-recovery full-disabled keyboard-gold boss-warning boss-terminal retry lab-hidden");
        }
        catch (Exception error) { code = 1; GD.PrintErr("RUN_HEALTH_UI_INPUT_FAILED: " + error); }
        finally { game?.QueueFree(); await Frames(3); }
        GetTree().Quit(code);
    }

    private static void SetApp(GameRoot game, RunApplication app)
    {
        typeof(GameRoot).GetField("_app", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, app);
        game.Flow.ResetPendingBattleFlow();
    }
    private static void CheckHealth(GameRoot game, int health)
    {
        var display = game.GetNode<ArmyOverviewController>("ArmyOverview").GetNode<ArmyResourceStrip>("%ResourceStrip").GetNode<RunHealthDisplay>("%RunHealth");
        Require(display.GetNode<SemanticChip>("Health").DisplayText.Contains($"{health}/100"), "updated global HUD");
        Require(display.GetGlobalRect().End.X <= 1600 && display.GetGlobalRect().Position.X >= 0, "HUD within viewport");
    }
    private async Task Click(Control control)
    {
        Require(control.IsVisibleInTree() && _stage.Viewport.GetVisibleRect().HasPoint(control.GetGlobalRect().GetCenter()), "visible click target " + control.GetPath());
        var point = control.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await Frames(2);
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(5);
    }
    private async Task KeyPress(Key key)
    {
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
        await Frames(4);
    }
    private async Task Capture(string name)
    {
        await Frames(3);
        var path = ProjectSettings.GlobalizePath($"{Output}/{name}.png");
        Require(_stage.Viewport.GetTexture().GetImage().SavePng(path) == Error.Ok, "save rendered capture");
        GD.Print("RUN_HEALTH_CAPTURE " + path);
    }
    private async Task Frames(int count) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static void Require(bool value, string message) => RunHealthContractSmoke.Require(value, message);
}
