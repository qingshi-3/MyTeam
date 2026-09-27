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

public sealed record MatrixPoisonTransferReceipt(string SourceId, string FromId, string ToId, int Layers);

public sealed partial class BattleSimulation
{
    private sealed record TraitBinding(string Id, int Team, CompiledTraitMechanic Spec, ImmutableHashSet<string> Members);
    private sealed record TraitPoison(long Sequence, string Source, int SourceTeam, string Target, int Expires, int NextTick,
        float Damage, string StatusInstance);
    private sealed record TraitChill(string Source, int Stacks, int Expires, string StatusInstance);
    private sealed record TraitEmber(string Source, int Stacks, int Expires, string StatusInstance = "");
    private sealed record TraitShield(string Source, string Target, int Expires, float Remaining);
    private sealed record TraitBrokenShield(string Target, float Absorbed, CombatSourceRef Origin,
        string ChainId = "", int Depth = 0);
    private sealed record TraitMana(string Source, string Target, float Amount);
    private sealed record TraitProduct(string Id, int Expires);
    private sealed record TraitMechanicsState(
        ImmutableDictionary<string, float> Counters,
        ImmutableDictionary<string, string> Targets,
        ImmutableArray<TraitPoison> Poison,
        ImmutableDictionary<string, TraitChill> Chill,
        ImmutableDictionary<string, TraitEmber> Ember,
        ImmutableArray<TraitShield> Shields,
        ImmutableArray<BattleCombatEvent> Events,
        ImmutableArray<TraitBrokenShield> Broken,
        ImmutableArray<TraitMana> Mana,
        ImmutableArray<TraitProduct> Products,
        ImmutableHashSet<string> ExcludedDeaths,
        long Sequence)
    {
        public static TraitMechanicsState Empty => new(ImmutableDictionary<string, float>.Empty,
            ImmutableDictionary<string, string>.Empty, [], ImmutableDictionary<string, TraitChill>.Empty,
            ImmutableDictionary<string, TraitEmber>.Empty, [], [], [], [], [], ImmutableHashSet<string>.Empty, 0);
    }
    private TraitMechanicsState _traitMechanics = TraitMechanicsState.Empty;
    private ImmutableArray<TraitBinding> _traitMechanicBindings = [];
    private bool _drainingTraitMechanics;
    private bool _traitObserveBreaks;
    private BattleUnitState? TraitUnit(string id) => _units.FirstOrDefault(u => u.RuntimeId == id);
    private float TraitCounter(string key) => _traitMechanics.Counters.GetValueOrDefault(key);
    private void TraitWrite(string key, float value) => _traitMechanics = _traitMechanics with
    { Counters = _traitMechanics.Counters.SetItem(key, value) };
    private static string TraitKey(TraitBinding b, string owner, string suffix) => $"{b.Team}:{b.Id}:{b.Spec.Kind}:{owner}:{suffix}";
    private CombatSourceRef TraitOrigin(TraitBinding b, string owner) =>
        new(CombatSourceKind.Trait, b.Id, owner, $"{_combatPipeline.ScopeId}:{b.Team}:{b.Id}:{owner}");
    private IEnumerable<TraitBinding> TraitFor(string owner, TraitMechanicKind kind) =>
        _traitMechanicBindings.Where(b => b.Spec.Kind == kind && b.Members.Contains(owner));
    private IEnumerable<BattleUnitState> TraitLiving(TraitBinding b) => _units
        .Where(u => u.Alive && u.Team == b.Team && b.Members.Contains(u.RuntimeId))
        .OrderBy(u => u.RuntimeId, StringComparer.Ordinal);
    private IEnumerable<BattleUnitState> TraitNear(BattleUnitState center, int team, float range, bool includeSelf = false) =>
        _units.Where(u => u.Alive && u.Team == team && (includeSelf || u != center) &&
                          u.Position.DistanceTo(center.Position) <= range)
            .OrderBy(u => u.Position.DistanceSquaredTo(center.Position)).ThenBy(u => u.RuntimeId, StringComparer.Ordinal);
    private bool TraitReady(string key, int cooldown)
    {
        if (TraitCounter(key) > TickIndex) return false;
        TraitWrite(key, TickIndex + Math.Max(1, cooldown));
        return true;
    }

    private void TraitMechanicsInitialize()
    {
        _traitMechanicBindings = CurrentTraitSnapshot.Values.Where(v => v.ActiveBreakpoint is not null)
            .SelectMany(v => (v.ActiveBreakpoint!.Mechanics.IsDefault ? [] : v.ActiveBreakpoint.Mechanics)
                .Select(m => new TraitBinding(v.TraitId, v.Team, m, _units.Where(u => u.Team == v.Team &&
                    v.Contributions.Any(c => c.OwnerRuntimeId == u.RuntimeId ||
                        !string.IsNullOrEmpty(u.SourceInstanceId) && c.OwnerRuntimeId == u.SourceInstanceId))
                    .Select(u => u.RuntimeId).ToImmutableHashSet(StringComparer.Ordinal))))
            .OrderBy(b => b.Team).ThenBy(b => b.Id, StringComparer.Ordinal).ToImmutableArray();
        var hasMatrixUnits = _units.Any(u => u.Definition.AbilityLoadout is { } loadout &&
            loadout.Abilities.Any(a => a.Operations.Any(o => o is CompiledMatrixOperation)));
        _traitObserveBreaks = hasMatrixUnits || _traitMechanicBindings.Any(b => b.Spec.Kind == TraitMechanicKind.BarrierRetaliation);
        if (!hasMatrixUnits && _traitMechanicBindings.IsEmpty) return;
        var origin = CombatSourceRef.System("trait_mechanics");
        foreach (var kind in new[] { BattleCombatEventKind.BattleStarted, BattleCombatEventKind.AttackLanded,
                     BattleCombatEventKind.DamageResolved, BattleCombatEventKind.HealthLost,
                     BattleCombatEventKind.UnitKilled, BattleCombatEventKind.UnitDefeated,
                     BattleCombatEventKind.HealingResolved, BattleCombatEventKind.ShieldResolved,
                     BattleCombatEventKind.ManaSkillResolved })
            _combatPipeline.Subscribe(kind, origin, 30, (e, sink) =>
            {
                if (!TraitInterested(e)) return;
                sink.Enqueue(origin, 30, _ =>
                {
                if (e.Kind == BattleCombatEventKind.DamageResolved)
                {
                    var index = -1;
                    for (var i = 0; i < _traitMechanics.Broken.Length; i++)
                        if (_traitMechanics.Broken[i].Target == e.TargetRuntimeId && _traitMechanics.Broken[i].Origin == e.Source &&
                            _traitMechanics.Broken[i].ChainId.Length == 0) { index = i; break; }
                    if (index >= 0) _traitMechanics = _traitMechanics with { Broken = _traitMechanics.Broken.SetItem(index,
                        _traitMechanics.Broken[index] with { ChainId = e.ChainId, Depth = e.Depth + 1 }) };
                }
                _traitMechanics = _traitMechanics with { Events = _traitMechanics.Events.Add(e) };
                });
            });
        if (_traitMechanicBindings.IsEmpty) return;
        foreach (var kind in new[] { BattleCombatCalculationKind.Damage, BattleCombatCalculationKind.Healing,
                     BattleCombatCalculationKind.Shield })
            _combatPipeline.SubscribeCalculation(kind, origin, 30, TraitMechanicsModify);
    }

    private bool TraitInterested(BattleCombatEvent e)
    {
        if (e.Kind == BattleCombatEventKind.DamageResolved && _traitMechanics.Broken.Any(b =>
                b.Target == e.TargetRuntimeId && b.Origin == e.Source && b.ChainId.Length == 0)) return true;
        foreach (var b in _traitMechanicBindings)
        {
            var source = b.Members.Contains(e.SourceRuntimeId);
            var target = b.Members.Contains(e.TargetRuntimeId);
            var kind = b.Spec.Kind;
            if (e.Kind == BattleCombatEventKind.BattleStarted && kind == TraitMechanicKind.BarrierRetaliation) return true;
            if (e.Kind == BattleCombatEventKind.AttackLanded && source &&
                (kind is TraitMechanicKind.RepeatedTargetAttack or TraitMechanicKind.ChillOnAttack or TraitMechanicKind.PoisonOnAttack ||
                 kind == TraitMechanicKind.DamageTakenRamp && b.Spec.Secondary > 0)) return true;
            if (e.Kind == BattleCombatEventKind.DamageResolved && source && e.DamageClass == CombatDamageClass.ActiveSkill &&
                kind == TraitMechanicKind.ActiveHitMarks) return true;
            if (e.Kind == BattleCombatEventKind.HealthLost &&
                (target && kind is TraitMechanicKind.HealthThresholdRescue or TraitMechanicKind.DamageTakenRamp or TraitMechanicKind.LowHealthLeech ||
                 source && kind == TraitMechanicKind.LowHealthLeech)) return true;
            if (e.Kind == BattleCombatEventKind.UnitKilled && source && kind == TraitMechanicKind.WoundedTargetReward && b.Spec.Secondary > 0) return true;
            if (e.Kind is BattleCombatEventKind.HealingResolved or BattleCombatEventKind.ShieldResolved && source &&
                kind == TraitMechanicKind.SupportProtection && b.Spec.Secondary > 0 && e.EffectiveValue > 0) return true;
            if (e.Kind == BattleCombatEventKind.ManaSkillResolved && source &&
                (kind == TraitMechanicKind.SharedCastResource || kind == TraitMechanicKind.ActiveDamageAndRefund && b.Spec.Secondary > 0)) return true;
            if (e.Kind == BattleCombatEventKind.UnitDefeated && kind is TraitMechanicKind.DeathResource or TraitMechanicKind.OwnedSummonBonus &&
                TraitUnit(e.TargetRuntimeId) is { } dead && dead.Team == b.Team &&
                (target || b.Members.Contains(dead.SummonerRuntimeId))) return true;
        }
        return false;
    }

    // Every queued reaction has its own world transaction; an already committed hit is
    // never undone by a failed dependent reaction, and every reaction ledger rolls back.
    private void DrainTraitMechanics()
    {
        if (_drainingTraitMechanics) return;
        _drainingTraitMechanics = true;
        try
        {
            var budget = 4096;
            while ((!_traitMechanics.Events.IsEmpty || !_traitMechanics.Broken.IsEmpty) && --budget > 0)
            {
                var checkpoint = new BattleWorldStateCheckpoint(this);
                try
                {
                    using var resolution = _combatPipeline.BeginAuthoritativeResolution();
                    if (!_traitMechanics.Broken.IsEmpty)
                    {
                        var broken = _traitMechanics.Broken[0];
                        _traitMechanics = _traitMechanics with { Broken = _traitMechanics.Broken.RemoveAt(0) };
                        using var causal = _combatPipeline.ContinueReaction(
                            broken.ChainId.Length > 0 ? broken.ChainId : $"shield:{broken.Target}:{TickIndex}", broken.Depth);
                        TraitOnBroken(broken);
                    }
                    else
                    {
                        var e = _traitMechanics.Events[0];
                        _traitMechanics = _traitMechanics with { Events = _traitMechanics.Events.RemoveAt(0) };
                        using var causal = _combatPipeline.ContinueReaction(e.ChainId, e.Depth + 1);
                        TraitMechanicsOnEvent(e);
                    }
                    resolution.Commit();
                    checkpoint.Commit();
                }
                catch { checkpoint.Rollback(); throw; }
            }
            if (budget <= 0) throw new InvalidOperationException("Trait reaction budget exceeded.");
        }
        finally { _drainingTraitMechanics = false; }
    }

    private float TraitMechanicsModify(BattleCombatCalculationRequest r, float amount)
    {
        var source = TraitUnit(r.SourceRuntimeId);
        var target = TraitUnit(r.TargetRuntimeId);
        if (source is null || target is null) return amount;
        if (r.Kind is BattleCombatCalculationKind.Healing or BattleCombatCalculationKind.Shield)
        {
            foreach (var b in TraitFor(source.RuntimeId, TraitMechanicKind.SupportProtection)) amount *= 1 + b.Spec.Amount;
            if (r.Source.Kind == CombatSourceKind.Ability)
                amount *= 1 + TraitCounter($"castboost_action:{r.Source.InstanceId}");
            return amount;
        }
        foreach (var b in _traitMechanicBindings.Where(b => b.Members.Contains(source.RuntimeId)))
        {
            var m = b.Spec;
            if (m.Kind == TraitMechanicKind.DamageTakenRamp)
                amount *= 1 + Math.Min(m.Limit, TraitCounter(TraitKey(b, source.RuntimeId, "stacks"))) * m.Amount;
            if (m.Kind == TraitMechanicKind.WoundedTargetReward && target.Health / target.MaxHealth < m.Threshold)
                amount *= 1 + m.Amount;
            if (m.Kind == TraitMechanicKind.ActiveDamageAndRefund && r.DamageClass == CombatDamageClass.ActiveSkill)
                amount *= 1 + m.Amount;
            if (m.Kind == TraitMechanicKind.LowHealthLeech && source.Alive && source.Health / source.MaxHealth < m.Threshold)
                amount *= 1 + m.Amount;
            if (m.Kind == TraitMechanicKind.ActiveHitMarks && r.DamageClass == CombatDamageClass.ActiveSkill)
                amount *= 1 + MatrixReadEmber(target.RuntimeId) * m.Amount;
        }
        if (source.IsTemporary)
            foreach (var b in TraitFor(source.SummonerRuntimeId, TraitMechanicKind.OwnedSummonBonus)) amount *= 1 + b.Spec.Secondary;
        // The poison debuff belongs to the affected enemy, not to membership of its source.
        foreach (var b in _traitMechanicBindings.Where(b => b.Spec.Kind == TraitMechanicKind.PoisonOnAttack &&
                     b.Spec.Extra > 0 && source.Team != b.Team))
            if (MatrixReadPoison(source.RuntimeId, b.Team) >= b.Spec.Limit) amount *= 1 - b.Spec.Extra;
        if (TraitCounter($"protection:{target.RuntimeId}") > TickIndex)
            amount *= 1 - TraitCounter($"protection_amount:{target.RuntimeId}");
        if (r.DamageClass == CombatDamageClass.ActiveSkill)
            amount *= 1 + TraitCounter($"castboost_action:{r.Source.InstanceId}");
        return amount;
    }

    private void TraitMechanicsOnEvent(BattleCombatEvent e)
    {
        var source = TraitUnit(e.SourceRuntimeId);
        var target = TraitUnit(e.TargetRuntimeId);
        if (e.Kind == BattleCombatEventKind.BattleStarted)
        {
            foreach (var b in _traitMechanicBindings.Where(b => b.Spec.Kind == TraitMechanicKind.BarrierRetaliation))
            foreach (var u in TraitLiving(b)) MatrixApplyTimedShield(u.RuntimeId, u.RuntimeId,
                u.MaxHealth * b.Spec.Amount, b.Spec.DurationTicks, TraitOrigin(b, u.RuntimeId));
            return;
        }
        foreach (var b in _traitMechanicBindings)
        {
            var m = b.Spec;
            var sourceMember = source is not null && b.Members.Contains(source.RuntimeId);
            var targetMember = target is not null && b.Members.Contains(target.RuntimeId);
            if (e.Kind == BattleCombatEventKind.HealthLost && targetMember && target is { Alive: true } &&
                source is not null && source.Team != target.Team)
            {
                if (m.Kind == TraitMechanicKind.HealthThresholdRescue &&
                    (e.TargetHealthRatio >= 0 ? e.TargetHealthRatio : target.Health / target.MaxHealth) <= m.Threshold &&
                    TraitCounter(TraitKey(b, target.RuntimeId, "used")) == 0)
                {
                    TraitWrite(TraitKey(b, target.RuntimeId, "used"), 1);
                    MatrixApplyTimedShield(target.RuntimeId, target.RuntimeId, target.MaxHealth * m.Amount,
                        m.DurationTicks, TraitOrigin(b, target.RuntimeId));
                    var ally = TraitNear(target, target.Team, float.MaxValue).FirstOrDefault(u => !u.IsTemporary && !b.Members.Contains(u.RuntimeId) &&
                        TraitCounter(TraitKey(b, u.RuntimeId, "received")) < m.Limit);
                    if (ally is not null)
                    {
                        TraitWrite(TraitKey(b, ally.RuntimeId, "received"), TraitCounter(TraitKey(b, ally.RuntimeId, "received")) + 1);
                        MatrixApplyTimedShield(target.RuntimeId, ally.RuntimeId, ally.MaxHealth * m.Secondary,
                            m.DurationTicks, TraitOrigin(b, target.RuntimeId));
                    }
                }
                if (m.Kind == TraitMechanicKind.DamageTakenRamp)
                {
                    var key = TraitKey(b, target.RuntimeId, "damage");
                    var total = TraitCounter(key) + e.EffectiveValue;
                    var threshold = target.MaxHealth * m.Threshold;
                    if (threshold > 0)
                    {
                        var gained = (int)(total / threshold);
                        TraitWrite(key, total % threshold);
                        var stacks = TraitKey(b, target.RuntimeId, "stacks");
                        TraitWrite(stacks, Math.Min(m.Limit, TraitCounter(stacks) + gained));
                    }
                }
            }
            if (e.Kind == BattleCombatEventKind.HealthLost && sourceMember && source is { Alive: true } && target is not null &&
                source.Team != target.Team && m.Kind == TraitMechanicKind.LowHealthLeech &&
                (e.SourceHealthRatio >= 0 ? e.SourceHealthRatio : source.Health / source.MaxHealth) < m.Threshold &&
                !source.IsTemporary && e.DamageClass != CombatDamageClass.Periodic)
                // The heal keeps Trait provenance. It is not an active skill heal and
                // cannot produce another damage/hit/paid-cast event by itself.
                HealLiving(source.RuntimeId, source, e.EffectiveValue * m.Secondary, TraitOrigin(b, source.RuntimeId));
            if (targetMember && target is { Alive: true } && m.Kind == TraitMechanicKind.LowHealthLeech &&
                m.Extra > 0 && (e.TargetHealthRatio >= 0 ? e.TargetHealthRatio : target.Health / target.MaxHealth) < .3f &&
                TraitCounter(TraitKey(b, target.RuntimeId, "rescue")) == 0 &&
                e.Kind == BattleCombatEventKind.HealthLost)
            {
                TraitWrite(TraitKey(b, target.RuntimeId, "rescue"), 1);
                MatrixApplyTimedShield(target.RuntimeId, target.RuntimeId, target.MaxHealth * m.Extra,
                    m.DurationTicks, TraitOrigin(b, target.RuntimeId));
            }
            if (sourceMember && source is { Alive: true } && target is not null && e.Kind == BattleCombatEventKind.AttackLanded)
                TraitOnAttack(b, source, target, e);
            if (sourceMember && source is { Alive: true } && target is { Alive: true } &&
                e.Kind == BattleCombatEventKind.DamageResolved && e.DamageClass == CombatDamageClass.ActiveSkill &&
                m.Kind == TraitMechanicKind.ActiveHitMarks)
                TraitOnActiveHit(b, source, target, e);
            if (sourceMember && source is { Alive: true } && e.Kind == BattleCombatEventKind.UnitKilled &&
                m.Kind == TraitMechanicKind.WoundedTargetReward && m.Secondary > 0 &&
                TraitReady(TraitKey(b, source.RuntimeId, "kill_cd"), m.CooldownTicks))
            {
                MatrixApplyTimedShield(source.RuntimeId, source.RuntimeId, source.MaxHealth * m.Secondary,
                    m.DurationTicks, TraitOrigin(b, source.RuntimeId));
                TraitTemporaryAttribute(source.RuntimeId, source.RuntimeId, "trait_kill_speed", CombatAttribute.MoveSpeed,
                    m.Extra, m.DurationTicks);
            }
            if (sourceMember && source is not null && target is { Alive: true } &&
                m.Kind == TraitMechanicKind.SupportProtection && m.Secondary > 0 && e.EffectiveValue > 0 &&
                e.Kind is BattleCombatEventKind.HealingResolved or BattleCombatEventKind.ShieldResolved)
            {
                TraitWrite($"protection:{target.RuntimeId}", TickIndex + m.DurationTicks);
                TraitWrite($"protection_amount:{target.RuntimeId}", m.Secondary);
            }
            if (sourceMember && source is { Alive: true } && e.Kind == BattleCombatEventKind.ManaSkillResolved && e.EffectiveValue > 0)
                TraitOnCast(b, source, e);
            if (e.Kind == BattleCombatEventKind.UnitDefeated && target is not null &&
                e.Reason != "Consumed" && !_traitMechanics.ExcludedDeaths.Contains(target.RuntimeId))
                TraitOnDeath(b, target);
        }
    }

    private void TraitMechanicsBeginAbility(string ownerId, string actionId, bool actualManaSkill)
    {
        if (!actualManaSkill || string.IsNullOrEmpty(actionId)) return;
        var boost = TraitCounter($"castboost:{ownerId}");
        if (boost <= 0) return;
        TraitWrite($"castboost_action:{actionId}", boost);
        TraitWrite($"castboost:{ownerId}", 0);
    }

    private void TraitOnAttack(TraitBinding b, BattleUnitState source, BattleUnitState target, BattleCombatEvent e)
    {
        var m = b.Spec;
        var action = string.IsNullOrEmpty(e.ActionId) ? $"{e.Tick}:{source.RuntimeId}" : e.ActionId;
        var seen = TraitKey(b, source.RuntimeId, "attack_action");
        if (_traitMechanics.Targets.GetValueOrDefault(seen) == action) return;
        _traitMechanics = _traitMechanics with { Targets = _traitMechanics.Targets.SetItem(seen, action) };
        if (m.Kind == TraitMechanicKind.RepeatedTargetAttack)
        {
            var lastKey = TraitKey(b, source.RuntimeId, "last_target");
            var countKey = TraitKey(b, source.RuntimeId, "hits");
            var last = _traitMechanics.Targets.GetValueOrDefault(lastKey);
            var count = TraitCounter(countKey);
            if (last != target.RuntimeId) count = m.Enabled && TraitUnit(last ?? "") is { Alive: false } ? Math.Min(2, count) : 0;
            count++;
            _traitMechanics = _traitMechanics with { Targets = _traitMechanics.Targets.SetItem(lastKey, target.RuntimeId) };
            if (count >= m.Count)
            {
                count = 0;
                if (target.Alive) ApplyDamage(source.RuntimeId, source, target, source.Damage * m.Amount,
                    TraitOrigin(b, source.RuntimeId), damageClass: CombatDamageClass.Derived);
            }
            TraitWrite(countKey, count);
        }
        if (m.Kind == TraitMechanicKind.DamageTakenRamp && m.Secondary > 0 &&
            TraitCounter(TraitKey(b, source.RuntimeId, "stacks")) >= m.Limit &&
            TraitReady(TraitKey(b, source.RuntimeId, "heal_cd"), m.CooldownTicks))
            HealLiving(source.RuntimeId, source, source.MaxHealth * m.Secondary, TraitOrigin(b, source.RuntimeId));
        if (!target.Alive) return;
        if (m.Kind == TraitMechanicKind.ChillOnAttack)
        {
            if (TraitReady(TraitKey(b, source.RuntimeId, "chill_cd"), m.CooldownTicks)) MatrixApplyChill(source.RuntimeId, target.RuntimeId, 1);
            if (m.Count >= 3 && _traitMechanics.Chill.TryGetValue(target.RuntimeId, out var chill) && MatrixReadChill(target.RuntimeId) >= 3 &&
                TraitCounter($"freeze_cd:{target.RuntimeId}") <= TickIndex)
            {
                var result = _statusScope.Apply(TraitStatus("matrix_freeze", "冻结", Math.Max(1, m.DurationTicks),
                    StatusBehaviorKind.DisableActions, harmful: true), source.RuntimeId, target.RuntimeId, TickIndex);
                if (result.Applied)
                {
                    TraitWrite($"freeze_cd:{target.RuntimeId}", TickIndex + 30);
                    _statusScope.MatrixRemoveInstance(chill.StatusInstance, TickIndex);
                    _traitMechanics = _traitMechanics with { Chill = _traitMechanics.Chill.Remove(target.RuntimeId) };
                    if (m.Enabled)
                        foreach (var other in TraitNear(target, target.Team, 2).Take(2)) MatrixApplyChill(source.RuntimeId, other.RuntimeId, 1);
                }
            }
        }
        if (m.Kind == TraitMechanicKind.PoisonOnAttack && TraitReady(TraitKey(b, source.RuntimeId, "poison_cd"), m.CooldownTicks))
            MatrixApplyPoison(source.RuntimeId, target.RuntimeId, 1);
    }

    private void TraitOnActiveHit(TraitBinding b, BattleUnitState source, BattleUnitState target, BattleCombatEvent e)
    {
        var action = string.IsNullOrEmpty(e.ActionId) ? $"{e.Source.InstanceId}:{e.Tick}" : e.ActionId;
        var key = TraitKey(b, target.RuntimeId, $"mark_action:{source.RuntimeId}:{action}");
        if (TraitCounter(key) > 0) return;
        TraitWrite(key, 1);
        if (MatrixReadEmber(target.RuntimeId) >= b.Spec.Count && b.Spec.Secondary > 0 &&
            TraitReady($"ember_cd:{target.RuntimeId}", b.Spec.CooldownTicks))
        {
            MatrixConsumeEmber(target.RuntimeId, int.MaxValue);
            var damage = Math.Max(source.Damage, source.Attributes.GetValue(CombatAttribute.SpellPower)) * b.Spec.Secondary;
            ApplyDamage(source.RuntimeId, source, target, damage, TraitOrigin(b, source.RuntimeId), damageClass: CombatDamageClass.Derived);
            if (b.Spec.Extra > 0)
                foreach (var other in TraitNear(target, target.Team, 1.5f).ToArray())
                    ApplyDamage(source.RuntimeId, source, other, damage * b.Spec.Extra, TraitOrigin(b, source.RuntimeId), damageClass: CombatDamageClass.Derived);
            PublishCombat(new(BattleCombatEventKind.StatusStackChanged, TraitOrigin(b, source.RuntimeId), source.RuntimeId,
                target.RuntimeId, TickIndex, SubjectStableId: "matrix_ember_detonation", PreviousStacks: b.Spec.Count));
        }
        if (target.Alive) MatrixApplyEmber(source.RuntimeId, target.RuntimeId, 1);
    }

    private void TraitOnCast(TraitBinding b, BattleUnitState source, BattleCombatEvent e)
    {
        var m = b.Spec;
        if (m.Kind == TraitMechanicKind.ActiveDamageAndRefund && m.Secondary > 0)
        {
            var key = TraitKey(b, source.RuntimeId, "casts");
            var count = TraitCounter(key) + 1;
            TraitWrite(key, count);
            if ((count - 1) % Math.Max(1, m.Count) == 0)
                MatrixGrantMana(source.RuntimeId, source.RuntimeId, e.EffectiveValue * m.Secondary, true);
        }
        if (m.Kind != TraitMechanicKind.SharedCastResource) return;
        var progress = TraitKey(b, "team", "casts");
        var ready = TraitKey(b, "team", "cast_cd");
        var next = TraitCounter(progress) + 1;
        if (TraitCounter(ready) > TickIndex) { TraitWrite(progress, Math.Min(m.Count - 1, next)); return; }
        if (next < m.Count) { TraitWrite(progress, next); return; }
        TraitWrite(progress, 0);
        TraitWrite(ready, TickIndex + m.CooldownTicks);
        foreach (var u in TraitLiving(b).Where(u => u.MaxMana > 0).OrderBy(u => u.CurrentMana / u.MaxMana)
                     .ThenBy(u => u.RuntimeId, StringComparer.Ordinal).Take(m.Limit))
        {
            MatrixGrantMana(source.RuntimeId, u.RuntimeId, m.Amount, true);
            if (m.Secondary > 0) TraitWrite($"castboost:{u.RuntimeId}", m.Secondary);
        }
    }

    private void TraitOnDeath(TraitBinding b, BattleUnitState dead)
    {
        var m = b.Spec;
        var owner = TraitUnit(dead.SummonerRuntimeId);
        if (dead.Team != b.Team) return;
        if (m.Kind == TraitMechanicKind.OwnedSummonBonus && m.Extra > 0 && dead.IsTemporary && owner is { Alive: true } &&
            b.Members.Contains(owner.RuntimeId))
        {
            var window = TraitKey(b, owner.RuntimeId, "mana_window");
            var amount = TraitKey(b, owner.RuntimeId, "mana_window_amount");
            if (TraitCounter(window) <= TickIndex) { TraitWrite(window, TickIndex + m.CooldownTicks); TraitWrite(amount, 0); }
            var gain = Math.Min(m.Extra, Math.Max(0, m.Limit - TraitCounter(amount)));
            TraitWrite(amount, TraitCounter(amount) + gain);
            MatrixGrantMana(owner.RuntimeId, owner.RuntimeId, gain, true);
        }
        if (m.Kind != TraitMechanicKind.DeathResource ||
            !(b.Members.Contains(dead.RuntimeId) || dead.IsTemporary && owner is not null && b.Members.Contains(owner.RuntimeId))) return;
        var consumed = TraitKey(b, dead.RuntimeId, "soul_given");
        if (TraitCounter(consumed) > 0) return;
        TraitWrite(consumed, 1);
        var souls = TraitKey(b, "team", "souls");
        TraitWrite(souls, TraitCounter(souls) + 1);
        TraitSpendSouls(b);
    }

    private void TraitSpendSouls(TraitBinding b)
    {
        var m = b.Spec;
        var souls = TraitKey(b, "team", "souls");
        if (TraitCounter(souls) < m.Count || TraitCounter(TraitKey(b, "team", "soul_cd")) > TickIndex) return;
        var recipients = TraitLiving(b).OrderBy(u => u.Health / u.MaxHealth).ThenBy(u => u.RuntimeId, StringComparer.Ordinal).Take(m.Limit).ToArray();
        if (recipients.Length == 0) return;
        TraitWrite(souls, TraitCounter(souls) - m.Count);
        TraitWrite(TraitKey(b, "team", "soul_cd"), TickIndex + m.CooldownTicks);
        foreach (var u in recipients) HealLiving(u.RuntimeId, u, u.MaxHealth * m.Amount, TraitOrigin(b, u.RuntimeId));
        if (!m.Enabled || string.IsNullOrEmpty(m.SummonContentId) ||
            _traitMechanics.Products.Count(p => TraitUnit(p.Id) is { Alive: true } u && u.Team == b.Team) >= 2) return;
        if (!_config.TacticalSummons.TryGetValue(m.SummonContentId, out var profile))
            throw new InvalidOperationException($"Missing trait summon profile '{m.SummonContentId}'.");
        var anchor = recipients[0];
        var before = _summonCounter;
        if (SpawnTemporaryNear(profile, b.Team, anchor.Position, anchor.MaxHealth * .25f / profile.MaxHealth,
                anchor.Damage * .3f / Math.Max(.001f, profile.Damage), anchor.RuntimeId))
        {
            var id = $"s-{b.Team}-{before}";
            _traitMechanics = _traitMechanics with { Products = _traitMechanics.Products.Add(new(id, TickIndex + m.DurationTicks)),
                ExcludedDeaths = _traitMechanics.ExcludedDeaths.Add(id) };
        }
    }

    private void TraitOnBroken(TraitBrokenShield broken)
    {
        var unit = TraitUnit(broken.Target);
        if (unit is null) return;
        PublishCombat(new(BattleCombatEventKind.StatusStackChanged, broken.Origin, broken.Origin.OwnerRuntimeId,
            unit.RuntimeId, TickIndex, EffectiveValue: broken.Absorbed, SubjectStableId: "matrix_shield_broken"));
        if (!unit.Alive) return;
        var derivedCounter = broken.Origin.Kind == CombatSourceKind.Trait && _traitMechanicBindings.Any(b =>
            b.Id == broken.Origin.StableId && b.Spec.Kind == TraitMechanicKind.BarrierRetaliation);
        foreach (var b in TraitFor(unit.RuntimeId, TraitMechanicKind.BarrierRetaliation))
        {
            var m = b.Spec;
            if (!derivedCounter && m.Secondary > 0 && TraitReady(TraitKey(b, unit.RuntimeId, "break_cd"), m.CooldownTicks))
                foreach (var enemy in TraitNear(unit, 1 - unit.Team, 1.5f).ToArray())
                    ApplyDamage(unit.RuntimeId, unit, enemy, Math.Min(broken.Absorbed * m.Secondary, unit.Definition.Damage * 1.2f),
                        TraitOrigin(b, unit.RuntimeId), damageClass: CombatDamageClass.Derived);
            if (m.Extra > 0 && TraitCounter(TraitKey(b, unit.RuntimeId, "repair")) == 0)
            {
                TraitWrite(TraitKey(b, unit.RuntimeId, "repair"), 1);
                MatrixApplyTimedShield(unit.RuntimeId, unit.RuntimeId, unit.MaxHealth * m.Extra, 40, TraitOrigin(b, unit.RuntimeId));
            }
        }
    }

    private static CompiledStatusDefinition TraitStatus(string id, string name, int ticks,
        StatusBehaviorKind behavior = StatusBehaviorKind.None, bool harmful = false,
        ImmutableArray<CompiledAttributeModifier> modifiers = default) => new(id, "", name, name, behavior,
        harmful ? StatusDisposition.Harmful : StatusDisposition.Helpful, StatusDurationKind.TimedTicks,
        Math.Max(1, ticks), StatusAggregationPolicy.Independent, 1, StatusOverflowPolicy.RejectNewStacks,
        StatusDurationRefreshPolicy.None, StatusPeriodicResetPolicy.KeepSchedule, StatusDispelCategory.Ordinary,
        StatusDeathPolicy.Remove, behavior == StatusBehaviorKind.DisableActions ? StatusControlDurationRule.LinearResistanceCeiling : StatusControlDurationRule.None,
        behavior == StatusBehaviorKind.DisableActions ? [StatusDefinitionCompiler.ActionDisabledTag] : [],
        modifiers.IsDefault ? [] : modifiers, 1, 0, null, [], [], null,
        new CompiledStatusPresentation("status", "", "", "", "", name));

    private void TraitTemporaryAttribute(string source, string target, string id, CombatAttribute attribute, float amount, int ticks)
    {
        foreach (var status in _statusScope.SnapshotOwner(target).Where(s => s.StableId == id))
            _statusScope.MatrixRemoveInstance(status.InstanceId, TickIndex);
        _statusScope.Apply(TraitStatus(id, "临时增益", ticks, modifiers:
            [new(attribute, AttributeModifierOperation.Multiply, new CompiledConstantMagnitude(1 + amount), 0, id)]), source, target, TickIndex);
    }

    private float MatrixApplyTimedShield(string sourceId, string targetId, float amount, int durationTicks, CombatSourceRef origin = default)
    {
        var target = TraitUnit(targetId);
        if (target is not { Alive: true } || amount <= 0) return 0;
        var extra = TraitFor(sourceId, TraitMechanicKind.BarrierRetaliation).Any(b => b.Spec.Enabled) ? 20 : 0;
        if (target.Shield <= 0) TraitWrite($"shield_absorbed:{targetId}", 0);
        var granted = ApplyShield(sourceId, target, amount, origin);
        if (granted > 0) _traitMechanics = _traitMechanics with
        { Shields = _traitMechanics.Shields.Add(new(sourceId, targetId, TickIndex + Math.Max(1, durationTicks + extra), granted)) };
        return granted;
    }

    private void TraitMechanicsShieldConsumed(BattleUnitState target, float amount, bool enemyDamage, CombatSourceRef origin)
    {
        var remaining = amount;
        var shields = _traitMechanics.Shields.ToBuilder();
        for (var i = 0; i < shields.Count && remaining > 0; i++)
        {
            if (shields[i].Target != target.RuntimeId) continue;
            var spent = Math.Min(remaining, shields[i].Remaining);
            remaining -= spent;
            shields[i] = shields[i] with { Remaining = shields[i].Remaining - spent };
        }
        _traitMechanics = _traitMechanics with { Shields = shields.Where(s => s.Remaining > 0).ToImmutableArray() };
        if (!_traitObserveBreaks) return;
        var key = $"shield_absorbed:{target.RuntimeId}";
        if (enemyDamage) TraitWrite(key, TraitCounter(key) + amount);
        if (target.Shield > 0) return;
        if (enemyDamage && amount > 0)
            _traitMechanics = _traitMechanics with { Broken = _traitMechanics.Broken.Add(new(target.RuntimeId, TraitCounter(key), origin)) };
        TraitWrite(key, 0);
    }

    private void MatrixGrantMana(string sourceId, string targetId, float amount, bool deferWhileLocked = false)
    {
        var unit = TraitUnit(targetId);
        if (unit is not { Alive: true } || unit.MaxMana <= 0 || amount <= 0) return;
        if (deferWhileLocked && (TickIndex < unit.ManaLockedUntilTick || unit.Trample is not null || unit.ProjectileSequence is not null ||
                                unit.ProjectileWindups.Any(p => p.SkillOrigin.IsSpecified)))
        {
            _traitMechanics = _traitMechanics with { Mana = _traitMechanics.Mana.Add(new(sourceId, targetId, amount)) };
            return;
        }
        BattleHeroMana.Gain(unit, amount, TickIndex);
    }

    private int MatrixReadPoison(string targetId, int? sourceTeam = null)
    {
        var deathKey = $"death_poison:{targetId}:{sourceTeam?.ToString() ?? "all"}";
        if (TraitUnit(targetId) is { Alive: false } && _traitMechanics.Counters.TryGetValue(deathKey, out var frozen)) return (int)frozen;
        var visible = TraitVisibleStatusIds(targetId);
        return _traitMechanics.Poison.Count(p => p.Target == targetId && p.Expires > TickIndex && visible.Contains(p.StatusInstance) &&
            (sourceTeam is null || p.SourceTeam == sourceTeam));
    }

    private HashSet<string> TraitVisibleStatusIds(string ownerId) => _statusScope.SnapshotOwner(ownerId)
        .Select(s => s.InstanceId).ToHashSet(StringComparer.Ordinal);

    private void MatrixApplyPoison(string sourceId, string targetId, int layers)
    {
        if (TraitUnit(sourceId) is not { } source || TraitUnit(targetId) is not { Alive: true }) return;
        var bonus = TraitFor(sourceId, TraitMechanicKind.PoisonOnAttack).Sum(b => b.Spec.Amount);
        for (var i = 0; i < Math.Clamp(layers, 0, 1000); i++)
        {
            var status = _statusScope.Apply(TraitStatus("matrix_poison", "棘毒", 120, harmful: true), sourceId, targetId, TickIndex);
            if (!status.Applied || status.Status is null) continue;
            var sequence = _traitMechanics.Sequence + 1;
            _traitMechanics = _traitMechanics with { Sequence = sequence,
                Poison = _traitMechanics.Poison.Add(new(sequence, sourceId, source.Team, targetId, TickIndex + 120, TickIndex + 10,
                    2 * (1 + bonus), status.Status.InstanceId)) };
        }
    }

    private int MatrixConsumePoison(string targetId, int layers)
    {
        var visible = TraitVisibleStatusIds(targetId);
        var selected = _traitMechanics.Poison.Where(p => p.Target == targetId && p.Expires > TickIndex && visible.Contains(p.StatusInstance))
            .OrderByDescending(p => p.Expires).ThenBy(p => p.Sequence).Take(Math.Max(0, layers)).ToArray();
        foreach (var p in selected) _statusScope.MatrixRemoveInstance(p.StatusInstance, TickIndex);
        var ids = selected.Select(p => p.Sequence).ToHashSet();
        _traitMechanics = _traitMechanics with { Poison = _traitMechanics.Poison.Where(p => !ids.Contains(p.Sequence)).ToImmutableArray() };
        return selected.Length;
    }

    private ImmutableArray<MatrixPoisonTransferReceipt> MatrixTransferPoison(string fromId, string toId, int layers, int? sourceTeam = null)
    {
        if (fromId == toId || TraitUnit(toId) is not { Alive: true }) return [];
        var visible = TraitVisibleStatusIds(fromId);
        var selected = _traitMechanics.Poison.Where(p => p.Target == fromId && p.Expires > TickIndex && visible.Contains(p.StatusInstance) &&
                (sourceTeam is null || p.SourceTeam == sourceTeam))
            .OrderByDescending(p => p.Expires).ThenBy(p => p.Sequence).Take(Math.Max(0, layers)).ToArray();
        foreach (var p in selected)
        {
            _statusScope.MatrixRemoveInstance(p.StatusInstance, TickIndex);
            var applied = _statusScope.Apply(TraitStatus("matrix_poison", "棘毒", p.Expires - TickIndex, harmful: true),
                p.Source, toId, TickIndex);
            _traitMechanics = _traitMechanics with { Poison = _traitMechanics.Poison.Remove(p) };
            if (applied.Applied && applied.Status is not null)
                _traitMechanics = _traitMechanics with { Poison = _traitMechanics.Poison.Add(p with
                { Target = toId, StatusInstance = applied.Status.InstanceId }) };
        }
        var receipts = selected.GroupBy(p => p.Source).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new MatrixPoisonTransferReceipt(g.Key, fromId, toId, g.Count())).ToImmutableArray();
        foreach (var receipt in receipts)
            PublishCombat(new(BattleCombatEventKind.StatusStackChanged, CombatSourceRef.System("poison_transfer"),
                receipt.SourceId, toId, TickIndex, SubjectStableId: "matrix_poison_transfer", CurrentStacks: receipt.Layers));
        return receipts;
    }

    private bool MatrixApplyChill(string sourceId, string targetId, int layers)
    {
        if (TraitUnit(targetId) is not { Alive: true } || layers <= 0) return false;
        var limit = TraitFor(sourceId, TraitMechanicKind.ChillOnAttack).Select(b => b.Spec.Count).DefaultIfEmpty(2).Max();
        _traitMechanics.Chill.TryGetValue(targetId, out var previous);
        var oldStacks = MatrixReadChill(targetId);
        var stacks = Math.Min(Math.Max(limit, oldStacks), oldStacks + layers);
        if (previous is not null) _statusScope.MatrixRemoveInstance(previous.StatusInstance, TickIndex);
        // Attribute projections are per stack. Their product yields the authored linear
        // total slow while the public Status model exposes the real number of layers.
        var perStackFactor = MathF.Pow(1 - .06f * stacks, 1f / stacks);
        var modifiers = ImmutableArray.Create(
            new CompiledAttributeModifier(CombatAttribute.AttackSpeed, AttributeModifierOperation.Multiply, new CompiledConstantMagnitude(perStackFactor), 0, "chill_attack"),
            new CompiledAttributeModifier(CombatAttribute.MoveSpeed, AttributeModifierOperation.Multiply, new CompiledConstantMagnitude(perStackFactor), 0, "chill_move"));
        var definition = TraitStatus("matrix_chill", "寒意", 40, harmful: true, modifiers: modifiers) with
        { AggregationPolicy = StatusAggregationPolicy.BySource, StackLimit = stacks };
        var applied = _statusScope.ApplyBatch(Enumerable.Range(0, stacks)
            .Select(_ => new StatusApplicationRequest(definition, sourceId, targetId, TickIndex)))[^1];
        if (!applied.Applied || applied.Status is null) return false;
        _traitMechanics = _traitMechanics with { Chill = _traitMechanics.Chill.SetItem(targetId, new(sourceId, stacks, TickIndex + 40, applied.Status.InstanceId)) };
        return true;
    }

    private int MatrixReadChill(string targetId) => TraitUnit(targetId) is { Alive: false }
        ? (int)TraitCounter($"death_chill:{targetId}")
        : _traitMechanics.Chill.TryGetValue(targetId, out var chill) && chill.Expires > TickIndex &&
          TraitVisibleStatusIds(targetId).Contains(chill.StatusInstance) ? chill.Stacks : 0;

    private int MatrixReadEmber(string targetId) => TraitUnit(targetId) is { Alive: false }
        ? (int)TraitCounter($"death_ember:{targetId}")
        : _traitMechanics.Ember.TryGetValue(targetId, out var ember) && ember.Expires > TickIndex &&
          TraitVisibleStatusIds(targetId).Contains(ember.StatusInstance) ? ember.Stacks : 0;
    private void MatrixApplyEmber(string sourceId, string targetId, int layers)
    {
        if (TraitUnit(targetId) is not { Alive: true }) return;
        _traitMechanics = _traitMechanics with { Ember = _traitMechanics.Ember.SetItem(targetId,
            new(sourceId, Math.Clamp(MatrixReadEmber(targetId) + layers, 0, 5), TickIndex + 80)) };
        TraitRefreshEmberStatus(targetId);
    }
    private int MatrixConsumeEmber(string targetId, int layers)
    {
        var old = MatrixReadEmber(targetId);
        var consumed = Math.Min(old, Math.Max(0, layers));
        if (_traitMechanics.Ember.TryGetValue(targetId, out var ember))
            _traitMechanics = _traitMechanics with { Ember = old == consumed ? _traitMechanics.Ember.Remove(targetId) :
                _traitMechanics.Ember.SetItem(targetId, ember with { Stacks = old - consumed }) };
        TraitRefreshEmberStatus(targetId);
        return consumed;
    }
    private void TraitRefreshEmberStatus(string targetId)
    {
        foreach (var status in _statusScope.SnapshotOwner(targetId).Where(s => s.StableId == "matrix_ember"))
            _statusScope.MatrixRemoveInstance(status.InstanceId, TickIndex);
        if (_traitMechanics.Ember.TryGetValue(targetId, out var ember) && ember.Expires > TickIndex && ember.Stacks > 0)
        {
            var definition = TraitStatus("matrix_ember", "余烬", ember.Expires - TickIndex, harmful: true) with
            { AggregationPolicy = StatusAggregationPolicy.BySource, StackLimit = ember.Stacks };
            var applied = _statusScope.ApplyBatch(Enumerable.Range(0, ember.Stacks)
                .Select(_ => new StatusApplicationRequest(definition, ember.Source, targetId, TickIndex)))[^1];
            _traitMechanics = _traitMechanics with { Ember = applied.Applied && applied.Status is not null
                ? _traitMechanics.Ember.SetItem(targetId, ember with { StatusInstance = applied.Status.InstanceId })
                : _traitMechanics.Ember.Remove(targetId) };
        }
    }

    private void TraitMechanicsBeforeDeath(BattleUnitState target)
    {
        // Death listeners execute after authoritative cleanup. Keep only immutable facts,
        // never the ticking layers, so killed-poisoned/frozen predicates remain truthful.
        var visible = TraitVisibleStatusIds(target.RuntimeId);
        TraitWrite($"death_poison:{target.RuntimeId}:all", _traitMechanics.Poison.Count(p => p.Target == target.RuntimeId &&
            p.Expires > TickIndex && visible.Contains(p.StatusInstance)));
        for (var team = 0; team <= 1; team++)
            TraitWrite($"death_poison:{target.RuntimeId}:{team}", _traitMechanics.Poison.Count(p => p.Target == target.RuntimeId &&
                p.Expires > TickIndex && p.SourceTeam == team && visible.Contains(p.StatusInstance)));
        TraitWrite($"death_chill:{target.RuntimeId}", _traitMechanics.Chill.TryGetValue(target.RuntimeId, out var chill) &&
            chill.Expires > TickIndex && visible.Contains(chill.StatusInstance) ? chill.Stacks : 0);
        TraitWrite($"death_ember:{target.RuntimeId}", _traitMechanics.Ember.TryGetValue(target.RuntimeId, out var ember) &&
            ember.Expires > TickIndex && visible.Contains(ember.StatusInstance) ? ember.Stacks : 0);
        foreach (var b in _traitMechanicBindings.Where(b => b.Spec.Kind == TraitMechanicKind.PoisonOnAttack &&
                     b.Spec.Enabled && target.Team != b.Team))
        {
            var next = TraitNear(target, target.Team, 3).FirstOrDefault();
            if (next is not null) MatrixTransferPoison(target.RuntimeId, next.RuntimeId, MatrixReadPoison(target.RuntimeId, b.Team) / 2, b.Team);
        }
        _traitMechanics = _traitMechanics with { Poison = _traitMechanics.Poison.Where(p => p.Target != target.RuntimeId).ToImmutableArray(),
            Chill = _traitMechanics.Chill.Remove(target.RuntimeId), Ember = _traitMechanics.Ember.Remove(target.RuntimeId) };
    }

    private void TraitMechanicsOnSummoned(BattleUnitState unit)
    {
        foreach (var b in TraitFor(unit.SummonerRuntimeId, TraitMechanicKind.OwnedSummonBonus))
        {
            var oldMax = unit.MaxHealth;
            unit.Attributes.ApplyModifier(new(CombatAttribute.MaxHealth, AttributeModifierOperation.Multiply,
                new CompiledConstantMagnitude(1 + b.Spec.Amount), 0, "summon_health"), TraitOrigin(b, unit.SummonerRuntimeId));
            unit.Health += unit.MaxHealth - oldMax;
        }
    }

    private bool TraitMechanicsExtraSummon(string ownerId)
    {
        var b = TraitFor(ownerId, TraitMechanicKind.OwnedSummonBonus).FirstOrDefault(b => b.Spec.Enabled);
        if (b is null || TraitCounter(TraitKey(b, ownerId, "extra_summon")) > 0) return false;
        TraitWrite(TraitKey(b, ownerId, "extra_summon"), 1);
        return true;
    }

    private bool MatrixExcludedDeath(string runtimeId) => _traitMechanics.ExcludedDeaths.Contains(runtimeId);

    private void TraitMechanicsAdvance()
    {
        if (_traitMechanics.Shields.IsEmpty && _traitMechanics.Mana.IsEmpty && _traitMechanics.Poison.IsEmpty &&
            _traitMechanics.Chill.IsEmpty && _traitMechanics.Ember.IsEmpty && _traitMechanics.Products.IsEmpty &&
            !_traitMechanicBindings.Any(b => b.Spec.Kind == TraitMechanicKind.DeathResource &&
                TraitCounter(TraitKey(b, "team", "souls")) >= b.Spec.Count)) return;
        var checkpoint = new BattleWorldStateCheckpoint(this);
        try
        {
            using var resolution = _combatPipeline.BeginAuthoritativeResolution();
            foreach (var shield in _traitMechanics.Shields.Where(s => s.Expires <= TickIndex).ToArray())
                if (TraitUnit(shield.Target) is { Alive: true } u)
                {
                    u.Shield = Math.Max(0, u.Shield - shield.Remaining);
                    if (u.Shield == 0) TraitWrite($"shield_absorbed:{u.RuntimeId}", 0);
                }
            _traitMechanics = _traitMechanics with { Shields = _traitMechanics.Shields.Where(s => s.Expires > TickIndex && TraitUnit(s.Target) is { Alive: true }).ToImmutableArray() };
            foreach (var pending in _traitMechanics.Mana.ToArray())
            {
                var u = TraitUnit(pending.Target);
                if (u is { Alive: true } && (TickIndex < u.ManaLockedUntilTick || u.Trample is not null || u.ProjectileSequence is not null || u.ProjectileWindups.Any(p => p.SkillOrigin.IsSpecified))) continue;
                _traitMechanics = _traitMechanics with { Mana = _traitMechanics.Mana.Remove(pending) };
                if (u is { Alive: true }) BattleHeroMana.Gain(u, pending.Amount, TickIndex);
            }
            foreach (var p in _traitMechanics.Poison.ToArray())
            {
                // A previous layer may kill this target and transfer all remaining layers.
                if (!_traitMechanics.Poison.Contains(p)) continue;
                var target = TraitUnit(p.Target);
                var visible = target is { Alive: true } && target.Statuses.Any(s => s.InstanceId == p.StatusInstance);
                if (!visible || p.Expires < TickIndex)
                { _traitMechanics = _traitMechanics with { Poison = _traitMechanics.Poison.Remove(p) }; continue; }
                if (p.NextTick > TickIndex) continue;
                _traitMechanics = _traitMechanics with { Poison = _traitMechanics.Poison.Replace(p, p with { NextTick = p.NextTick + 10 }) };
                ApplyDamage(p.Source, TraitUnit(p.Source), target!, p.Damage,
                    CombatSourceRef.Status("matrix_poison", p.Source, $"poison:{p.Sequence}"), EffectDamageType.True,
                    damageClass: CombatDamageClass.Periodic);
            }
            foreach (var pair in _traitMechanics.Chill.ToArray())
                if (pair.Value.Expires <= TickIndex || TraitUnit(pair.Key) is not { Alive: true } u ||
                    !u.Statuses.Any(s => s.InstanceId == pair.Value.StatusInstance))
                    _traitMechanics = _traitMechanics with { Chill = _traitMechanics.Chill.Remove(pair.Key) };
            foreach (var pair in _traitMechanics.Ember.ToArray())
                if (pair.Value.Expires <= TickIndex || TraitUnit(pair.Key) is not { Alive: true } u ||
                    !u.Statuses.Any(s => s.InstanceId == pair.Value.StatusInstance))
                    _traitMechanics = _traitMechanics with { Ember = _traitMechanics.Ember.Remove(pair.Key) };
            foreach (var b in _traitMechanicBindings.Where(b => b.Spec.Kind == TraitMechanicKind.DeathResource)) TraitSpendSouls(b);
            foreach (var product in _traitMechanics.Products.Where(p => p.Expires <= TickIndex).ToArray())
                if (TraitUnit(product.Id) is { Alive: true } u)
                {
                    RetireEnemyProduct(u, u.SummonerRuntimeId, "trait_product_expired");
                }
            _traitMechanics = _traitMechanics with { Products = _traitMechanics.Products.Where(p => p.Expires > TickIndex && TraitUnit(p.Id) is { Alive: true }).ToImmutableArray() };
            resolution.Commit(); checkpoint.Commit();
        }
        catch { checkpoint.Rollback(); throw; }
    }

    private void TraitMechanicsComplete() => _traitMechanics = TraitMechanicsState.Empty;
}
