using System.Collections.Immutable;
using TowerAutobattler.Attributes;
using TowerAutobattler.Content;

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
    string Fingerprint);

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
