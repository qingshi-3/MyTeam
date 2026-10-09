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
    private readonly BattleMatrixQueries _matrixQueries;
    private readonly BattleMatrixReactionScope _matrixReactions;
    private readonly BattleMatrixAbilityScope _matrixRuntime;
    private readonly BattlePermanentGrowthScope _permanentGrowth = new();
    private BattleUnitState? MatrixUnit(string id) => _units.FirstOrDefault(unit => unit.RuntimeId == id);
    private void MatrixBeforeDeath(BattleUnitState unit) => _matrixReactions.BeforeDeath(unit.RuntimeId);
    private void ObserveMatrixAbilityEvent(BattleCombatEvent fact) { _matrixRuntime.ObserveControl(fact); _matrixRuntime.ObserveOriginalAttack(fact); _matrixReactions.Observe(fact); }
    private void DrainMatrixAbilityEvents() => _matrixReactions.Drain();
    private bool PrepareMatrixOperation(CompiledMatrixOperation op, BattleUnitState owner, string target, out ImmutableArray<string> targets) => _matrixRuntime.PrepareMatrixOperation(op, owner, target, out targets);
    private void ExecuteMatrixOperation(ResolvedAbilityOperation resolved, AbilityExecutionPlan plan, BattleUnitState owner) => _matrixRuntime.ExecuteMatrixOperation(resolved, plan, owner);
    private void MatrixExecute(CompiledMatrixOperation op, BattleUnitState owner, string target, CombatSourceRef origin, BattleCombatEvent? fact, Vector2? center, string slot) => _matrixRuntime.MatrixExecute(op, owner, target, origin, fact, center, slot);
    private void MatrixDisplacementArrived(BattleUnitState source, BattleUnitState target, CombatSourceRef origin) => _matrixRuntime.MatrixDisplacementArrived(source, target, origin);
    private void MatrixDisplacementFinished(CombatSourceRef origin,bool completed = true) => _matrixRuntime.MatrixDisplacementFinished(origin,completed);
    public System.Collections.Immutable.ImmutableArray<BattleFrontalGuardSnapshot> FrontalGuards => _matrixRuntime.FrontalGuards;
    public System.Collections.Immutable.ImmutableArray<BattleGroundStormSnapshot> GroundStorms => _matrixRuntime.GroundStorms;
    private bool MatrixHoldsAction(string id) => _matrixRuntime.HoldsAction(id);
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
        public bool HasLineAccess(Vector2 from,Vector2 to) => owner.HasLineAccess(from,to);
        public int NextIndex(int count) => owner._random.NextInt(0,count);
        public bool CanOccupyTerrain(Vector2I cell) => owner._config.FloorRule.CanOccupy(cell);
        public Vector2? FindOpenNear(Vector2 origin, int team, float radius) => owner.FindOpenNear(origin, team, radius);
        public bool SpawnNear(UnitSnapshot profile, int team, Vector2 origin, float health, float damage, string source) => owner.SpawnTemporaryNear(profile, team, origin, health, damage, source);
        public bool ExtraSummon(string id) => owner.TraitMechanicsExtraSummon(id);
        public bool AdditionalAttack(BattleUnitState source, BattleUnitState target, float damage, string actionId,
            BattleDamageStatisticsAttribution statisticsAttribution = default) =>
            owner.ResolveAdditionalAttack(source, target, damage, actionId, statisticsAttribution);
        public void ApplyAuraStatus(string id, int ticks, float radius)
        {
            var status = BattleMechanicStatus.Create("attack_dodge_aura", "庇护领域", ticks) with
            {
                Magnitude = radius,
                DispelCategory = StatusDispelCategory.NonDispellable,
                AggregationPolicy = StatusAggregationPolicy.BySource,
                DurationRefreshPolicy = StatusDurationRefreshPolicy.Reset,
                Description = "身边范围内友军闪避普通与合格追加攻击；技能和持续伤害不受影响。",
                Presentation = new CompiledStatusPresentation("status", "", "", "", "", "庇护", "attack_dodge_aura")
            };
            owner._statusScope.Apply(status, id, id, owner.TickIndex);
        }
        public void Control(string source, string target, int ticks) => owner._statusScope.Apply(
            BattleMechanicStatus.Create("ability_stun", "眩晕", ticks, TowerAutobattler.Statuses.StatusBehaviorKind.DisableActions, harmful: true), source, target, owner.TickIndex);
        public void ApplyDisarm(string source, string target, int ticks) => owner._statusScope.Apply(
            BattleMechanicStatus.Create("ability_disarm", "缴械", ticks, harmful: true) with
            {
                GrantedTags = [StatusDefinitionCompiler.AttackDisabledTag],
                Description = "不能发起普通攻击；可以移动与施放技能，已有引导不受影响。",
                Presentation = new CompiledStatusPresentation("status", "", "", "", "", "缴械")
            }, source, target, owner.TickIndex);
        public void ApplyMatrixMarker(string source,string target,string id,string label,int ticks,float magnitude,bool harmful = false) =>
            owner._statusScope.Apply(BattleMechanicStatus.Create(id,label,ticks,harmful:harmful) with
            {
                Magnitude=magnitude, AggregationPolicy=StatusAggregationPolicy.ByTarget,
                DurationRefreshPolicy=StatusDurationRefreshPolicy.Reset,
                Presentation=new CompiledStatusPresentation("status","","","","",label)
            },source,target,owner.TickIndex);
        public void ClearMatrixMarker(string source,string target,string id) => owner._statusScope.Dispel(target,id,source);
        public void FireSequenceShot(BattleUnitState unit) => owner.FireSequenceShot(unit);
        public void LaunchProjectile(BattleUnitState source, BattleUnitState target, float damage, CombatSourceRef origin,
            float bounceRange = 0,float bounceSpellRatio = 0,Vector2? launchPosition = null,string excludedTargetId = "") =>
            owner.LaunchProjectile(source,target,damage,origin,bounceRange,bounceSpellRatio,launchPosition,excludedTargetId);
        public float Damage(string sourceId, BattleUnitState source, BattleUnitState target, float amount, CombatSourceRef origin,
            EffectDamageType type, CombatDamageClass damageClass, string actionId,
            BattleDamageStatisticsAttribution statisticsAttribution = default, bool isAreaDamage = false, CombatPoint? attackIncomingDirection = null) =>
            owner.ApplyDamage(sourceId, source, target, amount, origin, type, damageClass, actionId,
                statisticsAttribution,isAreaDamage,attackIncomingDirection);
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
