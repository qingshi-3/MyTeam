using System;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.Presentation;
using TowerAutobattler.UI;

public partial class BattleLabAppFlowContractSmoke : Node
{
    public override async void _Ready()
    {
        var exitCode = await RunAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(exitCode);
    }

    private async Task<int> RunAsync()
    {
        GameRoot? root = null;
        try
        {
            GetWindow().Size = new Vector2I(1600, 900);
            var scene = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn") ??
                        throw new InvalidOperationException("GameRoot scene missing");
            root = scene.Instantiate<GameRoot>();
            root.SaveNamespace = "tests/battle_lab_app_flow";
            AddChild(root);
            for (var frame = 0; frame < 600 && root.Content is null; frame++) await Frame();
            Require(root.Content is not null, "application package bootstrap");

            var screens = root.GetNode<AppScreenHost>("Screens");
            Require(screens.MainMenu.Visible, "main menu visible after bootstrap");
            await Click(screens.MainMenu.GetNode<Control>("Center/Panel/Menu/BattleLabButton"));
            Require(screens.BattleLab.Visible && screens.BattleLab.CurrentSnapshot is not null,
                "real main-menu Battle Lab entry");
            var before = screens.BattleLab.CurrentSnapshot!.CanonicalDigest;

            await Click(screens.BattleLab.GetNode<Control>("%StartButton"));
            Require(screens.Battle.Visible && screens.Battle.HasActiveBattle,
                "Battle Lab starts production BattleScreen");
            Require(screens.Battle.GetNode<Control>("%ResetBattleButton").Visible &&
                    screens.Battle.GetNode<Control>("%ReturnConfigurationButton").Visible,
                "Lab battle controls visible");
            screens.Battle.SetPaused(true);
            var guard = 0;
            while (screens.Battle.Outcome == BattleOutcome.Running && guard++ < 4000)
                Require(screens.Battle.StepOneTick(), "fixed-step app-flow progression");
            Require(screens.Battle.TerminalResult is not null, "Lab app-flow terminal result");
            await ToSignal(GetTree().CreateTimer(1.8), SceneTreeTimer.SignalName.Timeout);
            Require(screens.BattleReport.Visible, "Lab terminal reaches production BattleReport");
            await Click(screens.BattleReport.GetNode<Control>("%ReportContinue"));
            Require(screens.BattleLab.Visible && screens.BattleLab.CurrentSnapshot?.CanonicalDigest == before,
                "BattleReport returns to unchanged Lab configuration");
            Require(!screens.Battle.HasActiveBattle, "Lab runtime cleaned before configuration return");

            await Click(screens.BattleLab.GetNode<Control>("%BackButton"));
            Require(screens.MainMenu.Visible && !screens.BattleLab.Visible && !screens.Battle.HasActiveBattle,
                "real Battle Lab exit returns to main menu without retained runtime");
            await Click(screens.MainMenu.GetNode<Control>("Center/Panel/Menu/BattleLabButton"));
            Require(screens.BattleLab.Visible &&
                    screens.BattleLab.CurrentSnapshot?.CanonicalDigest == before,
                "real Battle Lab re-entry restores one clean editable binding");
            await Click(screens.BattleLab.GetNode<Control>("%StartButton"));
            Require(screens.Battle.Visible && screens.Battle.HasActiveBattle,
                "re-entered Battle Lab can start one fresh production battle");
            await Click(screens.Battle.GetNode<Control>("%ReturnConfigurationButton"));
            Require(screens.BattleLab.Visible && !screens.Battle.HasActiveBattle &&
                    screens.BattleLab.CurrentSnapshot?.CanonicalDigest == before,
                "re-entered Battle Lab returns to unchanged configuration without duplicate subscription effects");

            root.QueueFree();
            await Frame(3);
            GD.Print("BATTLE_LAB_APP_FLOW_CONTRACT_OK entry=main-menu host=independent " +
                     "reentry=menu-lab-battle-config battle=production report=production " +
                     "return=unchanged cleanup=runtime-subscriptions");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("BATTLE_LAB_APP_FLOW_CONTRACT_FAILED: " + exception);
            root?.QueueFree();
            return 1;
        }
    }

    private async Task Click(Control control)
    {
        var position = control.GetGlobalRect().GetCenter();
        GetViewport().PushInput(Mouse(position, true));
        await Frame();
        GetViewport().PushInput(Mouse(position, false));
        await Frame(2);
    }

    private static InputEventMouseButton Mouse(Vector2 position, bool pressed) => new()
    {
        Position = position,
        GlobalPosition = position,
        ButtonIndex = MouseButton.Left,
        ButtonMask = pressed ? MouseButtonMask.Left : 0,
        Pressed = pressed
    };

    private async Task Frame(int count = 1)
    {
        for (var index = 0; index < count; index++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
    }
}
