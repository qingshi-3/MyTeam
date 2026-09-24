using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Presentation;
using TowerAutobattler.Project;
using TowerAutobattler.Statuses;
using TowerAutobattler.UI;

// Current content, real Control input and isolated in-memory session; no player saves or preset writes.
public partial class PresentationBoundaryInputSmoke : Node
{
    private const string Output = "res://.godot/architecture-decoupling";
    private string _phase = "publication";

    public override async void _Ready()
    {
        var code = 0;
        Control? host = null;
        try
        {
            GetWindow().Mode = Window.ModeEnum.Windowed;
            GetWindow().Size = new Vector2I(1600, 900);
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var index = new BattleLabContentIndex(package);
            host = new Control { Theme = GD.Load<Theme>("res://content/ui/RealmTheme.tres") };
            AddChild(host);
            host.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

            _phase = "standalone cues";
            var layer = GD.Load<PackedScene>("res://scenes/ui/components/BattleFloatingCueLayer.tscn")
                .Instantiate<BattleFloatingCueLayer>();
            host.AddChild(layer);
            layer.Present([Damage(1, "owner", 2), Damage(2, "owner", 3), Damage(3, "other", 0)], [],
                _ => new Vector2(200, 200), point => point);
            var damage = layer.GetChildren().OfType<BattleFloatingCue>().Single();
            Require(damage.Text == "-5", "same-tick damage aggregates and zero damage is omitted");
            layer.Present([], [Status(StatusPresentationCueLifecycle.OnActive), Status(StatusPresentationCueLifecycle.Executed),
                Status(StatusPresentationCueLifecycle.Removed)], _ => new Vector2(200, 200), point => point);
            Require(layer.ActiveCueCount == 3, "same-batch activation is not duplicated, removal remains visible");
            layer.Present(Enumerable.Range(0, 80).Select(i => Damage(i + 10, "owner-" + i, 1)).ToArray(), [],
                _ => new Vector2(250, 250), point => point);
            Require(layer.ActiveCueCount == 64 && layer.ActiveTweenCount == 64, "transient cues and Tweens stay bounded");
            layer.Clear(); layer.Clear();
            Require(layer.GetChildCount() == 0 && layer.ActiveTweenCount == 0, "clear is complete and idempotent");
            layer.Present([Damage(100, "exit", 1)], [], _ => Vector2.Zero, point => point);
            host.RemoveChild(layer);
            Require(layer.ActiveCueCount == 0 && layer.ActiveTweenCount == 0, "independent layer exit clears its own cues");
            layer.Free();

            _phase = "preset input";
            var lab = GD.Load<PackedScene>("res://scenes/ui/BattleLabScreen.tscn").Instantiate<BattleLabScreenController>();
            host.AddChild(lab);
            var session = new BattleLabSession(index, 6);
            var store = new BattleLabPresetStore(lab.PresetCatalog);
            lab.Bind(index, session, store);
            var original = session.Freeze().CanonicalDigest;
            await Frames(4);
            var panel = lab.GetNode<BattleLabPresetPanel>("%PresetPanel");
            var open = lab.GetNode<Button>("%OpenPresetsButton");
            var search = panel.GetNode<LineEdit>("%PresetSearch");
            var choices = panel.GetNode<ItemList>("%PresetChoice");
            var load = panel.GetNode<Button>("%LoadPresetButton");
            await Click(open);
            Require(panel.IsOpen && search.HasFocus(), "preset browser opens with search focus");
            await ReplaceText(search, "zz_no_matching_preset_zz");
            Require(choices.ItemCount == 0 && load.Disabled, "empty search cannot request a load");
            await ReplaceText(search, "NE01");
            Require(choices.ItemCount > 0 && !load.Disabled, "real search finds current content");
            Require(session.Freeze().CanonicalDigest == original, "browsing does not mutate the session");
            await PressKey(Key.Tab);
            Require(choices.HasFocus(), "Tab moves into the preset list");
            await PressKey(Key.Tab, shift: true);
            Require(search.HasFocus(), "Shift-Tab returns to search");
            await Capture("preset-panel");
            await PressKey(Key.Escape);
            Require(!panel.IsOpen && open.HasFocus(), "Escape closes and restores focus");
            await Click(open);
            await Click(load);
            Require(!panel.IsOpen && session.Freeze().CanonicalDigest != original && session.Units.Count > 0,
                "real load intent commits current preset and closes the panel");
            await Click(lab.GetNode<Button>("%UndoButton"));
            Require(session.Freeze().CanonicalDigest == original, "preset load uses existing undo boundary");
            await Click(open);
            await Click(panel.GetNode<Button>("%RestoreDefaultButton"));
            Require(store.TryLoad(store.DefaultPresetName, out var defaultPreset) &&
                session.Freeze().CanonicalDigest == BattleLabPresetStore.ToSnapshot(defaultPreset).CanonicalDigest,
                "default request restores the authored default exactly");
            await Click(open);
            await ReplaceText(panel.GetNode<LineEdit>("%PresetName"), "invalid name");
            var beforeRejectedSave = session.Freeze().CanonicalDigest;
            await Click(panel.GetNode<Button>("%SavePresetButton"));
            Require(panel.GetNode<Label>("%PresetPreview").Text.Contains("名称需") &&
                session.Freeze().CanonicalDigest == beforeRejectedSave, "invalid save is rejected without session mutation or write");
            await PressKey(Key.Escape);

            _phase = "standalone preset intent";
            var independentPanel = GD.Load<PackedScene>("res://scenes/ui/components/BattleLabPresetPanel.tscn")
                .Instantiate<BattleLabPresetPanel>();
            host.AddChild(independentPanel);
            independentPanel.Bind(store, id => id);
            var requestedName = string.Empty;
            independentPanel.SaveRequested += name => requestedName = name;
            independentPanel.Open(store.DefaultPresetName, open);
            await Frames(2);
            await ReplaceText(independentPanel.GetNode<LineEdit>("%PresetName"), "boundary_probe");
            await Click(independentPanel.GetNode<Button>("%SavePresetButton"));
            Require(requestedName == "boundary_probe" && session.Freeze().CanonicalDigest == beforeRejectedSave &&
                panel.GetNode<LineEdit>("%PresetName").Text == "invalid name",
                "independent panel emits a save intent without owning session, persistence, or another panel's edit state");
            await PressKey(Key.Escape);
            independentPanel.QueueFree();
            await Frames(2);
            lab.Hide();

            _phase = "battle projection and replacement";
            var battle = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
            host.AddChild(battle);
            var adapter = new BattleLabPreparationAdapter(index);
            var snapshot = session.Freeze();
            battle.StartBattle(package.Content, adapter.Build(snapshot), "架构拆分验证");
            battle.SetPaused(true);
            var units = battle.ReadRuntimeUnits();
            Require(units.Length == snapshot.Units.Length && units.All(unit => unit.Health == unit.MaxHealth &&
                !string.IsNullOrWhiteSpace(unit.DisplayName)), "projection exposes current unit facts");
            var repeated = battle.ReadRuntimeUnits();
            Require(battle.TickIndex == 0 && units.Select(unit => unit.RuntimeId).SequenceEqual(repeated.Select(unit => unit.RuntimeId)),
                "reading projection does not advance the battle");
            for (var tick = 0; tick < 20 && battle.Outcome == BattleOutcome.Running; tick++)
                Require(battle.StepOneTick(), "paused battle accepts exactly one step");
            Require(units.All(unit => unit.Health == unit.MaxHealth), "earlier view snapshots remain immutable");
            await Capture("battle-after-extraction");
            var battleCues = battle.GetNode<BattleFloatingCueLayer>("%FloatingCueOverlay");
            battleCues.Present([Damage(500, "probe", 5)], [], _ => new Vector2(3, 3), point => point);
            Require(battle.ActiveFloatingCueCount > 0, "battle owns active extracted cues");
            battle.StartBattle(package.Content, adapter.Build(snapshot), "替换验证");
            battle.SetPaused(true);
            Require(battle.ActiveFloatingCueCount == 0 && battle.ActiveFloatingTweenCount == 0 && battle.TickIndex == 0,
                "replacement clears old cues and starts a clean clock");
            battle.StopBattle();
            Require(!battle.HasActiveBattle && battle.ActiveFloatingCueCount == 0 &&
                battle.GetNode<Node2D>("%UnitsRoot").GetChildCount() == 0, "stop clears battle and presenter ownership");
            Require(session.Freeze().CanonicalDigest == snapshot.CanonicalDigest, "battle never mutates the laboratory session");
            GD.Print("PRESENTATION_BOUNDARY_INPUT_OK standalone-cues aggregation capacity exit real-search tab escape load undo default rejected-save independent-save-intent projection replacement stop");
        }
        catch (Exception error) { GD.PrintErr($"PRESENTATION_BOUNDARY_INPUT_FAILED phase={_phase}: {error}"); code = 1; }
        finally { host?.QueueFree(); }
        await Frames(2);
        GetTree().Quit(code);
    }

    private static BattleCombatEvent Damage(long sequence, string target, float value) => new(
        sequence, "probe", null, "chain", 0, BattleCombatEventKind.DamageResolved, default, "source", target,
        1, value, value, value, default, "", 0, 0, "", default);

    private static StatusPresentationCue Status(StatusPresentationCueLifecycle lifecycle) => new(
        lifecycle, "", new StatusRuntimeSnapshot("probe", "测试状态", "", "source", "owner", "status", 1, 0, 0,
            1, 10, false, true, default, default, default, default, default, default, 0, [], [], [], "", ""), default, 1);

    private void Inject(InputEvent input) => Input.ParseInputEvent(input.XformedBy(GetViewport().GetFinalTransform()));
    private async Task Click(Control control)
    {
        Require(control.IsVisibleInTree(), "click target is visible: " + control.GetPath());
        var point = control.GetGlobalRect().GetCenter();
        Require(GetViewport().GetVisibleRect().HasPoint(point), "click target is in viewport: " + control.GetPath());
        Inject(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        Inject(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        Inject(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frames(3);
    }
    private async Task PressKey(Godot.Key key, bool ctrl = false, bool shift = false)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, CtrlPressed = ctrl, ShiftPressed = shift, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, CtrlPressed = ctrl, ShiftPressed = shift, Pressed = false });
        await Frames(2);
    }
    private async Task ReplaceText(LineEdit edit, string text)
    {
        await Click(edit); await PressKey(Godot.Key.A, ctrl: true); await PressKey(Godot.Key.Backspace);
        foreach (var character in text)
        {
            Input.ParseInputEvent(new InputEventKey { Unicode = character, Pressed = true });
            Input.ParseInputEvent(new InputEventKey { Unicode = character, Pressed = false });
        }
        await Frames(3);
        Require(edit.Text == text, "real text input reached the focused edit");
    }
    private async Task Capture(string name)
    {
        if (DisplayServer.GetName() == "headless") return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(Output));
        Require(GetViewport().GetTexture().GetImage().SavePng($"{Output}/{name}.png") == Error.Ok, "capture " + name);
    }
    private async Task Frames(int count) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
