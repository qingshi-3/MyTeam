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

internal sealed class BattleValueLedger(Func<int> tick, Func<IReadOnlyList<BattleUnitState>> units, BattleStatistics statistics)
{
    private sealed record ValueContribution(string SourceId, string TargetId, CombatAttribute Attribute,
        float Amount, AttributeModifierHandle Handle, bool Broadcast);
    private ImmutableDictionary<string, float> _counters = ImmutableDictionary<string, float>.Empty;
    private ImmutableDictionary<string, ValueContribution> _contributions = ImmutableDictionary<string, ValueContribution>.Empty;
    internal sealed class Checkpoint
    {
        private readonly BattleValueLedger _owner;
        private readonly ImmutableDictionary<string, float> _counters;
        private readonly ImmutableDictionary<string, ValueContribution> _contributions;
        internal Checkpoint(BattleValueLedger owner)
        { _owner = owner; _counters = owner._counters; _contributions = owner._contributions; }
        internal void Restore(BattleValueLedger owner)
        {
            if (!ReferenceEquals(owner, _owner)) throw new InvalidOperationException("Checkpoint belongs to another value ledger.");
            // The world checkpoint restores AttributeSet state before restoring this handle index.
            owner._counters = _counters; owner._contributions = _contributions;
        }
    }
    public Checkpoint Capture() => new(this);
    public void Restore(Checkpoint checkpoint) => checkpoint.Restore(this);
    public void Clear() { _counters = _counters.Clear(); _contributions = _contributions.Clear(); }
    public float IntrinsicArmor(BattleUnitState unit) => unit.Armor - _contributions.Values
        .Where(c => c.Broadcast && c.TargetId == unit.RuntimeId && c.Attribute == CombatAttribute.Armor).Sum(c => c.Amount);
    public bool HasDeadBroadcasts => _contributions.Values.Any(c => c.Broadcast && !units().Any(u => u.RuntimeId == c.SourceId && u.Alive));
    public void CleanDeadBroadcasts() => RemoveWhere(p => p.Value.Broadcast && !units().Any(u => u.RuntimeId == p.Value.SourceId && u.Alive));
    public void ReleaseSource(string id) => RemoveWhere(p => p.Value.Broadcast && p.Value.SourceId == id);
    public void ReleaseAllegiance(string id) => RemoveWhere(p => p.Value.Broadcast && (p.Value.SourceId == id || p.Value.TargetId == id));
    public void RemoveObsoleteBroadcast(string prefix, ImmutableArray<string> targets) => RemoveWhere(p => p.Key.StartsWith(prefix, StringComparison.Ordinal) && !targets.Contains(p.Value.TargetId));
    private void RemoveWhere(Func<KeyValuePair<string, ValueContribution>, bool> predicate)
    {
        foreach (var pair in _contributions.Where(predicate).ToArray())
        {
            units().First(u => u.RuntimeId == pair.Value.TargetId).Attributes.Remove(pair.Value.Handle);
            _contributions = _contributions.Remove(pair.Key);
        }
    }
    private static string CounterAddress(BattleUnitState unit, string key, bool shared) =>
        shared ? $"team:{unit.Team}:{key}" : $"unit:{unit.RuntimeId}:{key}";
    public float ReadCounter(BattleUnitState unit, string key, bool shared = false) =>
        _counters.GetValueOrDefault(CounterAddress(unit,key,shared));
    public void WriteCounter(BattleUnitState unit, string key, float value, bool shared = false)
    {
        if (!float.IsFinite(value)) throw new InvalidOperationException("Battle counter overflow.");
        _counters = _counters.SetItem(CounterAddress(unit,key,shared), Math.Max(0,value));
    }

    public void SetValueContribution(string key, BattleUnitState source, BattleUnitState target, CombatAttribute attribute,
        float amount, bool broadcast, CombatSourceRef origin)
    {
        if (_contributions.TryGetValue(key,out var old))
        {
            if (Math.Abs(old.Amount-amount) < .00001f) return;
            target.Attributes.Remove(old.Handle);
        }
        var handle = target.Attributes.ApplyModifier(new CompiledAttributeModifier(attribute,AttributeModifierOperation.Add,
            new CompiledConstantMagnitude(amount),0,key),origin);
        _contributions = _contributions.SetItem(key,new(source.RuntimeId,target.RuntimeId,attribute,amount,handle,broadcast));
    }

    private static string ResourceLabel(string key) => key switch {
        "grit" => "怒劲", "surge" => "全队共鸣", "shield_gifts" => "受盾次数", "wounds" => "蓄痛", "frost_history" => "历霜",
        "death_power" => "送葬", "eaten_attack" => "吞食攻击", "eaten_health" => "吞食生命",
        "raised_corpses" => "已用敌尸", "revivals" => "返场次数", "spent_shield" => "上次耗盾", _ => key };

    public void RecordBattleRevival(BattleUnitState unit)
    {
        var waiting = Math.Max(0,tick()-ReadCounter(unit,"_dead_since"));
        WriteCounter(unit,"_inactive_ticks",ReadCounter(unit,"_inactive_ticks")+waiting);
    }

    public int ActiveBattleTicks(BattleUnitView unit) => Math.Max(1,
        tick()-statistics.JoinTick(unit.RuntimeId)-(int)_counters.GetValueOrDefault($"unit:{unit.RuntimeId}:_inactive_ticks")-
        (unit.Alive ? 0 : Math.Max(0,tick()-(int)_counters.GetValueOrDefault($"unit:{unit.RuntimeId}:_dead_since"))));

    public string DescribeBattleResources(string runtimeId)
    {
        var unit = units().FirstOrDefault(u => u.RuntimeId == runtimeId);
        if (unit is null) return "";
        var own = $"unit:{runtimeId}:";
        var team = $"team:{unit.Team}:";
        return string.Join("  ",_counters.Where(p => (p.Key.StartsWith(own,StringComparison.Ordinal) || p.Key.StartsWith(team,StringComparison.Ordinal)) && !p.Key[(p.Key.LastIndexOf(':')+1)..].StartsWith("_",StringComparison.Ordinal))
            .OrderBy(p => p.Key,StringComparer.Ordinal).Select(p => $"{ResourceLabel(p.Key[(p.Key.LastIndexOf(':')+1)..])} {p.Value:0.#}"));
    }
}
