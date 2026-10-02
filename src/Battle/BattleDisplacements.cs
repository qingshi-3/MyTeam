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

public sealed record DisplacementReservation(string RuntimeId, Vector2 Position, float Radius);

public sealed partial class BattleSimulation
{
    private readonly BattleDisplacementScope _displacementRuntime;
    private bool IsDisplacing(BattleUnitState unit) => _displacementRuntime.IsDisplacing(unit);
    private bool IsAirborne(BattleUnitState unit) => _displacementRuntime.IsAirborne(unit);
    private IReadOnlyList<DisplacementReservation> CaptureDisplacementReservations() => _displacementRuntime.CaptureDisplacementReservations();
    private bool PrepareDisplacementOperation(CompiledDisplacementOperation op, BattleUnitState owner, string target, out ImmutableArray<string> targets) => _displacementRuntime.PrepareDisplacementOperation(op, owner, target, out targets);
    private void ExecuteDisplacementOperation(CompiledDisplacementOperation op, ImmutableArray<string> targets, BattleUnitState owner, CombatSourceRef origin) => _displacementRuntime.ExecuteDisplacementOperation(op, targets, owner, origin);
    private bool IsDisplacementTargetEligible(CompiledDisplacementOperation op, BattleUnitState owner, BattleUnitState target) => _displacementRuntime.IsDisplacementTargetEligible(op, owner, target);
    private List<BattleDisplacementScope.DisplacementPlan> BuildDisplacementPlans(CompiledDisplacementOperation op, BattleUnitState owner, IEnumerable<BattleUnitState> candidates) => _displacementRuntime.BuildDisplacementPlans(op, owner, candidates);
    private void AdvanceDisplacements(Dictionary<string, Vector2> previous) => _displacementRuntime.AdvanceDisplacements(previous);
    private void CancelDisplacement(string id) => _displacementRuntime.CancelDisplacement(id);
    private Vector2 ClipDisplacementTravel(BattleUnitState unit, Vector2 start, Vector2 end) => _displacementRuntime.ClipDisplacementTravel(unit, start, end);

    private sealed class DisplacementWorld(BattleSimulation owner) : IBattleDisplacementWorld
    {
        public int Tick => owner.TickIndex;
        public IReadOnlyList<BattleUnitState> Units => owner._units;
        public bool CanOccupy(Vector2I cell) => owner._config.FloorRule.CanOccupy(cell);
        public ImmutableArray<string> ResolveTargets(CompiledEffectTargetQuery query, BattleUnitState source, string target) => owner.ResolveBattleTargets(query, source, target);
        public void ReleaseUnit(string id) => owner._movement?.ReleaseUnit(id);
        public void CancelProjectileWindups(BattleUnitState unit) => owner.CancelProjectileWindups(unit);
        public void InterruptChannels(string id) => owner.InterruptBattleChannels(id);
        public float Damage(string source, BattleUnitState attacker, BattleUnitState target, float raw, CombatSourceRef origin, EffectDamageType type) => owner.ApplyDamage(source, attacker, target, raw, origin, type);
        public CombatDamageClass DamageClass(CombatSourceRef origin) => owner.DamageClassFor(origin);
        public void Arrived(BattleUnitState source, BattleUnitState target, CombatSourceRef origin) => owner.MatrixDisplacementArrived(source, target, origin);
        public void Finished(CombatSourceRef origin) => owner.MatrixDisplacementFinished(origin);
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
        public void Present(string type, string source, string target, float value, Vector2 position, string cue,
            Vector2 origin = default, BattleDisplacementCue? displacement = null) => owner.Emit(type, source, target, value, position, cue, origin: origin, displacement: displacement);
    }
}
