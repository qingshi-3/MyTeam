using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Growth;
using TowerAutobattler.Expedition;

namespace TowerAutobattler.Run;

// Evaluates a choice against a detached projection. Only the action owner commits it.
public sealed class RunChoiceEvaluator(ContentRegistry content, CompiledGameProject project,
    RunProgressionPersistenceService persistence, RunRewardEconomyService rewards)
{
    private RunRelicRules RelicRules => rewards.RelicRules;
    public RunDecisionResult ApplyChoice(ActiveRunDto working, PendingRunOffer offer, string? choiceId)
    {
        var chanceSucceeded = true;
        if (choiceId is null)
        {
            if (!offer.AllowSkip) return RunDecisionResult.Reject(RunDecisionFailure.NotEligible, "该选择不能跳过。");
            if (!offer.Choices.IsEmpty) RelicRules.SkipReward(working, offer);
        }
        else
        {
            var choice = offer.Choices.SingleOrDefault(candidate => candidate.StableId == choiceId);
            if (choice is null) return RunDecisionResult.Reject(RunDecisionFailure.UnknownChoice, "此选项不属于当前机会。");
            var eligibility = Check(working, choice);
            if (!eligibility.Succeeded) return eligibility;
            try
            {
                foreach (var cost in choice.Costs)
                    if (!Apply(working, cost, true)) return RunDecisionResult.Reject(RunDecisionFailure.InsufficientResources, "无法支付全部代价，未作修改。");
                if (offer.Kind == RunOfferKind.Shop && choice.Costs.Any(cost => cost.Kind == RunOperationKind.SpendGold && cost.Amount > 0))
                    RelicRules.PaidShopPurchase(working);
                chanceSucceeded = StableRoll((offer.SamplingKey.Length == 0 ? offer.OfferId : offer.SamplingKey) + ":outcome:" + choice.StableId) / (double)uint.MaxValue < choice.SuccessChance;
                if (choice.SuccessChance == 1) chanceSucceeded = true;
                foreach (var operation in chanceSucceeded ? choice.Operations : choice.FailureOperations)
                    if (!Apply(working, operation, false)) return RunDecisionResult.Reject(RunDecisionFailure.InvalidOperation, "当前无法完成全部效果，代价与收益均未提交。");
            }
            catch (OverflowException) { return RunDecisionResult.Reject(RunDecisionFailure.InvalidOperation, "数值超出支持范围，未作修改。"); }
        }
        return new(true, RunDecisionFailure.None, "选择已完成。", chanceSucceeded);
    }
    public RunDecisionResult Check(ActiveRunDto run, CompiledRunChoice choice)
    {
        if (!RunRecruitmentPolicy.CanOffer(run, choice))
            return RunDecisionResult.Reject(RunDecisionFailure.NotEligible, "普通招募不能取得同名英雄（包含后备）。");
        if (!RunItemSupplyPolicy.CanOffer(run, choice, content))
            return RunDecisionResult.Reject(RunDecisionFailure.NotEligible, "已拥有该唯一遗物，不能重复取得。");
        if (choice.Operations.Concat(choice.FailureOperations).Any(operation => operation.Kind == RunOperationKind.RevealSites) &&
            (run.Expedition is not { } map || !map.Sites.Any(site => !map.Cells[site.CellIndex].Revealed)))
            return RunDecisionResult.Reject(RunDecisionFailure.NotEligible, "所有地点均已揭示。");
        foreach (var condition in choice.Conditions)
        {
            var satisfied = condition.Kind switch
            {
                RunConditionKind.GoldAtLeast => run.Gold >= condition.Amount,
                RunConditionKind.RunHealthBelowMaximum => run.CurrentRunHealth < run.MaximumRunHealth,
                RunConditionKind.RosterHealthAtLeast => run.Roster.All(hero => hero.HealthRatio >= condition.Ratio),
                RunConditionKind.PopulationBelowCap => run.CurrentPopulation < RunPopulationPolicy.Evaluate(run, project.RunRules).EffectivePopulationCap,
                RunConditionKind.StartingHeroIs => run.Roster.Count > 0 && run.Roster[0].ContentId == condition.ContentId,
                RunConditionKind.HasContent => run.Roster.Count(hero => hero.ContentId == condition.ContentId) +
                    run.Items.Where(item => item.ContentId == condition.ContentId).Sum(item => (long)item.Stacks) +
                    run.EquipmentInventory.Count(item => item.ContentId == condition.ContentId) +
                    run.Roster.SelectMany(hero => hero.Equipment).Count(item => item.ContentId == condition.ContentId) >= condition.Amount,
                _ => false
            };
            if (!satisfied) return RunDecisionResult.Reject(RunDecisionFailure.NotEligible,
                condition.Kind == RunConditionKind.RunHealthBelowMaximum ? "全局生命已满。" : "尚未满足此选项的条件。");
        }
        var preview = persistence.CloneRun(run);
        try
        {
            foreach (var cost in choice.Costs)
                if (!Apply(preview, cost, true)) return RunDecisionResult.Reject(RunDecisionFailure.InsufficientResources, "资源不足以支付此选项的全部代价。");
        }
        catch (OverflowException) { return RunDecisionResult.Reject(RunDecisionFailure.InvalidOperation, "代价超出支持范围。"); }
        return new(true, RunDecisionFailure.None, "");
    }

    private PendingRunOffer VisibleOffer(ActiveRunDto run, PendingRunOffer offer) =>
        RunItemSupplyPolicy.VisibleOffer(run, RunRecruitmentPolicy.VisibleOffer(run, offer), content);

    private bool Apply(ActiveRunDto run, CompiledRunOperation operation, bool isCost)
    {
        switch (operation.Kind)
        {
            case RunOperationKind.GainGold: RelicRules.GainGold(run, operation.Amount); return true;
            case RunOperationKind.RecoverRunHealth:
                run.CurrentRunHealth = RunHealthPolicy.Recover(run, operation.Amount); return true;
            case RunOperationKind.SpendGold:
                if (run.Gold < operation.Amount) return false;
                run.Gold -= operation.Amount; return true;
            case RunOperationKind.SpendRunHealth:
                if (!isCost || run.CurrentRunHealth <= operation.Amount) return false;
                run.CurrentRunHealth -= operation.Amount; return true;
            case RunOperationKind.RevealSites:
                if (isCost || run.Expedition is not { } map) return false;
                var position = map.PositionIndex;
                var selected = map.Sites.Where(site => !map.Cells[site.CellIndex].Revealed)
                    .OrderBy(site => Math.Abs(site.CellIndex % map.Size - position % map.Size) +
                        Math.Abs(site.CellIndex / map.Size - position / map.Size))
                    .ThenBy(site => site.SiteId, StringComparer.Ordinal).Take(operation.Amount).ToArray();
                if (selected.Length == 0) return false;
                foreach (var site in selected) map.Cells[site.CellIndex].Revealed = true;
                return true;
            case RunOperationKind.Recruit:
            case RunOperationKind.GrantItem:
                for (var index = 0; index < operation.Amount; index++)
                    if (!(operation.Kind == RunOperationKind.Recruit
                        ? rewards.RecruitToProjection(run, operation.ContentId)
                        : rewards.GrantToProjection(run, operation.ContentId))) return false;
                return true;
            case RunOperationKind.RecoverRoster:
                var targets = run.Roster.Where((_, index) => operation.Target == RunRosterTarget.All ||
                    operation.Target == RunRosterTarget.StartingHero && index == 0 || operation.Target == RunRosterTarget.OtherHeroes && index != 0).ToArray();
                if (isCost && targets.Any(hero => hero.HealthRatio + operation.Ratio < operation.MinimumHealthRatio)) return false;
                foreach (var hero in targets) hero.HealthRatio = Math.Clamp(hero.HealthRatio + operation.Ratio, operation.MinimumHealthRatio, 1);
                return true;
            case RunOperationKind.GrantPopulation:
                var population = RunPopulationPolicy.Evaluate(run, project.RunRules);
                if (run.CurrentPopulation > population.EffectivePopulationCap - operation.Amount) return false;
                run.CurrentPopulation += operation.Amount; return true;
            case RunOperationKind.IncreasePopulationCap:
                if (run.PopulationCapSources.Any(source => source.SourceId == operation.SourceId)) return false;
                var cap = RunPopulationPolicy.Evaluate(run, project.RunRules).EffectivePopulationCap;
                if (operation.Amount > project.RunRules.PhysicalDeploymentCeiling - cap) return false;
                run.PopulationCapSources.Add(new() { SourceId = operation.SourceId, Amount = operation.Amount }); return true;
            default: return false;
        }
    }

    private static uint StableRoll(string key) => System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

    public static ImmutableArray<RunDecisionChange> Changes(ActiveRunDto before, ActiveRunDto after)
    {
        var result = ImmutableArray.CreateBuilder<RunDecisionChange>();
        if (before.Gold != after.Gold) result.Add(new(RunChangeKind.Gold, "", "", before.Gold, after.Gold));
        if (before.CurrentRunHealth != after.CurrentRunHealth)
            result.Add(new(RunChangeKind.RunHealth, "", "", before.CurrentRunHealth, after.CurrentRunHealth));
        if (before.CurrentPopulation != after.CurrentPopulation) result.Add(new(RunChangeKind.Population, "", "", before.CurrentPopulation, after.CurrentPopulation));
        var oldCap = before.PopulationCapSources.Sum(source => (long)source.Amount);
        var newCap = after.PopulationCapSources.Sum(source => (long)source.Amount);
        if (oldCap != newCap) result.Add(new(RunChangeKind.PopulationCap, "", "", oldCap, newCap));
        foreach (var hero in after.Roster)
        {
            var previous = before.Roster.SingleOrDefault(candidate => candidate.InstanceId == hero.InstanceId);
            if (previous is null) result.Add(new(RunChangeKind.HeroAdded, hero.InstanceId, hero.ContentId, 0, 1));
            else if (previous.HealthRatio != hero.HealthRatio) result.Add(new(RunChangeKind.HeroHealth, hero.InstanceId, hero.ContentId, previous.HealthRatio, hero.HealthRatio));
        }
        var oldIds = before.Items.Select(item => item.InstanceId).Concat(before.EquipmentInventory.Select(item => item.InstanceId))
            .Concat(before.Roster.SelectMany(hero => hero.Equipment).Select(item => item.InstanceId)).ToHashSet(StringComparer.Ordinal);
        foreach (var item in after.Items.Where(item => !oldIds.Contains(item.InstanceId))) result.Add(new(RunChangeKind.ItemAdded, item.InstanceId, item.ContentId, 0, item.Stacks));
        foreach (var item in after.EquipmentInventory.Where(item => !oldIds.Contains(item.InstanceId))) result.Add(new(RunChangeKind.ItemAdded, item.InstanceId, item.ContentId, 0, 1));
        return result.ToImmutable();
    }

}
