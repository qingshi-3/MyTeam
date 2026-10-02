using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TowerAutobattler.Battle;

// All writes consume already-resolved facts at their original simulation boundary.
// No combat/event subscription and no mutable statistics escape this owner.
internal sealed class BattleStatistics
{
    private readonly Dictionary<string, BattleUnitStatistics> _statistics = new(StringComparer.Ordinal);
    private ImmutableArray<BattleUnitReportSnapshot> _terminalReports;
    public void Join(string id, int tick) => _statistics.Add(id, new() { JoinTick = tick });
    public void Attack(string id) => _statistics[id].AttackActions++;
    public int AttackActions(string id) => _statistics[id].AttackActions;
    public int JoinTick(string id) => _statistics[id].JoinTick;
    public void Defeat(string id, int tick) => _statistics[id].DefeatTick ??= tick;
    public void Damage(string sourceId, string targetId, float effective, float absorbed, bool killed)
    {
        var target = _statistics[targetId];
        target.DamageTaken += effective;
        target.ShieldAbsorbed += absorbed;
        if (_statistics.TryGetValue(sourceId, out var source))
        {
            source.DamageDealt += effective;
            if (killed) source.Kills++;
        }
    }
    public void Heal(string sourceId, float effective)
    {
        if (!(effective > 0) || !_statistics.TryGetValue(sourceId, out var source)) return;
        source.HealingDone += effective;
        source.EffectiveHealingEvents++;
    }
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
        private readonly Dictionary<string, BattleUnitStatisticsState> _states;
        private readonly ImmutableArray<BattleUnitReportSnapshot> _reports;
        internal Checkpoint(BattleStatistics owner)
        {
            _owner = owner;
            _states = owner._statistics.ToDictionary(pair => pair.Key, pair => new BattleUnitStatisticsState(pair.Value), StringComparer.Ordinal);
            _reports = owner._terminalReports;
        }
        internal void Restore(BattleStatistics owner)
        {
            if (!ReferenceEquals(owner, _owner)) throw new InvalidOperationException("Statistics checkpoint belongs to another battle.");
            owner._statistics.Clear();
            foreach (var pair in _states) owner._statistics.Add(pair.Key, pair.Value.Restore());
            owner._terminalReports = _reports;
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
                unit.Position, activeTicks(unit));
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
    }

    private sealed record BattleUnitStatisticsState(
        float DamageDealt,
        float DamageTaken,
        float ShieldAbsorbed,
        float HealingDone,
        int Kills,
        int JoinTick,
        int? DefeatTick,
        int AttackActions,
        int EffectiveHealingEvents)
    {
        internal BattleUnitStatisticsState(BattleUnitStatistics statistics) : this(
            statistics.DamageDealt,
            statistics.DamageTaken,
            statistics.ShieldAbsorbed,
            statistics.HealingDone,
            statistics.Kills,
            statistics.JoinTick,
            statistics.DefeatTick,
            statistics.AttackActions,
            statistics.EffectiveHealingEvents)
        {
        }

        internal BattleUnitStatistics Restore() => new()
        {
            DamageDealt = DamageDealt,
            DamageTaken = DamageTaken,
            ShieldAbsorbed = ShieldAbsorbed,
            HealingDone = HealingDone,
            Kills = Kills,
            JoinTick = JoinTick,
            DefeatTick = DefeatTick,
            AttackActions = AttackActions,
            EffectiveHealingEvents = EffectiveHealingEvents
        };
    }
}
