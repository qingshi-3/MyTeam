using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Content;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;
using TowerAutobattler.Presentation;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Focused production entry + battle review. All pointer input stays in a 960x540 SubViewport.
public partial class BattleEnvironmentReview : Node
{
    private UiInputStage _stage = null!;
    private string _output = "";
    public override async void _Ready()
    {
        GameRoot? game = null;
        var saveNamespace = "tests/battle-environment/" + Guid.NewGuid().ToString("N");
        var saves = new SaveService(saveNamespace);
        try
        {
            Engine.MaxFps = 30;
            var outputArg = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--output-dir="));
            _output = ProjectSettings.GlobalizePath(outputArg is null
                ? "res://outputs/battle-environment-dynamic-20260930" : outputArg["--output-dir=".Length..]);
            Directory.CreateDirectory(Path.Combine(_output, "after-frames"));
            _stage = new UiInputStage(this, true);
            _stage.Resize(new(960, 540));
            if (OS.GetCmdlineUserArgs().Contains("--global-pages"))
            {
                await ReviewGlobalPages(saveNamespace);
                GD.Print("GLOBAL_COURTYARD_OK actual menu/settings/opening/deployment/route inputs; continuous shared backdrop");
                GetTree().Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--run-battle-only"))
            {
                await ReviewRunBattle(saveNamespace);
                GD.Print("BATTLE_ENVIRONMENT_RUN_OK actual route/start/pause/rules input; traits visible; viewport shading");
                GetTree().Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--environment-only"))
            {
                await ReviewEnvironmentMotion();
                GD.Print("BATTLE_ENVIRONMENT_MOTION_OK authored prefab; 16-second normal clock; no desktop input");
                GetTree().Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--layers-only"))
            {
                await ReviewLightingLayers();
                GD.Print("BATTLE_ENVIRONMENT_LAYERS_OK authored prefab; isolated surface/air; instance material ownership");
                GetTree().Quit();
                return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--deployment-only"))
            {
                await ReviewDeployment();
                GD.Print("BATTLE_ENVIRONMENT_DEPLOYMENT_OK 960x540 production board; actual selection and drag; memory-only fixture");
                GetTree().Quit();
                return;
            }
            game = GD.Load<PackedScene>("res://scenes/app/GrowthGameRoot.tscn").Instantiate<GameRoot>();
            game.SaveNamespace = saveNamespace;
            _stage.AddChild(game);
            await Until(() => game.Content is not null, "production package publishes");
            var screens = game.GetNode<AppScreenHost>("Screens");
            var backdrop = game.GetNode<BattleEnvironment>("Background");
            Require(backdrop.GetGlobalRect() == _stage.Viewport.GetVisibleRect(), "entry background fills the viewport");
            await Capture("main-menu.png");
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/ToolsButton"));
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/BattleLabButton"));
            await Until(() => screens.BattleLab.IsVisibleInTree(), "real menu opens battle lab");
            await Capture("deployment-preview.png");
            var start = screens.BattleLab.GetNode<Button>("%StartButton");
            if (_stage.Viewport.GetVisibleRect().HasPoint(start.GetGlobalRect().GetCenter())) await Click(start);
            else
            {
                // The existing lab footer exceeds the 960px QA host. Exercise
                // the real button through keyboard input, without expanding the viewport.
                start.GrabFocus();
                await Frame();
                foreach (var pressed in new[] { true, false })
                {
                    _stage.Push(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = pressed });
                    await Frame();
                }
                GD.Print("BATTLE_ENVIRONMENT_START_INPUT keyboard; existing lab footer outside 960x540");
            }
            await Until(() => screens.Battle.IsVisibleInTree() && screens.Battle.HasActiveBattle, "real start battle");
            var battle = screens.Battle;
            var environment = battle.GetNode<BattleEnvironment>("BattleEnvironment");
            Require(!backdrop.Visible, "battle draws only its own scenery");
            Require(environment.GetGlobalRect() == _stage.Viewport.GetVisibleRect(), "battle scenery fills the viewport");
            var actors = battle.GetNode<Node2D>("%UnitsRoot").GetChildren().OfType<UnitContentRoot>().ToArray();
            Require(actors.Length >= 2, "production actors present");
            foreach (var actor in actors.Where(a => a.Readability is not null))
            {
                var material = (ShaderMaterial)actor.GetNode<AnimatedSprite2D>("VisualRoot/UnitAnimationComponent/AnimatedSprite2D").Material;
                Require(material.GetShaderParameter("environment_enabled").AsBool(), "actor receives stage lighting");
            }
            var frames = new List<Image>();
            var times = new List<ulong>();
            for (int i = 0; i < 240; i++)
            {
                await Frame();
                Require(battle.LastRuntimeFailure.Length == 0, battle.LastRuntimeFailure);
                frames.Add(_stage.Viewport.GetTexture().GetImage());
                times.Add(Time.GetTicksMsec());
            }
            await Capture("battle.png");
            await Click(battle.GetNode<Button>("%PauseButton"));
            Require(battle.IsPaused, "real pause input");
            var tick = battle.TickIndex;
            var clock = environment.ElapsedSeconds;
            var surface = (ShaderMaterial)environment.GetNode<TextureRect>("Ground").Material;
            var air = (ShaderMaterial)environment.GetNode<ColorRect>("SunShafts").Material;
            var actorLight = (ShaderMaterial)actors.First(a => a.Readability is not null)
                .GetNode<AnimatedSprite2D>("VisualRoot/UnitAnimationComponent/AnimatedSprite2D").Material;
            Require(surface.GetShaderParameter("environment_elapsed").AsSingle() == clock &&
                air.GetShaderParameter("environment_elapsed").AsSingle() == clock &&
                actorLight.GetShaderParameter("environment_elapsed").AsSingle() == clock,
                "paving, shafts and actors share the dynamic lighting clock");
            var poses = actors.Select(a => a.Position).ToArray();
            for (int i = 0; i < 6; i++) await Frame();
            Require(battle.TickIndex == tick && environment.ElapsedSeconds == clock &&
                actors.Select(a => a.Position).SequenceEqual(poses), "pause freezes simulation, actors and air layers");
            Require(surface.GetShaderParameter("environment_elapsed").AsSingle() == clock &&
                air.GetShaderParameter("environment_elapsed").AsSingle() == clock &&
                actorLight.GetShaderParameter("environment_elapsed").AsSingle() == clock,
                "real pause freezes every lighting material");
            await Click(battle.GetNode<Button>("%RuleButton"));
            Require(battle.GetNode<Control>("%RulesPopup").IsVisibleInTree(), "rules popup remains usable");
            for (int i = 0; i < 12; i++) await Frame();
            await Capture("rules.png");
            await Click(battle.GetNode<Button>("RulesPopup/Panel/Inset/Layout/Header/Close"));
            await Until(() => !battle.GetNode<Control>("%RulesPopup").IsVisibleInTree(), "rule close animation completes");
            await ClickAt(actors.First(a => a.Visible).GlobalPosition);
            Require(battle.GetNode<Control>("%BattleInspectorDock").GetNode<Control>("InspectorPopup").Visible,
                "stage layers do not intercept unit selection");
            for (int i = 0; i < 12; i++) await Frame();
            await Capture("unit-details.png");
            await Click(battle.GetNode<Control>("%BattleInspectorDock").GetNode<Button>("%CloseInspector"));
            await Until(() => !battle.GetNode<Control>("%BattleInspectorDock").GetNode<Control>("InspectorPopup").Visible,
                "unit popup close animation completes");
            await Click(battle.GetNode<Button>("%StepButton"));
            Require(battle.TickIndex == tick + 1 && environment.ElapsedSeconds > clock, "real step advances both clocks");
            await Click(battle.GetNode<Button>("%PauseButton"));
            Require(!battle.IsPaused, "real resume input");
            await Click(battle.GetNode<Button>("%SpeedButton"));
            Require(battle.GetNode<Button>("%SpeedButton").Text.Contains("x2"), "real speed input");
            environment.ReducedMotion = true;
            clock = environment.ElapsedSeconds;
            environment.Advance(1);
            Require(environment.ElapsedSeconds == clock, "reduced motion retains static lighting");
            Require(surface.GetShaderParameter("environment_elapsed").AsSingle() == clock &&
                air.GetShaderParameter("reduced_motion").AsBool(), "reduced motion freezes shadows and removes dust");
            await Frame();
            await Capture("reduced-motion.png");
            battle.StopBattle();
            Require(environment.ElapsedSeconds == 0 && battle.GetNode<Node2D>("%UnitsRoot").GetChildCount() == 0,
                "battle cleanup resets environment clock and removes actors");
            for (int i = 0; i < frames.Count; i++)
            {
                frames[i].SavePng(Path.Combine(_output, $"after-frames/{i:0000}.png"));
                frames[i].Dispose();
            }
            File.WriteAllText(Path.Combine(_output, "after-frame-times.json"), System.Text.Json.JsonSerializer.Serialize(times));
            GD.Print($"BATTLE_ENVIRONMENT_RECORDING frames={times.Count} wall_ms={times[^1]-times[0]} simulation_tick={tick}");
            game.Free();
            game = null;
            saves.DeleteActiveRun();
            await Frame();
            GD.Print("BATTLE_ENVIRONMENT_REVIEW_OK 960x540 production entry, real lab start, pause/resume/step/speed/rules/selection; reduced-motion; cleanup");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PrintErr("BATTLE_ENVIRONMENT_REVIEW_FAILED " + error);
            game?.Free();
            GetTree().Quit(1);
        }
    }

    private async Task Click(Control control) => await ClickAt(control.GetGlobalRect().GetCenter());

    private async Task ReviewGlobalPages(string saveNamespace)
    {
        var game = GD.Load<PackedScene>("res://scenes/app/GrowthGameRoot.tscn").Instantiate<GameRoot>();
        game.SaveNamespace = saveNamespace;
        try
        {
            _stage.AddChild(game);
            await Until(() => game.Content is not null, "global root publishes");
            var appField = typeof(GameRoot).GetField("_app", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var original = (RunApplication)appField.GetValue(game)!;
            var app = new RunApplication(original.Content, new MemorySave("null"), original.Project, original.GrowthRules);
            appField.SetValue(game, app);
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            var backdrop = game.GetNode<BattleEnvironment>("Background");
            var initialClock = backdrop.ElapsedSeconds;
            await Capture("main-menu.png");
            await Activate(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/SettingsButton"));
            await Until(() => screens.Settings.IsVisibleInTree(), "native settings input");
            await Capture("settings.png");
            await Activate(screens.Settings.GetNode<Button>("Center/Panel/Layout/SaveButton"));
            await Until(() => screens.MainMenu.IsVisibleInTree(), "native settings return");
            await Activate(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
            await Until(() => screens.HeroSelection.IsVisibleInTree(), "native opening input");
            // Deterministic legal opening in the memory-only application; selection remains real input.
            Require(app.BeginOpeningRecruitment(2), "stable opening fixture");
            screens.HeroSelection.BindOpening(app);
            for (int i = 0; i < 6; i++) await Frame();
            await Capture("opening.png");
            foreach (var id in new[] { "hero_mx25", "hero_mx21" })
                await ActivateKey(Descendants<HeroLibraryTile>(screens.HeroSelection).First(tile => tile.StableId == id));
            await Activate(screens.HeroSelection.GetNode<Button>("%ConfirmOpening"));
            await Until(() => screens.Tower.IsVisibleInTree(), "native opening confirm reaches growth route");
            Require(backdrop.IsVisibleInTree() && backdrop.ElapsedSeconds > initialClock &&
                backdrop.GetGlobalRect() == _stage.Viewport.GetVisibleRect(), "same full-viewport backdrop survives navigation");
            await Capture("tower.png");
            if (!OS.GetCmdlineUserArgs().Contains("--baseline"))
            {
                var frames = new List<Image>();
                var times = new List<ulong>();
                var clocks = new List<float>();
                for (int i = 0; i < 240; i++)
                {
                    await Frame();
                    frames.Add(_stage.Viewport.GetTexture().GetImage());
                    times.Add(Time.GetTicksMsec());
                    clocks.Add(backdrop.ElapsedSeconds);
                }
                Require(clocks[^1] - clocks[0] > 7, "noncombat weather advances in real time");
                for (int i = 0; i < frames.Count; i++)
                {
                    Require(frames[i].SavePng(Path.Combine(_output, $"after-frames/{i:0000}.png")) == Error.Ok, "global frame");
                    frames[i].Dispose();
                }
                File.WriteAllText(Path.Combine(_output, "after-frame-times.json"), System.Text.Json.JsonSerializer.Serialize(times));
                File.WriteAllText(Path.Combine(_output, "environment-clocks.json"), System.Text.Json.JsonSerializer.Serialize(clocks));
                GD.Print($"GLOBAL_COURTYARD_RECORDING frames={times.Count} wall_ms={times[^1] - times[0]} lighting_seconds={clocks[^1] - clocks[0]}");
                app.Settings.ReduceUiMotion = true;
                var frozen = backdrop.ElapsedSeconds;
                for (int i = 0; i < 6; i++) await Frame();
                Require(backdrop.ElapsedSeconds == frozen, "noncombat reduced-motion setting freezes backdrop");
                app.Settings.ReduceUiMotion = false;
            }
            await Activate(Descendants<ChoiceCard>(screens.Tower).First(card => card.StableId == "Combat"));
            await Until(() => screens.Deployment.IsVisibleInTree(), "route input remains usable over global backdrop");
            await Capture("deployment.png");
            await Activate(screens.Deployment.GetNode<Button>("%StartBattleButton"));
            await Until(() => screens.Battle.IsVisibleInTree() && screens.Battle.HasActiveBattle, "native battle handoff");
            Require(!backdrop.IsVisibleInTree(), "global backdrop yields to battle lighting without double draw");
            await Activate(screens.Battle.GetNode<Button>("%PauseButton"));
            Require(screens.Battle.IsPaused, "native battle pause after global navigation");
            await Capture("battle-handoff.png");
            screens.Battle.StopBattle();
        }
        finally { game.Free(); }
    }

    // The Lab has no Run header/sidebar. Exercise the production layout that reserves both.
    private async Task ReviewRunBattle(string saveNamespace)
    {
        var game = GD.Load<PackedScene>("res://scenes/app/GrowthGameRoot.tscn").Instantiate<GameRoot>();
        game.SaveNamespace = saveNamespace;
        try
        {
            _stage.AddChild(game);
            await Until(() => game.Content is not null, "run content publishes");
            var appField = typeof(GameRoot).GetField("_app", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var original = (RunApplication)appField.GetValue(game)!;
            var app = new RunApplication(original.Content, new MemorySave("null"), original.Project, original.GrowthRules);
            Require(app.BeginOpeningRecruitment(2) && app.ToggleOpeningHero("hero_mx25") &&
                app.ToggleOpeningHero("hero_mx21") && app.ConfirmOpeningRecruitment(), "valid memory-only opening");
            appField.SetValue(game, app);
            game.Flow.ShowTower();
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            var traits = game.GetNode<ArmyOverviewController>("ArmyOverview").GetNode<RunTraitPanel>("%RunTraitPanel");
            for (int i = 0; i < 4; i++) await Frame();
            await Activate(Descendants<ChoiceCard>(screens.Tower).First(card => card.StableId == "Combat"));
            await Until(() => screens.Deployment.IsVisibleInTree(), "actual route opens preparation");
            await Activate(screens.Deployment.GetNode<Button>("%StartBattleButton"));
            await Until(() => screens.Battle.IsVisibleInTree() && screens.Battle.HasActiveBattle, "actual run battle starts");
            var battle = screens.Battle;
            if (OS.GetCmdlineUserArgs().Contains("--record-run")) await RecordRunLighting(battle);
            await Activate(battle.GetNode<Button>("%PauseButton"));
            Require(battle.IsPaused && traits.IsVisibleInTree(), "pause input with persistent traits");
            var environment = battle.GetNode<BattleEnvironment>("BattleEnvironment");
            var header = (Control)battle.FindChild("HeaderShade", true, false);
            var footer = (Control)battle.FindChild("FooterShade", true, false);
            for (int i = 0; i < 4; i++) await Frame();
            await Capture("battle.png");
            static float[] Rect(Control control)
            {
                var rect = control.GetGlobalRect();
                return [rect.Position.X, rect.Position.Y, rect.Size.X, rect.Size.Y];
            }
            File.WriteAllText(Path.Combine(_output, "layout.json"), System.Text.Json.JsonSerializer.Serialize(new
            {
                viewport = new[] { 0, 0, 960, 540 }, battle = Rect(battle), environment = Rect(environment),
                header = Rect(header), footer = Rect(footer), traits = Rect(traits), tick = battle.TickIndex
            }));
            Require(environment.GetGlobalRect() == _stage.Viewport.GetVisibleRect(), "run scenery covers viewport");
            Require(battle.GlobalPosition.X > 0 && battle.GlobalPosition.Y > 0, "real run reserves sidebar and header");
            if (!OS.GetCmdlineUserArgs().Contains("--baseline"))
            {
                Require(header.GetGlobalRect().Position.IsEqualApprox(Vector2.Zero) &&
                    Mathf.IsEqualApprox(header.Size.X, 960), "top shade starts at viewport origin and spans its width");
                Require(Mathf.IsEqualApprox(footer.GlobalPosition.X, 0) &&
                    Mathf.IsEqualApprox(footer.GetGlobalRect().End.Y, 540) && Mathf.IsEqualApprox(footer.Size.X, 960),
                    "bottom shade spans the viewport and reaches its bottom");
            }
            await Activate(battle.GetNode<Button>("%RuleButton"));
            Require(battle.GetNode<Control>("%RulesPopup").IsVisibleInTree(), "run rule popup input");
            for (int i = 0; i < 12; i++) await Frame();
            await Capture("rules.png");
            await Activate(battle.GetNode<Button>("RulesPopup/Panel/Inset/Layout/Header/Close"));
            await Until(() => !battle.GetNode<Control>("%RulesPopup").IsVisibleInTree(), "run rule close input");
            await Activate(battle.GetNode<Button>("%PauseButton"));
            Require(!battle.IsPaused, "run resume input");
            await Activate(battle.GetNode<Button>("%PauseButton"));
            Require(battle.IsPaused && battle.LastRuntimeFailure.Length == 0, "run pause remains usable");
            battle.StopBattle();
        }
        finally { game.Free(); }
    }

    private async Task RecordRunLighting(BattleScreenController battle)
    {
        var environment = battle.GetNode<BattleEnvironment>("BattleEnvironment");
        var surface = (ShaderMaterial)environment.GetNode<TextureRect>("Ground").Material;
        var air = (ShaderMaterial)environment.GetNode<ColorRect>("SunShafts").Material;
        var frames = new List<Image>();
        var times = new List<ulong>();
        var clocks = new List<float>();
        if (!OS.GetCmdlineUserArgs().Contains("--baseline"))
        {
            await Activate(battle.GetNode<Button>("%PauseButton"));
            await CaptureRunLightPair(environment, "start");
            await Activate(battle.GetNode<Button>("%PauseButton"));
        }
        for (int i = 0; i < 300; i++)
        {
            await Frame();
            Require(battle.HasActiveBattle && !battle.IsEnding && battle.LastRuntimeFailure.Length == 0,
                "actual run remains in combat during recording");
            Require(surface.GetShaderParameter("environment_elapsed").AsSingle() == environment.ElapsedSeconds &&
                air.GetShaderParameter("environment_elapsed").AsSingle() == environment.ElapsedSeconds,
                "actual run updates both lighting material clocks");
            frames.Add(_stage.Viewport.GetTexture().GetImage());
            times.Add(Time.GetTicksMsec());
            clocks.Add(environment.ElapsedSeconds);
        }
        Require(clocks[^1] - clocks[0] > 7.0f, "actual run lighting clock advances at normal speed");
        await Activate(battle.GetNode<Button>("%PauseButton"));
        Require(battle.IsPaused, "native pause after motion recording");
        var frozen = environment.ElapsedSeconds;
        for (int i = 0; i < 6; i++) await Frame();
        Require(environment.ElapsedSeconds == frozen, "clouds and beams freeze together");
        if (!OS.GetCmdlineUserArgs().Contains("--baseline")) await CaptureRunLightPair(environment, "end");
        // Source images are encoded only after capture, with the battle actually paused.
        for (int i = 0; i < frames.Count; i++)
        {
            Require(frames[i].SavePng(Path.Combine(_output, $"after-frames/{i:0000}.png")) == Error.Ok, "recorded run frame");
            frames[i].Dispose();
        }
        File.WriteAllText(Path.Combine(_output, "after-frame-times.json"), System.Text.Json.JsonSerializer.Serialize(times));
        File.WriteAllText(Path.Combine(_output, "environment-clocks.json"), System.Text.Json.JsonSerializer.Serialize(clocks));
        GD.Print($"BATTLE_ENVIRONMENT_RUN_RECORDING frames={times.Count} wall_ms={times[^1] - times[0]} lighting_seconds={clocks[^1] - clocks[0]}");
        await Activate(battle.GetNode<Button>("%PauseButton"));
        Require(!battle.IsPaused, "native resume after recording");
    }

    private async Task CaptureRunLightPair(BattleEnvironment environment, string phase)
    {
        var shafts = environment.GetNode<ColorRect>("SunShafts");
        await Frame();
        await Capture($"{phase}-composite.png");
        shafts.Hide();
        await Frame();
        await Capture($"{phase}-surface.png");
        shafts.Show();
        await Frame();
    }

    private async Task ActivateKey(Button button)
    {
        button.GrabFocus();
        for (int i = 0; i < 4; i++) await Frame();
        foreach (var pressed in new[] { true, false })
        {
            _stage.Push(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = pressed });
            await Frame();
        }
    }

    private async Task Activate(Button button)
    {
        if (_stage.Viewport.GetVisibleRect().HasPoint(button.GetGlobalRect().GetCenter())) await Click(button);
        else
        {
            button.GrabFocus();
            await Frame();
            foreach (var pressed in new[] { true, false })
            {
                _stage.Push(new InputEventKey { Keycode = Key.Enter, PhysicalKeycode = Key.Enter, Pressed = pressed });
                await Frame();
            }
            GD.Print("BATTLE_ENVIRONMENT_RUN_INPUT keyboard for existing out-of-viewport control: " + button.Name);
        }
    }

    private static IEnumerable<T> Descendants<T>(Node node) where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T typed) yield return typed;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    // Longer than the rejected global sway cycle. No units or HUD obscure the shadow motion.
    private async Task ReviewEnvironmentMotion()
    {
        var environment = GD.Load<PackedScene>("res://scenes/environments/SunlitCourtyard.tscn")
            .Instantiate<BattleEnvironment>();
        _stage.AddChild(environment);
        await EnvironmentFrame();
        var frames = new List<Image>();
        var times = new List<ulong>();
        var clocks = new List<float>();
        for (int i = 0; i < 240; i++)
        {
            await EnvironmentFrame();
            await EnvironmentFrame();
            frames.Add(_stage.Viewport.GetTexture().GetImage());
            times.Add(Time.GetTicksMsec());
            clocks.Add(environment.ElapsedSeconds);
        }
        environment.SetProcess(false);
        var clock = environment.ElapsedSeconds;
        Require(clock > 15, "environment uses elapsed real time");
        for (int i = 0; i < 6; i++) await EnvironmentFrame();
        Require(environment.ElapsedSeconds == clock, "idle environment can freeze at the current phase");
        await Capture("environment.png");
        for (int i = 0; i < frames.Count; i++)
        {
            Require(frames[i].SavePng(Path.Combine(_output, $"after-frames/{i:0000}.png")) == Error.Ok, "motion source frame");
            frames[i].Dispose();
        }
        File.WriteAllText(Path.Combine(_output, "after-frame-times.json"), System.Text.Json.JsonSerializer.Serialize(times));
        File.WriteAllText(Path.Combine(_output, "environment-clocks.json"), System.Text.Json.JsonSerializer.Serialize(clocks));
        GD.Print($"BATTLE_ENVIRONMENT_MOTION_RECORDING frames={times.Count} wall_ms={times[^1]-times[0]} presentation_seconds={clocks[^1]-clocks[0]}");
        environment.Free();
        await EnvironmentFrame();
    }

    private async Task EnvironmentFrame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        // Request rendering for the standalone SubViewport without waiting on its draw signal.
        // The authored environment still advances through its own real process clock.
        RenderingServer.ForceDraw(false);
    }

    private async Task ReviewLightingLayers()
    {
        var scene = GD.Load<PackedScene>("res://scenes/environments/SunlitCourtyard.tscn");
        var environment = scene.Instantiate<BattleEnvironment>();
        environment.AutoAnimate = false;
        _stage.AddChild(environment);
        await Frame();
        environment.Advance(2f);
        await Frame();
        await Capture("layers-composite.png");
        var ground = environment.GetNode<TextureRect>("Ground");
        var air = environment.GetNode<ColorRect>("SunShafts");
        air.Visible = false;
        await Frame();
        await Capture("layers-surface.png");
        ground.Visible = false;
        air.Visible = true;
        await Frame();
        await Capture("layers-air.png");
        var other = scene.Instantiate<BattleEnvironment>();
        other.AutoAnimate = false;
        other.Visible = false;
        _stage.AddChild(other);
        Require(ground.Material != other.GetNode<TextureRect>("Ground").Material &&
            air.Material != other.GetNode<ColorRect>("SunShafts").Material && other.ElapsedSeconds == 0,
            "each instance owns its lighting state");
        other.Free();
        environment.Free();
        await Frame();
    }

    private async Task ReviewDeployment()
    {
        var gate = await GamePackagePublisher.CreateReadyAsync(this,
            GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
        var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
        var fixture = File.ReadAllText(ProjectSettings.GlobalizePath("res://tests/fixtures/first-boss-ranged-stall.json"));
        var app = new RunApplication(package.Content, new MemorySave(fixture), package.Project);
        var run = app.ActiveRun ?? throw new InvalidOperationException("deployment fixture rejected");
        var root = GD.Load<PackedScene>("res://scenes/app/GameRoot.tscn").Instantiate<GameRoot>();
        root.SaveNamespace = "tests/environment-deployment/" + Guid.NewGuid().ToString("N");
        _stage.AddChild(root);
        await Until(() => root.Content is not null, "production deployment root publishes");
        root.Flow.Dispose();
        var screens = root.GetNode<AppScreenHost>("Screens");
        var screen = screens.Deployment;
        var encounter = app.CurrentEncounter();
        var commands = 0;
        screen.MoveRequested += command =>
        {
            Require(app.ApplyFormationCommand(command, screen.FloorRule!), "native drag commits legal move");
            commands++;
            screen.Bind(app, encounter);
        };
        screens.BindEquipmentManagement(app);
        screen.Bind(app, encounter);
        screens.Show(AppScreenId.Deployment, RunView.For(run), app.Content, app.Rules);
        for (int i = 0; i < 6; i++) await Frame();
        var cells = screen.GetNode<DeploymentBoard>("%DeploymentBoard").GetChildren().OfType<DeploymentCell>().ToArray();
        var source = cells.First(c => c.PieceId.Length > 0 && _stage.Viewport.GetVisibleRect().HasPoint(c.GetGlobalRect().GetCenter()));
        var target = cells.First(c => c.PieceId.Length == 0 && c.IsLegalTarget &&
            _stage.Viewport.GetVisibleRect().HasPoint(c.GetGlobalRect().GetCenter()));
        var id = source.PieceId;
        await Click(source);
        Require(commands == 0, "selection does not place a unit");
        await Capture("deployment.png");
        var from = source.GetGlobalRect().GetCenter();
        var to = target.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseMotion { Position = from, GlobalPosition = from });
        _stage.Push(new InputEventMouseButton { Position = from, GlobalPosition = from,
            ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        await Frame();
        var previous = from;
        for (int i = 1; i <= 8; i++)
        {
            var point = from.Lerp(to, i / 8f);
            _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point,
                Relative = point - previous, ButtonMask = MouseButtonMask.Left });
            previous = point;
            await Frame();
        }
        _stage.Push(new InputEventMouseButton { Position = to, GlobalPosition = to,
            ButtonIndex = MouseButton.Left, Pressed = false });
        await Until(() => commands == 1, "actual deployment drag commits once");
        Require(target.PieceId == id, "drag lands in the highlighted cell");
        await Capture("deployment-after-drag.png");
        root.Free();
        await Frame();
    }

    private sealed class MemorySave(string fixture) : IRunSaveService
    {
        private string? _json = fixture;
        public MetaProgressDto LoadMeta() => new();
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => _json is null ? null : System.Text.Json.JsonSerializer.Deserialize<ActiveRunDto>(_json);
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) { _json = System.Text.Json.JsonSerializer.Serialize(value); return true; }
        public void DeleteActiveRun() => _json = null;
    }
    private async Task ClickAt(Vector2 point)
    {
        Require(_stage.Viewport.GetVisibleRect().HasPoint(point), "input stays inside viewport: " + point);
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await Frame();
        foreach (var pressed in new[] { true, false })
        {
            _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, ButtonMask = pressed ? MouseButtonMask.Left : 0, Pressed = pressed });
            await Frame();
        }
    }
    private async Task Until(Func<bool> condition, string label)
    {
        for (int i = 0; i < 600 && !condition(); i++) await Frame();
        Require(condition(), label);
        await Frame();
    }
    private Task Capture(string path)
    {
        using var image = _stage.Viewport.GetTexture().GetImage();
        var error = image.SavePng(Path.Combine(_output, path));
        Require(error == Error.Ok, "capture " + path);
        return Task.CompletedTask;
    }
    private async Task Frame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (OS.GetCmdlineUserArgs().Contains("--run-battle-only") || OS.GetCmdlineUserArgs().Contains("--global-pages"))
        {
            // A paused, otherwise static Run screen still needs a render for input QA.
            RenderingServer.ForceDraw(false);
            return;
        }
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
    }
    private static void Require(bool value, string label)
    { if (!value) throw new InvalidOperationException(label); }
}
