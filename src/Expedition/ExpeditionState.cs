using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Run;

namespace TowerAutobattler.Expedition;

// Only run-owned mutable facts live here. Authored definitions remain immutable.
public sealed class ExpeditionStateDto
{
    public int Size { get; set; }
    public int GenerationVersion { get; set; }
    public ulong Seed { get; set; }
    public int PositionIndex { get; set; }
    public int StageIndex { get; set; }
    public int RemainingActions { get; set; }
    public List<ExpeditionCellDto> Cells { get; set; } = [];
    public List<ExpeditionSiteDto> Sites { get; set; } = [];
    public List<ImmutableArray<PendingRunOffer>> StageBossRewardGroups { get; set; } = [];
    public List<string> StageBossFloorRuleIds { get; set; } = [];
}

public sealed class ExpeditionCellDto
{
    public int Index { get; set; }
    public int BiomeIndex { get; set; }
    public bool Walkable { get; set; }
    public bool Revealed { get; set; }
    public string SiteId { get; set; } = string.Empty;
}

public sealed class ExpeditionSiteDto
{
    public string SiteId { get; set; } = string.Empty;
    public int CellIndex { get; set; }
    public string DefinitionId { get; set; } = string.Empty;
    public string EncounterProfileId { get; set; } = string.Empty;
    public string FloorRuleId { get; set; } = string.Empty;
    public ulong ContentSeed { get; set; }
    public bool Cleared { get; set; }
    public bool Handled { get; set; }
    public PendingRunOffer? ServiceOffer { get; set; }
    public ImmutableArray<PendingRunOffer> RewardGroups { get; set; } = [];
}

public static class ExpeditionMapState
{
    public static ExpeditionStateDto DeepClone(ExpeditionStateDto state) => new()
    {
        Size = state.Size,
        GenerationVersion = state.GenerationVersion,
        Seed = state.Seed,
        PositionIndex = state.PositionIndex,
        StageIndex = state.StageIndex,
        RemainingActions = state.RemainingActions,
        StageBossFloorRuleIds = [.. state.StageBossFloorRuleIds],
        StageBossRewardGroups = [.. state.StageBossRewardGroups],
        Cells = state.Cells.Select(cell => new ExpeditionCellDto
        {
            Index = cell.Index, BiomeIndex = cell.BiomeIndex, Walkable = cell.Walkable,
            Revealed = cell.Revealed, SiteId = cell.SiteId
        }).ToList(),
        Sites = state.Sites.Select(site => new ExpeditionSiteDto
        {
            SiteId = site.SiteId, CellIndex = site.CellIndex, DefinitionId = site.DefinitionId,
            EncounterProfileId = site.EncounterProfileId, FloorRuleId = site.FloorRuleId,
            ContentSeed = site.ContentSeed, Cleared = site.Cleared, Handled = site.Handled,
            // PendingRunOffer and its nested choices/operations are immutable values.
            ServiceOffer = site.ServiceOffer, RewardGroups = site.RewardGroups
        }).ToList()
    };
}
