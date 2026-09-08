using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Vfx;

// Isolated render/clock check: never constructs a battle or accesses a save.
public partial class VfxShaderPreview : Node2D, IVfxStage
{
    public float UnitScale => 1.8f;
    public Vector2 Project(Vector2 point, bool ground) => point;
    public float RadiusPixels(float radius) => radius * 80;
    public override async void _Ready()
    {
        try
        {
            var args = OS.GetCmdlineUserArgs();
            if (args.Contains("--preview-loop"))
            {
                CheckPreviewLoop();
                GD.Print("VFX_PREVIEW_LOOP_OK: sustained identity/clock across 4s and 8s, pause, explicit replay/end, transient replay.");
                GetTree().Quit();
                return;
            }
            if (args.Length > 1 && args[0] == "--motion")
            {
                await RecordPreview(args[1]);
                GetTree().Quit();
                return;
            }
            var player = GetNode<VfxPlayer>("Player");
            player.Bind(this);
            player.Paused = true;
            CheckRend(player);
            CheckShockAndOrbit(player);
            CheckFirefall(player);
            float[] times = [.05f, .15f, .27f, .48f];
            string[] ids = ["rend", "burst", "shield", "heal"];
            for (int row = 0; row < ids.Length; row++)
            for (int column = 0; column < times.Length; column++)
            {
                var definition = player.Catalog.Find(ids[row]);
                var instance = definition.Scene.Instantiate<VfxInstance>();
                // Same seed across columns makes the particle trajectory comparable.
                if (ids[row] == "heal") instance.GetChild<VfxParticleTrack>(0).Seed = 42;
                foreach (var emitter in instance.GetChildren().OfType<VfxBallisticTrack>()) emitter.Seed = 42;
                AddChild(instance);
                instance.Bind(definition, new(Vector2.Zero, new(150 + column * 300, 115 + row * 215)));
                var sampleTime = ids[row] == "shield" ? column * .65f + .2f :
                    ids[row] == "heal" ? times[column] * 2 : times[column];
                instance.Advance(sampleTime, this, false);
                if (ids[row] == "rend" && column == 1)
                {
                    var core = (ShaderMaterial)instance.GetNode<VfxRibbonTrack>("FirstSweep").Material;
                    var cross = (ShaderMaterial)instance.GetNode<VfxRibbonTrack>("SecondSweep").Material;
                    if ((float)core.GetShaderParameter("age") <= (float)cross.GetShaderParameter("age"))
                        throw new Exception("Slash layers lost their local delay.");
                }
            }
            var key = player.Play("shield", new(Vector2.Zero, new(-400, -400)), "owned");
            player.Play("shield", new(Vector2.Zero, new(-400, -400)), key);
            if (player.ActiveCount != 1) throw new Exception("Persistent ensure duplicated instance.");
            var first = player.GetChild<VfxInstance>(0).GetChild<VfxSpriteTrack>(0);
            var material = (ShaderMaterial)first.Material;
            player.Advance(.2f);
            var before = (float)material.GetShaderParameter("age");
            player._Process(1);
            if ((float)material.GetShaderParameter("age") != before) throw new Exception("Paused shader clock advanced.");
            player.Play("shield", new(Vector2.Zero, new(-400, -400)), "second");
            var second = player.GetChild<VfxInstance>(1).GetChild<VfxSpriteTrack>(0);
            if (ReferenceEquals(first.Material, second.Material)) throw new Exception("Material state shared.");
            player.End(key, VfxEndReason.Depleted);
            player.End("second", VfxEndReason.Completed);
            player.Advance(.3f);
            if (player.ActiveCount != 0) throw new Exception("Release failed to clean instances.");
            player.Play("heal", new(Vector2.Zero, new(-400, -400)), "healing");
            var healing = player.GetChild<VfxInstance>(0).GetChild<VfxParticleTrack>(0);
            var plus = healing.GetChild<Sprite2D>(0);
            player.Advance(.2f);
            var particlePosition = plus.Position;
            player._Process(1);
            if (plus.Position != particlePosition) throw new Exception("Paused particle moved.");
            player.Advance(.1f);
            if (plus.Position.Y >= particlePosition.Y) throw new Exception("Healing particle did not rise.");
            if (healing.GetChildren().Cast<Sprite2D>().Select(s => s.Scale.X).Distinct().Count() < 2)
                throw new Exception("Healing particles lost size variation.");
            player.Advance(2);
            if (player.ActiveCount != 0) throw new Exception("Healing emitter was not cleaned up.");
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var path = OS.GetCmdlineUserArgs().FirstOrDefault();
            if (path is not null && GetViewport().GetTexture().GetImage().SavePng(path) != Error.Ok)
                throw new Exception("Screenshot save failed.");
            GD.Print("VFX_SHADER_PREVIEW_OK: sampled render, pause, instance materials, idempotent ensure, release cleanup.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr(error); GetTree().Quit(1); }
    }

    private void CheckRend(VfxPlayer player)
    {
        var definition = player.Catalog.Find("rend");
        VfxInstance Create()
        {
            var instance = definition.Scene.Instantiate<VfxInstance>();
            foreach (var emitter in instance.GetChildren().OfType<VfxBallisticTrack>()) emitter.Seed = 42;
            AddChild(instance);
            instance.Bind(definition, new(Vector2.Zero, new(-500, -500)));
            return instance;
        }
        var stepped = Create();
        var direct = Create();
        var first = stepped.GetNode<VfxRibbonTrack>("FirstSweep");
        var second = stepped.GetNode<VfxRibbonTrack>("SecondSweep");
        stepped.Advance(.05f, this, false);
        if (!first.Visible || second.Visible) throw new Exception("Second stroke appeared before its cue.");
        stepped.Advance(.10f, this, false);
        direct.Advance(.15f, this, false);
        if (!first.Visible || !second.Visible) throw new Exception("Strokes no longer overlap to form the cross.");
        if (first.Material.GetInstanceId() == direct.GetNode<VfxRibbonTrack>("FirstSweep").Material.GetInstanceId())
            throw new Exception("Ribbon instances share mutable material state.");
        var fragment = stepped.GetNode<VfxBallisticTrack>("FirstFragments").GetChild<Sprite2D>(0);
        var other = direct.GetNode<VfxBallisticTrack>("FirstFragments").GetChild<Sprite2D>(0);
        if (!fragment.Visible || !fragment.Position.IsEqualApprox(other.Position))
            throw new Exception("Fragment movement depends on frame subdivision.");
        var before = fragment.Position;
        stepped.Advance(0, this, false);
        if (fragment.Position != before) throw new Exception("Paused fragment moved.");
        stepped.Advance(.28f, this, false);
        if (!first.Visible || !second.Visible) throw new Exception("First stroke faded before the extended overlap ended.");
        stepped.Advance(.2f, this, false);
        if (!first.Visible || !second.Visible) throw new Exception("Cross disappeared before the longer outro.");
        stepped.Advance(.42f, this, false);
        if (first.Visible || second.Visible) throw new Exception("Stroke tails remained after their fade completed.");
        direct.Advance(0, this, true);
        before = other.Position;
        direct.Advance(.025f, this, true);
        if (other.Position != before || (float)((ShaderMaterial)direct.GetNode<VfxRibbonTrack>("FirstSweep").Material)
                .GetShaderParameter("motion_amount") != 0)
            throw new Exception("Reduced motion still moves the ribbon or fragments.");
        stepped.Free(); direct.Free();
        player.Play("rend", new(Vector2.Zero, new(-500, -500)));
        player.Advance(1.2f);
        if (player.ActiveCount != 0) throw new Exception("Rend retained layers after completion.");
    }

    // Captures the real shared preview, using its scene and player. This is
    // rendered playback evidence, not a substitute for real button input QA.
    private async Task RecordPreview(string directory)
    {
        System.IO.Directory.CreateDirectory(directory);
        var preview = GD.Load<PackedScene>("res://scenes/app/VfxPreview.tscn").Instantiate<VfxPreviewController>();
        var canvas = new CanvasLayer();
        AddChild(canvas);
        canvas.AddChild(preview);
        preview.SetProcess(false);
        var player = preview.GetNode<VfxPlayer>("%Player");
        bool continuous = OS.GetCmdlineUserArgs().Contains("--continuous");
        player.SetProcess(false);
        player.Paused = !continuous;
        player.ReducedMotion = OS.GetCmdlineUserArgs().Contains("--reduced");
        preview.GetNode<CheckButton>("%Reduced").SetPressedNoSignal(player.ReducedMotion);
        if (OS.GetCmdlineUserArgs().Contains("--light"))
        {
            preview.GetNode<ColorRect>("%Backdrop").Color = new Color("c9c6bf");
            preview.GetNode<CheckButton>("%Light").SetPressedNoSignal(true);
        }
        var selectedId = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--effect="))?[9..] ?? "rend";
        int selectedIndex = player.Catalog.Effects.Select(effect => effect.StableId).ToList().IndexOf(selectedId);
        if (selectedIndex < 0) throw new Exception("Unknown capture effect.");
        var list = preview.GetNode<ItemList>("%Effects");
        list.Select(selectedIndex);
        // Programmatic harness signal, explicitly not a real-input QA claim.
        list.EmitSignal(ItemList.SignalName.ItemSelected, selectedIndex);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        int frames = continuous ? 540 : selectedId == "shield" ? 300 : selectedId == "firefall" ? 168 : selectedId == "burst" ? 96 : 72;
        for (int frame = 0; frame < frames; frame++)
        {
            // Container layout settles after Ready; update the real reference
            // unit projection without advancing the preview's clock or looping.
            preview._Process(continuous && frame > 0 ? 1.0 / 60 : 0);
            if (frame > 0) player.Advance(1f / 60);
            if (!continuous && selectedId == "shield" && frame == 120) player.Impact("preview");
            if (!continuous && selectedId == "shield" && frame == 240)
                player.End("preview", OS.GetCmdlineUserArgs().Contains("--normal-end") ? VfxEndReason.Completed : VfxEndReason.Depleted);
            preview.GetNode<Label>("%Clock").Text = $"{frame / 60f:0.00} 秒 · 1.00× · {player.ActiveCount} 实例";
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var path = System.IO.Path.Combine(directory, $"frame-{frame:D3}.png");
            if (GetViewport().GetTexture().GetImage().SavePng(path) != Error.Ok)
                throw new Exception("Motion frame save failed.");
        }
        GD.Print($"VFX_MOTION_CAPTURE_OK: {selectedId}, {frames} frames at 60 samples/second, shared preview scene, no battle/save.");
    }

    private void CheckShockAndOrbit(VfxPlayer player)
    {
        player.Play("burst", new(Vector2.Zero, new(-500, -500)), "shock");
        var burst = player.GetChild<VfxInstance>(0);
        var spark = burst.GetNode<VfxBallisticTrack>("RadialSparks").GetChild<Sprite2D>(0);
        player.Advance(.1f);
        var before = spark.Position;
        player._Process(.1);
        if (spark.Position != before) throw new Exception("Paused shock particle moved.");
        player.Advance(.12f);
        if (spark.Position.Length() <= before.Length() || spark.Position.Normalized().Dot(before.Normalized()) < .999f)
            throw new Exception("Shock particle stopped expanding or rotated away from its radial direction.");
        player.Advance(1.2f);
        if (player.ActiveCount != 0 || GodotObject.IsInstanceValid(spark)) throw new Exception("Shock retained particles.");
        player.Play("shield", new(Vector2.Zero, new(-500, -500)), "orbital");
        var shield = player.GetChild<VfxInstance>(0);
        var head = shield.GetNode<VfxOrbitTrack>("GoldOrbit").GetNode<Sprite2D>("Head");
        var headBack = shield.GetNode<VfxOrbitTrack>("GoldOrbit").GetNode<Sprite2D>("HeadBack");
        player.Advance(.2f);
        before = head.Position;
        float frontOpacity = head.Modulate.A;
        float backOpacity = headBack.Modulate.A;
        player._Process(.5);
        if (head.Position != before) throw new Exception("Paused orbit moved.");
        player.Advance(1.8f);
        if (head.Position.IsEqualApprox(before) || head.Modulate.A >= frontOpacity || headBack.Modulate.A <= backOpacity)
            throw new Exception("Orbital light failed to pass behind the shield.");
        var orbit = shield.GetNode<VfxOrbitTrack>("GoldOrbit");
        float crossingTime = (Mathf.Pi - orbit.Phase) / orbit.AngularSpeed;
        orbit.Sample(crossingTime - 1f / 120, true, 0, 0, false);
        frontOpacity = head.Modulate.A;
        backOpacity = headBack.Modulate.A;
        orbit.Sample(crossingTime + 1f / 120, true, 0, 0, false);
        if (Mathf.Abs(head.Modulate.A - frontOpacity) > .08f || Mathf.Abs(headBack.Modulate.A - backOpacity) > .08f)
            throw new Exception("Orbital head still jumps in brightness at the hemisphere boundary.");
        player.ReducedMotion = true;
        player.Advance(0);
        before = head.Position;
        player.Advance(.2f);
        if (head.Position != before) throw new Exception("Reduced-motion orbit kept moving.");
        player.Impact("orbital");
        player.Advance(0);
        if ((float)((ShaderMaterial)shield.GetNode<VfxSpriteTrack>("Core").Material).GetShaderParameter("impact") != 1)
            throw new Exception("Shield impact did not reach its surface material.");
        player.End("orbital", VfxEndReason.Depleted);
        player.Advance(.3f);
        if (player.ActiveCount != 0 || GodotObject.IsInstanceValid(head)) throw new Exception("Shield retained orbital nodes.");
        player.ReducedMotion = false;
    }

    private void CheckFirefall(VfxPlayer player)
    {
        player.Play("firefall", new(new(-740, -500), new(-500, -500), 1.5f), "fire");
        var fire = player.GetChild<VfxInstance>(0);
        var flight = fire.GetNode<VfxFallingTrack>("Flight");
        var projectile = flight.GetNode<Node2D>("Projectile");
        var trail = flight.GetNode<Node2D>("Trail").GetChild<Sprite2D>(0);
        var impact = fire.GetNode<VfxTimedGroup>("Impact");
        var material = (ShaderMaterial)projectile.GetNode<Sprite2D>("Body").Material;
        if (!Mathf.IsEqualApprox(fire.Scale.X, fire.Scale.Y) || impact.Scale.Y >= impact.Scale.X)
            throw new Exception("Firefall flattened its airborne motion or lost ground projection.");
        player.Advance(.12f);
        var firstPosition = projectile.Position;
        float launchRotation = projectile.Rotation;
        var trailPosition = trail.Position;
        float age = (float)material.GetShaderParameter("age");
        player._Process(1);
        if (projectile.Position != firstPosition || trail.Position != trailPosition ||
            (float)material.GetShaderParameter("age") != age)
            throw new Exception("Paused firefall moved or advanced its shader.");
        player.Advance(.10f);
        if (projectile.Position.Y >= firstPosition.Y || projectile.Position.X <= firstPosition.X ||
            projectile.Rotation <= launchRotation || trail.Position.DistanceTo(trailPosition) >= 4)
            throw new Exception("Lob failed to climb/turn or its trail followed the moving head.");
        player.Advance(.559f);
        if (!projectile.Visible || impact.Visible || projectile.Position.Length() > 2)
            throw new Exception("Fireball did not approach the contact point before its impact cue.");
        player.Advance(.002f);
        if (projectile.Visible || !impact.Visible || projectile.Position != Vector2.Zero)
            throw new Exception("Fireball landing and impact are out of sync.");
        var fireMaterial = (ShaderMaterial)impact.GetNode<VfxSpriteTrack>("FlameFront").Material;
        age = (float)fireMaterial.GetShaderParameter("age");
        player._Process(1);
        if ((float)fireMaterial.GetShaderParameter("age") != age)
            throw new Exception("Paused impact advanced its local clock.");
        player.Advance(.2f);
        var ash = impact.GetNode<VfxBallisticTrack>("AshPuffs").GetChild<Sprite2D>(0);
        var ashScale = ash.Scale;
        player.Advance(.15f);
        if (!ash.Visible || ash.Scale.X <= ashScale.X || ash.Scale.Y <= ashScale.Y)
            throw new Exception("Smoke failed to expand independently after impact.");
        player.Advance(1.3f);
        if (player.ActiveCount != 0 || GodotObject.IsInstanceValid(trail))
            throw new Exception("Firefall retained children after completion.");
        player.Play("firefall", new(Vector2.Zero, new(-500, -500)), "cancel");
        fire = player.GetChild<VfxInstance>(0);
        impact = fire.GetNode<VfxTimedGroup>("Impact");
        player.Advance(.71f);
        player.End("cancel", VfxEndReason.Completed);
        player.Advance(.1f);
        if (impact.Visible) throw new Exception("Cancelled descent still triggered a delayed explosion.");
        player.Advance(.2f);
        if (player.ActiveCount != 0) throw new Exception("Cancelled firefall was not cleaned up.");
        player.ReducedMotion = true;
        player.Play("firefall", new(Vector2.Zero, new(-500, -500)), "reduced");
        flight = player.GetChild<VfxInstance>(0).GetNode<VfxFallingTrack>("Flight");
        projectile = flight.GetNode<Node2D>("Projectile");
        firstPosition = projectile.Position;
        player.Advance(.3f);
        if (projectile.Position != firstPosition || flight.GetNode<Node2D>("Trail").GetChildren().Cast<Sprite2D>().Any(s => s.Visible))
            throw new Exception("Reduced-motion firefall still travels or emits a trail.");
        player.End("reduced", VfxEndReason.ScopeEnded);
        player.ReducedMotion = false;
        // Radius only sizes the impact. The launch anchor, arc and body retain
        // their stage-space size across radius changes and reversed throws.
        foreach (float distance in new[] { -240f, 0f, 240f })
        {
            Vector2[] positions = new Vector2[2];
            Vector2[] sizes = new Vector2[2];
            for (int i = 0; i < 2; i++)
            {
                var source = new Vector2(-500 + distance, -500);
                player.Play("firefall", new(source, new(-500, -500), i == 0 ? .5f : 3f), "projection");
                var instance = player.GetChild<VfxInstance>(0);
                var body = instance.GetNode<Node2D>("Flight/Projectile");
                if ((instance.Transform * body.Position).DistanceTo(source) > 40)
                    throw new Exception("Lob did not launch beside the projected caster.");
                player.Advance(.39f);
                positions[i] = instance.Transform * body.Position;
                sizes[i] = instance.Scale * body.Scale;
                player.End("projection", VfxEndReason.ScopeEnded);
            }
            if (!positions[0].IsEqualApprox(positions[1]) || !sizes[0].IsEqualApprox(sizes[1]))
                throw new Exception("Blast radius changed the flight path or projectile size.");
        }
    }

    private void CheckPreviewLoop()
    {
        var canvas = new CanvasLayer();
        AddChild(canvas);
        var preview = GD.Load<PackedScene>("res://scenes/app/VfxPreview.tscn").Instantiate<VfxPreviewController>();
        canvas.AddChild(preview);
        var player = preview.GetNode<VfxPlayer>("%Player");
        preview.SetProcess(false);
        player.SetProcess(false);
        void Select(string id)
        {
            int index = player.Catalog.Effects.Select(effect => effect.StableId).ToList().IndexOf(id);
            preview.GetNode<ItemList>("%Effects").EmitSignal(ItemList.SignalName.ItemSelected, index);
        }
        void Tick(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                preview._Process(1.0 / 60);
                player._Process(1.0 / 60);
            }
        }
        Select("shield");
        ulong originalId = player.GetChild<VfxInstance>(0).GetInstanceId();
        Tick(540);
        if (player.ActiveCount != 1 || player.GetChild<VfxInstance>(0).GetInstanceId() != originalId)
            throw new Exception("Persistent preview was destroyed/restarted by the loop timer.");
        var core = (ShaderMaterial)player.GetChild<VfxInstance>(0).GetNode<VfxSpriteTrack>("Core").Material;
        float age = (float)core.GetShaderParameter("age");
        if (age < 8.9f) throw new Exception("Persistent clock wrapped back into its entrance animation.");
        player.Paused = true;
        Tick(300);
        if ((float)core.GetShaderParameter("age") != age) throw new Exception("Paused preview advanced.");
        player.Paused = false;
        preview.GetNode<Button>("%Replay").EmitSignal(Button.SignalName.Pressed);
        if (player.GetChild<VfxInstance>(0).GetInstanceId() == originalId)
            throw new Exception("Explicit replay no longer restarts the effect.");
        preview.GetNode<Button>("%End").EmitSignal(Button.SignalName.Pressed);
        Tick(300);
        if (player.ActiveCount != 0) throw new Exception("Explicitly ended shield reappeared automatically.");
        Select("burst");
        originalId = player.GetChild<VfxInstance>(0).GetInstanceId();
        Tick(120);
        if (player.ActiveCount != 1 || player.GetChild<VfxInstance>(0).GetInstanceId() == originalId)
            throw new Exception("Transient preview stopped repeating.");
        Select("firefall");
        originalId = player.GetChild<VfxInstance>(0).GetInstanceId();
        Tick(110);
        if (player.ActiveCount != 1 || player.GetChild<VfxInstance>(0).GetInstanceId() != originalId)
            throw new Exception("Longer transient preview restarted before its authored outro finished.");
        Tick(60);
        if (player.ActiveCount != 1 || player.GetChild<VfxInstance>(0).GetInstanceId() == originalId)
            throw new Exception("Firefall preview failed to repeat after its completed outro.");
        canvas.Free();
    }
}
