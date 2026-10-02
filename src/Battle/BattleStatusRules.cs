using System;
using System.Linq;
using TowerAutobattler.Attributes;
using TowerAutobattler.Content;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    // These rules belong to ordinary statuses, so equipment, traits and abilities may reuse them.
    private void BindStatusRules()
    {
        var source = CombatSourceRef.System("status-rules");
        _abilityCombatSubscriptions.Add(_combatPipeline.SubscribeCalculation(BattleCombatCalculationKind.Damage, source, 0,
            (request, amount) =>
            {
                var attacker = _units.FirstOrDefault(unit => unit.RuntimeId == request.SourceRuntimeId);
                var target = _units.FirstOrDefault(unit => unit.RuntimeId == request.TargetRuntimeId);
                if (attacker is null || target is null || attacker.Team == target.Team) return amount;
                foreach (var status in _statusScope.ActiveDefinitions(attacker.RuntimeId))
                foreach (var rule in status.DamageRules.IsDefault ? [] : status.DamageRules)
                {
                    var matches = rule.Condition switch
                    {
                        StatusDamageCondition.TargetHealthAbove => target.Health / target.MaxHealth > rule.Threshold,
                        StatusDamageCondition.WithinRange => BattlefieldSpace.EdgeDistance(attacker, target) <= rule.Threshold,
                        StatusDamageCondition.BossTarget => target.Definition.IsBoss,
                        StatusDamageCondition.HarmfulStatusCount => target.Statuses.Where(s => s.Disposition == StatusDisposition.Harmful)
                            .Select(s => s.StableId).Distinct(StringComparer.Ordinal).Count() >= rule.Threshold,
                        StatusDamageCondition.MeleeAttack => attacker.AttackDelivery == AttackDelivery.Melee && request.DamageClass == CombatDamageClass.BasicAttack,
                        _ => false
                    };
                    if (matches) amount *= rule.Multiplier;
                }
                return amount;
            }));
        foreach (var kind in Enum.GetValues<BattleCombatEventKind>())
            _abilityCombatSubscriptions.Add(_combatPipeline.Subscribe(kind, source, 0, (fact, sink) =>
            {
                foreach (var owner in _units.Where(unit => unit.Alive).OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray())
                foreach (var status in _statusScope.ActiveDefinitions(owner.RuntimeId))
                foreach (var rule in status.Reactions.IsDefault ? [] : status.Reactions)
                {
                    if (rule.EventKind != fact.Kind || (rule.OwnerRole == StatusReactiveOwnerRole.OwnerIsSource
                        ? fact.SourceRuntimeId != owner.RuntimeId : fact.TargetRuntimeId != owner.RuntimeId)) continue;
                    var otherId = rule.OwnerRole == StatusReactiveOwnerRole.OwnerIsSource ? fact.TargetRuntimeId : fact.SourceRuntimeId;
                    var other = _units.FirstOrDefault(unit => unit.RuntimeId == otherId);
                    var targets = rule.Target switch
                    {
                        StatusReactionTarget.Owner => new[] { owner },
                        StatusReactionTarget.Counterpart => _units.Where(unit => unit.Alive && unit.RuntimeId == otherId).ToArray(),
                        StatusReactionTarget.EnemiesNearCounterpart when other is not null => _units.Where(unit => unit.Alive && unit.Team != owner.Team &&
                            BattlefieldSpace.EdgeDistance(unit, other) <= rule.Radius).OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray(),
                        _ => []
                    };
                    if (targets.Length == 0) continue;
                    if (!sink.Enqueue(CombatSourceRef.Status(status.StableId, owner.RuntimeId, status.StableId), 0, _ =>
                        _statusScope.ApplyBatch(targets.Where(target => target.Alive).Select(target =>
                            new StatusApplicationRequest(rule.Status, owner.RuntimeId, target.RuntimeId, fact.Tick)))))
                        throw new InvalidOperationException("Status reaction exceeded the battle reaction budget.");
                }
            }));
    }
}
