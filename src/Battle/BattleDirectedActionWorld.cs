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

internal interface IBattleDirectedActionWorld
{
    int Tick { get; }
    IReadOnlyList<BattleUnitState> Units { get; }
    bool CanOccupy(Vector2I cell);
    bool IsDisplacing(BattleUnitState unit);
    bool HasLineAccess(BattleUnitState source, BattleUnitState target);
    CombatSourceRef AbilityOrigin(CompiledAbilityDefinition ability, string ownerId);
    void ReleaseUnit(string id);
    void ReleaseGoal(string id);
    void CancelEnemyAction(string id);
    void CancelRangedActions(BattleUnitState unit);
    void Hit(BattleUnitState source, BattleUnitState target, float amount, CombatSourceRef origin, EffectDamageType type);
    void Resolve(Action work);
    void Publish(BattleCombatEventDraft fact);
    void Present(string type, string source, string target, float value, Vector2 position, string cue,
        int entityId = 0, Vector2 origin = default, BattleAttackTiming? attackTiming = null,
        BattleDisplacementCue? displacement = null, BattleLineCue? line = null, BattleTrampleCue? trample = null);
}

public sealed partial class BattleSimulation
{
    private sealed class DirectedActionWorld(BattleSimulation owner) : IBattleDirectedActionWorld
    {
        public int Tick => owner.TickIndex;
        public IReadOnlyList<BattleUnitState> Units => owner._units;
        public bool CanOccupy(Vector2I cell) => owner._config.FloorRule.CanOccupy(cell);
        public bool IsDisplacing(BattleUnitState unit) => owner.IsDisplacing(unit);
        public bool HasLineAccess(BattleUnitState source, BattleUnitState target) => owner.HasLineAccess(source, target);
        public CombatSourceRef AbilityOrigin(CompiledAbilityDefinition ability, string ownerId) => owner.AbilityOrigin(ability, ownerId);
        public void ReleaseUnit(string id) => owner._movement?.ReleaseUnit(id);
        public void ReleaseGoal(string id) => owner._movement?.ReleaseGoal(id);
        public void CancelEnemyAction(string id) => owner.CancelEnemyAction(id);
        public void CancelRangedActions(BattleUnitState unit)
        {
            owner.CancelChargedLine(unit); owner.CancelProjectileWindups(unit);
            unit.ProjectileSequence = null;
            owner.InterruptBattleChannels(unit.RuntimeId);
            owner._movement?.ReleaseUnit(unit.RuntimeId);
        }
        public void Hit(BattleUnitState source, BattleUnitState target, float amount, CombatSourceRef origin, EffectDamageType type) => owner.ResolveLineSkillHit(source, target, amount, origin, type);
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
            int entityId = 0, Vector2 origin = default, BattleAttackTiming? attackTiming = null,
            BattleDisplacementCue? displacement = null, BattleLineCue? line = null, BattleTrampleCue? trample = null) =>
            owner.Emit(type, source, target, value, position, cue, entityId, origin, attackTiming: attackTiming,
                displacement: displacement, line: line, trample: trample);
    }
}
