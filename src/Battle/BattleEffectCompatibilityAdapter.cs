using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Battle;

/// <summary>
/// Narrow typed-effect boundary over BattleSimulation's existing mutation authority.
/// Authored Ability/Status effects and floor damage/healing use this scope; the recursive
/// attack/death chain intentionally remains outside it until that ordering contract migrates together.
/// </summary>
internal sealed class BattleEffectCompatibilityAdapter : IDisposable
{
    private static readonly CompiledEffectBinding DamageBinding = CompatibilityBinding(
        "compat_floor_damage",
        EffectKind.Damage);
    private static readonly CompiledEffectBinding FloorHealBinding = CompatibilityBinding(
        "compat_floor_heal",
        EffectKind.Heal);
    private readonly BattleEffectScope _scope;

    public BattleEffectCompatibilityAdapter(string scopeId, IEffectRuntimeWorld world) =>
        _scope = new BattleEffectScope(scopeId, world);

    public IReadOnlyList<EffectTraceEntry> Trace => _scope.Trace;
    public BattleScopeTransitionResult? Transition => _scope.Transition;
    public int SubscriptionCount => _scope.SubscriptionCount;
    public int PendingInvocationCount => _scope.PendingInvocationCount;
    public int LiveRuntimeInstanceCount => _scope.LiveRuntimeInstanceCount;

    internal BattleEffectScope.EffectStateCheckpoint CaptureState() => _scope.CaptureState();

    internal void RestoreState(BattleEffectScope.EffectStateCheckpoint checkpoint) =>
        _scope.RestoreState(checkpoint);

    public float Damage(string sourceId, string targetId, float amount, int tick) =>
        Execute(DamageBinding, NormalizeSource(sourceId, "floor"), targetId, amount, tick);

    public float FloorHeal(string targetId, float amount, int tick) =>
        Execute(FloorHealBinding, "floor", targetId, amount, tick);

    public EffectQueueDrainResult ExecuteAuthored(
        CompiledEffectBinding binding,
        string sourceId,
        string ownerId,
        string explicitTargetId,
        int tick,
        float invocationValue) =>
        _scope.ExecuteImmediate(binding, sourceId, ownerId, explicitTargetId, tick, invocationValue);

    public EffectPreflightResult PreflightAuthored(
        CompiledEffectBinding binding,
        string sourceId,
        string ownerId,
        string explicitTargetId,
        int tick,
        float invocationValue) =>
        _scope.PreflightImmediate(binding, sourceId, ownerId, explicitTargetId, tick, invocationValue);

    public BattleScopeTransitionResult Complete(BattleScopeCompletionReason reason, int tick) =>
        _scope.Complete(reason, tick);

    public void Dispose() => _scope.Dispose();

    private float Execute(
        CompiledEffectBinding binding,
        string sourceId,
        string targetId,
        float amount,
        int tick)
    {
        var result = _scope.ExecuteImmediate(binding, sourceId, sourceId, targetId, tick, amount);
        if (result.Status is EffectExecutionStatus.Failed or EffectExecutionStatus.Interrupted)
            throw new InvalidOperationException(
                $"Battle effect compatibility execution failed for '{binding.StableId}': " +
                $"{result.Interruption} {result.Invocations.FirstOrDefault()?.Message}");
        return result.Invocations
            .SelectMany(invocation => invocation.Steps)
            .Where(step => step.Status == EffectExecutionStatus.Succeeded)
            .Select(step => step.EffectiveAmount)
            .FirstOrDefault();
    }

    private static CompiledEffectBinding CompatibilityBinding(string stableId, EffectKind kind) => new(
        stableId,
        0,
        new CompiledEffectTrigger(EffectTriggerKind.Manual, EffectDomainEventKind.None),
        [],
        new CompiledExplicitTargetQuery(),
        [new CompiledEffectStep(kind, EffectAmountSource.InvocationValue, 1)],
        new CompiledEffectBindingLimits(0, 0, 0, 0),
        null);

    private static string NormalizeSource(string sourceId, string fallback) =>
        string.IsNullOrWhiteSpace(sourceId) ? fallback : sourceId;
}

internal sealed class LegacyBattleMutationPort(
    Func<int, EffectWorldSnapshot> captureSnapshot,
    Func<PreparedEffectMutation, EffectCommitOutcome> commit) : IEffectRuntimeWorld
{
    public EffectWorldSnapshot CaptureSnapshot(int tick) => captureSnapshot(tick);

    public EffectModifierResult ResolveModifiers(
        EffectModifierRequest request,
        EffectWorldSnapshot snapshot) => EffectModifierResult.Identity(request.RequestedAmount);

    public EffectCommitOutcome Commit(PreparedEffectMutation mutation) => commit(mutation);
}
