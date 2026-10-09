using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Battle;
using TowerAutobattler.Domain;
using TowerAutobattler.Project;

namespace TowerAutobattler.Expedition;

public sealed class ExpeditionGenerationException(string message) : InvalidOperationException(message);

public static class ExpeditionMapGenerator
{
    public const int SupportedGenerationVersion = 1;

    public static ExpeditionStateDto Generate(CompiledExpedition definition, ulong seed)
    {
        var inputErrors = ValidateDefinition(definition);
        if (inputErrors.Length > 0) throw new ExpeditionGenerationException(string.Join("；", inputErrors));
        var lastReason = string.Empty;
        for (var attempt = 0; attempt < Math.Min(64, definition.MaximumAttempts); attempt++)
        {
            var result = TryGenerate(definition, seed, attempt, out lastReason);
            if (result is null) continue;
            var errors = ExpeditionMapValidation.ValidateGenerated(definition, result);
            if (errors.IsEmpty) return result;
            lastReason = string.Join("；", errors);
        }
        throw new ExpeditionGenerationException($"随机大陆生成失败，种子 {seed}，版本 {definition.GenerationVersion}：{lastReason}");
    }

    private static ExpeditionStateDto? TryGenerate(CompiledExpedition definition, ulong seed, int attempt, out string reason)
    {
        var size = definition.Size;
        var topology = Stream(seed, definition.GenerationVersion, attempt, 0x544F504FUL);
        var placement = Stream(seed, definition.GenerationVersion, attempt, 0x53495445UL);
        var orientation = Stream(seed, definition.GenerationVersion, attempt, 0x42494F4DUL);
        var map = new ExpeditionStateDto
        {
            Size = size, Seed = seed, GenerationVersion = definition.GenerationVersion,
            StageIndex = 0, RemainingActions = definition.Stages[0].Actions,
            Cells = Enumerable.Range(0, size * size).Select(index => new ExpeditionCellDto { Index = index }).ToList()
        };
        var count = definition.Biomes.Length;
        // Region widths, gate positions and the connected eroded terrain are all
        // sampled. Rotation alone is never the source of map variety.
        var widths = Enumerable.Repeat(3, count).ToArray();
        for (var remaining = size - (count - 1) - count * 3; remaining > 0; remaining--)
            widths[topology.NextInt(0, count)]++;
        var regions = new List<int>[count];
        var gates = new List<int>();
        var protectedCells = new HashSet<int>();
        var left = 0;
        for (var biome = 0; biome < count; biome++)
        {
            regions[biome] = [];
            for (var y = 0; y < size; y++)
            for (var x = left; x < left + widths[biome]; x++)
            {
                var index = y * size + x;
                map.Cells[index].BiomeIndex = biome;
                map.Cells[index].Walkable = true;
                regions[biome].Add(index);
            }
            left += widths[biome];
            if (biome + 1 == count) continue;
            for (var y = 0; y < size; y++) map.Cells[y * size + left].BiomeIndex = biome;
            var gate = topology.NextInt(2, size - 2) * size + left;
            gates.Add(gate);
            map.Cells[gate].Walkable = true;
            protectedCells.Add(gate - 1);
            protectedCells.Add(gate + 1);
            left++;
        }
        // Erosion preserves each region's actual four-neighbor connectivity and
        // its gate approaches. The result contains both loops and rough edges.
        foreach (var region in regions)
        {
            var target = (int)Math.Round(region.Count * (0.69 + topology.NextFloat() * 0.07));
            var candidates = region.Where(index => !protectedCells.Contains(index)).ToList();
            Shuffle(candidates, topology);
            var remaining = region.Count;
            foreach (var candidate in candidates)
            {
                if (remaining <= target) break;
                map.Cells[candidate].Walkable = false;
                if (ConnectedCount(map, region.First(index => map.Cells[index].Walkable),
                        index => map.Cells[index].Walkable && region.Contains(index)) == remaining - 1)
                    remaining--;
                else map.Cells[candidate].Walkable = true;
            }
        }
        var gateSet = gates.ToHashSet();
        var spawnCandidates = regions[0].Where(index => map.Cells[index].Walkable &&
            gates.All(gate => Distance(size, index, gate) >= 4) &&
            ExpeditionNavigation.Neighbors(size, index).Count(next => map.Cells[next].Walkable && !gateSet.Contains(next)) >= 2).ToList();
        Shuffle(spawnCandidates, placement);
        if (spawnCandidates.Count == 0) { reason = "起点缺少两条安全通路"; return null; }
        map.PositionIndex = spawnCandidates[0];
        protectedCells.Add(map.PositionIndex);
        foreach (var neighbor in ExpeditionNavigation.Neighbors(size, map.PositionIndex)) protectedCells.Add(neighbor);
        var occupied = new HashSet<int> { map.PositionIndex };
        var blocked = new HashSet<int>(gates);
        var orderedDefinitions = definition.Sites.OrderBy(site => site.StableId, StringComparer.Ordinal).ToArray();
        var combatDefinition = orderedDefinitions.First(site => site.Kind == TowerNodeType.Combat && site.CountPerBiome > 0);

        for (var biome = 0; biome < count; biome++)
        {
            var ordinals = orderedDefinitions.ToDictionary(site => site.StableId, _ => 0, StringComparer.Ordinal);
            if (biome < gates.Count)
            {
                var gate = gates[biome];
                AddSite(map, definition, biome, combatDefinition, ordinals[combatDefinition.StableId]++, gate, biome == 0);
                occupied.Add(gate);
            }
            foreach (var siteDefinition in orderedDefinitions.Where(site => IsEnemy(site.Kind)))
            {
                while (ordinals[siteDefinition.StableId] < siteDefinition.CountPerBiome)
                {
                    var candidates = regions[biome].Where(index => map.Cells[index].Walkable &&
                        !occupied.Contains(index) && !protectedCells.Contains(index) &&
                        ExpeditionNavigation.Neighbors(size, index).All(next => !blocked.Contains(next)) &&
                        ExpeditionNavigation.Neighbors(size, index).Count(next => map.Cells[next].Walkable) >= 2).ToList();
                    Shuffle(candidates, placement);
                    var cell = -1;
                    foreach (var candidate in candidates)
                    {
                        blocked.Add(candidate);
                        var allowed = regions[biome].Where(index => map.Cells[index].Walkable && !blocked.Contains(index)).ToArray();
                        var reachable = ConnectedCount(map, allowed[0], index => map.Cells[index].Walkable &&
                            map.Cells[index].BiomeIndex == biome && !blocked.Contains(index));
                        blocked.Remove(candidate);
                        if (reachable != allowed.Length) continue;
                        cell = candidate;
                        break;
                    }
                    if (cell < 0) { reason = $"地域 {biome} 缺少合法敌人位置"; return null; }
                    var opening = biome == 0 && siteDefinition.Kind == TowerNodeType.Combat;
                    AddSite(map, definition, biome, siteDefinition, ordinals[siteDefinition.StableId]++, cell, opening);
                    occupied.Add(cell);
                    blocked.Add(cell);
                }
            }
            foreach (var siteDefinition in orderedDefinitions.Where(site => !IsEnemy(site.Kind)))
            {
                while (ordinals[siteDefinition.StableId] < siteDefinition.CountPerBiome)
                {
                    var candidates = regions[biome].Where(index => map.Cells[index].Walkable && !occupied.Contains(index)).ToList();
                    Shuffle(candidates, placement);
                    if (candidates.Count == 0) { reason = $"地域 {biome} 内容容量不足"; return null; }
                    // First recruitment and camp are near the low-pressure spawn;
                    // other opportunities remain spread across the region.
                    if (biome == 0 && ordinals[siteDefinition.StableId] == 0 &&
                        siteDefinition.Kind is TowerNodeType.Recruitment or TowerNodeType.Rest)
                        candidates = candidates.OrderBy(index => Distance(size, map.PositionIndex, index)).ToList();
                    else
                        candidates = candidates.OrderByDescending(index => map.Sites.Where(site =>
                            map.Cells[site.CellIndex].BiomeIndex == biome).Select(site => Distance(size, site.CellIndex, index))
                            .DefaultIfEmpty(size).Min()).ToList();
                    var cell = candidates[0];
                    AddSite(map, definition, biome, siteDefinition, ordinals[siteDefinition.StableId]++, cell, false);
                    occupied.Add(cell);
                }
            }
        }
        // One low-region enemy beside a free route uses an opening profile. Other
        // low-region enemies may use the full authored low-pressure distribution.
        var lowOrdinary = map.Sites.Where(site => map.Cells[site.CellIndex].BiomeIndex == 0 &&
            definition.Sites.First(source => source.StableId == site.DefinitionId).Kind == TowerNodeType.Combat &&
            !gateSet.Contains(site.CellIndex)).OrderBy(site => Distance(size, map.PositionIndex, site.CellIndex)).ToArray();
        foreach (var site in lowOrdinary.Skip(1)) AssignEncounter(definition, map, site, 0, false);
        foreach (var stage in definition.Stages)
        {
            var profile = definition.Encounters[stage.BossProfileId];
            var stream = new DeterministicRandom(Derive(seed, definition.GenerationVersion, "boss:" + stage.StableId));
            map.StageBossFloorRuleIds.Add(PickRule(profile, stream));
        }
        Transform(map, orientation.NextInt(0, 8));
        ExpeditionNavigation.RevealAround(map, map.PositionIndex);
        reason = string.Empty;
        return map;
    }

    private static void AddSite(ExpeditionStateDto map, CompiledExpedition definition, int biome,
        CompiledExpeditionSite source, int ordinal, int cell, bool opening)
    {
        var id = definition.Biomes[biome].StableId + ":" + source.StableId + ":" +
            ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var site = new ExpeditionSiteDto
        {
            SiteId = id, CellIndex = cell, DefinitionId = source.StableId,
            ContentSeed = Derive(map.Seed, map.GenerationVersion, "content:" + id)
        };
        if (IsEnemy(source.Kind)) AssignEncounter(definition, map, site, biome, opening);
        map.Sites.Add(site);
        map.Cells[cell].SiteId = id;
    }

    private static void AssignEncounter(CompiledExpedition definition, ExpeditionStateDto map,
        ExpeditionSiteDto site, int biome, bool opening)
    {
        var kind = definition.Sites.First(source => source.StableId == site.DefinitionId).Kind;
        var profiles = definition.Biomes[biome].EncounterProfiles.Select(id => definition.Encounters[id])
            .Where(profile => profile.Encounter.NodeType == kind && (!opening || profile.OpeningCandidate))
            .OrderBy(profile => profile.StableId, StringComparer.Ordinal).ToArray();
        if (profiles.Length == 0) throw new ExpeditionGenerationException($"地域 {biome} 没有 {kind} 的有效遭遇配方");
        var random = new DeterministicRandom(Derive(map.Seed, map.GenerationVersion, "encounter:" + site.SiteId));
        var profile = profiles[random.NextInt(0, profiles.Length)];
        site.EncounterProfileId = profile.StableId;
        site.FloorRuleId = PickRule(profile, random);
    }

    private static string PickRule(CompiledExpeditionEncounter profile, DeterministicRandom random)
    {
        var rules = profile.Encounter.FloorRulePool.ContentIds.Order(StringComparer.Ordinal).ToArray();
        return rules[random.NextInt(0, rules.Length)];
    }

    private static ImmutableArray<string> ValidateDefinition(CompiledExpedition definition)
    {
        var errors = new List<string>();
        if (definition.Biomes.IsDefaultOrEmpty || definition.Stages.IsDefaultOrEmpty || definition.Sites.IsDefaultOrEmpty)
            return ["地图缺少地域、阶段或地点定义"];
        if (definition.GenerationVersion != SupportedGenerationVersion) errors.Add("不支持的地图生成版本");
        if (definition.Size < 12 || definition.Size > 64 || definition.Biomes.Length < 3 ||
            definition.Size < definition.Biomes.Length * 4 - 1) errors.Add("地图尺寸不足以容纳地域");
        if (definition.Biomes.Select(biome => biome.StableId).Distinct(StringComparer.Ordinal).Count() != definition.Biomes.Length)
            errors.Add("地域身份重复");
        if (definition.MaximumAttempts is < 1 or > 64) errors.Add("地图生成尝试次数应为 1–64");
        if (definition.Stages.IsDefaultOrEmpty || definition.Stages.Any(stage => stage.Actions <= 0)) errors.Add("缺少合法阶段预算");
        if (definition.Sites.IsDefaultOrEmpty || definition.Sites.Select(site => site.StableId).Distinct(StringComparer.Ordinal).Count() != definition.Sites.Length ||
            definition.Sites.Any(site => site.CountPerBiome <= 0 || site.Kind == TowerNodeType.Boss)) errors.Add("地点定义无效");
        if (!definition.Sites.Any(site => site.Kind == TowerNodeType.Combat && site.CountPerBiome > 0) ||
            !definition.Sites.Any(site => site.Kind == TowerNodeType.Recruitment && site.CountPerBiome > 0) ||
            !definition.Sites.Any(site => site.Kind == TowerNodeType.Rest && site.CountPerBiome > 0)) errors.Add("缺少起点所需的战斗、招募或营地");
        foreach (var biome in definition.Biomes)
        {
            if (biome.EncounterProfiles.Any(id => !definition.Encounters.ContainsKey(id))) { errors.Add($"地域 {biome.StableId} 引用未知遭遇"); continue; }
            foreach (var kind in definition.Sites.Where(site => IsEnemy(site.Kind)).Select(site => site.Kind).Distinct())
                if (!biome.EncounterProfiles.Any(id => definition.Encounters[id].Encounter.NodeType == kind))
                    errors.Add($"地域 {biome.StableId} 缺少 {kind} 遭遇");
        }
        if (definition.Biomes.Length > 0 && !definition.Biomes[0].EncounterProfiles.Any(id => definition.Encounters.TryGetValue(id, out var profile) &&
            profile.Encounter.NodeType == TowerNodeType.Combat && profile.OpeningCandidate)) errors.Add("出生地域缺少早期敌阵");
        foreach (var stage in definition.Stages)
            if (!definition.Encounters.TryGetValue(stage.BossProfileId, out var profile) || profile.Encounter.NodeType != TowerNodeType.Boss)
                errors.Add($"阶段 {stage.StableId} 缺少 Boss");
        if (definition.Encounters.Values.Any(profile => profile.Encounter.FloorRulePool.ContentIds.IsDefaultOrEmpty)) errors.Add("遭遇缺少战场");
        return [.. errors];
    }

    internal static bool IsEnemy(TowerNodeType kind) => kind is TowerNodeType.Combat or TowerNodeType.Elite;

    internal static int ConnectedCount(ExpeditionStateDto map, int start, Func<int, bool> allowed) =>
        Reachable(map, start, allowed).Count;

    internal static HashSet<int> Reachable(ExpeditionStateDto map, int start, Func<int, bool> allowed)
    {
        var found = new HashSet<int>();
        if (start < 0 || start >= map.Cells.Count || !allowed(start)) return found;
        var queue = new Queue<int>();
        queue.Enqueue(start);
        found.Add(start);
        while (queue.Count > 0)
            foreach (var next in ExpeditionNavigation.Neighbors(map.Size, queue.Dequeue()))
                if (allowed(next) && found.Add(next)) queue.Enqueue(next);
        return found;
    }

    private static void Transform(ExpeditionStateDto map, int orientation)
    {
        int Index(int index)
        {
            var x = index % map.Size;
            var y = index / map.Size;
            if (orientation >= 4) x = map.Size - 1 - x;
            for (var turn = 0; turn < orientation % 4; turn++) (x, y) = (map.Size - 1 - y, x);
            return y * map.Size + x;
        }
        foreach (var cell in map.Cells) cell.Index = Index(cell.Index);
        map.Cells.Sort((first, second) => first.Index.CompareTo(second.Index));
        foreach (var site in map.Sites) site.CellIndex = Index(site.CellIndex);
        map.PositionIndex = Index(map.PositionIndex);
    }

    internal static int Distance(int size, int first, int second) =>
        Math.Abs(first % size - second % size) + Math.Abs(first / size - second / size);

    private static DeterministicRandom Stream(ulong seed, int version, int attempt, ulong tag) =>
        new(Mix(seed ^ ((ulong)(uint)version << 32) ^ (ulong)(uint)attempt * 0x9E3779B97F4A7C15UL ^ tag));

    public static ulong Derive(ulong seed, int generationVersion, string purpose)
    {
        // FNV-1a of invariant authored IDs: no runtime-random string hash, UTF
        // culture sorting, global RNG state, or dictionary enumeration order.
        var value = 14695981039346656037UL;
        foreach (var character in purpose) { value ^= character; value *= 1099511628211UL; }
        return Mix(value ^ seed ^ ((ulong)(uint)generationVersion << 32));
    }

    private static ulong Mix(ulong value)
    {
        value ^= value >> 30;
        value *= 0xBF58476D1CE4E5B9UL;
        value ^= value >> 27;
        value *= 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }

    private static void Shuffle<T>(IList<T> values, DeterministicRandom random)
    {
        for (var index = values.Count - 1; index > 0; index--)
        {
            var other = random.NextInt(0, index + 1);
            (values[index], values[other]) = (values[other], values[index]);
        }
    }
}
