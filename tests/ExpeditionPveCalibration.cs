using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

// Controlled population snapshots against the current authored PvE profiles.
// This does not create/advance/save a Run, grant fabricated growth, or estimate
// the probability of receiving these heroes through natural recruitment offers.
public partial class ExpeditionPveCalibration : Node
{
    private const string SnapshotKind = "controlled_population_snapshot";
    private const string PlacementPolicy = "range_front_back_v1";
    private sealed record Probe(string Id, int[] Sequence);
    private sealed record Options(string Output, ulong[] Seeds, string? Probe, int? Count, string? Profile,
        bool DeploymentOnly, bool Remembered, string? RememberedReplay, bool V1Content, string? V1Replay, bool V1EffectProbes, bool V1Economy, bool V1RunItems, bool V1SustainProbe, bool V1EconomyGaps);
    private sealed record TeamSummary(int Team, int PermanentUnits, int TemporaryUnits,
        int PermanentSurvivors, int TemporarySurvivors, double PermanentHealthLeft,
        double TemporaryHealthLeft, double PermanentInitialMaxHealth, double TemporaryInitialMaxHealth,
        double PermanentFinalMaxHealth, double TemporaryFinalMaxHealth, double DamageDealt,
        double HealthDamageDealt, double ShieldDamageDealt, double OverkillDamageDealt,
        double DamageTaken, double EffectiveHealing, double WastedHealing, double ShieldAbsorbed,
        double ShieldGranted, double RecipientHardControlSeconds,
        double PermanentRecipientHardControlSeconds, double TemporaryRecipientHardControlSeconds,
        double LegacyActionLockSeconds, int ManaCasts, int PermanentManaCasts, int TemporaryManaCasts,
        int AbilityResolutions);
    private sealed record Observation(string Probe, int PopulationSnapshot, string Profile,
        int? BiomeIndex, int? BossStageIndex, ulong Seed, string Outcome, int Ticks,
        double Seconds, string Digest, TeamSummary Player, TeamSummary Enemy);
    private sealed record FailedCase(string Probe, int PopulationSnapshot, string Profile,
        ulong Seed, string FailureStage, string ExceptionType, string Message, string SourcePath);

    private static readonly Probe[] Probes =
    [
        new("polar_shooter", [2, 3, 4, 19, 7, 5, 18, 13]),
        new("forest_shape", [11, 16, 8, 18, 13, 17, 2, 4]),
        new("brawler_protector", [7, 18, 2, 16, 4, 8, 19, 13]),
        new("forest_assassin", [5, 17, 11, 16, 18, 19, 3, 2])
    ];
    private static readonly JsonSerializerOptions CompactJson = CreateJsonOptions(false);
    private static readonly JsonSerializerOptions PrettyJson = CreateJsonOptions(true);

    public override async void _Ready()
    {
        try
        {
            var options = ParseOptions(OS.GetCmdlineUserArgs());
            var publication = await GrowthContentPackage.CreateReadyAsync(this);
            var package = publication.Package ?? throw new InvalidOperationException(
                string.Join("; ", publication.Report.CoreErrors));
            var expedition = package.Project.Campaign.Progression as CompiledExpedition ??
                throw new InvalidOperationException("Current package is not an expedition.");
            Require(package.DefaultNewRunProfileId == RunProfileIds.EightTraitExpedition,
                "calibration must use the current authored expedition");
            if (options.V1EconomyGaps)
            {
                var source = options.V1Replay!;
                source = source.StartsWith("res://") || source.StartsWith("user://") ? ProjectSettings.GlobalizePath(source) : Path.GetFullPath(source);
                await CalibrateV1RunItems(package, expedition, source, options.Output, "v1_relic_r02", "normal");
                await CalibrateV1TreasurySources(package, expedition, source, options.Output);
                GetTree().Quit();
                return;
            }
            if (options.V1SustainProbe)
            {
                await CalibrateV1SustainProbe(package, expedition, options);
                GetTree().Quit();
                return;
            }
            if (options.V1RunItems)
            {
                var source = options.V1Replay!;
                source = source.StartsWith("res://") || source.StartsWith("user://") ? ProjectSettings.GlobalizePath(source) : Path.GetFullPath(source);
                await CalibrateV1RunItems(package, expedition, source, options.Output);
                GetTree().Quit();
                return;
            }
            if (options.V1Economy)
            {
                var source = options.V1Replay!;
                source = source.StartsWith("res://") || source.StartsWith("user://") ? ProjectSettings.GlobalizePath(source) : Path.GetFullPath(source);
                await CalibrateV1Economy(package, expedition, source, options.Output);
                GetTree().Quit();
                return;
            }
            if (options.V1EffectProbes)
            {
                await CalibrateV1Probes(package, expedition, options);
                GetTree().Quit();
                return;
            }
            if (options.V1Content)
            {
                await CalibrateV1Content(package, expedition, options);
                GetTree().Quit();
                return;
            }
            if (options.Remembered)
            {
                await CalibrateRemembered(package, expedition, options);
                GetTree().Quit();
                return;
            }
            if (options.DeploymentOnly)
            {
                VerifyDeployment(package, expedition, options.Seeds[0]);
                GetTree().Quit();
                return;
            }
            Require(package.GrowthRules is { } growth && growth.Heroes.Keys.Order().SequenceEqual(
                    AbilityPrototypeContent.UnitNumbers.Select(AbilityPrototypeContent.Id).Order()) &&
                growth.Heroes.TryGetValue("prototype_u16", out var flower) &&
                flower.ProductionModes.SequenceEqual(new[] { GrowthProductionMode.Cultivation }) &&
                flower.FixedAttackGain == 4 && flower.FixedMaxHealthGain == 40 && flower.GrowthRate == 0 &&
                growth.Spells.IsEmpty,
                "population snapshots use the current published roster and base +4/+40 flower cultivation; actual snapshots carry no growth or items");

            var cases = (from probe in Probes
                where options.Probe is null || probe.Id == options.Probe
                from count in new[] { 2, 4, 6, 8 }
                where options.Count is null || count == options.Count
                from profileId in options.Profile is null ? CheckpointProfiles(count) : [options.Profile]
                where options.Profile is null || profileId == options.Profile
                from seed in options.Seeds
                select (probe, count, profileId, seed)).ToArray();
            Require(cases.Length is > 0 and <= 200, "batch must contain 1..200 battles");
            Directory.CreateDirectory(options.Output);
            var detailsPath = Path.Combine(options.Output, "battles.jsonl");
            var preparation = new RunBattlePreparationService(package.Content, package.Project,
                new RunRelicService(package.Content), package.GrowthRules);
            var observations = new List<Observation>();
            var failures = new List<FailedCase>();
            WriteManifest(options, package, cases.Length, observations.Count, failures.Count);

            using (var writer = new StreamWriter(detailsPath, append: false))
            {
                foreach (var (probe, count, profileId, seed) in cases)
                {
                    var profile = expedition.Encounters[profileId];
                    var sourcePath = ProfilePath(profile);
                    var run = CreateSnapshot(package, probe, count, seed);
                    var plan = CreatePlan(profile);
                    // A fixed seed is shared by every group; do not mix lineup/index into it.
                    BattleConfig preparedConfig;
                    try { preparedConfig = preparation.Build(run, plan, true); }
                    catch (InvalidOperationException error)
                    {
                        // Preserve the authored failure. No fallback positions, body sizes,
                        // multipliers or actors may turn an unpreparable level into a battle.
                        var failure = new FailedCase(probe.Id, count, profileId, seed,
                            "BattlePreparation", error.GetType().Name, error.Message, sourcePath);
                        failures.Add(failure);
                        writer.WriteLine(JsonSerializer.Serialize(new
                        {
                            SchemaVersion = 1, SnapshotKind, Status = "blocked", FailedCase = failure,
                            AuthoredEnemies = profile.Composition.EnemyIds,
                            AuthoredEnemyCells = profile.Composition.Cells.Select(cell => new { cell.X, cell.Y }),
                            Notes = Notes()
                        }, CompactJson));
                        writer.Flush();
                        GD.Print($"PVE_CALIBRATION_BLOCKED probe={probe.Id} heroes={count} profile={profileId} seed={seed} stage=BattlePreparation message={error.Message}");
                        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                        continue;
                    }
                    // Simulation and report assertions remain fail-fast outside the preparation catch.
                    var config = CaptureConfiguration(preparedConfig, seed);
                    using var battle = new BattleSimulation(config);
                    var initial = BattleSimulation.CapturePreparation(battle);
                    Require(initial.Units.Where(unit => unit.Team == 0 && !unit.IsTemporary).Count() == count,
                        "all selected permanent heroes must enter the battle");
                    var result = battle.RunToEnd();
                    Require(result.Outcome != BattleOutcome.Running && result.Ticks <= BattleSimulation.MaxTicks,
                        "bounded terminal outcome");
                    Require(result.Units.All(unit => unit.DetailedStatistics is not null), "detailed statistics gate");
                    VerifyStatistics(result);
                    var biomeIndex = Enumerable.Range(0, expedition.Biomes.Length).Cast<int?>()
                        .FirstOrDefault(index => expedition.Biomes[index!.Value].EncounterProfiles.Contains(profileId));
                    var bossStageIndex = Enumerable.Range(0, expedition.Stages.Length).Cast<int?>()
                        .FirstOrDefault(index => expedition.Stages[index!.Value].BossProfileId == profileId);
                    var player = Summarize(result, initial, 0);
                    var enemy = Summarize(result, initial, 1);
                    var observation = new Observation(probe.Id, count, profileId, biomeIndex, bossStageIndex,
                        seed, result.Outcome.ToString(), result.Ticks, result.Ticks * BattleTiming.TickSeconds,
                        result.Digest, player, enemy);
                    observations.Add(observation);
                    writer.WriteLine(JsonSerializer.Serialize(new
                    {
                        SchemaVersion = 1, SnapshotKind, PlacementPolicy, Observation = observation,
                        ContentFingerprint = package.DefaultProfile.ContentFingerprint,
                        Source = new { Project = GrowthContentPackage.ProjectPath, Encounter = sourcePath,
                            Growth = GrowthContentPackage.RulesPath, GameProfile = run.ProfileId },
                        Checkpoint = new { BiomeIndex = biomeIndex, BossStageIndex = bossStageIndex,
                            ProfileId = profileId, profile.Encounter.NodeType, profile.Encounter.StableId,
                            CompositionId = profile.Composition.StableId, FloorRule = plan.FloorRuleId,
                            profile.FinalHealthMultiplier, profile.FinalDamageMultiplier },
                        PlayerInput = new { run.Seed, run.BattleNumber, run.CurrentPopulation,
                            NaturalAcquisitionVerified = false, NaturalRoute = (string?)null,
                            EquipmentCount = 0, RelicCount = 0, Materials = 0, Research = 0,
                            Roster = run.Roster.Select(hero => new { hero.InstanceId, hero.ContentId,
                                ScenePath = Entry(package.Content, hero.ContentId).Scene.ResourcePath,
                                DefinitionPath = Entry(package.Content, hero.ContentId).Definition.ResourcePath,
                                hero.Growth }) },
                        BattleInput = new { config.Seed, config.Identity, config.CaptureDetailedStatistics,
                            config.EmptyDeploymentSlots, config.StartingGold },
                        Traits = initial.Traits,
                        InitialUnits = initial.Units.Select(unit => InitialUnit(package.Content, unit)),
                        Result = new { result.Outcome, result.Ticks, result.Digest, result.GoldSpent,
                            result.SuccessfulTacticalCommandUses, result.PermanentGains,
                            Units = result.Units.Select(ReportUnit) },
                        Notes = Notes()
                    }, CompactJson));
                    writer.Flush();
                    if (observations.Count % options.Seeds.Length == 0)
                        GD.Print($"PVE_CALIBRATION_PROGRESS probe={probe.Id} heroes={count} profile={profileId} completed={observations.Count} blocked={failures.Count} planned={cases.Length}");
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                }
            }
            var groups = observations.GroupBy(row => new { row.Probe, row.PopulationSnapshot, row.Profile })
                .Select(group => new { group.Key, Battles = group.Count(),
                    Wins = group.Count(row => row.Outcome == nameof(BattleOutcome.PlayerVictory)),
                    Defeats = group.Count(row => row.Outcome == nameof(BattleOutcome.PlayerDefeat)),
                    Timeouts = group.Count(row => row.Outcome == nameof(BattleOutcome.Timeout)),
                    MeanSeconds = group.Average(row => row.Seconds),
                    MeanPermanentSurvivors = group.Average(row => row.Player.PermanentSurvivors),
                    MeanPermanentHealthRatio = group.Average(row => row.Player.PermanentFinalMaxHealth > 0
                        ? row.Player.PermanentHealthLeft / row.Player.PermanentFinalMaxHealth : 0),
                    MeanEnemyPermanentHealthRatio = group.Average(row => row.Enemy.PermanentFinalMaxHealth > 0
                        ? row.Enemy.PermanentHealthLeft / row.Enemy.PermanentFinalMaxHealth : 0),
                    MeanEnemyRecipientHardControlSeconds = group.Average(row => row.Enemy.RecipientHardControlSeconds),
                    MeanPlayerManaCasts = group.Average(row => row.Player.ManaCasts) }).ToArray();
            System.IO.File.WriteAllText(Path.Combine(options.Output, "summary.json"), JsonSerializer.Serialize(new
            { SchemaVersion = 1, SnapshotKind, PlannedBattles = cases.Length,
                CompletedBattles = observations.Count, BlockedBattles = failures.Count,
                Battles = observations.Count, Notes = Notes(), Groups = groups,
                Observations = observations, FailedCases = failures }, PrettyJson));
            System.IO.File.WriteAllText(Path.Combine(options.Output, "failures.json"), JsonSerializer.Serialize(failures, PrettyJson));
            WriteManifest(options, package, cases.Length, observations.Count, failures.Count);
            Require(observations.Count + failures.Count == cases.Length, "every planned case must be accounted for");
            GD.Print(failures.Count > 0
                ? $"CALIBRATION_COMPLETED_WITH_BLOCKERS planned={cases.Length} completed={observations.Count} blocked={failures.Count} output={options.Output}"
                : $"EXPEDITION_PVE_CALIBRATION_OK battles={observations.Count} output={options.Output}");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PrintErr("EXPEDITION_PVE_CALIBRATION_FAILED " + error);
            GetTree().Quit(1);
        }
    }

    private static void WriteManifest(Options options, CompiledGamePackage package, int planned, int completed, int blocked) =>
        System.IO.File.WriteAllText(Path.Combine(options.Output, "manifest.json"), JsonSerializer.Serialize(new
        {
            SchemaVersion = 1, SnapshotKind, PlacementPolicy, GameProfile = package.DefaultNewRunProfileId,
            ContentFingerprint = package.DefaultProfile.ContentFingerprint,
            ContentProjectPath = GrowthContentPackage.ProjectPath,
            GrowthRulesPath = GrowthContentPackage.RulesPath, Seeds = options.Seeds,
            Options = options, PlannedBattles = planned, CompletedBattles = completed, BlockedBattles = blocked,
            BattleNumber = 0, PopulationInvestment = package.Project.RunRules.InitialPopulation,
            MaxTicks = BattleSimulation.MaxTicks, TickSeconds = BattleTiming.TickSeconds,
            Probes = Probes, PopulationSnapshots = new[] { 2, 4, 6, 8 },
            Checkpoints = new[] { 2, 4, 6, 8 }.Select(count => new { Count = count, Profiles = CheckpointProfiles(count) }),
            Notes = Notes(),
            Rerun = "--headless --path <repository> res://tests/ExpeditionPveCalibration.tscn -- " +
                "--out <output-directory> [--seeds 20261006,1776,9173] [--only-probe <id>] " +
                "[--only-count 2|4|6|8] [--only-profile <profile-id>]"
        }, PrettyJson));

    private static string[] CheckpointProfiles(int count)
    {
        if (count == 2) return ["ember_combat_profile", "gloam_combat_profile", "crown_combat_profile"];
        var region = count == 4 ? "ember" : count == 6 ? "gloam" : "crown";
        return [region + "_combat_profile", region + "_elite_profile", region + "_boss_profile"];
    }

    private static ActiveRunDto CreateSnapshot(CompiledGamePackage package, Probe probe, int count, ulong seed)
    {
        var run = new ActiveRunDto
        {
            ProfileId = package.DefaultNewRunProfileId, Seed = seed, BattleNumber = 0,
            CurrentPopulation = package.Project.RunRules.InitialPopulation,
            Gold = 0, CurrentRunHealth = 100, MaximumRunHealth = 100,
            Growth = new GrowthRunDto { RulesId = package.GrowthRules!.StableId }
        };
        var front = new[] { new Vector2I(2, 2), new Vector2I(2, 4), new Vector2I(2, 0),
            new Vector2I(2, 3), new Vector2I(2, 1), new Vector2I(2, 5) };
        var back = new[] { new Vector2I(0, 2), new Vector2I(0, 4), new Vector2I(0, 0),
            new Vector2I(0, 3), new Vector2I(0, 1), new Vector2I(0, 5) };
        var occupied = new HashSet<Vector2I>();
        for (var i = 0; i < count; i++)
        {
            var hero = new RosterHeroInstanceDto { InstanceId = $"probe-hero-{i}",
                ContentId = AbilityPrototypeContent.Id(probe.Sequence[i]) };
            run.Roster.Add(hero);
            var definition = (UnitDefinition)Entry(package.Content, hero.ContentId).Definition;
            var preferred = definition.AttackRange < 2 ? front.Concat(back) : back.Concat(front);
            var cell = preferred.First(candidate => occupied.Add(candidate));
            run.Deployment[BattlefieldLayout.PlayerDeploymentSlot(cell)] = hero.InstanceId;
        }
        Require(run.Roster.All(hero => hero.Growth.AddedAttack == 0 && hero.Growth.AddedMaxHealth == 0 &&
                hero.Growth.ProductionTargetInstanceId.Length == 0 && hero.Growth.AscensionId.Length == 0 &&
                hero.Growth.History.Count == 0) && run.Growth!.History.Count == 0 && run.Growth.PendingNode is null,
            "controlled snapshots retain zero permanent growth and no saved production assignment");
        Require(run.Roster.Select(hero => hero.ContentId).Distinct().Count() == count, "unique player identities");
        Require(RunPopulationPolicy.Validate(run, package.Project.RunRules), "authoritative population validation");
        return run;
    }

    private static EncounterPlan CreatePlan(CompiledExpeditionEncounter profile) => new(
        profile.Encounter.Title("PvE对照"), profile.Encounter.FloorRulePool.ContentIds[0],
        profile.Composition.EnemyIds, profile.Encounter.NodeType == TowerNodeType.Boss,
        profile.Encounter.NodeType == TowerNodeType.Elite, profile.Encounter.StableId, profile.Encounter.NodeType)
    {
        CompositionId = profile.Composition.StableId, EnemyCells = profile.Composition.Cells,
        ExpeditionProfileId = profile.StableId, ExpeditionSiteId = "controlled-snapshot",
        ExpeditionBoss = profile.Encounter.NodeType == TowerNodeType.Boss
    };

    // Preparation-only checks use the same Run adapter and simulated opening as
    // deployment previews. They do not advance a journey or infer battle balance.
    private static void VerifyDeployment(CompiledGamePackage package, CompiledExpedition expedition, ulong seed)
    {
        var run = CreateSnapshot(package, Probes[0], 4, seed);
        var preparation = new RunBattlePreparationService(package.Content, package.Project,
            new RunRelicService(package.Content), package.GrowthRules);
        Require(expedition.Encounters.Count == 15, "V1 publishes nine ordinary, three elite and three Boss profiles");
        foreach (var id in expedition.Encounters.Keys.Order(StringComparer.Ordinal))
        {
            var profile = expedition.Encounters[id];
            var plan = CreatePlan(profile);
            var config = preparation.Build(run, plan, true);
            var enemies = config.Spawns.Where(spawn => spawn.Team == 1).ToArray();
            Require(enemies.Length == profile.Composition.EnemyIds.Length, id + " preserves every enemy");
            Require(enemies.Select(spawn => spawn.Cell).SequenceEqual(profile.Composition.Cells),
                id + " valid complete authored positions are preserved");
            VerifyEnemyBodies(enemies.Select(spawn => spawn.Cell).ToArray(),
                enemies.Select(spawn => spawn.Unit.BodyRadius).ToArray(), config.FloorRule, id);
            var preview = BattlePreparationProjection.Build(preparation.Build(run, plan, false));
            using var battle = new BattleSimulation(config);
            foreach (var spawn in enemies)
            {
                var shown = preview.Units.Single(unit => unit.SourceInstanceId == spawn.InstanceId);
                var actual = battle.Units.Single(unit => unit.SourceInstanceId == spawn.InstanceId);
                Require(shown.Position == BattlefieldSpace.CellCenter(spawn.Cell) && actual.Position == shown.Position,
                    id + " preview and actual opening preserve prepared full-body position");
                Near(actual.BodyRadius, spawn.Unit.BodyRadius, id + " authored body size preserved");
            }
            GD.Print("EXPEDITION_DEPLOYMENT_PROFILE_OK " + id);
        }

        // These are isolated malformed author inputs to the real adapter, not
        // alternate production formations or silent corrections to saved plans.
        var fixturePlan = CreatePlan(expedition.Encounters["ember_combat_profile"]);
        var radii = fixturePlan.EnemyIds.Select(id => ((UnitDefinition)Entry(package.Content, id).Definition).BodyRadius).ToArray();
        var clear = new ClearFloorRuleRuntime("deployment_fixture", "部署夹具", "只检查完整身体。");
        Vector2I[] Resolve(ImmutableArray<Vector2I> cells, IBattleFloorRuleRuntime? floor = null)
        {
            var request = RunBattlePreparationAdapter.CreateRequest(package.Content, run,
                fixturePlan with { EnemyCells = cells }, floor ?? clear, new ModifierSnapshot(),
                null, null, null, run.CurrentPopulation, true, package.GrowthRules);
            return request.Placements.Where(placement => placement.UnitInstanceId.StartsWith("enemy-", StringComparison.Ordinal))
                .Select(placement => placement.Cell).ToArray();
        }
        var legal = ImmutableArray.Create(new Vector2I(6, 1), new Vector2I(8, 4));
        Require(Resolve(legal).SequenceEqual(legal), "legal complete forward enemy formation kept");
        foreach (var malformed in new[]
        {
            ImmutableArray.Create(new Vector2I(5, 1), new Vector2I(8, 4)), // Body crosses enemy half.
            ImmutableArray.Create(new Vector2I(2, 1), new Vector2I(8, 4)), // Player deployment region.
            ImmutableArray.Create(new Vector2I(6, 1), new Vector2I(6, 2)), // Pair overlaps.
            ImmutableArray.Create(new Vector2I(6, 1), new Vector2I(9, 4))  // Body crosses arena edge.
        })
        {
            var resolved = Resolve(malformed);
            Require(resolved.All(cell => cell.X >= BattlefieldLayout.Width - BattlefieldLayout.PlayerDeploymentColumns),
                "invalid complete formation discards all anchors and uses original fallback region");
            VerifyEnemyBodies(resolved, radii, clear, "malformed author fallback");
            Require(Resolve(malformed).SequenceEqual(resolved), "fallback is deterministic");
        }
        var blocked = new DeploymentBlockedFloor(new Vector2I(6, 1));
        var terrainFallback = Resolve(legal, blocked);
        Require(terrainFallback.All(cell => cell.X >= BattlefieldLayout.Width - BattlefieldLayout.PlayerDeploymentColumns),
            "terrain conflict discards entire authored formation");
        VerifyEnemyBodies(terrainFallback, radii, blocked, "terrain fallback");
        GD.Print("EXPEDITION_DEPLOYMENT_CONTRACT_OK current-profiles=15 authored-preserved preview=actual full-bodies boundary overlap terrain whole-formation-fallback deterministic");
    }

    private static void VerifyEnemyBodies(Vector2I[] cells, float[] radii, IBattleFloorRuleRuntime floor, string context)
    {
        for (var index = 0; index < cells.Length; index++)
        {
            var position = BattlefieldSpace.CellCenter(cells[index]);
            Require(position.X - radii[index] >= BattlefieldLayout.Width / 2f - BattlefieldSpace.CellHalfExtent &&
                BattlefieldSpace.IsPositionTerrainClear(position, radii[index], BattlefieldLayout.Width,
                    BattlefieldLayout.Height, floor.CanOccupy), context + " full body stays in enemy half and clear arena");
            for (var other = 0; other < index; other++)
                Require(position.DistanceTo(BattlefieldSpace.CellCenter(cells[other])) >=
                    radii[index] + radii[other] + BattlefieldSpace.BodyClearance,
                    context + " pairwise body clearance");
        }
    }

    private sealed class DeploymentBlockedFloor(Vector2I blocked)
        : ClearFloorRuleRuntime("deployment_blocked_fixture", "部署地形夹具", "只检查禁行地形。")
    {
        public override bool CanOccupy(Vector2I cell) => cell != blocked;
    }

    private static BattleConfig CaptureConfiguration(BattleConfig source, ulong seed) => new()
    {
        CaptureDetailedStatistics = true, Seed = seed, Identity = source.Identity,
        MatrixStatusRules = source.MatrixStatusRules, FloorRule = source.FloorRule,
        Spawns = source.Spawns, HeroRule = source.HeroRule, Modifiers = source.Modifiers,
        Summons = source.Summons, EmptyDeploymentSlots = source.EmptyDeploymentSlots,
        StartingGold = source.StartingGold, Relics = source.Relics, RelicSummons = source.RelicSummons,
        Equipment = source.Equipment, Traits = source.Traits, TacticalCommands = source.TacticalCommands,
        TacticalSummons = source.TacticalSummons, BossTimeline = source.BossTimeline,
        AdditionalBossTimelines = source.AdditionalBossTimelines,
        ConfigureCombatBindings = source.ConfigureCombatBindings
    };

    private static TeamSummary Summarize(BattleResult result, BattlePreparationSnapshot initial, int team)
    {
        var units = result.Units.Where(unit => unit.Team == team).ToArray();
        var permanent = units.Where(unit => !unit.IsTemporary).ToArray();
        var temporary = units.Where(unit => unit.IsTemporary).ToArray();
        return new(team, permanent.Length, temporary.Length,
            permanent.Count(unit => unit.Alive), temporary.Count(unit => unit.Alive),
            permanent.Sum(unit => (double)unit.FinalHealth), temporary.Sum(unit => (double)unit.FinalHealth),
            initial.Units.Where(unit => unit.Team == team && !unit.IsTemporary).Sum(unit => (double)unit.MaxHealth),
            initial.Units.Where(unit => unit.Team == team && unit.IsTemporary).Sum(unit => (double)unit.MaxHealth),
            permanent.Sum(unit => (double)unit.MaxHealth), temporary.Sum(unit => (double)unit.MaxHealth),
            units.Sum(unit => (double)unit.DamageDealt), units.Sum(unit => (double)unit.DetailedStatistics!.HealthDamageDealt),
            units.Sum(unit => (double)unit.DetailedStatistics!.ShieldDamageDealt), units.Sum(unit => (double)unit.DetailedStatistics!.OverkillDamageDealt),
            units.Sum(unit => (double)unit.DamageTaken), units.Sum(unit => (double)unit.HealingDone),
            units.Sum(unit => (double)unit.DetailedStatistics!.HealingWasted), units.Sum(unit => (double)unit.ShieldAbsorbed),
            units.Sum(unit => (double)unit.DetailedStatistics!.ShieldGranted),
            units.Sum(unit => unit.DetailedStatistics!.HardControlTicksTaken) * BattleTiming.TickSeconds,
            permanent.Sum(unit => unit.DetailedStatistics!.HardControlTicksTaken) * BattleTiming.TickSeconds,
            temporary.Sum(unit => unit.DetailedStatistics!.HardControlTicksTaken) * BattleTiming.TickSeconds,
            units.Sum(unit => unit.DetailedStatistics!.LegacyActionLockTicks) * BattleTiming.TickSeconds,
            units.Sum(unit => unit.DetailedStatistics!.ManaCasts),
            permanent.Sum(unit => unit.DetailedStatistics!.ManaCasts), temporary.Sum(unit => unit.DetailedStatistics!.ManaCasts),
            units.Sum(unit => unit.DetailedStatistics!.AbilityResolutions));
    }

    private static void VerifyStatistics(BattleResult result)
    {
        foreach (var unit in result.Units)
        {
            var detail = unit.DetailedStatistics!;
            Near(unit.DamageDealt, detail.HealthDamageDealt + (double)detail.ShieldDamageDealt,
                unit.RuntimeId + " effective damage decomposition");
            Near(unit.DamageTaken, detail.HealthDamageTaken + (double)unit.ShieldAbsorbed,
                unit.RuntimeId + " incoming damage decomposition");
            Near(detail.HealthDamageDealt, detail.DamageSources.Sum(source => (double)source.HealthDamage),
                unit.RuntimeId + " health source total");
            Near(detail.ShieldDamageDealt, detail.DamageSources.Sum(source => (double)source.ShieldDamage),
                unit.RuntimeId + " shield source total");
            Near(detail.OverkillDamageDealt, detail.DamageSources.Sum(source => (double)source.OverkillDamage),
                unit.RuntimeId + " overkill source total");
            Require(detail.HardControlTicksTaken >= 0 && detail.HardControlTicksTaken <= result.Ticks,
                unit.RuntimeId + " unique recipient control bound");
        }
    }

    private static void Near(double actual, double expected, string context) => Require(
        double.IsFinite(actual) && double.IsFinite(expected) &&
        Math.Abs(actual - expected) <= Math.Max(.01, Math.Max(Math.Abs(actual), Math.Abs(expected)) * .00001),
        context + $" actual={actual} expected={expected}");

    private static object InitialUnit(ContentRegistry content, BattlePreparedUnit unit) => new
    {
        unit.RuntimeId, unit.SourceInstanceId, unit.Team, unit.IsTemporary,
        unit.Definition.ContentId, unit.Definition.DisplayName,
        ScenePath = Entry(content, unit.Definition.ContentId).Scene.ResourcePath,
        DefinitionPath = Entry(content, unit.Definition.ContentId).Definition.ResourcePath,
        Cell = new { unit.Cell.X, unit.Cell.Y }, Position = new { unit.Position.X, unit.Position.Y },
        unit.BodyRadius, unit.Health, unit.Shield, unit.CurrentMana, unit.MaxMana,
        Attributes = unit.Attributes.Values, unit.MaxHealth, AttackDamage = unit.Damage,
        unit.Armor, AttackSpeed = unit.Attributes.GetValue(CombatAttribute.AttackSpeed),
        SpellPower = unit.Attributes.GetValue(CombatAttribute.SpellPower),
        AbilityIds = unit.Definition.AbilityLoadout?.Abilities.Select(ability => ability.StableId).ToArray() ?? [],
        TraitIds = unit.Definition.TraitContributions.IsDefault ? [] :
            unit.Definition.TraitContributions.Select(trait => trait.TraitId).ToArray()
    };

    private static object ReportUnit(BattleUnitReportSnapshot unit) => new
    {
        unit.RuntimeId, unit.SourceInstanceId, unit.ContentId, unit.DisplayName, unit.Team,
        unit.IsHero, unit.IsTemporary, unit.Alive, unit.FinalHealth, unit.MaxHealth, unit.FinalShield,
        unit.DamageDealt, unit.DamageTaken, unit.ShieldAbsorbed, unit.HealingDone, unit.Kills,
        unit.JoinTick, unit.DefeatTick, unit.AttackActions, unit.EffectiveHealingEvents, unit.ActiveTicks,
        FinalCell = new { unit.FinalCell.X, unit.FinalCell.Y },
        FinalPosition = new { unit.FinalPosition.X, unit.FinalPosition.Y }, unit.DetailedStatistics
    };

    private static CatalogEntry Entry(ContentRegistry content, string id) => content.TryGet(id, out var entry)
        ? entry : throw new InvalidOperationException("Missing current content: " + id);

    private static string ProfilePath(CompiledExpeditionEncounter profile)
    {
        var path = "res://content/expedition/current/" + profile.StableId.Replace("_profile", "", StringComparison.Ordinal) + ".tres";
        Require(GD.Load<ExpeditionEncounterDefinition>(path)?.StableId == profile.StableId,
            "output must cite the actual matching authored profile resource");
        return path;
    }

    private static string[] Notes() =>
    [
        "Controlled population snapshots; selected lineups have not been obtained from actual opening/recruitment/discovery offers.",
        "Equal authored initial population budget; 2/4/6/8 selected heroes, zero permanent stat growth, equipment, relics, spells and ascension benefits.",
        "Ordinary/elite profiles belong to biomes; Boss profiles belong to stages. These test checkpoints do not lock biome access to a stage.",
        "Flower currently has fixed +4 attack/+40 health cultivation, but this tool does not simulate acquisition or action settlement; snapshots remain unassigned with zero permanent growth, and no 4% multiplier is fabricated.",
        "Hard control is the sum of recipient unique occupied ticks in target-seconds; source rows can overlap and are not extra duration.",
        "Temporary actors are separate; permanent health ratios use final maximum health (including actual transformations).",
        "Fixed seeds and placement permit same-content reruns. Automated results do not establish natural acquisition, natural completion rate, fun or user acceptance."
    ];

    private static JsonSerializerOptions CreateJsonOptions(bool indent)
    {
        var options = new JsonSerializerOptions { WriteIndented = indent };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static Options ParseOptions(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var deploymentOnly = args.Contains("--deployment-only");
        var remembered = args.Contains("--b3-remembered");
        var v1Effects = args.Contains("--v1-probes");
        var v1Content = args.Contains("--v1-content");
        var v1Economy = args.Contains("--v1-economy");
        var v1RunItems = args.Contains("--v1-run-items");
        var v1Sustain = args.Contains("--v1-sustain-probe");
        var v1EconomyGaps = args.Contains("--v1-economy-gaps");
        var v1 = v1Content || v1Effects || v1Economy || v1RunItems || v1Sustain || v1EconomyGaps;
        Require(args.Count(argument => argument == "--v1-economy-gaps") <= 1 && !(v1EconomyGaps && (v1Content || v1Effects || v1Economy || v1RunItems || v1Sustain)), "exclusive economy-gaps mode");
        Require(args.Count(argument => argument == "--v1-sustain-probe") <= 1 && !(v1Sustain && (v1Content || v1Effects || v1Economy || v1RunItems)), "exclusive sustain probe mode");
        Require(args.Count(argument => argument == "--v1-run-items") <= 1 && !(v1RunItems && (v1Content || v1Effects || v1Economy)), "exclusive run-items mode");
        Require(args.Count(argument => argument == "--v1-economy") <= 1 && !(v1Economy && (v1Content || v1Effects)), "exclusive economy mode");
        Require(args.Count(argument => argument == "--v1-probes") <= 1 && !(v1Content && v1Effects), "exclusive v1 calibration modes");
        Require(args.Count(argument => argument == "--deployment-only") <= 1, "duplicate deployment-only argument");
        Require(args.Count(argument => argument == "--b3-remembered") <= 1 && !(deploymentOnly && remembered), "exclusive calibration modes");
        Require(args.Count(argument => argument == "--v1-content") <= 1 && !(v1 && (remembered || deploymentOnly)), "exclusive v1 mode");
        args = args.Where(argument => argument != "--deployment-only" && argument != "--b3-remembered" && argument != "--v1-content" && argument != "--v1-probes" && argument != "--v1-economy" && argument != "--v1-run-items" && argument != "--v1-sustain-probe" && argument != "--v1-economy-gaps").ToArray();
        for (var i = 0; i < args.Length; i += 2)
        {
            Require(i + 1 < args.Length && new[] { "--out", "--seeds", "--only-probe", "--only-count", "--only-profile", "--b3-replay", "--v1-replay" }.Contains(args[i]),
                "expected --out/--seeds/--only-probe/--only-count/--only-profile followed by a value");
            Require(values.TryAdd(args[i], args[i + 1]), "duplicate calibration argument");
        }
        var output = values.GetValueOrDefault("--out", "res://outputs/expedition-pve-20261006/final");
        output = output.StartsWith("res://", StringComparison.Ordinal) || output.StartsWith("user://", StringComparison.Ordinal)
            ? ProjectSettings.GlobalizePath(output) : Path.GetFullPath(output);
        var seeds = values.GetValueOrDefault("--seeds", v1 ? "20261006,20261007" : remembered ? "20261006" : "20261006,1776,9173").Split(',').Select(ulong.Parse).ToArray();
        Require(seeds.Length is > 0 and <= 20 && seeds.Distinct().Count() == seeds.Length, "1..20 distinct seeds");
        var probe = values.GetValueOrDefault("--only-probe");
        Require(probe is null || (v1 ? V1Probes.Any(candidate => candidate.Id == probe) : Probes.Any(candidate => candidate.Id == probe)), "unknown probe");
        int? count = values.TryGetValue("--only-count", out var text) ? int.Parse(text) : null;
        Require(count is null || new[] { 2, 4, 6, 8 }.Contains(count.Value), "unknown population snapshot");
        var replay = values.GetValueOrDefault("--b3-replay");
        Require(replay is null || remembered, "b3 replay requires --b3-remembered");
        Require(!remembered || (probe is null && count is null && !values.ContainsKey("--only-profile") && seeds.Length <= 2),
            "B3 fixes three groups/four heroes/gloam elite and accepts only one or two battle seeds");
        var v1Replay = values.GetValueOrDefault("--v1-replay");
        Require(v1Replay is null || v1, "v1 replay requires --v1-content, --v1-probes or --v1-economy");
        Require(!v1Economy || (v1Replay is not null && probe is null), "v1 economy requires one --v1-replay checkpoint file and no --only-probe");
        Require(!v1RunItems || (v1Replay is not null && probe is null), "run-items requires eighteen-source --v1-replay directory");
        Require(!v1EconomyGaps || (v1Replay is not null && probe is null), "economy gaps require current source directory");
        Require(!v1Sustain || (v1Replay is not null && probe is null), "sustain probe requires current --v1-replay directory and fixed a_ranged stage3 U04");
        Require(!v1Effects || v1Replay is not null, "v1 effect probes require previously generated --v1-replay checkpoints");
        Require(!v1 || (count is null && !values.ContainsKey("--only-profile") &&
            seeds.SequenceEqual(new ulong[] { 20261006, 20261007 })), "v1 fixes three stages/profiles and two preregistered seeds");
        return new(output, seeds, probe, count, values.GetValueOrDefault("--only-profile"), deploymentOnly, remembered, replay, v1Content, v1Replay, v1Effects, v1Economy, v1RunItems, v1Sustain, v1EconomyGaps);
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
