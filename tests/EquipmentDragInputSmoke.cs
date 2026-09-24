using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Real Control input and native drag/drop against current authored components.
// Fixtures stay in memory; no production save or campaign battle is advanced.
public partial class EquipmentDragInputSmoke : Control
{
    public override async void _Ready()
    {
        var code = 0;
        try
        {
            Require(DisplayServer.GetName() != "headless", "Requires a rendered window for real drag/drop.");
            GetWindow().Size = new Vector2I(1600, 900);
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var save = new MemorySave();
            var app = new RunApplication(package.Content, save, package.Project);
            Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], 44551), "create memory run");
            if (app.ActiveRun!.Roster.Count < 2) Require(app.Recruit(app.ActiveRun.Roster[0].ContentId), "second hero fixture");
            Require(app.GrantItem("equipment_rimebrand") && app.GrantItem("equipment_vanguard_insignia"), "inventory fixture");
            var run = app.ActiveRun!;
            var heroA = run.Roster[0].InstanceId;
            var heroB = run.Roster[1].InstanceId;
            var blade = run.EquipmentInventory[0].InstanceId;
            var armor = run.EquipmentInventory[1].InstanceId;
            var panel = GetNode<EquipmentLoadoutPanel>("Panel");
            panel.Bind(app, heroA);
            await Frames(4);
            await Click(Item(panel, blade));
            await Click(Slot(panel, 0));
            Require(run.EquipmentInventory.Count == 2, "clicks only inspect, never equip");
            await Drag(Item(panel, blade), Slot(panel, 0));
            Require(run.Roster[0].Equipment.Single().InstanceId == blade, "drag equips first item");
            await Drag(Item(panel, armor), Slot(panel, 0));
            Require(run.EquipmentInventory.Single().InstanceId == blade && run.Roster[0].Equipment.Single().InstanceId == armor,
                "drag replacement returns original identity to bag");
            await Capture("equipment-drag-replacement");
            var before = JsonSerializer.Serialize(run);
            await Drag(Slot(panel, 0), new Vector2(1200, 60));
            Require(JsonSerializer.Serialize(run) == before, "invalid drop cancels without ownership changes");
            var targets = panel.GetNode<HFlowContainer>("%EquipmentHeroes").GetChildren().OfType<EquipmentDropZone>().ToArray();
            await Drag(Slot(panel, 0), targets[1].GetNode<Control>("Layout/Portrait"));
            Require(run.Roster[0].Equipment.Count == 0 && run.Roster[1].Equipment.Single().InstanceId == armor, "drag transfers to another hero");
            await Drag(Slot(panel, 0), targets[0].GetNode<Control>("Layout/Select"));
            Require(run.Roster[0].Equipment.Single().InstanceId == armor, "hero name button accepts equipment drop");
            await Drag(Slot(panel, 0), targets[1].GetNode<Control>("Layout/Select"));
            Require(run.Roster[1].Equipment.Single().InstanceId == armor, "name-button transfer preserves item identity");
            await Drag(Slot(panel, 0), panel.GetNode<Control>("%EquipmentReturnZone").GetGlobalRect().End - new Vector2(14, 14));
            Require(run.Roster[1].Equipment.Count == 0 && run.EquipmentInventory.Count == 2, "dragging back unloads to inventory");
            await Drag(Item(panel, armor), Slot(panel, 0));
            await Drag(Slot(panel, 0), Item(panel, blade));
            Require(run.Roster[1].Equipment.Count == 0 && run.EquipmentInventory.Count == 2,
                "dropping worn equipment onto an existing inventory tile also unloads");
            save.Fail = true;
            before = JsonSerializer.Serialize(run);
            await Drag(Item(panel, blade), Slot(panel, 0));
            Require(JsonSerializer.Serialize(run) == before, "failed persistence retains inventory and slots");
            save.Fail = false;
            var stale = Item(panel, blade).DragPayload();
            var other = new RunApplication(package.Content, new MemorySave(), package.Project);
            Require(other.StartNewRun(other.Meta.UnlockedHeroIds[0], 44551) && other.GrantItem("equipment_rimebrand"), "second run fixture");
            Require(!RunEquipmentDropRules.Evaluate(other, other.ActiveRun!.Roster[0].InstanceId, stale).Allowed, "cross-run same-seed payload rejected");
            panel.Hide();
            await DeploymentInput(package);

            var index = new BattleLabContentIndex(package);
            var session = new BattleLabSession(index, 7, 81, BattleLabPlacementMode.FreeExperiment);
            var contentId = index.PlayerUnits.First(unit => !unit.Definition.IsTestDummy).StableId;
            var added = session.AddAndPlace(contentId, BattleLabSide.Player, new Vector2I(1, 2));
            Require(added.Succeeded, "lab fixture player");
            var screen = GD.Load<PackedScene>("res://scenes/ui/BattleLabScreen.tscn").Instantiate<BattleLabScreenController>();
            AddChild(screen); screen.Bind(index, session);
            await Frames(4);
            var cell = Descendants<BattleLabBoardCell>(screen).Single(value => value.InstanceId == added.InstanceId);
            Require(!screen.GetNode<ContextPopup>("%LibraryPopup").IsOpen && !screen.GetNode<ContextPopup>("%DetailsPopup").IsOpen,
                "lab defaults to unobstructed board");
            var boardRect = screen.GetNode<Control>("%Battlefield").GetGlobalRect();
            await Capture("popup-lab-default");
            await Click(cell);
            Require(!screen.GetNode<ContextPopup>("%DetailsPopup").IsOpen, "selecting a lab piece does not force open details");
            await Click(screen.GetNode<Button>("%EquipmentDetailsButton"));
            Require(screen.GetNode<ContextPopup>("%DetailsPopup").IsOpen && !screen.GetNode<ContextPopup>("%DetailsPopup").Blocking,
                "equipment opens nonmodal tool window");
            Require(boardRect == screen.GetNode<Control>("%Battlefield").GetGlobalRect(), "lab popup leaves board geometry unchanged");
            var unitScroll = Descendants<ScrollContainer>(screen).Single(control => control.Name == "EquipmentPage");
            // Scroll is moved through real wheel events to reveal the equipment section.
            var library = screen.GetNode<BattleLabEquipmentPanel>("%EquipmentDragPanel");
            for (var attempt = 0; attempt < 20 && !IsVisibleInScroll(library, unitScroll); attempt++)
                await Wheel(unitScroll, 3);
            var labSource = Descendants<EquipmentSlotButton>(library).First(tile => tile.InstanceId == "catalog:equipment_rimebrand");
            for (var attempt = 0; attempt < 15 && !IsVisibleInScroll(labSource, unitScroll); attempt++) await Wheel(unitScroll, 2);
            var labSlot = Descendants<EquipmentSlotButton>(library).First(tile => tile.SlotIndex == 0);
            // The library and slots share one compact component, keeping the drag endpoints visible together.
            await Drag(labSource, labSlot);
            Require(session.Units.Single().Equipment.Length == 1, "lab native drop configures item");
            await Capture("equipment-drag-lab");
            var labId = session.Units.Single().Equipment.Single().InstanceId;
            var labBefore = session.Freeze().CanonicalDigest;
            await Drag(labSlot, new Vector2(1510, 38));
            Require(session.Freeze().CanonicalDigest == labBefore, "lab invalid drop retains exact config");
            await Drag(labSlot, library.GetNode<Control>("Library/Layout/Title"));
            Require(session.Units.Single().Equipment.IsEmpty, "lab return removes configuration");
            await Drag(labSource, labSlot);
            await Drag(labSlot, labSource);
            Require(session.Units.Single().Equipment.IsEmpty, "lab library artwork also receives unload drops");
            await Drag(labSource, cell);
            Require(session.Units.Single().Equipment.Length == 1, "lab equipment tool drops through to exposed board hero");
            await Drag(labSlot, labSource);
            await Click(screen.GetNode<Button>("%CloseDetailsButton"));
            Require(!screen.GetNode<ContextPopup>("%DetailsPopup").IsOpen, "lab close button clears tool window");
            Require(boardRect == screen.GetNode<Control>("%Battlefield").GetGlobalRect(), "closing tool preserves board geometry");
            await Click(screen.GetNode<Button>("%UnitDetailsButton"));
            Require(screen.GetNode<ContextPopup>("%DetailsPopup").Blocking, "unit details are a modal information window");
            await Capture("popup-lab-unit-info");
            await KeyPress(Key.Escape);
            await Click(screen.GetNode<Button>("%TeamDetailsButton"));
            await Capture("popup-lab-team");
            await KeyPress(Key.Escape);
            await Click(screen.GetNode<Button>("%SettingsDetailsButton"));
            await Capture("popup-lab-settings");
            await KeyPress(Key.Escape);
            await Click(screen.GetNode<Button>("%ToggleLibraryButton"));
            Require(screen.GetNode<ContextPopup>("%LibraryPopup").IsOpen, "library opens only on request");
            await Capture("popup-lab-library");
            await KeyPress(Key.Escape);
            Require(session.Equip(added.InstanceId, 0, "equipment_rimebrand"), "lab transfer fixture");
            var sourceId = session.Units.Single().Equipment.Single().InstanceId;
            labBefore = session.Freeze().CanonicalDigest;
            Require(!session.MoveEquipment(sourceId, "missing", 0) && session.Freeze().CanonicalDigest == labBefore,
                "invalid lab move has no partial changes");
            await Click(screen.GetNode<Button>("%ToggleLibraryButton"));
            var libraryCard = Descendants<BattleLabLibraryCard>(screen).First(card => card.Side == BattleLabSide.Player && card.IsVisibleInTree());
            var emptyCell = Descendants<BattleLabBoardCell>(screen).Single(value => value.Cell == new Vector2I(4, 3));
            labBefore = session.Freeze().CanonicalDigest;
            await Click(libraryCard);
            await Click(emptyCell);
            Require(session.Freeze().CanonicalDigest == labBefore, "click prototype then empty cell only inspects; never adds a unit");
            await Click(cell);
            await Click(emptyCell);
            Require(session.Freeze().CanonicalDigest == labBefore, "click unit then empty cell never moves it");
            await Drag(libraryCard, emptyCell);
            Require(session.Units.Count() == 2, "unit library tool drag places a real unit on exposed battlefield");
            await KeyPress(Key.Escape);
            var movedCell = Descendants<BattleLabBoardCell>(screen).Single(value => value.Cell == new Vector2I(3, 2));
            await Drag(cell, movedCell);
            Require(movedCell.InstanceId == added.InstanceId, "existing lab unit still moves by drag");
            GD.Print("EQUIPMENT_DRAG_INPUT_OK inspect-only,equip,replace,portrait-and-name-transfer,blank-and-tile-return,cancel,save-failure,cross-run,deployment-board,full-slot-reject,formation-swap,lab-config,lab-return");
        }
        catch (Exception exception) { code = 1; GD.PrintErr("EQUIPMENT_DRAG_INPUT_FAILED " + exception); }
        GetTree().Quit(code);
    }
    private async Task DeploymentInput(CompiledGamePackage package)
    {
        var json = System.IO.File.ReadAllText(ProjectSettings.GlobalizePath("res://tests/fixtures/first-boss-ranged-stall.json"));
        var app = new RunApplication(package.Content, new MemorySave(json), package.Project);
        var run = app.ActiveRun ?? throw new InvalidOperationException("deployment memory fixture rejected");
        Require(app.GrantItem("equipment_rimebrand") && app.GrantItem("equipment_vanguard_insignia") &&
            app.GrantItem("equipment_field_focus") && app.GrantItem("equipment_ne10_swift_gloves"), "deployment item fixture");
        var screen = GD.Load<PackedScene>("res://scenes/ui/DeploymentScreen.tscn").Instantiate<DeploymentScreenController>();
        AddChild(screen); var encounter = app.CurrentEncounter(); screen.Bind(app, encounter);
        screen.MoveRequested += command =>
        {
            Require(app.ApplyFormationCommand(command, screen.FloorRule!), "real formation command still commits");
            screen.Bind(app, encounter);
        };
        await Frames(4);
        var boardRect = screen.GetNode<Control>("%DeploymentBoard").GetGlobalRect();
        await Click(screen.GetNode<Button>("%EquipmentButton"));
        var popup = screen.GetNode<ContextPopup>("%EquipmentPopup");
        Require(popup.IsOpen && popup.Blocking, "equipment opens full-screen roster manager");
        Require(boardRect == screen.GetNode<Control>("%DeploymentBoard").GetGlobalRect(), "manager preserves underlying deployment geometry");
        // Same-screen roster equipment paths are covered by RosterLoadoutInputSmoke.
        // The full-screen modal intentionally hides formation targets until closed.
        await Click(popup.GetNode<Button>("Panel/Layout/Header/Close"));
        var boardHero = Descendants<DeploymentCell>(screen).First(cell => !string.IsNullOrEmpty(cell.PieceId));
        var heroId = boardHero.PieceId;
        var second = Descendants<DeploymentCell>(screen).First(cell => !string.IsNullOrEmpty(cell.PieceId) && cell.PieceId != heroId);
        var oldPosition = run.Deployment.IndexOf(heroId);
        var secondId = second.PieceId; var targetPosition = run.Deployment.IndexOf(secondId);
        await Drag(boardHero, second);
        Require(run.Deployment.IndexOf(heroId) == targetPosition && run.Deployment.IndexOf(secondId) == oldPosition,
            "hero native formation drag still swaps positions, never becomes equipment input");
        screen.QueueFree(); await Frames(3);
    }
    private static EquipmentSlotButton Item(EquipmentLoadoutPanel panel, string id) =>
        Descendants<EquipmentSlotButton>(panel).Single(tile => tile.InstanceId == id);
    private static EquipmentSlotButton Slot(EquipmentLoadoutPanel panel, int slot) =>
        panel.GetNode<HBoxContainer>("%EquipmentSlots").GetChildren().OfType<EquipmentSlotButton>().Single(tile => tile.SlotIndex == slot);
    private async Task Drag(Control source, Control destination)
    {
        await Reveal(source);
        await Drag(source, destination.GetGlobalRect().GetCenter());
    }
    private async Task Drag(Control source, Vector2 destination)
    {
        Require(source.IsVisibleInTree(), "drag source hidden");
        await Reveal(source);
        var start = source.GetGlobalRect().GetCenter();
        Require(GetViewport().GetVisibleRect().HasPoint(start) && GetViewport().GetVisibleRect().HasPoint(destination),
            $"drag endpoint outside viewport: source={source.GetPath()} {start}, destination={destination}");
        Input.WarpMouse(start); await Frames(1);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = start, GlobalPosition = start });
        Input.ParseInputEvent(new InputEventMouseButton { Position = start, GlobalPosition = start, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        for (var step = 1; step <= 8; step++)
        {
            var point = start.Lerp(destination, step / 8f);
            Input.WarpMouse(point);
            Input.ParseInputEvent(new InputEventMouseMotion { Position = point, GlobalPosition = point, Relative = (destination - start) / 8, ButtonMask = MouseButtonMask.Left });
            await Frames(1);
        }
        GD.Print($"EQUIPMENT_DRAG source={source.GetPath()} rect={source.GetGlobalRect()} destination={destination} native-active={GetViewport().GuiIsDragging()} hovered={GetViewport().GuiGetHoveredControl()?.GetPath()}");
        // A native cursor refresh may arrive after WarpMouse while awaiting a
        // frame. Release at the same viewport position as the last real motion.
        Input.ParseInputEvent(new InputEventMouseMotion { Position = destination, GlobalPosition = destination, ButtonMask = MouseButtonMask.Left });
        Input.ParseInputEvent(new InputEventMouseButton { Position = destination, GlobalPosition = destination, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(4);
    }
    private async Task Reveal(Control target)
    {
        // VisibleInTree does not imply the Control is inside its scroll clip.
        // Bring the real item under the pointer before attempting a drag.
        for (var ancestor = target.GetParent(); ancestor is not null; ancestor = ancestor.GetParent())
        {
            if (ancestor is not ScrollContainer scroll) continue;
            for (var attempt = 0; attempt < 24; attempt++)
            {
                var targetRect = target.GetGlobalRect(); var clip = scroll.GetGlobalRect();
                if (clip.HasPoint(targetRect.Position + Vector2.One) && clip.HasPoint(targetRect.End - Vector2.One)) break;
                var point = clip.GetCenter(); Input.WarpMouse(point); await Frames(1);
                var direction = targetRect.GetCenter().Y < clip.Position.Y ? MouseButton.WheelUp : MouseButton.WheelDown;
                Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = direction, Pressed = true });
                Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = direction, Pressed = false });
                await Frames(2);
            }
            Require(scroll.GetGlobalRect().HasPoint(target.GetGlobalRect().GetCenter()),
                "drag source could not be revealed in " + scroll.GetPath());
        }
    }
    private async Task KeyPress(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, Pressed = false });
        await Frames(3);
    }
    private async Task Click(Control target)
    {
        await ClickPoint(target.GetGlobalRect().GetCenter());
    }
    private async Task ClickPoint(Vector2 point)
    {
        Input.WarpMouse(point); await Frames(1);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(3);
    }
    private async Task Wheel(Control target, int count)
    {
        var point = target.GetGlobalRect().GetCenter(); Input.WarpMouse(point); await Frames(1);
        for (var i = 0; i < count; i++)
        {
            Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.WheelDown, Pressed = true });
            Input.ParseInputEvent(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.WheelDown, Pressed = false });
        }
        await Frames(3);
    }
    private static bool IsVisibleInScroll(Control control, Control scroll) => scroll.GetGlobalRect().HasPoint(control.GetGlobalRect().GetCenter());
    private async Task Frames(int count) { for (var i = 0; i < count; i++) { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw); } }
    private async Task Capture(string name)
    {
        await Frames(2); DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://.godot/ui-review"));
        GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://.godot/ui-review/" + name + ".png"));
    }
    private static IEnumerable<T> Descendants<T>(Node node) where T : Node
    { foreach (var child in node.GetChildren()) { if (child is T item) yield return item; foreach (var nested in Descendants<T>(child)) yield return nested; } }
    private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
    private sealed class MemorySave : IRunSaveService
    {
        private string? _json;
        public MemorySave(string? json = null) => _json = json;
        public bool Fail { get; set; }
        public MetaProgressDto LoadMeta() => new();
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => _json is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(_json);
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) { if (Fail) return false; _json = JsonSerializer.Serialize(value); return true; }
        public void DeleteActiveRun() => _json = null;
    }
}
