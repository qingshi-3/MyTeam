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
            if (args.Length > 1 && args[0] == "--library")
            {
                foreach (var id in new[] { "beam", "impact", "cleave", "lightning", "frost", "flamethrower", "burn", "poison", "empower", "weaken", "stun", "summon" })
                    await RecordPreview(System.IO.Path.Combine(args[1], id), id);
                GetTree().Quit();
                return;
            }
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
            CheckLibrary(player);
            CheckPlaybackParameters(player);
            CheckCleaveGeometry(player);
            CheckStatusMotion(player);
            CheckElementMotion(player);
            CheckFireMotion(player);
            CheckRemainingMotion(player);
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
    private async Task RecordPreview(string directory, string? effectOverride = null)
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
        var selectedId = effectOverride ?? OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--effect="))?[9..] ?? "rend";
        int selectedIndex = player.Catalog.Effects.Select(effect => effect.StableId).ToList().IndexOf(selectedId);
        if (selectedIndex < 0) throw new Exception("Unknown capture effect.");
        var list = preview.GetNode<ItemList>("%Effects");
        list.Select(selectedIndex);
        // Programmatic harness signal, explicitly not a real-input QA claim.
        list.EmitSignal(ItemList.SignalName.ItemSelected, selectedIndex);
        foreach (var (argument, control) in new[] { ("--cast=", "CastSpeed"), ("--flow=", "MotionSpeed"), ("--radius=", "Radius"),
            ("--heading=", "Heading"), ("--distance=", "Distance"),
            ("--flight=", "FlightTime"), ("--density=", "Density"), ("--particle-size=", "ParticleSize") })
        {
            var option=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith(argument));
            if(option is not null) preview.GetNode<HSlider>("%"+control).Value=double.Parse(option[argument.Length..],System.Globalization.CultureInfo.InvariantCulture);
        }
        bool eventSequence=OS.GetCmdlineUserArgs().Contains("--event-sequence");
        if(eventSequence) preview.GetNode<CheckButton>("%EventMode").ButtonPressed=true;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        float step = effectOverride is null ? 1f / 60 : 1f / 30;
        var definition = player.Catalog.Find(selectedId);
        int frames = continuous ? 540 : effectOverride is null && selectedId == "shield" ? 300
            : (int)Mathf.Ceil((definition.Persistent ? 3.0f : player.PlaybackFor("preview")!.ExpectedDuration + .4f) / step);
        if(eventSequence) frames=210;
        for (int frame = 0; frame < frames; frame++)
        {
            // Container layout settles after Ready; update the real reference
            // unit projection without advancing the preview's clock or looping.
            preview._Process(continuous && frame > 0 ? 1.0 / 60 : 0);
            if (frame > 0) player.Advance(step);
            if(eventSequence)
            {
                if(frame==18) preview.GetNode<Button>("%Fired").EmitSignal(Button.SignalName.Pressed);
                if(frame>=18 && frame<=90) preview.GetNode<HSlider>("%Progress").Value=(frame-18)/72.0;
                if(frame==96) preview.GetNode<Button>("%ContactCue").EmitSignal(Button.SignalName.Pressed);
                player.Advance(0);
            }
            if (!continuous && selectedId == "shield" && frame == 120) player.Impact("preview");
            if (!continuous && selectedId == "shield" && frame == 240)
                player.End("preview", OS.GetCmdlineUserArgs().Contains("--normal-end") ? VfxEndReason.Completed : VfxEndReason.Depleted);
            preview.GetNode<Label>("%Clock").Text = $"{frame * step:0.00} 秒 · 1.00× · {player.ActiveCount} 实例";
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var path = System.IO.Path.Combine(directory, $"frame-{frame:D3}.png");
            if (GetViewport().GetTexture().GetImage().SavePng(path) != Error.Ok)
                throw new Exception("Motion frame save failed.");
        }
        GD.Print($"VFX_MOTION_CAPTURE_OK: {selectedId}, {frames} frames at {1 / step:0} samples/second, shared preview scene, no battle/save.");
        canvas.Free();
    }

    private void CheckLibrary(VfxPlayer player)
    {
        foreach (var definition in player.Catalog.Effects)
        {
            player.Play(definition.StableId, new(new(-700,-500),new(-500,-500)), "library");
            var instance = player.GetChild<VfxInstance>(0);
            ulong identity = instance.GetInstanceId();
            player.Advance(definition.Persistent ? 8.1f : definition.Duration + .1f);
            if (definition.Persistent)
            {
                if (player.ActiveCount != 1 || instance.GetInstanceId() != identity)
                    throw new Exception($"{definition.StableId}: sustained effect restarted or disappeared.");
                var particles = instance.GetChildren().OfType<VfxAmbientTrack>().SelectMany(t => t.GetChildren().Cast<Sprite2D>()).ToArray();
                if (particles.Length > 0 && !particles.Any(p => p.Visible && p.Modulate.A > .03f))
                    throw new Exception($"{definition.StableId}: looping particles all went dark.");
                var poses = particles.Select(p => p.Transform).ToArray();
                player._Process(1);
                if (particles.Where((p,i) => p.Transform != poses[i]).Any())
                    throw new Exception($"{definition.StableId}: paused ambient particles moved.");
                player.ReducedMotion = true;
                player.Advance(0);
                var positions = particles.Select(p => p.Position).ToArray();
                var visibleBefore = particles.Select(p => p.Visible).ToArray();
                player.Advance(.005f);
                if (particles.Where((p,i) => visibleBefore[i] && p.Visible && p.Position != positions[i]).Any())
                    throw new Exception($"{definition.StableId}: reduced-motion particle still travels.");
                player.End("library", VfxEndReason.Completed);
                player.Advance(.3f);
                player.ReducedMotion = false;
            }
            if (player.ActiveCount != 0 || GodotObject.IsInstanceValid(instance))
                throw new Exception($"{definition.StableId}: retained effect after completion.");
        }
        var packed = GD.Load<PackedScene>("res://scenes/vfx/burn.tscn");
        VfxInstance Create()
        {
            var instance = packed.Instantiate<VfxInstance>();
            foreach (var track in instance.GetChildren().OfType<VfxAmbientTrack>()) track.Seed = 42;
            AddChild(instance);
            instance.Bind(player.Catalog.Find("burn"),new(new(-700,-500),new(-500,-500)));
            return instance;
        }
        var direct = Create();
        var stepped = Create();
        direct.Advance(5.5f,this,false);
        for (int i=0;i<55;i++) stepped.Advance(.1f,this,false);
        var a=direct.GetNode<VfxAmbientTrack>("Flames").GetChildren().Cast<Sprite2D>().ToArray();
        var b=stepped.GetNode<VfxAmbientTrack>("Flames").GetChildren().Cast<Sprite2D>().ToArray();
        for(int i=0;i<a.Length;i++)
            if(a[i].Visible!=b[i].Visible || a[i].Position.DistanceTo(b[i].Position) > .002f)
                throw new Exception("Looping particle motion depends on frame subdivision.");
        // Across many independent cycle boundaries, no whole-emitter blackout.
        for(int i=0;i<300;i++)
        {
            direct.Advance(1f/60,this,false);
            if(!a.Any(p=>p.Visible && p.Modulate.A>.1f)) throw new Exception("Burn cycle has a global gap.");
        }
        direct.Free(); stepped.Free();
        player.ReducedMotion = true;
        player.Play("flamethrower",new(new(-700,-500),new(-500,-500)),"breath");
        player.Advance(.3f);
        var jet=player.GetChild<VfxInstance>(0).GetNode<VfxAmbientTrack>("FlameJet").GetChildren().Cast<Sprite2D>().ToArray();
        var jetPositions=jet.Select(p=>p.Position).ToArray();
        var jetVisible=jet.Select(p=>p.Visible).ToArray();
        if(!jet.Any(p=>p.Visible && p.Position.X>100)) throw new Exception("Reduced breath lost its range silhouette.");
        player.Advance(.05f);
        if(jet.Where((p,i)=>p.Visible && jetVisible[i] && p.Position!=jetPositions[i]).Any())
            throw new Exception("Reduced breath still travels.");
        player.End("breath",VfxEndReason.ScopeEnded);
        player.ReducedMotion = false;
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
        player.Advance(.3f);
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
        player.Advance(1.01f);
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

    private void CheckPlaybackParameters(VfxPlayer player)
    {
        VfxContext Context(VfxPlaybackParameters? parameters = null, float radius = 1.5f) =>
            new(new(-740,-500),new(-500,-500),radius,parameters);
        player.Play("firefall",Context(new(CastSpeed:2)),"fast");
        player.Play("firefall",Context(new(CastSpeed:.5f)),"slow");
        var fast=player.PlaybackFor("fast")!;
        var slow=player.PlaybackFor("slow")!;
        player.Advance(.2f);
        if(fast.FiredAt is null || slow.FiredAt is not null || fast.ImpactAt is not null)
            throw new Exception("Independent cast rates failed to separate release times.");
        player.Advance(.74f);
        if(fast.ImpactAt is null || slow.ImpactAt is not null ||
            Mathf.Abs(fast.ImpactAt.Value-fast.FiredAt!.Value-.78f)>.001f)
            throw new Exception("Cast speed leaked into flight duration.");
        var impactMaterial=(ShaderMaterial)player.GetChild<VfxInstance>(0).GetNode<VfxTimedGroup>("Impact").GetNode<VfxSpriteTrack>("FlameFront").Material;
        float phaseAge=(float)impactMaterial.GetShaderParameter("age");
        player.Advance(.1f);
        if(Mathf.Abs((float)impactMaterial.GetShaderParameter("age")-phaseAge-.1f)>.001f)
            throw new Exception("Cast speed leaked into explosion/outro time.");
        player.Clear();

        // Slow blades may outlive the original resource duration, while the
        // same authored tail duration is preserved at every cast rate.
        player.Play("rend",Context(new(CastSpeed:.25f),0),"slow-rend");
        var rend=player.GetChild<VfxInstance>(0);
        player.Advance(1.2f);
        if(player.ActiveCount!=1 || !rend.GetNode<VfxRibbonTrack>("SecondSweep").Visible)
            throw new Exception("Slow blade was cut off by unscaled definition lifetime.");
        var blade=(ShaderMaterial)rend.GetNode<VfxRibbonTrack>("FirstSweep").Material;
        if(Mathf.Abs((float)blade.GetShaderParameter("sweep_duration")-.68f)>.001f ||
            Mathf.Abs((float)blade.GetShaderParameter("tail_duration")-.85f)>.001f)
            throw new Exception("Blade timing failed to isolate sweep from tail fade.");
        player.Advance(2); player.Clear();

        player.Play("shield",Context(new(MotionSpeed:2),0),"flow");
        var shield=player.GetChild<VfxInstance>(0);
        player.Advance(.4f);
        var core=(ShaderMaterial)shield.GetNode<VfxSpriteTrack>("Core").Material;
        if(Mathf.Abs((float)core.GetShaderParameter("age")-.8f)>.001f)
            throw new Exception("Per-instance internal motion rate was ignored.");
        player.End("flow",VfxEndReason.Completed); player.Advance(.26f);
        if(player.ActiveCount!=0) throw new Exception("Internal flow rate changed state release lifetime.");

        player.Play("frost",Context(),"small");
        player.Play("frost",Context(radius:3),"wide");
        var small=player.GetChild<VfxInstance>(0); var wide=player.GetChild<VfxInstance>(1);
        var smallShards=small.GetNode<VfxBallisticTrack>("Chips0"); var wideShards=wide.GetNode<VfxBallisticTrack>("Chips0");
        if(wideShards.GetChildCount()<=smallShards.GetChildCount() || wideShards.GetChildCount()>48 ||
            Mathf.Abs(wide.Scale.X*wide.Playback.ParticleScale-small.Scale.X*small.Playback.ParticleScale)>.001f)
            throw new Exception("Range scaling changed particle size or failed to add bounded density.");
        player.Clear();

        player.Play("heal",Context(new(ParticleScale:1.5f,Density:2),0),"heal-parameters");
        var healing=player.GetChild<VfxInstance>(0);
        if(healing.GetNode<VfxParticleTrack>("PlusParticles").GetChildCount()!=20 || healing.Playback.ParticleScale!=1.5f)
            throw new Exception("Healing particles did not consume shared size/density parameters.");
        player.Clear();

        // The battle adapter uses one identity per invocation; two casts from
        // one owner must not overwrite each other. No simulation is advanced.
        void Cue(TowerAutobattler.Battle.BattleVfxPhase phase,string id,float? progress=null) =>
            VfxBindingResolver.Present(player,new(phase,"firefall",CastSpeed:2,InstanceId:id,TravelProgress:progress),
                Context(new(ParticleScale:1.5f,Density:2,WidthScale:1.2f)),"same-hero");
        Cue(TowerAutobattler.Battle.BattleVfxPhase.SequenceStart,"a");
        Cue(TowerAutobattler.Battle.BattleVfxPhase.SequenceStart,"b");
        var external=player.PlaybackFor("sequence:a")!;
        if(external.Parameters.ParticleScale!=1.5f || external.Parameters.Density!=2 || external.Parameters.WidthScale!=1.2f)
            throw new Exception("Battle cue discarded the invocation's visual parameters.");
        player.Advance(3);
        if(player.ActiveCount!=2 || external.FiredAt is not null || external.ImpactAt is not null)
            throw new Exception("Event-driven sequence predicted release/impact from visual duration.");
        if(!player.GetChild<VfxInstance>(0).GetNode<VfxSpriteTrack>("Cast/Charge").Visible)
            throw new Exception("Waiting for an authoritative release lost the held charge pose.");
        Cue(TowerAutobattler.Battle.BattleVfxPhase.SequenceFired,"a");
        Cue(TowerAutobattler.Battle.BattleVfxPhase.SequenceProgress,"a",.4f);
        player.Advance(.1f);
        if(external.TravelProgress!=.4f || external.ImpactAt is not null)
            throw new Exception("External flight progress was not consumed independently.");
        Cue(TowerAutobattler.Battle.BattleVfxPhase.SequenceImpact,"a");
        float firstHit=external.ImpactAt!.Value;
        player.Advance(.1f);
        Cue(TowerAutobattler.Battle.BattleVfxPhase.SequenceImpact,"a");
        Cue(TowerAutobattler.Battle.BattleVfxPhase.SequenceFired,"a");
        if(external.ImpactAt!=firstHit) throw new Exception("Repeated/late phase cues restarted the sequence.");
        Cue(TowerAutobattler.Battle.BattleVfxPhase.SequenceEnd,"b");
        Cue(TowerAutobattler.Battle.BattleVfxPhase.SequenceImpact,"b");
        player.Advance(.3f);
        if(player.ActiveCount!=1) throw new Exception("Cancelled sequence revived from a late hit.");
        player.Advance(2);
        if(player.ActiveCount!=0) throw new Exception("External impact failed to clean its outro.");
        bool rejected=false;
        try { player.Play("rend",Context(new(CastSpeed:float.NaN)),"bad"); }
        catch(ArgumentOutOfRangeException) { rejected=true; }
        if(!rejected || player.ActiveCount!=0) throw new Exception("Invalid playback parameters polluted the player.");
    }

    private sealed class SkewStage : IVfxStage
    {
        public float UnitScale => 1.7f;
        public float RadiusPixels(float radius) => radius * 70;
        public Vector2 Project(Vector2 p, bool ground) => new(600+p.X*70+p.Y*25,350+p.Y*40);
    }
    private void CheckCleaveGeometry(VfxPlayer player)
    {
        var stage=new SkewStage();
        player.Bind(stage);
        void Near(Vector2 actual,Vector2 expected,string why)
        { if(actual.DistanceTo(expected)>.01f) throw new Exception(why); }
        var context=new VfxContext(new(-1,0),new(2,0),1.5f,new(CastSpeed:.25f));
        player.Play("cleave",context,"arc");
        var instance=player.GetChild<VfxInstance>(0);
        var plane=instance.GetNode<VfxSweepPlane>("Sweep");
        Near(plane.ToGlobal(Vector2.Zero),stage.Project(context.Source,false),"Cleave did not pivot on its caster.");
        Near(plane.ToGlobal(new(128,0)),stage.Project(context.Source+Vector2.Right*1.5f,false),"Cleave radius came from target distance.");
        var blade=plane.GetNode<VfxSectorTrack>("Blade");
        var mesh=(ArrayMesh)blade.Mesh;
        var vertices=mesh.SurfaceGetArrays(0)[(int)Godot.Mesh.ArrayType.Vertex].AsVector2Array();
        if(vertices.Any(v=>v.X<-.001f || v.Length()>128.01f)) throw new Exception("Cleave escaped its forward half-disc.");
        Near(vertices[6],new(0,-128),"Cleave did not start at -90 degrees.");
        Near(vertices[^1],new(0,128),"Cleave did not end at +90 degrees.");
        context=context with { Target=new(10,10) };
        player.UpdateContext("arc",context);player.Advance(.1f);
        Near(plane.ToGlobal(new(128,0)),stage.Project(context.Source+Vector2.Right*1.5f,false),"Moving target rotated or stretched an in-progress swing.");
        context=context with { Source=new(-2,1), Radius=3 };
        player.UpdateContext("arc",context);player.Advance(0);
        Near(plane.ToGlobal(Vector2.Zero),stage.Project(context.Source,false),"Cleave stopped following its owner.");
        Near(plane.ToGlobal(new(128,0)),stage.Project(context.Source+Vector2.Right*3,false),"Cleave ignored explicit radius.");
        player.Advance(1.0f);
        if(!blade.Visible) throw new Exception("Slow cleave was truncated before its angular sweep completed.");
        var shader=(ShaderMaterial)blade.Material;
        if(Math.Abs((float)shader.GetShaderParameter("sweep_duration")-1.2f)>.001f ||
            Math.Abs((float)shader.GetShaderParameter("tail_duration")-.24f)>.001f)
            throw new Exception("Cleave cast speed affected its real-time tail.");
        float before=(float)shader.GetShaderParameter("age");player.Advance(0);
        if((float)shader.GetShaderParameter("age")!=before) throw new Exception("Paused cleave advanced.");
        player.End("arc",VfxEndReason.Completed);player.Advance(.3f);
        if(player.ActiveCount!=0) throw new Exception("Cancelled cleave retained its children.");
        foreach(var direction in new[]{Vector2.Left,Vector2.Up,new Vector2(1,1).Normalized()})
        {
            context=new(new(1,-1),new(1,-1),1.5f,Direction:direction);
            player.Play("cleave",context,"direction");
            plane=player.GetChild<VfxInstance>(0).GetNode<VfxSweepPlane>("Sweep");
            Near(plane.ToGlobal(new(128,0)),stage.Project(context.Source+direction*1.5f,false),"Cleave rotated after projection instead of in world space.");
            var side=new Vector2(-direction.Y,direction.X);
            Near(plane.ToGlobal(new(0,128)),stage.Project(context.Source+side*1.5f*plane.SideRadiusRatio,false),"Cleave ellipse lost its authored side radius after rotation/projection.");
            player.Clear();
        }
        player.Bind(this);
    }

    private void CheckStatusMotion(VfxPlayer player)
    {
        var context = new VfxContext(new(-700,-500),new(-500,-500));
        player.Play("empower", context, "rise");
        var instance = player.GetChild<VfxInstance>(0);
        var plumes = instance.GetNode<VfxRiseTrack>("MainPlumes").GetChildren().Cast<Sprite2D>().ToArray();
        player.Advance(.15f);
        var first = plumes[0];
        if (!first.Visible || first.Offset.Y >= 0) throw new Exception("Empower did not grow above its foot anchor.");
        float y0 = first.Position.Y;
        player.Advance(.10f); float y1 = first.Position.Y;
        player.Advance(.10f); float y2 = first.Position.Y;
        if (y2 >= y1 || y1 >= y0 || y1-y2 <= y0-y1) throw new Exception("Empower impulse failed to accelerate upward.");
        float progress = (float)((ShaderMaterial)first.Material).GetShaderParameter("progress");
        player.Advance(0);
        if ((float)((ShaderMaterial)first.Material).GetShaderParameter("progress") != progress) throw new Exception("Paused plume dissolved.");
        player.Clear();
        player.Play("empower", context with { Playback = new(CastSpeed:.25f) }, "slow-rise");
        plumes = player.GetChild<VfxInstance>(0).GetNode<VfxRiseTrack>("MainPlumes").GetChildren().Cast<Sprite2D>().ToArray();
        player.Advance(.2f);
        if (plumes.Any(p=>p.Visible)) throw new Exception("Slow empowerment ignored cast-relative birth.");
        player.Advance(.25f);
        if (!plumes.Any(p=>p.Visible)) throw new Exception("Slow empowerment never started.");
        player.Clear();
        player.Play("empower",context,"cancel-rise");
        plumes = player.GetChild<VfxInstance>(0).GetNode<VfxRiseTrack>("MainPlumes").GetChildren().Cast<Sprite2D>().ToArray();
        player.End("cancel-rise",VfxEndReason.Completed); player.Advance(.15f);
        if (plumes.Any(p=>p.Visible)) throw new Exception("Cancelled empowerment spawned pending plumes.");
        player.Clear();
        context = context with { Radius=1.5f };
        player.Play("poison",context,"pool");
        instance=player.GetChild<VfxInstance>(0);
        var bubbles=instance.GetNode<VfxPoolBubbleTrack>("SurfaceBubbles").GetChildren().Cast<Sprite2D>().ToArray();
        if (bubbles.Select(p=>p.Material.GetInstanceId()).Distinct().Count()!=bubbles.Length)
            throw new Exception("Independent bubble cycles shared mutable material.");
        player.Advance(4.2f);
        float size=bubbles[0].GlobalScale.X*bubbles[0].Texture.GetWidth();
        player.UpdateContext("pool",context with { Radius=3 }); player.Advance(0);
        if (Math.Abs(bubbles[0].GlobalScale.X*bubbles[0].Texture.GetWidth()-size)>.01f)
            throw new Exception("Expanding pool inflated its individual bubbles.");
        bool sawDome=false,sawPop=false,sawRipple=false;
        for (int i=0;i<180;i++)
        {
            player.Advance(1f/60);
            var phases=bubbles.Where(p=>p.Visible).Select(p=>(float)((ShaderMaterial)p.Material).GetShaderParameter("phase")).ToArray();
            sawDome|=phases.Any(p=>p>.2f && p<.5f);
            sawPop|=phases.Any(p=>p>.55f && p<.65f);
            sawRipple|=phases.Any(p=>p>.7f && p<.85f);
            if (!phases.Any(p=>p>.08f && p<.88f)) throw new Exception("Surface bubbles all disappeared at a cycle boundary.");
        }
        if (!sawDome || !sawPop || !sawRipple) throw new Exception("Poison skipped a bubble stage.");
        var material=(ShaderMaterial)bubbles[0].Material;
        float phase=(float)material.GetShaderParameter("phase");player.Advance(0);
        if ((float)material.GetShaderParameter("phase")!=phase) throw new Exception("Paused poison continued its cycle.");
        player.ReducedMotion=true;player.Advance(.1f);
        phase=(float)material.GetShaderParameter("phase");player.Advance(.2f);
        if ((float)material.GetShaderParameter("phase")!=phase) throw new Exception("Reduced poison continued popping.");
        player.ReducedMotion=false;
        player.End("pool",VfxEndReason.Completed);player.Advance(.3f);
        if (player.ActiveCount!=0 || GodotObject.IsInstanceValid(instance)) throw new Exception("Poison retained its surface children.");
    }

    private void CheckElementMotion(VfxPlayer player)
    {
        var context=new VfxContext(new(-700,-500),new(-500,-500));
        player.Play("lightning",context,"bolt");
        var instance=player.GetChild<VfxInstance>(0);
        var main=instance.GetNode<VfxLightningTrack>("MainBolt");
        var after=instance.GetNode<VfxLightningTrack>("AfterBolt");
        var vertices=((ArrayMesh)main.Mesh).SurfaceGetArrays(0)[(int)Godot.Mesh.ArrayType.Vertex].AsVector2Array();
        if (((vertices[0]+vertices[1])*.5f).DistanceTo(main.From)>.001f ||
            ((vertices[64]+vertices[65])*.5f).DistanceTo(main.To)>.001f || vertices.Length<=66)
            throw new Exception("Lightning lost its authored endpoints or connected branch geometry.");
        player.Advance(.04f);
        if(!main.Visible || after.Visible) throw new Exception("Lightning main/after-discharge order is wrong.");
        float age=(float)((ShaderMaterial)main.Material).GetShaderParameter("age");player.Advance(0);
        if((float)((ShaderMaterial)main.Material).GetShaderParameter("age")!=age) throw new Exception("Paused lightning still flows.");
        player.End("bolt",VfxEndReason.Completed);player.Advance(.13f);
        if(after.Visible) throw new Exception("Cancelled strike emitted a pending after-discharge.");
        player.Clear();
        player.Play("frost",context with { Radius=1.5f },"ice");
        instance=player.GetChild<VfxInstance>(0);
        var crystal=instance.GetNode<VfxCrystalTrack>("Crystal0");
        var chips=instance.GetChildren().OfType<VfxBallisticTrack>().SelectMany(t=>t.GetChildren().Cast<Sprite2D>()).ToArray();
        var root=crystal.Position;
        player.Advance(.06f);float height=crystal.Scale.Y;
        player.Advance(.16f);
        if(crystal.Position!=root || crystal.Offset.Y>=0 || crystal.Scale.Y<=height || chips.Any(p=>p.Visible))
            throw new Exception("Ice did not grow at its base before fragment emission.");
        float size=crystal.GlobalScale.X;
        player.UpdateContext("ice",context with { Radius=3 });player.Advance(0);
        if(Math.Abs(crystal.GlobalScale.X-size)>.001f) throw new Exception("Frost radius inflated individual crystals.");
        player.Advance(.16f);
        if(!chips.Any(p=>p.Visible) || (float)((ShaderMaterial)crystal.Material).GetShaderParameter("fracture")<=0)
            throw new Exception("Crystal fracture and flying chips failed to meet.");
        player.Clear();
        player.Play("frost",context with { Playback=new(CastSpeed:.25f) },"slow-ice");
        instance=player.GetChild<VfxInstance>(0);
        var mist=instance.GetNode<VfxTimedGroup>("ColdMist");
        chips=instance.GetChildren().OfType<VfxBallisticTrack>().SelectMany(t=>t.GetChildren().Cast<Sprite2D>()).ToArray();
        player.Advance(.35f);
        if(mist.Visible || chips.Any(p=>p.Visible)) throw new Exception("Slow ice released mist/chips before fracture.");
        player.End("slow-ice",VfxEndReason.Completed);player.Advance(.1f);
        if(mist.Visible || chips.Any(p=>p.Visible)) throw new Exception("Cancelled ice released pending fracture layers.");
        player.Clear();
        var fragment=new VfxBallisticTrack { ParticleScene=GD.Load<PackedScene>("res://scenes/vfx/IcePrismParticle.tscn"),
            Amount=1,Seed=5,SpeedRange=Vector2.Zero,LiftSpeed=180,Gravity=220,Spin=3,LifetimeRange=Vector2.One };
        AddChild(fragment);var chip=fragment.GetChild<Sprite2D>(0);
        fragment.Sample(.12f,false,0,0,false);float y0=chip.Position.Y;
        fragment.Sample(.3f,false,0,0,false);float y1=chip.Position.Y;
        fragment.Sample(.68f,false,0,0,false);
        if(y1>=y0 || chip.Position.Y<=y1 || chip.Rotation==0) throw new Exception("Ice chip failed to rise, tumble and fall.");
        fragment.Sample(.2f,false,0,0,true);var pose=chip.Transform;float rotation=chip.Rotation;
        fragment.Sample(.3f,false,0,0,true);
        if(chip.Position!=pose.Origin || chip.Rotation!=rotation) throw new Exception("Reduced-motion chip still moves or tumbles.");
        fragment.Free();
    }

    private void CheckRemainingMotion(VfxPlayer player)
    {
        var context = new VfxContext(new(-700,-500), new(-500,-500));
        player.Play("stun",context,"orbit-detail");
        var instance=player.GetChild<VfxInstance>(0);
        var starTrack=instance.GetNode<VfxAmbientTrack>("Stars");
        var stars=starTrack.GetChildren().Cast<Sprite2D>().ToArray();
        float orbitSample=Mathf.Pi/starTrack.AngularSpeed-.001f;
        player.Advance(orbitSample);
        var sizes=stars.Select(s=>s.Scale).ToArray();
        var alphas=stars.Select(s=>s.Modulate.A).ToArray();
        player.Advance(.002f);
        if(stars.Where((s,i)=>s.Scale.DistanceTo(sizes[i])>.001f || Math.Abs(s.Modulate.A-alphas[i])>.01f).Any())
            throw new Exception("Star orbit jumps at the near/far boundary.");
        var steppedPoses=stars.Select(s=>s.Transform).ToArray();
        player.Advance(0);
        if(stars.Where((s,i)=>!s.Transform.IsEqualApprox(steppedPoses[i])).Any())
            throw new Exception("Paused star rhythm still advances.");
        var definition=player.Catalog.Find("stun");
        var direct=definition.Scene.Instantiate<VfxInstance>();
        AddChild(direct); direct.Bind(definition,context);
        direct.Advance(orbitSample+.002f,this,false);
        var directStars=direct.GetNode<VfxAmbientTrack>("Stars").GetChildren().Cast<Sprite2D>().ToArray();
        if(stars.Where((s,i)=>!s.Transform.IsEqualApprox(directStars[i].Transform)).Any())
            throw new Exception("Star rhythm depends on frame subdivision.");
        direct.Free();
        player.ReducedMotion=true; player.Advance(0);
        var poses=stars.Select(s=>s.Transform).ToArray();
        player.Advance(.5f);
        if(stars.Where((s,i)=>s.Transform!=poses[i]).Any())
            throw new Exception("Reduced star orbit still scales or tilts.");
        player.Clear(); player.ReducedMotion=false;
        player.Play("summon",context,"summon-cancel");
        instance=player.GetChild<VfxInstance>(0);
        player.Advance(.2f); player.End("summon-cancel",VfxEndReason.Completed); player.Advance(.2f);
        foreach(var cue in new[]{"ColumnCue","FootCue","ArrivalCue","ResidualLight"})
            if(instance.GetNode<Node2D>(cue).Visible)
                throw new Exception("Cancelled summon started a pending light layer.");
        player.Clear();
        player.Play("summon",context,"summon-clock");
        instance=player.GetChild<VfxInstance>(0); player.Advance(.6f);
        var column=instance.GetNode<Sprite2D>("ColumnCue/Column");
        var material=(ShaderMaterial)column.Material;
        float age=(float)material.GetShaderParameter("age");
        player.Advance(0);
        if((float)material.GetShaderParameter("age")!=age || !column.IsVisibleInTree())
            throw new Exception("Summon column flow ignored pause or failed to start.");
        player.Clear();
    }

    private void CheckFireMotion(VfxPlayer player)
    {
        var context = new VfxContext(new(-700,-500), new(-500,-500));
        player.Play("burn", context, "rooted-fire");
        var instance = player.GetChild<VfxInstance>(0);
        var tongues = instance.GetNode<VfxAmbientTrack>("Flames").GetChildren().Cast<Sprite2D>().ToArray();
        if (tongues.Select(t => t.Material.GetInstanceId()).Distinct().Count() != tongues.Length)
            throw new Exception("Flame particles share mutable flow materials.");
        player.Advance(.3f);
        var activeFlame = tongues.First(t => t.Visible);
        var shader = (ShaderMaterial)activeFlame.Material;
        float age = (float)shader.GetShaderParameter("age");
        player.Advance(0);
        if ((float)shader.GetShaderParameter("age") != age) throw new Exception("Paused fire texture flowed.");
        player.Advance(.05f);
        if ((float)shader.GetShaderParameter("age") <= age) throw new Exception("Fire texture did not flow.");
        player.ReducedMotion = true; player.Advance(0);
        age = (float)shader.GetShaderParameter("age");
        var position = activeFlame.Position;
        player.Advance(.4f);
        if (activeFlame.Position != position || (float)shader.GetShaderParameter("age") != age)
            throw new Exception("Reduced-motion flame particle still moves.");
        player.End("rooted-fire", VfxEndReason.Completed); player.Advance(.3f);
        if (GodotObject.IsInstanceValid(instance)) throw new Exception("Burn survived release.");
        player.ReducedMotion = false;
        player.Play("flamethrower", context with { Playback = new(CastSpeed:.5f) }, "slow-jet");
        instance = player.GetChild<VfxInstance>(0);
        var jet = instance.GetNode<VfxJetTrack>("JetBody");
        shader = (ShaderMaterial)jet.Material;
        var particles = instance.GetNode<VfxAmbientTrack>("FlameJet").GetChildren().Cast<Sprite2D>().ToArray();
        if (particles.Select(t => t.Material.GetInstanceId()).Distinct().Count() != particles.Length)
            throw new Exception("Jet lobes share animated materials.");
        player.Advance(.75f);
        if (!jet.Visible || Math.Abs((float)shader.GetShaderParameter("supply_duration") - 1) > .001f
            || Math.Abs((float)shader.GetShaderParameter("transit_duration") - .32f) > .001f)
            throw new Exception("Cast rate changed jet travel instead of supply duration.");
        player.Advance(.5f);
        if (!jet.Visible || !particles.Any(t => t.Visible)) throw new Exception("Slow jet cut off its downstream tail.");
        player.Clear();
        player.Play("flamethrower", context, "cancel-jet");
        instance = player.GetChild<VfxInstance>(0);
        particles = instance.GetNode<VfxAmbientTrack>("FlameJet").GetChildren().Cast<Sprite2D>().ToArray();
        player.End("cancel-jet", VfxEndReason.Completed); player.Advance(.1f);
        if (instance.GetNode<VfxJetTrack>("JetBody").Visible || particles.Any(t => t.Visible))
            throw new Exception("Cancelled jet started pending flames.");
        player.Clear();
        foreach (float heading in new[] { 0f, Mathf.Pi, .7f })
        {
            player.Play("flamethrower", context with { Target = context.Source + Vector2.FromAngle(heading) * 200 }, "tail-facing");
            instance = player.GetChild<VfxInstance>(0);
            player.Advance(.72f);
            var lobe = instance.GetNode<VfxAmbientTrack>("FlameJet").GetChildren().Cast<Sprite2D>()
                .First(p => p.Visible && p.Modulate.A > .2f);
            Vector2 before = lobe.GlobalPosition;
            player.Advance(.02f);
            // The dedicated jet texture leads along +X; the tapered wake points left.
            Vector2 hotHead = lobe.GlobalTransform.BasisXform(Vector2.Right).Normalized();
            if (!lobe.Visible || hotHead.Dot((lobe.GlobalPosition - before).Normalized()) < .85f)
                throw new Exception("Detached jet flame leads with its thin tail instead of its hot body.");
            player.Clear();
        }
        var holder = new Node2D { Rotation = 1.1f, Scale = new(1.6f, .55f) };
        AddChild(holder);
        VfxAmbientTrack Particle(float drag, float rise)
        {
            var emitter = new VfxAmbientTrack { ParticleScene = GD.Load<PackedScene>("res://scenes/vfx/ElementDust.tscn"),
                Amount = 1, Seed = 19, EmitDuration = 0, Lifetime = 2, Velocity = new(150,0),
                Spread = Vector2.Zero, Drag = drag, ScreenRiseAcceleration = rise };
            holder.AddChild(emitter); return emitter;
        }
        var free = Particle(0,0); var slowed = Particle(1.15f,0); var lifted = Particle(1.15f,65);
        foreach (var emitter in new[] { free, slowed, lifted }) emitter.Sample(.6f,false,0,0,false);
        var plain = free.GetChild<Sprite2D>(0); var slow = slowed.GetChild<Sprite2D>(0); var rise = lifted.GetChild<Sprite2D>(0);
        Vector2 lift = rise.GlobalPosition - slow.GlobalPosition;
        if (slow.Position.X <= 0 || slow.Position.X >= plain.Position.X || lift.Y >= -1 || Mathf.Abs(lift.X) > .001f)
            throw new Exception("Jet drag reversed travel or buoyancy rotated with the aim direction.");
        var pose = rise.Transform;
        lifted.Sample(.6f,false,0,0,true); var still = rise.Transform;
        lifted.Sample(.7f,false,0,0,true);
        if (rise.Position != still.Origin || pose.Origin == still.Origin)
            throw new Exception("Reduced-motion jet retained drag/buoyancy travel.");
        holder.Free();
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
        foreach(var control in new[] { "CastSpeed", "MotionSpeed", "ParticleSize", "Density", "Width" })
            if(Math.Abs(preview.GetNode<HSlider>("%"+control).Value-1)>.001)
                throw new Exception($"Preview default {control} disagrees with invocation defaults.");
        if(Math.Abs(preview.GetNode<HSlider>("%FlightTime").Value-.78)>.001)
            throw new Exception("Preview flight default disagrees with authored duration.");
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
        Tick(75);
        if (player.ActiveCount != 1 || player.GetChild<VfxInstance>(0).GetInstanceId() == originalId)
            throw new Exception("Firefall preview failed to repeat after its completed outro.");
        preview.GetNode<HSlider>("%CastSpeed").Value = .5;
        preview.GetNode<HSlider>("%ParticleSize").Value = 1.5;
        preview.GetNode<HSlider>("%Density").Value = 2;
        preview.GetNode<Button>("%Skill").EmitSignal(Button.SignalName.Pressed);
        var skill = player.GetChild<VfxInstance>(0).Playback.Parameters;
        if(Mathf.Abs(skill.CastSpeed-.5f)>.001f || Mathf.Abs(skill.ParticleScale-1.5f)>.001f || Mathf.Abs(skill.Density-2)>.001f)
            throw new Exception($"Skill preview ignored the visible playback controls: {skill}.");
        preview.GetNode<Button>("%End").EmitSignal(Button.SignalName.Pressed);
        Tick(20);
        if(player.ActiveCount!=0) throw new Exception("Skill preview did not respond to explicit cancellation.");
        canvas.Free();
    }
}
