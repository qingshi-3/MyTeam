using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Project;

namespace TowerAutobattler.Run;

public sealed record RunPopulationFacts(
    int CurrentPopulation,
    int OrdinaryPopulationCap,
    int EffectivePopulationCap,
    int PhysicalDeploymentCeiling,
    int DeployedPersistentHeroes,
    int PersistentRosterHeroes,
    bool IsValid,
    string RejectionReason)
{
    public int AvailableDeploymentPopulation =>
        Math.Min(CurrentPopulation, EffectivePopulationCap);
}

public static class RunPopulationPolicy
{
    public static RunPopulationFacts Evaluate(ActiveRunDto run, CompiledRunRules rules)
    {
        if (run is null || rules is null)
            return new RunPopulationFacts(0, 0, 0, 0, 0, 0, false, "人口上下文不可用。");
        var deployed = run.Deployment?.Count(id => !string.IsNullOrEmpty(id)) ?? 0;
        var rosterCount = run.Roster?.Count ?? 0;
        RunPopulationFacts Invalid(string reason, int effective = 0) => new(
            run.CurrentPopulation,
            rules.OrdinaryPopulationCap,
            effective == 0 ? rules.OrdinaryPopulationCap : effective,
            rules.PhysicalDeploymentCeiling,
            deployed,
            rosterCount,
            false,
            reason);

        if (run.Roster is null || run.Deployment is null || run.PopulationCapSources is null ||
            run.Roster.Any(hero => hero is null) || run.Deployment.Any(id => id is null))
            return Invalid("人口依赖的名册、阵型或来源数据为空。");
        var sourceIds = new HashSet<string>(StringComparer.Ordinal);
        long bonus = 0;
        foreach (var source in run.PopulationCapSources)
        {
            if (source is null || string.IsNullOrWhiteSpace(source.SourceId) || source.Amount <= 0 ||
                !sourceIds.Add(source.SourceId))
                return Invalid("人口上限来源无效或重复。");
            bonus += source.Amount;
            if (bonus > int.MaxValue) return Invalid("人口上限来源总量溢出。");
        }
        var effectiveLong = (long)rules.OrdinaryPopulationCap + bonus;
        if (effectiveLong > rules.PhysicalDeploymentCeiling || effectiveLong > int.MaxValue)
            return Invalid("人口上限来源超过物理部署上限。");
        var effective = (int)effectiveLong;
        if (run.CurrentPopulation <= 0 || run.CurrentPopulation > effective ||
            rosterCount > run.CurrentPopulation + rules.ReserveCapacity || deployed > run.CurrentPopulation)
            return Invalid("当前人口、名册或部署数量不合法。", effective);
        return new RunPopulationFacts(
            run.CurrentPopulation,
            rules.OrdinaryPopulationCap,
            effective,
            rules.PhysicalDeploymentCeiling,
            deployed,
            rosterCount,
            true,
            string.Empty);
    }

    public static bool Validate(ActiveRunDto run, CompiledRunRules rules) => Evaluate(run, rules).IsValid;
}
