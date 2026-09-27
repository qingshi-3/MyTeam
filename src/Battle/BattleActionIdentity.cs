using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private long _abilityActionSequence;
    private ImmutableDictionary<string, string> _abilityActionIds = ImmutableDictionary<string, string>.Empty;
    private string AbilityActionKey(string ownerId, string abilityId) => ownerId + "|" + abilityId;
    private string BeginAbilityAction(string ownerId, CompiledAbilityDefinition ability)
    {
        var id = $"ability:{ownerId}:{ability.StableId}:{++_abilityActionSequence}";
        _abilityActionIds = _abilityActionIds.SetItem(AbilityActionKey(ownerId, ability.StableId), id);
        return id;
    }
    private CombatDamageClass DamageClassFor(CombatSourceRef origin)
    {
        if (origin.Kind == CombatSourceKind.Status) return CombatDamageClass.Periodic;
        if (origin.Kind is CombatSourceKind.Trait or CombatSourceKind.Equipment or CombatSourceKind.Relic)
            return CombatDamageClass.Derived;
        if (origin.Kind != CombatSourceKind.Ability) return CombatDamageClass.Other;
        var definition = _abilityScope?.Find(origin.OwnerRuntimeId, origin.StableId);
        if (definition?.Operations.Any(o => o is CompiledCounterattackOperation) == true)
            return CombatDamageClass.Counterattack;
        return definition?.IsActiveSkill == true ? CombatDamageClass.ActiveSkill : CombatDamageClass.Derived;
    }
}
