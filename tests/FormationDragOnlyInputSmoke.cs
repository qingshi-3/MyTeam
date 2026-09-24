using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Real production UI with isolated in-memory formation commits. The root's own
// coordinator is disconnected before installing the single fixture command owner.
public partial class FormationDragOnlyInputSmoke : Control
{
    private string _step = "startup";
    public override async void _Ready()
    {
        var exit = 0;
        try
        {
            Require(DisplayServer.GetName() != "headless", "rendered window required");
            Require(GetWindow().Mode == Window.ModeEnum.Maximized && !GetWindow().Borderless,
                "production default is a maximized decorated window");
            GD.Print($"DEPLOYMENT_WINDOW mode={GetWindow().Mode} size={GetWindow().Size}");
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var fixture = System.IO.File.ReadAllText(ProjectSettings.GlobalizePath("res://tests/fixtures/first-boss-ranged-stall.json"));
            var save = new MemorySave(fixture);
            var app = new RunApplication(package.Content, save, package.Project);
            var run = app.ActiveRun ?? throw new InvalidOperationException("memory fixture rejected");
            var originalIds = run.Roster.Select(hero => hero.InstanceId).ToHashSet();
            Require(app.Recruit(package.Project.Campaign.RecruitmentPool.ContentIds.First(id =>
                run.Roster.All(hero => hero.ContentId != id))), "formally recruit one legal reserve");
            var reserveId = run.Roster.Single(hero => !originalIds.Contains(hero.InstanceId)).InstanceId;
            Require(!run.Deployment.Contains(reserveId), "new recruit is reserve");

            var game = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
            game.SaveNamespace = $"tests/formation-drag-only/{Guid.NewGuid():N}";
            AddChild(game);
            for (var frame = 0; frame < 180 && game.Content is null; frame++) await Frames(1);
            Require(game.Content is not null, "production root booted in unique namespace");
            game.Flow.Dispose();
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            var screen = screens.Deployment;
            var encounter = app.CurrentEncounter();
            var commands = 0;
            var withdrawals = 0;
            screen.MoveRequested += command =>
            {
                commands++;
                Require(app.ApplyFormationCommand(command, screen.FloorRule!), "native drag submits valid formation command");
                screen.Bind(app, encounter);
            };
            screen.WithdrawRequested += id =>
            {
                withdrawals++;
                Require(app.WithdrawDeploymentUnit(id), "native return drag submits valid withdrawal");
                screen.Bind(app, encounter);
            };
            screens.BindEquipmentManagement(app);
            screen.Bind(app, encounter);
            screens.Show(AppScreenId.Deployment, run, app.Content, app.Rules);
            await Frames(4);
            var board = screen.GetNode<DeploymentBoard>("%DeploymentBoard");
            var window = screen.GetNode<Control>("%ReserveBench");
            Require(window.IsVisibleInTree() && window.GetGlobalRect().Position.Y >= board.GetGlobalRect().End.Y,
                "reserve bench is already visible below the battlefield");
            Require(Descendants<DeploymentUnitCard>(screen).All(card => !run.Deployment.Contains(card.InstanceId)),
                "bench contains only undeployed heroes");

            _step = "reserve click only selects";
            var firstEmpty = Cells(screen).First(cell => cell.PieceId.Length == 0 && !window.GetGlobalRect().Intersects(cell.GetGlobalRect()));
            var occupied = Cells(screen).First(cell => cell.PieceId.Length > 0 && !window.GetGlobalRect().Intersects(cell.GetGlobalRect()));
            var occupiedId = occupied.PieceId;
            var before = JsonSerializer.Serialize(run); var writes = save.Writes; var sent = commands;
            await Click(Card(screen, reserveId).Portrait);
            Require(screen.SelectedPieceId == reserveId, "reserve portrait click selects hero");
            Require(Cells(screen).Where(cell => cell.PieceId.Length == 0).All(cell => !cell.IsDragHovered
                    && cell.ThemeTypeVariation.ToString() is not ("DeploymentCellLegal" or "DeploymentCellIllegal" or "DeploymentCellSwap")),
                "click selection does not advertise click-to-place targets");
            await Click(firstEmpty);
            Require(screen.SelectedPieceId.Length == 0, "empty cell click clears selection");
            await Click(Card(screen, reserveId).Portrait);
            await Click(occupied);
            Require(screen.SelectedPieceId == occupiedId, "occupied cell click selects its current hero");
            Unchanged(run, save, before, writes, commands, sent, "reserve then empty/occupied clicks");
            await Capture("click-select-only");

            _step = "reserve portrait native deploy";
            var firstCell = firstEmpty.Cell;
            writes = save.Writes; sent = commands;
            await Drag(Card(screen, reserveId).Portrait, firstEmpty.GetGlobalRect().GetCenter());
            Require(run.Deployment[BattlefieldLayout.PlayerDeploymentSlot(firstCell)] == reserveId,
                "reserve portrait drag deploys into exposed friendly cell");
            Require(save.Writes == writes + 1 && commands == sent + 1, "deployment commits exactly once");
            Require(Cell(screen, firstCell).PieceId == reserveId
                && !Descendants<DeploymentUnitCard>(screen).Any(card => card.InstanceId == reserveId),
                "deployed hero leaves bench immediately");
            Require(window.IsVisibleInTree() && screen.GetNode<Label>("%EmptyReserve").Visible,
                "empty bench remains available as a withdrawal target");
            await Capture("reserve-deployed");

            _step = "deployed click does not move";
            var nextEmpty = Cells(screen).First(cell => cell.PieceId.Length == 0 && !window.GetGlobalRect().Intersects(cell.GetGlobalRect()));
            before = JsonSerializer.Serialize(run); writes = save.Writes; sent = commands;
            await Click(Cell(screen, firstCell));
            await Click(nextEmpty);
            Unchanged(run, save, before, writes, commands, sent, "deployed hero then empty cell clicks");

            _step = "deployed native move";
            var nextCell = nextEmpty.Cell;
            writes = save.Writes; sent = commands;
            await Drag(Cell(screen, firstCell), nextEmpty.GetGlobalRect().GetCenter());
            Require(run.Deployment[BattlefieldLayout.PlayerDeploymentSlot(nextCell)] == reserveId
                && run.Deployment[BattlefieldLayout.PlayerDeploymentSlot(firstCell)].Length == 0,
                "native board drag moves into empty cell");
            Require(save.Writes == writes + 1 && commands == sent + 1, "move commits exactly once");

            _step = "deployed native swap";
            var swapTarget = Cells(screen).First(cell => cell.PieceId.Length > 0 && cell.PieceId != reserveId
                && !window.GetGlobalRect().Intersects(cell.GetGlobalRect()));
            var swapCell = swapTarget.Cell; var swapId = swapTarget.PieceId;
            writes = save.Writes; sent = commands;
            await Drag(Cell(screen, nextCell), swapTarget.GetGlobalRect().GetCenter());
            Require(run.Deployment[BattlefieldLayout.PlayerDeploymentSlot(swapCell)] == reserveId
                && run.Deployment[BattlefieldLayout.PlayerDeploymentSlot(nextCell)] == swapId,
                "native board drag exchanges both deployed heroes");
            Require(save.Writes == writes + 1 && commands == sent + 1, "swap commits exactly once");
            await Capture("board-swap");

            _step = "return zone is drag only";
            var returnZone = window;
            before = JsonSerializer.Serialize(run); writes = save.Writes; sent = commands;
            var withdrew = withdrawals;
            await Click(Cell(screen, swapCell));
            await Click(returnZone);
            Unchanged(run, save, before, writes, commands, sent, "return-zone click");
            Require(withdrawals == withdrew, "return-zone click sends no withdrawal");
            await Drag(Cell(screen, swapCell), returnZone.GetGlobalRect().GetCenter());
            Require(!run.Deployment.Contains(reserveId) && run.Roster.Any(hero => hero.InstanceId == reserveId),
                "dragging to return zone moves hero into reserve");
            Require(save.Writes == writes + 1 && withdrawals == withdrew + 1 && commands == sent,
                "return-zone drag commits exactly one withdrawal");
            await Capture("withdrawn-to-reserve");
            _step = "reserve native replace";
            swapTarget = Cells(screen).First(cell => cell.PieceId.Length > 0 && !window.GetGlobalRect().Intersects(cell.GetGlobalRect()));
            swapCell = swapTarget.Cell; var displacedId = swapTarget.PieceId;
            writes = save.Writes; sent = commands;
            await Drag(Card(screen, reserveId).Portrait, swapTarget.GetGlobalRect().GetCenter());
            Require(run.Deployment[BattlefieldLayout.PlayerDeploymentSlot(swapCell)] == reserveId
                && !run.Deployment.Contains(displacedId) && run.Roster.Any(hero => hero.InstanceId == displacedId),
                "reserve drop replaces occupant while preserving displaced hero in roster");
            Require(save.Writes == writes + 1 && commands == sent + 1, "replace commits exactly once");

            _step = "reserve drag enemy area cancels";
            var enemyPoint = (from x in Enumerable.Range(7, BattlefieldLayout.Width - 7)
                              from y in Enumerable.Range(0, BattlefieldLayout.Height)
                              let point = board.GlobalPosition + board.CurrentProjection.CellToLocal(new Vector2I(x, y))
                              where GetViewport().GetVisibleRect().HasPoint(point) && !window.GetGlobalRect().HasPoint(point)
                              select point).First();
            before = JsonSerializer.Serialize(run); writes = save.Writes; sent = commands;
            await Drag(Card(screen, displacedId).Portrait, enemyPoint);
            Unchanged(run, save, before, writes, commands, sent, "reserve drop in enemy region");

            _step = "escape cancels active native drag";
            nextEmpty = Cells(screen).First(cell => cell.PieceId.Length == 0 && !window.GetGlobalRect().Intersects(cell.GetGlobalRect()));
            before = JsonSerializer.Serialize(run); writes = save.Writes; sent = commands;
            var destination = nextEmpty.GetGlobalRect().GetCenter();
            await BeginDrag(Card(screen, displacedId).Portrait, destination);
            Require(GetViewport().GuiIsDragging(), "Escape test has active native drag");
            await KeyPress(Key.Escape);
            Require(!GetViewport().GuiIsDragging() && window.IsVisibleInTree(), "Escape cancels drag and retains bench");
            await Release(destination);
            Unchanged(run, save, before, writes, commands, sent, "Escape then pointer release");
            Require(board.CurrentDragHoverCell is null && Cells(screen).All(cell => !cell.IsDragHovered), "cancel clears transient drop preview");
            await Capture("escape-cancelled");
            await KeyPress(Key.Escape);
            Require(window.IsVisibleInTree(), "Escape cannot dismiss the persistent bench");

            _step = "withdraw onto occupied bench card";
            var deployed = Cells(screen).First(cell => cell.PieceId.Length > 0);
            var returningId = deployed.PieceId;
            writes = save.Writes; withdrew = withdrawals;
            await Drag(deployed, Card(screen, displacedId).Portrait.GetGlobalRect().GetCenter());
            Require(!run.Deployment.Contains(returningId) && Card(screen, returningId).IsVisibleInTree()
                    && save.Writes == writes + 1 && withdrawals == withdrew + 1,
                "dropping onto a bench hero also returns the deployed hero exactly once");

            _step = "full bench rejection and keyboard inspection";
            while (run.Roster.Count(hero => !run.Deployment.Contains(hero.InstanceId)) < app.Rules.ReserveCapacity)
                Require(app.Recruit(package.Project.Campaign.RecruitmentPool.ContentIds.First(id =>
                    run.Roster.All(hero => hero.ContentId != id))), "fill bench with unique heroes");
            screen.Bind(app, encounter);
            await Frames(3);
            before = JsonSerializer.Serialize(run); writes = save.Writes; sent = commands; withdrew = withdrawals;
            await Drag(Cells(screen).First(cell => cell.PieceId.Length > 0), returnZone.GetGlobalRect().GetCenter());
            Unchanged(run, save, before, writes, commands, sent, "full bench withdrawal");
            Require(withdrawals == withdrew, "full bench rejects withdrawal before persistence");
            for (var i = 0; i < 40 && GetViewport().GuiGetFocusOwner() is not DeploymentUnitCard; i++) await KeyPress(Key.Tab);
            Require(GetViewport().GuiGetFocusOwner() is DeploymentUnitCard, "Tab reaches a visible bench hero");
            await KeyPress(Key.Enter);
            Unchanged(run, save, before, writes, commands, sent, "keyboard bench inspection");
            ValidateBenchLayout(screen);
            await Capture("completed");

            _step = "restored smaller window";
            GetWindow().Mode = Window.ModeEnum.Windowed;
            GetWindow().Size = new Vector2I(1280, 720);
            await Frames(5);
            ValidateBenchLayout(screen);
            var smallReserve = Descendants<DeploymentUnitCard>(screen).First();
            before = JsonSerializer.Serialize(run); writes = save.Writes; sent = commands;
            await Click(smallReserve.Portrait);
            Unchanged(run, save, before, writes, commands, sent, "small-window bench selection");
            await Capture("restored-1280");
            GD.Print("FORMATION_DRAG_ONLY_INPUT_OK maximized-window,persistent-bottom-bench,portrait-select,click-no-placement,reserve-deploy,single-save,board-move,board-swap,drag-withdraw,reserve-replace,enemy-cancel,escape-cancel,full-bench,keyboard,restored-window");
        }
        catch (Exception exception)
        {
            exit = 1; GD.PrintErr($"FORMATION_DRAG_ONLY_INPUT_FAILED step={_step}: {exception}");
            if (DisplayServer.GetName() != "headless") await Capture("failure");
        }
        GetTree().Quit(exit);
    }

    private static IEnumerable<DeploymentCell> Cells(Node root) => Descendants<DeploymentCell>(root);
    private static DeploymentCell Cell(Node root, Vector2I cell) => Cells(root).Single(value => value.Cell == cell);
    private static DeploymentUnitCard Card(Node root, string id) => Descendants<DeploymentUnitCard>(root).Single(value => value.InstanceId == id);
    private static void ValidateBenchLayout(DeploymentScreenController screen)
    {
        var viewport = screen.GetViewport().GetVisibleRect().Grow(1);
        var bench = screen.GetNode<Control>("%ReserveBench").GetGlobalRect();
        Require(viewport.Encloses(bench) && viewport.Encloses(screen.GetNode<Button>("%StartBattleButton").GetGlobalRect()),
            "bench and start action fit the window");
        foreach (var card in Descendants<DeploymentUnitCard>(screen))
        {
            Require(bench.Encloses(card.GetGlobalRect()), "ordinary bench cards fit without clipping");
            foreach (var leaf in Descendants<Control>(card).Where(control => control.IsVisibleInTree()
                         && (control is Label || control is UnitPortrait || control is TextureRect)))
                Require(card.GetGlobalRect().Grow(1).Encloses(leaf.GetGlobalRect()), "bench card content fits: " + leaf.GetPath());
        }
    }
    private static void Unchanged(ActiveRunDto run, MemorySave save, string state, int writes, int sent, int expectedSent, string reason) =>
        Require(JsonSerializer.Serialize(run) == state && save.Writes == writes && sent == expectedSent, reason + " must not mutate, save or send a move command");
    private async Task Click(Control target)
    {
        await Reveal(target); var point = target.GetGlobalRect().GetCenter();
        WarpPointer(point); await Frames(1);
        Inject(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        Inject(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        Inject(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(3);
    }
    // Pointer events enter the OS-window coordinate path. Stretch maps them back
    // into Control coordinates; maximized and restored windows need the same path.
    private void WarpPointer(Vector2 point) => Input.WarpMouse(GetViewport().GetFinalTransform() * point);
    private void Inject(InputEvent input) => Input.ParseInputEvent(input.XformedBy(GetViewport().GetFinalTransform()));
    private async Task Drag(Control source, Vector2 destination) { await BeginDrag(source, destination); await Release(destination); }
    private async Task BeginDrag(Control source, Vector2 destination)
    {
        await Reveal(source); var start = source.GetGlobalRect().GetCenter();
        Require(GetViewport().GetVisibleRect().HasPoint(start) && GetViewport().GetVisibleRect().HasPoint(destination), "native drag endpoints visible");
        WarpPointer(start); await Frames(1);
        Inject(new InputEventMouseMotion { Position = start, GlobalPosition = start });
        Inject(new InputEventMouseButton { Position = start, GlobalPosition = start, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        for (var i = 1; i <= 8; i++)
        {
            var point = start.Lerp(destination, i / 8f); WarpPointer(point);
            Inject(new InputEventMouseMotion { Position = point, GlobalPosition = point, Relative = (destination - start) / 8, ButtonMask = MouseButtonMask.Left });
            await Frames(1);
        }
        Require(GetViewport().GuiIsDragging(), "pointer movement starts native unit drag: " + source.GetPath());
        GD.Print($"FORMATION_DRAG step={_step} source={source.GetPath()} destination={destination} hover={GetViewport().GuiGetHoveredControl()?.GetPath()}");
    }
    private async Task Release(Vector2 point)
    {
        Inject(new InputEventMouseMotion { Position = point, GlobalPosition = point, ButtonMask = MouseButtonMask.Left });
        Inject(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(4);
    }
    private async Task Reveal(Control target)
    {
        Require(target.IsVisibleInTree(), "input target hidden: " + target.GetPath());
        for (var ancestor = target.GetParent(); ancestor is not null; ancestor = ancestor.GetParent())
        {
            if (ancestor is not ScrollContainer scroll) continue;
            for (var attempt = 0; attempt < 60; attempt++)
            {
                var rect = target.GetGlobalRect(); var clip = scroll.GetGlobalRect();
                if (clip.Grow(2).Encloses(rect)) break;
                var point = clip.GetCenter();
                var direction = rect.Position.Y < clip.Position.Y ? MouseButton.WheelUp : MouseButton.WheelDown;
                WarpPointer(point); await Frames(1);
                Inject(new InputEventMouseMotion { Position = point, GlobalPosition = point });
                Inject(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = direction, Pressed = true });
                Inject(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = direction, Pressed = false });
                await Frames(2);
            }
            Require(scroll.GetGlobalRect().HasPoint(target.GetGlobalRect().GetCenter()), "input target cannot be revealed");
        }
    }
    private async Task KeyPress(Key key)
    {
        Inject(new InputEventKey { Keycode = key, Pressed = true });
        Inject(new InputEventKey { Keycode = key, Pressed = false });
        await Frames(3);
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
        await Frames(2); DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://.godot/ui-review"));
        GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://.godot/ui-review/formation-drag-{name}.png"));
    }
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T value) yield return value;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static void Require(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
    private sealed class MemorySave : IRunSaveService
    {
        private string? _json;
        public MemorySave(string json) => _json = json;
        public int Writes { get; private set; }
        public MetaProgressDto LoadMeta() => new();
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => _json is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(_json);
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) { Writes++; _json = JsonSerializer.Serialize(value); return true; }
        public void DeleteActiveRun() => _json = null;
    }
}
