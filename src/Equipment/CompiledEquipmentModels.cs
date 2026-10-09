using System.Collections.Immutable;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Statuses;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Equipment;

public sealed record CompiledEquipmentDefinition(
    string StableId,
    string ResourcePath,
    ImmutableArray<CompiledAttributeModifier> AttributeModifiers,
    ImmutableArray<CompiledEquipmentReactiveStatusBinding> ReactiveStatusBindings,
    ImmutableArray<CompiledTraitContribution> TraitContributions,
    string Fingerprint,
    ImmutableArray<CompiledStatusDefinition> GrantedStatuses = default,
    ImmutableArray<CompiledAttributeModifier> UniqueAttributeModifiers = default,
    CompiledEquipmentControlGuard? ControlGuard = null,
    CompiledEquipmentLifeSteal? LifeSteal = null);

public sealed record CompiledEquipmentLifeSteal(string RuleId, float Ratio, float AreaEfficiency);

public sealed record CompiledEquipmentControlGuard(string RuleId, int WindowTicks, float SubsequentDurationMultiplier);

public sealed record CompiledEquipmentReactiveStatusBinding(
    BattleCombatEventKind EventKind,
    EquipmentReactiveStatusTarget Target,
    EquipmentReactiveStatusSource Source,
    int Priority,
    CompiledStatusDefinition Status,
    StatusReactiveOwnerRole OwnerRole = StatusReactiveOwnerRole.OwnerIsSource,
    string UniqueRuleId = "");

public sealed class EquipmentInstanceState
{
    public string InstanceId { get; set; } = string.Empty;
    public string ContentId { get; set; } = string.Empty;
    public string OwnerHeroInstanceId { get; set; } = string.Empty;
    public int SlotIndex { get; set; }
}

public sealed record EquipmentBattleInstanceSnapshot(
    string InstanceId,
    string ContentId,
    string OwnerHeroInstanceId,
    int SlotIndex,
    CompiledEquipmentDefinition Definition);

public sealed record EquipmentBattlePreparation(
    string SourceFingerprint,
    ImmutableArray<EquipmentBattleInstanceSnapshot> Instances)
{
    public static EquipmentBattlePreparation Empty { get; } = new(
        EquipmentStateFingerprint.Empty,
        []);
}

public sealed record EquipmentOwnerBinding(
    string HeroInstanceId,
    string RuntimeId,
    bool IsPersistentRosterHero,
    BattleAttributeSet Attributes);

public enum EquipmentBattleCompletionReason
{
    None,
    BattleCompleted,
    Abort,
    Replacement,
    Exception,
    Disposal
}

public sealed record EquipmentBattleTransitionResult(
    string SourceFingerprint,
    EquipmentBattleCompletionReason Reason,
    int RemainingInstances,
    int RemainingModifierHandles,
    int RemainingSubscriptions = 0);
