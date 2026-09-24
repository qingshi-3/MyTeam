using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Presentation;
using TowerAutobattler.Project;
using TowerAutobattler.Vfx;

public partial class BattleGeometrySmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            Require(package.Content.TryGet("enemy_ee03_trample_brute", out var entry), "published giant");
            var authoredGiant = BattleSetupFactory.Snapshot(entry, package.Content);
            var giant = authoredGiant with
            {
                AbilityLoadout = null, TraitContributions = [], MaxHealth = 10000,
                Behavior = new(), AttributeDefinition = null
            };
            Require(giant.BodyRadius == .7f, "calibrated giant body");
            if (OS.GetCmdlineUserArgs().Contains("--input"))
                await CheckInput(package.Content, giant, authoredGiant);
            else
            {
                CheckReach(giant);
                CheckNavigation(giant, shouldPass: true);
                CheckNavigation(giant with { BodyRadius = .9f }, shouldPass: false);
                CheckSnapshotReadOnly(giant);
            }
            GD.Print("BATTLE_GEOMETRY_OK " + (OS.GetCmdlineUserArgs().Contains("--input") ? "input-render" : "reach navigation snapshot projection"));
            GetTree().Quit();
        }
        catch (Exception e) { GD.PrintErr("BATTLE_GEOMETRY_FAILED " + e); GetTree().Quit(1); }
    }

    private static BattleConfig Config(UnitSnapshot giant, bool stationary = false) => new()
    {
        Seed = 20260921, HeroRule = HeroRuleSnapshot.Neutral,
        FloorRule = new ClearFloorRuleRuntime("geometry", "常规", ""),
        Spawns = [new(giant with { Behavior = new(Stationary: stationary) }, 0, new(1, 2), "giant", IsPersistentRosterHero: true),
            new(Dummy(), 1, new(8, 2), "target")]
    };

    private static UnitSnapshot Dummy() => new("soldier_dummy_static", "固定靶", UnitRole.Fighter, false, false,
        10000, 0, .25f, 10, 3, 0, 0, 0, 0, [], new(Stationary: true, DisableBasicAttacks: true));

    private static void CheckReach(UnitSnapshot giant)
    {
        foreach (var (gap, expected) in new[] { (.24f, true), (.25f, true), (.27f, false) })
        {
            using var battle = new BattleSimulation(Config(giant, true));
            var source = battle.Units.Single(unit => unit.RuntimeId == "giant");
            var target = battle.Units.Single(unit => unit.RuntimeId == "target");
            target.Position = source.Position + Vector2.Right * (source.BodyRadius + target.BodyRadius + gap);
            battle.Step();
            var spatial = battle.ReadSpatialSnapshot().Units.Single(unit => unit.RuntimeId == "giant");
            Require(spatial.TargetWithinReach == expected && spatial.TargetLineClear, $"range predicate at gap {gap}");
            Require(battle.PendingEvents.Any(e => e.Type == "attack" && e.SourceRuntimeId == "giant") == expected,
                $"actual basic attack at gap {gap}");
            var frozen = battle.ReadSpatialSnapshot();
            source.BodyRadiusOverride = .4f;
            Require(frozen.Units.Single(unit => unit.RuntimeId == "giant").BodyRadius == .7f &&
                battle.ReadSpatialSnapshot().Units.Single(unit => unit.RuntimeId == "giant").BodyRadius == .4f,
                "snapshots copy dynamic body without retaining mutable units");
        }
        using var blocked = new BattleSimulation(new BattleConfig
        {
            Seed = 1, HeroRule = HeroRuleSnapshot.Neutral,
            FloorRule = new NarrowLanesRuntime("narrow", "窄道", ""),
            Spawns = [new(giant with { Range = 8, Behavior = new(Stationary: true) }, 0, new(2, 2), "giant", IsPersistentRosterHero: true),
                new(Dummy(), 1, new(7, 2), "target")]
        });
        var unit = blocked.Units.Single(u => u.RuntimeId == "giant");
        unit.ActionTargetRuntimeId = "target";
        var sample = blocked.ReadSpatialSnapshot().Units.Single(u => u.RuntimeId == "giant");
        Require(sample.TargetWithinReach && !sample.TargetLineClear, "in-range does not imply terrain access");
        blocked.Step();
        Require(!blocked.PendingEvents.Any(e => e.Type == "attack"), "blocked actual attack");
    }

    private static void CheckNavigation(UnitSnapshot giant, bool shouldPass)
    {
        var config = Config(giant);
        foreach (var y in new[] { 0, 1, 3, 4, 5 }) config.Spawns.Add(new(Dummy(), 0, new(4, y), $"wall{y}"));
        using var battle = new BattleSimulation(config);
        battle.Units.Single(u => u.RuntimeId == "wall1").Position = new(4, .9f);
        battle.Units.Single(u => u.RuntimeId == "wall3").Position = new(4, 3.1f);
        var mover = battle.Units.Single(u => u.RuntimeId == "giant");
        var attacked = false;
        for (var tick = 0; tick < 160 && !attacked; tick++)
        {
            battle.Step();
            Require(battle.Outcome == BattleOutcome.Running, "navigation fixture remains active");
            foreach (var other in battle.Units.Where(u => u != mover))
                Require(mover.Position.DistanceTo(other.Position) >= mover.BodyRadius + other.BodyRadius + BattlefieldSpace.BodyClearance - .001f,
                    "navigation never squeezes through overlapping bodies");
            attacked = battle.DrainEvents().Any(e => e.Type == "attack" && e.SourceRuntimeId == "giant");
        }
        Require(attacked == shouldPass, $"1.56-cell passage, diameter {giant.BodyRadius * 2}, position {mover.Position}, attacked {attacked}");
        GD.Print($"GEOMETRY_PASSAGE diameter={giant.BodyRadius * 2} gap=1.56 crossed={attacked} position={mover.Position}");
    }

    private static void CheckSnapshotReadOnly(UnitSnapshot giant)
    {
        using var observed = new BattleSimulation(Config(giant));
        using var control = new BattleSimulation(Config(giant));
        for (var i = 0; i < 60; i++)
        {
            observed.ReadSpatialSnapshot(); observed.Step(); observed.ReadSpatialSnapshot(); control.Step();
        }
        Require(observed.CreateResult().Digest == control.CreateResult().Digest, "diagnostics do not alter deterministic battle result");
        foreach (var size in new[] { new Vector2(560, 330), new Vector2(1500, 730), new Vector2(2200, 1200) })
        {
            var projection = BattlefieldProjection.Fit(size);
            var spriteWidth = 61 * 2.1f * projection.UnitScale;
            var bodyWidth = giant.BodyRadius * 2 * projection.CellPitch.X;
            Require(spriteWidth / bodyWidth is > 1 and < 1.07f, "visible neutral artwork and body retain proportion at every board scale");
        }
    }

    private async Task CheckInput(ContentRegistry content, UnitSnapshot giant, UnitSnapshot authoredGiant)
    {
        Require(DisplayServer.GetName() != "headless", "rendered input requires display");
        GetWindow().Mode = Window.ModeEnum.Windowed;
        GetWindow().Size = new(1600, 900);
        var screen = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
        screen.Theme = GD.Load<Theme>("res://content/ui/RealmTheme.tres");
        AddChild(screen);
        await Frames(3);
        var config = Config(giant);
        screen.StartBattle(content, config, "破阵巨兽 · 碰撞与普攻范围");
        screen.SetLabControlsVisible(true);
        var overlay = screen.GetNode<BattleGeometryOverlay>("%BattleGeometryOverlay");
        var board = screen.GetNode<BattleBoard>("%BattleBoard");
        var body = screen.GetNode<Button>("%CollisionToggle");
        var range = screen.GetNode<Button>("%AttackRangeToggle");
        var pause = screen.GetNode<Button>("%PauseButton");
        await Click(pause.GetGlobalRect().GetCenter());
        Require(screen.IsPaused && overlay.Snapshot is null, "default clean view and real pause");
        await Click(body.GetGlobalRect().GetCenter());
        await Click(range.GetGlobalRect().GetCenter());
        Require(overlay.ShowBodies && overlay.ShowReach && overlay.SelectedId == "giant", "both toggles and initial selection");
        await Capture("geometry-separated");
        await Click(pause.GetGlobalRect().GetCenter());
        for (var frame = 0; frame < 1000 && screen.ReadRuntimeUnits().Single(u => u.RuntimeId == "target").Health == 10000; frame++)
            await Frames(1);
        await Click(pause.GetGlobalRect().GetCenter());
        Require(screen.IsPaused && screen.ReadRuntimeUnits().Single(u => u.RuntimeId == "target").Health < 10000, "move into range and actual basic damage");
        Require(overlay.Snapshot!.Units.Single(u => u.RuntimeId == "giant").TargetWithinReach, "contact circle matches actual attack");
        foreach (var state in overlay.Snapshot.Units)
        {
            var presenter = screen.GetNode<Node2D>("%UnitsRoot").GetChildren().OfType<UnitContentRoot>().Single(p => p.RuntimeId == state.RuntimeId);
            Require(presenter.Position.DistanceTo(board.LogicalToLocal(state.Position)) < .01f, "paused art and authority circles coincide");
        }
        await Capture("geometry-contact");
        var target = overlay.Snapshot.Units.Single(u => u.RuntimeId == "target");
        await Click(board.GlobalPosition + board.LogicalToLocal(target.Position));
        Require(overlay.SelectedId == "target", "real body click switches observer");
        var tick = screen.TickIndex;
        await Click(screen.GetNode<Button>("%StepButton").GetGlobalRect().GetCenter());
        Require(screen.TickIndex == tick + 1 && overlay.Snapshot!.Tick == screen.TickIndex, "real single-step refreshes geometry");
        await Click(body.GetGlobalRect().GetCenter());
        await Click(range.GetGlobalRect().GetCenter());
        Require(overlay.Snapshot is null, "hide all overlays");
        // Range remains focused after the click; exercise native keyboard activation.
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Space, Pressed = true }, true);
        await Frames(1);
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Space, Pressed = false }, true);
        await Frames(2);
        Require(overlay.Snapshot is not null && overlay.ShowReach, "keyboard range toggle");
        screen.StopBattle();
        Require(overlay.Snapshot is null, "teardown clears prior battle geometry");
        screen.StartBattle(content, Config(giant), "重新开战");
        Require(overlay.Snapshot?.Tick == 0 && overlay.SelectedId == "giant", "new battle receives fresh geometry");
        screen.StopBattle();
        screen.StartBattle(content, Config(authoredGiant), "破阵巨兽 · 冲锋中的实际范围");
        screen.SetLabControlsVisible(true);
        if (!body.ButtonPressed) await Click(body.GetGlobalRect().GetCenter());
        var player = screen.GetNode<RangedAttackLayer>("%RangedAttackLayer").GetNode<VfxPlayer>("Player");
        var sawRush = false;
        for (var frame = 0; frame < 1200 && !sawRush; frame++)
        {
            await Frames(1);
            sawRush = player.GetChildren().OfType<VfxInstance>()
                .Any(v => v.GetNodeOrNull<VfxTrampleTrack>("Layers") is { Rush: true });
        }
        Require(sawRush, $"authored charge reaches moving phase with diagnostics enabled; tick={screen.TickIndex}, failure={screen.LastRuntimeFailure}");
        await Click(pause.GetGlobalRect().GetCenter());
        var rushing = overlay.Snapshot!.Units.Single(u => u.RuntimeId == "giant");
        var rushingPresenter = screen.GetNode<Node2D>("%UnitsRoot").GetChildren().OfType<UnitContentRoot>().Single(p => p.RuntimeId == "giant");
        Require(screen.IsPaused && rushingPresenter.Position.DistanceTo(board.LogicalToLocal(rushing.Position)) < .01f,
            "charge and authority geometry align when paused");
        await Capture("geometry-charge");
        screen.StopBattle();
        Require(overlay.Snapshot is null && player.ActiveCount == 0, "charge and diagnostics both clear");
        screen.QueueFree();
    }

    private async Task Click(Vector2 point)
    {
        GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left, Pressed = true }, true);
        await Frames(1);
        GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
            ButtonIndex = MouseButton.Left, Pressed = false }, true);
        await Frames(2);
    }
    private async Task Frames(int count) { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Capture(string name)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var directory = ProjectSettings.GlobalizePath("res://.godot/ui-review");
        DirAccess.MakeDirRecursiveAbsolute(directory);
        Require(GetViewport().GetTexture().GetImage().SavePng($"{directory}/{name}.png") == Error.Ok, "render capture");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
