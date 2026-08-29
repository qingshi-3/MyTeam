using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Presentation;

public partial class MovementPresentationContractSmoke : Node
{
    public override async void _Ready()
    {
        var code = await RunAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(code);
    }

    private async Task<int> RunAsync()
    {
        try
        {
            var catalog = GD.Load<ContentCatalog>("res://content/catalogs/alpha_catalog.tres")
                ?? throw new InvalidOperationException("catalog load");
            var gate = await ContentRegistry.CreateReadyAsync(this, catalog);
            var registry = gate.Registry ?? throw new InvalidOperationException("content gate: " + string.Join("; ", gate.Report.CoreErrors));

            IndependentSceneContract(registry);
            await FacingContractAsync();
            await ProductionActionFacingRouteAsync(registry);
            await SharedProductionFrameHitchContractAsync(registry);
            await TemporalMotionContractAsync();
            await LifecycleContractAsync();
            await ProductionMoveRouteAsync(registry);
            await SummonSnapAsync(registry);
            PresenterFreeDeterminism(registry);

            GD.Print("MOVEMENT_PRESENTATION_CONTRACT_OK timing=grid-march-0.24-0.14-0.09,eased-first-mid-final,ordered-centers,lag,hitch-0.125-0.25-0.375,one-segment-per-frame pause=speed-lift-continuity,no-wall-debt actions=move-base,phase-continuity facing=team-defaults,authored-left,segment-timing,vertical-retention,production-attack-heal,mutual-attack,defeat-lock,sprite-only lifecycle=snap,defeat,rebind,deactivate,exit,replacement-planning route=production-move,selection,summon simulation=unchanged");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("MOVEMENT_PRESENTATION_CONTRACT_FAILED: " + exception);
            return 1;
        }
    }

    private static void IndependentSceneContract(ContentRegistry registry)
    {
        var entries = registry.Catalog.Heroes.Cast<CatalogEntry>()
            .Concat(registry.Catalog.Soldiers.Cast<CatalogEntry>())
            .Concat(registry.Catalog.Enemies.Cast<CatalogEntry>());
        foreach (var entry in entries)
        {
            var unit = entry.Scene.Instantiate<UnitContentRoot>();
            try
            {
                if (unit.GetNodeOrNull<UnitMotionPresentationComponent>("UnitMotionPresentationComponent") is null ||
                    unit.GetNodeOrNull<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent") is null ||
                    unit.GetNodeOrNull<UnitReadabilityComponent>("VisualRoot/UnitAnimationComponent/UnitReadabilityComponent") is null)
                    throw new InvalidOperationException(entry.StableId + " omitted a required independent presentation component");
            }
            finally { unit.Free(); }
        }
    }

    private async Task TemporalMotionContractAsync()
    {
        var unit = await AttachCommanderAsync();
        try
        {
            unit.Bind("motion-temporal", 0, 100, 100);
            var motion = Motion(unit);
            var animation = Animation(unit);
            var sprite = unit.GetNode<AnimatedSprite2D>("VisualRoot/UnitAnimationComponent/AnimatedSprite2D");
            var readability = unit.GetNode<UnitReadabilityComponent>("VisualRoot/UnitAnimationComponent/UnitReadabilityComponent");
            var health = unit.GetNode<HealthViewComponent>("HealthViewComponent");
            var source = Vector2.Zero;
            var destination = new Vector2(88, 0);

            Near(motion.OneTimesCellSeconds, .24f, .0001f, "reusable scene did not author the 1x grid-march duration");
            Near(motion.TwoTimesCellSeconds, .14f, .0001f, "reusable scene did not author the 2x grid-march duration");
            Near(motion.FourTimesCellSeconds, .09f, .0001f, "reusable scene did not author the 4x grid-march duration");
            Near(animation.StepLiftPixels, 3f, .0001f, "reusable animation scene did not author restrained step lift");

            var spriteRest = sprite.Position;
            var readabilityRest = readability.Position;
            var healthRest = health.Position;
            motion.MaximumFrameDeltaSeconds = .1f;
            motion.MaximumVisualLagSeconds = 10f;
            unit.SnapPresentation(source, 100, 100);
            unit.QueueMovement(new Vector2(100, 0));
            motion._Process(.30);
            motion._Process(.06);
            Near(unit.Position.X, 15.625f, .02f, "quarter-step did not apply bounded smooth ease-in progress");
            var quarterPosition = unit.Position;
            var quarterLift = spriteRest.Y - sprite.Position.Y;
            if (sprite.Position.Y >= spriteRest.Y || sprite.Position.Y < spriteRest.Y - animation.StepLiftPixels - .01f)
                throw new InvalidOperationException("character-only lift was absent or exceeded its authored bound");
            Equal(readability.Position, readabilityRest, "step lift moved readability markers");
            Equal(health.Position, healthRest, "step lift moved the health bar");
            motion._Process(.06);
            Near(unit.Position.X, 50f, .02f, "mid-step eased progress did not remain centered");
            Near(sprite.Position.Y, spriteRest.Y - animation.StepLiftPixels, .02f, "character-only lift did not peak near mid-step");
            var midpointPosition = unit.Position;
            var midpointLift = spriteRest.Y - sprite.Position.Y;
            motion._Process(.06);
            Near(unit.Position.X, 84.375f, .02f, "three-quarter step did not apply bounded smooth ease-out progress");
            motion._Process(.06);
            Near(unit.Position, new Vector2(100, 0), .01f, "eased step missed its exact destination center");
            Equal(sprite.Position, spriteRest, "completed step retained decorative lift");
            Equal(readability.Position, readabilityRest, "completed step displaced readability markers");
            Equal(health.Position, healthRest, "completed step displaced the health bar");
            GD.Print($"GRID_MARCH_TEMPORAL_EVIDENCE one-cell=0,{quarterPosition.X:0.###},{midpointPosition.X:0.###},84.375,100 lift=0,{quarterLift:0.###},{midpointLift:0.###},{quarterLift:0.###},0 duration=0.24 easing=smoothstep markers=stable");
            motion.MaximumFrameDeltaSeconds = .05f;
            motion.MaximumVisualLagSeconds = .25f;

            unit.SnapPresentation(source, 100, 100);
            unit.QueueMovement(destination);
            Equal(unit.Position, source, "one-cell first sample moved before presentation time advanced");
            motion._Process(.30);
            Equal(unit.Position, source, "fresh one-cell move consumed its enqueue-frame hitch delta");
            motion._Process(.05);
            StrictlyBetween(unit.Position.X, source.X, destination.X, "one-cell midpoint was not spatially intermediate");
            AdvanceFrames(motion, 4, .05);
            Near(unit.Position, destination, .01f, "one-cell completion missed its destination");

            Rebind(unit, "motion-idle");
            unit.SnapPresentation(source, 100, 100);
            unit.QueueMovement(destination);
            unit.RefreshPresentation("idle", 99, 100);
            Equal(unit.Position, source, "ordinary idle/health synchronization snapped an active mover");
            if (animation.ActiveLogicalCue != "move")
                throw new InvalidOperationException("ordinary idle synchronization ended the active move base cue");

            Rebind(unit, "motion-ordered");
            var corner = new Vector2(88, 68);
            var returnLeft = new Vector2(0, 68);
            unit.SnapPresentation(source, 100, 100);
            unit.QueueMovement(destination);
            unit.QueueMovement(corner);
            unit.QueueMovement(returnLeft);
            motion._Process(.30);
            Equal(unit.Position, source, "ordered path consumed its enqueue-frame hitch delta");
            AdvanceFrames(motion, 2, .05);
            Near(unit.Position, destination, .02f, "ordered path did not visit the first adjacent waypoint");
            motion._Process(.05);
            Near(unit.Position.X, destination.X, .02f, "ordered path drew a diagonal shortcut after its first waypoint");
            StrictlyBetween(unit.Position.Y, destination.Y, corner.Y, "ordered path did not interpolate its second segment");
            motion._Process(.05);
            Near(unit.Position, corner, .02f, "ordered path did not visit the second cell center");
            motion._Process(.05);
            Near(unit.Position.Y, corner.Y, .02f, "ordered path rounded the down-to-left corner");
            Near(unit.Position, returnLeft, .02f, "ordered path did not finish at its left destination center");

            Rebind(unit, "motion-burst");
            unit.SnapPresentation(source, 100, 100);
            for (var index = 1; index <= 12; index++) unit.QueueMovement(new Vector2(index * 10, 0));
            var previous = unit.Position;
            motion._Process(.30);
            Equal(unit.Position, source, "catch-up burst consumed its enqueue-frame hitch delta");
            for (var sample = 0; sample < 15; sample++)
            {
                motion._Process(1.0 / 60.0);
                if (unit.Position.X + .001f < previous.X)
                    throw new InvalidOperationException("catch-up burst moved backward");
                if (unit.Position.DistanceTo(previous) > 10.01f)
                    throw new InvalidOperationException("catch-up burst teleported across an accepted waypoint");
                previous = unit.Position;
            }
            Near(unit.Position, new Vector2(120, 0), .02f, "catch-up burst exceeded the configured visual-lag budget");

            Rebind(unit, "motion-overflow");
            unit.SnapPresentation(source, 100, 100);
            for (var index = 1; index <= 15; index++) unit.QueueMovement(new Vector2(index * 10, 0));
            if (motion.PendingWaypointCount > motion.MaximumQueuedWaypoints)
                throw new InvalidOperationException("overflow exceeded the authored waypoint bound");
            motion._Process(.26);
            Equal(unit.Position, source, "bounded overflow consumed its enqueue-frame hitch delta");
            AdvanceFrames(motion, 16, 1.0 / 60.0);
            Near(unit.Position, new Vector2(150, 0), .02f, "bounded overflow retained a stale destination instead of the newest authoritative cell");

            Rebind(unit, "motion-pause");
            unit.SnapPresentation(source, 100, 100);
            unit.QueueMovement(destination);
            motion._Process(.30);
            motion._Process(.04);
            var pausedAt = unit.Position;
            unit.SetPresentationPaused(true);
            motion._Process(1.0);
            Equal(unit.Position, pausedAt, "pause drifted an in-progress spatial segment");
            unit.SetPresentationPaused(false);
            motion._Process(.30);
            Equal(unit.Position, pausedAt, "resume consumed pause-frame wall-clock debt");
            AdvanceFrames(motion, 4, .05);
            Near(unit.Position, destination, .02f, "resume did not continue from paused progress");

            Rebind(unit, "motion-speed");
            unit.SnapPresentation(source, 100, 100);
            unit.QueueMovement(destination);
            motion._Process(.30);
            motion._Process(.03);
            var beforeSpeedChange = unit.Position;
            unit.SetPresentationSpeed(2f);
            Equal(unit.Position, beforeSpeedChange, "1x to 2x speed change snapped the active segment");
            motion._Process(.30);
            Equal(unit.Position, beforeSpeedChange, "2x switch frame consumed pre-switch hitch credit");
            motion._Process(.02);
            var afterTwoTimes = unit.Position;
            if (afterTwoTimes.X <= beforeSpeedChange.X) throw new InvalidOperationException("2x speed change did not continue forward");
            unit.SetPresentationSpeed(4f);
            Equal(unit.Position, afterTwoTimes, "2x to 4x speed change restarted or snapped the active segment");
            motion._Process(.30);
            Equal(unit.Position, afterTwoTimes, "4x switch frame consumed pre-switch hitch credit");
            motion._Process(.015);
            var afterFourTimes = unit.Position;
            if (afterFourTimes.X <= afterTwoTimes.X || unit.Position == destination)
                throw new InvalidOperationException("speed change reversed or prematurely completed movement");
            unit.SetPresentationSpeed(1f);
            Equal(unit.Position, afterFourTimes, "4x to 1x speed change restarted or snapped the active segment");
            motion._Process(.30);
            Equal(unit.Position, afterFourTimes, "1x switch frame consumed pre-switch hitch credit");
            AdvanceFrames(motion, 4, .05);
            Near(unit.Position, destination, .02f, "1x to 2x to 4x to 1x retiming did not complete continuously");

            Rebind(unit, "motion-active-4x-hitch");
            unit.SnapPresentation(source, 100, 100);
            unit.SetPresentationSpeed(4f);
            unit.QueueMovement(destination);
            motion._Process(.30);
            motion._Process(.03);
            unit.QueueMovement(corner);
            unit.QueueMovement(new Vector2(176, 68));
            motion._Process(.30);
            if (unit.Position == new Vector2(176, 68) || !motion.IsMoving)
                throw new InvalidOperationException("active 4x hitch frame silently consumed the fresh chase queue");
            AdvanceFrames(motion, 6, .05);
            Near(unit.Position, new Vector2(176, 68), .02f, "active 4x hitch catch-up did not reach the newest waypoint");

            Rebind(unit, "motion-action-moving");
            unit.SnapPresentation(source, 100, 100);
            unit.QueueMovement(destination);
            unit.RefreshPresentation("attack", 100, 100);
            if (animation.ActiveLogicalCue != "attack") throw new InvalidOperationException("attack did not override move animation");
            animation._Process(animation.ActivePlaybackSeconds + .01);
            if (animation.ActiveLogicalCue != "move") throw new InvalidOperationException("action did not restore move while spatial travel remained");

            Rebind(unit, "motion-action-idle");
            unit.SnapPresentation(source, 100, 100);
            unit.QueueMovement(destination);
            unit.RefreshPresentation("hit", 100, 100);
            motion._Process(.30);
            AdvanceFrames(motion, 5, .05);
            if (animation.ActiveLogicalCue != "hit") throw new InvalidOperationException("motion completion erased an active action one-shot");
            animation._Process(animation.ActivePlaybackSeconds + .01);
            if (animation.ActiveLogicalCue != "idle") throw new InvalidOperationException("completed action did not restore idle after travel ended");

            Rebind(unit, "motion-action-pause");
            unit.SnapPresentation(source, 100, 100);
            unit.QueueMovement(destination);
            unit.RefreshPresentation("attack", 100, 100);
            var actionRemaining = animation.ActiveLogicalCue;
            unit.SetPresentationPaused(true);
            animation._Process(animation.ActivePlaybackSeconds + 1);
            if (animation.ActiveLogicalCue != actionRemaining) throw new InvalidOperationException("pause advanced an action animation state");
            unit.SetPresentationPaused(false);

            Rebind(unit, "motion-repeated-phase");
            unit.SnapPresentation(source, 100, 100);
            unit.QueueMovement(destination);
            var resolvedMove = sprite.Animation;
            var moveFrameCount = sprite.SpriteFrames.GetFrameCount(resolvedMove);
            if (moveFrameCount > 1)
            {
                sprite.Frame = Math.Min(3, moveFrameCount - 1);
                sprite.FrameProgress = .5f;
                motion._Process(.30);
                AdvanceFrames(motion, 5, .05);
                unit.QueueMovement(new Vector2(176, 0));
                if (sprite.Frame == 0 && sprite.FrameProgress <= .001f)
                    throw new InvalidOperationException("repeated grid step flashed the move clip opening pose");
                if (animation.RetainedMovePhase <= .001f)
                    throw new InvalidOperationException("repeated grid step did not retain bounded presentation-only move phase");
            }
        }
        finally { DetachAndFree(unit); }
    }

    private async Task FacingContractAsync()
    {
        var unit = await AttachCommanderAsync();
        try
        {
            unit.Bind("facing-player", 0, 100, 100);
            var animation = Animation(unit);
            var motion = Motion(unit);
            var sprite = unit.GetNode<AnimatedSprite2D>("VisualRoot/UnitAnimationComponent/AnimatedSprite2D");
            if (!animation.FacingRight || sprite.FlipH == animation.AuthoredFacingRight)
                throw new InvalidOperationException("player bind did not establish authored right-facing default");

            Rebind(unit, "facing-enemy", team: 1);
            if (animation.FacingRight || sprite.FlipH != animation.AuthoredFacingRight)
                throw new InvalidOperationException("enemy bind did not establish left-facing default");

            Rebind(unit, "facing-segments");
            motion.MaximumVisualLagSeconds = 10f;
            unit.SnapPresentation(Vector2.Zero, 100, 100);
            unit.QueueMovement(new Vector2(88, 0));
            unit.QueueMovement(new Vector2(88, 68));
            unit.QueueMovement(new Vector2(0, 68));
            if (!animation.FacingRight)
                throw new InvalidOperationException("queued future left segment changed facing before its segment began");
            motion._Process(.30);
            AdvanceFrames(motion, 5, .05);
            if (!animation.FacingRight)
                throw new InvalidOperationException("vertical segment failed to retain right-facing direction");
            AdvanceFrames(motion, 5, .05);
            if (animation.FacingRight)
                throw new InvalidOperationException("left segment did not face left when the segment began");

            unit.FaceToward(new Vector2(300, unit.Position.Y));
            unit.RefreshPresentation("attack", 100, 100);
            if (!animation.FacingRight)
                throw new InvalidOperationException("attack did not face its real target during presentation lag");
            unit.FaceToward(new Vector2(-300, unit.Position.Y));
            unit.RefreshPresentation("skill_cast", 100, 100);
            if (animation.FacingRight)
                throw new InvalidOperationException("heal/cast did not face its real target during presentation lag");

            unit.RefreshPresentation("defeated", 0, 100);
            unit.FaceToward(new Vector2(300, unit.Position.Y));
            if (animation.FacingRight)
                throw new InvalidOperationException("defeat did not lock facing against later updates");

            Rebind(unit, "facing-reset");
            if (!animation.FacingRight)
                throw new InvalidOperationException("rebind did not restore the player team default facing");

            animation.AuthoredFacingRight = false;
            Rebind(unit, "facing-authored-left-player");
            if (!animation.FacingRight || !sprite.FlipH)
                throw new InvalidOperationException("AuthoredFacingRight=false did not mirror authored-left art toward the player default");
            Rebind(unit, "facing-authored-left-enemy", team: 1);
            if (animation.FacingRight || sprite.FlipH)
                throw new InvalidOperationException("AuthoredFacingRight=false did not preserve authored-left art toward the enemy default");
            animation.AuthoredFacingRight = true;
            Rebind(unit, "facing-authored-reset");
            var readability = unit.GetNode<UnitReadabilityComponent>("VisualRoot/UnitAnimationComponent/UnitReadabilityComponent");
            var health = unit.GetNode<HealthViewComponent>("HealthViewComponent");
            if (readability.Scale.X < 0 || health.Scale.X < 0 || unit.Scale.X < 0)
                throw new InvalidOperationException("facing mirrored readability or health UI instead of only the character sprite");
        }
        finally { DetachAndFree(unit); }
    }

    private async Task ProductionActionFacingRouteAsync(ContentRegistry registry)
    {
        var heroEntry = registry.Catalog.Heroes.Single(entry => entry.StableId == "hero_hour_arbiter");
        var enemyEntry = registry.Catalog.Enemies.Single(entry => entry.StableId == "enemy_crossbow");
        var allyEntry = registry.Catalog.Soldiers[0];
        var heroRoot = heroEntry.Scene.Instantiate<UnitContentRoot>();
        var screen = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
        AddChild(screen);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        try
        {
            var rule = BattleSetupFactory.Snapshot(heroRoot.HeroRule!, heroRoot.HeroCommand!);
            var mutualHero = BattleSetupFactory.Snapshot(heroEntry) with
            {
                MaxHealth = 1000,
                Damage = 1,
                Range = 4,
                HealPower = 0,
                AttackTicks = 1000,
                MoveTicks = 1000
            };
            var mutualEnemy = BattleSetupFactory.Snapshot(enemyEntry) with
            {
                MaxHealth = 1000,
                Damage = 1,
                Range = 4,
                AttackTicks = 1000,
                MoveTicks = 1000
            };
            screen.StartBattle(registry, new BattleConfig
            {
                Seed = 411,
                FloorRule = new ClearFloorRuleRuntime("facing-mutual", "常规", "双方互攻朝向"),
                HeroRule = rule,
                Spawns =
                [
                    new BattleSpawn(mutualHero, 0, new Vector2I(3, 2), "mutual-player"),
                    new BattleSpawn(mutualEnemy, 1, new Vector2I(1, 2), "mutual-enemy")
                ]
            }, "双方互攻朝向");
            screen._Process(.13);
            var playerAnimation = Animation(Presenter(screen, "mutual-player"));
            var enemyAnimation = Animation(Presenter(screen, "mutual-enemy"));
            if (playerAnimation.FacingRight || !enemyAnimation.FacingRight ||
                playerAnimation.ActiveLogicalCue != "attack" || enemyAnimation.ActiveLogicalCue != "attack")
                throw new InvalidOperationException("production BattleScreen mutual attack routing did not face both sources toward real targets");

            var healer = BattleSetupFactory.Snapshot(heroEntry) with
            {
                MaxHealth = 1000,
                Damage = 0,
                AttackTicks = 1000,
                MoveTicks = 1000
            };
            var ally = BattleSetupFactory.Snapshot(allyEntry) with { MaxHealth = 1000, Damage = 0, MoveTicks = 1000 };
            var distantEnemy = mutualEnemy with { Damage = 0, Range = 1 };
            screen.StartBattle(registry, new BattleConfig
            {
                Seed = 412,
                FloorRule = new ClearFloorRuleRuntime("facing-heal", "常规", "治疗朝向"),
                HeroRule = rule,
                Spawns =
                [
                    new BattleSpawn(healer, 0, new Vector2I(3, 2), "production-healer"),
                    new BattleSpawn(ally, 0, new Vector2I(1, 2), "wounded-ally", .5f),
                    new BattleSpawn(distantEnemy, 1, new Vector2I(9, 5), "distant-enemy")
                ]
            }, "生产治疗朝向");
            screen._Process(.13);
            var healerAnimation = Animation(Presenter(screen, "production-healer"));
            if (healerAnimation.FacingRight || healerAnimation.ActiveLogicalCue != "skill_cast")
                throw new InvalidOperationException("production BattleScreen heal routing did not face the healer toward its protected ally");
        }
        finally
        {
            heroRoot.Free();
            RemoveChild(screen);
            screen.Free();
        }
    }

    private async Task LifecycleContractAsync()
    {
        UnitContentRoot? unit = await AttachCommanderAsync();
        try
        {
            unit.Bind("motion-defeat", 0, 100, 100);
            var motion = Motion(unit);
            var animation = Animation(unit);
            var sprite = unit.GetNode<AnimatedSprite2D>("VisualRoot/UnitAnimationComponent/AnimatedSprite2D");
            var spriteRest = sprite.Position;
            unit.SnapPresentation(Vector2.Zero, 100, 100);
            unit.QueueMovement(new Vector2(88, 0));
            motion._Process(.30);
            motion._Process(.05);
            var defeatedAt = unit.Position;
            unit.RefreshPresentation("defeated", 0, 100);
            if (!motion.IsTerminal || motion.IsMoving || motion.IsProcessing())
                throw new InvalidOperationException("defeat did not synchronously terminate queued motion");
            motion._Process(1);
            unit.QueueMovement(new Vector2(176, 0));
            motion._Process(1);
            Equal(unit.Position, defeatedAt, "defeated unit slid after terminal cancellation");
            Equal(sprite.Position, spriteRest, "defeat retained character-only step lift");

            Rebind(unit, "motion-rebind");
            if (motion.IsTerminal || motion.IsMoving || motion.IsProcessing())
                throw new InvalidOperationException("rebind retained terminal or active motion state");
            if (animation.RetainedMovePhase != 0f) throw new InvalidOperationException("rebind retained stale move phase");
            unit.SnapPresentation(new Vector2(20, 30), 100, 100);
            Equal(unit.Position, new Vector2(20, 30), "rebind/reset did not allow explicit snap placement");

            unit.Activate(new UnitBindingContext(new DeterministicRandom(1), NullEvents.Instance, NullCommands.Instance));
            unit.QueueMovement(new Vector2(108, 30));
            unit.Deactivate();
            if (motion.IsMoving || motion.IsProcessing())
                throw new InvalidOperationException("deactivate retained active movement processing");

            unit.SnapPresentation(unit.Position, 100, 100);
            unit.QueueMovement(new Vector2(196, 30));
            if (!motion.IsMoving) throw new InvalidOperationException("bound presentation could not prepare exit cleanup probe");
            RemoveChild(unit);
            if (motion.IsMoving || motion.IsProcessing())
                throw new InvalidOperationException("tree exit retained movement state or processing");
            unit.Free();
            unit = null;
        }
        finally
        {
            if (unit is not null && GodotObject.IsInstanceValid(unit)) DetachAndFree(unit);
        }
    }

    private async Task ProductionMoveRouteAsync(ContentRegistry registry)
    {
        var heroEntry = registry.Catalog.Heroes.Single(entry => entry.StableId == "hero_banner_marshal");
        var enemyEntry = registry.Catalog.Enemies[0];
        var heroRoot = heroEntry.Scene.Instantiate<UnitContentRoot>();
        var enemyRoot = enemyEntry.Scene.Instantiate<UnitContentRoot>();
        var screen = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
        AddChild(screen);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        try
        {
            var hero = BattleSetupFactory.Snapshot((UnitDefinition)heroEntry.Definition, heroRoot.Behavior) with
            {
                Damage = 0,
                Range = 1f,
                MoveTicks = 1
            };
            var enemy = BattleSetupFactory.Snapshot((UnitDefinition)enemyEntry.Definition, enemyRoot.Behavior) with
            {
                Damage = 1,
                Range = 20,
                AttackTicks = 1000,
                MoveTicks = 1000
            };
            var rule = BattleSetupFactory.Snapshot(heroRoot.HeroRule!, heroRoot.HeroCommand!);
            var config = new BattleConfig
            {
                Seed = 77,
                FloorRule = new ClearFloorRuleRuntime("motion-route", "常规", "移动路由测试"),
                HeroRule = rule,
                Spawns =
                [
                    new BattleSpawn(hero, 0, new Vector2I(0, 2), "z-mover"),
                    new BattleSpawn(enemy, 1, new Vector2I(9, 2), "a-enemy")
                ]
            };

            screen.StartBattle(registry, config, "移动表现生产路由");
            var board = screen.GetNode<BattleBoard>("%BattleBoard");
            var presenter = Presenter(screen, "z-mover");
            var motion = Motion(presenter);
            var source = board.CellToLocal(new Vector2I(0, 2));
            var destination = board.CellToLocal(new Vector2I(1, 2));
            Equal(presenter.Position, source, "initial production presenter did not snap to its spawn cell");

            screen._Process(.13);
            Equal(presenter.Position, source, "production move event teleported before spatial presentation advanced");
            if (!motion.IsMoving || Animation(presenter).ActiveLogicalCue != "hit")
                throw new InvalidOperationException(
                    $"cue arbitration discarded a simultaneous production move destination " +
                    $"(moving={motion.IsMoving}, queued={motion.PendingWaypointCount}, cue={Animation(presenter).ActiveLogicalCue}, " +
                    $"position={presenter.Position}, source={source}, destination={destination})");
            motion._Process(.05);
            Equal(presenter.Position, source, "production move route consumed its enqueue-frame delta");
            motion._Process(.05);
            StrictlyBetween(presenter.Position.X, source.X, destination.X, "production move route did not interpolate the accepted event cell");

            var clickPosition = board.GlobalPosition + presenter.Position;
            GetViewport().PushInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                Pressed = true,
                Position = clickPosition,
                GlobalPosition = clickPosition
            }, true);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (!screen.GetNode<Control>("Margin/Layout/BattleBoard/SelectedUnitPanel").Visible)
                throw new InvalidOperationException("pointer hit testing did not follow the presenter's visible interpolated position");

            var pause = screen.GetNode<Button>("%PauseButton");
            pause.EmitSignal(BaseButton.SignalName.Pressed);
            var pausePosition = presenter.Position;
            motion._Process(.5);
            Equal(presenter.Position, pausePosition, "production pause did not freeze motion presentation");
            pause.EmitSignal(BaseButton.SignalName.Pressed);

            var speed = screen.GetNode<Button>("%SpeedButton");
            var beforeSpeed = presenter.Position;
            speed.EmitSignal(BaseButton.SignalName.Pressed);
            speed.EmitSignal(BaseButton.SignalName.Pressed);
            if (motion.SpeedScale != 4f) throw new InvalidOperationException("production speed changes were not propagated to motion presentation");
            Equal(presenter.Position, beforeSpeed, "production speed switch snapped the presenter");
            AdvanceFrames(motion, 4, .2);
            Near(presenter.Position, destination, .02f, "production presenter did not finish its routed move destination");

            var replacedMotion = motion;
            var replacedSimulation = Simulation(screen);
            var replacedPlanning = Movement(replacedSimulation);
            screen.StartBattle(registry, config, "移动表现替换清理");
            if (GodotObject.IsInstanceValid(replacedMotion))
                throw new InvalidOperationException("battle replacement retained the previous motion component");
            if (replacedPlanning.PlanningStateCount != 0 || replacedSimulation.Units.Any(unit =>
                    !string.IsNullOrEmpty(unit.ActionTargetRuntimeId) || !string.IsNullOrEmpty(unit.ActionTargetName)))
                throw new InvalidOperationException("battle replacement retained old planning state or target-facing facts");
            var replacement = Presenter(screen, "z-mover");
            if (Motion(replacement).IsMoving || replacement.Position != source)
                throw new InvalidOperationException("replacement battle did not begin from a clean snapped presentation");
        }
        finally
        {
            heroRoot.Free();
            enemyRoot.Free();
            RemoveChild(screen);
            screen.Free();
        }
    }

    private async Task SharedProductionFrameHitchContractAsync(ContentRegistry registry)
    {
        var heroEntry = registry.Catalog.Heroes.Single(entry => entry.StableId == "hero_banner_marshal");
        var enemyEntry = registry.Catalog.Enemies[0];
        var heroRoot = heroEntry.Scene.Instantiate<UnitContentRoot>();
        var enemyRoot = enemyEntry.Scene.Instantiate<UnitContentRoot>();
        var screen = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
        AddChild(screen);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        try
        {
            var hero = BattleSetupFactory.Snapshot((UnitDefinition)heroEntry.Definition, heroRoot.Behavior) with
            {
                Damage = 0,
                Range = 1f,
                MoveTicks = 1
            };
            var enemy = BattleSetupFactory.Snapshot((UnitDefinition)enemyEntry.Definition, enemyRoot.Behavior) with
            {
                Damage = 1,
                Range = 20,
                AttackTicks = 1000,
                MoveTicks = 1000
            };
            var rule = BattleSetupFactory.Snapshot(heroRoot.HeroRule!, heroRoot.HeroCommand!);
            var config = new BattleConfig
            {
                Seed = 177,
                FloorRule = new ClearFloorRuleRuntime("motion-shared-hitch", "常规", "共享卡顿帧"),
                HeroRule = rule,
                Spawns =
                [
                    new BattleSpawn(hero, 0, new Vector2I(0, 2), "z-hitch-mover"),
                    new BattleSpawn(enemy, 1, new Vector2I(9, 2), "a-hitch-enemy")
                ]
            };

            foreach (var sharedDelta in new[] { .125, .25, .375 })
            {
                screen.StartBattle(registry, config, $"共享 {sharedDelta:0.00}s 卡顿帧");
                screen.SetProcess(false);
                var presenter = Presenter(screen, "z-hitch-mover");
                var motion = Motion(presenter);
                var source = screen.GetNode<BattleBoard>("%BattleBoard").CellToLocal(new Vector2I(0, 2));

                screen._Process(.001);
                screen._Process(sharedDelta);
                if (!motion.IsMoving)
                    throw new InvalidOperationException($"shared {sharedDelta:0.00}s production frame did not enqueue movement");
            motion._Process(sharedDelta);
            Equal(presenter.Position, source,
                $"shared {sharedDelta:0.00}s production frame consumed time from before its fresh move event");
                var previous = presenter.Position;
                for (var frame = 0; frame < 15; frame++)
                {
                    motion._Process(1.0 / 60.0);
                    if (presenter.Position.X + .001f < previous.X || presenter.Position.DistanceTo(previous) > 88.01f)
                        throw new InvalidOperationException($"shared {sharedDelta:0.00}s hitch recovery skipped backward or across multiple cells");
                    previous = presenter.Position;
                }
                var finalX = sharedDelta <= .125 ? 1 : sharedDelta <= .25 ? 2 : 3;
                var expected = screen.GetNode<BattleBoard>("%BattleBoard").CellToLocal(new Vector2I(finalX, 2));
                Near(presenter.Position, expected, .02f,
                    $"shared {sharedDelta:0.00}s hitch recovery exceeded effective presentation budget");
            }

        }
        finally
        {
            heroRoot.Free();
            enemyRoot.Free();
            RemoveChild(screen);
            screen.Free();
        }
    }

    private async Task SummonSnapAsync(ContentRegistry registry)
    {
        var merchantEntry = registry.Catalog.Heroes.Single(entry => entry.StableId == "hero_gilded_factor");
        var enemyEntry = registry.Catalog.Enemies[0];
        var merchantRoot = merchantEntry.Scene.Instantiate<UnitContentRoot>();
        var enemyRoot = enemyEntry.Scene.Instantiate<UnitContentRoot>();
        var screen = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn").Instantiate<BattleScreenController>();
        AddChild(screen);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        try
        {
            var hero = BattleSetupFactory.Snapshot((UnitDefinition)merchantEntry.Definition, merchantRoot.Behavior);
            var enemy = BattleSetupFactory.Snapshot((UnitDefinition)enemyEntry.Definition, enemyRoot.Behavior);
            var rule = BattleSetupFactory.Snapshot(merchantRoot.HeroRule!, merchantRoot.HeroCommand!);
            var summonEntry = registry.Catalog.Soldiers.Single(entry => entry.StableId == merchantRoot.HeroRule!.SummonContentId);
            var summon = BattleSetupFactory.Snapshot(summonEntry);
            screen.StartBattle(registry, new BattleConfig
            {
                Seed = 91,
                FloorRule = new ClearFloorRuleRuntime("motion-summon", "常规", "召唤落点测试"),
                HeroRule = rule,
                Summons = new SummonProfiles(Mercenary: summon),
                StartingGold = 5,
                Spawns =
                [
                    new BattleSpawn(hero, 0, new Vector2I(0, 2), "merchant"),
                    new BattleSpawn(enemy, 1, new Vector2I(9, 2), "enemy")
                ]
            }, "召唤直接落点");
            screen.GetNode<Button>("Margin/Layout/Hud/HeroCommandHud/Layout/Mana/CommandButton")
                .EmitSignal(BaseButton.SignalName.Pressed);
            var summoned = screen.GetNode<Node2D>("%UnitsRoot").GetChildren().OfType<UnitContentRoot>()
                .Single(unit => unit.RuntimeId.StartsWith("s-", StringComparison.Ordinal));
            var expected = screen.GetNode<BattleBoard>("%BattleBoard").CellToLocal(new Vector2I(0, 3));
            Equal(summoned.Position, expected, "summoned presenter slid in instead of snapping to its spawn cell");
            if (Motion(summoned).IsMoving) throw new InvalidOperationException("summoned presenter retained synthetic movement");
        }
        finally
        {
            merchantRoot.Free();
            enemyRoot.Free();
            RemoveChild(screen);
            screen.Free();
        }
    }

    private static void PresenterFreeDeterminism(ContentRegistry registry)
    {
        var heroEntry = registry.Catalog.Heroes.Single(entry => entry.StableId == "hero_banner_marshal");
        var enemyEntry = registry.Catalog.Enemies[0];
        var heroRoot = heroEntry.Scene.Instantiate<UnitContentRoot>();
        var enemyRoot = enemyEntry.Scene.Instantiate<UnitContentRoot>();
        try
        {
            var hero = BattleSetupFactory.Snapshot((UnitDefinition)heroEntry.Definition, heroRoot.Behavior);
            var enemy = BattleSetupFactory.Snapshot((UnitDefinition)enemyEntry.Definition, enemyRoot.Behavior);
            var rule = BattleSetupFactory.Snapshot(heroRoot.HeroRule!, heroRoot.HeroCommand!);
            var config = new BattleConfig
            {
                Seed = 9127,
                FloorRule = new ClearFloorRuleRuntime("motion-determinism-a", "常规", "确定性"),
                HeroRule = rule,
                Spawns =
                [
                    new BattleSpawn(hero, 0, new Vector2I(0, 2), "hero"),
                    new BattleSpawn(enemy, 1, new Vector2I(9, 2), "enemy")
                ]
            };
            var repeatConfig = new BattleConfig
            {
                Seed = config.Seed,
                FloorRule = new ClearFloorRuleRuntime("motion-determinism-b", "常规", "确定性"),
                HeroRule = rule,
                Spawns = config.Spawns.ToList()
            };
            using var first = new BattleSimulation(config);
            using var second = new BattleSimulation(repeatConfig);
            var firstMoves = new List<string>();
            var secondMoves = new List<string>();
            while (first.Outcome == BattleOutcome.Running)
            {
                first.Step();
                firstMoves.AddRange(first.DrainEvents().Where(entry => entry.Type == "move")
                    .Select(entry => $"{entry.Tick}:{entry.SourceRuntimeId}:{entry.Cell.X},{entry.Cell.Y}"));
            }
            while (second.Outcome == BattleOutcome.Running)
            {
                second.Step();
                secondMoves.AddRange(second.DrainEvents().Where(entry => entry.Type == "move")
                    .Select(entry => $"{entry.Tick}:{entry.SourceRuntimeId}:{entry.Cell.X},{entry.Cell.Y}"));
            }
            var firstResult = first.CreateResult();
            var secondResult = second.CreateResult();
            if (firstResult.Digest != secondResult.Digest || firstResult.Ticks != secondResult.Ticks ||
                !firstMoves.SequenceEqual(secondMoves, StringComparer.Ordinal))
                throw new InvalidOperationException("presenter-free result, digest, or move-event sequence lost determinism");
        }
        finally
        {
            heroRoot.Free();
            enemyRoot.Free();
        }
    }

    private async Task<UnitContentRoot> AttachCommanderAsync()
    {
        var unit = GD.Load<PackedScene>("res://content/heroes/hero_banner_marshal.tscn").Instantiate<UnitContentRoot>();
        AddChild(unit);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        return unit;
    }

    private static UnitMotionPresentationComponent Motion(UnitContentRoot unit) =>
        unit.GetNode<UnitMotionPresentationComponent>("UnitMotionPresentationComponent");

    private static UnitAnimationComponent Animation(UnitContentRoot unit) =>
        unit.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");

    private static UnitContentRoot Presenter(BattleScreenController screen, string runtimeId) =>
        screen.GetNode<Node2D>("%UnitsRoot").GetChildren().OfType<UnitContentRoot>()
            .Single(unit => unit.RuntimeId == runtimeId);

    private static BattleSimulation Simulation(BattleScreenController screen) =>
        (BattleSimulation)(typeof(BattleScreenController)
            .GetField("_simulation", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(screen)
            ?? throw new InvalidOperationException("production battle simulation unavailable"));

    private static DeterministicGridMovementService Movement(BattleSimulation simulation) =>
        (DeterministicGridMovementService)(typeof(BattleSimulation)
            .GetField("_movement", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(simulation)
            ?? throw new InvalidOperationException("production movement service unavailable"));

    private static void Rebind(UnitContentRoot unit, string runtimeId, int team = 0) => unit.Bind(runtimeId, team, 100, 100);

    private static void DetachAndFree(UnitContentRoot unit)
    {
        if (unit.GetParent() is not null) unit.GetParent().RemoveChild(unit);
        unit.Free();
    }

    private static void Equal(Vector2 actual, Vector2 expected, string message)
    {
        if (!actual.IsEqualApprox(expected)) throw new InvalidOperationException($"{message}: expected {expected}, actual {actual}");
    }

    private static void Near(Vector2 actual, Vector2 expected, float tolerance, string message)
    {
        if (actual.DistanceTo(expected) > tolerance) throw new InvalidOperationException($"{message}: expected {expected}, actual {actual}");
    }

    private static void Near(float actual, float expected, float tolerance, string message)
    {
        if (Math.Abs(actual - expected) > tolerance) throw new InvalidOperationException($"{message}: expected {expected}, actual {actual}");
    }

    private static void StrictlyBetween(float actual, float start, float end, string message)
    {
        var minimum = Math.Min(start, end);
        var maximum = Math.Max(start, end);
        if (actual <= minimum || actual >= maximum)
            throw new InvalidOperationException($"{message}: expected between {start} and {end}, actual {actual}");
    }

    private static void AdvanceFrames(UnitMotionPresentationComponent motion, int count, double delta)
    {
        for (var frame = 0; frame < count; frame++) motion._Process(delta);
    }

    private sealed class NullEvents : ISemanticBattleEventSink
    {
        public static readonly NullEvents Instance = new();
        public void Publish(SemanticBattleEvent battleEvent) { }
    }

    private sealed class NullCommands : IBattleCommandGateway
    {
        public static readonly NullCommands Instance = new();
        public bool Submit(BattleCommandRequest command) => false;
    }
}
