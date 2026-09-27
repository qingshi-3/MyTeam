using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.UI;

// Isolated production preset popup: native input, transition samples, no store.
public partial class UiPopupMotionInputSmoke : Node
{
    private UiInputStage _stage = null!;
    private readonly List<Dictionary<string, object>> _samples = [];
    private readonly List<Image> _frames = [];
    private bool _reduced;
    private int _frame;
    private const string Output = "res://.godot/ui-review/popup-motion";

    public override async void _Ready()
    {
        var exit = 0;
        UiMotionBinding? binding = null;
        try
        {
            _stage = new UiInputStage(this, true);
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(Output));
            var host = new Control { Theme = GD.Load<Theme>("res://content/ui/RealmTheme.tres") };
            _stage.AddChild(host);
            host.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var opener = new Button { Text = "打开实验预设", ThemeTypeVariation = "SecondaryButton",
                Position = new Vector2(50, 50), Size = new Vector2(220, 60) };
            host.AddChild(opener);
            var clicks = 0;
            var panel = GD.Load<PackedScene>("res://scenes/ui/components/BattleLabPresetPanel.tscn")
                .Instantiate<BattleLabPresetPanel>();
            host.AddChild(panel);
            opener.Pressed += () => { clicks++; panel.Open("", opener); };
            binding = new UiMotionBinding(host, () => _reduced);
            await Frames(4);
            var dialog = panel.GetNode<Control>("Center/Dialog");
            var name = panel.GetNode<LineEdit>("%PresetName");
            var close = panel.GetNode<Button>("%ClosePresetsButton");

            await Click(opener);
            Require(panel.IsOpen && dialog.Modulate.A < 1, "open starts visible fade");
            await Record(dialog, "open", .36);
            Require(dialog.OffsetTransformScale.IsEqualApprox(Vector2.One) && dialog.Modulate.A == 1,
                "open settles exactly with no drift");
            await Click(name);
            foreach (var letter in "motion_probe") PressKey(0, letter);
            await Frames(2);
            Require(name.Text == "motion_probe", "native text input after animation");
            await Click(close);
            Require(panel.IsOpen, "modal remains present during closing");
            await Click(opener);
            Require(clicks == 1, "closing blocker prevents click-through");
            await Record(dialog, "close", .20);
            Require(!panel.IsOpen && opener.HasFocus(), "close hides and restores focus once settled");

            // Close during the entrance: the current transform is the new origin.
            await Click(opener);
            await Frames(2);
            PressKey(Key.Escape);
            await Record(dialog, "interrupted-open", .22);
            Require(!panel.IsOpen && dialog.OffsetTransformScale.IsEqualApprox(Vector2.One),
                "early Escape leaves no partial scale or stale callback");
            await Click(opener);
            _reduced = true;
            await Frames(3);
            Require(dialog.Modulate.A == 1 && dialog.OffsetTransformScale.IsEqualApprox(Vector2.One),
                "reduced motion enabled mid-entrance settles immediately");
            PressKey(Key.Escape);
            await Frames(2);
            Require(!panel.IsOpen, "reduced close is immediate");
            await Click(opener);
            Require(panel.IsOpen && dialog.Modulate.A == 1 && dialog.OffsetTransformPosition == Vector2.Zero,
                "reduced open has no displacement");
            PressKey(Key.Escape);
            await Frames(2);
            _reduced = false;
            await Click(opener);
            await Frames(2);
            host.Hide();
            await Frames(2);
            host.Show();
            await Frames(3);
            Require(!panel.Visible && dialog.OffsetTransformScale.IsEqualApprox(Vector2.One) && dialog.Modulate.A == 1,
                "owner hide cancels motion and cannot resurrect popup");

            // Presentation-only transitions cannot emit an extra activation.
            await Frames(20);
            Require(clicks == 5, "no animation callback replays an action");
            var point = opener.GetGlobalRect().GetCenter();
            _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
            _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
            await Record(opener, "button-hold", .10);
            Require(opener.OffsetTransformScale.X < .97f, "held button compresses");
            // Cancel off-target: release must rebound without committing its command.
            var outside = new Vector2(350, 50);
            _stage.Push(new InputEventMouseMotion { Position = outside, GlobalPosition = outside,
                ButtonMask = MouseButtonMask.Left });
            _stage.Push(new InputEventMouseButton { Position = outside, GlobalPosition = outside,
                ButtonIndex = MouseButton.Left, Pressed = false });
            await Record(opener, "button-release", .32);
            Require(clicks == 5 && opener.OffsetTransformScale.IsEqualApprox(Vector2.One),
                "cancelled press rebounds without activation");
            Require(_samples.Exists(sample => (string)sample["phase"] == "button-release" && (float)sample["scale"] > 1.001f),
                "duplicate release events preserve spring overshoot");

            BattleLabHoverHint.Bind(opener, new BattleLabTooltipInfo("动作提示", Hint: "悬停与键盘焦点交接不闪烁"));
            _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
            await ToSignal(GetTree().CreateTimer(.65), SceneTreeTimer.SignalName.Timeout);
            var hint = opener.GetNode<BattleLabHoverHint>("BattleLabHoverHint");
            BattleLabTooltip? tooltip = null;
            foreach (var node in hint.GetChildren()) if (node is BattleLabTooltip item) tooltip = item;
            Require(tooltip?.Visible == true, "hover displays a real hint");
            // Native keyboard focus enters the already-hovered button.
            opener.ReleaseFocus();
            PressKey(Key.Tab);
            await Frames(1);
            Require(opener.HasFocus() && tooltip!.Visible, "focus handoff does not blink the existing hint");
            BattleLabHoverHint.HideAll(true);
            foreach (var frame in _frames)
            {
                frame.SavePng(Output + $"/frame-{_frame++:D3}.png");
                frame.Dispose();
            }
            System.IO.File.WriteAllText(ProjectSettings.GlobalizePath(Output + "/samples.json"),
                System.Text.Json.JsonSerializer.Serialize(_samples, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            GD.Print("UI_POPUP_MOTION_OK entrance close interruption blocker native-text reduced-midflight owner-hide focus press-rebound hover-focus-stability no-replayed-action");
        }
        catch (Exception error) { GD.PrintErr("UI_POPUP_MOTION_FAILED " + error); exit = 1; }
        finally { binding?.Dispose(); }
        GetTree().Quit(exit);
    }

    private async Task Record(Control dialog, string phase, double seconds)
    {
        var until = Time.GetTicksMsec() + (ulong)(seconds * 1000);
        do
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            _samples.Add(new() { ["phase"] = phase, ["ticks"] = Time.GetTicksMsec(),
                ["scale"] = dialog.OffsetTransformScale.X, ["y"] = dialog.OffsetTransformPosition.Y,
                ["alpha"] = dialog.Modulate.A, ["visible"] = dialog.IsVisibleInTree() });
            _frames.Add(_stage.Viewport.GetTexture().GetImage());
            await Frames(1);
        } while (Time.GetTicksMsec() < until);
    }

    private async Task Click(Control control)
    {
        var point = control.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left,
            ButtonMask = MouseButtonMask.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(1);
    }

    private void PressKey(Key code, uint unicode = 0)
    {
        _stage.Push(new InputEventKey { Keycode = code, Unicode = unicode, Pressed = true });
        _stage.Push(new InputEventKey { Keycode = code, Unicode = unicode, Pressed = false });
    }
    private async Task Frames(int count) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

