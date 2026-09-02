using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Run;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Equipment;

public static class EquipmentBattlePreparationBuilder
{
    public static EquipmentBattlePreparation Build(
        ActiveRunDto run,
        CompiledContentGraph graph,
        IReadOnlyCollection<string> deployedHeroInstanceIds)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(deployedHeroInstanceIds);
        if (run.Roster is null) throw new ArgumentException("Run roster is missing.", nameof(run));
        var deployed = deployedHeroInstanceIds.ToHashSet(StringComparer.Ordinal);
        var instances = ImmutableArray.CreateBuilder<EquipmentBattleInstanceSnapshot>();
        foreach (var hero in run.Roster.Where(hero => hero is not null && deployed.Contains(hero.InstanceId)))
        {
            if (hero.Equipment is null)
                throw new ArgumentException("Roster hero equipment state is missing.", nameof(run));
            foreach (var equipment in hero.Equipment.OrderBy(item => item.SlotIndex))
            {
                if (equipment is null || equipment.OwnerHeroInstanceId != hero.InstanceId ||
                    string.IsNullOrWhiteSpace(equipment.InstanceId) || equipment.SlotIndex < 0)
                    throw new ArgumentException("Roster hero equipment state is invalid.", nameof(run));
                var definition = graph.ResolveEquipment(equipment.ContentId);
                instances.Add(new EquipmentBattleInstanceSnapshot(
                    equipment.InstanceId,
                    equipment.ContentId,
                    equipment.OwnerHeroInstanceId,
                    equipment.SlotIndex,
                    definition));
            }
        }
        var result = instances.ToImmutable();
        return new EquipmentBattlePreparation(EquipmentStateFingerprint.Compute(result), result);
    }
}

public sealed class EquipmentBattleScope : IDisposable
{
    private readonly Dictionary<string, RuntimeEquipmentInstance> _instances = new(StringComparer.Ordinal);
    private readonly string _sourceFingerprint;
    private EquipmentBattleRuntimeContext? _context;
    private EquipmentBattleTransitionResult? _transition;

    public EquipmentBattleScope(
        string scopeId,
        EquipmentBattlePreparation preparation,
        IEnumerable<EquipmentOwnerBinding> owners)
    {
        if (string.IsNullOrWhiteSpace(scopeId))
            throw new ArgumentException("Equipment Battle scope id is required.", nameof(scopeId));
        ArgumentNullException.ThrowIfNull(preparation);
        ArgumentNullException.ThrowIfNull(owners);
        ScopeId = scopeId;
        _sourceFingerprint = preparation.SourceFingerprint;
        if (!string.Equals(
                preparation.SourceFingerprint,
                EquipmentStateFingerprint.Compute(preparation.Instances),
                StringComparison.Ordinal))
            throw new ArgumentException("Equipment Battle preparation fingerprint is invalid.", nameof(preparation));

        var ownerMap = owners.ToDictionary(owner => owner.HeroInstanceId, StringComparer.Ordinal);
        var ownerSlots = new HashSet<(string Owner, int Slot)>();
        try
        {
            foreach (var snapshot in preparation.Instances)
            {
                if (snapshot.Definition is null || snapshot.ContentId != snapshot.Definition.StableId ||
                    string.IsNullOrWhiteSpace(snapshot.InstanceId) ||
                    string.IsNullOrWhiteSpace(snapshot.OwnerHeroInstanceId) || snapshot.SlotIndex < 0)
                    throw new ArgumentException("Equipment Battle preparation contains an invalid instance.", nameof(preparation));
                if (_instances.ContainsKey(snapshot.InstanceId))
                    throw new ArgumentException("Equipment Battle preparation contains a duplicate instance id.", nameof(preparation));
                if (!ownerSlots.Add((snapshot.OwnerHeroInstanceId, snapshot.SlotIndex)))
                    throw new ArgumentException("Equipment Battle preparation contains a duplicate owner slot.", nameof(preparation));
                if (!ownerMap.TryGetValue(snapshot.OwnerHeroInstanceId, out var owner) ||
                    !owner.IsPersistentRosterHero || string.IsNullOrWhiteSpace(owner.RuntimeId) ||
                    owner.Attributes is null || owner.Attributes.OwnerRuntimeId != owner.RuntimeId)
                    throw new ArgumentException(
                        "Persistent Equipment owner is absent or is not a persistent roster hero.",
                        nameof(owners));

                var source = new CombatSourceRef(
                    CombatSourceKind.Equipment,
                    snapshot.ContentId,
                    owner.RuntimeId,
                    snapshot.InstanceId);
                var handles = ImmutableArray.CreateBuilder<AttributeModifierHandle>();
                var subscriptions = ImmutableArray.CreateBuilder<IDisposable>();
                var runtime = new RuntimeEquipmentInstance(snapshot, owner, source, handles, subscriptions);
                _instances.Add(snapshot.InstanceId, runtime);
                foreach (var modifier in snapshot.Definition.AttributeModifiers)
                    handles.Add(owner.Attributes.ApplyModifier(
                        modifier,
                        source,
                        new BattleAttributeMagnitudeContext(owner.Attributes, owner.Attributes)));
            }
        }
        catch
        {
            RemoveAll(bestEffort: true);
            throw;
        }
    }

    public string ScopeId { get; }
    public bool IsCompleted => _transition is not null;
    public int LiveInstanceCount => _instances.Count;
    public int LiveModifierHandleCount => _instances.Values.Sum(instance => instance.Handles.Count);
    public int LiveSubscriptionCount => _instances.Values.Sum(instance => instance.Subscriptions.Count);
    public EquipmentBattleTransitionResult? Transition => _transition;

    public void Activate(EquipmentBattleRuntimeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (IsCompleted) throw new InvalidOperationException("Equipment Battle scope has completed.");
        if (_context is not null) throw new InvalidOperationException("Equipment Battle scope is already active.");
        _context = context;
        try
        {
            foreach (var instance in _instances.Values.OrderBy(value => value.Snapshot.InstanceId, StringComparer.Ordinal))
            foreach (var group in instance.Snapshot.Definition.ReactiveStatusBindings
                         .GroupBy(binding => binding.EventKind)
                         .OrderBy(group => group.Key))
            {
                var bindings = group.OrderBy(binding => binding.Priority)
                    .ThenBy(binding => binding.Target)
                    .ThenBy(binding => binding.Status.StableId, StringComparer.Ordinal)
                    .ToImmutableArray();
                instance.Subscriptions.Add(context.CombatBindings.Subscribe(
                    group.Key,
                    instance.Source,
                    bindings[0].Priority,
                    (combatEvent, reactions) => OnCombatEvent(instance, bindings, combatEvent, reactions)));
            }
        }
        catch
        {
            foreach (var instance in _instances.Values.OrderByDescending(value => value.Snapshot.InstanceId, StringComparer.Ordinal))
                DisposeSubscriptions(instance, bestEffort: true);
            _context = null;
            throw;
        }
    }

    public bool Remove(string equipmentInstanceId)
    {
        if (IsCompleted || !_instances.Remove(equipmentInstanceId, out var instance)) return false;
        Remove(instance, bestEffort: false);
        return true;
    }

    public EquipmentBattleTransitionResult Complete(EquipmentBattleCompletionReason reason)
    {
        if (_transition is not null) return _transition;
        if (reason == EquipmentBattleCompletionReason.None) throw new ArgumentOutOfRangeException(nameof(reason));
        Exception? failure = null;
        try { RemoveAll(bestEffort: false); }
        catch (Exception exception) { failure = exception; }
        _transition = new EquipmentBattleTransitionResult(
            _sourceFingerprint,
            failure is null ? reason : EquipmentBattleCompletionReason.Exception,
            LiveInstanceCount,
            LiveModifierHandleCount,
            LiveSubscriptionCount);
        _context = null;
        if (failure is not null) throw failure;
        return _transition;
    }

    public void Dispose()
    {
        if (_transition is null) Complete(EquipmentBattleCompletionReason.Disposal);
    }

    private void RemoveAll(bool bestEffort)
    {
        Exception? failure = null;
        foreach (var instance in _instances.Values.OrderBy(instance => instance.Snapshot.InstanceId, StringComparer.Ordinal))
            try { Remove(instance, bestEffort); }
            catch (Exception exception) { failure ??= exception; }
        _instances.Clear();
        if (!bestEffort && failure is not null) throw failure;
    }

    private static void Remove(RuntimeEquipmentInstance instance, bool bestEffort)
    {
        Exception? failure = null;
        try { DisposeSubscriptions(instance, bestEffort); }
        catch (Exception exception) { failure = exception; }
        foreach (var handle in instance.Handles.ToImmutable().Reverse())
            if (!instance.Owner.Attributes.Remove(handle) && !bestEffort)
                failure ??= new InvalidOperationException(
                    $"Equipment modifier handle could not be removed: {instance.Snapshot.InstanceId}");
        instance.Handles.Clear();
        if (!bestEffort && failure is not null) throw failure;
    }

    private void OnCombatEvent(
        RuntimeEquipmentInstance instance,
        ImmutableArray<CompiledEquipmentReactiveStatusBinding> bindings,
        BattleCombatEvent combatEvent,
        BattleCombatReactionSink reactions)
    {
        var context = _context;
        if (context is null || IsCompleted ||
            !string.Equals(combatEvent.SourceRuntimeId, instance.Owner.RuntimeId, StringComparison.Ordinal))
            return;
        var applications = bindings.Select(binding => new StatusApplicationRequest(
                binding.Status,
                binding.Source == EquipmentReactiveStatusSource.Owner
                    ? instance.Owner.RuntimeId
                    : instance.Snapshot.InstanceId,
                binding.Target == EquipmentReactiveStatusTarget.Owner
                    ? instance.Owner.RuntimeId
                    : combatEvent.TargetRuntimeId,
                combatEvent.Tick))
            .Where(application => !string.IsNullOrWhiteSpace(application.OwnerId))
            .ToImmutableArray();
        if (applications.IsEmpty) return;
        if (!reactions.Enqueue(instance.Source, bindings[0].Priority, _ =>
            {
                // Eligibility is resolved at reaction execution, after every earlier authoritative
                // mutation in the chain. This prevents a post-death event from recreating Status
                // state after the Status scope has already applied its death policy.
                var eligible = applications
                    .Where(application => context.CanReceiveStatus(application.OwnerId))
                    .ToImmutableArray();
                if (eligible.IsEmpty) return;
                var results = context.ApplyStatuses(eligible);
                var failed = results.FirstOrDefault(result => !result.Applied);
                if (failed is not null)
                    throw new InvalidOperationException(
                        $"Equipment reactive Status batch failed: {failed.FailureReason}");
            }))
            throw new InvalidOperationException(
                "Equipment reactive Status batch could not enter the Battle reaction queue.");
    }

    private static void DisposeSubscriptions(RuntimeEquipmentInstance instance, bool bestEffort)
    {
        Exception? failure = null;
        foreach (var subscription in instance.Subscriptions.ToImmutable().Reverse())
            try { subscription.Dispose(); }
            catch (Exception exception) { failure ??= exception; }
        instance.Subscriptions.Clear();
        if (!bestEffort && failure is not null) throw failure;
    }

    private sealed class RuntimeEquipmentInstance(
        EquipmentBattleInstanceSnapshot snapshot,
        EquipmentOwnerBinding owner,
        CombatSourceRef source,
        ImmutableArray<AttributeModifierHandle>.Builder handles,
        ImmutableArray<IDisposable>.Builder subscriptions)
    {
        public EquipmentBattleInstanceSnapshot Snapshot { get; } = snapshot;
        public EquipmentOwnerBinding Owner { get; } = owner;
        public CombatSourceRef Source { get; } = source;
        public ImmutableArray<AttributeModifierHandle>.Builder Handles { get; } = handles;
        public ImmutableArray<IDisposable>.Builder Subscriptions { get; } = subscriptions;
    }
}

public sealed class EquipmentBattleRuntimeContext
{
    public required BattleCombatBindingRegistry CombatBindings { get; init; }
    public required Func<string, bool> CanReceiveStatus { get; init; }
    public required Func<ImmutableArray<StatusApplicationRequest>, ImmutableArray<StatusApplicationResult>> ApplyStatuses
        { get; init; }
}

internal static class EquipmentStateFingerprint
{
    public static string Empty { get; } = Hash(string.Empty);

    public static string Compute(IEnumerable<EquipmentBattleInstanceSnapshot> instances)
    {
        ArgumentNullException.ThrowIfNull(instances);
        var canonical = string.Join("|", instances.Select(instance =>
            $"{instance.InstanceId}:{instance.ContentId}:{instance.OwnerHeroInstanceId}:" +
            $"{instance.SlotIndex}:{instance.Definition?.Fingerprint}"));
        return Hash(canonical);
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
