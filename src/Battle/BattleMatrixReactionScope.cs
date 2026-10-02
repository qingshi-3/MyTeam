using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Battle;

internal interface IBattleMatrixReactionWorld : IBattleMatrixReadWorld
{
    int Tick { get; }
    int MaximumReactions { get; }
    bool ExcludedDeath(string id);
    void WriteCounter(string ownerId, string key, float value);
    void PruneBeforeFact();
    void PruneBuffs();
    void ResolveReaction(BattleCombatEvent? fact, Action work);
    void Execute(CompiledMatrixOperation operation, string ownerId, string targetId,
        CombatSourceRef origin, BattleCombatEvent? fact, string slot);
}

// Owns registration, bounded event draining, eligibility, cooldowns, once-per-X
// leases, periodic reactions and death-gift copies. The host commits effects through
// its existing world transaction, which also captures this module's checkpoint.
internal sealed class BattleMatrixReactionScope(IBattleMatrixReactionWorld world, BattleMatrixQueries queries)
{
    internal sealed record Rule(string Owner, string Key, CompiledMatrixOperation Operation, CombatSourceRef Origin);
    internal sealed record State(ImmutableArray<Rule> Rules, ImmutableArray<BattleCombatEvent> Pending,
        ImmutableDictionary<string, float> Counters, ImmutableDictionary<string, int> Ready,
        ImmutableHashSet<string> Used, ImmutableDictionary<string, ImmutableHashSet<MatrixCondition>> DeathConditions)
    {
        internal static State Empty => new([], [], ImmutableDictionary<string, float>.Empty,
            ImmutableDictionary<string, int>.Empty, ImmutableHashSet<string>.Empty,
            ImmutableDictionary<string, ImmutableHashSet<MatrixCondition>>.Empty);
    }
    internal readonly record struct Checkpoint(BattleMatrixReactionScope Owner, State Value);
    private readonly IBattleMatrixReactionWorld _world = world;
    private readonly BattleMatrixQueries _queries = queries;
    private State _state = State.Empty;
    private bool _draining;

    internal bool HasPendingOrPeriodic => !_state.Pending.IsEmpty || _state.Rules.Any(r => r.Operation.Event == MatrixEventKind.Tick);
    internal Checkpoint Capture() => new(this, _state);
    internal void Restore(Checkpoint checkpoint)
    {
        if (!ReferenceEquals(checkpoint.Owner, this)) throw new InvalidOperationException("Matrix reaction checkpoint belongs to another battle.");
        _state = checkpoint.Value;
    }
    internal void Clear() => _state = State.Empty;
    internal void Register(string owner, string key, CompiledMatrixOperation operation, CombatSourceRef origin)
    {
        if (!_state.Rules.Any(r => r.Key == key)) _state = _state with { Rules = _state.Rules.Add(new(owner, key, operation, origin)) };
    }
    internal bool WantsEvent(BattleCombatEvent fact) => !_state.Rules.IsEmpty &&
        (fact.Kind == BattleCombatEventKind.UnitDefeated || _state.Rules.Any(r => MatrixEventMatches(r.Operation.Event, fact)));
    internal void Observe(BattleCombatEvent fact)
    {
        if (_state.Rules.IsEmpty) return;
        if (_state.Pending.Length >= _world.MaximumReactions) throw new InvalidOperationException("Matrix reaction queue exceeded its bounded budget.");
        _state = _state with { Pending = _state.Pending.Add(fact) };
        if (fact.Kind == BattleCombatEventKind.ControlApplied)
            _state = _state with { Counters = _state.Counters.SetItem($"control-window:{fact.TargetRuntimeId}", fact.Sequence) };
    }
    internal void BeforeDeath(string id)
    {
        var unit = _world.FindUnit(id);
        _state = _state with
        {
            DeathConditions = _state.DeathConditions.SetItem(id, Enum.GetValues<MatrixCondition>()
            .Where(condition => _queries.MatchesCondition(condition, unit, .6f)).ToImmutableHashSet())
        };
    }
    internal void EchoDeathGift(IEnumerable<string> candidates, string explicitTarget)
    {
        var donor = candidates.FirstOrDefault(id => _state.Rules.Any(r => r.Owner == id && r.Key.Contains(":death-gift:", StringComparison.Ordinal)) &&
            !_state.Used.Contains($"death-gift-copy:{id}"));
        if (donor is null) return;
        _state = _state with { Used = _state.Used.Add($"death-gift-copy:{donor}") };
        foreach (var gift in _state.Rules.Where(r => r.Owner == donor && r.Key.Contains(":death-gift:", StringComparison.Ordinal)).ToArray())
            foreach (var child in gift.Operation.Effects) _world.Execute(child, donor, explicitTarget, gift.Origin, null, gift.Key);
    }
    internal void AdvancePeriodic()
    {
        foreach (var rule in _state.Rules.Where(r => r.Operation.Event == MatrixEventKind.Tick).ToArray()) RunRule(rule, null);
    }
    internal void Drain()
    {
        if (_draining) return;
        _draining = true;
        try
        {
            var budget = 0;
            while (!_state.Pending.IsEmpty)
            {
                if (++budget > _world.MaximumReactions) throw new InvalidOperationException("Matrix reaction budget exceeded.");
                var fact = _state.Pending[0];
                _state = _state with { Pending = _state.Pending.RemoveAt(0) };
                _world.PruneBeforeFact();
                foreach (var rule in _state.Rules.ToArray()) RunRule(rule, fact);
                if (fact.Kind == BattleCombatEventKind.UnitDefeated && _world.FindUnit(fact.TargetRuntimeId) is { } dead)
                    foreach (var rule in _state.Rules.Where(r => r.Owner == dead.RuntimeId))
                        foreach (var op in MatrixAbilityCompiler.Flatten(rule.Operation).Where(o => o.ClearOnDeath && o.Key.Length > 0))
                            _world.WriteCounter(dead.RuntimeId, op.Key, 0);
            }
        }
        finally { _draining = false; }
    }

    private bool MatrixRelationMatches(MatrixRelation relation, BattleUnitView owner, BattleUnitView? other) => relation switch
    {
        MatrixRelation.Any => true,
        MatrixRelation.Owner => other == owner,
        MatrixRelation.Ally => other?.Team == owner.Team,
        MatrixRelation.OtherAlly => other is not null && other != owner && other.Team == owner.Team,
        MatrixRelation.Enemy => other is not null && other.Team != owner.Team,
        MatrixRelation.OwnedSummon => other is not null && other.Team == owner.Team && other.SummonerRuntimeId == owner.RuntimeId,
        _ => false
    };

    private bool MatrixEventMatches(MatrixEventKind kind, BattleCombatEvent e) => kind switch
    {
        MatrixEventKind.AttackHit => e.Kind == BattleCombatEventKind.AttackLanded && e.DamageClass is CombatDamageClass.BasicAttack or CombatDamageClass.Other,
        MatrixEventKind.SkillHit => e.Kind == BattleCombatEventKind.SkillHitLanded && e.DamageClass == CombatDamageClass.ActiveSkill,
        MatrixEventKind.HealthLost => e.Kind == BattleCombatEventKind.HealthLost,
        MatrixEventKind.ShieldReceived => e.Kind == BattleCombatEventKind.ShieldResolved,
        MatrixEventKind.ShieldBroken => e.Kind == BattleCombatEventKind.StatusStackChanged && e.SubjectStableId == "matrix_shield_broken",
        MatrixEventKind.Healing => e.Kind == BattleCombatEventKind.HealingResolved,
        MatrixEventKind.ManaCast => e.Kind == BattleCombatEventKind.ManaSkillResolved && e.EffectiveValue > 0,
        MatrixEventKind.Death => e.Kind == BattleCombatEventKind.UnitDefeated && e.Reason is "" or "Consumed" && !_world.ExcludedDeath(e.TargetRuntimeId),
        MatrixEventKind.Control => e.Kind == BattleCombatEventKind.ControlApplied,
        MatrixEventKind.PoisonTransferred => e.Kind == BattleCombatEventKind.StatusStackChanged && e.SubjectStableId == "matrix_poison_transfer",
        MatrixEventKind.EmberDetonated => e.Kind == BattleCombatEventKind.StatusStackChanged && e.SubjectStableId == "matrix_ember_detonation",
        MatrixEventKind.SummonHit => e.Kind == BattleCombatEventKind.AttackLanded,
        MatrixEventKind.PoisonApplied => e.Kind == BattleCombatEventKind.StatusApplied && e.SubjectStableId == "matrix_poison",
        _ => false
    };

    private void RunRule(Rule rule, BattleCombatEvent? fact)
    {
        var op = rule.Operation; var owner = _world.FindUnit(rule.Owner);
        if (owner is null || (op.LivingOwnerRequired && !owner.Alive) || !_queries.MatchesCondition(op.OwnerCondition, owner, op.HealthThreshold)) return;
        if (op.RequiredCounter.Length > 0 && _world.Counter(owner.RuntimeId, op.RequiredCounter) < op.CounterThreshold) return;
        if (fact is null && op.Event != MatrixEventKind.Tick) return;
        var source = fact is null ? owner : _world.FindUnit(fact.SourceRuntimeId);
        var target = fact is null ? owner : _world.FindUnit(fact.TargetRuntimeId);
        if (fact is not null && (!MatrixEventMatches(op.Event, fact) || !MatrixRelationMatches(op.SourceRelation, owner, source) ||
            !MatrixRelationMatches(op.TargetRelation, owner, target) || fact.EffectiveValue < op.MinimumEventValue)) return;
        if (op.ActiveSkillOnly && (fact is null || fact.Source.Kind != CombatSourceKind.Ability ||
            source?.Definition.AbilityLoadout?.Find(fact.Source.StableId)?.IsActiveSkill != true)) return;
        if (op.Event == MatrixEventKind.PoisonApplied && target is not null && _world.Poison(target.RuntimeId) < op.CounterThreshold) return;
        var conditionMatches = fact?.Kind == BattleCombatEventKind.UnitDefeated && target is not null &&
            _state.DeathConditions.TryGetValue(target.RuntimeId, out var prior)
            ? prior.Contains(op.TargetCondition) : _queries.MatchesCondition(op.TargetCondition, target, op.HealthThreshold);
        if (fact?.Kind == BattleCombatEventKind.HealingResolved && op.TargetCondition == MatrixCondition.LowHealth && target is not null)
            conditionMatches = (fact.TargetHealthRatio >= 0 ? fact.TargetHealthRatio : (target.Health - fact.EffectiveValue) / target.MaxHealth) < op.HealthThreshold;
        if (!conditionMatches || op.TemporaryOnly && target?.IsTemporary != true) return;
        var counterpart = target == owner ? source : target;
        if (counterpart is not null && counterpart.Position.DistanceTo(owner.Position) > op.Range) return;
        var key = rule.Key + (op.PerTargetCooldown ? ":" + counterpart?.RuntimeId : "");
        if (_state.Ready.GetValueOrDefault(key) > _world.Tick) return;
        var window = op.TargetCondition == MatrixCondition.Frozen ? _state.Counters.GetValueOrDefault($"control-window:{target?.RuntimeId}") : 0;
        var unique = op.OncePerBattle ? rule.Key : op.OncePerSource ? $"{rule.Key}:source:{source?.RuntimeId}" : op.OncePerTarget ? $"{rule.Key}:target:{counterpart?.RuntimeId}:{window}" :
            op.OncePerAction ? $"{rule.Key}:action:{fact?.ActionId}" : "";
        if (unique.Length > 0 && _state.Used.Contains(unique)) return;
        if (op.OncePerAction && string.IsNullOrEmpty(fact?.ActionId)) return;
        _world.ResolveReaction(fact, () =>
        {
            var countKey = op.OncePerAction ? $"{rule.Key}:count:{fact!.ActionId}" : $"{rule.Key}:count";
            if (op.OncePerAction)
            {
                var hitKey = $"{countKey}:{target?.RuntimeId}";
                if (_state.Used.Contains(hitKey)) { return; }
                _state = _state with { Used = _state.Used.Add(hitKey) };
            }
            var count = _state.Counters.GetValueOrDefault(countKey) + 1;
            _state = _state with { Counters = _state.Counters.SetItem(countKey, count) };
            if ((int)count % op.Every != 0) { return; }
            _state = _state with { Ready = _state.Ready.SetItem(key, _world.Tick + Math.Max(op.CooldownTicks, op.Event == MatrixEventKind.Tick ? op.IntervalTicks : 0)) };
            if (unique.Length > 0) _state = _state with { Used = _state.Used.Add(unique) };
            var effectOrigin = rule.Origin with { InstanceId = $"{rule.Origin.InstanceId}:reaction:{fact?.Sequence ?? _world.Tick}" };
            _world.PruneBuffs();
            for (var i = 0; i < op.Effects.Length; i++)
            {
                var child = op.Effects[i];
                if (op.Event == MatrixEventKind.Tick && child.Kind == MatrixOperationKind.TimedAttribute)
                    child = child with { OwnerCondition = op.OwnerCondition, TargetCondition = op.TargetCondition, HealthThreshold = op.HealthThreshold };
                _world.Execute(child, owner.RuntimeId, counterpart?.RuntimeId ?? "", effectOrigin, fact, $"{rule.Key}:{i}");
            }
        });
    }

}
