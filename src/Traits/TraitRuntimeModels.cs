using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Traits;

public enum TraitContributionSourceKind { Hero, Equipment, ExplicitExtra }

public sealed record TraitContributionInput(
    string TraitId,
    int Value,
    int Team,
    TraitContributionSourceKind SourceKind,
    string SourceInstanceId,
    string OwnerRuntimeId,
    string ContentIdentity,
    bool IsPersistent,
    bool IsTemporary,
    bool IsDeployed);

public sealed record TraitContributionSnapshot(
    string TraitId,
    int Value,
    int Team,
    TraitContributionSourceKind SourceKind,
    string SourceInstanceId,
    string OwnerRuntimeId,
    string ContentIdentity,
    bool IsPersistent,
    bool IsTemporary,
    bool IsDeployed);

public sealed record TraitPresentationSnapshot(
    string TraitId,
    string DisplayName,
    string SemanticIconKey,
    int Team,
    int Value,
    int? ActiveMinValue,
    int? ActiveMaxValue,
    string DisplayStyle,
    string Text);

public sealed record TraitValueSnapshot(
    string TraitId,
    int Team,
    int Value,
    CompiledTraitBreakpoint? ActiveBreakpoint,
    ImmutableArray<TraitContributionSnapshot> Contributions,
    TraitPresentationSnapshot Presentation);

public sealed record TraitSnapshot(
    string Fingerprint,
    ImmutableArray<TraitValueSnapshot> Values,
    ImmutableArray<TraitContributionSnapshot> Contributions)
{
    public int Value(string traitId, int team) =>
        Values.FirstOrDefault(value => value.TraitId == traitId && value.Team == team)?.Value ?? 0;

    public TraitValueSnapshot Resolve(string traitId, int team) =>
        Values.FirstOrDefault(value => value.TraitId == traitId && value.Team == team) ??
        throw new KeyNotFoundException($"Trait snapshot has no value for '{traitId}' on team {team}.");
}

public sealed record TraitExplicitContribution(
    string TraitId,
    int Value,
    int Team,
    string SourceInstanceId,
    string ContentIdentity);

public sealed record TraitBattlePreparation(
    string SourceFingerprint,
    ImmutableArray<CompiledTraitDefinition> Definitions,
    ImmutableArray<TraitContributionInput> Contributions)
{
    public static TraitBattlePreparation Empty { get; } = TraitBattlePreparationBuilder.Build([], []);
}

public sealed record TraitOwnerBinding(
    string RuntimeId,
    int Team,
    BattleAttributeSet Attributes);

public enum TraitBattleCompletionReason
{
    None,
    BattleCompleted,
    Abort,
    Replacement,
    Exception,
    Disposal
}

public sealed record TraitBattleTransitionResult(
    string SourceFingerprint,
    TraitBattleCompletionReason Reason,
    int RemainingTiers,
    int RemainingModifierHandles);
