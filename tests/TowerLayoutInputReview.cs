using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Growth;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Focused production-page review, with controlled ownership/floor fixtures.
// All hover, press, cancellation and page transitions use isolated GUI input.
public partial class TowerLayoutInputReview : Node
{
    private const string Output = "res://.godot/ui-review/tower-layout";
    private UiInputStage _stage = null!;
    private readonly UiDragMotionRecorder _recording = new();

    public override async void _Ready()
    {
        var exit = 0;
        GrowthGameRoot? game = null;
        try
        {
            Require(DisplayServer.GetName() != "headless", "rendered input required");
            _stage = new UiInputStage(this, true);
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(Output));
            game = GD.Load<PackedScene>("res://scenes/app/GrowthGameRoot.tscn").Instantiate<GrowthGameRoot>();
            game.SaveNamespace = $"tests/tower-layout/{Guid.NewGuid():N}";
            _stage.AddChild(game);
            await Until(() => game.Content is not null, "published production content");
            var app = (RunApplication)typeof(GameRoot).GetField("_app", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(game)!;
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            app.Settings.ReduceUiMotion = false;
            StartRoute(app, TowerNodeType.Event);
            AddRelics(app);
            game.Flow.ShowTower();
            await Frames(6);
            Move(new Vector2(800, 92));
            await Record("idle", .35);
            CheckCards(screens.Tower);
            await Capture("route");
            var route = Route(screens, "Event");
            var before = JsonSerializer.Serialize(app.ActiveRun);
            var rect = route.GetGlobalRect();
            Move(rect.GetCenter());
            await Record("hover", .4);
            Require(route.OffsetTransformScale.X > 1 && route.OffsetTransformScale.X < 1.01f,
                "large card hover remains restrained");
            MouseButton(rect.GetCenter(), true);
            await Record("pressed", .22);
            Require(route.OffsetTransformScale.X < 1, "actual press visibly compresses card");
            Move(new Vector2(800, 92));
            await Record("cancel-drag-out", .2);
            MouseButton(new Vector2(800, 92), false);
            await Record("cancel-release", .4);
            Require(screens.Tower.IsVisibleInTree() && JsonSerializer.Serialize(app.ActiveRun) == before,
                "drag-out cancellation never selects or saves a node");
            Require(route.OffsetTransformScale.IsEqualApprox(Vector2.One)
                && route.GetGlobalRect().IsEqualApprox(rect), "cancellation settles without changing hit geometry");
            app.Settings.ReduceUiMotion = true;
            Move(rect.GetCenter());
            await Frames(4);
            Require(route.OffsetTransformScale.IsEqualApprox(Vector2.One), "reduced motion retains normal geometry");
            app.Settings.ReduceUiMotion = false;
            Move(new Vector2(800, 92));
            await Frames(3);
            route.GrabFocus();
            PressKey(Key.Enter);
            await Until(() => screens.Reward.IsVisibleInTree(), "keyboard route activation opens real event offer");
            await Record("event-enter", .35);
            await Capture("event");
            Require(app.ActiveRun!.PendingNode && app.ActiveRun.SelectedNode == TowerNodeType.Event,
                "one keyboard action selects the event");

            StartRoute(app, TowerNodeType.Shop);
            AddRelics(app);
            game.Flow.ShowTower();
            await Frames(5);
            await Click(Route(screens, "Shop"));
            await Until(() => screens.Shop.IsVisibleInTree(), "actual route opens shop");
            await Capture("shop");
            Require(Descendants<RunOfferChoiceCard>(screens.Shop).Any(card => card.IsVisibleInTree()), "real shop inventory rendered");

            StartRoute(app, TowerNodeType.Elite);
            AddRelics(app);
            game.Flow.ShowTower();
            await Frames(5);
            before = JsonSerializer.Serialize(app.ActiveRun);
            await Click(Route(screens, "Elite"));
            var opponents = screens.Tower.GetNode<OpponentChoicePanel>("OpponentChoicePanel");
            await Until(() => opponents.IsVisibleInTree(), "elite opens real opponent choices");
            await Frames(5);
            foreach (var card in Descendants<ChoiceCard>(opponents).Where(card => card.IsVisibleInTree()))
            foreach (var label in Descendants<Label>(card).Where(label => label.IsVisibleInTree()))
                Require(card.GetGlobalRect().Grow(-6).Encloses(label.GetGlobalRect()), "opponent text fits its reading panel");
            Require(opponents.GetNode<Control>("%EnemyDetails").Size.Y >= 300, "enemy sheet has usable reading height");
            await Capture("elite-opponents");
            await Click(opponents.GetNode<Button>("%BackToRoute"));
            Require(JsonSerializer.Serialize(app.ActiveRun) == before, "return from preview preserves route state");
            CheckCards(screens.Tower);
            await Capture("elite-route");

            app.ActiveRun!.FloorIndex = app.Project.Campaign.FloorsPerRegion - 1;
            game.Flow.ShowTower();
            await Frames(6);
            CheckCards(screens.Tower);
            var boss = Route(screens, "Boss");
            Require(Mathf.Abs(boss.GetGlobalRect().GetCenter().X - 800) < 1,
                "one mandatory boss card stays centered");
            Require(boss.GetNode<Label>("%RouteConsequence").Text.Contains("结束征程"), "boss failure rule is permanently visible");
            await Capture("boss-route");
            GD.Print("TOWER_LAYOUT_INPUT_OK actual-hover press cancel keyboard reduced-motion shop elite-back boss-centered stable-hit-geometry");
        }
        catch (Exception error) { exit = 1; GD.PrintErr("TOWER_LAYOUT_INPUT_FAILED " + error); }
        finally
        {
            _recording.Save("tower-route-motion");
            game?.QueueFree();
            await Frames(3);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Frames(2);
            GetTree().Quit(exit);
        }
    }

    private static void StartRoute(RunApplication app, TowerNodeType target)
    {
        for (ulong seed = 1; seed <= 64; seed++)
        {
            Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], seed), "legitimate test run");
            if (app.CurrentOptions().Any(option => option.Type == target)) return;
        }
        throw new InvalidOperationException("offered node fixture: " + target);
    }
    private static void AddRelics(RunApplication app)
    {
        foreach (var id in new[] { "item_gilded_contract", "item_war_drum", "item_soul_lantern", "item_last_banner" })
            app.ActiveRun!.Items.Add(new ItemInstanceDto { InstanceId = "layout-" + id, ContentId = id });
    }
    private static TowerRouteCard Route(AppScreenHost screens, string id) =>
        Descendants<TowerRouteCard>(screens.Tower).First(card => card.IsVisibleInTree() && !card.IsQueuedForDeletion() && card.StableId == id);
    private static void CheckCards(Control page)
    {
        var area = page.GetGlobalRect();
        foreach (var card in Descendants<TowerRouteCard>(page).Where(card => card.IsVisibleInTree() && !card.IsQueuedForDeletion()))
        {
            Require(area.Encloses(card.GetGlobalRect()), "route card within page");
            Require(Mathf.Abs(card.Size.X / card.Size.Y - 952f / 1376f) < .002,
                "approved irregular frame keeps its aspect ratio");
            foreach (var label in Descendants<Label>(card).Where(label => label.IsVisibleInTree()))
                Require(card.GetGlobalRect().Grow(-18).Encloses(label.GetGlobalRect()), "all route text remains inside safe stone edge");
        }
    }
    private void Move(Vector2 point) => _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
    private void MouseButton(Vector2 point, bool pressed) => _stage.Push(new InputEventMouseButton
        { Position = point, GlobalPosition = point, ButtonIndex = Godot.MouseButton.Left, ButtonMask = pressed ? MouseButtonMask.Left : 0, Pressed = pressed });
    private void PressKey(Key code)
    {
        _stage.Push(new InputEventKey { Keycode = code, PhysicalKeycode = code, Pressed = true });
        _stage.Push(new InputEventKey { Keycode = code, PhysicalKeycode = code, Pressed = false });
    }
    private async Task Click(Control control)
    {
        var point = control.GetGlobalRect().GetCenter();
        Move(point); await Frames(3); MouseButton(point, true); await Frames(2); MouseButton(point, false); await Frames(5);
    }
    private async Task Record(string phase, double seconds)
    {
        var end = Time.GetTicksMsec() + (ulong)(seconds * 1000);
        do { await Frames(1); _recording.Sample(_stage.Viewport, phase); } while (Time.GetTicksMsec() < end);
    }
    private async Task Capture(string name)
    {
        await Record("settle", .3);
        using var image = _stage.Viewport.GetTexture().GetImage();
        Require(image.SavePng(ProjectSettings.GlobalizePath($"{Output}/{name}.png")) == Error.Ok, "rendered capture");
    }
    private async Task Until(Func<bool> ready, string reason)
    {
        for (var i = 0; i < 500 && !ready(); i++) await Frames(1);
        Require(ready(), reason);
    }
    private async Task Frames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Callable.From(() => RenderingServer.ForceDraw(false)).CallDeferred();
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T item) yield return item;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
