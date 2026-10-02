using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Authored production content with controlled ownership, real isolated GUI input.
public partial class RunRelicShelfInputSmoke : Node
{
    private UiInputStage _stage = null!;
    private const string Output = "res://.godot/run-relic-review";

    public override async void _Ready()
    {
        var code = 0;
        GameRoot? game = null;
        try
        {
            Require(DisplayServer.GetName() != "headless", "rendered input required");
            _stage = new UiInputStage(this, true);
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(Output));
            game = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
            game.SaveNamespace = $"tests/run-relic-ui/{Guid.NewGuid():N}";
            _stage.AddChild(game);
            await Until(() => game.Content is not null, "production content publication");
            var content = game.Content!;
            var compiled = GameProjectCompiler.Compile(game.ProjectDefinition!, content.Graph);
            var package = new CompiledGamePackage(content, compiled.Project!, content.PublicationVersion);
            var (app, _) = RunHealthFixture.New(package);
            typeof(GameRoot).GetField("_app", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, app);
            var run = app.ActiveRun!;
            run.Items.Clear();
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            var army = game.GetNode<ArmyOverviewController>("ArmyOverview");
            var shelf = army.GetNode<RunRelicShelf>("%RunRelicShelf");
            if (OS.GetCmdlineUserArgs().Contains("--header-review-only"))
            {
                foreach (var id in new[] { "item_gilded_contract", "item_war_drum", "item_soul_lantern", "item_last_banner" })
                    run.Items.Add(new ItemInstanceDto { InstanceId = "header-" + id, ContentId = id });
                game.Flow.ShowTower();
                await Frames(6);
                await VerifyHeader(army, screens, app, shelf);
                GD.Print("RUN_HEADER_INPUT_OK baseline safe-margins real-entry-press escape keyboard low-health long-values read-only");
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--docked-pointer-only"))
            {
                AddAllRelics(run, content);
                game.Flow.ShowTower();
                await Frames(6);
                var finalTile = Tiles(shelf).Last();
                finalTile.GrabFocus();
                await WaitSeconds(.4);
                await VerifyDockPointer(finalTile, shelf);
                GD.Print("RUN_RELIC_DOCK_INPUT_OK slow-pointer-bridge reading-panel leave-dismiss");
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--tooltip-copy-only"))
            {
                AddAllRelics(run, content);
                game.Flow.ShowTower();
                await Frames(6);
                var model = ArmyOverviewFactory.Build(RunView.For(run), content, app.Rules);
                foreach (var relic in model.Relics)
                foreach (var kind in Enum.GetValues<BattleCombatEventKind>())
                    Require(!relic.Hint.PlainText.Contains(kind.ToString()), "player copy has no internal event enum: " + relic.ContentId);
                Tiles(shelf).Last().GrabFocus();
                await WaitSeconds(.4);
                await Capture("overflow-focus");
                await VerifyLabContext(game, screens, army);
                GD.Print("RUN_RELIC_SHELF_COPY_OK all-owned-effects-chinese actual-focus lab-battle-hidden");
                return;
            }
            game.Flow.ShowTower();
            await Frames(6);
            Require(shelf.IsVisibleInTree() && shelf.GetNode<Label>("%EmptyRelics").IsVisibleInTree(), "visible empty shelf");
            await Capture("empty-route");

            string[] ids = ["item_gilded_contract", "item_war_drum", "item_soul_lantern", "item_last_banner"];
            foreach (var id in ids) run.Items.Add(new ItemInstanceDto { InstanceId = "review-" + id, ContentId = id });
            run.Items.Add(new ItemInstanceDto { InstanceId = "review-extra", ContentId = ids[0], Stacks = 2 });
            game.Flow.ShowTower();
            await Frames(6);
            Require(Tiles(shelf).Count() == 4 && !shelf.GetNode<Label>("%EmptyRelics").Visible, "all owned relics without equipment");
            var gold = Tiles(shelf).Single(tile => tile.ContentId == ids[0]);
            Require(gold.GetNode<Label>("Stacks").Text == "×3", "duplicate instances merge visually into total stacks");
            var before = JsonSerializer.Serialize(run);
            await Hover(gold);
            var tooltip = Descendants<BattleLabTooltip>(gold).Single(hint => hint.Visible);
            Require(tooltip.GetNode<Label>("%Title").Text == "鎏金军契" &&
                tooltip.GetNode<CombatRichText>("%SingleEntry").Text.Contains("每层额外") &&
                !tooltip.GetNode<CombatRichText>("%SingleEntry").Text.Contains("尚未领取"), "full published effects use owned context");
            Require(tooltip.GetNode<Label>("%Subtitle").Text.Contains("3 层"), "owned stack count in hint");
            Require(tooltip.GetNode<Control>("%TooltipPanel").Theme == game.Theme, "shared Chinese typography and tooltip material");
            CheckHeader(army, screens.Tower, shelf);
            await Capture("route-hover");
            await Click(gold);
            Require(!army.IsOpen && JsonSerializer.Serialize(run) == before, "relic activation is reading only");
            await Move(new Vector2(780, 150));
            await WaitSeconds(.35);
            Require(!tooltip.Visible, "hint exits after pointer leaves");
            gold.GrabFocus();
            await WaitSeconds(.35);
            Require(tooltip.Visible, "keyboard focus opens full hint");
            await PressKey(Key.Escape);
            Require(!tooltip.Visible, "Escape dismisses hint");
            await Click(army.GetNode<Button>("%SummaryButton"));
            Require(army.IsOpen && !shelf.IsVisibleInTree() && !tooltip.Visible, "army modal hides shelf and stale hint");
            await Capture("army-open");
            await PressKey(Key.Escape);
            await Until(() => !army.IsOpen, "army closed");
            Require(shelf.IsVisibleInTree(), "shelf restored after modal");

            var initialOffset = screens.Tower.OffsetTop;
            foreach (var screen in new[] { AppScreenId.Reward, AppScreenId.Recruitment, AppScreenId.Shop, AppScreenId.Event, AppScreenId.Rest })
            {
                screens.Show(screen, RunView.For(run), content, app.Rules);
                await Frames(2);
                Require(shelf.IsVisibleInTree(), "persistent on " + screen);
                CheckHeader(army, screens.Screen(screen), shelf);
            }
            game.Flow.ShowTower();
            await Frames(4);
            Require(screens.Tower.OffsetTop == initialOffset && ReferenceEquals(gold, Tiles(shelf).Single(tile => tile.ContentId == ids[0])),
                "navigation preserves tiles and never accumulates header inset");

            // Overflow uses all real relic definitions; ownership is a controlled fixture.
            AddAllRelics(run, content);
            game.Flow.ShowTower();
            await Frames(6);
            var scroll = shelf.GetNode<ScrollContainer>("%RelicScroll");
            Require(scroll.GetHScrollBar().Visible, "overflow exposes scroll bar");
            var last = Tiles(shelf).Last();
            last.GrabFocus();
            await WaitSeconds(.4);
            Require(scroll.ScrollHorizontal > 0 && scroll.GetGlobalRect().HasPoint(last.GetGlobalRect().GetCenter()), "keyboard reaches offscreen relics");
            await Capture("overflow-focus");
            await VerifyDockPointer(last, shelf);
            await Move(scroll.GetGlobalRect().GetCenter());
            var scrollBefore = scroll.ScrollHorizontal;
            await Wheel(MouseButton.WheelLeft, scroll.GetGlobalRect().GetCenter());
            Require(scroll.ScrollHorizontal < scrollBefore, "real wheel scroll moves the shelf");

            // Real route/deployment/start inputs retain the shelf during an actual battle.
            var combat = Descendants<ChoiceCard>(screens.Tower).First(card => card.StableId is "Combat" or "Elite");
            await Click(combat);
            await Until(() => screens.Deployment.IsVisibleInTree(), "route opens preparation");
            Require(shelf.IsVisibleInTree(), "preparation shelf");
            await Capture("deployment");
            await Click(screens.Deployment.GetNode<Button>("%StartBattleButton"));
            await Until(() => screens.Battle.IsVisibleInTree() && screens.Battle.HasActiveBattle, "real battle start");
            screens.Battle.SetPaused(true);
            Require(shelf.IsVisibleInTree() && !army.GetNode<Button>("%SummaryButton").IsVisibleInTree(), "battle keeps read-only relic shelf");
            CheckHeader(army, screens.Battle, shelf);
            var first = Tiles(shelf).First();
            first.GrabFocus();
            await WaitSeconds(.35);
            Require(Descendants<BattleLabTooltip>(first).Any(hint => hint.Visible), "actual battle relic explanation");
            await Capture("battle-hover");
            screens.Battle.StopBattle();
            await VerifyLabContext(game, screens, army);
            GD.Print("RUN_RELIC_SHELF_INPUT_OK empty owned duplicates full-hover keyboard escape modal restore stable-navigation overflow pointer-bridge wheel actual-battle lab-hidden");
        }
        catch (Exception error) { code = 1; GD.PrintErr("RUN_RELIC_SHELF_INPUT_FAILED: " + error); }
        finally { game?.QueueFree(); await Frames(3); GetTree().Quit(code); }
    }

    private async Task VerifyHeader(ArmyOverviewController army, AppScreenHost screens, RunApplication app, RunRelicShelf shelf)
    {
        var summary = army.GetNode<Button>("%SummaryButton");
        var entry = army.GetNode<Control>("%ArmyEntry");
        var before = JsonSerializer.Serialize(app.ActiveRun);
        var model = ArmyOverviewFactory.Build(app.Run!, app.Content, app.Rules);
        await Move(new Vector2(780, 150));
        CheckHeader(army, screens.Tower, shelf);
        CheckMetricGeometry(army);
        await HeaderCapture("header-normal", summary);
        await Hover(entry);
        await HeaderCapture("header-hover", summary);
        var point = entry.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        await Frames(3);
        Require(!army.IsOpen, "entry waits for actual release");
        await HeaderCapture("header-pressed", summary);
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(5);
        Require(army.IsOpen && !shelf.IsVisibleInTree(), "army entry opens existing modal");
        await PressKey(Key.Escape);
        await Until(() => !army.IsOpen, "army escape closes");
        Require(shelf.IsVisibleInTree(), "entry restores relic shelf");
        summary.GrabFocus();
        await PressKey(Key.Enter);
        Require(army.IsOpen, "keyboard still opens army");
        await PressKey(Key.Escape);
        await Until(() => !army.IsOpen, "keyboard close");
        summary.ReleaseFocus();
        await Move(new Vector2(780, 150));
        // These are reading snapshots only; never rewrite a Run for display QA.
        army.Bind(model with { CurrentRunHealth = 25, Deployed = 12, CurrentPopulation = 18, Reserve = 8, ItemCount = 99, Gold = 9999 });
        await Frames(6);
        CheckMetricGeometry(army);
        var health = army.GetNode<ArmyResourceStrip>("%ResourceStrip").GetNode<RunHealthDisplay>("%RunHealth");
        Require(health.GetNode<SemanticChip>("Health").GetNode<Label>("%SemanticCaption").Text.Contains("危急"), "low health has an explicit text cue");
        Require(health.GetNode<ProgressBar>("Gauge").Value == 25, "health gauge retains its bound value");
        Require(ReferenceEquals(health.GetNode<ProgressBar>("Gauge").GetThemeStylebox("fill"), health.GaugeCriticalFill), "critical health uses its authored warning fill");
        await HeaderCapture("header-critical", summary);
        Require(JsonSerializer.Serialize(app.ActiveRun) == before, "reading snapshots and entry never mutate run");
        army.Bind(model);
        await Frames(4);
        Require(ReferenceEquals(health.GetNode<ProgressBar>("Gauge").GetThemeStylebox("fill"), health.GaugeNormalFill), "normal health restores its authored fill");
        await HeaderCapture("header-final", summary);
    }
    private static void CheckMetricGeometry(ArmyOverviewController army)
    {
        var summary = army.GetNode<Control>("%SummaryButton").GetGlobalRect();
        var strip = army.GetNode<ArmyResourceStrip>("%ResourceStrip");
        var rows = Descendants<SemanticChip>(strip).ToArray();
        var baseline = rows[0].GetNode<Label>("%SemanticText").GetGlobalRect().Position.Y;
        foreach (var chip in rows)
        {
            var value = chip.GetNode<Label>("%SemanticText");
            Require(Mathf.Abs(value.GetGlobalRect().Position.Y - baseline) <= 1, "numeric rows align including health");
            Require(summary.Grow(-4).Encloses(value.GetGlobalRect()), "numeric value stays in header safe margin");
            Require(summary.Grow(-4).Encloses(chip.GetNode<Label>("%SemanticCaption").GetGlobalRect()), "caption stays in header safe margin");
        }
        Require(!strip.GetGlobalRect().Intersects(army.GetNode<Control>("%ArmyEntry").GetGlobalRect()), "resources do not overlap army action");
    }
    private async Task HeaderCapture(string name, Control summary)
    {
        await Capture(name);
        using var image = _stage.Viewport.GetTexture().GetImage();
        var rect = summary.GetGlobalRect().Grow(8);
        using var crop = image.GetRegion(new Rect2I((Vector2I)rect.Position, (Vector2I)rect.Size));
        Require(crop.SavePng(ProjectSettings.GlobalizePath($"{Output}/{name}-crop.png")) == Error.Ok, "actual header crop");
    }
    private async Task VerifyDockPointer(RunRelicIcon last, RunRelicShelf shelf)
    {
        last.ReleaseFocus();
        await Hover(last);
        var reading = Descendants<BattleLabTooltip>(last).Single(hint => hint.Visible);
        var readingRect = reading.GetNode<Control>("%TooltipPanel").GetGlobalRect();
        var departure = new Vector2(last.GetGlobalRect().GetCenter().X, shelf.GetGlobalRect().End.Y + 8);
        await Move(departure);
        var destination = new Vector2(readingRect.End.X - 24, readingRect.Position.Y + 40);
        for (var step = 1; step <= 5; step++)
        {
            await Move(departure.Lerp(destination, step / 5f));
            await WaitSeconds(.12);
            Require(reading.Visible, "slow pointer crossing from end icon preserves docked reading hint");
        }
        await Capture("overflow-pointer");
        await Move(new Vector2(780, 150));
        await WaitSeconds(.35);
        Require(!reading.Visible, "leaving the icon-panel bridge still dismisses hint");
    }
    private static IEnumerable<RunRelicIcon> Tiles(Node shelf) => Descendants<RunRelicIcon>(shelf);
    private async Task VerifyLabContext(GameRoot game, AppScreenHost screens, ArmyOverviewController army)
    {
        game.Flow.ShowBattleLab();
        await Frames(4);
        Require(!army.Visible && !Descendants<BattleLabTooltip>(army).Any(hint => hint.Visible), "lab hides Run relics and hints");
        await Click(screens.BattleLab.GetNode<Button>("%StartButton"));
        await Until(() => screens.Battle.IsVisibleInTree() && screens.Battle.HasActiveBattle, "real lab battle starts");
        screens.Battle.SetPaused(true);
        Require(!army.Visible && screens.Battle.OffsetTop == 0, "lab battle never displays unrelated Run relics or reserves their space");
    }
    private static void AddAllRelics(ActiveRunDto run, ContentRegistry content)
    {
        run.Items.Clear();
        foreach (var entry in content.Catalog.AllEntries().Where(entry => entry.Definition is ItemDefinition { ProductKind: ItemProductKind.Relic }))
            run.Items.Add(new ItemInstanceDto { InstanceId = "review-" + entry.StableId, ContentId = entry.StableId });
    }
    private static void CheckHeader(ArmyOverviewController army, Control page, RunRelicShelf shelf)
    {
        Require(shelf.GetGlobalRect().End.Y <= page.GetGlobalRect().Position.Y, "shelf stays above page controls");
        var summary = army.GetNode<Control>("%SummaryButton");
        Require(!summary.IsVisibleInTree() || !summary.GetGlobalRect().Intersects(shelf.GetGlobalRect()), "shelf and resource strip never overlap");
    }
    private async Task Move(Vector2 point)
    {
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await Frames(2);
    }
    private async Task Hover(Control control) { await Move(control.GetGlobalRect().GetCenter()); await WaitSeconds(.4); }
    private async Task Click(Control control)
    {
        Require(control.IsVisibleInTree(), "visible input target " + control.GetPath());
        var point = control.GetGlobalRect().GetCenter();
        await Move(point);
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(5);
    }
    private async Task Wheel(MouseButton button, Vector2 point)
    {
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = button, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = button, Pressed = false });
        await Frames(4);
    }
    private async Task PressKey(Key key)
    {
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
        await Frames(5);
    }
    private async Task Capture(string name)
    {
        await Frames(2);
        // Native offscreen windows may skip ordinary redraws. Capture a completed
        // engine draw so state reviews never reuse the previous viewport image.
        Callable.From(() => RenderingServer.ForceDraw(false)).CallDeferred();
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Require(_stage.Viewport.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"{Output}/{name}.png")) == Error.Ok, "rendered screenshot");
    }
    private async Task Until(Func<bool> ready, string message)
    {
        for (var i = 0; i < 400 && !ready(); i++) await Frames(1);
        Require(ready(), message);
    }
    private async Task WaitSeconds(double seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    private async Task Frames(int count) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
