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

internal sealed partial class BattleTraitReactionScope(IBattleTraitQueries read, IBattleTraitCommands commands,
    IBattleTraitTransactions transactions, BattleTraitBindings bindings, BattleStatusScope statuses,
    BattleElementStatusScope elements, BattleSupportEffectScope support)
{
    private sealed record TraitProduct(string Id, int Expires);
    private sealed record State(ImmutableDictionary<string, float> Counters, ImmutableDictionary<string, string> Targets,
        ImmutableArray<BattleCombatEvent> Events, ImmutableArray<TraitProduct> Products, ImmutableHashSet<string> ExcludedDeaths)
    {
        public static State Empty => new(ImmutableDictionary<string, float>.Empty, ImmutableDictionary<string, string>.Empty, [], [], ImmutableHashSet<string>.Empty);
    }
    private State _state = State.Empty;
    private bool _draining;
    private int TickIndex => read.Tick;
    private ImmutableArray<BattleTraitBinding> _traitMechanicBindings => bindings.Entries;
    private BattleUnitView? TraitUnit(string id) => read.Units.FirstOrDefault(unit => unit.RuntimeId == id);
    private float TraitCounter(string key) => _state.Counters.GetValueOrDefault(key);
    private void TraitWrite(string key, float value) => _state = _state with { Counters = _state.Counters.SetItem(key, value) };
    private CombatSourceRef TraitOrigin(BattleTraitBinding binding, string owner) => bindings.Origin(binding, owner);
    private IEnumerable<BattleTraitBinding> TraitFor(string owner, TraitMechanicKind kind) => _traitMechanicBindings.Where(b => b.Spec.Kind == kind && IsMember(b, owner));
    public void Observe(BattleCombatEvent fact)
    {
        support.Observe(fact);
        _state = _state with { Events = _state.Events.Add(fact) };
    }
    internal sealed class Checkpoint
    {
        private readonly BattleTraitReactionScope _owner;
        private readonly State _state;
        internal Checkpoint(BattleTraitReactionScope owner) { _owner = owner; _state = owner._state; }
        internal void Restore(BattleTraitReactionScope owner)
        {
            if (!ReferenceEquals(owner, _owner)) throw new InvalidOperationException("Checkpoint belongs to another battle module.");
            owner._state = _state;
        }
    }
    public Checkpoint Capture() => new(this);
    public void Restore(Checkpoint checkpoint) => checkpoint.Restore(this);
    public void Clear() => _state = State.Empty;
    private static string TraitKey(BattleTraitBinding b, string owner, string suffix) => $"{b.Team}:{b.Id}:{b.Spec.Kind}:{owner}:{suffix}";
    private IEnumerable<BattleUnitView> TraitLiving(BattleTraitBinding b) => read.Units
        .Where(u => u.Alive && u.Team == b.Team && IsMember(b, u.RuntimeId))
        .OrderBy(u => u.RuntimeId, StringComparer.Ordinal);
    private IEnumerable<BattleUnitView> TraitNear(BattleUnitView center, int team, float range, bool includeSelf = false) =>
        read.Units.Where(u => u.Alive && u.Team == team && (includeSelf || u != center) &&
                          u.Position.DistanceTo(center.Position) <= range)
            .OrderBy(u => u.Position.DistanceSquaredTo(center.Position)).ThenBy(u => u.RuntimeId, StringComparer.Ordinal);
    private bool TraitReady(string key, int cooldown)
    {
        if (TraitCounter(key) > TickIndex) return false;
        TraitWrite(key, TickIndex + Math.Max(1, cooldown));
        return true;
    }

    public bool IsInterested(BattleCombatEvent e)
    {
        if (e.Kind == BattleCombatEventKind.BattleStarted && !_traitMechanicBindings.IsEmpty) return true;
        if (e.Kind == BattleCombatEventKind.UnitTransformed && TraitFor(e.SourceRuntimeId, TraitMechanicKind.TransformHealth).Any()) return true;
        if (e.Kind == BattleCombatEventKind.AttackLanded && _traitMechanicBindings.Any(b => IsMember(b, e.SourceRuntimeId) &&
            b.Spec.Kind is TraitMechanicKind.FreezeOnAttack or TraitMechanicKind.AdditionalAttacks)) return true;
        if (e.Kind == BattleCombatEventKind.DamageResolved && support.HasUnlinkedBreak(e)) return true;
        foreach (var b in _traitMechanicBindings)
        {
            var source = IsMember(b, e.SourceRuntimeId);
            var target = IsMember(b, e.TargetRuntimeId);
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
    public void Drain()
    {
        if (_draining) return;
        _draining = true;
        try
        {
            var budget = 4096;
            while ((!_state.Events.IsEmpty || support.HasBroken) && --budget > 0)
            {
                transactions.Resolve(() =>
                {
                    if (support.HasBroken)
                    {
                        var broken = support.TakeBroken();
                        transactions.Continue(broken.ChainId.Length > 0 ? broken.ChainId : $"shield:{broken.Target}:{TickIndex}",
                            broken.Depth, () => TraitOnBroken(broken));
                    }
                    else
                    {
                        var fact = _state.Events[0];
                        _state = _state with { Events = _state.Events.RemoveAt(0) };
                        transactions.Continue(fact.ChainId, fact.Depth + 1, () => TraitMechanicsOnEvent(fact));
                    }
                });
            }
            if (budget <= 0) throw new InvalidOperationException("Trait reaction budget exceeded.");
        }
        finally { _draining = false; }
    }

    public float Modify(BattleCombatCalculationRequest r, float amount)
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
        if (TraitCounter($"copy_damage:{source.RuntimeId}") is > 0 and var factor) amount *= factor;
        foreach (var b in _traitMechanicBindings.Where(b => IsMember(b, source.RuntimeId)))
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
                amount *= 1 + elements.ReadEmber(target.RuntimeId) * m.Amount;
        }
        if (source.IsTemporary)
            foreach (var b in TraitFor(source.SummonerRuntimeId, TraitMechanicKind.OwnedSummonBonus)) amount *= 1 + b.Spec.Secondary;
        // The poison debuff belongs to the affected enemy, not to membership of its source.
        foreach (var b in _traitMechanicBindings.Where(b => b.Spec.Kind == TraitMechanicKind.PoisonOnAttack &&
                     b.Spec.Extra > 0 && source.Team != b.Team))
            if (elements.ReadPoison(source.RuntimeId, b.Team) >= b.Spec.Limit) amount *= 1 - b.Spec.Extra;
        if (TraitCounter($"protection:{target.RuntimeId}") > TickIndex)
            amount *= 1 - TraitCounter($"protection_amount:{target.RuntimeId}");
        if (r.DamageClass == CombatDamageClass.ActiveSkill)
            amount *= 1 + TraitCounter($"castboost_action:{r.Source.InstanceId}");
        return amount;
    }

    private void TraitMechanicsOnEvent(BattleCombatEvent e)
    {
        RosterTraitEvent(e);
        var source = TraitUnit(e.SourceRuntimeId);
        var target = TraitUnit(e.TargetRuntimeId);
        if (e.Kind == BattleCombatEventKind.BattleStarted)
        {
            foreach (var b in _traitMechanicBindings.Where(b => b.Spec.Kind == TraitMechanicKind.BarrierRetaliation))
            foreach (var u in TraitLiving(b)) support.ApplyShield(u.RuntimeId, u.RuntimeId,
                u.MaxHealth * b.Spec.Amount, b.Spec.DurationTicks, TraitOrigin(b, u.RuntimeId));
            return;
        }
        foreach (var b in _traitMechanicBindings)
        {
            var m = b.Spec;
            var sourceMember = source is not null && IsMember(b, source.RuntimeId);
            var targetMember = target is not null && IsMember(b, target.RuntimeId);
            if (e.Kind == BattleCombatEventKind.HealthLost && targetMember && target is { Alive: true } &&
                source is not null && source.Team != target.Team)
            {
                if (m.Kind == TraitMechanicKind.HealthThresholdRescue &&
                    (e.TargetHealthRatio >= 0 ? e.TargetHealthRatio : target.Health / target.MaxHealth) <= m.Threshold &&
                    TraitCounter(TraitKey(b, target.RuntimeId, "used")) == 0)
                {
                    TraitWrite(TraitKey(b, target.RuntimeId, "used"), 1);
                    support.ApplyShield(target.RuntimeId, target.RuntimeId, target.MaxHealth * m.Amount,
                        m.DurationTicks, TraitOrigin(b, target.RuntimeId));
                    var ally = TraitNear(target, target.Team, float.MaxValue).FirstOrDefault(u => !u.IsTemporary && !IsMember(b, u.RuntimeId) &&
                        TraitCounter(TraitKey(b, u.RuntimeId, "received")) < m.Limit);
                    if (ally is not null)
                    {
                        TraitWrite(TraitKey(b, ally.RuntimeId, "received"), TraitCounter(TraitKey(b, ally.RuntimeId, "received")) + 1);
                        support.ApplyShield(target.RuntimeId, ally.RuntimeId, ally.MaxHealth * m.Secondary,
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
                commands.Heal(source.RuntimeId, source.RuntimeId, e.EffectiveValue * m.Secondary, TraitOrigin(b, source.RuntimeId));
            if (targetMember && target is { Alive: true } && m.Kind == TraitMechanicKind.LowHealthLeech &&
                m.Extra > 0 && (e.TargetHealthRatio >= 0 ? e.TargetHealthRatio : target.Health / target.MaxHealth) < .3f &&
                TraitCounter(TraitKey(b, target.RuntimeId, "rescue")) == 0 &&
                e.Kind == BattleCombatEventKind.HealthLost)
            {
                TraitWrite(TraitKey(b, target.RuntimeId, "rescue"), 1);
                support.ApplyShield(target.RuntimeId, target.RuntimeId, target.MaxHealth * m.Extra,
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
                support.ApplyShield(source.RuntimeId, source.RuntimeId, source.MaxHealth * m.Secondary,
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
                e.Reason != "Consumed" && !_state.ExcludedDeaths.Contains(target.RuntimeId))
                TraitOnDeath(b, target);
        }
    }

    public void BeginAbility(string ownerId, string actionId, bool actualManaSkill)
    {
        if (!actualManaSkill || string.IsNullOrEmpty(actionId)) return;
        foreach (var b in TraitFor(ownerId, TraitMechanicKind.CastStartShield))
            if (TraitUnit(ownerId) is { Alive: true } unit)
                support.RefreshShield(ownerId, ownerId, unit.MaxHealth * b.Spec.Amount, b.Spec.DurationTicks, TraitOrigin(b, ownerId));
        var boost = TraitCounter($"castboost:{ownerId}");
        if (boost <= 0) return;
        TraitWrite($"castboost_action:{actionId}", boost);
        TraitWrite($"castboost:{ownerId}", 0);
    }

    private void TraitOnAttack(BattleTraitBinding b, BattleUnitView source, BattleUnitView target, BattleCombatEvent e)
    {
        var m = b.Spec;
        var action = string.IsNullOrEmpty(e.ActionId) ? $"{e.Tick}:{source.RuntimeId}" : e.ActionId;
        var seen = TraitKey(b, source.RuntimeId, "attack_action");
        if (_state.Targets.GetValueOrDefault(seen) == action) return;
        _state = _state with { Targets = _state.Targets.SetItem(seen, action) };
        if (m.Kind == TraitMechanicKind.RepeatedTargetAttack)
        {
            var lastKey = TraitKey(b, source.RuntimeId, "last_target");
            var countKey = TraitKey(b, source.RuntimeId, "hits");
            var last = _state.Targets.GetValueOrDefault(lastKey);
            var count = TraitCounter(countKey);
            if (last != target.RuntimeId) count = m.Enabled && TraitUnit(last ?? "") is { Alive: false } ? Math.Min(2, count) : 0;
            count++;
            _state = _state with { Targets = _state.Targets.SetItem(lastKey, target.RuntimeId) };
            if (count >= m.Count)
            {
                count = 0;
                if (target.Alive) commands.Damage(source.RuntimeId, target.RuntimeId, source.Damage * m.Amount,
                    TraitOrigin(b, source.RuntimeId), damageClass: CombatDamageClass.Derived);
            }
            TraitWrite(countKey, count);
        }
        if (m.Kind == TraitMechanicKind.DamageTakenRamp && m.Secondary > 0 &&
            TraitCounter(TraitKey(b, source.RuntimeId, "stacks")) >= m.Limit &&
            TraitReady(TraitKey(b, source.RuntimeId, "heal_cd"), m.CooldownTicks))
            commands.Heal(source.RuntimeId, source.RuntimeId, source.MaxHealth * m.Secondary, TraitOrigin(b, source.RuntimeId));
        if (!target.Alive) return;
        if (m.Kind == TraitMechanicKind.ChillOnAttack)
        {
            if (TraitReady(TraitKey(b, source.RuntimeId, "chill_cd"), m.CooldownTicks)) elements.ApplyChill(source.RuntimeId, target.RuntimeId, 1);
            if (m.Count >= 3 && elements.ChillInstance(target.RuntimeId) is { } chillInstance && elements.ReadChill(target.RuntimeId) >= 3 &&
                TraitCounter($"freeze_cd:{target.RuntimeId}") <= TickIndex)
            {
                var result = statuses.Apply(BattleMechanicStatus.Create("matrix_freeze", "冻结", Math.Max(1, m.DurationTicks),
                    StatusBehaviorKind.DisableActions, harmful: true), source.RuntimeId, target.RuntimeId, TickIndex);
                if (result.Applied)
                {
                    TraitWrite($"freeze_cd:{target.RuntimeId}", TickIndex + 30);
                    elements.ClearChill(target.RuntimeId, chillInstance);
                    if (m.Enabled)
                        foreach (var other in TraitNear(target, target.Team, 2).Take(2)) elements.ApplyChill(source.RuntimeId, other.RuntimeId, 1);
                }
            }
        }
        if (m.Kind == TraitMechanicKind.PoisonOnAttack && TraitReady(TraitKey(b, source.RuntimeId, "poison_cd"), m.CooldownTicks))
            elements.ApplyPoison(source.RuntimeId, target.RuntimeId, 1);
    }

    private void TraitOnActiveHit(BattleTraitBinding b, BattleUnitView source, BattleUnitView target, BattleCombatEvent e)
    {
        var action = string.IsNullOrEmpty(e.ActionId) ? $"{e.Source.InstanceId}:{e.Tick}" : e.ActionId;
        var key = TraitKey(b, target.RuntimeId, $"mark_action:{source.RuntimeId}:{action}");
        if (TraitCounter(key) > 0) return;
        TraitWrite(key, 1);
        if (elements.ReadEmber(target.RuntimeId) >= b.Spec.Count && b.Spec.Secondary > 0 &&
            TraitReady($"ember_cd:{target.RuntimeId}", b.Spec.CooldownTicks))
        {
            elements.ConsumeEmber(target.RuntimeId, int.MaxValue);
            var damage = Math.Max(source.Damage, source.Attributes.GetValue(CombatAttribute.SpellPower)) * b.Spec.Secondary;
            commands.Damage(source.RuntimeId, target.RuntimeId, damage, TraitOrigin(b, source.RuntimeId), damageClass: CombatDamageClass.Derived);
            if (b.Spec.Extra > 0)
                foreach (var other in TraitNear(target, target.Team, 1.5f).ToArray())
                    commands.Damage(source.RuntimeId, other.RuntimeId, damage * b.Spec.Extra, TraitOrigin(b, source.RuntimeId), damageClass: CombatDamageClass.Derived, isAreaDamage: true);
            commands.Publish(new(BattleCombatEventKind.StatusStackChanged, TraitOrigin(b, source.RuntimeId), source.RuntimeId,
                target.RuntimeId, TickIndex, SubjectStableId: "matrix_ember_detonation", PreviousStacks: b.Spec.Count));
        }
        if (target.Alive) elements.ApplyEmber(source.RuntimeId, target.RuntimeId, 1);
    }

    private void TraitOnCast(BattleTraitBinding b, BattleUnitView source, BattleCombatEvent e)
    {
        var m = b.Spec;
        if (m.Kind == TraitMechanicKind.ActiveDamageAndRefund && m.Secondary > 0)
        {
            var key = TraitKey(b, source.RuntimeId, "casts");
            var count = TraitCounter(key) + 1;
            TraitWrite(key, count);
            if ((count - 1) % Math.Max(1, m.Count) == 0)
                support.GrantMana(source.RuntimeId, source.RuntimeId, e.EffectiveValue * m.Secondary, true);
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
            support.GrantMana(source.RuntimeId, u.RuntimeId, m.Amount, true);
            if (m.Secondary > 0) TraitWrite($"castboost:{u.RuntimeId}", m.Secondary);
        }
    }

    private void TraitOnDeath(BattleTraitBinding b, BattleUnitView dead)
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
            support.GrantMana(owner.RuntimeId, owner.RuntimeId, gain, true);
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

    private void TraitSpendSouls(BattleTraitBinding b)
    {
        var m = b.Spec;
        var souls = TraitKey(b, "team", "souls");
        if (TraitCounter(souls) < m.Count || TraitCounter(TraitKey(b, "team", "soul_cd")) > TickIndex) return;
        var recipients = TraitLiving(b).OrderBy(u => u.Health / u.MaxHealth).ThenBy(u => u.RuntimeId, StringComparer.Ordinal).Take(m.Limit).ToArray();
        if (recipients.Length == 0) return;
        TraitWrite(souls, TraitCounter(souls) - m.Count);
        TraitWrite(TraitKey(b, "team", "soul_cd"), TickIndex + m.CooldownTicks);
        foreach (var u in recipients) commands.Heal(u.RuntimeId, u.RuntimeId, u.MaxHealth * m.Amount, TraitOrigin(b, u.RuntimeId));
        if (!m.Enabled || string.IsNullOrEmpty(m.SummonContentId) ||
            _state.Products.Count(p => TraitUnit(p.Id) is { Alive: true } u && u.Team == b.Team) >= 2) return;
        if (!read.SummonProfiles.TryGetValue(m.SummonContentId, out var profile))
            throw new InvalidOperationException($"Missing trait summon profile '{m.SummonContentId}'.");
        var anchor = recipients[0];
        var id = commands.SpawnNear(profile, b.Team, anchor.Position, anchor.MaxHealth * .25f / profile.MaxHealth,
            anchor.Damage * .3f / Math.Max(.001f, profile.Damage), anchor.RuntimeId);
        if (id is not null)
        {
            _state = _state with { Products = _state.Products.Add(new(id, TickIndex + m.DurationTicks)),
                ExcludedDeaths = _state.ExcludedDeaths.Add(id) };
        }
    }

    private void TraitOnBroken(BattleBrokenShield broken)
    {
        var unit = TraitUnit(broken.Target);
        if (unit is null) return;
        commands.Publish(new(BattleCombatEventKind.StatusStackChanged, broken.Origin, broken.Origin.OwnerRuntimeId,
            unit.RuntimeId, TickIndex, EffectiveValue: broken.Absorbed, SubjectStableId: "matrix_shield_broken"));
        if (!unit.Alive) return;
        var derivedCounter = broken.Origin.Kind == CombatSourceKind.Trait && _traitMechanicBindings.Any(b =>
            b.Id == broken.Origin.StableId && b.Spec.Kind == TraitMechanicKind.BarrierRetaliation);
        foreach (var b in TraitFor(unit.RuntimeId, TraitMechanicKind.BarrierRetaliation))
        {
            var m = b.Spec;
            if (!derivedCounter && m.Secondary > 0 && TraitReady(TraitKey(b, unit.RuntimeId, "break_cd"), m.CooldownTicks))
                foreach (var enemy in TraitNear(unit, 1 - unit.Team, 1.5f).ToArray())
                    commands.Damage(unit.RuntimeId, enemy.RuntimeId, Math.Min(broken.Absorbed * m.Secondary, unit.Definition.Damage * 1.2f),
                        TraitOrigin(b, unit.RuntimeId), damageClass: CombatDamageClass.Derived, isAreaDamage: true);
            if (m.Extra > 0 && TraitCounter(TraitKey(b, unit.RuntimeId, "repair")) == 0)
            {
                TraitWrite(TraitKey(b, unit.RuntimeId, "repair"), 1);
                support.ApplyShield(unit.RuntimeId, unit.RuntimeId, unit.MaxHealth * m.Extra, 40, TraitOrigin(b, unit.RuntimeId));
            }
        }
    }

    private void TraitTemporaryAttribute(string source, string target, string id, CombatAttribute attribute, float amount, int ticks)
    {
        foreach (var status in statuses.SnapshotOwner(target).Where(s => s.StableId == id))
            statuses.MatrixRemoveInstance(status.InstanceId, TickIndex);
        statuses.Apply(BattleMechanicStatus.Create(id, "临时增益", ticks, modifiers:
            [new(attribute, AttributeModifierOperation.Multiply, new CompiledConstantMagnitude(1 + amount), 0, id)]), source, target, TickIndex);
    }

    public void BeforeDeath(string id)
    {
        var target = TraitUnit(id)!;
        elements.FreezeDeath(id);
        foreach (var b in _traitMechanicBindings.Where(b => b.Spec.Kind == TraitMechanicKind.PoisonOnAttack &&
                     b.Spec.Enabled && target.Team != b.Team))
        {
            var next = TraitNear(target, target.Team, 3).FirstOrDefault();
            if (next is not null) elements.TransferPoison(target.RuntimeId, next.RuntimeId, elements.ReadPoison(target.RuntimeId, b.Team) / 2, b.Team);
        }
        elements.RemoveLive(id);
    }

    public void OnSummoned(string id)
    {
        var unit = TraitUnit(id)!;
        foreach (var binding in TraitFor(unit.SummonerRuntimeId, TraitMechanicKind.OwnedSummonBonus))
            commands.BuffSummonHealth(id, binding.Spec.Amount, TraitOrigin(binding, unit.SummonerRuntimeId));
    }

    public bool ExtraSummon(string ownerId)
    {
        var b = TraitFor(ownerId, TraitMechanicKind.OwnedSummonBonus).FirstOrDefault(b => b.Spec.Enabled);
        if (b is null || TraitCounter(TraitKey(b, ownerId, "extra_summon")) > 0) return false;
        TraitWrite(TraitKey(b, ownerId, "extra_summon"), 1);
        return true;
    }

    public bool ExcludedDeath(string runtimeId) => _state.ExcludedDeaths.Contains(runtimeId);

    public void Advance()
    {
        if (!support.HasPending && !elements.HasPending && _state.Products.IsEmpty &&
            !_traitMechanicBindings.Any(b => b.Spec.Kind == TraitMechanicKind.DelayedTeamStrike && TraitCounter(TraitKey(b, "team", "fired")) == 0) &&
            !_traitMechanicBindings.Any(b => b.Spec.Kind == TraitMechanicKind.DeathResource &&
                TraitCounter(TraitKey(b, "team", "souls")) >= b.Spec.Count)) return;
        transactions.Resolve(() =>
        {
            AdvanceDelayedTeamStrike();
            support.Advance();
            elements.Advance();
            foreach (var b in _traitMechanicBindings.Where(b => b.Spec.Kind == TraitMechanicKind.DeathResource)) TraitSpendSouls(b);
            foreach (var product in _state.Products.Where(p => p.Expires <= TickIndex).ToArray())
                if (TraitUnit(product.Id) is { Alive: true } u)
                {
                    commands.Retire(u.RuntimeId, u.SummonerRuntimeId, "trait_product_expired");
                }
            _state = _state with { Products = _state.Products.Where(p => p.Expires > TickIndex && TraitUnit(p.Id) is { Alive: true }).ToImmutableArray() };
        });
    }
}
