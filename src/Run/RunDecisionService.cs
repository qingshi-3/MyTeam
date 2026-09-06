using System;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TowerAutobattler.Content;
using TowerAutobattler.Project;

namespace TowerAutobattler.Run;

public sealed class RunDecisionService(ContentRegistry content, CompiledGameProject project, RunProgressionPersistenceService persistence)
{
    public static RunOfferKind? KindFor(TowerNodeType type) => type switch
    {
        TowerNodeType.Recruitment => RunOfferKind.Recruitment,
        TowerNodeType.Shop => RunOfferKind.Shop,
        TowerNodeType.Event => RunOfferKind.Event,
        TowerNodeType.Rest => RunOfferKind.Rest,
        _ => null
    };

    public PendingRunOffer CreateOffer(ActiveRunDto run, RunOfferKind kind)
    {
        var campaign = project.Campaign.RunOffers.Count == 0
            ? RunOfferDefaults.WithDefaults(project.Campaign, project.RunRules,
                id => content.TryGet(id, out var entry) ? entry : throw new InvalidOperationException("Missing offer content: " + id))
            : project.Campaign;
        var definition = campaign.RunOffers[kind];
        var identity = $"{run.Seed:x16}:{run.FloorIndex}:{run.BattleNumber}:{definition.StableId}";
        var pooled = definition.PoolChoices.OrderBy(choice => StableRoll(identity + ":" + choice.StableId))
            .Take(definition.PoolChoiceCount);
        return new PendingRunOffer(identity, kind, definition.DisplayName, definition.AllowSkip, definition.Repeatable,
            run.FloorIndex, run.BattleNumber, definition.Choices.Concat(pooled).ToImmutableArray());
    }

    public RunDecisionResult Resolve(ActiveRunDto? run, string offerId, string? choiceId)
    {
        if (run?.PendingOffer is not { } offer) return RunDecisionResult.Reject(RunDecisionFailure.NoOffer, "没有待处理的选择。");
        if (offer.OfferId != offerId) return RunDecisionResult.Reject(RunDecisionFailure.StaleOffer, "这个选择已失效，请查看当前机会。");
        if (!persistence.ValidateRun(run)) return RunDecisionResult.Reject(RunDecisionFailure.InvalidOperation, "当前征程状态无效，未作修改。");
        var working = persistence.CloneRun(run);
        var chanceSucceeded = true;
        if (choiceId is null)
        {
            if (!offer.AllowSkip) return RunDecisionResult.Reject(RunDecisionFailure.NotEligible, "该选择不能跳过。");
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
                chanceSucceeded = StableRoll(offer.OfferId + ":outcome:" + choice.StableId) / (double)uint.MaxValue < choice.SuccessChance;
                if (choice.SuccessChance == 1) chanceSucceeded = true;
                foreach (var operation in chanceSucceeded ? choice.Operations : choice.FailureOperations)
                    if (!Apply(working, operation, false)) return RunDecisionResult.Reject(RunDecisionFailure.InvalidOperation, "当前无法完成全部效果，代价与收益均未提交。");
            }
            catch (OverflowException) { return RunDecisionResult.Reject(RunDecisionFailure.InvalidOperation, "数值超出支持范围，未作修改。"); }
        }
        if (choiceId is null || !offer.Repeatable)
        {
            working.PendingOffer = null;
            if (offer.Kind != RunOfferKind.CombatReward)
            {
                working.FloorIndex++;
                working.PendingNode = false;
            }
        }
        if (!persistence.ValidateRun(working)) return RunDecisionResult.Reject(RunDecisionFailure.InvalidOperation, "结果不符合征程规则，未作修改。");
        var changes = Changes(run, working);
        if (!persistence.TryPublish(working, run)) return RunDecisionResult.Reject(RunDecisionFailure.PersistenceFailed, "保存失败，机会、代价和原状态均已保留，请重试。");
        return new RunDecisionResult(true, RunDecisionFailure.None, chanceSucceeded ? "选择已完成。" : "冒险未成功，已结算该选项注明的结果。", chanceSucceeded) { Changes = changes };
    }

    public RunDecisionResult Check(ActiveRunDto run, CompiledRunChoice choice)
    {
        foreach (var condition in choice.Conditions)
        {
            var satisfied = condition.Kind switch
            {
                RunConditionKind.GoldAtLeast => run.Gold >= condition.Amount,
                RunConditionKind.RosterHealthAtLeast => run.Roster.All(hero => hero.HealthRatio >= condition.Ratio),
                RunConditionKind.PopulationBelowCap => run.CurrentPopulation < RunPopulationPolicy.Evaluate(run, project.RunRules).EffectivePopulationCap,
                RunConditionKind.StartingHeroIs => run.Roster.Count > 0 && run.Roster[0].ContentId == condition.ContentId,
                RunConditionKind.HasContent => run.Roster.Count(hero => hero.ContentId == condition.ContentId) +
                    run.Items.Where(item => item.ContentId == condition.ContentId).Sum(item => (long)item.Stacks) +
                    run.EquipmentInventory.Count(item => item.ContentId == condition.ContentId) +
                    run.Roster.SelectMany(hero => hero.Equipment).Count(item => item.ContentId == condition.ContentId) >= condition.Amount,
                _ => false
            };
            if (!satisfied) return RunDecisionResult.Reject(RunDecisionFailure.NotEligible, "尚未满足此选项的条件。");
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

    private bool Apply(ActiveRunDto run, CompiledRunOperation operation, bool isCost)
    {
        switch (operation.Kind)
        {
            case RunOperationKind.GainGold: run.Gold = checked(run.Gold + operation.Amount); return true;
            case RunOperationKind.SpendGold:
                if (run.Gold < operation.Amount) return false;
                run.Gold -= operation.Amount; return true;
            case RunOperationKind.Recruit:
            case RunOperationKind.GrantItem:
                // These primitive helpers edit a detached projection through a sink
                // that never writes; only Resolve's outer commit may publish it.
                var detached = new DetachedSave(run);
                var detachedPersistence = new RunProgressionPersistenceService(content, detached, project);
                var rewards = new RunRewardEconomyService(content, project, detachedPersistence, run);
                for (var index = 0; index < operation.Amount; index++)
                    if (!(operation.Kind == RunOperationKind.Recruit ? rewards.Recruit(run, operation.ContentId) : rewards.GrantItem(run, operation.ContentId))) return false;
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

    private static ImmutableArray<RunDecisionChange> Changes(ActiveRunDto before, ActiveRunDto after)
    {
        var result = ImmutableArray.CreateBuilder<RunDecisionChange>();
        if (before.Gold != after.Gold) result.Add(new(RunChangeKind.Gold, "", "", before.Gold, after.Gold));
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

    private sealed class DetachedSave(ActiveRunDto run) : IRunSaveService
    {
        public MetaProgressDto LoadMeta() => new();
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => run;
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) => true;
        public void DeleteActiveRun() { }
    }
}
