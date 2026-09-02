using System;
using System.Collections.Generic;

namespace TowerAutobattler.Effects;

public interface IEffectProcessor
{
    EffectKind Kind { get; }

    PreparedEffectMutation Prepare(
        EffectInvocationContext context,
        string bindingId,
        int stepIndex,
        string targetId,
        float requestedAmount,
        EffectOrderingKey ordering,
        EffectWorldSnapshot snapshot,
        IEffectRuntimeWorld world);
}

public sealed class EffectProcessorRegistry
{
    private readonly Dictionary<EffectKind, IEffectProcessor> _processors = [];

    public EffectProcessorRegistry Register(IEffectProcessor processor)
    {
        ArgumentNullException.ThrowIfNull(processor);
        if (!_processors.TryAdd(processor.Kind, processor))
            throw new InvalidOperationException($"An effect processor is already registered for {processor.Kind}.");
        return this;
    }

    public bool Contains(EffectKind kind) => _processors.ContainsKey(kind);

    public IEffectProcessor Get(EffectKind kind) =>
        _processors.TryGetValue(kind, out var processor)
            ? processor
            : throw new InvalidOperationException($"No effect processor is registered for {kind}.");

    public static EffectProcessorRegistry CreateDefault() => new EffectProcessorRegistry()
        .Register(new DamageEffectProcessor())
        .Register(new HealEffectProcessor())
        .Register(new ShieldEffectProcessor());
}

public abstract class EffectProcessorBase(EffectKind kind) : IEffectProcessor
{
    public EffectKind Kind { get; } = kind;

    public PreparedEffectMutation Prepare(
        EffectInvocationContext context,
        string bindingId,
        int stepIndex,
        string targetId,
        float requestedAmount,
        EffectOrderingKey ordering,
        EffectWorldSnapshot snapshot,
        IEffectRuntimeWorld world)
    {
        if (!float.IsFinite(requestedAmount) || requestedAmount < 0)
            throw new InvalidOperationException($"Effect amount for {bindingId}[{stepIndex}] is invalid.");
        var request = new EffectModifierRequest(
            context,
            bindingId,
            stepIndex,
            Kind,
            targetId,
            requestedAmount);
        var resolved = world.ResolveModifiers(request, snapshot);
        if (!float.IsFinite(resolved.ResolvedAmount) || resolved.ResolvedAmount < 0)
            throw new InvalidOperationException($"Resolved effect amount for {bindingId}[{stepIndex}] is invalid.");
        return new PreparedEffectMutation(request, resolved, ordering);
    }
}

public sealed class DamageEffectProcessor() : EffectProcessorBase(EffectKind.Damage);
public sealed class HealEffectProcessor() : EffectProcessorBase(EffectKind.Heal);
public sealed class ShieldEffectProcessor() : EffectProcessorBase(EffectKind.Shield);

