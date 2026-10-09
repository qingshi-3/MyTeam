using System;
using System.Linq;
using TowerAutobattler.Attributes;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Battle;

internal sealed partial class BattleTraitReactionScope
{
    private bool IsMember(BattleTraitBinding binding, string id) => binding.Members.Contains(id) ||
        _state.Targets.TryGetValue("copy:" + id, out var original) && binding.Members.Contains(original);

    private void ApplyOpeningBonuses(BattleTraitBinding binding, string id)
    {
        if (binding.Spec.Kind == TraitMechanicKind.BaseHealthBonus)
            commands.AddBaseHealth(id, binding.Spec.Amount, TraitOrigin(binding, id));
        if (binding.Spec.Kind == TraitMechanicKind.CriticalTraining)
        {
            commands.AddAttribute(id, CombatAttribute.CriticalChance, binding.Spec.Amount, TraitOrigin(binding, id));
            commands.AddAttribute(id, CombatAttribute.CriticalDamage, binding.Spec.Secondary, TraitOrigin(binding, id));
        }
    }

    private void RosterTraitEvent(BattleCombatEvent fact)
    {
        if (fact.Kind == BattleCombatEventKind.BattleStarted)
        {
            foreach (var binding in _traitMechanicBindings)
                foreach (var member in TraitLiving(binding).ToArray()) ApplyOpeningBonuses(binding, member.RuntimeId);
            foreach (var binding in _traitMechanicBindings.Where(b => b.Spec.Kind == TraitMechanicKind.OpeningCopy))
            {
                var pool = TraitLiving(binding).Where(u => !u.IsTemporary).ToArray();
                if (pool.Length == 0) continue;
                var original = pool[Math.Min(pool.Length - 1, (int)(commands.NextRandom() * pool.Length))];
                if (!read.SummonProfiles.TryGetValue(original.Definition.ContentId, out var profile))
                    throw new InvalidOperationException($"Missing clean clone profile: {original.Definition.ContentId}");
                var id = commands.SpawnNear(profile with { AllowTemporaryMana = true }, binding.Team, original.Position, binding.Spec.Amount, 1, original.RuntimeId);
                if (id is null) continue; // No legal space: no overlap or silent replacement.
                _state = _state with { Targets = _state.Targets.SetItem("copy:" + id, original.RuntimeId), ExcludedDeaths = _state.ExcludedDeaths.Add(id) };
                TraitWrite("copy_damage:" + id, binding.Spec.Secondary);
                foreach (var inherited in _traitMechanicBindings.Where(b => b.Members.Contains(original.RuntimeId))) ApplyOpeningBonuses(inherited, id);
                commands.Present(original.RuntimeId, id, "skill_cast");
            }
            return;
        }
        if (fact.Kind == BattleCombatEventKind.UnitTransformed)
            foreach (var binding in TraitFor(fact.SourceRuntimeId, TraitMechanicKind.TransformHealth))
            {
                var key = TraitKey(binding, fact.SourceRuntimeId, "transformed");
                if (TraitCounter(key) > 0) continue;
                TraitWrite(key, 1);
                commands.AddBaseHealth(fact.SourceRuntimeId, binding.Spec.Amount, TraitOrigin(binding, fact.SourceRuntimeId));
            }
        if (fact.Kind != BattleCombatEventKind.AttackLanded || TraitUnit(fact.SourceRuntimeId) is not { Alive: true } source ||
            TraitUnit(fact.TargetRuntimeId) is not { } target) return;
        foreach (var binding in TraitFor(source.RuntimeId, TraitMechanicKind.FreezeOnAttack))
        {
            var key = TraitKey(binding, source.RuntimeId, $"freeze:{fact.ActionId}:{target.RuntimeId}");
            if (!target.Alive || TraitCounter(key) > 0) continue;
            TraitWrite(key, 1);
            if (commands.NextRandom() < binding.Spec.Amount)
                statuses.Apply(BattleMechanicStatus.Create("matrix_freeze", "冻结", binding.Spec.DurationTicks,
                    StatusBehaviorKind.DisableActions, harmful: true), source.RuntimeId, target.RuntimeId, TickIndex);
        }
        if (fact.ActionId.StartsWith("derived:", StringComparison.Ordinal)) return;
        foreach (var binding in TraitFor(source.RuntimeId, TraitMechanicKind.AdditionalAttacks))
        {
            var key = TraitKey(binding, source.RuntimeId, "extra:" + fact.ActionId);
            if (TraitCounter(key) > 0 || source.Attributes.GetValue(CombatAttribute.AttackRange) < 1.5f) continue;
            TraitWrite(key, 1);
            if (commands.NextRandom() >= binding.Spec.Amount) continue;
            var range = source.Attributes.GetValue(CombatAttribute.AttackRange);
            foreach (var other in read.Units.Where(u => u.Alive && u.Team != source.Team && u.RuntimeId != target.RuntimeId &&
                         u.Position.DistanceTo(source.Position) <= range + source.BodyRadius + u.BodyRadius)
                         .OrderBy(u => u.Position.DistanceSquaredTo(source.Position)).ThenBy(u => u.RuntimeId, StringComparer.Ordinal).Take(binding.Spec.Count).ToArray())
                commands.AdditionalAttack(source.RuntimeId, other.RuntimeId, source.Damage * binding.Spec.Secondary,
                    $"derived:{fact.ActionId}:{binding.Id}:{other.RuntimeId}",
                    new(CombatSourceKind.Trait, binding.Id, CombatDamageClass.Derived));
        }
    }

    private void AdvanceDelayedTeamStrike()
    {
        foreach (var binding in _traitMechanicBindings.Where(b => b.Spec.Kind == TraitMechanicKind.DelayedTeamStrike))
        {
            var key = TraitKey(binding, "team", "fired");
            if (TickIndex < binding.Spec.DurationTicks || TraitCounter(key) > 0) continue;
            TraitWrite(key, 1);
            var anchor = read.Units.Where(u => u.Team == binding.Team && binding.Members.Contains(u.RuntimeId))
                .OrderBy(u => u.RuntimeId, StringComparer.Ordinal).FirstOrDefault();
            if (anchor is null) continue;
            foreach (var enemy in read.Units.Where(u => u.Alive && u.Team != binding.Team).OrderBy(u => u.RuntimeId, StringComparer.Ordinal).ToArray())
            {
                commands.Damage(anchor.RuntimeId, enemy.RuntimeId, Math.Min(binding.Spec.Limit, enemy.MaxHealth * binding.Spec.Amount),
                    TraitOrigin(binding, anchor.RuntimeId), damageClass: CombatDamageClass.Derived, isAreaDamage: true);
                if (enemy.Alive) statuses.Apply(BattleMechanicStatus.Create("storm_stun", "雷击眩晕", binding.Spec.Count,
                    StatusBehaviorKind.DisableActions, harmful: true), anchor.RuntimeId, enemy.RuntimeId, TickIndex);
                commands.Present(anchor.RuntimeId, enemy.RuntimeId, "skill_cast");
            }
            foreach (var member in TraitLiving(binding).ToArray())
                commands.AddAttribute(member.RuntimeId, CombatAttribute.AttackSpeed, binding.Spec.Secondary, TraitOrigin(binding, member.RuntimeId));
        }
    }
}
