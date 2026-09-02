using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;

namespace TowerAutobattler.BattleLab;

[Flags]
public enum BattleLabUnitClassification
{
    None = 0,
    PlayerHero = 1,
    PveNormal = 2,
    PveElite = 4,
    PveBoss = 8,
    PublishedSummon = 16
}

public sealed record BattleLabPublishedUnit(
    CatalogEntry Entry,
    UnitDefinition Definition,
    BattleLabUnitClassification Classification,
    ImmutableArray<BattleLabSide> AllowedSides)
{
    public string StableId => Definition.Id;
    public string DisplayName => Definition.DisplayName;
}

public sealed record BattleLabPublishedItem(CatalogEntry Entry, ItemDefinition Definition)
{
    public string StableId => Definition.Id;
    public string DisplayName => Definition.DisplayName;
}

public sealed class BattleLabContentIndex
{
    private readonly ImmutableDictionary<string, BattleLabPublishedUnit> _units;
    private readonly ImmutableDictionary<string, ImmutableHashSet<Godot.Vector2I>> _occupiableCells;

    public BattleLabContentIndex(CompiledGamePackage package)
    {
        Package = package ?? throw new ArgumentNullException(nameof(package));
        Rules = package.Project.RunRules;
        DefaultFloorRuleId = package.Project.FloorRules.Keys
            .OrderBy(id => id, StringComparer.Ordinal)
            .FirstOrDefault() ?? throw new InvalidOperationException("项目没有可用的地形规则。");
        var occupiableCells = ImmutableDictionary.CreateBuilder<string, ImmutableHashSet<Godot.Vector2I>>(
            StringComparer.Ordinal);
        foreach (var (id, scene) in package.Project.FloorRules.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var root = scene.Instantiate<FloorRuleContentRoot>();
            try
            {
                var runtime = root.CreateRuntime();
                occupiableCells.Add(id, Enumerable.Range(0, BattlefieldLayout.Height)
                    .SelectMany(y => Enumerable.Range(0, BattlefieldLayout.Width)
                        .Select(x => new Godot.Vector2I(x, y)))
                    .Where(runtime.CanOccupy)
                    .ToImmutableHashSet());
            }
            finally { root.Free(); }
        }
        _occupiableCells = occupiableCells.ToImmutable();
        var encounterSets = package.Project.Campaign.Regions
            .SelectMany(region => region.Encounters.Values)
            .ToArray();
        var normalIds = encounterSets
            .Where(encounter => encounter.NodeType == TowerNodeType.Combat)
            .SelectMany(encounter => encounter.EnemyPool.ContentIds.Append(encounter.LeadEnemyId))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        var eliteIds = encounterSets
            .Where(encounter => encounter.NodeType == TowerNodeType.Elite)
            .SelectMany(encounter => encounter.EnemyPool.ContentIds.Append(encounter.LeadEnemyId))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        var summonIds = CollectSummonIds(package).ToHashSet(StringComparer.Ordinal);
        var units = ImmutableDictionary.CreateBuilder<string, BattleLabPublishedUnit>(StringComparer.Ordinal);
        foreach (var entry in package.Content.Catalog.AllEntries())
        {
            if (entry?.Definition is not UnitDefinition definition) continue;
            var classification = BattleLabUnitClassification.None;
            if (definition.IsHero && !definition.IsEnemy)
                classification |= BattleLabUnitClassification.PlayerHero;
            if (definition.IsEnemy)
            {
                if (normalIds.Contains(definition.Id)) classification |= BattleLabUnitClassification.PveNormal;
                if (eliteIds.Contains(definition.Id)) classification |= BattleLabUnitClassification.PveElite;
                if (definition.Role == UnitRole.Boss) classification |= BattleLabUnitClassification.PveBoss;
                if (definition.Role != UnitRole.Boss &&
                    (classification & (BattleLabUnitClassification.PveNormal |
                                       BattleLabUnitClassification.PveElite)) == 0)
                    classification |= BattleLabUnitClassification.PveNormal;
            }
            if (summonIds.Contains(definition.Id)) classification |= BattleLabUnitClassification.PublishedSummon;
            if (classification == BattleLabUnitClassification.None) continue;
            var allowed = ImmutableArray.CreateBuilder<BattleLabSide>();
            if ((classification & BattleLabUnitClassification.PlayerHero) != 0) allowed.Add(BattleLabSide.Player);
            if ((classification & (BattleLabUnitClassification.PveNormal | BattleLabUnitClassification.PveElite |
                                   BattleLabUnitClassification.PveBoss | BattleLabUnitClassification.PublishedSummon)) != 0)
                allowed.Add(BattleLabSide.Enemy);
            units.Add(definition.Id, new BattleLabPublishedUnit(entry, definition, classification, allowed.ToImmutable()));
        }
        _units = units.ToImmutable();
        PlayerHeroes = _units.Values.Where(unit => unit.Classification.HasFlag(BattleLabUnitClassification.PlayerHero))
            .OrderBy(unit => unit.StableId, StringComparer.Ordinal).ToImmutableArray();
        PveUnits = _units.Values.Where(unit => unit.AllowedSides.Contains(BattleLabSide.Enemy))
            .OrderBy(unit => unit.Classification).ThenBy(unit => unit.StableId, StringComparer.Ordinal).ToImmutableArray();
        Equipment = package.Content.Catalog.Items.Where(entry =>
                entry.Definition is ItemDefinition { ProductKind: ItemProductKind.Equipment } definition &&
                package.Content.Graph.TryGetEquipment(definition.Id, out _))
            .Select(entry => new BattleLabPublishedItem(entry, (ItemDefinition)entry.Definition))
            .OrderBy(item => item.StableId, StringComparer.Ordinal).ToImmutableArray();
        Relics = package.Content.Catalog.Items.Where(entry =>
                entry.Definition is ItemDefinition { ProductKind: ItemProductKind.Relic } definition &&
                package.Content.Graph.TryGetRelic(definition.Id, out _))
            .Select(entry => new BattleLabPublishedItem(entry, (ItemDefinition)entry.Definition))
            .OrderBy(item => item.StableId, StringComparer.Ordinal).ToImmutableArray();
    }

    public CompiledGamePackage Package { get; }
    public CompiledRunRules Rules { get; }
    public ImmutableArray<BattleLabPublishedUnit> PlayerHeroes { get; }
    public ImmutableArray<BattleLabPublishedUnit> PveUnits { get; }
    public ImmutableArray<BattleLabPublishedItem> Equipment { get; }
    public ImmutableArray<BattleLabPublishedItem> Relics { get; }
    public string DefaultFloorRuleId { get; }

    public bool TryGetUnit(string stableId, out BattleLabPublishedUnit unit) =>
        _units.TryGetValue(stableId, out unit!);

    public string ResolveFloorRuleId(string? stableId)
    {
        var resolved = string.IsNullOrWhiteSpace(stableId) ? DefaultFloorRuleId : stableId;
        if (!Package.Project.FloorRules.ContainsKey(resolved))
            throw new InvalidOperationException($"未知的地形规则：{resolved}");
        return resolved;
    }

    public Godot.PackedScene ResolveFloorRuleScene(string stableId)
    {
        var resolved = ResolveFloorRuleId(stableId);
        return Package.Project.FloorRules[resolved];
    }

    public bool CanOccupy(string floorRuleId, Godot.Vector2I cell)
    {
        var resolved = ResolveFloorRuleId(floorRuleId);
        return _occupiableCells[resolved].Contains(cell);
    }

    private static IEnumerable<string> CollectSummonIds(CompiledGamePackage package)
    {
        foreach (var ability in package.Content.Graph.Abilities)
        foreach (var summon in ability.Operations.OfType<CompiledSummonAbilityOperation>())
            if (!string.IsNullOrWhiteSpace(summon.SummonContentId)) yield return summon.SummonContentId;
        foreach (var command in package.Content.Graph.TacticalCommands)
        foreach (var summon in command.Ability.Operations.OfType<CompiledSummonAbilityOperation>())
            if (!string.IsNullOrWhiteSpace(summon.SummonContentId)) yield return summon.SummonContentId;
        foreach (var relic in package.Content.Graph.Relics)
        foreach (var summon in relic.BattleStartEffects.OfType<CompiledRelicBattleStartSummon>())
            if (!string.IsNullOrWhiteSpace(summon.ContentId)) yield return summon.ContentId;
        foreach (var entry in package.Content.Catalog.AllEntries())
        {
            if (entry?.Definition is not UnitDefinition) continue;
            var root = entry.Scene.Instantiate<UnitContentRoot>();
            try
            {
                if (!string.IsNullOrWhiteSpace(root.Behavior?.SummonContentId))
                    yield return root.Behavior.SummonContentId;
            }
            finally { root.Free(); }
        }
    }
}
