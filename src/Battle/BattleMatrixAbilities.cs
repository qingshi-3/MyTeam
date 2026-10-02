using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private readonly BattleMatrixQueries _matrixQueries;
    private readonly BattleMatrixReactionScope _matrixReactions;
    private readonly BattleMatrixAbilityScope _matrixRuntime;
    private readonly BattlePermanentGrowthScope _permanentGrowth = new();
    private BattleUnitState? MatrixUnit(string id) => _units.FirstOrDefault(unit => unit.RuntimeId == id);
    private void MatrixBeforeDeath(BattleUnitState unit) => _matrixReactions.BeforeDeath(unit.RuntimeId);
    private void ObserveMatrixAbilityEvent(BattleCombatEvent fact) => _matrixReactions.Observe(fact);
    private void DrainMatrixAbilityEvents() => _matrixReactions.Drain();
    private bool PrepareMatrixOperation(CompiledMatrixOperation op, BattleUnitState owner, string target, out ImmutableArray<string> targets) => _matrixRuntime.PrepareMatrixOperation(op, owner, target, out targets);
    private void ExecuteMatrixOperation(ResolvedAbilityOperation resolved, AbilityExecutionPlan plan, BattleUnitState owner) => _matrixRuntime.ExecuteMatrixOperation(resolved, plan, owner);
    private void MatrixExecute(CompiledMatrixOperation op, BattleUnitState owner, string target, CombatSourceRef origin, BattleCombatEvent? fact, Vector2? center, string slot) => _matrixRuntime.MatrixExecute(op, owner, target, origin, fact, center, slot);
    private void MatrixDisplacementArrived(BattleUnitState source, BattleUnitState target, CombatSourceRef origin) => _matrixRuntime.MatrixDisplacementArrived(source, target, origin);
    private void MatrixDisplacementFinished(CombatSourceRef origin) => _matrixRuntime.MatrixDisplacementFinished(origin);
    private void AdvanceMatrixAbilities() => _matrixRuntime.AdvanceMatrixAbilities();
    private void MatrixPruneBuffs() => _matrixRuntime.MatrixPruneBuffs();
    private float MatrixAbilityCalculation(BattleCombatCalculationRequest request, float amount) => _matrixRuntime.MatrixAbilityCalculation(request, amount);
    private void ClearMatrixAbilities() => _matrixRuntime.ClearMatrixAbilities();

    private sealed class MatrixOperationWorld(BattleSimulation owner) : IBattleMatrixOperationWorld
    {
        public int Tick => owner.TickIndex;
        public IReadOnlyList<BattleUnitState> Units => owner._units;
        public IReadOnlyDictionary<string, UnitSnapshot> SummonProfiles => owner._config.TacticalSummons;
        public CombatSourceRef AbilityOrigin(CompiledAbilityDefinition ability, string ownerId) => owner.AbilityOrigin(ability, ownerId);
        public bool HasLineAccess(BattleUnitState source, BattleUnitState target) => owner.HasLineAccess(source, target);
        public Vector2? FindOpenNear(Vector2 origin, int team, float radius) => owner.FindOpenNear(origin, team, radius);
        public bool SpawnNear(UnitSnapshot profile, int team, Vector2 origin, float health, float damage, string source) => owner.SpawnTemporaryNear(profile, team, origin, health, damage, source);
        public bool ExtraSummon(string id) => owner.TraitMechanicsExtraSummon(id);
        public void FireSequenceShot(BattleUnitState unit) => owner.FireSequenceShot(unit);
        public void LaunchProjectile(BattleUnitState source, BattleUnitState target, float damage, CombatSourceRef origin) => owner.LaunchProjectile(source, target, damage, origin);
        public float Damage(string sourceId, BattleUnitState source, BattleUnitState target, float amount, CombatSourceRef origin, EffectDamageType type, CombatDamageClass damageClass, string actionId) => owner.ApplyDamage(sourceId, source, target, amount, origin, type, damageClass, actionId);
        public float Heal(string source, BattleUnitState target, float amount, CombatSourceRef origin) => owner.HealLiving(source, target, amount, origin);
        public void Retire(BattleUnitState unit, string source, string reason) => owner.RetireEnemyProduct(unit, source, reason);
        public void Resolve(Action work)
        {
            var checkpoint = new BattleWorldStateCheckpoint(owner);
            try
            {
                using var resolution = owner._combatPipeline.BeginAuthoritativeResolution();
                work(); resolution.Commit(); checkpoint.Commit();
            }
            catch { checkpoint.Rollback(); throw; }
        }
        public void Publish(BattleCombatEventDraft fact) => owner.PublishCombat(fact);
        public void Present(string type, string source, string target, float value, Vector2 position, string cue) => owner.Emit(type, source, target, value, position, cue);
    }
}
