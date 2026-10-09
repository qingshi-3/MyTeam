using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Components;
using TowerAutobattler.Domain;
using TowerAutobattler.Effects;
using TowerAutobattler.Equipment;
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private float ApplyDamage(string sourceRuntimeId, BattleUnitState? source, BattleUnitState target, float raw,
        CombatSourceRef origin = default, EffectDamageType damageType = EffectDamageType.Normal,
        CombatDamageClass damageClass = CombatDamageClass.Other, string actionId = "",
        BattleDamageStatisticsAttribution statisticsAttribution = default,
        bool isAreaDamage = false, CombatPoint? attackIncomingDirection = null) =>
        _vitality.Damage(sourceRuntimeId, source, target, raw, origin, damageType, damageClass, actionId, statisticsAttribution, isAreaDamage, attackIncomingDirection);
    private float HealLiving(string sourceRuntimeId, BattleUnitState target, float amount, CombatSourceRef origin = default) =>
        _vitality.Heal(sourceRuntimeId, target, amount, origin);
    private float ApplyShield(string sourceRuntimeId, BattleUnitState target, float amount, CombatSourceRef origin = default) =>
        _vitality.Shield(sourceRuntimeId, target, amount, origin);
    private void HandleDeath(BattleUnitState? source, BattleUnitState target) => _vitality.HandleDeath(source, target);

    private sealed class VitalityWorld(BattleSimulation owner) : IBattleVitalityWorld
    {
        public int Tick => owner.TickIndex;
        public IReadOnlyList<BattleUnitState> Units => owner._units;
        public float ModifyIncomingDamage(BattleUnitState target, float amount) => owner._config.FloorRule.ModifyIncomingDamage(owner.CreateRuleContext(), target, amount);
        public float Armor(BattleUnitState target) => owner.EffectiveArmor(target);
        public CombatSourceRef Source(string runtimeId, BattleUnitState? explicitUnit = null) => owner.ResolveCombatSource(runtimeId, explicitUnit);
        public CombatDamageClass DamageClass(CombatSourceRef source) => owner.DamageClassFor(source);
        public void ShieldConsumed(BattleUnitState target, float amount, bool enemyDamage, CombatSourceRef origin) => owner.TraitMechanicsShieldConsumed(target, amount, enemyDamage, origin);
        public void HealthChanged(BattleUnitState target) => owner.RefreshGritActionRequests(target);
        public void BeforeDeath(BattleUnitState target)
        {
            owner.MatrixBeforeDeath(target);
            owner.TraitMechanicsBeforeDeath(target);
        }
        public void CancelProjectiles(BattleUnitState target)
        {
            owner.CancelProjectileWindups(target);
            target.ProjectileSequence = null;
        }
        public void CancelActionsAndGrants(BattleUnitState target)
        {
            owner._enemyRuntime.ObserveDefeated(target.RuntimeId);
            owner.RemoveDefeatedTechniques(target);
            owner.CancelEnemyAction(target.RuntimeId);
            owner.RemoveEnemyProducts(target.RuntimeId);
            owner.CancelChargedLine(target);
            owner.CancelTrample(target);
            owner.CancelDisplacement(target.RuntimeId);
            owner.WriteCounter(target, "_dead_since", Tick);
            owner.RevokePassiveGrants(target.RuntimeId);
        }
        public void ReleaseMovement(string id) => owner._movement?.ReleaseUnit(id);
        public void ReleaseBindings(string id) => owner.ReleaseBattleLifecycleBindings(id);
        public bool SpawnNear(UnitSnapshot? profile, int team, Vector2 position, float health, float damage, string source) => owner.SpawnTemporaryNear(profile, team, position, health, damage, source);
        public void Publish(BattleCombatEventDraft fact) => owner.PublishCombat(fact);
        public void Present(string type, string source, string target, float value, Vector2 position, string cue, BattleVfxCue? vfx = null) =>
            owner.Emit(type, source, target, value, position, cue, vfx: vfx);
    }
}
