using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;

namespace TowerAutobattler.Statuses;

internal sealed class StatusRuntimeInstance(
    CompiledStatusDefinition definition,
    string ownerId,
    long applicationSequence,
    int appliedTick,
    int durationTicks)
{
    public CompiledStatusDefinition Definition { get; } = definition;
    public string GrantId { get; init; } = "";
    public string OwnerId { get; } = ownerId;
    public string InstanceId { get; } = $"status:{definition.StableId}:{ownerId}:{applicationSequence}";
    public long ApplicationSequence { get; } = applicationSequence;
    public int AppliedTick { get; } = appliedTick;
    public int LastAppliedTick { get; set; } = appliedTick;
    public int Stacks { get; set; } = 1;
    public int RemainingTicks { get; set; } = durationTicks;
    // The due-time boundary is independent of when this owner's turn is visited.
    // RemainingTicks is its presentation projection, never a new schedule anchor.
    public int ExpirationTick { get; set; } = definition.DurationKind == StatusDurationKind.TimedTicks
        ? checked(appliedTick + durationTicks)
        : 0;
    public int NextPeriodTick { get; set; } = definition.PeriodicIntervalTicks > 0
        ? checked(appliedTick + definition.PeriodicIntervalTicks)
        : 0;
    public List<StatusSourceContribution> Contributions { get; } = [];

    public StatusRuntimeInstance Clone()
    {
        var clone = new StatusRuntimeInstance(Definition, OwnerId, ApplicationSequence, AppliedTick, RemainingTicks)
        {
            GrantId = GrantId,
            LastAppliedTick = LastAppliedTick,
            Stacks = Stacks,
            RemainingTicks = RemainingTicks,
            ExpirationTick = ExpirationTick,
            NextPeriodTick = NextPeriodTick
        };
        clone.Contributions.AddRange(Contributions.Select(item => item.Clone()));
        return clone;
    }
}

internal sealed class StatusSourceContribution(string sourceId)
{
    public string SourceId { get; } = sourceId;
    public List<StatusSourceStack> Stacks { get; } = [];
    public long ApplicationSequence => Stacks.Count == 0 ? 0 : Stacks[0].ApplicationSequence;
    public int AppliedTick => Stacks.Count == 0 ? 0 : Stacks[0].AppliedTick;

    public StatusSourceContribution Clone()
    {
        var clone = new StatusSourceContribution(SourceId);
        clone.Stacks.AddRange(Stacks.Select(item => item.Clone()));
        return clone;
    }
}

internal sealed class StatusSourceStack(long applicationSequence, int appliedTick)
{
    public long ApplicationSequence { get; } = applicationSequence;
    public int AppliedTick { get; } = appliedTick;
    public Dictionary<int, float> CapturedMagnitudes { get; } = [];

    public StatusSourceStack Clone()
    {
        var clone = new StatusSourceStack(ApplicationSequence, AppliedTick);
        foreach (var pair in CapturedMagnitudes) clone.CapturedMagnitudes.Add(pair.Key, pair.Value);
        return clone;
    }
}
