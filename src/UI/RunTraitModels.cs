using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.Run;
using TowerAutobattler.Traits;

namespace TowerAutobattler.UI;

public sealed record RunTraitViewModel(
    string TraitId, string DisplayName, Texture2D? Icon, int Value, int ActiveIndex,
    int? NextThreshold, IReadOnlyList<int> Thresholds, string Status, BattleLabTooltipInfo Hint);

// The run projection uses the same eligible, deduplicated contributions as battle preparation.
public static class RunTraitModels
{
    public static IReadOnlyList<RunTraitViewModel> Build(RunView run, ContentRegistry content)
    {
        var inputs = RunTraitSnapshotBuilder.CollectInputs(run, content.Graph);
        var snapshot = TraitSnapshotBuilder.Build(content.Graph.Traits, inputs);
        var rows = new List<RunTraitViewModel>();
        foreach (var definition in content.Graph.Traits)
        {
            var value = snapshot.Resolve(definition.StableId, 0);
            var owned = inputs.Where(input => input.TraitId == definition.StableId).ToArray();
            if (value.Value == 0 && owned.Length == 0) continue;
            var thresholds = definition.Breakpoints.Select(tier => tier.MinValue).ToArray();
            var next = thresholds.Where(threshold => threshold > value.Value).Select(threshold => (int?)threshold).FirstOrDefault();
            var activeIndex = value.ActiveBreakpoint?.Index ?? -1;
            var status = activeIndex < 0 ? next is { } first ? $"差 {first - value.Value} 激活" : "未激活"
                : next is { } threshold ? $"已激活 · 差 {threshold - value.Value} 升档" : "已激活 · 最高档";
            var members = value.Contributions.Select(input => Member(input.OwnerRuntimeId, input.SourceKind,
                input.ContentIdentity, input.Value, run, content)).ToArray();
            var reserves = owned.Where(input => !input.IsDeployed).GroupBy(input => input.OwnerRuntimeId)
                .Select(group => HeroName(group.Key, run, content)).Distinct().ToArray();
            var policy = definition.CountingPolicy;
            var counting = policy.DeploymentPolicy == TraitDeploymentPolicy.DeployedOnly ? "仅上阵成员计数" : "名册成员计数";
            counting += policy.DuplicateContentPolicy switch
            {
                TraitDuplicateContentPolicy.UniqueOwner => "；同一成员的自身标签与徽章不重复计数",
                TraitDuplicateContentPolicy.UniqueContent => "；同类来源按内容去重",
                _ => "；符合规则的贡献分别计数"
            };
            counting += policy.CountEquipment ? "；已穿戴装备按规则贡献" : "；装备不贡献";
            var rules = definition.Breakpoints.Select(tier =>
                $"（{tier.MinValue}）{(tier.Index == activeIndex ? "当前生效" : tier.MinValue <= value.Value ? "已替换" : "未启用")}\n" +
                TraitModelText.Describe(tier));
            var membership = "计入成员：" + (members.Length == 0 ? "暂无" : string.Join("、", members));
            if (reserves.Length > 0) membership += "\n后备成员：" + string.Join("、", reserves) +
                (policy.DeploymentPolicy == TraitDeploymentPolicy.DeployedOnly ? "（未计入）" : "（按名册规则计入）");
            rows.Add(new RunTraitViewModel(definition.StableId, definition.DisplayName,
                SemanticIcons.Catalog.TryResolve(definition.SemanticIconKey, out var icon) ? icon.Icon : null,
                value.Value, activeIndex, next, thresholds, status,
                new BattleLabTooltipInfo(definition.DisplayName, $"计数 {value.Value} · {status}",
                    Stats: membership + "\n\n" + counting + "。仅当前档效果生效。", Abilities: string.Join("\n\n", rules),
                    SingleEntry: true, AnchorBeside: true)));
        }
        return rows.OrderByDescending(row => row.ActiveIndex >= 0)
            .ThenByDescending(row => row.ActiveIndex)
            .ThenBy(row => row.NextThreshold is { } threshold ? threshold - row.Value : 0)
            .ThenByDescending(row => row.Value).ThenBy(row => row.TraitId, StringComparer.Ordinal).ToArray();
    }

    private static string HeroName(string id, RunView run, ContentRegistry content)
    {
        var hero = run.Roster.FirstOrDefault(hero => hero.InstanceId == id);
        return hero is not null && content.TryGet(hero.ContentId, out var entry) && entry.Definition is UnitDefinition unit
            ? unit.DisplayName : "额外贡献";
    }

    private static string Member(string owner, TraitContributionSourceKind kind, string identity, int value,
        RunView run, ContentRegistry content) => HeroName(owner, run, content) +
        (kind == TraitContributionSourceKind.Equipment && content.TryGet(identity, out var entry) && entry.Definition is ItemDefinition item
            ? $"（{item.DisplayName}）" : "") + (value > 1 ? $" +{value}" : "");
}
