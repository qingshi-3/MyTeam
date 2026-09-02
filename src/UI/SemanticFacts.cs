using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.Run;
using TowerAutobattler.Traits;

namespace TowerAutobattler.UI;

public sealed record SemanticFact(StringName Key, string Text, StringName? ThemeTypeVariation = null);

public static class SemanticIconKeys
{
    public static readonly StringName Health = "health";
    public static readonly StringName Damage = "damage";
    public static readonly StringName Shield = "shield";
    public static readonly StringName Healing = "healing";
    public static readonly StringName Mana = "mana";
    public static readonly StringName TacticalPoint = "tactical_point";
    public static readonly StringName Gold = "gold";
    public static readonly StringName Time = "time";
    public static readonly StringName Kills = "kills";
    public static readonly StringName Deaths = "deaths";
    public static readonly StringName Hero = "hero";
    public static readonly StringName Melee = "melee";
    public static readonly StringName Ranged = "ranged";
    public static readonly StringName Risk = "risk";
    public static readonly StringName Loot = "loot";
    public static readonly StringName Reach = "reach";

    public static IReadOnlyList<StringName> Required { get; } = new StringName[]
        {
            Health, Damage, Shield, Healing, Mana, TacticalPoint, Gold, Time, Kills, Deaths, Hero, Melee, Ranged, Risk, Loot, Reach
        }
        .Concat(Enum.GetValues<UnitRole>().Select(Responsibility))
        .Concat(Enum.GetValues<UnitFaction>().Select(Faction))
        .Concat(Enum.GetValues<TowerNodeType>().Select(TowerNodeSemantic))
        .ToArray();

    public static StringName Responsibility(UnitRole role) => $"role.{role.ToString().ToLowerInvariant()}";
    public static StringName Faction(UnitFaction faction) => $"faction.{faction.ToString().ToLowerInvariant()}";
    public static StringName TowerNodeSemantic(TowerNodeType type) => $"tower.{type.ToString().ToLowerInvariant()}";
}

public static class UnitSemanticFacts
{
    private static readonly IReadOnlyDictionary<string, UnitFaction> GameplayFactions =
        new Dictionary<string, UnitFaction>(StringComparer.Ordinal)
        {
            ["order"] = UnitFaction.Order,
            ["desert"] = UnitFaction.Desert,
            ["undead"] = UnitFaction.Undead,
            ["beast"] = UnitFaction.Beast,
            ["machine"] = UnitFaction.Machine,
            ["frost"] = UnitFaction.Frost
        };

    public static SemanticFact Responsibility(UnitRole role, bool includeLabel = true) => new(
        SemanticIconKeys.Responsibility(role),
        (includeLabel ? "职责·" : string.Empty) + PlayerFacingText.DescribeUnitRole(role),
        "TraitIdentity");

    public static IReadOnlyList<SemanticFact> Traits(UnitFaction faction, IEnumerable<StringName> tags)
    {
        var factions = new List<UnitFaction> { faction };
        foreach (var tag in tags.Select(value => value.ToString()))
            if (GameplayFactions.TryGetValue(tag, out var taggedFaction) && !factions.Contains(taggedFaction))
                factions.Add(taggedFaction);
        return factions.Select(value => new SemanticFact(
            SemanticIconKeys.Faction(value), PlayerFacingText.DescribeUnitFaction(value), "TraitIdentity")).ToArray();
    }

    public static SemanticFact Health(string value, bool includeLabel = true) => new(
        SemanticIconKeys.Health, (includeLabel ? "生命 " : string.Empty) + value, "HealthValue");
    public static SemanticFact Damage(string value, bool includeLabel = true) => new(
        SemanticIconKeys.Damage, (includeLabel ? "伤害 " : string.Empty) + value, "DamageValue");
    public static SemanticFact Reach(float value, bool includeLabel = true) => new(
        SemanticIconKeys.Reach,
        includeLabel
            ? $"{UnitRangeClassifier.Describe(value)} · 距离 {value.ToString("0.#", CultureInfo.InvariantCulture)}"
            : $"{UnitRangeClassifier.Describe(value)} {value.ToString("0.#", CultureInfo.InvariantCulture)}",
        "RangeValue");
}

public static class TraitSemanticFacts
{
    public static SemanticFact From(TraitPresentationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (string.IsNullOrWhiteSpace(snapshot.SemanticIconKey) || string.IsNullOrWhiteSpace(snapshot.Text))
            throw new ArgumentException("Trait presentation snapshot is invalid.", nameof(snapshot));
        return new SemanticFact(
            new StringName(snapshot.SemanticIconKey),
            snapshot.Text,
            new StringName(snapshot.DisplayStyle));
    }
}
