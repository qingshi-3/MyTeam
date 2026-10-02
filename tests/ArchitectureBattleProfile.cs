using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Growth;
using TowerAutobattler.Presentation;

// Fixed real-content workload; no player saves, synthetic stats or gameplay changes.
public partial class ArchitectureBattleProfile : Node
{
    private const int SampleTicks = 300;
    private static readonly string[] Heroes = ["hero_hc08_poison_keeper", "hero_hc13_shield_crit",
        "hero_hc18_healing_reader", "hero_hc23_frost_historian", "hero_hc24_swarm_keeper",
        "hero_hc25_death_provider", "hero_hc26_ash_returner", "hero_hc20_blood_drummer",
        "hero_hc14_stone_fist", "hero_mx25"];
    private AuthoredContentPackage? _authored;
    private string _output = "";
    private UiInputStage _stage = null!;
    private int _drawnFrames;
    private bool _trackingDraws;
    private bool _manualRender;
    private void CountDraw() => _drawnFrames++;

    public override void _Process(double delta)
    {
        // Last in the process order: render the actual scene after its simulation
        // and presentation have advanced. macOS occlusion must not skip GPU work.
        if (_manualRender) RenderingServer.ForceDraw(false, delta);
    }

    public override async void _Ready()
    {
        var exit = 0;
        BattleScreenController? screen = null;
        try
        {
            var args = OS.GetCmdlineUserArgs();
            _output = ProjectSettings.GlobalizePath(args.FirstOrDefault(a => a.StartsWith("--output="))?[9..]
                ?? "res://outputs/architecture-completion-20261001/profile");
            Directory.CreateDirectory(_output);
            var assembly = typeof(BattleSimulation).Assembly;
            var optimized = assembly.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled != true;
            var configuration = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;
            if (!optimized || configuration != "ExportRelease")
                throw new InvalidOperationException($"Profile requires optimized ExportRelease; got {configuration}, optimized={optimized}");
            GD.Print($"ARCHITECTURE_PROFILE_BUILD configuration={configuration} optimized={optimized} pid={System.Environment.ProcessId}");
            _authored = GrowthContentPackage.LoadAuthored();
            var publication = await GamePackagePublisher.CreateAuthoredReadyAsync(this, _authored);
            var package = publication.Package ?? throw new InvalidOperationException(string.Join(';', publication.Report.CoreErrors));
            var config = Build(new BattleLabContentIndex(package));
            if (args.Contains("--gui"))
            {
                if (DisplayServer.GetName() == "headless") throw new InvalidOperationException("GUI profile needs rendering");
                Engine.MaxFps = 60;
                ProcessPriority = int.MaxValue;
                RenderingServer.RenderLoopEnabled = false;
                _manualRender = true;
                RenderingServer.FramePostDraw += CountDraw;
                _trackingDraws = true;
                _stage = new UiInputStage(this, true);
                _stage.Resize(new(960, 540));
                screen = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
                _stage.AddChild(screen);
                foreach (var speed in new[] { 1f, 4f })
                {
                    screen.StartBattle(package.Content, config, "架构验证 · 实际内容");
                    await Frame();
                    await Frame();
                    await Frame();
                    await Click(screen.GetNode<Button>("%PauseButton"));
                    if (!screen.IsPaused) throw new InvalidOperationException("Pause input failed");
                    if (speed == 4)
                    {
                        await Click(screen.GetNode<Button>("%SpeedButton"));
                        await Click(screen.GetNode<Button>("%SpeedButton"));
                    }
                    if (screen.SpeedScale != speed) throw new InvalidOperationException("Speed input failed");
                    await Click(screen.GetNode<Button>("%PauseButton"));
                    await MeasureScreen(screen, speed, args.Contains("--record"));
                    screen.StopBattle();
                    await Frame();
                }
            }
            else
            {
                Measure(config, "warmup", false);
                var first = Measure(config, "first", true);
                var second = Measure(config, "second", true);
                if (first != second) throw new InvalidOperationException("Repeated real-content battle changed fingerprint");
            }
            GD.Print("ARCHITECTURE_BATTLE_PROFILE_OK");
            // Keep the native host alive while EventPipe flushes its final rundown.
            if (args.Contains("--trace")) await ToSignal(GetTree().CreateTimer(5), SceneTreeTimer.SignalName.Timeout);
        }
        catch (Exception e) { GD.PrintErr("ARCHITECTURE_BATTLE_PROFILE_FAILED " + e); exit = 1; }
        finally
        {
            if (_trackingDraws) RenderingServer.FramePostDraw -= CountDraw;
            if (_manualRender) RenderingServer.RenderLoopEnabled = true;
            _manualRender = false;
            screen?.QueueFree();
            _authored = null;
        }
        await Frame();
        GetTree().Quit(exit);
    }

    private static BattleConfig Build(BattleLabContentIndex index)
    {
        var session = new BattleLabSession(index, 10, 20261001);
        for (var side = 0; side < 2; side++)
        for (var i = 0; i < Heroes.Length; i++)
        {
            var placed = session.AddAndPlace(Heroes[i], (BattleLabSide)side, new(side == 0 ? i / 6 : 9 - i / 6, i % 6));
            if (!placed.Succeeded) throw new InvalidOperationException(Heroes[i] + ": " + placed.RejectionReason);
        }
        return new BattleLabPreparationAdapter(index).Build(session.Freeze());
    }

    private string Measure(BattleConfig config, string label, bool save)
    {
        using var battle = new BattleSimulation(config);
        var times = new List<double>(SampleTicks);
        var startBytes = GC.GetAllocatedBytesForCurrentThread();
        var timer = Stopwatch.StartNew();
        ArchitectureProfileEvents.Log.Begin(label);
        for (var i = 0; i < SampleTicks && battle.Outcome == BattleOutcome.Running; i++)
        {
            var start = Stopwatch.GetTimestamp();
            battle.Step();
            battle.DrainEvents();
            times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        ArchitectureProfileEvents.Log.End(label);
        var bytes = GC.GetAllocatedBytesForCurrentThread() - startBytes;
        var elapsed = timer.Elapsed.TotalMilliseconds;
        var result = battle.CreateResult();
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
            result.Digest, result.Outcome, result.Ticks,
            Units = result.Units.Select(u => new { u.RuntimeId, u.Team, u.Alive, u.FinalHealth, u.FinalShield,
                u.DamageDealt, u.DamageTaken, u.HealingDone, u.Kills, X = u.FinalCell.X, Y = u.FinalCell.Y }),
            Events = battle.CombatEvents
        }))));
        var summary = new { label, seed = 20261001, heroesPerSide = Heroes, ticks = times.Count,
            totalMs = elapsed, meanMs = times.Average(), p95Ms = Percentile(times, .95), maxMs = times.Max(),
            allocatedBytes = bytes, retainedUnits = battle.Units.Count, events = battle.CombatEvents.Count, fingerprint };
        GD.Print("ARCHITECTURE_CORE_MEASURE " + JsonSerializer.Serialize(summary));
        if (save) File.WriteAllText(Path.Combine(_output, label + ".json"), JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
        return fingerprint;
    }

    private async Task MeasureScreen(BattleScreenController screen, float speed, bool record)
    {
        var label = $"x{speed:0}";
        var startTick = screen.TickIndex;
        var startDraws = _drawnFrames;
        var times = new List<double>(2400);
        var timer = Stopwatch.StartNew();
        var last = Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        var recording = new List<(double Time, Image Image)>();
        var nextCapture = 0d;
        ArchitectureProfileEvents.Log.Begin(label);
        while (screen.TickIndex < SampleTicks && screen.HasActiveBattle)
        {
            await Frame();
            times.Add(Stopwatch.GetElapsedTime(last).TotalMilliseconds);
            last = Stopwatch.GetTimestamp();
            if (screen.LastRuntimeFailure.Length > 0) throw new InvalidOperationException(screen.LastRuntimeFailure);
            if (timer.Elapsed.TotalSeconds > 90) throw new InvalidOperationException("Real-time profile timed out");
            if (record && timer.Elapsed.TotalSeconds >= nextCapture && timer.Elapsed.TotalSeconds < 12)
            {
                recording.Add((timer.Elapsed.TotalSeconds, _stage.Viewport.GetTexture().GetImage()));
                nextCapture += 1d / 15;
            }
        }
        ArchitectureProfileEvents.Log.End(label);
        if (!record && _drawnFrames - startDraws < times.Count * .9)
            throw new InvalidOperationException("Offscreen host stopped rendering; discard this GUI timing sample");
        var summary = new { label, seed = 20261001, record, startTick, endTick = screen.TickIndex,
            rendering = "forced-offscreen-after-process",
            drawnFrames = _drawnFrames - startDraws,
            elapsedSeconds = timer.Elapsed.TotalSeconds, frames = times.Count, meanFrameMs = times.Average(),
            p95FrameMs = Percentile(times, .95), maxFrameMs = times.Max(), allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - bytes };
        GD.Print("ARCHITECTURE_SCREEN_MEASURE " + JsonSerializer.Serialize(summary));
        File.WriteAllText(Path.Combine(_output, label + ".json"), JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
        using (var screenshot = _stage.Viewport.GetTexture().GetImage()) screenshot.SavePng(Path.Combine(_output, label + ".png"));
        if (record)
        {
            var distinct = recording.Select(frame => Convert.ToHexString(SHA256.HashData(frame.Image.GetData()))).Distinct().Count();
            if (distinct < recording.Count * .8) throw new InvalidOperationException("Recording contains stale/repeated frames");
            var directory = Path.Combine(_output, label + "-frames");
            Directory.CreateDirectory(directory);
            for (var i = 0; i < recording.Count; i++)
            { recording[i].Image.SavePng(Path.Combine(directory, $"{i:D4}.png")); recording[i].Image.Dispose(); }
            File.WriteAllText(Path.Combine(directory, "times.json"), JsonSerializer.Serialize(recording.Select(f => f.Time)));
        }
    }

    private static double Percentile(List<double> values, double quantile) => values.Order().ElementAt(Math.Clamp((int)Math.Ceiling(values.Count * quantile) - 1, 0, values.Count - 1));
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    private async Task Click(Button button)
    {
        if (!button.IsVisibleInTree() || !_stage.Viewport.GetVisibleRect().Encloses(button.GetGlobalRect()))
            throw new InvalidOperationException("Profile input target outside isolated viewport: " + button.Name);
        var position = button.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseMotion { Position = position, GlobalPosition = position });
        await Frame();
        foreach (var pressed in new[] { true, false })
            _stage.Push(new InputEventMouseButton { Position = position, GlobalPosition = position,
                ButtonIndex = MouseButton.Left, ButtonMask = pressed ? MouseButtonMask.Left : 0, Pressed = pressed });
        await Frame();
        await Frame();
    }
}

[EventSource(Name = "MyTeam-ArchitectureProfile")]
internal sealed class ArchitectureProfileEvents : EventSource
{
    public static readonly ArchitectureProfileEvents Log = new();
    [Event(1)] public void Begin(string label) => WriteEvent(1, label);
    [Event(2)] public void End(string label) => WriteEvent(2, label);
}
