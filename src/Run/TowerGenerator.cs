using System;
using System.Collections.Generic;
using TowerAutobattler.Battle;

namespace TowerAutobattler.Run;

public sealed class TowerGenerator(IReadOnlyList<TowerRegionDefinition> regions)
{
    private static readonly TowerNodeType[] Rotation =
    [
        TowerNodeType.Combat, TowerNodeType.Recruitment, TowerNodeType.Event,
        TowerNodeType.Elite, TowerNodeType.Shop, TowerNodeType.Rest
    ];

    public TowerRegionDefinition RegionFor(int floorIndex) => regions[Math.Clamp(floorIndex / 5, 0, regions.Count - 1)];

    public IReadOnlyList<TowerNodeOption> Options(ActiveRunDto run)
    {
        var localFloor = run.FloorIndex % 5;
        var region = RegionFor(run.FloorIndex);
        if (localFloor == 4)
            return [new TowerNodeOption(TowerNodeType.Boss, $"挑战：{region.DisplayName}层主", "击败层主才能进入下一段高塔。", 3)];

        var offset = (int)((run.Seed + (ulong)run.FloorIndex * 7) % (ulong)Rotation.Length);
        var result = new List<TowerNodeOption>();
        for (var index = 0; index < 3; index++)
        {
            var type = Rotation[(offset + index * 2) % Rotation.Length];
            result.Add(Describe(type, region, run.FloorIndex));
        }
        return result;
    }

    public EncounterPlan Encounter(ActiveRunDto run, TowerNodeType type)
    {
        var regionIndex = Math.Clamp(run.FloorIndex / 5, 0, regions.Count - 1);
        var region = regions[regionIndex];
        var isBoss = type == TowerNodeType.Boss;
        var isElite = type == TowerNodeType.Elite;
        var enemyIds = new List<string>();
        if (isBoss) enemyIds.Add(region.BossId);
        var count = isBoss ? 3 + regionIndex : isElite ? 6 + regionIndex : 4 + regionIndex;
        var random = new DeterministicRandom(run.Seed ^ (ulong)(run.FloorIndex + 1) * 0x9E3779B9UL ^ (isElite ? 17UL : 0UL));
        while (enemyIds.Count < count)
            enemyIds.Add(region.EnemyPool[random.NextInt(0, region.EnemyPool.Length)]);
        var ruleId = isBoss && !string.IsNullOrWhiteSpace(region.BossFloorRuleId)
            ? region.BossFloorRuleId
            : region.FloorRulePool[random.NextInt(0, region.FloorRulePool.Length)];
        var title = isBoss ? $"{region.DisplayName}层主战" : isElite ? $"{region.DisplayName}精英战" : $"{region.DisplayName}遭遇战";
        return new EncounterPlan(title, ruleId, enemyIds, isBoss, isElite);
    }

    private static TowerNodeOption Describe(TowerNodeType type, TowerRegionDefinition region, int floor) => type switch
    {
        TowerNodeType.Combat => new(type, "守军", $"与{region.DisplayName}守军交战，获得金币和战利品。", 1),
        TowerNodeType.Elite => new(type, "精英", "更危险的敌阵，胜利后获得更多金币和战利品。", 3),
        TowerNodeType.Recruitment => new(type, "征募营", "从三名候选士兵中招募一名。", 0),
        TowerNodeType.Shop => new(type, "行商", "花费金币购买物品。", 0),
        TowerNodeType.Event => new(type, "塔内异象", "做出带有风险与收益的选择。", 1),
        TowerNodeType.Rest => new(type, "营火", "恢复英雄与士兵，或领取少量军费。", 0),
        _ => new(type, $"第{floor + 1}层", region.Description, 1)
    };
}
