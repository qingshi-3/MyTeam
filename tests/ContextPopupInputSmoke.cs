using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Composition;
using TowerAutobattler.App;
using TowerAutobattler.Content;
using TowerAutobattler.Presentation;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Authored screens, real input and rendered output. All run data is an in-memory
// fixture; this does not advance a campaign or write the player's save files.
public partial class ContextPopupInputSmoke : Control
{
    private string _step = "startup";
    public override async void _Ready()
    {
        var exit = 0;
        try
        {
            Require(DisplayServer.GetName() != "headless", "rendered window required");
            GetWindow().Size = new Vector2I(1600, 900);
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var fixture = System.IO.File.ReadAllText(ProjectSettings.GlobalizePath("res://tests/fixtures/first-boss-ranged-stall.json"));
            var save = new MemorySave(fixture);
            var app = new RunApplication(package.Content, save, package.Project);
            Require(app.ActiveRun is not null, "memory fixture accepted");
            var before = JsonSerializer.Serialize(app.ActiveRun);
            var saves = save.Writes;
            await Deployment(app);
            await Army(app);
            await Battle(app, package);
            await ComposedHeader(app);
            Require(JsonSerializer.Serialize(app.ActiveRun) == before && save.Writes == saves,
                "information interactions must not mutate or persist run state");
            GD.Print("CONTEXT_POPUP_INPUT_OK deployment-clean,select-without-popup,details,escape,backdrop,focus-loop,army-fullscreen,battle-unit,statistics,rules,stable-board,read-only-memory-run");
        }
        catch (Exception exception)
        {
            exit = 1;
            GD.PrintErr($"CONTEXT_POPUP_INPUT_FAILED step={_step}: {exception}");
            if (DisplayServer.GetName() != "headless") await Capture("failure");
        }
        GetTree().Quit(exit);
    }

    private async Task ComposedHeader(RunApplication app)
    {
        _step = "production composition header";
        var game = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
        game.SaveNamespace = $"tests/header-layout/{Guid.NewGuid():N}";
        AddChild(game);
        for (var frame = 0; frame < 180 && game.Content is null; frame++) await Frames(1);
        Require(game.Content is not null, "production root ready in isolated namespace");
        var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
        var army = game.GetNode<ArmyOverviewController>("ArmyOverview");
        screens.BindEquipmentManagement(app);
        screens.Deployment.Bind(app, app.CurrentEncounter());
        screens.Show(AppScreenId.Deployment, app.ActiveRun, app.Content, app.Rules);
        await Frames(4);
        var resource = army.GetNode<Button>("%SummaryButton");
        var headerRect = resource.GetGlobalRect();
        foreach (var (buttonName, popupName) in new[]
        {
            ("UnitDetailsButton", "UnitDetailsPopup"),
            ("EquipmentButton", "EquipmentPopup"), ("EncounterButton", "EncounterPopup")
        })
        {
            var button = screens.Deployment.GetNode<Button>("%" + buttonName);
            InViewport(button);
            Require(!headerRect.Intersects(button.GetGlobalRect()), "global resource strip must not overlap " + buttonName);
            await Click(button);
            Require(screens.Deployment.GetNode<ContextPopup>("%" + popupName).IsOpen,
                "real composed-header click opens " + popupName + " rather than army summary");
            await KeyPress(Key.Escape);
        }
        await Capture("composed-deployment-header");
        await Click(resource);
        Require(army.IsOpen, "global resource entry still opens army");
        await Capture("composed-army");
        await KeyPress(Key.Escape);
        Require(!army.IsOpen, "global army closes");
        var top = screens.Deployment.OffsetTop;
        // Route visibility is initialized by the real ScreenRouter. These are
        // presentation transitions only, not simulated campaign progression.
        foreach (var id in new[] { AppScreenId.Tower, AppScreenId.Recruitment, AppScreenId.Shop, AppScreenId.Reward })
        {
            screens.Show(id, app.ActiveRun, app.Content, app.Rules);
            await Frames(2);
            Require(army.IsVisibleInTree() && screens.Screen(id).GlobalPosition.Y >= headerRect.End.Y,
                "global header reserves a separate band for " + id);
        }
        screens.Show(AppScreenId.Battle, app.ActiveRun, app.Content, app.Rules);
        await Frames(2);
        Require(!army.IsVisibleInTree() && screens.Battle.OffsetTop == 0, "battle restores full height without global army bar");
        screens.Show(AppScreenId.Deployment, app.ActiveRun, app.Content, app.Rules);
        await Frames(2);
        Require(screens.Deployment.OffsetTop == top, "returning to deployment does not accumulate header padding");
        game.QueueFree();
        await Frames(3);
    }

    private async Task Deployment(RunApplication app)
    {
        _step = "deployment default";
        var screen = GD.Load<PackedScene>("res://scenes/ui/DeploymentScreen.tscn").Instantiate<DeploymentScreenController>();
        AddChild(screen);
        screen.Bind(app, app.CurrentEncounter());
        await Frames(4);
        var board = screen.GetNode<Control>("%DeploymentBoard");
        var rect = board.GetGlobalRect();
        Require(rect.Size.X > Size.X * .85f, "deployment default gives width to battlefield");
        Require(Descendants<ContextPopup>(screen).All(popup => !popup.IsOpen), "deployment windows default closed");
        await Capture("deployment-default");
        var unit = Descendants<DeploymentCell>(screen).First(cell => !string.IsNullOrEmpty(cell.PieceId));
        await Click(unit);
        Require(screen.SelectedPieceId == unit.PieceId, "real battlefield click selects hero");
        Require(Descendants<ContextPopup>(screen).All(popup => !popup.IsOpen), "formation selection does not open information");
        SameRect(board, rect);

        _step = "deployment details and keyboard";
        var opener = screen.GetNode<Button>("%UnitDetailsButton");
        var details = screen.GetNode<ContextPopup>("%UnitDetailsPopup");
        await Click(opener);
        Require(details.IsOpen, "detail entry opens window");
        Require(screen.GetNode<PreparedUnitDetailPanel>("%PreparedUnitDetailPanel").IsVisibleInTree(), "selected hero has visible details");
        SameRect(board, rect);
        InViewport(details.GetNode<Control>("Panel"));
        await Capture("deployment-details");
        for (var i = 0; i < 16; i++)
        {
            await KeyPress(Key.Tab, shift: i >= 8);
            var focus = GetViewport().GuiGetFocusOwner();
            Require(focus is not null && details.IsAncestorOf(focus), "Tab stays inside modal details");
        }
        await KeyPress(Key.Escape);
        Require(!details.IsOpen && GetViewport().GuiGetFocusOwner() == opener, "Escape closes and restores opener focus");
        await Click(opener);
        await ClickPoint(new Vector2(40, 130));
        Require(!details.IsOpen, "clicking backdrop closes details");
        SameRect(board, rect);

        _step = "deployment encounter";
        await Click(screen.GetNode<Button>("%EncounterButton"));
        var encounter = screen.GetNode<ContextPopup>("%EncounterPopup");
        Require(encounter.IsOpen && !details.IsOpen, "encounter opens alone");
        Require(!string.IsNullOrWhiteSpace(screen.GetNode<Label>("%EncounterInfo").Text), "encounter information retained");
        SameRect(board, rect);
        await Capture("deployment-encounter");
        await Click(encounter.GetNode<Button>("Panel/Layout/Header/Close"));
        Require(!encounter.IsOpen, "encounter explicit close");
        screen.QueueFree();
        await Frames(3);
    }

    private async Task Army(RunApplication app)
    {
        _step = "army centered modal";
        var army = GD.Load<PackedScene>("res://scenes/ui/components/ArmyOverview.tscn").Instantiate<ArmyOverviewController>();
        AddChild(army);
        army.BindEquipmentManagement(app);
        army.Bind(ArmyOverviewFactory.Build(app.ActiveRun!, app.Content, app.Rules));
        await Frames(3);
        Require(!army.IsOpen, "army defaults closed");
        var opener = army.GetNode<Button>("%SummaryButton");
        await Click(opener);
        Require(army.IsOpen, "army entry opens authored component");
        var panel = army.GetNode<Control>("%Drawer");
        var viewport = GetViewport().GetVisibleRect();
        InViewport(panel);
        Require(Mathf.Abs(panel.GetGlobalRect().GetCenter().X - viewport.GetCenter().X) < 2
            && panel.GetGlobalRect().Size.X >= viewport.Size.X * .95f,
            "army fills the viewport with roster and inventory together");
        Require(army.GetNode<RosterLoadoutView>("%ArmyEquipmentPanel").IsVisibleInTree(), "army roster bound");
        await Capture("army-details");
        await Click(army.GetNode<Button>("%CloseButton"));
        Require(!army.IsOpen && GetViewport().GuiGetFocusOwner() == opener, "army close restores focus");
        await Click(opener);
        await KeyPress(Key.Escape);
        Require(!army.IsOpen, "army Escape closes");
        army.QueueFree();
        await Frames(3);
    }

    private async Task Battle(RunApplication app, CompiledGamePackage package)
    {
        _step = "battle default";
        var battle = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
        AddChild(battle);
        battle.StartBattle(package.Content, app.BuildBattleConfig(app.CurrentEncounter(), false), "首层首领 · 信息窗口检查");
        battle.SetPaused(true);
        await Frames(5);
        Require(battle.HasActiveBattle && string.IsNullOrEmpty(battle.LastRuntimeFailure), "battle fixture initialized");
        var board = battle.GetNode<Control>("%BattleBoard");
        var rect = board.GetGlobalRect();
        var dock = battle.GetNode<BattleInspectorDock>("%BattleInspectorDock");
        var inspector = dock.GetNode<ContextPopup>("%InspectorPopup");
        var rules = battle.GetNode<ContextPopup>("%RulesPopup");
        Require(!inspector.IsOpen && !rules.IsOpen, "battle information defaults closed");
        Require(rect.Size.X > Size.X * .85f, "battle starts with a wide battlefield");
        await Capture("battle-default");
        var selected = battle.ReadRuntimeUnits().First(unit => unit.Team == 0);
        var presenter = Descendants<UnitContentRoot>(battle).Single(unit => unit.RuntimeId == selected.RuntimeId);

        _step = "battle unit click";
        await ClickPoint(presenter.GlobalPosition);
        Require(inspector.IsOpen && dock.Details.IsVisibleInTree(), "real battlefield unit click opens detail window");
        SameRect(board, rect);
        InViewport(dock.GetNode<Control>("%ExpandedInspector"));
        await Capture("battle-details");
        await Click(dock.GetNode<Button>("%CloseInspector"));
        Require(!inspector.IsOpen, "unit explicit close");
        SameRect(board, rect);

        _step = "battle statistics and rules";
        await Click(dock.GetNode<Button>("%StatisticsToggle"));
        Require(inspector.IsOpen && dock.WantsStatistics && !rules.IsOpen, "statistics opens independently");
        Require(dock.GetNode<Control>("%StatRows").GetChildCount() > 0, "statistics rows supplied by real battle");
        SameRect(board, rect);
        await Capture("battle-statistics");
        // Modal backdrop consumes this first click; the underlying command must
        // not run through it. A second intentional click then opens the rules.
        await Click(battle.GetNode<Button>("%RuleButton"));
        Require(!inspector.IsOpen && !rules.IsOpen, "modal closes without clicking through to rules");
        await Click(battle.GetNode<Button>("%RuleButton"));
        Require(rules.IsOpen && !inspector.IsOpen && !dock.WantsStatistics, "rules and inspector never stack");
        SameRect(board, rect);
        await Capture("battle-rules");
        await KeyPress(Key.Escape);
        Require(!rules.IsOpen, "battle rules Escape closes");
        await Click(dock.GetNode<Button>("%UnitToggle"));
        Require(inspector.IsOpen && !dock.WantsStatistics && dock.Details.IsVisibleInTree(), "unit entry preserves selected unit after closing");
        await KeyPress(Key.Escape);
        Require(!inspector.IsOpen && !rules.IsOpen && battle.IsPaused, "closing information preserves battle pause");
        SameRect(board, rect);
        battle.StopBattle();
        battle.QueueFree();
        await Frames(3);
    }

    private void SameRect(Control board, Rect2 expected)
    {
        var actual = board.GetGlobalRect();
        Require(actual.Position.DistanceTo(expected.Position) < 1 && actual.Size.DistanceTo(expected.Size) < 1,
            $"information must not move/resize battlefield: {expected} -> {actual}");
    }
    private void InViewport(Control control) => Require(control.IsVisibleInTree()
        && GetViewport().GetVisibleRect().Grow(2).Encloses(control.GetGlobalRect()), "window outside viewport: " + control.GetPath());
    private async Task Click(Control control)
    {
        Require(control.IsVisibleInTree(), "click target hidden: " + control.GetPath());
        await ClickPoint(control.GetGlobalRect().GetCenter());
    }
    private async Task ClickPoint(Vector2 point)
    {
        Require(GetViewport().GetVisibleRect().HasPoint(point), "click outside viewport: " + point);
        Input.WarpMouse(point);
        await Frames(1);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        Input.ParseInputEvent(new InputEventMouseMotion { Position = point, GlobalPosition = point, ButtonMask = MouseButtonMask.Left });
        Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(3);
    }
    private async Task KeyPress(Key key, bool shift = false)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, ShiftPressed = shift, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, ShiftPressed = shift, Pressed = false });
        await Frames(2);
    }
    private async Task Frames(int count)
    {
        for (var i = 0; i < count; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }
    private async Task Capture(string name)
    {
        await Frames(2);
        var directory = ProjectSettings.GlobalizePath("res://.godot/ui-review");
        DirAccess.MakeDirRecursiveAbsolute(directory);
        var path = directory + "/popup-" + name + ".png";
        Require(GetViewport().GetTexture().GetImage().SavePng(path) == Error.Ok, "screenshot save failed: " + path);
        GD.Print("CONTEXT_POPUP_CAPTURE " + path);
    }
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T item) yield return item;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
    private sealed class MemorySave(string json) : IRunSaveService
    {
        private string? _json = json;
        public int Writes { get; private set; }
        public MetaProgressDto LoadMeta() => new();
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => _json is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(_json);
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) { Writes++; _json = JsonSerializer.Serialize(value); return true; }
        public void DeleteActiveRun() { Writes++; _json = null; }
    }
}
