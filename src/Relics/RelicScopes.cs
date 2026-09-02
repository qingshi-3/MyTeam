using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Relics;

public class RelicRunInstanceState
{
    public string InstanceId { get; init; } = string.Empty;
    public string ContentId { get; set; } = string.Empty;
    public int Stacks { get; set; } = 1;
    public int Charges { get; set; }
    public int Roll { get; set; }
    public List<RelicCounterStateSnapshot> Counters { get; set; } = [];
}

public sealed class RelicRunScope : IDisposable
{
    private readonly RelicRunKey _runKey;
    private readonly Dictionary<string, RuntimeInstance> _instances = new(StringComparer.Ordinal);
    private readonly HashSet<string> _appliedTransitions = new(StringComparer.Ordinal);
    private long _nextRegistrationSequence;
    private RelicRunScopeTransitionResult? _transition;

    public RelicRunScope(RelicRunKey runKey)
    {
        if (string.IsNullOrWhiteSpace(runKey.HeroId))
            throw new ArgumentException("Relic run owner requires a hero id.", nameof(runKey));
        if (runKey.FloorIndex < 0 || runKey.BattleNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(runKey));
        _runKey = runKey;
    }

    public RelicRunKey RunKey => _runKey;
    public int LiveRunInstanceCount => _instances.Count;
    public bool IsCompleted => _transition is not null;
    public RelicRunScopeTransitionResult? Transition => _transition;

    public IDisposable Activate(CompiledRelicDefinition definition, RelicRunInstanceState state)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(state);
        EnsureActive();
        ValidateState(definition, state);
        var instance = new RuntimeInstance(definition, state, ++_nextRegistrationSequence);
        if (!_instances.TryAdd(state.InstanceId, instance))
            throw new InvalidOperationException($"Duplicate relic instance id: {state.InstanceId}");
        return new Registration(this, state.InstanceId);
    }

    public RelicBattlePreparation PrepareBattle()
    {
        EnsureActive();
        var ordered = OrderedInstances();
        var snapshots = ordered.Select(instance => instance.BattleSnapshot()).ToImmutableArray();
        var sourceFingerprint = Fingerprint(ordered.Select(instance => instance.RunSnapshot()));
        return new RelicBattlePreparation(
            TransitionIdentity(_runKey, sourceFingerprint),
            _runKey,
            sourceFingerprint,
            snapshots,
            AggregateModifiers(snapshots));
    }

    public RelicRunApplyResult Apply(RelicBattleTransitionResult transition)
    {
        var validation = ValidateCore(transition, RelicBattleCompletionReason.PlayerVictory, out var ordered);
        if (!validation.Succeeded) return validation;

        _appliedTransitions.Add(transition.TransitionId);
        for (var index = 0; index < ordered.Length; index++)
        {
            var state = ordered[index].State;
            var next = transition.ProjectedInstances[index];
            state.Stacks = next.Stacks;
            state.Charges = next.Charges;
            state.Roll = next.Roll;
            state.Counters.Clear();
            state.Counters.AddRange(CanonicalCounters(next.Counters));
        }
        return new RelicRunApplyResult(
            true,
            string.Empty,
            transition.TransitionId,
            transition.GoldDelta,
            ordered.Select(instance => instance.RunSnapshot()).ToImmutableArray());
    }

    public RelicRunApplyResult Validate(
        RelicBattleTransitionResult transition,
        RelicBattleCompletionReason expectedReason)
    {
        var validation = ValidateCore(transition, expectedReason, out var ordered);
        if (!validation.Succeeded) return validation;
        return new RelicRunApplyResult(
            true,
            string.Empty,
            transition.TransitionId,
            transition.GoldDelta,
            ordered.Select(instance => instance.RunSnapshot()).ToImmutableArray());
    }

    public RelicRunScopeTransitionResult Complete(RelicRunCompletionReason reason)
    {
        if (_transition is not null) return _transition;
        if (reason == RelicRunCompletionReason.None) throw new ArgumentOutOfRangeException(nameof(reason));
        _instances.Clear();
        _appliedTransitions.Clear();
        _transition = new RelicRunScopeTransitionResult(_runKey, reason, LiveRunInstanceCount);
        return _transition;
    }

    public void Dispose()
    {
        if (_transition is null) Complete(RelicRunCompletionReason.Disposal);
    }

    private RuntimeInstance[] OrderedInstances() =>
        _instances.Values.OrderBy(instance => instance.Sequence).ToArray();

    private void Deactivate(string instanceId) => _instances.Remove(instanceId);

    private void EnsureActive()
    {
        if (_transition is not null) throw new InvalidOperationException("Relic run scope has completed.");
    }

    private RelicRunApplyResult ValidateCore(
        RelicBattleTransitionResult transition,
        RelicBattleCompletionReason expectedReason,
        out RuntimeInstance[] ordered)
    {
        ArgumentNullException.ThrowIfNull(transition);
        EnsureActive();
        ordered = OrderedInstances();
        if (expectedReason == RelicBattleCompletionReason.None)
            throw new ArgumentOutOfRangeException(nameof(expectedReason));
        if (transition.Reason != expectedReason)
            return Failed(transition, "Relic transition completion reason does not match the battle result.");
        if (transition.RunKey != _runKey)
            return Failed(transition, "Relic transition belongs to a different Run or battle step.");
        var currentFingerprint = Fingerprint(ordered.Select(instance => instance.RunSnapshot()));
        if (!string.Equals(transition.SourceFingerprint, currentFingerprint, StringComparison.Ordinal) ||
            !string.Equals(transition.TransitionId, TransitionIdentity(_runKey, currentFingerprint), StringComparison.Ordinal))
            return Failed(transition, "Relic transition source state does not match the active Run.");
        if (_appliedTransitions.Contains(transition.TransitionId))
            return Failed(transition, "Relic transition was already applied.");
        if (transition.RemainingBattleInstances != 0 || transition.RemainingCounters != 0 ||
            transition.RemainingSubscriptions != 0 || transition.RemainingModifierHandles != 0)
            return Failed(transition, "Relic transition retained mutable Battle state.");
        if (transition.ProjectedInstances.Length != ordered.Length)
            return Failed(transition, "Relic transition instance set does not match the active Run.");

        var counterValues = new Dictionary<(string InstanceId, string CounterId), int>();
        var counterDefinitions = new Dictionary<(string InstanceId, string CounterId), CompiledRelicReactiveCounter>();
        for (var index = 0; index < ordered.Length; index++)
        {
            var runtime = ordered[index];
            var next = transition.ProjectedInstances[index];
            if (next.InstanceId != runtime.State.InstanceId || next.ContentId != runtime.Definition.StableId ||
                next.Stacks != runtime.State.Stacks || next.Charges != runtime.State.Charges || next.Roll != runtime.State.Roll)
                return Failed(transition, "Relic transition contains an invalid instance projection.");
            if (!HasExactRunCounterSet(runtime.Definition, next.Counters, out _))
                return Failed(transition, "Relic transition contains an invalid Run-counter projection.");
            foreach (var counter in runtime.Definition.ReactiveCounters)
            {
                var key = (runtime.State.InstanceId, counter.CounterId);
                counterDefinitions.Add(key, counter);
                counterValues.Add(key, counter.Scope == RelicCounterScope.Run
                    ? runtime.State.Counters.Single(value => value.CounterId == counter.CounterId).Value
                    : 0);
            }
        }

        long previousSequence = 0;
        long previousEventSequence = 0;
        foreach (var counterTransition in transition.CounterTransitions)
        {
            var key = (counterTransition.InstanceId, counterTransition.CounterId);
            if (!counterDefinitions.TryGetValue(key, out var definition) ||
                counterTransition.Sequence <= previousSequence ||
                counterTransition.EventSequence < previousEventSequence ||
                counterTransition.EventKind != definition.EventKind ||
                counterTransition.Increment < 0 ||
                counterTransition.PreviousValue != counterValues[key])
                return Failed(transition, "Relic transition counter trace is invalid.");
            var value = (long)counterTransition.PreviousValue + counterTransition.Increment;
            var executions = value < definition.Threshold
                ? 0
                : 1 + (value - definition.Threshold) / definition.Consumption;
            value -= executions * definition.Consumption;
            if (executions > int.MaxValue ||
                counterTransition.ThresholdExecutions != executions ||
                counterTransition.CurrentValue != value)
                return Failed(transition, "Relic transition counter consumption is invalid.");
            counterValues[key] = (int)value;
            previousSequence = counterTransition.Sequence;
            previousEventSequence = counterTransition.EventSequence;
        }

        for (var index = 0; index < ordered.Length; index++)
        {
            var runtime = ordered[index];
            var projected = transition.ProjectedInstances[index];
            foreach (var counter in runtime.Definition.ReactiveCounters.Where(counter => counter.Scope == RelicCounterScope.Run))
                if (projected.Counters.Single(value => value.CounterId == counter.CounterId).Value !=
                    counterValues[(runtime.State.InstanceId, counter.CounterId)])
                    return Failed(transition, "Relic transition Run-counter value is not authenticated by its trace.");
        }

        var expectedContributions = expectedReason == RelicBattleCompletionReason.PlayerVictory
            ? ExpectedVictoryContributions(ordered)
            : ImmutableArray<RelicRunOutcomeContribution>.Empty;
        var expectedGold = expectedContributions.Sum(contribution =>
            contribution.Kind == RelicRunOutcomeKind.VictoryGold ? contribution.Amount : 0);
        if (!transition.Contributions.SequenceEqual(expectedContributions) || transition.GoldDelta != expectedGold)
            return Failed(transition, "Relic transition outcomes do not match the active Run definitions.");

        return new RelicRunApplyResult(
            true,
            string.Empty,
            transition.TransitionId,
            transition.GoldDelta,
            ordered.Select(instance => instance.RunSnapshot()).ToImmutableArray());
    }

    private static RelicRunApplyResult Failed(RelicBattleTransitionResult transition, string reason) =>
        RelicRunApplyResult.Failed(transition.TransitionId, reason);

    private static void ValidateState(CompiledRelicDefinition definition, RelicRunInstanceState state)
    {
        if (string.IsNullOrWhiteSpace(state.InstanceId)) throw new ArgumentException("Relic instance id is required.", nameof(state));
        if (!string.Equals(state.ContentId, definition.StableId, StringComparison.Ordinal))
            throw new ArgumentException("Relic instance content id does not match its compiled definition.", nameof(state));
        if (state.Stacks <= 0) throw new ArgumentOutOfRangeException(nameof(state), "Relic stacks must be positive.");
        if (state.Charges < 0) throw new ArgumentOutOfRangeException(nameof(state), "Relic charges cannot be negative.");
        if (state.Counters is null)
            throw new ArgumentException("Relic Run-counter collection is missing.", nameof(state));
        if (!HasExactRunCounterSet(definition, state.Counters, out var reason))
            throw new ArgumentException(reason, nameof(state));
    }

    public static ImmutableArray<RelicCounterStateSnapshot> InitialRunCounters(CompiledRelicDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return definition.ReactiveCounters
            .Where(counter => counter.Scope == RelicCounterScope.Run)
            .Select(counter => new RelicCounterStateSnapshot(counter.CounterId, 0))
            .ToImmutableArray();
    }

    public static bool HasExactRunCounterSet(
        CompiledRelicDefinition definition,
        IEnumerable<RelicCounterStateSnapshot>? counters,
        out string reason)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (counters is null)
        {
            reason = "Relic Run-counter collection is missing.";
            return false;
        }
        var values = counters.ToArray();
        if (values.Any(value => value is null || string.IsNullOrWhiteSpace(value.CounterId) || value.Value < 0))
        {
            reason = "Relic Run-counter collection contains an invalid value.";
            return false;
        }
        if (values.Select(value => value.CounterId).Distinct(StringComparer.Ordinal).Count() != values.Length)
        {
            reason = "Relic Run-counter collection contains a duplicate id.";
            return false;
        }
        var expected = definition.ReactiveCounters
            .Where(counter => counter.Scope == RelicCounterScope.Run && counter.ResetPolicy == RelicCounterResetPolicy.RunEnd)
            .Select(counter => counter.CounterId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        var actual = values.Select(value => value.CounterId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
        {
            reason = "Relic Run-counter collection does not match its compiled definition.";
            return false;
        }
        var definitions = definition.ReactiveCounters
            .Where(counter => counter.Scope == RelicCounterScope.Run &&
                              counter.ResetPolicy == RelicCounterResetPolicy.RunEnd)
            .ToDictionary(counter => counter.CounterId, StringComparer.Ordinal);
        if (values.Any(value => value.Value >= definitions[value.CounterId].Threshold))
        {
            reason = "Relic Run-counter value is outside its canonical threshold range.";
            return false;
        }
        reason = string.Empty;
        return true;
    }

    internal static RelicBattleModifierSnapshot AggregateModifiers(IEnumerable<RelicBattleInstanceSnapshot> instances)
    {
        float armyHp = 1, armyDamage = 1, heroHp = 1, heroDamage = 1;
        float armyLifeSteal = 0, heroLifeSteal = 0, formationArmor = 0, formationDamage = 1;
        var shield = 0;
        var empty = 0;
        var summon = false;
        var summonContentId = string.Empty;
        foreach (var instance in instances)
        foreach (var modifier in instance.Definition.BattleModifiers)
        {
            var stacks = instance.Stacks;
            switch (modifier.Kind)
            {
                case RelicBattleModifierKind.ArmyHealthMultiplier: armyHp *= MathF.Pow(modifier.Amount, stacks); break;
                case RelicBattleModifierKind.ArmyDamageMultiplier: armyDamage *= MathF.Pow(modifier.Amount, stacks); break;
                case RelicBattleModifierKind.HeroHealthMultiplier: heroHp *= MathF.Pow(modifier.Amount, stacks); break;
                case RelicBattleModifierKind.HeroDamageMultiplier: heroDamage *= MathF.Pow(modifier.Amount, stacks); break;
                case RelicBattleModifierKind.ArmyLifeStealBonus: armyLifeSteal += modifier.Amount * stacks; break;
                case RelicBattleModifierKind.HeroLifeStealBonus: heroLifeSteal += modifier.Amount * stacks; break;
                case RelicBattleModifierKind.StartBattleShield: shield += (int)MathF.Round(modifier.Amount) * stacks; break;
                case RelicBattleModifierKind.EmptySlotPower: empty += (int)MathF.Round(modifier.Amount) * stacks; break;
                case RelicBattleModifierKind.SummonToken:
                    summon = true;
                    summonContentId = modifier.ContentId;
                    break;
                case RelicBattleModifierKind.FormationAdjacentArmor: formationArmor += modifier.Amount * stacks; break;
                case RelicBattleModifierKind.FormationAdjacentDamageMultiplier: formationDamage *= MathF.Pow(modifier.Amount, stacks); break;
                default: throw new InvalidOperationException($"Unsupported relic battle modifier: {modifier.Kind}");
            }
        }
        return new RelicBattleModifierSnapshot(
            armyHp, armyDamage, heroHp, heroDamage, armyLifeSteal, heroLifeSteal,
            shield, empty, summon, formationArmor, formationDamage, summonContentId);
    }

    internal static string Fingerprint(IEnumerable<RelicRunInstanceSnapshot> instances)
    {
        var text = string.Join("|", instances.Select(instance =>
            $"{instance.InstanceId}:{instance.ContentId}:{instance.Stacks}:{instance.Charges}:{instance.Roll}:" +
            string.Join(',', CanonicalCounters(instance.Counters).Select(counter => $"{counter.CounterId}={counter.Value}"))));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }

    internal static string TransitionIdentity(RelicRunKey runKey, string sourceFingerprint)
    {
        var text = string.Create(CultureInfo.InvariantCulture,
            $"{runKey.Seed:x16}:{runKey.HeroId}:{runKey.FloorIndex}:{runKey.BattleNumber}:{sourceFingerprint}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }

    private static ImmutableArray<RelicCounterStateSnapshot> CanonicalCounters(
        IEnumerable<RelicCounterStateSnapshot> counters) => counters
        .OrderBy(counter => counter.CounterId, StringComparer.Ordinal)
        .Select(counter => new RelicCounterStateSnapshot(counter.CounterId, counter.Value))
        .ToImmutableArray();

    private static ImmutableArray<RelicRunOutcomeContribution> ExpectedVictoryContributions(
        IEnumerable<RuntimeInstance> instances)
    {
        var contributions = ImmutableArray.CreateBuilder<RelicRunOutcomeContribution>();
        foreach (var instance in instances)
        foreach (var outcome in instance.Definition.VictoryOutcomes)
            contributions.Add(new RelicRunOutcomeContribution(
                instance.State.InstanceId,
                instance.State.ContentId,
                outcome.Kind,
                outcome.Amount * instance.State.Stacks));
        return contributions.ToImmutable();
    }

    private sealed class RuntimeInstance(
        CompiledRelicDefinition definition,
        RelicRunInstanceState state,
        long sequence)
    {
        public long Sequence { get; } = sequence;
        public CompiledRelicDefinition Definition { get; } = definition;
        public RelicRunInstanceState State { get; } = state;

        public RelicRunInstanceSnapshot RunSnapshot() => new(
            State.InstanceId,
            State.ContentId,
            State.Stacks,
            State.Charges,
            State.Roll,
            CanonicalCounters(State.Counters));

        public RelicBattleInstanceSnapshot BattleSnapshot()
        {
            var persisted = State.Counters.ToDictionary(counter => counter.CounterId, counter => counter.Value, StringComparer.Ordinal);
            var counters = Definition.ReactiveCounters.Select(counter => new RelicCounterStateSnapshot(
                counter.CounterId,
                counter.Scope == RelicCounterScope.Run ? persisted[counter.CounterId] : 0)).ToImmutableArray();
            return new RelicBattleInstanceSnapshot(
                State.InstanceId,
                State.ContentId,
                State.Stacks,
                State.Charges,
                State.Roll,
                counters,
                Definition);
        }
    }

    private sealed class Registration(RelicRunScope owner, string instanceId) : IDisposable
    {
        private RelicRunScope? _owner = owner;
        public void Dispose()
        {
            _owner?.Deactivate(instanceId);
            _owner = null;
        }
    }
}

public sealed record RelicBattleUnitBinding(
    string RuntimeId,
    int Team,
    bool IsHero,
    bool IsTemporary,
    bool IsInitial,
    bool Alive,
    CombatCell Cell,
    BattleAttributeSet Attributes);

public sealed class RelicBattleRuntimeContext
{
    public required BattleCombatBindingRegistry CombatBindings { get; init; }
    public required Func<ImmutableArray<RelicBattleUnitBinding>> QueryUnits { get; init; }
    public required Action<CompiledEffectBinding, string, string, string, int, float> ExecuteEffect { get; init; }
    public required Func<string, int, float, float, string, bool> Summon { get; init; }
    public required Func<int> CurrentTick { get; init; }
    public int EmptyDeploymentSlots { get; init; }
}

public sealed class RelicBattleScope : IDisposable
{
    private readonly RelicBattlePreparation _preparation;
    private readonly Dictionary<string, BattleRuntimeInstance> _instances;
    private readonly Dictionary<ProjectionKey, ModifierProjection> _modifierProjections = [];
    private readonly List<IDisposable> _subscriptions = [];
    private readonly List<RelicCounterTransitionSnapshot> _counterTransitions = [];
    private RelicBattleRuntimeContext? _context;
    private long _counterTransitionSequence;
    private bool _battleStartExecuted;
    private RelicBattleTransitionResult? _transition;

    public RelicBattleScope(RelicBattlePreparation preparation)
    {
        ArgumentNullException.ThrowIfNull(preparation);
        if (string.IsNullOrWhiteSpace(preparation.TransitionId) || string.IsNullOrWhiteSpace(preparation.SourceFingerprint))
            throw new ArgumentException("Relic battle preparation identity is invalid.", nameof(preparation));
        var sourceProjection = preparation.Instances.Select(instance => new RelicRunInstanceSnapshot(
            instance.InstanceId,
            instance.ContentId,
            instance.Stacks,
            instance.Charges,
            instance.Roll,
            instance.Counters.Where(counter => instance.Definition.ReactiveCounters.Any(definition =>
                definition.CounterId == counter.CounterId && definition.Scope == RelicCounterScope.Run)).ToImmutableArray()));
        var computedFingerprint = RelicRunScope.Fingerprint(sourceProjection);
        if (!string.Equals(preparation.SourceFingerprint, computedFingerprint, StringComparison.Ordinal) ||
            !string.Equals(preparation.TransitionId,
                RelicRunScope.TransitionIdentity(preparation.RunKey, computedFingerprint),
                StringComparison.Ordinal))
            throw new ArgumentException("Relic battle preparation identity does not match its instance projection.", nameof(preparation));
        var instances = new Dictionary<string, BattleRuntimeInstance>(StringComparer.Ordinal);
        for (var index = 0; index < preparation.Instances.Length; index++)
        {
            var snapshot = preparation.Instances[index];
            if (snapshot.Definition is null || string.IsNullOrWhiteSpace(snapshot.InstanceId) ||
                snapshot.ContentId != snapshot.Definition.StableId || snapshot.Stacks <= 0 || snapshot.Charges < 0)
                throw new ArgumentException("Relic battle preparation contains an invalid instance.", nameof(preparation));
            if (!HasExactBattleCounterSet(snapshot, out var reason))
                throw new ArgumentException(reason, nameof(preparation));
            if (!instances.TryAdd(snapshot.InstanceId, new BattleRuntimeInstance(snapshot, index)))
                throw new ArgumentException("Relic battle preparation contains a duplicate instance id.", nameof(preparation));
        }
        if (preparation.Modifiers != RelicRunScope.AggregateModifiers(preparation.Instances))
            throw new ArgumentException("Relic battle preparation modifiers do not match its instances.", nameof(preparation));
        _preparation = preparation;
        _instances = instances;
    }

    public int LiveBattleInstanceCount => _instances.Count;
    public int LiveCounterCount => _instances.Values.Sum(instance => instance.Counters.Count);
    public int SubscriptionCount => _subscriptions.Count;
    public int ModifierHandleCount => _modifierProjections.Count;
    internal ImmutableArray<RelicModifierProjectionSnapshot> ModifierProjections => _modifierProjections
        .Values
        .Select(projection => new RelicModifierProjectionSnapshot(projection.Source, projection.Handle))
        .OrderBy(projection => projection.Source)
        .ThenBy(projection => projection.Handle.Sequence)
        .ToImmutableArray();
    public bool IsCompleted => _transition is not null;
    public RelicBattleTransitionResult? Transition => _transition;
    public IReadOnlyList<RelicCounterTransitionSnapshot> CounterTransitions => _counterTransitions;

    public void Activate(RelicBattleRuntimeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_transition is not null) throw new InvalidOperationException("Relic Battle scope has completed.");
        if (_context is not null) throw new InvalidOperationException("Relic Battle scope is already active.");
        _context = context;
        try
        {
            foreach (var instance in OrderedInstances())
            {
                foreach (var binding in instance.Definition.AttributeBindings)
                {
                    RefreshProjection(instance, binding);
                    if (binding.Target is not CompiledRelicPlayerFormationAdjacentTarget) continue;
                    foreach (var eventKind in new[]
                             {
                                 BattleCombatEventKind.UnitMoved,
                                 BattleCombatEventKind.UnitSummoned,
                                 BattleCombatEventKind.UnitDefeated
                             })
                    {
                        var source = Source(instance, binding.BindingId);
                        _subscriptions.Add(context.CombatBindings.Subscribe(
                            eventKind,
                            source,
                            binding.Modifier.Priority,
                            (_, sink) => sink.Enqueue(source, binding.Modifier.Priority,
                                _ => RefreshProjection(instance, binding))));
                    }
                }
                foreach (var counter in instance.Definition.ReactiveCounters)
                {
                    var source = Source(instance, counter.CounterId);
                    _subscriptions.Add(context.CombatBindings.Subscribe(
                        counter.EventKind,
                        source,
                        counter.Priority,
                        (combatEvent, sink) =>
                        {
                            var increment = CounterIncrement(counter, combatEvent);
                            if (increment <= 0) return;
                            sink.Enqueue(source, counter.Priority,
                                _ => AdvanceCounter(instance, counter, combatEvent, increment));
                        }));
                }
            }
        }
        catch
        {
            Cleanup(bestEffort: true);
            _context = null;
            throw;
        }
    }

    public void ExecuteBattleStartEffects()
    {
        EnsureActive();
        if (_battleStartExecuted) return;
        _battleStartExecuted = true;
        var tick = _context!.CurrentTick();
        var executedOnce = new HashSet<(string ContentId, string BindingId)>();
        foreach (var instance in OrderedInstances())
        foreach (var effect in instance.Definition.BattleStartEffects)
        {
            var executions = effect.RepeatPolicy switch
            {
                RelicBattleStartRepeatPolicy.PerStack => instance.Stacks,
                RelicBattleStartRepeatPolicy.OncePerBattleBinding =>
                    executedOnce.Add((instance.ContentId, effect.BindingId)) ? 1 : 0,
                _ => throw new InvalidOperationException(
                    $"Unsupported Relic Battle-start repeat policy: {effect.RepeatPolicy}")
            };
            for (var stack = 0; stack < executions; stack++)
            {
                var sourceId = $"{instance.InstanceId}:{effect.BindingId}:{stack}";
                switch (effect)
                {
                    case CompiledRelicBattleStartShield shield:
                        foreach (var target in Units().Where(unit =>
                                     unit.Team == 0 && unit.IsInitial && !unit.IsTemporary)
                                 .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal))
                            _context.ExecuteEffect(
                                shield.Effect,
                                sourceId,
                                instance.InstanceId,
                                target.RuntimeId,
                                tick,
                                shield.Amount);
                        break;
                    case CompiledRelicBattleStartSummon summon:
                        if (!_context.Summon(
                                summon.ContentId,
                                0,
                                summon.HealthMultiplier,
                                summon.DamageMultiplier,
                                sourceId))
                            throw new InvalidOperationException(
                                $"Relic '{instance.ContentId}' could not summon '{summon.ContentId}'.");
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Unsupported Relic Battle-start effect: {effect.GetType().Name}");
                }
            }
        }
    }

    public int CounterValue(string instanceId, string counterId)
    {
        if (!_instances.TryGetValue(instanceId, out var instance) || !instance.Counters.TryGetValue(counterId, out var value))
            throw new KeyNotFoundException($"Relic counter '{instanceId}/{counterId}' is not active.");
        return value;
    }

    internal RelicStateCheckpoint CaptureState()
    {
        EnsureActive();
        return new RelicStateCheckpoint(this);
    }

    internal void RestoreState(RelicStateCheckpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        checkpoint.Restore(this);
    }

    public RelicBattleTransitionResult Complete(RelicBattleCompletionReason reason)
    {
        if (_transition is not null) return _transition;
        if (reason == RelicBattleCompletionReason.None) throw new ArgumentOutOfRangeException(nameof(reason));
        var projected = OrderedInstances().Select(instance => instance.Project()).ToImmutableArray();
        var contributions = ImmutableArray.CreateBuilder<RelicRunOutcomeContribution>();
        var goldDelta = 0;
        if (reason == RelicBattleCompletionReason.PlayerVictory)
        {
            foreach (var instance in OrderedInstances())
            foreach (var outcome in instance.Definition.VictoryOutcomes)
            {
                var amount = outcome.Amount * instance.Stacks;
                contributions.Add(new RelicRunOutcomeContribution(
                    instance.InstanceId,
                    instance.ContentId,
                    outcome.Kind,
                    amount));
                if (outcome.Kind == RelicRunOutcomeKind.VictoryGold) goldDelta += amount;
            }
        }

        Exception? cleanupFailure = null;
        try { Cleanup(bestEffort: false); }
        catch (Exception exception) { cleanupFailure = exception; }
        _instances.Clear();
        _context = null;
        _transition = new RelicBattleTransitionResult(
            _preparation.TransitionId,
            _preparation.RunKey,
            _preparation.SourceFingerprint,
            cleanupFailure is null ? reason : RelicBattleCompletionReason.Exception,
            projected,
            _counterTransitions.ToImmutableArray(),
            cleanupFailure is null ? contributions.ToImmutable() : [],
            cleanupFailure is null ? goldDelta : 0,
            LiveBattleInstanceCount,
            LiveCounterCount,
            SubscriptionCount,
            ModifierHandleCount);
        if (cleanupFailure is not null) throw cleanupFailure;
        return _transition;
    }

    public void Dispose()
    {
        if (_transition is null) Complete(RelicBattleCompletionReason.Disposal);
    }

    private BattleRuntimeInstance[] OrderedInstances() =>
        _instances.Values.OrderBy(instance => instance.Sequence).ToArray();

    private ImmutableArray<RelicBattleUnitBinding> Units() =>
        _context?.QueryUnits() ?? [];

    private void EnsureActive()
    {
        if (_transition is not null) throw new InvalidOperationException("Relic Battle scope has completed.");
        if (_context is null) throw new InvalidOperationException("Relic Battle scope is not active.");
    }

    private void RefreshProjection(BattleRuntimeInstance instance, CompiledRelicAttributeBinding binding)
    {
        switch (binding.StackPolicy)
        {
            case RelicAttributeStackPolicy.PerStack:
                RefreshPerStackProjection(instance, binding);
                return;
            case RelicAttributeStackPolicy.LinearAcrossStacksAndInstances:
                RefreshLinearProjection(instance, binding);
                return;
            default:
                throw new InvalidOperationException(
                    $"Unsupported Relic Attribute stack policy: {binding.StackPolicy}");
        }
    }

    private void RefreshPerStackProjection(
        BattleRuntimeInstance instance,
        CompiledRelicAttributeBinding binding)
    {
        EnsureActive();
        var targets = SelectTargets(binding.Target).OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal).ToArray();
        var desired = new HashSet<ProjectionKey>();
        foreach (var target in targets)
        for (var stack = 0; stack < instance.Stacks; stack++)
        {
            var key = new ProjectionKey(instance.InstanceId, binding.BindingId, target.RuntimeId, stack);
            desired.Add(key);
            if (_modifierProjections.ContainsKey(key)) continue;
            var modifier = binding.Modifier with
            {
                SlotId = $"{binding.Modifier.SlotId}:{binding.BindingId}:{stack}"
            };
            var source = Source(instance, $"{binding.BindingId}:{stack}", target.RuntimeId);
            var handle = target.Attributes.ApplyModifier(
                modifier,
                source,
                new BattleAttributeMagnitudeContext(
                    target.Attributes,
                    target.Attributes,
                    teamCount: TeamCount));
            _modifierProjections.Add(key, new ModifierProjection(target.Attributes, source, handle));
        }

        var stale = _modifierProjections.Keys
            .Where(key => key.InstanceId == instance.InstanceId && key.BindingId == binding.BindingId && !desired.Contains(key))
            .ToArray();
        foreach (var key in stale)
        {
            var projection = _modifierProjections[key];
            if (!projection.Attributes.Remove(projection.Handle))
                throw new InvalidOperationException($"Relic modifier '{key}' could not be removed.");
            _modifierProjections.Remove(key);
        }
    }

    private void RefreshLinearProjection(
        BattleRuntimeInstance instance,
        CompiledRelicAttributeBinding binding)
    {
        EnsureActive();
        var group = OrderedInstances().Where(candidate =>
                candidate.Definition.StableId == instance.Definition.StableId &&
                candidate.Definition.AttributeBindings.Any(candidateBinding =>
                    candidateBinding.BindingId == binding.BindingId &&
                    candidateBinding.StackPolicy == RelicAttributeStackPolicy.LinearAcrossStacksAndInstances))
            .ToArray();
        if (group.Length == 0)
            throw new InvalidOperationException(
                $"Relic linear Attribute group '{instance.Definition.StableId}/{binding.BindingId}' is empty.");

        // Registration order is the deterministic instance order. The first instance owns the
        // single group source/handle while every matching instance contributes all of its stacks.
        var owner = group[0];
        var ownerBinding = owner.Definition.AttributeBindings.Single(candidate =>
            candidate.BindingId == binding.BindingId &&
            candidate.StackPolicy == RelicAttributeStackPolicy.LinearAcrossStacksAndInstances);
        if (ownerBinding.Modifier.Magnitude is not CompiledConstantMagnitude constant)
            throw new InvalidOperationException(
                $"Relic linear Attribute group '{instance.Definition.StableId}/{binding.BindingId}' requires a constant magnitude.");

        var totalStacks = group.Sum(candidate => (long)candidate.Stacks);
        var scaledValue = ownerBinding.Modifier.Operation switch
        {
            AttributeModifierOperation.Add =>
                constant.Value * _context!.EmptyDeploymentSlots * totalStacks,
            AttributeModifierOperation.Multiply =>
                1f + _context!.EmptyDeploymentSlots * (constant.Value - 1f) * totalStacks,
            _ => throw new InvalidOperationException(
                $"Relic linear Attribute group '{instance.Definition.StableId}/{binding.BindingId}' requires Add or Multiply.")
        };
        if (!float.IsFinite(scaledValue))
            throw new InvalidOperationException(
                $"Relic linear Attribute group '{instance.Definition.StableId}/{binding.BindingId}' produced a non-finite magnitude.");

        var targets = SelectTargets(ownerBinding.Target)
            .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal)
            .ToArray();
        var desired = new HashSet<ProjectionKey>();
        foreach (var target in targets)
        {
            var key = new ProjectionKey(owner.InstanceId, binding.BindingId, target.RuntimeId, 0);
            desired.Add(key);
            if (_modifierProjections.ContainsKey(key)) continue;
            var modifier = ownerBinding.Modifier with
            {
                Magnitude = constant with { Value = scaledValue },
                SlotId = $"{ownerBinding.Modifier.SlotId}:{binding.BindingId}:linear"
            };
            var source = Source(owner, $"{binding.BindingId}:linear", target.RuntimeId);
            var handle = target.Attributes.ApplyModifier(
                modifier,
                source,
                new BattleAttributeMagnitudeContext(
                    target.Attributes,
                    target.Attributes,
                    teamCount: TeamCount));
            _modifierProjections.Add(key, new ModifierProjection(target.Attributes, source, handle));
        }

        var stale = _modifierProjections.Keys
            .Where(key => key.InstanceId == owner.InstanceId &&
                          key.BindingId == binding.BindingId &&
                          !desired.Contains(key))
            .ToArray();
        foreach (var key in stale)
        {
            var projection = _modifierProjections[key];
            if (!projection.Attributes.Remove(projection.Handle))
                throw new InvalidOperationException($"Relic modifier '{key}' could not be removed.");
            _modifierProjections.Remove(key);
        }
    }

    private IEnumerable<RelicBattleUnitBinding> SelectTargets(CompiledRelicUnitTarget target)
    {
        var units = Units();
        return target switch
        {
            CompiledRelicPlayerArmyTarget => units.Where(unit =>
                unit.Team == 0 && unit.IsInitial && !unit.IsTemporary && !unit.IsHero),
            CompiledRelicPlayerHeroesTarget => units.Where(unit =>
                unit.Team == 0 && unit.IsInitial && !unit.IsTemporary && unit.IsHero),
            CompiledRelicPlayerEmptySlotHeroesTarget => units.Where(unit =>
                unit.Team == 0 && unit.IsInitial && !unit.IsTemporary && unit.IsHero),
            CompiledRelicPlayerFormationAdjacentTarget => units.Where(unit =>
                unit.Team == 0 && unit.Alive && units.Any(other =>
                    other.RuntimeId != unit.RuntimeId && other.Team == unit.Team && other.Alive &&
                    Distance(unit.Cell, other.Cell) <= 1.5f)),
            _ => throw new InvalidOperationException($"Unsupported Relic target: {target.GetType().Name}")
        };
    }

    private float TeamCount(AttributeTeamCountKind kind, int team)
    {
        var units = Units().Where(unit => unit.Team == team);
        return kind switch
        {
            AttributeTeamCountKind.Persistent => units.Count(unit => unit.IsInitial && !unit.IsTemporary),
            AttributeTeamCountKind.Deployed => units.Count(unit => unit.IsInitial),
            AttributeTeamCountKind.Alive => units.Count(unit => unit.Alive),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    private int CounterIncrement(CompiledRelicReactiveCounter counter, BattleCombatEvent combatEvent)
    {
        var units = Units();
        return counter.Source switch
        {
            RelicCounterSourceKind.Population => units.Count(unit =>
                unit.Team == counter.Team && unit.IsInitial && (counter.IncludeTemporary || !unit.IsTemporary)),
            RelicCounterSourceKind.Alive => units.Count(unit =>
                unit.Team == counter.Team && unit.Alive && (counter.IncludeTemporary || !unit.IsTemporary)),
            RelicCounterSourceKind.Attack => MatchesUnit(
                units, combatEvent.SourceRuntimeId, counter.Team, counter.IncludeTemporary) ? 1 : 0,
            RelicCounterSourceKind.Death => MatchesUnit(
                units, combatEvent.TargetRuntimeId, counter.Team, counter.IncludeTemporary) ? 1 : 0,
            _ => throw new InvalidOperationException($"Unsupported Relic counter source: {counter.Source}")
        };
    }

    private void AdvanceCounter(
        BattleRuntimeInstance instance,
        CompiledRelicReactiveCounter counter,
        BattleCombatEvent combatEvent,
        int increment)
    {
        EnsureActive();
        var previous = instance.Counters[counter.CounterId];
        var currentValue = (long)previous + increment;
        var executionCount = currentValue < counter.Threshold
            ? 0
            : 1 + (currentValue - counter.Threshold) / counter.Consumption;
        currentValue -= executionCount * counter.Consumption;
        if (executionCount > int.MaxValue)
            throw new InvalidOperationException(
                $"Relic counter '{instance.InstanceId}/{counter.CounterId}' exceeded its representable execution count.");
        var current = (int)currentValue;
        var executions = (int)executionCount;
        instance.Counters[counter.CounterId] = current;
        _counterTransitions.Add(new RelicCounterTransitionSnapshot(
            ++_counterTransitionSequence,
            instance.InstanceId,
            counter.CounterId,
            combatEvent.Kind,
            combatEvent.Sequence,
            previous,
            increment,
            executions,
            current));

        for (var execution = 0; execution < executions; execution++)
        {
            var targetId = ThresholdTarget(counter, combatEvent);
            if (string.IsNullOrWhiteSpace(targetId))
                throw new InvalidOperationException(
                    $"Relic counter '{instance.InstanceId}/{counter.CounterId}' has no legal threshold target.");
            _context!.ExecuteEffect(
                counter.ThresholdEffect,
                $"{instance.InstanceId}:{counter.CounterId}:{_counterTransitionSequence}:{execution}",
                instance.InstanceId,
                targetId,
                combatEvent.Tick,
                increment);
        }
    }

    private string ThresholdTarget(CompiledRelicReactiveCounter counter, BattleCombatEvent combatEvent) =>
        counter.Target switch
        {
            RelicThresholdTargetKind.EventSource => combatEvent.SourceRuntimeId,
            RelicThresholdTargetKind.EventTarget => combatEvent.TargetRuntimeId,
            RelicThresholdTargetKind.FirstAliveTeamUnit => Units()
                .Where(unit => unit.Team == counter.TargetTeam && unit.Alive)
                .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal)
                .Select(unit => unit.RuntimeId)
                .FirstOrDefault() ?? string.Empty,
            _ => throw new InvalidOperationException($"Unsupported Relic threshold target: {counter.Target}")
        };

    private static bool MatchesUnit(
        IEnumerable<RelicBattleUnitBinding> units,
        string runtimeId,
        int team,
        bool includeTemporary) => units.Any(unit =>
        unit.RuntimeId == runtimeId && unit.Team == team && (includeTemporary || !unit.IsTemporary));

    private static float Distance(CombatCell first, CombatCell second)
    {
        var x = first.X - second.X;
        var y = first.Y - second.Y;
        return MathF.Sqrt(x * x + y * y);
    }

    private static CombatSourceRef Source(
        BattleRuntimeInstance instance,
        string bindingId,
        string ownerRuntimeId = "") => new(
        CombatSourceKind.Relic,
        instance.Definition.StableId,
        string.IsNullOrWhiteSpace(ownerRuntimeId) ? instance.InstanceId : ownerRuntimeId,
        $"{instance.InstanceId}:{bindingId}");

    private void Cleanup(bool bestEffort)
    {
        Exception? failure = null;
        foreach (var subscription in _subscriptions.AsEnumerable().Reverse())
            try { subscription.Dispose(); }
            catch (Exception exception) { failure ??= exception; }
        _subscriptions.Clear();
        foreach (var projection in _modifierProjections.Values.Reverse())
            try
            {
                if (!projection.Attributes.Remove(projection.Handle) && !bestEffort)
                    throw new InvalidOperationException("Relic Attribute modifier handle was not active during cleanup.");
            }
            catch (Exception exception) { failure ??= exception; }
        _modifierProjections.Clear();
        foreach (var instance in _instances.Values) instance.Counters.Clear();
        if (failure is not null && !bestEffort) throw failure;
    }

    private static bool HasExactBattleCounterSet(RelicBattleInstanceSnapshot snapshot, out string reason)
    {
        if (snapshot.Counters.IsDefault || snapshot.Counters.Any(counter =>
                counter is null || string.IsNullOrWhiteSpace(counter.CounterId) || counter.Value < 0))
        {
            reason = "Relic Battle-counter collection contains an invalid value.";
            return false;
        }
        var actual = snapshot.Counters.Select(counter => counter.CounterId).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        var expected = snapshot.Definition.ReactiveCounters.Select(counter => counter.CounterId)
            .OrderBy(id => id, StringComparer.Ordinal).ToArray();
        if (actual.Distinct(StringComparer.Ordinal).Count() != actual.Length ||
            !actual.SequenceEqual(expected, StringComparer.Ordinal))
        {
            reason = "Relic Battle-counter collection does not match its compiled definition.";
            return false;
        }
        foreach (var counter in snapshot.Definition.ReactiveCounters)
            if (snapshot.Counters.Single(value => value.CounterId == counter.CounterId).Value >= counter.Threshold)
            {
                reason = "Relic Battle-counter value is outside its canonical threshold range.";
                return false;
            }
        foreach (var counter in snapshot.Definition.ReactiveCounters.Where(counter => counter.Scope == RelicCounterScope.Battle))
            if (snapshot.Counters.Single(value => value.CounterId == counter.CounterId).Value != 0)
            {
                reason = "Relic Battle-owned counter must start at zero.";
                return false;
            }
        reason = string.Empty;
        return true;
    }

    private sealed class BattleRuntimeInstance
    {
        public BattleRuntimeInstance(RelicBattleInstanceSnapshot snapshot, long sequence)
        {
            Sequence = sequence;
            InstanceId = snapshot.InstanceId;
            ContentId = snapshot.ContentId;
            Stacks = snapshot.Stacks;
            Charges = snapshot.Charges;
            Roll = snapshot.Roll;
            Definition = snapshot.Definition;
            Counters = snapshot.Counters.ToDictionary(counter => counter.CounterId, counter => counter.Value, StringComparer.Ordinal);
        }

        public long Sequence { get; }
        public string InstanceId { get; }
        public string ContentId { get; }
        public int Stacks { get; }
        public int Charges { get; }
        public int Roll { get; }
        public Dictionary<string, int> Counters { get; }
        public CompiledRelicDefinition Definition { get; }

        public RelicRunInstanceSnapshot Project() => new(
            InstanceId,
            ContentId,
            Stacks,
            Charges,
            Roll,
            Definition.ReactiveCounters
                .Where(counter => counter.Scope == RelicCounterScope.Run)
                .Select(counter => new RelicCounterStateSnapshot(counter.CounterId, Counters[counter.CounterId]))
                .OrderBy(counter => counter.CounterId, StringComparer.Ordinal)
                .ToImmutableArray());
    }

    internal sealed class RelicStateCheckpoint
    {
        private readonly RelicBattleScope _owner;
        private readonly Dictionary<BattleRuntimeInstance, Dictionary<string, int>> _counters;
        private readonly Dictionary<ProjectionKey, ModifierProjection> _modifierProjections;
        private readonly int _transitionCount;
        private readonly long _transitionSequence;
        private readonly bool _battleStartExecuted;

        internal RelicStateCheckpoint(RelicBattleScope owner)
        {
            _owner = owner;
            _counters = owner._instances.Values.ToDictionary(
                instance => instance,
                instance => new Dictionary<string, int>(instance.Counters, StringComparer.Ordinal));
            _modifierProjections = owner._modifierProjections.ToDictionary(pair => pair.Key, pair => pair.Value);
            _transitionCount = owner._counterTransitions.Count;
            _transitionSequence = owner._counterTransitionSequence;
            _battleStartExecuted = owner._battleStartExecuted;
        }

        internal void Restore(RelicBattleScope owner)
        {
            if (!ReferenceEquals(owner, _owner))
                throw new InvalidOperationException("Relic checkpoint belongs to another Battle scope.");
            if (owner._transition is not null)
                throw new InvalidOperationException("Cannot restore a completed Relic scope.");
            foreach (var pair in _counters)
            {
                pair.Key.Counters.Clear();
                foreach (var counter in pair.Value) pair.Key.Counters.Add(counter.Key, counter.Value);
            }
            owner._modifierProjections.Clear();
            foreach (var pair in _modifierProjections) owner._modifierProjections.Add(pair.Key, pair.Value);
            if (owner._counterTransitions.Count < _transitionCount)
                throw new InvalidOperationException("Relic counter history changed before rollback.");
            if (owner._counterTransitions.Count > _transitionCount)
                owner._counterTransitions.RemoveRange(_transitionCount, owner._counterTransitions.Count - _transitionCount);
            owner._counterTransitionSequence = _transitionSequence;
            owner._battleStartExecuted = _battleStartExecuted;
        }
    }

    private readonly record struct ProjectionKey(
        string InstanceId,
        string BindingId,
        string TargetRuntimeId,
        int Stack);

    private sealed record ModifierProjection(
        BattleAttributeSet Attributes,
        CombatSourceRef Source,
        AttributeModifierHandle Handle);
}

internal sealed record RelicModifierProjectionSnapshot(
    CombatSourceRef Source,
    AttributeModifierHandle Handle);
