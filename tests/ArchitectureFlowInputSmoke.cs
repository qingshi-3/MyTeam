using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

// Real authored screens and isolated inputs. Campaign outcomes and storage faults
// are controlled fixtures; laboratory completion runs the actual simulation.
public partial class ArchitectureFlowInputSmoke : Node
{
    private UiInputStage _stage = null!;
    private string _directory = "";
    public override async void _Ready()
    {
        GameRoot? game = null;
        GameFlowCoordinator? flow = null;
        var exit = 0;
        try
        {
            Require(DisplayServer.GetName() != "headless", "rendered input required");
            var space = "tests/architecture-flow/" + Guid.NewGuid().ToString("N");
            _directory = ProjectSettings.GlobalizePath("user://" + space);
            _stage = new UiInputStage(this, true);
            _stage.Resize(new(960, 540));
            game = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
            game.SaveNamespace = space;
            _stage.AddChild(game);
            await Until(() => game.Content is not null, "production package ready");
            var content = game.Content!;
            var compiled = GameProjectCompiler.Compile(game.ProjectDefinition!, content.Graph);
            var package = new CompiledGamePackage(content, compiled.Project!, content.PublicationVersion);
            var (app, save) = RunHealthFixture.New(package);
            game.Flow.Dispose();
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            flow = new(() => app, screens, package.Project.Presentation, () => { });
            flow.Start();
            await Frames(3);
            var beforeLab = save.Json;
            await EnterLab(screens);
            var frozen = screens.BattleLab.CurrentSnapshot!.CanonicalDigest;
            await FocusActivate(screens.BattleLab.GetNode<Button>("%StartButton"));
            Require(screens.Battle.HasActiveBattle && screens.Battle.IsVisibleInTree(), "lab starts real battle");
            await Activate(screens.Battle.GetNode<Button>("%PauseButton"));
            Require(screens.Battle.IsPaused, "real pause input");
            await Activate(screens.Battle.GetNode<Button>("%ResetBattleButton"));
            Require(screens.Battle.HasActiveBattle && !screens.Battle.IsPaused, "lab reset creates independent battle");
            await Activate(screens.Battle.GetNode<Button>("%ReturnConfigurationButton"));
            Require(!screens.Battle.HasActiveBattle && screens.BattleLab.CurrentSnapshot!.CanonicalDigest == frozen,
                "lab return cleans runtime and restores frozen configuration");
            await Activate(screens.BattleLab.GetNode<Button>("%BackButton"));
            await EnterLab(screens);
            Require(screens.BattleLab.CurrentSnapshot!.CanonicalDigest == frozen, "lab reentry keeps configuration");
            await FocusActivate(screens.BattleLab.GetNode<Button>("%StartButton"));
            await Activate(screens.Battle.GetNode<Button>("%PauseButton"));
            var budget = 0;
            while (screens.Battle.Outcome == BattleOutcome.Running && budget++ < 2000)
                Require(screens.Battle.StepOneTick(), "lab deterministic completion");
            Require(screens.Battle.TerminalResult is not null, "lab produced terminal result");
            await Until(() => screens.BattleReport.IsVisibleInTree(), "lab terminal transition to report", 360);
            await Capture("lab-report.png");
            await Activate(screens.BattleReport.GetNode<Button>("%ReportContinue"));
            Require(screens.BattleLab.IsVisibleInTree() && !screens.Battle.HasActiveBattle &&
                screens.BattleLab.CurrentSnapshot!.CanonicalDigest == frozen && save.Json == beforeLab,
                "lab report returns without campaign settlement or persistence writes");
            await Activate(screens.BattleLab.GetNode<Button>("%BackButton"));

            RunHealthFixture.BattleNode(app, TowerNodeType.Combat);
            var encounter = app.CurrentEncounter();
            flow.SetEncounterForTesting(encounter);
            flow.ShowDeployment();
            await Frames(3);
            var battleResult = RunHealthFixture.Result(app.BuildBattleConfig(encounter, false), BattleOutcome.PlayerVictory);
            var beforeFailure = save.Json;
            var floor = app.Run!.FloorIndex;
            save.FailWrites = true;
            flow.ResetPendingBattleFlow();
            flow.AcceptBattleResult(battleResult);
            Require(!flow.BattleResolutionCommitted && flow.PendingBattleResolution?.Failure == RunBattleResolutionFailure.PersistenceFailed &&
                app.Run.FloorIndex == floor && save.Json == beforeFailure, "failed settlement preserves authoritative run");
            var rejected = flow.PendingBattleResolution;
            flow.AcceptBattleResult(battleResult);
            Require(ReferenceEquals(rejected, flow.PendingBattleResolution), "duplicate completion retried automatically");
            flow.ShowBattleReport();
            await Frames(3);
            var retry = screens.BattleReport.GetNode<Button>("%ReportContinue");
            Require(retry.Text == "重试结算", "retry shown on authored report");
            await Activate(retry);
            Require(!flow.BattleResolutionCommitted && screens.BattleReport.IsVisibleInTree(), "failed real retry stays actionable");
            await Capture("settlement-retry.png");
            save.FailWrites = false;
            await Activate(retry);
            Require(flow.BattleResolutionCommitted && app.Run.FloorIndex == floor + 1 && screens.Reward.IsVisibleInTree(),
                "real retry commits once and routes to reward");
            var accepted = flow.PendingBattleResolution;
            var afterSuccess = save.Json;
            flow.AcceptBattleResult(battleResult); flow.ShowBattleReport();
            Require(ReferenceEquals(accepted, flow.PendingBattleResolution) && save.Json == afterSuccess,
                "duplicate callbacks after success changed settlement");
            await Capture("settlement-reward.png");
            GD.Print("ARCHITECTURE_FLOW_INPUT_OK viewport=960x540 lab=start,pause,reset,return,reenter,terminal-report campaign=controlled-outcome,failed-retry,successful-retry,exactly-once desktop-pointer=unchanged");
        }
        catch (Exception e) { GD.PrintErr("ARCHITECTURE_FLOW_INPUT_FAILED " + e); exit = 1; if (_stage is not null) await Capture("flow-failure.png"); }
        finally { flow?.Dispose(); game?.QueueFree(); }
        await Frames(5);
        GetTree().Quit(exit);
    }
    private async Task EnterLab(AppScreenHost screens)
    {
        var button = screens.MainMenu.GetNode<Button>("Center/Panel/Menu/BattleLabButton");
        if (!button.IsVisibleInTree()) await Activate(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ToolsButton"));
        await Activate(button);
        Require(screens.BattleLab.IsVisibleInTree() && screens.BattleLab.CurrentSnapshot is not null, "real lab entry");
    }
    private async Task Until(Func<bool> condition, string label, int budget = 180)
    {
        for (var i = 0; i < budget && !condition(); i++) await Frames(1);
        Require(condition(), label); await Frames(3);
    }
    private async Task Frames(int count)
    {
        for (var i = 0; i < count; i++) { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); RenderingServer.ForceDraw(false); }
    }
    private async Task Activate(Control target)
    {
        var point = target.GetGlobalRect().GetCenter();
        Require(target.IsVisibleInTree(), "visible input target: " + target.GetPath());
        if (!_stage.Viewport.GetVisibleRect().HasPoint(point))
        {
            Require(target is Button, "offscreen input must expose keyboard focus");
            await FocusActivate((Button)target);
            return;
        }
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point }); await Frames(1);
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(5);
    }
    private async Task FocusActivate(Button target)
    {
        // Existing Lab controls extend beyond the fixed 960x540 test viewport.
        // Exercise its actual keyboard path; do not fake a click outside the viewport.
        Require(target.IsVisibleInTree() && !target.Disabled, "enabled keyboard target");
        for (var i = 0; i < 128 && _stage.Viewport.GuiGetFocusOwner() != target; i++)
        {
            _stage.Push(new InputEventKey { Keycode = Key.Tab, PhysicalKeycode = Key.Tab, Pressed = true });
            _stage.Push(new InputEventKey { Keycode = Key.Tab, PhysicalKeycode = Key.Tab, Pressed = false });
            await Frames(1);
        }
        Require(_stage.Viewport.GuiGetFocusOwner() == target, "keyboard reached " + target.Name);
        GD.Print("FLOW_INPUT_KEYBOARD " + target.Name);
        _stage.Push(new InputEventKey { Keycode = Key.Space, PhysicalKeycode = Key.Space, Pressed = true });
        _stage.Push(new InputEventKey { Keycode = Key.Space, PhysicalKeycode = Key.Space, Pressed = false });
        await Frames(5);
    }
    private async Task Capture(string name)
    {
        await Frames(2);
        using var screenshot = _stage.Viewport.GetTexture().GetImage();
        Require(screenshot.SavePng(ProjectSettings.GlobalizePath("res://outputs/architecture-roots-20261001/" + name)) == Error.Ok, "capture");
    }
    public override void _ExitTree() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
}
