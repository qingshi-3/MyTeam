using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using TowerAutobattler.Composition;
using TowerAutobattler.Expedition;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;
using TowerAutobattler.Battle;

public partial class FlowerCultivationContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var ready = await GrowthContentPackage.CreateReadyAsync(this);
            var package = ready.Package ?? throw new InvalidOperationException(string.Join(';', ready.Report.CoreErrors));
            VerifyConfiguration(package);
            var (app, save) = NewFlowerRun(package);
            VerifyProjection(package, app);
            VerifyServiceTransaction(package, app, save);
            VerifyBattleOutcomes(package);
            await VerifyBreedingTransactions();
            VerifyRememberedTransactions(package);
            GD.Print("FLOWER_CULTIVATION_CONTRACT_OK fixed-pair frozen-participants self-target retarget atomic-save restore dedupe paid-defeat stage-boss final-no-output invalid-config fingerprint-protection");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("FLOWER_CULTIVATION_CONTRACT_FAILED " + error); GetTree().Quit(1); }
    }

    private static void VerifyConfiguration(CompiledGamePackage package)
    {
        Require((int)GrowthProductionMode.Attack == 0 && (int)GrowthProductionMode.Vitality == 1 &&
            (int)GrowthProductionMode.Research == 2 && (int)GrowthProductionMode.Cultivation == 3, "historical enum ordinals stay stable");
        var rules = package.GrowthRules!;
        var flower = rules.Heroes["prototype_u16"];
        Require(flower.ProductionModes.SequenceEqual(new[] { GrowthProductionMode.Cultivation }) &&
            flower.FixedAttackGain == 4 && flower.FixedMaxHealthGain == 40 && flower.GrowthRate == 0,
            "flower authors only fixed +4/+40 cultivation, without legacy percent output");
        Require(rules.Heroes.Values.Count(hero => !hero.ProductionModes.IsEmpty) == 1, "only the authorized flower produces");
        foreach (var bad in new[] { float.NaN, float.PositiveInfinity, -1f })
        {
            RejectConfig(hero => hero.FixedAttackGain = bad);
            RejectConfig(hero => hero.FixedMaxHealthGain = bad);
        }
        RejectConfig(hero => hero.ProductionModes = [99]);
        RejectConfig(hero => hero.ProductionModes = [3, 3]);
        var facts = GrowthPresentation.Project("prototype_u16", BattleSetupFactory.Snapshot(
            package.Content.Catalog.Heroes.Single(hero => hero.StableId == "prototype_u16"), package.Content), rules);
        Require(facts.Description.Contains("攻击永久 +4") && facts.Description.Contains("生命永久 +40") &&
            facts.Description.Contains("未指定时不产出"), "shared inspection describes fixed pair and explicit assignment");

        void RejectConfig(Action<GrowthHeroDefinition> edit)
        {
            var definition = (GrowthRulesDefinition)GD.Load<GrowthRulesDefinition>(GrowthContentPackage.RulesPath).Duplicate(true);
            edit(definition.Heroes.Single(hero => hero.ContentId == "prototype_u16"));
            var rejected = false;
            try { GrowthRulesCompiler.Compile(definition, package.Content); }
            catch (InvalidOperationException) { rejected = true; }
            Require(rejected, "invalid authoring must be rejected before publication");
        }
    }

    private static (RunApplication, MemorySave) NewFlowerRun(CompiledGamePackage package, string? partnerContentId = null)
    {
        var save = new MemorySave();
        var app = new RunApplication(package, save, "flower-contract");
        for (ulong seed = 20261006; seed < 20261134; seed++)
        {
            Require(app.BeginOpeningRecruitment(seed), "real opening draft saves");
            var candidates = app.ActiveRun!.OpeningRecruitment!.CandidateIds;
            if (!candidates.Contains("prototype_u16") || partnerContentId is not null && !candidates.Contains(partnerContentId)) continue;
            Require(app.ToggleOpeningHero("prototype_u16") && app.ToggleOpeningHero(partnerContentId ?? candidates.First(id =>
                id != "prototype_u16" && package.GrowthRules!.Heroes[id].BreedingYield == 0)) &&
                app.ConfirmOpeningRecruitment(), "flower and a naturally offered partner enter authored map");
            return (app, save);
        }
        throw new InvalidOperationException("No reproducible flower opening in bounded seed search.");
    }

    private async System.Threading.Tasks.Task VerifyBreedingTransactions()
    {
        // A focused authoring fixture isolates production from the new combat
        // ability. The production catalog itself is not edited by this check.
        var authored = (GrowthRulesDefinition)GD.Load<GrowthRulesDefinition>(GrowthContentPackage.RulesPath).Duplicate(true);
        foreach (var definition in authored.Heroes)
        {
            definition.BreedingYield = 0;
            definition.AttackGainPerBreedingPoint = 0;
            definition.MaxHealthGainPerBreedingPoint = 0;
        }
        var tree = authored.Heroes.Single(hero => hero.ContentId == "prototype_u04");
        tree.BreedingYield = 1; tree.AttackGainPerBreedingPoint = 1; tree.MaxHealthGainPerBreedingPoint = 10;
        var ready = await GrowthContentPackage.CreateReadyAsync(this, authored);
        var package = ready.Package ?? throw new InvalidOperationException(string.Join(';', ready.Report.CoreErrors));
        var (app, save) = NewFlowerRun(package, "prototype_u04");
        foreach (var invalidValue in new[] { float.NaN, float.PositiveInfinity, -1f })
        {
            RejectBreedingConfig(hero => hero.AttackGainPerBreedingPoint = invalidValue);
            RejectBreedingConfig(hero => hero.MaxHealthGainPerBreedingPoint = invalidValue);
        }
        RejectBreedingConfig(hero => hero.BreedingYield = -1);
        RejectBreedingConfig(hero => hero.BreedingYield = 0);
        var persistence = new RunProgressionPersistenceService(package.Content, save, package.Project, package.GrowthRules, package.DefaultProfile);
        var service = new GrowthRunService(package.Content, package.Project, package.GrowthRules, persistence);
        var flowerId = app.ActiveRun!.Roster.Single(hero => hero.ContentId == "prototype_u16").InstanceId;
        var treeId = app.ActiveRun.Roster.Single(hero => hero.ContentId == "prototype_u04").InstanceId;
        Require(app.SetGrowthAssignment(flowerId, treeId, GrowthProductionMode.Cultivation).Succeeded, "fixture saves flower target");
        var untouched = Snapshot(app.ActiveRun);
        var projected = persistence.CloneRun(app.ActiveRun);
        for (var step = 1; step <= 4; step++) Settle(projected, step);
        var grown = projected.Roster.Single(hero => hero.InstanceId == treeId);
        Require(grown.Growth.BreedingPoints == 4 && grown.Growth.AddedAttack == 22 && grown.Growth.AddedMaxHealth == 220,
            "four shared opportunities use old breeding points: 4+5+6+7");
        Require(service.Validate(projected), "breeding totals reconcile with ordered settlement records");
        var restored = JsonSerializer.Deserialize<ActiveRunDto>(Snapshot(projected))!;
        Settle(restored, 5);
        Require(restored.Roster.Single(hero => hero.InstanceId == treeId).Growth.AddedAttack == 30,
            "fifth seed is eight after serialization, without replaying old points");
        Require(grown.Growth.BreedingPoints == 4, "projection and restored histories are independent");
        var invalid = persistence.CloneRun(projected);
        invalid.Growth!.History[0].Breeding!.Amount = 2;
        Require(!service.Validate(invalid) && service.Validate(projected), "altered breeding history rejected without modifying source");
        invalid = persistence.CloneRun(projected);
        invalid.Roster.Single(hero => hero.InstanceId == treeId).Growth.BreedingPoints++;
        Require(!service.Validate(invalid), "unproven breeding points rejected");
        var earlyTree = persistence.CloneRun(app.ActiveRun);
        var flowerSlot = earlyTree.Deployment.IndexOf(flowerId);
        earlyTree.Deployment[flowerSlot] = "";
        for (var step = 1; step <= 3; step++) Settle(earlyTree, step);
        earlyTree.Deployment[flowerSlot] = flowerId;
        Settle(earlyTree, 4);
        Require(earlyTree.Roster.Single(hero => hero.InstanceId == treeId).Growth.AddedAttack == 7,
            "tree can breed before flower; the first shared seed uses three old points");
        var benchTree = persistence.CloneRun(projected);
        benchTree.Deployment[benchTree.Deployment.IndexOf(treeId)] = "";
        var flower = benchTree.Roster.Single(hero => hero.InstanceId == flowerId);
        flower.Growth.ProductionTargetInstanceId = flowerId;
        Settle(benchTree, 5);
        Require(flower.Growth.AddedAttack == 4 && benchTree.Roster.Single(hero => hero.InstanceId == treeId).Growth.BreedingPoints == 4,
            "bench retains breeding but neither amplifies nor increments");
        var frozen = persistence.CloneRun(projected);
        frozen.Action = new() { Source = new($"{frozen.RunId}:breeding:5", 5, 0, "fixture-site", null), Phase = RunActionPhase.Service };
        Require(service.FreezeNonCombat(frozen), "breeding snapshot freezes");
        var detached = persistence.CloneRun(frozen);
        detached.Action!.GrowthSnapshot!.Breeding!.Points++;
        Require(!service.SettleNode(detached) && frozen.Action!.GrowthSnapshot!.Breeding!.Points == 4,
            "clone does not alias frozen state; changed old points reject settlement");
        var late = persistence.CloneRun(projected);
        var treeSlot = late.Deployment.IndexOf(treeId);
        late.Deployment[treeSlot] = "";
        late.Action = new() { Source = new($"{late.RunId}:breeding:5", 5, 0, "fixture-site", null), Phase = RunActionPhase.Service };
        Require(service.FreezeNonCombat(late), "commitment excludes the undeployed tree");
        late.Deployment[treeSlot] = treeId;
        Require(service.SettleNode(late) && late.Roster.Single(hero => hero.InstanceId == treeId).Growth.BreedingPoints == 4,
            "joining after commitment does not retroactively breed or amplify");
        Require(Snapshot(app.ActiveRun) == untouched, "conditional projection tests leave active run untouched");

        var site = app.ActiveRun.Expedition!.Sites.Where(value => value.ServiceOffer is { Choices.IsEmpty: false } && !value.Handled)
            .Select(value => (Site: value, Route: RouteTo(app.ActiveRun.Expedition, value)))
            .Where(value => value.Route is not null).OrderBy(value => value.Route!.Count).First();
        foreach (var cell in site.Route!) Require(app.MoveExpedition(cell), "breeding service follows actual map navigation");
        Require(app.InspectExpeditionSite(site.Site.SiteId), "breeding service preview");
        var offer = app.PendingOffer!;
        var choice = offer.Choices.First(value => app.CheckOfferChoice(value).Succeeded);
        var before = Snapshot(app.ActiveRun); var disk = save.Json;
        save.FailWrites = true;
        Require(!app.ResolveOffer(offer.OfferId, choice.StableId).Succeeded && Snapshot(app.ActiveRun) == before && save.Json == disk,
            "save failure rolls back breeding, seed, cost, stock and action together");
        save.FailWrites = false;
        Require(app.ResolveOffer(offer.OfferId, choice.StableId).Succeeded, "breeding service retries once");
        Require(app.ActiveRun.Roster.Single(hero => hero.InstanceId == treeId).Growth.BreedingPoints == 1,
            "only committed service advances breeding");
        var resumed = new RunApplication(package, save, "flower-contract");
        Require(resumed.ActiveRun is not null && Snapshot(resumed.ActiveRun) == Snapshot(app.ActiveRun),
            "actual save and restore preserve breeding with history");
        Require(!resumed.ResolveOffer(offer.OfferId, choice.StableId).Succeeded, "completed service cannot breed twice");
        GD.Print("BREEDING_PROJECTION_CONTRACT_OK old-points first-tree bench clone ledger atomic-save restore dedupe");

        void Settle(ActiveRunDto run, int sequence)
        {
            run.Action = new() { Source = new($"{run.RunId}:breeding:{sequence}", sequence, 0, "fixture-site", null), Phase = RunActionPhase.Service };
            Require(service.FreezeNonCombat(run) && service.SettleNode(run), "conditional breeding action settles");
        }

        void RejectBreedingConfig(Action<GrowthHeroDefinition> edit)
        {
            var invalidRules = (GrowthRulesDefinition)authored.Duplicate(true);
            edit(invalidRules.Heroes.Single(hero => hero.ContentId == "prototype_u04"));
            var rejected = false;
            try { GrowthRulesCompiler.Compile(invalidRules, package.Content); }
            catch (InvalidOperationException) { rejected = true; }
            Require(rejected, "invalid breeding author values reject before publication");
        }
    }

    private static void VerifyProjection(CompiledGamePackage package, RunApplication app)
    {
        var rules = package.GrowthRules!;
        var persistence = new RunProgressionPersistenceService(package.Content, new MemorySave(), package.Project, rules, package.DefaultProfile);
        var service = new GrowthRunService(package.Content, package.Project, rules, persistence);
        var flowerId = app.ActiveRun!.Roster.Single(hero => hero.ContentId == "prototype_u16").InstanceId;
        var partnerId = app.ActiveRun.Roster.Single(hero => hero.InstanceId != flowerId).InstanceId;
        var initial = Snapshot(app.ActiveRun);
        Check(false, true, true, false, false, false, 0);
        Check(true, false, true, false, false, false, 0);
        Check(true, true, false, false, false, false, 0);
        Check(true, true, true, false, false, false, 4);
        Check(true, true, true, true, false, false, 4);
        Check(true, true, true, false, true, false, 4);
        Check(true, true, true, false, false, true, 0);
        Require(Snapshot(app.ActiveRun) == initial, "isolated projections never mutate formal active run");

        void Check(bool assigned, bool producerDeployed, bool targetDeployed, bool self, bool retargetAfterFreeze, bool unsupported, float expected)
        {
            var run = persistence.CloneRun(app.ActiveRun!);
            var targetId = self ? flowerId : partnerId;
            var flower = run.Roster.Single(hero => hero.InstanceId == flowerId);
            flower.Growth.ProductionMode = GrowthProductionMode.Cultivation;
            flower.Growth.ProductionTargetInstanceId = assigned ? targetId : "";
            if (!producerDeployed) run.Deployment[run.Deployment.IndexOf(flowerId)] = "";
            if (!targetDeployed) run.Deployment[run.Deployment.IndexOf(partnerId)] = "";
            run.Action = new() { Source = new($"{run.RunId}:projection:1", 1, 0, "fixture-site", null), Phase = RunActionPhase.Service };
            Require(service.FreezeNonCombat(run), "projection freezes existing deployed participants");
            var snapshot = run.Action.GrowthSnapshot!;
            if (retargetAfterFreeze) flower.Growth.ProductionTargetInstanceId = flowerId;
            // A hero acquired after commitment must not gain this action's output.
            run.Roster.Add(new() { InstanceId = "new-after-freeze",
                ContentId = rules.Heroes.Keys.First(id => run.Roster.All(hero => hero.ContentId != id)), Growth = new() });
            if (unsupported) snapshot.Assignments[0].Mode = (GrowthProductionMode)99;
            var serialized = JsonSerializer.Deserialize<ActiveRunDto>(Snapshot(run))!;
            var succeeded = service.SettleNode(serialized);
            Require(succeeded != unsupported, "unsupported frozen mode is rejected");
            var target = serialized.Roster.Single(hero => hero.InstanceId == targetId);
            Require(target.Growth.AddedAttack == expected && target.Growth.AddedMaxHealth == expected * 10,
                "frozen cultivation adds the fixed pair together");
            Require(serialized.Roster.Single(hero => hero.InstanceId == "new-after-freeze").Growth.AddedAttack == 0,
                "post-freeze acquisition cannot receive retroactive growth");
            if (expected > 0)
            {
                Require(target.Growth.History.Count == 2 && target.Growth.History.Single(gain => gain.Mode == GrowthProductionMode.Attack).Amount == 4 &&
                    target.Growth.History.Single(gain => gain.Mode == GrowthProductionMode.Vitality).Amount == 40 &&
                    serialized.Growth!.History.Single().Gains.Count == 2, "one settlement records both exact gains");
                Require(!service.SettleNode(serialized) && target.Growth.AddedAttack == 4, "serialized settled action cannot pay twice");
            }
        }
    }

    private static void VerifyServiceTransaction(CompiledGamePackage package, RunApplication app, MemorySave save)
    {
        var run = app.ActiveRun!;
        var flowerId = run.Roster.Single(hero => hero.ContentId == "prototype_u16").InstanceId;
        var targetId = run.Roster.Single(hero => hero.InstanceId != flowerId).InstanceId;
        var before = Snapshot(run);
        Require(!app.SetGrowthAssignment(flowerId, targetId, GrowthProductionMode.Attack).Succeeded && Snapshot(run) == before,
            "flower rejects unsupported legacy percentage assignment");
        save.FailWrites = true;
        Require(!app.SetGrowthAssignment(flowerId, targetId, GrowthProductionMode.Cultivation).Succeeded && Snapshot(run) == before,
            "failed assignment preserves unassigned state");
        save.FailWrites = false;
        Require(app.SetGrowthAssignment(flowerId, targetId, GrowthProductionMode.Cultivation).Succeeded, "explicit assignment saves");
        var site = run.Expedition!.Sites.Where(value => value.ServiceOffer is { Choices.IsEmpty: false } && !value.Handled)
            .Select(value => (Site: value, Route: RouteTo(run.Expedition, value)))
            .Where(value => value.Route is not null).OrderBy(value => value.Route!.Count).First();
        foreach (var cell in site.Route!) Require(app.MoveExpedition(cell), "real navigation reveals each committed step");
        Require(run.Growth!.History.Count == 0 && app.InspectExpeditionSite(site.Site.SiteId), "moving and inspecting cannot produce");
        var offer = app.PendingOffer ?? throw new InvalidOperationException("Service inspection must expose its offer.");
        Require(app.PreviewExpeditionSiteOffer(site.Site.SiteId) is not null && run.Growth.History.Count == 0,
            "repeated service preview has no output");
        var choice = offer.Choices.First(value => app.CheckOfferChoice(value).Succeeded);
        before = Snapshot(run);
        var disk = save.Json;
        save.FailWrites = true;
        Require(!app.ResolveOffer(offer.OfferId, choice.StableId).Succeeded && Snapshot(run) == before && save.Json == disk,
            "failed action save rolls back both attributes, action cost and service reward");
        save.FailWrites = false;
        Require(app.ResolveOffer(offer.OfferId, choice.StableId).Succeeded, "valid service commits complete pair");
        var target = run.Roster.Single(hero => hero.InstanceId == targetId);
        Require(target.Growth.AddedAttack == 4 && target.Growth.AddedMaxHealth == 40 && run.Growth.History.Count == 1,
            "first successful paid service grants exactly one seed");
        var restored = new RunApplication(package, save, "flower-contract");
        Require(restored.ActiveRun is not null && Snapshot(restored.ActiveRun) == Snapshot(run), "real load retains pair and histories");
        var restoredBefore = Snapshot(restored.ActiveRun!);
        Require(!restored.ResolveOffer(offer.OfferId, choice.StableId).Succeeded && Snapshot(restored.ActiveRun!) == restoredBefore,
            "old service receipt cannot produce after restoration");
        Require(restored.SetGrowthAssignment(flowerId, flowerId, GrowthProductionMode.Cultivation).Succeeded &&
            restored.ActiveRun!.Roster.Single(hero => hero.InstanceId == targetId).Growth.AddedAttack == 4,
            "retargeting keeps the old partner's permanent gains");
        // Content fingerprints remain an explicit rejection boundary; rejected data stays intact.
        var altered = save.LoadActiveRun()!;
        altered.ContentFingerprint = new string('0', 64);
        save.SaveActiveRun(altered);
        disk = save.Json;
        var writes = save.Writes;
        var rejected = new RunApplication(package, save, "flower-contract");
        Require(rejected.ActiveRun is null && rejected.ActiveRunLoadDiagnostic is not null && save.Json == disk && save.Writes == writes,
            "mismatched old fingerprint refuses load and cannot overwrite original save");
    }

    private static void VerifyBattleOutcomes(CompiledGamePackage package)
    {
        var (app, save) = NewFlowerRun(package);
        var run = app.ActiveRun!;
        var flowerId = run.Roster.Single(hero => hero.ContentId == "prototype_u16").InstanceId;
        var targetId = run.Roster.Single(hero => hero.InstanceId != flowerId).InstanceId;
        Require(app.SetGrowthAssignment(flowerId, targetId, GrowthProductionMode.Cultivation).Succeeded,
            "battle fixture saves explicit flower assignment");
        var definition = (CompiledExpedition)package.Project.Campaign.Progression;
        var reachable = run.Expedition!.Sites.Where(site => site.EncounterProfileId.Length > 0 &&
                definition.Encounters[site.EncounterProfileId].Encounter.NodeType == TowerNodeType.Combat)
            .Select(site => (Site: site, Route: RouteTo(run.Expedition, site)))
            .Where(site => site.Route is not null).OrderBy(site => site.Route!.Count).First();
        foreach (var cell in reachable.Route!) Require(app.MoveExpedition(cell), "walk through real map to ordinary enemy");
        var encounter = app.PreviewExpeditionEncounter(reachable.Site.SiteId);
        var actionsBefore = run.Expedition.RemainingActions;
        var healthBefore = run.CurrentRunHealth;
        Require(app.TryBeginGrowthBattle(encounter).Succeeded && run.Action!.GrowthSnapshot!.Assignments.Count == 1,
            "ordinary battle freezes the legal actual formation and one cultivation entitlement");
        // Reuse the existing controlled outcome fixture: it initializes the formal
        // simulator/configuration and creates its signed relic/action receipt. It
        // does not certify natural win/loss balance, or fabricate a Run settlement.
        var defeat = RunHealthFixture.Result(app.BuildBattleConfig(encounter), BattleOutcome.PlayerDefeat);
        Require(defeat.Identity!.OriginKey == run.Action!.Source.ActionId, "defeat belongs to the committed action");
        app = new RunApplication(package, save, "flower-contract");
        run = app.ActiveRun!;
        var frozen = Snapshot(run);
        save.FailWrites = true;
        Require(!app.ResolveBattle(defeat, encounter).Accepted && Snapshot(run) == frozen,
            "failed defeat publication preserves frozen action and both ungranted attributes");
        save.FailWrites = false;
        Require(app.ResolveBattle(defeat, encounter).Accepted && app.ActiveRun is not null &&
            run.Expedition!.RemainingActions == actionsBefore - 1 && run.CurrentRunHealth < healthBefore &&
            run.CurrentRunHealth > 0 && run.Action is null &&
            !run.Expedition.Sites.Single(site => site.SiteId == reachable.Site.SiteId).Cleared,
            "ordinary paid defeat continues after its genuine action and health costs");
        AssertPair(run, targetId, 4, 40, 1);
        var settled = Snapshot(run);
        Require(!app.ResolveBattle(defeat, encounter).Accepted && Snapshot(run) == settled,
            "restored defeat receipt cannot grant another seed");

        for (var stage = 0; stage < definition.Stages.Length; stage++)
        {
            var boss = app.PreviewExpeditionBoss();
            var countBefore = run.Growth!.History.Count;
            var attackBefore = run.Roster.Single(hero => hero.InstanceId == targetId).Growth.AddedAttack;
            var healthGainBefore = run.Roster.Single(hero => hero.InstanceId == targetId).Growth.AddedMaxHealth;
            var beforeBegin = Snapshot(run);
            var began = app.TryBeginGrowthBattle(boss);
            GD.Print($"FLOWER_BOSS_BEGIN_DIAGNOSTIC stage={stage + 1} profile={boss.ExpeditionProfileId} succeeded={began.Succeeded} message={began.Message}");
            if (!began.Succeeded)
            {
                Require(stage == definition.Stages.Length - 1 && began.Message == "战斗配置无法启动：敌方部署区无法容纳本场大型单位与同伴。" &&
                    Snapshot(run) == beforeBegin, "only the known final-Boss spatial blockage permits test-only space substitution");
                GD.Print("FLOWER_FINAL_AUTHORED_BOSS_BLOCKED: production Boss remains unverified; final transaction check uses an isolated test-only three-unit composition from the legal first Boss.");
                (app, save) = FinalSpaceFixture(package, run);
                run = app.ActiveRun!;
                boss = app.PreviewExpeditionBoss();
                began = app.TryBeginGrowthBattle(boss);
                GD.Print($"FLOWER_BOSS_BEGIN_DIAGNOSTIC stage={stage + 1} profile={boss.ExpeditionProfileId} fixture=legal-first-boss-space succeeded={began.Succeeded} message={began.Message}");
            }
            Require(began.Succeeded && run.Action!.GrowthSnapshot!.Assignments.Count == 1,
                $"stage {stage + 1} Boss must commit one legal frozen cultivation entitlement: {began.Message}");
            var victory = RunHealthFixture.Result(app.BuildBattleConfig(boss), BattleOutcome.PlayerVictory);
            Require(app.ResolveBattle(victory, boss).Accepted, "controlled signed Boss victory commits through real Run result entry");
            if (stage < definition.Stages.Length - 1)
            {
                AssertPair(run, targetId, attackBefore + 4, healthGainBefore + 40, countBefore + 1);
                while (app.PendingOffer is { } reward)
                    Require(app.ResolveOffer(reward.OfferId, null).Succeeded, "finish authored stage reward if present");
                Require(run.Expedition!.StageIndex == stage + 1, "successful stage boundary advances normally");
            }
            else
            {
                Require(app.ActiveRun is null && save.Json is null && run.TerminalVictory,
                    "final Boss completes and clears the saved active run");
                AssertPair(run, targetId, attackBefore, healthGainBefore, countBefore);
            }
        }
        GD.Print("FLOWER_CULTIVATION_BATTLE_BOUNDARIES_OK paid-defeat +4/+40 nonfinal-boss +4/+40 final-boss +0/+0");
    }

    private static (RunApplication, MemorySave) FinalSpaceFixture(CompiledGamePackage package, ActiveRunDto formalRun)
    {
        // Test-only compilation projection, following the diagnostic-package pattern.
        // No authoring resources, formal run, or original save are changed. Preserve
        // the final profile's identity, multipliers, reward and outcome policy; only
        // substitute the three-unit spacing already legal in the first authored Boss.
        var expedition = (CompiledExpedition)package.Project.Campaign.Progression;
        var first = expedition.Encounters[expedition.Stages[0].BossProfileId];
        var final = expedition.Encounters[expedition.Stages[^1].BossProfileId];
        var composition = final.Composition with { EnemyIds = first.Composition.EnemyIds, Cells = first.Composition.Cells };
        var fixtureEncounter = final with
        {
            Composition = composition,
            Encounter = final.Encounter with { Compositions = [composition], BaseEnemyCount = composition.EnemyIds.Length }
        };
        var project = package.Project with
        {
            Campaign = package.Project.Campaign with
            { Progression = expedition with { Encounters = expedition.Encounters.SetItem(final.StableId, fixtureEncounter) } }
        };
        var source = package.DefaultProfile;
        const string fixtureProfileId = "flower-final-space-test";
        var profile = source with
        {
            ProfileId = fixtureProfileId, Project = project,
            ContentFingerprint = RunProfileContentFingerprint.Compute(fixtureProfileId, source.CompatibilityVersion,
                source.ProgressionProtocolVersion, source.RewardProtocolVersion, source.IdentityProtocolVersion,
                project, source.GrowthRules, package.Content.Graph)
        };
        var fixturePackage = new CompiledGamePackage(package.Content, package.ProfilesById.Add(fixtureProfileId, profile),
            fixtureProfileId, package.PublicationVersion) { AuthoringOwner = package.AuthoringOwner };
        // Seed this isolated memory fixture from the formally reached final preparation
        // state. This is explicit test setup, not a production compatibility migration.
        var fixtureRun = JsonSerializer.Deserialize<ActiveRunDto>(Snapshot(formalRun))!;
        fixtureRun.ProfileId = fixtureProfileId;
        fixtureRun.ContentFingerprint = profile.ContentFingerprint;
        var fixtureSave = new MemorySave();
        Require(fixtureSave.SaveActiveRun(fixtureRun), "test-only final preparation is saved under its distinct matching profile");
        var fixtureApp = new RunApplication(fixturePackage, fixtureSave, fixtureProfileId);
        Require(fixtureApp.ActiveRun is not null && fixtureApp.ActiveRun.Expedition!.StageIndex == expedition.Stages.Length - 1,
            "test-only final preparation passes ordinary real load validation");
        return (fixtureApp, fixtureSave);
    }

    private static void AssertPair(ActiveRunDto run, string targetId, float attack, float health, int settlements)
    {
        var target = run.Roster.Single(hero => hero.InstanceId == targetId);
        Require(target.Growth.AddedAttack == attack && target.Growth.AddedMaxHealth == health &&
            run.Growth!.History.Count == settlements && target.Growth.History.Count == settlements * 2,
            "battle settlement pair and both gain histories match the exact nonterminal action count");
    }

    private static List<int>? RouteTo(ExpeditionStateDto map, ExpeditionSiteDto site)
    {
        var targets = ExpeditionNavigation.Neighbors(map.Size, site.CellIndex).Where(cell => !ExpeditionNavigation.IsBlocked(map, cell)).ToHashSet();
        if (!ExpeditionNavigation.IsBlocked(map, site.CellIndex)) targets.Add(site.CellIndex);
        var previous = new Dictionary<int, int> { [map.PositionIndex] = map.PositionIndex };
        var queue = new Queue<int>(); queue.Enqueue(map.PositionIndex);
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            if (targets.Contains(cell))
            {
                var route = new List<int>();
                for (; cell != map.PositionIndex; cell = previous[cell]) route.Add(cell);
                route.Reverse(); return route;
            }
            foreach (var next in ExpeditionNavigation.Neighbors(map.Size, cell))
                if (!previous.ContainsKey(next) && !ExpeditionNavigation.IsBlocked(map, next))
                { previous[next] = cell; queue.Enqueue(next); }
        }
        return null;
    }
    private static string Snapshot(ActiveRunDto run) => JsonSerializer.Serialize(run);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class MemorySave : IRunSaveService
    {
        public string? Json { get; private set; }
        public bool FailWrites { get; set; }
        public int Writes { get; private set; }
        private MetaProgressDto _meta = new();
        public ActiveRunDto? LoadActiveRun() => Json is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(Json);
        public MetaProgressDto LoadMeta() => JsonSerializer.Deserialize<MetaProgressDto>(JsonSerializer.Serialize(_meta))!;
        public SettingsDto LoadSettings() => new();
        public bool SaveActiveRun(ActiveRunDto value) { if (FailWrites) return false; Json = Snapshot(value); Writes++; return true; }
        public bool SaveMeta(MetaProgressDto value) { _meta = value; return true; }
        public bool SaveSettings(SettingsDto value) => true;
        public void DeleteActiveRun() => Json = null;
    }
}
