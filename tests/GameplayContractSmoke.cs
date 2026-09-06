using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;
using TowerAutobattler.Presentation;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.UI;

public partial class GameplayContractSmoke : Node
{
    private static readonly MovementFixtureAttributeOwner MovementAttributes = new();

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
            var catalog = GD.Load<ContentCatalog>("res://content/catalogs/alpha_catalog.tres") ?? throw new InvalidOperationException("catalog load");
            var gate = await TestProjectFixture.PublishAsync(this);
            var registry = gate.Package?.Content ?? throw new InvalidOperationException("content gate: " + string.Join("; ", gate.Report.CoreErrors));

            TeamScopedModifiersAndAuras();
            KillGrowthAndDeathOrdering();
            DeathSummonAndHazardShield();
            ContinuousSpaceGeometryAndMovement();
            ContinuousSpatialEffectsAndObjectives();
            ContinuousPlanningRecoveryContracts();
            SymmetricContinuousTrafficRecovery();
            ContinuousCooldownAndRollback();
            UniformShortRangeLineOfSight();
            HealerPursuesWoundedAlly();
            HealerWithoutLegalHealJoinsCombat();
            GoalReleaseOnAction();
            HealingLegalityAndProtection();
            QueuedMoverDeathCleanup();
            SameTickDeathCellReuseAndLifecycleCleanup();
            LifestealDeathIsTerminal(registry);
            ActualTimeArbiterEngagement(registry);
            SelectedUnitActionStateText();
            PresentationCueArbitration();
            ReadabilityAndIndependentTactics(registry);
            await AnimationLifecycleAsync(registry);
            TacticalCommandSceneParameters(registry);
            TacticalCommandEconomy();
            BossContracts();
            ProductionAbilityCompatibility(registry);
            FloorLifecycleExactlyOnce();
            EffectKernelCompatibilityAndLifecycle();
            await DefaultBattleSpeedAsync(registry);
            BattleResultSnapshotAndStatistics();
            BattleReportDerivationContracts();
            RunConversionAndSettings(registry);

            GD.Print("GAMEPLAY_CONTRACT_OK combat=team-aura,growth,death,hazard,continuous-space,swept-terrain,swept-bodies,dense-traffic,symmetric-traffic,distinct-engagement,splash,pierce,live-aura,beacon,edge-los,move-cooldown,spatial-rollback,healing,los-range2,death-cleanup,death-terminal,summon-nonoverlap,time-arbiter,bosses effects=typed-determinism,lifecycle-zero abilities=8-command-determinism,2-boss-equivalence report=immutable,effective-damage,shield,healing,command-healing,kills,join-defeat,actions,events,rates,shares,awards,environment pace=0.8-1.6-3.2,continuous-position-digest,end-hold-fade-fast-forward presentation=cue-priority,full-frames,cast-fallback commands=independent-scenes,tactical-points,transactional-economy lifecycle=exactly-once run=conversion,settings");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr($"GAMEPLAY_CONTRACT_FAILED: {exception}");
            return 1;
        }
        finally
        {
            MovementAttributes.Dispose();
        }
    }

    private static void TeamScopedModifiersAndAuras()
    {
        var enemyAura = Unit("enemy-aura", behavior: new UnitBehaviorSnapshot(AdjacentDamageAura: .2f));
        var config = Config(
            [
                Spawn(Hero("player"), 0, 0, 0, "player"),
                Spawn(Unit("enemy", damage: 100, range: 3), 1, 2, 0, "a-enemy"),
                Spawn(enemyAura, 1, 2, 1, "b-enemy-aura")
            ],
            rule: Rule(formationDamage: .5f),
            modifiers: new ModifierSnapshot(FormationAdjacentDamageMultiplier: 2f));
        using var simulation = new BattleSimulation(config);
        simulation.Step();
        var dealt = AttackValue(simulation, "a-enemy");
        Near(dealt, 120f, .01f, "enemy inherited player formation damage");

        var armorAura = Unit("player-armor-aura", behavior: new UnitBehaviorSnapshot(AdjacentArmorAura: 10));
        using var armorSimulation = new BattleSimulation(Config(
        [
            Spawn(Hero("player"), 0, 0, 0, "player"),
            Spawn(armorAura, 0, 0, 1, "player-aura"),
            Spawn(Unit("enemy", damage: 100, range: 3), 1, 2, 0, "a-enemy")
        ]));
        armorSimulation.Step();
        Near(AttackValue(armorSimulation, "a-enemy"), 100f * 100f / 170f, .02f, "allied armor aura missing");

        using var enemyArmorSimulation = new BattleSimulation(Config(
        [
            Spawn(Hero("attacker", damage: 100, range: 3), 0, 0, 0, "a-player"),
            Spawn(Unit("target", health: 1000, damage: 0, range: .5f, moveTicks: 1000), 1, 2, 0, "a-target"),
            Spawn(Unit("target-ally", health: 1000, damage: 0, range: .5f, moveTicks: 1000), 1, 2, 1, "b-target-ally")
        ], rule: Rule(formationArmor: 90), modifiers: new ModifierSnapshot(FormationAdjacentArmor: 90)));
        enemyArmorSimulation.Step();
        Near(AttackValue(enemyArmorSimulation, "a-player"), 100f, .01f, "enemy inherited player formation armor");
    }

    private static void KillGrowthAndDeathOrdering()
    {
        using var enemyKill = new BattleSimulation(Config(
        [
            Spawn(Hero("player", health: 1000), 0, 0, 5, "player"),
            Spawn(Unit("victim", health: 10), 0, 2, 0, "victim"),
            Spawn(Unit("enemy-beast", damage: 100, range: 3, tags: ["beast"]), 1, 4, 0, "a-enemy"),
            Spawn(Unit("enemy-beast-ally", tags: ["beast"]), 1, 4, 1, "b-enemy")
        ], rule: Rule(killGrowth: .5f, requiredTag: "beast")));
        enemyKill.Step();
        Near(enemyKill.Units.Single(unit => unit.RuntimeId == "a-enemy").Damage, 100, .001f, "enemy beast gained player kill growth");
        Near(enemyKill.Units.Single(unit => unit.RuntimeId == "b-enemy").Damage, 10, .001f, "enemy ally gained player kill growth");

        using var playerKill = new BattleSimulation(Config(
        [
            Spawn(Hero("player-beast", damage: 100, range: 3, tags: ["beast"]), 0, 0, 0, "a-player"),
            Spawn(Unit("enemy", health: 10, damage: 100, range: 3), 1, 2, 0, "z-dead")
        ], rule: Rule(killGrowth: .5f, requiredTag: "beast")));
        playerKill.Step();
        Near(playerKill.Units.Single(unit => unit.RuntimeId == "a-player").Damage, 150, .001f, "player kill growth missing");
        if (playerKill.DrainEvents().Any(e => e.Type == "attack" && e.SourceRuntimeId == "z-dead"))
            throw new InvalidOperationException("unit killed earlier in the tick still acted");
    }

    private static void DeathSummonAndHazardShield()
    {
        var summon = Unit("death-summon", health: 5);
        using var deathSimulation = new BattleSimulation(Config(
        [
            Spawn(Hero("player", health: 1000), 0, 0, 1, "player"),
            new BattleSpawn(Unit("soldier", health: 100), 0, new Vector2I(0, 0), "soldier", .05f),
            Spawn(Unit("enemy", health: 1000), 1, 9, 5, "enemy")
        ], floor: new DamageEveryTickRule(10), rule: Rule(summonOnDeath: true), summons: new SummonProfiles(DeathSummon: summon)));
        deathSimulation.Step();
        deathSimulation.Step();
        var temporary = deathSimulation.Units.Where(unit => unit.IsTemporary).ToArray();
        if (temporary.Length != 1 || temporary[0].Alive)
            throw new InvalidOperationException("death summon recursed or did not resolve once");

        using var shieldSimulation = new BattleSimulation(Config(
        [
            new BattleSpawn(Hero("shielded", health: 100), 0, new Vector2I(0, 0), "shielded", .05f),
            Spawn(Unit("enemy", health: 1000), 1, 9, 5, "enemy")
        ], floor: new DamageEveryTickRule(12), modifiers: new ModifierSnapshot(StartShield: 5)));
        shieldSimulation.Step();
        var shielded = shieldSimulation.Units.Single(unit => unit.RuntimeId == "shielded");
        if (shielded.Alive || shielded.Shield != 0)
            throw new InvalidOperationException("floor hazard did not consume shield before lethal health damage");
        if (!shieldSimulation.DrainEvents().Any(e => e.Type == "defeated" && e.SourceRuntimeId == "floor" && e.TargetRuntimeId == "shielded"))
            throw new InvalidOperationException("lethal floor hazard did not emit an attributed defeated event");
    }

    private static void ContinuousSpaceGeometryAndMovement()
    {
        var wall = new Vector2I(2, 2);
        bool Terrain(Vector2I cell) => cell != wall;
        var horizontal = BattlefieldSpace.FirstTerrainHitFraction(
            new Vector2(0, 2), new Vector2(5, 0), .2f, 10, 6, Terrain);
        var vertical = BattlefieldSpace.FirstTerrainHitFraction(
            new Vector2(2, 0), new Vector2(0, 5), .2f, 10, 6, Terrain);
        var parallelOutside = BattlefieldSpace.FirstTerrainHitFraction(
            new Vector2(0, 0), new Vector2(5, 0), .2f, 10, 6, Terrain);
        if (horizontal is <= 0 or >= 1 || vertical is <= 0 or >= 1 ||
            !Mathf.IsEqualApprox(parallelOutside, 1f))
            throw new InvalidOperationException(
                $"axis-aligned terrain sweeps were incorrect: horizontal={horizontal}, vertical={vertical}, parallel={parallelOutside}");

        if (!BattlefieldSpace.TryMovingCircleTimeOfImpact(
                Vector2.Zero, new Vector2(4, 0), .3f,
                new Vector2(2, 0), Vector2.Zero, .3f,
                out var stationaryContact) || stationaryContact is <= 0 or >= 1)
            throw new InvalidOperationException("moving circle tunneled through a stationary circle");
        if (!BattlefieldSpace.TryMovingCircleTimeOfImpact(
                Vector2.Zero, new Vector2(2, 0), .3f,
                new Vector2(2, 0), new Vector2(-2, 0), .3f,
                out var movingContact) || movingContact is <= 0 or >= 1)
            throw new InvalidOperationException("opposing moving circles had no deterministic contact time");

        using (var duel = new BattleSimulation(Config(
               [
                   Spawn(Hero("continuous-hero", health: 1000, damage: 0, range: 1, moveTicks: 4), 0, 0, 2, "hero"),
                   Spawn(Unit("continuous-enemy", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 5, 2, "enemy")
               ])))
        {
            duel.Units.Single(unit => unit.RuntimeId == "enemy").MoveCooldown = 999;
            duel.Step();
            var hero = duel.Units.Single(unit => unit.RuntimeId == "hero");
            var move = duel.DrainEvents().SingleOrDefault(battleEvent =>
                battleEvent.Type == "move" && battleEvent.SourceRuntimeId == hero.RuntimeId);
            if (move is null || move.Position.IsEqualApprox(BattlefieldSpace.CellCenter(hero.Cell)) ||
                !move.Position.IsEqualApprox(hero.Position))
                throw new InvalidOperationException("battle movement remained quantized to authored cell centers");
        }

        using (var engagement = new BattleSimulation(Config(
               [
                   Spawn(Hero("engage-a", health: 1000, damage: 0, range: 1, moveTicks: 3), 0, 0, 1, "a"),
                   Spawn(Unit("engage-b", health: 1000, damage: 0, range: 1, moveTicks: 3), 0, 0, 2, "b"),
                   Spawn(Unit("engage-c", health: 1000, damage: 0, range: 1, moveTicks: 3), 0, 0, 3, "c"),
                   Spawn(Unit("engage-target", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 5, 2, "target")
               ])))
        {
            engagement.Units.Single(unit => unit.RuntimeId == "target").MoveCooldown = 999;
            var attackersThatActed = new HashSet<string>(StringComparer.Ordinal);
            for (var tick = 0; tick < 48 && engagement.Outcome == BattleOutcome.Running; tick++)
            {
                engagement.Step();
                AssertNoLivingOverlaps(engagement.Units, "multi-attacker engagement");
                foreach (var battleEvent in engagement.DrainEvents().Where(battleEvent =>
                             battleEvent.Type == "attack" && battleEvent.SourceRuntimeId is "a" or "b" or "c"))
                {
                    var attacker = engagement.Units.Single(unit => unit.RuntimeId == battleEvent.SourceRuntimeId);
                    var target = engagement.Units.Single(unit => unit.RuntimeId == "target");
                    if (!BattlefieldSpace.IsWithinReach(attacker, target, attacker.AttackRange))
                        throw new InvalidOperationException("multi-attacker event resolved outside continuous body-edge reach");
                    attackersThatActed.Add(attacker.RuntimeId);
                }
            }
            var attackers = engagement.Units.Where(unit => unit.Team == 0).Select(unit => unit.Position).ToArray();
            if (attackersThatActed.Count != 3 || attackers.Distinct().Count() != attackers.Length ||
                attackers.All(position => position.IsEqualApprox(BattlefieldSpace.CellCenter(BattlefieldSpace.PositionToCell(position)))))
                throw new InvalidOperationException(
                    $"multiple attackers did not reach distinct useful continuous engagement positions: acted={string.Join(',', attackersThatActed)}");
        }

        var denseSpawns = new List<BattleSpawn>();
        for (var index = 0; index < 9; index++)
        {
            var x = index / 6;
            var y = index % 6;
            denseSpawns.Add(Spawn(index == 0 ? Hero("dense-hero", health: 2000, damage: 0, moveTicks: 5) :
                Unit($"dense-player-{index}", health: 2000, damage: 0, moveTicks: 5), 0, x, y, $"p-{index:00}"));
            denseSpawns.Add(Spawn(Unit($"dense-enemy-{index}", health: 2000, damage: 0, moveTicks: 5), 1,
                9 - x, y, $"e-{index:00}"));
        }
        using var dense = new BattleSimulation(Config(
            denseSpawns,
            floor: new NarrowLanesRuntime("dense-narrow", "拥挤狭路", "continuous-space performance fixture")));
        var denseStarts = dense.Units.ToDictionary(unit => unit.RuntimeId, unit => unit.Position, StringComparer.Ordinal);
        var denseActions = 0;
        var stopwatch = Stopwatch.StartNew();
        for (var tick = 0; tick < 36 && dense.Outcome == BattleOutcome.Running; tick++)
        {
            dense.Step();
            AssertNoLivingOverlaps(dense.Units, "dense opposing traffic");
            denseActions += dense.DrainEvents().Count(battleEvent => battleEvent.Type is "move" or "attack");
        }
        stopwatch.Stop();
        var playerProgress = dense.Units.Where(unit => unit.Team == 0)
            .Max(unit => unit.Position.X - denseStarts[unit.RuntimeId].X);
        var enemyProgress = dense.Units.Where(unit => unit.Team == 1)
            .Max(unit => denseStarts[unit.RuntimeId].X - unit.Position.X);
        GD.Print($"CONTINUOUS_SPACE_DENSE_9V9_36_TICKS_MS={stopwatch.ElapsedMilliseconds} actions={denseActions} progress={playerProgress:0.###}/{enemyProgress:0.###}");
        if (stopwatch.ElapsedMilliseconds > 5000)
            throw new InvalidOperationException($"dense continuous movement exceeded smoke budget: {stopwatch.ElapsedMilliseconds}ms");
        if (denseActions < 18 || playerProgress < .5f || enemyProgress < .5f)
            throw new InvalidOperationException(
                $"dense opposing traffic made no useful congestion progress: actions={denseActions}, progress={playerProgress}/{enemyProgress}");
    }

    private static void AssertNoLivingOverlaps(IEnumerable<BattleUnitState> units, string context)
    {
        var living = units.Where(unit => unit.Alive).OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray();
        for (var first = 0; first < living.Length; first++)
        for (var second = first + 1; second < living.Length; second++)
            if (living[first].Position.DistanceTo(living[second].Position) + .0001f <
                living[first].BodyRadius + living[second].BodyRadius + BattlefieldSpace.BodyClearance)
                throw new InvalidOperationException(
                    $"{context} overlapped {living[first].RuntimeId} and {living[second].RuntimeId}");
    }

    private static void ContinuousSpatialEffectsAndObjectives()
    {
        using (var splash = new BattleSimulation(Config(
               [
                   Spawn(Unit("splash-attacker", isHero: true, health: 1000, damage: 100, range: 10, splash: 2.2f), 0, 0, 1, "a-attacker"),
                   Spawn(Unit("splash-primary", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 3, 1, "b-primary"),
                   Spawn(Unit("splash-edge", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 5, 2, "c-splash-edge")
               ])))
        {
            var attacker = splash.Units.Single(unit => unit.RuntimeId == "a-attacker");
            var primary = splash.Units.Single(unit => unit.RuntimeId == "b-primary");
            var edge = splash.Units.Single(unit => unit.RuntimeId == "c-splash-edge");
            attacker.Position = new Vector2(.5f, 1f);
            primary.Position = new Vector2(3f, 1f);
            edge.Position = new Vector2(5.2f, 2f);
            if (Math.Abs(primary.Cell.X - edge.Cell.X) + Math.Abs(primary.Cell.Y - edge.Cell.Y) <= attacker.Definition.SplashRadius)
                throw new InvalidOperationException("splash edge fixture did not leave the old cell-distance radius");
            splash.Step();
            Near(primary.Health, 900f, .01f, "continuous splash primary damage");
            Near(edge.Health, 955f, .01f, "continuous splash missed a body intersecting the radius edge");
        }

        using (var piercing = new BattleSimulation(Config(
               [
                   Spawn(Unit("piercing-attacker", isHero: true, health: 1000, damage: 100, range: 10,
                       behavior: new UnitBehaviorSnapshot(PiercingLine: true)), 0, 0, 2, "a-attacker"),
                   Spawn(Unit("piercing-primary", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 3, 2, "b-primary"),
                   Spawn(Unit("piercing-capsule", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 5, 3, "c-capsule")
               ])))
        {
            var attacker = piercing.Units.Single(unit => unit.RuntimeId == "a-attacker");
            var primary = piercing.Units.Single(unit => unit.RuntimeId == "b-primary");
            var capsule = piercing.Units.Single(unit => unit.RuntimeId == "c-capsule");
            attacker.Position = new Vector2(.5f, 2.45f);
            primary.Position = new Vector2(3f, 2.45f);
            capsule.Position = new Vector2(5f, 2.55f);
            if (primary.Cell.Y == capsule.Cell.Y)
                throw new InvalidOperationException("piercing fixture did not cross a derived grid row");
            piercing.Step();
            Near(capsule.Health, 965f, .01f,
                "continuous piercing capsule missed or applied duplicate damage to the behind target");
        }

        using (var aura = new BattleSimulation(Config(
               [
                   Spawn(Unit("live-aura-attacker", health: 1000, damage: 100, range: 10), 1, 1, 1, "a-attacker"),
                   Spawn(Unit("live-damage-aura", health: 1000, damage: 0, range: 1, moveTicks: 1000,
                       behavior: new UnitBehaviorSnapshot(AdjacentDamageAura: .5f)), 1, 3, 1, "z-damage-aura"),
                   Spawn(Hero("live-aura-target", health: 1000, damage: 0, range: 1, moveTicks: 1000), 0, 7, 1, "b-target"),
                   Spawn(Unit("live-armor-aura", health: 1000, damage: 0, range: 1, moveTicks: 1000,
                       behavior: new UnitBehaviorSnapshot(AdjacentArmorAura: 10)), 0, 9, 1, "z-armor-aura")
               ])))
        {
            var attacker = aura.Units.Single(unit => unit.RuntimeId == "a-attacker");
            var damageAura = aura.Units.Single(unit => unit.RuntimeId == "z-damage-aura");
            var target = aura.Units.Single(unit => unit.RuntimeId == "b-target");
            var armorAura = aura.Units.Single(unit => unit.RuntimeId == "z-armor-aura");
            attacker.Position = new Vector2(1f, 1f);
            damageAura.Position = new Vector2(2.7f, 1f);
            target.Position = new Vector2(7f, 1f);
            armorAura.Position = new Vector2(8.7f, 1f);
            if (Math.Abs(attacker.Cell.X - damageAura.Cell.X) <= 1 || Math.Abs(target.Cell.X - armorAura.Cell.X) <= 1)
                throw new InvalidOperationException("live aura fixture remained adjacent in derived grid cells");
            aura.Step();
            Near(target.Health, 1000f - 150f * 100f / 170f, .02f,
                "live damage/armor auras ignored continuous body-edge distance");
        }

        using (var beacon = new BattleSimulation(Config(
               [
                   new BattleSpawn(Hero("beacon-edge", health: 200, damage: 0, range: 1, moveTicks: 1000), 0,
                       new Vector2I(7, 3), "beacon-edge", .5f),
                   Spawn(Unit("beacon-enemy", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 9, 5, "beacon-enemy")
               ], floor: new HealingBeaconRuntime("continuous-beacon", "信标", "连续半径", 1, 20))))
        {
            var hero = beacon.Units.Single(unit => unit.RuntimeId == "beacon-edge");
            hero.Position = new Vector2(6.75f, 3f);
            if (Math.Abs(hero.Cell.X - BattleSimulation.Width / 2) <= 1)
                throw new InvalidOperationException("beacon fixture remained within the old adjacent-cell band");
            beacon.Step();
            Near(hero.Health, 120f, .01f, "beacon control ignored body-edge distance at a continuous position");
        }

        var wall = new Vector2I(7, 1);
        using var edgeLos = new BattleSimulation(Config(
        [
            Spawn(Hero("edge-los-attacker", health: 1000, damage: 1, range: 2.2f, moveTicks: 3), 0, 0, 5, "attacker"),
            Spawn(Unit("edge-los-target", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 9, 0, "target")
        ], floor: new BlockedCellsRule([wall])));
        var ranged = edgeLos.Units.Single(unit => unit.RuntimeId == "attacker");
        var edgeTarget = edgeLos.Units.Single(unit => unit.RuntimeId == "target");
        edgeTarget.MoveCooldown = 999;
        if (BattlefieldSpace.IsSegmentTerrainClear(
                ranged.Position, edgeTarget.Position, 0f, BattleSimulation.Width, BattleSimulation.Height,
                cell => cell != wall))
            throw new InvalidOperationException("edge LOS fixture did not begin behind blocked direct line access");
        var attacked = false;
        for (var tick = 0; tick < 120 && !attacked; tick++)
        {
            edgeLos.Step();
            attacked = edgeLos.DrainEvents().Any(battleEvent =>
                battleEvent.Type == "attack" && battleEvent.SourceRuntimeId == ranged.RuntimeId);
            AssertNoLivingOverlaps(edgeLos.Units, "edge LOS engagement");
        }
        if (!attacked || !BattlefieldSpace.IsWithinReach(ranged, edgeTarget, ranged.AttackRange) ||
            !BattlefieldSpace.IsSegmentTerrainClear(
                ranged.Position, edgeTarget.Position, 0f, BattleSimulation.Width, BattleSimulation.Height,
                cell => cell != wall) ||
            !BattlefieldSpace.IsPositionTerrainClear(
                ranged.Position, ranged.BodyRadius, BattleSimulation.Width, BattleSimulation.Height,
                cell => cell != wall))
            throw new InvalidOperationException(
                $"ranged unit did not find a legal inner engagement point around the edge target: {ranged.Position}");
    }

    private static void SymmetricContinuousTrafficRecovery()
    {
        var leftGoal = State(Unit("left-goal", range: 1, moveTicks: 1000), "left-goal");
        leftGoal.Position = new Vector2(0f, 1f);
        var rightGoal = State(Unit("right-goal", range: 1, moveTicks: 1000), "right-goal", team: 1);
        rightGoal.Position = new Vector2(9f, 1f);
        var movingRight = State(Unit("moving-right", range: 1, moveTicks: 3), "moving-right");
        movingRight.Position = new Vector2(2f, 1f);
        var movingLeft = State(Unit("moving-left", range: 1, moveTicks: 3), "moving-left", team: 1);
        movingLeft.Position = new Vector2(7f, 1f);
        var units = new List<BattleUnitState> { leftGoal, rightGoal, movingRight, movingLeft };
        using var movement = new DeterministicContinuousMovementService(10, 3, () => units, _ => true, (_, _) => true, 0x51DEUL);
        for (var tick = 0; tick < 80; tick++)
        {
            movement.BeginTick();
            if (!BattlefieldSpace.IsWithinReach(movingRight, rightGoal, movingRight.AttackRange))
            {
                movement.SelectTarget(movingRight, [rightGoal]);
                movement.QueueMove(movingRight);
            }
            if (!BattlefieldSpace.IsWithinReach(movingLeft, leftGoal, movingLeft.AttackRange))
            {
                movement.SelectTarget(movingLeft, [leftGoal]);
                movement.QueueMove(movingLeft);
            }
            movement.ResolveIntents((_, _) => { });
            AssertNoLivingOverlaps(units, "symmetric opposing traffic");
        }
        if (!BattlefieldSpace.IsWithinReach(movingRight, rightGoal, movingRight.AttackRange) ||
            !BattlefieldSpace.IsWithinReach(movingLeft, leftGoal, movingLeft.AttackRange) ||
            movingRight.Position.X <= 5f || movingLeft.Position.X >= 4f)
            throw new InvalidOperationException(
                $"deterministic PairSide traffic did not clear the head-on encounter: right={movingRight.Position}, left={movingLeft.Position}");
    }

    private static void ContinuousCooldownAndRollback()
    {
        using (var slowed = new BattleSimulation(Config(
               [
                   Spawn(Unit("slow-attacker", isHero: true, health: 1000, damage: 1, range: 10, attackTicks: 1000,
                       behavior: new UnitBehaviorSnapshot(SlowOnHitTicks: 4)), 0, 0, 2, "a-attacker"),
                   Spawn(Unit("slow-target", health: 1000, damage: 0, range: 1, moveTicks: 2,
                       behavior: new UnitBehaviorSnapshot()), 1, 4, 2, "z-target")
               ])))
        {
            var target = slowed.Units.Single(unit => unit.RuntimeId == "z-target");
            var start = target.Position;
            var cooldowns = new List<int>();
            for (var tick = 0; tick < 4; tick++)
            {
                slowed.Step();
                slowed.DrainEvents();
                cooldowns.Add(target.MoveCooldown);
                if (tick < 3 && !target.Position.IsEqualApprox(start))
                    throw new InvalidOperationException("MoveCooldown slow ended before its 0.1-second ticks elapsed");
            }
            if (!cooldowns.SequenceEqual([3, 2, 1, 0]) || target.Position.IsEqualApprox(start))
                throw new InvalidOperationException(
                    $"MoveCooldown did not retain fixed-tick semantics: {string.Join(',', cooldowns)} at {target.Position}");
        }

        using var rollback = new BattleSimulation(Config(
        [
            Spawn(Hero("rollback-mover", health: 1000, damage: 0, range: 1, moveTicks: 3), 0, 0, 2, "mover"),
            Spawn(Unit("rollback-target", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 6, 2, "target")
        ]));
        var mover = rollback.Units.Single(unit => unit.RuntimeId == "mover");
        var targetUnit = rollback.Units.Single(unit => unit.RuntimeId == "target");
        var movement = Movement(rollback);
        rollback.DrainEvents();
        movement.BeginTick();
        movement.SelectTarget(mover, [targetUnit]);
        if (!movement.QueueMove(mover)) throw new InvalidOperationException("rollback fixture did not queue continuous movement");
        var beforePosition = mover.Position;
        var beforeDigest = rollback.CreateResult().Digest;
        var planningBefore = movement.PlanningStateCount;
        var checkpointType = typeof(BattleSimulation).GetNestedType("BattleWorldStateCheckpoint", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("battle rollback checkpoint type missing");
        var checkpoint = checkpointType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Single().Invoke([rollback]);
        var emit = typeof(BattleSimulation).GetMethod(
            "Emit", BindingFlags.Instance | BindingFlags.NonPublic, null,
            [typeof(string), typeof(string), typeof(string), typeof(float), typeof(Vector2), typeof(string)], null)
            ?? throw new InvalidOperationException("continuous event append method missing");
        void ResolveWithEvent() => movement.ResolveIntents((unit, position) =>
            emit.Invoke(rollback, ["move", unit.RuntimeId, "", 0f, position, "move"]));
        ResolveWithEvent();
        var attemptedPosition = mover.Position;
        var attemptedDigest = rollback.CreateResult().Digest;
        if (attemptedPosition.IsEqualApprox(beforePosition) || attemptedPosition.IsEqualApprox(BattlefieldSpace.CellCenter(mover.Cell)))
            throw new InvalidOperationException("rollback fixture did not reach a non-cell-center attempted position");
        checkpointType.GetMethod("Rollback", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(checkpoint, null);
        if (!mover.Position.IsEqualApprox(beforePosition) || rollback.CreateResult().Digest != beforeDigest ||
            movement.PlanningStateCount != planningBefore || movement.PendingRequestCount != 1)
            throw new InvalidOperationException("rollback did not restore continuous position, digest, and planning request state");
        ResolveWithEvent();
        if (!mover.Position.IsEqualApprox(attemptedPosition) || rollback.CreateResult().Digest != attemptedDigest ||
            rollback.DrainEvents().Count(battleEvent => battleEvent.Type == "move") != 1)
            throw new InvalidOperationException("continuous rollback retry changed position, digest, or event identity");
    }

    private static void ContinuousPlanningRecoveryContracts()
    {
        var forward = RunContinuousRequestOrder(false);
        var reverse = RunContinuousRequestOrder(true);
        if (!forward.SequenceEqual(reverse))
            throw new InvalidOperationException("continuous movement depended on QueueMove enumeration order");

        var mover = State(Unit("detour-mover", range: 1, moveTicks: 3), "mover");
        mover.Cell = new Vector2I(0, 2);
        var target = State(Unit("detour-target", range: 1, moveTicks: 1000), "target", team: 1);
        target.Cell = new Vector2I(6, 2);
        var detourUnits = new List<BattleUnitState> { mover, target };
        var wall = new HashSet<Vector2I> { new(2, 1), new(2, 2), new(2, 3) };
        using (var detour = new DeterministicContinuousMovementService(
                   7, 5, () => detourUnits, cell => !wall.Contains(cell), (_, _) => true, 73))
        {
            var start = mover.Position;
            for (var tick = 0; tick < 30 && !BattlefieldSpace.IsWithinReach(mover, target, mover.AttackRange); tick++)
            {
                detour.BeginTick();
                if (detour.SelectTarget(mover, [target]) is null || !detour.QueueMove(mover))
                    throw new InvalidOperationException("reachable detour lost its strategic target");
                detour.ResolveIntents((_, _) => { });
                if (!BattlefieldSpace.IsPositionTerrainClear(
                        mover.Position, mover.BodyRadius, 7, 5, cell => !wall.Contains(cell)))
                    throw new InvalidOperationException("continuous detour entered blocked terrain");
            }
            if (mover.Position.DistanceTo(start) < 1f || !BattlefieldSpace.IsWithinReach(mover, target, mover.AttackRange))
                throw new InvalidOperationException("continuous detour did not recover through the reachable side lane");
        }

        var retargetMover = State(Unit("retarget-mover", range: 1), "retarget-mover");
        retargetMover.Cell = new Vector2I(2, 2);
        var unreachable = State(Unit("unreachable", range: 1), "unreachable", team: 1);
        unreachable.Cell = new Vector2I(5, 2);
        var reachable = State(Unit("reachable", range: 1), "reachable", team: 1);
        reachable.Cell = new Vector2I(0, 2);
        var retargetUnits = new List<BattleUnitState> { retargetMover, unreachable, reachable };
        using (var retarget = new DeterministicContinuousMovementService(
                   7, 5, () => retargetUnits, cell => cell.X != 3, (_, _) => true, 79))
        {
            retarget.BeginTick();
            if (retarget.SelectTarget(retargetMover, [unreachable, reachable]) != reachable)
                throw new InvalidOperationException("target selection retained a spatially unreachable first candidate");
        }

        var chain = Enumerable.Range(0, 6)
            .Select(index =>
            {
                var unit = State(Unit($"chain-{index}", range: 1, moveTicks: 3), $"chain-{index}");
                unit.Cell = new Vector2I(index, 1);
                return unit;
            })
            .ToList();
        var chainTarget = State(Unit("chain-target", range: 1, moveTicks: 1000), "chain-target", team: 1);
        chainTarget.Cell = new Vector2I(9, 1);
        var chainUnits = chain.Append(chainTarget).ToList();
        using var chainService = new DeterministicContinuousMovementService(
            10, 3, () => chainUnits, cell => cell.Y == 1, (_, _) => true, 83);
        var rearStart = chain[0].Position;
        for (var tick = 0; tick < 24; tick++)
        {
            chainService.BeginTick();
            foreach (var unit in chain)
            {
                chainService.SelectTarget(unit, [chainTarget]);
                if (!BattlefieldSpace.IsWithinReach(unit, chainTarget, unit.AttackRange))
                    chainService.QueueMove(unit);
            }
            chainService.ResolveIntents((_, _) => { });
            AssertNoLivingOverlaps(chainUnits, "six-unit follow chain");
        }
        if (chain[0].Position.X <= rearStart.X + .25f)
            throw new InvalidOperationException("six-unit continuous follow chain remained permanently stuck");
    }

    private static Vector2[] RunContinuousRequestOrder(bool reverse)
    {
        var first = State(Unit("order-first", range: 1, moveTicks: 3), "first");
        first.Cell = new Vector2I(0, 1);
        var second = State(Unit("order-second", range: 1, moveTicks: 3), "second");
        second.Cell = new Vector2I(0, 2);
        var target = State(Unit("order-target", range: 1, moveTicks: 1000), "target", team: 1);
        target.Cell = new Vector2I(5, 1);
        var units = new List<BattleUnitState> { first, second, target };
        using var service = new DeterministicContinuousMovementService(6, 4, () => units, _ => true, (_, _) => true, 67);
        service.BeginTick();
        foreach (var mover in reverse ? new[] { second, first } : new[] { first, second })
        {
            service.SelectTarget(mover, [target]);
            service.QueueMove(mover);
        }
        service.ResolveIntents((_, _) => { });
        return new[] { first.Position, second.Position };
    }

    private static void UniformShortRangeLineOfSight()
    {
        var wall = new[] { new Vector2I(1, 2) };
        using var attack = new BattleSimulation(Config(
        [
            Spawn(Hero("range-two-attacker", damage: 100, range: 2), 0, 0, 2, "attacker"),
            Spawn(Unit("range-two-target", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 2, 2, "target")
        ], floor: new BlockedCellsRule(wall)));
        attack.Step();
        if (attack.DrainEvents().Any(battleEvent => battleEvent.Type == "attack" && battleEvent.SourceRuntimeId == "attacker"))
            throw new InvalidOperationException("range-two attack ignored the intermediate terrain wall");

        using var healing = new BattleSimulation(Config(
        [
            Spawn(Unit("range-two-healer", isHero: true, health: 200, damage: 0, range: 2, heal: 25), 0, 0, 2, "healer"),
            new BattleSpawn(Unit("wounded", health: 100, damage: 0, range: 1, moveTicks: 1000), 0,
                new Vector2I(2, 2), "wounded", .5f),
            Spawn(Unit("range-two-enemy", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 9, 5, "enemy")
        ], floor: new BlockedCellsRule(wall)));
        healing.Step();
        if (healing.DrainEvents().Any(battleEvent => battleEvent.Type == "heal" && battleEvent.SourceRuntimeId == "healer"))
            throw new InvalidOperationException("range-two healing ignored the intermediate terrain wall");
    }

    private static void HealerPursuesWoundedAlly()
    {
        using var simulation = new BattleSimulation(Config(
        [
            Spawn(Hero("player"), 0, 0, 5, "player"),
            Spawn(Unit("healer", heal: 10, range: 1), 0, 0, 0, "a-healer"),
            new BattleSpawn(Unit("wounded", health: 100), 0, new Vector2I(0, 3), "wounded", .5f),
            Spawn(Unit("enemy", health: 1000), 1, 9, 0, "enemy")
        ]));
        var healer = simulation.Units.Single(unit => unit.RuntimeId == "a-healer");
        healer.AttackCooldown = 10;
        var start = healer.Position;
        simulation.Step();
        if (healer.Position.Y <= start.Y || Math.Abs(healer.Position.X - start.X) > .15f ||
            healer.ActionTargetRuntimeId != "wounded")
            throw new InvalidOperationException($"healer pursued enemy instead of distant wounded ally: {healer.Position}");
    }

    private static void HealerWithoutLegalHealJoinsCombat()
    {
        using var healerSimulation = new BattleSimulation(Config(
        [
            Spawn(Hero("player", health: 1000), 0, 0, 5, "player"),
            Spawn(Unit("healer", heal: 10, range: 4), 0, 0, 2, "a-healer"),
            Spawn(Unit("enemy", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 6, 2, "enemy")
        ]));
        healerSimulation.Units.Single(unit => unit.RuntimeId == "enemy").MoveCooldown = 999;
        healerSimulation.Step();
        var healerMoved = healerSimulation.DrainEvents().Any(battleEvent =>
            battleEvent.Type == "move" && battleEvent.SourceRuntimeId == "a-healer");
        if (!healerMoved)
            throw new InvalidOperationException("full-health healer failed to join ordinary combat when no legal heal target existed");

        using var rangedSimulation = new BattleSimulation(Config(
        [
            Spawn(Hero("player", health: 1000), 0, 0, 5, "player"),
            Spawn(Unit("ranged", range: 4), 0, 0, 2, "a-ranged"),
            Spawn(Unit("enemy", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 6, 2, "enemy")
        ]));
        rangedSimulation.Units.Single(unit => unit.RuntimeId == "enemy").MoveCooldown = 999;
        rangedSimulation.Step();
        var rangedMoved = rangedSimulation.DrainEvents().Any(battleEvent =>
            battleEvent.Type == "move" && battleEvent.SourceRuntimeId == "a-ranged");
        if (!rangedMoved)
            throw new InvalidOperationException("non-healer control unit did not engage from the same full-health setup");
    }

    private static void GoalReleaseOnAction()
    {
        using var simulation = new BattleSimulation(Config(
        [
            Spawn(Hero("goal-release", health: 1000, damage: 0, range: 1), 0, 0, 1, "mover"),
            Spawn(Unit("stationary-target", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 4, 1, "target")
        ]));
        simulation.Units.Single(unit => unit.RuntimeId == "target").MoveCooldown = 999;
        var movement = Movement(simulation);
        var attacked = false;
        for (var tick = 0; tick < 8 && !attacked; tick++)
        {
            simulation.Step();
            attacked = simulation.DrainEvents().Any(battleEvent => battleEvent.Type == "attack" &&
                battleEvent.SourceRuntimeId == "mover");
        }
        var mover = simulation.Units.Single(unit => unit.RuntimeId == "mover");
        if (!attacked || movement.ActiveGoalCount != 0 || movement.RetargetLeaseCount != 0 || mover.WaitingTicks != 0)
            throw new InvalidOperationException("entering legal attack range retained a ghost goal or blocked-route lease");
    }

    private static void HealingLegalityAndProtection()
    {
        var wall = Enumerable.Range(0, 6).Select(y => new Vector2I(4, y));
        using var simulation = new BattleSimulation(Config(
        [
            Spawn(Hero("player", health: 1000), 0, 0, 5, "player"),
            Spawn(Unit("healer", heal: 10, range: 2, attackTicks: 5), 0, 0, 2, "healer"),
            new BattleSpawn(Unit("reachable", health: 100), 0, new Vector2I(1, 2), "reachable", .5f),
            new BattleSpawn(Unit("unreachable", health: 100), 0, new Vector2I(7, 2), "unreachable", .1f),
            Spawn(Unit("enemy", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 9, 5, "enemy")
        ], floor: new BlockedCellsRule(wall)));
        simulation.Step();
        var first = simulation.DrainEvents();
        if (!first.Any(battleEvent => battleEvent.Type == "heal" && battleEvent.TargetRuntimeId == "reachable") ||
            first.Any(battleEvent => battleEvent.Type == "heal" && battleEvent.TargetRuntimeId == "unreachable"))
            throw new InvalidOperationException("healer did not skip unreachable lowest-health ally for reachable legal alternative");
        simulation.Step();
        var healer = simulation.Units.Single(unit => unit.RuntimeId == "healer");
        if (healer.Mode != BattleUnitMode.Recovering || healer.LastActionKind != BattleActionKind.Heal || healer.ActionTargetRuntimeId != "reachable")
            throw new InvalidOperationException("healer did not retain valid protected ally through cooldown");

        using var los = new BattleSimulation(Config(
        [
            Spawn(Hero("player", health: 1000), 0, 0, 5, "player"),
            Spawn(Unit("healer", heal: 10, range: 6), 0, 1, 2, "healer"),
            new BattleSpawn(Unit("blocked-wounded", health: 100), 0, new Vector2I(6, 2), "blocked-wounded", .5f),
            Spawn(Unit("enemy", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 9, 5, "enemy")
        ], floor: new BlockedCellsRule(wall)));
        los.Step();
        if (los.DrainEvents().Any(battleEvent => battleEvent.Type == "heal" && battleEvent.TargetRuntimeId == "blocked-wounded"))
            throw new InvalidOperationException("healing ignored line access");
    }

    private static void QueuedMoverDeathCleanup()
    {
        using var simulation = new BattleSimulation(Config(
        [
            Spawn(Hero("player", health: 1000), 0, 0, 5, "player"),
            Spawn(Unit("mover", health: 10, range: 1), 0, 0, 2, "a-mover"),
            Spawn(Unit("killer", health: 1000, damage: 1000, range: 10, moveTicks: 1000), 1, 9, 2, "z-killer")
        ]));
        simulation.Step();
        var events = simulation.DrainEvents();
        var mover = simulation.Units.Single(unit => unit.RuntimeId == "a-mover");
        if (mover.Alive || mover.Mode != BattleUnitMode.Defeated ||
            events.Any(battleEvent => battleEvent.Type == "move" && battleEvent.SourceRuntimeId == "a-mover"))
            throw new InvalidOperationException("dead queued mover emitted movement or lost defeated mode");
        simulation.Step();
        AssertNoLivingOverlaps(simulation.Units, "death cleanup next tick");
    }

    private static void SameTickDeathCellReuseAndLifecycleCleanup()
    {
        using (var simulation = new BattleSimulation(Config(
        [
            Spawn(Hero("chaser", health: 1000, damage: 0, range: 1), 0, 0, 2, "a-chaser"),
            Spawn(Unit("killer", health: 1000, damage: 1000, range: 3), 0, 0, 1, "b-killer"),
            Spawn(Unit("dead", health: 10, damage: 0, range: 1, moveTicks: 1000), 1, 2, 1, "z-dead")
        ])))
        {
            simulation.Step();
            var movement = Movement(simulation);
            var dead = simulation.Units.Single(unit => unit.RuntimeId == "z-dead");
            if (dead.Alive || dead.Mode != BattleUnitMode.Defeated ||
                simulation.Units.Any(unit => unit.ActionTargetRuntimeId == dead.RuntimeId) ||
                movement.ActiveGoalCount != 0 || movement.PendingRequestCount != 0 || movement.PlanningStateCount != 0)
                throw new InvalidOperationException("same-tick death retained dependent target facts, goals, requests, or non-terminal mode");
        }

        var deathSummon = Unit("same-tick-summon", health: 1000, damage: 0, moveTicks: 10);
        using (var reuse = new BattleSimulation(Config(
        [
            Spawn(Hero("player-anchor", health: 1000, damage: 0, range: 1, moveTicks: 1000), 0, 0, 5, "player"),
            Spawn(Unit("doomed-soldier", health: 10, damage: 0, range: 1, moveTicks: 1000), 0, 2, 1, "b-dead"),
            Spawn(Unit("enemy-killer", health: 1000, damage: 1000, range: 1, moveTicks: 1000), 1, 3, 1, "a-killer"),
            Spawn(Unit("enemy-mover", health: 1000, damage: 0, range: 1, moveTicks: 2), 1, 4, 2, "c-follower")
        ], rule: Rule(summonOnDeath: true), summons: new SummonProfiles(DeathSummon: deathSummon))))
        {
            reuse.Step();
            var dead = reuse.Units.Single(unit => unit.RuntimeId == "b-dead");
            var summoned = reuse.Units.SingleOrDefault(unit => unit.IsTemporary && unit.Alive);
            if (dead.Alive || summoned is null)
                throw new InvalidOperationException("same-tick death did not release the body and create its configured summon");
            AssertNoLivingOverlaps(reuse.Units, "same-tick death summon obstacle");
        }

        var disposable = new BattleSimulation(Config(
        [
            Spawn(Hero("disposable", health: 1000, damage: 0, range: 1), 0, 0, 2, "disposable"),
            Spawn(Unit("target", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 6, 2, "target")
        ]));
        disposable.Step();
        var disposableMovement = Movement(disposable);
        if (disposableMovement.ActiveGoalCount == 0)
            throw new InvalidOperationException("disposal cleanup probe did not establish planning state");
        disposable.Dispose();
        if (disposableMovement.ActiveGoalCount != 0 || disposableMovement.PendingRequestCount != 0 ||
            disposableMovement.PlanningStateCount != 0 ||
            disposable.Units.Any(unit => !string.IsNullOrEmpty(unit.ActionTargetRuntimeId) || !string.IsNullOrEmpty(unit.ActionTargetName)))
            throw new InvalidOperationException("battle disposal retained movement planning or target-facing facts");
    }

    private static void LifestealDeathIsTerminal(ContentRegistry registry)
    {
        var fragileLifestealer = Unit("fragile-lifestealer", health: 100, damage: 20, range: 1, attackTicks: 1) with
        {
            LifeSteal = .5f
        };
        var deathBurst = Unit("death-burst", health: 10, damage: 0, range: 1, moveTicks: 1000,
            behavior: new UnitBehaviorSnapshot(OnDeathDamage: 10));
        using (var simulation = new BattleSimulation(Config(
        [
            Spawn(fragileLifestealer, 0, 1, 1, "a-lifestealer"),
            Spawn(deathBurst, 1, 2, 1, "b-death-burst"),
            Spawn(Unit("follower", health: 1000, damage: 0, range: 1), 0, 0, 1, "c-follower"),
            Spawn(Hero("player-anchor", health: 1000, damage: 0, range: 1, moveTicks: 1000), 0, 0, 5, "p-anchor"),
            Spawn(Unit("enemy-anchor", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 9, 1, "z-anchor")
        ])))
        {
            var attacker = simulation.Units.Single(unit => unit.RuntimeId == "a-lifestealer");
            attacker.Health = 5;
            simulation.Step();
            var events = simulation.DrainEvents();
            var movement = Movement(simulation);
            var follower = simulation.Units.Single(unit => unit.RuntimeId == "c-follower");
            if (attacker.Health != 0 || attacker.Alive || attacker.Mode != BattleUnitMode.Defeated ||
                !string.IsNullOrEmpty(attacker.ActionTargetRuntimeId) || movement.HasPlanningState(attacker.RuntimeId))
                throw new InvalidOperationException($"on-death damage lifesteal resurrected or retained planning for its attacker: " +
                                                    $"health={attacker.Health}, mode={attacker.Mode}, target={attacker.ActionTargetRuntimeId}");
            if (follower.Cell != attacker.Cell || !events.Any(battleEvent => battleEvent.Type == "move" &&
                    battleEvent.SourceRuntimeId == follower.RuntimeId && battleEvent.Cell == attacker.Cell))
                throw new InvalidOperationException("lifesteal attacker's defeated cell was not reusable in the same tick");

            simulation.Step();
            var nextEvents = simulation.DrainEvents();
            if (nextEvents.Any(battleEvent => battleEvent.SourceRuntimeId == attacker.RuntimeId) ||
                movement.HasPlanningState(attacker.RuntimeId) ||
                simulation.Units.Where(unit => unit.Alive).Select(unit => unit.Cell).Distinct().Count() != simulation.Units.Count(unit => unit.Alive))
                throw new InvalidOperationException("defeated lifesteal attacker acted, planned, or occupied a live cell on the following tick");
        }

        using (var ordinary = new BattleSimulation(Config(
        [
            Spawn(Unit("living-lifestealer", health: 100, damage: 20, range: 1, attackTicks: 1) with { LifeSteal = .5f },
                0, 1, 1, "living-lifestealer"),
            Spawn(Unit("living-target", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 2, 1, "living-target")
        ])))
        {
            var attacker = ordinary.Units.Single(unit => unit.RuntimeId == "living-lifestealer");
            ordinary.Units.Single(unit => unit.RuntimeId == "living-target").AttackCooldown = 999;
            attacker.Health = 20;
            ordinary.Step();
            Near(attacker.Health, 30, .01f, "living attacker lost ordinary lifesteal");
        }

        var realLifestealer = BattleSetupFactory.Snapshot(
            registry.Catalog.Soldiers.Single(entry => entry.StableId == "soldier_blood_baroness"));
        var realDeathBurst = BattleSetupFactory.Snapshot(
            registry.Catalog.Soldiers.Single(entry => entry.StableId == "soldier_abyss_crawler"));
        if (realLifestealer.LifeSteal <= 0 || realDeathBurst.Behavior.OnDeathDamage <= 0)
            throw new InvalidOperationException("real lifesteal/death-burst content no longer establishes the terminal-death regression");
        using (var actualContent = new BattleSimulation(Config(
        [
            Spawn(realLifestealer, 0, 1, 1, "a-real-lifestealer"),
            Spawn(realDeathBurst, 1, 2, 1, "b-real-death-burst"),
            Spawn(Hero("real-player-anchor", health: 1000, damage: 0, range: 1, moveTicks: 1000), 0, 0, 5, "p-real-anchor"),
            Spawn(Unit("real-enemy-anchor", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 9, 5, "z-real-anchor")
        ])))
        {
            var attacker = actualContent.Units.Single(unit => unit.RuntimeId == "a-real-lifestealer");
            var target = actualContent.Units.Single(unit => unit.RuntimeId == "b-real-death-burst");
            attacker.Health = realDeathBurst.Behavior.OnDeathDamage / 2f;
            target.Health = 1;
            actualContent.Step();
            if (attacker.Health != 0 || attacker.Alive || attacker.Mode != BattleUnitMode.Defeated ||
                target.Alive || Movement(actualContent).HasPlanningState(attacker.RuntimeId))
                throw new InvalidOperationException("soldier_blood_baroness was revived after killing soldier_abyss_crawler");
        }

        using (var floorHealing = new BattleSimulation(Config(
        [
            Spawn(Hero("floor-anchor", health: 1000, damage: 0, range: 1, moveTicks: 1000), 0, 0, 5, "floor-anchor"),
            Spawn(Unit("floor-victim", health: 10, damage: 0, range: 1), 0, 1, 1, "floor-victim"),
            Spawn(Unit("floor-enemy", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 9, 5, "floor-enemy")
        ], floor: new LethalThenHealRule("floor-victim", 1000, 1000))))
        {
            floorHealing.Step();
            var victim = floorHealing.Units.Single(unit => unit.RuntimeId == "floor-victim");
            if (victim.Health != 0 || victim.Alive || victim.Mode != BattleUnitMode.Defeated)
                throw new InvalidOperationException("BattleRuleContext.Heal revived a unit killed earlier in the same floor-rule tick");
        }

        using var deadCommandBattle = new BattleSimulation(Config(
        [
            Spawn(Hero("dead-blood-rush"), 0, 0, 2, "dead-blood-rush"),
            Spawn(Unit("dead-command-enemy", health: 1000, damage: 0, moveTicks: 1000), 1, 9, 2, "dead-command-enemy")
        ], tacticalCommand: LoadTacticalCommand("TacticalBloodRush")));
        var deadHero = deadCommandBattle.Units.Single(unit => unit.RuntimeId == "dead-blood-rush");
        deadHero.Health = 0;
        deadHero.Mode = BattleUnitMode.Defeated;
        var pointsBefore = deadCommandBattle.TacticalPoints;
        if (deadCommandBattle.TryUseTacticalCommand(0).Succeeded || deadCommandBattle.TacticalPoints != pointsBefore ||
            deadHero.Health != 0 || deadHero.Alive)
            throw new InvalidOperationException("Blood Rush revived a defeated hero or bypassed the authoritative typed command gate");
    }

    private static void ActualTimeArbiterEngagement(ContentRegistry registry)
    {
        var heroEntry = registry.Catalog.Heroes.Single(entry => entry.StableId == "hero_hour_arbiter");
        var enemyEntry = registry.Catalog.Enemies.Single(entry => entry.StableId == "enemy_crossbow");
        var heroRoot = heroEntry.Scene.Instantiate<UnitContentRoot>();
        try
        {
            var hero = BattleSetupFactory.Snapshot(heroEntry);
            var enemy = BattleSetupFactory.Snapshot(enemyEntry) with { Damage = 0, MoveTicks = 1000 };
            var rule = BattleSetupFactory.Snapshot(heroRoot.HeroRule!);
            using var simulation = new BattleSimulation(Config(
            [
                Spawn(hero, 0, 0, 2, "time-arbiter"),
                Spawn(enemy, 1, 9, 2, "longer-range-enemy")
            ], rule: rule));
            simulation.Units.Single(unit => unit.RuntimeId == "longer-range-enemy").MoveCooldown = 999;
            var participated = false;
            for (var tick = 0; tick < 24 && simulation.Outcome == BattleOutcome.Running; tick++)
            {
                simulation.Step();
                participated |= simulation.DrainEvents().Any(battleEvent =>
                    battleEvent.SourceRuntimeId == "time-arbiter" && battleEvent.Type is ("move" or "attack"));
            }
            var state = simulation.Units.Single(unit => unit.RuntimeId == "time-arbiter");
            if (!participated || state.Cell == new Vector2I(0, 2) ||
                state.Mode == BattleUnitMode.Waiting && string.IsNullOrWhiteSpace(state.ActionTargetRuntimeId))
                throw new InvalidOperationException("actual Time Arbiter content stood idle against a longer-range enemy");
        }
        finally { heroRoot.Free(); }
    }

    private static DeterministicContinuousMovementService Movement(BattleSimulation simulation) =>
        (DeterministicContinuousMovementService)(typeof(BattleSimulation)
            .GetField("_movement", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(simulation)
            ?? throw new InvalidOperationException("battle movement service unavailable"));

    private static void SelectedUnitActionStateText()
    {
        var state = State(Unit("inspected"), "inspected");
        state.ActionTargetName = "目标";
        var expected = new Dictionary<BattleUnitMode, string>
        {
            [BattleUnitMode.Seeking] = "正在接敌",
            [BattleUnitMode.Moving] = "正在移动",
            [BattleUnitMode.Waiting] = "等待可用路线",
            [BattleUnitMode.Attacking] = "正在攻击",
            [BattleUnitMode.Casting] = "正在治疗",
            [BattleUnitMode.Disabled] = "被控制",
            [BattleUnitMode.Defeated] = "已被击败"
        };
        foreach (var pair in expected)
        {
            state.Mode = pair.Key;
            if (!SelectedUnitPanel.DescribeAction(state).Contains(pair.Value, StringComparison.Ordinal))
                throw new InvalidOperationException($"selected-unit action text omitted {pair.Key}");
        }
        state.Mode = BattleUnitMode.Recovering;
        state.AttackCooldown = 7;
        state.LastActionKind = BattleActionKind.Attack;
        var attackCooldown = SelectedUnitPanel.DescribeAction(state);
        state.LastActionKind = BattleActionKind.Heal;
        var healCooldown = SelectedUnitPanel.DescribeAction(state);
        if (!attackCooldown.Contains("攻击冷却 0.7 秒", StringComparison.Ordinal) ||
            !healCooldown.Contains("治疗冷却 0.7 秒", StringComparison.Ordinal) ||
            attackCooldown.Contains("tick", StringComparison.OrdinalIgnoreCase) || healCooldown.Contains("tick", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("selected-unit cooldown text did not distinguish attack/heal in player-facing seconds");
    }

    private static void TacticalCommandEconomy()
    {
        var mercenary = Unit("mercenary", health: 50);
        var paid = LoadTacticalCommand("TacticalPaidReinforcement");
        var summonProfiles = TacticalSummons(("soldier_aegis_guard", mercenary));
            using var funded = new BattleSimulation(Config(
            [
                Spawn(Hero("merchant"), 0, 0, 0, "merchant"),
                Spawn(Unit("enemy", health: 1000), 1, 9, 5, "enemy")
            ], tacticalCommand: paid, tacticalSummons: summonProfiles, startingGold: 5));
            if (!funded.TryUseTacticalCommand(0).Succeeded || funded.GoldSpent != 5 || funded.TacticalPoints != 2 || funded.Units.Count(unit => unit.IsTemporary) != 1)
                throw new InvalidOperationException("funded merchant command contract");

            using var poor = new BattleSimulation(Config(
            [
                Spawn(Hero("merchant"), 0, 0, 0, "merchant"),
                Spawn(Unit("enemy", health: 1000), 1, 9, 5, "enemy")
            ], tacticalCommand: paid, tacticalSummons: summonProfiles, startingGold: 4));
            if (poor.TryUseTacticalCommand(0).Succeeded || poor.GoldSpent != 0 || poor.TacticalPoints != 3 || poor.Units.Any(unit => unit.IsTemporary))
                throw new InvalidOperationException("insufficient merchant command consumed resources");

            using var missingSummon = new BattleSimulation(Config(
            [
                Spawn(Hero("merchant"), 0, 0, 0, "merchant"),
                Spawn(Unit("enemy", health: 1000), 1, 9, 5, "enemy")
            ], tacticalCommand: paid, startingGold: 10));
            if (missingSummon.TryUseTacticalCommand(0).Succeeded || missingSummon.GoldSpent != 0 || missingSummon.TacticalPoints != 3)
                throw new InvalidOperationException("missing summon consumed transactional command resources");

            using var points = new BattleSimulation(Config(BasicSpawns(), tacticalCommand: LoadTacticalCommand("TacticalRally")));
            if (!points.TryUseTacticalCommand(0).Succeeded || points.TacticalPoints != 2 ||
                !points.TryUseTacticalCommand(0).Succeeded || points.TacticalPoints != 1 ||
                !points.TryUseTacticalCommand(0).Succeeded || points.TacticalPoints != 0 ||
                points.TryUseTacticalCommand(0).Succeeded || points.TacticalPoints != 0)
                throw new InvalidOperationException("three-point tactical-command lifecycle");
            using var nextBattle = new BattleSimulation(Config(BasicSpawns(), tacticalCommand: LoadTacticalCommand("TacticalRally")));
            if (nextBattle.TacticalPoints != 3 || nextBattle.MaximumTacticalPoints != 3)
                throw new InvalidOperationException("new battle did not restore tactical points");
    }

    private static void ReadabilityAndIndependentTactics(ContentRegistry registry)
    {
        var brood = (UnitDefinition)registry.Catalog.Heroes.Single(entry => entry.StableId == "hero_brood_matriarch").Definition;
        var bone = (UnitDefinition)registry.Catalog.Heroes.Single(entry => entry.StableId == "hero_bone_regent").Definition;
        if (UnitRangeClassifier.Classify(2.2f) != UnitReachClass.Near ||
            UnitRangeClassifier.Classify(2.3f) != UnitReachClass.Near ||
            UnitRangeClassifier.Classify(3.5f) != UnitReachClass.Ranged ||
            UnitRangeClassifier.Classify(brood.AttackRange) != UnitReachClass.Near ||
            UnitRangeClassifier.Classify(bone.AttackRange) != UnitReachClass.Ranged)
            throw new InvalidOperationException("centralized hero reach classification");
        foreach (var entry in registry.Catalog.Heroes)
        {
            var root = entry.Scene.Instantiate<UnitContentRoot>();
            try
            {
                if (root.HeroRule is null || root.GetNodeOrNull<Node>("HeroCommand") is not null)
                    throw new InvalidOperationException(entry.StableId + " still owns a tactical command or lacks its hero rule");
            }
            finally { root.Free(); }
        }
        if (registry.Graph.TacticalCommands.Length != 8 ||
            registry.Graph.TacticalCommands.Any(command => command.TacticalPointCost != 1))
            throw new InvalidOperationException("independent tactical-command publication or authored point costs changed");
    }

    private async Task AnimationLifecycleAsync(ContentRegistry registry)
    {
        var entry = registry.Catalog.Heroes.Single(hero => hero.StableId == "hero_banner_marshal");
        var unit = entry.Scene.Instantiate<UnitContentRoot>();
        AddChild(unit);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        try
        {
            unit.Bind("animation-probe", 0, 100, 100);
            var animation = unit.GetNode<TowerAutobattler.Components.UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
            unit.ApplyPresentation("attack", Vector2.Zero, 100, 100);
            unit.ApplyPresentation("idle", Vector2.Zero, 100, 100);
            if (animation.ActiveCue == "idle") throw new InvalidOperationException("same-frame idle overwrote attack one-shot");
            unit.ApplyPresentation("hit", Vector2.Zero, 100, 100);
            if (animation.ActiveLogicalCue != "attack" || animation.PendingCue != "hit")
                throw new InvalidOperationException("bounded presentation queue did not preserve attack before hit");
            animation._Process(animation.ActivePlaybackSeconds + .01);
            if (animation.ActiveLogicalCue != "hit") throw new InvalidOperationException("queued hit did not follow the completed attack");

            animation.ResetPresentation();
            unit.ApplyPresentation("skill_cast", Vector2.Zero, 100, 100);
            if (animation.ActiveCue != "cast") throw new InvalidOperationException("general hero skill_cast did not resolve the real cast animation");
            animation.ResetPresentation();
            unit.ApplyPresentation("defeated", Vector2.Zero, 0, 100);
            unit.ApplyPresentation("defeated", Vector2.Zero, 0, 100);
            unit.ApplyPresentation("idle", Vector2.Zero, 0, 100);
            if (!animation.IsTerminal) throw new InvalidOperationException("defeat did not become an idempotent terminal presentation");
            await ToSignal(GetTree().CreateTimer(1.5), SceneTreeTimer.SignalName.Timeout);
            if (unit.Visible) throw new InvalidOperationException("defeated unit did not fade and hide");
        }
        finally
        {
            RemoveChild(unit);
            unit.Free();
        }

        var uniqueFrames = new HashSet<string>(StringComparer.Ordinal);
        var adjustedAttackClips = 0;
        var adjustedDefeatClips = 0;
        var entries = registry.Catalog.Heroes.Cast<CatalogEntry>()
            .Concat(registry.Catalog.Soldiers.Cast<CatalogEntry>())
            .Concat(registry.Catalog.Enemies.Cast<CatalogEntry>());
        foreach (var contentEntry in entries)
        {
            var contentUnit = contentEntry.Scene.Instantiate<UnitContentRoot>();
            AddChild(contentUnit);
            try
            {
                var component = contentUnit.GetNode<UnitAnimationComponent>("VisualRoot/UnitAnimationComponent");
                if (!uniqueFrames.Add(component.Frames.ResourcePath)) continue;
                contentUnit.Bind("frames-" + uniqueFrames.Count, 0, 100, 100);
                component.PlayCue("attack");
                ValidateCompleteClip(component, "attack", ref adjustedAttackClips);
                component.ResetPresentation();
                component.PlayCue("defeated");
                ValidateCompleteClip(component, "defeated", ref adjustedDefeatClips);
            }
            finally
            {
                RemoveChild(contentUnit);
                contentUnit.Free();
            }
        }
        if (uniqueFrames.Count != 35 || adjustedAttackClips < 34 || adjustedDefeatClips != 35)
            throw new InvalidOperationException($"animation package coverage: unique={uniqueFrames.Count}, adjusted attack={adjustedAttackClips}, defeated={adjustedDefeatClips}");
    }

    private static void PresentationCueArbitration()
    {
        BattleEvent[] mutualAttack =
        [
            new(1, "attack", "a", "b", 1, Vector2I.Zero, "attack"),
            new(1, "damage", "b", "a", 1, Vector2I.Zero, "hit"),
            new(1, "attack", "b", "a", 1, Vector2I.Zero, "attack"),
            new(1, "damage", "a", "b", 1, Vector2I.Zero, "hit")
        ];
        var selected = BattlePresentationCueArbiter.Select(mutualAttack);
        if (selected["a"] != "attack" || selected["b"] != "attack")
            throw new InvalidOperationException("same-tick mutual hit overrode an authored attack cue");
        BattleEvent[] lethal =
        [
            new(2, "attack", "a", "b", 1, Vector2I.Zero, "attack"),
            new(2, "defeated", "b", "a", 1, Vector2I.Zero, "defeated"),
            new(2, "tactical_command", "a", "", 0, Vector2I.Zero, "skill_cast")
        ];
        if (BattlePresentationCueArbiter.Select(lethal)["a"] != "defeated")
            throw new InvalidOperationException("terminal cue did not win presentation arbitration");
    }

    private static void ValidateCompleteClip(UnitAnimationComponent animation, string label, ref int adjustedCount)
    {
        var resolved = new StringName(animation.ActiveCue);
        if (animation.ActiveFrameCount != animation.Frames.GetFrameCount(resolved) || animation.ActiveFrameCount <= 0)
            throw new InvalidOperationException($"{label} omitted authored frames for {animation.Frames.ResourcePath}");
        Near(animation.ActivePlaybackSeconds * animation.PlaybackSpeedScale, animation.ActiveAuthoredSeconds, .002f,
            $"{label} playback did not fit the complete authored duration");
        if (animation.ActiveAuthoredSeconds > animation.ActivePlaybackSeconds + .001f) adjustedCount++;
    }

    private static void TacticalCommandSceneParameters(ContentRegistry registry)
    {
        var fixtures = new (string Scene, string Label, string[] DescriptionFacts)[]
        {
            ("TacticalRally", "rally", ["22", "0.2"]),
            ("TacticalRaiseDead", "raise dead", ["2", "75%", "80%"]),
            ("TacticalBeastRoar", "beast roar", ["12%"]),
            ("TacticalOverclock", "overclock", ["攻击与移动等待"]),
            ("TacticalBloodRush", "blood rush", ["28%", "8%"]),
            ("TacticalDuelFocus", "duel focus", ["35%"]),
            ("TacticalTimeStop", "time stop", ["1.8", "1/2"]),
            ("TacticalPaidReinforcement", "paid reinforcement", ["115%", "100%"])
        };
        var abilityIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var fixture in fixtures)
        {
            var command = LoadTacticalCommandRoot(fixture.Scene);
            try
            {
                ValidateTacticalCommand(command, registry, fixture.Label, fixture.DescriptionFacts);
                var compiled = command.Resolve(registry.Graph);
                if (!abilityIds.Add(compiled.Ability.StableId) || command.Definition.AbilityLoadout.Abilities.Count != 1)
                    throw new InvalidOperationException(fixture.Label + " scene does not expose one immutable primary Ability");
            }
            finally { command.Free(); }
        }
    }

    private static void BossContracts()
    {
        using var firstBoss = new BattleSimulation(Config(
        [
            Spawn(Hero("player", damage: 0, range: .5f, moveTicks: 1000), 0, 0, 0, "player"),
            Spawn(Unit("boss-one", isBoss: true, health: 1000, damage: 0, range: .5f, moveTicks: 1000,
                behavior: new UnitBehaviorSnapshot(PeriodicShieldTicks: 50, PeriodicShieldAmount: 55)), 1, 9, 5, "boss")
        ]));
        for (var i = 0; i < 49; i++) firstBoss.Step();
        Near(firstBoss.Units.Single(unit => unit.RuntimeId == "boss").Shield, 0, .001f, "first boss shield triggered early");
        firstBoss.Step();
        Near(firstBoss.Units.Single(unit => unit.RuntimeId == "boss").Shield, 55, .001f, "first boss periodic shield missing");

        var minion = Unit("minion", health: 20);
        using var secondBoss = new BattleSimulation(Config(
        [
            Spawn(Hero("front", health: 1000, range: 1), 0, 1, 1, "front"),
            Spawn(Unit("backline", health: 1000, range: 6), 0, 1, 4, "backline"),
            Spawn(Unit("boss-two", isBoss: true, health: 1000, damage: 10, range: 10,
                behavior: new UnitBehaviorSnapshot(PeriodicSummonTicks: 1, PeriodicSummonLimit: 2, PreferBacklineTargets: true)),
                1, 8, 2, "a-boss", behaviorSummon: minion)
        ]));
        secondBoss.Step();
        if (!secondBoss.DrainEvents().Any(e => e.Type == "attack" && e.SourceRuntimeId == "a-boss" && e.TargetRuntimeId == "backline"))
            throw new InvalidOperationException("second boss did not prefer the backline");
        for (var i = 0; i < 5; i++) { secondBoss.Step(); secondBoss.DrainEvents(); }
        var summoned = secondBoss.Units.Count(unit => unit.Team == 1 && unit.IsTemporary && unit.Alive);
        if (summoned is < 1 or > 2)
            throw new InvalidOperationException($"second boss summon count outside contract: {summoned}");

        var warded = WardDamage(new Vector2I(0, 3));
        var controlled = WardDamage(new Vector2I(5, 3));
        if (controlled < warded * 4.9f)
            throw new InvalidOperationException($"final boss ward was not weakened by beacon control: {warded} -> {controlled}");
    }

    private static void ProductionAbilityCompatibility(ContentRegistry registry)
    {
        if (!registry.TryGet("soldier_abyss_crawler", out var skeletonEntry) ||
            !registry.TryGet("soldier_aegis_guard", out var mercenaryEntry))
            throw new InvalidOperationException("production tactical summon dependencies are missing");
        var tacticalSummons = TacticalSummons(
            (skeletonEntry.StableId, BattleSetupFactory.Snapshot(skeletonEntry, registry)),
            (mercenaryEntry.StableId, BattleSetupFactory.Snapshot(mercenaryEntry, registry)));
        foreach (var command in registry.Graph.TacticalCommands.OrderBy(command => command.StableId, StringComparer.Ordinal))
        {
                var ability = command.Ability;
                if (ability.DisplayName != command.DisplayName || ability.Description != command.Description ||
                    ability.GoldCost != command.GoldCost)
                    throw new InvalidOperationException($"{command.StableId}: production tactical command projection diverged");
                var hero = Hero("compat-hero", health: 400, damage: 20, range: 2);
                var ally = Unit("compat-beast", health: 240, damage: 17, range: 3, attackTicks: 30,
                    moveTicks: 30, tags: ["soldier", "beast"]);
                var enemyA = Unit("compat-enemy-a", health: 1000, damage: 0, range: .5f, attackTicks: 1000, moveTicks: 1000);
                var enemyB = Unit("compat-enemy-b", health: 1000, damage: 0, range: .5f, attackTicks: 1000, moveTicks: 1000);
                var spawns = new[]
                {
                    Spawn(hero, 0, 0, 2, "hero"),
                    Spawn(ally, 0, 1, 2, "ally"),
                    Spawn(enemyA, 1, 9, 1, "enemy-a"),
                    Spawn(enemyB, 1, 9, 4, "enemy-b")
                };
                using var first = new BattleSimulation(Config(spawns, tacticalCommand: command,
                    tacticalSummons: tacticalSummons, startingGold: 20));
                using var second = new BattleSimulation(Config(spawns, tacticalCommand: command,
                    tacticalSummons: tacticalSummons, startingGold: 20));
                first.DrainEvents();
                second.DrainEvents();
                PrepareCommandCompatibilityState(first);
                PrepareCommandCompatibilityState(second);
                var before = first.Units.Select(AbilityCompatibilitySnapshot).ToArray();

                var firstUse = first.TryUseTacticalCommand(0);
                var secondUse = second.TryUseTacticalCommand(0);
                if (!firstUse.Succeeded || !secondUse.Succeeded)
                    throw new InvalidOperationException($"{command.StableId}: authored command did not execute deterministically: {firstUse.FailureReason} | {secondUse.FailureReason}");
                if (first.TacticalPoints != BattleTacticalCommandScope.MaximumTacticalPoints - command.TacticalPointCost ||
                    first.GoldSpent != command.GoldCost || first.SuccessfulTacticalCommandUses != 1 ||
                    before.SequenceEqual(first.Units.Select(AbilityCompatibilitySnapshot)))
                    throw new InvalidOperationException($"{command.StableId}: authored command did not commit its declared resources and world effect");
                AssertAbilityCompatibility(first, second, command.StableId);
                if (!first.EffectTrace.SequenceEqual(second.EffectTrace))
                    throw new InvalidOperationException($"{command.StableId}: authored command effect trace is not deterministic");
        }

        ProductionBorealAbilityCompatibility(registry);
        ProductionShadowAbilityCompatibility(registry);
    }

    private static void PrepareCommandCompatibilityState(BattleSimulation simulation)
    {
        foreach (var unit in simulation.Units)
        {
            unit.AttackCooldown = 12;
            unit.MoveCooldown = 9;
        }
        var hero = simulation.Units.Single(unit => unit.RuntimeId == "hero");
        hero.Health = hero.MaxHealth * .4f;
    }

    private static void ProductionBorealAbilityCompatibility(ContentRegistry registry)
    {
        if (!registry.TryGet("enemy_boreal_boss", out var entry))
            throw new InvalidOperationException("boreal boss entry missing");
        var production = BattleSetupFactory.Snapshot(entry, registry) with { Damage = 0, Range = .5f, MoveTicks = 1000 };
        var legacy = production with
        {
            AbilityLoadout = null,
            Behavior = production.Behavior with { PeriodicShieldTicks = 50, PeriodicShieldAmount = 55 }
        };
        using var typedBattle = new BattleSimulation(Config(
        [
            Spawn(Hero("boreal-target", health: 100000, damage: 0, range: .5f, moveTicks: 1000), 0, 0, 2, "player"),
            Spawn(production, 1, 9, 2, "boss")
        ]));
        using var legacyBattle = new BattleSimulation(Config(
        [
            Spawn(Hero("boreal-target", health: 100000, damage: 0, range: .5f, moveTicks: 1000), 0, 0, 2, "player"),
            Spawn(legacy, 1, 9, 2, "boss")
        ]));
        typedBattle.DrainEvents();
        legacyBattle.DrainEvents();
        for (var tick = 1; tick <= 50; tick++)
        {
            typedBattle.Step();
            legacyBattle.Step();
            var typedEvents = typedBattle.DrainEvents();
            var legacyEvents = legacyBattle.DrainEvents();
            if (!typedEvents.SequenceEqual(legacyEvents))
                throw new InvalidOperationException($"boreal ability event order changed at tick {tick}");
            if (tick == 49 && typedBattle.Units.Single(unit => unit.RuntimeId == "boss").Shield != 0)
                throw new InvalidOperationException("boreal ability triggered before tick 50");
        }
        AssertAbilityCompatibility(typedBattle, legacyBattle, "enemy_boreal_boss");
        Near(typedBattle.Units.Single(unit => unit.RuntimeId == "boss").Shield, 55, .001f,
            "boreal typed shield value");
    }

    private static void ProductionShadowAbilityCompatibility(ContentRegistry registry)
    {
        if (!registry.TryGet("enemy_shadow_boss", out var entry) || !registry.TryGet("enemy_carrion", out var summonEntry))
            throw new InvalidOperationException("shadow boss dependency entry missing");
        var summon = BattleSetupFactory.Snapshot(summonEntry, registry);
        var production = BattleSetupFactory.Snapshot(entry, registry) with { Damage = 0, Range = .5f, MoveTicks = 1000 };
        var legacy = production with
        {
            AbilityLoadout = null,
            Behavior = production.Behavior with { PeriodicSummonTicks = 60, PeriodicSummonLimit = 4 }
        };
        using var typedBattle = new BattleSimulation(Config(
        [
            Spawn(Hero("shadow-target", health: 100000, damage: 0, range: .5f, moveTicks: 1000), 0, 0, 2, "player"),
            Spawn(production, 1, 9, 2, "boss", summon)
        ]));
        using var legacyBattle = new BattleSimulation(Config(
        [
            Spawn(Hero("shadow-target", health: 100000, damage: 0, range: .5f, moveTicks: 1000), 0, 0, 2, "player"),
            Spawn(legacy, 1, 9, 2, "boss", summon)
        ]));
        typedBattle.DrainEvents();
        legacyBattle.DrainEvents();
        for (var tick = 1; tick <= 300; tick++)
        {
            typedBattle.Step();
            legacyBattle.Step();
            var typedEvents = typedBattle.DrainEvents();
            var legacyEvents = legacyBattle.DrainEvents();
            if (!typedEvents.SequenceEqual(legacyEvents))
                throw new InvalidOperationException($"shadow ability event order changed at tick {tick}");
            if (tick == 59 && typedBattle.Units.Any(unit => unit.IsTemporary))
                throw new InvalidOperationException("shadow ability triggered before tick 60");
        }
        AssertAbilityCompatibility(typedBattle, legacyBattle, "enemy_shadow_boss");
        if (typedBattle.Units.Count(unit => unit.Team == 1 && unit.IsTemporary && unit.Alive) != 4)
            throw new InvalidOperationException("shadow ability did not preserve interval or living summon limit");
    }

    private static void AssertAbilityCompatibility(BattleSimulation typed, BattleSimulation legacy, string label)
    {
        if (typed.TacticalPoints != legacy.TacticalPoints || typed.GoldSpent != legacy.GoldSpent ||
            typed.SuccessfulTacticalCommandUses != legacy.SuccessfulTacticalCommandUses ||
            typed.CreateResult().Digest != legacy.CreateResult().Digest)
            throw new InvalidOperationException($"{label}: ability resource counters or digest diverged between equivalent executions");
        var typedEvents = typed.DrainEvents();
        var legacyEvents = legacy.DrainEvents();
        if (!typedEvents.SequenceEqual(legacyEvents))
            throw new InvalidOperationException($"{label}: ability resource event order diverged between equivalent executions");
        var typedUnits = typed.Units.Select(AbilityCompatibilitySnapshot).ToArray();
        var legacyUnits = legacy.Units.Select(AbilityCompatibilitySnapshot).ToArray();
        if (!typedUnits.SequenceEqual(legacyUnits))
            throw new InvalidOperationException($"{label}: ability execution changed complete gameplay unit state nondeterministically");
    }

    private static object AbilityCompatibilitySnapshot(BattleUnitState unit) => new
    {
        unit.RuntimeId,
        unit.SourceInstanceId,
        unit.Definition.ContentId,
        unit.Team,
        unit.Cell,
        unit.Health,
        unit.MaxHealth,
        unit.Damage,
        unit.LifeSteal,
        unit.Shield,
        unit.AttackCooldown,
        unit.MoveCooldown,
        unit.DisabledTicks,
        unit.WaitingTicks,
        unit.IsTemporary,
        unit.Mode,
        unit.LastActionKind,
        unit.ActionTargetRuntimeId,
        unit.ActionTargetName
    };

    private static float WardDamage(Vector2I heroCell)
    {
        using var simulation = new BattleSimulation(Config(
        [
            new BattleSpawn(Hero("player", damage: 100, range: 10), 0, heroCell, "a-player"),
            Spawn(Unit("final-boss", isBoss: true, health: 1000, damage: 0, range: .5f, moveTicks: 1000), 1, 9, 3, "boss")
        ], floor: new BossWardRuntime("ward", "结界", "test", 1000, 0)));
        simulation.Step();
        return AttackValue(simulation, "a-player");
    }

    private static void FloorLifecycleExactlyOnce()
    {
        var throwingStart = new TrackingFloorRule { ThrowOnStart = true };
        ExpectThrows(() => _ = new BattleSimulation(Config(BasicSpawns(), floor: throwingStart)), "floor start failure");
        if (throwingStart.Started != 1 || throwingStart.Ended != 1)
            throw new InvalidOperationException("failed floor start was not ended exactly once");

        var abortedRule = new TrackingFloorRule();
        var aborted = new BattleSimulation(Config(BasicSpawns(), floor: abortedRule));
        aborted.Abort();
        aborted.Abort();
        aborted.Dispose();
        if (abortedRule.Started != 1 || abortedRule.Ended != 1 || aborted.Outcome != BattleOutcome.Timeout)
            throw new InvalidOperationException("repeated abort ended floor more than once");

        var completedRule = new TrackingFloorRule();
        var completed = new BattleSimulation(Config(
        [
            Spawn(Hero("player", damage: 100, range: 5), 0, 0, 0, "a-player"),
            Spawn(Unit("enemy", health: 5), 1, 2, 0, "enemy")
        ], floor: completedRule));
        completed.RunToEnd();
        completed.Abort();
        completed.Dispose();
        if (completedRule.Started != 1 || completedRule.Ended != 1)
            throw new InvalidOperationException("normal completion plus abort ended floor more than once");
    }

    private static void EffectKernelCompatibilityAndLifecycle()
    {
        static BattleConfig CompatibilityConfig() => Config(
        [
            new BattleSpawn(Hero("compat-player", health: 200, damage: 100, range: 3), 0,
                new Vector2I(0, 2), "a-compat-player", .5f),
            Spawn(Unit("compat-enemy", health: 60, damage: 0, range: 1, moveTicks: 1000), 1,
                2, 2, "z-compat-enemy")
        ], floor: new DamageAndHealEveryTickRule(5, 2),
            tacticalCommand: LoadTacticalCommand("TacticalBloodRush"));

        using var first = new BattleSimulation(CompatibilityConfig());
        using var second = new BattleSimulation(CompatibilityConfig());
        if (!first.TryUseTacticalCommand(0).Succeeded || !second.TryUseTacticalCommand(0).Succeeded)
            throw new InvalidOperationException("typed effect determinism command setup failed");
        var firstResult = first.RunToEnd();
        var secondResult = second.RunToEnd();
        if (firstResult.Outcome != secondResult.Outcome || firstResult.Ticks != secondResult.Ticks ||
            firstResult.Digest != secondResult.Digest || firstResult.GoldSpent != secondResult.GoldSpent ||
            firstResult.SuccessfulTacticalCommandUses != secondResult.SuccessfulTacticalCommandUses ||
            !firstResult.Units.SequenceEqual(secondResult.Units) || !first.EffectTrace.SequenceEqual(second.EffectTrace))
            throw new InvalidOperationException("typed effect execution changed outcome, ticks, digest, trace, or unit snapshots between equivalent runs");
        var compatibilityBindings = first.EffectTrace.Select(entry => entry.BindingId).ToHashSet(StringComparer.Ordinal);
        if (!compatibilityBindings.SetEquals(["ability_blood_rush_heal", "compat_floor_damage", "compat_floor_heal"]))
            throw new InvalidOperationException("representative floor/command mechanics did not pass through the typed effect boundary");
        ExpectValidTransition(first, BattleScopeCompletionReason.PlayerVictory, "typed deterministic victory A");
        ExpectValidTransition(second, BattleScopeCompletionReason.PlayerVictory, "typed deterministic victory B");

        var defeat = new BattleSimulation(Config(
        [
            Spawn(Hero("defeated", health: 10, damage: 0, range: 3), 0, 0, 2, "z-defeated"),
            Spawn(Unit("killer", damage: 100, range: 3), 1, 2, 2, "a-killer")
        ]));
        defeat.Step();
        ExpectValidTransition(defeat, BattleScopeCompletionReason.PlayerDefeat, "natural defeat");
        defeat.Dispose();

        var timeout = new BattleSimulation(Config(
        [
            Spawn(Hero("timeout-player", health: 10000, damage: 0, range: 10), 0, 0, 2, "timeout-player"),
            Spawn(Unit("timeout-enemy", health: 10000, damage: 0, range: 10, attackTicks: int.MaxValue), 1,
                2, 2, "timeout-enemy")
        ]));
        timeout.RunToEnd();
        ExpectValidTransition(timeout, BattleScopeCompletionReason.Timeout, "natural timeout");
        timeout.Dispose();

        var aborted = new BattleSimulation(Config(BasicSpawns()));
        aborted.Abort();
        ExpectValidTransition(aborted, BattleScopeCompletionReason.Abort, "explicit abort");
        aborted.Dispose();

        var replaced = new BattleSimulation(Config(BasicSpawns()));
        replaced.Replace();
        ExpectValidTransition(replaced, BattleScopeCompletionReason.Replacement, "battle replacement");
        replaced.Dispose();

        var disposed = new BattleSimulation(Config(BasicSpawns()));
        disposed.Dispose();
        ExpectValidTransition(disposed, BattleScopeCompletionReason.Disposal, "direct disposal");

        var throwingTick = new BattleSimulation(Config(BasicSpawns(), floor: new ThrowingTickRule()));
        ExpectThrows(() => throwingTick.Step(), "effect-scope tick exception");
        ExpectValidTransition(throwingTick, BattleScopeCompletionReason.Exception, "tick exception");
        throwingTick.Dispose();

        var throwingEndRule = new TrackingFloorRule { ThrowOnEnd = true };
        var throwingEnd = new BattleSimulation(Config(BasicSpawns(), floor: throwingEndRule));
        ExpectThrows(() => throwingEnd.Dispose(), "effect-scope end callback exception");
        ExpectValidTransition(throwingEnd, BattleScopeCompletionReason.Exception, "end callback exception");
        if (throwingEndRule.Ended != 1)
            throw new InvalidOperationException("throwing floor end callback was invoked more than once");
    }

    private static void ExpectValidTransition(
        BattleSimulation simulation,
        BattleScopeCompletionReason reason,
        string label)
    {
        var transition = simulation.EffectTransition ??
            throw new InvalidOperationException(label + " omitted effect transition");
        if (transition.Reason != reason || !transition.Validate().IsValid ||
            transition.RemainingSubscriptions != 0 || transition.RemainingInvocations != 0 ||
            transition.RemainingRuntimeInstances != 0)
            throw new InvalidOperationException(label + " retained effect-scope state");
    }

    private static void RunConversionAndSettings(ContentRegistry registry)
    {
        var save = new SaveService("tests/gameplay-contract");
        save.DeleteActiveRun();
        save.SaveMeta(new MetaProgressDto { UnlockedHeroIds = registry.Catalog.Heroes.Select(entry => entry.StableId).ToList() });
        save.SaveSettings(new SettingsDto { MasterVolume = .37f, DefaultBattleSpeed = 4f });
        var regions = Regions();
        var app = new RunApplication(registry, save, TestProjectFixture.Load(registry));
        var master = AudioServer.GetBusIndex("Master");
        if (master >= 0) Near(AudioServer.GetBusVolumeDb(master), Mathf.LinearToDb(.37f), .01f, "saved master volume not applied at startup");

        AssertConversion(app, "hero_crimson_count", 4);
        AssertConversion(app, "hero_edge_ascetic", 6);
        AssertConversion(app, "hero_banner_marshal", 0);
        save.DeleteActiveRun();
    }

    private async Task DefaultBattleSpeedAsync(ContentRegistry registry)
    {
        var scene = GD.Load<PackedScene>("res://scenes/ui/BattleScreen.tscn");
        var screen = scene.Instantiate<BattleScreenController>();
        AddChild(screen);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        try
        {
            foreach (var (displaySpeed, expectedTicks) in new[] { (1f, 8), (2f, 16), (4f, 32) })
            {
                screen.StartBattle(registry, PaceConfig(), $"速度测试 x{displaySpeed:0}", displaySpeed);
                screen.SetProcess(false);
                for (var frame = 0; frame < 8; frame++) screen._Process(.125);
                var simulation = ScreenSimulation(screen);
                if (screen.SpeedScale != displaySpeed || screen.GetNode<Button>("%SpeedButton").Text != $"速度 x{displaySpeed:0}")
                    throw new InvalidOperationException("default battle speed was not applied to battle HUD");
                if (simulation.TickIndex != expectedTicks)
                    throw new InvalidOperationException($"display x{displaySpeed:0} advanced {simulation.TickIndex} ticks in one real second; expected {expectedTicks}");
            }

            var deterministicResults = new List<BattleResult>();
            foreach (var displaySpeed in new[] { 1f, 2f, 4f })
            {
                BattleResult? captured = null;
                void Capture(BattleResult result) => captured = result;
                screen.Finished += Capture;
                screen.StartBattle(registry, Config(BasicSpawns()), $"确定性 x{displaySpeed:0}", displaySpeed);
                screen.SetProcess(false);
                for (var frame = 0; captured is null && frame < 10000; frame++) screen._Process(.05);
                screen.Finished -= Capture;
                deterministicResults.Add(captured ?? throw new InvalidOperationException("display-speed deterministic battle did not resolve"));
            }
            if (deterministicResults.Skip(1).Any(result =>
                    result.Outcome != deterministicResults[0].Outcome || result.Ticks != deterministicResults[0].Ticks ||
                    result.Digest != deterministicResults[0].Digest || !result.Units.SequenceEqual(deterministicResults[0].Units)))
                throw new InvalidOperationException("display speed changed terminal outcome, tick, digest, or report statistics");

            var terminalConfig = Config(
            [
                Spawn(Hero("terminal-hero", health: 1000, damage: 100, range: 3), 0, 0, 2, "terminal-hero"),
                Spawn(Unit("terminal-enemy", health: 1, damage: 0, range: 1, moveTicks: 1000), 1, 2, 2, "terminal-enemy")
            ]);
            var resultSignals = 0;
            var transitionSignals = 0;
            screen.Finished += _ => resultSignals++;
            screen.EndTransitionFinished += () => transitionSignals++;
            screen.StartBattle(registry, terminalConfig, "终局测试", 1f);
            screen.SetProcess(false);
            screen._Process(.13);
            var terminalSimulation = ScreenSimulation(screen);
            if (terminalSimulation.Outcome != BattleOutcome.PlayerVictory || terminalSimulation.TickIndex != 1)
                throw new InvalidOperationException("terminal fixture did not resolve on its first authoritative tick");
            screen._Process(5);
            if (terminalSimulation.TickIndex != 1 || resultSignals != 1)
                throw new InvalidOperationException("terminal controller stepped or reported resolution more than once");
            if (screen.GetNodeOrNull<ColorRect>("%EndFadeOverlay") is null ||
                !screen.GetNode<Button>("%PauseButton").Disabled || !screen.GetNode<Button>("%SpeedButton").Disabled ||
                screen.IsProcessing())
                throw new InvalidOperationException("terminal presentation did not lock interaction and stop simulation processing");
            var overlay = screen.GetNode<ColorRect>("%EndFadeOverlay");
            var tween = (Tween)(typeof(BattleScreenController).GetField("_endTween", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(screen)
                ?? throw new InvalidOperationException("terminal presentation omitted its owned Tween"));
            tween.CustomStep(1.0);
            if (overlay.Color.A != 0 || transitionSignals != 0)
                throw new InvalidOperationException("terminal hold faded or routed before its real-time 1.1-second boundary");
            tween.CustomStep(.25);
            if (overlay.Color.A <= 0 || overlay.Color.A >= 1 || transitionSignals != 0)
                throw new InvalidOperationException("terminal fade did not expose an intermediate opaque-black transition sample");
            tween.CustomStep(.4);
            if (Math.Abs(overlay.Color.A - 1) > .001f || transitionSignals != 1)
                throw new InvalidOperationException("terminal fade did not reach opaque black and report once");

            screen.StartBattle(registry, terminalConfig, "终局快进测试", 1f);
            screen.SetProcess(false);
            screen._Process(.13);
            GetViewport().PushInput(new InputEventAction { Action = "ui_accept", Pressed = true }, true);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GetViewport().PushInput(new InputEventAction { Action = "ui_accept", Pressed = true }, true);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (resultSignals != 2 || transitionSignals != 2 || Math.Abs(overlay.Color.A - 1) > .001f)
                throw new InvalidOperationException("confirm fast-forward skipped, duplicated, or failed to finish the report transition");
        }
        finally
        {
            RemoveChild(screen);
            screen.Free();
        }
    }

    private static void BattleResultSnapshotAndStatistics()
    {
        using var simulation = new BattleSimulation(Config(
        [
            Spawn(Hero("report-attacker", health: 1000, damage: 100, range: 3), 0, 0, 2, "report-attacker"),
            Spawn(Unit("report-target", health: 50, damage: 0, range: 1, moveTicks: 1000), 1, 2, 2, "report-target")
        ]));
        var liveTarget = simulation.Units.Single(unit => unit.RuntimeId == "report-target");
        liveTarget.Shield = 30;
        simulation.Step();
        var result = simulation.CreateResult();
        var rows = ((IEnumerable)(result.GetType().GetProperty("Units")?.GetValue(result)
            ?? throw new InvalidOperationException("battle result omitted unit snapshots"))).Cast<object>().ToArray();
        var attacker = rows.Single(row => ResultString(row, "RuntimeId") == "report-attacker");
        var target = rows.Single(row => ResultString(row, "RuntimeId") == "report-target");
        if (attacker is BattleUnitState || target is BattleUnitState)
            throw new InvalidOperationException("battle result retained live BattleUnitState references");
        Near(ResultNumber(attacker, "DamageDealt"), 80, .01f, "effective damage included overkill or omitted shield");
        Near(ResultNumber(target, "DamageTaken"), 80, .01f, "damage dealt/taken authority diverged");
        Near(ResultNumber(target, "ShieldAbsorbed"), 30, .01f, "shield absorption report fact");
        Near(ResultNumber(attacker, "Kills"), 1, .01f, "concrete lethal source kill credit");
        Near(ResultNumber(attacker, "JoinTick"), 0, .01f, "initial unit join tick");
        Near(ResultNumber(attacker, "AttackActions"), 1, .01f, "one attack action authority");
        Near(ResultNumber(target, "DefeatTick"), 1, .01f, "terminal defeat tick authority");
        liveTarget.Health = 999;
        Near(ResultNumber(target, "FinalHealth"), 0, .01f, "result snapshot changed after live battle mutation");

        using var healing = new BattleSimulation(Config(
        [
            Spawn(Unit("report-healer", isHero: true, health: 200, damage: 0, range: 3, heal: 80), 0, 0, 2, "report-healer"),
            new BattleSpawn(Unit("report-wounded", health: 100, damage: 0, range: 1, moveTicks: 1000), 0,
                new Vector2I(1, 2), "report-wounded", .5f),
            Spawn(Unit("report-enemy", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 9, 5, "report-enemy")
        ]));
        healing.Step();
        var healingRows = ((IEnumerable)healing.CreateResult().GetType().GetProperty("Units")!.GetValue(healing.CreateResult())!).Cast<object>().ToArray();
        var healer = healingRows.Single(row => ResultString(row, "RuntimeId") == "report-healer");
        Near(ResultNumber(healer, "HealingDone"), 50, .01f, "effective healing counted overheal or lost source credit");
        Near(ResultNumber(healer, "EffectiveHealingEvents"), 1, .01f, "positive effective heal event authority");

        using var commandHealing = new BattleSimulation(Config(
        [
            new BattleSpawn(Hero("report-blood-rush-full", health: 200, damage: 10), 0,
                new Vector2I(0, 2), "report-blood-rush-full", .5f),
            Spawn(Unit("report-blood-rush-enemy", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 9, 5,
                "report-blood-rush-enemy")
        ], tacticalCommand: LoadTacticalCommand("TacticalBloodRush")));
        var commandHero = commandHealing.Units.Single(unit => unit.RuntimeId == "report-blood-rush-full");
        commandHero.AttackCooldown = 7;
        var commandBefore = commandHealing.CreateResult();
        if (!commandHealing.TryUseTacticalCommand(0).Succeeded || commandHealing.TacticalPoints != 2 || commandHealing.GoldSpent != 0)
            throw new InvalidOperationException("blood-rush full-heal command changed transaction semantics");
        Near(commandHero.Health, 156, .01f, "blood-rush authored 28% healing changed final health");
        Near(commandHero.Damage, 10.8f, .01f, "blood-rush authored 8% damage multiplier changed behavior");
        if (commandHero.AttackCooldown != 0)
            throw new InvalidOperationException("blood-rush command stopped resetting attack cooldown");
        var commandAfter = commandHealing.CreateResult();
        if (commandAfter.SuccessfulTacticalCommandUses != 1)
            throw new InvalidOperationException("successful command use count did not increment at commit");
        Near(commandAfter.Units.Single(unit => unit.RuntimeId == "report-blood-rush-full").HealingDone, 56, .01f,
            "blood-rush authored effective healing was not attributed to the hero");
        using var commandDigestControl = new BattleSimulation(Config(
        [
            new BattleSpawn(Hero("report-blood-rush-full", health: 200, damage: 10), 0,
                new Vector2I(0, 2), "report-blood-rush-full", .5f),
            Spawn(Unit("report-blood-rush-enemy", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 9, 5,
                "report-blood-rush-enemy")
        ], tacticalCommand: LoadTacticalCommand("TacticalRally")));
        if (!commandDigestControl.TryUseTacticalCommand(0).Succeeded || commandAfter.Ticks != commandBefore.Ticks ||
            commandAfter.Digest != commandDigestControl.CreateResult().Digest)
            throw new InvalidOperationException("blood-rush healing statistics changed tick or command-event digest authority");

        using var commandOverheal = new BattleSimulation(Config(
        [
            new BattleSpawn(Hero("report-blood-rush-overheal", health: 200, damage: 10), 0,
                new Vector2I(0, 2), "report-blood-rush-overheal", .95f),
            Spawn(Unit("report-blood-rush-overheal-enemy", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 9, 5,
                "report-blood-rush-overheal-enemy")
        ], tacticalCommand: LoadTacticalCommand("TacticalBloodRush")));
        if (!commandOverheal.TryUseTacticalCommand(0).Succeeded)
            throw new InvalidOperationException("blood-rush overheal command unexpectedly failed");
        var overhealHero = commandOverheal.CreateResult().Units.Single(unit => unit.RuntimeId == "report-blood-rush-overheal");
        Near(overhealHero.FinalHealth, 200, .01f, "blood-rush overheal changed final-health cap");
        Near(overhealHero.HealingDone, 10, .01f, "blood-rush report counted overhealing instead of effective healing");
        if (overhealHero.EffectiveHealingEvents != 1)
            throw new InvalidOperationException("partially effective command heal did not count exactly one event");

        using var lifesteal = new BattleSimulation(Config(
        [
            new BattleSpawn(Unit("report-lifesteal", isHero: true, health: 200, damage: 100, range: 3, lifeSteal: .5f), 0,
                new Vector2I(0, 2), "report-lifesteal", .5f),
            Spawn(Unit("report-lifesteal-target", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 2, 2, "report-lifesteal-target")
        ]));
        lifesteal.Step();
        var lifestealRow = lifesteal.CreateResult().Units.Single(unit => unit.RuntimeId == "report-lifesteal");
        Near(lifestealRow.DamageDealt, 100, .01f, "lifesteal effective damage authority");
        Near(lifestealRow.HealingDone, 50, .01f, "lifesteal effective healing authority");

        using var area = new BattleSimulation(Config(
        [
            Spawn(Unit("report-area", isHero: true, health: 1000, damage: 100, range: 4, splash: 2,
                behavior: new UnitBehaviorSnapshot(PiercingLine: true)), 0, 0, 2, "report-area"),
            Spawn(Unit("report-primary", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 2, 2, "a-report-primary"),
            Spawn(Unit("report-behind", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 3, 2, "b-report-behind")
        ]));
        area.Step();
        var areaResult = area.CreateResult();
        Near(areaResult.Units.Single(unit => unit.RuntimeId == "report-area").DamageDealt, 180, .01f,
            "splash and pierce effective damage aggregation");
        if (areaResult.Units.Single(unit => unit.RuntimeId == "report-area").AttackActions != 1)
            throw new InvalidOperationException("splash and pierce multiplied one attack action");
        Near(areaResult.Units.Single(unit => unit.RuntimeId == "b-report-behind").DamageTaken, 80, .01f,
            "splash and pierce target aggregation");

        using var deathEffect = new BattleSimulation(Config(
        [
            Spawn(Unit("report-death-attacker", isHero: true, health: 50, damage: 100, range: 3), 0, 0, 2, "report-death-attacker"),
            Spawn(Unit("report-death-source", health: 10, damage: 0, range: 1, moveTicks: 1000,
                behavior: new UnitBehaviorSnapshot(OnDeathDamage: 100)), 1, 1, 2, "report-death-source")
        ]));
        deathEffect.Step();
        var deathResult = deathEffect.CreateResult();
        var defeatedSource = deathResult.Units.Single(unit => unit.RuntimeId == "report-death-source");
        Near(defeatedSource.DamageDealt, 50, .01f, "defeated on-death source damage credit");
        Near(defeatedSource.Kills, 1, .01f, "defeated on-death source kill credit");

        using var environment = new BattleSimulation(Config(
        [
            new BattleSpawn(Hero("report-floor-hero", health: 200), 0, new Vector2I(0, 2), "report-floor-hero", .5f),
            Spawn(Unit("report-floor-enemy", health: 1000, damage: 0, range: 1, moveTicks: 1000), 1, 9, 5, "report-floor-enemy")
        ], floor: new DamageAndHealEveryTickRule(10, 40)));
        environment.Step();
        var environmentResult = environment.CreateResult();
        var floorHero = environmentResult.Units.Single(unit => unit.RuntimeId == "report-floor-hero");
        Near(floorHero.DamageTaken, 10, .01f, "floor damage missing from target facts");
        Near(floorHero.HealingDone, 0, .01f, "environment healing was falsely credited to its target");
        Near(environmentResult.Units.Sum(unit => unit.DamageDealt), 0, .01f, "environment damage was falsely credited to a unit");

        var construct = Unit("report-construct", health: 30, damage: 3);
        using var summons = new BattleSimulation(Config(BasicSpawns(),
            rule: Rule() with { AddBattleConstruct = true },
            summons: new SummonProfiles(HeroConstruct: construct)));
        var summonRow = summons.CreateResult().Units.Single(unit => unit.IsTemporary);
        if (summonRow.ContentId != construct.ContentId || summonRow.DamageDealt != 0 || summonRow.HealingDone != 0 || summonRow.JoinTick != 0)
            throw new InvalidOperationException("temporary summon did not receive an independent zero-initialized statistics row");

        using var lateSummon = new BattleSimulation(Config(BasicSpawns(),
            tacticalCommand: LoadTacticalCommand("TacticalPaidReinforcement"),
            tacticalSummons: TacticalSummons(("soldier_aegis_guard", construct)),
            startingGold: 20));
        lateSummon.Step();
        if (!lateSummon.TryUseTacticalCommand(0).Succeeded)
            throw new InvalidOperationException("late summon command fixture failed");
        var lateRow = lateSummon.CreateResult().Units.Single(unit => unit.IsTemporary);
        if (lateRow.JoinTick != 1 || lateSummon.CreateResult().SuccessfulTacticalCommandUses != 1)
            throw new InvalidOperationException("late summon join tick or successful command count");
        lateSummon.TryUseTacticalCommand(0);
        lateSummon.TryUseTacticalCommand(0);
        if (lateSummon.TryUseTacticalCommand(0).Succeeded || lateSummon.CreateResult().SuccessfulTacticalCommandUses != 3)
            throw new InvalidOperationException("failed command changed successful command count");
    }

    private static void BattleReportDerivationContracts()
    {
        var units = new[]
        {
            new BattleUnitReportSnapshot("a", "a", "a", "甲", UnitRole.Fighter, 0, true, false, true,
                Vector2I.Zero, 100, 100, 0, 10, 100, 20, 0, 40, 1, 0, null, 2, 1),
            new BattleUnitReportSnapshot("b", "b", "b", "乙", UnitRole.Support, 0, false, true, false,
                Vector2I.One, 0, 100, 0, 10, 100, 80, 0, 40, 0, 5, 15, 1, 1),
            new BattleUnitReportSnapshot("z", "z", "z", "敌", UnitRole.Boss, 1, false, false, true,
                new Vector2I(2, 2), 200, 300, 0, 10, 50, 230, 0, 0, 0)
        }.ToImmutableArray();
        var result = new BattleResult(BattleOutcome.PlayerVictory, 20, new string('b', 64), units, 0, 2);

        var offense = BattleReportViewModels.Build(result, 0, BattleReportDimension.Offense);
        if (offense.Units.Select(unit => unit.Unit.RuntimeId).SequenceEqual(new[] { "a", "b" }) is false ||
            Math.Abs(offense.Units[0].DamageShare - .5f) > .001f ||
            Math.Abs(offense.Units[1].ActiveLifetimeSeconds - 1f) > .001f ||
            Math.Abs(offense.Units[1].DamagePerSecond - 100f) > .001f)
            throw new InvalidOperationException("offense ranking/share/active-lifetime rate derivation");
        if (offense.Units.Any(unit => !unit.Awards.HasFlag(BattleReportAwards.DamageLeader) ||
                                     !unit.Awards.HasFlag(BattleReportAwards.HealingLeader)) ||
            offense.Units[0].Awards.HasFlag(BattleReportAwards.DamageTakenLeader) ||
            !offense.Units[1].Awards.HasFlag(BattleReportAwards.DamageTakenLeader))
            throw new InvalidOperationException("positive tied or unique report awards");
        if (Math.Abs(offense.EnemyTeam.EnvironmentDamage - 30f) > .001f)
            throw new InvalidOperationException("positive environment damage reconciliation");

        var survival = BattleReportViewModels.Build(result, 0, BattleReportDimension.Survival);
        if (survival.Units[0].Unit.RuntimeId != "b")
            throw new InvalidOperationException("survival ranking");
        var enemyHealing = BattleReportViewModels.Build(result, 1, BattleReportDimension.Healing);
        if (!enemyHealing.ShowHealingEmptyState || enemyHealing.Units[0].HealingShare != 0 ||
            enemyHealing.Units[0].HealingPerSecond != 0 ||
            enemyHealing.Units[0].Awards.HasFlag(BattleReportAwards.HealingLeader))
            throw new InvalidOperationException("zero-safe healing state or zero-category award");
    }

    private static string ResultString(object row, string property) =>
        row.GetType().GetProperty(property)?.GetValue(row)?.ToString()
        ?? throw new InvalidOperationException($"battle result row omitted {property}");

    private static float ResultNumber(object row, string property)
    {
        var value = row.GetType().GetProperty(property)?.GetValue(row)
            ?? throw new InvalidOperationException($"battle result row omitted {property}");
        return Convert.ToSingle(value);
    }

    private static BattleSimulation ScreenSimulation(BattleScreenController screen) =>
        (BattleSimulation)(typeof(BattleScreenController).GetField("_simulation", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(screen)
            ?? throw new InvalidOperationException("battle screen simulation unavailable"));

    private static BattleConfig PaceConfig() => Config(
    [
        Spawn(Hero("pace-hero", health: 1000000, damage: 0, range: .5f, moveTicks: 1000), 0, 0, 0, "pace-hero"),
        Spawn(Unit("pace-enemy", health: 1000000, damage: 0, range: .5f, moveTicks: 1000), 1, 9, 5, "pace-enemy")
    ]);

    private static void DeploymentTransactions(ContentRegistry registry)
    {
        var save = new SaveService("tests/deployment-transactions");
        save.DeleteActiveRun();
        save.SaveMeta(new MetaProgressDto { UnlockedHeroIds = registry.Catalog.Heroes.Select(entry => entry.StableId).ToList() });
        var app = new RunApplication(registry, save, TestProjectFixture.Load(registry));
        if (!app.StartNewRun("hero_banner_marshal", 707)) throw new InvalidOperationException("deployment run start");
        var run = app.ActiveRun!;
        var first = run.Deployment[0];
        var second = run.Deployment[1];
        if (!app.MoveDeploymentUnit(first, 1) || run.Deployment[0] != second || run.Deployment[1] != first)
            throw new InvalidOperationException("deployed-to-occupied atomic swap");

        var recruitId = registry.Catalog.Soldiers.First(entry => run.Roster.All(unit => unit.ContentId != entry.StableId)).StableId;
        if (!app.Recruit(recruitId)) throw new InvalidOperationException("reserve recruit for deployment transaction");
        var reserve = run.Roster.Last().InstanceId;
        var displaced = run.Deployment[0];
        if (!app.MoveDeploymentUnit(reserve, 0) || run.Deployment[0] != reserve || run.Deployment.Contains(displaced))
            throw new InvalidOperationException("reserve-to-occupied replacement");
        if (!app.MoveDeploymentUnit(first, 5) || run.Deployment[1] != string.Empty || run.Deployment[5] != first)
            throw new InvalidOperationException("deployed-to-empty movement");
        if (!app.WithdrawDeploymentUnit(first) || run.Deployment[5] != string.Empty)
            throw new InvalidOperationException("deployment withdrawal");
        var snapshot = string.Join("|", run.Deployment);
        if (app.MoveDeploymentUnit(reserve, 0) || string.Join("|", run.Deployment) != snapshot)
            throw new InvalidOperationException("invalid deployment changed formation");

        var reloaded = new RunApplication(registry, save, TestProjectFixture.Load(registry));
        if (reloaded.ActiveRun is null || string.Join("|", reloaded.ActiveRun.Deployment) != snapshot)
            throw new InvalidOperationException("deployment did not persist through existing save contract");
        save.DeleteActiveRun();

        DeploymentRollbackAndReserveCapacity(registry);
    }

    private static void DeploymentRollbackAndReserveCapacity(ContentRegistry registry)
    {
        var failingSave = new FailingRunSaveService(registry.Catalog.Heroes.Select(entry => entry.StableId));
        var app = new RunApplication(registry, failingSave, TestProjectFixture.Load(registry));
        if (!app.StartNewRun("hero_banner_marshal", 808)) throw new InvalidOperationException("rollback deployment run start");
        var run = app.ActiveRun!;
        var first = run.Deployment[0];
        var second = run.Deployment[1];
        var recruitIds = registry.Catalog.Soldiers.Where(entry => run.Roster.All(unit => unit.ContentId != entry.StableId)).Take(6).ToArray();
        if (!app.Recruit(recruitIds[0].StableId)) throw new InvalidOperationException("rollback reserve recruit");
        var reserve = run.Roster.Last().InstanceId;

        failingSave.FailActiveRunSaves = true;
        ExpectDeploymentRollback(app, failingSave, () => app.MoveDeploymentUnit(first, 1), "failed-save swap");
        ExpectDeploymentRollback(app, failingSave, () => app.MoveDeploymentUnit(first, 5), "failed-save move");
        ExpectDeploymentRollback(app, failingSave, () => app.MoveDeploymentUnit(reserve, 0), "failed-save replacement");
        ExpectDeploymentRollback(app, failingSave, () => app.WithdrawDeploymentUnit(first), "failed-save withdrawal");

        failingSave.FailActiveRunSaves = false;
        for (var index = 1; index < recruitIds.Length; index++)
            if (!app.Recruit(recruitIds[index].StableId)) throw new InvalidOperationException("full reserve recruit");
        var initiallyReserved = run.Roster.Where(unit => !run.Deployment.Contains(unit.InstanceId)).Take(3).ToArray();
        for (var slot = 3; slot < 6; slot++)
            if (!app.MoveDeploymentUnit(initiallyReserved[slot - 3].InstanceId, slot)) throw new InvalidOperationException("fill deployment for reserve capacity");
        if (run.Roster.Count != 9 || run.Deployment.Count(id => !string.IsNullOrEmpty(id)) != 6)
            throw new InvalidOperationException("9/6/3 reserve fixture setup");
        var fullSnapshot = string.Join("|", run.Deployment);
        var savesBefore = failingSave.ActiveRunSaveCalls;
        if (app.WithdrawDeploymentUnit(run.Deployment[0]) || string.Join("|", run.Deployment) != fullSnapshot || failingSave.ActiveRunSaveCalls != savesBefore)
            throw new InvalidOperationException("full reserve withdrawal changed formation or reached persistence");
    }

    private static void ExpectDeploymentRollback(RunApplication app, FailingRunSaveService save, Func<bool> action, string label)
    {
        var before = string.Join("|", app.ActiveRun!.Deployment);
        var calls = save.ActiveRunSaveCalls;
        if (action() || string.Join("|", app.ActiveRun.Deployment) != before || save.ActiveRunSaveCalls != calls + 1)
            throw new InvalidOperationException(label + " did not roll memory back after one failed save");
    }

    private static void AssertConversion(RunApplication app, string heroId, int expected)
    {
        app.AbandonRun();
        if (!app.StartNewRun(heroId, 123)) throw new InvalidOperationException("start conversion run: " + heroId);
        var before = app.ActiveRun!.Gold;
        var converted = app.ConvertRecruitToGold();
        if (converted != expected || app.ActiveRun.Gold != before + expected)
            throw new InvalidOperationException($"recruit conversion {heroId}: expected {expected}, got {converted}");
    }

    private static IReadOnlyList<TowerRegionDefinition> Regions() =>
    [
        GD.Load<TowerRegionDefinition>("res://content/tower/region_ember_foundry.tres"),
        GD.Load<TowerRegionDefinition>("res://content/tower/region_gloam_crypt.tres"),
        GD.Load<TowerRegionDefinition>("res://content/tower/region_crown_engine.tres")
    ];

    private static float AttackValue(BattleSimulation simulation, string source) => simulation.DrainEvents()
        .First(e => e.Type == "attack" && e.SourceRuntimeId == source).Value;

    private static BattleConfig Config(
        IEnumerable<BattleSpawn> spawns,
        IBattleFloorRuleRuntime? floor = null,
        HeroRuleSnapshot? rule = null,
        ModifierSnapshot? modifiers = null,
        SummonProfiles? summons = null,
        int startingGold = 0,
        CompiledTacticalCommandDefinition? tacticalCommand = null,
        IReadOnlyDictionary<string, UnitSnapshot>? tacticalSummons = null) => new()
        {
            Seed = 17,
            FloorRule = floor ?? new ClearFloorRuleRuntime("clear", "常规", "test"),
            Spawns = spawns.ToList(),
            HeroRule = rule ?? Rule(),
            Modifiers = modifiers ?? new ModifierSnapshot(),
            Summons = summons ?? new SummonProfiles(),
            StartingGold = startingGold,
            TacticalCommands = tacticalCommand is null ? null : TacticalPreparation(tacticalCommand),
            TacticalSummons = tacticalSummons ?? ImmutableDictionary<string, UnitSnapshot>.Empty
        };

    private static List<BattleSpawn> BasicSpawns() =>
    [
        Spawn(Hero("player", health: 1000), 0, 0, 0, "player"),
        Spawn(Unit("enemy", health: 1000), 1, 9, 5, "enemy")
    ];

    private static BattleSpawn Spawn(UnitSnapshot unit, int team, int x, int y, string id, UnitSnapshot? behaviorSummon = null) =>
        new(unit, team, new Vector2I(x, y), id, BehaviorSummon: behaviorSummon);

    private static UnitSnapshot Hero(string id, float health = 200, float damage = 10, float range = 1, int moveTicks = 1, IReadOnlyList<string>? tags = null) =>
        Unit(id, true, false, health, damage, range, moveTicks: moveTicks, tags: tags);

    private static UnitSnapshot Unit(
        string id,
        bool isHero = false,
        bool isBoss = false,
        float health = 200,
        float damage = 10,
        float range = 1,
        int attackTicks = 10,
        int moveTicks = 1,
        float armor = 0,
        float heal = 0,
        IReadOnlyList<string>? tags = null,
        UnitBehaviorSnapshot? behavior = null,
        float lifeSteal = 0,
        float splash = 0) =>
        new(id, id, isBoss ? UnitRole.Boss : UnitRole.Fighter, isHero, isBoss, health, damage, range, attackTicks, moveTicks,
            armor, heal, splash, lifeSteal, tags ?? Array.Empty<string>(), behavior ?? new UnitBehaviorSnapshot());

    private static HeroRuleSnapshot Rule(
        float formationArmor = 0,
        float formationDamage = 0,
        float killGrowth = 0,
        string requiredTag = "",
        bool summonOnDeath = false)
    {
        return new HeroRuleSnapshot(
            1, 1, 1, 0, 0, 0, false,
            requiredTag, 1, 1, formationArmor, formationDamage, killGrowth, 0,
            summonOnDeath, false, 0, 0, string.Empty);
    }

    private static TacticalCommandContentRoot LoadTacticalCommandRoot(string sceneName) =>
        GD.Load<PackedScene>($"res://content/tactical-commands/commands/{sceneName}.tscn")
            .Instantiate<TacticalCommandContentRoot>();

    private static CompiledTacticalCommandDefinition LoadTacticalCommand(string sceneName)
    {
        var command = LoadTacticalCommandRoot(sceneName);
        try
        {
            var authoredLoadout = command.Definition.AbilityLoadout;
            var abilityCompilation = AbilityDefinitionCompiler.CompileLoadout(authoredLoadout);
            var loadout = abilityCompilation.Loadout ?? throw new InvalidOperationException(
                $"{sceneName} ability compile: {string.Join("; ", abilityCompilation.Report.CoreErrors)}");
            var result = TacticalCommandDefinitionCompiler.Compile(
                command.Definition,
                authored => ReferenceEquals(authored, authoredLoadout) ? loadout : null);
            return result.Definition ?? throw new InvalidOperationException(
                $"{sceneName} tactical compile: {string.Join("; ", result.Report.CoreErrors)}");
        }
        finally { command.Free(); }
    }

    private static void ValidateTacticalCommand(
        TacticalCommandContentRoot command,
        ContentRegistry registry,
        string label,
        params string[] descriptionValues)
    {
        var report = command.ValidateAuthoring();
        if (report.HasCoreErrors) throw new InvalidOperationException($"{label} sentinel authoring: {string.Join("; ", report.CoreErrors)}");
        var compiled = command.Resolve(registry.Graph);
        if (compiled.TacticalPointCost != 1)
            throw new InvalidOperationException(label + " tactical-point cost is not scene-authored as one");
        foreach (var value in descriptionValues)
            if (!compiled.Description.Contains(value, StringComparison.Ordinal))
                throw new InvalidOperationException($"{label} generated description omitted sentinel {value}: {compiled.Description}");
    }

    private static TacticalCommandBattlePreparation TacticalPreparation(
        CompiledTacticalCommandDefinition primary)
    {
        var secondary = LoadTacticalCommand(primary.StableId == "tactical_rally"
            ? "TacticalTimeStop"
            : "TacticalRally");
        var commands = ImmutableArray.Create(primary, secondary);
        return new TacticalCommandBattlePreparation(
            TacticalCommandBattlePreparationBuilder.Fingerprint(commands),
            commands);
    }

    private static IReadOnlyDictionary<string, UnitSnapshot> TacticalSummons(
        params (string StableId, UnitSnapshot Unit)[] entries) =>
        entries.ToImmutableDictionary(entry => entry.StableId, entry => entry.Unit, StringComparer.Ordinal);

    private static BattleUnitState State(UnitSnapshot definition, string runtimeId, float? health = null, int team = 0) => new()
    {
        RuntimeId = runtimeId,
        SourceInstanceId = runtimeId,
        Definition = definition,
        Attributes = MovementAttributes.Create(definition, runtimeId),
        Team = team,
        Cell = new Vector2I(team == 0 ? 0 : 9, 2),
        Health = health ?? definition.MaxHealth
    };

    private sealed class MovementFixtureAttributeOwner : IDisposable
    {
        private readonly List<BattleAttributeScope> _scopes = [];
        private int _sequence;

        public BattleAttributeSet Create(UnitSnapshot definition, string runtimeId)
        {
            var scope = new BattleAttributeScope($"gameplay_movement_fixture_{++_sequence}");
            _scopes.Add(scope);
            var compiled = definition.AttributeDefinition ?? AttributeDefinitionCompiler.Legacy(
                new Dictionary<CombatAttribute, float>
                {
                    [CombatAttribute.MaxHealth] = definition.MaxHealth,
                    [CombatAttribute.AttackDamage] = definition.Damage,
                    [CombatAttribute.Armor] = definition.Armor,
                    [CombatAttribute.AttackRange] = definition.Range,
                    [CombatAttribute.HealingPower] = definition.HealPower,
                    [CombatAttribute.LifeSteal] = definition.LifeSteal
                });
            var projection = AttributeDefinitionCompiler.WithBaseValues(
                compiled,
                new Dictionary<CombatAttribute, float>
                {
                    [CombatAttribute.MaxHealth] = definition.MaxHealth,
                    [CombatAttribute.AttackDamage] = definition.Damage,
                    [CombatAttribute.Armor] = definition.Armor,
                    [CombatAttribute.AttackRange] = definition.Range,
                    [CombatAttribute.HealingPower] = definition.HealPower,
                    [CombatAttribute.LifeSteal] = definition.LifeSteal
                });
            return scope.CreateSet(runtimeId, projection);
        }

        public void Dispose()
        {
            foreach (var scope in _scopes)
                scope.Complete(AttributeScopeCompletionReason.Disposal, 0);
            _scopes.Clear();
        }
    }

    private static void Near(float actual, float expected, float tolerance, string message)
    {
        if (Math.Abs(actual - expected) > tolerance)
            throw new InvalidOperationException($"{message}: expected {expected}, got {actual}");
    }

    private static void ExpectThrows(Action action, string message)
    {
        try { action(); }
        catch (Exception) { return; }
        throw new InvalidOperationException("expected exception: " + message);
    }

    private sealed class FailingRunSaveService(IEnumerable<string> unlockedHeroIds) : IRunSaveService
    {
        private readonly MetaProgressDto _meta = new() { UnlockedHeroIds = unlockedHeroIds.ToList() };
        private readonly SettingsDto _settings = new();
        private ActiveRunDto? _run;

        public bool FailActiveRunSaves { get; set; }
        public int ActiveRunSaveCalls { get; private set; }
        public MetaProgressDto LoadMeta() => _meta;
        public SettingsDto LoadSettings() => _settings;
        public ActiveRunDto? LoadActiveRun() => _run;
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value)
        {
            ActiveRunSaveCalls++;
            if (FailActiveRunSaves) return false;
            _run = value;
            return true;
        }
        public void DeleteActiveRun() => _run = null;
    }

    private sealed class DamageEveryTickRule(float amount) : ClearFloorRuleRuntime("damage", "伤害", "test")
    {
        public override void OnTick(BattleRuleContext context)
        {
            foreach (var unit in context.Units.Where(unit => unit.Alive).ToArray())
                context.Damage("floor", unit, amount);
        }
    }

    private sealed class DamageAndHealEveryTickRule(float damage, float healing)
        : ClearFloorRuleRuntime("damage-heal", "环境统计", "test")
    {
        public override void OnTick(BattleRuleContext context)
        {
            foreach (var unit in context.Units.Where(unit => unit.Alive).ToArray())
            {
                context.Damage("floor", unit, damage);
                context.Heal(unit, healing);
            }
        }
    }

    private sealed class LethalThenHealRule(string runtimeId, float damage, float healing)
        : ClearFloorRuleRuntime("lethal-then-heal", "终态治疗探针", "test")
    {
        private bool _applied;

        public override void OnTick(BattleRuleContext context)
        {
            if (_applied) return;
            _applied = true;
            var target = context.Units.Single(unit => unit.RuntimeId == runtimeId);
            context.Damage("floor", target, damage);
            context.Heal(target, healing);
        }
    }

    private sealed class BlockedCellsRule(IEnumerable<Vector2I> cells) : ClearFloorRuleRuntime("blocked", "阻挡", "test")
    {
        private readonly HashSet<Vector2I> _cells = cells.ToHashSet();
        public override bool CanOccupy(Vector2I cell) => !_cells.Contains(cell);
    }

    private sealed class TrackingFloorRule : ClearFloorRuleRuntime
    {
        public int Started { get; private set; }
        public int Ended { get; private set; }
        public bool ThrowOnStart { get; init; }
        public bool ThrowOnEnd { get; init; }

        public TrackingFloorRule() : base("tracking", "追踪", "test") { }
        public override void OnBattleStarted(BattleRuleContext context)
        {
            Started++;
            if (ThrowOnStart) throw new InvalidOperationException("expected start failure");
        }
        public override void OnBattleEnded(BattleRuleContext context, BattleOutcome outcome)
        {
            Ended++;
            if (ThrowOnEnd) throw new InvalidOperationException("expected end failure");
        }
    }

    private sealed class ThrowingTickRule() : ClearFloorRuleRuntime("throwing-tick", "异常", "test")
    {
        public override void OnTick(BattleRuleContext context) =>
            throw new InvalidOperationException("expected tick failure");
    }
}
