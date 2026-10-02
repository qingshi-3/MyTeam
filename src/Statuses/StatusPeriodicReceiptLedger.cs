using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;

namespace TowerAutobattler.Statuses;

internal sealed class StatusPeriodicReceiptLedger
{
    private sealed record Receipt(string InstanceId, string OwnerId, string StableId, int Tick, float Value);
    private ImmutableDictionary<string, Receipt> _receipts = ImmutableDictionary<string, Receipt>.Empty.WithComparers(StringComparer.Ordinal);
    internal sealed class Checkpoint
    {
        private readonly StatusPeriodicReceiptLedger _owner;
        private readonly ImmutableDictionary<string, Receipt> _state;
        internal Checkpoint(StatusPeriodicReceiptLedger owner) { _owner = owner; _state = owner._receipts; }
        internal void Restore(StatusPeriodicReceiptLedger owner)
        {
            if (!ReferenceEquals(owner, _owner)) throw new InvalidOperationException("Receipt checkpoint belongs to another Status scope.");
            owner._receipts = _state;
        }
    }
    public Checkpoint Capture() => new(this);
    public void Restore(Checkpoint checkpoint) => checkpoint.Restore(this);
    public void Schedule(string id, string instance, string owner, string stableId, int tick, float value) =>
        _receipts = _receipts.Add(id, new(instance, owner, stableId, tick, value));
    public double PendingValue(string owner, string stableId) => _receipts.Values
        .Where(receipt => receipt.OwnerId == owner && receipt.StableId == stableId).Sum(receipt => (double)receipt.Value);
    public void Consume(string owner, string stableId) => _receipts = _receipts.RemoveRange(
        _receipts.Where(pair => pair.Value.OwnerId == owner && pair.Value.StableId == stableId).Select(pair => pair.Key));
    public void Remove(string id) => _receipts = _receipts.Remove(id);
    public void Clear() => _receipts = _receipts.Clear();
    public bool TryClaim(StatusEffectInvocation invocation)
    {
        if (!_receipts.TryGetValue(invocation.PeriodicReceiptId, out var receipt)) return false;
        if (receipt.InstanceId != invocation.InstanceId || receipt.OwnerId != invocation.OwnerId ||
            receipt.StableId != invocation.Definition.StableId || receipt.Tick != invocation.Tick || receipt.Value != invocation.PeriodicValue)
            throw new InvalidOperationException("Periodic effect receipt does not match its scheduled invocation.");
        Remove(invocation.PeriodicReceiptId);
        return true;
    }
}
