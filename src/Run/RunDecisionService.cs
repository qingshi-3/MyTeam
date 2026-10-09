using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Growth;

namespace TowerAutobattler.Run;

public sealed class RunDecisionService(ContentRegistry content, CompiledGameProject project, RunProgressionPersistenceService persistence,
    GrowthRunService? growth = null)
{
    public RunRelicRules RelicRules => new(content);

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
        var definition = CampaignWithDefaults().RunOffers[kind];
        return CreateOffer(run, definition);
    }

    public ImmutableArray<PendingRunOffer> CreateCombatRewardGroups(ActiveRunDto run)
    {
        var campaign = CampaignWithDefaults();
        var definition = campaign.RunOffers[RunOfferKind.CombatReward];
        if (run.Growth is null || !campaign.SeparateItemRewardCategories)
            return [CreateOffer(run, definition)];
        return RunOfferDefaults.ItemRewardGroups(definition, id => content.TryGet(id, out var entry)
                ? entry : throw new InvalidOperationException("Missing reward content: " + id))
            .Select(group => CreateOffer(run, group)).Where(group => !group.Choices.IsEmpty).ToImmutableArray();
    }

    private CompiledCampaign CampaignWithDefaults() => project.Campaign.RunOffers.Count == 0
            ? RunOfferDefaults.WithDefaults(project.Campaign, project.RunRules,
                id => content.TryGet(id, out var entry) ? entry : throw new InvalidOperationException("Missing offer content: " + id))
            : project.Campaign;

    public PendingRunOffer CreateOffer(ActiveRunDto run, CompiledRunOffer definition,
        string? frozenIdentity = null, ImmutableArray<int> recruitmentWeights = default, string? samplingIdentity = null)
    {
        var identity = frozenIdentity ?? $"{run.Seed:x16}:{run.FloorIndex}:{run.BattleNumber}:{definition.StableId}";
        var samplingKey = samplingIdentity ?? identity;
        var selected = new HashSet<string>(StringComparer.Ordinal);
        var fixedChoices = definition.Choices
            .Where(choice => RunItemSupplyPolicy.CanOffer(run, choice, content) &&
                RunRecruitmentPolicy.TrySelect(run, choice, selected)).ToImmutableArray();
        var poolOrder = definition.PoolChoices.OrderBy(choice => StableRoll(samplingKey + ":" + choice.StableId)).AsEnumerable();
        if (definition.PoolAction == RunPoolAction.Recruit && project.Campaign.RecruitmentSupply is { } supply)
        {
            // Authored pool recipes each grant one hero. Use the operation identity,
            // never the presentation id, to preserve ordinary uniqueness semantics.
            var eligible = poolOrder.Where(choice => RunRecruitmentPolicy.CanOffer(run, choice))
                .Where(choice => choice.Operations.Length == 1 && choice.Operations[0].Kind == RunOperationKind.Recruit)
                .Where(choice => !selected.Contains(choice.Operations[0].ContentId))
                .GroupBy(choice => choice.Operations[0].ContentId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            poolOrder = RecruitmentSupplySampler.Draw(supply, eligible.Keys, recruitmentWeights.IsDefault ? supply.WeightsAt(run.FloorIndex) : recruitmentWeights,
                definition.PoolChoiceCount, samplingKey).Select(id => eligible[id]);
        }
        var pooled = poolOrder
            .Where(choice => RunItemSupplyPolicy.CanOffer(run, choice, content) &&
                RunRecruitmentPolicy.TrySelect(run, choice, selected))
            .Take(definition.PoolChoiceCount).ToImmutableArray();
        if (definition.PoolAction == RunPoolAction.PayGoldOrHealth)
            pooled = definition.PoolChoices.Where(choice => RunItemSupplyPolicy.CanOffer(run, choice, content))
                .GroupBy(choice => choice.ContentId, StringComparer.Ordinal)
                .OrderBy(group => StableRoll(samplingKey + ":" + group.Key)).ThenBy(group => group.Key, StringComparer.Ordinal)
                .Take(1).SelectMany(group => group).ToImmutableArray();
        var choices = fixedChoices.AddRange(pooled);
        return new PendingRunOffer(identity, definition.Kind, definition.DisplayName, definition.AllowSkip || choices.IsEmpty, definition.Repeatable,
            frozenIdentity is null ? run.FloorIndex : -1, frozenIdentity is null ? run.BattleNumber : 0, choices)
            { RecipeId = definition.StableId, RewardCategory = definition.RewardCategory, SamplingKey = samplingIdentity ?? string.Empty };
    }

    public RunDecisionResult Resolve(ActiveRunDto? run, string offerId, string? choiceId)
    {
        if (run?.PendingOffer is not { } offer) return RunDecisionResult.Reject(RunDecisionFailure.NoOffer, "没有待处理的选择。");
        if (offer.OfferId != offerId) return RunDecisionResult.Reject(RunDecisionFailure.StaleOffer, "这个选择已失效，请查看当前机会。");
        if (!persistence.ValidateRun(run)) return RunDecisionResult.Reject(RunDecisionFailure.InvalidOperation, "当前征程状态无效，未作修改。");
        offer = RelicRules.PresentOffer(run, VisibleOffer(run, RunOfferDefaults.RefreshHealthChoices(offer, project.RunRules)));
        var working = persistence.CloneRun(run);
        var evaluation = Evaluator.ApplyChoice(working, offer, choiceId);
        if (!evaluation.Succeeded) return evaluation;
        var chanceSucceeded = evaluation.ChanceSucceeded;
        if (choiceId is null || !offer.Repeatable)
        {
            if (offer.Kind == RunOfferKind.CombatReward && !working.PendingRewardGroups.IsEmpty)
            {
                working.PendingOffer = working.PendingRewardGroups[0];
                working.PendingRewardGroups = working.PendingRewardGroups.RemoveAt(0);
            }
            else working.PendingOffer = null;
            if (offer.Kind != RunOfferKind.CombatReward)
            {
                if (growth is not null && !growth.SettleNode(working))
                    return RunDecisionResult.Reject(RunDecisionFailure.InvalidOperation, "成长节点结算无效，选择、收益与推进均未提交。");
                working.FloorIndex++;
                working.PendingNode = false;
            }
        }
        else
        {
            // Repeatable permits buying other inventory in this visit. It never
            // replenishes the just-sold stock or implicitly redraws the shelf.
            working.PendingOffer = VisibleOffer(working, working.PendingOffer! with
            {
                Choices = working.PendingOffer!.Choices.Where(choice => choice.StableId != choiceId).ToImmutableArray()
            });
        }
        if (!persistence.ValidateRun(working)) return RunDecisionResult.Reject(RunDecisionFailure.InvalidOperation, "结果不符合征程规则，未作修改。");
        var changes = RunChoiceEvaluator.Changes(run, working);
        if (!persistence.TryPublish(working, run)) return RunDecisionResult.Reject(RunDecisionFailure.PersistenceFailed, "保存失败，机会、代价和原状态均已保留，请重试。");
        return new RunDecisionResult(true, RunDecisionFailure.None, chanceSucceeded ? "选择已完成。" : "冒险未成功，已结算该选项注明的结果。", chanceSucceeded) { Changes = changes };
    }

    public RunDecisionResult Check(ActiveRunDto run, CompiledRunChoice choice) => Evaluator.Check(run, choice);
    private RunChoiceEvaluator Evaluator => new(content, project, persistence,
        new RunRewardEconomyService(content, project, persistence, null, growth));
    private PendingRunOffer VisibleOffer(ActiveRunDto run, PendingRunOffer offer) =>
        RunItemSupplyPolicy.VisibleOffer(run, RunRecruitmentPolicy.VisibleOffer(run, offer), content);
    private static uint StableRoll(string key) => System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
}
