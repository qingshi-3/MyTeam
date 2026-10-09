using System;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Growth;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;

namespace TowerAutobattler.Composition;

// Temporary composition entry while the roster is being redesigned. It publishes
// the ordinary content graph and ready gate, without a campaign or save session.
internal static class SkillSampleContent
{
    internal const string CatalogPath = "res://content/catalogs/skill_samples.tres";

    internal static async Task<ContentRegistry> LoadAsync(Node owner)
    {
        var gate = await ContentRegistry.PublishReadyAsync(owner, () =>
        {
            var catalog = GD.Load<ContentCatalog>(CatalogPath);
            // The experiment publishes the same optional ascension loadouts as
            // the live content without creating a campaign or a save session.
            var growth = GD.Load<GrowthRulesDefinition>("res://content/expedition/current/growth_rules.tres");
            var loadouts = growth.Heroes.SelectMany(hero => new AbilityLoadoutDefinition?[]
                { hero.BaseLoadout, hero.AscensionBenefit?.BattleLoadout }).Where(value => value is not null).ToArray();
            var compilation = ContentValidator.CompileProductionGraph(catalog, loadouts);
            return new ContentPublicationPreparation<ContentCatalog>(
                catalog, compilation.Graph, catalog, compilation.Report);
        });
        return gate.Registry ?? throw new InvalidOperationException(string.Join("; ", gate.Report.CoreErrors));
    }

    internal static BattleConfig CreateBattle(ContentRegistry content, string sampleId)
    {
        if (sampleId is not ("hero_hc38_grit_brawler" or "enemy_ee03_trample_brute" or
            "enemy_ee07_furnace_lizard" or "enemy_ee09_swap_envoy"))
            throw new ArgumentException("Unknown retained skill sample.", nameof(sampleId));

        UnitSnapshot Unit(string id) => content.TryGet(id, out var entry)
            ? BattleSetupFactory.Snapshot(entry, content)
            : throw new InvalidOperationException($"Missing skill sample: {id}");

        var config = new BattleConfig
        {
            Seed = 20261004,
            CaptureDetailedStatistics = true,
            FloorRule = new ClearFloorRuleRuntime("rule_clear", "技能验证", "保留技能与测试假人的独立战斗"),
            HeroRule = new(1, 1, 1, 0, 0, 0, false, "", 1, 1, 0, 0, 0, 0, false, false, 0, 0, ""),
        };
        // Experiment ownership permits an enemy-authored skill to be tried on
        // the player side; its definition and combat numbers stay unchanged.
        config.Spawns.Add(new(Unit(sampleId), 0, new(2, 2), "sample", IsPersistentRosterHero: true));
        var target = sampleId == "hero_hc38_grit_brawler" ? "soldier_dummy_melee" : "soldier_dummy_static";
        config.Spawns.Add(new(Unit(target), 1, new(4, 2), "target_a"));
        config.Spawns.Add(new(Unit(target), 1, new(6, 2), "target_b"));
        config.Spawns.Add(new(Unit(target), 1, new(7, 3), "target_c"));
        return config;
    }
}
