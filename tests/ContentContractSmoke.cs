using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
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
            var gate = await ContentRegistry.CreateReadyAsync(this, catalog);
            var registry = gate.Registry ?? throw new InvalidOperationException("content gate: " + string.Join("; ", gate.Report.CoreErrors));
            VerifyPortraitCoverage(catalog);
            VerifyPortraitFingerprintSource();
            await RunStructuralGateFailureContract(catalog);
            await RunReadyGateFailureContracts(catalog);

            var before = catalog.AllEntries().ToDictionary(entry => entry.StableId, entry => DefinitionFingerprint.Compute(entry.Definition), StringComparer.Ordinal);
            var events = new FakeEventSink();
            var commands = new FakeCommandGateway();
            var unitContext = new UnitBindingContext(new DeterministicRandom(71), events, commands);
            var modifierRegistry = new FakeModifierRegistry();

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
                    item.Bind(new ItemInstanceState { InstanceId = "smoke-" + entry.StableId, Stacks = 2, Charges = 1, Roll = 9 });
                    item.Activate(new ItemBindingContext(modifierRegistry));
                    ExpectThrows(() => item.Bind(new ItemInstanceState()), "active item rebind");
                    item.Deactivate();
                    if (modifierRegistry.Active != 0 || item.LifecycleState != ContentLifecycleState.Bound) throw new InvalidOperationException("item lifecycle");
                }
                node.Free();
            }
            RunLifecycleRollbackContracts(catalog);
            if (!events.Events.Any(e => e.Type == SemanticBattleEventType.Activated) ||
                !commands.Submit(new BattleCommandRequest(BattleCommandType.UseHeroCommand, "smoke-command")))
                throw new InvalidOperationException("typed binding communication");

            foreach (var scene in catalog.FloorRules)
            {
                var floor = scene.Instantiate<FloorRuleContentRoot>();
                if (floor.ValidateAuthoring().HasCoreErrors) throw new InvalidOperationException("floor validation");
                floor.Free();
            }

            RunPresenterFreeBattle(catalog);
            RunSaveRoundTrip(registry);
            RunSourceGuard();

            foreach (var entry in catalog.AllEntries())
                if (before[entry.StableId] != DefinitionFingerprint.Compute(entry.Definition))
                    throw new InvalidOperationException("definition mutated: " + entry.StableId);

            GD.Print($"CONTENT_CONTRACT_OK entries={catalog.AllEntries().Count} floors={catalog.FloorRules.Count} events={events.Events.Count} portraits=45(8,24,13)");
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
        if (catalog.Heroes.Count != 8 || catalog.Soldiers.Count != 24 || catalog.Enemies.Count != 13)
            throw new InvalidOperationException("portrait coverage catalog counts changed");
        var units = catalog.Heroes.Concat(catalog.Soldiers).Concat(catalog.Enemies).ToArray();
        var portraits = units.Select(entry => ((UnitDefinition)entry.Definition).Portrait).ToArray();
        if (portraits.Length != 45 || portraits.Any(portrait => portrait is null ||
                string.IsNullOrWhiteSpace(portrait.ResourcePath) || portrait.ResolveTexture() is null ||
                portrait.Zoom is < .5f or > 4f || Math.Abs(portrait.OffsetRatio.X) > 1 || Math.Abs(portrait.OffsetRatio.Y) > 1))
            throw new InvalidOperationException("production portrait validation did not resolve 45 authored crops");
        if (portraits.Select(portrait => portrait!.ResourcePath).Distinct(StringComparer.Ordinal).Count() != 45 ||
            portraits.Select(portrait => portrait!.StableId).Distinct(StringComparer.Ordinal).Count() != 45)
            throw new InvalidOperationException("production units do not own independent portrait resources");
        foreach (var entry in units)
        {
            var portrait = ((UnitDefinition)entry.Definition).Portrait!;
            if (!string.Equals(portrait.StableId, entry.StableId, StringComparison.Ordinal) || portrait.Validate(entry.StableId).HasCoreErrors)
                throw new InvalidOperationException("invalid portrait binding: " + entry.StableId);
        }
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

    private static void RunPresenterFreeBattle(ContentCatalog catalog)
    {
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
            HeroRule = BattleSetupFactory.Snapshot(hero.HeroRule!, hero.HeroCommand!),
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
            HeroId = catalog.Heroes[0].StableId,
            Items = [new ItemInstanceDto { InstanceId = "item-1", ContentId = catalog.Items[0].StableId, Stacks = 2, Charges = 3, Roll = 4 }]
        };
        var service = new SaveService("tests/content-contract");
        var restored = service.Deserialize<ActiveRunDto>(service.Serialize(run)) ?? throw new InvalidOperationException("save deserialize");
        if (restored.Version != 2 || restored.Items.Count != 1 || restored.Items[0].ContentId != run.Items[0].ContentId || restored.Items[0].Stacks != 2)
            throw new InvalidOperationException("save round-trip");
        if (!service.SaveActiveRun(run)) throw new InvalidOperationException("save write");
        var diskRestored = service.LoadActiveRun();
        if (diskRestored?.Items.SingleOrDefault()?.Roll != 4) throw new InvalidOperationException("disk save round-trip");

        var regions = new[]
        {
            GD.Load<TowerRegionDefinition>("res://content/tower/region_ember_foundry.tres"),
            GD.Load<TowerRegionDefinition>("res://content/tower/region_gloam_crypt.tres"),
            GD.Load<TowerRegionDefinition>("res://content/tower/region_crown_engine.tres")
        };
        if (new RunApplication(registry, service, regions).ActiveRun is null)
            throw new InvalidOperationException("valid active run rejected");

        run.Roster.Add(new UnitInstanceDto { InstanceId = "", ContentId = catalog.Soldiers[0].StableId });
        service.SaveActiveRun(run);
        if (new RunApplication(registry, service, regions).ActiveRun is not null)
            throw new InvalidOperationException("blank unit instance id accepted");
        run.Roster.Clear();
        run.Items[0].InstanceId = " ";
        service.SaveActiveRun(run);
        if (new RunApplication(registry, service, regions).ActiveRun is not null)
            throw new InvalidOperationException("blank item instance id accepted");
        service.DeleteActiveRun();
    }

    private static void RunSourceGuard()
    {
        var sourceRoot = ProjectSettings.GlobalizePath("res://src");
        foreach (var path in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(path);
            var relative = Path.GetRelativePath(sourceRoot, path);
            var guardedBoundary = relative.StartsWith("Content" + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                                  relative.StartsWith("Battle" + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                                  relative.StartsWith("Components" + Path.DirectorySeparatorChar, StringComparison.Ordinal);
            var issues = ProductionSourceGuard.FindIssues(source, guardedBoundary);
            if (issues.Count > 0) throw new InvalidOperationException($"production source guard in {path}: {string.Join("; ", issues)}");
        }

        ExpectGuardIssue("\"item_sword\"", false, "single-segment concrete id");
        ExpectGuardIssue("GetNode(\"/" + "root/App\")", true, "root path");
        ExpectGuardIssue("GetTree().Current" + "Scene", true, "current scene");
        ExpectGuardIssue("GetTree().Get" + "Root()", true, "root getter");
        ExpectGuardIssue("GetNodesIn" + "Group(\"units\")", true, "group discovery");
        ExpectGuardIssue("Call" + "Group(\"units\", \"Wake\")", true, "group call");
        ExpectGuardIssue("Get" + "Parent()", true, "parent traversal");
        ExpectGuardIssue("new NodePath(\"" + "../Battle\")", true, "cross-root node path");
    }

    private static void ExpectGuardIssue(string source, bool checkDiscovery, string label)
    {
        if (ProductionSourceGuard.FindIssues(source, checkDiscovery).Count == 0)
            throw new InvalidOperationException("source guard missed " + label);
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
        var gate = await ContentRegistry.CreateReadyAsync(
            this, catalog, additionalStructuralValidationScenes: [scene]);
        var matches = gate.Report.CoreErrors.Count(error => error.Contains(marker, StringComparison.Ordinal));
        if (gate.Registry is not null || matches != 1)
            throw new InvalidOperationException($"structural gate did not capture the one-shot first-pass failure exactly once: {matches}");
    }

    private async Task ExpectReadyGateFailure(ContentCatalog catalog, string scenePath, string marker)
    {
        var scene = GD.Load<PackedScene>(scenePath) ?? throw new InvalidOperationException("failure fixture load: " + scenePath);
        var gate = await ContentRegistry.CreateReadyAsync(this, catalog, [scene]);
        if (gate.Registry is not null || !gate.Report.CoreErrors.Any(error => error.Contains(marker, StringComparison.Ordinal)))
            throw new InvalidOperationException("ready-frame gate did not reject lifecycle failure: " + marker);
    }

    private static void RunLifecycleRollbackContracts(ContentCatalog catalog)
    {
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
            item.Bind(new ItemInstanceState { InstanceId = "rollback-item" });
            ExpectThrows(() => item.Activate(new ItemBindingContext(new ThrowingModifierRegistry())), "item activation rollback");
            if (item.LifecycleState != ContentLifecycleState.Bound)
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
        public bool Submit(BattleCommandRequest command) => command.Type == BattleCommandType.UseHeroCommand && !string.IsNullOrWhiteSpace(command.SourceRuntimeId);
    }

    private sealed class FakeModifierRegistry : IRunModifierRegistry
    {
        public int Active { get; private set; }
        public IDisposable Register(string itemInstanceId, RunModifierProviderComponent provider)
        {
            Active++;
            return new Registration(() => Active--);
        }
    }

    private sealed class ThrowingModifierRegistry : IRunModifierRegistry
    {
        public IDisposable Register(string itemInstanceId, RunModifierProviderComponent provider) =>
            throw new InvalidOperationException("expected registration failure");
    }

    private sealed class Registration(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose() { _dispose?.Invoke(); _dispose = null; }
    }
}
