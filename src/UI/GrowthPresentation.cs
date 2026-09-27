using System;
using System.Linq;
using TowerAutobattler.Battle;
using TowerAutobattler.Growth;

namespace TowerAutobattler.UI;

// Shared read-only projection for recruitment and opening inspection. Runtime
// ownership remains in RunApplication; presentation never mutates growth state.
public static class GrowthPresentation
{
    public sealed record Facts(UnitSnapshot Snapshot, string Title, string Description)
    {
        public bool HasGrowthPlan => !string.IsNullOrWhiteSpace(Description);
    }

    public static Facts Project(string contentId, UnitSnapshot snapshot, CompiledGrowthRules? rules)
    {
        if (rules is null || !rules.Heroes.TryGetValue(contentId, out var hero))
            return new(snapshot, string.Empty, string.Empty);
        var projected = hero.BaseLoadout is null ? snapshot : snapshot with { AbilityLoadout = hero.BaseLoadout };
        var production = hero.ProductionModes.Select(mode => mode switch
        {
            GrowthProductionMode.Attack => $"攻击培养：每完成一个节点，指定友军的攻击永久增加其初始攻击的 {hero.GrowthRate:P0}。",
            GrowthProductionMode.Vitality => $"生命培养：每完成一个节点，指定友军的生命上限永久增加其初始生命的 {hero.GrowthRate:P0}。",
            GrowthProductionMode.Research => $"研究：每完成一个节点，获得 {hero.ResearchYield} 点研究。",
            _ => throw new ArgumentOutOfRangeException()
        }).ToArray();
        var lines = production.ToList();
        if (production.Length > 0)
            lines.Add("每次选一种产出；生产者与培养对象须在出发前上阵。成长在本局保留，最终战不再产出。");
        if (!string.IsNullOrWhiteSpace(hero.AscensionId))
            lines.Add($"升阶 · {rules.AscensionCost} 材料（仅一次）\n{hero.AscensionDescription}\n另获高阶伙伴三选一。");
        return new(projected, lines.Count == 0 ? string.Empty : "成长方案", string.Join("\n\n", lines));
    }
}
