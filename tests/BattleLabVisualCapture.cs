using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Content;
using TowerAutobattler.Presentation;
using TowerAutobattler.UI;

public partial class BattleLabVisualCapture : Node
{
    private const string OutputPath = "res://.godot/qa/battle-lab";
    private readonly Vector2I[] _sizes = [new(1280, 720), new(1600, 900)];
    private Vector2I _captureSize;

    public override async void _Ready()
    {
        var code = await CaptureAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(code);
    }

    private async Task<int> CaptureAsync()
    {
        try
        {
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(OutputPath));
            var gate = await TestProjectFixture.PublishAsync(this);
            var package = gate.Package ?? throw new InvalidOperationException(
                "Battle Lab visual package: " + string.Join(';', gate.Report.CoreErrors));
            var index = new BattleLabContentIndex(package);
            var catalog = GD.Load<BattleLabPresetCatalog>("res://content/battle-lab/battle_lab_presets.tres") ??
                          throw new InvalidOperationException("Battle Lab preset catalog missing");
            var store = new BattleLabPresetStore(catalog);
            Require(store.TryLoad("冰霜体系验证", out var preset), "Frost visual preset load");
            var snapshot = BattleLabPresetStore.ToSnapshot(preset);

            foreach (var size in _sizes)
            {
                _captureSize = size;
                GetWindow().Size = size;
                await RenderFrames(3);
                await CaptureLab(index, store, snapshot);
                await CaptureBattle(package.Content, index, snapshot);
            }

            GD.Print("BATTLE_LAB_VISUAL_CAPTURE_OK sizes=1280x720,1600x900 " +
                     "captures=lab-top,lab-build,battle-hud path=" + OutputPath);
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("BATTLE_LAB_VISUAL_CAPTURE_FAILED: " + exception);
            return 1;
        }
    }

    private async Task CaptureLab(
        BattleLabContentIndex index,
        BattleLabPresetStore store,
        BattleLabStartSnapshot snapshot)
    {
        var session = new BattleLabSession(index, snapshot.CurrentPopulation,
            snapshot.Seed, snapshot.Mode, snapshot.FloorRuleId);
        session.Restore(snapshot);
        var host = CreateHost();
        var scene = GD.Load<PackedScene>("res://scenes/ui/BattleLabScreen.tscn") ??
                    throw new InvalidOperationException("BattleLabScreen scene missing");
        var screen = scene.Instantiate<BattleLabScreenController>();
        host.AddChild(screen);
        try
        {
            await RenderFrames(3);
            screen.Bind(index, session, store);
            await RenderFrames(4);
            await Capture("BattleLabTop.png");

            var scroll = screen.GetNode<ScrollContainer>("Margin/Root/ContentScroll");
            scroll.ScrollVertical = (int)scroll.GetVScrollBar().MaxValue;
            await RenderFrames(4);
            await Capture("BattleLabBuild.png");
        }
        finally
        {
            await DisposeHost(host);
        }
    }

    private async Task CaptureBattle(
        TowerAutobattler.Content.ContentRegistry content,
        BattleLabContentIndex index,
        BattleLabStartSnapshot snapshot)
    {
        var host = CreateHost();
        var scene = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn") ??
                    throw new InvalidOperationException("BattleScreen scene missing");
        var screen = scene.Instantiate<BattleScreenController>();
        host.AddChild(screen);
        try
        {
            await RenderFrames(3);
            var config = new BattleLabPreparationAdapter(index).Build(snapshot);
            screen.StartBattle(content, config, "冰霜体系验证");
            screen.SetLabControlsVisible(true);
            screen.SetPaused(true);
            Require(screen.StepOneTick(), "Battle visual fixed step");
            await RenderFrames(3);

            var selected = screen.ReadRuntimeUnits().First(unit => unit.Team == 0);
            var presenter = Descendants<UnitContentRoot>(screen)
                .Single(unit => unit.RuntimeId == selected.RuntimeId);
            var point = presenter.GlobalPosition;
            GetViewport().PushInput(Mouse(point, true), true);
            await RenderFrame();
            GetViewport().PushInput(Mouse(point, false), true);
            await RenderFrames(2);
            Require(screen.GetNode<Control>("%SelectedUnitPanel").Visible,
                "Battle visual real selection opens inspector");
            await Capture("BattleHud.png");
        }
        finally
        {
            screen.StopBattle();
            await DisposeHost(host);
        }
    }

    private Control CreateHost()
    {
        var host = new Control
        {
            Size = GetViewport().GetVisibleRect().Size,
            Theme = GD.Load<Theme>("res://content/ui/RealmTheme.tres")
        };
        AddChild(host);
        var background = new ColorRect
        {
            Color = new Color("101522"),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        host.AddChild(background);
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return host;
    }

    private async Task Capture(string suffix)
    {
        await RenderFrame();
        var viewportImage = GetViewport().GetTexture().GetImage();
        Require(viewportImage.GetWidth() >= _captureSize.X && viewportImage.GetHeight() >= _captureSize.Y,
            $"capture viewport smaller than {_captureSize.X}x{_captureSize.Y}");
        var image = viewportImage.GetRegion(new Rect2I(Vector2I.Zero, _captureSize));
        var name = $"UI_{image.GetWidth()}x{image.GetHeight()}_{suffix}";
        var error = image.SavePng($"{ProjectSettings.GlobalizePath(OutputPath)}/{name}");
        if (error != Error.Ok) throw new InvalidOperationException($"capture {name}: {error}");
    }

    private async Task DisposeHost(Control host)
    {
        if (host.GetParent() is not null) host.GetParent().RemoveChild(host);
        host.Free();
        await RenderFrames(2);
    }

    private async Task RenderFrames(int count)
    {
        for (var index = 0; index < count; index++) await RenderFrame();
    }

    private async Task RenderFrame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
    }

    private static InputEventMouseButton Mouse(Vector2 position, bool pressed) => new()
    {
        Position = position,
        GlobalPosition = position,
        ButtonIndex = MouseButton.Left,
        ButtonMask = pressed ? MouseButtonMask.Left : 0,
        Pressed = pressed
    };

    private static System.Collections.Generic.IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
    }
}
