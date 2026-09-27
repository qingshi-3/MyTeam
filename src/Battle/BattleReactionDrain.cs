using System;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private bool _drainingCombatMechanics;

    private void BindMatrixAbilityEvents()
    {
        if (!_units.Select(unit => unit.Definition).Concat(_config.TacticalSummons.Values)
                .Any(unit => unit.AbilityLoadout?.Abilities.Any(ability => ability.Operations.Any(op => op is CompiledMatrixOperation)) == true)) return;
        var origin = CombatSourceRef.System("matrix_ability_bridge");
        foreach (var kind in Enum.GetValues<BattleCombatEventKind>())
            _abilityCombatSubscriptions.Add(_combatPipeline.Subscribe(kind, origin, 40, (e, sink) =>
            {
                if (_matrixAbilities.Rules.IsEmpty) return;
                if (e.Kind != BattleCombatEventKind.UnitDefeated &&
                    !_matrixAbilities.Rules.Any(rule => MatrixEventMatches(rule.Operation.Event, e))) return;
                if (!sink.Enqueue(origin, 40, _ => ObserveMatrixAbilityEvent(e)))
                    throw new InvalidOperationException("Matrix ability reaction exceeded combat chain budget.");
            }));
        foreach (var kind in Enum.GetValues<BattleCombatCalculationKind>())
            _abilityCombatSubscriptions.Add(_combatPipeline.SubscribeCalculation(kind, origin, 40, MatrixAbilityCalculation));
    }

    // All modules receive the same committed facts. Alternate their queues until stable,
    // so a shield-break reaction can cause a skill reaction in this fixed tick.
    private void DrainAbilityReactions()
    {
        if (_drainingCombatMechanics) return;
        _drainingCombatMechanics = true;
        try
        {
            for (var iteration = 0; iteration < _combatPipeline.Limits.MaxDepth + 2; iteration++)
            {
                var eventCount = _combatPipeline.Events.Count;
                DrainLegacyAbilityReactions();
                DrainTraitMechanics();
                DrainMatrixAbilityEvents();
                if (_pendingAbilityReactions.Count == 0 && _combatPipeline.Events.Count == eventCount) return;
            }
            throw new InvalidOperationException("Cross-module combat reaction chain exceeded the depth budget.");
        }
        finally { _drainingCombatMechanics = false; }
    }
}
