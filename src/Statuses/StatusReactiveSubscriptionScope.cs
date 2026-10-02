using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;

namespace TowerAutobattler.Statuses;

internal sealed class StatusReactiveSubscriptionScope(
    Func<IEnumerable<StatusRuntimeInstance>> _instances, Func<bool> _completed,
    Func<StatusCombatReactiveSubscriptionRequest, IDisposable>? _combatReactiveRegistrar,
    Func<StatusEffectInvocation, bool> _reactiveEffectSink)
{
    internal readonly record struct ReactiveSubscriptionKey(string InstanceId, int BindingIndex);
    private readonly Dictionary<ReactiveSubscriptionKey, IDisposable> _reactiveSubscriptions = [];
    public int Count => _reactiveSubscriptions.Count;
    internal sealed class Checkpoint
    {
        private readonly StatusReactiveSubscriptionScope _owner;
        private readonly Dictionary<ReactiveSubscriptionKey, IDisposable> _state;
        internal Checkpoint(StatusReactiveSubscriptionScope owner)
        { _owner = owner; _state = new(owner._reactiveSubscriptions); }
        internal void Rollback(StatusReactiveSubscriptionScope owner)
        {
            if (!ReferenceEquals(owner, _owner)) throw new InvalidOperationException("Subscription checkpoint belongs to another Status scope.");
            foreach (var key in owner._reactiveSubscriptions.Keys.Where(key => !_state.ContainsKey(key))
                .OrderBy(key => key.InstanceId, StringComparer.Ordinal).ThenBy(key => key.BindingIndex).Reverse().ToArray())
            {
                if (!owner._reactiveSubscriptions.Remove(key, out var handle)) continue;
                try { handle.Dispose(); } catch { }
            }
            owner._reactiveSubscriptions.Clear();
            foreach (var pair in _state) owner._reactiveSubscriptions.Add(pair.Key, pair.Value);
        }
    }
    public Checkpoint Capture() => new(this);
    public void Rollback(Checkpoint checkpoint) => checkpoint.Rollback(this);
    public void CommitWorld()
    {
        var desired = _instances().SelectMany(instance =>
            Enumerable.Range(0, instance.Definition.CombatReactiveBindings.Length)
                .Select(index => new ReactiveSubscriptionKey(instance.InstanceId, index))).ToHashSet();
        foreach (var key in _reactiveSubscriptions.Keys.Where(key => !desired.Contains(key))
            .OrderBy(key => key.InstanceId, StringComparer.Ordinal).ThenBy(key => key.BindingIndex).ToArray())
        {
            if (!_reactiveSubscriptions.Remove(key, out var handle)) continue;
            try { handle.Dispose(); } catch { }
        }
    }
    public ReactiveSubscriptionTransaction Synchronize(IReadOnlySet<string> instanceIds, bool deferDetach)
    {
        if (instanceIds.Count == 0) return ReactiveSubscriptionTransaction.Empty;
        var desired = new Dictionary<ReactiveSubscriptionKey, CompiledStatusCombatReactiveBinding>();
        foreach (var instance in _instances().Where(item => instanceIds.Contains(item.InstanceId))
                     .OrderBy(item => item.ApplicationSequence))
        for (var bindingIndex = 0; bindingIndex < instance.Definition.CombatReactiveBindings.Length; bindingIndex++)
            desired.Add(
                new ReactiveSubscriptionKey(instance.InstanceId, bindingIndex),
                instance.Definition.CombatReactiveBindings[bindingIndex]);

        if (desired.Count > 0 && _combatReactiveRegistrar is null)
            throw new InvalidOperationException("Status combat reactive bindings require a Battle-local registrar.");
        var obsolete = _reactiveSubscriptions.Keys.Where(key =>
                instanceIds.Contains(key.InstanceId) && !desired.ContainsKey(key))
            .OrderBy(key => key.InstanceId, StringComparer.Ordinal)
            .ThenBy(key => key.BindingIndex)
            .ToArray();
        var added = new List<ReactiveSubscriptionKey>();
        var transaction = new ReactiveSubscriptionTransaction(this, added, obsolete, deferDetach);
        try
        {
            foreach (var pair in desired.Where(pair => !_reactiveSubscriptions.ContainsKey(pair.Key))
                         .OrderBy(pair => pair.Key.InstanceId, StringComparer.Ordinal)
                         .ThenBy(pair => pair.Key.BindingIndex))
            {
                var key = pair.Key;
                var binding = pair.Value;
                var instance = _instances().Single(item => item.InstanceId == key.InstanceId);
                var subscriptionSource = CombatSourceRef.Status(
                    instance.Definition.StableId,
                    instance.OwnerId,
                    $"{instance.InstanceId}:reactive:{key.BindingIndex}");
                var handle = _combatReactiveRegistrar!(new StatusCombatReactiveSubscriptionRequest(
                    binding.EventKind,
                    subscriptionSource,
                    binding.Priority,
                    (combatEvent, reactionSink) =>
                        HandleReactiveEvent(key, binding, combatEvent, reactionSink)));
                if (handle is null)
                    throw new InvalidOperationException(
                        $"Status '{instance.Definition.StableId}' reactive registrar returned null.");
                _reactiveSubscriptions.Add(key, handle);
                added.Add(key);
            }
            return transaction;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private void HandleReactiveEvent(
        ReactiveSubscriptionKey key,
        CompiledStatusCombatReactiveBinding binding,
        BattleCombatEvent combatEvent,
        BattleCombatReactionSink reactionSink)
    {
        if (_completed()) return;
        var instance = _instances().FirstOrDefault(item => item.InstanceId == key.InstanceId);
        if (instance is null) return;
        var matches = binding.OwnerRole switch
        {
            StatusReactiveOwnerRole.OwnerIsSource => combatEvent.SourceRuntimeId == instance.OwnerId,
            StatusReactiveOwnerRole.OwnerIsTarget => combatEvent.TargetRuntimeId == instance.OwnerId,
            _ => false
        };
        if (!matches) return;
        if (binding.SourceKind != CombatSourceKind.None && combatEvent.Source.Kind != binding.SourceKind) return;
        if (binding.FilterDamageType && combatEvent.DamageType != binding.DamageType) return;
        var sourceId = binding.EffectSourcePolicy switch
        {
            StatusReactiveEffectSourcePolicy.PrimaryContribution => PrimarySource(instance),
            _ => string.Empty
        };
        if (string.IsNullOrWhiteSpace(sourceId)) return;
        var explicitTargetId = binding.OwnerRole switch
        {
            StatusReactiveOwnerRole.OwnerIsSource => combatEvent.TargetRuntimeId,
            StatusReactiveOwnerRole.OwnerIsTarget => combatEvent.SourceRuntimeId,
            _ => string.Empty
        };
        var invocation = new StatusEffectInvocation(
            StatusEffectInvocationKind.Reactive,
            instance.Definition,
            sourceId,
            instance.OwnerId,
            instance.InstanceId,
            explicitTargetId,
            combatEvent.Tick,
            binding.Binding,
            StatusRemovalReason.None,
            combatEvent) { UseOverheal = binding.UseOverheal };
        var reactionSource = CombatSourceRef.Status(
            instance.Definition.StableId,
            instance.OwnerId,
            $"{instance.InstanceId}:reactive:{key.BindingIndex}");
        if (!reactionSink.Enqueue(reactionSource, binding.Priority, _ =>
            {
                if (!_reactiveEffectSink(invocation))
                    throw new InvalidOperationException(
                        $"Status reactive effect '{binding.Binding.StableId}' failed to execute.");
            }))
            throw new InvalidOperationException(
                $"Status reactive effect '{binding.Binding.StableId}' failed to enqueue.");
    }

    public Exception? Clear()
    {
        Exception? failure = null;
        // Unsubscription is irreversible cleanup. Finish every detach, clear Status ownership,
        // and report the first failure only after no live handle remains; ordinary state rollback
        // must never recreate a subscription whose external detach already ran.
        foreach (var handle in _reactiveSubscriptions.OrderBy(pair => pair.Key.InstanceId, StringComparer.Ordinal)
                     .ThenBy(pair => pair.Key.BindingIndex).Select(pair => pair.Value).ToArray())
            try { handle.Dispose(); }
            catch (Exception exception) { failure ??= exception; }
        _reactiveSubscriptions.Clear();
        return failure;
    }

    private static string PrimarySource(StatusRuntimeInstance instance) => instance.Contributions
        .OrderBy(item => item.ApplicationSequence).Select(item => item.SourceId).FirstOrDefault() ?? string.Empty;
    internal sealed class ReactiveSubscriptionTransaction
    {
        public static ReactiveSubscriptionTransaction Empty { get; } = new();

        private readonly StatusReactiveSubscriptionScope? _scope;
        private readonly IReadOnlyList<ReactiveSubscriptionKey>? _added;
        private readonly IReadOnlyList<ReactiveSubscriptionKey>? _obsolete;
        private bool _finished;
        private readonly bool _deferDetach;

        private ReactiveSubscriptionTransaction()
        {
        }

        public ReactiveSubscriptionTransaction(
            StatusReactiveSubscriptionScope scope,
            IReadOnlyList<ReactiveSubscriptionKey> added,
            IReadOnlyList<ReactiveSubscriptionKey> obsolete, bool deferDetach)
        {
            _scope = scope;
            _added = added;
            _obsolete = obsolete;
            _deferDetach = deferDetach;
        }

        public void Commit()
        {
            if (_finished || _scope is null || _obsolete is null) return;
            if (_deferDetach)
            {
                // The Battle transaction keeps obsolete listeners attached but logically dormant:
                // their Status instance is absent, so handlers no-op. Final detach happens only
                // after the complete Ability world commit succeeds.
                _finished = true;
                return;
            }
            foreach (var key in _obsolete)
            {
                if (!_scope._reactiveSubscriptions.Remove(key, out var handle)) continue;
                // External detach cannot be rolled back. Preserve the committed removal direction
                // even when a cleanup wrapper reports after it has already unsubscribed.
                try { handle.Dispose(); }
                catch { }
            }
            _finished = true;
        }

        public void Rollback()
        {
            if (_finished || _scope is null || _added is null) return;
            foreach (var key in _added.Reverse())
            {
                if (!_scope._reactiveSubscriptions.Remove(key, out var handle)) continue;
                // Failed application owns no subscription. Cleanup exceptions cannot convert that
                // rollback into a pseudo-commit or restore an externally detached listener.
                try { handle.Dispose(); }
                catch { }
            }
            _finished = true;
        }
    }

}
