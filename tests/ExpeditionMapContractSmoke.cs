using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Godot;
using TowerAutobattler.Domain;
using TowerAutobattler.Expedition;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;

public partial class ExpeditionMapContractSmoke : Node
{
    public override void _Ready()
    {
        try
        {
            VerifyGenerationAndRecovery();
            VerifyNavigationAndProjection();
            VerifyOriginProtocol();
            GD.Print("EXPEDITION_MAP_CONTRACT_OK deterministic variable-topology quotas guards bypass fog navigation recovery clone rejected-input legacy-origin action-origin");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PrintErr("EXPEDITION_MAP_CONTRACT_FAILED: " + error);
            GetTree().Quit(1);
        }
    }

    public static void VerifyGenerationAndRecovery()
    {
        var definition = Fixture();
        var topologySignatures = new HashSet<string>(StringComparer.Ordinal);
        foreach (var seed in new ulong[] { 1, 2, 3, 17, 111, 20261002, 0, ulong.MaxValue })
        {
            var generated = ExpeditionMapGenerator.Generate(definition, seed);
            Require(ExpeditionMapValidation.ValidateGenerated(definition, generated).IsEmpty, "generated invariants");
            Require(generated.Sites.Count == 39 && generated.Cells.Count == 256, "authored quotas");
            var same = ExpeditionMapGenerator.Generate(definition, seed);
            Require(JsonSerializer.Serialize(generated) == JsonSerializer.Serialize(same), "same input is byte-for-byte deterministic");
            topologySignatures.Add(string.Join(',', generated.Cells.Where(cell => cell.Walkable).Select(cell => cell.Index)));
            var facts = ExpeditionMapValidation.InspectGeneration(generated);
            Require(facts.InitialServices is >= 2 and <= 5 && facts.BypassableEnemies >= 2 && facts.GuardingEnemies >= 2,
                "initial opportunities and actual graph pressure");
            var restored = JsonSerializer.Deserialize<ExpeditionStateDto>(JsonSerializer.Serialize(generated))!;
            Require(ExpeditionMapValidation.ValidateSaved(definition, restored).IsEmpty, "save round trip");
            var working = ExpeditionMapState.DeepClone(restored);
            var enemy = working.Sites.First(site => site.EncounterProfileId.Length > 0);
            enemy.Cleared = true;
            enemy.Handled = true;
            working.PositionIndex = enemy.CellIndex;
            ExpeditionNavigation.RevealAround(working, enemy.CellIndex);
            working.StageIndex = 1;
            working.RemainingActions = 0;
            Require(ExpeditionMapValidation.ValidateSaved(definition, working).IsEmpty, "restoring cleared guards does not rerun birth rules");
            Require(!restored.Sites.First(site => site.SiteId == enemy.SiteId).Cleared && restored.StageIndex == 0,
                "mutable map state has one cloned owner");
            working.StageBossFloorRuleIds[1] = "not_a_boss_rule";
            Require(!ExpeditionMapValidation.ValidateSaved(definition, working).IsEmpty, "reject mismatched frozen boss rule");
        }
        Require(topologySignatures.Count >= 6, "different seeds vary actual terrain");
        var rejected = false;
        try { ExpeditionMapGenerator.Generate(definition with { MaximumAttempts = 0 }, 1); }
        catch (ExpeditionGenerationException) { rejected = true; }
        Require(rejected, "invalid generation input fails explicitly");
    }

    private static void VerifyNavigationAndProjection()
    {
        var state = new ExpeditionStateDto
        {
            Size = 5, PositionIndex = 12,
            Cells = Enumerable.Range(0, 25).Select(index => new ExpeditionCellDto { Index = index, Walkable = true }).ToList()
        };
        ExpeditionNavigation.RevealAround(state, 12);
        Require(state.Cells.Count(cell => cell.Revealed) == 9, "eight-neighbor reveal");
        var hidden = ExpeditionMapProjection.Create(state).Cells[0];
        Require(!hidden.Revealed && !hidden.Walkable && hidden.BiomeIndex == -1 && hidden.SiteId.Length == 0,
            "fog projection contains no future topology or content");
        Require(ExpeditionNavigation.FindKnownPath(state, 0).IsEmpty, "pathfinding does not use unseen target");
        Require(!ExpeditionNavigation.TryStep(state, 6, out _) && state.PositionIndex == 12,
            "diagonal movement rejected without mutation");
        state.Sites.Add(new ExpeditionSiteDto { SiteId = "guard", CellIndex = 13, EncounterProfileId = "enemy" });
        state.Cells[13].SiteId = "guard";
        Require(!ExpeditionNavigation.TryStep(state, 13, out _) && state.PositionIndex == 12, "uncleared enemy blocks its cell");
        Require(ExpeditionNavigation.FindKnownPath(state, 13).IsEmpty, "enemy target cannot be auto-walked");
        state.Sites[0].Cleared = true;
        Require(ExpeditionNavigation.TryStep(state, 13, out _) && state.PositionIndex == 13, "cleared enemy restores actual passage");
        Require(state.Cells[14].Revealed && state.Cells[9].Revealed, "movement reveals new frontier");
        var captured = ExpeditionNavigation.FindKnownPath(state, 6);
        Require(!captured.IsEmpty && captured.All(index => state.Cells[index].Revealed), "path contains only previously known cells");
        var actionBudget = state.RemainingActions;
        foreach (var index in captured) Require(ExpeditionNavigation.TryStep(state, index, out _), "captured known path legal steps");
        Require(state.RemainingActions == actionBudget, "navigation cannot spend an action");
    }

    private static CompiledExpedition Fixture()
    {
        var encounterPool = new CompiledContentPool("enemies", ContentPoolKind.Enemy, ["enemy"]);
        var rulePool = new CompiledContentPool("rules", ContentPoolKind.FloorRule, ["field_a", "field_b"]);
        var bossRulePool = new CompiledContentPool("boss_rules", ContentPoolKind.FloorRule, ["boss_field"]);
        var profiles = ImmutableDictionary.CreateBuilder<string, CompiledExpeditionEncounter>(StringComparer.Ordinal);
        var biomes = ImmutableArray.CreateBuilder<CompiledExpeditionBiome>();
        var stages = ImmutableArray.CreateBuilder<CompiledExpeditionStage>();
        for (var biome = 0; biome < 3; biome++)
        {
            var ids = new List<string>();
            foreach (var (suffix, kind, opening) in new[]
            {
                ("early", TowerNodeType.Combat, true), ("normal", TowerNodeType.Combat, false),
                ("elite", TowerNodeType.Elite, false), ("boss", TowerNodeType.Boss, false)
            })
            {
                var id = $"biome_{biome}_{suffix}";
                var encounter = new CompiledEncounter(id + "_encounter", kind, id, encounterPool,
                    kind == TowerNodeType.Boss ? bossRulePool : rulePool, "enemy", 1, false, 0, null);
                var composition = new CompiledEncounterComposition(id + "_formation", id, 0, 0, ["enemy"], [new Vector2I(0, 0)]);
                profiles[id] = new CompiledExpeditionEncounter(id, encounter, composition, 1, 1, opening,
                    new CompiledRewardRecipe(id + "_reward", 0, []));
                if (kind != TowerNodeType.Boss) ids.Add(id);
            }
            biomes.Add(new CompiledExpeditionBiome("biome_" + biome, "地域", Colors.Green, [.. ids], [60, 40, 0, 0]));
            stages.Add(new CompiledExpeditionStage("stage_" + biome, "阶段", 6, biome > 0, $"biome_{biome}_boss"));
        }
        return new CompiledExpedition("map_fixture", 16, 1, 64, biomes.ToImmutable(), stages.ToImmutable(),
        [
            new("combat", "敌人", TowerNodeType.Combat, 7, 1, null),
            new("elite", "精英", TowerNodeType.Elite, 1, 1, null),
            new("recruit", "招募", TowerNodeType.Recruitment, 2, 1, null),
            new("shop", "商店", TowerNodeType.Shop, 1, 1, null),
            new("camp", "营地", TowerNodeType.Rest, 1, 1, null),
            new("ruin", "遗迹", TowerNodeType.Event, 1, 1, null)
        ], profiles.ToImmutable());
    }

    private static void VerifyOriginProtocol()
    {
        var key = new RelicRunKey(17, "hero", 2, 3);
        using var legacy = new RelicRunScope(key);
        var preparation = legacy.PrepareBattle();
        var expected = string.Create(CultureInfo.InvariantCulture,
            $"{key.Seed:x16}:{key.HeroId}:{key.FloorIndex}:{key.BattleNumber}:{preparation.SourceFingerprint}");
        Require(preparation.TransitionId == Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(expected))).ToLowerInvariant(),
            "legacy relic receipt hash bytes unchanged");
        using var first = new RelicRunScope(new RelicRunKey(17, "hero", -1, 3) { OriginKey = "run_a:action_1" });
        using var second = new RelicRunScope(new RelicRunKey(17, "hero", -1, 3) { OriginKey = "run_a:action_2" });
        Require(first.PrepareBattle().TransitionId != second.PrepareBattle().TransitionId,
            "different actions cannot share a relic receipt at the same battle count");
        var rejected = false;
        try { using var invalid = new RelicRunScope(new RelicRunKey(17, "hero", -1, 3)); }
        catch (ArgumentOutOfRangeException) { rejected = true; }
        Require(rejected, "missing floor requires an explicit origin");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
