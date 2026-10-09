using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Experience;
using TowerAutobattler.Presentation;
using TowerAutobattler.UI;

public partial class ExperienceSliceInputSmoke : Node
{
    public override async void _Ready()
    {
        ExperienceSliceRoot? root = null;
        var exit = 0;
        try
        {
            GetWindow().Size = new Vector2I(1600, 900);
            root = GD.Load<PackedScene>("res://tests/fixtures/legacy-roster/scenes/app/ExperienceSlice.tscn").Instantiate<ExperienceSliceRoot>();
            AddChild(root);
            for (var frame = 0; frame < 600 && root.Session is null && string.IsNullOrEmpty(root.BootstrapFailure); frame++) await Frame();
            var session = root.Session ?? throw new InvalidOperationException(root.BootstrapFailure);
            var panel = root.GetNode<ExperienceSlicePanel>("Screens/ExperiencePanel");
            var deployment = root.GetNode<DeploymentScreenController>("Screens/DeploymentScreen");
            var battle = root.GetNode<BattleScreenController>("Screens/BattleScreen");
            var report = root.GetNode<BattleReportScreen>("Screens/BattleReportScreen");
            await Frame(4);
            Require(panel.Visible, "default experience screen");
            await Click(panel.GetNode<Container>("%ItemChoices").GetChildren().OfType<ChoiceCard>().First());
            Require(panel.GetNode<Label>("%Preview").Text.Contains("→"), "actual beneficiary preview");
            await Capture("experience-relic.png");
            await Click(panel.GetNode<Control>("%Primary"));
            Require(session.Stage == ExperienceStage.Recruit, "relic confirmation progresses once");
            var candidates = panel.GetNode<Container>("%RecruitChoices").GetChildren().OfType<Control>().Where(c => !c.IsQueuedForDeletion()).ToArray();
            await Click(candidates[1]);
            await Click(panel.GetNode<Control>("%Primary"));
            Require(session.Stage == ExperienceStage.PrepareFirst && session.Run.Roster.Count == 5, "recruit real click");
            await Click(panel.GetNode<Control>("%Primary"));
            Require(deployment.Visible, "shared deployment opens");
            // Exercise existing select/cell input paths in the focused deployment tests;
            // here use the default four-person formation and retain the recruit in reserve.
            await Click(deployment.GetNode<Control>("%StartBattleButton"));
            Require(battle.Visible && battle.HasActiveBattle, "actual battle entry");
            await FinishBattle(battle, report);
            Require(session.Stage == ExperienceStage.ReportFirst && session.LastResult?.Outcome == BattleOutcome.PlayerVictory, "real first outcome and report");
            await Click(report.GetNode<Control>("%ReportContinue"));
            Require(panel.Visible && session.Stage == ExperienceStage.Equipment, "battle report leads to equipment");
            await Click(panel.GetNode<Container>("%ItemChoices").GetChildren().OfType<ChoiceCard>().Last(c => !c.IsQueuedForDeletion()));
            await Click(panel.GetNode<Control>("%Primary"));
            Require(session.Stage == ExperienceStage.PrepareSecond, "equipment earned once");
            // Native OptionButton receives keyboard input, not a direct selection signal.
            var owner = panel.GetNode<OptionButton>("%OwnerPicker");
            owner.GrabFocus();
            await PressKey(Key.Space); await PressKey(Key.Down); await PressKey(Key.Enter);
            await Click(panel.GetNode<Control>("%Equip"));
            Require(session.Run.Roster.SelectMany(h => h.Equipment).Any(e => e.InstanceId == "slice-reward-1"), "real equipment assignment");
            await Capture("experience-equipment.png");
            await Click(panel.GetNode<Control>("%Unequip"));
            Require(!session.Run.Roster.SelectMany(h => h.Equipment).Any(e => e.InstanceId == "slice-reward-1") && session.Inventory.Count == 2, "return to bag keeps owned item");
            await Click(panel.GetNode<Control>("%Equip"));
            await Click(panel.GetNode<Control>("%Primary"));
            await Click(deployment.GetNode<Control>("%StartBattleButton"));
            await FinishBattle(battle, report);
            await Click(report.GetNode<Control>("%ReportContinue"));
            Require(panel.Visible && session.Stage == ExperienceStage.Complete, "second battle reaches review");
            await Click(panel.GetNode<Control>("%RetryAfter"));
            Require(session.Stage == ExperienceStage.Equipment && session.Results.Count == 1 && session.History.Count == 1, "real checkpoint retry");
            await Click(panel.GetNode<Control>("%Retry"));
            Require(session.Stage == ExperienceStage.Relic && session.Run.Items.Count == 0 && session.History.Count == 2, "real start retry");
            await Click(panel.GetNode<Control>("%Exit"));
            var dialog = root.GetNode<ConfirmationDialog>("ExitDialog");
            Require(dialog.Visible, "exit warns memory-only lifetime");
            await PressKey(Key.Escape);
            Require(!dialog.Visible && panel.Visible, "cancel exit preserves session");
            root.QueueFree(); await Frame(3); root = null;
            GD.Print("EXPERIENCE_SLICE_INPUT_OK pointer=choice-confirm-battle-report-equipment keyboard=owner-picker exit=cancel retry=both screenshot=rendered-if-available");
        }
        catch (Exception exception) { exit = 1; GD.PrintErr("EXPERIENCE_SLICE_INPUT_FAILED: " + exception); }
        finally { if (root is not null) { root.QueueFree(); await Frame(3); } }
        GetTree().Quit(exit);
    }

    private async Task FinishBattle(BattleScreenController battle, BattleReportScreen report)
    {
        // This accelerates simulation only; navigation/choices still use real GUI input.
        battle.SetPaused(true);
        var steps = 0;
        while (battle.Outcome == BattleOutcome.Running && steps++ < 4000) battle.StepOneTick();
        Require(battle.Outcome != BattleOutcome.Running, "bounded battle finishes");
        await ToSignal(GetTree().CreateTimer(1.9), SceneTreeTimer.SignalName.Timeout);
        Require(report.Visible && !battle.HasActiveBattle, "report navigation cleans runtime");
    }
    private async Task Click(Control control)
    {
        Require(control.IsVisibleInTree(), "click target visible " + control.Name);
        var pos = control.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseButton { Position = pos, GlobalPosition = pos, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        await Frame();
        GetViewport().PushInput(new InputEventMouseButton { Position = pos, GlobalPosition = pos, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frame(3);
    }
    private async Task PressKey(Godot.Key key)
    {
        GetViewport().PushInput(new InputEventKey { Keycode = key, Pressed = true }); await Frame();
        GetViewport().PushInput(new InputEventKey { Keycode = key, Pressed = false }); await Frame(2);
    }
    private async Task Capture(string name)
    {
        if (DisplayServer.GetName() == "headless") return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var image = GetViewport().GetTexture().GetImage();
        Require(image is not null && !image.IsEmpty(), "rendered screenshot");
        var path = ProjectSettings.GlobalizePath("res://.godot/qa");
        DirAccess.MakeDirRecursiveAbsolute(path);
        Require(image!.SavePng(path + "/" + name) == Error.Ok, "capture saved");
        image.Dispose();
    }
    private async Task Frame(int count = 1) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
}
