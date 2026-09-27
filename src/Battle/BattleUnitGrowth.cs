using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Battle;

// The battle receives values, never a mutable Run object or a shared Resource.
public sealed record BattleUnitGrowth(
    float AddedAttack,
    float AddedMaxHealth,
    CompiledAbilityLoadout? LoadoutOverride = null,
    CompiledAbilityLoadout? BattleSpell = null)
{
    public UnitSnapshot Apply(UnitSnapshot original)
    {
        if (!float.IsFinite(AddedAttack) || AddedAttack < 0 ||
            !float.IsFinite(AddedMaxHealth) || AddedMaxHealth < 0)
            throw new InvalidOperationException("单位成长值无效。");
        var damage = original.Damage + AddedAttack;
        var health = original.MaxHealth + AddedMaxHealth;
        var loadout = LoadoutOverride ?? original.AbilityLoadout;
        if (BattleSpell is { } spell)
        {
            var abilities = (loadout?.Abilities ?? ImmutableArray<CompiledAbilityDefinition>.Empty).AddRange(spell.Abilities);
            if (abilities.Select(ability => ability.StableId).Distinct(StringComparer.Ordinal).Count() != abilities.Length)
                throw new InvalidOperationException("法术与单位能力身份重复。");
            loadout = new CompiledAbilityLoadout(abilities);
        }
        return original with
        {
            Damage = damage,
            MaxHealth = health,
            AbilityLoadout = loadout,
            AttributeDefinition = original.AttributeDefinition is { } attributes
                ? AttributeDefinitionCompiler.WithBaseValues(attributes, new Dictionary<CombatAttribute, float>
                {
                    [CombatAttribute.AttackDamage] = damage,
                    [CombatAttribute.MaxHealth] = health
                }) : null
        };
    }
}
