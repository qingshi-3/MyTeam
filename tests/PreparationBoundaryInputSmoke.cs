using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Real default-entry scenes and GUI input, isolated at 960x540. No desktop pointer operations.
public partial class PreparationBoundaryInputSmoke : Node
{
    private UiInputStage _stage = null!;
    private string _directory = "";
    public override async void _Ready()
    {
        var exit = 0;
        GameRoot? game = null;
        try
        {
            Require(DisplayServer.GetName() != "headless", "rendered input required");
            var space = "tests/preparation-boundary/" + Guid.NewGuid().ToString("N");
            _directory = ProjectSettings.GlobalizePath("user://" + space);
            _stage = new UiInputStage(this, true);
            _stage.Resize(new(960, 540));
            game = GD.Load<PackedScene>(ProjectSettings.GetSetting("application/run/main_scene").AsString()).Instantiate<GameRoot>();
            game.SaveNamespace = space;
            _stage.AddChild(game);
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            await Until(() => game.Content is not null && screens.MainMenu.IsVisibleInTree(), "ready menu");
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
            await Until(() => screens.HeroSelection.IsVisibleInTree(), "opening");
            var tiles = Descendants<HeroLibraryTile>(screens.HeroSelection).Where(t => t.IsVisibleInTree()).ToArray();
            Require(tiles.Length == 6, "six actual candidates");
            await Click(tiles[0]);
            await Click(tiles[1]);
            await Click(screens.HeroSelection.GetNode<Button>("%ConfirmOpening"));
            await Until(() => screens.Deployment.IsVisibleInTree() || screens.Tower.IsVisibleInTree(), "confirmed opening route");
            if (screens.Tower.IsVisibleInTree())
            {
                var combat = Descendants<TowerRouteCard>(screens.Tower).First(card => card.StableId is "Combat" or "Elite" or "Boss");
                // Default Growth starts at route selection; Alpha starts in preparation.
                // Traverse actual GUI focus, then use the regular accept action.
                for (var i = 0; i < 32 && _stage.Viewport.GuiGetFocusOwner() != combat; i++) await PressKey(Godot.Key.Tab);
                Require(_stage.Viewport.GuiGetFocusOwner() == combat, "keyboard reached combat route");
                await PressKey(Godot.Key.Space);
                var opponents = screens.Tower.GetNode<OpponentChoicePanel>("OpponentChoicePanel");
                if (opponents.IsVisibleInTree())
                {
                    var confirmOpponent = opponents.GetNode<Button>("%ConfirmOpponent");
                    for (var i = 0; i < 32 && _stage.Viewport.GuiGetFocusOwner() != confirmOpponent; i++) await PressKey(Godot.Key.Tab);
                    Require(_stage.Viewport.GuiGetFocusOwner() == confirmOpponent && !confirmOpponent.Disabled,
                        "keyboard reached selected opponent confirmation");
                    await PressKey(Godot.Key.Space);
                }
            }
            await Until(() => screens.Deployment.IsVisibleInTree(), "confirmed preparation");
            var file = Path.Combine(_directory, "active_run.json");
            var before = File.ReadAllText(file);
            var saved = new SaveService(space).LoadActiveRun()!;
            Require(saved.Roster.Count == 2 && saved.OpeningRecruitment is null, "selection persisted");
            await Capture("preparation-default.png");
            await Click(screens.Deployment.GetNode<Button>("%EquipmentButton"));
            var popup = screens.Deployment.GetNode<ContextPopup>("%EquipmentPopup");
            await Until(() => popup.IsOpen, "open roster manager");
            var view = screens.Deployment.GetNode<RosterLoadoutView>("%EquipmentLoadoutPanel");
            var selectors = Descendants<RosterHeroSelector>(view).ToArray();
            Require(selectors.Length == 2, "read view rendered both heroes");
            var target = selectors.First(s => s.HeroId != view.SelectedHeroId);
            var targetId = target.HeroId;
            await Click(target);
            Require(view.SelectedHeroId == targetId && view.GetNode<RosterHeroCard>("%SelectedCard").HeroId == targetId,
                "real selector refreshes prepared details");
            await Capture("preparation-roster.png");
            await PressKey(Godot.Key.Escape);
            Require(!popup.IsOpen, "escape closes actual manager");
            Require(File.ReadAllText(file) == before, "inspection changed persistent state");
            // Reopen to exercise cached live Run wrappers plus rebuilt frozen preparation.
            await Click(screens.Deployment.GetNode<Button>("%EquipmentButton"));
            Require(popup.IsOpen && view.GetNode<RosterHeroCard>("%SelectedCard").HeroId.Length > 0,
                "reopen read binding");
            await PressKey(Godot.Key.Escape);
            GD.Print("PREPARATION_BOUNDARY_INPUT_OK viewport=960x540 default-entry opening-confirm deployment roster-switch reopen readonly-save desktop-pointer=unchanged");
        }
        catch (Exception e) { GD.PrintErr("PREPARATION_BOUNDARY_INPUT_FAILED " + e); exit = 1; if (_stage is not null) await Capture("preparation-input-failure.png"); }
        finally { game?.QueueFree(); }
        await Frames(3);
        GetTree().Quit(exit);
    }
    public override void _ExitTree() { if (_directory.Length > 0 && Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private async Task Until(Func<bool> condition, string message)
    {
        for (var i = 0; i < 180 && !condition(); i++) await Frames(1);
        Require(condition(), message); await Frames(3);
    }
    private async Task Frames(int count)
    {
        for (var i = 0; i < count; i++) { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); RenderingServer.ForceDraw(false); }
    }
    private async Task Click(Control target)
    {
        var point = target.GetGlobalRect().GetCenter();
        Require(target.IsVisibleInTree() && _stage.Viewport.GetVisibleRect().HasPoint(point), "visible click: " + target.GetPath());
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point }); await Frames(1);
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(5);
    }
    private async Task PressKey(Godot.Key key)
    {
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
        await Frames(8);
    }
    private async Task Capture(string name)
    {
        await Frames(2);
        var directory = ProjectSettings.GlobalizePath("res://outputs/architecture-boundaries-20261001");
        Directory.CreateDirectory(directory);
        using var screenshot = _stage.Viewport.GetTexture().GetImage();
        Require(screenshot.SavePng(Path.Combine(directory, name)) == Error.Ok, "capture input evidence");
    }
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren()) { if (child is T item) yield return item; foreach (var descendant in Descendants<T>(child)) yield return descendant; }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
