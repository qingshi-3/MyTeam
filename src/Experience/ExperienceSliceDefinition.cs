using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

namespace TowerAutobattler.Experience;

// Authored experiment inputs. Compilation freezes these fields; no live battle reads Resources.
[GlobalClass]
public partial class ExperienceSliceDefinition : Resource
{
    [Export] public string StableId { get; set; } = "";
    [Export] public int Revision { get; set; } = 1;
    [Export] public int Seed { get; set; } = 9052026;
    [Export] public string StartingHeroId { get; set; } = "";
    [Export] public string[] StartingCompanions { get; set; } = [];
    [Export] public string[] RecruitIds { get; set; } = [];
    [Export] public string[] RelicIds { get; set; } = [];
    [Export] public string[] EquipmentIds { get; set; } = [];
    [Export] public string StartingEquipmentId { get; set; } = "";
    [Export] public string EnemyFrontId { get; set; } = "";
    [Export] public string EnemySupportId { get; set; } = "";
    [Export] public string FloorRuleId { get; set; } = "";
    [Export(PropertyHint.Range, "2,8,1")] public int FirstEnemyCount { get; set; } = 4;
    [Export(PropertyHint.Range, "2,8,1")] public int SecondEnemyCount { get; set; } = 6;

    public CompiledExperienceSlice Compile(CompiledGamePackage package)
    {
        if (string.IsNullOrWhiteSpace(StableId) || Revision < 1 || Seed < 1 ||
            StartingCompanions.Length is < 1 or > 17 || RecruitIds.Length is < 1 or > 3 || RelicIds.Length != 3 ||
            EquipmentIds.Length != 3 || FirstEnemyCount is < 2 or > 8 || SecondEnemyCount is < 2 or > 8)
            throw new InvalidOperationException("试验定义需要 1–17 名初始同伴、1–3 项招募、各三项物品候选及合法版本／敌人数。");
        foreach (var ids in new[] { StartingCompanions, RecruitIds, RelicIds, EquipmentIds })
            if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
                throw new InvalidOperationException("同一试验候选组不能重复。");
        Unit(StartingHeroId, false);
        if (Required(StartingHeroId).Definition is not UnitDefinition { IsHero: true })
            throw new InvalidOperationException("试验起始英雄必须具备现有起始契约。");
        foreach (var id in StartingCompanions.Concat(RecruitIds)) Unit(id, false);
        if (StartingCompanions.Contains(StartingHeroId, StringComparer.Ordinal))
            throw new InvalidOperationException("试验初始同伴不能重复起始英雄。");
        Unit(EnemyFrontId, true); Unit(EnemySupportId, true);
        foreach (var id in RelicIds)
            if (Required(id).Definition is not ItemDefinition { ProductKind: ItemProductKind.Relic } ||
                !package.Content.Graph.TryGetRelic(id, out _))
                throw new InvalidOperationException("不是已发布遗物：" + id);
        foreach (var id in EquipmentIds.Append(StartingEquipmentId))
            if (Required(id).Definition is not ItemDefinition { ProductKind: ItemProductKind.Equipment } ||
                !package.Content.Graph.TryGetEquipment(id, out _))
                throw new InvalidOperationException("不是已发布装备：" + id);
        if (!package.Project.FloorRules.ContainsKey(FloorRuleId))
            throw new InvalidOperationException("试验场地未发布：" + FloorRuleId);

        var campaign = package.Project.Campaign;
        var enemyPool = new CompiledContentPool(StableId + "_enemies", ContentPoolKind.Enemy, [EnemySupportId]);
        var floors = new CompiledContentPool(StableId + "_floor", ContentPoolKind.FloorRule, [FloorRuleId]);
        var encounters = campaign.Regions[0].Encounters
            .SetItem(TowerNodeType.Combat, Encounter("first", TowerNodeType.Combat, "第一战 · 守卫与弩手", FirstEnemyCount))
            .SetItem(TowerNodeType.Elite, Encounter("second", TowerNodeType.Elite, "第二战 · 更密集的远程火力", SecondEnemyCount));
        // Both combats use ordinary Run settlement. A third, unused floor keeps the second
        // victory in Run scope; completing this experiment is not a production final-boss win.
        var project = package.Project with
        {
            StableId = StableId,
            Campaign = campaign with
            {
                StableId = StableId, FloorsPerRegion = 3,
                Regions = [campaign.Regions[0] with { DisplayName = "玩法试验", Encounters = encounters }],
                StarterPool = new CompiledContentPool(StableId + "_start", ContentPoolKind.Soldier, StartingCompanions.ToImmutableArray()),
                RecruitmentPool = new CompiledContentPool(StableId + "_recruits", ContentPoolKind.Soldier, RecruitIds.ToImmutableArray()),
                NodeTable = campaign.NodeTable with { BossLocalFloor = 2, Rotation = [TowerNodeType.Combat, TowerNodeType.Elite], RegularOptionCount = 2, RotationStride = 1 }
            },
            RunRules = package.Project.RunRules with { StarterRosterHeroCount = StartingCompanions.Length }
        };
        return new(StableId, Revision, (ulong)Seed, StartingHeroId, StartingEquipmentId,
            RecruitIds.ToImmutableArray(), RelicIds.ToImmutableArray(), EquipmentIds.ToImmutableArray(), project);

        CatalogEntry Required(string id) => package.Content.TryGet(id, out var entry) ? entry :
            throw new InvalidOperationException("试验内容未发布：" + id);
        void Unit(string id, bool enemy)
        {
            if (Required(id).Definition is not UnitDefinition unit || unit.IsEnemy != enemy || unit.IsTestDummy)
                throw new InvalidOperationException("试验单位阵营错误：" + id);
        }
        CompiledEncounter Encounter(string suffix, TowerNodeType type, string title, int count) =>
            new(StableId + "_" + suffix, type, title, enemyPool, floors, EnemyFrontId, count, false, 42, null);
    }
}

public sealed record CompiledExperienceSlice(string StableId, int Revision, ulong Seed,
    string StartingHeroId, string StartingEquipmentId, ImmutableArray<string> RecruitIds,
    ImmutableArray<string> RelicIds, ImmutableArray<string> EquipmentIds, CompiledGameProject Project);
