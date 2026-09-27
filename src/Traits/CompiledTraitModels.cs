using System.Collections.Immutable;
using TowerAutobattler.Attributes;
using TowerAutobattler.Content;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Traits;

public sealed record CompiledTraitContribution(string TraitId, int Value);

public sealed record CompiledTraitCountingPolicy(
    TraitDeploymentPolicy DeploymentPolicy,
    TraitTemporaryUnitPolicy TemporaryUnitPolicy,
    TraitDuplicateContentPolicy DuplicateContentPolicy,
    bool CountEquipment,
    bool CountExplicitExtra);

public sealed record CompiledTraitBreakpoint(
    int Index,
    int MinValue,
    int MaxValue,
    string DisplayStyle,
    ImmutableArray<CompiledAttributeModifier> AttributeModifiers,
    string Fingerprint,
    TraitTargetPolicy TargetPolicy = TraitTargetPolicy.AllTeam,
    ImmutableArray<CompiledStatusDefinition> GrantedStatuses = default,
    ImmutableArray<CompiledTraitMechanic> Mechanics = default);

public sealed record CompiledTraitMechanic(
    TraitMechanicKind Kind, float Amount, float Secondary, float Threshold, float Extra,
    int Count, int Limit, int DurationTicks, int CooldownTicks, bool Enabled, string SummonContentId);

public sealed record CompiledTraitDefinition(
    string StableId,
    string ResourcePath,
    string DisplayName,
    string SemanticIconKey,
    CompiledTraitCountingPolicy CountingPolicy,
    ImmutableArray<CompiledTraitBreakpoint> Breakpoints,
    string Fingerprint);

public sealed record TraitCompilationResult(
    CompiledTraitDefinition? Definition,
    ValidationReport Report);

public sealed record TraitBatchCompilationResult(
    ImmutableArray<CompiledTraitDefinition> Definitions,
    ValidationReport Report);

public sealed record TraitContributionCompilationResult(
    ImmutableArray<CompiledTraitContribution> Contributions,
    ValidationReport Report);
