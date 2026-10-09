using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Expedition;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Transaction fixtures may inspect generated routes and set isolated resource-
// boundary inputs. They do not establish natural acquisition, UI or balance.
public partial class V1RunEventsContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var publication = await GrowthContentPackage.CreateReadyAsync(this);
            var package = publication.Package ?? throw new InvalidOperationException(string.Join(';', publication.Report.CoreErrors));
            VerifyPublishedRelicText(package);
            VerifyReveal(package);
            VerifyAltar(package);
            VerifyBankEntry(package);
            VerifyBounty(package);
            GD.Print("V1_RUN_EVENTS_CONTRACT_OK reveal-cost-save-retry frozen-shared-reward health-minimum owned-exhausted recipe-restore bank-boss-zero-paid-stop bounty-native-victory-site-once remainder-no-retroactivity");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("V1_RUN_EVENTS_CONTRACT_FAILED " + error); GetTree().Quit(1); }
    }

    private static void VerifyPublishedRelicText(CompiledGamePackage package)
    {
        var ids = Enumerable.Range(1, 11).Select(index => $"v1_relic_r{index:D2}").ToArray();
        Require(package.Content.Graph.Relics.Select(relic => relic.StableId).Order(StringComparer.Ordinal).SequenceEqual(ids),
            "all eleven formal relics are published for their player-visible rule contract");
        var runRuleCount = 0;
        foreach (var id in ids)
        {
            var relic = package.Content.Graph.ResolveRelic(id);
            foreach (var rule in relic.RunRules)
            {
                Require(!string.IsNullOrWhiteSpace(RelicRuleText.Describe(rule)), id + " published run rule has readable text");
                runRuleCount++;
            }
            Require(package.Content.TryGet(id, out var entry) && entry.Definition is ItemDefinition, id + " independent item definition exists");
            // This is the actual reward / shop detail path that failed during
            // the second Boss's real reward group, including combat-only relics.
            var details = RunOfferDetailText.ItemRules(package.Content, (ItemDefinition)entry.Definition);
            Require(relic.RunRules.IsEmpty || !string.IsNullOrWhiteSpace(details),
                id + " actual item detail composes without a missing Run rule branch");
        }
        Require(runRuleCount == 6, "six published Run rules are all covered, combat-only relics are not mistaken for Run rules");
        var bank = package.Content.Graph.ResolveRelic("v1_relic_r02").RunRules.Single();
        var bankText = RelicRuleText.Describe(bank);
        Require(bankText.Contains("Boss", StringComparison.Ordinal) && bankText.Contains("免费", StringComparison.Ordinal) &&
            bankText.Contains("首次商店支付金币后永久停止", StringComparison.Ordinal), "R02 describes Boss/free-entry exclusions and paid-stop policy");
        var bounty = package.Content.Graph.ResolveRelic("v1_relic_r11").RunRules.Single();
        var bountyText = RelicRuleText.Describe(bounty);
        Require(bountyText.Contains("每累计 6 名", StringComparison.Ordinal) && bountyText.Contains("原生非临时", StringComparison.Ordinal) &&
            bountyText.Contains("余数保留", StringComparison.Ordinal) && bountyText.Contains("不追溯", StringComparison.Ordinal),
            "R11 describes actual victory-only native-kill threshold and persistent remainder");
        foreach (var kind in Enum.GetValues<RelicRunRuleKind>())
            Require(!string.IsNullOrWhiteSpace(RelicRuleText.Describe(bounty with { Kind = kind })), "known rule kind has an explicit description: " + kind);
        var rejected = false;
        try { RelicRuleText.Describe(bounty with { Kind = (RelicRunRuleKind)int.MaxValue }); }
        catch (ArgumentOutOfRangeException) { rejected = true; }
        Require(rejected, "unknown run rule kinds still fail explicitly instead of hiding behind fallback prose");
        GD.Print("V1_RELIC_RULE_TEXT_OK eleven-published six-run-rules actual-item-details r02-boss-zero r11-native-victory unknown-rejected");
    }

    private static void VerifyReveal(CompiledGamePackage package)
    {
        var fixture = EventFixture(package, "v1_event_v05");
        var (app, save) = NewRun(fixture);
        var target = ReachEvent(app);
        Require(app.InspectExpeditionSite(target.SiteId), "inspect authored scout event");
        var offer = app.PendingOffer!;
        var choice = offer.Choices.Single(value => value.Operations.Any(operation => operation.Kind == RunOperationKind.RevealSites));
        var run = app.ActiveRun!;
        var before = Snapshot(run);
        var disk = save.Json;
        var map = run.Expedition!;
        var position = map.PositionIndex;
        var expected = map.Sites.Where(site => !map.Cells[site.CellIndex].Revealed)
            .OrderBy(site => Math.Abs(site.CellIndex % map.Size - position % map.Size) + Math.Abs(site.CellIndex / map.Size - position / map.Size))
            .ThenBy(site => site.SiteId, StringComparer.Ordinal).Take(3).Select(site => site.CellIndex).ToArray();
        Require(expected.Length == 3, "scout fixture has at least three unknown sites");
        var oldRevealed = map.Cells.Where(cell => cell.Revealed).Select(cell => cell.Index).ToHashSet();
        var oldStock = Stock(map);
        var actions = map.RemainingActions;
        var materials = run.Growth!.Materials;
        save.FailWrites = true;
        Require(app.ResolveOffer(offer.OfferId, choice.StableId).Failure == RunDecisionFailure.PersistenceFailed &&
            Snapshot(run) == before && save.Json == disk, "scout save failure rolls back revelation, action and production");
        save.FailWrites = false;
        Require(app.ResolveOffer(offer.OfferId, choice.StableId).Succeeded, "scout retry succeeds once");
        Require(run.Expedition!.PositionIndex == position && run.Expedition.RemainingActions == actions - 1 &&
            run.Growth!.Materials == materials + 1 && Stock(run.Expedition) == oldStock &&
            run.Expedition.Cells.Where(cell => cell.Revealed && !oldRevealed.Contains(cell.Index)).Select(cell => cell.Index).Order()
                .SequenceEqual(expected.Order()), "reveal exact nearest sites, no movement, stock reroll, guard clear or free action");
        var completed = Snapshot(run);
        Require(!app.ResolveOffer(offer.OfferId, choice.StableId).Succeeded && Snapshot(run) == completed, "scout duplicate cannot produce twice");
        var restored = new RunApplication(fixture, save, "v1-events");
        Require(restored.ActiveRun is not null && Snapshot(restored.ActiveRun) == completed, "scout persists through cold restore");
        var allKnown = Clone(run);
        foreach (var site in allKnown.Expedition!.Sites) allKnown.Expedition.Cells[site.CellIndex].Revealed = true;
        Require(!Decisions(fixture, save).Check(allKnown, choice).Succeeded, "all-known intelligence choice is ineligible");
        GD.Print("V1_SCOUT_TRANSACTION_OK");
    }

    private static void VerifyAltar(CompiledGamePackage package)
    {
        var fixture = EventFixture(package, "v1_event_v06");
        var (app, save) = NewRun(fixture);
        var target = ReachEvent(app);
        Require(app.InspectExpeditionSite(target.SiteId), "inspect authored altar");
        var offer = app.PendingOffer!;
        var pooled = offer.Choices.Where(choice => choice.ContentId.Length > 0).ToArray();
        Require(pooled.Length == 2 && pooled.Select(choice => choice.ContentId).Distinct().Count() == 1 &&
            offer.RecipeId == "v1_event_v06", "two authored costs share one frozen relic identity");
        var health = pooled.Single(choice => choice.Costs.Single().Kind == RunOperationKind.SpendRunHealth);
        var gold = pooled.Single(choice => choice.Costs.Single().Kind == RunOperationKind.SpendGold);
        var run = app.ActiveRun!;
        var decisions = Decisions(fixture, save);
        var boundary = Clone(run);
        boundary.CurrentRunHealth = 10;
        Require(!decisions.Check(boundary, health).Succeeded, "ten health cannot pay ten and survive");
        boundary.CurrentRunHealth = 11;
        Require(decisions.Check(boundary, health).Succeeded, "eleven health can pay ten");
        boundary.Gold = 7;
        Require(!decisions.Check(boundary, gold).Succeeded, "seven gold cannot pay eight");
        boundary.Gold = 8;
        Require(decisions.Check(boundary, gold).Succeeded, "eight gold can pay eight");
        Require(!RunDecisionValidation.ValidOperation(new(RunOperationKind.SpendRunHealth, 10), _ => true, _ => true),
            "health payment is a cost, never a negative recovery or reward operation");
        var ownedSave = save.Fork();
        var ownedApp = new RunApplication(fixture, ownedSave, "v1-events");
        Require(ownedApp.GrantItem(health.ContentId) && ownedApp.InspectExpeditionSite(target.SiteId), "owned-target boundary uses a formal item grant");
        Require(ownedApp.PendingOffer!.Choices.All(choice => choice.ContentId != health.ContentId) &&
            ownedApp.PendingOffer.Choices.Any(choice => choice.Operations.Any(operation => operation.Kind == RunOperationKind.GainGold && operation.Amount == 4)),
            "both owned relic claims disappear together while fixed leave choice remains");
        var full = Clone(run);
        var persistence = Persistence(fixture, save);
        var rewards = new RunRewardEconomyService(fixture.Content, fixture.Project, persistence, null);
        var recipe = fixture.Project.Campaign.OfferRecipesById["v1_event_v06"];
        foreach (var id in recipe.Pool!.ContentIds) Require(rewards.GrantToProjection(full, id), "isolated exhausted pool fixture grants each unique relic once");
        var exhausted = decisions.CreateOffer(full, recipe, "exhausted-fixture", samplingIdentity: "exhausted-fixture");
        Require(exhausted.Choices.Length == 1 && exhausted.Choices[0].ContentId.Length == 0, "empty unique pool preserves only authored alternative");
        var malformed = Clone(run);
        var stock = malformed.Expedition!.Sites.Single(site => site.SiteId == target.SiteId);
        stock.ServiceOffer = stock.ServiceOffer! with { RecipeId = "v1_event_v05" };
        Require(!persistence.ValidateRun(malformed), "wrong frozen recipe cannot restore");
        malformed = Clone(run); stock = malformed.Expedition!.Sites.Single(site => site.SiteId == target.SiteId);
        stock.ServiceOffer = stock.ServiceOffer! with { Choices = stock.ServiceOffer.Choices.Where(choice => choice.StableId != gold.StableId).ToImmutableArray() };
        Require(!persistence.ValidateRun(malformed), "half a shared reward pair cannot restore");
        var original = Snapshot(run); var disk = save.Json;
        var oldHp = run.CurrentRunHealth; var oldGold = run.Gold; var oldActions = run.Expedition!.RemainingActions; var oldMaterials = run.Growth!.Materials;
        save.FailWrites = true;
        Require(app.ResolveOffer(offer.OfferId, health.StableId).Failure == RunDecisionFailure.PersistenceFailed &&
            Snapshot(run) == original && save.Json == disk, "altar payment and grant roll back together on save failure");
        save.FailWrites = false;
        Require(app.ResolveOffer(offer.OfferId, health.StableId).Succeeded && run.CurrentRunHealth == oldHp - 10 && run.Gold == oldGold &&
            run.Items.Count(item => item.ContentId == health.ContentId) == 1 && run.Expedition!.RemainingActions == oldActions - 1 &&
            run.Growth!.Materials == oldMaterials + 1, "one legal health payment grants the fixed relic and settles exactly once");
        Require(!app.ResolveOffer(offer.OfferId, gold.StableId).Succeeded, "other cost cannot claim the same event twice");
        Require(new RunApplication(fixture, save, "v1-events").ActiveRun is { } restored && Snapshot(restored) == Snapshot(run), "altar cold restore stable");
        GD.Print("V1_ALTAR_TRANSACTION_OK");
    }

    private static void VerifyBankEntry(CompiledGamePackage package)
    {
        var (app, save) = NewRun(package);
        var acquisitionGold = app.ActiveRun!.Gold;
        Require(app.GrantItem("v1_relic_r02") && app.ActiveRun!.Gold == acquisitionGold,
            "formal R02 acquisition is not a paid-site entry and grants no immediate gold");
        var run = app.ActiveRun!;
        var projection = Clone(run);
        var rules = new RunRelicRules(package.Content);
        var bank = projection.Items.Single(item => item.ContentId == "v1_relic_r02");
        var gold = projection.Gold;
        rules.EnterNode(projection, TowerNodeType.Combat);
        rules.EnterNode(projection, TowerNodeType.Rest);
        Require(projection.Gold == gold + 4 && bank.Charges == 0, "two eligible pre-purchase entries each grant exactly two gold");
        var beforeBoss = Snapshot(projection);
        rules.EnterNode(projection, TowerNodeType.Boss);
        Require(Snapshot(projection) == beforeBoss, "Boss grants no bank gold and does not consume its future entitlement");
        rules.EnterNode(projection, TowerNodeType.Shop);
        Require(projection.Gold == gold + 6 && bank.Charges == 0, "first Shop entry still pays before the actual purchase");
        rules.PaidShopPurchase(projection);
        var afterPayment = Snapshot(projection);
        foreach (var type in new[] { TowerNodeType.Combat, TowerNodeType.Rest, TowerNodeType.Shop, TowerNodeType.Boss })
            rules.EnterNode(projection, type);
        Require(bank.Charges == 1 && Snapshot(projection) == afterPayment, "paid purchase permanently stops bank income on every later entry");

        // Exercise the shared preview / transaction / restore path, without
        // replacing a combat result or making this fixture a Boss balance gate.
        var boss = app.PreviewExpeditionBoss();
        var disk = save.Json;
        var original = Snapshot(run);
        var preview = app.BuildBattleConfig(boss);
        Require(preview.StartingGold == acquisitionGold && Snapshot(run) == original && save.Json == disk,
            "Boss preparation preview has no R02 gold benefit and never publishes");
        save.FailWrites = true;
        Require(!app.TryBeginGrowthBattle(boss).Succeeded && Snapshot(run) == original && save.Json == disk,
            "Boss entry save failure rolls back its action without R02 income");
        save.FailWrites = false;
        Require(app.TryBeginGrowthBattle(boss).Succeeded && run.Gold == acquisitionGold &&
            run.Items.Single(item => item.ContentId == "v1_relic_r02").Charges == 0,
            "formal Boss entry retains gold and the unpaid bank entitlement");
        var frozen = Snapshot(run);
        Require(app.TryBeginGrowthBattle(boss).Succeeded && Snapshot(run) == frozen,
            "retrying the same frozen Boss entry has no additional income or state change");
        var restored = new RunApplication(package, save, "v1-events");
        Require(restored.ActiveRun is { } loaded && Snapshot(loaded) == frozen &&
            restored.BuildBattleConfig(restored.CurrentEncounter()).StartingGold == acquisitionGold,
            "cold restore keeps the Boss freeze and zero bank entry income");
        GD.Print("V1_BANK_BOSS_ENTRY_OK ordinary-two shop-before-payment paid-stops preview-save-retry-restore");
    }

    private static void VerifyBounty(CompiledGamePackage package)
    {
        Require(package.Content.Graph.TryGetRelic("v1_relic_r11", out _), "published R11 is required for its transaction contract");
        var (app, save) = NewRun(package);
        var first = ReachOrdinary(app); FinishBattle(app, first);
        Require(app.GrantItem("v1_relic_r11"), "explicit contract setup grants R11 after a genuine earlier clear");
        var run = app.ActiveRun!;
        var item = run.Items.Single(value => value.ContentId == "v1_relic_r11");
        Require(item.RunRuleStates.Single() is { GrantedAtSequence: 1, Remainder: 0, ClearedSites.Count: 0 }, "earlier kills are not reconstructed on acquisition");
        var initialEquipment = run.EquipmentInventory.Count;
        var total = 0;
        for (var index = 0; index < 3; index++)
        {
            var encounter = ReachOrdinary(app);
            Require(app.TryBeginGrowthBattle(encounter).Succeeded, "freeze a real native-enemy bounty battle");
            using var simulation = new BattleSimulation(app.BuildBattleConfig(encounter));
            var result = simulation.RunToEnd();
            Require(result.Outcome == BattleOutcome.PlayerVictory, "bounty route needs a genuine victory");
            var count = encounter.EnemyIds.Count;
            var projection = Clone(run);
            var rules = new RunRelicRules(package.Content);
            var report = result.Units.First(unit => unit.Team == 1);
            var extended = result with { Units = result.Units.Add(report with { RuntimeId = "temporary-test", SourceInstanceId = "temporary-test", IsTemporary = true })
                .Add(report with { RuntimeId = "late-test", SourceInstanceId = "late-test", JoinTick = 1 }) };
            var grants = 0;
            Require(rules.SettleBattle(projection, extended, encounter, run.Gold, (_, _) => { grants++; return true; }) &&
                projection.Items.Single(value => value.ContentId == "v1_relic_r11").RunRuleStates.Single().ClearedSites[^1].NativeKills == count,
                "summoned/copied/late report entries add no native kill credit");
            var projectedOnce = Snapshot(projection); var grantOnce = grants;
            Require(rules.SettleBattle(projection, extended, encounter, run.Gold, (_, _) => { grants++; return true; }) &&
                Snapshot(projection) == projectedOnce && grants == grantOnce, "same site cannot pay twice even before publication");
            foreach (var ignored in new[] { result with { Outcome = BattleOutcome.PlayerDefeat }, result with { Outcome = BattleOutcome.Timeout } })
            {
                projection = Clone(run); var beforePolicy = Snapshot(projection);
                Require(rules.SettleBattle(projection, ignored, encounter, run.Gold, (_, _) => false) && Snapshot(projection) == beforePolicy,
                    "controlled defeat/timeout policy does not count kills or modify the ledger");
            }
            projection = Clone(run);
            projection.Items.Single(value => value.ContentId == "v1_relic_r11").RunRuleStates.Single().GrantedAtSequence = run.Action!.Source.Sequence;
            var newlyAcquired = Snapshot(projection);
            Require(rules.SettleBattle(projection, result, encounter, run.Gold, (_, _) => false) && Snapshot(projection) == newlyAcquired,
                "a relic acquired in this action cannot count its own clear");
            var snapshot = Snapshot(run); var disk = save.Json;
            if (index == 2)
            {
                save.FailWrites = true;
                Require(!app.ResolveBattle(result, encounter).Accepted && Snapshot(run) == snapshot && save.Json == disk,
                    "threshold reward plus remainder and site identity all roll back on save failure");
                save.FailWrites = false;
            }
            Require(app.ResolveBattle(result, encounter).Accepted, "publish actual bounty victory");
            DrainRewards(app);
            total += count;
            var state = run.Items.Single(value => value.ContentId == "v1_relic_r11").RunRuleStates.Single();
            Require(state.Remainder == total % 6 && state.ClearedSites.Count == index + 1 &&
                run.EquipmentInventory.Count == initialEquipment + total / 6, "exact reward threshold and remainder, without optional encounter rewards");
            Require(!app.ResolveBattle(result, encounter).Accepted, "accepted battle receipt cannot be replayed");
        }
        var restoredApp = new RunApplication(package, save, "v1-events");
        Require(restoredApp.ActiveRun is not null && Snapshot(restoredApp.ActiveRun) == Snapshot(run), "bounty ledger cold restore");
        var invalid = Clone(run);
        invalid.Items.Single(value => value.ContentId == "v1_relic_r11").RunRuleStates.Single().Remainder++;
        Require(!Persistence(package, save).ValidateRun(invalid), "remainder must reconcile with distinct clear receipts");
        var beforeBoss = Snapshot(run.Items.Single(value => value.ContentId == "v1_relic_r11"));
        var boss = app.PreviewExpeditionBoss();
        Require(app.TryBeginGrowthBattle(boss).Succeeded, "actual Boss battle begins");
        using (var bossBattle = new BattleSimulation(app.BuildBattleConfig(boss)))
        {
            var actual = bossBattle.RunToEnd();
            // The two-hero transaction fixture is deliberately not a Boss balance
            // gate. Test the victory-only policy on a disposable projection, then
            // commit only the unmodified result produced by the real simulation.
            var projection = Clone(run);
            var beforePolicy = Snapshot(projection);
            Require(new RunRelicRules(package.Content).SettleBattle(projection,
                actual with { Outcome = BattleOutcome.PlayerVictory }, boss, run.Gold, (_, _) =>
                    throw new InvalidOperationException("Boss must never request a bounty reward")) &&
                Snapshot(projection) == beforePolicy, "Boss victory policy excludes all native kill credit");
            Require(app.ResolveBattle(actual, boss).Accepted, "unmodified actual Boss result commits");
            DrainRewards(app);
            GD.Print("V1_BOUNTY_BOSS_OUTCOME " + actual.Outcome);
        }
        Require(Snapshot(run.Items.Single(value => value.ContentId == "v1_relic_r11")) == beforeBoss, "actual first Boss grants no bounty credit");
        GD.Print("V1_BOUNTY_TRANSACTION_OK");
    }

    private static CompiledGamePackage EventFixture(CompiledGamePackage package, string recipeId)
    {
        Require(package.Project.Campaign.OfferRecipesById.TryGetValue(recipeId, out var recipe), "event recipe published: " + recipeId);
        var map = (CompiledExpedition)package.Project.Campaign.Progression;
        var project = package.Project with { Campaign = package.Project.Campaign with { Progression = map with
        { Sites = map.Sites.Select(site => site.Kind == TowerNodeType.Event ? site with { Offer = null, OfferRecipes = [recipe!] } : site).ToImmutableArray() } } };
        var id = "v1-event-contract-" + recipeId;
        var source = package.DefaultProfile;
        var profile = source with { ProfileId = id, Project = project, ContentFingerprint = RunProfileContentFingerprint.Compute(id,
            source.CompatibilityVersion, source.ProgressionProtocolVersion, source.RewardProtocolVersion, source.IdentityProtocolVersion,
            project, source.GrowthRules, package.Content.Graph) };
        return new(package.Content, package.ProfilesById.Add(id, profile), id, package.PublicationVersion) { AuthoringOwner = package.AuthoringOwner };
    }

    private static (RunApplication App, MemorySave Save) NewRun(CompiledGamePackage package)
    {
        var save = new MemorySave();
        var app = new RunApplication(package, save, "v1-events");
        for (ulong seed = 20261007; seed < 20261135; seed++)
        {
            Require(app.BeginOpeningRecruitment(seed), "real opening draft");
            if (!new[] { "prototype_u11", "prototype_u02" }.All(app.ActiveRun!.OpeningRecruitment!.CandidateIds.Contains)) continue;
            Require(app.ToggleOpeningHero("prototype_u11") && app.ToggleOpeningHero("prototype_u02") && app.ConfirmOpeningRecruitment(), "real two-hero opening");
            return (app, save);
        }
        throw new InvalidOperationException("No eligible opening in bounded contract search.");
    }

    private static ExpeditionSiteDto ReachEvent(RunApplication app)
    {
        var map = app.ActiveRun!.Expedition!;
        var expedition = (CompiledExpedition)app.Project.Campaign.Progression;
        var site = map.Sites.Where(value => expedition.Sites.Single(definition => definition.StableId == value.DefinitionId).Kind == TowerNodeType.Event)
            .Select(value => (Site: value, Path: Route(map, value.CellIndex, true)))
            .Where(value => value.Path is not null).OrderBy(value => value.Path!.Count(index => ExpeditionNavigation.IsBlocked(map, index)))
            .ThenBy(value => value.Path!.Count).First();
        foreach (var cell in site.Path!)
        {
            Require(app.ActiveRun!.Expedition!.RemainingActions > 1, "event fixture reserves one action for its actual choice");
            if (ExpeditionNavigation.SiteAt(app.ActiveRun.Expedition, cell) is { Cleared: false, EncounterProfileId.Length: > 0 } guard)
                FinishBattle(app, app.PreviewExpeditionEncounter(guard.SiteId));
            Require(app.MoveExpedition(cell), "actual route movement to event");
        }
        return app.ActiveRun!.Expedition!.Sites.Single(value => value.SiteId == site.Site.SiteId);
    }

    private static EncounterPlan ReachOrdinary(RunApplication app)
    {
        var definition = (CompiledExpedition)app.Project.Campaign.Progression;
        var ordinarySiteIds = definition.Sites.Where(site => site.Kind == TowerNodeType.Combat)
            .Select(site => site.StableId).ToHashSet(StringComparer.Ordinal);
        for (var step = 0; step < app.ActiveRun!.Expedition!.Cells.Count; step++)
        {
            var map = app.ActiveRun!.Expedition!;
            var targets = map.Sites.Where(site => !site.Cleared && map.Cells[site.CellIndex].Revealed && site.EncounterProfileId.Length > 0 &&
                    ordinarySiteIds.Contains(site.DefinitionId) &&
                    definition.Encounters.TryGetValue(site.EncounterProfileId, out var profile) &&
                    profile.Encounter.NodeType == TowerNodeType.Combat)
                .Select(site => (Site: site, Path: Route(map, site.CellIndex, false, revealedOnly: true))).Where(value => value.Path is not null)
                .OrderBy(value => value.Path!.Count).ToArray();
            if (targets.Length > 0)
            {
                var target = targets[0];
                var path = target.Path!;
                foreach (var cell in path.Take(path.Count - 1)) Require(app.MoveExpedition(cell), "actual movement to ordinary guard");
                if (!app.CanInteractExpeditionSite(target.Site.SiteId))
                    throw new InvalidOperationException("ordinary encounter path ended before a legal interaction: " + target.Site.SiteId);
                return app.PreviewExpeditionEncounter(target.Site.SiteId);
            }
            var frontier = map.Cells.Where(cell => cell.Revealed && cell.Walkable &&
                    !ExpeditionNavigation.IsBlocked(map, cell.Index) && ExpeditionNavigation.RevealsNewCells(map, cell.Index))
                .Select(cell => ExpeditionNavigation.FindKnownPath(map, cell.Index))
                .Where(path => !path.IsEmpty).OrderBy(path => path.Length).FirstOrDefault();
            if (frontier.IsEmpty || !app.MoveExpedition(frontier[^1])) break;
        }
        throw new InvalidOperationException("No ordinary encounter is reachable after bounded exploration of legal revealed frontiers.");
    }

    private static List<int>? Route(ExpeditionStateDto map, int target, bool guards, bool revealedOnly = false)
    {
        var previous = new Dictionary<int, int> { [map.PositionIndex] = map.PositionIndex };
        var costs = new Dictionary<int, int> { [map.PositionIndex] = 0 };
        var queue = new PriorityQueue<int, int>(); queue.Enqueue(map.PositionIndex, 0);
        while (queue.TryDequeue(out var cell, out var cost))
        {
            if (cost != costs[cell]) continue;
            if (cell == target)
            {
                var result = new List<int>();
                for (; cell != map.PositionIndex; cell = previous[cell]) result.Add(cell);
                result.Reverse(); return result;
            }
            foreach (var next in ExpeditionNavigation.Neighbors(map.Size, cell))
            {
                if (!map.Cells[next].Walkable || revealedOnly && !map.Cells[next].Revealed ||
                    !guards && next != target && ExpeditionNavigation.IsBlocked(map, next)) continue;
                var candidate = cost + (ExpeditionNavigation.IsBlocked(map, next) ? 100 : 1);
                if (costs.TryGetValue(next, out var old) && old <= candidate) continue;
                costs[next] = candidate; previous[next] = cell; queue.Enqueue(next, candidate);
            }
        }
        return null;
    }

    private static void FinishBattle(RunApplication app, EncounterPlan encounter)
    {
        Require(app.TryBeginGrowthBattle(encounter).Succeeded, "actual battle begin");
        using var battle = new BattleSimulation(app.BuildBattleConfig(encounter));
        var result = battle.RunToEnd();
        Require(result.Outcome == BattleOutcome.PlayerVictory && app.ResolveBattle(result, encounter).Accepted, "actual battle victory commits");
        DrainRewards(app);
    }
    private static void DrainRewards(RunApplication app)
    {
        while (app.PendingOffer is { } offer) Require(app.ResolveOffer(offer.OfferId, null).Succeeded, "skip optional reward groups");
    }
    private static RunProgressionPersistenceService Persistence(CompiledGamePackage package, MemorySave save) =>
        new(package.Content, save, package.Project, package.GrowthRules, package.DefaultProfile);
    private static RunDecisionService Decisions(CompiledGamePackage package, MemorySave save) =>
        new(package.Content, package.Project, Persistence(package, save));
    private static ActiveRunDto Clone(ActiveRunDto run) => JsonSerializer.Deserialize<ActiveRunDto>(Snapshot(run))!;
    private static string Snapshot<T>(T value) => JsonSerializer.Serialize(value);
    private static string Stock(ExpeditionStateDto map) => Snapshot(map.Sites.Select(site => new
    { site.SiteId, site.Cleared, site.ServiceOffer, site.EncounterProfileId, site.RewardGroups }));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class MemorySave : IRunSaveService
    {
        public string? Json;
        public bool FailWrites;
        private MetaProgressDto _meta = new();
        public ActiveRunDto? LoadActiveRun() => Json is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(Json);
        public MetaProgressDto LoadMeta() => JsonSerializer.Deserialize<MetaProgressDto>(Snapshot(_meta))!;
        public SettingsDto LoadSettings() => new();
        public bool SaveActiveRun(ActiveRunDto value) { if (FailWrites) return false; Json = Snapshot(value); return true; }
        public bool SaveMeta(MetaProgressDto value) { _meta = value; return true; }
        public bool SaveSettings(SettingsDto value) => true;
        public void DeleteActiveRun() => Json = null;
        public MemorySave Fork() => new() { Json = Json, _meta = LoadMeta() };
    }
}
