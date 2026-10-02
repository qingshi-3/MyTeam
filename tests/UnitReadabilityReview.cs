using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Battle;
using TowerAutobattler.Abilities;
using TowerAutobattler.Components;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Presentation;
using TowerAutobattler.Project;

// Same production battle, content, camera and speed; input stays inside a 960x540 viewport.
public partial class UnitReadabilityReview : Node
{
    private UiInputStage _stage = null!;
    private string _output = "";
    public override async void _Ready()
    {
        try
        {
            _output = ProjectSettings.GlobalizePath("res://.godot/unit-readability-review");
            Directory.CreateDirectory(_output);
            _stage = new UiInputStage(this, true);
            _stage.Resize(new(960, 540));
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var store = new BattleLabPresetStore(GD.Load<BattleLabPresetCatalog>("res://content/battle-lab/battle_lab_presets.tres"));
            var preset = store.BuiltIns.Single(p => p.Key == "默认配置");
            var onlyMode = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--mode="))?[7..];
            var staticOnly = OS.GetCmdlineUserArgs().Contains("--static-only");
            if (staticOnly) onlyMode = "feedback";
            foreach (var mode in new[] { "original", "outline", "shadow", "feedback" }.Where(m => onlyMode is null || onlyMode == m))
            {
                var directory = Path.Combine(_output, mode);
                Directory.CreateDirectory(directory);
                var config = new BattleLabPreparationAdapter(new BattleLabContentIndex(package))
                    .Build(BattleLabPresetStore.ToSnapshot(preset.Value));
                var screen = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
                _stage.AddChild(screen);
                await Frame();
                screen.StartBattle(package.Content, config, "单位轮廓与命中对照");
                screen.SetLabControlsVisible(true);
                screen.SetSpeed(1);
                var actors = screen.GetNode<Node2D>("%UnitsRoot").GetChildren().OfType<UnitContentRoot>().ToArray();
                Require(actors.Length >= 2, "production units loaded");
                foreach (var actor in actors)
                {
                    actor.Readability!.Configure(mode == "original" ? 0 : 1,
                        mode is "shadow" or "feedback", mode == "feedback");
                    if (mode == "original")
                        actor.GetNode<AnimatedSprite2D>("VisualRoot/UnitAnimationComponent/AnimatedSprite2D").Material = null;
                }
                screen.SetPaused(true);
                if (mode == "feedback") await CheckIsolationAndLifecycle(screen, actors);
                int hitSignals = 0;
                screen.GetNode<RangedAttackLayer>("%RangedAttackLayer").UnitHitPresented += id => hitSignals++;
                screen.SetPaused(false);
                if (staticOnly)
                {
                    for (int i = 0; i < 180 && screen.TickIndex < 24; i++) await Frame();
                    screen.SetPaused(true);
                    Require(screen.TickIndex >= 24, "static comparison reaches real combat");
                    var sprites = actors.Select(a => a.GetNode<AnimatedSprite2D>("VisualRoot/UnitAnimationComponent/AnimatedSprite2D")).ToArray();
                    var materials = sprites.Select(s => s.Material).ToArray();
                    foreach (var variant in new[] { "original", "outline", "shadow" })
                    {
                        for (int i = 0; i < actors.Length; i++)
                        {
                            sprites[i].Material = variant == "original" ? null : materials[i];
                            actors[i].Readability!.Configure(variant == "original" ? 0 : 1, variant == "shadow", false);
                        }
                        await Frame();
                        _stage.Viewport.GetTexture().GetImage().SavePng(Path.Combine(_output, $"static-{variant}.png"));
                    }
                    GD.Print("UNIT_READABILITY_STATIC_OK same paused production battle tick=" + screen.TickIndex);
                    screen.StopBattle();
                    screen.Free();
                    GetTree().Quit();
                    return;
                }
                int hitFrames = 0;
                bool pauseChecked = false;
                for (int frame = 0; frame < 210; frame++)
                {
                    await Frame();
                    Require(screen.LastRuntimeFailure.Length == 0, screen.LastRuntimeFailure);
                    if (actors.Any(a => a.Readability!.HitAmount > 0)) hitFrames++;
                    _stage.Viewport.GetTexture().GetImage().SavePng(Path.Combine(directory, $"{frame:0000}.png"));
                }
                // Input validation follows recording so all four clips retain identical timing.
                if (mode == "feedback")
                {
                    for (int i = 0; i < 120 && !actors.Any(a => a.Readability!.HitAmount > 0); i++) await Frame();
                    if (actors.Any(a => a.Readability!.HitAmount > 0))
                    {
                        await Click(screen.GetNode<Button>("%PauseButton"));
                        Require(screen.IsPaused, "real pause input");
                        var amounts = actors.Select(a => a.Readability!.HitAmount).ToArray();
                        var tick = screen.TickIndex;
                        for (int i = 0; i < 3; i++) await Frame();
                        Require(screen.TickIndex == tick && actors.Select(a => a.Readability!.HitAmount).SequenceEqual(amounts),
                            "pause freezes battle and hit feedback");
                        await Click(screen.GetNode<Button>("%PauseButton"));
                        Require(!screen.IsPaused, "real resume input");
                        pauseChecked = true;
                    }
                }
                GD.Print($"UNIT_READABILITY_COMBAT mode={mode} ticks={screen.TickIndex} signals={hitSignals} visible={hitFrames} paused={screen.IsPaused}");
                if (mode == "feedback") Require(hitFrames > 0 && pauseChecked, "actual combat facts produce visible feedback");
                var feedbackComponents = actors.Select(a => a.Readability!).ToArray();
                screen.StopBattle();
                Require(feedbackComponents.All(a => a.HitAmount == 0), "scope cleanup clears feedback");
                if (mode == "feedback") CheckProjectileHitTiming(screen.GetNode<RangedAttackLayer>("%RangedAttackLayer"));
                _stage.Viewport.RemoveChild(screen);
                screen.Free();
                GD.Print($"UNIT_READABILITY_REVIEW mode={mode} hit_frames={hitFrames} pause_input={pauseChecked}");
            }
            GD.Print("UNIT_READABILITY_REVIEW_OK 960x540 production battle; modes=" +
                (onlyMode ?? "original,outline,shadow,feedback") + "; cleanup");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("UNIT_READABILITY_REVIEW_FAILED " + error); GetTree().Quit(1); }
    }

    private async Task CheckIsolationAndLifecycle(BattleScreenController screen, UnitContentRoot[] actors)
    {
        screen.SetPaused(true);
        var a = actors[0]; var b = actors[1];
        var visual = a.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
        var sprite = visual.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        Require(sprite.Material != b.GetNode<AnimatedSprite2D>("VisualRoot/UnitAnimationComponent/AnimatedSprite2D").Material,
            "unit instances have independent material state");
        var position = a.Position; var pose = visual.ActiveLogicalCue; var frame = sprite.Frame;
        a.PresentHitFeedback();
        Require(a.Readability!.HitAmount > 0 && b.Readability!.HitAmount == 0, "hit only affects recipient");
        Require(a.Position == position && visual.ActiveLogicalCue == pose && sprite.Frame == frame,
            "hit never changes authority position or animation pose");
        a.StepAttackPresentation(.04f);
        var amount = a.Readability.HitAmount;
        a.PresentHitFeedback();
        Require(a.Readability.HitAmount == amount, "rapid hits do not restart flash");
        a.StepAttackPresentation(.2f);
        Require(a.Readability.HitAmount == 0, "single step expires feedback");
        a.Readability.ReducedMotion = true;
        a.PresentHitFeedback();
        Require(a.Readability.HitAmount <= .1f, "reduced feedback intensity");
        a.Readability.ReducedMotion = false;
        a.Readability.ResetFeedback();
        await Task.CompletedTask;
    }

    private async Task Click(Button button)
    {
        var point = button.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await Frame();
        foreach (var pressed in new[] { true, false })
        {
            _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, ButtonMask = pressed ? MouseButtonMask.Left : 0, Pressed = pressed });
            await Frame();
        }
    }

    private static void CheckProjectileHitTiming(RangedAttackLayer layer)
    {
        int signals = 0;
        void Hit(string id) { if (id == "hit-probe") signals++; }
        layer.UnitHitPresented += Hit;
        var start = Vector2.Zero; var end = new Vector2(0, 2);
        var contact = start.Lerp(end, .5f);
        var cue = new BattleLineCue("piercing_arrow_charge", "piercing_arrow", 0, 12, .18f, ChargedLineDelivery.Projectile);
        layer.Present([new(1, "projectile_spawn", "source-probe", "", 0, default, "", start, 91, end, Line: cue)], false);
        layer.Present([new(2, "damage", "source-probe", "hit-probe", 5, default, "hit", contact),
            new(2, "projectile_move", "source-probe", "", 0, default, "", end, 91),
            new(2, "projectile_impact", "source-probe", "hit-probe", 0, default, "", contact, 91)], false);
        Require(signals == 0, "damage flash waits with interpolated projectile contact");
        layer.AdvanceFlights(.025f);
        Require(signals == 0, "no flash before visible contact");
        layer.AdvanceFlights(.03f);
        Require(signals == 1, "contact emits exactly one hit feedback");
        layer.AdvanceFlights(.1f);
        Require(signals == 1, "contact feedback is not replayed");
        layer.Present([new(3, "damage", "source-probe", "hit-probe", 0, default, "hit", contact)], false);
        Require(signals == 1, "zero damage does not flash");
        layer.UnitHitPresented -= Hit;
        layer.Clear();
    }
    private async Task Frame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
    }
    private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
}
