using System.Collections.Immutable;
using Godot;
using TowerAutobattler.Run;
namespace TowerAutobattler.Project;

public abstract record CompiledProgression;
public sealed record CompiledTowerProgression(int FloorsPerRegion, ImmutableArray<CompiledTowerRegion> Regions,
    CompiledTowerNodeTable NodeTable) : CompiledProgression;
public sealed record CompiledExpedition(string StableId, int Size, int GenerationVersion, int MaximumAttempts,
    ImmutableArray<CompiledExpeditionBiome> Biomes, ImmutableArray<CompiledExpeditionStage> Stages,
    ImmutableArray<CompiledExpeditionSite> Sites,
    ImmutableDictionary<string, CompiledExpeditionEncounter> Encounters) : CompiledProgression;
public sealed record CompiledExpeditionBiome(string StableId, string DisplayName, Color Color,
    ImmutableArray<string> EncounterProfiles, ImmutableArray<int> RecruitmentWeights);
public sealed record CompiledExpeditionStage(string StableId, string DisplayName, int Actions,
    bool AdvancedDiscovery, string BossProfileId);
public sealed record CompiledExpeditionSite(string StableId, string DisplayName, TowerNodeType Kind,
    int CountPerBiome, int ActionCost, CompiledRunOffer? Offer)
{
    public ImmutableArray<CompiledRunOffer> OfferRecipes { get; init; } = [];
}
public sealed record CompiledRewardRecipe(string StableId, int Gold, ImmutableArray<CompiledRunOffer> Groups);
public sealed record CompiledExpeditionEncounter(string StableId, CompiledEncounter Encounter,
    CompiledEncounterComposition Composition, float FinalHealthMultiplier, float FinalDamageMultiplier,
    bool OpeningCandidate, CompiledRewardRecipe Reward);
