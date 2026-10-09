using System;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;

namespace TowerAutobattler.Relics;

public sealed partial class RelicBattleScope
{
    private ImmutableArray<(BattleRuntimeInstance Instance, CompiledRelicCombatRule Rule)> _damageCombatRules = [];
    private ImmutableArray<(BattleRuntimeInstance Instance, CompiledRelicCombatRule Rule)> _criticalCombatRules = [];
    private ImmutableArray<(BattleRuntimeInstance Instance, CompiledRelicCombatRule Rule)> _timedCombatRules = [];
    private ImmutableHashSet<(string Instance, string Rule)> _timedCombatConsumed = ImmutableHashSet<(string, string)>.Empty;
    private ImmutableHashSet<(string Instance, string Rule, long Event)> _criticalCombatConsumed = ImmutableHashSet<(string, string, long)>.Empty;

    public bool HasEndOfTickRules => !_timedCombatRules.IsEmpty && _timedCombatConsumed.Count < _timedCombatRules.Length;
    public bool HasDueEndOfTickRules => !IsCompleted && _context is not null && HasEndOfTickRules &&
        _timedCombatRules.Any(value => value.Rule.Tick == _context.CurrentTick() &&
            !_timedCombatConsumed.Contains((value.Instance.InstanceId, value.Rule.RuleId)));

    private void ActivateCombatRules()
    {
        var rules = OrderedInstances().SelectMany(instance => instance.Definition.CombatRules.IsDefaultOrEmpty
            ? Enumerable.Empty<(BattleRuntimeInstance Instance, CompiledRelicCombatRule Rule)>()
            : instance.Definition.CombatRules.Select(rule => (Instance: instance, Rule: rule))).ToImmutableArray();
        _damageCombatRules = rules.Where(value => value.Rule.Kind is RelicCombatRuleKind.DamageAboveHealthRatio or
            RelicCombatRuleKind.DamageWithinBodyDistance or RelicCombatRuleKind.DamageAgainstBoss).ToImmutableArray();
        _criticalCombatRules = rules.Where(value => value.Rule.Kind == RelicCombatRuleKind.HealOnAttackCritical).ToImmutableArray();
        _timedCombatRules = rules.Where(value => value.Rule.Kind == RelicCombatRuleKind.HealMissingHealthAtTick).ToImmutableArray();
        if ((!_criticalCombatRules.IsEmpty || !_timedCombatRules.IsEmpty) && _context!.HealUnit is null)
            throw new InvalidOperationException("Relic healing rules require an authoritative heal callback.");
        foreach (var value in _damageCombatRules)
            _subscriptions.Add(_context!.CombatBindings.SubscribeCalculation(BattleCombatCalculationKind.Damage,
                Source(value.Instance, value.Rule.RuleId), 35,
                (request, amount) => ModifyCombatDamage(request, amount, value.Instance, value.Rule)));
        if (!_criticalCombatRules.IsEmpty)
        {
            var source = Source(_criticalCombatRules[0].Instance, "combat_critical");
            _subscriptions.Add(_context!.CombatBindings.Subscribe(BattleCombatEventKind.CriticalHit, source, 35,
                (fact, sink) =>
                {
                    if (fact.DamageClass != CombatDamageClass.BasicAttack || string.IsNullOrEmpty(fact.ActionId)) return;
                    if (!sink.Enqueue(source, 35, _ => HealAttackCritical(fact)))
                        throw new InvalidOperationException("Relic critical healing exceeded the combat reaction budget.");
                }));
        }
    }

    private float ModifyCombatDamage(BattleCombatCalculationRequest request, float amount,
        BattleRuntimeInstance instance, CompiledRelicCombatRule rule)
    {
        if (amount <= 0) return amount;
        var units = _context!.QueryUnits();
        var source = units.FirstOrDefault(unit => unit.RuntimeId == request.SourceRuntimeId);
        var target = units.FirstOrDefault(unit => unit.RuntimeId == request.TargetRuntimeId);
        if (source is null || source.Team != 0 || target is null || target.Team == 0 || !target.Alive) return amount;
        var matches = rule.Kind switch
            {
                RelicCombatRuleKind.DamageAboveHealthRatio => target.Attributes.GetValue(CombatAttribute.MaxHealth) > 0 &&
                    target.Health / target.Attributes.GetValue(CombatAttribute.MaxHealth) > rule.Threshold,
                RelicCombatRuleKind.DamageWithinBodyDistance => BodyDistance(source, target) <= rule.Threshold,
                RelicCombatRuleKind.DamageAgainstBoss => target.IsBoss,
                _ => false
            };
        return matches ? amount * (1 + rule.Amount * instance.Stacks) : amount;
    }

    private static double BodyDistance(RelicBattleUnitBinding source, RelicBattleUnitBinding target)
    {
        var dx = (double)source.Position.X - target.Position.X;
        var dy = (double)source.Position.Y - target.Position.Y;
        return Math.Max(0, Math.Sqrt(dx * dx + dy * dy) - source.BodyRadius - target.BodyRadius);
    }

    private void HealAttackCritical(BattleCombatEvent fact)
    {
        var owner = _context!.QueryUnits().FirstOrDefault(unit => unit.RuntimeId == fact.SourceRuntimeId);
        if (owner is not { Alive: true, Team: 0 }) return;
        foreach (var value in _criticalCombatRules)
        {
            var key = (value.Instance.InstanceId, value.Rule.RuleId, fact.Sequence);
            if (_criticalCombatConsumed.Contains(key)) continue;
            _criticalCombatConsumed = _criticalCombatConsumed.Add(key);
            _context.HealUnit!(owner.RuntimeId, value.Rule.Amount * value.Instance.Stacks,
                Source(value.Instance, value.Rule.RuleId, owner.RuntimeId));
        }
    }

    // Caller runs this in a Battle world transaction after this tick's damage,
    // deaths and reactions, before outcome completion. No catch-up for late units.
    public void EndOfTick()
    {
        if (!HasEndOfTickRules || IsCompleted) return;
        EnsureActive();
        var tick = _context!.CurrentTick();
        foreach (var value in _timedCombatRules)
        {
            var key = (value.Instance.InstanceId, value.Rule.RuleId);
            if (tick < value.Rule.Tick || _timedCombatConsumed.Contains(key)) continue;
            _timedCombatConsumed = _timedCombatConsumed.Add(key);
            if (tick != value.Rule.Tick) continue;
            foreach (var unit in _context.QueryUnits().Where(unit => unit.Alive && unit.Team == 0)
                .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal))
            {
                var missing = Math.Max(0, unit.Attributes.GetValue(CombatAttribute.MaxHealth) - unit.Health);
                if (missing > 0) _context.HealUnit!(unit.RuntimeId, missing * value.Rule.Amount * value.Instance.Stacks,
                    Source(value.Instance, value.Rule.RuleId, unit.RuntimeId));
            }
        }
    }

    private void ClearCombatRules()
    {
        _damageCombatRules = []; _criticalCombatRules = []; _timedCombatRules = [];
        _timedCombatConsumed = _timedCombatConsumed.Clear();
        _criticalCombatConsumed = _criticalCombatConsumed.Clear();
    }
}
