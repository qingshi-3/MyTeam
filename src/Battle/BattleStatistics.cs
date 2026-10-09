using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Battle;

// All writes consume already-resolved facts at their original simulation boundary.
// No combat/event subscription and no mutable statistics escape this owner.
internal sealed class BattleStatistics(bool captureDetails = false)
{
    private const int MaxSourceBuckets = 64;
    private int _tick;
    private readonly Dictionary<string, BattleUnitStatistics> _statistics = new(StringComparer.Ordinal);
    private ImmutableArray<BattleUnitReportSnapshot> _terminalReports;
    public bool CapturesDetails => captureDetails;
    public void AdvanceTick(int tick) { if (captureDetails) _tick = tick; }
    public void Join(string id, int tick) => _statistics.Add(id, new() { JoinTick = tick, Details = captureDetails ? new() { AliveSince = tick } : null });
    public void Attack(string id) => _statistics[id].AttackActions++;
    public int AttackActions(string id) => _statistics[id].AttackActions;
    public int JoinTick(string id) => _statistics[id].JoinTick;
    public void Defeat(string id, int tick)
    {
        var unit = _statistics[id];
        unit.DefeatTick ??= tick;
        if (unit.Details is { AliveSince: >= 0 } details)
        {
            details.AliveTicks += Math.Max(0, tick - details.AliveSince);
            details.AliveSince = -1;
        }
    }
    public void Damage(string sourceId, string targetId, float effective, float absorbed, bool killed,
        float healthRemoved = 0, float overkill = 0, CombatSourceRef origin = default,
        CombatDamageClass damageClass = CombatDamageClass.Other, EffectDamageType damageType = EffectDamageType.Normal)
    {
        var target = _statistics[targetId];
        target.DamageTaken += effective;
        target.ShieldAbsorbed += absorbed;
        if (target.Details is { } received) received.HealthDamageTaken += healthRemoved;
        if (_statistics.TryGetValue(sourceId, out var source))
        {
            source.DamageDealt += effective;
            if (killed) source.Kills++;
            if (source.Details is { } details)
            {
                details.HealthDamageDealt += healthRemoved;
                details.ShieldDamageDealt += absorbed;
                details.OverkillDamageDealt += overkill;
                details.OwnDamageSources();
                var key = BoundedKey(details.DamageSources, new DamageKey(origin.Kind, origin.StableId ?? "", damageClass, damageType));
                var old = details.DamageSources.GetValueOrDefault(key);
                details.DamageSources[key] = new(old.Health + healthRemoved, old.Shield + absorbed, old.Overkill + overkill, old.Hits + 1);
            }
        }
    }
    public void Heal(string sourceId, float effective) => Heal(sourceId, "", effective, effective, default);
    public void Heal(string sourceId, string targetId, float effective, float resolved, CombatSourceRef origin)
    {
        if (_statistics.TryGetValue(targetId, out var target) && target.Details is { } received)
            received.HealingReceived += Math.Max(0, effective);
        if (!_statistics.TryGetValue(sourceId, out var source)) return;
        if (effective > 0) { source.HealingDone += effective; source.EffectiveHealingEvents++; }
        if (source.Details is not { } details) return;
        var wasted = Math.Max(0, resolved - Math.Max(0, effective));
        details.HealingWasted += wasted;
        details.OwnHealingSources();
        var key = BoundedKey(details.HealingSources, new HealingKey(origin.Kind, origin.StableId ?? ""));
        var old = details.HealingSources.GetValueOrDefault(key);
        details.HealingSources[key] = new(old.Effective + Math.Max(0, effective), old.Wasted + wasted, old.Events + 1);
    }
    public void Shield(string sourceId, float amount)
    {
        if (_statistics.TryGetValue(sourceId, out var source) && source.Details is { } details)
            details.ShieldGranted += Math.Max(0, amount);
    }
    // Read facts after successful publication; never subscribe, enqueue reactions or draw RNG.
    public void Observe(BattleCombatEventDraft fact)
    {
        if (!captureDetails) return;
        if (fact.Kind == BattleCombatEventKind.UnitRevived && _statistics.TryGetValue(fact.TargetRuntimeId, out var revived)
            && revived.Details is { AliveSince: < 0 } life) life.AliveSince = fact.Tick;
        var actorId = fact.Kind is BattleCombatEventKind.AbilityResolved or BattleCombatEventKind.ManaSkillResolved
            ? fact.TargetRuntimeId : fact.SourceRuntimeId;
        if (!_statistics.TryGetValue(actorId, out var actor) || actor.Details is not { } details) return;
        switch (fact.Kind)
        {
            case BattleCombatEventKind.AttackLanded: details.LandedAttacks++; break;
            case BattleCombatEventKind.AttackDodged: details.DodgedAttacks++; break;
            case BattleCombatEventKind.CriticalHit: details.CriticalHits++; break;
            case BattleCombatEventKind.AbilityResolved: details.AbilityResolutions++; break;
            case BattleCombatEventKind.ManaSkillResolved: details.ManaCasts++; break;
        }
    }
    public void ControlTick(string targetId, ImmutableArray<StatusRuntimeSnapshot> statuses, bool hardControl,
        bool legacyActionLock, int tick)
    {
        if (!captureDetails) return;
        var details = _statistics[targetId].Details!;
        if (legacyActionLock) details.LegacyActionLockTicks++;
        if (!hardControl) return;
        details.HardControlTicksTaken++;
        foreach (var status in statuses)
        {
            if (!status.GrantedTags.Contains(StatusDefinitionCompiler.ActionDisabledTag)) continue;
            if (status.SourceContributions.IsDefaultOrEmpty)
                Credit(status, status.SourceId);
            else foreach (var contribution in status.SourceContributions)
                if (contribution.Stacks > 0) Credit(status, contribution.SourceId);
        }
        void Credit(StatusRuntimeSnapshot status, string sourceId)
        {
            details.OwnControlSources();
            var key = BoundedKey(details.ControlSources, new ControlKey(status.StableId, status.DisplayName, sourceId));
            var old = details.ControlSources.GetValueOrDefault(key);
            if (old.LastTick != tick) details.ControlSources[key] = new(old.Ticks + 1, tick);
            if (_statistics.TryGetValue(sourceId, out var source) && source.Details is { } inflicted &&
                inflicted.ControlTargets.GetValueOrDefault(targetId, -1) != tick)
            {
                inflicted.OwnControlTargets();
                inflicted.ControlTargets[targetId] = tick;
                inflicted.HardControlTicksDealt++;
            }
        }
    }
    private static DamageKey BoundedKey(Dictionary<DamageKey, DamageValue> values, DamageKey key) =>
        values.ContainsKey(key) || values.Count < MaxSourceBuckets ? key : DamageKey.Overflow;
    private static HealingKey BoundedKey(Dictionary<HealingKey, HealingValue> values, HealingKey key) =>
        values.ContainsKey(key) || values.Count < MaxSourceBuckets ? key : HealingKey.Overflow;
    private static ControlKey BoundedKey(Dictionary<ControlKey, ControlValue> values, ControlKey key) =>
        values.ContainsKey(key) || values.Count < MaxSourceBuckets ? key : ControlKey.Overflow;
    public ImmutableArray<BattleUnitReportSnapshot> ReadReports(IEnumerable<BattleUnitView> units, Func<BattleUnitView, int> activeTicks) =>
        _terminalReports.IsDefault ? BuildReports(units, activeTicks) : _terminalReports;
    public void Freeze(IEnumerable<BattleUnitView> units, Func<BattleUnitView, int> activeTicks)
    {
        if (_terminalReports.IsDefault) _terminalReports = BuildReports(units, activeTicks);
    }
    public Checkpoint Capture() => new(this);
    public void Restore(Checkpoint checkpoint) => checkpoint.Restore(this);

    internal sealed class Checkpoint
    {
        private readonly BattleStatistics _owner;
        private readonly Dictionary<string, BattleUnitStatistics> _states;
        private readonly ImmutableArray<BattleUnitReportSnapshot> _reports;
        private readonly int _tick;
        internal Checkpoint(BattleStatistics owner)
        {
            _owner = owner;
            _states = owner._statistics.ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), StringComparer.Ordinal);
            _reports = owner._terminalReports;
            _tick = owner._tick;
        }
        internal void Restore(BattleStatistics owner)
        {
            if (!ReferenceEquals(owner, _owner)) throw new InvalidOperationException("Statistics checkpoint belongs to another battle.");
            owner._statistics.Clear();
            foreach (var pair in _states) owner._statistics.Add(pair.Key, pair.Value.Clone());
            owner._terminalReports = _reports;
            owner._tick = _tick;
        }
    }

    private ImmutableArray<BattleUnitReportSnapshot> BuildReports(IEnumerable<BattleUnitView> units, Func<BattleUnitView, int> activeTicks) =>
        units.Select(unit =>
        {
            var statistics = _statistics[unit.RuntimeId];
            return new BattleUnitReportSnapshot(
                unit.RuntimeId,
                unit.SourceInstanceId,
                unit.Definition.ContentId,
                unit.Definition.DisplayName,
                unit.Definition.Role,
                unit.InitialTeam >= 0 ? unit.InitialTeam : unit.Team,
                unit.Definition.IsHero,
                unit.IsTemporary,
                unit.Alive,
                unit.Cell,
                unit.Health,
                unit.MaxHealth,
                unit.Shield,
                unit.Damage,
                statistics.DamageDealt,
                statistics.DamageTaken,
                statistics.ShieldAbsorbed,
                statistics.HealingDone,
                statistics.Kills,
                statistics.JoinTick,
                statistics.DefeatTick,
                statistics.AttackActions,
                statistics.EffectiveHealingEvents,
                unit.Position, activeTicks(unit), statistics.Details?.Snapshot(_tick));
        }).ToImmutableArray();

    private sealed class BattleUnitStatistics
    {
        public float DamageDealt { get; set; }
        public float DamageTaken { get; set; }
        public float ShieldAbsorbed { get; set; }
        public float HealingDone { get; set; }
        public int Kills { get; set; }
        public int JoinTick { get; init; }
        public int? DefeatTick { get; set; }
        public int AttackActions { get; set; }
        public int EffectiveHealingEvents { get; set; }
        public DetailsState? Details { get; set; }
        public BattleUnitStatistics Clone()
        {
            var clone = (BattleUnitStatistics)MemberwiseClone();
            clone.Details = Details?.Clone();
            return clone;
        }
    }

    private readonly record struct DamageKey(CombatSourceKind Kind, string Id, CombatDamageClass Class, EffectDamageType Type, bool IsOverflow = false)
    { public static DamageKey Overflow => new(CombatSourceKind.None, "", CombatDamageClass.Other, EffectDamageType.Normal, true); }
    private readonly record struct DamageValue(float Health, float Shield, float Overkill, int Hits);
    private readonly record struct HealingKey(CombatSourceKind Kind, string Id, bool IsOverflow = false)
    { public static HealingKey Overflow => new(CombatSourceKind.None, "", true); }
    private readonly record struct HealingValue(float Effective, float Wasted, int Events);
    private readonly record struct ControlKey(string Id, string Name, string Source, bool IsOverflow = false)
    { public static ControlKey Overflow => new("", "其他控制来源", "", true); }
    private readonly record struct ControlValue(int Ticks, int LastTick);
    private sealed class DetailsState
    {
        public float HealthDamageDealt, HealthDamageTaken, ShieldDamageDealt, OverkillDamageDealt;
        public float HealingWasted, HealingReceived, ShieldGranted;
        public int LandedAttacks, DodgedAttacks, CriticalHits, AbilityResolutions, ManaCasts;
        public int AliveTicks, AliveSince, HardControlTicksTaken, HardControlTicksDealt, LegacyActionLockTicks;
        public Dictionary<DamageKey, DamageValue> DamageSources = [];
        public Dictionary<HealingKey, HealingValue> HealingSources = [];
        public Dictionary<ControlKey, ControlValue> ControlSources = [];
        public Dictionary<string, int> ControlTargets = new(StringComparer.Ordinal);
        private bool _damageShared, _healingShared, _controlShared, _targetsShared;
        // Checkpoints share immutable-until-written bucket maps. Only the metric actually
        // mutated copies its map; frequent transactions do not clone every source of every unit.
        public DetailsState Clone()
        {
            _damageShared = _healingShared = _controlShared = _targetsShared = true;
            return (DetailsState)MemberwiseClone();
        }
        public void OwnDamageSources() { if (_damageShared) { DamageSources = new(DamageSources); _damageShared = false; } }
        public void OwnHealingSources() { if (_healingShared) { HealingSources = new(HealingSources); _healingShared = false; } }
        public void OwnControlSources() { if (_controlShared) { ControlSources = new(ControlSources); _controlShared = false; } }
        public void OwnControlTargets() { if (_targetsShared) { ControlTargets = new(ControlTargets, StringComparer.Ordinal); _targetsShared = false; } }
        public BattleUnitDetailedStatisticsSnapshot Snapshot(int tick) => new(
            HealthDamageDealt, HealthDamageTaken, ShieldDamageDealt, OverkillDamageDealt,
            HealingWasted, HealingReceived, ShieldGranted, LandedAttacks, DodgedAttacks, CriticalHits,
            AbilityResolutions, ManaCasts, AliveTicks + (AliveSince < 0 ? 0 : Math.Max(0, tick - AliveSince)),
            HardControlTicksTaken, HardControlTicksDealt, LegacyActionLockTicks,
            DamageSources.OrderBy(p => p.Key.Kind).ThenBy(p => p.Key.Id, StringComparer.Ordinal).ThenBy(p => p.Key.Class).ThenBy(p => p.Key.Type)
                .Select(p => new BattleDamageSourceSnapshot(p.Key.Kind, p.Key.Id, p.Key.Class, p.Key.Type,
                    p.Value.Health, p.Value.Shield, p.Value.Overkill, p.Value.Hits, p.Key.IsOverflow)).ToImmutableArray(),
            HealingSources.OrderBy(p => p.Key.Kind).ThenBy(p => p.Key.Id, StringComparer.Ordinal)
                .Select(p => new BattleHealingSourceSnapshot(p.Key.Kind, p.Key.Id, p.Value.Effective, p.Value.Wasted, p.Value.Events, p.Key.IsOverflow)).ToImmutableArray(),
            ControlSources.OrderBy(p => p.Key.Id, StringComparer.Ordinal).ThenBy(p => p.Key.Source, StringComparer.Ordinal)
                .Select(p => new BattleControlSourceSnapshot(p.Key.Id, p.Key.Name, p.Key.Source, p.Value.Ticks, p.Key.IsOverflow)).ToImmutableArray());
    }
}
