using System.Collections.Immutable;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;
namespace TowerAutobattler.Abilities;

public sealed record CompiledBattleValueTerm(BattleValueMetric Metric, BattleValueSubject Subject,
    CombatAttribute Attribute, string Key, float Scale, bool TeamShared, bool OwnStatusOnly);
public sealed record CompiledBattleValueOperation(BattleValueAction Action, CompiledEffectTargetQuery TargetQuery,
    BattleTargetPolicy TargetPolicy, float Amount, ImmutableArray<CompiledBattleValueTerm> Terms,
    EffectDamageType DamageType, CombatAttribute Attribute, string CounterKey, bool TeamShared,
    float Minimum, float Maximum, int Every, bool Broadcast, string Label) : CompiledAbilityOperation;
public sealed record CompiledConsumeStatusOperation(CompiledEffectTargetQuery TargetQuery, string StatusId,
    float DamageMultiplier, EffectDamageType DamageType) : CompiledAbilityOperation;
public sealed record CompiledEchoOperation(float Range, bool BehindOnly) : CompiledAbilityOperation;
public sealed record CompiledLifecycleOperation(LifecycleAbilityKind Kind, CompiledEffectTargetQuery TargetQuery,
    int DelayTicks, int DurationTicks, float HealthRatio, float AttackTransferRatio, string SummonContentId,
    int MaximumLivingSummons) : CompiledAbilityOperation;

public sealed record CompiledDisplacementOperation(DisplacementKind Kind, CompiledEffectTargetQuery TargetQuery,
    float Distance, int DurationTicks, float StopDistance, bool BehindTarget, bool ExcludeBoss,
    float ImpactDamage, float AttackRatio, float ImpactRadius, EffectDamageType DamageType,
    CompiledStatusDefinition? ImpactStatus, float ArcHeight) : CompiledAbilityOperation;
