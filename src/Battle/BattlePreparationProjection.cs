using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Battle;

public sealed record BattlePreparedAttributes(ImmutableDictionary<CombatAttribute, float> Values)
{
    public float GetValue(CombatAttribute attribute) => Values.GetValueOrDefault(attribute);
}

public sealed record BattlePreparedUnit(string RuntimeId, string SourceInstanceId, UnitSnapshot Definition,
    int Team, Vector2 Position, Vector2I Cell, float BodyRadius, bool IsTemporary, float Health, float Shield,
    float CurrentMana, float MaxMana, BattlePreparedAttributes Attributes, ImmutableArray<StatusRuntimeSnapshot> Statuses)
{
    public float MaxHealth => Attributes.GetValue(CombatAttribute.MaxHealth);
    public float Damage => Attributes.GetValue(CombatAttribute.AttackDamage);
    public float Armor => Attributes.GetValue(CombatAttribute.Armor);
    public float HealingPower => Attributes.GetValue(CombatAttribute.HealingPower);
    public float LifeSteal => Attributes.GetValue(CombatAttribute.LifeSteal);
}

public sealed record BattlePreparationSnapshot(ImmutableArray<BattlePreparedUnit> Units, TraitSnapshot Traits,
    ImmutableArray<BattleOpeningAllyLink> OpeningAllyLinks = default);

// One projection boundary for deployment, opponent browsing, reserves and laboratory.
// It reuses authoritative initialization, including grants and battle-start effects.
public static class BattlePreparationProjection
{
    public static BattlePreparationSnapshot Build(BattleConfig config) => BattleSimulation.PrepareProjection(config);
}

public sealed partial class BattleSimulation
{
    internal static BattlePreparationSnapshot PrepareProjection(BattleConfig config)
    {
        using var world = new BattleSimulation(config, preparationOnly: true);
        return CapturePreparation(world);
    }

    internal static BattlePreparationSnapshot CapturePreparation(BattleSimulation world) => new(
        world.UnitViews.Select(unit => new BattlePreparedUnit(unit.RuntimeId, unit.SourceInstanceId,
            unit.Definition with { AbilityLoadout = unit.AbilityLoadout },
            unit.Team, unit.Position, unit.Cell, unit.BodyRadius, unit.IsTemporary, unit.Health, unit.Shield,
            unit.CurrentMana, unit.MaxMana, new BattlePreparedAttributes(Enum.GetValues<CombatAttribute>()
                .ToImmutableDictionary(attribute => attribute, attribute => unit.Attributes.GetValue(attribute))),
            unit.Statuses)).ToImmutableArray(), world.TraitSnapshot, world.OpeningAllyLinks);
}
