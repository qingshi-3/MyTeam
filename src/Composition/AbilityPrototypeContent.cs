using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Composition;

// Disposable, save-free composition of published authored content. No combat rules live here.
internal static class AbilityPrototypeContent
{
    internal static readonly int[] UnitNumbers = [1, 2, 3, 4, 5, 6, 7, 8, 11, 13, 14, 16, 17, 18, 19, 20, 21, 22, 25];
    internal static readonly string[] PresetNames = ["极地射手", "森林换形", "斗士圣盾", "森林刺客"];
    internal static readonly int[][] Presets = [[2,4,7,3,5,19], [8,11,18,13,16,17], [2,4,7,8,16,18], [11,16,18,5,17,19]];
    internal static string Id(int number) => $"prototype_u{number:00}";
    internal static UnitSnapshot Snapshot(ContentRegistry content, int number) => content.TryGet(Id(number), out var entry)
        ? BattleSetupFactory.Snapshot(entry, content) : throw new InvalidOperationException("缺少原型单位：" + number);

    internal static BattleConfig CreateBattle(ContentRegistry content, IReadOnlyList<int> player, IReadOnlyList<int> enemy, ulong seed = 20261004, bool captureDetailedStatistics = true)
    {
        var profiles = UnitNumbers.ToDictionary(Id, number => Snapshot(content, number), StringComparer.Ordinal);
        var spawns = new List<BattleSpawn>();
        var contributions = new List<TraitContributionInput>();
        foreach (var (team, selected) in new[] { (0, player), (1, enemy) })
        {
            if (selected.Count != 6 || selected.All(n => n == 0)) throw new InvalidOperationException("双方各需至少一名单位。每方最多六个位置。");
            if (selected.Where(n => n > 0).Distinct().Count() != selected.Count(n => n > 0)) throw new InvalidOperationException("同一方不能重复选择同一个原型单位。");
            for (var slot = 0; slot < selected.Count; slot++)
            {
                var number = selected[slot];
                if (number == 0) continue;
                if (!UnitNumbers.Contains(number)) throw new InvalidOperationException("未知原型单位。");
                var profile = profiles[Id(number)];
                var instance = $"prototype-{team}-{slot}";
                var x = slot < 3 ? 3 : 1;
                var cell = new Vector2I(team == 0 ? x : BattlefieldLayout.Width - 1 - x, slot % 3 * 2);
                spawns.Add(new(profile, team, cell, instance, IsPersistentRosterHero: true));
                foreach (var contribution in profile.TraitContributions)
                    contributions.Add(new(contribution.TraitId, contribution.Value, team, TraitContributionSourceKind.Hero,
                        instance + ":" + contribution.TraitId, instance, profile.ContentId, true, false, true));
            }
        }
        var config = new BattleConfig
        {
            Seed = seed, HeroRule = HeroRuleSnapshot.Neutral, CaptureDetailedStatistics = captureDetailedStatistics,
            FloorRule = new ClearFloorRuleRuntime("rule_clear", "能力实验", "八羁绊能力原型；外观临时。"),
            Traits = TraitBattlePreparationBuilder.Build(content.Graph.Traits, contributions),
            TacticalSummons = profiles
        };
        config.Spawns.AddRange(spawns);
        return config;
    }
}
