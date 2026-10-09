using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

namespace TowerAutobattler.Project;

public static partial class GameProjectCompiler
{
    private static CompiledCampaign? CompileExpeditionCampaign(CampaignDefinition campaign,
        ExpeditionProgressionDefinition authored, CompilationContext context)
    {
        var report = context.Report;
        context.RegisterStableId(authored.StableId, authored);
        if (authored.Size is < 16 or > 32 || authored.GenerationVersion != 1 || authored.MaximumAttempts is < 1 or > 128 ||
            authored.Biomes is not { Length: 3 } || authored.Stages is not { Length: 3 } || authored.Sites is not { Length: 6 })
            report.Error("Expedition requires a 16–32 grid, generation v1, three biomes/stages and bounded attempts.");
        if ((authored.Biomes ?? []).Any(biome => biome is null || biome.Encounters is null ||
                biome.Encounters.Any(profile => profile is null) || biome.RecruitmentWeights is null) ||
            (authored.Stages ?? []).Any(stage => stage is null || stage.Boss is null) ||
            (authored.Sites ?? []).Any(site => site is null))
            report.Error("Expedition contains a missing biome, stage, site, encounter list or recruitment weights.");
        if (!campaign.SeparateItemRewardCategories)
            report.Error("Expedition rewards require explicit equipment/relic category groups.");
        if (report.HasCoreErrors) return null;
        var authoredBiomes = authored.Biomes!;
        var authoredStages = authored.Stages!;
        var authoredSites = authored.Sites!;
        if (authoredBiomes.Select(biome => biome.StableId).Distinct(StringComparer.Ordinal).Count() != authoredBiomes.Length ||
            authoredStages.Select(stage => stage.StableId).Distinct(StringComparer.Ordinal).Count() != authoredStages.Length ||
            authoredSites.Select(site => site.StableId).Distinct(StringComparer.Ordinal).Count() != authoredSites.Length)
            report.Error("Expedition biome, stage and site identities must be unique in their lists.");
        var starter = context.CompilePool(campaign.StarterPool, ContentPoolKind.Soldier, "Expedition.StarterPool");
        var recruits = context.CompilePool(campaign.RecruitmentPool, ContentPoolKind.Soldier, "Expedition.RecruitmentPool");
        var items = context.CompilePool(campaign.ItemRewardPool, ContentPoolKind.Item, "Expedition.ItemPool");
        var shop = context.CompilePool(campaign.ShopPool, ContentPoolKind.Item, "Expedition.ShopPool");
        if (starter is null || recruits is null || items is null || shop is null) return null;
        var supply = CompileRecruitmentSupply(campaign, recruits, context);
        if (supply is null) { report.Error("Expedition requires recruitment tiers."); return null; }
        var definitions = authoredBiomes.SelectMany(biome => biome.Encounters)
            .Concat(authoredStages.Select(stage => stage.Boss)).Where(value => value is not null).Cast<ExpeditionEncounterDefinition>().Distinct().ToArray();
        var recipeDefinitions = definitions.Select(value => value.Reward).Where(value => value is not null).Cast<RunRewardRecipeDefinition>().Distinct().ToArray();
        if (recipeDefinitions.Any(recipe => recipe.Groups is null || recipe.Groups.Any(group => group is null)))
        { report.Error("Expedition reward recipes require non-null ordered groups."); return null; }
        var offers = CompileOfferRecipes((campaign.RunOffers ?? []).Concat(authoredSites.Select(site => site.Offer)
            .Where(offer => offer is not null).Cast<RunOfferDefinition>()).Concat(authoredSites.SelectMany(site => site.OfferRecipes ?? []))
            .Concat(recipeDefinitions.SelectMany(recipe => recipe.Groups)), context).ToBuilder();
        var encounters = new Dictionary<EncounterDefinition, CompiledEncounter>();
        var profiles = ImmutableDictionary.CreateBuilder<string, CompiledExpeditionEncounter>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            context.RegisterStableId(definition.StableId, definition);
            if (definition.Encounter is not { } source || definition.Reward is not { } reward ||
                !float.IsFinite(definition.FinalHealthMultiplier) || definition.FinalHealthMultiplier is <= 0 or > 100 ||
                !float.IsFinite(definition.FinalDamageMultiplier) || definition.FinalDamageMultiplier is <= 0 or > 100)
            { report.Error($"{definition.StableId}: missing encounter/reward or invalid final multipliers."); continue; }
            if (!encounters.TryGetValue(source, out var encounter))
            {
                encounter = CompileEncounter(source, context, 0);
                if (encounter is null) continue;
                encounters.Add(source, encounter);
            }
            var composition = encounter.Compositions.FirstOrDefault(value => value.StableId == definition.CompositionId);
            if (composition is null) { report.Error($"{definition.StableId}: unknown complete composition."); continue; }
            if (definition.OpeningCandidate && encounter.NodeType != TowerNodeType.Combat)
                report.Error($"{definition.StableId}: only ordinary battles can be opening candidates.");
            context.RegisterStableId(reward.StableId, reward);
            if (reward.Gold is < 0 or > 10000 || reward.Groups.Length > 2) report.Error($"{reward.StableId}: invalid reward recipe.");
            var groups = ImmutableArray.CreateBuilder<CompiledRunOffer>();
            foreach (var group in reward.Groups)
            {
                if (!offers.TryGetValue(group.StableId, out var compiled))
                { report.Error($"{group.StableId}: reward group recipe did not compile."); continue; }
                var categories = RunOfferDefaults.ItemRewardGroups(compiled, context.Entry);
                if (compiled.Kind != RunOfferKind.CombatReward || categories.Length != 1 || categories[0].RewardCategory == RunRewardCategory.None)
                    report.Error($"{group.StableId}: reward group must grant one homogeneous item category.");
                else
                {
                    // Both the reward plan and identity registry must expose the
                    // same category, including subsequent restore/claim lookups.
                    var categorized = compiled.RewardCategory == categories[0].RewardCategory ? compiled :
                        categories[0] with { StableId = compiled.StableId, DisplayName = compiled.DisplayName };
                    offers[compiled.StableId] = categorized;
                    groups.Add(categorized);
                }
            }
            if (groups.Select(group => group.StableId).Distinct().Count() != groups.Count ||
                groups.Select(group => group.RewardCategory).Distinct().Count() != groups.Count)
                report.Error($"{reward.StableId}: duplicate reward group/category.");
            if (!profiles.TryAdd(definition.StableId, new(definition.StableId, encounter, composition,
                definition.FinalHealthMultiplier, definition.FinalDamageMultiplier, definition.OpeningCandidate,
                new(reward.StableId, reward.Gold, groups.ToImmutable())))) report.Error("Duplicate expedition encounter profile.");
        }
        var biomes = ImmutableArray.CreateBuilder<CompiledExpeditionBiome>();
        foreach (var biome in authoredBiomes)
        {
            context.RegisterStableId(biome.StableId, biome);
            if (string.IsNullOrWhiteSpace(biome.DisplayName) || biome.RecruitmentWeights.Length != supply.OpeningTierWeights.Length ||
                biome.RecruitmentWeights.Any(value => value < 0) || biome.RecruitmentWeights.Sum(value => (long)value) is <= 0 or > int.MaxValue ||
                biome.Encounters.Any(profile => !profiles.ContainsKey(profile.StableId) || profile.Encounter?.NodeType == TowerNodeType.Boss) ||
                biome.Encounters.Select(profile => profile.StableId).Distinct(StringComparer.Ordinal).Count() != biome.Encounters.Length)
                report.Error($"{biome.StableId}: invalid biome supply/encounters.");
            foreach (var type in new[] { TowerNodeType.Combat, TowerNodeType.Elite })
                if (!biome.Encounters.Any(profile => profile.Encounter?.NodeType == type)) report.Error($"{biome.StableId}: missing {type}.");
            biomes.Add(new(biome.StableId, biome.DisplayName, biome.Color,
                [.. biome.Encounters.Select(profile => profile.StableId)], [.. biome.RecruitmentWeights]));
        }
        if (authoredBiomes.Length > 0 && !authoredBiomes[0].Encounters.Any(profile => profile.OpeningCandidate))
            report.Error("Opening biome requires authored early encounters.");
        var stages = ImmutableArray.CreateBuilder<CompiledExpeditionStage>();
        foreach (var stage in authoredStages)
        {
            context.RegisterStableId(stage.StableId, stage);
            if (stage.Actions is < 1 or > 20 || string.IsNullOrWhiteSpace(stage.DisplayName) || stage.Boss is null ||
                !profiles.TryGetValue(stage.Boss.StableId, out var boss) || boss.Encounter.NodeType != TowerNodeType.Boss)
            { report.Error($"{stage.StableId}: invalid stage/boss."); continue; }
            stages.Add(new(stage.StableId, stage.DisplayName, stage.Actions, stage.AdvancedDiscovery, stage.Boss.StableId));
        }
        if (stages.Count == authoredStages.Length && profiles.TryGetValue(stages[^1].BossProfileId, out var finalBoss) &&
            (finalBoss.Reward.Gold != 0 || !finalBoss.Reward.Groups.IsEmpty))
            report.Error("The final boss must not author reusable run rewards after terminal completion.");
        var sites = ImmutableArray.CreateBuilder<CompiledExpeditionSite>();
        foreach (var site in authoredSites)
        {
            context.RegisterStableId(site.StableId, site);
            if (site.CountPerBiome < 1 || site.CountPerBiome > authored.Size * authored.Size || site.ActionCost != 1 ||
                string.IsNullOrWhiteSpace(site.DisplayName) || !Enum.IsDefined(site.Kind) || site.Kind == TowerNodeType.Boss)
                report.Error($"{site.StableId}: invalid site quota/cost.");
            var expectedKind = RunDecisionService.KindFor(site.Kind);
            CompiledRunOffer? offer = null;
            var candidates = (site.OfferRecipes ?? []).Where(recipe => recipe is not null && offers.ContainsKey(recipe.StableId))
                .Select(recipe => offers[recipe.StableId]).ToImmutableArray();
            if (site.OfferRecipes is null || candidates.Length != site.OfferRecipes.Length ||
                !candidates.IsEmpty && (site.Kind != TowerNodeType.Event || site.Offer is not null ||
                    candidates.Any(recipe => recipe.Kind != RunOfferKind.Event) || candidates.Select(recipe => recipe.StableId).Distinct().Count() != candidates.Length))
                report.Error($"{site.StableId}: event recipe pool must be non-null, unique Event recipes and replace the single Offer.");
            if (expectedKind is not null && candidates.IsEmpty && (site.Offer is null || !offers.TryGetValue(site.Offer.StableId, out offer) || offer.Kind != expectedKind))
                report.Error($"{site.StableId}: service requires its own matching offer recipe.");
            if (expectedKind is null && site.Offer is not null)
                report.Error($"{site.StableId}: battle rewards belong to encounter recipes, not a service offer.");
            sites.Add(new(site.StableId, site.DisplayName, site.Kind, site.CountPerBiome, site.ActionCost, offer) { OfferRecipes = candidates });
        }
        var totalSites = sites.Sum(site => (long)site.CountPerBiome) * biomes.Count;
        if (sites.Select(site => site.Kind).Distinct().Count() != 6 || totalSites < stages.Sum(stage => stage.Actions) ||
            totalSites >= authored.Size * authored.Size)
            report.Error("Expedition requires six distinct site kinds and enough action opportunities.");
        if (report.HasCoreErrors) return null;
        var progression = new CompiledExpedition(authored.StableId, authored.Size, authored.GenerationVersion, authored.MaximumAttempts,
            biomes.ToImmutable(), stages.ToImmutable(), sites.ToImmutable(), profiles.ToImmutable());
        return new CompiledCampaign(campaign.StableId, progression, starter, recruits, items, shop)
        { RecruitmentSupply = supply, OfferRecipesById = offers.ToImmutable(), SeparateItemRewardCategories = true };
    }
}
