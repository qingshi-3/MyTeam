using System;

namespace TowerAutobattler.Battle;

// A battle-local owner for an immutable mechanism state root. World transactions
// carry opaque checkpoints; they cannot accidentally install another battle's state.
// Drain guards describe the current call stack and deliberately are not rolled back.
internal class BattleMechanismScope<TState> where TState : notnull
{
    private readonly TState _empty;
    internal BattleMechanismScope(TState empty) { _empty = empty; State = empty; }
    internal TState State { get; private set; }
    private bool _draining;
    internal void Replace(TState state) => State = state;
    internal void Clear() => State = _empty;
    internal bool TryBeginDrain()
    {
        if (_draining) return false;
        _draining = true;
        return true;
    }
    internal void EndDrain() => _draining = false;
    internal Checkpoint Capture() => new(this, State);
    internal void Restore(Checkpoint checkpoint)
    {
        if (!ReferenceEquals(checkpoint.Owner, this))
            throw new InvalidOperationException("Mechanism checkpoint belongs to a different battle scope.");
        State = checkpoint.State;
    }
    internal readonly record struct Checkpoint(BattleMechanismScope<TState> Owner, TState State);
}
