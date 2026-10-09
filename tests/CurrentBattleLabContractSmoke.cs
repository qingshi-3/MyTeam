using System;
using System.Linq;
using System.Text.Json;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

// Current authored content only. No player saves or user-preset writes.
public partial class CurrentBattleLabContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var publication = await GrowthContentPackage.CreateReadyAsync(this);
            var package = publication.Package ?? throw new InvalidOperationException(
                string.Join("; ", publication.Report.CoreErrors));
            var index = new BattleLabContentIndex(package);
            var adapter = new BattleLabPreparationAdapter(index);
            var catalog = GD.Load<BattleLabPresetCatalog>("res://content/battle-lab/battle_lab_presets.tres")
                ?? throw new InvalidOperationException("Current lab catalog missing.");
            var store = new BattleLabPresetStore(catalog);
            Require(store.TryLoad(store.DefaultPresetName, out var initial), "authored default loads");
            Require(initial.Units.Count(unit => unit.Side == BattleLabSide.Player) == 2 &&
                initial.Units.Count(unit => unit.Side == BattleLabSide.Enemy) == 2,
                "default exposes a real editable 2v2 battle");
            Require(initial.Units.Where(unit => unit.Side == BattleLabSide.Enemy).All(unit =>
                index.TryGetUnit(unit.ContentId, out var published) && published.Definition.IsEnemy &&
                !published.Definition.IsTestDummy), "default opponents are current actual enemies");
            var expectedEquipmentIds = new[]
                { "v1_equipment_e02", "v1_equipment_e03", "v1_equipment_e06", "v1_equipment_e07", "v1_equipment_e10", "v1_equipment_e11", "v1_equipment_e15" };
            var expectedRelicIds = Enumerable.Range(1, 11).Select(number => $"v1_relic_r{number:00}").ToArray();
            Require(index.Equipment.Length == 7 && index.Equipment.Select(item => item.StableId)
                    .Order(StringComparer.Ordinal).SequenceEqual(expectedEquipmentIds.Order(StringComparer.Ordinal)) &&
                index.Relics.Length == 11 && index.Relics.Select(item => item.StableId)
                    .Order(StringComparer.Ordinal).SequenceEqual(expectedRelicIds),
                "only the current seven equipment and eleven relics are published");

            foreach (var (name, dto) in store.BuiltIns)
            {
                Require(dto.CurrentPopulation == index.Rules.InitialPopulation &&
                    dto.Mode == BattleLabPlacementMode.FreeExperiment &&
                    package.Project.FloorRules.ContainsKey(dto.FloorRuleId), "published preset metadata: " + name);
                Require(dto.Relics.Count == 0 && dto.Units.All(unit => unit.Equipment.Count == 0),
                    "preset does not revive retired items: " + name);
                var snapshot = BattleLabPresetStore.ToSnapshot(dto);
                var session = new BattleLabSession(index, dto.CurrentPopulation,
                    dto.Seed, dto.Mode, dto.FloorRuleId);
                session.Restore(snapshot);
                var frozen = session.Freeze().CanonicalDigest;
                var config = adapter.Build(snapshot);
                Require(config.CaptureDetailedStatistics, "lab enables detailed statistics: " + name);
                using var battle = new BattleSimulation(config);
                if (name == "森林 · 开场复制")
                    Require(battle.Units.Any(unit => unit.Team == 0 && unit.IsTemporary),
                        "forest preset executes its production opening copy");
                var result = battle.RunToEnd();
                Require(result.Outcome != BattleOutcome.Running && result.Ticks <= BattleSimulation.MaxTicks,
                    "preset reaches a bounded terminal result: " + name);
                Require(result.Units.All(unit => unit.DetailedStatistics is not null),
                    "every reported unit retains details: " + name);
                Require(session.Freeze().CanonicalDigest == frozen,
                    "battle does not write runtime mutation into editable configuration: " + name);
                GD.Print($"CURRENT_BATTLE_LAB_PRESET name={name} outcome={result.Outcome} ticks={result.Ticks} units={result.Units.Length}");
            }

            using var emptyCatalog = new BattleLabPresetCatalog();
            var emptyStore = new BattleLabPresetStore(emptyCatalog);
            Require(emptyStore.BuiltIns.Count == 0 && emptyStore.DefaultPresetName.Length == 0,
                "truly empty catalog supports a blank lab");
            var blank = new BattleLabSession(index, index.Rules.InitialPopulation);
            Require(adapter.BuildProjection(blank.Freeze()).Spawns.Count == 0, "blank lab projects safely");
            Require(blank.AddAndPlace("prototype_u02", BattleLabSide.Player, new(2, 2)).Succeeded &&
                blank.AddAndPlace("enemy_ee03_trample_brute", BattleLabSide.Enemy, new(7, 2)).Succeeded,
                "blank lab remains editable from current published library");
            Require(adapter.Build(blank.Freeze()).CaptureDetailedStatistics, "edited blank lab prepares actual battle");
            using var dangling = new BattleLabPresetCatalog { DefaultPresetName = "不存在" };
            ExpectRejected(() => new BattleLabPresetStore(dangling), "empty presets with dangling default");
            using var malformed = new BattleLabPresetCatalog();
            using var malformedDefinition = new BattleLabBuiltInPreset();
            malformed.Presets.Add(malformedDefinition);
            ExpectRejected(() => new BattleLabPresetStore(malformed), "nonempty malformed catalog");
            using var populated = new BattleLabPresetCatalog { DefaultPresetName = "不存在" };
            using var validDefinition = new BattleLabBuiltInPreset
            {
                DisplayName = "有效预设",
                PresetJson = JsonSerializer.Serialize(initial)
            };
            populated.Presets.Add(validDefinition);
            ExpectRejected(() => new BattleLabPresetStore(populated), "nonempty invalid default");
            VerifyRunStatisticsBoundary(package);
            GD.Print($"CURRENT_BATTLE_LAB_CONTRACT_OK presets={store.BuiltIns.Count} empty-catalog-editable invalid-catalog-rejected current-content detailed-lab compact-run");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PrintErr("CURRENT_BATTLE_LAB_CONTRACT_FAILED " + error);
            GetTree().Quit(1);
        }
    }

    private static void VerifyRunStatisticsBoundary(CompiledGamePackage package)
    {
        var expedition = (CompiledExpedition)package.Project.Campaign.Progression;
        var profile = expedition.Encounters["ember_combat_profile"];
        var plan = new EncounterPlan(profile.Encounter.Title("实验室边界检查"),
            profile.Encounter.FloorRulePool.ContentIds[0], profile.Composition.EnemyIds,
            false, false, profile.Encounter.StableId, profile.Encounter.NodeType)
        {
            CompositionId = profile.Composition.StableId,
            EnemyCells = profile.Composition.Cells,
            ExpeditionProfileId = profile.StableId,
            ExpeditionSiteId = "current-lab-contract"
        };
        var run = new ActiveRunDto
        {
            ProfileId = package.DefaultNewRunProfileId,
            Seed = 20261006,
            CurrentPopulation = package.Project.RunRules.InitialPopulation,
            CurrentRunHealth = 100,
            MaximumRunHealth = 100,
            Growth = new GrowthRunDto { RulesId = package.GrowthRules!.StableId }
        };
        run.Roster.Add(new RosterHeroInstanceDto { InstanceId = "run-hero-1", ContentId = "prototype_u02" });
        run.Roster.Add(new RosterHeroInstanceDto { InstanceId = "run-hero-2", ContentId = "prototype_u03" });
        run.Deployment[BattlefieldLayout.PlayerDeploymentSlot(new(2, 2))] = "run-hero-1";
        run.Deployment[BattlefieldLayout.PlayerDeploymentSlot(new(0, 4))] = "run-hero-2";
        var preparation = new RunBattlePreparationService(package.Content, package.Project,
            new RunRelicService(package.Content), package.GrowthRules);
        var config = preparation.Build(run, plan, true);
        Require(!config.CaptureDetailedStatistics, "formal run preparation retains compact statistics");
        using var battle = new BattleSimulation(config);
        Require(battle.ReadUnitReports().All(unit => unit.DetailedStatistics is null),
            "formal run reports do not acquire lab detail storage");
    }

    private static void ExpectRejected(Action action, string reason)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("Catalog was accepted: " + reason);
    }

    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException(reason);
    }
}
