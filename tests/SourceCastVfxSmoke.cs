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

// Isolated authored actors and the production player. No run state or saved player data.
public partial class SourceCastVfxSmoke : Node2D, IVfxStage
{
    public float UnitScale => 2;
    public Vector2 Project(Vector2 point, bool ground) => point;
    public float RadiusPixels(float radius) => radius * 64;
    private static readonly string[] Actors = ["BloodBefore", "BloodAfter", "MendBefore", "MendAfter"];
    private static readonly string[] Effects = ["spell_cast", "cast_blood", "spell_cast", "cast_mend"];

    public override async void _Ready()
    {
        try
        {
            GetWindow().Size = new(880, 370);
            GetWindow().ContentScaleSize = new(880, 370);
            var args = OS.GetCmdlineUserArgs();
            var capture = args.FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
            var reduced = args.Contains("--reduced");
            if (capture is not null) Directory.CreateDirectory(capture);
            if (args.Contains("--preview"))
            {
                await CheckPreview(capture);
                GD.Print("SOURCE_CAST_PREVIEW_OK real-input selection source-frames pause single-step speed replay cleanup");
                GetTree().Quit();
                return;
            }
            if (reduced) GetNode<ColorRect>("Backdrop").Color = new("b2b6b9");
            var player = GetNode<VfxPlayer>("Player");
            player.Bind(this);
            player.SetProcess(false);
            player.ReducedMotion = reduced;
            await CheckProductionFacts();
            if (args.Contains("--battle")) { GetTree().Quit(); return; }
            for (var i = 0; i < Actors.Length; i++)
                GetNode<UnitContentRoot>(Actors[i]).GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent").ResetPresentation();
            for (int frame = 0; frame < 100; frame++)
            {
                if (frame == 15)
                    for (int i = 0; i < Actors.Length; i++)
                    {
                        var unit = GetNode<UnitContentRoot>(Actors[i]);
                        unit.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent").PlayCue("skill_cast");
                        player.Play(Effects[i], Context(unit), Actors[i]);
                    }
                for (int i = 0; i < Actors.Length; i++)
                    player.UpdateContext(Actors[i], Context(GetNode<UnitContentRoot>(Actors[i])));
                player.Advance(1f / 60);
                await Frame();
                if (frame == 26) CheckSurface(player);
                if (capture is not null && frame % 2 == 0 && DisplayServer.GetName() != "headless")
                {
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    GetViewport().GetTexture().GetImage().SavePng(Path.Combine(capture, $"{frame / 2:D4}.png"));
                }
            }
            Require(player.ActiveCount == 0, "all four cast effects expire");
            CheckFollowingAndOwnership(player);
            GD.Print("SOURCE_CAST_VFX_OK production-facts blood/mend idle silhouette frame facing transform pause cancel independent-materials expiry");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            if (DisplayServer.GetName() != "headless")
                GetViewport().GetTexture().GetImage().SavePng("res://.godot/idle-cast-review/check-failure.png");
            GD.PrintErr("SOURCE_CAST_VFX_FAILED " + error); GetTree().Quit(1);
        }
    }

    private VfxContext Context(UnitContentRoot unit) => new(unit.BodyGlobalPosition, unit.BodyGlobalPosition,
        TargetDisplayPosition: ToLocal(unit.BodyGlobalPosition), Body: unit.CaptureBodyVisual(GlobalTransform.AffineInverse()));

    private void CheckSurface(VfxPlayer player)
    {
        foreach (var effect in player.GetChildren().OfType<VfxInstance>().Where(e => e.Context.Body is not null && e.HasNode("BodySurface")))
        {
            var surface = effect.GetNode<VfxBodySurfaceTrack>("BodySurface");
            var body = effect.Context.Body!;
            Require(surface.Visible && surface.Texture == body.Texture, "overlay uses current actor frame");
            Require(surface.GlobalTransform.IsEqualApprox(GlobalTransform * body.Transform), "overlay matches body transform");
        }
    }

    private void CheckFollowingAndOwnership(VfxPlayer player)
    {
        var unit = GetNode<UnitContentRoot>("BloodAfter");
        var animation = unit.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
        var sprite = animation.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        var originalMaterial = sprite.Material;
        player.Play("cast_blood", Context(unit), "follow");
        player.Advance(.12f);
        var first = player.GetChildren().OfType<VfxInstance>().Single();
        var surface = first.GetNode<VfxBodySurfaceTrack>("BodySurface");
        var shader = (ShaderMaterial)surface.Material;
        var age = shader.GetShaderParameter("age").AsDouble();
        player.Advance(0);
        Require(shader.GetShaderParameter("age").AsDouble() == age, "zero clock freezes overlay");
        unit.Position += new Vector2(13, -7);
        animation.FaceHorizontal(-1);
        sprite.Frame = Math.Min(1, sprite.SpriteFrames.GetFrameCount("idle") - 1);
        player.UpdateContext("follow", Context(unit));
        player.Advance(.02f);
        CheckSurface(player);
        Require(surface.FlipH == sprite.FlipH, "overlay follows facing");
        player.Play("cast_blood", Context(unit), "other");
        var second = player.GetChildren().OfType<VfxInstance>().Single(e => e != first);
        Require(second.GetNode<VfxBodySurfaceTrack>("BodySurface").Material != shader, "cast material is instance-owned");
        Require(sprite.Material == originalMaterial, "actor material is never replaced");
        player.End("follow", VfxEndReason.ScopeEnded);
        Require(player.ActiveCount == 1, "ending one cast preserves another");
        player.Clear();
        Require(player.ActiveCount == 0, "scope cleanup");
    }

    private async Task CheckProductionFacts()
    {
        var gate = await GamePackagePublisher.CreateReadyAsync(this,
            GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
        var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
        var index = new BattleLabContentIndex(package);
        var session = new BattleLabSession(index, 6, 1820, BattleLabPlacementMode.FreeExperiment, "rule_clear");
        foreach (var (id, side, x, y) in new[] {
            ("hero_hc20_blood_drummer", 0, 2, 2), ("hero_hc18_healing_reader", 0, 2, 3),
            ("soldier_dummy_static", 0, 3, 2), ("soldier_dummy_static", 1, 9, 4) })
            Require(session.AddAndPlace(id, (BattleLabSide)side, new(x,y)).Succeeded, "private cast fixture placement");
        var config = new BattleLabPreparationAdapter(index).Build(session.Freeze());
        for (int i = 0; i < 2; i++)
        {
            var hero = config.Spawns[i].Unit;
            config.Spawns[i] = config.Spawns[i] with { Unit = hero with { AttributeDefinition = hero.AttributeDefinition! with {
                Attributes = hero.AttributeDefinition!.Attributes.Select(v => v.Attribute switch {
                    CombatAttribute.StartingMana => v with { BaseValue = 60 },
                    CombatAttribute.ManaPerSecond or CombatAttribute.ManaPerAttack or CombatAttribute.ManaPerDamageRatio => v with { BaseValue = 0 },
                    _ => v }).ToImmutableArray() } } };
        }
        config.Spawns[2] = config.Spawns[2] with { HealthRatio = .2f };
        using var battle = new BattleSimulation(config);
        battle.Step();
        var casts = battle.DrainEvents().Where(e => e.Type == "ability").ToArray();
        Require(casts.Length == 2 && casts.Count(e => e.SourceVfx == "cast_blood") == 1 &&
            casts.Count(e => e.SourceVfx == "cast_mend") == 1, "each successful ability emits exactly one configured source effect");
        if (OS.GetCmdlineUserArgs().Contains("--battle")) await CheckBattle(package, config);
    }

    private void HideComparison()
    {
        foreach (var child in GetChildren().OfType<CanvasItem>()) child.Hide();
        GetWindow().Size = new(1440, 900);
        GetWindow().ContentScaleSize = new(1600, 900);
    }

    private async Task CheckBattle(CompiledGamePackage package, BattleConfig config)
    {
        HideComparison();
        var screen = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
        GetTree().Root.AddChild(screen); await Frame();
        screen.StartBattle(package.Content, config, "血鼓与济世医师 · 原待机配合施法特效");
        var layer = screen.GetNode<RangedAttackLayer>("%RangedAttackLayer");
        var player = layer.GetNode<VfxPlayer>("Player");
        var capture = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?[10..];
        var observed = new System.Collections.Generic.HashSet<string>();
        for (int frame = 0; frame < 120; frame++)
        {
            await Frame();
            foreach (var effect in player.GetChildren().OfType<VfxInstance>().Where(e => e.HasNode("BodySurface")))
            {
                observed.Add(effect.SceneFilePath);
                var body = effect.Context.Body ?? throw new InvalidOperationException("production caster has no body snapshot");
                var surface = effect.GetNode<VfxBodySurfaceTrack>("BodySurface");
                Require(surface.Texture == body.Texture && surface.GlobalTransform.IsEqualApprox(player.GlobalTransform * body.Transform),
                    "production overlay matches injected body frame and transform");
            }
            if (capture is not null && frame % 2 == 0 && DisplayServer.GetName() != "headless")
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng(Path.Combine(capture, $"{frame / 2:D4}.png"));
            }
            Require(screen.LastRuntimeFailure.Length == 0, screen.LastRuntimeFailure);
        }
        Require(observed.SetEquals(new[] { "res://scenes/vfx/cast_blood.tscn", "res://scenes/vfx/cast_mend.tscn" }), "both casts render through production screen");
        screen.StopBattle();
        Require(player.ActiveCount == 0 && layer.CastVfxCount == 0, "production screen clears cast instances");
        screen.QueueFree(); await Frame();
        GD.Print("SOURCE_CAST_BATTLE_OK blood/mend production-screen body-frame transform cleanup");
    }

    private async Task CheckPreview(string? capture)
    {
        HideComparison();
        await Frame();
        var preview = GD.Load<PackedScene>("res://scenes/app/VfxPreview.tscn").Instantiate<VfxPreviewController>();
        GetTree().Root.AddChild(preview); await Frame(); await Frame();
        var player = preview.GetNode<VfxPlayer>("%Player");
        var search = preview.GetNode<LineEdit>("%Search");
        var list = preview.GetNode<ItemList>("%Effects");
        var source = preview.GetNode<AnimatedSprite2D>("%SourceUnit");
        await Click(preview.GetNode<CheckButton>("%Loop"));
        foreach (var id in new[] { "cast_blood", "cast_mend" })
        {
            await Click(search);
            await KeyInput(Key.A, ctrl: true);
            await KeyInput(Key.Backspace);
            foreach (char c in id) await KeyInput((Key)c, c);
            Require(list.ItemCount == 1, "search finds unique source effect");
            await ClickAt(list.GetGlobalRect().Position + list.GetItemRect(0).GetCenter());
            Require(source.SpriteFrames == player.Catalog.Find(id).PreviewFrames && source.Animation == "idle", "preview uses correct original hero idle");
            if (!player.Paused) await Click(preview.GetNode<Button>("%Pause"));
            Require(player.Paused, "pause button receives input");
            await Click(preview.GetNode<Button>("%Replay"));
            await Click(preview.GetNode<Button>("%Step"));
            var effect = player.GetChildren().OfType<VfxInstance>().Single();
            var age = effect.Playback.Age;
            var pose = (source.Frame, source.FrameProgress);
            await Frame(); await Frame();
            Require(age == effect.Playback.Age && pose == (source.Frame, source.FrameProgress),
                $"pause freezes source and effect: paused={player.Paused}, age={age}->{effect.Playback.Age}, pose={pose}->{(source.Frame, source.FrameProgress)}");
            await Click(preview.GetNode<Button>("%Step"));
            Require(Mathf.IsEqualApprox(effect.Playback.Age - age, 1f / 30) && pose != (source.Frame, source.FrameProgress), "step advances both clocks once");
            var body = effect.GetNode<VfxBodySurfaceTrack>("BodySurface");
            Require(body.Visible && body.Texture == source.SpriteFrames.GetFrameTexture("idle", source.Frame) &&
                body.GlobalTransform.IsEqualApprox(source.GlobalTransform), "preview overlay remains registered to the stepped frame");
            if (capture is not null)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng(Path.Combine(capture, id + ".png"));
            }
        }
        var speed = preview.GetNode<HSlider>("%Speed");
        await Click(speed); await KeyInput(Key.End);
        Require(Mathf.IsEqualApprox(player.Speed, (float)speed.MaxValue), "keyboard changes playback speed");
        await Click(preview.GetNode<Button>("%Pause"));
        Require(!player.Paused, "resume input");
        for (int i = 0; i < 80; i++) await Frame();
        Require(player.ActiveCount == 0, "sped-up cast expires without repeating");
        await Click(preview.GetNode<Button>("%Replay"));
        Require(player.ActiveCount == 1, "replay restores exactly one cast");
        preview.QueueFree(); await Frame();
    }

    private async Task Click(Control control) => await ClickAt(control.GetGlobalRect().GetCenter());
    private async Task ClickAt(Vector2 point)
    {
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = true }, true);
        await Frame();
        GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false }, true);
        await Frame(); await Frame();
    }
    private async Task KeyInput(Key key, uint unicode = 0, bool ctrl = false)
    {
        GetViewport().PushInput(new InputEventKey { Keycode = key, Unicode = unicode, CtrlPressed = ctrl, Pressed = true }, true);
        GetViewport().PushInput(new InputEventKey { Keycode = key, Unicode = unicode, CtrlPressed = ctrl, Pressed = false }, true);
        await Frame();
    }
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
