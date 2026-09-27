using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;
using TowerAutobattler.UI;

public partial class LabDragMotionInputSmoke : Node
{
    private UiInputStage _stage = null!;
    private string _step = "boot";
    public override async void _Ready()
    {
        var exit = 0;
        UiMotionBinding? motion = null;
        try
        {
            _stage = new UiInputStage(this, true);
            var gate = await GamePackagePublisher.CreateReadyAsync(this, GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var index = new BattleLabContentIndex(gate.Package ?? throw new Exception("package unavailable"));
            var session = new BattleLabSession(index, 6);
            var lab = GD.Load<PackedScene>("res://scenes/ui/BattleLabScreen.tscn").Instantiate<BattleLabScreenController>();
            _stage.AddChild(lab);
            motion = new UiMotionBinding(lab, () => false);
            lab.Bind(index, session);
            await Frames(4);
            await Click(lab.GetNode<Button>("%ToggleLibraryButton"));
            var search = lab.GetNode<LineEdit>("%UnitSearch");
            await Click(search);
            foreach (var character in "HC01") PressKey(0, character);
            await Frames(4);
            var card = Children<BattleLabLibraryCard>(lab).Single(node => node.IsVisibleInTree() && node.Side == BattleLabSide.Player);
            BattleLabBoardCell Cell(int x, int y) => Children<BattleLabBoardCell>(lab).Single(cell => cell.Cell == new Vector2I(x, y));
            _step = "library place";
            await Begin(card, Cell(6, 2));
            await Release(Cell(6, 2));
            Require(session.Units.Count == 1 && session.At(new Vector2I(6, 2)) is not null, "library drag places one unit");
            await Begin(card, Cell(7, 2));
            await Release(Cell(7, 2));
            var id = session.At(new Vector2I(6, 2))!.InstanceId;
            _step = "swap";
            await Begin(Cell(6, 2), Cell(7, 2));
            await Release(Cell(7, 2));
            Require(session.At(new Vector2I(7, 2))?.InstanceId == id, "existing pointer drag swaps units");
            _step = "escape cancel";
            var before = session.Freeze().CanonicalDigest;
            await Begin(Cell(7, 2), Cell(8, 2));
            PressKey(Key.Escape);
            await Release(Cell(8, 2));
            Require(session.Freeze().CanonicalDigest == before, "Escape does not place or duplicate a unit");
            _step = "recall";
            await Begin(Cell(7, 2), card);
            await Release(card);
            Require(session.Units.Count == 1 && !session.TryGet(id, out _), "return to library recalls existing unit");
            await Click(lab.GetNode<Button>("%CloseLibraryButton"));
            await Click(Cell(6, 2));
            await Click(lab.GetNode<Button>("%EquipmentDetailsButton"));
            _step = "catalog equipment";
            var panel = lab.GetNode<BattleLabEquipmentPanel>("%EquipmentDragPanel");
            var tile = Children<EquipmentSlotButton>(panel).First(button => button.SlotIndex < 0 && button.IsVisibleInTree());
            var slot = Children<EquipmentSlotButton>(panel).First(button => button.SlotIndex == 0);
            await Begin(tile, slot);
            await Release(slot);
            Require(session.Units.Single().Equipment.Length == 1, "catalog equipment commits a new instance");
            await Begin(slot, tile);
            await Release(tile);
            Require(session.Units.Single().Equipment.Length == 0, "lab equipment return removes its instance");
            Require(!Children<UiDragVisual>(_stage.Viewport).Any(), "all manual and native presentations cleaned up");
            GD.Print("LAB_DRAG_MOTION_OK production-scene,library-place,swap,escape,recall,catalog-equip,return,isolated-input");
        }
        catch (Exception error) { GD.PrintErr("LAB_DRAG_MOTION_FAILED " + _step + ": " + error); exit = 1; }
        finally { motion?.Dispose(); }
        GetTree().Quit(exit);
    }
    private async Task Begin(Control source, Control target)
    {
        var start = source.GetGlobalRect().GetCenter(); var end = target.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseMotion { Position = start, GlobalPosition = start });
        _stage.Push(new InputEventMouseButton { Position = start, GlobalPosition = start, ButtonIndex = MouseButton.Left,
            Pressed = true, ButtonMask = MouseButtonMask.Left });
        for (var i = 1; i <= 15; i++)
        {
            var p = start.Lerp(end, i / 15f);
            _stage.Push(new InputEventMouseMotion { Position = p, GlobalPosition = p, Relative = (end - start) / 15,
                ButtonMask = MouseButtonMask.Left });
            await Frames(2);
        }
        Require(Children<UiDragVisual>(_stage.Viewport).Any(node => !node.IsFinishing), "pointer drag has visible presentation");
    }
    private async Task Release(Control target)
    {
        var p = target.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = false });
        await Pause();
    }
    private async Task Click(Control target)
    {
        var p = target.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseMotion { Position = p, GlobalPosition = p });
        _stage.Push(new InputEventMouseButton { Position = p, GlobalPosition = p, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left });
        await Release(target);
    }
    private void PressKey(Key key, uint unicode = 0)
    {
        _stage.Push(new InputEventKey { Keycode = key, Unicode = unicode, Pressed = true });
        _stage.Push(new InputEventKey { Keycode = key, Unicode = unicode, Pressed = false });
    }
    private async Task Pause() => await ToSignal(GetTree().CreateTimer(.32), SceneTreeTimer.SignalName.Timeout);
    private async Task Frames(int count) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static IEnumerable<T> Children<T>(Node node) where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T item) yield return item;
            foreach (var item2 in Children<T>(child)) yield return item2;
        }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
