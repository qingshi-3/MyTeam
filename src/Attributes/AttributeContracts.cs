using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TowerAutobattler.Attributes;

public enum CombatAttribute
{
    MaxHealth,
    AttackDamage,
    SpellPower,
    AttackSpeed,
    Armor,
    MagicResistance,
    AttackRange,
    MoveSpeed,
    CriticalChance,
    CriticalDamage,
    MaxMana,
    StartingMana,
    HealingPower,
    LifeSteal,
    ControlResistance
}

public enum AttributeModifierOperation { Add, Multiply, Override }
public enum AttributeCaptureMode { Snapshot, Live }
public enum AttributeTeamCountKind { Persistent, Deployed, Alive }
public enum CombatSourceKind { None, Unit, Ability, Status, Equipment, Trait, Relic, TacticalCommand, FloorRule, System }

public readonly record struct CombatSourceRef(
    CombatSourceKind Kind,
    string StableId,
    string OwnerRuntimeId,
    string InstanceId) : IComparable<CombatSourceRef>
{
    public static CombatSourceRef None => new(CombatSourceKind.None, string.Empty, string.Empty, string.Empty);
    public static CombatSourceRef System(string stableId) =>
        new(CombatSourceKind.System, Require(stableId, nameof(stableId)), string.Empty, stableId);
    public static CombatSourceRef Unit(string stableId, string ownerRuntimeId, string instanceId) =>
        new(CombatSourceKind.Unit, Require(stableId, nameof(stableId)), Require(ownerRuntimeId, nameof(ownerRuntimeId)),
            Require(instanceId, nameof(instanceId)));
    public static CombatSourceRef Status(string stableId, string ownerRuntimeId, string instanceId) =>
        new(CombatSourceKind.Status, Require(stableId, nameof(stableId)), Require(ownerRuntimeId, nameof(ownerRuntimeId)),
            Require(instanceId, nameof(instanceId)));

    public bool IsSpecified => Kind != CombatSourceKind.None && !string.IsNullOrWhiteSpace(StableId);

    public int CompareTo(CombatSourceRef other)
    {
        var result = Kind.CompareTo(other.Kind);
        if (result != 0) return result;
        result = string.Compare(StableId, other.StableId, StringComparison.Ordinal);
        if (result != 0) return result;
        result = string.Compare(OwnerRuntimeId, other.OwnerRuntimeId, StringComparison.Ordinal);
        return result != 0 ? result : string.Compare(InstanceId, other.InstanceId, StringComparison.Ordinal);
    }

    private static string Require(string value, string parameter) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Combat source identity is required.", parameter) : value;
}

public readonly record struct AttributeModifierHandle(
    string ScopeId,
    string OwnerRuntimeId,
    long Sequence,
    Guid ScopeInstanceId)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(ScopeId) && !string.IsNullOrWhiteSpace(OwnerRuntimeId) &&
                           Sequence > 0 && ScopeInstanceId != Guid.Empty;
}

public sealed record CompiledAttributeDefinition(
    CombatAttribute Attribute,
    float BaseValue,
    float Minimum,
    float Maximum);

public abstract record CompiledAttributeMagnitude(AttributeCaptureMode CaptureMode);
public sealed record CompiledConstantMagnitude(float Value, AttributeCaptureMode CaptureMode = AttributeCaptureMode.Snapshot)
    : CompiledAttributeMagnitude(CaptureMode);
public sealed record CompiledSourceAttributeMagnitude(CombatAttribute Attribute, AttributeCaptureMode CaptureMode)
    : CompiledAttributeMagnitude(CaptureMode);
public sealed record CompiledTargetAttributeMagnitude(CombatAttribute Attribute, AttributeCaptureMode CaptureMode)
    : CompiledAttributeMagnitude(CaptureMode);
public sealed record CompiledContextValueMagnitude(string Key, AttributeCaptureMode CaptureMode)
    : CompiledAttributeMagnitude(CaptureMode);
public sealed record CompiledTeamCountMagnitude(AttributeTeamCountKind CountKind, int Team, AttributeCaptureMode CaptureMode)
    : CompiledAttributeMagnitude(CaptureMode);
public sealed record CompiledTraitValueMagnitude(string TraitId, int Team, AttributeCaptureMode CaptureMode)
    : CompiledAttributeMagnitude(CaptureMode);

public sealed record CompiledAttributeModifier(
    CombatAttribute Attribute,
    AttributeModifierOperation Operation,
    CompiledAttributeMagnitude Magnitude,
    int Priority,
    string SlotId);

public sealed record CompiledAttributeSetDefinition(
    ImmutableArray<CompiledAttributeDefinition> Attributes,
    string Fingerprint)
{
    public CompiledAttributeDefinition Find(CombatAttribute attribute) =>
        Attributes.FirstOrDefault(item => item.Attribute == attribute) ??
        throw new KeyNotFoundException($"Attribute '{attribute}' is not defined.");
}

public sealed class BattleAttributeMagnitudeContext
{
    private readonly Func<string, float> _contextValue;
    private readonly Func<AttributeTeamCountKind, int, float> _teamCount;
    private readonly Func<string, int, float> _traitValue;

    public BattleAttributeMagnitudeContext(
        BattleAttributeSet? source = null,
        BattleAttributeSet? target = null,
        Func<string, float>? contextValue = null,
        Func<AttributeTeamCountKind, int, float>? teamCount = null,
        Func<string, int, float>? traitValue = null)
    {
        Source = source;
        Target = target;
        _contextValue = contextValue ?? (_ => 0);
        _teamCount = teamCount ?? ((_, _) => 0);
        _traitValue = traitValue ?? ((_, _) => 0);
    }

    public BattleAttributeSet? Source { get; }
    public BattleAttributeSet? Target { get; }
    public float ContextValue(string key) => _contextValue(key);
    public float TeamCount(AttributeTeamCountKind kind, int team) => _teamCount(kind, team);
    public float TraitValue(string traitId, int team) => _traitValue(traitId, team);
}

public enum AttributeScopeCompletionReason { None, BattleCompleted, Abort, Replacement, Exception, Disposal }

public sealed record AttributeScopeTransitionResult(
    string ScopeId,
    AttributeScopeCompletionReason Reason,
    int FinalTick,
    int RemainingSets,
    int RemainingModifiers);
