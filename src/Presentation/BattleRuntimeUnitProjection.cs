using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Presentation;

public sealed record BattleScreenRuntimeUnitSnapshot(
    string RuntimeId,
    string SourceInstanceId,
    string ContentId,
    string DisplayName,
    UnitRole Role,
    bool IsHero,
    int Team,
    Vector2I Cell,
    bool Alive,
    float Health,
    float MaxHealth,
    float Damage,
    float AttackSpeed,
    float Reach,
    float ControlResistance,
    ImmutableArray<BattleScreenEquipmentSnapshot> Equipment,
    ImmutableArray<TraitContributionSnapshot> TraitContributions,
    ImmutableArray<TraitPresentationSnapshot> TeamTraits,
    ImmutableArray<StatusRuntimeSnapshot> Statuses,
    BattleUnitMode Mode,
    BattleActionKind LastActionKind,
    string ActionTargetName,
    int AttackCooldown,
    int DisabledTicks,
    Vector2 Position = default,
    float CurrentMana = 0,
    float MaxMana = 0,
    float Shield = 0,
    ImmutableArray<CompiledAbilityDefinition> Abilities = default,
    string LastAbilityName = "",
    bool IsPersistentRosterHero = false,
    bool IsTemporary = false,
    long AttackHitStacks = 0, bool HasAttackGrowth = false, float Armor = 0, string BattleResources = "",
    AttackHitGrowthSnapshot? AttackGrowth = null, BattleUnitSkillsSnapshot? Skills = null);

public sealed record BattleScreenEquipmentSnapshot(
    string InstanceId,
    string ContentId,
    int SlotIndex,
    string DisplayName = "",
    string Description = "",
    Texture2D? Icon = null);

// Copies authoritative facts into UI values without advancing or retaining a battle.
public static class BattleRuntimeUnitProjection
{
    public static BattleScreenRuntimeUnitSnapshot Build(
        BattleUnitState unit, ContentRegistry? content, BattleConfig? config,
        TraitSnapshot? traitSnapshot, string battleResources, BattleUnitSkillsSnapshot? skills = null)
    {
        var equipment = config?.Equipment.Instances
            .Where(item => item.OwnerHeroInstanceId == unit.SourceInstanceId)
            .OrderBy(item => item.SlotIndex)
            .Select(item =>
            {
                var definition = content is not null && content.TryGet(item.ContentId, out var entry)
                    ? entry.Definition as ItemDefinition : null;
                return new BattleScreenEquipmentSnapshot(item.InstanceId, item.ContentId, item.SlotIndex,
                    definition?.DisplayName ?? "未知装备", definition?.Description ?? "", definition?.Icon);
            })
            .ToImmutableArray() ?? [];
        var contributions = traitSnapshot?.Contributions
            .Where(item => item.OwnerRuntimeId == unit.SourceInstanceId ||
                           item.OwnerRuntimeId == unit.RuntimeId)
            .OrderBy(item => item.TraitId, StringComparer.Ordinal)
            .ThenBy(item => item.SourceInstanceId, StringComparer.Ordinal)
            .ToImmutableArray() ?? [];
        var teamTraits = traitSnapshot?.Values
            .Where(value => value.Team == unit.Team)
            .OrderBy(value => value.TraitId, StringComparer.Ordinal)
            .Select(value => value.Presentation)
            .ToImmutableArray() ?? [];
        return new BattleScreenRuntimeUnitSnapshot(
            unit.RuntimeId,
            unit.SourceInstanceId,
            unit.Definition.ContentId,
            unit.Definition.DisplayName,
            unit.Definition.Role,
            unit.Definition.IsHero,
            unit.Team,
            unit.Cell,
            unit.Alive,
            unit.Health,
            unit.MaxHealth,
            unit.Damage,
            unit.Attributes.GetValue(CombatAttribute.AttackSpeed),
            unit.Attributes.GetValue(CombatAttribute.AttackRange),
            unit.Attributes.GetValue(CombatAttribute.ControlResistance),
            equipment,
            contributions,
            teamTraits,
            unit.Statuses,
            unit.Mode,
            unit.LastActionKind,
            unit.ActionTargetName,
            unit.AttackCooldown,
            unit.DisabledTicks,
            unit.Position,
            unit.CurrentMana,
            unit.MaxMana,
            unit.Shield,
            skills?.Abilities ?? unit.Definition.AbilityLoadout?.Abilities ?? [],
            unit.LastAbilityName,
            unit.IsPersistentRosterHero,
            unit.IsTemporary, unit.AttackHitStacks, unit.Definition.AttackHitGrowth is not null, unit.Armor,
            battleResources, unit.Definition.AttackHitGrowth, skills);
    }
}
