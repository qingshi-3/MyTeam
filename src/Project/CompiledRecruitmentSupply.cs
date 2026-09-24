using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;

namespace TowerAutobattler.Project;

public sealed record CompiledRecruitmentStage(int StartFloorIndex, ImmutableArray<int> TierWeights);

public sealed record CompiledRecruitmentSupply(
    ImmutableDictionary<string, int> TierByHero,
    ImmutableArray<int> OpeningTierWeights,
    ImmutableArray<CompiledRecruitmentStage> Stages)
{
    public const int OpeningCandidateCount = 6;
    public const int OpeningSelectionCount = 2;
    public int TierOf(string id) => TierByHero.GetValueOrDefault(id);
    public ImmutableArray<int> WeightsAt(int floorIndex) =>
        Stages.Last(stage => stage.StartFloorIndex <= floorIndex).TierWeights;
}
