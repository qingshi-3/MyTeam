using System;

namespace TowerAutobattler.Content;

public enum SemanticBattleEventType { Selected, Activated, Deactivated }
public enum BattleCommandType { UseTacticalCommand }
public enum ContentLifecycleState { Unbound, Bound, Active }

public sealed record SemanticBattleEvent(SemanticBattleEventType Type, string SourceRuntimeId, string TargetRuntimeId, float Value);
public sealed record BattleCommandRequest(BattleCommandType Type, string SourceRuntimeId);

public interface IDeterministicRandom { int NextInt(int minimumInclusive, int maximumExclusive); float NextFloat(); }
public interface ISemanticBattleEventSink { void Publish(SemanticBattleEvent battleEvent); }
public interface IBattleCommandGateway { bool Submit(BattleCommandRequest command); }
public sealed record UnitBindingContext(IDeterministicRandom Random, ISemanticBattleEventSink Events, IBattleCommandGateway Commands);
public sealed class ItemBindingContext
{
    public ItemBindingContext(
        Relics.RelicRunScope relics,
        Relics.CompiledRelicDefinition definition)
    {
        Relics = relics ?? throw new ArgumentNullException(nameof(relics));
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public Relics.RelicRunScope Relics { get; }
    public Relics.CompiledRelicDefinition Definition { get; }
}

public sealed class ItemInstanceState : Relics.RelicRunInstanceState;
