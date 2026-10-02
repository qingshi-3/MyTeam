using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private readonly BattleValueLedger _values;
    private readonly BattleValueOperations _valueOperations;
    private BattleCombatEvent? _abilityTriggerEvent;
    private bool _executingEcho;
    private float ReadCounter(BattleUnitState unit, string key, bool shared = false) => _values.ReadCounter(unit, key, shared);
    private void WriteCounter(BattleUnitState unit, string key, float value, bool shared = false) => _values.WriteCounter(unit, key, value, shared);
    private int ActiveBattleTicks(BattleUnitView unit) => _values.ActiveBattleTicks(unit);
    public string DescribeBattleResources(string id) => _values.DescribeBattleResources(id);
    private bool TargetMatches(BattleTargetPolicy policy, BattleUnitState owner, BattleUnitState target) => _valueOperations.TargetMatches(policy, owner, target);
    private ImmutableArray<string> ResolveBattleTargets(CompiledEffectTargetQuery query, BattleUnitState owner, string target, BattleTargetPolicy policy = BattleTargetPolicy.Any) => _valueOperations.ResolveBattleTargets(query, owner, target, policy);
    private bool PrepareBattleOperation(CompiledAbilityOperation op, BattleUnitState owner, string target, out ImmutableArray<string> targets) => _valueOperations.PrepareBattleOperation(op, owner, target, out targets);
    private void ExecuteBattleOperation(ResolvedAbilityOperation resolved, AbilityExecutionPlan plan, BattleUnitState owner) => _valueOperations.ExecuteBattleOperation(resolved, plan, owner);

    private sealed class ValueWorld(BattleSimulation owner) : IBattleValueWorld
    {
        public int Tick => owner.TickIndex;
        public bool ExecutingEcho => owner._executingEcho;
        public BattleCombatEvent? TriggerEvent => owner._abilityTriggerEvent;
        public IReadOnlyList<BattleUnitState> Units => owner._units;
        public EffectWorldSnapshot Snapshot(int tick) => owner.CaptureEffectSnapshot(tick);
        public CombatSourceRef AbilityOrigin(CompiledAbilityDefinition ability, string ownerId) => owner.AbilityOrigin(ability, ownerId);
        public float Damage(string source, BattleUnitState attacker, BattleUnitState target, float amount, CombatSourceRef origin, EffectDamageType type) => owner.ApplyDamage(source, attacker, target, amount, origin, type);
        public float Heal(string source, BattleUnitState target, float amount, CombatSourceRef origin) => owner.HealLiving(source, target, amount, origin);
        public float Shield(string source, BattleUnitState target, float amount, CombatSourceRef origin) => owner.ApplyShield(source, target, amount, origin);
        public void ShieldConsumed(BattleUnitState target, float amount, bool enemyDamage, CombatSourceRef origin) => owner.TraitMechanicsShieldConsumed(target, amount, enemyDamage, origin);
        public void EnqueueEcho(string donor, CompiledAbilityDefinition echo, string source) =>
            owner._pendingAbilityReactions.Add(new PendingAbilityReaction(donor, AbilityTriggerKind.OwnerDefeated, "", Tick,
                $"{owner._combatPipeline.ScopeId}:echo:{source}:{Tick}", 1, Echo: echo));
        public void Present(string type, string source, string target, float value, Vector2 position, string cue, BattleVfxCue? vfx = null) =>
            owner.Emit(type, source, target, value, position, cue, vfx: vfx);
    }
}
