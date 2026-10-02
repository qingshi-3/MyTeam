using System;
using System.Collections.Generic;
using System.Linq;
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
        if (!string.IsNullOrEmpty(run.SelectedOpponentOptionId))
            return OpponentOptions(run, type).FirstOrDefault(option => option.OptionId == run.SelectedOpponentOptionId)?.Encounter
                ?? throw new InvalidOperationException("Selected opponent is not a current candidate.");
        var regionIndex = Math.Clamp(run.FloorIndex / campaign.FloorsPerRegion, 0, campaign.Regions.Length - 1);
        var region = campaign.Regions[regionIndex];
        if (!region.Encounters.TryGetValue(type, out var encounter))
            throw new InvalidOperationException($"Region {region.StableId} has no encounter for {type}.");
        var enemyIds = new List<string>();
        if (!string.IsNullOrWhiteSpace(encounter.LeadEnemyId)) enemyIds.Add(encounter.LeadEnemyId);
        var count = encounter.BaseEnemyCount + (encounter.AddRegionIndexToCount ? regionIndex : 0);
        var random = new DeterministicRandom(
            run.Seed ^ (ulong)(run.FloorIndex + 1) * 0x9E3779B9UL ^ (ulong)encounter.SeedSalt);
        if (!encounter.Compositions.IsDefaultOrEmpty)
        {
            var localFloor = run.FloorIndex % campaign.FloorsPerRegion;
            var candidates = encounter.Compositions.Where(candidate => !candidate.OpponentChoiceOnly && candidate.MinLocalFloor <= localFloor &&
                localFloor <= candidate.MaxLocalFloor).ToArray();
            if (candidates.Length == 0) throw new InvalidOperationException("No composition covers this floor.");
            var composition = candidates[random.NextInt(0, candidates.Length)];
            var floorRule = encounter.FloorRulePool.ContentIds[random.NextInt(0, encounter.FloorRulePool.ContentIds.Length)];
            return new EncounterPlan(encounter.Title(region.DisplayName) + " · " + composition.DisplayName,
                floorRule, composition.EnemyIds, encounter.NodeType == TowerNodeType.Boss,
                encounter.NodeType == TowerNodeType.Elite, encounter.StableId, encounter.NodeType)
            { CompositionId = composition.StableId, EnemyCells = composition.Cells };
        }
        if (!encounter.AlternateLeadEnemyIds.IsDefaultOrEmpty)
        {
            var variant = random.NextInt(0, encounter.AlternateLeadEnemyIds.Length+1);
            if (variant > 0) { if (enemyIds.Count>0) enemyIds[0]=encounter.AlternateLeadEnemyIds[variant-1]; else enemyIds.Add(encounter.AlternateLeadEnemyIds[variant-1]); }
        }
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

    // New runs choose opponents only for elite challenges. Ordinary battles keep their
    // deterministic encounter; their old candidates remain resolvable below for saved v9 fights.
    public IReadOnlyList<OpponentOption> SelectableOpponentOptions(ActiveRunDto run, TowerNodeType type) =>
        type == TowerNodeType.Elite ? OpponentOptions(run, type) : [];

    // A read-only deterministic resolver, also retained for already committed v9 ordinary choices.
    // Visiting or leaving the screen never rerolls these choices.
    public IReadOnlyList<OpponentOption> OpponentOptions(ActiveRunDto run, TowerNodeType type)
    {
        if (run.Growth is null || type is not (TowerNodeType.Combat or TowerNodeType.Elite)) return [];
        var region = RegionFor(run.FloorIndex);
        if (!region.Encounters.TryGetValue(type, out var encounter) || encounter.OpponentChoiceCount == 0) return [];
        var localFloor = run.FloorIndex % campaign.FloorsPerRegion;
        var candidates = encounter.Compositions.Where(candidate => candidate.MinLocalFloor <= localFloor &&
            localFloor <= candidate.MaxLocalFloor).ToList();
        var random = new DeterministicRandom(run.Seed ^ (ulong)(run.FloorIndex + 1) * 0x9E3779B9UL ^
            (ulong)encounter.SeedSalt ^ 0xD6E8FEB86659FD93UL);
        // Shuffle once rather than OrderBy(random): a stable total order and no hidden roster inputs.
        for (var index = candidates.Count - 1; index > 0; index--)
        {
            var swap = random.NextInt(0, index + 1);
            (candidates[index], candidates[swap]) = (candidates[swap], candidates[index]);
        }
        var result = new List<OpponentOption>();
        foreach (var composition in candidates.Take(encounter.OpponentChoiceCount))
        {
            var ruleId = encounter.FloorRulePool.ContentIds[random.NextInt(0, encounter.FloorRulePool.ContentIds.Length)];
            var plan = new EncounterPlan(encounter.Title(region.DisplayName) + " · " + composition.DisplayName,
                ruleId, composition.EnemyIds, false, type == TowerNodeType.Elite, encounter.StableId, type)
            { CompositionId = composition.StableId, EnemyCells = composition.Cells };
            result.Add(new OpponentOption($"opponent:{run.Seed:x16}:{run.FloorIndex}:{(int)type}:{composition.StableId}",
                type, composition.DisplayName, composition.PressureSummary, plan)
            {
                RewardGold = composition.RewardGold,
                RewardResearch = composition.RewardResearch,
                RewardMaterials = composition.RewardMaterials
            });
        }
        return result;
    }
}
