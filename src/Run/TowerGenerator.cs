using System;
using System.Collections.Generic;
using TowerAutobattler.Battle;
using TowerAutobattler.Project;

namespace TowerAutobattler.Run;

public sealed class TowerGenerator(CompiledCampaign campaign)
{
    public CompiledTowerRegion RegionFor(int floorIndex) => campaign.Regions[
        Math.Clamp(floorIndex / campaign.FloorsPerRegion, 0, campaign.Regions.Length - 1)];

    public IReadOnlyList<TowerNodeOption> Options(ActiveRunDto run)
    {
        var localFloor = run.FloorIndex % campaign.FloorsPerRegion;
        var region = RegionFor(run.FloorIndex);
        var table = campaign.NodeTable;
        if (localFloor == table.BossLocalFloor)
        {
            var boss = table.Nodes[TowerNodeType.Boss];
            return [new TowerNodeOption(boss.Type, boss.Title(region.DisplayName), boss.Description(region.DisplayName), boss.Risk)];
        }

        var offset = (int)((run.Seed + (ulong)run.FloorIndex * (ulong)table.FloorSeedStride) % (ulong)table.Rotation.Length);
        var result = new List<TowerNodeOption>();
        for (var index = 0; index < table.RegularOptionCount; index++)
        {
            var type = table.Rotation[(offset + index * table.RotationStride) % table.Rotation.Length];
            var node = table.Nodes[type];
            result.Add(new TowerNodeOption(type, node.Title(region.DisplayName), node.Description(region.DisplayName), node.Risk));
        }
        return result;
    }

    public EncounterPlan Encounter(ActiveRunDto run, TowerNodeType type)
    {
        var regionIndex = Math.Clamp(run.FloorIndex / campaign.FloorsPerRegion, 0, campaign.Regions.Length - 1);
        var region = campaign.Regions[regionIndex];
        if (!region.Encounters.TryGetValue(type, out var encounter))
            throw new InvalidOperationException($"Region {region.StableId} has no encounter for {type}.");
        var enemyIds = new List<string>();
        if (!string.IsNullOrWhiteSpace(encounter.LeadEnemyId)) enemyIds.Add(encounter.LeadEnemyId);
        var count = encounter.BaseEnemyCount + (encounter.AddRegionIndexToCount ? regionIndex : 0);
        var random = new DeterministicRandom(
            run.Seed ^ (ulong)(run.FloorIndex + 1) * 0x9E3779B9UL ^ (ulong)encounter.SeedSalt);
        while (enemyIds.Count < count)
            enemyIds.Add(encounter.EnemyPool.ContentIds[random.NextInt(0, encounter.EnemyPool.ContentIds.Length)]);
        var ruleId = encounter.FloorRulePool.ContentIds[
            random.NextInt(0, encounter.FloorRulePool.ContentIds.Length)];
        return new EncounterPlan(
            encounter.Title(region.DisplayName),
            ruleId,
            enemyIds,
            encounter.NodeType == TowerNodeType.Boss,
            encounter.NodeType == TowerNodeType.Elite,
            encounter.StableId,
            encounter.NodeType);
    }
}
