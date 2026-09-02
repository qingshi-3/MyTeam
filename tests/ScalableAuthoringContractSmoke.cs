using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Composition;
using TowerAutobattler.Domain;
using TowerAutobattler.Effects;
using TowerAutobattler.Equipment;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;
using TowerAutobattler.Run;
using TowerAutobattler.Statuses;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.Traits;

public partial class ScalableAuthoringContractSmoke : Node
{
    private const string Root = "res://tests/fixtures/phase5";

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
            var fixture = LoadFixture();
            VerifyDeepFingerprintSensitivity();
            VerifyEquipmentReactiveCompilationContracts();
            VerifyCompiledRelicPolicyFingerprintSensitivity();
            var before = ResourceFingerprint(fixture);

            var gate = await GamePackagePublisher.CreateAuthoredReadyAsync(this, fixture.Package);
            var publication = gate.Package ?? throw new InvalidOperationException(
                "phase5 authored package rejected: " + string.Join("; ", gate.Report.CoreErrors));
            var registry = publication.Content;
            var project = publication.Project;
            var compiledBefore = CompiledPackageFingerprint(publication);
            Expect(!gate.Report.HasCoreErrors && publication.PublicationVersion == 1 &&
                   registry.Catalog.AllEntries().Count == 6 &&
                   registry.Graph.Abilities.Length == fixture.Abilities.Count &&
                   registry.Graph.Statuses.Length == fixture.Statuses.Count &&
                   registry.Graph.Relics.Length == fixture.Relics.Count &&
                   registry.Graph.Equipment.Length == fixture.Equipment.Count &&
                   registry.Graph.Traits.Length == fixture.Traits.Count,
                "valid authored package did not publish one complete compiled game package");

            await VerifySharedAbilityPackageAsync(fixture);
            await VerifyIndependentSceneLifecycleAsync(registry);
            VerifyDeterministicRuntimeAndIsolation(registry, project);
            VerifyReactiveRelicPersistenceAtomicity(registry, fixture.Project);
            VerifyVersionTwoMigration(registry, project);
            await VerifyTransactionalRejectionAsync(fixture);
            VerifyEffectDependencyTransaction();
            VerifySourceGuards();

            Expect(ResourceFingerprint(fixture) == before,
                "authored unit/item/ability/status/relic/project resources mutated during two runtime instances");
            Expect(CompiledPackageFingerprint(publication) == compiledBefore,
                "published compiled content/project graph mutated during runtime execution");

            GD.Print("SCALABLE_AUTHORING_CONTRACT_OK package=hero,soldier,boss,item,ability,status,relic,equipment,floor,encounter,campaign " +
                     "publication=atomic-invalid-ability-project-timeline,shared-ability-canonical boss=two-distinct-loadouts-effects " +
                     "fingerprint=authored-compiled-deep diagnostics=path-operation lifecycle=zero " +
                     "isolation=two-runtime deterministic=true timing=domain migration=v2-v3 " +
                     "compatibility=hero-command-and-item-provider-removed centers=zero-id-dispatch");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("SCALABLE_AUTHORING_CONTRACT_FAILED: " + exception);
            return 1;
        }
    }

    private static Fixture LoadFixture()
    {
        var catalog = Required<ContentCatalog>($"{Root}/fixture_catalog.tres");
        AbilityLoadoutDefinition[] loadouts =
        [
            Required<AbilityLoadoutDefinition>($"{Root}/abilities/loadout_phase5_hero.tres"),
            Required<AbilityLoadoutDefinition>($"{Root}/abilities/loadout_phase5_boss.tres"),
            Required<AbilityLoadoutDefinition>($"{Root}/abilities/loadout_phase5_boss_second.tres")
        ];
        AbilityDefinition[] abilities =
        [
            Required<AbilityDefinition>($"{Root}/abilities/ability_phase5_focus.tres"),
            Required<AbilityDefinition>($"{Root}/abilities/ability_phase5_boss_shield.tres"),
            Required<AbilityDefinition>($"{Root}/abilities/ability_phase5_boss_second_shield.tres")
        ];
        StatusDefinition[] statuses =
        [
            Required<StatusDefinition>($"{Root}/statuses/status_phase5_focus.tres")
        ];
        RelicDefinition[] relics =
        [
            Required<RelicDefinition>($"{Root}/relics/item_phase5_fixture.tres"),
            Required<RelicDefinition>($"{Root}/relics/item_phase5_reactive_fixture.tres")
        ];
        EquipmentDefinition[] equipment =
        [
            Required<EquipmentDefinition>($"{Root}/equipment/equipment_phase5_fixture.tres")
        ];
        TraitDefinition[] traits =
        [
            Required<TraitDefinition>($"{Root}/traits/trait_phase5_guard.tres")
        ];
        var project = Required<GameProjectDefinition>($"{Root}/project/project_phase5_fixture.tres");
        return new Fixture(
            new AuthoredContentPackage(project, catalog, loadouts, abilities, statuses, relics)
            {
                Equipment = equipment,
                Traits = traits,
                TacticalCommands =
                [
                    Required<TacticalCommandDefinition>($"{Root}/tactical/tactical_phase5_focus.tres"),
                    Required<TacticalCommandDefinition>($"{Root}/tactical/tactical_phase5_reserve.tres")
                ],
                TacticalCommandScenes =
                [
                    Required<PackedScene>($"{Root}/tactical/TacticalPhase5Focus.tscn"),
                    Required<PackedScene>($"{Root}/tactical/TacticalPhase5Reserve.tscn")
                ]
            },
            project,
            loadouts,
            abilities,
            statuses,
            relics,
            equipment,
            traits);
    }

    private async Task VerifySharedAbilityPackageAsync(Fixture fixture)
    {
        var sharedAbility = fixture.Abilities.Single(ability =>
            ability.StableId == "ability_phase5_boss_second_shield");
        var sharedLoadout = new AbilityLoadoutDefinition { Abilities = [sharedAbility] };
        var sharedProject = ProjectWithRegisteredSharedBossPhase(fixture.Project, sharedLoadout);
        var sharedPackage = new AuthoredContentPackage(
            sharedProject,
            fixture.Package.Catalog,
            fixture.Loadouts.Append(sharedLoadout).ToArray(),
            fixture.Abilities,
            fixture.Statuses,
            fixture.Relics)
            {
                Equipment = fixture.Equipment,
                Traits = fixture.Traits,
                TacticalCommands = fixture.Package.TacticalCommands,
                TacticalCommandScenes = fixture.Package.TacticalCommandScenes
            };
        var gate = await GamePackagePublisher.CreateAuthoredReadyAsync(this, sharedPackage);
        var publication = gate.Package ?? throw new InvalidOperationException(
            "shared Ability package rejected: " + string.Join("; ", gate.Report.CoreErrors));
        var originalLoadout = publication.Content.Graph.ResolveLoadout(fixture.Loadouts[2]);
        var reusedLoadout = publication.Content.Graph.ResolveLoadout(sharedLoadout);
        var originalAbility = originalLoadout.Find(sharedAbility.StableId);
        var reusedAbility = reusedLoadout.Find(sharedAbility.StableId);
        Expect(!gate.Report.HasCoreErrors &&
               publication.Content.Graph.Abilities.Length == fixture.Abilities.Count &&
               publication.Content.Graph.AbilityLoadouts.Length == fixture.Loadouts.Count + 1 &&
               originalAbility is not null && ReferenceEquals(originalAbility, reusedAbility),
            "complete package did not retain one canonical compiled record for a shared Ability Resource");

        var worldA = new SharedAbilityWorld();
        var worldB = new SharedAbilityWorld();
        using var scopeA = new BattleAbilityScope("shared_package_a", worldA, 0);
        using var scopeB = new BattleAbilityScope("shared_package_b", worldB, 0);
        scopeA.RegisterLoadout("owner", originalLoadout);
        scopeB.RegisterLoadout("owner", reusedLoadout);
        Expect(scopeA.ActivateAutomatic("owner", 1).Single().Succeeded && worldA.CommitCount == 1,
            "first runtime owner could not execute the shared canonical Ability");
        Expect(scopeA.ActivateAutomatic("owner", 1).Single().Failure == AbilityActivationFailure.UsageLimit &&
               worldA.CommitCount == 1,
            "first runtime owner did not retain its own maximum-use state");
        Expect(scopeB.ActivateAutomatic("owner", 1).Single().Succeeded && worldB.CommitCount == 1 &&
               scopeA.LiveRuntimeInstanceCount == 1 && scopeB.LiveRuntimeInstanceCount == 1,
            "shared canonical Ability leaked runtime state between owners");
    }

    private async Task VerifyIndependentSceneLifecycleAsync(ContentRegistry registry)
    {
        foreach (var entry in registry.Catalog.AllEntries())
        {
            var instance = entry.Scene.Instantiate();
            AddChild(instance);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            try
            {
                switch (instance)
                {
                    case UnitContentRoot unit:
                        Expect(!unit.ValidateAuthoring().HasCoreErrors, entry.StableId + " independent unit authoring");
                        unit.Bind("lifecycle-" + entry.StableId, 0, 50, 100);
                        unit.Activate(new UnitBindingContext(new RandomProbe(), new EventProbe(), new CommandProbe()));
                        Expect(unit.IsActive && unit.LifecycleState == ContentLifecycleState.Active,
                            entry.StableId + " did not enter active lifecycle");
                        unit.Deactivate();
                        Expect(!unit.IsActive && unit.LifecycleState == ContentLifecycleState.Bound,
                            entry.StableId + " did not leave active lifecycle");
                        break;
                    case ItemContentRoot item:
                        Expect(!item.ValidateAuthoring().HasCoreErrors, entry.StableId + " independent item authoring");
                        if (item.Definition.ProductKind == ItemProductKind.Equipment)
                        {
                            var equipment = registry.Graph.ResolveEquipment(entry.StableId);
                            Expect(item.Equipment is not null && item.Relic is null &&
                                   ReferenceEquals(equipment, registry.Graph.Equipment.Single()),
                                "Equipment item did not resolve its canonical compiled definition");
                            break;
                        }
                        using (var relicScope = new RelicRunScope(new RelicRunKey(9, "hero_phase5_fixture", 0, 0)))
                        {
                            var relic = registry.Graph.ResolveRelic(entry.StableId);
                            item.Bind(new ItemInstanceState
                            {
                                InstanceId = "lifecycle-item",
                                ContentId = entry.StableId,
                                Stacks = 1,
                                Counters = RelicRunScope.InitialRunCounters(relic).ToList()
                            });
                            item.Activate(new ItemBindingContext(relicScope, relic));
                            Expect(relicScope.LiveRunInstanceCount == 1 && item.LifecycleState == ContentLifecycleState.Active,
                                "item did not register one typed relic instance");
                            item.Deactivate();
                            Expect(relicScope.LiveRunInstanceCount == 0 && item.LifecycleState == ContentLifecycleState.Bound,
                                "item deactivation retained a relic instance");
                        }
                        break;
                    default:
                        throw new InvalidOperationException(entry.StableId + " has an unsupported independent scene root");
                }
            }
            finally
            {
                RemoveChild(instance);
                instance.Free();
            }
        }

        var floor = Required<PackedScene>($"{Root}/rule_phase5_clear.tscn").Instantiate<FloorRuleContentRoot>();
        try
        {
            Expect(floor.Id == "rule_phase5_clear" && !floor.ValidateAuthoring().HasCoreErrors,
                "independent floor-rule scene did not validate");
        }
        finally { floor.Free(); }
    }

    private static void VerifyDeterministicRuntimeAndIsolation(ContentRegistry registry, CompiledGameProject project)
    {
        var saveA = new MemoryRunSaveService();
        var saveB = new MemoryRunSaveService();
        var appA = new RunApplication(registry, saveA, project);
        var appB = new RunApplication(registry, saveB, project);
        Expect(appA.StartNewRun("hero_phase5_fixture", 0x505UL) &&
               appB.StartNewRun("hero_phase5_fixture", 0x505UL),
            "fixture Run applications did not start");
        Expect(!ReferenceEquals(appA.ActiveRun, appB.ActiveRun), "two Runs published the same mutable DTO");
        Expect(appA.GrantItem("item_phase5_fixture") && appB.GrantItem("item_phase5_fixture"),
            "fixture relic acquisition failed");
        Expect(!ReferenceEquals(appA.ActiveRun!.Items[0], appB.ActiveRun!.Items[0]),
            "two Runs shared mutable relic instance state");
        Expect(appA.SelectNode(TowerNodeType.Boss) && appB.SelectNode(TowerNodeType.Boss),
            "fixture Boss node selection failed");

        var encounterA = appA.CurrentEncounter();
        var encounterB = appB.CurrentEncounter();
        Expect(SameEncounter(encounterA, encounterB) && encounterA.EncounterId == "encounter_phase5_boss",
            "seeded fixture encounter was not deterministic");
        var configA = appA.BuildBattleConfig(encounterA);
        var configB = appB.BuildBattleConfig(encounterB);
        using (var phaseBattle = new BattleSimulation(configA))
        {
            var boss = phaseBattle.Units.Single(unit => unit.RuntimeId == "enemy-0");
            Expect(boss.BossPhaseId == "phase_phase5_opening" && boss.Shield == 9,
                "fixture Boss opening phase did not install its distinct loadout");
            boss.Health = boss.MaxHealth * .4f;
            phaseBattle.Step();
            Expect(boss.BossPhaseId == "phase_phase5_second" && boss.Shield == 26,
                "fixture Boss threshold did not replace and execute the second distinct Ability loadout");
        }
        using var battleA = new BattleSimulation(configA);
        using var battleB = new BattleSimulation(configB);

        Expect(battleA.Units.Single(unit => unit.RuntimeId == "enemy-0").Shield == 9 &&
               battleB.Units.Single(unit => unit.RuntimeId == "enemy-0").Shield == 9,
            "authored Boss opening ability did not execute through the timeline");
        var heroDamageA = battleA.Units.Single(unit => unit.RuntimeId == "player-hero").Damage;
        var heroDamageB = battleB.Units.Single(unit => unit.RuntimeId == "player-hero").Damage;
        Expect(battleA.TryUseTacticalCommand(0).Succeeded && battleB.TryUseTacticalCommand(0).Succeeded,
            "authored fixture tactical command failed");
        Expect(battleA.TacticalPoints == 2 && battleB.TacticalPoints == 2 &&
               battleA.Units.Single(unit => unit.RuntimeId == "player-hero").Damage > heroDamageA &&
               battleB.Units.Single(unit => unit.RuntimeId == "player-hero").Damage > heroDamageB,
            "authored tactical-point/status facts did not agree with runtime behavior");

        var resultA = battleA.RunToEnd();
        var resultB = battleB.RunToEnd();
        Expect(resultA.Outcome == BattleOutcome.PlayerVictory && SameBattleResult(resultA, resultB),
            "same-seed authored battles were not deterministic");
        Expect(battleA.EffectTransition?.Validate().IsValid == true &&
               battleA.AbilityTransition is { RemainingRuntimeInstances: 0 } &&
               battleA.StatusTransition is { RemainingInstances: 0 } &&
               battleA.RelicTransition is { RemainingBattleInstances: 0 },
            "battle A retained Effect/Ability/Status/Relic runtime state");
        Expect(battleB.EffectTransition?.Validate().IsValid == true &&
               battleB.AbilityTransition is { RemainingRuntimeInstances: 0 } &&
               battleB.StatusTransition is { RemainingInstances: 0 } &&
               battleB.RelicTransition is { RemainingBattleInstances: 0 },
            "battle B retained Effect/Ability/Status/Relic runtime state");
        Expect(battleA.EffectTrace.Select(TraceFingerprint).SequenceEqual(battleB.EffectTrace.Select(TraceFingerprint)),
            "same-seed authored effect traces diverged");

        Expect(appA.CompleteBattle(resultA, encounterA) && appA.ActiveRun is null,
            "fixture Run A did not apply its final transition exactly once");
        Expect(appB.ActiveRun is { PendingNode: true } && appB.ActiveRun.Items.Count == 1,
            "completing Run A mutated Run B");
        Expect(appB.CompleteBattle(resultB, encounterB) && appB.ActiveRun is null,
            "fixture Run B did not apply its independent final transition");
    }

    private static void VerifyReactiveRelicPersistenceAtomicity(
        ContentRegistry registry,
        GameProjectDefinition authoredProject)
    {
        var campaign = authoredProject.Campaign ?? throw new InvalidOperationException("fixture campaign missing");
        var twoFloorCampaign = new CampaignDefinition
        {
            StableId = campaign.StableId,
            FloorsPerRegion = 2,
            Regions = campaign.Regions,
            NodeTable = campaign.NodeTable,
            StarterPool = campaign.StarterPool,
            RecruitmentPool = campaign.RecruitmentPool,
            ItemRewardPool = campaign.ItemRewardPool,
            ShopPool = campaign.ShopPool
        };
        var compilation = GameProjectCompiler.Compile(
            CopyProject(authoredProject, campaign: twoFloorCampaign),
            registry.Graph);
        var project = compilation.Project ?? throw new InvalidOperationException(
            "two-floor Reactive Relic fixture project rejected: " +
            string.Join("; ", compilation.Report.CoreErrors));
        var save = new MemoryRunSaveService();
        var app = new RunApplication(registry, save, project);
        Expect(app.StartNewRun("hero_phase5_fixture", 0xC017UL) &&
               app.GrantItem("item_phase5_reactive_fixture"),
            "Reactive Relic persistence fixture setup failed");
        var active = app.ActiveRun ?? throw new InvalidOperationException("Reactive Relic active Run missing");
        var item = active.Items.Single(value => value.ContentId == "item_phase5_reactive_fixture");
        Expect(item.Counters is [{ CounterId: "allies_alive", Value: 0 }],
            "Reactive Relic reward initialization did not publish its exact Run counter set");
        var canonicalJson = JsonSerializer.Serialize(active);
        foreach (var invalidValue in new[] { 999, int.MaxValue })
        {
            var invalidRun = JsonSerializer.Deserialize<ActiveRunDto>(canonicalJson) ??
                throw new InvalidOperationException("Reactive Relic canonical-range persistence clone failed");
            invalidRun.Items.Single(value => value.ContentId == "item_phase5_reactive_fixture")
                .Counters.Single(value => value.CounterId == "allies_alive").Value = invalidValue;
            var rejected = new RunApplication(registry, new MemoryRunSaveService(invalidRun), project);
            Expect(rejected.ActiveRun is null &&
                   rejected.ActiveRunLoadDiagnostic is { Kind: ActiveRunLoadFailureKind.ValidationRejected },
                $"v4 Reactive Relic Run counter value {invalidValue} was not rejected by persistence");
        }
        Expect(app.SelectNode(TowerNodeType.Boss), "Reactive Relic persistence fixture node selection failed");
        var encounter = app.CurrentEncounter();
        using var battle = new BattleSimulation(app.BuildBattleConfig(encounter));
        var result = battle.RunToEnd();
        var projectedValue = result.RelicTransition?.ProjectedInstances
            .Single(value => value.ContentId == item.ContentId).Counters
            .Single(value => value.CounterId == "allies_alive").Value ?? -1;
        Expect(result.Outcome == BattleOutcome.PlayerVictory && projectedValue > 0,
            "Reactive Relic Battle did not project an authenticated Run counter advance");

        var beforeFailure = JsonSerializer.Serialize(active);
        save.FailNextActiveRunSave = true;
        Expect(!app.CompleteBattle(result, encounter) &&
               ReferenceEquals(active, app.ActiveRun) &&
               JsonSerializer.Serialize(active) == beforeFailure &&
               item.Counters.Single().Value == 0,
            "failed settlement publication advanced the authoritative Reactive Relic Run counter");
        Expect(app.CompleteBattle(result, encounter) &&
               ReferenceEquals(active, app.ActiveRun) &&
               active.Items.Single(value => value.ContentId == item.ContentId).Counters.Single().Value == projectedValue,
            "Reactive Relic settlement retry did not persist exactly one authenticated counter advance");
        var persisted = save.LoadActiveRun() ?? throw new InvalidOperationException(
            "Reactive Relic settlement retry was not persisted");
        Expect(persisted.Items.Single(value => value.ContentId == item.ContentId).Counters.Single().Value == projectedValue,
            "Reactive Relic reload lost the persisted Run counter");
    }

    private static void VerifyVersionTwoMigration(ContentRegistry registry, CompiledGameProject project)
    {
        var v2 = new ActiveRunDto
        {
            Version = 2,
            Seed = 55,
            LegacyHeroId = "hero_phase5_fixture",
            LegacyHeroHealthRatio = 1f,
            Roster =
            [
                new RosterHeroInstanceDto
                {
                    InstanceId = "unit-1",
                    ContentId = "soldier_phase5_fixture",
                    HealthRatio = .75f
                }
            ],
            Deployment = ["unit-1", "", "", "", "", ""],
            LegacyHeroCell = null,
            LegacyDeploymentCells = [],
            Items =
            [
                JsonSerializer.Deserialize<ItemInstanceDto>(
                    "{\"InstanceId\":\"item-2\",\"ContentId\":\"item_phase5_fixture\"," +
                    "\"Stacks\":1,\"Charges\":2,\"Roll\":3}") ??
                throw new InvalidOperationException("v2 item without Counters did not deserialize")
            ],
            FloorIndex = 0,
            BattleNumber = 0
        };
        var app = new RunApplication(registry, new MemoryRunSaveService(v2), project);
        Expect(app.ActiveRun is { Version: 4 } migrated && migrated.Roster.Count == 2 &&
               migrated.CurrentPopulation == 2 &&
               migrated.Deployment.Count == 18 && migrated.Items.Single().Charges == 2 &&
               migrated.Items.Single().Roll == 3 && migrated.Items.Single().Counters.Count == 0,
            "fixture v2→v3 migration lost formation or relic state");
    }

    private async Task VerifyTransactionalRejectionAsync(Fixture fixture)
    {
        var missingEquipmentPackage = fixture.Package with { Equipment = [] };
        var missingEquipmentGate = await GamePackagePublisher.CreateAuthoredReadyAsync(this, missingEquipmentPackage);
        ExpectRejectedPublication(missingEquipmentGate, "unregistered Equipment package");
        Expect(missingEquipmentGate.Report.CoreErrors.Any(error =>
                error.Contains("unregistered Equipment definition", StringComparison.Ordinal)),
            "unregistered Equipment package omitted cross-graph reachability diagnostics");

        var missingTraitPackage = fixture.Package with { Traits = [] };
        var missingTraitGate = await GamePackagePublisher.CreateAuthoredReadyAsync(this, missingTraitPackage);
        ExpectRejectedPublication(missingTraitGate, "missing Trait package");
        Expect(missingTraitGate.Report.CoreErrors.Any(error =>
                error.Contains("missing Trait", StringComparison.Ordinal)),
            "missing Trait package omitted contribution reachability diagnostics");

        var orphanEquipment = new EquipmentDefinition
        {
            StableId = "equipment_phase5_orphan",
            AttributeModifiers =
            [
                new AttributeModifierSpec
                {
                    Attribute = CombatAttribute.Armor,
                    Operation = AttributeModifierOperation.Add,
                    Magnitude = new ConstantAttributeMagnitudeSpec { Value = 1 },
                    SlotId = "phase5_orphan_armor"
                }
            ]
        };
        var orphanEquipmentPackage = fixture.Package with
        {
            Equipment = fixture.Equipment.Append(orphanEquipment).ToArray()
        };
        var orphanEquipmentGate = await GamePackagePublisher.CreateAuthoredReadyAsync(this, orphanEquipmentPackage);
        ExpectRejectedPublication(orphanEquipmentGate, "orphan Equipment package");
        Expect(orphanEquipmentGate.Report.CoreErrors.Any(error =>
                error.Contains("Orphan Equipment definition", StringComparison.Ordinal)),
            "orphan Equipment package omitted authored/reference symmetry diagnostics");

        var productionPathProbe = ContentValidator.CompileProductionGraph(fixture.Package.Catalog, []);
        Expect(productionPathProbe.Report.CoreErrors.Any(error =>
                error.Contains("outside the production Equipment directory", StringComparison.Ordinal)),
            "production Equipment path validation omitted the authored Resource path diagnostic");

        var invalidLoadout = Required<AbilityLoadoutDefinition>($"{Root}/abilities/loadout_phase5_invalid.tres");
        var invalidAbility = Required<AbilityDefinition>($"{Root}/abilities/ability_phase5_invalid.tres");
        var invalidPackage = new AuthoredContentPackage(
            fixture.Project,
            fixture.Package.Catalog,
            fixture.Loadouts.Append(invalidLoadout).ToArray(),
            fixture.Abilities.Append(invalidAbility).ToArray(),
            fixture.Statuses,
            fixture.Relics)
            {
                Equipment = fixture.Equipment,
                Traits = fixture.Traits,
                TacticalCommands = fixture.Package.TacticalCommands,
                TacticalCommandScenes = fixture.Package.TacticalCommandScenes
            };
        var gate = await GamePackagePublisher.CreateAuthoredReadyAsync(this, invalidPackage);
        ExpectRejectedPublication(gate, "invalid Ability package");
        var diagnostic = string.Join(" | ", gate.Report.CoreErrors);
        Expect(diagnostic.Contains("res://tests/fixtures/phase5/abilities/ability_phase5_invalid.tres", StringComparison.Ordinal) &&
               diagnostic.Contains("operation[0]", StringComparison.Ordinal),
            "invalid package diagnostics omitted authored path or operation index: " + diagnostic);
        var batch = AbilityDefinitionCompiler.CompileBatch(invalidPackage.Loadouts);
        Expect(batch.Report.HasCoreErrors && batch.Abilities.IsEmpty,
            "invalid ability batch retained a partial compiled publication");

        var invalidProject = CopyProject(fixture.Project, stableId: "Invalid Project Id");
        var invalidProjectGate = await GamePackagePublisher.CreateAuthoredReadyAsync(
            this,
            CopyPackage(fixture, invalidProject));
        ExpectRejectedPublication(invalidProjectGate, "invalid GameProject package");
        Expect(invalidProjectGate.Report.CoreErrors.Any(error =>
                error.Contains("invalid stable id", StringComparison.OrdinalIgnoreCase)),
            "invalid GameProject rejection omitted its project diagnostic");

        var unregisteredLoadout = new AbilityLoadoutDefinition
        {
            Abilities = [fixture.Abilities[0]]
        };
        var invalidTimelineProject = ProjectWithUnregisteredBossPhase(fixture.Project, unregisteredLoadout);
        var invalidTimelineGate = await GamePackagePublisher.CreateAuthoredReadyAsync(
            this,
            CopyPackage(fixture, invalidTimelineProject));
        ExpectRejectedPublication(invalidTimelineGate, "unregistered Boss-phase loadout package");
        Expect(invalidTimelineGate.Report.CoreErrors.Any(error =>
                error.Contains("unregistered ability loadout", StringComparison.OrdinalIgnoreCase)),
            "unregistered Boss phase did not fail cross-graph reachability before publication");

        var collisionAbility = CopyAbility(fixture.Abilities.Single(ability =>
            ability.StableId == "ability_phase5_boss_second_shield"));
        var collisionLoadout = new AbilityLoadoutDefinition { Abilities = [collisionAbility] };
        var collisionProject = ProjectWithAdditionalBossPhase(
            fixture.Project,
            collisionLoadout,
            "timeline_phase5_collision",
            "phase_phase5_collision",
            "冲突阶段");
        var collisionPackage = new AuthoredContentPackage(
            collisionProject,
            fixture.Package.Catalog,
            fixture.Loadouts.Append(collisionLoadout).ToArray(),
            fixture.Abilities.Append(collisionAbility).ToArray(),
            fixture.Statuses,
            fixture.Relics)
            {
                Equipment = fixture.Equipment,
                Traits = fixture.Traits,
                TacticalCommands = fixture.Package.TacticalCommands,
                TacticalCommandScenes = fixture.Package.TacticalCommandScenes
            };
        var collisionGate = await GamePackagePublisher.CreateAuthoredReadyAsync(this, collisionPackage);
        ExpectRejectedPublication(collisionGate, "distinct-Resource stable-id collision package");
        var collisionDiagnostic = string.Join(" | ", collisionGate.Report.CoreErrors);
        Expect(collisionDiagnostic.Contains("distinct resources", StringComparison.OrdinalIgnoreCase),
            "distinct-Resource stable-id collision omitted its identity diagnostic: " + collisionDiagnostic);
        var collisionBatch = AbilityDefinitionCompiler.CompileBatch(collisionPackage.Loadouts);
        Expect(collisionBatch.Report.HasCoreErrors && collisionBatch.Abilities.IsEmpty &&
               collisionBatch.Loadouts.IsEmpty,
            "distinct-Resource stable-id collision retained a partial Ability publication");
    }

    private static void ExpectRejectedPublication(GamePackagePublicationResult gate, string label)
    {
        Expect(gate.Package is null && gate.PublishedVersion == 0 && gate.Report.HasCoreErrors,
            label + " exposed a Registry, Project, compiled graph, or non-zero publication version");
    }

    private static AuthoredContentPackage CopyPackage(Fixture fixture, GameProjectDefinition project) => new(
        project,
        fixture.Package.Catalog,
        fixture.Loadouts,
        fixture.Abilities,
        fixture.Statuses,
        fixture.Relics)
        {
            Equipment = fixture.Equipment,
            Traits = fixture.Traits,
            TacticalCommands = fixture.Package.TacticalCommands,
            TacticalCommandScenes = fixture.Package.TacticalCommandScenes
        };

    private static GameProjectDefinition CopyProject(
        GameProjectDefinition source,
        string? stableId = null,
        CampaignDefinition? campaign = null) => new()
    {
        StableId = stableId ?? source.StableId,
        Content = source.Content,
        Campaign = campaign ?? source.Campaign,
        RunRules = source.RunRules,
        Presentation = source.Presentation
    };

    private static GameProjectDefinition ProjectWithUnregisteredBossPhase(
        GameProjectDefinition source,
        AbilityLoadoutDefinition unregisteredLoadout) =>
        ProjectWithAdditionalBossPhase(
            source,
            unregisteredLoadout,
            "timeline_phase5_unregistered",
            "phase_phase5_unregistered",
            "未注册阶段");

    private static GameProjectDefinition ProjectWithRegisteredSharedBossPhase(
        GameProjectDefinition source,
        AbilityLoadoutDefinition sharedLoadout) =>
        ProjectWithAdditionalBossPhase(
            source,
            sharedLoadout,
            "timeline_phase5_shared",
            "phase_phase5_shared",
            "共享阶段");

    private static GameProjectDefinition ProjectWithAdditionalBossPhase(
        GameProjectDefinition source,
        AbilityLoadoutDefinition loadout,
        string timelineStableId,
        string phaseStableId,
        string phaseDisplayName)
    {
        var campaign = source.Campaign ?? throw new InvalidOperationException("fixture campaign missing");
        var sourceRegion = campaign.Regions[0];
        var sourceBoss = sourceRegion.Encounters.Single(encounter => encounter.NodeType == TowerNodeType.Boss);
        var sourceTimeline = sourceBoss.BossTimeline ?? throw new InvalidOperationException("fixture Boss timeline missing");
        var timeline = new BossTimelineDefinition
        {
            StableId = timelineStableId,
            BossContentId = sourceTimeline.BossContentId,
            Phases =
            [
                .. sourceTimeline.Phases,
                new BossPhaseDefinition
                {
                    StableId = phaseStableId,
                    DisplayName = phaseDisplayName,
                    StartHealthRatio = .25f,
                    AbilityLoadout = loadout
                }
            ]
        };
        var boss = new EncounterDefinition
        {
            StableId = sourceBoss.StableId,
            NodeType = sourceBoss.NodeType,
            TitlePattern = sourceBoss.TitlePattern,
            EnemyPool = sourceBoss.EnemyPool,
            FloorRulePool = sourceBoss.FloorRulePool,
            LeadEnemyId = sourceBoss.LeadEnemyId,
            BaseEnemyCount = sourceBoss.BaseEnemyCount,
            AddRegionIndexToCount = sourceBoss.AddRegionIndexToCount,
            SeedSalt = sourceBoss.SeedSalt,
            BossTimeline = timeline
        };
        var region = new TowerRegionDefinition
        {
            Id = sourceRegion.Id,
            DisplayName = sourceRegion.DisplayName,
            Description = sourceRegion.Description,
            AccentColor = sourceRegion.AccentColor,
            Encounters = sourceRegion.Encounters
                .Select(encounter => encounter.NodeType == TowerNodeType.Boss ? boss : encounter)
                .ToArray()
        };
        var updatedCampaign = new CampaignDefinition
        {
            StableId = campaign.StableId,
            FloorsPerRegion = campaign.FloorsPerRegion,
            Regions = [region, .. campaign.Regions.Skip(1)],
            NodeTable = campaign.NodeTable,
            StarterPool = campaign.StarterPool,
            RecruitmentPool = campaign.RecruitmentPool,
            ItemRewardPool = campaign.ItemRewardPool,
            ShopPool = campaign.ShopPool
        };
        return CopyProject(source, campaign: updatedCampaign);
    }

    private static AbilityDefinition CopyAbility(AbilityDefinition source) => new()
    {
        StableId = source.StableId,
        DisplayName = source.DisplayName,
        ActivationKind = source.ActivationKind,
        Trigger = source.Trigger,
        ManaCost = source.ManaCost,
        GoldCost = source.GoldCost,
        CooldownTicks = source.CooldownTicks,
        MaxUses = source.MaxUses,
        IntervalTicks = source.IntervalTicks,
        Operations = source.Operations,
        Presentation = source.Presentation
    };

    private static void VerifyEffectDependencyTransaction()
    {
        var cycle = Required<EffectBindingSpec>($"{Root}/effects/cycle_heal.tres");
        var result = EffectBindingCompiler.CompileBatch([cycle]);
        Expect(result.Report.HasCoreErrors && result.Bindings.IsEmpty &&
               result.Report.CoreErrors.Any(error => error.Contains("dependency cycle", StringComparison.OrdinalIgnoreCase)),
            "effect dependency cycle did not reject the complete batch");
    }

    private static void VerifySourceGuards()
    {
        var productionSource = SourceTree("src");
        Expect(!productionSource.Contains("phase5_fixture", StringComparison.Ordinal) &&
               !productionSource.Contains("ability_phase5", StringComparison.Ordinal),
            "production source contains concrete fixture dispatch");

        var gameRoot = Source("src/App/GameRoot.cs");
        var coordinator = Source("src/App/GameFlowCoordinator.cs");
        var publisher = Source("src/Composition/GamePackagePublisher.cs");
        var registry = Source("src/Content/ContentRegistry.cs");
        Expect(gameRoot.Contains("GamePackagePublisher.CreateReadyAsync", StringComparison.Ordinal) &&
               !gameRoot.Contains("ContentRegistry.", StringComparison.Ordinal) &&
               !gameRoot.Contains("GameProjectCompiler.", StringComparison.Ordinal) &&
               !gameRoot.Contains("ResolveBattle", StringComparison.Ordinal) &&
               !gameRoot.Contains("CompleteBattle", StringComparison.Ordinal),
            "GameRoot bypasses the one complete package publication boundary");
        Expect(coordinator.Contains("App.ResolveBattle", StringComparison.Ordinal) &&
               !coordinator.Contains("SaveActiveRun", StringComparison.Ordinal) &&
               !coordinator.Contains("DeleteActiveRun", StringComparison.Ordinal),
            "GameFlowCoordinator bypasses typed Run settlement or owns persistence mutation");
        Expect(publisher.Contains("ContentValidator.CompileProductionGraph", StringComparison.Ordinal) &&
               publisher.Contains("GameProjectCompiler.Compile", StringComparison.Ordinal) &&
               publisher.Contains("ContentRegistry.PublishReadyAsync", StringComparison.Ordinal) &&
               registry.Contains("prepared.Graph", StringComparison.Ordinal) &&
               registry.Contains("prepared.Payload", StringComparison.Ordinal),
            "production package publication no longer runs strict catalog/compiled-graph/project gates atomically");

        var compilerCalls = new[]
        {
            "AbilityDefinitionCompiler.Compile",
            "StatusDefinitionCompiler.Compile",
            "RelicDefinitionCompiler.Compile"
        };
        var compilerAuthorities = new HashSet<string>(StringComparer.Ordinal)
        {
            "Abilities/AbilityDefinitionCompiler.cs",
            "Content/ContentValidator.cs"
        };
        var sourceRoot = Global("src");
        foreach (var path in Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceRoot, path).Replace('\\', '/');
            if (compilerAuthorities.Contains(relative)) continue;
            var source = File.ReadAllText(path);
            foreach (var compilerCall in compilerCalls)
                Expect(!source.Contains(compilerCall, StringComparison.Ordinal),
                    relative + " retained an independent compilation authority: " + compilerCall);
        }

        var abilityCompiler = Source("src/Abilities/AbilityDefinitionCompiler.cs");
        var statusCompiler = Source("src/Statuses/StatusDefinitionCompiler.cs");
        var compileBatchStart = abilityCompiler.IndexOf(
            "public static AbilityBatchCompilationResult CompileBatch", StringComparison.Ordinal);
        var compileBatchEnd = abilityCompiler.IndexOf(
            "private static CompiledAbilityDefinition? CompileInternal", StringComparison.Ordinal);
        Expect(compileBatchStart >= 0 && compileBatchEnd > compileBatchStart,
            "Ability batch compiler source boundary is unavailable");
        var compileBatch = abilityCompiler[compileBatchStart..compileBatchEnd];
        Expect(compileBatch.Contains("compiledByResource", StringComparison.Ordinal) &&
               compileBatch.Contains("stableIdOwners", StringComparison.Ordinal) &&
               compileBatch.Contains("ReferenceEqualityComparer.Instance", StringComparison.Ordinal) &&
               !compileBatch.Contains("var ids = new HashSet", StringComparison.Ordinal),
            "Ability batch compiler reverted to batch-wide compiled stable-id collision authority");
        Expect(!abilityCompiler.Contains("BattleSimulation", StringComparison.Ordinal) &&
               !statusCompiler.Contains("BattleSimulation", StringComparison.Ordinal) &&
               abilityCompiler.Contains("BattleTiming.TickSeconds", StringComparison.Ordinal) &&
               statusCompiler.Contains("BattleTiming.TickSeconds", StringComparison.Ordinal),
            "Ability/Status compilation depends on the Battle runtime center instead of neutral timing authority");
        var command = Source("src/TacticalCommands/TacticalCommandContentRoot.cs");
        var battle = Source("src/Battle/BattleSimulation.cs");
        var setup = Source("src/Battle/BattleSetupFactory.cs");
        var retiredCommandDirectory = Global("src/Battle/Commands");
        Expect(!command.Contains("IHeroCommandRuntime", StringComparison.Ordinal) &&
               !command.Contains("CompiledAbilityLegacyRuntime", StringComparison.Ordinal) &&
               !command.Contains("_legacyDisplayName", StringComparison.Ordinal) &&
               !battle.Contains("HeroRule.Command.", StringComparison.Ordinal) &&
               !battle.Contains("_legacyCurrentMana", StringComparison.Ordinal) &&
               !setup.Contains("CreateRuntime()", StringComparison.Ordinal) &&
               (!Directory.Exists(retiredCommandDirectory) ||
                !Directory.GetFiles(retiredCommandDirectory, "*.cs").Any()),
            "retired Hero command fallback authority is still present");
        Expect(!File.Exists(Global("src/Components/RunModifierProviderComponent.cs")) &&
               !File.Exists(Global("src/Components/ModifierProviderComponent.cs")) &&
               !File.Exists(Global("scenes/components/ModifierProviderComponent.tscn")),
            "retired item modifier-provider authority is still present");
    }

    private static string ResourceFingerprint(Fixture fixture)
    {
        var roots = new List<Resource?>
        {
            fixture.Package.Catalog,
            fixture.Project
        };
        roots.AddRange(fixture.Loadouts);
        roots.AddRange(fixture.Abilities);
        roots.AddRange(fixture.Statuses);
        roots.AddRange(fixture.Relics);
        roots.AddRange(fixture.Equipment);
        roots.AddRange(fixture.Traits);
        return ResourceGraphFingerprint.Compute(roots);
    }

    private static void VerifyDeepFingerprintSensitivity()
    {
        var effect = new ShieldEffectSpec { AmountSource = EffectAmountSource.Fixed, Amount = 9 };
        var binding = new EffectBindingSpec
        {
            StableId = "fingerprint_effect",
            Trigger = new EffectTriggerSpec { Kind = EffectTriggerKind.Manual },
            Conditions = [],
            TargetQuery = new OwnerTargetQuerySpec(),
            Effects = [effect],
            Limits = new EffectBindingLimitsSpec { MaxUses = 1 },
            Presentation = new EffectPresentationSpec { DisplayName = "指纹效果", ReportLabel = "护盾" }
        };
        var ability = new AbilityDefinition
        {
            StableId = "ability_fingerprint",
            DisplayName = "指纹能力",
            ActivationKind = AbilityActivationKind.ManualCommand,
            ManaCost = 1,
            Operations = [new EffectAbilityOperationSpec { Binding = binding }]
        };
        var abilityBefore = ResourceGraphFingerprint.Compute([ability]);
        effect.Amount = 10;
        Expect(ResourceGraphFingerprint.Compute([ability]) != abilityBefore,
            "deep authored fingerprint ignored nested Ability effect parameters");

        var periodicEffect = new ShieldEffectSpec { Amount = 3 };
        var periodicBinding = new EffectBindingSpec
        {
            StableId = "fingerprint_periodic",
            Trigger = new EffectTriggerSpec { Kind = EffectTriggerKind.Manual },
            Conditions = [],
            TargetQuery = new OwnerTargetQuerySpec(),
            Effects = [periodicEffect],
            Limits = new EffectBindingLimitsSpec()
        };
        var status = new StatusDefinition
        {
            StableId = "status_fingerprint",
            DisplayName = "指纹状态",
            Behavior = StatusBehaviorKind.None,
            DurationKind = StatusDurationKind.TimedTicks,
            DurationTicks = 5,
            StackLimit = 1,
            PeriodicIntervalTicks = 2,
            PeriodicEffect = periodicBinding,
            Presentation = new StatusPresentationSpec { ReportLabel = "状态报告" }
        };
        var statusBefore = ResourceGraphFingerprint.Compute([status]);
        periodicEffect.Amount = 4;
        Expect(ResourceGraphFingerprint.Compute([status]) != statusBefore,
            "deep authored fingerprint ignored nested Status periodic-effect parameters");
        statusBefore = ResourceGraphFingerprint.Compute([status]);
        status.Presentation.ReportLabel = "变更报告";
        Expect(ResourceGraphFingerprint.Compute([status]) != statusBefore,
            "deep authored fingerprint ignored Status presentation parameters");

        var modifier = new RelicBattleModifierSpec
            { Kind = RelicBattleModifierKind.StartBattleShield, Amount = 5 };
        var outcome = new RelicRunOutcomeSpec { Kind = RelicRunOutcomeKind.VictoryGold, Amount = 2 };
        var relic = new RelicDefinition
        {
            StableId = "relic_fingerprint",
            BattleModifiers = [modifier],
            VictoryOutcomes = [outcome]
        };
        var relicBefore = ResourceGraphFingerprint.Compute([relic]);
        modifier.Amount = 6;
        Expect(ResourceGraphFingerprint.Compute([relic]) != relicBefore,
            "deep authored fingerprint ignored Relic modifier parameters");
        relicBefore = ResourceGraphFingerprint.Compute([relic]);
        outcome.Amount = 3;
        Expect(ResourceGraphFingerprint.Compute([relic]) != relicBefore,
            "deep authored fingerprint ignored Relic outcome parameters");

        var typedAttribute = new RelicAttributeBindingSpec
        {
            BindingId = "relic_policy_attribute",
            Target = new RelicPlayerEmptySlotHeroesTargetSpec(),
            StackPolicy = RelicAttributeStackPolicy.PerStack,
            Modifier = new AttributeModifierSpec
            {
                Attribute = CombatAttribute.AttackDamage,
                Operation = AttributeModifierOperation.Multiply,
                Magnitude = new ConstantAttributeMagnitudeSpec { Value = 1.08f },
                SlotId = "relic_policy_attribute"
            }
        };
        var typedStart = new RelicBattleStartSummonSpec
        {
            BindingId = "relic_policy_start",
            RepeatPolicy = RelicBattleStartRepeatPolicy.PerStack,
            ContentId = "summon_fixture",
            HealthMultiplier = 1,
            DamageMultiplier = 1
        };
        var typedRelic = new RelicDefinition
        {
            StableId = "relic_policy_fingerprint",
            AttributeBindings = [typedAttribute],
            BattleStartEffects = [typedStart]
        };
        relicBefore = ResourceGraphFingerprint.Compute([typedRelic]);
        typedAttribute.StackPolicy = RelicAttributeStackPolicy.LinearAcrossStacksAndInstances;
        Expect(ResourceGraphFingerprint.Compute([typedRelic]) != relicBefore,
            "deep authored fingerprint ignored Relic stack policy");
        relicBefore = ResourceGraphFingerprint.Compute([typedRelic]);
        typedStart.RepeatPolicy = RelicBattleStartRepeatPolicy.OncePerBattleBinding;
        Expect(ResourceGraphFingerprint.Compute([typedRelic]) != relicBefore,
            "deep authored fingerprint ignored Relic repeat policy");

        var equipmentMagnitude = new ConstantAttributeMagnitudeSpec { Value = 7 };
        var equipmentModifier = new AttributeModifierSpec
        {
            Attribute = CombatAttribute.Armor,
            Operation = AttributeModifierOperation.Add,
            Magnitude = equipmentMagnitude,
            Priority = 2,
            SlotId = "fingerprint_equipment_armor"
        };
        var equipment = new EquipmentDefinition
        {
            StableId = "equipment_fingerprint",
            AttributeModifiers = [equipmentModifier]
        };
        var equipmentBefore = ResourceGraphFingerprint.Compute([equipment]);
        equipmentMagnitude.Value = 8;
        Expect(ResourceGraphFingerprint.Compute([equipment]) != equipmentBefore,
            "deep authored fingerprint ignored nested Equipment modifier magnitude");

        var traitMagnitude = new ConstantAttributeMagnitudeSpec { Value = 3 };
        var trait = new TraitDefinition
        {
            StableId = "trait_fingerprint",
            DisplayName = "指纹羁绊",
            SemanticIconKey = "trait.fingerprint",
            CountingPolicy = new TraitCountingPolicySpec(),
            Breakpoints =
            [
                new TraitBreakpointSpec
                {
                    MinValue = 2,
                    MaxValue = 4,
                    DisplayStyle = "TraitFingerprint",
                    AttributeModifiers =
                    [
                        new AttributeModifierSpec
                        {
                            Attribute = CombatAttribute.AttackDamage,
                            Operation = AttributeModifierOperation.Add,
                            Magnitude = traitMagnitude,
                            SlotId = "trait_fingerprint_attack"
                        }
                    ]
                }
            ]
        };
        var traitBefore = ResourceGraphFingerprint.Compute([trait]);
        traitMagnitude.Value = 4;
        Expect(ResourceGraphFingerprint.Compute([trait]) != traitBefore,
            "deep authored fingerprint ignored nested Trait breakpoint magnitude");

        var pool = new ContentPoolDefinition
        {
            StableId = "pool_fingerprint",
            Kind = ContentPoolKind.Enemy,
            ContentIds = ["enemy_a"]
        };
        var phase = new BossPhaseDefinition
        {
            StableId = "phase_fingerprint",
            DisplayName = "指纹阶段",
            StartHealthRatio = 1,
            AbilityLoadout = new AbilityLoadoutDefinition { Abilities = [ability] }
        };
        var encounter = new EncounterDefinition
        {
            StableId = "encounter_fingerprint",
            NodeType = TowerNodeType.Boss,
            EnemyPool = pool,
            FloorRulePool = new ContentPoolDefinition
                { StableId = "rules_fingerprint", Kind = ContentPoolKind.FloorRule, ContentIds = ["rule_a"] },
            LeadEnemyId = "enemy_a",
            BossTimeline = new BossTimelineDefinition
            {
                StableId = "timeline_fingerprint",
                BossContentId = "enemy_a",
                Phases = [phase]
            }
        };
        var project = new GameProjectDefinition
        {
            StableId = "project_fingerprint",
            Campaign = new CampaignDefinition
            {
                StableId = "campaign_fingerprint",
                Regions =
                [
                    new TowerRegionDefinition
                    {
                        Id = "region_fingerprint",
                        Encounters = [encounter]
                    }
                ],
                StarterPool = pool,
                RecruitmentPool = pool,
                ItemRewardPool = pool,
                ShopPool = pool
            }
        };
        var projectBefore = ResourceGraphFingerprint.Compute([project]);
        pool.ContentIds[0] = "enemy_b";
        Expect(ResourceGraphFingerprint.Compute([project]) != projectBefore,
            "deep authored fingerprint ignored Project pool content");
        projectBefore = ResourceGraphFingerprint.Compute([project]);
        phase.StartHealthRatio = .5f;
        Expect(ResourceGraphFingerprint.Compute([project]) != projectBefore,
            "deep authored fingerprint ignored Encounter/Boss phase/loadout composition");
    }

    private static void VerifyEquipmentReactiveCompilationContracts()
    {
        var statusA = new StatusDefinition
        {
            StableId = "status_equipment_reactive_a",
            DisplayName = "装备反应甲",
            Behavior = StatusBehaviorKind.None,
            DurationKind = StatusDurationKind.Permanent,
            StackLimit = 1
        };
        var statusB = new StatusDefinition
        {
            StableId = "status_equipment_reactive_b",
            DisplayName = "装备反应乙",
            Behavior = StatusBehaviorKind.None,
            DurationKind = StatusDurationKind.Permanent,
            StackLimit = 1
        };
        var statusBatch = StatusDefinitionCompiler.CompileBatch([statusA, statusB]);
        Expect(!statusBatch.Report.HasCoreErrors && statusBatch.Definitions.Length == 2,
            "Equipment reactive compiler fixture Status graph did not compile: " +
            string.Join(" | ", statusBatch.Report.CoreErrors));
        var statuses = statusBatch.Definitions.ToDictionary(status => status.StableId, StringComparer.Ordinal);
        CompiledStatusDefinition? ResolveStatus(StatusDefinition? authored) =>
            authored is null ? null : statuses.GetValueOrDefault(authored.StableId);

        static EquipmentReactiveStatusBindingSpec Binding(
            StatusDefinition status,
            BattleCombatEventKind eventKind = BattleCombatEventKind.AttackLanded,
            EquipmentReactiveStatusTarget target = EquipmentReactiveStatusTarget.Owner,
            EquipmentReactiveStatusSource source = EquipmentReactiveStatusSource.EquipmentInstance,
            int priority = 7) => new()
        {
            EventKind = eventKind,
            Target = target,
            Source = source,
            Priority = priority,
            Status = status
        };

        CompiledEquipmentDefinition CompileOne(EquipmentReactiveStatusBindingSpec binding)
        {
            var result = EquipmentDefinitionCompiler.Compile(new EquipmentDefinition
            {
                StableId = "equipment_reactive_fingerprint",
                ReactiveStatusBindings = [binding]
            }, ResolveStatus);
            Expect(!result.Report.HasCoreErrors && result.Definition is not null,
                "valid Equipment reactive binding did not compile: " + string.Join(" | ", result.Report.CoreErrors));
            return result.Definition!;
        }

        var baselineAuthored = new EquipmentDefinition
        {
            StableId = "equipment_reactive_authored",
            ReactiveStatusBindings = [Binding(statusA)]
        };
        var authoredFingerprint = ResourceGraphFingerprint.Compute([baselineAuthored]);
        baselineAuthored.ReactiveStatusBindings[0].Priority = 8;
        Expect(ResourceGraphFingerprint.Compute([baselineAuthored]) != authoredFingerprint,
            "deep authored fingerprint ignored Equipment reactive Status binding fields");

        var baseline = CompileOne(Binding(statusA));
        Expect(CompileOne(Binding(statusA, eventKind: BattleCombatEventKind.AttackDeclared)).Fingerprint != baseline.Fingerprint &&
               CompileOne(Binding(statusA, target: EquipmentReactiveStatusTarget.EventTarget)).Fingerprint != baseline.Fingerprint &&
               CompileOne(Binding(statusA, source: EquipmentReactiveStatusSource.Owner)).Fingerprint != baseline.Fingerprint &&
               CompileOne(Binding(statusA, priority: 8)).Fingerprint != baseline.Fingerprint &&
               CompileOne(Binding(statusB)).Fingerprint != baseline.Fingerprint,
            "compiled Equipment fingerprint ignored reactive event/target/source/priority/Status dependency");

        var missingStatus = new StatusDefinition
        {
            StableId = "status_equipment_reactive_missing",
            DisplayName = "未注册装备状态",
            Behavior = StatusBehaviorKind.None,
            DurationKind = StatusDurationKind.Permanent,
            StackLimit = 1
        };
        var valid = new EquipmentDefinition
        {
            StableId = "equipment_reactive_valid",
            ReactiveStatusBindings = [Binding(statusA)]
        };
        var missing = new EquipmentDefinition
        {
            StableId = "equipment_reactive_missing",
            ReactiveStatusBindings = [Binding(missingStatus)]
        };
        var missingBatch = EquipmentDefinitionCompiler.CompileBatch([valid, missing], ResolveStatus);
        Expect(missingBatch.Report.HasCoreErrors && missingBatch.Definitions.IsEmpty &&
               missingBatch.Report.CoreErrors.Any(error =>
                   error.Contains("Status dependency is not registered", StringComparison.Ordinal)),
            "missing Equipment→Status dependency did not reject the complete compiled batch atomically");

        var nullBinding = EquipmentDefinitionCompiler.CompileBatch(
            [new EquipmentDefinition
            {
                StableId = "equipment_reactive_null",
                ReactiveStatusBindings = [null!]
            }], ResolveStatus);
        Expect(nullBinding.Report.HasCoreErrors && nullBinding.Definitions.IsEmpty &&
               nullBinding.Report.CoreErrors.Any(error => error.Contains("binding[0] is missing", StringComparison.Ordinal)),
            "null Equipment reactive Status binding did not return an atomic compile diagnostic");

        var invalidEnums = EquipmentDefinitionCompiler.CompileBatch(
            [new EquipmentDefinition
            {
                StableId = "equipment_reactive_invalid_enums",
                ReactiveStatusBindings =
                [
                    Binding(statusA,
                        (BattleCombatEventKind)int.MaxValue,
                        (EquipmentReactiveStatusTarget)int.MaxValue,
                        (EquipmentReactiveStatusSource)int.MaxValue)
                ]
            }], ResolveStatus);
        var invalidDiagnostic = string.Join(" | ", invalidEnums.Report.CoreErrors);
        Expect(invalidEnums.Definitions.IsEmpty &&
               invalidDiagnostic.Contains("event kind is invalid", StringComparison.Ordinal) &&
               invalidDiagnostic.Contains("target policy is invalid", StringComparison.Ordinal) &&
               invalidDiagnostic.Contains("source policy is invalid", StringComparison.Ordinal),
            "invalid Equipment reactive Status enums did not return complete compile diagnostics");

        var duplicate = EquipmentDefinitionCompiler.CompileBatch(
            [new EquipmentDefinition
            {
                StableId = "equipment_reactive_duplicate",
                ReactiveStatusBindings = [Binding(statusA), Binding(statusA)]
            }], ResolveStatus);
        Expect(duplicate.Report.HasCoreErrors && duplicate.Definitions.IsEmpty &&
               duplicate.Report.CoreErrors.Any(error =>
                   error.Contains("duplicate reactive Status binding", StringComparison.Ordinal)),
            "duplicate Equipment reactive Status binding did not reject the complete compiled batch");

        static StatusDefinition SourceAttributeStatus(string id) => new()
        {
            StableId = id,
            DisplayName = "来源属性状态",
            Behavior = StatusBehaviorKind.None,
            DurationKind = StatusDurationKind.Permanent,
            StackLimit = 1,
            AttributeModifiers =
            [
                new AttributeModifierSpec
                {
                    Attribute = CombatAttribute.AttackSpeed,
                    Operation = AttributeModifierOperation.Add,
                    Magnitude = new SourceAttributeMagnitudeSpec
                    {
                        Attribute = CombatAttribute.AttackDamage,
                        CaptureMode = AttributeCaptureMode.Snapshot
                    },
                    SlotId = "equipment_source_attribute_probe"
                }
            ]
        };

        var directSourceAttribute = SourceAttributeStatus("status_equipment_source_attribute_direct");
        var directStatusBatch = StatusDefinitionCompiler.CompileBatch([directSourceAttribute]);
        Expect(!directStatusBatch.Report.HasCoreErrors && directStatusBatch.Definitions.Length == 1,
            "direct source-Attribute Status fixture did not compile");
        var directCompiled = directStatusBatch.Definitions.Single();
        var directEquipment = EquipmentDefinitionCompiler.Compile(
            new EquipmentDefinition
            {
                StableId = "equipment_source_attribute_direct",
                ReactiveStatusBindings = [Binding(directSourceAttribute)]
            },
            status => status == directSourceAttribute ? directCompiled : null);
        Expect(directEquipment.Definition is null && directEquipment.Report.HasCoreErrors &&
               directEquipment.Report.CoreErrors.Any(error =>
                   error.Contains("source Attribute", StringComparison.OrdinalIgnoreCase)),
            "Equipment-instance sourced direct Status modifier was not rejected at compilation");

        var overflowSourceAttribute = SourceAttributeStatus("status_equipment_source_attribute_overflow");
        var overflowRoot = new StatusDefinition
        {
            StableId = "status_equipment_source_attribute_root",
            DisplayName = "来源属性溢出根",
            Behavior = StatusBehaviorKind.None,
            DurationKind = StatusDurationKind.Permanent,
            AggregationPolicy = StatusAggregationPolicy.ByTarget,
            StackLimit = 2,
            OverflowPolicy = StatusOverflowPolicy.ApplyStatusAndConsumeAtLimit,
            OverflowStatus = overflowSourceAttribute,
            OverflowConsumeStacks = 0
        };
        var overflowStatusBatch = StatusDefinitionCompiler.CompileBatch([overflowRoot, overflowSourceAttribute]);
        Expect(!overflowStatusBatch.Report.HasCoreErrors && overflowStatusBatch.Definitions.Length == 2,
            "reachable source-Attribute Status fixture did not compile: " +
            string.Join(" | ", overflowStatusBatch.Report.CoreErrors));
        var overflowCompiled = overflowStatusBatch.Definitions.Single(status => status.StableId == overflowRoot.StableId);
        var overflowEquipment = EquipmentDefinitionCompiler.Compile(
            new EquipmentDefinition
            {
                StableId = "equipment_source_attribute_overflow",
                ReactiveStatusBindings = [Binding(overflowRoot)]
            },
            status => status == overflowRoot ? overflowCompiled : null);
        Expect(overflowEquipment.Definition is null && overflowEquipment.Report.HasCoreErrors &&
               overflowEquipment.Report.CoreErrors.Any(error =>
                   error.Contains("source Attribute", StringComparison.OrdinalIgnoreCase)),
            "Equipment-instance sourced reachable overflow Status modifier was not rejected at compilation");

        var ownerSourceEquipment = EquipmentDefinitionCompiler.Compile(
            new EquipmentDefinition
            {
                StableId = "equipment_owner_source_attribute",
                ReactiveStatusBindings =
                [
                    Binding(directSourceAttribute, source: EquipmentReactiveStatusSource.Owner)
                ]
            },
            status => status == directSourceAttribute ? directCompiled : null);
        Expect(ownerSourceEquipment.Definition is not null && !ownerSourceEquipment.Report.HasCoreErrors,
            "owner-sourced Status incorrectly rejected a resolvable source Attribute");
    }

    private static void VerifyCompiledRelicPolicyFingerprintSensitivity()
    {
        var modifier = new CompiledAttributeModifier(
            CombatAttribute.AttackDamage,
            AttributeModifierOperation.Multiply,
            new CompiledConstantMagnitude(1.08f),
            0,
            "compiled_policy_modifier");
        var attribute = new CompiledRelicAttributeBinding(
            "compiled_policy_attribute",
            new CompiledRelicPlayerEmptySlotHeroesTarget(),
            RelicAttributeStackPolicy.PerStack,
            modifier);
        var start = new CompiledRelicBattleStartSummon(
            "compiled_policy_start",
            RelicBattleStartRepeatPolicy.PerStack,
            "fixture_summon",
            1,
            1);
        var baseline = new CompiledRelicDefinition(
            "fixture_compiled_policy",
            "res://fixture_compiled_policy.tres",
            [attribute],
            [start],
            [],
            [],
            [],
            "fixed-definition-fingerprint");
        var stackChanged = baseline with
        {
            AttributeBindings =
            [
                attribute with
                {
                    StackPolicy = RelicAttributeStackPolicy.LinearAcrossStacksAndInstances
                }
            ]
        };
        var repeatChanged = baseline with
        {
            BattleStartEffects =
            [
                start with
                {
                    RepeatPolicy = RelicBattleStartRepeatPolicy.OncePerBattleBinding
                }
            ]
        };
        var baselineProjection = CompiledRelicProjectionFingerprint(baseline);
        Expect(CompiledRelicProjectionFingerprint(stackChanged) != baselineProjection &&
               CompiledRelicProjectionFingerprint(repeatChanged) != baselineProjection,
            "compiled package fingerprint projection ignored Relic stack or repeat policy");
    }

    private static string CompiledRelicProjectionFingerprint(CompiledRelicDefinition relic)
    {
        var text = new StringBuilder();
        text.Append("relic:").Append(relic.StableId).Append(':').Append(relic.ResourcePath).Append(':')
            .Append(relic.Fingerprint).Append('{');
        foreach (var binding in relic.AttributeBindings)
            text.Append("attribute:").Append(binding.BindingId).Append(':')
                .Append(binding.Target.GetType().Name).Append(':').Append(binding.StackPolicy).Append(':')
                .Append(binding.Modifier.Attribute).Append(':')
                .Append(binding.Modifier.Operation).Append(':').Append(binding.Modifier.Priority).Append(':')
                .Append(binding.Modifier.SlotId).Append(':')
                .Append(CompiledMagnitude(binding.Modifier.Magnitude)).Append(';');
        foreach (var start in relic.BattleStartEffects)
            text.Append("start:").Append(start.GetType().Name).Append(':')
                .Append(start.BindingId).Append(':').Append(start.RepeatPolicy).Append(';');
        text.Append("};");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()))).ToLowerInvariant();
    }

    private static string CompiledPackageFingerprint(CompiledGamePackage package)
    {
        var text = new StringBuilder();
        text.Append("version=").Append(package.PublicationVersion).Append(';');
        var graph = package.Content.Graph;
        for (var index = 0; index < graph.AbilityLoadouts.Length; index++)
        {
            text.Append("loadout[").Append(index).Append("]{");
            foreach (var ability in graph.AbilityLoadouts[index].Abilities)
                text.Append(ability.StableId).Append(',');
            text.Append("};");
        }
        foreach (var ability in graph.Abilities.OrderBy(value => value.StableId, StringComparer.Ordinal))
            AppendCompiledAbility(text, ability);
        foreach (var status in graph.Statuses.OrderBy(value => value.StableId, StringComparer.Ordinal))
            AppendCompiledStatus(text, status);
        foreach (var relic in graph.Relics.OrderBy(value => value.StableId, StringComparer.Ordinal))
        {
            text.Append("relic:").Append(relic.StableId).Append(':').Append(relic.ResourcePath).Append(':')
                .Append(relic.Fingerprint).Append('{');
            foreach (var binding in relic.AttributeBindings)
                text.Append("attribute:").Append(binding.BindingId).Append(':')
                    .Append(binding.Target.GetType().Name).Append(':').Append(binding.StackPolicy).Append(':')
                    .Append(binding.Modifier.Attribute).Append(':')
                    .Append(binding.Modifier.Operation).Append(':').Append(binding.Modifier.Priority).Append(':')
                    .Append(binding.Modifier.SlotId).Append(':')
                    .Append(CompiledMagnitude(binding.Modifier.Magnitude)).Append(';');
            foreach (var start in relic.BattleStartEffects)
                switch (start)
                {
                    case CompiledRelicBattleStartShield shield:
                        text.Append("start-shield:").Append(shield.BindingId).Append(':')
                            .Append(shield.RepeatPolicy).Append(':').Append(shield.Amount).Append(':');
                        AppendCompiledEffect(text, shield.Effect);
                        break;
                    case CompiledRelicBattleStartSummon summon:
                        text.Append("start-summon:").Append(summon.BindingId).Append(':')
                            .Append(summon.RepeatPolicy).Append(':').Append(summon.ContentId).Append(':')
                            .Append(F(summon.HealthMultiplier)).Append(':')
                            .Append(F(summon.DamageMultiplier)).Append(';');
                        break;
                    default:
                        text.Append("unknown-start:").Append(start.GetType().FullName).Append(';');
                        break;
                }
            foreach (var modifier in relic.BattleModifiers)
                text.Append("legacy:").Append(modifier.Kind).Append(':').Append(F(modifier.Amount)).Append(':')
                    .Append(modifier.ContentId).Append(';');
            foreach (var counter in relic.ReactiveCounters)
            {
                text.Append("counter:").Append(counter.CounterId).Append(':').Append(counter.Scope).Append(':')
                    .Append(counter.ResetPolicy).Append(':').Append(counter.Source).Append(':')
                    .Append(counter.EventKind).Append(':').Append(counter.Team).Append(':')
                    .Append(counter.IncludeTemporary).Append(':').Append(counter.Threshold).Append(':')
                    .Append(counter.Consumption).Append(':').Append(counter.Priority).Append(':')
                    .Append(counter.Target).Append(':').Append(counter.TargetTeam).Append(':');
                AppendCompiledEffect(text, counter.ThresholdEffect);
            }
            text.Append("outcomes{");
            foreach (var outcome in relic.VictoryOutcomes)
                text.Append(outcome.Kind).Append(':').Append(outcome.Amount).Append(';');
            text.Append("}};");
        }
        foreach (var equipment in graph.Equipment.OrderBy(value => value.StableId, StringComparer.Ordinal))
        {
            text.Append("equipment:").Append(equipment.StableId).Append(':')
                .Append(equipment.ResourcePath).Append(':').Append(equipment.Fingerprint).Append('{');
            foreach (var modifier in equipment.AttributeModifiers)
                text.Append(modifier.Attribute).Append(':').Append(modifier.Operation).Append(':')
                    .Append(modifier.Priority).Append(':').Append(modifier.SlotId).Append(':')
                    .Append(CompiledMagnitude(modifier.Magnitude)).Append(';');
            foreach (var binding in equipment.ReactiveStatusBindings)
                text.Append("reactive:").Append(binding.EventKind).Append(':')
                    .Append(binding.Target).Append(':').Append(binding.Source).Append(':')
                    .Append(binding.Priority).Append(':').Append(binding.Status.StableId).Append(':')
                    .Append(binding.Status.ResourcePath).Append(';');
            text.Append("};");
        }
        foreach (var trait in graph.Traits.OrderBy(value => value.StableId, StringComparer.Ordinal))
        {
            text.Append("trait:").Append(trait.StableId).Append(':').Append(trait.ResourcePath).Append(':')
                .Append(trait.DisplayName).Append(':').Append(trait.SemanticIconKey).Append(':')
                .Append(trait.CountingPolicy).Append(':').Append(trait.Fingerprint).Append('{');
            foreach (var breakpoint in trait.Breakpoints)
                text.Append(breakpoint.Index).Append(':').Append(breakpoint.MinValue).Append(':')
                    .Append(breakpoint.MaxValue).Append(':').Append(breakpoint.DisplayStyle).Append(':')
                    .Append(breakpoint.Fingerprint).Append(';');
            text.Append("};");
        }
        AppendCompiledProject(text, package.Project);
        return Hash(text.ToString());
    }

    private static void AppendCompiledAbility(StringBuilder text, CompiledAbilityDefinition ability)
    {
        text.Append("ability:").Append(ability.StableId).Append(':').Append(ability.DisplayName).Append(':')
            .Append(ability.Description).Append(':').Append(ability.ActivationKind).Append(':')
            .Append(ability.Trigger).Append(':').Append(ability.ManaCost).Append(':').Append(ability.GoldCost)
            .Append(':').Append(ability.CooldownTicks).Append(':').Append(ability.MaxUses).Append(':')
            .Append(ability.IntervalTicks).Append('{');
        foreach (var operation in ability.Operations)
            switch (operation)
            {
                case CompiledEffectAbilityOperation effect:
                    text.Append("effect:").Append(effect.InvocationValueSource).Append(':')
                        .Append(F(effect.InvocationValueScale)).Append(':');
                    AppendCompiledEffect(text, effect.Binding);
                    break;
                case CompiledCooldownAbilityOperation cooldown:
                    text.Append("cooldown:").Append(CompiledTarget(cooldown.TargetQuery)).Append(':')
                        .Append(cooldown.AttackAdjustment).Append(':').Append(cooldown.AttackValue).Append(':')
                        .Append(cooldown.MoveAdjustment).Append(':').Append(cooldown.MoveValue).Append(';');
                    break;
                case CompiledApplyStatusAbilityOperation status:
                    text.Append("apply-status:").Append(CompiledTarget(status.TargetQuery)).Append(':');
                    AppendCompiledStatus(text, status.Status);
                    break;
                case CompiledSummonAbilityOperation summon:
                    text.Append("summon:").Append(summon.Profile).Append(':').Append(summon.Count).Append(':')
                        .Append(F(summon.HealthMultiplier)).Append(':').Append(F(summon.DamageMultiplier)).Append(':')
                        .Append(summon.MaximumLivingTemporaryUnits).Append(':').Append(summon.RequireAtLeastOne)
                        .Append(':').Append(summon.SummonContentId).Append(';');
                    break;
                default:
                    text.Append("unknown:").Append(operation.GetType().FullName).Append(';');
                    break;
            }
        if (ability.Presentation is not null)
            text.Append("presentation:").Append(ability.Presentation.SemanticIcon).Append(':')
                .Append(ability.Presentation.Cue).Append(':').Append(ability.Presentation.ReportLabel).Append(';');
        text.Append("};");
    }

    private static void AppendCompiledStatus(StringBuilder text, CompiledStatusDefinition status)
    {
        text.Append("status:").Append(status.StableId).Append(':').Append(status.DisplayName).Append(':')
            .Append(status.ResourcePath).Append(':').Append(status.Description).Append(':').Append(status.Behavior)
            .Append(':').Append(status.Disposition).Append(':').Append(status.DurationKind).Append(':')
            .Append(status.DurationTicks).Append(':')
            .Append(status.AggregationPolicy).Append(':').Append(status.StackLimit).Append(':')
            .Append(status.OverflowPolicy).Append(':').Append(status.DurationRefreshPolicy).Append(':')
            .Append(status.PeriodicResetPolicy).Append(':').Append(status.DispelCategory).Append(':')
            .Append(status.DeathPolicy).Append(':').Append(status.ControlDurationRule).Append(':')
            .Append(F(status.Magnitude)).Append(':').Append(status.PeriodicIntervalTicks).Append('{')
            .Append("tags:").AppendJoin(',', status.GrantedTags).Append(';');
        foreach (var modifier in status.AttributeModifiers)
            text.Append("modifier:").Append(modifier.Attribute).Append(':').Append(modifier.Operation).Append(':')
                .Append(modifier.Priority).Append(':').Append(modifier.SlotId).Append(':')
                .Append(CompiledMagnitude(modifier.Magnitude)).Append(';');
        if (status.PeriodicEffect is not null) AppendCompiledEffect(text, status.PeriodicEffect);
        foreach (var lifecycle in status.LifecycleBindings)
        {
            text.Append("lifecycle:").Append(lifecycle.Trigger).Append(':');
            AppendCompiledEffect(text, lifecycle.Binding);
        }
        foreach (var reactive in status.CombatReactiveBindings)
        {
            text.Append("reactive:").Append(reactive.EventKind).Append(':').Append(reactive.OwnerRole).Append(':')
                .Append(reactive.EffectSourcePolicy).Append(':').Append(reactive.Priority).Append(':');
            AppendCompiledEffect(text, reactive.Binding);
        }
        if (status.OverflowTransition is not null)
            text.Append("transition:").Append(status.OverflowTransition.Target.StableId).Append(':')
                .Append(status.OverflowTransition.ConsumeStacks).Append(';');
        if (status.Presentation is not null)
            text.Append("presentation:").Append(status.Presentation.SemanticIcon).Append(':')
                .Append(status.Presentation.ExecutedCue).Append(':').Append(status.Presentation.OnActiveCue).Append(':')
                .Append(status.Presentation.WhileActiveCue).Append(':').Append(status.Presentation.RemovedCue).Append(':')
                .Append(status.Presentation.ReportLabel).Append(';');
        text.Append("};");
    }

    private static string CompiledMagnitude(CompiledAttributeMagnitude magnitude) => magnitude switch
    {
        CompiledConstantMagnitude constant => $"constant:{F(constant.Value)}:{constant.CaptureMode}",
        CompiledSourceAttributeMagnitude source => $"source:{source.Attribute}:{source.CaptureMode}",
        CompiledTargetAttributeMagnitude target => $"target:{target.Attribute}:{target.CaptureMode}",
        CompiledContextValueMagnitude context => $"context:{context.Key}:{context.CaptureMode}",
        CompiledTeamCountMagnitude count => $"count:{count.CountKind}:{count.Team}:{count.CaptureMode}",
        CompiledTraitValueMagnitude trait => $"trait:{trait.TraitId}:{trait.Team}:{trait.CaptureMode}",
        _ => magnitude.GetType().FullName ?? "unknown-magnitude"
    };

    private static void AppendCompiledEffect(StringBuilder text, CompiledEffectBinding binding)
    {
        text.Append("binding:").Append(binding.StableId).Append(':').Append(binding.Priority).Append(':')
            .Append(binding.Trigger.Kind).Append(':').Append(binding.Trigger.EventKind).Append(':')
            .Append(CompiledTarget(binding.TargetQuery)).Append('{');
        foreach (var condition in binding.Conditions)
            text.Append(condition switch
            {
                CompiledEntityAliveCondition alive =>
                    $"alive:{alive.Entity}:{alive.ExpectedAlive};",
                _ => "unknown-condition:" + condition.GetType().FullName + ";"
            });
        foreach (var effect in binding.Effects)
            text.Append("step:").Append(effect.Kind).Append(':').Append(effect.AmountSource).Append(':')
                .Append(F(effect.Amount)).Append(';');
        text.Append("limits:").Append(binding.Limits.MaxUses).Append(':')
            .Append(binding.Limits.MinimumIntervalTicks).Append(':').Append(binding.Limits.MaxDepth).Append(':')
            .Append(binding.Limits.MaxRepeatedEdges).Append(';');
        if (binding.Presentation is not null)
            text.Append("presentation:").Append(binding.Presentation.DisplayName).Append(':')
                .Append(binding.Presentation.ReportLabel).Append(':').Append(binding.Presentation.Cue).Append(';');
        text.Append("};");
    }

    private static string CompiledTarget(CompiledEffectTargetQuery target) => target switch
    {
        CompiledExplicitTargetQuery => "explicit",
        CompiledSourceTargetQuery => "source",
        CompiledOwnerTargetQuery => "owner",
        CompiledRelativeTeamTargetQuery relative =>
            $"relative:{relative.Team}:{relative.IncludeDefeated}:{relative.RequiredTag}",
        _ => target.GetType().FullName ?? "unknown-target"
    };

    private static void AppendCompiledProject(StringBuilder text, CompiledGameProject project)
    {
        text.Append("project:").Append(project.StableId).Append(':').Append(project.Content.ResourcePath).Append('{');
        var campaign = project.Campaign;
        text.Append("campaign:").Append(campaign.StableId).Append(':').Append(campaign.FloorsPerRegion).Append('{');
        AppendCompiledPool(text, campaign.StarterPool);
        AppendCompiledPool(text, campaign.RecruitmentPool);
        AppendCompiledPool(text, campaign.ItemRewardPool);
        AppendCompiledPool(text, campaign.ShopPool);
        var table = campaign.NodeTable;
        text.Append("nodes:").Append(table.BossLocalFloor).Append(':').Append(table.RegularOptionCount).Append(':')
            .Append(table.RotationStride).Append(':').Append(table.FloorSeedStride).Append(':')
            .AppendJoin(',', table.Rotation).Append('{');
        foreach (var node in table.Nodes.OrderBy(pair => pair.Key))
            text.Append(node.Key).Append(':').Append(node.Value.TitlePattern).Append(':')
                .Append(node.Value.DescriptionPattern).Append(':').Append(node.Value.Risk).Append(';');
        text.Append("};");
        foreach (var region in campaign.Regions)
        {
            text.Append("region:").Append(region.StableId).Append(':').Append(region.DisplayName).Append(':')
                .Append(region.Description).Append(':').Append(F(region.AccentColor.R)).Append(':')
                .Append(F(region.AccentColor.G)).Append(':').Append(F(region.AccentColor.B)).Append(':')
                .Append(F(region.AccentColor.A)).Append('{');
            foreach (var encounter in region.Encounters.OrderBy(pair => pair.Key).Select(pair => pair.Value))
            {
                text.Append("encounter:").Append(encounter.StableId).Append(':').Append(encounter.NodeType).Append(':')
                    .Append(encounter.TitlePattern).Append(':').Append(encounter.LeadEnemyId).Append(':')
                    .Append(encounter.BaseEnemyCount).Append(':').Append(encounter.AddRegionIndexToCount).Append(':')
                    .Append(encounter.SeedSalt).Append('{');
                AppendCompiledPool(text, encounter.EnemyPool);
                AppendCompiledPool(text, encounter.FloorRulePool);
                if (encounter.BossTimeline is not null)
                {
                    text.Append("timeline:").Append(encounter.BossTimeline.StableId).Append(':')
                        .Append(encounter.BossTimeline.BossContentId).Append('{');
                    foreach (var phase in encounter.BossTimeline.Phases)
                    {
                        text.Append("phase:").Append(phase.StableId).Append(':').Append(phase.DisplayName).Append(':')
                            .Append(F(phase.StartHealthRatio)).Append(':');
                        if (phase.AbilityLoadout is null) text.Append("null;");
                        else text.AppendJoin(',', phase.AbilityLoadout.Abilities.Select(value => value.StableId)).Append(';');
                    }
                    text.Append("};");
                }
                text.Append("};");
            }
            text.Append("};");
        }
        text.Append("};");
        AppendCompiledRunRules(text, project.RunRules);
        text.Append("presentation:").Append(project.Presentation.SemanticIcons.ResourcePath).Append(':')
            .Append(project.Presentation.ChoiceCard.ResourcePath).Append(':')
            .Append(project.Presentation.UnitChoiceCard.ResourcePath).Append(':')
            .Append(project.Presentation.ItemChoiceCard.ResourcePath).Append(';');
        foreach (var floorRule in project.FloorRules.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            text.Append("floor:").Append(floorRule.Key).Append(':').Append(floorRule.Value.ResourcePath).Append(';');
        text.Append("};");
    }

    private static void AppendCompiledPool(StringBuilder text, CompiledContentPool pool) =>
        text.Append("pool:").Append(pool.StableId).Append(':').Append(pool.Kind).Append(':')
            .AppendJoin(',', pool.ContentIds).Append(';');

    private static void AppendCompiledRunRules(StringBuilder text, CompiledRunRules rules)
    {
        text.Append("rules:").Append(rules.OrdinaryPopulationCap).Append(':')
            .Append(rules.PhysicalDeploymentCeiling).Append(':').Append(rules.ReserveCapacity).Append(':')
            .Append(rules.StarterRosterHeroCount).Append(':').Append(rules.InitialPopulation).Append(':')
            .Append(rules.EquipmentSlotCapacity).Append(':').Append(rules.RecruitmentChoiceCount).Append(':')
            .Append(rules.ItemChoiceCount).Append(':').Append(rules.StartingGold).Append(':')
            .Append(rules.NormalBattleGold).Append(':').Append(rules.EliteBattleGold).Append(':')
            .Append(rules.BossBattleGold).Append(':').Append(F(rules.VictoryHeroRecovery)).Append(':')
            .Append(F(rules.VictorySoldierRecovery)).Append(':').Append(F(rules.MinimumVictoryHeroHealth)).Append(':')
            .Append(F(rules.MinimumLivingSoldierHealth)).Append(':').Append(F(rules.DefeatedSoldierHealth)).Append(':')
            .Append(rules.RiskyEventSuccessGold).Append(':').Append(F(rules.RiskyEventSuccessChance)).Append(':')
            .Append(F(rules.RiskyEventHealthLoss)).Append(':').Append(F(rules.RiskyEventMinimumHealth)).Append(':')
            .Append(rules.SafeEventGold).Append(':').Append(F(rules.RestHeroHealing)).Append(':')
            .Append(F(rules.RestSoldierHealing)).Append(':').Append(rules.RestGold).Append(':')
            .Append(rules.InitialUnlockedHeroCount).Append(';');
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static string TraceFingerprint(EffectTraceEntry entry) =>
        $"{entry.TraceSequence}:{entry.Ordering}:{entry.BindingId}:{entry.StepIndex}:{entry.TargetId}:" +
        $"{entry.Status}:{entry.Interruption}:{entry.AppliedAmount:R}:{entry.EffectiveAmount:R}";

    private static bool SameEncounter(EncounterPlan first, EncounterPlan second) =>
        first.Title == second.Title && first.FloorRuleId == second.FloorRuleId &&
        first.EnemyIds.SequenceEqual(second.EnemyIds) && first.IsBoss == second.IsBoss &&
        first.IsElite == second.IsElite && first.EncounterId == second.EncounterId &&
        first.NodeType == second.NodeType;

    private static bool SameBattleResult(BattleResult first, BattleResult second) =>
        first.Outcome == second.Outcome && first.Ticks == second.Ticks && first.Digest == second.Digest &&
        first.GoldSpent == second.GoldSpent &&
        first.SuccessfulTacticalCommandUses == second.SuccessfulTacticalCommandUses &&
        first.Units.SequenceEqual(second.Units) && first.Identity == second.Identity &&
        SameRelicTransition(first.RelicTransition, second.RelicTransition);

    private static bool SameRelicTransition(
        RelicBattleTransitionResult? first,
        RelicBattleTransitionResult? second)
    {
        if (ReferenceEquals(first, second)) return true;
        if (first is null || second is null) return false;
        return first.TransitionId == second.TransitionId && first.RunKey == second.RunKey &&
               first.SourceFingerprint == second.SourceFingerprint && first.Reason == second.Reason &&
               first.ProjectedInstances.SequenceEqual(second.ProjectedInstances) &&
               first.Contributions.SequenceEqual(second.Contributions) && first.GoldDelta == second.GoldDelta &&
               first.RemainingBattleInstances == second.RemainingBattleInstances;
    }

    private static T Required<T>(string path) where T : GodotObject =>
        GD.Load<T>(path) ?? throw new InvalidOperationException("fixture resource failed to load: " + path);

    private static string Source(string relativePath) => File.ReadAllText(Global(relativePath));
    private static string SourceTree(string relativeDirectory) => string.Join('\n',
        Directory.GetFiles(Global(relativeDirectory), "*.cs", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(File.ReadAllText));
    private static string Global(string relativePath) => ProjectSettings.GlobalizePath("res://" + relativePath.Replace('\\', '/'));

    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed record Fixture(
        AuthoredContentPackage Package,
        GameProjectDefinition Project,
        IReadOnlyList<AbilityLoadoutDefinition> Loadouts,
        IReadOnlyList<AbilityDefinition> Abilities,
        IReadOnlyList<StatusDefinition> Statuses,
        IReadOnlyList<RelicDefinition> Relics,
        IReadOnlyList<EquipmentDefinition> Equipment,
        IReadOnlyList<TraitDefinition> Traits);

    private sealed class RandomProbe : IDeterministicRandom
    {
        public int NextInt(int minimumInclusive, int maximumExclusive) => minimumInclusive;
        public float NextFloat() => 0;
    }

    private sealed class EventProbe : ISemanticBattleEventSink
    {
        public void Publish(SemanticBattleEvent battleEvent) { }
    }

    private sealed class CommandProbe : IBattleCommandGateway
    {
        public bool Submit(BattleCommandRequest command) => true;
    }

    private sealed class SharedAbilityWorld : IAbilityRuntimeWorld
    {
        public int CommitCount { get; private set; }

        public AbilityWorldSnapshot CaptureSnapshot(int tick) => new(
            tick,
            new[] { new AbilityEntitySnapshot("owner", 0, true, 100, []) }
                .ToImmutableDictionary(entity => entity.RuntimeId, StringComparer.Ordinal));

        public AbilityPreparationResult Prepare(
            CompiledAbilityDefinition ability,
            string sourceId,
            string ownerId,
            string explicitTargetId,
            int tick)
        {
            var operations = ability.Operations.Select((operation, index) =>
                new ResolvedAbilityOperation(index, operation, [ownerId], 0)).ToImmutableArray();
            return new AbilityPreparationResult(
                true,
                AbilityActivationFailure.None,
                string.Empty,
                new AbilityExecutionPlan(ability, sourceId, ownerId, tick, operations, [], ability.GoldCost));
        }

        public AbilityCommitResult Commit(AbilityExecutionPlan plan)
        {
            CommitCount++;
            return new AbilityCommitResult(
                true,
                AbilityActivationFailure.None,
                string.Empty,
                [plan.Ability.StableId]);
        }
    }

    private sealed class MemoryRunSaveService : IRunSaveService
    {
        private readonly MetaProgressDto _meta = new();
        private readonly SettingsDto _settings = new();
        private ActiveRunDto? _run;

        public MemoryRunSaveService(ActiveRunDto? run = null) => _run = run;
        public bool FailNextActiveRunSave { get; set; }
        public MetaProgressDto LoadMeta() => _meta;
        public SettingsDto LoadSettings() => _settings;
        public ActiveRunDto? LoadActiveRun() => _run;
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value)
        {
            if (FailNextActiveRunSave)
            {
                FailNextActiveRunSave = false;
                return false;
            }
            _run = value;
            return true;
        }
        public void DeleteActiveRun() => _run = null;
    }
}
