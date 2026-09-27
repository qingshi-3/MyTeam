using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;
using TowerAutobattler.UI;

public partial class EnemyDifficultyUiInputSmoke : Node
{
    private UiInputStage _stage = null!;
    public override async void _Ready()
    {
        GameRoot? game = null;
        var exit = 0;
        try
        {
            Require(DisplayServer.GetName() != "headless", "rendered driver required");
            _stage = new UiInputStage(this, true);
            GetWindow().Position = new Vector2I(-4000, -4000);
            game = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
            game.SaveNamespace = $"tests/difficulty-ui/{Guid.NewGuid():N}";
            _stage.AddChild(game);
            for (var i = 0; i < 300 && game.Content is null; i++) await Frames(1);
            Require(game.Content is not null, "production bootstrap");
            var project = GameProjectCompiler.Compile(game.ProjectDefinition!, game.Content!.Graph).Project!;
            var package = new CompiledGamePackage(game.Content, project, game.Content.PublicationVersion);
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            foreach (var floor in new[] { 8, 14 })
            {
                var (app, _) = RunHealthFixture.New(package);
                RunHealthFixture.BattleNode(app, floor == 14 ? TowerNodeType.Boss : TowerNodeType.Elite);
                app.ActiveRun!.FloorIndex = floor;
                typeof(GameRoot).GetField("_app", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, app);
                game.Flow.ResetPendingBattleFlow();
                var encounter = app.CurrentEncounter();
                game.Flow.SetEncounterForTesting(encounter);
                game.Flow.ShowDeployment();
                await Frames(8);
                Require(screens.Deployment.GetNode<Label>("%Title").Text == encounter.Title && encounter.Title.Contains('·'), "composition visible in title");
                await Click(screens.Deployment.GetNode<Button>("%EncounterButton"));
                Require(screens.Deployment.GetNode<Control>("%EncounterPopup").Visible &&
                    screens.Deployment.GetNode<Label>("%EncounterInfo").Text.Length > 0, "encounter click reveals enemy and terrain preview");
                await Capture($"floor-{floor + 1}-preview");
                await Click(screens.Deployment.GetNode<Button>("%EncounterButton"));
                screens.Deployment.GetNode<Button>("%StartBattleButton").GrabFocus();
                _stage.Push(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = true });
                _stage.Push(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = false });
                await Frames(5);
                Require(screens.Battle.HasActiveBattle && screens.Battle.IsVisibleInTree(), "keyboard launches same encounter");
                await Click(screens.Battle.GetNode<Button>("%PauseButton"));
                Require(screens.Battle.GetNode<Label>("%BattleTitle").Text.Contains(encounter.Title), "battle retains composition title");
                await Capture($"floor-{floor + 1}-battle");
                screens.Battle.StopBattle();
            }
            GD.Print("ENEMY_DIFFICULTY_UI_INPUT_OK encounter-preview keyboard-start mouse-pause mid-elite final-boss");
        }
        catch (Exception error) { exit = 1; GD.PrintErr("ENEMY_DIFFICULTY_UI_INPUT_FAILED " + error); }
        finally { game?.QueueFree(); await Frames(3); }
        GetTree().Quit(exit);
    }
    private async Task Click(Control control)
    {
        Require(control.IsVisibleInTree(), "click target visible");
        var point = control.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await Frames(2);
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(5);
    }
    private async Task Capture(string name)
    {
        const string output = "res://.godot/difficulty-review";
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(output));
        await Frames(3);
        Require(_stage.Viewport.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"{output}/{name}.png")) == Error.Ok, "rendered capture");
    }
    private async Task Frames(int count) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
