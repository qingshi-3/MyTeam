using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Components;
using TowerAutobattler.Domain;
using TowerAutobattler.Effects;
using TowerAutobattler.Equipment;
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Battle;

// Creates authoritative bodies from immutable authored profiles. Placement remains a world query.
internal sealed class BattleUnitFactory(BattleConfig _config, BattleAttributeScope _attributes)
{
    public BattleUnitState CreateInitial(BattleSpawn spawn, int index, Vector2I resolvedCell, Vector2 resolvedPosition)
    {
        var unit = spawn.Unit;
        var taggedForHero = !string.IsNullOrWhiteSpace(_config.HeroRule.RequiredSoldierTag) && unit.Tags.Contains(_config.HeroRule.RequiredSoldierTag);
        var healthMultiplier = spawn.Team == 0
            ? (unit.IsHero ? _config.Modifiers.HeroHealthMultiplier : _config.Modifiers.ArmyHealthMultiplier * _config.HeroRule.SoldierHealthMultiplier)
            : 1f;
        if (spawn.Team == 0 && !unit.IsHero && taggedForHero) healthMultiplier *= _config.HeroRule.TaggedSoldierHealthMultiplier;
        var damageMultiplier = spawn.Team == 0
            ? (unit.IsHero
                ? _config.Modifiers.HeroDamageMultiplier * _config.HeroRule.HeroDamageMultiplier *
                  (1f + _config.EmptyDeploymentSlots * (_config.HeroRule.EmptySlotHeroBonus + _config.Modifiers.EmptySlotPower / 100f))
                : _config.Modifiers.ArmyDamageMultiplier * _config.HeroRule.SoldierDamageMultiplier)
            : 1f;
        if (spawn.Team == 0 && !unit.IsHero && taggedForHero) damageMultiplier *= _config.HeroRule.TaggedSoldierDamageMultiplier;
        var maxHealth = unit.MaxHealth * healthMultiplier;
        var damage = unit.Damage * damageMultiplier;
        var lifeSteal = Mathf.Clamp(unit.LifeSteal + (spawn.Team == 0
            ? (unit.IsHero ? _config.Modifiers.HeroLifeStealBonus + _config.HeroRule.HeroLifeStealBonus : _config.Modifiers.ArmyLifeStealBonus)
            : 0), 0, .8f);
        var runtimeId = string.IsNullOrWhiteSpace(spawn.InstanceId)
            ? $"{(spawn.Team == 0 ? "p" : "e")}-{index}"
            : spawn.InstanceId;
        var attributes = _attributes.CreateSet(
            runtimeId,
            CreateAttributes(unit, maxHealth, damage, lifeSteal));
        return new BattleUnitState
        {
            RuntimeId = runtimeId,
            SourceInstanceId = spawn.InstanceId,
            Definition = unit,
            Attributes = attributes,
            Team = spawn.Team,
            InitialTeam = spawn.Team,
            Cell = resolvedCell,
            Position = resolvedPosition,
            Health = maxHealth * Mathf.Clamp(spawn.HealthRatio, .05f, 1f),
            Shield = spawn.Team == 0 ? _config.Modifiers.StartShield +
                (unit.IsHero ? maxHealth * _config.EmptyDeploymentSlots * _config.HeroRule.EmptySlotStartShield : 0) : 0,
            IsTemporary = spawn.IsTemporary,
            IsPersistentRosterHero = spawn.IsPersistentRosterHero ??
                (spawn.Team == 0 && !spawn.IsTemporary && unit.IsHero),
            BehaviorSummon = spawn.BehaviorSummon
        };
    }

    internal static CompiledAttributeSetDefinition CreateAttributes(
        UnitSnapshot unit,
        float maxHealth,
        float damage,
        float lifeSteal)
    {
        var definition = unit.AttributeDefinition ?? AttributeDefinitionCompiler.Legacy(
            new Dictionary<CombatAttribute, float>
            {
                [CombatAttribute.MaxHealth] = unit.MaxHealth,
                [CombatAttribute.AttackDamage] = unit.Damage,
                [CombatAttribute.SpellPower] = 0,
                [CombatAttribute.AttackSpeed] = 1,
                [CombatAttribute.Armor] = unit.Armor,
                [CombatAttribute.AttackRange] = unit.Range,
                [CombatAttribute.MoveSpeed] = 1,
                [CombatAttribute.CriticalChance] = 0,
                [CombatAttribute.CriticalDamage] = 1.5f,
                [CombatAttribute.MaxMana] = 0,
                [CombatAttribute.StartingMana] = 0,
                [CombatAttribute.HealingPower] = unit.HealPower,
                [CombatAttribute.LifeSteal] = unit.LifeSteal,
                [CombatAttribute.ControlResistance] = 0
            });
        return AttributeDefinitionCompiler.WithBaseValues(
            definition,
            new Dictionary<CombatAttribute, float>
            {
                [CombatAttribute.MaxHealth] = maxHealth,
                [CombatAttribute.AttackDamage] = damage,
                [CombatAttribute.Armor] = unit.Armor,
                [CombatAttribute.AttackRange] = unit.Range,
                [CombatAttribute.HealingPower] = unit.HealPower,
                [CombatAttribute.LifeSteal] = lifeSteal
            });
    }

}
