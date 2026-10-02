using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.Traits;
using TowerAutobattler.UI;

// Production Growth UI/content. Controlled ownership lives only in an in-memory run.
public partial class RunTraitPanelInputSmoke : Node
{
    private UiInputStage _stage = null!;
    private readonly UiDragMotionRecorder _motion = new();
    private const string Output = "res://.godot/ui-review/trait-tracker";
    private string _step = "boot";

    public override async void _Ready()
    {
        var code = 0;
        GameRoot? game = null;
        try
        {
            Require(DisplayServer.GetName() != "headless", "rendered isolated input required");
            _stage = new UiInputStage(this, true);
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(Output));
            game = GD.Load<PackedScene>("res://scenes/app/GrowthGameRoot.tscn").Instantiate<GameRoot>();
            game.SaveNamespace = $"tests/trait-tracker/{Guid.NewGuid():N}";
            _stage.AddChild(game);
            await Until(() => game.Content is not null, "production publication");
            var content = game.Content!;
            var compiled = GameProjectCompiler.Compile(game.ProjectDefinition!, content.Graph);
            var save = new RunHealthMemorySave();
            var app = new RunApplication(content, save, compiled.Project!, growthRules: GrowthContentPackage.LoadRules(content));
            Require(app.BeginOpeningRecruitment(2) && app.ToggleOpeningHero("hero_mx25") &&
                app.ToggleOpeningHero("hero_mx21") && app.ConfirmOpeningRecruitment(), "legitimate initial run");
            typeof(GameRoot).GetField("_app", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(game, app);
            var run = app.ActiveRun!;
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            var army = game.GetNode<ArmyOverviewController>("ArmyOverview");
            var panel = army.GetNode<RunTraitPanel>("%RunTraitPanel");
            Require(!panel.IsVisibleInTree(), "main menu has no unrelated run traits");

            _step = "empty";
            run.Roster.Clear(); run.Deployment = ActiveRunFormationSchema.EmptyDeployment();
            game.Flow.ShowTower(); await Frames(6);
            Require(panel.IsVisibleInTree() && panel.GetNode<Label>("%EmptyTraits").Visible, "empty run explanation");
            await Capture("empty");
            CheckEquipmentMembership(run, content);

            _step = "owned lineup";
            var heroes = content.Catalog.AllEntries().Where(entry => entry.Definition is UnitDefinition { IsHero: true }).ToArray();
            var frost = heroes.Where(entry => content.Graph.ResolveUnitTraitContributions(entry.StableId)
                .Any(trait => trait.TraitId == "mx_frost")).Take(4).ToArray();
            Require(frost.Length == 4, "four published frost members");
            var other = heroes.Where(entry => entry.StableId.StartsWith("hero_mx", StringComparison.Ordinal) &&
                !content.Graph.ResolveUnitTraitContributions(entry.StableId).Any(trait => trait.TraitId == "mx_frost"))
                .OrderBy(entry => entry.StableId).Take(4).ToArray();
            foreach (var entry in frost.Concat(other))
                run.Roster.Add(new RosterHeroInstanceDto { InstanceId = "trait-" + entry.StableId, ContentId = entry.StableId });
            run.CurrentPopulation = 8;
            var onBoard = run.Roster.Take(2).Concat(run.Roster.Skip(4)).ToArray();
            for (var index = 0; index < onBoard.Length; index++) run.Deployment[index] = onBoard[index].InstanceId;
            run.EquipmentInventory.Add(new EquipmentInstanceState { InstanceId = "ui-badge", ContentId = "equipment_ne10_winter_badge", SlotIndex = -1 });
            game.Flow.ShowTower(); await Frames(6);
            CheckCounts(run, content, panel);
            var row = Rows(panel).Single(row => row.Model!.TraitId == "mx_frost");
            Require(row.Model!.Value == 2 && row.Model.ActiveIndex == 0 && row.Model.NextThreshold == 4,
                "owned reserves excluded from active tier");
            Require(row.Model.Hint.PlainText.Contains("后备成员") && row.Model.Hint.PlainText.Contains("未计入"), "reserve membership explained");
            await Capture("route");
            _step = "hover and keyboard";
            var before = JsonSerializer.Serialize(run); var stored = save.Json;
            await Reveal(row); await Move(row.GetGlobalRect().GetCenter()); await Wait(.4);
            var hint = Descendants<BattleLabTooltip>(row).Single(tooltip => tooltip.Visible);
            Require(hint.GetNode<Label>("%Title").Text == "霜羽" && hint.GetNode<CombatRichText>("%SingleEntry").Text.Contains("当前生效"), "published effect tooltip");
            var dock = hint.GetNode<Control>("%TooltipPanel").GetGlobalRect();
            Require(_stage.Viewport.GetVisibleRect().Encloses(dock), "full reading panel within viewport");
            await Capture("hover");
            await Move(new Vector2(row.GetGlobalRect().End.X + 6, row.GetGlobalRect().GetCenter().Y)); await Wait(.35);
            Require(hint.Visible, "pointer bridge keeps reading panel open");
            await Move(dock.GetCenter()); await Wait(.3); Require(hint.Visible, "pointer can enter full rules");
            await PressKey(Key.Escape); Require(!hint.Visible, "escape dismisses explanation");
            row.GrabFocus(); await Wait(.35); Require(hint.Visible, "keyboard focus opens explanation");
            await PressKey(Key.Escape); Require(!hint.Visible, "keyboard escape dismisses");
            row.ReleaseFocus(); await Click(row);
            Require(JsonSerializer.Serialize(run) == before && save.Json == stored, "trait reading never changes run or save");
            await Move(new Vector2(1200, 800)); await Wait(.35);
            await Click(army.GetNode<Button>("%SummaryButton"));
            Require(army.IsOpen && !panel.IsVisibleInTree() && !hint.Visible, "army inspection hides sidebar and stale popup");
            await PressKey(Key.Escape); await Until(() => !army.IsOpen, "army closes");
            Require(panel.IsVisibleInTree(), "sidebar restores after modal");

            _step = "navigation";
            var inset = screens.Tower.OffsetLeft;
            foreach (var id in new[] { AppScreenId.Reward, AppScreenId.Recruitment, AppScreenId.Shop, AppScreenId.Event, AppScreenId.Rest })
            {
                screens.Show(id, RunView.For(run), content, app.Rules); await Frames(2);
                Require(panel.IsVisibleInTree() && screens.Screen(id).OffsetLeft >= panel.GetGlobalRect().End.X, "persistent sidebar on " + id);
            }
            game.Flow.ShowTower(); await Frames(4);
            Require(screens.Tower.OffsetLeft == inset && ReferenceEquals(row, Rows(panel).Single(tile => tile.Model!.TraitId == "mx_frost")), "stable navigation and tile identity");

            _step = "actual formation";
            await Click(Descendants<ChoiceCard>(screens.Tower).First(card => card.StableId == "Combat"));
            await Until(() => screens.Deployment.IsVisibleInTree(), "real route to preparation");
            var deployment = screens.Deployment;
            Require(panel.IsVisibleInTree(), "visible in actual preparation");
            _step = "actual equipment";
            await Click(deployment.GetNode<Button>("%EquipmentButton"));
            var equipment = deployment.GetNode<RosterLoadoutView>("%EquipmentLoadoutPanel");
            var badge = Descendants<EquipmentSlotButton>(equipment.GetNode<Control>("%RosterInventory")).Single(tile => tile.InstanceId == "ui-badge");
            var selectedCard = equipment.GetNode<RosterHeroCard>("%SelectedCard");
            await Drag(badge, selectedCard.Slot(0).GetGlobalRect().GetCenter());
            Require(Rows(panel).Single(tile => tile.Model!.TraitId == "trait_ne10_winter").Model!.Value == 1,
                "local native equipment transaction refreshes run trait count");
            await Drag(selectedCard.Slot(0), equipment.GetNode<Control>("%RosterReturnZone").GetGlobalRect().GetCenter());
            Require(!Rows(panel).Any(tile => tile.Model!.TraitId == "trait_ne10_winter"), "unequipping removes equipment-only membership");
            await PressKey(Key.Escape);
            await Until(() => !deployment.GetNode<ContextPopup>("%EquipmentPopup").IsOpen, "equipment closes");
            Require(panel.IsVisibleInTree(), "trait panel restored after equipment");
            _step = "actual formation";
            var deployedId = run.Roster[0].InstanceId;
            var source = Cells(deployment).Single(cell => cell.PieceId == deployedId);
            await Drag(source, deployment.GetNode<Control>("%Body").GetGlobalRect().GetCenter());
            Require(!run.Deployment.Contains(deployedId) && row.Model!.Value == 1 && row.Model.ActiveIndex < 0, "real withdrawal updates inactive count");
            await Capture("withdrawn");
            await Deploy(deployment, deployedId);
            Require(row.Model!.Value == 2 && row.Model.ActiveIndex == 0, "real deployment activates first tier");
            await Deploy(deployment, run.Roster[2].InstanceId);
            await Deploy(deployment, run.Roster[3].InstanceId);
            Require(row.Model!.Value == 4 && row.Model.ActiveIndex == 1 && row.Model.NextThreshold == 6, "real drags reach second tier");
            CheckCounts(run, content, panel);
            Require(_stage.Viewport.GetVisibleRect().Encloses(deployment.GetNode<Button>("%StartBattleButton").GetGlobalRect()), "battle start remains visible");
            await Capture("deployment");
            await Click(deployment.GetNode<Button>("%StartBattleButton"));
            await Until(() => screens.Battle.IsVisibleInTree() && screens.Battle.HasActiveBattle, "actual battle");
            screens.Battle.SetPaused(true);
            Require(panel.IsVisibleInTree() && row.Model!.Value == 4, "battle retains starting lineup traits");
            await Capture("battle");
            screens.Battle.StopBattle();

            _step = "overflow and hidden contexts";
            run.Roster.Clear(); run.Deployment = ActiveRunFormationSchema.EmptyDeployment();
            foreach (var entry in heroes.Where(entry => entry.StableId.StartsWith("hero_mx", StringComparison.Ordinal)).Take(18))
                run.Roster.Add(new RosterHeroInstanceDto { InstanceId = "overflow-" + entry.StableId, ContentId = entry.StableId });
            game.Flow.ShowTower(); await Frames(5);
            var scroll = panel.GetNode<ScrollContainer>("%TraitScroll");
            Require(scroll.GetVScrollBar().Visible, "long list scrolls");
            var last = Rows(panel).Last(); last.GrabFocus(); await Wait(.35);
            Require(scroll.ScrollVertical > 0 && scroll.GetGlobalRect().HasPoint(last.GetGlobalRect().GetCenter()), "keyboard reaches final trait");
            await Capture("overflow");
            await Move(scroll.GetGlobalRect().GetCenter());
            var oldScroll = scroll.ScrollVertical;
            _stage.Push(new InputEventMouseButton { Position = scroll.GetGlobalRect().GetCenter(), ButtonIndex = MouseButton.WheelUp, Pressed = true });
            await Frames(4); Require(scroll.ScrollVertical < oldScroll, "real wheel scroll");
            game.Flow.ShowBattleLab(); await Frames(4);
            Require(!panel.IsVisibleInTree() && !Descendants<BattleLabTooltip>(panel).Any(tooltip => tooltip.Visible), "lab hides unrelated run traits");
            await Click(screens.BattleLab.GetNode<Button>("%StartButton"));
            await Until(() => screens.Battle.IsVisibleInTree() && screens.Battle.HasActiveBattle, "real lab battle");
            screens.Battle.SetPaused(true);
            Require(!panel.IsVisibleInTree() && screens.Battle.OffsetLeft == 0, "lab battle restores full authored width");
            GD.Print("RUN_TRAIT_PANEL_INPUT_OK shared-counts reserves equipment-dedup hover bridge keyboard escape modal navigation native-withdraw-deploy tier-up battle overflow lab-hidden read-only");
        }
        catch (Exception error) { code = 1; GD.PrintErr($"RUN_TRAIT_PANEL_INPUT_FAILED step={_step}: {error}"); if (_stage is not null) await Capture("failure"); }
        finally { _motion.Save("trait-tracker-motion"); game?.QueueFree(); await Frames(3); GetTree().Quit(code); }
    }

    private static void CheckCounts(ActiveRunDto run, ContentRegistry content, RunTraitPanel panel)
    {
        var snapshot = RunTraitSnapshotBuilder.Build(run, content.Graph);
        foreach (var row in Rows(panel))
        {
            Require(row.Model!.Value == snapshot.Value(row.Model.TraitId, 0), "display equals authoritative contribution snapshot");
            Require(row.Model.Icon is not null, "every displayed trait resolves its authored semantic icon");
        }
        var rows = Rows(panel).ToArray();
        Require(!rows.SkipWhile(row => row.Model!.ActiveIndex >= 0).Any(row => row.Model!.ActiveIndex >= 0), "active rows precede inactive rows");
    }
    private static void CheckEquipmentMembership(ActiveRunDto run, ContentRegistry content)
    {
        var native = content.Catalog.AllEntries().First(entry => entry.Definition is UnitDefinition { IsHero: true } &&
            content.Graph.ResolveUnitTraitContributions(entry.StableId).Any(trait => trait.TraitId == "trait_ne10_winter"));
        var hero = new RosterHeroInstanceDto { InstanceId = "badge-owner", ContentId = native.StableId,
            Equipment = [new EquipmentInstanceState { InstanceId = "badge", ContentId = "equipment_ne10_winter_badge", OwnerHeroInstanceId = "badge-owner" }] };
        run.Roster.Add(hero); run.Deployment[0] = hero.InstanceId;
        var row = RunTraitModels.Build(RunView.For(run), content).Single(row => row.TraitId == "trait_ne10_winter");
        Require(row.Value == 1 && row.Hint.PlainText.Contains("不重复计数"), "native and badge on one owner count once");
        run.Roster.Clear(); run.Deployment = ActiveRunFormationSchema.EmptyDeployment();
    }
    private async Task Deploy(DeploymentScreenController screen, string id)
    {
        var card = Descendants<DeploymentUnitCard>(screen).Single(card => card.InstanceId == id);
        var destination = Cells(screen).First(cell => cell.PieceId.Length == 0 && cell.IsVisibleInTree());
        await Drag(card.Portrait, destination.GetGlobalRect().GetCenter());
    }
    private async Task Drag(Control source, Vector2 destination)
    {
        await Reveal(source); var start = source.GetGlobalRect().GetCenter(); await Move(start);
        _stage.Push(new InputEventMouseButton { Position = start, GlobalPosition = start, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        for (var step = 1; step <= 20; step++)
        {
            var point = start.Lerp(destination, step / 20f);
            _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point, Relative = (destination - start) / 20, ButtonMask = MouseButtonMask.Left });
            await Frames(2); _motion.Sample(_stage.Viewport, _step);
        }
        Require(_stage.Viewport.GuiIsDragging(), "native drag started");
        _stage.Push(new InputEventMouseButton { Position = destination, GlobalPosition = destination, ButtonIndex = MouseButton.Left, Pressed = false });
        for (var frame = 0; frame < 12; frame++) { await Frames(1); _motion.Sample(_stage.Viewport, _step); }
    }
    private async Task Reveal(Control target)
    {
        for (var parent = target.GetParent(); parent is not null; parent = parent.GetParent())
            if (parent is ScrollContainer scroll) { scroll.EnsureControlVisible(target); await Frames(3); }
    }
    private async Task Click(Control target)
    {
        await Reveal(target); var point = target.GetGlobalRect().GetCenter();
        Require(target.IsVisibleInTree() && _stage.Viewport.GetVisibleRect().HasPoint(point), "visible input target " + target.GetPath());
        await Move(point);
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(5);
    }
    private async Task Move(Vector2 point) { _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point }); await Frames(2); }
    private async Task PressKey(Key key) { _stage.Push(new InputEventKey { Keycode = key, Pressed = true }); _stage.Push(new InputEventKey { Keycode = key, Pressed = false }); await Frames(5); }
    private async Task Wait(double seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    private async Task Frames(int count) { for (var frame = 0; frame < count; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Until(Func<bool> ready, string reason) { for (var frame = 0; frame < 400 && !ready(); frame++) await Frames(1); Require(ready(), reason); }
    private async Task Capture(string name) { await Frames(2); using var image = _stage.Viewport.GetTexture().GetImage(); Require(image.SavePng(ProjectSettings.GlobalizePath($"{Output}/{name}.png")) == Error.Ok, "render capture"); }
    private static IEnumerable<RunTraitRow> Rows(Node node) => Descendants<RunTraitRow>(node);
    private static IEnumerable<DeploymentCell> Cells(Node node) => Descendants<DeploymentCell>(node);
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren()) { if (child is T result) yield return result; foreach (var nested in Descendants<T>(child)) yield return nested; }
    }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
}
