using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Domain;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private readonly BattleTechniqueScope _techniqueRuntime;
    private bool TechniqueBusy(BattleUnitState unit) => _techniqueRuntime.TechniqueBusy(unit);
    private BattleUnitState? DuelOpponent(BattleUnitState unit) => _techniqueRuntime.DuelOpponent(unit);
    private bool CanAimHook(BattleUnitState owner, BattleUnitState target, CompiledHookOperation hook) => _techniqueRuntime.CanAimHook(owner, target, hook);
    private bool PrepareTechnique(CompiledCombatTechniqueOperation op, BattleUnitState owner, string targetId, out ImmutableArray<string> targets) => _techniqueRuntime.PrepareTechnique(op, owner, targetId, out targets);
    private float CurrentGrit(BattleUnitState owner, string key) => _techniqueRuntime.CurrentGrit(owner, key);
    private void ExecuteTechnique(CompiledCombatTechniqueOperation op, BattleUnitState owner, ImmutableArray<string> targets, CompiledAbilityDefinition ability) => _techniqueRuntime.ExecuteTechnique(op, owner, targets, ability);
    private void AdvanceTechniques() => _techniqueRuntime.AdvanceTechniques();
    private void AdvanceHookFlights(IReadOnlyDictionary<string, Vector2> previous) => _techniqueRuntime.AdvanceHookFlights(previous);
    private void RemoveDefeatedTechniques(BattleUnitState unit) => _techniqueRuntime.RemoveDefeatedTechniques(unit);
    private void RefreshGritActionRequests(BattleUnitState owner) => _techniqueRuntime.RefreshGritActionRequests(owner);

    private sealed class TechniqueWorld(BattleSimulation owner) : IBattleTechniqueWorld
    {
        public int Tick => owner.TickIndex;
        public IReadOnlyList<BattleUnitState> Units => owner._units;
        public BattleCombatEvent? TriggerEvent => owner._abilityTriggerEvent;
        public bool CanOccupy(Vector2I cell) => owner._config.FloorRule.CanOccupy(cell);
        public bool IsDisplacing(BattleUnitState unit) => owner.IsDisplacing(unit);
        public bool EnemyBusy(BattleUnitState unit) => owner.EnemyActionBusy(unit);
        public bool HasLineAccess(BattleUnitState source, BattleUnitState target) => owner.HasLineAccess(source, target);
        public CombatSourceRef AbilityOrigin(CompiledAbilityDefinition ability, string ownerId) => owner.AbilityOrigin(ability, ownerId);
        public float ReadCounter(BattleUnitState unit, string key) => owner.ReadCounter(unit, key);
        public void WriteCounter(BattleUnitState unit, string key, float value) => owner.WriteCounter(unit, key, value);
        public void Hit(BattleUnitState source, BattleUnitState target, float amount, CombatSourceRef origin, EffectDamageType type, bool isAreaDamage = false) => owner.ResolveLineSkillHit(source, target, amount, origin, type, isAreaDamage);
        public float Heal(string source, BattleUnitState target, float amount, CombatSourceRef origin) => owner.HealLiving(source, target, amount, origin);
        public void TimedShield(string source, string target, float amount, int duration, CombatSourceRef origin) => owner.MatrixApplyTimedShield(source, target, amount, duration, origin);
        public void ReleaseUnit(string id) => owner._movement?.ReleaseUnit(id);
        public void CancelProjectileWindups(BattleUnitState unit) => owner.CancelProjectileWindups(unit);
        public void CancelChargedLine(BattleUnitState unit) => owner.CancelChargedLine(unit);
        public void CancelTrample(BattleUnitState unit) => owner.CancelTrample(unit);
        public void InterruptChannels(string id) => owner.InterruptBattleChannels(id);
        public float? LineHitTime(Vector2 start, Vector2 delta, float radius, BattleUnitState target) => BattleSimulation.LineHitTime(start, delta, radius, target);
        public bool CanDisplace(CompiledDisplacementOperation op, BattleUnitState source, BattleUnitState target) => owner.IsDisplacementTargetEligible(op, source, target) && owner.BuildDisplacementPlans(op, source, [target]).Count > 0;
        public void Displace(CompiledDisplacementOperation op, ImmutableArray<string> targets, BattleUnitState source, CombatSourceRef origin) => owner.ExecuteDisplacementOperation(op, targets, source, origin);
        public bool IsDisplacedBy(string id, string source) => owner._displacementRuntime.IsDisplacedBy(id, source);
        public void CancelDisplacement(string id) => owner.CancelDisplacement(id);
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
        public void Present(string type, string source, string target, float value, Vector2 position, string cue,
            Vector2 origin = default, BattleVfxCue? vfx = null, BattleAttackTiming? attackTiming = null, BattleLineCue? line = null) =>
            owner.Emit(type, source, target, value, position, cue, origin: origin, vfx: vfx, attackTiming: attackTiming, line: line);
    }
}
