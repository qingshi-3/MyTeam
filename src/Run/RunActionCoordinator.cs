using System;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Expedition;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;

namespace TowerAutobattler.Run;

// The expedition's only action transaction owner. Navigation and authored choices
// project facts into a clone; this boundary validates and publishes the whole result.
public sealed class RunActionCoordinator(ContentRegistry content, CompiledGameProject project,
    RunProgressionPersistenceService persistence, GrowthRunService growth,
    RunRewardEconomyService rewards, RunDecisionService offers, RunBattlePreparationService preparation)
{
    private CompiledExpedition Definition => (CompiledExpedition)project.Campaign.Progression;
    private RunChoiceEvaluator Choices => new(content, project, persistence, rewards);
    public bool Locked(ActiveRunDto run) => run.OpeningRecruitment is not null || run.Action is not null ||
        run.Growth?.PendingDiscovery is not null || !string.IsNullOrEmpty(run.TerminalCompletionId);
    public bool CanMove(ActiveRunDto run, int cell) => run.Expedition is { RemainingActions: > 0 } map &&
        !Locked(run) && !ExpeditionNavigation.FindKnownPath(map, cell).IsEmpty;
    public bool CanInteract(ActiveRunDto run, string id) => run.Expedition is { RemainingActions: > 0 } map &&
        !Locked(run) && ExpeditionNavigation.CanInteractWithSite(map, id) && map.Sites.Any(site => site.SiteId == id &&
            !site.Cleared && (!site.Handled || SiteDefinition(site).Kind == TowerNodeType.Shop));
    public bool CanBoss(ActiveRunDto run) => run.Expedition is not null && !Locked(run);

    public bool Initialize(ActiveRunDto run)
    {
        try
        {
            var map = ExpeditionMapGenerator.Generate(Definition, run.Seed);
            foreach (var site in map.Sites)
            {
                var identity = $"{run.RunId}:site:{site.SiteId}:{site.ContentSeed:x16}";
                var definition = SiteDefinition(site);
                if (RunOfferSampling.SiteRecipe(definition, site, map.GenerationVersion) is { } offer)
                    site.ServiceOffer = offers.CreateOffer(run, offer, identity + ":" + offer.StableId,
                        Definition.Biomes[map.Cells[site.CellIndex].BiomeIndex].RecruitmentWeights,
                        $"exp:{map.GenerationVersion}:{site.ContentSeed:x16}:{offer.StableId}");
                if (site.EncounterProfileId.Length > 0)
                    site.RewardGroups = FreezeRewards(run, Definition.Encounters[site.EncounterProfileId].Reward, identity, $"exp:{map.GenerationVersion}:{site.ContentSeed:x16}");
            }
            for (var stage = 0; stage < Definition.Stages.Length; stage++)
                map.StageBossRewardGroups.Add(FreezeRewards(run, Definition.Encounters[Definition.Stages[stage].BossProfileId].Reward,
                    $"{run.RunId}:stage:{stage}:boss", $"exp:{map.GenerationVersion}:{run.Seed:x16}:boss:{stage}"));
            run.Expedition = map;
            run.PendingNode = false;
            return true;
        }
        catch (Exception exception) { Godot.GD.PushWarning("大陆生成失败，保留选队草稿：" + exception.Message); return false; }
    }

    private ImmutableArray<PendingRunOffer> FreezeRewards(ActiveRunDto run, CompiledRewardRecipe recipe, string identity, string samplingKey) =>
        recipe.Groups.Select(group => offers.CreateOffer(run, group, identity + ":" + group.StableId, samplingIdentity: samplingKey + ":" + group.StableId)).ToImmutableArray();
    private CompiledExpeditionSite SiteDefinition(ExpeditionSiteDto site) => Definition.Sites.Single(value => value.StableId == site.DefinitionId);

    public bool Move(ActiveRunDto run, int target)
    {
        if (!CanMove(run, target)) return false;
        var path = ExpeditionNavigation.FindKnownPath(run.Expedition!, target);
        foreach (var cell in path)
        {
            var working = persistence.CloneRun(run);
            if (!ExpeditionNavigation.TryStep(working.Expedition!, cell, out _) || !persistence.TryPublish(working, run)) return false;
        }
        return true;
    }

    public EncounterPlan Preview(ActiveRunDto run, string siteId, bool boss = false)
    {
        var map = run.Expedition ?? throw new InvalidOperationException("尚未进入大陆。");
        ExpeditionSiteDto? site = null;
        if (!boss)
        {
            if (!ExpeditionNavigation.CanInspectSite(map, siteId)) throw new InvalidOperationException("尚未发现该地点。");
            site = map.Sites.Single(value => value.SiteId == siteId);
            if (site.EncounterProfileId.Length == 0) throw new InvalidOperationException("此处没有敌人。");
        }
        var profile = Definition.Encounters[boss ? Definition.Stages[map.StageIndex].BossProfileId : site!.EncounterProfileId];
        var composition = profile.Composition;
        return new(profile.Encounter.Title(boss ? Definition.Stages[map.StageIndex].DisplayName : Definition.Biomes[map.Cells[site!.CellIndex].BiomeIndex].DisplayName) + " · " + composition.DisplayName,
            boss ? map.StageBossFloorRuleIds[map.StageIndex] : site!.FloorRuleId,
            composition.EnemyIds, boss, profile.Encounter.NodeType == TowerNodeType.Elite,
            profile.Encounter.StableId, profile.Encounter.NodeType)
        {
            CompositionId = composition.StableId, EnemyCells = composition.Cells,
            ExpeditionProfileId = profile.StableId, ExpeditionSiteId = boss ? "" : siteId, ExpeditionBoss = boss
        };
    }

    public PendingRunOffer? SiteOffer(ActiveRunDto run, string siteId)
    {
        if (run.Expedition is not { } map || !ExpeditionNavigation.CanInspectSite(map, siteId)) return null;
        var site = map.Sites.Single(value => value.SiteId == siteId);
        return site.ServiceOffer is { } offer ? Present(run, offer) : null;
    }

    public bool Inspect(ActiveRunDto run, string siteId) => CanInteract(run, siteId);

    private static RunActionDto NewAction(ActiveRunDto run, string siteId, bool boss)
    {
        var sequence = run.NextActionSequence;
        run.NextActionSequence = checked(sequence + 1);
        return run.Action = new RunActionDto {
            Source = new($"{run.RunId}:action:{sequence}", sequence, run.Expedition!.StageIndex, siteId, null),
            IsStageBoss = boss
        };
    }
    private static PendingRunOffer Activate(ActiveRunDto run, PendingRunOffer offer) => offer with {
        Source = run.Action!.Source, FloorIndex = -1, BattleNumber = run.BattleNumber
    };
    private PendingRunOffer Present(ActiveRunDto run, PendingRunOffer offer) => rewards.RelicRules.PresentOffer(run,
        RunItemSupplyPolicy.VisibleOffer(run, RunRecruitmentPolicy.VisibleOffer(run, offer), content));

    public BattleConfig BuildBattlePreview(ActiveRunDto run, EncounterPlan plan, bool requireLegalFormation)
    {
        if (run.Action is { Phase: RunActionPhase.Battle })
            return preparation.Build(run, plan, requireLegalFormation);
        // The same entry benefits affect both the displayed army and the final
        // battle. Preview keeps its distinct receipt origin and never saves.
        var preview = persistence.CloneRun(run);
        rewards.RelicRules.EnterNode(preview, plan.NodeType);
        return preparation.Build(preview, plan, requireLegalFormation);
    }

    public GrowthCommandResult BeginBattle(ActiveRunDto run, EncounterPlan plan)
    {
        if (run.Action is { Phase: RunActionPhase.Battle, BattleFreeze: not null } existing)
            return SamePlan(existing.Encounter, plan) && persistence.ValidateRun(run)
                ? GrowthCommandResult.Success() : GrowthCommandResult.Reject("冻结的战斗与请求不一致。");
        if (plan.ExpeditionBoss ? !CanBoss(run) : !CanInteract(run, plan.ExpeditionSiteId))
            return GrowthCommandResult.Reject("现在不能进入该战斗。");
        var expected = Preview(run, plan.ExpeditionSiteId, plan.ExpeditionBoss);
        if (!SamePlan(expected, plan)) return GrowthCommandResult.Reject("遭遇已变化，请重新查看。");
        var working = persistence.CloneRun(run);
        var action = NewAction(working, plan.ExpeditionSiteId, plan.ExpeditionBoss);
        action.Phase = RunActionPhase.Battle; action.Encounter = expected; action.ActionPaid = true;
        if (plan.ExpeditionBoss) working.Expedition!.RemainingActions = 0;
        else working.Expedition!.RemainingActions -= SiteDefinition(working.Expedition.Sites.Single(site => site.SiteId == plan.ExpeditionSiteId)).ActionCost;
        // Capture participants/trigger owners before entry benefits; consume the spell once.
        var frozen = growth.BeginBattleProjection(working, expected);
        if (!frozen.Succeeded) return frozen;
        rewards.RelicRules.EnterNode(working, expected.NodeType);
        action.BattleFreeze = new() { Gold = working.Gold, BattleNumber = working.BattleNumber,
            InputsFingerprint = RunProgressionContext.BattleInputsFingerprint(working) };
        try { preparation.Build(working, expected, true); }
        catch (Exception exception) { return GrowthCommandResult.Reject("战斗配置无法启动：" + exception.Message); }
        return persistence.TryPublish(working, run) ? GrowthCommandResult.Success() : GrowthCommandResult.Reject("保存失败，行动与资源未消耗。");
    }

    public RunDecisionResult CheckChoice(ActiveRunDto run, CompiledRunChoice choice, string? previewSiteId)
    {
        var siteId = run.Action is { Phase: RunActionPhase.Service } service ? service.Source.SiteId : previewSiteId;
        var site = run.Expedition?.Sites.SingleOrDefault(value => value.SiteId == siteId);
        if (site is null || site.Handled) return Choices.Check(run, choice);
        // Affordability previews use the same entry benefits as the transaction,
        // without consuming the entitlement or mutating the authoritative run.
        var working = persistence.CloneRun(run);
        rewards.RelicRules.EnterNode(working, SiteDefinition(site).Kind);
        return Choices.Check(working, choice);
    }

    public RunDecisionResult Resolve(ActiveRunDto run, string offerId, string? choiceId, string? previewSiteId = null)
    {
        if (!persistence.ValidateRun(run) || run.Growth?.PendingDiscovery is not null)
            return RunDecisionResult.Reject(RunDecisionFailure.NotEligible, "请先完成当前事务。");
        if (run.Action is null && choiceId is null && previewSiteId is not null &&
            SiteOffer(run, previewSiteId)?.OfferId == offerId)
            return new(true, RunDecisionFailure.None, "已返回大陆。");
        var working = persistence.CloneRun(run);
        if (working.Action is null && previewSiteId is not null && CanInteract(run, previewSiteId))
        {
            var stockSite = working.Expedition!.Sites.Single(site => site.SiteId == previewSiteId);
            if (stockSite.ServiceOffer is { } stock && stock.OfferId == offerId)
            {
                var opened = NewAction(working, previewSiteId, false);
                opened.Phase = RunActionPhase.Service;
                opened.ActionPaid = stockSite.Handled;
                opened.ServiceOffer = Activate(working, stock);
            }
        }
        if (working.Action is not { } action || action.CurrentOffer is not { } pending || pending.OfferId != offerId || action.Phase == RunActionPhase.Battle)
            return RunDecisionResult.Reject(RunDecisionFailure.StaleOffer, "这个机会已失效。");
        var current = working.Action!;
        var isService = current.Phase == RunActionPhase.Service;
        var site = isService ? working.Expedition!.Sites.Single(site => site.SiteId == current.Source.SiteId) : null;
        // Leaving a shop does not trigger entry, production, or an encounter charge.
        if (isService && choiceId is null)
        {
            CloseAction(working);
            return CommitChoice(run, working, new(true, RunDecisionFailure.None, "已返回大陆。"));
        }
        if (isService && !current.ActionPaid)
        {
            if (working.Expedition!.RemainingActions < SiteDefinition(site!).ActionCost || !growth.FreezeNonCombat(working))
                return RunDecisionResult.Reject(RunDecisionFailure.NotEligible, "本阶段行动已用完。");
            rewards.RelicRules.EnterNode(working, SiteDefinition(site!).Kind);
        }
        var evaluation = Choices.ApplyChoice(working, Present(working, pending), choiceId);
        if (!evaluation.Succeeded) return evaluation;
        if (isService)
        {
            if (!current.ActionPaid)
            {
                working.Expedition!.RemainingActions -= SiteDefinition(site!).ActionCost;
                current.ActionPaid = true; site!.Handled = true;
                if (!growth.SettleNode(working)) return RunDecisionResult.Reject(RunDecisionFailure.InvalidOperation, "成长结算失败。");
            }
            if (pending.Repeatable)
            {
                site!.ServiceOffer = site.ServiceOffer! with { Choices = site.ServiceOffer!.Choices.Where(c => c.StableId != choiceId).ToImmutableArray() };
                current.ServiceOffer = Activate(working, site.ServiceOffer);
                if (working.Expedition!.RemainingActions == 0 || site.ServiceOffer.Choices.IsEmpty) CloseAction(working);
            }
            else CloseAction(working);
        }
        else
        {
            current.RewardCursor++;
            AdvanceExhaustedRewards(working);
        }
        return CommitChoice(run, working, evaluation);
    }

    private RunDecisionResult CommitChoice(ActiveRunDto before, ActiveRunDto working, RunDecisionResult result)
    {
        var changes = RunChoiceEvaluator.Changes(before, working);
        return persistence.TryPublish(working, before)
            ? result with { Changes = changes, Message = result.ChanceSucceeded ? result.Message : "冒险未成功，已结算该选项注明的结果。" }
            : RunDecisionResult.Reject(RunDecisionFailure.PersistenceFailed, "保存失败，原机会和资源已保留。");
    }
    public bool LeaveService(ActiveRunDto run)
    {
        if (run.Action is not { Phase: RunActionPhase.Service }) return false;
        var working = persistence.CloneRun(run); CloseAction(working);
        return persistence.TryPublish(working, run);
    }

    public RunBattleResolution Complete(ActiveRunDto run, BattleResult result, EncounterPlan encounter)
    {
        if (run.Action is not { Phase: RunActionPhase.Battle, BattleFreeze: not null } action ||
            !persistence.ValidateRun(run) || !SamePlan(action.Encounter, encounter) ||
            result.Identity is not { } identity || identity.OriginKey != action.Source.ActionId || identity.FloorIndex != -1 ||
            identity.RunSeed != run.Seed || identity.BattleNumber != run.BattleNumber || identity.EncounterId != encounter.EncounterId ||
            identity.CompositionId != encounter.CompositionId || identity.NodeType != encounter.NodeType ||
            result.Outcome is not (BattleOutcome.PlayerVictory or BattleOutcome.PlayerDefeat or BattleOutcome.Timeout) ||
            result.GoldSpent < 0 || result.GoldSpent > action.BattleFreeze.Gold)
            return new(false, false, run, result.Outcome, RunBattleResolutionFailure.Rejected);
        var victory = result.Outcome == BattleOutcome.PlayerVictory;
        var finalVictory = victory && action.IsStageBoss && run.Expedition!.StageIndex == Definition.Stages.Length - 1;
        var ended = finalVictory || !victory && (action.IsStageBoss || run.CurrentRunHealth <= RunHealthPolicy.DefeatLoss(project.RunRules, encounter.NodeType));
        var working = persistence.CloneRun(run);
        if (!new RunBattleResultProjector(project, preparation, rewards, growth).TryApply(run, working, result, encounter, ended, out var consequence))
            return new(false, false, run, result.Outcome, RunBattleResolutionFailure.Rejected);
        working.LastBattleConsequence = consequence;
        if (ended)
        {
            GrowthRunService.SetPending(working, null);
            working.Action = null;
            working.TerminalCompletionId = Guid.NewGuid().ToString("N"); working.TerminalVictory = finalVictory;
            if (!persistence.TryPublish(working, run) || !persistence.TryCompleteTerminal(run))
                return new(false, false, run, result.Outcome, RunBattleResolutionFailure.PersistenceFailed);
            return new(true, finalVictory, null, result.Outcome, RunBattleResolutionFailure.None) { Consequence = consequence };
        }
        if (!growth.SettleNode(working)) return new(false, false, run, result.Outcome, RunBattleResolutionFailure.Rejected);
        var settled = working.Action!;
        settled.BattleFreeze = null; settled.Phase = RunActionPhase.Rewards;
        if (victory)
        {
            if (settled.IsStageBoss) settled.RewardGroups = working.Expedition!.StageBossRewardGroups[working.Expedition.StageIndex].Select(offer => Activate(working, offer)).ToImmutableArray();
            else
            {
                var site = working.Expedition!.Sites.Single(site => site.SiteId == settled.Source.SiteId);
                site.Cleared = true;
                settled.RewardGroups = site.RewardGroups.Select(offer => Activate(working, offer)).ToImmutableArray();
            }
        }
        AdvanceExhaustedRewards(working);
        if (!persistence.TryPublish(working, run)) return new(false, false, run, result.Outcome, RunBattleResolutionFailure.PersistenceFailed);
        return new(true, victory, run, result.Outcome, RunBattleResolutionFailure.None) { Consequence = consequence };
    }

    private void AdvanceExhaustedRewards(ActiveRunDto run)
    {
        while (run.Action!.CurrentOffer is { } offer && Present(run, offer).Choices.IsEmpty) run.Action.RewardCursor++;
        if (run.Action.CurrentOffer is null) CloseAction(run);
    }
    private void CloseAction(ActiveRunDto run)
    {
        var action = run.Action!;
        // A source with no payment and no production is merely a cancelled visit.
        if (action.ActionPaid && !run.CompletedActionIds.Contains(action.Source.ActionId)) run.CompletedActionIds.Add(action.Source.ActionId);
        if (action.IsStageBoss)
        {
            run.Expedition!.StageIndex++;
            run.Expedition.RemainingActions = Definition.Stages[run.Expedition.StageIndex].Actions;
        }
        run.Action = null;
    }
    public static bool SamePlan(EncounterPlan? a, EncounterPlan? b) => a is not null && b is not null &&
        a.EncounterId == b.EncounterId && a.CompositionId == b.CompositionId && a.FloorRuleId == b.FloorRuleId &&
        a.ExpeditionProfileId == b.ExpeditionProfileId && a.ExpeditionSiteId == b.ExpeditionSiteId && a.ExpeditionBoss == b.ExpeditionBoss &&
        a.NodeType == b.NodeType && a.EnemyIds.SequenceEqual(b.EnemyIds) && a.EnemyCells.SequenceEqual(b.EnemyCells);
}
