using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.UI;

// In-memory rule and scene-binding checks. No windows, player saves or preset writes.
public partial class BattleLabOpenUnitsContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var index = new BattleLabContentIndex(package);
            var adapter = new BattleLabPreparationAdapter(index);
            Require(index.Units.Select(unit => unit.StableId).ToHashSet().SetEquals(
                package.Content.Catalog.AllEntries().Where(entry => entry?.Definition is UnitDefinition)
                    .Select(entry => ((UnitDefinition)entry.Definition).Id)), "every published unit reaches the unified index");

            foreach (var unit in index.Units)
            {
                var session = new BattleLabSession(index, 1);
                var a = Place(session, unit.StableId, BattleLabSide.Player, new(2, 2));
                var b = Place(session, unit.StableId, BattleLabSide.Enemy, new(7, 2));
                float baseArmor;
                using (var plain = new BattleSimulation(adapter.Build(session.Freeze())))
                {
                    baseArmor = plain.Units[0].Armor;
                    Require(plain.Units[0].MaxHealth == plain.Units[1].MaxHealth &&
                        plain.Units[0].Damage == plain.Units[1].Damage, "neutral preparation: " + unit.StableId);
                }
                const string equipment = "equipment_vanguard_insignia";
                Require(session.Equip(a, 0, equipment) && session.Equip(b, 0, equipment), "either team can equip");
                var config = adapter.Build(session.Freeze());
                Require(config.Equipment.Instances.Length == 2, "both equipment sources prepared");
                using var battle = new BattleSimulation(config);
                Require(battle.Units[0].MaxHealth == battle.Units[1].MaxHealth &&
                    battle.Units[0].Damage == battle.Units[1].Damage &&
                    battle.Units[0].Armor == battle.Units[1].Armor, "equipment symmetry: " + unit.StableId);
                if (unit.Definition.IsTestDummy)
                    Require(Mathf.IsEqualApprox(battle.Units[1].Armor, baseArmor + 12),
                        "team B equipment changes actual combat attributes");
                battle.Step();
                var copy = new BattleLabSession(index, 1);
                copy.Restore(BattleLabPresetStore.ToSnapshot(BattleLabPresetStore.ToDto(session.Freeze())));
                Require(copy.Freeze().CanonicalDigest == session.Freeze().CanonicalDigest, "preset round trip");
                var item = session.Units.Single(value => value.InstanceId == a).Equipment.Single().InstanceId;
                Require(session.MoveEquipment(item, b, 1) &&
                    session.Units.Single(value => value.InstanceId == b).Equipment.Any(value => value.InstanceId == item),
                    "equipment transfers across test teams preserving identity");
            }

            var free = new BattleLabSession(index, 1, mode: BattleLabPlacementMode.Formal);
            var hero = index.PlayerHeroes[0].StableId;
            foreach (var cell in new[] { new Vector2I(1, 1), new(3, 1), new(5, 1), new(7, 1), new(8, 4) })
                Place(free, hero, BattleLabSide.Player, cell);
            var opposite = Place(free, hero, BattleLabSide.Enemy, new(1, 4));
            Require(free.Units.Count == 6 && free.Mode == BattleLabPlacementMode.FreeExperiment,
                "legacy formal constructor cannot reintroduce population or side regions");
            Require(free.Move(opposite, new(8, 4)).SwappedInstanceId is not null, "cross-team swap");
            var before = free.Freeze().CanonicalDigest;
            Require(!free.AddAndPlace("enemy_ee03_trample_brute", BattleLabSide.Enemy, new(0, 0)).Succeeded &&
                free.Freeze().CanonicalDigest == before, "large body boundary remains atomic");
            Require(!free.AddAndPlace(hero, (BattleLabSide)17, new(3, 4)).Succeeded, "invalid team still rejected");
            var frozen = free.Freeze();
            var legacy = frozen with { Mode = BattleLabPlacementMode.Formal,
                CanonicalDigest = BattleLabSession.CanonicalDigest(BattleLabPlacementMode.Formal, frozen.CurrentPopulation,
                    frozen.Seed, frozen.FloorRuleId, frozen.PrimaryHeroInstanceId, frozen.Units, frozen.Relics) };
            free.Restore(legacy);
            Require(free.Mode == BattleLabPlacementMode.FreeExperiment && free.Units.Count == 6,
                "legacy presets become free in memory without losing units");
            try { free.Restore(legacy with { CanonicalDigest = "tampered" }); throw new Exception("tamper accepted"); }
            catch (InvalidOperationException) { }
            Require(free.Freeze().CanonicalDigest == before, "failed restore leaves session untouched");

            var bosses = new BattleLabSession(index, 1);
            Place(bosses, "enemy_eb01_rampart_warden", BattleLabSide.Player, new(2, 1));
            Place(bosses, "enemy_eb02_shell_matriarch", BattleLabSide.Enemy, new(7, 1));
            Place(bosses, "enemy_eb02_shell_matriarch", BattleLabSide.Player, new(4, 4));
            var bossConfig = adapter.Build(bosses.Freeze());
            Require(bossConfig.AdditionalBossTimelines.Length == 2, "multiple boss timelines retained");
            using (var one = new BattleSimulation(bossConfig))
            using (var two = new BattleSimulation(bossConfig))
            {
                Require(one.Units.All(unit => unit.BossPhaseId.Length > 0), "each boss instance starts its own phase on either team");
                for (var tick = 0; tick < 100; tick++) { one.Step(); two.Step(); }
                Require(one.CreateResult().Digest == two.CreateResult().Digest, "mixed bosses stay deterministic");
                Require(one.PendingEvents.Any(e => e.Type == "enemy_action"), "boss abilities actually execute");
            }
            var trample = new BattleLabSession(index, 1);
            var trampleId = Place(trample, "enemy_ee03_trample_brute", BattleLabSide.Player, new(2, 2));
            Place(trample, "soldier_dummy_static", BattleLabSide.Enemy, new(7, 2));
            using (var battle = new BattleSimulation(adapter.Build(trample.Freeze())))
            {
                for (var tick = 0; tick < 80; tick++) battle.Step();
                Require(battle.PendingEvents.Any(e => e.Type == "trample_rush" && e.SourceRuntimeId == trampleId),
                    "EE03 really charges from team A");
            }

            var store = new BattleLabPresetStore(GD.Load<BattleLabPresetCatalog>("res://content/battle-lab/battle_lab_presets.tres"));
            foreach (var preset in store.BuiltIns)
            {
                using var battle = new BattleSimulation(adapter.Build(BattleLabPresetStore.ToSnapshot(preset.Value)));
                battle.Step();
            }
            var screen = GD.Load<PackedScene>("res://scenes/ui/BattleLabScreen.tscn").Instantiate<BattleLabScreenController>();
            AddChild(screen);
            screen.Bind(index, bosses);
            Require(Descendants<BattleLabLibraryCard>(screen).Count() == index.Units.Length,
                "authored screen starts on all units");
            var team = screen.GetNode<OptionButton>("%PlacementTeam");
            team.Select(1); team.EmitSignal(OptionButton.SignalName.ItemSelected, 1L);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(Descendants<BattleLabLibraryCard>(screen).Count() == index.Units.Length &&
                Descendants<BattleLabLibraryCard>(screen).All(card => card.Side == BattleLabSide.Enemy),
                "team selector binds the same complete library");
            screen.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GD.Print($"BATTLE_LAB_OPEN_UNITS_OK units={index.Units.Length} presets={store.BuiltIns.Count}; both teams, equipment, free placement, legacy restore, bosses, charge, UI bindings");
            GetTree().Quit();
        }
        catch (Exception exception) { GD.PrintErr("BATTLE_LAB_OPEN_UNITS_FAILED " + exception); GetTree().Quit(1); }
    }

    private static string Place(BattleLabSession session, string id, BattleLabSide side, Vector2I cell)
    {
        var result = session.AddAndPlace(id, side, cell);
        Require(result.Succeeded, $"{id}/{side}: {result.RejectionReason}");
        return result.InstanceId;
    }
    private static IEnumerable<T> Descendants<T>(Node node) where T : Node =>
        node.GetChildren().SelectMany(child => (child is T typed ? new[] { typed } : []).Concat(Descendants<T>(child)));
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
