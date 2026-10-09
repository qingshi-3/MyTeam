using System;
using System.Collections.Generic;
using System.Linq;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;
using TowerAutobattler.Expedition;

namespace TowerAutobattler.Run;

// Pure configuration validator shared by persistence and non-persistent tools.
// It performs no loads, saves, migration, publication, or mutation.
public static class ActiveRunConfigurationValidator
{
    public static bool Validate(
        ActiveRunDto run,
        ContentRegistry content,
        CompiledGameProject project)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(project);
        var rules = project.RunRules;
        var expedition = project.Campaign.Progression as CompiledExpedition;
        if (run is null || run.Roster is null || run.Deployment is null || run.Items is null || run.EquipmentInventory is null ||
            run.EquippedTacticalCommandIds is null || run.PopulationCapSources is null ||
            run.CompletedActionIds is null || run.CompletedActionIds.Any(string.IsNullOrWhiteSpace) ||
            run.CompletedActionIds.Distinct(StringComparer.Ordinal).Count() != run.CompletedActionIds.Count ||
            run.Revision < 0 || run.NextActionSequence < 1 || run.CompletedActionIds.Count >= run.NextActionSequence ||
            run.PendingRewardGroups.IsDefault ||
            run.Version != ActiveRunFormationSchema.CurrentVersion || run.FloorIndex < 0 || run.Gold < 0 || run.BattleNumber < 0 ||
            run.LegacyHeroId is not null || run.LegacyHeroHealthRatio != 0 ||
            run.LegacyHeroCell is not null || run.LegacyDeploymentCells is not null ||
            run.Deployment.Any(id => id is null))
            return false;
        if (run.SelectedOpponentOptionId is null) return false;
        if (expedition is null)
        {
            if (project.Campaign.Progression is not CompiledTowerProgression || run.Tower is null || run.Expedition is not null || run.Action is not null ||
                run.NextActionSequence != 1 || run.CompletedActionIds.Count != 0 ||
                run.FloorIndex >= project.Campaign.TotalFloors || !ValidateOpponentSelection(run, project)) return false;
        }
        else if (run.Tower is not null || run.FloorIndex != 0 || run.PendingNode || run.SelectedNode != default ||
                 run.SelectedOpponentOptionId.Length != 0 || run.LegacyPendingOpponent || !run.PendingRewardGroups.IsEmpty ||
                 run.Growth?.PendingNode is not null || !Guid.TryParseExact(run.RunId, "N", out _)) return false;
        if (run.Action is { } provisionalAction && provisionalAction.RewardGroups.IsDefault) return false;
        if (!RunHealthPolicy.ValidConsequence(run.LastBattleConsequence) ||
            run.MaximumRunHealth <= 0 || run.CurrentRunHealth < 0 || run.CurrentRunHealth > run.MaximumRunHealth ||
            run.CurrentRunHealth == 0 && (string.IsNullOrEmpty(run.TerminalCompletionId) || run.TerminalVictory))
            return false;
        if (!RunPopulationPolicy.Validate(run, rules) ||
            !ActiveRunTacticalCommandPolicy.Validate(run, rules, content.Graph) ||
            run.Deployment.Count != rules.PhysicalDeploymentCeiling)
            return false;
        if (run.OpeningRecruitment is { } opening)
        {
            // An empty roster is valid only for this explicitly persisted opening phase.
            // The test campaign opens its authored pool independently of legacy account unlocks.
            var supply = project.Campaign.RecruitmentSupply;
            return supply is not null && !opening.CandidateIds.IsDefault && !opening.SelectedIds.IsDefault &&
                opening.CandidateIds.Length == CompiledRecruitmentSupply.OpeningCandidateCount &&
                opening.CandidateIds.Distinct(StringComparer.Ordinal).Count() == opening.CandidateIds.Length &&
                opening.CandidateIds.All(id => !string.IsNullOrWhiteSpace(id) && supply.TierOf(id) is > 0 &&
                    supply.OpeningTierWeights[supply.TierOf(id) - 1] > 0 &&
                    content.TryGet(id, out var entry) && entry.Definition is UnitDefinition { IsHero: true, IsEnemy: false, IsTestDummy: false }) &&
                opening.SelectedIds.Length <= CompiledRecruitmentSupply.OpeningSelectionCount &&
                opening.SelectedIds.Distinct(StringComparer.Ordinal).Count() == opening.SelectedIds.Length &&
                opening.SelectedIds.All(id => opening.CandidateIds.Contains(id)) &&
                run.Roster.Count == 0 && run.Deployment.All(string.IsNullOrEmpty) &&
                run.Expedition is null && run.Action is null && run.CompletedActionIds.Count == 0 && run.NextActionSequence == 1 &&
                run.FloorIndex == 0 && run.BattleNumber == 0 && !run.PendingNode && run.PendingOffer is null && run.PendingRewardGroups.IsEmpty &&
                string.IsNullOrEmpty(run.TerminalCompletionId) && !run.TerminalVictory && run.Seed != 0 &&
                run.Items.Count == 0 && run.EquipmentInventory.Count == 0 && run.PopulationCapSources.Count == 0 &&
                run.CurrentPopulation == rules.InitialPopulation && run.Gold == rules.StartingGold;
        }
        if (run.Roster.Count == 0 || run.Roster[0] is null ||
            !content.TryGet(run.Roster[0].ContentId, out var startingHero) ||
            startingHero.Definition is not UnitDefinition { IsHero: true }) return false;
        if (run.TerminalCompletionId is null || run.TerminalVictory && string.IsNullOrEmpty(run.TerminalCompletionId) ||
            !string.IsNullOrEmpty(run.TerminalCompletionId) && !Guid.TryParseExact(run.TerminalCompletionId, "N", out _) ||
            !string.IsNullOrEmpty(run.TerminalCompletionId) && run.PendingOffer is not null ||
            !RunDecisionValidation.ValidOffer(run.PendingOffer, run,
                id => content.TryGet(id, out _),
                id => content.TryGet(id, out var entry) && entry.Definition is UnitDefinition { IsEnemy: false, IsTestDummy: false },
                id => content.TryGet(id, out var entry) && entry.Definition is ItemDefinition)) return false;
        if (expedition is not null)
        {
            if (!ValidateExpedition(run, content, expedition)) return false;
        }
        else
        {
            if (!ValidateRewardGroups(run, content)) return false;
            if (run.PendingOffer is { } pending &&
                (pending.Kind == Project.RunOfferKind.CombatReward ? run.PendingNode :
                    !run.PendingNode || RunDecisionService.KindFor(run.SelectedNode) != pending.Kind)) return false;
            if (run.PendingNode && RunDecisionService.KindFor(run.SelectedNode) is not null && run.PendingOffer is null) return false;
        }

        var instanceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var unit in run.Roster)
            if (unit is null || string.IsNullOrWhiteSpace(unit.InstanceId) || !instanceIds.Add(unit.InstanceId) ||
                !float.IsFinite(unit.HealthRatio) || unit.HealthRatio < 0 || unit.HealthRatio > 1 || unit.Rank <= 0 ||
                unit.Equipment is null || unit.Equipment.Count > rules.EquipmentSlotCapacity ||
                !content.TryGet(unit.ContentId, out var entry) ||
                entry.Definition is not UnitDefinition { IsEnemy: false, IsTestDummy: false })
                return false;
        if (run.Deployment.Any(id => !string.IsNullOrEmpty(id) && !instanceIds.Contains(id))) return false;
        var deployedIds = run.Deployment.Where(id => !string.IsNullOrEmpty(id)).ToArray();
        if (deployedIds.Distinct(StringComparer.Ordinal).Count() != deployedIds.Length ||
            !RunFormationPolicy.Validate(run, rules))
            return false;

        var durableInstanceIds = new HashSet<string>(instanceIds, StringComparer.Ordinal);
        foreach (var unit in run.Roster)
        {
            var slots = new HashSet<int>();
            foreach (var equipment in unit.Equipment)
                if (equipment is null || string.IsNullOrWhiteSpace(equipment.InstanceId) ||
                    !durableInstanceIds.Add(equipment.InstanceId) || equipment.OwnerHeroInstanceId != unit.InstanceId ||
                    equipment.SlotIndex < 0 || equipment.SlotIndex >= rules.EquipmentSlotCapacity ||
                    !slots.Add(equipment.SlotIndex) || !content.TryGet(equipment.ContentId, out var entry) ||
                    entry.Definition is not ItemDefinition { ProductKind: ItemProductKind.Equipment } ||
                    !content.Graph.TryGetEquipment(equipment.ContentId, out _))
                    return false;
        }
        foreach (var equipment in run.EquipmentInventory)
            if (equipment is null || string.IsNullOrWhiteSpace(equipment.InstanceId) ||
                !durableInstanceIds.Add(equipment.InstanceId) || equipment.OwnerHeroInstanceId != string.Empty ||
                equipment.SlotIndex != -1 || !content.TryGet(equipment.ContentId, out var entry) ||
                entry.Definition is not ItemDefinition { ProductKind: ItemProductKind.Equipment } ||
                !content.Graph.TryGetEquipment(equipment.ContentId, out _))
                return false;
        foreach (var item in run.Items)
        {
            if (item is null || item.Stacks <= 0 || item.Charges < 0 || item.Counters is null ||
                string.IsNullOrWhiteSpace(item.InstanceId) || !durableInstanceIds.Add(item.InstanceId) ||
                !content.TryGet(item.ContentId, out var entry) ||
                entry.Definition is not ItemDefinition { ProductKind: ItemProductKind.Relic } ||
                !content.Graph.TryGetRelic(item.ContentId, out var relic) ||
                !RunRelicRules.ValidateRunRuleStates(run, item, relic, project) ||
                !RelicRunScope.HasExactRunCounterSet(relic,
                    item.Counters.Select(counter => counter is null ? null! :
                        new RelicCounterStateSnapshot(counter.CounterId, counter.Value)), out _))
                return false;
        }
        return true;
    }

    private static bool ValidateExpedition(ActiveRunDto run, ContentRegistry content, CompiledExpedition definition)
    {
        if (run.Tower is not null || run.Expedition is not { } map || map.Seed != run.Seed ||
            !ExpeditionMapValidation.ValidateSaved(definition, map).IsEmpty) return false;
        bool HasContent(string id) => content.TryGet(id, out _);
        bool Recruitable(string id) => content.TryGet(id, out var entry) && entry.Definition is UnitDefinition { IsEnemy: false, IsTestDummy: false };
        bool Item(string id) => content.TryGet(id, out var entry) && entry.Definition is ItemDefinition;
        bool Stock(PendingRunOffer? offer) => RunDecisionValidation.ValidStockOffer(offer, HasContent, Recruitable, Item);
        bool Active(PendingRunOffer? offer) => RunDecisionValidation.ValidOffer(offer, run, HasContent, Recruitable, Item);
        var siteDefinitions = definition.Sites.ToDictionary(site => site.StableId, StringComparer.Ordinal);
        var offerIds = new HashSet<string>(StringComparer.Ordinal);
        if (map.StageBossRewardGroups is null || map.StageBossRewardGroups.Count != definition.Stages.Length) return false;
        for (var stage = 0; stage < definition.Stages.Length; stage++)
        {
            var groups = map.StageBossRewardGroups[stage];
            var recipe = definition.Encounters[definition.Stages[stage].BossProfileId].Reward;
            if (!MatchesRewardGroups(groups, recipe.Groups, Stock, content)) return false;
            foreach (var group in groups) if (!offerIds.Add(group.OfferId)) return false;
        }
        foreach (var site in map.Sites)
        {
            if (site.RewardGroups.IsDefault) return false;
            var source = siteDefinitions[site.DefinitionId];
            if (ExpeditionMapGenerator.IsEnemy(source.Kind))
            {
                var recipe = definition.Encounters[site.EncounterProfileId].Reward;
                if (!MatchesRewardGroups(site.RewardGroups, recipe.Groups, Stock, content)) return false;
                foreach (var group in site.RewardGroups) if (!offerIds.Add(group.OfferId)) return false;
            }
            else
            {
                var selectedRecipe = RunOfferSampling.SiteRecipe(source, site, map.GenerationVersion);
                if (site.ServiceOffer is null)
                {
                    if (!site.Handled || source.Kind == TowerNodeType.Shop) return false;
                }
                else if (selectedRecipe is null || !Stock(site.ServiceOffer) || !MatchesRecipe(site.ServiceOffer, selectedRecipe) ||
                         !source.OfferRecipes.IsDefaultOrEmpty && (site.ServiceOffer.RecipeId != selectedRecipe.StableId ||
                             site.ServiceOffer.SamplingKey != $"exp:{map.GenerationVersion}:{site.ContentSeed:x16}:{selectedRecipe.StableId}") ||
                         !offerIds.Add(site.ServiceOffer.OfferId)) return false;
            }
        }
        if (!ValidateExpeditionHistory(run, definition, siteDefinitions)) return false;
        if (!string.IsNullOrEmpty(run.TerminalCompletionId))
            return run.Action is null && (!run.TerminalVictory || map.StageIndex == definition.Stages.Length - 1);
        if (run.Action is not { } action) return true;
        if (!Enum.IsDefined(action.Phase) || action.Source is not { } identity ||
            string.IsNullOrWhiteSpace(identity.ActionId) || identity.Sequence <= 0 || identity.Sequence != run.NextActionSequence - 1 ||
            identity.StageIndex != map.StageIndex || identity.SiteId is null || identity.LegacyFloorIndex is not null ||
            run.CompletedActionIds.Contains(identity.ActionId, StringComparer.Ordinal) ||
            action.RewardGroups.IsDefault || action.RewardCursor < 0 || action.RewardCursor > action.RewardGroups.Length)
            return false;

        ExpeditionSiteDto? siteForAction = null;
        CompiledExpeditionSite? siteDefinition = null;
        if (action.IsStageBoss)
        {
            if (identity.SiteId.Length != 0 || map.RemainingActions != 0 || !action.ActionPaid || action.Phase == RunActionPhase.Service)
                return false;
        }
        else
        {
            siteForAction = map.Sites.SingleOrDefault(site => site.SiteId == identity.SiteId);
            if (siteForAction is null || !ExpeditionNavigation.CanInteractWithSite(map, identity.SiteId)) return false;
            siteDefinition = siteDefinitions[siteForAction.DefinitionId];
        }

        if (action.Phase == RunActionPhase.Service)
        {
            if (siteDefinition is null || siteForAction is null ||
                RunOfferSampling.SiteRecipe(siteDefinition, siteForAction, map.GenerationVersion) is not { } recipe || action.IsStageBoss || action.Encounter is not null ||
                action.BattleFreeze is not null || action.GrowthSnapshot is not null || !action.RewardGroups.IsEmpty || action.RewardCursor != 0 ||
                action.ServiceOffer is null || !Active(action.ServiceOffer) || !MatchesRecipe(action.ServiceOffer, recipe) ||
                siteForAction!.ServiceOffer is null || action.ServiceOffer.OfferId != siteForAction.ServiceOffer.OfferId ||
                action.ServiceOffer.Choices.Any(choice => !siteForAction.ServiceOffer.Choices.Any(stock => SameChoice(choice, stock))) ||
                map.RemainingActions == 0) return false;
            return action.ActionPaid
                ? siteDefinition.Kind == TowerNodeType.Shop && siteForAction.Handled
                : !siteForAction.Handled;
        }

        if (!action.ActionPaid || action.ServiceOffer is not null || action.Encounter is not { } encounter ||
            !MatchesEncounter(encounter, definition, action, map, siteForAction)) return false;
        if (action.Phase == RunActionPhase.Battle)
        {
            if (siteForAction is { Cleared: true } || !action.RewardGroups.IsEmpty || action.RewardCursor != 0 ||
                action.BattleFreeze is not { } freeze || freeze.BattleNumber != run.BattleNumber || freeze.Gold != run.Gold ||
                string.IsNullOrWhiteSpace(freeze.InputsFingerprint) ||
                run.Growth is not null && action.GrowthSnapshot is not { IsBattle: true } ||
                action.GrowthSnapshot is { } snapshot && (snapshot.Source != identity || snapshot.BattleNumber != run.BattleNumber))
                return false;
            if (!action.IsStageBoss && map.RemainingActions > definition.Stages[map.StageIndex].Actions - siteDefinition!.ActionCost) return false;
            if (!MatchesBattleFingerprint(run, freeze.InputsFingerprint)) return false;
        }
        else if (action.Phase == RunActionPhase.Rewards)
        {
            if (action.BattleFreeze is not null || action.GrowthSnapshot is not null || siteForAction is { Cleared: false } ||
                !MatchesRewardGroups(action.RewardGroups, definition.Encounters[encounter.ExpeditionProfileId].Reward.Groups, Active, content))
                return false;
            // A cursor equal to Length is a valid exhausted queue. The coordinator
            // closes it and applies the stage transition in the same transaction.
        }
        else return false;
        return true;
    }

    private static bool MatchesBattleFingerprint(ActiveRunDto run, string fingerprint)
    {
        try { return StringComparer.Ordinal.Equals(fingerprint, RunProgressionContext.BattleInputsFingerprint(run)); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.Text.Json.JsonException)
        { return false; }
    }

    private static bool ValidateExpeditionHistory(ActiveRunDto run, CompiledExpedition definition,
        IReadOnlyDictionary<string, CompiledExpeditionSite> siteDefinitions)
    {
        if (run.Growth is null) return true;
        if (run.Growth.History is null) return false;
        var map = run.Expedition!;
        var sites = map.Sites.ToDictionary(site => site.SiteId, StringComparer.Ordinal);
        var spent = new long[definition.Stages.Length];
        var bossSettled = new bool[definition.Stages.Length];
        foreach (var settlement in run.Growth.History)
        {
            if (settlement is null || settlement.Source is not { } source ||
                string.IsNullOrWhiteSpace(source.ActionId) || source.Sequence <= 0 || source.Sequence >= run.NextActionSequence ||
                source.StageIndex < 0 || source.StageIndex > map.StageIndex || source.SiteId is null || source.LegacyFloorIndex is not null ||
                !run.CompletedActionIds.Contains(source.ActionId, StringComparer.Ordinal) && run.Action?.Source != source) return false;
            if (source.SiteId.Length == 0)
            {
                if (!settlement.IsBattle) return false;
                bossSettled[source.StageIndex] = true;
                continue;
            }
            if (!sites.TryGetValue(source.SiteId, out var site)) return false;
            var siteDefinition = siteDefinitions[site.DefinitionId];
            if (settlement.IsBattle != ExpeditionMapGenerator.IsEnemy(siteDefinition.Kind)) return false;
            spent[source.StageIndex] += siteDefinition.ActionCost;
        }
        if (run.Action is { Phase: RunActionPhase.Battle, ActionPaid: true, IsStageBoss: false } pending)
        {
            if (pending.Source is not { } source || source.StageIndex != map.StageIndex || source.SiteId is null ||
                !sites.TryGetValue(source.SiteId, out var site)) return false;
            spent[map.StageIndex] += siteDefinitions[site.DefinitionId].ActionCost;
        }
        for (var stage = 0; stage < map.StageIndex; stage++)
            if (!bossSettled[stage] || spent[stage] > definition.Stages[stage].Actions) return false;
        var consumed = definition.Stages[map.StageIndex].Actions - map.RemainingActions;
        var terminal = !string.IsNullOrEmpty(run.TerminalCompletionId);
        var abandonedRemainder = map.RemainingActions == 0 &&
            (run.Action is { IsStageBoss: true } || bossSettled[map.StageIndex] || terminal);
        if (spent[map.StageIndex] > consumed) return false;
        if (!abandonedRemainder)
        {
            // Terminal settlement does not grant another Growth production tick.
            // Its final ordinary battle may therefore have paid once without history.
            var unrecorded = terminal ? definition.Sites.Where(site => ExpeditionMapGenerator.IsEnemy(site.Kind))
                .Select(site => site.ActionCost).DefaultIfEmpty(0).Max() : 0;
            if (consumed - spent[map.StageIndex] > unrecorded) return false;
        }
        return true;
    }

    private static bool MatchesEncounter(EncounterPlan plan, CompiledExpedition definition, RunActionDto action,
        ExpeditionStateDto map, ExpeditionSiteDto? site)
    {
        if (plan.EnemyIds is null || plan.EnemyCells.IsDefault || string.IsNullOrEmpty(plan.ExpeditionProfileId) ||
            !definition.Encounters.TryGetValue(plan.ExpeditionProfileId, out var profile) ||
            plan.ExpeditionBoss != action.IsStageBoss || plan.ExpeditionSiteId != action.Source.SiteId ||
            plan.EncounterId != profile.Encounter.StableId || plan.NodeType != profile.Encounter.NodeType ||
            plan.CompositionId != profile.Composition.StableId || !plan.EnemyIds.SequenceEqual(profile.Composition.EnemyIds, StringComparer.Ordinal) ||
            !plan.EnemyCells.SequenceEqual(profile.Composition.Cells) ||
            plan.IsBoss != (plan.NodeType == TowerNodeType.Boss) || plan.IsElite != (plan.NodeType == TowerNodeType.Elite)) return false;
        return action.IsStageBoss
            ? plan.ExpeditionProfileId == definition.Stages[map.StageIndex].BossProfileId && plan.FloorRuleId == map.StageBossFloorRuleIds[map.StageIndex]
            : site is not null && plan.ExpeditionProfileId == site.EncounterProfileId && plan.FloorRuleId == site.FloorRuleId;
    }

    private static bool MatchesRewardGroups(System.Collections.Immutable.ImmutableArray<PendingRunOffer> groups,
        System.Collections.Immutable.ImmutableArray<CompiledRunOffer> recipes, Func<PendingRunOffer?, bool> validate,
        ContentRegistry content)
    {
        if (groups.IsDefault || groups.Length > recipes.Length || groups.Any(group => group is null) ||
            groups.Select(group => group.OfferId).Distinct(StringComparer.Ordinal).Count() != groups.Length) return false;
        var recipeIndex = 0;
        foreach (var group in groups)
        {
            if (!validate(group) || group.Kind != RunOfferKind.CombatReward || group.Repeatable) return false;
            while (recipeIndex < recipes.Length && !MatchesRecipe(group, recipes[recipeIndex])) recipeIndex++;
            if (recipeIndex == recipes.Length) return false;
            recipeIndex++;
            if (group.RewardCategory == RunRewardCategory.None) continue;
            var kind = group.RewardCategory == RunRewardCategory.Equipment ? ItemProductKind.Equipment : ItemProductKind.Relic;
            if (!group.AllowSkip || group.Choices.Any(choice => choice.SuccessChance != 1 || !choice.FailureOperations.IsEmpty ||
                    choice.Operations.Length != 1 || choice.Operations[0] is not { Kind: RunOperationKind.GrantItem, Amount: 1 } operation ||
                    !content.TryGet(operation.ContentId, out var entry) || entry.Definition is not ItemDefinition item || item.ProductKind != kind))
                return false;
        }
        return true;
    }

    private static bool MatchesRecipe(PendingRunOffer offer, CompiledRunOffer recipe) =>
        (offer.RecipeId.Length == 0 || offer.RecipeId == recipe.StableId) &&
        offer.Kind == recipe.Kind && offer.RewardCategory == recipe.RewardCategory && offer.Repeatable == recipe.Repeatable &&
        offer.DisplayName == recipe.DisplayName && (offer.AllowSkip == recipe.AllowSkip || offer.Choices.IsEmpty && offer.AllowSkip) &&
        offer.Choices.Length <= recipe.Choices.Length + recipe.PoolChoiceCount * (recipe.PoolAction == RunPoolAction.PayGoldOrHealth ? 2 : 1) &&
        (recipe.PoolAction != RunPoolAction.PayGoldOrHealth || SharedRewardChoicesMatch(offer, recipe)) &&
        offer.Choices.All(choice => recipe.Choices.Concat(recipe.PoolChoices).Any(authored => SameChoice(choice, authored)));

    private static bool SharedRewardChoicesMatch(PendingRunOffer offer, CompiledRunOffer recipe)
    {
        if (offer.RecipeId != recipe.StableId) return false;
        var choices = offer.Choices.Where(choice => recipe.PoolChoices.Any(authored => authored.StableId == choice.StableId)).ToArray();
        return choices.Length == 0 || choices.Length == 2 && choices.Select(choice => choice.ContentId).Distinct(StringComparer.Ordinal).Count() == 1;
    }

    private static bool SameChoice(CompiledRunChoice first, CompiledRunChoice second) =>
        first.StableId == second.StableId && first.DisplayName == second.DisplayName && first.Description == second.Description &&
        first.ContentId == second.ContentId && first.SuccessChance == second.SuccessChance &&
        first.Conditions.SequenceEqual(second.Conditions) && first.Costs.SequenceEqual(second.Costs) &&
        first.Operations.SequenceEqual(second.Operations) && first.FailureOperations.SequenceEqual(second.FailureOperations);

    private static bool ValidateRewardGroups(ActiveRunDto run, ContentRegistry content)
    {
        if (run.PendingRewardGroups.IsEmpty && run.PendingOffer is not { RewardCategory: not RunRewardCategory.None }) return true;
        if (run.Growth is null || run.PendingNode || !string.IsNullOrEmpty(run.TerminalCompletionId) ||
            run.PendingOffer is not { Kind: RunOfferKind.CombatReward } current || run.PendingRewardGroups.Length > 2)
            return false;
        var groups = run.PendingRewardGroups.Insert(0, current);
        if (groups.Select(group => group?.OfferId).Distinct(StringComparer.Ordinal).Count() != groups.Length ||
            groups.Where(group => group is not null && group.RewardCategory != RunRewardCategory.None)
                .Select(group => group.RewardCategory).Distinct().Count() !=
            groups.Count(group => group is not null && group.RewardCategory != RunRewardCategory.None)) return false;
        foreach (var group in groups)
        {
            if (group is null || group.Kind != RunOfferKind.CombatReward || group.Repeatable ||
                !RunDecisionValidation.ValidOffer(group, run,
                    id => content.TryGet(id, out _),
                    id => content.TryGet(id, out var entry) && entry.Definition is UnitDefinition { IsEnemy: false, IsTestDummy: false },
                    id => content.TryGet(id, out var entry) && entry.Definition is ItemDefinition)) return false;
            if (group.RewardCategory == RunRewardCategory.None) continue;
            var expectedKind = group.RewardCategory == RunRewardCategory.Equipment ? ItemProductKind.Equipment : ItemProductKind.Relic;
            if (!group.AllowSkip || group.Choices.Any(choice => choice.SuccessChance != 1 ||
                !choice.FailureOperations.IsEmpty || choice.Operations.Length != 1 ||
                choice.Operations[0] is not { Kind: RunOperationKind.GrantItem, Amount: 1 } operation ||
                !content.TryGet(operation.ContentId, out var entry) ||
                entry.Definition is not ItemDefinition item || item.ProductKind != expectedKind)) return false;
        }
        return true;
    }

    private static bool ValidateOpponentSelection(ActiveRunDto run, CompiledGameProject project)
    {
        var selected = !string.IsNullOrEmpty(run.SelectedOpponentOptionId);
        if (!run.PendingNode || !string.IsNullOrEmpty(run.TerminalCompletionId) ||
            run.SelectedNode is not (TowerNodeType.Combat or TowerNodeType.Elite))
            return !selected && !run.LegacyPendingOpponent;
        var tower = new TowerGenerator(project.Campaign);
        var candidates = tower.OpponentOptions(run, run.SelectedNode);
        // Already locked ordinary opponents and migration continuations keep their
        // original plan and reward even though new ordinary battles no longer choose.
        if (selected)
            return !run.LegacyPendingOpponent && candidates.Any(option => option.OptionId == run.SelectedOpponentOptionId);
        if (run.LegacyPendingOpponent) return candidates.Count > 0;
        return tower.SelectableOpponentOptions(run, run.SelectedNode).Count == 0;
    }
}
