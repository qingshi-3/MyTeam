using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.App;
using TowerAutobattler.Battle;
using TowerAutobattler.Components;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Growth;
using TowerAutobattler.Presentation;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Rendered production-path probe for movement regressions seen during an ordinary Growth battle.
// The diagnostic config keeps attacks harmless and opponents distant so the three content families
// remain in sustained travel long enough to observe real rendered frames.
public partial class BattleMovementRenderSmoke : Node
{
    private const string OutputPath = "res://.godot/ui-review/battle-movement";
    private static readonly string[] ProbeIds = ["growth-mx", "growth-gx", "alpha-hero"];
    private UiInputStage _stage = null!;

    public override async void _Ready()
    {
        var exit = 0;
        GrowthGameRoot? game = null;
        BattleScreenController? battle = null;
        var testNamespace = $"tests/battle-movement-render/{Guid.NewGuid():N}";
        var testSave = new SaveService(testNamespace);
        var productionSave = new SaveService("growth-journey");
        var productionBefore = JsonSerializer.Serialize(productionSave.LoadActiveRun());
        try
        {
            Require(DisplayServer.GetName() != "headless", "movement render probe requires the rendered driver");
            _stage = new UiInputStage(this, true);
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(OutputPath));
            if (OS.GetCmdlineUserArgs().Contains("--saved-roster-only"))
            {
                await RunSavedRosterOnly();
                GD.Print("BATTLE_MOVEMENT_SAVED_ROSTER_OK");
                GetTree().Quit();
                return;
            }

            game = GD.Load<PackedScene>("res://scenes/app/GrowthGameRoot.tscn").Instantiate<GrowthGameRoot>();
            game.SaveNamespace = testNamespace;
            _stage.AddChild(game);
            await Until(() => game.Content is not null, "GrowthGameRoot publishes its real content package");
            var screens = game.GetNode<AppScreenHost>(game.ScreenHostPath);
            await Click(screens.MainMenu.GetNode<Button>("Center/Panel/Menu/NewRunButton"));
            await Until(() => screens.HeroSelection.IsVisibleInTree(), "real New Journey opens six-choice screen");
            var candidates = Descendants<HeroLibraryTile>(screens.HeroSelection)
                .Where(tile => tile.IsVisibleInTree()).ToArray();
            Require(candidates.Length == 6, "real Growth opening has six candidates");
            await Click(candidates[0]);
            await Click(candidates[1]);
            await Click(screens.HeroSelection.GetNode<Button>("%ConfirmOpening"));
            await Until(() => screens.Deployment.IsVisibleInTree(), "real opening enters deployment");
            await Click(screens.Deployment.GetNode<Button>("%StartBattleButton"));
            battle = screens.Battle;
            await Until(() => battle.IsVisibleInTree() && battle.HasActiveBattle, "real deployment starts first Growth battle");

            var journey = await SampleJourney(battle, TimeSpan.FromSeconds(4));
            AssertJourneyEvidence(journey);
            var peak = journey.Max(frame => frame.RenderMilliseconds);
            var over100 = journey.Count(frame => frame.RenderMilliseconds >= 100);
            GD.Print($"MOVEMENT_JOURNEY_TIMING frames={journey.Count} peak_ms={peak:0.###} over_100ms={over100} " +
                     $"average_ms={journey.Average(frame => frame.RenderMilliseconds):0.###}");
            WriteJourneyLog(journey);
            Require(JsonSerializer.Serialize(productionSave.LoadActiveRun()) == productionBefore,
                "isolated Growth journey does not overwrite the production save");

            var registry = game.Content!;
            battle.StopBattle();
            battle.StartBattle(registry, BuildConfig(registry), "受控持续移动诊断");
            await Until(() => ProbeIds.All(id => Motion(Presenter(battle, id)).IsMoving),
                "all MX/GX/alpha representatives enter production movement");

            var x1 = await SampleRun(battle, "x1", 28, capture: false);
            AssertContinuous(x1, "x1 default rendered movement", requireFrameChange: true);

            battle.StartBattle(registry, BuildConfig(registry), "受控持续移动诊断 pause");
            await Until(() => ProbeIds.All(id => Motion(Presenter(battle, id)).IsMoving), "pause reset enters movement");
            var pause = battle.GetNode<Button>("%PauseButton");
            await Click(pause);
            Require(battle.IsPaused, "real pause click pauses the battle");
            var paused = ReadFrame(battle, "paused-before");
            await RenderFrames(8);
            var frozen = ReadFrame(battle, "paused-after");
            foreach (var id in ProbeIds)
                Require(paused.Units[id].Position.IsEqualApprox(frozen.Units[id].Position) &&
                        paused.Units[id].SpriteFrame == frozen.Units[id].SpriteFrame,
                    $"pause freezes rendered position and walk frame for {id}");
            await Click(pause);
            Require(!battle.IsPaused, "second real pause click resumes the battle");

            battle.StartBattle(registry, BuildConfig(registry), "受控持续移动诊断 x2");
            await Until(() => ProbeIds.All(id => Motion(Presenter(battle, id)).IsMoving), "x2 reset enters movement");
            var speed = battle.GetNode<Button>("%SpeedButton");
            await Click(speed);
            Require(Mathf.IsEqualApprox(battle.SpeedScale, 2f), "real speed click selects x2");
            var x2 = await SampleRun(battle, "x2", 16, capture: false);
            AssertContinuous(x2, "x2 rendered movement", requireFrameChange: false);
            battle.StartBattle(registry, BuildConfig(registry), "受控持续移动诊断 x4");
            await Until(() => ProbeIds.All(id => Motion(Presenter(battle, id)).IsMoving), "x4 reset enters movement");
            speed = battle.GetNode<Button>("%SpeedButton");
            await Click(speed);
            await Click(speed);
            Require(Mathf.IsEqualApprox(battle.SpeedScale, 4f), "real speed clicks select x4 after reset");
            var x4 = await SampleRun(battle, "x4", 12, capture: false);
            AssertMovingState(x4, "x4 rendered movement");

            battle.StartBattle(registry, BuildConfig(registry), "受控持续移动诊断 geometry");
            await Until(() => ProbeIds.All(id => Motion(Presenter(battle, id)).IsMoving), "geometry reset enters movement");
            var collision = battle.GetNode<Button>("%CollisionToggle");
            var beforeGeometry = ReadFrame(battle, "geometry-before");
            await Click(collision);
            Require(collision.ButtonPressed, "real geometry click enables collision overlay");
            var geometryOn = await SampleRun(battle, "geometry-on", 10, capture: false);
            AssertMovingState(geometryOn, "geometry-on");
            await Click(collision);
            Require(!collision.ButtonPressed, "second real geometry click disables collision overlay");
            var geometryOff = await SampleRun(battle, "geometry-off", 10, capture: false);
            AssertMovingState(geometryOff, "geometry-off");
            var attackRange = battle.GetNode<Button>("%AttackRangeToggle");
            await Click(attackRange);
            Require(attackRange.ButtonPressed, "real geometry click enables attack range overlay");
            var rangeOn = await SampleRun(battle, "range-on", 8, capture: false);
            AssertMovingState(rangeOn, "range-on");
            await Click(attackRange);
            Require(!attackRange.ButtonPressed, "second real geometry click disables attack range overlay");
            foreach (var id in ProbeIds)
                Require(beforeGeometry.Units[id].Position.DistanceTo(geometryOff[^1].Units[id].Position) > .1f,
                    $"geometry toggle does not stall {id}");

            battle.StartBattle(registry, BuildConfig(registry), "受控持续移动逐帧录制");
            await Until(() => ProbeIds.All(id => Motion(Presenter(battle, id)).IsMoving), "capture reset enters movement");
            await CaptureJourneySequence(battle, 60);

            foreach (var frame in x1.Concat(x2).Concat(x4).Concat(geometryOn).Concat(geometryOff))
                GD.Print(frame.Format());
            GD.Print("BATTLE_MOVEMENT_RENDER_OK content=growth-mx,growth-gx,alpha " +
                     $"journey_frames={journey.Count} peak_ms={peak:0.###} over_100ms={over100} " +
                     "input=real-growth-entry,pause,speed-x1-x2-x4,geometry-on-off captures=" + OutputPath);
        }
        catch (Exception error)
        {
            exit = 1;
            GD.PrintErr("BATTLE_MOVEMENT_RENDER_FAILED " + error);
        }
        finally
        {
            battle?.StopBattle();
            game?.QueueFree();
            await Frames(3);
            testSave.DeleteActiveRun();
        }
        GetTree().Quit(exit);
    }

    private async Task RunSavedRosterOnly()
    {
        var production = new SaveService("growth-journey");
        var activeJson = production.Serialize(production.LoadActiveRun() ??
            throw new InvalidOperationException("production Growth save has no active run"));
        var active = production.Deserialize<ActiveRunDto>(activeJson) ??
            throw new InvalidOperationException("production Growth save clone failed");
        var before = production.Serialize(production.LoadActiveRun());
        var publication = await GrowthContentPackage.CreateReadyAsync(this);
        var package = publication.Package ?? throw new InvalidOperationException(
            "Growth package publication failed: " + string.Join("; ", publication.Report.CoreErrors));
        var memory = new ReadOnlyRunSave(
            production.Deserialize<ActiveRunDto>(activeJson)!, production.LoadMeta(), production.LoadSettings());
        var app = new RunApplication(package.Content, memory, package.Project,
            GrowthContentPackage.LoadRules(package.Content));
        var encounter = app.CurrentEncounter();
        var config = app.BuildBattleConfig(encounter, false);
        var battle = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
        _stage.AddChild(battle);
        await RenderFrames(4);
        try
        {
            battle.StartBattle(package.Content, config, encounter.Title);
            Require(!battle.GetNode<Button>("%CollisionToggle").ButtonPressed &&
                    !battle.GetNode<Button>("%AttackRangeToggle").ButtonPressed,
                "saved-roster measurement begins with geometry off");
            var movement = await SampleJourney(battle, TimeSpan.FromSeconds(5));
            var playerContent = config.Spawns.Where(spawn => spawn.Team == 0)
                .Select(spawn => spawn.Unit.ContentId).ToArray();
            var anomalies = AnalyzeMovement(movement);

            var selectable = Descendants<UnitContentRoot>(battle).First(unit =>
                battle.ReadRuntimeUnits().Any(state => state.RuntimeId == unit.RuntimeId && state.Team == 0));
            await ClickAt(selectable.GlobalPosition);
            var inspector = battle.GetNode<BattleInspectorDock>("%BattleInspectorDock");
            Require(inspector.Details.Visible, "real unit click opens selected details");
            var statistics = inspector.GetNode<Button>("%StatisticsToggle");
            await Click(statistics);
            Require(inspector.GetNode<Control>("%InspectorPopup").Visible, "real statistics click opens stats panel");
            var stats = await SampleJourney(battle, TimeSpan.FromSeconds(2));
            await Click(statistics);

            var result = new
            {
                Source = "production-growth-save-read-only-memory-clone",
                Active = new { active.Seed, active.FloorIndex, active.BattleNumber, active.SelectedNode },
                PlayerContent = playerContent,
                Movement = Summarize(movement),
                StatsOpen = Summarize(stats),
                Anomalies = anomalies,
                Frames = movement
            };
            using var file = FileAccess.Open($"{OutputPath}/saved-roster.json", FileAccess.ModeFlags.Write);
            if (file is null) throw new InvalidOperationException("open saved roster json: " + FileAccess.GetOpenError());
            file.StoreString(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            GD.Print("MOVEMENT_SAVED_ROSTER_BRIEF " + JsonSerializer.Serialize(new
            {
                players = playerContent,
                movement = result.Movement,
                statsOpen = result.StatsOpen,
                anomalies
            }));
            Require(production.Serialize(production.LoadActiveRun()) == before,
                "saved-roster probe leaves production Growth save byte-equivalent as data");
        }
        finally
        {
            battle.StopBattle();
            battle.QueueFree();
            await Frames(3);
        }
    }

    private static object Summarize(IReadOnlyList<JourneyFrame> frames) => new
    {
        frames = frames.Count,
        meanMs = frames.Average(frame => frame.RenderMilliseconds),
        maxMs = frames.Max(frame => frame.RenderMilliseconds),
        over100Ms = frames.Count(frame => frame.RenderMilliseconds >= 100),
        movingFrames = frames.Count(frame => frame.Units.Values.Any(unit => unit.IsMoving))
    };

    private static object AnalyzeMovement(IReadOnlyList<JourneyFrame> frames)
    {
        var ids = frames.SelectMany(frame => frame.Units.Keys).Distinct().ToArray();
        var units = ids.Select(id =>
        {
            var samples = frames.Where(frame => frame.Units.TryGetValue(id, out var unit) &&
                    unit.IsMoving && unit.LogicalCue == "move")
                .Select(frame => frame.Units[id]).ToArray();
            var longest = 0;
            var run = 0;
            var previous = -1;
            foreach (var sample in samples)
            {
                run = sample.SpriteFrame == previous ? run + 1 : 1;
                previous = sample.SpriteFrame;
                longest = Math.Max(longest, run);
            }
            return new
            {
                id,
                movingSamples = samples.Length,
                notPlayingWhileMoving = samples.Count(sample => !sample.Playing),
                longestSameIntegerFrame = longest,
                nonLoopingMove = samples.Count(sample => sample.LoopMode == SpriteFrames.LoopMode.None.ToString())
            };
        }).ToArray();
        return new { units, suspicious = units.Count(unit => unit.notPlayingWhileMoving > 0 || unit.longestSameIntegerFrame > 30 || unit.nonLoopingMove > 0) };
    }

    private async Task ClickAt(Vector2 point)
    {
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await RenderOneFrame();
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = MouseButton.Left, Pressed = false });
        await RenderFrames(2);
    }

    private async Task<List<JourneyFrame>> SampleJourney(BattleScreenController battle, TimeSpan duration)
    {
        var frames = new List<JourneyFrame>();
        var watch = new Stopwatch();
        var elapsed = Stopwatch.StartNew();
        for (var index = 0; elapsed.Elapsed < duration && index < 2000; index++)
        {
            watch.Restart();
            await RenderOneFrame();
            watch.Stop();
            var units = Descendants<UnitContentRoot>(battle).Where(unit => !string.IsNullOrEmpty(unit.RuntimeId))
                .ToDictionary(unit => unit.RuntimeId, unit =>
                {
                    var motion = Motion(unit);
                    var animation = Animation(unit);
                    var sprite = unit.GetNode<AnimatedSprite2D>("VisualRoot/UnitAnimationComponent/AnimatedSprite2D");
                    return new JourneyUnitFrame(unit.Position, motion.IsMoving, animation.ActiveCue,
                        animation.ActiveLogicalCue, sprite.Frame, sprite.FrameProgress, sprite.IsPlaying(), sprite.SpeedScale,
                        sprite.SpriteFrames.GetAnimationLoopMode(sprite.Animation).ToString(),
                        sprite.SpriteFrames.GetAnimationSpeed(sprite.Animation));
                });
            var frame = new JourneyFrame(index, watch.Elapsed.TotalMilliseconds, units);
            frames.Add(frame);
        }
        Require(elapsed.Elapsed >= duration, $"captured the requested {duration.TotalSeconds:0.#} real seconds");
        return frames;
    }

    private async Task CaptureJourneySequence(BattleScreenController battle, int count)
    {
        var images = new List<Image>(count);
        for (var index = 0; index < count; index++)
        {
            await RenderFrames(2); // approximately 30 fps under the probe's max-fps 60 launch
            images.Add(_stage.Viewport.GetTexture().GetImage());
        }
        for (var index = 0; index < images.Count; index++)
        {
            var error = images[index].SavePng(ProjectSettings.GlobalizePath($"{OutputPath}/journey-{index:000}.png"));
            Require(error == Error.Ok, $"save buffered journey frame {index}: {error}");
        }
        GD.Print($"MOVEMENT_JOURNEY_CAPTURE frames={images.Count} cadence=every-2-render-frames path={OutputPath}");
    }

    private static void WriteJourneyLog(IEnumerable<JourneyFrame> frames)
    {
        using var file = FileAccess.Open($"{OutputPath}/journey-frames.log", FileAccess.ModeFlags.Write);
        if (file is null) throw new InvalidOperationException("open journey frame log: " + FileAccess.GetOpenError());
        foreach (var frame in frames) file.StoreLine(frame.Format());
    }

    private static void AssertJourneyEvidence(IReadOnlyList<JourneyFrame> frames)
    {
        var ids = frames.SelectMany(frame => frame.Units.Keys).Distinct().ToArray();
        Require(ids.Length >= 3, "real first battle renders both armies");
        var movers = ids.Where(id => frames.Any(frame => frame.Units.TryGetValue(id, out var unit) &&
            unit.IsMoving && unit.LogicalCue == "move" && unit.Playing)).ToArray();
        Require(movers.Length > 0, "real first battle exposes at least one rendered moving unit");
        Require(movers.Any(id => frames.Where(frame => frame.Units.ContainsKey(id))
                .Select(frame => frame.Units[id].Position).Distinct().Count() >= 3),
            "real first battle exposes stable intermediate rendered positions");
        Require(movers.Any(id => frames.Where(frame => frame.Units.ContainsKey(id))
                .Select(frame => (frame.Units[id].SpriteFrame, Bucket(frame.Units[id].FrameProgress))).Distinct().Count() >= 2),
            "real first battle advances an authored walk clip");
    }

    private static BattleConfig BuildConfig(ContentRegistry registry)
    {
        UnitSnapshot Probe(string id) => BattleSetupFactory.Snapshot(Entry(registry, id)) with
        {
            MaxHealth = 100000,
            Damage = 0,
            HealPower = 0,
            Range = .1f,
            AttackTicks = 1000,
            MoveTicks = 20
        };
        var enemy = BattleSetupFactory.Snapshot(Entry(registry, "enemy_rust_guard")) with
        {
            MaxHealth = 100000,
            Damage = 0,
            HealPower = 0,
            Range = .1f,
            AttackTicks = 1000,
            MoveTicks = 1000
        };
        return new BattleConfig
        {
            Seed = 20260927,
            FloorRule = new ClearFloorRuleRuntime("movement-render", "常规", "移动逐帧诊断"),
            HeroRule = HeroRuleSnapshot.Neutral,
            Spawns =
            [
                new BattleSpawn(Probe("hero_mx01"), 0, new Vector2I(0, 1), "growth-mx"),
                new BattleSpawn(Probe("gx02_venom_walker"), 0, new Vector2I(0, 3), "growth-gx"),
                new BattleSpawn(Probe("hero_hc01_crossbow"), 0, new Vector2I(0, 5), "alpha-hero"),
                new BattleSpawn(enemy, 1, new Vector2I(9, 1), "enemy-top"),
                new BattleSpawn(enemy, 1, new Vector2I(9, 3), "enemy-middle"),
                new BattleSpawn(enemy, 1, new Vector2I(9, 5), "enemy-bottom")
            ]
        };
    }

    private static CatalogEntry Entry(ContentRegistry registry, string id) =>
        registry.Catalog.Heroes.Cast<CatalogEntry>()
            .Concat(registry.Catalog.Soldiers)
            .Concat(registry.Catalog.Enemies)
            .Single(entry => entry.StableId == id);

    private async Task<List<RenderFrame>> SampleRun(BattleScreenController battle, string phase, int count, bool capture)
    {
        var frames = new List<RenderFrame>(count);
        for (var index = 0; index < count; index++)
        {
            await RenderOneFrame();
            var frame = ReadFrame(battle, $"{phase}-{index:00}");
            frames.Add(frame);
            if (capture) Capture($"{phase}-{index:00}.png");
        }
        return frames;
    }

    private static RenderFrame ReadFrame(BattleScreenController battle, string label)
    {
        var states = battle.ReadRuntimeUnits().ToDictionary(unit => unit.RuntimeId);
        var units = ProbeIds.ToDictionary(id => id, id =>
        {
            var presenter = Presenter(battle, id);
            var motion = Motion(presenter);
            var animation = Animation(presenter);
            var sprite = presenter.GetNode<AnimatedSprite2D>("VisualRoot/UnitAnimationComponent/AnimatedSprite2D");
            var authority = states[id];
            return new UnitFrame(presenter.Position, authority.Position, motion.IsMoving,
                animation.ActiveCue, animation.ActiveLogicalCue, sprite.Frame, sprite.FrameProgress,
                sprite.IsPlaying(), sprite.SpeedScale,
                sprite.SpriteFrames.GetAnimationLoopMode(sprite.Animation).ToString(),
                sprite.SpriteFrames.GetAnimationSpeed(sprite.Animation));
        });
        return new RenderFrame(label, battle.SpeedScale, battle.IsPaused, units);
    }

    private static void AssertContinuous(IReadOnlyList<RenderFrame> frames, string label, bool requireFrameChange)
    {
        AssertMovingState(frames, label);
        foreach (var id in ProbeIds)
        {
            var positions = frames.Select(frame => frame.Units[id].Position).ToArray();
            Require(positions.Zip(positions.Skip(1), (a, b) => a.DistanceTo(b) > .01f).Count(changed => changed) >= 2,
                $"{label}: {id} has multiple visible render-frame positions");
            Require(positions.Distinct().Count() >= 3,
                $"{label}: {id} exposes stable intermediate positions");
            if (requireFrameChange)
                Require(frames.Select(frame => (frame.Units[id].SpriteFrame, Bucket(frame.Units[id].FrameProgress))).Distinct().Count() >= 2,
                    $"{label}: {id} advances its authored walk frames");
        }
    }

    private static int Bucket(float progress) => Mathf.FloorToInt(progress * 4f);

    private static void AssertMovingState(IReadOnlyList<RenderFrame> frames, string label)
    {
        foreach (var id in ProbeIds)
            Require(frames.Any(frame => frame.Units[id] is { IsMoving: true, LogicalCue: "move", Playing: true }),
                $"{label}: {id} retains IsMoving/move/IsPlaying");
    }

    private void Capture(string name)
    {
        var image = _stage.Viewport.GetTexture().GetImage();
        var error = image.SavePng(ProjectSettings.GlobalizePath($"{OutputPath}/{name}"));
        Require(error == Error.Ok, $"save rendered movement frame {name}: {error}");
    }

    private async Task Click(Control target)
    {
        Require(target.IsVisibleInTree(), "click target is visible: " + target.GetPath());
        var point = target.GetGlobalRect().GetCenter();
        _stage.Push(new InputEventMouseMotion { Position = point, GlobalPosition = point });
        await RenderOneFrame();
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true });
        _stage.Push(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = MouseButton.Left, Pressed = false });
        await RenderFrames(2);
    }

    private async Task Until(Func<bool> predicate, string label)
    {
        for (var frame = 0; frame < 600 && !predicate(); frame++) await RenderOneFrame();
        Require(predicate(), label);
    }

    private async Task RenderFrames(int count) { for (var i = 0; i < count; i++) await RenderOneFrame(); }
    private async Task RenderOneFrame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
    }
    private async Task Frames(int count) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }

    private static UnitContentRoot Presenter(BattleScreenController battle, string id) =>
        Descendants<UnitContentRoot>(battle).Single(unit => unit.RuntimeId == id);
    private static UnitMotionPresentationComponent Motion(UnitContentRoot unit) =>
        unit.GetNode<UnitMotionPresentationComponent>("UnitMotionPresentationComponent");
    private static UnitAnimationComponent Animation(UnitContentRoot unit) =>
        unit.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
    private static IEnumerable<T> Descendants<T>(Node root) where T : Node
    {
        foreach (var child in root.GetChildren())
        {
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }

    private sealed record UnitFrame(Vector2 Position, Vector2 AuthorityPosition, bool IsMoving,
        string ActiveCue, string LogicalCue, int SpriteFrame, float FrameProgress, bool Playing, float SpeedScale,
        string LoopMode, double AnimationFps);
    private sealed record RenderFrame(string Label, float BattleSpeed, bool Paused, Dictionary<string, UnitFrame> Units)
    {
        public string Format() => "MOVEMENT_RENDER_FRAME " + Label + $" battleSpeed={BattleSpeed:0.#} paused={Paused} " +
            string.Join(" ", Units.Select(pair => $"{pair.Key}[pos={pair.Value.Position.X:0.##},{pair.Value.Position.Y:0.##} " +
                $"authority={pair.Value.AuthorityPosition.X:0.###},{pair.Value.AuthorityPosition.Y:0.###} " +
                $"moving={pair.Value.IsMoving} cue={pair.Value.ActiveCue}/{pair.Value.LogicalCue} " +
                $"sprite={pair.Value.SpriteFrame}+{pair.Value.FrameProgress:0.##} playing={pair.Value.Playing} " +
                $"speed={pair.Value.SpeedScale:0.##} loop={pair.Value.LoopMode} fps={pair.Value.AnimationFps:0.##}]"));
    }
    private sealed record JourneyUnitFrame(Vector2 Position, bool IsMoving, string ActiveCue, string LogicalCue,
        int SpriteFrame, float FrameProgress, bool Playing, float SpeedScale, string LoopMode, double AnimationFps)
    {
        public float PositionX => Position.X;
        public float PositionY => Position.Y;
    }
    private sealed record JourneyFrame(int Index, double RenderMilliseconds, Dictionary<string, JourneyUnitFrame> Units)
    {
        public string Format() => $"MOVEMENT_JOURNEY_FRAME index={Index} render_ms={RenderMilliseconds:0.###} " +
            string.Join(" ", Units.Select(pair => $"{pair.Key}[pos={pair.Value.Position.X:0.##},{pair.Value.Position.Y:0.##} " +
                $"moving={pair.Value.IsMoving} cue={pair.Value.ActiveCue}/{pair.Value.LogicalCue} " +
                $"sprite={pair.Value.SpriteFrame}+{pair.Value.FrameProgress:0.##} playing={pair.Value.Playing} " +
                $"speed={pair.Value.SpeedScale:0.##} loop={pair.Value.LoopMode} fps={pair.Value.AnimationFps:0.##}]"));
    }

    private sealed class ReadOnlyRunSave(ActiveRunDto active, MetaProgressDto meta, SettingsDto settings) : IRunSaveService
    {
        public MetaProgressDto LoadMeta() => meta;
        public SettingsDto LoadSettings() => settings;
        public ActiveRunDto? LoadActiveRun() => active;
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) => true;
        public void DeleteActiveRun() { }
    }
}
