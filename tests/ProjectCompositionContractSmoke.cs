using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

public partial class ProjectCompositionContractSmoke : Node
{
    public override async void _Ready()
    {
        var code = await RunAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GetTree().Quit(code);
    }

    private async Task<int> RunAsync()
    {
        try
        {
            var authored = GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres");
            var compiled = ExpectValid(authored, "production project");
            VerifyProductionValues(compiled);
            VerifyLegacyDeterministicParity(compiled);
            VerifyTransactionalRejection(authored);
            VerifySourceBoundaries();
            await VerifyComposableFixture(authored);
            GD.Print("PROJECT_COMPOSITION_CONTRACT_OK production=3x5,rules,pools,encounters,timelines " +
                     "determinism=legacy-parity invalid=category,reference,collision,encounter,timeline,node " +
                     "extension=campaign,pool,encounter,no-center-edit timeline=distinct-loadout-effect-authority " +
                     "layers=domain-project-run-battle-one-way screens=local-controller");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("PROJECT_COMPOSITION_CONTRACT_FAILED: " + exception);
            return 1;
        }
    }

    private static void VerifyProductionValues(CompiledGameProject project)
    {
        Equal(project.StableId, "project_my_team_alpha", "project id");
        Equal(project.Campaign.StableId, "campaign_alpha_tower", "campaign id");
        Equal(project.Campaign.Regions.Length, 3, "region count");
        Equal(project.Campaign.FloorsPerRegion, 5, "floors per region");
        Equal(project.Campaign.TotalFloors, 15, "total floors");
        Equal(project.Campaign.NodeTable.BossLocalFloor, 4, "boss local floor");
        Equal(project.Campaign.NodeTable.RegularOptionCount, 3, "regular option count");
        Equal(project.Campaign.NodeTable.RotationStride, 2, "rotation stride");
        Equal(project.Campaign.NodeTable.FloorSeedStride, 7, "floor seed stride");
        if (!project.Campaign.NodeTable.Rotation.SequenceEqual(new[]
            {
                TowerNodeType.Combat, TowerNodeType.Recruitment, TowerNodeType.Event,
                TowerNodeType.Elite, TowerNodeType.Shop, TowerNodeType.Rest
            }))
            throw new InvalidOperationException("node rotation values changed");
        Equal(project.Campaign.StarterPool.ContentIds.Length, project.Content.Soldiers.Count, "starter pool coverage");
        Equal(project.Campaign.RecruitmentPool.ContentIds.Length, project.Content.Soldiers.Count, "recruitment pool coverage");
        var relicIds = project.Content.Items
            .Where(entry => entry.Definition is ItemDefinition { ProductKind: ItemProductKind.Relic })
            .Select(entry => entry.StableId)
            .ToHashSet(StringComparer.Ordinal);
        var equipmentIds = project.Content.Items
            .Where(entry => entry.Definition is ItemDefinition { ProductKind: ItemProductKind.Equipment })
            .Select(entry => entry.StableId)
            .ToHashSet(StringComparer.Ordinal);
        Equal(relicIds.Count, 12, "relic catalog count");
        Equal(equipmentIds.Count, 2, "equipment catalog count including the production Frost slice");
        Equal(project.Campaign.ItemRewardPool.ContentIds.Length, relicIds.Count, "relic reward pool coverage");
        Equal(project.Campaign.ShopPool.ContentIds.Length, relicIds.Count, "relic shop pool coverage");
        if (!project.Campaign.ItemRewardPool.ContentIds.All(relicIds.Contains) ||
            !project.Campaign.ShopPool.ContentIds.All(relicIds.Contains) ||
            project.Campaign.ItemRewardPool.ContentIds.Any(equipmentIds.Contains) ||
            project.Campaign.ShopPool.ContentIds.Any(equipmentIds.Contains))
            throw new InvalidOperationException("ordinary Relic pools contain non-Relic Equipment content");

        var rules = project.RunRules;
        Equal(rules.OrdinaryPopulationCap, 10, "ordinary population cap");
        Equal(rules.PhysicalDeploymentCeiling, 18, "physical deployment ceiling");
        Equal(rules.ReserveCapacity, 3, "reserve capacity");
        Equal(rules.StarterRosterHeroCount, 3, "starter roster hero count");
        Equal(rules.InitialPopulation, 7, "compatibility initial population");
        Equal(rules.EquipmentSlotCapacity, 3, "equipment slot capacity");
        Equal(rules.RecruitmentChoiceCount, 3, "recruitment choices");
        Equal(rules.ItemChoiceCount, 3, "item choices");
        Equal(rules.StartingGold, 16, "starting gold");
        Equal(rules.NormalBattleGold, 7, "normal reward");
        Equal(rules.EliteBattleGold, 12, "elite reward");
        Equal(rules.BossBattleGold, 18, "boss reward");
        Equal(rules.VictoryHeroRecovery, .12f, "hero victory recovery");
        Equal(rules.VictorySoldierRecovery, .15f, "soldier victory recovery");
        Equal(rules.MinimumVictoryHeroHealth, .15f, "minimum hero health");
        Equal(rules.MinimumLivingSoldierHealth, .1f, "minimum living soldier health");
        Equal(rules.DefeatedSoldierHealth, .25f, "defeated soldier health");
        Equal(rules.RiskyEventSuccessGold, 18, "risky gold");
        Equal(rules.RiskyEventSuccessChance, .65f, "risky chance");
        Equal(rules.RiskyEventHealthLoss, .25f, "risky health loss");
        Equal(rules.RiskyEventMinimumHealth, .25f, "risky minimum health");
        Equal(rules.SafeEventGold, 6, "safe event gold");
        Equal(rules.RestHeroHealing, .35f, "rest hero healing");
        Equal(rules.RestSoldierHealing, .45f, "rest soldier healing");
        Equal(rules.RestGold, 8, "rest gold");
        Equal(rules.InitialUnlockedHeroCount, 3, "initial unlocked heroes");

        foreach (var region in project.Campaign.Regions)
        {
            foreach (var type in new[] { TowerNodeType.Combat, TowerNodeType.Elite, TowerNodeType.Boss })
                if (!region.Encounters.ContainsKey(type))
                    throw new InvalidOperationException($"{region.StableId} missing {type} encounter");
            var boss = region.Encounters[TowerNodeType.Boss];
            if (boss.BossTimeline is not { Phases.Length: > 0 } timeline ||
                timeline.BossContentId != boss.LeadEnemyId)
                throw new InvalidOperationException($"{region.StableId} boss timeline mismatch");
        }
    }

    private static void VerifyLegacyDeterministicParity(CompiledGameProject project)
    {
        var generator = new TowerGenerator(project.Campaign);
        foreach (var seed in new[] { 1UL, 1101UL, 2202UL, 3303UL, 0xDEADBEEFUL })
        for (var floor = 0; floor < 15; floor++)
        {
            var run = new ActiveRunDto { Seed = seed, FloorIndex = floor };
            ActiveRunFormationSchema.InitializeVersion4(run);
            var actualOptions = generator.Options(run);
            var expectedTypes = LegacyOptionTypes(seed, floor);
            if (!actualOptions.Select(option => option.Type).SequenceEqual(expectedTypes))
                throw new InvalidOperationException($"legacy option parity seed={seed} floor={floor}");
            foreach (var type in new[] { TowerNodeType.Combat, TowerNodeType.Elite, TowerNodeType.Boss })
            {
                var actual = generator.Encounter(run, type);
                var expected = LegacyEncounter(seed, floor, type);
                if (actual.Title != expected.Title || actual.FloorRuleId != expected.FloorRuleId ||
                    actual.IsBoss != expected.IsBoss || actual.IsElite != expected.IsElite ||
                    !actual.EnemyIds.SequenceEqual(expected.EnemyIds))
                    throw new InvalidOperationException(
                        $"legacy encounter parity seed={seed} floor={floor} type={type}");
            }
        }
    }

    private static void VerifyTransactionalRejection(GameProjectDefinition valid)
    {
        var campaign = valid.Campaign!;
        ExpectInvalid(Project(valid, Campaign(campaign,
            starter: new ContentPoolDefinition
            {
                StableId = "pool_invalid_category",
                Kind = ContentPoolKind.Item,
                ContentIds = [valid.Content!.Items[0].StableId]
            })), "expected Soldier", "pool category");

        ExpectInvalid(Project(valid, Campaign(campaign,
            starter: new ContentPoolDefinition
            {
                StableId = "pool_invalid_reference",
                Kind = ContentPoolKind.Soldier,
                ContentIds = ["soldier_missing_fixture"]
            })), "not valid Soldier", "pool reference");

        ExpectInvalid(new GameProjectDefinition
        {
            StableId = "collision_fixture",
            Content = valid.Content,
            Campaign = Campaign(campaign, stableId: "collision_fixture"),
            RunRules = valid.RunRules,
            Presentation = valid.Presentation
        }, "collides", "stable-id collision");

        var baseRegion = campaign.Regions[0];
        var missingBoss = Region(baseRegion,
            baseRegion.Encounters.Where(value => value.NodeType != TowerNodeType.Boss).ToArray());
        ExpectInvalid(Project(valid, Campaign(campaign, regions: [missingBoss])),
            "missing Boss encounter", "encounter completeness");

        var boss = baseRegion.Encounters.Single(value => value.NodeType == TowerNodeType.Boss);
        var invalidTimeline = new BossTimelineDefinition
        {
            StableId = "timeline_invalid_fixture",
            BossContentId = valid.Content!.Enemies.First(entry => entry.StableId != boss.LeadEnemyId).StableId,
            Phases = boss.BossTimeline!.Phases
        };
        var invalidBoss = Encounter(boss, timeline: invalidTimeline);
        var timelineRegion = Region(baseRegion,
            baseRegion.Encounters.Select(value => value.NodeType == TowerNodeType.Boss ? invalidBoss : value).ToArray());
        ExpectInvalid(Project(valid, Campaign(campaign, regions: [timelineRegion])),
            "does not match", "boss timeline");

        var invalidTable = NodeTable(campaign.NodeTable!, bossLocalFloor: campaign.FloorsPerRegion);
        ExpectInvalid(Project(valid, Campaign(campaign, nodeTable: invalidTable)),
            "within FloorsPerRegion", "node table bounds");

        var belowStartingRoster = (RunRulesDefinition)valid.RunRules!.Duplicate();
        belowStartingRoster.InitialPopulation = belowStartingRoster.StarterRosterHeroCount;
        ExpectInvalid(Project(valid, campaign, runRules: belowStartingRoster),
            "initial population", "initial population below starting roster");
        var aboveOrdinaryCap = (RunRulesDefinition)valid.RunRules.Duplicate();
        aboveOrdinaryCap.InitialPopulation = aboveOrdinaryCap.OrdinaryPopulationCap + 1;
        ExpectInvalid(Project(valid, campaign, runRules: aboveOrdinaryCap),
            "initial population", "initial population above ordinary cap");
    }

    private async Task VerifyComposableFixture(GameProjectDefinition valid)
    {
        var alpha = valid.Campaign!;
        var baseRegion = alpha.Regions[0];
        var alphaCombat = baseRegion.Encounters.Single(value => value.NodeType == TowerNodeType.Combat);
        var fixtureEnemyPool = new ContentPoolDefinition
        {
            StableId = "pool_fixture_enemies",
            Kind = ContentPoolKind.Enemy,
            ContentIds = [alphaCombat.EnemyPool!.ContentIds[0]]
        };
        var fixtureCombat = Encounter(
            alphaCombat,
            stableId: "encounter_fixture_combat",
            enemyPool: fixtureEnemyPool);
        var fixtureRegion = new TowerRegionDefinition
        {
            Id = "region_fixture",
            DisplayName = "组合测试区",
            Description = "只由已存在原语组合。",
            AccentColor = Colors.CornflowerBlue,
            Encounters = baseRegion.Encounters
                .Select(value => value.NodeType == TowerNodeType.Combat ? fixtureCombat : value)
                .ToArray()
        };
        var fixtureCampaign = Campaign(
            alpha,
            stableId: "campaign_fixture",
            regions: [fixtureRegion]);
        var fixtureProject = Project(valid, fixtureCampaign, "project_fixture");
        var compiled = ExpectValid(fixtureProject, "composable fixture");
        var generator = new TowerGenerator(compiled.Campaign);
        var run = new ActiveRunDto { Seed = 741UL, FloorIndex = 0 };
        ActiveRunFormationSchema.InitializeVersion4(run);
        var encounter = generator.Encounter(run, TowerNodeType.Combat);
        Equal(encounter.EncounterId, "encounter_fixture_combat", "fixture encounter identity");
        if (encounter.EnemyIds.Any(id => id != fixtureEnemyPool.ContentIds[0]))
            throw new InvalidOperationException("fixture encounter ignored its authored enemy pool");

        var gate = await GamePackagePublisher.CreateReadyAsync(this, fixtureProject);
        var package = gate.Package ?? throw new InvalidOperationException(
            "fixture package: " + string.Join("; ", gate.Report.CoreErrors));
        var registry = package.Content;
        compiled = package.Project;
        var save = new MemorySave(valid.Content!.Heroes.Select(entry => entry.StableId));
        var app = new RunApplication(registry, save, compiled);
        if (!app.StartNewRun(valid.Content.Heroes[0].StableId, 741UL))
            throw new InvalidOperationException("fixture RunApplication rejected compiled project");
        app.ActiveRun!.SelectedNode = TowerNodeType.Combat;
        app.ActiveRun.PendingNode = true;
        var planned = app.CurrentEncounter();
        Equal(planned.EncounterId, "encounter_fixture_combat", "facade fixture encounter");
        var config = app.BuildBattleConfig(planned, false);
        if (config.Identity?.EncounterId != planned.EncounterId)
            throw new InvalidOperationException("fixture battle identity mismatch");
        using var simulation = new BattleSimulation(config);
        simulation.Step();
        VerifyBossTimelineRuntimeAuthority(valid, registry);
    }

    private static void VerifyBossTimelineRuntimeAuthority(
        GameProjectDefinition valid,
        ContentRegistry registry)
    {
        var campaign = valid.Campaign!;
        var sourceRegion = campaign.Regions[0];
        var sourceBoss = sourceRegion.Encounters.Single(value => value.NodeType == TowerNodeType.Boss);
        var opening = sourceBoss.BossTimeline!.Phases[0];
        var secondLoadout = campaign.Regions[1].Encounters
            .Single(value => value.NodeType == TowerNodeType.Boss)
            .BossTimeline!.Phases[0].AbilityLoadout ??
            throw new InvalidOperationException("second production Boss loadout missing");
        var shiftedTimeline = new BossTimelineDefinition
        {
            StableId = "timeline_phase_runtime_fixture",
            BossContentId = sourceBoss.LeadEnemyId,
            Phases =
            [
                opening,
                new BossPhaseDefinition
                {
                    StableId = "phase_runtime_fixture_second",
                    DisplayName = "阈值阶段",
                    StartHealthRatio = .5f,
                    AbilityLoadout = secondLoadout
                }
            ]
        };
        var shiftedBoss = Encounter(sourceBoss, timeline: shiftedTimeline);
        var shiftedRegion = Region(sourceRegion, sourceRegion.Encounters
            .Select(value => value.NodeType == TowerNodeType.Boss ? shiftedBoss : value)
            .ToArray());
        var project = ExpectValid(Project(valid, Campaign(campaign,
            stableId: "campaign_phase_runtime_fixture",
            regions: [shiftedRegion, .. campaign.Regions.Skip(1)]),
            "project_phase_runtime_fixture"), "boss phase runtime fixture");
        var app = new RunApplication(
            registry,
            new MemorySave(valid.Content!.Heroes.Select(entry => entry.StableId)),
            project);
        if (!app.StartNewRun(valid.Content.Heroes[0].StableId, 9441UL))
            throw new InvalidOperationException("boss phase runtime Run could not start");
        app.ActiveRun!.FloorIndex = project.Campaign.NodeTable.BossLocalFloor;
        app.ActiveRun.SelectedNode = TowerNodeType.Boss;
        app.ActiveRun.PendingNode = true;
        var encounter = app.CurrentEncounter();
        var config = app.BuildBattleConfig(encounter, false);
        if (config.BossTimeline is not { Phases.Length: 2 } timeline ||
            timeline.StableId != shiftedTimeline.StableId)
            throw new InvalidOperationException("compiled encounter timeline did not reach BattleConfig");
        var openingLoadout = timeline.Phases[0].AbilityLoadout;
        var thresholdLoadout = timeline.Phases[1].AbilityLoadout;
        if (openingLoadout is null || thresholdLoadout is null ||
            openingLoadout.Abilities.Select(value => value.StableId).SequenceEqual(
                thresholdLoadout.Abilities.Select(value => value.StableId)))
            throw new InvalidOperationException("Boss phase fixture did not retain two distinct non-null compiled loadouts");
        var bossSpawn = config.Spawns.Single(spawn => spawn.Unit.ContentId == timeline.BossContentId);
        if (bossSpawn.Unit.AbilityLoadout is not null)
            throw new InvalidOperationException("boss scene loadout remained a competing Battle runtime authority");
        var carrion = BattleSetupFactory.Snapshot(
            registry.Catalog.Enemies.Single(entry => entry.StableId == "enemy_carrion"),
            registry) with { Damage = 0 };
        var bossSpawnIndex = config.Spawns.FindIndex(spawn => spawn.Unit.ContentId == timeline.BossContentId);
        config.Spawns[bossSpawnIndex] = config.Spawns[bossSpawnIndex] with { BehaviorSummon = carrion };

        using (var openingBattle = new BattleSimulation(config))
        {
            foreach (var unit in openingBattle.Units) unit.Damage = 0;
            var boss = openingBattle.Units.Single(unit => unit.Definition.ContentId == timeline.BossContentId);
            if (boss.BossPhaseId != opening.StableId)
                throw new InvalidOperationException("opening Boss phase was not activated from the encounter timeline");
            for (var tick = 0; tick < 50; tick++) openingBattle.Step();
            if (Math.Abs(boss.Shield - 55f) > .001f)
                throw new InvalidOperationException("opening timeline loadout did not execute in Battle");
        }

        using (var shiftedBattle = new BattleSimulation(config))
        {
            foreach (var unit in shiftedBattle.Units) unit.Damage = 0;
            var boss = shiftedBattle.Units.Single(unit => unit.Definition.ContentId == timeline.BossContentId);
            var initialUnitCount = shiftedBattle.Units.Count;
            boss.Health = boss.MaxHealth * .4f;
            for (var tick = 0; tick < 60; tick++) shiftedBattle.Step();
            if (boss.BossPhaseId != "phase_runtime_fixture_second" || boss.Shield != 0 ||
                shiftedBattle.Units.Count <= initialUnitCount ||
                !shiftedBattle.Units.Any(unit => unit.IsTemporary && unit.Definition.ContentId == "enemy_carrion"))
                throw new InvalidOperationException(
                    "Boss health threshold did not replace and execute the distinct second phase loadout");
        }
    }

    private static void VerifySourceBoundaries()
    {
        var root = ProjectSettings.GlobalizePath("res://");
        var gameRoot = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "src", "App", "GameRoot.cs"));
        foreach (var forbidden in new[]
                 {
                     "GD.Load<", "alpha_catalog", "region_ember", "region_gloam", "region_crown",
                     "RunApplication.Risky", "RunApplication.Rest", "RunApplication.DeploymentCapacity",
                     "ChoiceCard", "HeroSelectionViewModel", "DeploymentUnitViewModel", "EncounterPlan",
                     "GetNode<Label>", "GetNode<Button>", "GetNode<Container>", "TowerNodeType"
                 })
            if (gameRoot.Contains(forbidden, StringComparison.Ordinal))
                throw new InvalidOperationException($"GameRoot retained composition authority: {forbidden}");
        if (System.IO.File.ReadAllLines(System.IO.Path.Combine(root, "src", "App", "GameRoot.cs")).Length > 100 ||
            !gameRoot.Contains("GameFlowCoordinator", StringComparison.Ordinal) ||
            !gameRoot.Contains("AppScreenHost", StringComparison.Ordinal))
            throw new InvalidOperationException("GameRoot is not a narrow bootstrap composition root");

        var flow = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "src", "App", "GameFlowCoordinator.cs"));
        foreach (var forbidden in new[] { "GetNode<", "Instantiate<ChoiceCard>", "HeroSelectionViewModel", "DeploymentUnitViewModel" })
            if (flow.Contains(forbidden, StringComparison.Ordinal))
                throw new InvalidOperationException($"flow coordinator retained screen-local binding: {forbidden}");
        foreach (var controller in new[]
                 {
                     "MainMenuScreenController", "TowerScreenController", "RewardScreenController",
                     "ShopScreenController", "EventScreenController", "RestScreenController",
                     "ResultScreenController", "SettingsScreenController"
                 })
            if (!System.IO.File.Exists(System.IO.Path.Combine(root, "src", "UI", controller + ".cs")))
                throw new InvalidOperationException($"screen-local controller is missing: {controller}");

        var facade = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "src", "Run", "RunApplication.cs"));
        foreach (var concreteId in new[] { "hero_", "soldier_", "enemy_", "item_", "rule_", "encounter_" })
            if (facade.Contains(concreteId, StringComparison.Ordinal))
                throw new InvalidOperationException($"Run facade dispatches concrete content: {concreteId}");
        foreach (var service in new[]
                 {
                     "RunFormationService", "RunNodeResolutionService", "RunBattlePreparationService",
                     "RunRewardEconomyService", "RunProgressionPersistenceService"
                 })
            if (!facade.Contains(service, StringComparison.Ordinal))
                throw new InvalidOperationException($"Run facade omitted cohesive service: {service}");

        var generator = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "src", "Run", "TowerGenerator.cs"));
        if (generator.Contains("TowerRegionDefinition", StringComparison.Ordinal) ||
            generator.Contains("switch", StringComparison.Ordinal) ||
            generator.Contains("enemy_", StringComparison.Ordinal) ||
            generator.Contains("rule_", StringComparison.Ordinal))
            throw new InvalidOperationException("TowerGenerator retained authored-content or node-description dispatch");

        var battle = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "src", "Battle", "BattleSimulation.cs"));
        foreach (var forbidden in new[]
                 {
                     "GameProjectDefinition", "CampaignDefinition", "EncounterDefinition",
                     "ContentPoolDefinition", "TowerRegionDefinition"
                 })
            if (battle.Contains(forbidden, StringComparison.Ordinal))
                throw new InvalidOperationException($"BattleSimulation gained project-composition authority: {forbidden}");

        foreach (var path in System.IO.Directory.GetFiles(System.IO.Path.Combine(root, "src", "Project"), "*.cs"))
        {
            var source = System.IO.File.ReadAllText(path);
            if (source.Contains("using TowerAutobattler.Run", StringComparison.Ordinal))
                throw new InvalidOperationException($"Project depends on Run: {System.IO.Path.GetFileName(path)}");
        }
        foreach (var path in System.IO.Directory.GetFiles(System.IO.Path.Combine(root, "src", "Battle"), "*.cs",
                     System.IO.SearchOption.AllDirectories))
        {
            var source = System.IO.File.ReadAllText(path);
            if (source.Contains("TowerAutobattler.Run", StringComparison.Ordinal))
                throw new InvalidOperationException($"Battle depends on Run: {System.IO.Path.GetFileName(path)}");
        }

        var region = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "src", "Project", "TowerRegionDefinition.cs"));
        foreach (var obsolete in new[] { "EnemyPool", "BossId", "BossFloorRuleId", "FloorRulePool" })
            if (region.Contains(obsolete, StringComparison.Ordinal))
                throw new InvalidOperationException($"region retained legacy authoring surface: {obsolete}");
        var formation = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "src", "Run", "RunFormationService.cs"));
        var persistence = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "src", "Run", "RunProgressionPersistenceService.cs"));
        if (formation.Contains("RunProgressionPersistenceService", StringComparison.Ordinal) ||
            persistence.Contains("RunFormationService", StringComparison.Ordinal) ||
            !persistence.Contains("RunFormationPolicy.Validate", StringComparison.Ordinal))
            throw new InvalidOperationException("formation and persistence services still form a responsibility cycle");
        var preparation = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "src", "Run", "RunBattlePreparationService.cs"));
        var models = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "src", "Battle", "BattleModels.cs"));
        if (!preparation.Contains("ToBattleTimeline", StringComparison.Ordinal) ||
            !models.Contains("BossTimelineSnapshot", StringComparison.Ordinal) ||
            !battle.Contains("SynchronizeBossPhase", StringComparison.Ordinal) ||
            !battle.Contains("ReplaceLoadout", StringComparison.Ordinal))
            throw new InvalidOperationException("Boss timeline is not an Encounter-to-Battle runtime authority");
    }

    private static GameProjectDefinition Project(
        GameProjectDefinition source,
        CampaignDefinition campaign,
        string stableId = "project_transaction_fixture",
        RunRulesDefinition? runRules = null) => new()
    {
        StableId = stableId,
        Content = source.Content,
        Campaign = campaign,
        RunRules = runRules ?? source.RunRules,
        Presentation = source.Presentation
    };

    private static CampaignDefinition Campaign(
        CampaignDefinition source,
        string? stableId = null,
        TowerRegionDefinition[]? regions = null,
        TowerNodeTableDefinition? nodeTable = null,
        ContentPoolDefinition? starter = null) => new()
    {
        StableId = stableId ?? source.StableId,
        FloorsPerRegion = source.FloorsPerRegion,
        Regions = regions ?? source.Regions,
        NodeTable = nodeTable ?? source.NodeTable,
        StarterPool = starter ?? source.StarterPool,
        RecruitmentPool = source.RecruitmentPool,
        ItemRewardPool = source.ItemRewardPool,
        ShopPool = source.ShopPool
    };

    private static TowerRegionDefinition Region(
        TowerRegionDefinition source,
        EncounterDefinition[] encounters) => new()
    {
        Id = source.Id,
        DisplayName = source.DisplayName,
        Description = source.Description,
        AccentColor = source.AccentColor,
        Encounters = encounters
    };

    private static EncounterDefinition Encounter(
        EncounterDefinition source,
        string? stableId = null,
        ContentPoolDefinition? enemyPool = null,
        BossTimelineDefinition? timeline = null) => new()
    {
        StableId = stableId ?? source.StableId,
        NodeType = source.NodeType,
        TitlePattern = source.TitlePattern,
        EnemyPool = enemyPool ?? source.EnemyPool,
        FloorRulePool = source.FloorRulePool,
        LeadEnemyId = source.LeadEnemyId,
        BaseEnemyCount = source.BaseEnemyCount,
        AddRegionIndexToCount = source.AddRegionIndexToCount,
        SeedSalt = source.SeedSalt,
        BossTimeline = timeline ?? source.BossTimeline
    };

    private static TowerNodeTableDefinition NodeTable(
        TowerNodeTableDefinition source,
        int bossLocalFloor) => new()
    {
        Nodes = source.Nodes,
        Rotation = source.Rotation,
        BossLocalFloor = bossLocalFloor,
        RegularOptionCount = source.RegularOptionCount,
        RotationStride = source.RotationStride,
        FloorSeedStride = source.FloorSeedStride
    };

    private static CompiledGameProject ExpectValid(GameProjectDefinition authored, string label)
    {
        var result = CompileProject(authored);
        return result.Project ?? throw new InvalidOperationException(
            $"{label} rejected: {string.Join("; ", result.Report.CoreErrors)}");
    }

    private static void ExpectInvalid(GameProjectDefinition authored, string diagnostic, string label)
    {
        var result = CompileProject(authored);
        if (result.Project is not null || !result.Report.CoreErrors.Any(error =>
                error.Contains(diagnostic, StringComparison.Ordinal)))
            throw new InvalidOperationException(
                $"{label} was not rejected transactionally with '{diagnostic}': " +
                string.Join("; ", result.Report.CoreErrors));
    }

    private static GameProjectCompilationResult CompileProject(GameProjectDefinition authored)
    {
        var content = ContentValidator.CompileProductionGraph(
            authored.Content,
            GameProjectCompiler.CollectAbilityLoadoutReferences(authored));
        if (content.Graph is null)
            return new GameProjectCompilationResult(null, content.Report);
        var project = GameProjectCompiler.Compile(authored, content.Graph);
        project.Report.Merge(content.Report);
        return project;
    }

    private static IReadOnlyList<TowerNodeType> LegacyOptionTypes(ulong seed, int floor)
    {
        if (floor % 5 == 4) return [TowerNodeType.Boss];
        var rotation = new[]
        {
            TowerNodeType.Combat, TowerNodeType.Recruitment, TowerNodeType.Event,
            TowerNodeType.Elite, TowerNodeType.Shop, TowerNodeType.Rest
        };
        var offset = (int)((seed + (ulong)floor * 7UL) % (ulong)rotation.Length);
        return Enumerable.Range(0, 3).Select(index => rotation[(offset + index * 2) % rotation.Length]).ToArray();
    }

    private static EncounterPlan LegacyEncounter(ulong seed, int floor, TowerNodeType type)
    {
        var regions = new[]
        {
            new LegacyRegion("余烬铸层",
                ["enemy_rust_guard", "enemy_crossbow", "enemy_cutpurse"],
                "enemy_boreal_boss", "rule_narrow_lanes", ["rule_clear", "rule_narrow_lanes"]),
            new LegacyRegion("幽暮墓层",
                ["enemy_hexer", "enemy_carrion", "enemy_blood_reaver"],
                "enemy_shadow_boss", "rule_hazard_pulse", ["rule_hazard_pulse", "rule_healing_beacon"]),
            new LegacyRegion("冠冕机层",
                ["enemy_scale_brute", "enemy_wyrm", "enemy_ice_blade", "enemy_ice_hawker"],
                "enemy_clockwork_boss", "rule_boss_ward",
                ["rule_narrow_lanes", "rule_hazard_pulse", "rule_healing_beacon"])
        };
        var regionIndex = Math.Clamp(floor / 5, 0, regions.Length - 1);
        var region = regions[regionIndex];
        var isBoss = type == TowerNodeType.Boss;
        var isElite = type == TowerNodeType.Elite;
        var enemies = new List<string>();
        if (isBoss) enemies.Add(region.BossId);
        var count = isBoss ? 3 + regionIndex : isElite ? 6 + regionIndex : 4 + regionIndex;
        var random = new DeterministicRandom(
            seed ^ (ulong)(floor + 1) * 0x9E3779B9UL ^ (isElite ? 17UL : 0UL));
        while (enemies.Count < count)
            enemies.Add(region.EnemyPool[random.NextInt(0, region.EnemyPool.Length)]);
        var rule = isBoss
            ? region.BossRule
            : region.FloorRules[random.NextInt(0, region.FloorRules.Length)];
        var title = isBoss
            ? $"{region.Name}层主战"
            : isElite ? $"{region.Name}精英战" : $"{region.Name}遭遇战";
        return new EncounterPlan(title, rule, enemies, isBoss, isElite);
    }

    private static void Equal<T>(T actual, T expected, string label) where T : IEquatable<T>
    {
        if (!actual.Equals(expected))
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    }

    private sealed record LegacyRegion(
        string Name,
        string[] EnemyPool,
        string BossId,
        string BossRule,
        string[] FloorRules);

    private sealed class MemorySave : IRunSaveService
    {
        private readonly MetaProgressDto _meta;
        private readonly SettingsDto _settings = new();
        private ActiveRunDto? _run;

        public MemorySave(IEnumerable<string> heroes) =>
            _meta = new MetaProgressDto { UnlockedHeroIds = heroes.ToList() };

        public MetaProgressDto LoadMeta() => _meta;
        public SettingsDto LoadSettings() => _settings;
        public ActiveRunDto? LoadActiveRun() => _run;
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) { _run = value; return true; }
        public void DeleteActiveRun() => _run = null;
    }
}
