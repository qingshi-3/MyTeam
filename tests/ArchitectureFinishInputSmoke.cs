using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;
using TowerAutobattler.UI;

public partial class ArchitectureFinishInputSmoke : Node
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
            var space = "tests/architecture-finish/" + Guid.NewGuid().ToString("N");
            _directory = ProjectSettings.GlobalizePath("user://" + space);
            _stage = new UiInputStage(this, true);
            _stage.Resize(new(960, 540));
            game = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
            game.SaveNamespace = space;
            _stage.AddChild(game);
            for (var i = 0; i < 180 && game.Content is null; i++) await Frames(1);
            Require(game.Content is not null, "production publication");
            await Frames(8);
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            var labButton = screens.MainMenu.GetNode<Button>("Center/Panel/Menu/BattleLabButton");
            if (!labButton.IsVisibleInTree()) await Activate(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ToolsButton"));
            await Activate(labButton);
            var lab = screens.BattleLab;
            Require(lab.IsVisibleInTree(), "real laboratory entry");
            var project = GameProjectCompiler.Compile(game.ProjectDefinition!, game.Content!.Graph).Project!;
            var index = new BattleLabContentIndex(new CompiledGamePackage(game.Content, project, game.Content.PublicationVersion));
            var session = new BattleLabSession(index, 8, 713);
            VerifyEditorSession(index);
            // Only the starting formation is a fixture. All following edits use actual GUI input.
            lab.Bind(index, session);
            await Frames(4);
            await Activate(lab.GetNode<Button>("%ToggleLibraryButton"));
            var source = Descendants<BattleLabLibraryCard>(lab).First(card => card.Side == BattleLabSide.Player &&
                _stage.Viewport.GetVisibleRect().HasPoint(card.GetGlobalRect().GetCenter()));
            var pane = lab.GetNode<Control>("%LibraryPane");
            var cells = Descendants<BattleLabBoardCell>(lab).Where(cell =>
                _stage.Viewport.GetVisibleRect().HasPoint(cell.GetGlobalRect().GetCenter()) &&
                !pane.GetGlobalRect().HasPoint(cell.GetGlobalRect().GetCenter())).OrderBy(cell => cell.Cell.Y).ThenBy(cell => cell.Cell.X).ToArray();
            Require(cells.Length >= 3, "visible board destinations");
            await Drag(source.GetGlobalRect().GetCenter(), cells[0].GetGlobalRect().GetCenter());
            Require(session.Units.Count == 1 && session.At(cells[0].Cell) is not null, "library drag confirmed placement: " + lab.LastFeedback);
            var placed = session.Freeze().CanonicalDigest;
            await Activate(lab.GetNode<Button>("%CloseLibraryButton"));
            for (var i = 0; i < 30 && pane.IsVisibleInTree(); i++) await Frames(1);
            await Frames(3);
            var dragSignals = 0;
            cells[0].UnitDragRequested += _ => dragSignals++;
            await Drag(cells[0].GetGlobalRect().GetCenter(), cells[1].GetGlobalRect().GetCenter());
            Require(session.At(cells[1].Cell) is not null && session.At(cells[0].Cell) is null, "unit drag confirmed move signals=" + dragSignals + " source=" + cells[0].GetGlobalRect() + " target=" + cells[1].GetGlobalRect() + ": " + lab.LastFeedback);
            await PressKey(Key.Z, control: true);
            Require(session.Freeze().CanonicalDigest == placed, "real undo restores placement and identity");
            await Drag(cells[0].GetGlobalRect().GetCenter(), new Vector2(950, 525), cancel: true);
            Require(session.Freeze().CanonicalDigest == placed, "Escape cancels active drag");
            await Capture("lab-drag-undo.png");
            await Activate(lab.GetNode<Button>("%UnitDetailsButton"));
            await Capture("lab-inspection.png");
            await PressKey(Key.Escape);
            var details = lab.GetNode<Control>("%DetailsPopup");
            for (var i = 0; i < 30 && details.IsVisibleInTree(); i++) await Frames(1);
            await Activate(lab.GetNode<Button>("%ToggleLibraryButton"));
            Require(lab.GetNode<ContextPopup>("%LibraryPopup").IsOpen, "recall library opened");
            await Drag(cells[0].GetGlobalRect().GetCenter(), pane.GetGlobalRect().GetCenter());
            Require(session.Units.Count == 0, "real recall to library: " + lab.LastFeedback);
            await Activate(lab.GetNode<Button>("%CloseLibraryButton"));
            for (var i = 0; i < 30 && pane.IsVisibleInTree(); i++) await Frames(1);
            await Frames(3);
            await PressKey(Key.Z, control: true);
            Require(session.Freeze().CanonicalDigest == placed, "undo recall restores unit");
            // Fault injection probes the new session/presenter lifetime, without a second full battle replay.
            var opponent = session.AddAndPlace(index.PveUnits.First(unit => unit.Definition.IsEnemy && unit.Definition.BodyRadius <= .5f).StableId,
                BattleLabSide.Enemy, new Vector2I(7, 3));
            Require(opponent.Succeeded, "failure probe opponent: " + opponent.RejectionReason);
            BattleLabBattleLifecycleContractSmoke.VerifyFailureTransactions(screens.Battle, game.Content, index, session.Freeze());
            Require(!screens.Battle.HasActiveBattle, "failure probes cleaned simulation");
            GD.Print("ARCHITECTURE_FINISH_INPUT_OK viewport=960x540 real=library-drag,move,undo,cancel,recall,inspection editor=bounded-history,preset-identity,failed-edit,published-projection failures=presenter,start,step,process desktop-pointer=unchanged");
        }
        catch (Exception e) { GD.PrintErr("ARCHITECTURE_FINISH_INPUT_FAILED " + e); exit = 1; if (_stage is not null) await Capture("finish-input-failure.png"); }
        finally { game?.QueueFree(); }
        await Frames(5);
        GetTree().Quit(exit);
    }
    private static void VerifyEditorSession(BattleLabContentIndex index)
    {
        var session = new BattleLabSession(index, 8, 100);
        var initial = session.Freeze();
        var editor = new BattleLabEditorSession(index, session, null);
        var first = editor.Prepared();
        Require(ReferenceEquals(first, editor.Prepared()), "unchanged prepared projection reused");
        for (var i = 0; i < 40; i++)
            Require(editor.Edit("seed", () => { session.SetRules(session.Mode, 8, session.Seed + 1, session.FloorRuleId); return true; }, "ok", "failed").Succeeded, "edit");
        Require(editor.UndoCount == 32 && !ReferenceEquals(first, editor.Prepared()), "bounded history and projection invalidation");
        var before = session.Freeze();
        Require(!editor.Edit("reject", () => false, "ok", "rejected").Succeeded &&
            before.CanonicalDigest == session.Freeze().CanonicalDigest && editor.UndoCount == 32, "failed edit has no undo record");
        Require(editor.LoadPreset("missing").Succeeded == false && before.CanonicalDigest == session.Freeze().CanonicalDigest,
            "missing preset leaves state unchanged");
        Require(editor.ValidatePresetName("bad name").Length > 0, "preset name gate");
        Require(editor.Undo().Succeeded && session.Seed == 139, "independent editor undo");
        Require(editor.PresetDigest == initial.CanonicalDigest, "undo preserves original preset identity");
    }
    private async Task Frames(int count)
    {
        for (var i = 0; i < count; i++) { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); RenderingServer.ForceDraw(false); }
    }
    private async Task PressKey(Key key, bool control = false)
    {
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, CtrlPressed = control, Pressed = true });
        _stage.Push(new InputEventKey { Keycode = key, PhysicalKeycode = key, CtrlPressed = control, Pressed = false });
        await Frames(6);
    }
    private async Task Activate(Button target)
    {
        var point = target.GetGlobalRect().GetCenter();
        Require(target.IsVisibleInTree() && !target.Disabled, "enabled input " + target.Name);
        if (!_stage.Viewport.GetVisibleRect().HasPoint(point))
        {
            for (var i = 0; i < 128 && _stage.Viewport.GuiGetFocusOwner() != target; i++) await PressKey(Key.Tab);
            Require(_stage.Viewport.GuiGetFocusOwner() == target, "focus target " + target.Name);
            await PressKey(Key.Space); return;
        }
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point }); await Frames(1);
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(6);
    }
    private async Task Drag(Vector2 start, Vector2 end, bool cancel = false)
    {
        _stage.Push(new InputEventMouseMotion { Position = start, GlobalPosition = start }); await Frames(2);
        _stage.Push(new InputEventMouseButton { Position = start, GlobalPosition = start, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true }); await Frames(2);
        var previous = start;
        for (var i = 1; i <= 8; i++)
        {
            var next = start.Lerp(end, i / 8f);
            _stage.Push(new InputEventMouseMotion { Position = next, GlobalPosition = next, Relative = next - previous, ButtonMask = MouseButtonMask.Left });
            previous = next; await Frames(2);
        }
        if (cancel) await PressKey(Key.Escape);
        _stage.Push(new InputEventMouseButton { Position = end, GlobalPosition = end, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(8);
    }
    private async Task Capture(string name)
    {
        await Frames(2);
        using var screenshot = _stage.Viewport.GetTexture().GetImage();
        Require(screenshot.SavePng(ProjectSettings.GlobalizePath("res://outputs/architecture-finish-20261001/" + name)) == Error.Ok, "capture");
    }
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    { foreach (var child in root.GetChildren()) { if (child is T item) yield return item; foreach (var descendant in Descendants<T>(child)) yield return descendant; } }
    public override void _ExitTree() { if (_directory.Length > 0 && Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
}
