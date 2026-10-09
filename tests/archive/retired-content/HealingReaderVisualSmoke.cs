using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Presentation;
using TowerAutobattler.Vfx;

// Production scenes and input paths with private starting state; never opens a player save.
public partial class HealingReaderVisualSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            GetWindow().Size = new(1440, 900);
            var args = OS.GetCmdlineUserArgs();
            var capture = args.FirstOrDefault(arg => arg.StartsWith("--capture="))?[10..];
            var before = args.Contains("--before");
            var traceAnimation = args.Contains("--trace-animation");
            if (capture is not null) Directory.CreateDirectory(capture);
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var index = new BattleLabContentIndex(package);
            var session = new BattleLabSession(index, 6, 1816, BattleLabPlacementMode.FreeExperiment, "rule_clear");
            foreach (var (id, side, x, y) in new[] {
                ("hero_hc18_healing_reader", 0, 2, 2), ("soldier_dummy_static", 0, 5, 1),
                ("soldier_dummy_static", 0, 5, 3), ("soldier_dummy_static", 1, 9, 4) })
                Require(session.AddAndPlace(id, (BattleLabSide)side, new(x, y)).Succeeded, "place private fixture");
            var config = new BattleLabPreparationAdapter(index).Build(session.Freeze());
            var hero = config.Spawns[0].Unit;
            config.Spawns[0] = config.Spawns[0] with { Unit = hero with {
                AttributeDefinition = hero.AttributeDefinition! with {
                    Attributes = hero.AttributeDefinition!.Attributes.Select(value => value.Attribute switch {
                        CombatAttribute.StartingMana => value with { BaseValue = 60 },
                        CombatAttribute.ManaPerSecond or CombatAttribute.ManaPerAttack or CombatAttribute.ManaPerDamageRatio => value with { BaseValue = 0 },
                        _ => value }).ToImmutableArray() } } };
            for (var i = 1; i <= 2; i++) config.Spawns[i] = config.Spawns[i] with { HealthRatio = .15f };
            var screen = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
            AddChild(screen); await Frame();
            var layer = screen.GetNode<RangedAttackLayer>("%RangedAttackLayer");
            var player = layer.GetNode<VfxPlayer>("Player");
            screen.StartBattle(package.Content, config, "济世医师 · 贴身扫光与双目标治疗");
            screen.SetLabControlsVisible(true);
            var sawCast = false;
            var sawTwoRecipients = false;
            var pausedChecked = false;
            var casterId = screen.ReadRuntimeUnits().Single(unit => unit.ContentId == "hero_hc18_healing_reader").RuntimeId;
            using var traceBattle = new BattleSimulation(config);
            var casterView = screen.GetNode<Node2D>("%UnitsRoot").GetChildren().OfType<UnitContentRoot>().Single(unit => unit.RuntimeId == casterId);
            var animation = casterView.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
            var sprite = animation.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
            var previousPose = "";
            var castFacts = 0;
            var basicHealFacts = 0;
            var skillHealFacts = 0;
            var attackStarts = 0;
            var previousCue = "idle";
            var previousFrame = 0;
            for (var frame = 0; frame < 210; frame++)
            {
                await Frame();
                while (traceBattle.TickIndex < screen.TickIndex)
                {
                    traceBattle.Step();
                    foreach (var fact in traceBattle.DrainEvents().Where(fact => fact.SourceRuntimeId == casterId))
                    {
                        if (traceAnimation)
                            GD.Print($"HEALER_EVENT frame={frame} tick={fact.Tick} type={fact.Type} value={fact.Value} cue={fact.Cue}");
                        if (fact.Type == "ability")
                        {
                            castFacts++;
                            Require(animation.ActiveLogicalCue == "skill_cast" && animation.ActiveCue == "idle",
                                "active skill uses neutral caster pose instead of repeating the basic-heal clip");
                        }
                        if (fact.Type == "heal" && fact.Value == 70) skillHealFacts++;
                        if (fact.Type == "heal" && fact.Value == 16)
                        {
                            basicHealFacts++;
                            Require(animation.ActiveLogicalCue == "attack" && animation.ActiveCue == "attack" && sprite.Frame == 0,
                                "basic healing starts its own clip at the actual event, without delayed replay");
                        }
                    }
                }
                Require(animation.PendingCue == "", "healer never queues an obsolete action after its neutral skill pose");
                if (animation.ActiveCue == "attack" && (previousCue != "attack" || sprite.Frame < previousFrame)) attackStarts++;
                previousCue = animation.ActiveCue;
                previousFrame = sprite.Frame;
                if (traceAnimation)
                {
                    var pose = $"{animation.ActiveLogicalCue}/{animation.ActiveCue}/{sprite.Frame}/{animation.PendingCue}";
                    if (pose != previousPose) GD.Print($"HEALER_POSE frame={frame} tick={screen.TickIndex} pose={pose}");
                    previousPose = pose;
                }
                var casts = player.GetChildren().OfType<VfxInstance>().Where(instance => instance.SceneFilePath == "res://scenes/vfx/cast_mend.tscn").ToArray();
                if (casts.Length > 0)
                {
                    sawCast = true;
                    Require(casts.Length == 1 && layer.CastVfxCount == 1, "one source effect for the multi-target cast");
                    var view = screen.GetNode<Node2D>("%UnitsRoot").GetChildren().OfType<UnitContentRoot>().Single(unit => unit.RuntimeId == casterId);
                    Require(casts[0].Context.Source == casts[0].Context.Target &&
                        casts[0].Position.DistanceTo(view.Position + layer.BodyOffset * layer.UnitScale) < .01f, "source effect attaches to caster body");
                    if (before) casts[0].Visible = false;
                    var heals = player.GetChildren().OfType<VfxInstance>().Where(instance => instance.SceneFilePath == "res://scenes/vfx/heal.tscn").ToArray();
                    sawTwoRecipients |= heals.Select(instance => instance.Context.Target).Distinct().Count() == 2;
                    // Keep comparison capture at normal speed; verify actual pause separately below.
                }
                if (capture is not null && frame % 2 == 0 && DisplayServer.GetName() != "headless")
                {
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    Require(GetViewport().GetTexture().GetImage().SavePng(Path.Combine(capture, $"{frame / 2:D4}.png")) == Error.Ok, "capture");
                }
                Require(screen.LastRuntimeFailure.Length == 0, screen.LastRuntimeFailure);
            }
            Require(castFacts == 1 && skillHealFacts == 2 && basicHealFacts >= 2 && attackStarts == basicHealFacts,
                "one skill, two 70-point heals, and exactly one ordinary action for each 16-point heal");
            Require(sawCast && sawTwoRecipients && layer.CastVfxCount == 0, "caster and both targets render; transient source effect expires");
            screen.StartBattle(package.Content, config, "施法暂停检查");
            screen.SetLabControlsVisible(true);
            for (var frame = 0; frame < 120 && !pausedChecked; frame++)
            {
                await Frame();
                if (layer.CastVfxCount == 0) continue;
                await Click(screen.GetNode<Button>("%PauseButton"));
                Require(screen.IsPaused, "actual pause input");
                var instance = player.GetChildren().OfType<VfxInstance>().Single(effect => effect.SceneFilePath == "res://scenes/vfx/cast_mend.tscn");
                var age = instance.Playback.Age;
                var pose = instance.GetNode<Sprite2D>("BodyLight").Transform;
                var pausedAnimation = screen.GetNode<Node2D>("%UnitsRoot").GetChildren().OfType<UnitContentRoot>()
                    .Single(unit => unit.RuntimeId == casterId).GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
                var pausedSprite = pausedAnimation.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
                var pausedFrame = pausedSprite.Frame;
                var pausedProgress = pausedSprite.FrameProgress;
                await Frame(8);
                Require(instance.Playback.Age == age && instance.GetNode<Sprite2D>("BodyLight").Transform == pose, "pause freezes clock and glow");
                Require(pausedSprite.Frame == pausedFrame && pausedSprite.FrameProgress == pausedProgress, "pause freezes neutral cast pose");
                await Click(screen.GetNode<Button>("%SpeedButton"));
                Require(screen.SpeedScale == 2, "actual speed input");
                await Click(screen.GetNode<Button>("%PauseButton"));
                Require(!screen.IsPaused, "actual resume input");
                await Frame(50);
                Require(layer.CastVfxCount == 0, "accelerated effect expires");
                Require(pausedAnimation.PendingCue == "", "speed changes and resume leave no delayed cast");
                pausedChecked = true;
            }
            Require(pausedChecked, "pause test reached cast");
            screen.StopBattle();
            Require(player.ActiveCount == 0 && layer.CastVfxCount == 0, "replacement and teardown clear source effects");
            CheckOwnedLifetime(layer, player);
            CheckNeutralPoseLifecycle();
            screen.QueueFree(); await Frame();
            // Fixed-FPS captures can run faster than wall time; let the audio thread
            // release its stopped Ogg playback before the fixture shuts the engine down.
            var audioDeadline = Time.GetTicksMsec() + 350;
            while (Time.GetTicksMsec() < audioDeadline) await Frame();
            GD.Print($"HEALING_READER_VISUAL_OK before={before} casts={castFacts} skill-heals={skillHealFacts} basic-heals={basicHealFacts} attack-starts={attackStarts} neutral-cast no-replay pause speed resume body-follow defeat reduced-motion cleanup");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("HEALING_READER_VISUAL_FAILED " + error); GetTree().Quit(1); }
    }

    private void CheckNeutralPoseLifecycle()
    {
        var unit = GD.Load<PackedScene>("res://content/heroes/hero_hc18_healing_reader.tscn").Instantiate<UnitContentRoot>();
        AddChild(unit);
        var animation = unit.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
        animation.PlayCue("attack");
        animation.PlayCue("attack");
        animation.PlayCue("skill_cast");
        Require(animation.ActiveCue == "idle" && animation.PendingCue == "" && animation.PlaybackSpeedScale == 1,
            "neutral skill replaces stale basic action and keeps neutral clip at authored speed");
        animation.PlayCue("attack");
        Require(animation.ActiveLogicalCue == "attack" && animation.PendingCue == "", "next basic action immediately replaces neutral skill");
        animation._Process(1);
        Require(animation.ActiveLogicalCue == "idle", "completed basic heal cannot replay a stale skill");
        animation.PlayCue("skill_cast");
        animation.PlayCue("defeated");
        animation.PlayCue("attack");
        Require(animation.IsTerminal && animation.PendingCue == "", "defeat cancels neutral cast without pending revival");
        animation.ResetPresentation();
        Require(animation.ActiveLogicalCue == "idle" && animation.PendingCue == "", "new presentation clears cast state");
        unit.QueueFree();
    }

    private static void CheckOwnedLifetime(RangedAttackLayer layer, VfxPlayer player)
    {
        layer.SetClock(true, 1);
        player.ReducedMotion = true;
        var body = new Vector2(100, 100);
        layer.BindUnitBodyPositions(_ => body);
        var fact = new BattleEvent(1, "ability", "caster", "recipient", 60, new(2, 2), "skill_cast", new(2, 2));
        layer.Present([fact, fact], false);
        Require(player.ActiveCount == 1, "duplicate fact does not multiply source effect");
        player.Advance(.1f);
        var instance = player.GetChildren().OfType<VfxInstance>().Single();
        Require(instance.GetNode<Sprite2D>("BodyLight").Visible, "reduced motion retains caster feedback");
        body = new(130, 80);
        layer._Process(0); player.Advance(0);
        Require(instance.Position == body, "source follows injected body position");
        layer.Present([new BattleEvent(2, "defeated", "enemy", "caster", 0, new(2, 2), "death")], false);
        Require(player.ActiveCount == 0 && layer.CastVfxCount == 0, "source defeat immediately removes attached effect");
        layer.Clear();
    }
    private async Task Click(Control control)
    {
        var point = control.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true); await Frame();
        GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left }, true); await Frame();
        GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false }, true); await Frame(2);
    }
    private async Task Frame(int count = 1) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
