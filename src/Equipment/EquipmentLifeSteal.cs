using System;
using System.Linq;
using TowerAutobattler.Battle;

namespace TowerAutobattler.Equipment;

public sealed partial class EquipmentBattleScope
{
    private bool IsFirstProvider(RuntimeEquipmentInstance instance, Func<RuntimeEquipmentInstance, bool> eligible) =>
        ReferenceEquals(instance, _instances.Values.Where(value => value.Owner.RuntimeId == instance.Owner.RuntimeId && eligible(value))
            .OrderBy(value => value.Snapshot.SlotIndex).ThenBy(value => value.Snapshot.InstanceId, StringComparer.Ordinal).FirstOrDefault());

    private void ActivateLifeSteal(EquipmentBattleRuntimeContext context)
    {
        foreach (var instance in _instances.Values.Where(value => value.Snapshot.Definition.LifeSteal is not null))
        {
            if (context.QueryUnit is null || context.HealUnit is null)
                throw new InvalidOperationException("Equipment health-damage life steal requires unit and healing bindings.");
            var rule = instance.Snapshot.Definition.LifeSteal!;
            instance.Subscriptions.Add(context.CombatBindings.Subscribe(BattleCombatEventKind.HealthLost, instance.Source, 0,
                (fact, sink) =>
                {
                    if (fact.SourceRuntimeId != instance.Owner.RuntimeId || fact.EffectiveValue <= 0 ||
                        !IsFirstProvider(instance, value => value.Snapshot.Definition.LifeSteal?.RuleId == rule.RuleId)) return;
                    var source = context.QueryUnit(fact.SourceRuntimeId);
                    var target = context.QueryUnit(fact.TargetRuntimeId);
                    if (source is not { Alive: true } || target is null || source.Team == target.Team) return;
                    var amount = fact.EffectiveValue * rule.Ratio * (fact.IsAreaDamage ? rule.AreaEfficiency : 1);
                    if (!sink.Enqueue(instance.Source, 0, _ =>
                    {
                        if (!IsCompleted && _instances.ContainsKey(instance.Snapshot.InstanceId) &&
                            context.QueryUnit(fact.SourceRuntimeId) is { Alive: true })
                            context.HealUnit(fact.SourceRuntimeId, amount, instance.Source);
                    })) throw new InvalidOperationException("Equipment life steal could not enter the reaction queue.");
                }));
        }
    }
}
