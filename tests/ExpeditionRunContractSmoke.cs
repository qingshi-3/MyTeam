using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Expedition;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;
using TowerAutobattler.Run;

// This is a transaction/restore contract probe. Its driver can inspect generated
// data to find a legal route, but issues real facade movement and choice commands.
// It is not a UI-input, natural balance, or whole-campaign acceptance test.
public partial class ExpeditionRunContractSmoke : Node
{
    public override async void _Ready()
    {
        var code = 0;
        try
        {
            var published = await GrowthContentPackage.CreateReadyAsync(this);
            var package = published.Package ?? throw new InvalidOperationException(string.Join(';', published.Report.CoreErrors));
            Require(package.ProfilesById.ContainsKey(RunProfileIds.GrowthExpedition) &&
                package.ProfilesById.ContainsKey(RunProfileIds.GrowthExpeditionLegacy) &&
                package.ProfilesById.ContainsKey(RunProfileIds.GrowthTowerLegacy), "formal package publishes all three complete profiles");
            VerifyRecruitmentProfileCompatibility(package);
            if (OS.GetCmdlineUserArgs().Contains("--recruitment-only"))
                GD.Print("EXPEDITION_RECRUITMENT_CONTRACT_OK default-v2 three-unique opening-excluded v1-known-hash v1-draft-continue v1-fixed-stock unknown-hash-preserved");
            else
            {
                VerifyExpeditionTransactions(package);
                VerifyLegacySessionReplacement(package);
                VerifyLegacyVersion10Migration(package);
                VerifyUnknownProfileProtection(package);
                VerifyEntryGoldAndInterestBoundaries(package);
                GD.Print("EXPEDITION_RUN_CONTRACT_OK formal-package opening rollback fixed-stock same-seed restore navigation service-once battle-origin battle-rollback last-action boss-ready frozen-boss stage-reward-refill controlled-boss-terminal meta-retry legacy-session v10-migration unknown-profile bank-padding-post-entry interest-7-8-9 recruitment-v2 v1-fixed-stock");
            }
        }
        catch (Exception error) { code = 1; GD.PrintErr("EXPEDITION_RUN_CONTRACT_FAILED: " + error); }
        GetTree().Quit(code);
    }

    private static void VerifyRecruitmentProfileCompatibility(CompiledGamePackage package)
    {
        const string legacyFingerprint = "b6b2527889ed6a8e0d4c2e0c6cc02de99f3625a5ff58cf6ca8cdc0f953c83992";
        Require(package.DefaultNewRunProfileId == RunProfileIds.GrowthExpedition &&
            RunProfileIds.GrowthExpedition == "growth_expedition_v2" &&
            RunProfileIds.GrowthExpeditionLegacy == "growth_expedition_v1", "formal new runs select the explicit v2 profile");
        var legacyProfile = package.ProfilesById[RunProfileIds.GrowthExpeditionLegacy];
        Require(legacyProfile.ContentFingerprint == legacyFingerprint,
            "published v1 retains the exact fingerprint of existing user saves");

        var (current, currentSave) = NewBoundaryRun(package);
        Require(current.ActiveRun!.ProfileId == RunProfileIds.GrowthExpedition, "new map uses v2 header");
        VerifyFixedRecruitmentChoices(current, 3);
        var currentSnapshot = Snapshot(current.ActiveRun!);
        var currentStock = Stock(current.ActiveRun!.Expedition!);
        var currentJson = currentSave.Json;
        var writes = currentSave.SuccessfulRunWrites;
        var currentRestored = new RunApplication(package, currentSave, "growth-journey");
        Require(currentRestored.ActiveRun is { Expedition: not null } &&
            Snapshot(currentRestored.ActiveRun) == currentSnapshot && Stock(currentRestored.ActiveRun.Expedition) == currentStock &&
            currentSave.Json == currentJson && currentSave.SuccessfulRunWrites == writes,
            "v2 continue preserves all frozen stock without rewriting the run");
        VerifyUnknownProfileProtection(package, currentSave);

        // Build through the retained complete v1 configuration, then continue
        // through the production package whose new-run default is v2.
        var legacySave = new ExpeditionRunMemorySave();
        var legacy = new RunApplication(package.ForProfile(RunProfileIds.GrowthExpeditionLegacy), legacySave, "growth-journey");
        Require(legacy.BeginOpeningRecruitment(20261003), "v1 fixture creates a real saved opening draft");
        foreach (var id in legacy.ActiveRun!.OpeningRecruitment!.CandidateIds.Take(2))
            Require(legacy.ToggleOpeningHero(id), "v1 draft persists its original starting selections");
        var draftSnapshot = Snapshot(legacy.ActiveRun!);
        var draftJson = legacySave.Json;
        writes = legacySave.SuccessfulRunWrites;
        var draftRestored = new RunApplication(package, legacySave, "growth-journey");
        Require(draftRestored.ActiveRun is { OpeningRecruitment: not null, Expedition: null } draft &&
            draft.ProfileId == RunProfileIds.GrowthExpeditionLegacy && draft.ContentFingerprint == legacyFingerprint &&
            ReferenceEquals(draftRestored.Project, legacyProfile.Project) && Snapshot(draft) == draftSnapshot &&
            legacySave.Json == draftJson && legacySave.SuccessfulRunWrites == writes,
            "v1 opening draft resumes under its complete original profile without migration or replacement");
        VerifyUnknownProfileProtection(package, legacySave);
        Require(draftRestored.ConfirmOpeningRecruitment(), "continued v1 draft confirms with its original recruitment recipe");
        VerifyFixedRecruitmentChoices(draftRestored, 1);
        var legacySnapshot = Snapshot(draftRestored.ActiveRun!);
        var legacyStock = Stock(draftRestored.ActiveRun!.Expedition!);
        var legacyJson = legacySave.Json;
        writes = legacySave.SuccessfulRunWrites;
        var legacyRestored = new RunApplication(package, legacySave, "growth-journey");
        Require(legacyRestored.ActiveRun is { Expedition: not null } oldRun &&
            oldRun.ProfileId == RunProfileIds.GrowthExpeditionLegacy && oldRun.ContentFingerprint == legacyFingerprint &&
            ReferenceEquals(legacyRestored.Project, legacyProfile.Project) && Snapshot(oldRun) == legacySnapshot &&
            Stock(oldRun.Expedition) == legacyStock && legacySave.Json == legacyJson && legacySave.SuccessfulRunWrites == writes,
            "v1 confirmed map continues with identical fixed one-candidate stock and run state");
        VerifyFixedRecruitmentChoices(legacyRestored, 1);
        VerifyUnknownProfileProtection(package, legacySave);
        Require(package.DefaultNewRunProfileId == RunProfileIds.GrowthExpedition,
            "continuing v1 cannot change the package new-run default");
        Require(legacyRestored.BeginOpeningRecruitment(20261004) &&
            legacyRestored.ActiveRun!.ProfileId == RunProfileIds.GrowthExpedition,
            "a new game after continuing v1 selects v2 as a new session");
    }

    private static void VerifyFixedRecruitmentChoices(RunApplication app, int expectedCount)
    {
        var run = app.ActiveRun!;
        var map = run.Expedition!;
        var definition = (CompiledExpedition)app.Project.Campaign.Progression;
        var sites = map.Sites.Where(site => definition.Sites.Single(source => source.StableId == site.DefinitionId).Kind ==
            TowerNodeType.Recruitment).ToArray();
        Require(sites.Length == 6, "all six authored recruitment sites are populated");
        foreach (var site in sites)
        {
            var offer = site.ServiceOffer;
            Require(offer is not null && offer.Kind == RunOfferKind.Recruitment && offer.Choices.Length == expectedCount,
                $"{run.ProfileId} {site.SiteId} has exactly {expectedCount} fixed recruitment choices");
            var recruits = offer!.Choices.Select(choice => choice.Operations.Where(operation => operation.Kind == RunOperationKind.Recruit).ToArray()).ToArray();
            Require(recruits.All(operations => operations.Length == 1), "each recruitment choice grants precisely one hero identity");
            var ids = recruits.Select(operations => operations[0].ContentId).ToArray();
            Require(ids.Distinct(StringComparer.Ordinal).Count() == expectedCount &&
                ids.All(id => run.Roster.All(hero => hero.ContentId != id)),
                "each frozen recruitment offer has unique identities and excludes both starting heroes");
        }
    }

    private static void VerifyExpeditionTransactions(CompiledGamePackage package)
    {
        var (app, save) = NewBoundaryRun(package);
        var run = app.ActiveRun!;
        var map = run.Expedition!;
        var definition = (CompiledExpedition)app.Project.Campaign.Progression;
        Require(run.ProfileId == RunProfileIds.GrowthExpedition && map.Size == 16 && map.Sites.Count == 39,
            "new opening binds default map profile and full authored map");
        Require(map.Sites.Where(site => definition.Sites.Single(source => source.StableId == site.DefinitionId).Kind == TowerNodeType.Recruitment)
            .SelectMany(site => site.ServiceOffer!.Choices).SelectMany(choice => choice.Operations)
            .Where(operation => operation.Kind == RunOperationKind.Recruit)
            .All(operation => run.Roster.All(hero => hero.ContentId != operation.ContentId)), "starting identities excluded from fixed recruits");
        var frozenStock = Stock(map);
        VerifySameSeedContent(package, run);
        var restored = new RunApplication(package, save, "growth-journey");
        Require(restored.ActiveRun is { Expedition: not null } && Stock(restored.ActiveRun.Expedition) == frozenStock,
            "load restores complete fixed site and boss stock");
        app = restored;
        run = app.ActiveRun!;
        var before = Snapshot(run);
        var beforeDisk = save.Json;
        Require(!app.MoveExpedition(-1) && !app.InspectExpeditionSite("unknown-site") &&
            !app.ResolveOffer("unknown-offer", "unknown-choice").Succeeded && Snapshot(run) == before && save.Json == beforeDisk,
            "invalid commands cannot mutate or save an action");

        var definitions = definition.Sites.ToDictionary(site => site.StableId, StringComparer.Ordinal);
        // The initial free region has five authored services. Distinct recruit
        // fixture identities ensure six real actions can reach the budget edge
        // with one simulator battle; no counter/history field is fabricated.
        var services = run.Expedition!.Sites.Where(site => run.Expedition.Cells[site.CellIndex].BiomeIndex == 0 &&
            site.EncounterProfileId.Length == 0).OrderBy(site => ServiceOrder(definitions[site.DefinitionId].Kind)).ToArray();
        Require(services.Length == 5, "five initial services form boundary fixture");
        foreach (var service in services)
        {
            WalkToSite(app, service.SiteId);
            var prior = Snapshot(run);
            var writes = save.SuccessfulRunWrites;
            Require(app.InspectExpeditionSite(service.SiteId) && Snapshot(run) == prior && save.SuccessfulRunWrites == writes && run.Action is null,
                "service inspection is read-only and creates no pending action");
            var offer = app.PendingOffer ?? throw new InvalidOperationException("visible site has no offer");
            Require(app.PreviewExpeditionSiteOffer(service.SiteId)?.OfferId == offer.OfferId && Snapshot(run) == prior,
                "repeated preview retains frozen identity");
            Require(!app.ResolveOffer(offer.OfferId, "unknown-choice").Succeeded && Snapshot(run) == prior,
                "invalid choice has no partial entry benefits or cost");
            if (definitions[service.DefinitionId].Kind == TowerNodeType.Rest)
            {
                var heal = offer.Choices.First(choice => choice.Operations.Any(operation => operation.Kind == RunOperationKind.RecoverRunHealth));
                Require(!app.ResolveOffer(offer.OfferId, heal.StableId).Succeeded && Snapshot(run) == prior,
                    "full-health recovery cannot spend an action");
            }
            var choice = offer.Choices.Where(candidate => candidate.SuccessChance == 1 && app.CheckOfferChoice(candidate).Succeeded)
                .OrderBy(candidate => candidate.Costs.Where(cost => cost.Kind == RunOperationKind.SpendGold).Sum(cost => cost.Amount))
                .FirstOrDefault() ?? throw new InvalidOperationException("fixture service has no legal deterministic choice: " + service.SiteId);
            var budget = run.Expedition.RemainingActions;
            var settlements = run.Growth!.History.Count;
            save.FailWrites = true;
            Require(!app.ResolveOffer(offer.OfferId, choice.StableId).Succeeded && Snapshot(run) == prior,
                "failed service save rolls back entry, payment, grants, stock and growth");
            save.FailWrites = false;
            Require(app.ResolveOffer(offer.OfferId, choice.StableId).Succeeded && run.Expedition.RemainingActions == budget - 1 &&
                run.Growth.History.Count == settlements + 1 && run.Expedition.Sites.Single(site => site.SiteId == service.SiteId).Handled,
                "successful first service pays and produces exactly once");
            var afterPurchase = Snapshot(run);
            Require(!app.ResolveOffer(offer.OfferId, choice.StableId).Succeeded && Snapshot(run) == afterPurchase,
                "replayed choice cannot grant or spend twice");
            if (definitions[service.DefinitionId].Kind == TowerNodeType.Shop)
            {
                app.FinishNonCombatNode();
                var count = run.Growth.History.Count;
                var remaining = run.Expedition.RemainingActions;
                Require(app.InspectExpeditionSite(service.SiteId), "paid shop remains a real visitable stock");
                var reoffer = app.PendingOffer!;
                Require(reoffer.Choices.All(candidate => candidate.StableId != choice.StableId), "purchased stock never returns on revisit");
                var second = reoffer.Choices.Where(candidate => app.CheckOfferChoice(candidate).Succeeded)
                    .OrderBy(candidate => candidate.Costs.Where(cost => cost.Kind == RunOperationKind.SpendGold).Sum(cost => cost.Amount)).FirstOrDefault();
                if (second is not null)
                    Require(app.ResolveOffer(reoffer.OfferId, second.StableId).Succeeded &&
                        run.Growth.History.Count == count && run.Expedition.RemainingActions == remaining,
                        "later shop transaction buys stock without a second encounter or production");
                app.FinishNonCombatNode();
            }
            Require(run.Action is null, "service transaction is fully closed");
        }
        Require(run.Expedition.RemainingActions == 1 && run.Growth!.History.Count == 5, "five real authored actions leave one point");

        var enemy = FindReachableOpeningEnemy(app, definition);
        WalkToSite(app, enemy.SiteId);
        var encounter = app.PreviewExpeditionEncounter(enemy.SiteId);
        before = Snapshot(run);
        Require(!app.TryBeginGrowthBattle(encounter with { ExpeditionProfileId = "unknown-profile" }).Succeeded && Snapshot(run) == before,
            "stale battle preview cannot charge or freeze");
        save.FailWrites = true;
        Require(!app.TryBeginGrowthBattle(encounter).Succeeded && Snapshot(run) == before,
            "battle-start failure rolls back final action and full frozen input");
        save.FailWrites = false;
        var started = app.TryBeginGrowthBattle(encounter);
        Require(started.Succeeded, "formal battle starts: " + started.Message);
        Require(run.Expedition.RemainingActions == 0 && app.ExpeditionBattleCommitted && !app.CanChallengeExpeditionBoss,
            "last action finishes its battle before BossReady");
        var committed = Snapshot(run);
        Require(app.TryBeginGrowthBattle(encounter).Succeeded && Snapshot(run) == committed, "retry resumes one frozen battle without charging again");
        var reload = new RunApplication(package, save, "growth-journey");
        Require(reload.ExpeditionBattleCommitted && Snapshot(reload.ActiveRun!) == committed,
            "continue restores committed battle, not a fresh preview");
        app = reload;
        run = app.ActiveRun!;
        encounter = app.CurrentEncounter();
        var config = app.BuildBattleConfig(encounter);
        Require(config.Identity is { FloorIndex: -1 } identity && identity.OriginKey == run.Action!.Source.ActionId &&
            config.Relics?.RunKey.OriginKey == identity.OriginKey, "battle and relic receipt share committed action origin");
        BattleResult result;
        using (var simulation = new BattleSimulation(config)) result = simulation.RunToEnd();
        Require(result.Outcome is BattleOutcome.PlayerVictory or BattleOutcome.PlayerDefeat or BattleOutcome.Timeout,
            "formal authored simulator produces a final receipt");
        var signed = result.Identity!;
        foreach (var bad in new[]
        {
            result with { Identity = signed with { OriginKey = signed.OriginKey + ":other" } },
            result with { Identity = signed with { BattleNumber = signed.BattleNumber + 1 } },
            result with { Identity = signed with { FloorIndex = 0 } }
        })
            Require(!app.ResolveBattle(bad, encounter).Accepted && Snapshot(run) == committed,
                "ordinary battle rejects foreign action, counter and tower identity");
        save.FailWrites = true;
        var rejected = app.ResolveBattle(result, encounter);
        Require(!rejected.Accepted && rejected.Failure == RunBattleResolutionFailure.PersistenceFailed && Snapshot(run) == committed,
            "result publication failure retains the exact frozen battle and all resources");
        save.FailWrites = false;
        var accepted = app.ResolveBattle(result, encounter);
        Require(accepted.Accepted && app.ActiveRun is not null && run.BattleNumber == signed.BattleNumber + 1,
            "real battle receipt commits one counter and continuation");
        Require(run.Expedition!.Sites.Single(site => site.SiteId == enemy.SiteId).Cleared == (result.Outcome == BattleOutcome.PlayerVictory),
            "only victory opens the physical enemy cell");
        var afterBattle = Snapshot(run);
        Require(!app.ResolveBattle(result, encounter).Accepted && Snapshot(run) == afterBattle, "duplicate result cannot settle again");
        while (app.PendingOffer is { } reward)
        {
            Require(!app.CanChallengeExpeditionBoss, "BossReady waits for every pending reward");
            Require(app.ResolveOffer(reward.OfferId, null).Succeeded, "finish actual reward group before phase transition");
        }
        Require(run.Action is null && run.Expedition.RemainingActions == 0 && run.Growth!.History.Count == 6 && app.CanChallengeExpeditionBoss,
            "six real actions settle before BossReady");
        Require(!app.MoveExpedition(run.Expedition.PositionIndex) && !app.InspectExpeditionSite(services[0].SiteId),
            "BossReady rejects further map transactions");
        VerifyFrozenBoss(app, save, package, result);
    }

    private static void VerifyFrozenBoss(RunApplication app, ExpeditionRunMemorySave save, CompiledGamePackage package, BattleResult previousBattle)
    {
        var run = app.ActiveRun!;
        var before = Snapshot(run);
        var boss = app.PreviewExpeditionBoss();
        Require(Snapshot(run) == before && boss.ExpeditionBoss && boss.IsBoss, "Boss preview has no entry effects");
        save.FailWrites = true;
        Require(!app.TryBeginGrowthBattle(boss).Succeeded && Snapshot(run) == before, "failed Boss commit has no partial state");
        save.FailWrites = false;
        var started = app.TryBeginGrowthBattle(boss);
        Require(started.Succeeded, "stage Boss commits through shared battle path: " + started.Message);
        var committed = Snapshot(run);
        var config = app.BuildBattleConfig(boss);
        Require(run.Expedition!.RemainingActions == 0 && run.Growth!.History.Count == 6 && run.Action!.IsStageBoss &&
            config.Identity!.OriginKey == run.Action.Source.ActionId && config.Identity.OriginKey != previousBattle.Identity!.OriginKey,
            "Boss has an independent origin and no extra exploration cost or premature growth");
        Require(!app.CheckGrowthAction().Succeeded && !app.MoveDeploymentUnit(run.Roster[0].InstanceId, 0),
            "committed Boss locks actual growth and formation commands");
        foreach (var bad in new[]
        {
            previousBattle,
            previousBattle with { Identity = config.Identity! with { OriginKey = previousBattle.Identity!.OriginKey } },
            previousBattle with { Identity = config.Identity! with { BattleNumber = config.Identity!.BattleNumber + 1 } }
        })
            Require(!app.ResolveBattle(bad, boss).Accepted && Snapshot(run) == committed,
                "Boss rejects previous encounter, foreign action and counter replay");
        var restored = new RunApplication(package, save, "growth-journey");
        Require(restored.ExpeditionBattleCommitted && Snapshot(restored.ActiveRun!) == committed &&
            restored.BuildBattleConfig(restored.CurrentEncounter()).Identity == config.Identity,
            "frozen Boss restores identical origin and authored battle input");
        VerifyBossRewardsAndTerminal(restored, save);
    }

    private static void VerifyBossRewardsAndTerminal(RunApplication app, ExpeditionRunMemorySave save)
    {
        // Controlled simulator receipts exercise transaction boundaries. Clearing
        // enemy health is fixture setup, not evidence of a naturally winning team.
        for (var stage = 0; stage < 2; stage++)
        {
            var run = app.ActiveRun!;
            if (!app.ExpeditionBattleCommitted)
            {
                Require(run.Expedition!.RemainingActions == 6, "next stage starts with its authored budget");
                var count = run.Growth!.History.Count;
                var begin = app.TryBeginGrowthBattle(app.PreviewExpeditionBoss());
                Require(begin.Succeeded && run.Expedition.RemainingActions == 0 && run.Growth.History.Count == count,
                    "early Boss forfeits unused budget without fabricated growth: " + begin.Message);
            }
            var boss = app.CurrentEncounter();
            var result = RunHealthFixture.Result(app.BuildBattleConfig(boss), BattleOutcome.PlayerVictory);
            var committed = Snapshot(run);
            var countBefore = run.Growth!.History.Count;
            save.FailWrites = true;
            Require(!app.ResolveBattle(result, boss).Accepted && Snapshot(run) == committed,
                "Boss result save failure retains its complete frozen state");
            save.FailWrites = false;
            var resolved = app.ResolveBattle(result, boss);
            Require(resolved.Accepted && run.Expedition!.StageIndex == stage && run.Expedition.RemainingActions == 0 &&
                run.Growth.History.Count == countBefore + 1 && run.Action is { Phase: RunActionPhase.Rewards } &&
                app.PendingOffer?.RewardCategory == RunRewardCategory.Relic,
                "stage Boss grants growth once and waits on its actual relic reward");
            var afterBattle = Snapshot(run);
            Require(!app.ResolveBattle(result, boss).Accepted && Snapshot(run) == afterBattle,
                "duplicate Boss result cannot grant a second reward or growth");
            var reward = app.PendingOffer!;
            var claim = stage == 0
                ? reward.Choices.First(choice => app.CheckOfferChoice(choice).Succeeded).StableId : null;
            var itemCount = run.Items.Count;
            save.FailWrites = true;
            Require(!app.ResolveOffer(reward.OfferId, claim).Succeeded && Snapshot(run) == afterBattle,
                "failed last reward publication cannot advance stage or refill actions");
            save.FailWrites = false;
            Require(app.ResolveOffer(reward.OfferId, claim).Succeeded && run.Action is null &&
                run.Expedition!.StageIndex == stage + 1 && run.Expedition.RemainingActions == 6 &&
                run.Growth.History.Count == countBefore + 1, "last reward advances one stage and refills once");
            if (claim is not null) Require(run.Items.Count == itemCount + 1, "stage relic claim grants its real item once");
            var afterReward = Snapshot(run);
            Require(!app.ResolveOffer(reward.OfferId, claim).Succeeded && Snapshot(run) == afterReward,
                "old stage reward cannot refill or settle twice");
        }

        var finalRun = app.ActiveRun!;
        Require(app.IsFinalExpeditionStage && finalRun.Expedition!.StageIndex == 2 && app.Meta.HighestRegion == 3,
            "final stage and Meta high-water mark use stage facts");
        var finalBoss = app.PreviewExpeditionBoss();
        Require(app.TryBeginGrowthBattle(finalBoss).Succeeded, "final Boss commits through ordinary frozen battle path");
        var finalResult = RunHealthFixture.Result(app.BuildBattleConfig(finalBoss), BattleOutcome.PlayerVictory);
        var historyBefore = finalRun.Growth!.History.Count;
        var victoriesBefore = app.Meta.Victories;
        save.FailMetaWrites = true;
        var failure = app.ResolveBattle(finalResult, finalBoss);
        Require(!failure.Accepted && failure.Failure == RunBattleResolutionFailure.PersistenceFailed && save.Json is not null &&
            app.ActiveRun is { TerminalVictory: true, Action: null } && finalRun.TerminalCompletionId.Length > 0 &&
            finalRun.Growth.History.Count == historyBefore && app.PendingOffer is null &&
            save.LoadMeta().Victories == victoriesBefore,
            "failed terminal Meta save preserves retryable victory and grants no reusable final growth/reward");
        var terminalJson = save.Json;
        var terminalId = finalRun.TerminalCompletionId;
        Require(!app.ResumeTerminalCompletion() && save.Json == terminalJson && save.LoadMeta().Victories == victoriesBefore,
            "repeated failed Meta save cannot erase run or award victory");
        save.FailMetaWrites = false;
        save.FailDelete = true;
        Require(!app.ResumeTerminalCompletion() && save.Json == terminalJson &&
            save.LoadMeta().Victories == victoriesBefore + 1 && save.LoadMeta().AppliedRunCompletionIds.Count(id => id == terminalId) == 1,
            "delete failure retains terminal receipt after one durable Meta award");
        save.FailDelete = false;
        Require(app.ResumeTerminalCompletion() && app.ActiveRun is null && save.Json is null &&
            save.LoadMeta().Victories == victoriesBefore + 1 && !app.ResumeTerminalCompletion(),
            "terminal retry deletes run only after durable Meta and never awards twice");
    }

    private static (RunApplication App, ExpeditionRunMemorySave Save) NewBoundaryRun(CompiledGamePackage package)
    {
        var save = new ExpeditionRunMemorySave();
        var app = new RunApplication(package, save, "growth-journey");
        for (ulong seed = 20261003; seed < 20261019; seed++)
        {
            Require(app.BeginOpeningRecruitment(seed), "formal opening draft saves");
            var opening = app.ActiveRun!.OpeningRecruitment!;
            Require(opening.CandidateIds.Length == 6 && opening.SelectedIds.IsEmpty, "opening remains six choose two");
            foreach (var id in opening.CandidateIds.Take(2)) Require(app.ToggleOpeningHero(id), "select real starting hero");
            if (seed == 20261003)
            {
                var draft = Snapshot(app.ActiveRun!);
                var disk = save.Json;
                save.FailWrites = true;
                Require(!app.ConfirmOpeningRecruitment() && Snapshot(app.ActiveRun!) == draft && save.Json == disk,
                    "failed map publication preserves selected opening draft");
                save.FailWrites = false;
            }
            Require(app.ConfirmOpeningRecruitment(), "opening confirmation atomically publishes a populated map");
            var definition = (CompiledExpedition)app.Project.Campaign.Progression;
            var map = app.ActiveRun!.Expedition!;
            var recruits = map.Sites.Where(site => map.Cells[site.CellIndex].BiomeIndex == 0 &&
                definition.Sites.Single(source => source.StableId == site.DefinitionId).Kind == TowerNodeType.Recruitment)
                .Select(site => site.ServiceOffer!.Choices.SelectMany(choice => choice.Operations)
                    .Where(operation => operation.Kind == RunOperationKind.Recruit).Select(operation => operation.ContentId).ToArray()).ToArray();
            // Cross-site duplicates are legal fixed content. Select a reproducible
            // fixture in which taking the first site's first choice still leaves
            // a different recruit at the second site,
            // independently of whether each recipe freezes one or three choices.
            if (recruits.Length == 2 && recruits[0].Length > 0 && recruits[1].Any(second => second != recruits[0][0]))
            {
                var before = Snapshot(app.ActiveRun!);
                save.FailWrites = true;
                Require(!app.BeginOpeningRecruitment(seed + 1000) && Snapshot(app.ActiveRun!) == before,
                    "failed new draft preserves previous complete session");
                save.FailWrites = false;
                return (app, save);
            }
        }
        throw new InvalidOperationException("Could not obtain a distinct-recruit boundary fixture in the bounded authored sample.");
    }

    private static void VerifyLegacySessionReplacement(CompiledGamePackage package)
    {
        var save = new ExpeditionRunMemorySave();
        var legacyPackage = package.ForProfile(RunProfileIds.GrowthTowerLegacy);
        var legacy = new RunApplication(legacyPackage, save, "growth-journey");
        Require(legacy.StartNewRun(legacy.Meta.UnlockedHeroIds[0], 1776), "complete legacy profile can create fixture run");
        var app = new RunApplication(package, save, "growth-journey");
        Require(!app.IsExpedition && app.ActiveRun?.ProfileId == RunProfileIds.GrowthTowerLegacy,
            "saved profile selects complete legacy session despite new default");
        var before = Snapshot(app.ActiveRun!);
        save.FailWrites = true;
        Require(!app.BeginOpeningRecruitment(20261003) && !app.IsExpedition && Snapshot(app.ActiveRun!) == before,
            "failed new draft does not replace legacy service bindings");
        save.FailWrites = false;
        Require(app.BeginOpeningRecruitment(20261003) && app.IsExpedition && app.ActiveRun?.ProfileId == RunProfileIds.GrowthExpedition,
            "successful new draft atomically replaces the whole legacy session");
    }

    private static void VerifySameSeedContent(CompiledGamePackage package, ActiveRunDto original)
    {
        var another = new RunApplication(package, new ExpeditionRunMemorySave(), "growth-journey");
        Require(another.BeginOpeningRecruitment(original.Seed), "second same-seed draft");
        foreach (var hero in original.Roster.Take(2)) Require(another.ToggleOpeningHero(hero.ContentId), "same-seed starting identity");
        Require(another.ConfirmOpeningRecruitment() && another.ActiveRun!.RunId != original.RunId,
            "same seed still has a distinct durable run identity");
        Require(Content(original.Expedition!) == Content(another.ActiveRun!.Expedition!),
            "sampling is determined by seed and authored source, independently of run/offer identity");
    }

    private static void VerifyLegacyVersion10Migration(CompiledGamePackage package)
    {
        var save = new ExpeditionRunMemorySave();
        var legacy = new RunApplication(package.ForProfile(RunProfileIds.GrowthTowerLegacy), save, "growth-journey");
        Require(legacy.StartNewRun(legacy.Meta.UnlockedHeroIds[0], 1776), "legacy migration fixture begins with authored content");
        var service = legacy.CurrentOptions().FirstOrDefault(option => RunDecisionService.KindFor(option.Type) is not null);
        Require(service is not null && legacy.SelectNode(service.Type), "legacy migration fixture owns a real pending service");
        var pending = legacy.ActiveRun!.PendingOffer!;
        var json = JsonNode.Parse(save.Json!)!.AsObject();
        var tower = json["Tower"]!.AsObject();
        foreach (var property in tower) json[property.Key] = property.Value?.DeepClone();
        foreach (var field in new[] { "Tower", "Expedition", "Action", "CompletedActionIds", "NextActionSequence", "Revision",
            "RunId", "ProfileId", "ContentFingerprint", "ProfileCompatibilityVersion", "ProgressionProtocolVersion",
            "RewardProtocolVersion", "IdentityProtocolVersion" }) json.Remove(field);
        json["Version"] = 10;
        if (json["PendingOffer"] is JsonObject oldOffer) { oldOffer.Remove("Source"); oldOffer.Remove("SamplingKey"); }
        if (json["Growth"]?["PendingNode"] is JsonObject oldGrowthSnapshot) oldGrowthSnapshot.Remove("Source");
        var v10 = json.ToJsonString();
        save.SetFixtureJson(v10);
        var metaBefore = JsonSerializer.Serialize(save.LoadMeta());
        save.FailWrites = true;
        var failed = new RunApplication(package, save, "growth-journey");
        Require(failed.ActiveRun is null && failed.ActiveRunLoadDiagnostic?.Kind == ActiveRunLoadFailureKind.MigrationPublicationFailed &&
            save.Json == v10 && JsonSerializer.Serialize(save.LoadMeta()) == metaBefore,
            "failed v10 migration publication preserves original JSON and Meta");
        save.FailWrites = false;
        var migrated = new RunApplication(package, save, "growth-journey");
        Require(migrated.ActiveRun is { Version: ActiveRunFormationSchema.CurrentVersion, Expedition: null, Tower: not null } current &&
            current.ProfileId == RunProfileIds.GrowthTowerLegacy && current.PendingOffer?.OfferId == pending.OfferId &&
            JsonSerializer.Serialize(current.PendingOffer!.Choices) == JsonSerializer.Serialize(pending.Choices) &&
            current.Growth?.PendingNode is not null && !migrated.IsExpedition && save.Json != v10,
            "v10 flat state migrates to complete legacy profile without redrawing service or growth entitlement");
        var written = JsonNode.Parse(save.Json!)!.AsObject();
        Require(written.ContainsKey("Tower") && !written.ContainsKey("FloorIndex") && !written.ContainsKey("PendingOffer"),
            "migrated current schema has one serialized tower owner");
    }

    private static void VerifyUnknownProfileProtection(CompiledGamePackage package)
    {
        var save = new ExpeditionRunMemorySave();
        var app = new RunApplication(package, save, "growth-journey");
        Require(app.BeginOpeningRecruitment(20261003), "profile rejection fixture has valid new header");
        VerifyUnknownProfileProtection(package, save);
    }

    private static void VerifyUnknownProfileProtection(CompiledGamePackage package, ExpeditionRunMemorySave save)
    {
        var valid = save.Json!;
        var meta = JsonSerializer.Serialize(save.LoadMeta());
        foreach (var corrupt in new[] { "ProfileId", "ContentFingerprint" })
        {
            var json = JsonNode.Parse(valid)!.AsObject();
            json[corrupt] = corrupt == "ContentFingerprint" ? new string('0', 64) : "unknown_or_changed_profile";
            var invalid = json.ToJsonString();
            save.SetFixtureJson(invalid);
            var writes = save.SuccessfulRunWrites;
            var metaWrites = save.SuccessfulMetaWrites;
            var rejected = new RunApplication(package, save, "growth-journey");
            Require(rejected.ActiveRun is null && rejected.ActiveRunLoadDiagnostic is not null && save.Json == invalid &&
                save.SuccessfulRunWrites == writes && save.SuccessfulMetaWrites == metaWrites &&
                JsonSerializer.Serialize(save.LoadMeta()) == meta, "unknown profile/fingerprint cannot fall back, migrate or overwrite");
        }
        save.SetFixtureJson(valid);
    }

    private static void VerifyEntryGoldAndInterestBoundaries(CompiledGamePackage package)
    {
        // Explicit rule-boundary fixture: real authored relic grants, no map
        // rewards or battle counter are fabricated in the persisted expedition.
        var save = new ExpeditionRunMemorySave();
        var app = new RunApplication(package, save, "growth-journey");
        Require(app.BeginOpeningRecruitment(20261003), "entry-gold fixture begins real opening");
        foreach (var id in app.ActiveRun!.OpeningRecruitment!.CandidateIds.Take(2)) Require(app.ToggleOpeningHero(id), "entry-gold opening selection");
        Require(app.ConfirmOpeningRecruitment(), "entry-gold fixture enters authored map");
        var run = app.ActiveRun!;
        var boss = app.PreviewExpeditionBoss();
        var baseline = PreparedPlayerHealth(app.BuildBattleConfig(boss));
        Require(app.GrantItem("item_rl02_bank") && app.GrantItem("item_rl09_padding"), "facade grants real bank and padding relics");
        var before = Snapshot(run);
        var writes = save.SuccessfulRunWrites;
        var goldBefore = run.Gold;
        var bank = package.Content.Graph.ResolveRelic("item_rl02_bank").RunRules.Single(rule => rule.Kind == RelicRunRuleKind.NodeGoldUntilPurchase);
        var padding = package.Content.Graph.ResolveRelic("item_rl09_padding").RunRules.Single(rule => rule.Kind == RelicRunRuleKind.GoldBattleHealth);
        Require(bank.Amount == 2 && padding.Amount == 2, "fixture uses current authored +2 bank and 2 health per gold rules");
        var expectedGold = goldBefore + 2;
        var preview = app.BuildBattleConfig(boss);
        var previewHealth = PreparedPlayerHealth(preview);
        Require(preview.StartingGold == expectedGold && Snapshot(run) == before && save.SuccessfulRunWrites == writes,
            "read-only preview uses post-entry gold without publishing entry benefits");
        foreach (var (id, health) in baseline)
            Require(Math.Abs(previewHealth[id] - health - expectedGold * padding.Amount) < .01f,
                "preview temporary health uses bank-adjusted gold: " + id);
        save.FailWrites = true;
        Require(!app.TryBeginGrowthBattle(boss).Succeeded && Snapshot(run) == before && run.Gold == goldBefore,
            "failed battle save cannot retain entry gold or frozen padding input");
        save.FailWrites = false;
        Require(app.BuildBattleConfig(boss).StartingGold == expectedGold && Snapshot(run) == before,
            "preview after failed commit does not compound bank income");
        var started = app.TryBeginGrowthBattle(boss);
        Require(started.Succeeded && run.Gold == expectedGold && run.Action!.BattleFreeze!.Gold == expectedGold,
            "formal battle freezes bank income exactly once: " + started.Message);
        var formal = app.BuildBattleConfig(boss);
        var formalHealth = PreparedPlayerHealth(formal);
        Require(formal.StartingGold == expectedGold && formalHealth.Count == previewHealth.Count &&
            formalHealth.All(pair => Math.Abs(pair.Value - previewHealth[pair.Key]) < .01f),
            "formal gold and temporary health match read-only post-entry preview");
        var committed = Snapshot(run);
        Require(app.TryBeginGrowthBattle(boss).Succeeded && Snapshot(run) == committed && run.Gold == expectedGold,
            "repeated begin resumes the freeze without a second bank grant");
        var restored = new RunApplication(package, save, "growth-journey");
        Require(restored.ActiveRun!.Gold == expectedGold && restored.ActiveRun.Action!.BattleFreeze!.Gold == expectedGold &&
            restored.BuildBattleConfig(restored.CurrentEncounter()).StartingGold == expectedGold,
            "restoring a frozen battle never reapplies entry income");

        // Pure fixed-input rule probes avoid simulating seven to nine battles.
        // BattleNumber here explicitly means completed battles, as in settlement.
        var rules = new RunRelicRules(package.Content);
        var interest = package.Content.Graph.ResolveRelic("item_rl08_piggy").RunRules.Single(rule => rule.Kind == RelicRunRuleKind.VictoryInterest);
        Require(interest.Limit == 8, "interest boundary fixture uses authored eighth battle limit");
        var reward = new BattleResult(BattleOutcome.PlayerVictory, 1, "interest-boundary-fixture", [], 10);
        var encounter = new EncounterPlan("规则边界", "", [], false, false);
        var choice = new CompiledRunChoice("piggy", "存钱罐", "", [], [],
            [new(RunOperationKind.GrantItem, 1, ContentId: "item_rl08_piggy")], FailureOperations: []);
        foreach (var completed in new[] { 7, 8, 9 })
        {
            var probe = new ActiveRunDto
            {
                Gold = 11, BattleNumber = completed, CurrentRunHealth = 100, MaximumRunHealth = 100,
                Items = [new ItemInstanceDto { InstanceId = "fixture-piggy", ContentId = "item_rl08_piggy" }]
            };
            Require(rules.SettleBattle(probe, reward, encounter, 100, (_, _) => false) &&
                probe.Gold == 11 + (completed <= 8 ? 18 : 0),
                "interest uses held minus spent gold at completed battle " + completed);
            var supply = new ActiveRunDto { BattleNumber = completed };
            var shelf = new PendingRunOffer("fixed-piggy", RunOfferKind.CombatReward, "固定遗物", true, false, 0, completed, [choice]);
            Require(RunItemSupplyPolicy.CanAcquire(supply, "item_rl08_piggy", package.Content) == (completed < 8) &&
                RunItemSupplyPolicy.VisibleOffer(supply, shelf, package.Content).Choices.Length == (completed < 8 ? 1 : 0),
                "expired piggy is removed from new acquisition and frozen offers after battle " + completed);
        }
    }

    private static Dictionary<string, float> PreparedPlayerHealth(BattleConfig config)
    {
        // Initializes the formal attribute/trait/relic path only; never advances
        // ticks or generates a battle receipt for this gold-binding check.
        using var initialized = new BattleSimulation(config);
        Require(initialized.RemainingGold == config.StartingGold, "runtime initial spend baseline equals prepared gold");
        return initialized.Units.Where(unit => unit.Team == 0 && !unit.IsTemporary)
            .ToDictionary(unit => unit.SourceInstanceId, unit => unit.MaxHealth, StringComparer.Ordinal);
    }

    private static ExpeditionSiteDto FindReachableOpeningEnemy(RunApplication app, CompiledExpedition definition)
    {
        var map = app.ActiveRun!.Expedition!;
        return map.Sites.Where(site => site.EncounterProfileId.Length > 0 && definition.Encounters[site.EncounterProfileId].OpeningCandidate)
            .OrderBy(site => site.CellIndex).First(site => RouteToSite(map, site).Length > 0 ||
                ExpeditionNavigation.CanInteractWithSite(map, site.SiteId));
    }

    private static void WalkToSite(RunApplication app, string siteId)
    {
        var run = app.ActiveRun!;
        var map = run.Expedition!;
        var site = map.Sites.Single(value => value.SiteId == siteId);
        var route = RouteToSite(map, site);
        var budget = map.RemainingActions;
        var gold = run.Gold;
        var settlements = run.Growth!.History.Count;
        foreach (var step in route) Require(app.MoveExpedition(step), "legal four-neighbor move commits and reveals");
        Require(app.CanInteractExpeditionSite(siteId) && run.Expedition!.RemainingActions == budget && run.Gold == gold &&
            run.Growth.History.Count == settlements, "walking and discovery cannot produce or charge encounters");
    }

    private static ImmutableArray<int> RouteToSite(ExpeditionStateDto map, ExpeditionSiteDto site)
    {
        var targets = new HashSet<int>(ExpeditionNavigation.Neighbors(map.Size, site.CellIndex)
            .Where(index => !ExpeditionNavigation.IsBlocked(map, index)));
        if (!ExpeditionNavigation.IsBlocked(map, site.CellIndex)) targets.Add(site.CellIndex);
        var previous = Enumerable.Repeat(-1, map.Cells.Count).ToArray();
        var queue = new Queue<int>();
        queue.Enqueue(map.PositionIndex);
        previous[map.PositionIndex] = map.PositionIndex;
        var destination = -1;
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (targets.Contains(current)) { destination = current; break; }
            foreach (var next in ExpeditionNavigation.Neighbors(map.Size, current))
                if (previous[next] < 0 && !ExpeditionNavigation.IsBlocked(map, next))
                { previous[next] = current; queue.Enqueue(next); }
        }
        if (destination < 0) return [];
        var route = new List<int>();
        for (var index = destination; index != map.PositionIndex; index = previous[index]) route.Add(index);
        route.Reverse();
        return [.. route];
    }

    private static int ServiceOrder(TowerNodeType kind) => kind switch
    {
        TowerNodeType.Rest => 0, TowerNodeType.Event => 1, TowerNodeType.Recruitment => 2, TowerNodeType.Shop => 3, _ => 4
    };
    private static string Snapshot(ActiveRunDto run) => JsonSerializer.Serialize(run);
    private static string Stock(ExpeditionStateDto map) => JsonSerializer.Serialize(new
    {
        Sites = map.Sites.OrderBy(site => site.SiteId, StringComparer.Ordinal).Select(site => new
        { site.SiteId, site.ContentSeed, site.EncounterProfileId, site.FloorRuleId, site.ServiceOffer, site.RewardGroups }),
        map.StageBossFloorRuleIds, map.StageBossRewardGroups
    });
    private static object? ContentOffer(PendingRunOffer? offer) => offer is null ? null : new
    {
        offer.SamplingKey, offer.Kind, offer.RewardCategory, offer.AllowSkip, offer.Repeatable, offer.Choices
    };
    private static string Content(ExpeditionStateDto map) => JsonSerializer.Serialize(new
    {
        map.Size, map.GenerationVersion, map.PositionIndex, map.Cells,
        Sites = map.Sites.OrderBy(site => site.SiteId, StringComparer.Ordinal).Select(site => new
        {
            site.SiteId, site.ContentSeed, site.EncounterProfileId, site.FloorRuleId,
            Service = ContentOffer(site.ServiceOffer), Rewards = site.RewardGroups.Select(ContentOffer).ToArray()
        }),
        map.StageBossFloorRuleIds,
        BossRewards = map.StageBossRewardGroups.Select(groups => groups.Select(ContentOffer).ToArray()).ToArray()
    });
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

internal sealed class ExpeditionRunMemorySave : IRunSaveService
{
    public string? Json { get; private set; }
    public bool FailWrites { get; set; }
    public bool FailMetaWrites { get; set; }
    public bool FailDelete { get; set; }
    public int SuccessfulRunWrites { get; private set; }
    public int SuccessfulMetaWrites { get; private set; }
    private MetaProgressDto _meta = new();
    public ActiveRunDto? LoadActiveRun() => Json is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(Json);
    public MetaProgressDto LoadMeta() => JsonSerializer.Deserialize<MetaProgressDto>(JsonSerializer.Serialize(_meta))!;
    public SettingsDto LoadSettings() => new() { ReduceUiMotion = true };
    public bool SaveMeta(MetaProgressDto value)
    {
        if (FailMetaWrites) return false;
        _meta = JsonSerializer.Deserialize<MetaProgressDto>(JsonSerializer.Serialize(value))!;
        SuccessfulMetaWrites++;
        return true;
    }
    public bool SaveSettings(SettingsDto value) => true;
    public bool SaveActiveRun(ActiveRunDto value)
    {
        if (FailWrites) return false;
        Json = JsonSerializer.Serialize(value);
        SuccessfulRunWrites++;
        return true;
    }
    public void SetFixtureJson(string json) => Json = json;
    public void DeleteActiveRun()
    {
        if (FailDelete) throw new System.IO.IOException("controlled terminal delete failure");
        Json = null;
    }
}
