using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private readonly BattleEnemyActionScope _enemyRuntime;
    private bool EnemyActionBusy(BattleUnitState unit) => _enemyRuntime.IsBusy(unit.RuntimeId);
    private bool IsRampartBody(BattleUnitState unit) => _enemyRuntime.IsRampart(unit.RuntimeId);
    private string SelectEnemyActionTarget(BattleUnitState owner, CompiledEnemyAction operation) => _enemyRuntime.SelectTarget(owner.RuntimeId, operation);
    private bool PrepareEnemyAction(BattleUnitState owner, CompiledEnemyAction operation, string targetId) => _enemyRuntime.Prepare(owner.RuntimeId, operation, targetId);
    private void BeginEnemyAction(BattleUnitState owner, BattleUnitState target, CompiledEnemyAction operation, CompiledAbilityDefinition ability) =>
        _enemyRuntime.Begin(owner.RuntimeId, target.RuntimeId, operation, ability.DisplayName, AbilityOrigin(ability, owner.RuntimeId));
    private void AdvanceEnemyActions() => _enemyRuntime.Advance();
    private void AdvanceReturningBlades(IReadOnlyDictionary<string, Vector2> previous) => _enemyRuntime.AdvanceBlades(previous);
    private void CancelEnemyAction(string id) => _enemyRuntime.Cancel(id);
    private void RemoveEnemyProducts(string id, bool wallsOnly = false) => _enemyRuntime.RemoveProducts(id, wallsOnly);

    private void RegisterEnemyDamageCalculations() => _abilityCombatSubscriptions.Add(
        _combatPipeline.SubscribeCalculation(BattleCombatCalculationKind.Damage,
            CombatSourceRef.System("enemy_seat_guard"), 40, _enemyRuntime.ModifyIncomingDamage));

    // Integration only: state and enemy rules live in BattleEnemyActionScope.
    private sealed class EnemyWorld(BattleSimulation battle) : IBattleEnemyQueries, IBattleEnemyCommands, IBattleEnemyTransactions
    {
        private BattleUnitState Unit(string id) => battle._units.First(unit => unit.RuntimeId == id);
        public int Tick => battle.TickIndex;
        public IReadOnlyList<BattleUnitView> Units => battle.UnitViews;
        public IReadOnlyDictionary<string, UnitSnapshot> Profiles => battle._config.TacticalSummons;
        public IReadOnlyList<DisplacementReservation> Reservations => battle.CaptureDisplacementReservations();
        public bool CanOccupyTerrain(Vector2I cell) => battle._config.FloorRule.CanOccupy(cell);
        public bool Controlled(string id) => Unit(id).DisabledTicks > 0 || battle._statusScope.HasTag(id, StatusDefinitionCompiler.ActionDisabledTag);
        public bool Displacing(string id) => battle.IsDisplacing(Unit(id));
        public bool Airborne(string id) => battle.IsAirborne(Unit(id));
        public bool HasAttackInFlight(string id) => Unit(id).ProjectileSequence is not null || !Unit(id).ProjectileWindups.IsEmpty;
        public CompiledBroodPhase? Brood(string id) => battle._abilityScope?.FindOperation<CompiledBroodPhase>(id);
        public bool IsInitialNative(string id) => battle._config.Spawns.Any(spawn =>
            spawn.InstanceId == id && !spawn.IsTemporary);
        public bool HasLineAccess(Vector2 from, Vector2 to) => battle.HasLineAccess(from, to);
        public bool CanOccupy(Vector2 position, float radius) => battle.CanOccupyPosition(position, radius);
        public void BeginCast(string ownerId, string targetId, string displayName)
        {
            var owner = Unit(ownerId);
            owner.LastAbilityName = displayName;
            owner.LastActionKind = BattleActionKind.Ability;
            owner.Mode = BattleUnitMode.Casting;
            SetActionTarget(owner, Unit(targetId));
            battle._movement?.ReleaseUnit(ownerId);
        }
        public void ContinueCast(string id) => Unit(id).Mode = BattleUnitMode.Casting;
        public void Hit(string source, string target, float damage, CombatSourceRef origin, EffectDamageType type, bool isAreaDamage = false) =>
            battle.ResolveLineSkillHit(Unit(source), Unit(target), damage, origin, type, isAreaDamage);
        public void PeriodicHit(string source, string target, float damage, CombatSourceRef origin) =>
            battle.ApplyDamage(source, Unit(source), Unit(target), damage, origin,
                EffectDamageType.Normal, CombatDamageClass.Periodic, isAreaDamage: true);
        private static string GuardGrant(string source, string target) => $"enemy_seat_guard:{source}:{target}";
        public void SeatGuardMarker(string source, string target, bool active)
        {
            var grant = GuardGrant(source, target);
            if (!active) { battle._statusScope.RevokeGrant(grant); return; }
            var status = BattleMechanicStatus.Create("enemy_seat_guard", "岩座守护", 1) with
            {
                DurationKind = StatusDurationKind.Permanent, DurationTicks = 0,
                AggregationPolicy = StatusAggregationPolicy.BySource,
                DispelCategory = StatusDispelCategory.NonDispellable,
                Description = "受到伤害降低20%；岩座守护者死亡时失效。"
            };
            battle._statusScope.ReplaceGrants([], [new(status, source, target, battle.TickIndex, grant)]);
        }
        public void CompletionArmor(string id, float amount, int ticks)
        {
            var status = BattleMechanicStatus.Create("enemy_stone_armor", "岩震余势", ticks,
                modifiers: [new(CombatAttribute.Armor, AttributeModifierOperation.Add, new CompiledConstantMagnitude(amount), 0, "stone_armor")]) with
            {
                AggregationPolicy = StatusAggregationPolicy.BySource,
                OverflowPolicy = StatusOverflowPolicy.RefreshDuration,
                DurationRefreshPolicy = StatusDurationRefreshPolicy.Reset,
                Description = $"完整岩震后防御增加{amount:0.#}。"
            };
            battle._statusScope.Apply(status, id, id, battle.TickIndex);
        }
        public string? Spawn(UnitSnapshot profile, int team, Vector2 position, string ownerId)
        {
            var id = $"s-{team}-{battle._summonCounter}";
            return battle.SpawnTemporary(profile, team, position, 1, 1, ownerId) ? id : null;
        }
        public void Swap(string firstId, string secondId) => battle.SwapBodies(Unit(firstId), Unit(secondId));
        public void InterruptForForm(string id)
        {
            var owner = Unit(id);
            battle.CancelEnemyAction(id); battle.CancelChargedLine(owner); battle.CancelTrample(owner);
            battle.CancelProjectileWindups(owner); owner.ProjectileSequence = null;
            battle._movement?.ReleaseUnit(id);
        }
        public void TransformBrood(string id, float radius, float reach)
        {
            var owner = Unit(id);
            owner.BodyRadiusOverride = radius;
            owner.AttackDeliveryOverride = AttackDelivery.Projectile;
            owner.ProjectileVisualId = "acid_spit";
            owner.Attributes.SetBaseValue(CombatAttribute.AttackRange, reach);
            owner.BossPhaseId = "exposed";
            battle._movement?.ReleaseUnit(id);
        }
        public void Retreat(string ownerId, string targetId, Vector2 start, Vector2 end, float distance)
        {
            battle._displacementRuntime.Retreat(Unit(ownerId), targetId, start, end, distance);
        }
        public void Retire(string unitId, string ownerId, string reason) => battle.RetireEnemyProduct(Unit(unitId), ownerId, reason);
        public void Present(BattleEvent fact) => battle.Emit(fact.Type, fact.SourceRuntimeId, fact.TargetRuntimeId, fact.Value,
            fact.Position, fact.Cue, entityId: fact.EntityId, origin: fact.Origin, attackTiming: fact.AttackTiming, enemyAction: fact.EnemyAction);
        public void Resolve(Action work)
        {
            var checkpoint = new BattleWorldStateCheckpoint(battle);
            try
            {
                using var resolution = battle._combatPipeline.BeginAuthoritativeResolution();
                work();
                resolution.Commit();
                checkpoint.Commit();
            }
            catch { checkpoint.Rollback(); throw; }
        }
        public void CheckpointOnly(Action work)
        {
            var checkpoint = new BattleWorldStateCheckpoint(battle);
            try { work(); checkpoint.Commit(); }
            catch { checkpoint.Rollback(); throw; }
        }
        public void ResolveHit(Action work)
        {
            using var resolution = battle._combatPipeline.BeginAuthoritativeResolution();
            work();
            resolution.Commit();
        }
    }

    private void SwapBodies(BattleUnitState first, BattleUnitState second)
    {
        var a = first.Position; var b = second.Position;
        CancelEnemyAction(second.RuntimeId); CancelChargedLine(second); CancelTrample(second);
        CancelProjectileWindups(second); second.ProjectileSequence = null;
        InterruptBattleChannels(second.RuntimeId);
        _movement?.ReleaseUnit(first.RuntimeId); _movement?.ReleaseUnit(second.RuntimeId);
        first.Position = b; second.Position = a;
        ClearActionTarget(first); ClearActionTarget(second);
        foreach (var (unit,start,end) in new[] { (first,a,b), (second,b,a) })
            Emit("displacement",first.RuntimeId,unit.RuntimeId,0,end,"",origin:start,
                displacement:new(DisplacementKind.Blink,start,end,TickIndex,1,1,true,false,0,start,0));
        Emit("swap_complete",first.RuntimeId,second.RuntimeId,0,b,"",origin:a);
    }

    private void RetireEnemyProduct(BattleUnitState unit, string ownerId, string reason)
    {
        // Expiry/hatching is removal, not combat damage or a credited kill.
        unit.Health = 0; unit.Mode = BattleUnitMode.Defeated;
        _statistics.Defeat(unit.RuntimeId, TickIndex);
        _lifecycleRuntime.SpendCorpse(unit.RuntimeId);
        _movement?.ReleaseUnit(unit.RuntimeId);
        HandleDeath(null,unit);
        Emit(reason,ownerId,unit.RuntimeId,0,unit.Position,"defeated");
    }
}
