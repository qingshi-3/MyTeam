using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;

namespace TowerAutobattler.Run;

// An action is a durable transaction, independently of the map cell and battle counter.
public sealed record RunActionSource(string ActionId, long Sequence, int StageIndex, string SiteId, int? LegacyFloorIndex);
public enum RunActionPhase { Service, Battle, Rewards }
public sealed class RunActionDto
{
    public RunActionSource Source { get; set; } = null!;
    public RunActionPhase Phase { get; set; }
    public EncounterPlan? Encounter { get; set; }
    public GrowthNodeSnapshotDto? GrowthSnapshot { get; set; }
    public RunBattleFreezeDto? BattleFreeze { get; set; }
    public PendingRunOffer? ServiceOffer { get; set; }
    public ImmutableArray<PendingRunOffer> RewardGroups { get; set; } = [];
    public int RewardCursor { get; set; }
    public bool IsStageBoss { get; set; }
    public bool ActionPaid { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public PendingRunOffer? CurrentOffer => Phase == RunActionPhase.Service ? ServiceOffer :
        Phase == RunActionPhase.Rewards && RewardCursor >= 0 && RewardCursor < RewardGroups.Length ? RewardGroups[RewardCursor] : null;
}

// Final preparation inputs, captured after entry triggers and one-shot consumption.
// The fingerprint covers both the immutable source and the complete run-owned inputs.
public sealed class RunBattleFreezeDto
{
    public int BattleNumber { get; set; }
    public int Gold { get; set; }
    public string InputsFingerprint { get; set; } = string.Empty;
}

public static class RunProgressionContext
{
    public static RunActionSource? Source(ActiveRunDto run) => run.Action?.Source;
    public static string Identity(ActiveRunDto run) => run.Expedition is null
        ? $"{run.Seed:x16}:{run.FloorIndex}:{run.BattleNumber}"
        : $"{run.RunId}:{run.Expedition.StageIndex}:{run.NextActionSequence}";
    public static bool IsAdvancedDiscovery(ActiveRunDto run, CompiledGameProject project) =>
        project.Campaign.Progression is CompiledExpedition definition && run.Expedition is { } map
            ? definition.Stages[map.StageIndex].AdvancedDiscovery : run.FloorIndex >= 5;
    public static string BattleInputsFingerprint(ActiveRunDto run)
    {
        var json = JsonSerializer.Serialize(new {
            run.RunId, run.ProfileId, run.ContentFingerprint, run.Seed, run.BattleNumber, run.Gold,
            run.Action!.Source, run.Action.Encounter, run.Roster, run.Deployment,
            run.Items, run.EquipmentInventory, run.CurrentPopulation, run.PopulationCapSources,
            run.EquippedTacticalCommandIds, GrowthSnapshot = GrowthRunService.GetPending(run),
            run.Growth?.RulesId
        });
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json)));
    }
}
