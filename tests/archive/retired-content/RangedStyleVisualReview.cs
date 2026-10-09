using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Content;
using TowerAutobattler.Composition;
using TowerAutobattler.Presentation;
using TowerAutobattler.Project;
using TowerAutobattler.Vfx;

// Real content, production BattleScreen and isolated GUI input. No save services.
public partial class RangedStyleVisualReview : Node
{
    private UiInputStage _stage = null!;
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    public override async void _Ready()
    {
        try
        {
            Engine.MaxFps = 30;
            var args = OS.GetCmdlineUserArgs();
            var before = args.Contains("--before");
            var left = args.Contains("--left");
            var reduced = args.Contains("--reduced");
            var name = args.FirstOrDefault(a => a.StartsWith("--case="))?[7..] ?? "core";
            var output = ProjectSettings.GlobalizePath(args.FirstOrDefault(a => a.StartsWith("--output="))?[9..]
                ?? "res://.godot/ranged-style-review/" + name);
            Directory.CreateDirectory(output);
            _stage = new(this, true);
            _stage.Resize(new(960, 540));
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var screen = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
            _stage.AddChild(screen);
            await Frame();
            var index = new BattleLabContentIndex(package);
            var ids = name switch
            {
                "elements" => new[] { "hero_hc10_ember_cashout", "hero_hc08_poison_keeper", "enemy_ice_hawker" },
                "spirit" => new[] { "hero_hc25_death_provider", "hero_hc12_control_clock", "hero_hc29_mind_binder" },
                "weapons" => new[] { "enemy_clockwork_boss", "enemy_ee08_return_blade", "hero_hc01_crossbow" },
                "rays" => new[] { "enemy_es04_prism_caster", "enemy_hexer", "enemy_ee09_swap_envoy" },
                "priest" => new[] { "hero_hc19_overheal_priest", "hero_hc18_healing_reader" },
                _ => new[] { "hero_hc01_crossbow", "hero_hc05_tide_caster", "hero_hc06_surge_conductor" }
            };
            var session = new BattleLabSession(index, 10, 912, BattleLabPlacementMode.FreeExperiment, "rule_clear");
            for (var i = 0; i < ids.Length; i++)
            {
                Require(session.AddAndPlace(ids[i], BattleLabSide.Player, new(left ? 6 : 2, i * 2)).Succeeded, "actor placement " + ids[i]);
                Require(session.AddAndPlace("soldier_dummy_static", BattleLabSide.Enemy, new(left ? 2 : 6, i * 2)).Succeeded, "target placement");
            }
            var config = new BattleLabPreparationAdapter(index).Build(session.Freeze());
            if (before)
                for (var i = 0; i < config.Spawns.Count; i++)
                {
                    var spawn = config.Spawns[i];
                    config.Spawns[i] = spawn with { Unit = spawn.Unit with
                    { AttackVisualId = name == "priest" ? "basic_arcane" :
                        spawn.Unit.AttackDelivery == AttackDelivery.Beam ? "beam" : "projectile" } };
                }
            var expected = config.Spawns.Where(s => s.Team == 0).Select(s => s.Unit.AttackVisualId).ToHashSet();
            screen.StartBattle(package.Content, config, string.Join(" / ", ids.Select(id => index.Units.Single(u => u.StableId == id).DisplayName)));
            screen.SetLabControlsVisible(true);
            var layer = screen.GetNode<RangedAttackLayer>("%RangedAttackLayer");
            var player = layer.GetNode<VfxPlayer>("Player");
            player.ReducedMotion = reduced;
            if (name == "priest" && !before) expected.Add("basic_holy_pillar");
            var seen = new HashSet<string>();
            var frames = new List<Image>();
            var times = new List<ulong>();
            var trace = new StringBuilder("frame,tick,scene,x,y,angle\n");
            for (var frame = 0; frame < 180; frame++)
            {
                await Frame();
                Require(screen.LastRuntimeFailure.Length == 0, screen.LastRuntimeFailure);
                foreach (var vfx in player.GetChildren().OfType<VfxInstance>())
                {
                    var id = Path.GetFileNameWithoutExtension(vfx.SceneFilePath);
                    seen.Add(id);
                    if (expected.Contains(id)) trace.AppendLine(FormattableString.Invariant($"{frame},{screen.TickIndex},{id},{vfx.Position.X},{vfx.Position.Y},{vfx.Rotation}"));
                }
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                frames.Add(_stage.Viewport.GetTexture().GetImage());
                times.Add(Time.GetTicksMsec());
            }
            Require(expected.IsSubsetOf(seen), "production attack visuals missing: " + string.Join(',', expected.Except(seen)));
            if (name == "priest" && !before)
            {
                for (var i = 0; i < 80 && !player.GetChildren().OfType<VfxInstance>()
                    .Any(v => v.SceneFilePath.EndsWith("basic_holy_pillar.tscn")); i++) await Frame();
                Require(player.GetChildren().OfType<VfxInstance>().Any(v =>
                    v.SceneFilePath.EndsWith("basic_holy_pillar.tscn")), "pause must cover a living pillar");
            }
            await Click(screen.GetNode<Button>("%PauseButton"));
            Require(screen.IsPaused, "actual pause input");
            var tick = screen.TickIndex;
            var poses = player.GetChildren().OfType<VfxInstance>().Select(v => (v, v.Transform, Age: v.Playback.Age,
                v.GetNodeOrNull<VfxBasicProjectileTrack>("Track")?.Core.Rotation)).ToArray();
            for (var i = 0; i < 6; i++) await Frame();
            Require(screen.TickIndex == tick && poses.All(p => p.v.Transform == p.Transform && p.v.Playback.Age == p.Age &&
                p.v.GetNodeOrNull<VfxBasicProjectileTrack>("Track")?.Core.Rotation == p.Rotation), "pause freezes projectile pose/spin");
            await Click(screen.GetNode<Button>("%StepButton"));
            Require(screen.TickIndex == tick + 1, "actual step input");
            await Click(screen.GetNode<Button>("%PauseButton"));
            Require(!screen.IsPaused, "actual resume input");
            await Click(screen.GetNode<Button>("%SpeedButton"));
            Require(screen.SpeedScale == 2, "actual speed input");
            screen.StopBattle();
            Require(layer.ProjectileCount == 0 && player.ActiveCount == 0, "battle teardown clears bodies and trails");
            for (var i = 0; i < frames.Count; i++)
            {
                Require(frames[i].SavePng(Path.Combine(output, $"{i:D4}.png")) == Error.Ok, "frame output");
                frames[i].Dispose();
            }
            File.WriteAllText(Path.Combine(output, "times.csv"), "frame,time_ms\n" + string.Join('\n', times.Select((t,i) => $"{i},{t-times[0]}")));
            File.WriteAllText(Path.Combine(output, "trace.csv"), trace.ToString());
            GD.Print($"RANGED_STYLE_VISUAL_OK case={name} baseline={before} left={left} reduced={reduced} expected={string.Join(',',expected)} real pause/step/resume/speed cleanup; frames={times.Count} span_ms={times[^1]-times[0]}");
            screen.QueueFree();
            await Frame();
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("RANGED_STYLE_VISUAL_FAILED: " + error); GetTree().Quit(1); }
    }
    private async Task Click(Control control)
    {
        var point = control.GetGlobalRect().GetCenter();
        Require(_stage.Viewport.GetVisibleRect().HasPoint(point), "input target inside 960x540 " + control.Name);
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false });
        await Frame();
        await Frame();
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
