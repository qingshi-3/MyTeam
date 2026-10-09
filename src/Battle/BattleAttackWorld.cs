using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Domain;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private readonly BattleAttackScope _attacks;
    private void Attack(BattleUnitState owner, BattleUnitState target) => _attacks.Attack(owner, target);
    private bool ResolveAttackHit(BattleUnitState owner, BattleUnitState target, float raw, bool instantPiercing, string actionId = "",
        BattleDamageStatisticsAttribution statisticsAttribution = default, CombatPoint? attackIncomingDirection = null) =>
        _attacks.ResolveAttackHit(owner, target, raw, instantPiercing, actionId, statisticsAttribution, attackIncomingDirection);
    private void SelectAttackChainTarget(BattleUnitState owner, BattleUnitState target) => _attacks.SelectAttackChainTarget(owner, target);
    private void RegisterGrowingHit(BattleUnitState owner, BattleUnitState target) => _attacks.RegisterGrowingHit(owner, target);
    private void AdvanceGrowingAttacks(BattleUnitState owner, BattleUnitState target) => _attacks.AdvanceGrowingAttacks(owner, target);
    private void AdvanceProjectileSequence(BattleUnitState owner) => _attacks.AdvanceProjectileSequence(owner);
    private void FireSequenceShot(BattleUnitState owner) => _attacks.FireSequenceShot(owner);
    private bool AdvanceProjectileWindups(BattleUnitState owner) => _attacks.AdvanceProjectileWindups(owner);
    private void CancelProjectileWindups(BattleUnitState owner) => _attacks.CancelProjectileWindups(owner);

    private sealed class AttackWorld(BattleSimulation owner) : IBattleAttackWorld
    {
        public int Tick => owner.TickIndex;
        public IReadOnlyList<BattleUnitState> Units => owner._units;
        public CombatSourceRef Source(string id, BattleUnitState? unit = null) => owner.ResolveCombatSource(id, unit);
        public bool ProtectedFromAttack(BattleUnitState target) => owner._matrixRuntime.ProtectsFromAttack(target);
        public float DamageValue(BattleUnitState unit) => owner.EffectiveDamage(unit);
        public EnemyOriginalAttackPreparation PrepareOriginalAttack(string ownerRuntimeId) => owner._enemyRuntime.PrepareOriginalAttack(ownerRuntimeId);
        public float Damage(string sourceRuntimeId, BattleUnitState? source, BattleUnitState target, float raw,
            CombatSourceRef origin = default, EffectDamageType damageType = EffectDamageType.Normal,
            CombatDamageClass damageClass = CombatDamageClass.Other, string actionId = "",
            BattleDamageStatisticsAttribution statisticsAttribution = default, bool isAreaDamage = false, CombatPoint? attackIncomingDirection = null) =>
            owner.ApplyDamage(sourceRuntimeId, source, target, raw, origin, damageType, damageClass, actionId, statisticsAttribution, isAreaDamage, attackIncomingDirection);
        public float Heal(string source, BattleUnitState target, float amount) => owner.HealLiving(source, target, amount);
        public float ReadCounter(BattleUnitState unit, string key) => owner.ReadCounter(unit, key);
        public void WriteCounter(BattleUnitState unit, string key, float value) => owner.WriteCounter(unit, key, value);
        public bool HasLineAccess(BattleUnitState source, BattleUnitState target) => owner.HasLineAccess(source, target);
        public void LaunchProjectile(BattleUnitState source, BattleUnitState target, float damage, CombatSourceRef origin = default) => owner.LaunchProjectile(source, target, damage, origin);
        public void CancelChargedLine(BattleUnitState unit) => owner.CancelChargedLine(unit);
        public void ReleaseGoal(string id) => owner._movement!.ReleaseGoal(id);
        public void Publish(BattleCombatEventDraft fact) => owner.PublishCombat(fact);
        public void Present(string type, string source, string target, float value, Vector2 position, string cue,
            Vector2 origin = default, BattleVfxCue? vfx = null, BattleAttackTiming? attackTiming = null, string sourceVfx = "") =>
            owner.Emit(type, source, target, value, position, cue, origin: origin, vfx: vfx, attackTiming: attackTiming, sourceVfx: sourceVfx);
    }
}
