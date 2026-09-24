using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;
using TowerAutobattler.UI;

public partial class DesignedContentInputSmoke : Node
{
    public override async void _Ready()
    {
        var code = 0;
        try
        {
            GetWindow().Size = new Vector2I(1600, 900);
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var index = new BattleLabContentIndex(gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors)));
            var session = new BattleLabSession(index, 6);
            var screen = GD.Load<PackedScene>("res://scenes/ui/BattleLabScreen.tscn").Instantiate<BattleLabScreenController>();
            AddChild(screen);
            await Frame(3);
            screen.Bind(index, session, new BattleLabPresetStore(screen.PresetCatalog));
            await Frame(3);
            BattleLabLibraryCard Card(string id, BattleLabSide side) => Descendants<BattleLabLibraryCard>(screen)
                .Single(card => card.ContentId == id && card.Side == side);
            BattleLabBoardCell Cell(int x, int y) => Descendants<BattleLabBoardCell>(screen).Single(cell => cell.Cell == new Vector2I(x, y));
            await Drag(Card("hero_hc01_crossbow", BattleLabSide.Player), Cell(2, 2));
            await Drag(Card("soldier_dummy_static", BattleLabSide.Enemy), Cell(7, 2));
            Require(session.Units.Count == 2, "real drag creates hero and dummy");
            await Click(Cell(2, 2));
            var toggle = screen.GetNode<CheckButton>("%RetentionUpgrade");
            Require(toggle.Visible, "upgrade available for selected hero");
            var scroll = screen.GetNode<ScrollContainer>("Margin/Root/ContentScroll");
            for (var i = 0; i < 20 && !scroll.GetGlobalRect().HasPoint(toggle.GetGlobalRect().GetCenter()); i++)
            {
                var point = scroll.GetGlobalRect().GetCenter();
                GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.WheelDown, Pressed = true });
                await Frame();
            }
            await Click(toggle);
            Require(session.Units.Single(unit => unit.Side == BattleLabSide.Player).RetainAttackStacks, "real upgrade click changes instance");
            Require(screen.GetNode<Label>("%Inspector").Text.Contains("测试暂值", StringComparison.Ordinal), "draft values visible");
            var roundTrip = BattleLabPresetStore.ToSnapshot(BattleLabPresetStore.ToDto(session.Freeze()));
            Require(roundTrip.Units.Any(unit => unit.RetainAttackStacks), "preset carries upgrade");
            var started = false;
            screen.StartRequested += () => started = true;
            await Click(screen.GetNode<Button>("%StartButton"));
            Require(started && BattleLabDerivedProjectionBuilder.Build(session).IsReady, "real start button accepts deployed config");
            if (DisplayServer.GetName() != "headless")
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var path = ProjectSettings.GlobalizePath("res://.godot/designed-content-input.png");
                Require(GetViewport().GetTexture().GetImage().SavePng(path) == Error.Ok, "render capture");
                GD.Print("DESIGNED_CONTENT_INPUT_CAPTURE " + path);
            }
            await Click(screen.GetNode<Control>("%PresetPanel").GetNode<Button>("%RestoreDefaultButton"));
            Require(session.Units.Count == 5 && session.Units.All(unit => !unit.RetainAttackStacks), "default restore clears test override");
            screen.QueueFree();
            await Frame(2);
            GD.Print("DESIGNED_CONTENT_INPUT_OK real-drag real-upgrade real-start default-restore preset-roundtrip");
        }
        catch (Exception e) { GD.PrintErr("DESIGNED_CONTENT_INPUT_FAILED: " + e); code = 1; }
        GetTree().Quit(code);
    }

    private async Task Drag(Control source, Control destination)
    {
        var from = source.GetGlobalRect().GetCenter();
        var to = destination.GetGlobalRect().GetCenter();
        GetViewport().PushInput(Mouse(from, true)); await Frame();
        GetViewport().PushInput(new InputEventMouseMotion { Position = to, GlobalPosition = to, Relative = to - from, ButtonMask = MouseButtonMask.Left });
        await Frame(); GetViewport().PushInput(Mouse(to, false)); await Frame(2);
    }
    private async Task Click(Control control)
    {
        var point = control.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }); await Frame();
        GetViewport().PushInput(Mouse(point, true)); await Frame();
        GetViewport().PushInput(Mouse(point, false)); await Frame(2);
    }
    private static InputEventMouseButton Mouse(Vector2 position, bool pressed) => new()
    { Position = position, GlobalPosition = position, ButtonIndex = MouseButton.Left, Pressed = pressed, ButtonMask = pressed ? MouseButtonMask.Left : 0 };
    private async Task Frame(int count = 1) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        { if (child is T match) yield return match; foreach (var item in Descendants<T>(child)) yield return item; }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
