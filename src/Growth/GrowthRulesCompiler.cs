using System;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Content;

namespace TowerAutobattler.Growth;

public static class GrowthRulesCompiler
{
    public static CompiledGrowthRules Compile(GrowthRulesDefinition definition, ContentRegistry content)
        => Compile(definition, content.Catalog, content.Graph);

    // Compilation uses the candidate graph, before its registry is published.
    internal static CompiledGrowthRules Compile(GrowthRulesDefinition definition, ContentCatalog catalog, CompiledContentGraph graph)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var entries = catalog.AllEntries().ToDictionary(entry => entry.StableId, StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(definition.StableId)) throw new InvalidOperationException("Growth rules StableId is required.");
        if (definition.MaterialsPerNode < 0 || definition.AscensionCost <= 0)
            throw new InvalidOperationException("成长材料配置无效。");

        var heroes = definition.Heroes.ToImmutableDictionary(hero => hero.ContentId, hero =>
        {
            if (hero.ProductionModes.Any(mode => !Enum.IsDefined((GrowthProductionMode)mode)) ||
                hero.ProductionModes.Distinct().Count() != hero.ProductionModes.Length ||
                !float.IsFinite(hero.GrowthRate) || hero.GrowthRate < 0 || hero.ResearchYield < 0 ||
                !float.IsFinite(hero.FixedAttackGain) || hero.FixedAttackGain < 0 ||
                !float.IsFinite(hero.FixedMaxHealthGain) || hero.FixedMaxHealthGain < 0 || hero.BreedingYield < 0 ||
                !float.IsFinite(hero.AttackGainPerBreedingPoint) || hero.AttackGainPerBreedingPoint < 0 ||
                !float.IsFinite(hero.MaxHealthGainPerBreedingPoint) || hero.MaxHealthGainPerBreedingPoint < 0 ||
                hero.BreedingYield == 0 && (hero.AttackGainPerBreedingPoint != 0 || hero.MaxHealthGainPerBreedingPoint != 0))
                throw new InvalidOperationException($"成长产出配置无效：{hero.ContentId}");
            if (!entries.TryGetValue(hero.ContentId, out var entry) || entry.Definition is not UnitDefinition { IsHero: true })
                throw new InvalidOperationException($"Growth hero is not a published hero: {hero.ContentId}");
            if (string.IsNullOrWhiteSpace(hero.AscensionId))
                throw new InvalidOperationException($"Growth hero requires an ascension identity: {hero.ContentId}");
            var benefit = hero.AscensionBenefit;
            if (benefit is not null && (benefit.BattleLoadout is null || string.IsNullOrWhiteSpace(benefit.Description)))
                throw new InvalidOperationException($"升阶收益须同时配置实际能力与说明；未设计时请留空：{hero.ContentId}");
            return new CompiledGrowthHero(hero.ContentId, [.. hero.ProductionModes.Select(mode => (GrowthProductionMode)mode)], hero.GrowthRate,
                hero.ResearchYield, hero.AscensionId,
                hero.AscensionDisplay is { } display ? new(display.DisplayName, display.CompletedLabel) : null,
                benefit is null ? null : new(benefit.Description, graph.ResolveLoadout(benefit.BattleLoadout!)),
                hero.BaseLoadout is null ? null : graph.ResolveLoadout(hero.BaseLoadout), hero.MaterialCategory,
                hero.FixedAttackGain, hero.FixedMaxHealthGain, hero.BreedingYield,
                hero.AttackGainPerBreedingPoint, hero.MaxHealthGainPerBreedingPoint);
        }, StringComparer.Ordinal);

        if (heroes.Values.Count(hero => hero.BreedingYield > 0) > 1)
            throw new InvalidOperationException("当前育种规则只支持一种常驻育种英雄，多个来源尚未定义叠加规则。");

        var spells = definition.Spells.ToImmutableDictionary(spell => spell.StableId, spell =>
        {
            if (string.IsNullOrEmpty(spell.StableId) || spell.ResearchCost <= 0)
                throw new InvalidOperationException("研究法术身份或花费无效。");
            if (spell.BattleLoadout is null) throw new InvalidOperationException($"Growth spell loadout is missing: {spell.StableId}");
            return new CompiledGrowthSpell(spell.StableId, spell.DisplayName, spell.Description,
                spell.ResearchCost, graph.ResolveLoadout(spell.BattleLoadout));
        }, StringComparer.Ordinal);

        ValidatePool("first", definition.FirstDiscoveryPool, heroes);
        ValidatePool("advanced", definition.AdvancedDiscoveryPool, heroes);
        return new CompiledGrowthRules(definition.StableId, heroes, [.. definition.FirstDiscoveryPool],
            [.. definition.AdvancedDiscoveryPool], spells, definition.MaterialsPerNode, definition.AscensionCost);
    }

    private static void ValidatePool(string name, string[] ids,
        ImmutableDictionary<string, CompiledGrowthHero> heroes)
    {
        if (ids.Distinct(StringComparer.Ordinal).Count() < 3)
            throw new InvalidOperationException($"Growth {name} discovery pool requires at least three distinct identities.");
        foreach (var id in ids)
            if (!heroes.ContainsKey(id))
                throw new InvalidOperationException($"Growth {name} discovery hero is not fully configured: {id}");
    }
}
