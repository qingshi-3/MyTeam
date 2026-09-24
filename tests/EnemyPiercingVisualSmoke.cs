using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Presentation;
using TowerAutobattler.Project;
using TowerAutobattler.Vfx;

// Isolated production presets and BattleScreen; no run/save service is involved.
public partial class EnemyPiercingVisualSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var capture = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var screen = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
            AddChild(screen);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var layer = screen.GetNode<RangedAttackLayer>("%RangedAttackLayer");
            var player = layer.GetNode<VfxPlayer>("Player");
            CheckTerminalFlight(layer, player);
            var presets = new BattleLabPresetStore(GD.Load<BattleLabPresetCatalog>("res://content/battle-lab/battle_lab_presets.tres"));
            var index = new BattleLabContentIndex(package);
            foreach (var code in new[] { "ES01", "ES04" })
            {
                var preset = presets.BuiltIns.Single(p => p.Key.StartsWith(code + " ·", StringComparison.Ordinal));
                var config = new BattleLabPreparationAdapter(index).Build(BattleLabPresetStore.ToSnapshot(preset.Value));
                // Keep the visual fixture stationary so every frame compares the same line.
                for (var i = 0; i < config.Spawns.Count; i++)
                    if (config.Spawns[i].Team == 0)
                        config.Spawns[i] = config.Spawns[i] with { Unit = config.Spawns[i].Unit with
                            { Behavior = new(Stationary: true, DisableBasicAttacks: true) } };
                screen.StartBattle(package.Content, config, code + " · 蓄力贯穿 · 1 倍速");
                screen.SetLabControlsVisible(true);
                screen.SetSpeed(1);
                screen.SetPaused(false);
                var folder = capture is null ? null : Path.Combine(capture, code);
                if (folder is not null) Directory.CreateDirectory(folder);
                var paused = false;
                var sawCharge = false;
                var sawRelease = false;
                var captured = 0;
                for (var frame = 0; frame < 330; frame++)
                {
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    Require(screen.LastRuntimeFailure.Length == 0, screen.LastRuntimeFailure);
                    sawCharge |= player.GetChildren().OfType<VfxInstance>().Any(v => v.GetNodeOrNull<VfxChargedLineTrack>("Layers") is { Beam: false });
                    sawRelease |= code == "ES01" ? layer.ProjectileCount > 0 : player.GetChildren().OfType<VfxInstance>()
                        .Any(v => v.GetNodeOrNull<VfxChargedLineTrack>("Layers") is { Beam: true });
                    if (!paused && screen.TickIndex >= 16)
                    {
                        await Click(screen.GetNode<Button>("%PauseButton"));
                        Require(screen.IsPaused, "pause button receives the mouse release");
                        var tick = screen.TickIndex;
                        var ages = player.GetChildren().OfType<VfxInstance>().Select(v => (v, v.Playback.Age)).ToArray();
                        for (var j = 0; j < 5; j++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                        Require(screen.TickIndex == tick && ages.All(p => p.v.Playback.Age == p.Age), "mouse pause freezes charge and simulation");
                        await Click(screen.GetNode<Button>("%PauseButton"));
                        paused = true;
                    }
                    if (folder is not null && frame % 4 == 0)
                    {
                        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                        Require(GetViewport().GetTexture().GetImage().SavePng(Path.Combine(folder, $"{captured++:D4}.png")) == Error.Ok, "capture");
                    }
                }
                Require(sawCharge && sawRelease, "formal screen presents charge then release: " + code);
                screen.StopBattle();
                Require(player.ActiveCount == 0 && layer.ProjectileCount == 0, "battle teardown clears all visuals");
            }
            // Reduced motion retains the warning and gradual source light; motes stop.
            player.ReducedMotion = true;
            layer.SetClock(true, 1);
            player.Play("piercing_beam_charge", new(new(8, 4), new(1, 1), .18f));
            player.Advance(.8f);
            var effect = player.GetChildren().OfType<VfxInstance>().Single();
            var track = effect.GetNode<VfxChargedLineTrack>("Layers");
            Require(track.Strip.Visible && track.SourceGlow.Modulate.A > .18f && track.Motes.GetChildren().OfType<Sprite2D>().All(m => !m.Visible), "reduced motion retains information without orbiting motes");
            layer.Clear();
            screen.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print("ENEMY_PIERCING_VISUAL_OK shared-formal charge-release pause-input reduced-motion terminal-flight cleanup");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("ENEMY_PIERCING_VISUAL_FAILED " + error); GetTree().Quit(1); }
    }
    private static void CheckTerminalFlight(RangedAttackLayer layer, VfxPlayer player)
    {
        layer.SetClock(true, 1);
        var start = new Vector2(1, 2);
        var end = new Vector2(0, 2);
        var cue = new BattleLineCue("piercing_arrow_charge", "piercing_arrow", 0, 12, .18f, ChargedLineDelivery.Projectile);
        layer.Present([new(1, "projectile_spawn", "", "", 0, default, "", start, 91, end, Line: cue)], false);
        var instance = player.GetChildren().OfType<VfxInstance>().Single();
        layer.Present([new(2, "projectile_end", "", "", 0, default, "", end, 91)], false);
        Require(layer.ProjectileCount == 1, "end fact retains the unrendered terminal segment");
        layer.AdvanceFlights(.05f); player.Advance(0);
        Require(instance.Position.DistanceTo(layer.Project(start.Lerp(end, .5f), false)) < .01f,
            "terminal segment has a real intermediate display position");
        layer.AdvanceFlights(.05f); player.Advance(0);
        Require(instance.Position.DistanceTo(layer.Project(end, false)) < .01f && layer.ProjectileCount == 1,
            "full endpoint is presented before removal");
        layer.AdvanceFlights(0);
        Require(layer.ProjectileCount == 1, "paused terminal frame stays visible");
        layer.AdvanceFlights(.016f);
        Require(layer.ProjectileCount == 0 && player.ActiveCount == 0, "completed terminal segment cleans up");
        layer.Present([new(3, "projectile_spawn", "", "", 0, default, "", start, 92, end, Line: cue)], false);
        layer.Present([new(4, "projectile_impact", "", "", 0, default, "", start.Lerp(end, .5f), 92),
            new(4, "projectile_move", "", "", 0, default, "", end, 92)], false);
        Require(player.ActiveCount == 1, "impact waits for the visible arrow to reach contact");
        layer.AdvanceFlights(.025f);
        Require(player.ActiveCount == 1, "no scratch before contact");
        layer.AdvanceFlights(.03f);
        Require(player.ActiveCount == 2, "scratch starts as visible arrow crosses contact");
        layer.Clear();
    }
    private async Task Click(Button button)
    {
        var position = button.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion { Position = position, GlobalPosition = position }, true);
        GetViewport().PushInput(new InputEventMouseButton { Position = position, GlobalPosition = position, ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true }, true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetViewport().PushInput(new InputEventMouseButton { Position = position, GlobalPosition = position, ButtonIndex = MouseButton.Left, Pressed = false }, true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        // ProcessFrame fires before queued input and node processing. Observe only
        // after the release event has traversed the Control input path.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
