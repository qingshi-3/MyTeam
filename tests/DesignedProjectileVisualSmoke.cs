using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Presentation;
using TowerAutobattler.Project;
using TowerAutobattler.Vfx;

// Current content and the formal BattleScreen, isolated from all save services.
public partial class DesignedProjectileVisualSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var args = OS.GetCmdlineUserArgs();
            var before = args.Contains("--before");
            var capture = args.FirstOrDefault(arg => arg.StartsWith("--capture="))?[10..];
            var strideArg = args.FirstOrDefault(arg => arg.StartsWith("--stride="));
            var stride = strideArg is null ? 4 : int.Parse(strideArg[9..]);
            var speedArg = args.FirstOrDefault(arg => arg.StartsWith("--speed="));
            var speed = speedArg is null ? 1 : float.Parse(speedArg[8..], System.Globalization.CultureInfo.InvariantCulture);
            if (capture is not null) Directory.CreateDirectory(capture);
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var screen = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
            AddChild(screen);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var layer = screen.GetNode<RangedAttackLayer>("%RangedAttackLayer");
            var player = layer.GetNode<VfxPlayer>("Player");
            if (before)
            {
                // A local snapshot captures the reported look without touching shared Resources.
                player.Catalog = (VfxCatalog)player.Catalog.Duplicate(true);
                var old = player.Catalog.Find("projectile");
                old.Scene = GD.Load<PackedScene>("res://.godot/arrow-fix/before-projectile.tscn");
                old.FaceDirection = false;
                old.Size = 32;
            }
            else CheckDirectionAndImmediateVisibility(layer, player);

            var index = new BattleLabContentIndex(package);
            var session = new BattleLabSession(index, 6, 912, BattleLabPlacementMode.FreeExperiment, "rule_clear");
            Require(session.AddAndPlace("hero_hc01_crossbow", BattleLabSide.Player, new(2, 2)).Succeeded, "hero placement");
            Require(session.AddAndPlace("soldier_dummy_static", BattleLabSide.Enemy, new(6, 2)).Succeeded, "target placement");
            screen.StartBattle(package.Content, new BattleLabPreparationAdapter(index).Build(session.Freeze()), "HC01 普攻与三箭连射 · 显示检查");
            screen.SetLabControlsVisible(true);
            screen.SetSpeed(speed);
            var heroView = screen.GetNode<Node2D>("%UnitsRoot").GetChildren().OfType<UnitContentRoot>().Single(unit => unit.Team == 0);
            var animation = heroView.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
            var sprite = animation.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
            var trace = new StringBuilder("frame,tick,animation_frame,animation_progress,projectile,x,y\n");
            var volleyInstances = new HashSet<ulong>();
            var basicInstances = new HashSet<ulong>();
            for (var frame = 0; frame < 540; frame++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var hero = screen.ReadRuntimeUnits().Single(unit => unit.Team == 0);
                foreach (var visual in player.GetChildren().OfType<VfxInstance>().Where(instance => instance.Context.Direction.HasValue))
                {
                    trace.AppendLine(FormattableString.Invariant($"{frame},{screen.TickIndex},{sprite.Frame},{sprite.FrameProgress},{visual.GetInstanceId()},{visual.Position.X},{visual.Position.Y}"));
                    if (hero.Mode == BattleUnitMode.Casting) volleyInstances.Add(visual.GetInstanceId());
                    else if (hero.LastAbilityName.Length == 0) basicInstances.Add(visual.GetInstanceId());
                    if (!before)
                    {
                        var core = visual.GetNode<Sprite2D>("Core");
                        Require(core.IsVisibleInTree() && core.Modulate.A == 1 && core.Material is null,
                            "every ordinary/skill projectile is immediately opaque");
                    }
                }
                if (capture is not null && frame % stride == 0 && DisplayServer.GetName() != "headless")
                {
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    Require(GetViewport().GetTexture().GetImage().SavePng(Path.Combine(capture, $"{frame / stride:D4}.png")) == Error.Ok, "capture");
                }
                Require(screen.LastRuntimeFailure.Length == 0, screen.LastRuntimeFailure);
            }
            Require(basicInstances.Count > 0 && volleyInstances.Count >= 3, "basic shots and three distinct volley visuals reach formal screen");
            if (capture is not null) File.WriteAllText(Path.Combine(capture, "trace.csv"), trace.ToString());
            screen.SetPaused(true);
            var paused = player.GetChildren().OfType<VfxInstance>().Where(instance => instance.Context.Direction.HasValue)
                .Select(instance => (instance, instance.Position, instance.Rotation)).ToArray();
            for (var i = 0; i < 5; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(paused.All(state => state.instance.Position == state.Position && state.instance.Rotation == state.Rotation), "pause freezes flight pose");
            screen.StopBattle();
            Require(layer.ProjectileCount == 0 && player.ActiveCount == 0, "battle teardown clears all arrow visuals");
            screen.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print($"DESIGNED_PROJECTILE_VISUAL_OK baseline={before} basic={basicInstances.Count} volley={volleyInstances.Count} direction=right,left,diagonal pause cleanup");
            GetTree().Quit();
        }
        catch (Exception exception) { GD.PrintErr("DESIGNED_PROJECTILE_VISUAL_FAILED: " + exception); GetTree().Quit(1); }
    }

    private static void CheckDirectionAndImmediateVisibility(RangedAttackLayer layer, VfxPlayer player)
    {
        layer.SetClock(true, 1);
        player.ReducedMotion = true;
        var origin = new Vector2(3, 2);
        foreach (var direction in new[] { Vector2.Right, Vector2.Left, new Vector2(-1, 1).Normalized() })
        {
            layer.Present([new BattleEvent(1, "projectile_spawn", "", "", 0, default, "", origin, 1, origin + direction)], false);
            var instance = player.GetChildren().OfType<VfxInstance>().Single();
            var core = instance.GetNode<Sprite2D>("Core");
            Require(core.Visible && core.Modulate.A == 1 && core.Material is null && core.Texture is AtlasTexture,
                "arrow has a real texture, no birth fade or energy shader");
            var expected = (layer.Project(origin + direction, false) - layer.Project(origin, false)).Normalized();
            Require(instance.Transform.X.Normalized().Dot(expected) > .999f, "arrow faces projected flight heading");
            var rotation = instance.Rotation;
            layer.Present([new BattleEvent(2, "projectile_move", "", "", 0, default, "", origin + direction, 1)], false);
            layer.AdvanceFlights(0); // consume the new segment's render-frame boundary
            layer.AdvanceFlights(.05f);
            player.Advance(0);
            Require(instance.Position.DistanceTo(layer.Project(origin + direction * .5f, false)) < .01f,
                "flight has an intermediate rendered position between authority samples");
            layer.Present([new BattleEvent(2, "projectile_move", "", "", 0, default, "", origin + direction, 1)], true);
            Require(Math.Abs(instance.Rotation - rotation) < .001f, "move facts retain launch heading");
            layer.Present([new BattleEvent(3, "projectile_end", "", "", 0, default, "", origin + direction, 1)], true);
            Require(layer.ProjectileCount == 0 && player.ActiveCount == 0, "impact removes projectile immediately");
        }
        player.ReducedMotion = false;
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
