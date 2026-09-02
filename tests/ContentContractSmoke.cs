using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Relics;
using TowerAutobattler.Run;

public partial class ContentContractSmoke : Node
{
    public override async void _Ready()
    {
        var exitCode = await RunAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(exitCode);
    }

    private async Task<int> RunAsync()
    {
        try
        {
            var catalog = GD.Load<ContentCatalog>("res://content/catalogs/alpha_catalog.tres") ?? throw new InvalidOperationException("catalog load");
            var gate = await TestProjectFixture.PublishAsync(this);
            var registry = gate.Package?.Content ?? throw new InvalidOperationException("content gate: " + string.Join("; ", gate.Report.CoreErrors));
            VerifyPortraitCoverage(catalog);
            VerifyExpansionConformance(catalog);
            VerifyPortraitFingerprintSource();
            await RunStructuralGateFailureContract(catalog);
            await RunReadyGateFailureContracts(catalog);

            var before = catalog.AllEntries().ToDictionary(entry => entry.StableId, entry => DefinitionFingerprint.Compute(entry.Definition), StringComparer.Ordinal);
            var events = new FakeEventSink();
            var commands = new FakeCommandGateway();
            var unitContext = new UnitBindingContext(new DeterministicRandom(71), events, commands);

            foreach (var entry in catalog.AllEntries())
            {
                var node = entry.Scene.Instantiate();
                if (node is UnitContentRoot unit)
                {
                    unit.Bind("smoke-" + entry.StableId, unit.Definition.IsEnemy ? 1 : 0, unit.Definition.MaxHealth, unit.Definition.MaxHealth);
                    unit.Activate(unitContext);
                    ExpectThrows(() => unit.Bind("illegal", 0, 1, 1), "active unit rebind");
                    unit.Deactivate();
                    if (unit.LifecycleState != ContentLifecycleState.Bound) throw new InvalidOperationException("unit lifecycle");
                }
                else if (node is ItemContentRoot item)
                {
                    if (item.Definition.ProductKind == ItemProductKind.Equipment)
                    {
                        if (item.Relic is not null || item.Equipment is null ||
                            registry.Graph.ResolveEquipment(entry.StableId).StableId != entry.StableId)
                            throw new InvalidOperationException("Equipment item publication lifecycle");
                        node.Free();
                        continue;
                    }
                    using var relics = new RelicRunScope(new RelicRunKey(71, catalog.Heroes[0].StableId, 0, 0));
                    item.Bind(new ItemInstanceState
                    {
                        InstanceId = "smoke-" + entry.StableId,
                        ContentId = entry.StableId,
                        Stacks = 2,
                        Charges = 1,
                        Roll = 9
                    });
                    item.Activate(new ItemBindingContext(relics, registry.Graph.ResolveRelic(entry.StableId)));
                    ExpectThrows(() => item.Bind(new ItemInstanceState()), "active item rebind");
                    item.Deactivate();
                    if (relics.LiveRunInstanceCount != 0 || item.LifecycleState != ContentLifecycleState.Bound) throw new InvalidOperationException("item lifecycle");
                }
                node.Free();
            }
            RunLifecycleRollbackContracts(registry);
            if (!events.Events.Any(e => e.Type == SemanticBattleEventType.Activated) ||
                !commands.Submit(new BattleCommandRequest(BattleCommandType.UseTacticalCommand, "smoke-command")))
                throw new InvalidOperationException("typed binding communication");

            foreach (var scene in catalog.FloorRules)
            {
                var floor = scene.Instantiate<FloorRuleContentRoot>();
                if (floor.ValidateAuthoring().HasCoreErrors) throw new InvalidOperationException("floor validation");
                floor.Free();
            }

            RunPresenterFreeBattle(registry);
            RunSaveRoundTrip(registry);
            RunSourceGuard();

            foreach (var entry in catalog.AllEntries())
                if (before[entry.StableId] != DefinitionFingerprint.Compute(entry.Definition))
                    throw new InvalidOperationException("definition mutated: " + entry.StableId);

            var unitCount = catalog.Heroes.Count + catalog.Soldiers.Count + catalog.Enemies.Count;
            GD.Print($"CONTENT_CONTRACT_OK entries={catalog.AllEntries().Count} floors={catalog.FloorRules.Count} events={events.Events.Count} portraits={unitCount}({catalog.Heroes.Count},{catalog.Soldiers.Count},{catalog.Enemies.Count}) expansion=extra-valid,invalid-identity-reference-category source-guard={ProductionSourceGuard.GuardedConcreteIdFamilies.Length}-families-data-driven");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr($"CONTENT_CONTRACT_FAILED: {exception}");
            return 1;
        }
    }

    private static void VerifyPortraitCoverage(ContentCatalog catalog)
    {
        var units = catalog.Heroes.Concat(catalog.Soldiers).Concat(catalog.Enemies).ToArray();
        var portraits = units.Select(entry => ((UnitDefinition)entry.Definition).Portrait).ToArray();
        if (portraits.Length != units.Length || portraits.Any(portrait => portrait is null ||
                string.IsNullOrWhiteSpace(portrait.ResourcePath) || portrait.ResolveTexture() is null ||
                portrait.Zoom is < .5f or > 4f || Math.Abs(portrait.OffsetRatio.X) > 1 || Math.Abs(portrait.OffsetRatio.Y) > 1))
            throw new InvalidOperationException("production portrait validation did not resolve one authored crop per cataloged unit");
        if (portraits.Select(portrait => portrait!.ResourcePath).Distinct(StringComparer.Ordinal).Count() != units.Length ||
            portraits.Select(portrait => portrait!.StableId).Distinct(StringComparer.Ordinal).Count() != units.Length)
            throw new InvalidOperationException("production units do not own independent portrait resources");
        foreach (var entry in units)
        {
            var portrait = ((UnitDefinition)entry.Definition).Portrait!;
            if (!string.Equals(portrait.StableId, entry.StableId, StringComparison.Ordinal) || portrait.Validate(entry.StableId).HasCoreErrors)
                throw new InvalidOperationException("invalid portrait binding: " + entry.StableId);
        }
    }

    private static void VerifyExpansionConformance(ContentCatalog production)
    {
        var fixture = GD.Load<CatalogEntry>("res://tests/fixtures/fixture_unit_entry.tres")
            ?? throw new InvalidOperationException("expansion fixture load");

        var expanded = CloneCatalog(production);
        expanded.Soldiers.Add(fixture);
        var expandedReport = ContentValidator.ValidateAuthoredEntries(expanded);
        if (expandedReport.HasCoreErrors)
            throw new InvalidOperationException("additional valid content was rejected: " + string.Join("; ", expandedReport.CoreErrors));

        var duplicateIdentity = CloneCatalog(expanded);
        duplicateIdentity.Soldiers.Add(fixture);
        ExpectDiagnostic(
            ContentValidator.ValidateAuthoredEntries(duplicateIdentity),
            "Duplicate content id: fixture_unit", "duplicate identity");

        var invalidReference = CloneCatalog(production);
        invalidReference.Soldiers.Add(new CatalogEntry
        {
            Scene = fixture.Scene,
            Definition = GD.Load<ItemDefinition>("res://tests/fixtures/fixture_item.tres")
                ?? throw new InvalidOperationException("invalid-reference fixture load")
        });
        ExpectDiagnostic(
            ContentValidator.ValidateAuthoredEntries(invalidReference),
            "scene root and catalog do not reference the same definition", "definition reference");

        var invalidCategory = CloneCatalog(production);
        invalidCategory.Heroes.Add(fixture);
        ExpectDiagnostic(
            ContentValidator.ValidateAuthoredEntries(invalidCategory),
            "category flags do not match Hero", "category");
    }

    private static ContentCatalog CloneCatalog(ContentCatalog source)
    {
        var clone = new ContentCatalog();
        foreach (var entry in source.Heroes) clone.Heroes.Add(entry);
        foreach (var entry in source.Soldiers) clone.Soldiers.Add(entry);
        foreach (var entry in source.Enemies) clone.Enemies.Add(entry);
        foreach (var entry in source.Items) clone.Items.Add(entry);
        foreach (var floorRule in source.FloorRules) clone.FloorRules.Add(floorRule);
        return clone;
    }

    private static void ExpectDiagnostic(ValidationReport report, string expected, string label)
    {
        if (!report.HasCoreErrors || !report.CoreErrors.Any(error => error.Contains(expected, StringComparison.Ordinal)))
            throw new InvalidOperationException($"authored-entry validator missed {label}: {string.Join("; ", report.CoreErrors)}");
    }

    private static void VerifyPortraitFingerprintSource()
    {
        var portrait = new UnitPortraitDefinition
        {
            StableId = "fingerprint_probe",
            Frames = GD.Load<SpriteFrames>("res://assets/donor-units/f1_general/frames.tres")
        };
        var definition = new UnitDefinition { Id = "fingerprint_probe", Portrait = portrait };
        var before = DefinitionFingerprint.Compute(definition);
        portrait.Frames = GD.Load<SpriteFrames>("res://assets/donor-units/f1_tank/frames.tres");
        if (before == DefinitionFingerprint.Compute(definition))
            throw new InvalidOperationException("portrait SpriteFrames source path is absent from definition fingerprint");
    }

    private static void RunPresenterFreeBattle(ContentRegistry registry)
    {
        var catalog = registry.Catalog;
        var heroEntry = catalog.Heroes[0];
        var soldierEntry = catalog.Soldiers[0];
        var enemyEntry = catalog.Enemies[0];
        var hero = heroEntry.Scene.Instantiate<UnitContentRoot>();
        var soldier = soldierEntry.Scene.Instantiate<UnitContentRoot>();
        var enemy = enemyEntry.Scene.Instantiate<UnitContentRoot>();
        var floor = catalog.FloorRules[0].Instantiate<FloorRuleContentRoot>();
        var config = new BattleConfig
        {
            Seed = 12345,
            FloorRule = floor.CreateRuntime(),
            HeroRule = BattleSetupFactory.Snapshot(hero.HeroRule!),
            Spawns =
            [
                new BattleSpawn(BattleSetupFactory.Snapshot((UnitDefinition)heroEntry.Definition, hero.Behavior), 0, new Vector2I(1, 2), "hero"),
                new BattleSpawn(BattleSetupFactory.Snapshot((UnitDefinition)soldierEntry.Definition, soldier.Behavior), 0, new Vector2I(2, 2), "soldier"),
                new BattleSpawn(BattleSetupFactory.Snapshot((UnitDefinition)enemyEntry.Definition, enemy.Behavior), 1, new Vector2I(8, 2), "enemy")
            ]
        };
        var result = new BattleSimulation(config).RunToEnd();
        var repeatConfig = new BattleConfig
        {
            Seed = config.Seed,
            FloorRule = floor.CreateRuntime(),
            HeroRule = config.HeroRule,
            Spawns = config.Spawns
        };
        var repeat = new BattleSimulation(repeatConfig).RunToEnd();
        if (result.Outcome == BattleOutcome.Running || string.IsNullOrWhiteSpace(result.Digest))
            throw new InvalidOperationException("presenter-free battle");
        if (result.Digest != repeat.Digest || result.Outcome != repeat.Outcome || result.Ticks != repeat.Ticks)
            throw new InvalidOperationException("fixed-seed battle digest");
        hero.Free();
        soldier.Free();
        enemy.Free();
        floor.Free();
    }

    private static void RunSaveRoundTrip(ContentRegistry registry)
    {
        var catalog = registry.Catalog;
        var run = new ActiveRunDto
        {
            Seed = 87,
            Roster = [new RosterHeroInstanceDto
            {
                InstanceId = "player-hero",
                ContentId = catalog.Heroes[0].StableId
            }],
            CurrentPopulation = 1,
            Items = [new ItemInstanceDto { InstanceId = "item-1", ContentId = catalog.Items[0].StableId, Stacks = 2, Charges = 3, Roll = 4 }]
        };
        var project = TestProjectFixture.Load(registry);
        ActiveRunFormationSchema.InitializeVersion4(run, project.RunRules);
        run.Deployment[BattlefieldLayout.PlayerDeploymentSlot(BattlefieldLayout.Version2HeroCell)] = "player-hero";
        var service = new SaveService("tests/content-contract");
        var restored = service.Deserialize<ActiveRunDto>(service.Serialize(run)) ?? throw new InvalidOperationException("save deserialize");
        var restoredItem = restored.Items.SingleOrDefault();
        if (restored.Version != 4 || restored.Roster.Single().InstanceId != "player-hero" ||
            restored.Deployment.Count != 18 ||
            restoredItem is null || restoredItem.InstanceId != "item-1" || restoredItem.ContentId != run.Items[0].ContentId ||
            restoredItem.Stacks != 2 || restoredItem.Charges != 3 || restoredItem.Roll != 4)
            throw new InvalidOperationException("save round-trip");
        if (!service.SaveActiveRun(run)) throw new InvalidOperationException("save write");
        var diskRestored = service.LoadActiveRun();
        var diskItem = diskRestored?.Items.SingleOrDefault();
        if (diskItem?.InstanceId != "item-1" || diskItem.ContentId != run.Items[0].ContentId ||
            diskItem.Stacks != 2 || diskItem.Charges != 3 || diskItem.Roll != 4)
            throw new InvalidOperationException("disk save round-trip");

        if (new RunApplication(registry, service, project).ActiveRun is null)
            throw new InvalidOperationException("valid active run rejected");

        run.Roster.Add(new RosterHeroInstanceDto { InstanceId = "", ContentId = catalog.Soldiers[0].StableId });
        service.SaveActiveRun(run);
        if (new RunApplication(registry, service, project).ActiveRun is not null)
            throw new InvalidOperationException("blank unit instance id accepted");
        run.Roster.RemoveAt(run.Roster.Count - 1);
        run.Items[0].InstanceId = " ";
        service.SaveActiveRun(run);
        if (new RunApplication(registry, service, project).ActiveRun is not null)
            throw new InvalidOperationException("blank item instance id accepted");
        run.Items[0].InstanceId = "item-1";
        run.Items[0].Charges = -1;
        service.SaveActiveRun(run);
        if (new RunApplication(registry, service, project).ActiveRun is not null)
            throw new InvalidOperationException("negative relic charges accepted");
        service.DeleteActiveRun();
    }

    private static void RunSourceGuard()
    {
        var sourceRoot = ProjectSettings.GlobalizePath("res://src");
        foreach (var path in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(path);
            var relative = Path.GetRelativePath(sourceRoot, path);
            var strictTreeBoundary = relative.StartsWith("Content" + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                                     relative.StartsWith("Battle" + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                                     relative.StartsWith("Components" + Path.DirectorySeparatorChar, StringComparison.Ordinal);
            var issues = ProductionSourceGuard.FindIssues(source, true, strictTreeBoundary);
            if (issues.Count > 0) throw new InvalidOperationException($"production source guard in {path}: {string.Join("; ", issues)}");
        }

        var families = ProductionSourceGuard.GuardedConcreteIdFamilies;
        var requiredFamilies = new HashSet<string>(StringComparer.Ordinal)
        {
            "hero", "soldier", "enemy", "item", "rule", "effect", "ability", "status", "relic", "equipment",
            "tactical", "trait", "encounter", "campaign", "project", "pool", "phase", "timeline", "region",
            "reward", "loadout"
        };
        var duplicateFamilies = families
            .GroupBy(family => family, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(family => family, StringComparer.Ordinal)
            .ToArray();
        if (duplicateFamilies.Length > 0)
            throw new InvalidOperationException("source guard contains duplicate concrete-id families: " + string.Join(", ", duplicateFamilies));
        var missingFamilies = requiredFamilies
            .Except(families, StringComparer.Ordinal)
            .OrderBy(family => family, StringComparer.Ordinal)
            .ToArray();
        if (missingFamilies.Length > 0)
            throw new InvalidOperationException("source guard is missing required concrete-id families: " + string.Join(", ", missingFamilies));

        foreach (var family in families)
        {
            var probeId = family + "_guard_probe";
            ExpectExactGuardIssue($"\"{probeId}\"", false, $"concrete id '{probeId}'", family + " concrete id");
            ExpectNoGuardIssue($"\"{family}\"", false, family + " prefix-only near miss");
            ExpectNoGuardIssue($"\"{family}_\"", false, family + " incomplete concrete-id near miss");
        }
        ExpectExactGuardIssue(
            "\"trait_guard_probe\"",
            false,
            "concrete id 'trait_guard_probe'",
            "Trait concrete id");
        ExpectNoGuardIssue("\"trait\"", false, "Trait prefix-only near miss");
        ExpectNoGuardIssue("\"trait_\"", false, "Trait incomplete concrete-id near miss");
        ExpectNoGuardIssue("\"tactical_command\"", false, "tactical command semantic allowlist");
        ExpectNoGuardIssue("\"tactical_point\"", false, "tactical point semantic allowlist");
        ExpectGuardIssue("\"D:\\\\godot\\\\rpg\\\\content.tres\"", false, "escaped donor absolute path");
        ExpectGuardIssue("\"D:/godot/rpg/content.tres\"", false, "slash donor absolute path");
        ExpectGuardIssue("GetNode(\"/" + "root/App\")", true, "root path");
        ExpectGuardIssue("GetTree().Current" + "Scene", true, "current scene");
        ExpectGuardIssue("GetTree().Get" + "Root()", true, "root getter");
        ExpectGuardIssue("GetNodesIn" + "Group(\"units\")", true, "group discovery");
        ExpectGuardIssue("Call" + "Group(\"units\", \"Wake\")", true, "group call");
        ExpectGuardIssue("Get" + "Parent()", true, "parent traversal", true);
        ExpectGuardIssue("new NodePath(\"" + "../Battle\")", true, "cross-root node path", true);
    }

    private static void ExpectGuardIssue(
        string source,
        bool checkDiscovery,
        string label,
        bool checkLocalTreeTraversal = false)
    {
        if (ProductionSourceGuard.FindIssues(source, checkDiscovery, checkLocalTreeTraversal).Count == 0)
            throw new InvalidOperationException("source guard missed " + label);
    }

    private static void ExpectExactGuardIssue(
        string source,
        bool checkDiscovery,
        string expectedIssue,
        string label,
        bool checkLocalTreeTraversal = false)
    {
        var issues = ProductionSourceGuard.FindIssues(source, checkDiscovery, checkLocalTreeTraversal);
        if (issues.Count != 1 || !string.Equals(issues[0], expectedIssue, StringComparison.Ordinal))
            throw new InvalidOperationException($"source guard returned unexpected evidence for {label}: [{string.Join("; ", issues)}]");
    }

    private static void ExpectNoGuardIssue(
        string source,
        bool checkDiscovery,
        string label,
        bool checkLocalTreeTraversal = false)
    {
        var issues = ProductionSourceGuard.FindIssues(source, checkDiscovery, checkLocalTreeTraversal);
        if (issues.Count > 0)
            throw new InvalidOperationException($"source guard false positive for {label}: [{string.Join("; ", issues)}]");
    }

    private async Task RunReadyGateFailureContracts(ContentCatalog catalog)
    {
        await ExpectReadyGateFailure(catalog, "res://tests/fixtures/content_instantiate_failure.tscn", "CONTENT_GATE_INSTANTIATE_FAILURE");
        await ExpectReadyGateFailure(catalog, "res://tests/fixtures/content_ready_failure.tscn", "CONTENT_GATE_READY_FAILURE");
        await ExpectReadyGateFailure(catalog, "res://tests/fixtures/content_process_failure.tscn", "CONTENT_GATE_PROCESS_FAILURE");
        await ExpectReadyGateFailure(catalog, "res://tests/fixtures/content_exit_failure.tscn", "CONTENT_GATE_EXIT_FAILURE");
    }

    private async Task RunStructuralGateFailureContract(ContentCatalog catalog)
    {
        const string marker = "CONTENT_GATE_STRUCTURAL_INSTANTIATE_FAILURE";
        var scene = GD.Load<PackedScene>("res://tests/fixtures/content_structural_instantiate_failure.tscn")
            ?? throw new InvalidOperationException("structural failure fixture load");
        var gate = await TestProjectFixture.PublishAsync(
            this, additionalStructuralValidationScenes: [scene]);
        var matches = gate.Report.CoreErrors.Count(error => error.Contains(marker, StringComparison.Ordinal));
        if (gate.Package is not null || matches != 1)
            throw new InvalidOperationException($"structural gate did not capture the one-shot first-pass failure exactly once: {matches}");
    }

    private async Task ExpectReadyGateFailure(ContentCatalog catalog, string scenePath, string marker)
    {
        var scene = GD.Load<PackedScene>(scenePath) ?? throw new InvalidOperationException("failure fixture load: " + scenePath);
        var gate = await TestProjectFixture.PublishAsync(this, [scene]);
        if (gate.Package is not null || !gate.Report.CoreErrors.Any(error => error.Contains(marker, StringComparison.Ordinal)))
            throw new InvalidOperationException("ready-frame gate did not reject lifecycle failure: " + marker);
    }

    private static void RunLifecycleRollbackContracts(ContentRegistry registry)
    {
        var catalog = registry.Catalog;
        var unit = catalog.Heroes[0].Scene.Instantiate<UnitContentRoot>();
        try
        {
            unit.Bind("rollback-unit", 0, 1, 1);
            ExpectThrows(() => unit.Activate(new UnitBindingContext(new DeterministicRandom(1), new ThrowingEventSink(SemanticBattleEventType.Activated), new FakeCommandGateway())), "unit activate rollback");
            if (unit.LifecycleState != ContentLifecycleState.Bound || unit.IsActive)
                throw new InvalidOperationException("unit activation rollback state");

            unit.Activate(new UnitBindingContext(new DeterministicRandom(1), new ThrowingEventSink(SemanticBattleEventType.Deactivated), new FakeCommandGateway()));
            ExpectThrows(unit.Deactivate, "unit deactivate cleanup");
            if (unit.LifecycleState != ContentLifecycleState.Bound || unit.IsActive)
                throw new InvalidOperationException("unit deactivation cleanup state");

            unit.Activate(new UnitBindingContext(new DeterministicRandom(1), new ThrowingEventSink(SemanticBattleEventType.Deactivated), new FakeCommandGateway()));
            ExpectThrows(unit._ExitTree, "unit exit-tree cleanup");
            if (unit.LifecycleState != ContentLifecycleState.Unbound || unit.IsActive || !string.IsNullOrEmpty(unit.RuntimeId))
                throw new InvalidOperationException("unit exit-tree cleanup state");
        }
        finally { unit.Free(); }

        var item = catalog.Items[0].Scene.Instantiate<ItemContentRoot>();
        try
        {
            ExpectThrows(() => item.Bind(new ItemInstanceState { InstanceId = " " }), "blank item instance id");
            item.Bind(new ItemInstanceState { InstanceId = "rollback-item", ContentId = "wrong_content" });
            using var relics = new RelicRunScope(new RelicRunKey(1, catalog.Heroes[0].StableId, 0, 0));
            ExpectThrows(
                () => item.Activate(new ItemBindingContext(relics, registry.Graph.ResolveRelic(item.Definition.Id))),
                "item activation rollback");
            if (item.LifecycleState != ContentLifecycleState.Bound || relics.LiveRunInstanceCount != 0)
                throw new InvalidOperationException("item activation rollback state");
        }
        finally { item.Free(); }
    }

    private static void ExpectThrows(Action action, string name)
    {
        try { action(); }
        catch (Exception) { return; }
        throw new InvalidOperationException("expected failure: " + name);
    }

    private sealed class FakeEventSink : ISemanticBattleEventSink
    {
        public List<SemanticBattleEvent> Events { get; } = [];
        public void Publish(SemanticBattleEvent battleEvent) => Events.Add(battleEvent);
    }

    private sealed class ThrowingEventSink(SemanticBattleEventType type) : ISemanticBattleEventSink
    {
        public void Publish(SemanticBattleEvent battleEvent)
        {
            if (battleEvent.Type == type) throw new InvalidOperationException("expected sink failure");
        }
    }

    private sealed class FakeCommandGateway : IBattleCommandGateway
    {
        public bool Submit(BattleCommandRequest command) => command.Type == BattleCommandType.UseTacticalCommand && !string.IsNullOrWhiteSpace(command.SourceRuntimeId);
    }

}
