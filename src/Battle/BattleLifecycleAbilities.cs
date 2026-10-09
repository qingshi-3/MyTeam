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
    private readonly BattleLifecycleScope _lifecycleRuntime;
    private bool PrepareLifecycleOperation(CompiledLifecycleOperation op, BattleUnitState owner, string target, out ImmutableArray<string> targets) => _lifecycleRuntime.PrepareLifecycleOperation(op, owner, target, out targets);
    private void ExecuteLifecycleOperation(CompiledLifecycleOperation op, ImmutableArray<string> targets, BattleUnitState owner, CombatSourceRef origin) => _lifecycleRuntime.ExecuteLifecycleOperation(op, targets, owner, origin);
    private void AdvanceBattleLifecycles() => _lifecycleRuntime.AdvanceBattleLifecycles();
    private void ReleaseBattleLifecycleBindings(string id) => _lifecycleRuntime.ReleaseBattleLifecycleBindings(id);
    private void InterruptBattleChannels(string id)
    {
        _matrixRuntime.CancelCompletedHeroActions(id);
        _lifecycleRuntime.InterruptBattleChannels(id);
    }

    private sealed class LifecycleWorld(BattleSimulation owner) : IBattleLifecycleWorld
    {
        public int Tick => owner.TickIndex;
        public bool ExecutingEcho => owner._executingEcho;
        public IReadOnlyList<BattleUnitState> Units => owner._units;
        public IReadOnlyDictionary<string, UnitSnapshot> SummonProfiles => owner._config.TacticalSummons;
        public ImmutableArray<string> ResolveTargets(CompiledEffectTargetQuery query, BattleUnitState source, string target, BattleTargetPolicy policy) => owner.ResolveBattleTargets(query, source, target, policy);
        public ImmutableArray<string> ResolveChannelTargets(CompiledEffectTargetQuery query, string source, string target) => BattleAbilityEffectOperations.ResolveTargets(query, owner.CaptureEffectSnapshot(Tick), owner.CaptureAbilitySnapshot(Tick), source, source, target);
        public bool TargetMatches(BattleTargetPolicy policy, BattleUnitState source, BattleUnitState target) => owner.TargetMatches(policy, source, target);
        public bool HasLineAccess(BattleUnitState source, BattleUnitState target) => owner.HasLineAccess(source, target);
        public Vector2? FindOpenNear(Vector2 origin, int team, float radius) => owner.FindOpenNear(origin, team, radius);
        public bool SpawnNear(UnitSnapshot profile, int team, Vector2 origin, float health, float damage, string source) => owner.SpawnTemporaryNear(profile, team, origin, health, damage, source);
        public float Heal(string source, BattleUnitState target, float amount, CombatSourceRef origin) => owner.HealLiving(source, target, amount, origin);
        public void HandleDeath(BattleUnitState? source, BattleUnitState target) => owner.HandleDeath(source, target);
        public void CancelProjectileWindups(BattleUnitState unit) => owner.CancelProjectileWindups(unit);
        public void CancelEnemyAction(string id) => owner.CancelEnemyAction(id);
        public void CancelDisplacement(string id) => owner.CancelDisplacement(id);
        public void ReleaseUnit(string id) => owner._movement?.ReleaseUnit(id);
        public void RearmBodyGrants(string id)
        { owner._vitality.Revive(id); owner._traitScope?.RearmOwnerGrants(id); owner._equipmentScope?.RearmOwnerGrants(id); }
        public void RearmPassiveGrants(string id)
        {
            owner._abilityScope?.RearmPassiveGrants(id);
            owner._pendingAbilityReactions.Add(new PendingAbilityReaction(id, AbilityTriggerKind.None, "", Tick,
                $"{owner._combatPipeline.ScopeId}:revive:{id}:{Tick}", 0, PassiveGrant: true));
        }
        public void ChangeTraitTeam(string id, int team) => owner._traitScope?.ChangeOwnerTeam(id, team);
        public CombatSourceRef Source(string id) => owner.ResolveCombatSource(id);
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
