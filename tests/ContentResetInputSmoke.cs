using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Presentation;

public partial class ContentResetInputSmoke : Node
{
    private UiInputStage _stage = null!;
    private const string Output = "res://outputs/content-reset-20261004";

    public override async void _Ready()
    {
        try
        {
            _stage = new UiInputStage(this, isolated: true);
            var game = GD.Load<PackedScene>("res://scenes/app/ContentRebuildRoot.tscn").Instantiate<ContentRebuildRoot>();
            _stage.AddChild(game);
            await Until(() => game.ContentReady, "retained content ready");
            await Draw();
            Require(game.GetNode<Button>("Menu/Panel/Margin/Rows/NewRun").Disabled &&
                game.GetNode<Button>("Menu/Panel/Margin/Rows/Continue").Disabled, "no unfinished campaign offered");
            foreach (var name in new[] { "Punch", "Charge", "Fire", "Swap", "Vfx", "Environment", "Quit" })
                Require(_stage.Viewport.GetVisibleRect().Encloses(game.GetNode<Button>("%" + name).GetGlobalRect()), "visible button " + name);
            await Capture("menu");
            var battle = game.GetNode<BattleScreenController>("Battle");
            foreach (var name in new[] { "Punch", "Charge", "Fire", "Swap" })
            {
                await Click(game.GetNode<Button>("%" + name));
                await Until(() => battle.TickIndex >= 45, "battle advances: " + name);
                Require(battle.HasActiveBattle && battle.LastRuntimeFailure.Length == 0, "real battle runs: " + name);
                await Capture(name);
                await Click(battle.GetNode<Button>("%PauseButton"));
                Require(battle.IsPaused, "pause input");
                var tick = battle.TickIndex;
                await ToSignal(GetTree().CreateTimer(.15), SceneTreeTimer.SignalName.Timeout);
                Require(battle.TickIndex == tick, "paused simulation");
                await Click(battle.GetNode<Button>("%ResetBattleButton"));
                Require(battle.TickIndex < tick && !battle.IsPaused, "reset input rebuilds battle");
                await Click(battle.GetNode<Button>("%ReturnConfigurationButton"));
                Require(game.GetNode<Control>("Menu").IsVisibleInTree() && !battle.HasActiveBattle, "return cleans battle");
                GD.Print("CONTENT_RESET_INPUT_SAMPLE_OK " + name);
            }
            // Keyboard activation goes through the ordinary GUI focus pipeline.
            game.GetNode<Button>("%Charge").GrabFocus();
            _stage.Push(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = true });
            await Draw();
            _stage.Push(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = false });
            await Until(() => battle.HasActiveBattle, "keyboard activation");
            await Click(battle.GetNode<Button>("%ReturnConfigurationButton"));
            game.QueueFree();
            await Draw();
            GD.Print("CONTENT_RESET_INPUT_OK 960x540 isolated pointer keyboard four-battles pause reset return");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("CONTENT_RESET_INPUT_FAILED " + error); GetTree().Quit(1); }
    }

    private async Task Draw()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
    }

    private async Task Until(Func<bool> condition, string label)
    {
        var deadline = Time.GetTicksMsec() + 15000;
        while (!condition())
        {
            if (Time.GetTicksMsec() > deadline) throw new InvalidOperationException("Timeout: " + label);
            await Draw();
        }
    }

    private async Task Click(Button button)
    {
        await Draw();
        var point = button.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        await Draw();
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = MouseButton.Left, Pressed = false });
        await Draw();
    }

    private async Task Capture(string name)
    {
        await Draw();
        Directory.CreateDirectory(ProjectSettings.GlobalizePath(Output));
        Require(_stage.Viewport.GetTexture().GetImage().SavePng(Output + "/" + name + ".png") == Error.Ok, "capture");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
