using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TowerAutobattler.Abilities;

public sealed record AbilityRuntimeStatus(CompiledAbilityDefinition Definition, int Uses, int ReadyTick, int RegisteredTick);

public interface IAbilityRuntimeWorld
{
    AbilityWorldSnapshot CaptureSnapshot(int tick);
    AbilityPreparationResult Prepare(
        CompiledAbilityDefinition ability,
        string sourceId,
        string ownerId,
        string explicitTargetId,
        int tick);
    AbilityCommitResult Commit(AbilityExecutionPlan plan);
}

public sealed class BattleAbilityScope : IDisposable
{
    private readonly string _scopeId;
    private readonly IAbilityRuntimeWorld _world;
    private readonly Dictionary<RuntimeKey, RuntimeInstance> _instances = [];
    private AbilityScopeTransitionResult? _transition;
    private int _lastTick;

    public BattleAbilityScope(string scopeId, IAbilityRuntimeWorld world, int maximumMana)
    {
        if (string.IsNullOrWhiteSpace(scopeId))
            throw new ArgumentException("Ability scope id is required.", nameof(scopeId));
        if (maximumMana < 0) throw new ArgumentOutOfRangeException(nameof(maximumMana));
        _scopeId = scopeId;
        _world = world ?? throw new ArgumentNullException(nameof(world));
        MaxMana = maximumMana;
        CurrentMana = maximumMana;
    }

    public string ScopeId => _scopeId;
    public int MaxMana { get; }
    public int CurrentMana { get; private set; }
    public int LiveRuntimeInstanceCount => _instances.Count;
    public bool IsCompleted => _transition is not null;
    public AbilityScopeTransitionResult? Transition => _transition;

    public void RegisterLoadout(string ownerId, CompiledAbilityLoadout loadout, int tick = 0)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("Ability owner id is required.", nameof(ownerId));
        ArgumentNullException.ThrowIfNull(loadout);
        if (_transition is not null)
            throw new InvalidOperationException("Cannot register abilities after the scope has completed.");
        foreach (var ability in loadout.Abilities)
        {
            var key = new RuntimeKey(ownerId, ability.StableId);
            if (!_instances.TryAdd(key, new RuntimeInstance(ability, ownerId, tick)))
                throw new InvalidOperationException($"Ability '{ability.StableId}' is already registered for '{ownerId}'.");
        }
    }

    public void ReplaceLoadout(string ownerId, CompiledAbilityLoadout loadout, int tick = 0)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new ArgumentException("Ability owner id is required.", nameof(ownerId));
        ArgumentNullException.ThrowIfNull(loadout);
        if (_transition is not null)
            throw new InvalidOperationException("Cannot replace abilities after the scope has completed.");
        foreach (var key in _instances.Keys.Where(key => key.OwnerId == ownerId).ToArray())
            _instances.Remove(key);
        RegisterLoadout(ownerId, loadout, tick);
    }

    // Projection only: inspecting a skill must never prepare, activate or advance it.
    public ImmutableArray<AbilityRuntimeStatus> ReadStates(string ownerId) => _instances.Values
        .Where(instance => instance.OwnerId == ownerId)
        .OrderBy(instance => instance.Definition.StableId, StringComparer.Ordinal)
        .Select(instance => new AbilityRuntimeStatus(instance.Definition, instance.Uses, instance.ReadyTick, instance.RegisteredTick))
        .ToImmutableArray();

    internal T? FindOperation<T>(string ownerId) where T : CompiledAbilityOperation => _instances.Values
        .Where(instance => instance.OwnerId == ownerId).SelectMany(instance => instance.Definition.Operations)
        .OfType<T>().FirstOrDefault();

    public CompiledAbilityDefinition? Find(string ownerId, string abilityId) =>
        _instances.TryGetValue(new RuntimeKey(ownerId, abilityId), out var instance)
            ? instance.Definition
            : null;

    internal bool HasTrigger(string ownerId, AbilityTriggerKind trigger) => _instances.Values.Any(instance =>
        instance.OwnerId == ownerId && instance.Definition.ActivationKind == AbilityActivationKind.Triggered &&
        instance.Definition.Trigger == trigger);

    public AbilityActivationResult TryActivateManual(
        string ownerId,
        string abilityId,
        int tick,
        string explicitTargetId = "") =>
        TryActivate(ownerId, abilityId, AbilityActivationKind.ManualCommand, AbilityTriggerKind.None, tick, explicitTargetId);

    public ImmutableArray<AbilityActivationResult> ActivateAutomatic(string ownerId, int tick, string explicitTargetId = "")
    {
        if (_transition is not null) return [];
        return _instances.Values
            .Where(instance => instance.OwnerId == ownerId &&
                               instance.Definition.ActivationKind == AbilityActivationKind.Automatic &&
                               (instance.Definition.Trigger == AbilityTriggerKind.BattleStarted && tick == 0 ||
                                instance.Definition.Trigger == AbilityTriggerKind.PeriodicTick && tick > 0 &&
                                tick % instance.Definition.IntervalTicks == 0 ||
                                instance.Definition.Trigger == AbilityTriggerKind.ManaFull && tick > 0))
            .OrderBy(instance => instance.Definition.StableId, StringComparer.Ordinal)
            .Select(instance => TryActivate(
                ownerId,
                instance.Definition.StableId,
                AbilityActivationKind.Automatic,
                instance.Definition.Trigger,
                tick,
                explicitTargetId))
            .ToImmutableArray();
    }

    public ImmutableArray<AbilityActivationResult> ActivateTriggered(
        string ownerId,
        AbilityTriggerKind trigger,
        int tick,
        string explicitTargetId = "")
    {
        if (trigger is AbilityTriggerKind.None or AbilityTriggerKind.BattleStarted or AbilityTriggerKind.PeriodicTick or AbilityTriggerKind.ManaFull or AbilityTriggerKind.ActionQueued)
            throw new ArgumentOutOfRangeException(nameof(trigger));
        if (_transition is not null) return [];
        return _instances.Values
            .Where(instance => instance.OwnerId == ownerId &&
                               instance.Definition.ActivationKind == AbilityActivationKind.Triggered &&
                               instance.Definition.Trigger == trigger)
            .OrderBy(instance => instance.Definition.StableId, StringComparer.Ordinal)
            .Select(instance => TryActivate(
                ownerId,
                instance.Definition.StableId,
                AbilityActivationKind.Triggered,
                trigger,
                tick,
                explicitTargetId))
            .ToImmutableArray();
    }

    public ImmutableArray<CompiledAbilityDefinition> Passives(string ownerId) => _instances.Values
        .Where(instance => instance.OwnerId == ownerId && instance.Definition.ActivationKind == AbilityActivationKind.Passive)
        .OrderBy(instance => instance.Definition.StableId, StringComparer.Ordinal)
        .Select(instance => instance.Definition)
        .ToImmutableArray();

    internal AbilityActivationResult TryActivateQueuedAutomatic(string ownerId, string abilityId, int tick) =>
        TryActivate(ownerId, abilityId, AbilityActivationKind.Automatic, AbilityTriggerKind.ActionQueued, tick, "");

    public ImmutableArray<AbilityActivationResult> ActivatePassives(string ownerId, int tick) => _instances.Values
        .Where(instance => instance.OwnerId == ownerId &&
            instance.Definition.ActivationKind == AbilityActivationKind.Passive && instance.Uses == 0)
        .OrderBy(instance => instance.Definition.StableId, StringComparer.Ordinal)
        .ToArray()
        .Select(instance => TryActivate(ownerId, instance.Definition.StableId,
            AbilityActivationKind.Passive, AbilityTriggerKind.None, tick, string.Empty)).ToImmutableArray();

    internal void RearmPassiveGrants(string ownerId)
    {
        foreach (var instance in _instances.Values.Where(i => i.OwnerId == ownerId && i.Definition.ActivationKind == AbilityActivationKind.Passive))
        { instance.Uses = 0; instance.ReadyTick = 0; }
    }

    internal ScopeStateCheckpoint CaptureState() => new(this);

    internal void RestoreState(ScopeStateCheckpoint checkpoint) => checkpoint.Restore(this);

    public AbilityScopeTransitionResult Complete(AbilityScopeCompletionReason reason, int finalTick)
    {
        if (_transition is not null) return _transition;
        if (reason == AbilityScopeCompletionReason.None) throw new ArgumentOutOfRangeException(nameof(reason));
        if (finalTick < 0) throw new ArgumentOutOfRangeException(nameof(finalTick));
        _lastTick = Math.Max(_lastTick, finalTick);
        _instances.Clear();
        _transition = new AbilityScopeTransitionResult(_scopeId, reason, _lastTick, LiveRuntimeInstanceCount);
        return _transition;
    }

    public void Dispose()
    {
        if (_transition is null) Complete(AbilityScopeCompletionReason.Disposal, _lastTick);
    }

    private AbilityActivationResult TryActivate(
        string ownerId,
        string abilityId,
        AbilityActivationKind entryPoint,
        AbilityTriggerKind trigger,
        int tick,
        string explicitTargetId)
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        _lastTick = Math.Max(_lastTick, tick);
        if (_transition is not null)
            return Failed(abilityId, AbilityActivationFailure.ScopeCompleted, "能力作用域已经结束。");
        if (!_instances.TryGetValue(new RuntimeKey(ownerId, abilityId), out var instance))
            return Failed(abilityId, AbilityActivationFailure.MissingAbility, "未找到可用能力。");
        var ability = instance.Definition;
        if (ability.ActivationKind != entryPoint || ability.Trigger != trigger)
            return Failed(abilityId, AbilityActivationFailure.WrongEntryPoint, "能力入口与定义不匹配。");
        if (entryPoint == AbilityActivationKind.ManualCommand && CurrentMana < ability.ManaCost)
            return Failed(abilityId, AbilityActivationFailure.InsufficientMana, "法力不足。");
        if (instance.ReadyTick > tick)
            return Failed(abilityId, AbilityActivationFailure.Cooldown, "能力仍在冷却中。");
        if (ability.MaxUses > 0 && instance.Uses >= ability.MaxUses)
            return Failed(abilityId, AbilityActivationFailure.UsageLimit, "能力使用次数已经耗尽。");

        AbilityPreparationResult prepared;
        try
        {
            prepared = _world.Prepare(ability, ownerId, ownerId, explicitTargetId, tick);
        }
        catch (Exception exception)
        {
            return Failed(abilityId, AbilityActivationFailure.ConditionsUnmet, exception.Message);
        }
        if (!prepared.Succeeded || prepared.Plan is null)
            return Failed(abilityId,
                prepared.Failure == AbilityActivationFailure.None
                    ? AbilityActivationFailure.ConditionsUnmet
                    : prepared.Failure,
                string.IsNullOrWhiteSpace(prepared.FailureReason) ? "当前没有合法的能力目标。" : prepared.FailureReason);

        AbilityCommitResult committed;
        try
        {
            committed = _world.Commit(prepared.Plan);
        }
        catch (Exception exception)
        {
            return Failed(abilityId, AbilityActivationFailure.CommitFailed, exception.Message);
        }
        if (!committed.Succeeded)
            return Failed(abilityId,
                committed.Failure == AbilityActivationFailure.None
                    ? AbilityActivationFailure.CommitFailed
                    : committed.Failure,
                string.IsNullOrWhiteSpace(committed.FailureReason) ? "能力提交失败。" : committed.FailureReason);

        // Manual compatibility commands retain their own budget. Mana-full skills are paid
        // by their concrete owner inside the world's atomic commit, never by this shared pool.
        if (entryPoint == AbilityActivationKind.ManualCommand) CurrentMana -= ability.ManaCost;
        instance.Uses++;
        instance.ReadyTick = tick + ability.CooldownTicks;
        return new AbilityActivationResult(
            true,
            AbilityActivationFailure.None,
            string.Empty,
            abilityId,
            ability.Trigger == AbilityTriggerKind.ManaFull ? committed.OwnerManaSpent : ability.ManaCost,
            prepared.Plan.GoldCost,
            committed.ResolvedFacts);
    }

    private static AbilityActivationResult Failed(
        string abilityId,
        AbilityActivationFailure failure,
        string reason) => new(false, failure, reason, abilityId, 0, 0, []);

    private readonly record struct RuntimeKey(string OwnerId, string AbilityId);

    internal sealed class ScopeStateCheckpoint
    {
        private readonly BattleAbilityScope _owner;
        private readonly (RuntimeKey Key, RuntimeInstance Instance, int Uses, int ReadyTick)[] _entries;
        private readonly int _mana;
        private readonly int _tick;
        internal ScopeStateCheckpoint(BattleAbilityScope owner)
        {
            _owner = owner;
            _entries = owner._instances.Select(pair =>
                (pair.Key, pair.Value, pair.Value.Uses, pair.Value.ReadyTick)).ToArray();
            _mana = owner.CurrentMana;
            _tick = owner._lastTick;
        }
        internal void Restore(BattleAbilityScope owner)
        {
            if (!ReferenceEquals(owner, _owner)) throw new InvalidOperationException("Ability checkpoint owner mismatch.");
            owner._instances.Clear();
            foreach (var entry in _entries)
            {
                entry.Instance.Uses = entry.Uses;
                entry.Instance.ReadyTick = entry.ReadyTick;
                owner._instances.Add(entry.Key, entry.Instance);
            }
            owner.CurrentMana = _mana;
            owner._lastTick = _tick;
        }
    }

    private sealed class RuntimeInstance(CompiledAbilityDefinition definition, string ownerId, int registeredTick)
    {
        public CompiledAbilityDefinition Definition { get; } = definition;
        public string OwnerId { get; } = ownerId;
        public int RegisteredTick { get; } = registeredTick;
        public int Uses { get; set; }
        public int ReadyTick { get; set; }
    }
}
