using System.Collections.Immutable;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Abilities;

public sealed record CompiledScaledStatusAbilityOperation(
    CompiledStatusDefinition Status,
    CompiledEffectTargetQuery TargetQuery,
    int BaseStacks,
    string StatusId,
    float ExistingStackRatio,
    float Chance,
    string ChanceStatusId,
    float ChancePerStack,
    string ApplicationVfx = "",
    string ChanceTraitId = "",
    ImmutableArray<float> ChanceByTraitTier = default,
    float MaximumChance = 1f) : CompiledAbilityOperation;
