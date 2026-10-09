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
            GrowthProductionMode.Cultivation => $"树种培养：每次有效非终局行动，指定伙伴的攻击永久 +{hero.FixedAttackGain:0.##}、生命永久 +{hero.FixedMaxHealthGain:0.##}。",
            GrowthProductionMode.Research => $"研究：每完成一个节点，获得 {hero.ResearchYield} 点研究。",
            _ => throw new ArgumentOutOfRangeException()
        }).ToArray();
        var lines = production.ToList();
        if (hero.BreedingYield > 0)
            lines.Add($"育种：每次上阵参与有效非终局行动后增加 {hero.BreedingYield} 点育种。与花灵同时上阵时，每点使之后的树种额外增加 {hero.AttackGainPerBreedingPoint:0.##} 攻击、{hero.MaxHealthGainPerBreedingPoint:0.##} 生命；本次树种先使用原育种值。后备保留累计，但不育种或放大树种。");
        if (production.Length > 0)
            lines.Add("生产者与培养对象须在行动提交前上阵，也可培养自己；未指定时不产出。移动、预览、免费回访不产出，成长在本局保留，终局不再产出。");
        if (!string.IsNullOrWhiteSpace(hero.AscensionId))
            lines.Add($"升阶 · {rules.AscensionCost} 材料（仅一次）\n{AscensionBenefitText(hero)}\n另获高阶伙伴三选一。");
        return new(projected, lines.Count == 0 ? string.Empty : "成长方案", string.Join("\n\n", lines));
    }

    public static string AscensionBenefitText(CompiledGrowthHero hero) =>
        hero.AscensionBenefit?.Description ?? "本次升阶不改变该英雄的战斗能力。";

    public static string AscensionStatus(CompiledGrowthHero hero, bool ascended) => ascended
        ? hero.AscensionDisplay?.CompletedLabel ?? "已升阶"
        : hero.AscensionDisplay?.DisplayName ?? "升阶";
}
