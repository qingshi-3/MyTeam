using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Battle;

public sealed partial class BattleSimulation
{
    private readonly BattleTraitBindings _traitBindings;
    private readonly BattleTraitReactionScope _traitReactions;
    private readonly BattleElementStatusScope _elementStatuses;
    private readonly BattleSupportEffectScope _supportEffects;

    private void TraitMechanicsInitialize()
    {
        _traitBindings.Initialize(CurrentTraitSnapshot, UnitViews);
        if (!_traitBindings.NeedsEvents) return;
        var origin = CombatSourceRef.System("trait_mechanics");
        foreach (var kind in new[] { BattleCombatEventKind.BattleStarted, BattleCombatEventKind.AttackLanded,
                     BattleCombatEventKind.DamageResolved, BattleCombatEventKind.HealthLost,
                     BattleCombatEventKind.UnitKilled, BattleCombatEventKind.UnitDefeated,
                     BattleCombatEventKind.HealingResolved, BattleCombatEventKind.ShieldResolved,
                     BattleCombatEventKind.ManaSkillResolved, BattleCombatEventKind.UnitTransformed })
            _combatPipeline.Subscribe(kind, origin, 30, (e, sink) =>
            {
                if (!_traitReactions.IsInterested(e)) return;
                sink.Enqueue(origin, 30, _ => _traitReactions.Observe(e));
            });
        if (_traitBindings.Entries.IsEmpty) return;
        foreach (var kind in new[] { BattleCombatCalculationKind.Damage, BattleCombatCalculationKind.Healing,
                     BattleCombatCalculationKind.Shield })
            _combatPipeline.SubscribeCalculation(kind, origin, 30, _traitReactions.Modify);
    }

    private void DrainTraitMechanics() => _traitReactions.Drain();
    private void TraitMechanicsBeginAbility(string ownerId, string actionId, bool derived) => _traitReactions.BeginAbility(ownerId, actionId, derived);
    private void TraitMechanicsBeforeDeath(BattleUnitState target) => _traitReactions.BeforeDeath(target.RuntimeId);
    private void TraitMechanicsOnSummoned(BattleUnitState unit) => _traitReactions.OnSummoned(unit.RuntimeId);
    private bool TraitMechanicsExtraSummon(string ownerId) => _traitReactions.ExtraSummon(ownerId);
    private bool MatrixExcludedDeath(string runtimeId) => _traitReactions.ExcludedDeath(runtimeId);
    private void TraitMechanicsAdvance() => _traitReactions.Advance();
    private void TraitMechanicsComplete()
    {
        _traitReactions.Clear();
        _elementStatuses.Clear();
        _supportEffects.Clear();
    }
    private float MatrixApplyTimedShield(string sourceId, string targetId, float amount, int durationTicks, CombatSourceRef origin = default) =>
        _supportEffects.ApplyShield(sourceId, targetId, amount, durationTicks, origin);
    private void TraitMechanicsShieldConsumed(BattleUnitState target, float amount, bool enemyDamage, CombatSourceRef origin) =>
        _supportEffects.ShieldConsumed(BattleUnitView.For(target), amount, enemyDamage, origin);
    private void MatrixGrantMana(string sourceId, string targetId, float amount, bool deferWhileLocked = false) =>
        _supportEffects.GrantMana(sourceId, targetId, amount, deferWhileLocked);
    private int MatrixReadPoison(string targetId, int? sourceTeam = null) => _elementStatuses.ReadPoison(targetId, sourceTeam);
    private void MatrixApplyPoison(string sourceId, string targetId, int layers) => _elementStatuses.ApplyPoison(sourceId, targetId, layers);
    private int MatrixConsumePoison(string targetId, int layers) => _elementStatuses.ConsumePoison(targetId, layers);
    private ImmutableArray<MatrixPoisonTransferReceipt> MatrixTransferPoison(string fromId, string toId, int layers, int? sourceTeam = null) =>
        _elementStatuses.TransferPoison(fromId, toId, layers, sourceTeam);
    private bool MatrixApplyChill(string sourceId, string targetId, int layers) => _elementStatuses.ApplyChill(sourceId, targetId, layers);
    private int MatrixReadChill(string targetId) => _elementStatuses.ReadChill(targetId);
    private int MatrixReadEmber(string targetId) => _elementStatuses.ReadEmber(targetId);
    private void MatrixApplyEmber(string sourceId, string targetId, int layers) => _elementStatuses.ApplyEmber(sourceId, targetId, layers);
    private int MatrixConsumeEmber(string targetId, int layers) => _elementStatuses.ConsumeEmber(targetId, layers);

    private sealed class TraitWorld(BattleSimulation owner) : IBattleTraitQueries, IBattleTraitCommands, IBattleTraitTransactions
    {
        private BattleUnitState Unit(string id) => owner._units.First(unit => unit.RuntimeId == id);
        public int Tick => owner.TickIndex;
        public IReadOnlyList<BattleUnitView> Units => owner.UnitViews;
        public IReadOnlyDictionary<string, UnitSnapshot> SummonProfiles => owner._config.TacticalSummons;
        public bool ManaLocked(string id)
        {
            var unit = Unit(id);
            return Tick < unit.ManaLockedUntilTick || unit.Trample is not null || unit.ProjectileSequence is not null ||
                unit.ProjectileWindups.Any(projectile => projectile.SkillOrigin.IsSpecified);
        }
        public void Damage(string source, string target, float amount, CombatSourceRef origin,
            EffectDamageType damageType = EffectDamageType.Normal, CombatDamageClass damageClass = CombatDamageClass.Other,
            bool isAreaDamage = false) =>
            owner.ApplyDamage(source, owner._units.FirstOrDefault(unit => unit.RuntimeId == source), Unit(target), amount, origin, damageType, damageClass, isAreaDamage: isAreaDamage);
        public void Heal(string source, string target, float amount, CombatSourceRef origin) => owner.HealLiving(source, Unit(target), amount, origin);
        public float Shield(string source, string target, float amount, CombatSourceRef origin) => owner.ApplyShield(source, Unit(target), amount, origin);
        public float NextRandom() => owner._random.NextFloat();
        public void AddBaseHealth(string target, float ratio, CombatSourceRef origin)
        {
            var unit = Unit(target);
            var previous = unit.MaxHealth;
            AddAttribute(target, CombatAttribute.MaxHealth, unit.Attributes.GetBaseValue(CombatAttribute.MaxHealth) * ratio, origin);
            unit.Health += unit.MaxHealth - previous;
        }
        public void AddAttribute(string target, CombatAttribute attribute, float amount, CombatSourceRef origin) =>
            Unit(target).Attributes.ApplyModifier(new(attribute, AttributeModifierOperation.Add,
                new CompiledConstantMagnitude(amount), 0, origin.StableId), origin);
        public void AdditionalAttack(string source, string target, float amount, string actionId,
            BattleDamageStatisticsAttribution statisticsAttribution = default) =>
            owner.ResolveAdditionalAttack(Unit(source), Unit(target), amount, actionId, statisticsAttribution);
        public void Present(string source, string target, string cue) =>
            owner.Emit("ability", source, target, 0, Unit(target).Position, cue);
        public void GainMana(string target, float amount) => BattleHeroMana.Gain(Unit(target), amount, Tick);
        public void RemoveShield(string target, float amount) { var unit = Unit(target); unit.Shield = Math.Max(0, unit.Shield - amount); }
        public void BuffSummonHealth(string target, float amount, CombatSourceRef origin)
        {
            var unit = Unit(target);
            var oldMax = unit.MaxHealth;
            unit.Attributes.ApplyModifier(new(CombatAttribute.MaxHealth, AttributeModifierOperation.Multiply,
                new CompiledConstantMagnitude(1 + amount), 0, "summon_health"), origin);
            unit.Health += unit.MaxHealth - oldMax;
        }
        public string? SpawnNear(UnitSnapshot profile, int team, Vector2 position, float health, float damage, string source)
        {
            var sequence = owner._summonCounter;
            return owner.SpawnTemporaryNear(profile, team, position, health, damage, source) ? $"s-{team}-{sequence}" : null;
        }
        public void Retire(string target, string source, string reason) => owner.RetireEnemyProduct(Unit(target), source, reason);
        public void Publish(BattleCombatEventDraft fact) => owner.PublishCombat(fact);
        public void Resolve(Action work)
        {
            var checkpoint = new BattleWorldStateCheckpoint(owner);
            try
            {
                using var resolution = owner._combatPipeline.BeginAuthoritativeResolution();
                work();
                resolution.Commit();
                checkpoint.Commit();
            }
            catch { checkpoint.Rollback(); throw; }
        }
        public void Continue(string chain, int depth, Action work)
        {
            using var causal = owner._combatPipeline.ContinueReaction(chain, depth);
            work();
        }
    }
}
