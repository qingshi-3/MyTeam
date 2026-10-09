using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Domain;
using TowerAutobattler.Project;

namespace TowerAutobattler.Expedition;

public sealed record ExpeditionGenerationFacts(int WalkableCells, int InitialFreeCells,
    int InitialServices, int BypassableEnemies, int GuardingEnemies, int LongestEmptyCorridor);

public static class ExpeditionMapValidation
{
    // Resume validation accepts legitimate progress: revealed fog, cleared
    // enemies, handled sites and depleted stock never have to look newly born.
    // The Run validator owns offers, action locks and inventory/roster legality.
    public static ImmutableArray<string> ValidateSaved(CompiledExpedition definition, ExpeditionStateDto state)
    {
        var errors = new List<string>();
        if (state.Size != definition.Size || state.GenerationVersion != definition.GenerationVersion ||
            state.GenerationVersion != ExpeditionMapGenerator.SupportedGenerationVersion)
            errors.Add("地图尺寸或生成协议与配置不符");
        if (state.Cells is null || state.Size <= 0 || state.Size > 64 || state.Cells.Count != state.Size * state.Size)
            return [.. errors, "地图格子数量无效"];
        if (state.Sites is null || state.StageBossFloorRuleIds is null) return [.. errors, "地图内容缺失"];
        if (state.Cells.Where((cell, index) => cell is null || cell.Index != index ||
            cell.BiomeIndex < 0 || cell.BiomeIndex >= definition.Biomes.Length || cell.SiteId is null).Any())
            return [.. errors, "地图格子身份或地域无效"];
        if (state.Sites.Any(site => site is null || string.IsNullOrWhiteSpace(site.SiteId) ||
                string.IsNullOrWhiteSpace(site.DefinitionId) || site.EncounterProfileId is null || site.FloorRuleId is null) ||
            state.Sites.Select(site => site.SiteId).Distinct(StringComparer.Ordinal).Count() != state.Sites.Count)
            return [.. errors, "地图地点身份无效或重复"];
        if (state.StageIndex < 0 || state.StageIndex >= definition.Stages.Length)
            errors.Add("地图阶段无效");
        else if (state.RemainingActions < 0 || state.RemainingActions > definition.Stages[state.StageIndex].Actions)
            errors.Add("阶段行动数量无效");
        if (state.StageBossFloorRuleIds.Count != definition.Stages.Length) errors.Add("阶段 Boss 战场数量无效");
        else
            for (var index = 0; index < definition.Stages.Length; index++)
                if (!definition.Encounters.TryGetValue(definition.Stages[index].BossProfileId, out var profile) ||
                    !profile.Encounter.FloorRulePool.ContentIds.Contains(state.StageBossFloorRuleIds[index], StringComparer.Ordinal))
                    errors.Add($"阶段 {index} 的 Boss 战场不属于其作者配方");
        var definitions = definition.Sites.ToDictionary(site => site.StableId, StringComparer.Ordinal);
        var sites = state.Sites.ToDictionary(site => site.SiteId, StringComparer.Ordinal);
        var usedCells = new HashSet<int>();
        foreach (var site in state.Sites)
        {
            if (site.CellIndex < 0 || site.CellIndex >= state.Cells.Count || !usedCells.Add(site.CellIndex))
            { errors.Add($"地点 {site.SiteId} 格子无效或重复"); continue; }
            var cell = state.Cells[site.CellIndex];
            if (!cell.Walkable || cell.SiteId != site.SiteId) errors.Add($"地点 {site.SiteId} 与地形引用不一致");
            if (!definitions.TryGetValue(site.DefinitionId, out var source))
            { errors.Add($"地点 {site.SiteId} 引用未知定义"); continue; }
            var expectedPrefix = definition.Biomes[cell.BiomeIndex].StableId + ":" + source.StableId + ":";
            if (!site.SiteId.StartsWith(expectedPrefix, StringComparison.Ordinal) ||
                !int.TryParse(site.SiteId[expectedPrefix.Length..], System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var ordinal) || ordinal < 0 || ordinal >= source.CountPerBiome)
                errors.Add($"地点 {site.SiteId} 的地域身份无效");
            if (site.ContentSeed != ExpeditionMapGenerator.Derive(state.Seed, state.GenerationVersion, "content:" + site.SiteId))
                errors.Add($"地点 {site.SiteId} 的内容种子无效");
            if (ExpeditionMapGenerator.IsEnemy(source.Kind))
            {
                if (!definition.Encounters.TryGetValue(site.EncounterProfileId, out var profile) ||
                    !definition.Biomes[cell.BiomeIndex].EncounterProfiles.Contains(site.EncounterProfileId, StringComparer.Ordinal) ||
                    profile.Encounter.NodeType != source.Kind)
                    errors.Add($"地点 {site.SiteId} 的遭遇不属于其地域或类型");
                else if (!profile.Encounter.FloorRulePool.ContentIds.Contains(site.FloorRuleId, StringComparer.Ordinal))
                    errors.Add($"地点 {site.SiteId} 的战场不属于其遭遇");
                if (site.ServiceOffer is not null) errors.Add($"敌人地点 {site.SiteId} 不可包含商店服务");
            }
            else if (site.EncounterProfileId.Length > 0 || site.FloorRuleId.Length > 0 || site.Cleared || !site.RewardGroups.IsDefaultOrEmpty)
                errors.Add($"服务地点 {site.SiteId} 含有战斗状态");
        }
        foreach (var cell in state.Cells.Where(cell => cell.SiteId.Length > 0))
            if (!sites.TryGetValue(cell.SiteId, out var site) || site.CellIndex != cell.Index)
                errors.Add($"格子 {cell.Index} 的地点反向引用无效");
        for (var biome = 0; biome < definition.Biomes.Length; biome++)
        foreach (var source in definition.Sites)
            if (state.Sites.Count(site => site.CellIndex >= 0 && site.CellIndex < state.Cells.Count &&
                state.Cells[site.CellIndex].BiomeIndex == biome && site.DefinitionId == source.StableId) != source.CountPerBiome)
                errors.Add($"地域 {biome} 的 {source.StableId} 地点配额不符");
        var walkable = state.Cells.Count(cell => cell.Walkable);
        if (walkable < Math.Ceiling(state.Cells.Count * .55) || walkable > Math.Floor(state.Cells.Count * .70))
            errors.Add("可通行地形比例不在 55%–70% 范围");
        var firstWalkable = state.Cells.FindIndex(cell => cell.Walkable);
        if (firstWalkable < 0 || ExpeditionMapGenerator.ConnectedCount(state, firstWalkable, index => state.Cells[index].Walkable) != walkable)
            errors.Add("地图存在无法抵达的地形孤岛");
        if (!ExpeditionNavigation.InBounds(state, state.PositionIndex) ||
            !state.Cells[state.PositionIndex].Revealed || ExpeditionNavigation.IsBlocked(state, state.PositionIndex))
            errors.Add("队伍位置不合法");
        else
        {
            var position = state.PositionIndex;
            for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
            {
                var x = position % state.Size + dx;
                var y = position / state.Size + dy;
                if (x >= 0 && y >= 0 && x < state.Size && y < state.Size && !state.Cells[y * state.Size + x].Revealed)
                    errors.Add("队伍周围的可见信息缺失");
            }
        }
        return [.. errors];
    }

    public static ImmutableArray<string> ValidateGenerated(CompiledExpedition definition, ExpeditionStateDto state)
    {
        var errors = ValidateSaved(definition, state).ToList();
        if (errors.Count > 0) return [.. errors];
        if (state.StageIndex != 0 || state.RemainingActions != definition.Stages[0].Actions ||
            state.Sites.Any(site => site.Cleared || site.Handled || site.ServiceOffer is not null || !site.RewardGroups.IsDefaultOrEmpty))
            errors.Add("新地图不能带已处理状态或业务库存");
        var blocked = state.Sites.Where(site => site.EncounterProfileId.Length > 0).Select(site => site.CellIndex).ToHashSet();
        var free = ExpeditionMapGenerator.Reachable(state, state.PositionIndex, index => state.Cells[index].Walkable && !blocked.Contains(index));
        var sources = definition.Sites.ToDictionary(site => site.StableId, StringComparer.Ordinal);
        var freeSites = state.Sites.Where(site => free.Contains(site.CellIndex)).ToArray();
        if (state.Cells[state.PositionIndex].BiomeIndex != 0 ||
            ExpeditionNavigation.Neighbors(state.Size, state.PositionIndex).Count(free.Contains) < 2)
            errors.Add("低压力出生点缺少两条安全通路");
        if (!freeSites.Any(site => sources[site.DefinitionId].Kind == TowerNodeType.Recruitment) ||
            !freeSites.Any(site => sources[site.DefinitionId].Kind == TowerNodeType.Rest))
            errors.Add("起点安全区域缺少招募或营地");
        if (freeSites.Length > 5 || free.Select(index => state.Cells[index].BiomeIndex).Distinct().Count() == definition.Biomes.Length)
            errors.Add("初始安全区域可免费取得过多地点或走遍全部地域");
        if (!state.Sites.Any(site => site.EncounterProfileId.Length > 0 &&
            definition.Encounters[site.EncounterProfileId].OpeningCandidate &&
            ExpeditionNavigation.Neighbors(state.Size, site.CellIndex).Any(free.Contains)))
            errors.Add("起点边界缺少已配置的早期普通敌阵");
        var facts = InspectGeneration(state);
        if (facts.BypassableEnemies < 2) errors.Add("地图缺少两处真实绕路敌人");
        if (facts.GuardingEnemies < 2) errors.Add("地图缺少两处控制后续机会的守关敌人");
        if (facts.LongestEmptyCorridor > 4) errors.Add("地图含超过四格的无内容空路");
        return [.. errors];
    }

    public static ExpeditionGenerationFacts InspectGeneration(ExpeditionStateDto state)
    {
        var enemies = state.Sites.Where(site => site.EncounterProfileId.Length > 0).ToArray();
        var blocked = enemies.Select(site => site.CellIndex).ToHashSet();
        var freeComponents = new Dictionary<int, int>();
        var componentCells = new List<HashSet<int>>();
        foreach (var cell in state.Cells.Where(cell => cell.Walkable && !blocked.Contains(cell.Index)))
        {
            if (freeComponents.ContainsKey(cell.Index)) continue;
            var component = ExpeditionMapGenerator.Reachable(state, cell.Index, index => state.Cells[index].Walkable && !blocked.Contains(index));
            foreach (var index in component) freeComponents[index] = componentCells.Count;
            componentCells.Add(component);
        }
        var bypass = 0;
        var guards = 0;
        var serviceCells = state.Sites.Where(site => site.EncounterProfileId.Length == 0).Select(site => site.CellIndex).ToHashSet();
        foreach (var enemy in enemies)
        {
            var neighbors = ExpeditionNavigation.Neighbors(state.Size, enemy.CellIndex).Where(freeComponents.ContainsKey).ToArray();
            var components = neighbors.Select(index => freeComponents[index]).Distinct().ToArray();
            if (neighbors.Length >= 2 && components.Length == 1) bypass++;
            // Clearing a guard must connect distinct free areas with a real
            // opportunity behind them; a decorative cul-de-sac does not count.
            if (components.Length >= 2 && components.Count(id => componentCells[id].Overlaps(serviceCells)) >= 2) guards++;
        }
        var initial = freeComponents.TryGetValue(state.PositionIndex, out var initialId)
            ? componentCells[initialId] : new HashSet<int>();
        return new(state.Cells.Count(cell => cell.Walkable), initial.Count,
            initial.Count(serviceCells.Contains), bypass, guards, LongestCorridor(state));
    }

    private static int LongestCorridor(ExpeditionStateDto state)
    {
        var corridorCells = state.Cells.Where(cell => cell.Walkable && cell.SiteId.Length == 0 &&
            ExpeditionNavigation.Neighbors(state.Size, cell.Index).Count(next => state.Cells[next].Walkable) == 2)
            .Select(cell => cell.Index).ToHashSet();
        var visited = new HashSet<int>();
        var longest = 0;
        foreach (var cell in corridorCells.Order())
        {
            if (visited.Contains(cell)) continue;
            var segment = ExpeditionMapGenerator.Reachable(state, cell, corridorCells.Contains);
            visited.UnionWith(segment);
            longest = Math.Max(longest, segment.Count);
        }
        return longest;
    }
}
