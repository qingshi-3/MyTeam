using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Content;

namespace TowerAutobattler.Project;

public static partial class GameProjectCompiler
{
    private static CompiledRecruitmentSupply? CompileRecruitmentSupply(
        CampaignDefinition campaign, CompiledContentPool pool, CompilationContext context)
    {
        if (campaign.RecruitmentSupply is not { } authored) return null;
        var source = Source(authored);
        var tiers = ImmutableDictionary.CreateBuilder<string, int>(StringComparer.Ordinal);
        var groups = authored.Tiers ?? [];
        if (groups.Length is < 2 or > 5) context.Report.Error($"{source}: requires 2–5 recruitment tiers.");
        for (var index = 0; index < groups.Length; index++)
        {
            if (groups[index]?.HeroIds is not { Length: > 0 } ids)
            { context.Report.Error($"{source}: tier {index + 1} is empty."); continue; }
            foreach (var id in ids)
                if (string.IsNullOrWhiteSpace(id) || !pool.ContentIds.Contains(id) ||
                    !tiers.TryAdd(id, index + 1))
                    context.Report.Error($"{source}: unknown, repeated or out-of-pool hero '{id}'.");
                else if (context.Entry(id).Definition is not UnitDefinition { IsHero: true, IsEnemy: false, IsTestDummy: false })
                    context.Report.Error($"{source}: '{id}' must be a playable hero.");
        }
        if (!pool.ContentIds.ToHashSet(StringComparer.Ordinal).SetEquals(tiers.Keys))
            context.Report.Error($"{source}: every run-pool hero needs exactly one tier.");
        bool ValidWeights(int[]? weights) => weights is not null && weights.Length == groups.Length &&
            weights.All(weight => weight >= 0) && weights.Sum(weight => (long)weight) is > 0 and <= int.MaxValue;
        if (!ValidWeights(authored.OpeningTierWeights)) context.Report.Error($"{source}: invalid opening tier weights.");
        else if (tiers.Count(pair => authored.OpeningTierWeights[pair.Value - 1] > 0) < CompiledRecruitmentSupply.OpeningCandidateCount)
            context.Report.Error($"{source}: opening must supply six distinct heroes.");
        var stages = ImmutableArray.CreateBuilder<CompiledRecruitmentStage>();
        var previous = -1;
        foreach (var stage in authored.Stages ?? [])
        {
            if (stage is null || stage.StartFloorIndex <= previous || stage.StartFloorIndex < 0 ||
                stage.StartFloorIndex >= campaign.FloorsPerRegion * campaign.Regions.Length || !ValidWeights(stage.TierWeights))
            { context.Report.Error($"{source}: stage floors must increase and weights must be valid."); continue; }
            previous = stage.StartFloorIndex;
            stages.Add(new(stage.StartFloorIndex, [.. stage.TierWeights]));
        }
        if (stages.Count == 0 || stages[0].StartFloorIndex != 0)
            context.Report.Error($"{source}: stages must start at floor index zero.");
        return context.Report.HasCoreErrors ? null : new(tiers.ToImmutable(), [.. authored.OpeningTierWeights], stages.ToImmutable());
    }
}
