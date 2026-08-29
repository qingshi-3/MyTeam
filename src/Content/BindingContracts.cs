using System;

namespace TowerAutobattler.Content;

public enum SemanticBattleEventType { Selected, Activated, Deactivated }
public enum BattleCommandType { UseHeroCommand }
public enum ContentLifecycleState { Unbound, Bound, Active }

public sealed record SemanticBattleEvent(SemanticBattleEventType Type, string SourceRuntimeId, string TargetRuntimeId, float Value);
public sealed record BattleCommandRequest(BattleCommandType Type, string SourceRuntimeId);

public interface IDeterministicRandom { int NextInt(int minimumInclusive, int maximumExclusive); float NextFloat(); }
public interface ISemanticBattleEventSink { void Publish(SemanticBattleEvent battleEvent); }
public interface IBattleCommandGateway { bool Submit(BattleCommandRequest command); }
public interface IRunModifierRegistry { IDisposable Register(string itemInstanceId, Components.RunModifierProviderComponent provider); }

public sealed record UnitBindingContext(IDeterministicRandom Random, ISemanticBattleEventSink Events, IBattleCommandGateway Commands);
public sealed record ItemBindingContext(IRunModifierRegistry Modifiers);

public sealed class ItemInstanceState
{
    public string InstanceId { get; init; } = string.Empty;
    public int Stacks { get; set; } = 1;
    public int Charges { get; set; }
    public int Roll { get; set; }
}
