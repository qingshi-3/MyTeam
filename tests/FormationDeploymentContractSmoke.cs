using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

public partial class FormationDeploymentContractSmoke : Node
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
            var gate = await TestProjectFixture.PublishAsync(this);
            var registry = gate.Package?.Content ?? throw new InvalidOperationException(
                "content gate: " + string.Join("; ", gate.Report.CoreErrors));
            VersionThreeMigrationAndExplicitRejection(registry);
            SchemaResidueNullOverflowAndDiagnostics(registry);
            LegacyNullAndPublicationFailureContracts(registry);
            RecruitAndNewRunPublicationAtomicity(registry);
            UnifiedFormationPopulationAndRollback(registry);
            HeroOwnedEquipmentPersistenceAndBattleProjection(registry);
            ExactBattleSpawnParity(registry);
            ResponsiveProjectionContract();
            await AuthoredUiContract(registry);
            GD.Print("FORMATION_DEPLOYMENT_CONTRACT_OK schema=v3-v4 roster=unified population=10-18 " +
                     "formation=18-cells,atomic migration=lossless-or-reject battle=exact persistent=group " +
                     "rollback=exact ui=authored-18");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("FORMATION_DEPLOYMENT_CONTRACT_FAILED: " + exception);
            return 1;
        }
    }

    private static void VersionThreeMigrationAndExplicitRejection(ContentRegistry registry)
    {
        const string saveNamespace = "tests/formation-schema-v4";
        var service = new SaveService(saveNamespace);
        service.DeleteActiveRun();
        var unlocked = registry.Catalog.Heroes.Select(entry => entry.StableId).ToList();
        service.SaveMeta(new MetaProgressDto { UnlockedHeroIds = unlocked, Victories = 7, HighestRegion = 2 });
        service.SaveSettings(new SettingsDto { MasterVolume = .37f, DefaultBattleSpeed = 1.5f });
        var project = TestProjectFixture.Load(registry);
        var legacyRoster = registry.Catalog.Soldiers.Take(3).Select((entry, index) =>
            new RosterHeroInstanceDto
            {
                InstanceId = $"legacy-unit-{index + 1}",
                ContentId = entry.StableId,
                HealthRatio = .71f + index * .03f,
                Rank = index + 1
            }).ToList();
        var legacy = new ActiveRunDto
        {
            Version = 3,
            Seed = 0xA11CEUL,
            LegacyHeroId = registry.Catalog.Heroes[0].StableId,
            LegacyHeroHealthRatio = .63f,
            Roster = legacyRoster,
            Deployment = [legacyRoster[0].InstanceId, legacyRoster[1].InstanceId, legacyRoster[2].InstanceId, "", "", ""],
            LegacyHeroCell = FormationCellDto.FromCell(new Vector2I(2, 5)),
            LegacyDeploymentCells = ActiveRunFormationSchema.CloneCells(BattlefieldLayout.Version2SoldierCells),
            Items =
            [
                JsonSerializer.Deserialize<ItemInstanceDto>(
                    $"{{\"InstanceId\":\"item-4\",\"ContentId\":\"{registry.Catalog.Items[0].StableId}\"," +
                    "\"Stacks\":2,\"Charges\":3,\"Roll\":9}") ??
                throw new InvalidOperationException("v3 item without Counters did not deserialize")
            ],
            Gold = 47,
            FloorIndex = 6,
            BattleNumber = 5,
            PendingNode = true,
            SelectedNode = TowerNodeType.Elite
        };
        Require(service.SaveActiveRun(legacy), "isolated v3 fixture save");

        var app = new RunApplication(registry, service, project);
        var migrated = app.ActiveRun ?? throw new InvalidOperationException("lossless v3 fixture rejected");
        Require(migrated.Version == 4 && migrated.Roster.Count == 4 &&
                migrated.Roster[0].ContentId == legacy.LegacyHeroId &&
                migrated.Roster[0].HealthRatio == legacy.LegacyHeroHealthRatio &&
                migrated.Roster[2].Rank == 2 && migrated.CurrentPopulation == 7 &&
                migrated.Roster.All(hero => hero.Equipment is { Count: 0 }),
            "v3 identities, order, health, rank, or population changed");
        Require(CellOf(migrated, migrated.Roster[0].InstanceId) == new Vector2I(2, 5),
            "v3 starting hero cell changed");
        for (var index = 0; index < legacyRoster.Count; index++)
            Require(CellOf(migrated, legacyRoster[index].InstanceId) == BattlefieldLayout.Version2SoldierCells[index],
                "v3 roster cell changed: " + index);
        Require(migrated.Items.Single().InstanceId == "item-4" && migrated.Items.Single().Charges == 3 &&
                migrated.Items.Single().Roll == 9 && migrated.Items.Single().Counters.Count == 0 &&
                migrated.Gold == 47 && migrated.FloorIndex == 6 &&
                migrated.BattleNumber == 5 && migrated.PendingNode && migrated.SelectedNode == TowerNodeType.Elite,
            "v3 item/relic or unrelated Run facts changed");
        var published = service.Serialize(service.LoadActiveRun());
        Require(!published.Contains("\"HeroId\"", StringComparison.Ordinal) &&
                !published.Contains("\"HeroHealthRatio\"", StringComparison.Ordinal) &&
                !published.Contains("\"HeroCell\"", StringComparison.Ordinal) &&
                !published.Contains("\"DeploymentCells\"", StringComparison.Ordinal),
            "v4 publication retained split schema fields");

        var staleDeployment = service.Deserialize<ActiveRunDto>(published)!;
        staleDeployment.Deployment[staleDeployment.Deployment.FindIndex(id => !string.IsNullOrEmpty(id))] =
            "stale-roster-instance";
        Require(service.SaveActiveRun(staleDeployment), "stale deployment fixture save");
        var staleRejected = new RunApplication(registry, service, project);
        Require(staleRejected.ActiveRun is null &&
                staleRejected.ActiveRunLoadDiagnostic is { Kind: ActiveRunLoadFailureKind.ValidationRejected },
            "deployment identity outside the roster was not explicitly rejected");

        var ambiguous = service.Deserialize<ActiveRunDto>(service.Serialize(legacy))!;
        ambiguous.LegacyDeploymentCells![1] = ambiguous.LegacyDeploymentCells[0].Clone();
        Require(service.SaveActiveRun(ambiguous), "ambiguous v3 fixture save");
        var rejected = new RunApplication(registry, service, project);
        Require(rejected.ActiveRun is null &&
                rejected.ActiveRunLoadDiagnostic is { Kind: ActiveRunLoadFailureKind.MigrationRejected },
            "ambiguous v3 formation was not explicitly diagnosed and rejected");
        var meta = service.LoadMeta();
        var settings = service.LoadSettings();
        Require(meta.Victories == 7 && meta.HighestRegion == 2 &&
                Math.Abs(settings.MasterVolume - .37f) < .0001f &&
                Math.Abs(settings.DefaultBattleSpeed - 1.5f) < .0001f,
            "active-run rejection damaged Meta or Settings");

        var largerRoster = service.Deserialize<ActiveRunDto>(service.Serialize(legacy))!;
        while (largerRoster.Roster.Count < 8)
        {
            var index = largerRoster.Roster.Count + 1;
            largerRoster.Roster.Add(new RosterHeroInstanceDto
            {
                InstanceId = $"legacy-reserve-{index}",
                ContentId = registry.Catalog.Soldiers[index % registry.Catalog.Soldiers.Count].StableId,
                HealthRatio = .8f
            });
        }
        Require(ActiveRunFormationSchema.TryMigrateToCurrent(largerRoster, project.RunRules) &&
                largerRoster.Roster.Count == 9 && largerRoster.CurrentPopulation == 9,
            "v3 migration did not preserve a roster larger than compatibility population");
        var overOrdinaryCap = service.Deserialize<ActiveRunDto>(service.Serialize(legacy))!;
        while (overOrdinaryCap.Roster.Count < 10)
        {
            var index = overOrdinaryCap.Roster.Count + 1;
            overOrdinaryCap.Roster.Add(new RosterHeroInstanceDto
            {
                InstanceId = $"legacy-over-cap-{index}",
                ContentId = registry.Catalog.Soldiers[index % registry.Catalog.Soldiers.Count].StableId
            });
        }
        var overCapSignature = service.Serialize(overOrdinaryCap);
        Require(!ActiveRunFormationSchema.TryMigrateToCurrent(overOrdinaryCap, project.RunRules) &&
                service.Serialize(overOrdinaryCap) == overCapSignature,
            "v3 roster above ordinary population cap was truncated or mutated");
        service.DeleteActiveRun();
    }

    private static void SchemaResidueNullOverflowAndDiagnostics(ContentRegistry registry)
    {
        const string saveNamespace = "tests/formation-schema-v4-invalid";
        var service = new SaveService(saveNamespace);
        service.DeleteActiveRun();
        var project = TestProjectFixture.Load(registry);
        var setup = new RunApplication(registry, service, project);
        Require(setup.StartNewRun("hero_banner_marshal", 404), "current-schema invalid fixture start");
        var relicId = registry.Catalog.Items.First(entry =>
            entry.Definition is ItemDefinition { ProductKind: ItemProductKind.Relic }).StableId;
        Require(setup.GrantItem(relicId), "current-schema Relic counter fixture acquisition");
        var validJson = service.Serialize(service.LoadActiveRun());

        void ExpectValidationRejected(string label, Action<ActiveRunDto> corrupt)
        {
            var fixture = service.Deserialize<ActiveRunDto>(validJson) ??
                throw new InvalidOperationException(label + " fixture clone failed");
            corrupt(fixture);
            Require(service.SaveActiveRun(fixture), label + " fixture save");
            var rejected = new RunApplication(registry, service, project);
            Require(rejected.ActiveRun is null &&
                    rejected.ActiveRunLoadDiagnostic is { Kind: ActiveRunLoadFailureKind.ValidationRejected },
                label + " was not rejected as current-schema validation failure");
        }

        ExpectValidationRejected("v4 legacy hero id residue", run => run.LegacyHeroId = "legacy");
        ExpectValidationRejected("v4 legacy hero health residue", run => run.LegacyHeroHealthRatio = .5f);
        ExpectValidationRejected("v4 legacy hero cell residue",
            run => run.LegacyHeroCell = FormationCellDto.FromCell(new Vector2I(0, 0)));
        ExpectValidationRejected("v4 legacy deployment cells residue",
            run => run.LegacyDeploymentCells = [FormationCellDto.FromCell(new Vector2I(0, 0))]);

        ExpectValidationRejected("v4 null roster", run => run.Roster = null!);
        ExpectValidationRejected("v4 null deployment", run => run.Deployment = null!);
        ExpectValidationRejected("v4 null items", run => run.Items = null!);
        ExpectValidationRejected("v4 null population sources", run => run.PopulationCapSources = null!);
        ExpectValidationRejected("v4 null roster entry", run => run.Roster[1] = null!);
        ExpectValidationRejected("v4 null hero Equipment", run => run.Roster[1].Equipment = null!);
        ExpectValidationRejected("v4 null Equipment entry", run => run.Roster[1].Equipment.Add(null!));
        ExpectValidationRejected("v4 null deployment entry", run => run.Deployment[0] = null!);
        ExpectValidationRejected("v4 null item entry", run => run.Items.Add(null!));
        ExpectValidationRejected("v4 null Relic counter collection", run => run.Items[0].Counters = null!);
        ExpectValidationRejected("v4 null Relic counter entry", run => run.Items[0].Counters.Add(null!));
        ExpectValidationRejected("v4 duplicate Relic counter id", run =>
        {
            run.Items[0].Counters.Add(new RelicCounterStateDto { CounterId = "duplicate", Value = 0 });
            run.Items[0].Counters.Add(new RelicCounterStateDto { CounterId = "duplicate", Value = 0 });
        });
        ExpectValidationRejected("v4 unknown or definition-mismatched Relic counter", run =>
            run.Items[0].Counters.Add(new RelicCounterStateDto { CounterId = "unknown", Value = 0 }));
        ExpectValidationRejected("v4 negative Relic counter", run =>
            run.Items[0].Counters.Add(new RelicCounterStateDto { CounterId = "unknown", Value = -1 }));
        ExpectValidationRejected("v4 null population-source entry", run => run.PopulationCapSources.Add(null!));
        var equipmentId = registry.Catalog.Items.Single(entry =>
            entry.StableId == "equipment_vanguard_insignia" &&
            entry.Definition is ItemDefinition { ProductKind: ItemProductKind.Equipment }).StableId;
        EquipmentInstanceState Equipment(ActiveRunDto run, int ownerIndex, int slot, string instanceId) => new()
        {
            InstanceId = instanceId,
            ContentId = equipmentId,
            OwnerHeroInstanceId = run.Roster[ownerIndex].InstanceId,
            SlotIndex = slot
        };
        ExpectValidationRejected("v4 Equipment owner mismatch", run =>
        {
            var equipment = Equipment(run, 0, 0, "equipment-owner-mismatch");
            equipment.OwnerHeroInstanceId = run.Roster[1].InstanceId;
            run.Roster[0].Equipment.Add(equipment);
        });
        ExpectValidationRejected("v4 duplicate Equipment slot", run =>
        {
            run.Roster[0].Equipment.Add(Equipment(run, 0, 0, "equipment-slot-a"));
            run.Roster[0].Equipment.Add(Equipment(run, 0, 0, "equipment-slot-b"));
        });
        ExpectValidationRejected("v4 fourth Equipment slot", run =>
            run.Roster[0].Equipment.Add(Equipment(run, 0, project.RunRules.EquipmentSlotCapacity,
                "equipment-fourth-slot")));
        ExpectValidationRejected("v4 unknown Equipment content", run =>
        {
            var equipment = Equipment(run, 0, 0, "equipment-unknown");
            equipment.ContentId = "equipment_missing";
            run.Roster[0].Equipment.Add(equipment);
        });
        ExpectValidationRejected("v4 Relic classified as Equipment", run =>
        {
            var equipment = Equipment(run, 0, 0, "equipment-relic-content");
            equipment.ContentId = relicId;
            run.Roster[0].Equipment.Add(equipment);
        });
        ExpectValidationRejected("v4 duplicate durable Equipment identity", run =>
        {
            run.Roster[0].Equipment.Add(Equipment(run, 0, 0, "equipment-duplicate"));
            run.Roster[1].Equipment.Add(Equipment(run, 1, 0, "equipment-duplicate"));
        });
        ExpectValidationRejected("v4 overflowing population sources", run =>
        {
            run.PopulationCapSources =
            [
                new PopulationCapSourceDto { SourceId = "overflow-a", Amount = int.MaxValue },
                new PopulationCapSourceDto { SourceId = "overflow-b", Amount = int.MaxValue }
            ];
            var facts = RunPopulationPolicy.Evaluate(run, project.RunRules);
            Require(!facts.IsValid && !string.IsNullOrWhiteSpace(facts.RejectionReason),
                "overflowing population sources did not produce invalid facts");
        });
        service.DeleteActiveRun();
    }

    private static void LegacyNullAndPublicationFailureContracts(ContentRegistry registry)
    {
        const string saveNamespace = "tests/formation-schema-v3-invalid";
        var service = new SaveService(saveNamespace);
        service.DeleteActiveRun();
        var project = TestProjectFixture.Load(registry);

        void ExpectMigrationRejected(int version, string label, Action<ActiveRunDto> corrupt)
        {
            var fixture = CreateLegacyRun(registry, version);
            corrupt(fixture);
            var before = service.Serialize(fixture);
            Require(service.SaveActiveRun(fixture), label + " fixture save");
            var rejected = new RunApplication(registry, service, project);
            Require(rejected.ActiveRun is null &&
                    rejected.ActiveRunLoadDiagnostic is { Kind: ActiveRunLoadFailureKind.MigrationRejected },
                label + " was not rejected as legacy migration failure");
            Require(service.Serialize(service.LoadActiveRun()) == before,
                label + " mutated the rejected legacy save");
        }

        void ExpectDirectMigrationRejected(int version, string label, Action<ActiveRunDto> corrupt)
        {
            var fixture = CreateLegacyRun(registry, version);
            corrupt(fixture);
            var before = RunSignature(fixture);
            Require(!ActiveRunFormationSchema.TryMigrateToCurrent(fixture) &&
                    RunSignature(fixture) == before,
                label + " was not rejected before mutation");
        }

        foreach (var version in new[] { 2, 3 })
        {
            ExpectMigrationRejected(version, $"v{version} null roster", run => run.Roster = null!);
            ExpectMigrationRejected(version, $"v{version} null deployment", run => run.Deployment = null!);
            ExpectMigrationRejected(version, $"v{version} null items", run => run.Items = null!);
            ExpectMigrationRejected(version, $"v{version} null population sources",
                run => run.PopulationCapSources = null!);
            ExpectMigrationRejected(version, $"v{version} null roster entry", run => run.Roster[0] = null!);
            ExpectMigrationRejected(version, $"v{version} null roster Equipment",
                run => run.Roster[0].Equipment = null!);
            ExpectDirectMigrationRejected(version, $"v{version} direct null roster Equipment",
                run => run.Roster[0].Equipment = null!);
            ExpectDirectMigrationRejected(version, $"v{version} direct non-empty roster Equipment", run =>
                run.Roster[0].Equipment.Add(new EquipmentInstanceState
                {
                    InstanceId = "legacy-equipment-1",
                    ContentId = "equipment_vanguard_insignia",
                    OwnerHeroInstanceId = run.Roster[0].InstanceId,
                    SlotIndex = 0
                }));
            ExpectMigrationRejected(version, $"v{version} null deployment entry", run => run.Deployment[0] = null!);
            ExpectMigrationRejected(version, $"v{version} null item entry", run => run.Items[0] = null!);
        }
        ExpectMigrationRejected(3, "v3 null hero cell", run => run.LegacyHeroCell = null);
        ExpectMigrationRejected(3, "v3 null deployment-cell list", run => run.LegacyDeploymentCells = null);
        ExpectMigrationRejected(3, "v3 null deployment-cell entry",
            run => run.LegacyDeploymentCells![0] = null!);
        ExpectMigrationRejected(3, "v3 invalid roster content after shape migration",
            run => run.Roster[0].ContentId = "missing_legacy_content");
        service.DeleteActiveRun();

        var publicationSave = new MigrationPublicationFailingSaveService(
            registry,
            CreateLegacyRun(registry, 3));
        var originalSignature = publicationSave.StoredRunSignature;
        var publicationRejected = new RunApplication(registry, publicationSave, project);
        Require(publicationRejected.ActiveRun is null &&
                publicationRejected.ActiveRunLoadDiagnostic is
                    { Kind: ActiveRunLoadFailureKind.MigrationPublicationFailed } &&
                publicationSave.ActiveRunSaveCalls == 1 &&
                publicationSave.AttemptedPublication is
                    { Version: ActiveRunFormationSchema.CurrentVersion },
            "failed migration publication was not diagnosed exactly once");
        Require(publicationSave.StoredRunSignature == originalSignature &&
                publicationSave.StoredRun.Version == 3 &&
                publicationSave.StoredRun.LegacyHeroId is not null,
            "failed migration publication changed the original v3 save");
        Require(publicationRejected.Meta.Victories == 7 && publicationRejected.Meta.HighestRegion == 2 &&
                Math.Abs(publicationRejected.Settings.MasterVolume - .37f) < .0001f &&
                Math.Abs(publicationRejected.Settings.DefaultBattleSpeed - 1.5f) < .0001f,
            "failed migration publication changed Meta or Settings");
    }

    private static void RecruitAndNewRunPublicationAtomicity(ContentRegistry registry)
    {
        var project = TestProjectFixture.Load(registry);
        var save = new CountingRunSaveService(registry);
        var app = new RunApplication(registry, save, project);
        Require(app.StartNewRun("hero_banner_marshal", 505), "recruit atomicity run start");
        var run = app.ActiveRun!;
        var recruitId = registry.Catalog.Soldiers[0].StableId;
        var expectedInstanceId = $"roster-hero-{run.Roster
            .Select(hero => ParseInstanceSuffix(hero.InstanceId))
            .Concat(run.Items.Select(item => ParseInstanceSuffix(item.InstanceId)))
            .Max() + 1}";
        var before = RunSignature(run);
        var saveCalls = save.ActiveRunSaveCalls;
        save.FailActiveRunSaves = true;
        Require(!app.Recruit(recruitId) && save.ActiveRunSaveCalls == saveCalls + 1 &&
                RunSignature(run) == before,
            "failed recruit did not preserve the complete authoritative Run");
        save.FailActiveRunSaves = false;
        Require(app.Recruit(recruitId) && save.ActiveRunSaveCalls == saveCalls + 2 &&
                run.Roster.Last().InstanceId == expectedInstanceId,
            "failed recruit consumed the next deterministic instance id");

        var overflowSave = new CountingRunSaveService(registry);
        var overflowApp = new RunApplication(registry, overflowSave, project);
        Require(overflowApp.StartNewRun("hero_banner_marshal", 506), "instance overflow fixture start");
        var overflowRun = overflowApp.ActiveRun!;
        var previousId = overflowRun.Roster[1].InstanceId;
        const string maximumId = "roster-hero-2147483647";
        overflowRun.Roster[1].InstanceId = maximumId;
        overflowRun.Deployment[overflowRun.Deployment.IndexOf(previousId)] = maximumId;
        var overflowSignature = RunSignature(overflowRun);
        var overflowSaveCalls = overflowSave.ActiveRunSaveCalls;
        Require(!overflowApp.Recruit(recruitId) && overflowSave.ActiveRunSaveCalls == overflowSaveCalls &&
                RunSignature(overflowRun) == overflowSignature,
            "maximum instance suffix escaped as an exception or mutated the Run");

        var failedCreateSave = new CountingRunSaveService(registry) { FailActiveRunSaves = true };
        var failedCreateApp = new RunApplication(registry, failedCreateSave, project);
        Require(!failedCreateApp.StartNewRun("hero_banner_marshal", 607) &&
                failedCreateApp.ActiveRun is null && failedCreateSave.ActiveRunSaveCalls == 1,
            "failed new-run publication leaked an Active Run");
        failedCreateSave.FailActiveRunSaves = false;
        Require(failedCreateApp.StartNewRun("hero_banner_marshal", 607),
            "new run did not recover after failed publication");
        var recoveredIds = failedCreateApp.ActiveRun!.Roster.Select(hero => hero.InstanceId).ToArray();

        var cleanCreateSave = new CountingRunSaveService(registry);
        var cleanCreateApp = new RunApplication(registry, cleanCreateSave, project);
        Require(cleanCreateApp.StartNewRun("hero_banner_marshal", 607) &&
                recoveredIds.SequenceEqual(cleanCreateApp.ActiveRun!.Roster.Select(hero => hero.InstanceId)),
            "failed new-run publication consumed deterministic instance ids");
    }

    private static void UnifiedFormationPopulationAndRollback(ContentRegistry registry)
    {
        var save = new CountingRunSaveService(registry);
        var app = new RunApplication(registry, save, TestProjectFixture.Load(registry));
        Require(app.StartNewRun("hero_banner_marshal", 808), "unified run start");
        var run = app.ActiveRun!;
        var facts = app.Population!;
        Require(run.Roster.Count == 4 && run.Roster[0].ContentId == "hero_banner_marshal" &&
                run.Deployment.Count == 18 && facts.CurrentPopulation == 7 &&
                facts.OrdinaryPopulationCap == 10 && facts.EffectivePopulationCap == 10 &&
                facts.PhysicalDeploymentCeiling == 18 && facts.DeployedPersistentHeroes == 4,
            "new run did not publish unified roster with compatibility 7/10/18 facts");

        var clear = new ClearFloorRuleRuntime("clear", "常规", "test");
        var first = run.Roster[0].InstanceId;
        var second = run.Roster[1].InstanceId;
        ExpectOneSave(save, () => app.ApplyFormationCommand(
            FormationMoveCommand.RosterHero(first, new Vector2I(0, 0)), clear), "roster hero move");
        ExpectOneSave(save, () => app.ApplyFormationCommand(
            FormationMoveCommand.RosterHero(first, CellOf(run, second)), clear), "roster hero swap");

        var recruitId = registry.Catalog.Soldiers.First(entry =>
            run.Roster.All(hero => hero.ContentId != entry.StableId)).StableId;
        for (var index = 0; index < 3; index++)
        {
            Require(app.Recruit(recruitId), "recruit toward initial population");
            var deployedRecruit = run.Roster.Last().InstanceId;
            var deploySlot = run.Deployment.FindIndex(string.IsNullOrEmpty);
            ExpectOneSave(save, () => app.ApplyFormationCommand(FormationMoveCommand.RosterHero(
                deployedRecruit, BattlefieldLayout.PlayerDeploymentCells[deploySlot]), clear),
                "deploy through initial population");
        }
        Require(app.Recruit(recruitId), "reserve roster hero recruit");
        var reserve = run.Roster.Last().InstanceId;
        var emptyCell = BattlefieldLayout.PlayerDeploymentCells.First(cell =>
            run.Deployment[BattlefieldLayout.PlayerDeploymentSlot(cell)].Length == 0);
        var rejected = app.EvaluateFormationCommand(FormationMoveCommand.RosterHero(reserve, emptyCell), clear);
        Require(!rejected.IsValid && rejected.Population is { CurrentPopulation: 7, DeployedPersistentHeroes: 7 },
            "population-full evaluation lacked direct population facts");
        var blockedSaves = save.ActiveRunSaveCalls;
        var blockedSignature = JsonSerializer.Serialize(run.Deployment);
        var blocked = new BlockingFloorRule(emptyCell);
        Require(!app.ApplyFormationCommand(FormationMoveCommand.RosterHero(reserve, emptyCell), blocked) &&
                save.ActiveRunSaveCalls == blockedSaves &&
                JsonSerializer.Serialize(run.Deployment) == blockedSignature,
            "floor-blocked formation command mutated or persisted state");
        var occupiedId = run.Deployment.First(id => !string.IsNullOrEmpty(id));
        ExpectOneSave(save, () => app.ApplyFormationCommand(
            FormationMoveCommand.RosterHero(reserve, CellOf(run, occupiedId)), clear), "reserve replacement");

        Require(app.GrantPopulation(3) && app.Population is { CurrentPopulation: 10, EffectivePopulationCap: 10 },
            "ordinary population grant/cap changed");
        var cappedSignature = PopulationSignature(run);
        var cappedSaves = save.ActiveRunSaveCalls;
        Require(!app.GrantPopulation(1) && PopulationSignature(run) == cappedSignature &&
                save.ActiveRunSaveCalls == cappedSaves,
            "failed ordinary-cap grant mutated or persisted state");
        Require(app.GrantPopulationFromSource("population-test-source", 8, 8) &&
                app.Population is { CurrentPopulation: 18, EffectivePopulationCap: 18 },
            "explicit above-cap source or physical clamp changed");
        var sourceSignature = PopulationSignature(run);
        var sourceSaves = save.ActiveRunSaveCalls;
        Require(!app.GrantPopulationFromSource("population-test-source", 0, 1) &&
                PopulationSignature(run) == sourceSignature && save.ActiveRunSaveCalls == sourceSaves,
            "duplicate above-cap source mutated or persisted state");
        while (run.Roster.Count < 18)
            Require(app.Recruit(registry.Catalog.Soldiers[run.Roster.Count % registry.Catalog.Soldiers.Count].StableId),
                "recruit toward physical ceiling");
        foreach (var hero in run.Roster.Where(hero => !run.Deployment.Contains(hero.InstanceId)).ToArray())
        {
            var slot = run.Deployment.FindIndex(string.IsNullOrEmpty);
            ExpectOneSave(save, () => app.ApplyFormationCommand(FormationMoveCommand.RosterHero(
                hero.InstanceId, BattlefieldLayout.PlayerDeploymentCells[slot]), clear), "deploy through cell 18");
        }
        Require(run.Deployment.All(id => !string.IsNullOrEmpty(id)) && app.Population!.DeployedPersistentHeroes == 18,
            "physical 18-cell formation did not fill atomically");

        var signature = JsonSerializer.Serialize(run.Deployment);
        save.FailActiveRunSaves = true;
        var moved = run.Deployment[0];
        Require(!app.ApplyFormationCommand(FormationMoveCommand.RosterHero(
                    moved, BattlefieldLayout.PlayerDeploymentCells[1]), clear) &&
                JsonSerializer.Serialize(run.Deployment) == signature,
            "failed formation save did not restore exact 18-cell state");
        save.FailActiveRunSaves = false;
    }

    private static void ExactBattleSpawnParity(ContentRegistry registry)
    {
        var save = new CountingRunSaveService(registry);
        var app = new RunApplication(registry, save, TestProjectFixture.Load(registry));
        Require(app.StartNewRun("hero_banner_marshal", 1001), "spawn parity run start");
        var run = app.ActiveRun!;
        run.SelectedNode = TowerNodeType.Combat;
        run.PendingNode = true;
        var encounter = app.CurrentEncounter();
        var config = app.BuildBattleConfig(encounter);
        var playerSpawns = config.Spawns.Where(spawn => spawn.Team == 0).ToArray();
        Require(playerSpawns.Length == run.Deployment.Count(id => !string.IsNullOrEmpty(id)) &&
                playerSpawns.All(spawn => spawn.IsPersistentRosterHero == true),
            "battle setup did not classify every deployed roster hero as persistent");
        foreach (var spawn in playerSpawns)
            Require(spawn.Cell == CellOf(run, spawn.InstanceId), "battle spawn cell changed: " + spawn.InstanceId);
    }

    private static void HeroOwnedEquipmentPersistenceAndBattleProjection(ContentRegistry registry)
    {
        var project = TestProjectFixture.Load(registry);
        var save = new CountingRunSaveService(registry);
        var app = new RunApplication(registry, save, project);
        Require(app.StartNewRun("hero_banner_marshal", 909), "Equipment Run start");
        var run = app.ActiveRun!;
        Require(project.RunRules.EquipmentSlotCapacity == 3 &&
                run.Roster.All(hero => hero.Equipment is { Count: 0 }),
            "new Run did not publish empty authored three-slot Equipment ownership");
        var equipmentId = registry.Catalog.Items.Single(entry =>
            entry.StableId == "equipment_vanguard_insignia" &&
            entry.Definition is ItemDefinition { ProductKind: ItemProductKind.Equipment }).StableId;
        var relicId = registry.Catalog.Items.First(entry =>
            entry.Definition is ItemDefinition { ProductKind: ItemProductKind.Relic }).StableId;
        var firstId = run.Roster[0].InstanceId;
        var secondId = run.Roster[1].InstanceId;
        var reserveId = run.Roster[2].InstanceId;

        ExpectOneSave(save, () => app.EquipItem(firstId, 0, equipmentId),
            "first hero Equipment equip");
        ExpectOneSave(save, () => app.EquipItem(secondId, 0, equipmentId),
            "second hero same-definition Equipment equip");
        var firstInstance = run.Roster.Single(hero => hero.InstanceId == firstId).Equipment.Single().InstanceId;
        var secondInstance = run.Roster.Single(hero => hero.InstanceId == secondId).Equipment.Single().InstanceId;
        Require(firstInstance != secondInstance &&
                run.Roster.Single(hero => hero.InstanceId == firstId).Equipment.Single().OwnerHeroInstanceId == firstId &&
                run.Roster.Single(hero => hero.InstanceId == secondId).Equipment.Single().OwnerHeroInstanceId == secondId,
            "same-definition Equipment did not retain isolated owner/instance identity");
        ExpectOneSave(save, () => app.EquipItem(firstId, 2, equipmentId),
            "authored third Equipment slot equip");
        ExpectOneSave(save, () => app.RemoveEquipment(firstId, 2),
            "authored third Equipment slot remove");

        var rejectedSignature = RunSignature(run);
        var rejectedSaves = save.ActiveRunSaveCalls;
        Require(!app.EquipItem("temporary-or-missing-owner", 0, equipmentId) &&
                !app.EquipItem(firstId, project.RunRules.EquipmentSlotCapacity, equipmentId) &&
                !app.EquipItem(firstId, 1, relicId) &&
                !app.EquipItem(firstId, 1, "equipment_missing") &&
                save.ActiveRunSaveCalls == rejectedSaves && RunSignature(run) == rejectedSignature,
            "invalid/non-roster Equipment ownership mutated or persisted the Run");

        save.FailActiveRunSaves = true;
        var failedSignature = RunSignature(run);
        var failedSaves = save.ActiveRunSaveCalls;
        Require(!app.EquipItem(firstId, 0, equipmentId) &&
                save.ActiveRunSaveCalls == failedSaves + 1 && RunSignature(run) == failedSignature,
            "failed Equipment replacement mutated authoritative Run state");
        Require(!app.RemoveEquipment(firstId, 0) &&
                save.ActiveRunSaveCalls == failedSaves + 2 && RunSignature(run) == failedSignature,
            "failed Equipment removal mutated authoritative Run state");
        save.FailActiveRunSaves = false;
        ExpectOneSave(save, () => app.EquipItem(firstId, 0, equipmentId),
            "Equipment replacement after save recovery");
        Require(run.Roster.Single(hero => hero.InstanceId == firstId).Equipment.Single().InstanceId != firstInstance &&
                run.Roster.Single(hero => hero.InstanceId == firstId).Equipment.Single().InstanceId != secondInstance,
            "Equipment replacement reused an existing instance identity");
        ExpectOneSave(save, () => app.EquipItem(reserveId, 0, equipmentId),
            "reserve hero Equipment equip");
        ExpectOneSave(save, () => app.WithdrawDeploymentUnit(reserveId),
            "reserve hero withdrawal");

        var reloaded = new RunApplication(registry, save, project);
        var persisted = reloaded.ActiveRun ?? throw new InvalidOperationException("Equipment Run did not reload");
        Require(persisted.Roster[0].Equipment.Single().OwnerHeroInstanceId == persisted.Roster[0].InstanceId &&
                persisted.Roster[1].Equipment.Single().OwnerHeroInstanceId == persisted.Roster[1].InstanceId &&
                persisted.Roster[2].Equipment.Single().OwnerHeroInstanceId == persisted.Roster[2].InstanceId,
            "Equipment JSON clone/load lost owner, slot, or instance state");
        persisted.SelectedNode = TowerNodeType.Combat;
        persisted.PendingNode = true;
        var encounter = reloaded.CurrentEncounter();
        var config = reloaded.BuildBattleConfig(encounter);
        var deployedEquipmentOwners = config.Equipment.Instances
            .Select(instance => instance.OwnerHeroInstanceId).ToHashSet(StringComparer.Ordinal);
        Require(config.Equipment.Instances.Length == 2 &&
                deployedEquipmentOwners.SetEquals(new[]
                    { persisted.Roster[0].InstanceId, persisted.Roster[1].InstanceId }) &&
                !deployedEquipmentOwners.Contains(persisted.Roster[2].InstanceId),
            "Battle preparation projected reserve or omitted deployed hero Equipment");

        using var simulation = new BattleSimulation(config);
        foreach (var instance in config.Equipment.Instances)
        {
            var unit = simulation.Units.Single(candidate => candidate.SourceInstanceId == instance.OwnerHeroInstanceId);
            var spawn = config.Spawns.Single(candidate => candidate.InstanceId == instance.OwnerHeroInstanceId);
            Require(Math.Abs(unit.Armor - (spawn.Unit.Armor + 12)) < .0001f,
                "production Equipment did not project only to its runtime owner");
        }
        simulation.Abort();
        Require(simulation.EquipmentTransition is
                {
                    Reason: EquipmentBattleCompletionReason.Abort,
                    RemainingInstances: 0,
                    RemainingModifierHandles: 0,
                    RemainingSubscriptions: 0
                },
            "Battle completion retained Equipment instances, modifier handles, or subscriptions");
    }

    private static void ResponsiveProjectionContract()
    {
        var wide = BattlefieldProjection.Fit(new Vector2(1244, 600));
        var compact = BattlefieldProjection.Fit(new Vector2(900, 430));
        Require(wide.CellPitch.X >= 108f && wide.CellPitch.Y >= 82f &&
                compact.CellPitch.X >= 87f && compact.CellPitch.Y >= 67f,
            "responsive board pitch changed");
        Require(BattlefieldLayout.PlayerDeploymentCells.Length == 18 &&
                BattlefieldLayout.PlayerDeploymentCells.Distinct().Count() == 18,
            "canonical player cell map changed");
    }

    private async Task AuthoredUiContract(ContentRegistry registry)
    {
        var save = new CountingRunSaveService(registry);
        var app = new RunApplication(registry, save, TestProjectFixture.Load(registry));
        Require(app.StartNewRun("hero_banner_marshal", 1717), "authored UI run start");
        var run = app.ActiveRun!;
        run.SelectedNode = TowerNodeType.Combat;
        run.PendingNode = true;
        var encounter = app.CurrentEncounter();
        var deployment = GD.Load<PackedScene>("res://scenes/ui/DeploymentScreen.tscn")
            .Instantiate<DeploymentScreenController>();
        AddChild(deployment);
        deployment.Bind(app, encounter);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var board = deployment.GetNode<DeploymentBoard>("%DeploymentBoard");
        var cells = board.GetChildren().OfType<DeploymentCell>().ToArray();
        var cards = deployment.GetNode<VBoxContainer>("%RosterChoices").GetChildren()
            .OfType<DeploymentUnitCard>().ToArray();
        Require(cells.Length == 18 && cells.All(cell => cell.FocusMode == Control.FocusModeEnum.All) &&
                cards.Length == run.Roster.Count,
            "authored deployment UI did not expose all cells/roster heroes");
        deployment.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static ActiveRunDto CreateLegacyRun(ContentRegistry registry, int version)
    {
        var roster = registry.Catalog.Soldiers.Take(2).Select((entry, index) =>
            new RosterHeroInstanceDto
            {
                InstanceId = $"legacy-unit-{index + 1}",
                ContentId = entry.StableId,
                HealthRatio = .8f - index * .1f,
                Rank = index + 1
            }).ToList();
        return new ActiveRunDto
        {
            Version = version,
            Seed = 0xC0FFEEUL,
            LegacyHeroId = registry.Catalog.Heroes[0].StableId,
            LegacyHeroHealthRatio = .61f,
            LegacyHeroCell = version == 3
                ? FormationCellDto.FromCell(new Vector2I(2, 5))
                : null,
            LegacyDeploymentCells = version == 3
                ? ActiveRunFormationSchema.CloneCells(BattlefieldLayout.Version2SoldierCells)
                : null,
            Roster = roster,
            Deployment = [roster[0].InstanceId, "", "", "", "", ""],
            Items =
            [
                JsonSerializer.Deserialize<ItemInstanceDto>(
                    $"{{\"InstanceId\":\"item-3\",\"ContentId\":\"{registry.Catalog.Items[0].StableId}\",\"Stacks\":1}}") ??
                throw new InvalidOperationException("legacy item without Counters did not deserialize")
            ],
            FloorIndex = 1,
            Gold = 23
        };
    }

    private static string RunSignature(ActiveRunDto run) => JsonSerializer.Serialize(run);

    private static int ParseInstanceSuffix(string instanceId)
    {
        var separator = instanceId.LastIndexOf('-');
        return separator >= 0 && int.TryParse(instanceId[(separator + 1)..], out var value) ? value : 0;
    }

    private static Vector2I CellOf(ActiveRunDto run, string instanceId)
    {
        var slot = run.Deployment.IndexOf(instanceId);
        return slot >= 0 ? BattlefieldLayout.PlayerDeploymentCells[slot] : new Vector2I(-1, -1);
    }

    private static string PopulationSignature(ActiveRunDto run) =>
        $"{run.CurrentPopulation}|" + string.Join(';', run.PopulationCapSources.Select(source =>
            $"{source.SourceId}:{source.Amount}"));

    private static void ExpectOneSave(CountingRunSaveService save, Func<bool> action, string label)
    {
        var before = save.ActiveRunSaveCalls;
        Require(action() && save.ActiveRunSaveCalls == before + 1, label + " did not save exactly once");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class BlockingFloorRule(Vector2I blocked)
        : ClearFloorRuleRuntime("blocked", "阻挡", "test")
    {
        public override bool CanOccupy(Vector2I cell) => cell != blocked;
    }

    private sealed class CountingRunSaveService : IRunSaveService
    {
        private readonly JsonSerializerOptions _json = new();
        private readonly MetaProgressDto _meta;
        private readonly SettingsDto _settings = new();
        private ActiveRunDto? _run;

        public CountingRunSaveService(ContentRegistry registry) =>
            _meta = new MetaProgressDto
            {
                UnlockedHeroIds = registry.Catalog.Heroes.Select(entry => entry.StableId).ToList()
            };

        public bool FailActiveRunSaves { get; set; }
        public int ActiveRunSaveCalls { get; private set; }
        public MetaProgressDto LoadMeta() => _meta;
        public SettingsDto LoadSettings() => _settings;
        public ActiveRunDto? LoadActiveRun() => Clone(_run);
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value)
        {
            ActiveRunSaveCalls++;
            if (FailActiveRunSaves) return false;
            _run = Clone(value);
            return true;
        }
        public void DeleteActiveRun() => _run = null;

        private ActiveRunDto? Clone(ActiveRunDto? value) => value is null
            ? null
            : JsonSerializer.Deserialize<ActiveRunDto>(JsonSerializer.Serialize(value, _json), _json);
    }

    private sealed class MigrationPublicationFailingSaveService : IRunSaveService
    {
        private readonly JsonSerializerOptions _json = new();
        private readonly MetaProgressDto _meta;
        private readonly SettingsDto _settings = new()
        {
            MasterVolume = .37f,
            DefaultBattleSpeed = 1.5f
        };

        public MigrationPublicationFailingSaveService(ContentRegistry registry, ActiveRunDto storedRun)
        {
            StoredRun = Clone(storedRun);
            _meta = new MetaProgressDto
            {
                UnlockedHeroIds = registry.Catalog.Heroes.Select(entry => entry.StableId).ToList(),
                Victories = 7,
                HighestRegion = 2
            };
        }

        public ActiveRunDto StoredRun { get; }
        public string StoredRunSignature => JsonSerializer.Serialize(StoredRun, _json);
        public ActiveRunDto? AttemptedPublication { get; private set; }
        public int ActiveRunSaveCalls { get; private set; }
        public MetaProgressDto LoadMeta() => _meta;
        public SettingsDto LoadSettings() => _settings;
        public ActiveRunDto? LoadActiveRun() => Clone(StoredRun);
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value)
        {
            ActiveRunSaveCalls++;
            AttemptedPublication = Clone(value);
            return false;
        }
        public void DeleteActiveRun() { }

        private ActiveRunDto Clone(ActiveRunDto value) =>
            JsonSerializer.Deserialize<ActiveRunDto>(JsonSerializer.Serialize(value, _json), _json) ??
            throw new InvalidOperationException("migration publication fixture clone failed");
    }
}
