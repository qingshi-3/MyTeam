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

// Only the authoritative vitality resolver receives mutable bodies; event consumers use BattleUnitView.
internal interface IBattleVitalityWorld
{
    int Tick { get; }
    IReadOnlyList<BattleUnitState> Units { get; }
    float ModifyIncomingDamage(BattleUnitState target, float amount);
    float Armor(BattleUnitState target);
    CombatSourceRef Source(string runtimeId, BattleUnitState? explicitUnit = null);
    CombatDamageClass DamageClass(CombatSourceRef source);
    void ShieldConsumed(BattleUnitState target, float amount, bool enemyDamage, CombatSourceRef origin);
    void HealthChanged(BattleUnitState target);
    void BeforeDeath(BattleUnitState target);
    void CancelProjectiles(BattleUnitState target);
    void CancelActionsAndGrants(BattleUnitState target);
    void ReleaseMovement(string id);
    void ReleaseBindings(string id);
    bool SpawnNear(UnitSnapshot? profile, int team, Vector2 position, float health, float damage, string source);
    void Publish(BattleCombatEventDraft fact);
    void Present(string type, string source, string target, float value, Vector2 position, string cue, BattleVfxCue? vfx = null);
}

internal sealed class BattleVitalityScope(IBattleVitalityWorld world, BattleConfig config,
    BattleCombatEventPipeline pipeline, BattleStatistics statistics, BattleStatusScope statuses)
{
    private ImmutableHashSet<string> _processedDeaths = ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal);
    internal sealed class Checkpoint
    {
        private readonly BattleVitalityScope _owner;
        private readonly ImmutableHashSet<string> _deaths;
        internal Checkpoint(BattleVitalityScope owner) { _owner = owner; _deaths = owner._processedDeaths; }
        internal void Restore(BattleVitalityScope owner)
        {
            if (!ReferenceEquals(owner, _owner)) throw new InvalidOperationException("Checkpoint belongs to another vitality scope.");
            owner._processedDeaths = _deaths;
        }
    }
    public Checkpoint Capture() => new(this);
    public void Restore(Checkpoint checkpoint) => checkpoint.Restore(this);
    public void Revive(string id) => _processedDeaths = _processedDeaths.Remove(id);
    private IEnumerable<BattleUnitState> Allies(int team) => world.Units.Where(unit => unit.Team == team && unit.Alive);
    private static CombatCell ToCombatCell(Vector2I cell) => new(cell.X, cell.Y);
    private static CombatPoint ToCombatPoint(Vector2 position) => new(position.X, position.Y);
    private static void ClearActionTarget(BattleUnitState unit)
    {
        unit.ActionTargetRuntimeId = string.Empty;
        unit.ActionTargetName = string.Empty;
    }
    public float Damage(string sourceRuntimeId, BattleUnitState? source, BattleUnitState target, float raw,
        CombatSourceRef origin = default, EffectDamageType damageType = EffectDamageType.Normal,
        CombatDamageClass damageClass = CombatDamageClass.Other, string actionId = "",
        BattleDamageStatisticsAttribution statisticsAttribution = default,
        bool isAreaDamage = false, CombatPoint? attackIncomingDirection = null)
    {
        if (!target.Alive) return 0;
        using var resolution = pipeline.BeginAuthoritativeResolution();
        var healthBefore = target.Health;
        var wasAlive = target.Alive;
        if (target.Team == 0 && target.Definition.IsHero && config.HeroRule.EmptySlotHeroDefense > 0)
            raw *= Math.Max(.25f, 1f - config.EmptyDeploymentSlots * config.HeroRule.EmptySlotHeroDefense);
        raw = world.ModifyIncomingDamage(target, raw);
        if (source is not null && source.Definition.Behavior.ExecuteHealthThreshold > 0 && target.Health / target.MaxHealth <= source.Definition.Behavior.ExecuteHealthThreshold)
            raw *= 1.5f;
        var combatSource = origin.IsSpecified ? origin : world.Source(sourceRuntimeId, source);
        if (damageClass == CombatDamageClass.Other) damageClass = world.DamageClass(combatSource);
        if (string.IsNullOrEmpty(actionId) && combatSource.Kind == CombatSourceKind.Ability) actionId = combatSource.InstanceId;
        var creditedKiller = source ?? world.Units.FirstOrDefault(unit => unit.RuntimeId == sourceRuntimeId);
        var sourceHealthRatio = creditedKiller is { MaxHealth: > 0 } ? creditedKiller.Health / creditedKiller.MaxHealth : -1;
        var calculated = pipeline.Resolve(new BattleCombatCalculationRequest(
            BattleCombatCalculationKind.Damage,
            combatSource,
            sourceRuntimeId,
            target.RuntimeId,
            world.Tick,
            Math.Max(0, raw), damageType, damageClass, actionId, isAreaDamage, attackIncomingDirection));
        raw = calculated.ResolvedAmount;
        var resistance = damageType switch
        {
            EffectDamageType.Normal => world.Armor(target),
            EffectDamageType.True => 0,
            _ => throw new InvalidOperationException("Unsupported damage type.")
        };
        // True damage bypasses resistance, while shared incoming/outgoing modifiers and shields still apply.
        var damage = raw <= 0 ? 0 : damageType == EffectDamageType.True ? raw :
            Math.Max(1f, raw * 100f / (100f + resistance * 7f));
        var resolvedDamage = damage;
        var absorbed = Math.Min(target.Shield, damage);
        target.Shield -= absorbed;
        world.ShieldConsumed(target, absorbed, creditedKiller is not null && creditedKiller.Team != target.Team, combatSource);
        if (absorbed > 0)
            world.Present("vfx", sourceRuntimeId, target.RuntimeId, absorbed, target.Position, "",
                vfx: new BattleVfxCue(target.Shield > 0 ? BattleVfxPhase.ShieldImpact : BattleVfxPhase.ShieldDepleted, "shield"));
        damage -= absorbed;
        target.Health = Math.Max(0, target.Health - damage);
        world.HealthChanged(target);
        var healthRemoved = Math.Min(healthBefore, damage);
        var effectiveDamage = absorbed + healthRemoved;
        BattleHeroMana.OnDamage(target, effectiveDamage, world.Tick);
        statistics.Damage(sourceRuntimeId, target.RuntimeId, effectiveDamage, absorbed, wasAlive && !target.Alive,
            healthRemoved, Math.Max(0, resolvedDamage - absorbed - healthRemoved),
            statisticsAttribution.IsSpecified ? statisticsAttribution.Origin : combatSource,
            statisticsAttribution.IsSpecified ? statisticsAttribution.DamageClass : damageClass, damageType);
        world.Publish(new BattleCombatEventDraft(
            BattleCombatEventKind.DamageResolved,
            combatSource,
            sourceRuntimeId,
            target.RuntimeId,
            world.Tick,
            RequestedValue: calculated.RequestedAmount,
            AppliedValue: resolvedDamage,
            EffectiveValue: effectiveDamage,
            Cell: ToCombatCell(target.Cell),
            Position: ToCombatPoint(target.Position),
            DamageType: damageType, DamageClass: damageClass, ActionId: actionId,
            IsAreaDamage: isAreaDamage, AttackIncomingDirection: attackIncomingDirection));
        if (healthRemoved > 0)
            world.Publish(new BattleCombatEventDraft(BattleCombatEventKind.HealthLost,combatSource,sourceRuntimeId,target.RuntimeId,
                world.Tick,EffectiveValue:healthRemoved,DamageType:damageType,DamageClass:damageClass,ActionId:actionId,
                SourceHealthRatio:sourceHealthRatio,TargetHealthRatio:target.Health / target.MaxHealth,
                IsAreaDamage:isAreaDamage,AttackIncomingDirection:attackIncomingDirection));
        if (!target.Alive)
        {
            world.BeforeDeath(target);
            statistics.Defeat(target.RuntimeId, world.Tick);
            target.Mode = BattleUnitMode.Defeated;
            target.LastActionKind = BattleActionKind.None;
            ClearActionTarget(target);
            target.WaitingTicks = 0;
            world.Publish(new BattleCombatEventDraft(
                BattleCombatEventKind.UnitDefeated,
                combatSource,
                sourceRuntimeId,
                target.RuntimeId,
                world.Tick,
                AppliedValue: resolvedDamage,
                EffectiveValue: effectiveDamage,
                Cell: ToCombatCell(target.Cell),
                Position: ToCombatPoint(target.Position),
                DamageType: damageType, DamageClass: damageClass, ActionId: actionId,
            IsAreaDamage: isAreaDamage, AttackIncomingDirection: attackIncomingDirection));
            if (creditedKiller is not null)
                world.Publish(new BattleCombatEventDraft(
                    BattleCombatEventKind.UnitKilled,
                    combatSource,
                    sourceRuntimeId,
                    target.RuntimeId,
                    world.Tick,
                    AppliedValue: resolvedDamage,
                    EffectiveValue: effectiveDamage,
                    Cell: ToCombatCell(target.Cell),
                    Position: ToCombatPoint(target.Position),
                    DamageType: damageType, DamageClass: damageClass, ActionId: actionId,
            IsAreaDamage: isAreaDamage, AttackIncomingDirection: attackIncomingDirection));
            world.CancelProjectiles(target);
            world.Present("defeated", sourceRuntimeId, target.RuntimeId, damage, target.Position, "defeated");
            HandleDeath(source, target);
        }
        else world.Present("damage", sourceRuntimeId, target.RuntimeId, damage, target.Position, "hit");
        resolution.Commit();
        return damage;
    }

    public float Heal(string sourceRuntimeId, BattleUnitState target, float amount, CombatSourceRef origin = default)
    {
        if (!target.Alive || amount <= 0) return 0;
        using var resolution = pipeline.BeginAuthoritativeResolution();
        var combatSource = origin.IsSpecified ? origin : world.Source(sourceRuntimeId);
        var calculated = pipeline.Resolve(new BattleCombatCalculationRequest(
            BattleCombatCalculationKind.Healing,
            combatSource,
            sourceRuntimeId,
            target.RuntimeId,
            world.Tick,
            amount));
        var before = target.Health;
        var beforeRatio = target.MaxHealth > 0 ? before / target.MaxHealth : 0;
        target.Health = Math.Min(target.MaxHealth, target.Health + calculated.ResolvedAmount);
        world.HealthChanged(target);
        var effectiveHealing = target.Health - before;
        statistics.Heal(sourceRuntimeId, target.RuntimeId, effectiveHealing, calculated.ResolvedAmount, combatSource);
        world.Publish(new BattleCombatEventDraft(
            BattleCombatEventKind.HealingResolved,
            combatSource,
            sourceRuntimeId,
            target.RuntimeId,
            world.Tick,
            RequestedValue: calculated.RequestedAmount,
            AppliedValue: calculated.ResolvedAmount,
            EffectiveValue: effectiveHealing,
            Cell: ToCombatCell(target.Cell),
            Position: ToCombatPoint(target.Position),
            DamageClass: world.DamageClass(combatSource), ActionId: combatSource.InstanceId,
            TargetHealthRatio: beforeRatio));
        resolution.Commit();
        return effectiveHealing;
    }

    public float Shield(string sourceRuntimeId, BattleUnitState target, float amount, CombatSourceRef origin = default)
    {
        if (!target.Alive || amount <= 0) return 0;
        using var resolution = pipeline.BeginAuthoritativeResolution();
        var combatSource = origin.IsSpecified ? origin : world.Source(sourceRuntimeId);
        var calculated = pipeline.Resolve(new BattleCombatCalculationRequest(
            BattleCombatCalculationKind.Shield,
            combatSource,
            sourceRuntimeId,
            target.RuntimeId,
            world.Tick,
            amount));
        target.Shield += calculated.ResolvedAmount;
        statistics.Shield(sourceRuntimeId, calculated.ResolvedAmount);
        if (calculated.ResolvedAmount > 0)
            world.Present("vfx", sourceRuntimeId, target.RuntimeId, calculated.ResolvedAmount, target.Position, "",
                vfx: new BattleVfxCue(BattleVfxPhase.ShieldActive, "shield"));
        world.Publish(new BattleCombatEventDraft(
            BattleCombatEventKind.ShieldResolved,
            combatSource,
            sourceRuntimeId,
            target.RuntimeId,
            world.Tick,
            RequestedValue: calculated.RequestedAmount,
            AppliedValue: calculated.ResolvedAmount,
            EffectiveValue: calculated.ResolvedAmount,
            Cell: ToCombatCell(target.Cell),
            Position: ToCombatPoint(target.Position)));
        resolution.Commit();
        return calculated.ResolvedAmount;
    }

    public void HandleDeath(BattleUnitState? source, BattleUnitState target)
    {
        if (_processedDeaths.Contains(target.RuntimeId)) return;
        _processedDeaths = _processedDeaths.Add(target.RuntimeId);
        world.CancelActionsAndGrants(target);
        foreach (var unit in world.Units.Where(unit => unit.ActionTargetRuntimeId == target.RuntimeId))
            ClearActionTarget(unit);
        world.ReleaseMovement(target.RuntimeId);
        statuses.HandleOwnerDeath(target.RuntimeId);
        if (target.Definition.Behavior.OnDeathDamage > 0)
            foreach (var enemy in Allies(1 - target.Team).Where(enemy => BattlefieldSpace.EdgeDistance(enemy, target) <= 1.5f).ToArray())
                Damage(target.RuntimeId, null, enemy, target.Definition.Behavior.OnDeathDamage);
        if (target.Team == 0 && config.HeroRule.SummonOnAllyDeath && !target.Definition.IsHero && !target.IsTemporary)
            world.SpawnNear(config.Summons.DeathSummon, 0, target.Position, .6f, .65f, target.RuntimeId);
        if (source is { Team: 0 } && config.HeroRule.KillGrowth > 0 &&
            (string.IsNullOrWhiteSpace(config.HeroRule.RequiredSoldierTag) || source.Definition.Tags.Contains(config.HeroRule.RequiredSoldierTag)))
            foreach (var ally in Allies(0).Where(ally => string.IsNullOrWhiteSpace(config.HeroRule.RequiredSoldierTag) || ally.Definition.Tags.Contains(config.HeroRule.RequiredSoldierTag)))
                ally.Attributes.ApplyModifier(
                    new CompiledAttributeModifier(
                        CombatAttribute.AttackDamage,
                        AttributeModifierOperation.Multiply,
                        new CompiledConstantMagnitude(1f + config.HeroRule.KillGrowth),
                        0,
                        $"kill_{target.RuntimeId}"),
                    new CombatSourceRef(
                        CombatSourceKind.System,
                        "legacy_kill_growth",
                        source.RuntimeId,
                        $"{source.RuntimeId}:{target.RuntimeId}"));
        world.ReleaseBindings(target.RuntimeId);
    }

}
