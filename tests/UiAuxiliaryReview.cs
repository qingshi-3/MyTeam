using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Experience;
using TowerAutobattler.UI;
using TowerAutobattler.Vfx;

// Companion to UiClarityReview: authored standalone tools, not a simulated full run.
public partial class UiAuxiliaryReview : Node
{
    private UiInputStage _stage = null!;
    public override async void _Ready()
    {
        var exit = 0;
        Control? current = null;
        try
        {
            Require(DisplayServer.GetName() != "headless", "rendered review required");
            _stage = new UiInputStage(this, true);
            var experience = GD.Load<PackedScene>("res://scenes/app/ExperienceSlice.tscn").Instantiate<ExperienceSliceRoot>();
            current = experience;
            _stage.AddChild(experience);
            for (var i = 0; i < 600 && experience.Session is null && experience.BootstrapFailure.Length == 0; i++) await Frames(1);
            Require(experience.Session is not null, "production practice initialization: " + experience.BootstrapFailure);
            var panel = experience.GetNode<ExperienceSlicePanel>("Screens/ExperiencePanel");
            await Capture("practice-relic");
            await Click(panel.GetNode<Container>("%ItemChoices").GetChildren().OfType<ChoiceCard>().First());
            await Click(panel.GetNode<Control>("%Primary"));
            Require(experience.Session!.Stage == ExperienceStage.Recruit, "real relic confirmation reaches recruit");
            await Capture("practice-recruit");
            var recruit = panel.GetNode<Container>("%RecruitChoices").GetChildren().OfType<UnitChoiceCard>()
                .FirstOrDefault(c => !c.IsQueuedForDeletion() && !c.Disabled);
            if (recruit is not null)
            {
                await Click(recruit);
                await Click(panel.GetNode<Control>("%Primary"));
            }
            else
            {
                Require(panel.GetNode<Label>("%Hint").Text.Contains("均已在队伍"), "owned candidates explain why they cannot be recruited again");
                await Click(panel.GetNode<Control>("%Skip"));
            }
            Require(experience.Session.Stage == ExperienceStage.PrepareFirst, "real recruitment choice or explicit skip reaches preparation");
            await Capture("practice-prepare");
            experience.QueueFree();
            current = null;
            await Frames(4);

            var preview = GD.Load<PackedScene>("res://scenes/app/VfxPreview.tscn").Instantiate<VfxPreviewController>();
            current = preview;
            _stage.AddChild(preview);
            await Frames(6);
            await Click(preview.GetNode<Control>("%Pause"));
            await Capture("vfx-preview");
            await Click(preview.GetNode<Control>("%Step"));
            await Click(preview.GetNode<Control>("%Replay"));
            GD.Print("UI_AUXILIARY_REVIEW_OK practice=relic,recruit,prepare vfx=paused,step,replay input=isolated save=memory-only");
        }
        catch (Exception error) { exit = 1; GD.PrintErr("UI_AUXILIARY_REVIEW_FAILED " + error); }
        finally { current?.QueueFree(); await Frames(3); }
        GetTree().Quit(exit);
    }

    private async Task Click(Control control)
    {
        var point = control.GetGlobalRect().GetCenter();
        Require(control.IsVisibleInTree() && _stage.Viewport.GetVisibleRect().HasPoint(point), "visible input target " + control.GetPath());
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(4);
    }
    private async Task Capture(string name)
    {
        var end = Time.GetTicksMsec() + 320;
        while (Time.GetTicksMsec() < end) await Frames(1);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        const string output = "res://.godot/ui-review/polish-after";
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(output));
        using var image = _stage.Viewport.GetTexture().GetImage();
        Require(image.SavePng(ProjectSettings.GlobalizePath($"{output}/{name}.png")) == Error.Ok, "capture " + name);
    }
    private async Task Frames(int count) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
