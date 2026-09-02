using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Battle;

public static class BattleSetupFactory
{
    public static UnitSnapshot Snapshot(
        UnitDefinition definition,
        UnitBehaviorComponent? behavior = null,
        CompiledAbilityLoadout? abilityLoadout = null,
        CompiledContentGraph? graph = null) => new(
        definition.Id, definition.DisplayName, definition.Role, definition.IsHero, definition.Role == UnitRole.Boss,
        definition.MaxHealth, definition.AttackDamage, definition.AttackRange,
        Math.Max(1, Mathf.RoundToInt(definition.AttackCooldown / BattleTiming.TickSeconds)),
        Math.Max(1, Mathf.RoundToInt(definition.MoveInterval / BattleTiming.TickSeconds)),
        definition.Armor, definition.HealPower, definition.SplashRadius, definition.LifeSteal,
        definition.Tags.Select(tag => tag.ToString()).ToArray(),
        behavior is null ? new UnitBehaviorSnapshot() : new UnitBehaviorSnapshot(
            behavior.SlowOnHitTicks, behavior.AdjacentArmorAura, behavior.AdjacentDamageAura,
            behavior.ExecuteHealthThreshold, behavior.LowHealthDamageBonus, behavior.OnDeathDamage, behavior.PiercingLine,
            behavior.PeriodicShieldTicks, behavior.PeriodicShieldAmount, behavior.PeriodicSummonTicks, behavior.PeriodicSummonLimit,
            behavior.PreferBacklineTargets, behavior.SummonContentId),
        abilityLoadout,
        AttributeDefinitionCompiler.Legacy(new Dictionary<CombatAttribute, float>
        {
            [CombatAttribute.MaxHealth] = definition.MaxHealth,
            [CombatAttribute.AttackDamage] = definition.AttackDamage,
            [CombatAttribute.SpellPower] = 0,
            [CombatAttribute.AttackSpeed] = 1,
            [CombatAttribute.Armor] = definition.Armor,
            [CombatAttribute.MagicResistance] = 0,
            [CombatAttribute.AttackRange] = definition.AttackRange,
            [CombatAttribute.MoveSpeed] = 1,
            [CombatAttribute.CriticalChance] = 0,
            [CombatAttribute.CriticalDamage] = 1.5f,
            [CombatAttribute.MaxMana] = 0,
            [CombatAttribute.StartingMana] = 0,
            [CombatAttribute.HealingPower] = definition.HealPower,
            [CombatAttribute.LifeSteal] = definition.LifeSteal,
            [CombatAttribute.ControlResistance] = definition.BaseControlResistance
        }),
        graph?.ResolveUnitTraitContributions(definition.Id) ??
        ImmutableArray<CompiledTraitContribution>.Empty);

    public static UnitSnapshot Snapshot(CatalogEntry entry, ContentRegistry? content = null)
    {
        var root = entry.Scene.Instantiate<UnitContentRoot>();
        try
        {
            var loadout = root.AbilityLoadout is null || content is null
                ? null
                : root.AbilityLoadout.Resolve(content.Graph);
            return Snapshot((UnitDefinition)entry.Definition, root.Behavior, loadout, content?.Graph);
        }
        finally { root.Free(); }
    }

    public static HeroRuleSnapshot Snapshot(HeroRuleComponent component) => new(
        component.SoldierHealthMultiplier,
        component.SoldierDamageMultiplier, component.HeroDamageMultiplier,
        component.EmptySlotHeroBonus, component.EmptySlotHeroDefense, component.EmptySlotStartShield, component.PreferBossTargets,
        component.RequiredSoldierTag.ToString(), component.TaggedSoldierHealthMultiplier,
        component.TaggedSoldierDamageMultiplier, component.FormationArmorBonus, component.FormationDamageBonus,
        component.KillGrowth, component.HeroLifeStealBonus, component.SummonOnAllyDeath, component.AddBattleConstruct,
        component.BattleGoldBonus, component.RecruitConversionGold, component.SummonContentId);

}
