using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.UI;

// Native production equipment controls, with a receiver that can refuse commit.
// No persistence store; checks that accepting a drag is not equated with a save.
public partial class UiDragMotionInputSmoke : Node
{
    private UiInputStage _stage = null!;
    private EquipmentSlotButton _source = null!, _target = null!;
    private bool _reduced;
    public override async void _Ready()
    {
        UiMotionBinding? binding = null;
        var exit = 0;
        try
        {
            _stage = new UiInputStage(this, true);
            var host = new Control { Theme = GD.Load<Theme>("res://content/ui/RealmTheme.tres") };
            _stage.AddChild(host);
            host.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var scene = GD.Load<PackedScene>("res://scenes/ui/components/EquipmentSlotButton.tscn");
            _source = scene.Instantiate<EquipmentSlotButton>();
            _target = scene.Instantiate<EquipmentSlotButton>();
            host.AddChild(_source); host.AddChild(_target);
            _source.Position = new Vector2(200, 250); _target.Position = new Vector2(700, 250);
            var item = GD.Load<ItemDefinition>("res://content/definitions/items/equipment_vanguard_insignia.tres");
            var tint = new Color(.93f, .96f, 1, .9f);
            _source.Modulate = tint;
            binding = new UiMotionBinding(host, () => _reduced);
            var refuse = true;
            var received = 0;
            _target.EquipmentDropped += (id, slot) =>
            {
                received++;
                if (refuse) return;
                _target.Bind(id, slot, item, false, true);
                _source.Bind("", -1, null, false, true);
            };
            void Reset()
            {
                _source.Bind("motion-item", -1, item, false, true);
                _target.Bind("", 0, null, false, true);
            }
            Reset(); await Frames(4);
            Require(GetWindow().Mode == Window.ModeEnum.Windowed && GetWindow().Size == new Vector2I(960, 540), "small test host");

            var visual = await Begin();
            Require(_source.Modulate.A < tint.A * .5f, "lift leaves a source silhouette");
            Require(visual.GetNode<Control>("Face").Scale.X > 1, "lift increases visual size");
            await Release();
            Require(visual.IsFinishing && visual.IsReturning && !visual.HasCommittedDestination && received == 1,
                "accepted drop with rejected commit returns to source");
            await Settle();
            Require(_source.Modulate == tint && _source.InstanceId == "motion-item", "failed commit preserves source and original tint");

            refuse = false;
            visual = await Begin();
            await Release();
            Require(visual.IsFinishing && !visual.IsReturning && visual.HasCommittedDestination,
                "changed bound identity supplies a real landing destination");
            Require(_target.InstanceId == "motion-item" && received == 2, "successful command occurs exactly once before animation completes");
            await Settle();

            Reset();
            _target.CanReceive = _ => false;
            visual = await Begin();
            await Release();
            Require(visual.IsReturning && received == 2, "illegal target does not send a command");
            // Begin again before the return finishes: old feedback cannot dim the new source.
            _target.CanReceive = null;
            visual = await Begin();
            _reduced = true;
            await Frames(3);
            Require(visual.GetNode<Control>("Face").Scale.IsEqualApprox(Vector2.One) &&
                visual.GetNode<Control>("Face").Rotation == 0, "reduced motion clears active tilt and lift");
            await Release();
            await Settle();
            Require(_source.Modulate == tint && received == 3, "rapid new drag preserves tint and commits once");

            _reduced = false; Reset();
            await Begin();
            host.Hide();
            await Frames(3);
            Require(!Visuals().Any() && _source.Modulate == tint, "owner hiding removes detached visuals and restores tint");
            _stage.Viewport.GuiCancelDrag();
            await Frames(2);
            host.Show();
            Require(received == 3, "cleanup cannot replay a drop");
            GD.Print("UI_DRAG_MOTION_OK native-input,lift,commit-rejection,committed-landing,illegal-target,rapid-redrag,reduced-midflight,owner-hide,source-tint,small-host");
        }
        catch (Exception error) { GD.PrintErr("UI_DRAG_MOTION_FAILED " + error); exit = 1; }
        finally { binding?.Dispose(); }
        GetTree().Quit(exit);
    }
    private System.Collections.Generic.IEnumerable<UiDragVisual> Visuals() => _stage.Viewport.GetChildren().OfType<UiDragVisual>();
    private async Task<UiDragVisual> Begin()
    {
        var start = _source.GetGlobalRect().GetCenter(); var end = _target.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseMotion { Position = start, GlobalPosition = start });
        _stage.Push(new InputEventMouseButton { Position = start, GlobalPosition = start, ButtonIndex = MouseButton.Left,
            Pressed = true, ButtonMask = MouseButtonMask.Left });
        for (var i = 1; i <= 20; i++)
        {
            var point = start.Lerp(end, i / 20f);
            _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point, Relative = (end - start) / 20,
                ButtonMask = MouseButtonMask.Left });
            await ToSignal(GetTree().CreateTimer(.012), SceneTreeTimer.SignalName.Timeout);
        }
        Require(_stage.Viewport.GuiIsDragging(), "native mouse movement starts dragging");
        return Visuals().Single(visual => !visual.IsFinishing);
    }
    private async Task Release()
    {
        var point = _target.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(1);
    }
    private async Task Settle()
    {
        await ToSignal(GetTree().CreateTimer(.3), SceneTreeTimer.SignalName.Timeout);
        Require(!Visuals().Any(), "drag visuals end without orphan nodes");
    }
    private async Task Frames(int count) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
