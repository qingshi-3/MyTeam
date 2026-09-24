using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Presentation;
using TowerAutobattler.Statuses;
using TowerAutobattler.Vfx;

// One production cast, with private starting mana for repeatable capture; no save services.
public partial class DesignedTauntVisualSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            GetWindow().Size = new(1600, 900);
            var args = OS.GetCmdlineUserArgs();
            var capture = args.FirstOrDefault(arg => arg.StartsWith("--capture="))?[10..];
            var before = args.Contains("--before");
            if (capture is not null) Directory.CreateDirectory(capture);
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var index = new BattleLabContentIndex(package);
            var session = new BattleLabSession(index, 6, 912, BattleLabPlacementMode.FreeExperiment, "rule_clear");
            foreach (var (id, team, x, y) in new[] {
                ("hero_hc03_iron_guard",0,4,2), ("soldier_dummy_static",1,5,2),
                ("soldier_dummy_melee",1,4,4), ("soldier_dummy_ranged",1,5,3), ("soldier_dummy_static",1,8,2) })
                Require(session.AddAndPlace(id, (BattleLabSide)team, new(x,y)).Succeeded, "place fixture");
            var config = new BattleLabPreparationAdapter(index).Build(session.Freeze());
            var guard = config.Spawns[0].Unit;
            var attributes = guard.AttributeDefinition!;
            config.Spawns[0] = config.Spawns[0] with { Unit = guard with {
                Behavior = guard.Behavior with { Stationary = true, DisableBasicAttacks = true },
                AttributeDefinition = attributes with { Attributes = attributes.Attributes.Select(value => value.Attribute switch {
                    CombatAttribute.StartingMana => value with { BaseValue = 80 },
                    CombatAttribute.ManaPerSecond or CombatAttribute.ManaPerAttack or CombatAttribute.ManaPerDamageRatio => value with { BaseValue = 0 },
                    _ => value }).ToImmutableArray() } } };

            var screen = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
            AddChild(screen); await Frame();
            var layer = screen.GetNode<RangedAttackLayer>("%RangedAttackLayer");
            var player = layer.GetNode<VfxPlayer>("Player");
            CheckGroundPlane();
            CheckLifecycle(config, layer, player);
            screen.StartBattle(package.Content, config, "HC03 铁壁挑衅 · 范围与受控标记");
            screen.SetLabControlsVisible(true);
            bool sawCast = false, sawMarks = false, expired = false;
            var marks = new Dictionary<string, ulong>();
            for (int frame = 0; frame < 390; frame++)
            {
                await Frame();
                var states = screen.ReadRuntimeUnits();
                foreach (var instance in player.GetChildren().OfType<VfxInstance>())
                {
                    if (instance.HasNode("GroundPlane"))
                    {
                        sawCast = true;
                        Require(Math.Abs(instance.Context.Radius - 2.5f) < .001f, "cast reads actual query radius");
                        Require(instance.Context.Source == instance.Context.Target, "range centered on cast owner");
                        if (before) instance.Visible = false;
                    }
                    if (before && instance.HasNode("HeadMark")) instance.Visible = false;
                }
                var affected = states.Where(unit => unit.Statuses.Any(status => status.Behavior == StatusBehaviorKind.Taunt)).ToArray();
                if (affected.Length > 0)
                {
                    sawMarks = true;
                    Require(affected.Length == 3 && layer.StatusVfxCount == 3, "inside targets marked; outside target not marked");
                    foreach (var unit in affected)
                    {
                        var key = $"status:{unit.RuntimeId}:taunted";
                        var mark = player.GetChildren().OfType<VfxInstance>().Single(instance => instance.HasNode("HeadMark") && instance.Context.Target == unit.Position);
                        var id = mark.GetInstanceId();
                        if (marks.TryGetValue(key, out var previous)) Require(previous == id, "refresh preserves instance");
                        marks[key] = id;
                        var view = screen.GetNode<Node2D>("%UnitsRoot").GetChildren().OfType<UnitContentRoot>().Single(view => view.RuntimeId == unit.RuntimeId);
                        Require(mark.Position.DistanceTo(view.Position + layer.BodyOffset * layer.UnitScale) < .01f,
                            "head mark follows visible moving unit, not fixed-tick jumps");
                    }
                }
                if (sawMarks && affected.Length == 0)
                {
                    expired = true;
                    Require(layer.StatusVfxCount == 0 && !player.GetChildren().OfType<VfxInstance>().Any(instance => instance.HasNode("HeadMark")), "expiration leaves no stale marks");
                }
                if (frame == 55)
                {
                    await Click(screen.GetNode<Button>("%PauseButton"));
                    Require(screen.IsPaused, "real pause click");
                    var paused = player.GetChildren().OfType<VfxInstance>().Where(instance => instance.HasNode("HeadMark"))
                        .Select(instance => (instance, Age: instance.Playback.Age, Position: instance.Position, Transform: instance.GetNode<Node2D>("HeadMark").Transform)).ToArray();
                    await Frame(5);
                    Require(paused.All(value => value.instance.Playback.Age == value.Age && value.instance.Position == value.Position &&
                        value.instance.GetNode<Node2D>("HeadMark").Transform == value.Transform), "pause freezes effect age and pose");
                    await Click(screen.GetNode<Button>("%PauseButton"));
                    Require(!screen.IsPaused, "real resume click");
                }
                if (capture is not null && DisplayServer.GetName() != "headless")
                {
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    Require(GetViewport().GetTexture().GetImage().SavePng(Path.Combine(capture, $"{frame:D4}.png")) == Error.Ok, "capture");
                }
                Require(screen.LastRuntimeFailure.Length == 0, screen.LastRuntimeFailure);
            }
            Require(sawCast && sawMarks && expired, "cast, status and expiration all rendered");
            screen.StopBattle();
            Require(player.ActiveCount == 0 && layer.StatusVfxCount == 0, "teardown cleanup");
            screen.QueueFree(); await Frame();
            GD.Print($"DESIGNED_TAUNT_VFX_OK before={before} area inside/outside follow refresh expiry source-death recipient-death pause reduced-motion cleanup");
            GetTree().Quit();
        }
        catch (Exception exception) { GD.PrintErr("DESIGNED_TAUNT_VFX_FAILED: " + exception); GetTree().Quit(1); }
    }

    private static void CheckLifecycle(BattleConfig config, RangedAttackLayer layer, VfxPlayer player)
    {
        using var battle = new BattleSimulation(config);
        battle.Step();
        var cast = battle.PendingEvents.Single(fact => fact.Vfx?.EffectId == "taunt_call");
        Require(cast.Vfx!.Radius == 2.5f && cast.Origin == cast.Position, "one exact-range cue per cast");
        layer.SetClock(true, 1);
        layer.SynchronizeUnits(battle.Units);
        Require(layer.StatusVfxCount == 3, "actual status snapshot drives markers");
        player.ReducedMotion = true;
        player.Advance(.3f);
        var mark = player.GetChildren().OfType<VfxInstance>().First(instance => instance.HasNode("HeadMark"));
        var pose = mark.GetNode<Node2D>("HeadMark").Transform;
        player.Advance(.7f);
        Require(mark.GetNode<Node2D>("HeadMark").Transform == pose, "reduced motion preserves fixed readable mark");
        player.ReducedMotion = false;
        var target = battle.Units.First(unit => unit.Statuses.Any(status => status.Behavior == StatusBehaviorKind.Taunt));
        var health = target.Health;
        target.Health = 0; layer.SynchronizeUnits(battle.Units);
        Require(layer.StatusVfxCount == 2, "recipient death clears its marker");
        target.Health = health;
        layer.SynchronizeUnits(battle.Units);
        Require(layer.StatusVfxCount == 3, "snapshot reconstruction restores current state");
        var source = battle.Units.Single(unit => unit.Team == 0);
        source.Health = 0; layer.SynchronizeUnits(battle.Units);
        Require(layer.StatusVfxCount == 0, "invalid taunt source clears force-target markers");
        layer.Clear();
    }

    private void CheckGroundPlane()
    {
        var definition = GD.Load<VfxDefinition>("res://content/vfx/taunt_call.tres");
        var effect = definition.Scene.Instantiate<VfxInstance>();
        effect.Bind(definition, new(new(2,3), new(7,1), 2.5f));
        AddChild(effect);
        var stage = new SkewStage();
        effect.Advance(.15f, stage, false);
        var plane = effect.GetNode<Node2D>("GroundPlane");
        Require((plane.GlobalTransform * Vector2.Zero).DistanceTo(stage.Project(new(2,3), true)) < .01f, "cast source ground center");
        Require((plane.GlobalTransform * new Vector2(128,0)).DistanceTo(stage.Project(new(4.5f,3), true)) < .01f, "exact projected x radius");
        Require((plane.GlobalTransform * new Vector2(0,128)).DistanceTo(stage.Project(new(2,5.5f), true)) < .01f, "exact projected y radius");
        effect.Free();
    }
    private sealed class SkewStage : IVfxStage
    {
        public float UnitScale => 1;
        public float RadiusPixels(float radius) => 100 * radius;
        public Vector2 Project(Vector2 point, bool ground) => new(60 + point.X * 100 + point.Y * 20, 70 + point.Y * 65 + (ground ? 0 : -22));
    }
    private async Task Click(Control control)
    {
        var point = control.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true); await Frame();
        GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left }, true); await Frame();
        GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false }, true); await Frame(2);
    }
    private async Task Frame(int count = 1) { for (int i=0; i<count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
