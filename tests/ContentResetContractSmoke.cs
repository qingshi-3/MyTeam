using System;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;

public partial class ContentResetContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var content = await SkillSampleContent.LoadAsync(this);
            var expectedItemIds = new[]
            {
                "v1_equipment_e02", "v1_equipment_e03", "v1_equipment_e06", "v1_equipment_e07",
                "v1_equipment_e10", "v1_equipment_e11", "v1_equipment_e15",
                "v1_relic_r01", "v1_relic_r02", "v1_relic_r03", "v1_relic_r04", "v1_relic_r05",
                "v1_relic_r06", "v1_relic_r07", "v1_relic_r08", "v1_relic_r09", "v1_relic_r10", "v1_relic_r11"
            };
            var expectedEncounterEnemyIds = new[]
            {
                "enemy_e31_iron_guard", "enemy_e32_wood_archer", "enemy_e33_field_medic", "enemy_e34_ambush_scout",
                "v1_boss_breach", "v1_boss_furnace", "v1_boss_stone_seat",
                "enemy_e31_iron_guard_ember_guard", "enemy_e31_iron_guard_crown_guard",
                "enemy_e32_wood_archer_ember_guard", "enemy_e32_wood_archer_gloam_guard",
                "enemy_e32_wood_archer_crown_guard", "enemy_e33_field_medic_gloam_guard",
                "enemy_e33_field_medic_crown_guard", "enemy_e34_ambush_scout_gloam_guard"
            };
            var encounterEnemyIds = content.Catalog.Enemies.Select(enemy => enemy.StableId)
                .Where(id => (id.StartsWith("enemy_e", StringComparison.Ordinal) &&
                        !id.StartsWith("enemy_ee", StringComparison.Ordinal)) ||
                    id.StartsWith("v1_boss_", StringComparison.Ordinal));
            Require(encounterEnemyIds.Order(StringComparer.Ordinal).SequenceEqual(expectedEncounterEnemyIds.Order(StringComparer.Ordinal)),
                "the fifteen authored B7 encounter enemies are published");
            Require(content.Catalog.AllEntries().Count() == AbilityPrototypeContent.UnitNumbers.Length + 8 + expectedItemIds.Length + 15,
                "four skills, four dummies, current prototype roster, fifteen B7 encounter enemies and v1 items");
            Require(content.Graph.Traits.Length == 8 && content.Graph.Traits.All(t => t.StableId.StartsWith("prototype_", StringComparison.Ordinal)) &&
                content.Graph.Equipment.Length == 7 && content.Graph.Relics.Length == 11 &&
                content.Catalog.Items.Count == 18 && content.Catalog.Items.Select(item => item.StableId)
                    .Order(StringComparer.Ordinal).SequenceEqual(expectedItemIds.Order(StringComparer.Ordinal)),
                "only the seven v1 equipment and eleven v1 relics are published");
            Require(content.TryGet("hero_hc38_grit_brawler", out var punch), "punch sample published");
            GritActionQueueContractSmoke.Run(BattleSetupFactory.Snapshot(punch, content));
            foreach (var id in new[] { "hero_hc38_grit_brawler", "enemy_ee03_trample_brute",
                "enemy_ee07_furnace_lizard", "enemy_ee09_swap_envoy" })
            {
                using var a = new BattleSimulation(SkillSampleContent.CreateBattle(content, id));
                using var b = new BattleSimulation(SkillSampleContent.CreateBattle(content, id));
                for (var tick = 0; tick < 240; tick++) { a.Step(); b.Step(); }
                Require(a.CreateResult().Digest == b.CreateResult().Digest, "sample determinism: " + id);
                Require(a.CombatEvents.Any(e => e.Kind == BattleCombatEventKind.AbilityResolved), "sample casts: " + id);
                GD.Print("CONTENT_RESET_SAMPLE_OK " + id);
            }
            GD.Print("CONTENT_RESET_CONTRACT_OK publication nineteen-prototypes fifteen-B7-enemies new-traits seven-equipment eleven-relics grit-queue rollback four-casts deterministic");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("CONTENT_RESET_CONTRACT_FAILED " + error); GetTree().Quit(1); }
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
