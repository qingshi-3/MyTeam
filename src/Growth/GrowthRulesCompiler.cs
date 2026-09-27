using System;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Content;

namespace TowerAutobattler.Growth;

public static class GrowthRulesCompiler
{
    public static CompiledGrowthRules Compile(GrowthRulesDefinition definition, ContentRegistry content)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(content);
        if (string.IsNullOrWhiteSpace(definition.StableId)) throw new InvalidOperationException("Growth rules StableId is required.");
        if (definition.MaterialsPerNode < 0 || definition.AscensionCost <= 0)
            throw new InvalidOperationException("成长材料配置无效。");

        var heroes = definition.Heroes.ToImmutableDictionary(hero => hero.ContentId, hero =>
        {
            if (hero.ProductionModes.Any(mode => !Enum.IsDefined((GrowthProductionMode)mode)) ||
                hero.ProductionModes.Distinct().Count() != hero.ProductionModes.Length ||
                !float.IsFinite(hero.GrowthRate) || hero.GrowthRate < 0 || hero.ResearchYield < 0)
                throw new InvalidOperationException($"成长产出配置无效：{hero.ContentId}");
            if (!content.TryGet(hero.ContentId, out var entry) || entry.Definition is not UnitDefinition { IsHero: true })
                throw new InvalidOperationException($"Growth hero is not a published hero: {hero.ContentId}");
            if (string.IsNullOrWhiteSpace(hero.AscensionId) || hero.AscendedLoadout is null)
                throw new InvalidOperationException($"Growth hero requires an authored ascension loadout: {hero.ContentId}");
            return new CompiledGrowthHero(hero.ContentId, [.. hero.ProductionModes.Select(mode => (GrowthProductionMode)mode)], hero.GrowthRate,
                hero.ResearchYield, hero.AscensionId, hero.AscensionName, hero.AscensionDescription,
                content.Graph.ResolveLoadout(hero.AscendedLoadout),
                hero.BaseLoadout is null ? null : content.Graph.ResolveLoadout(hero.BaseLoadout), hero.MaterialCategory);
        }, StringComparer.Ordinal);

        var spells = definition.Spells.ToImmutableDictionary(spell => spell.StableId, spell =>
        {
            if (string.IsNullOrEmpty(spell.StableId) || spell.ResearchCost <= 0)
                throw new InvalidOperationException("研究法术身份或花费无效。");
            if (spell.BattleLoadout is null) throw new InvalidOperationException($"Growth spell loadout is missing: {spell.StableId}");
            return new CompiledGrowthSpell(spell.StableId, spell.DisplayName, spell.Description,
                spell.ResearchCost, content.Graph.ResolveLoadout(spell.BattleLoadout));
        }, StringComparer.Ordinal);

        ValidatePool("first", definition.FirstDiscoveryPool, heroes, content);
        ValidatePool("advanced", definition.AdvancedDiscoveryPool, heroes, content);
        return new CompiledGrowthRules(definition.StableId, heroes, [.. definition.FirstDiscoveryPool],
            [.. definition.AdvancedDiscoveryPool], spells, definition.MaterialsPerNode, definition.AscensionCost);
    }

    private static void ValidatePool(string name, string[] ids,
        ImmutableDictionary<string, CompiledGrowthHero> heroes, ContentRegistry content)
    {
        if (ids.Distinct(StringComparer.Ordinal).Count() < 3)
            throw new InvalidOperationException($"Growth {name} discovery pool requires at least three distinct identities.");
        foreach (var id in ids)
            if (!heroes.ContainsKey(id) || !content.TryGet(id, out _))
                throw new InvalidOperationException($"Growth {name} discovery hero is not fully configured: {id}");
    }
}
