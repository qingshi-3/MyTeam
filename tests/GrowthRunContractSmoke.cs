using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using TowerAutobattler.Growth;
using TowerAutobattler.Domain;
using TowerAutobattler.Battle;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

public partial class GrowthRunContractSmoke : Node
{
    public override async void _Ready()
    {
        var code = 0;
        try
        {
            var published = await GrowthContentPackage.CreateReadyAsync(this);
            var package = published.Package ?? throw new InvalidOperationException(string.Join(';', published.Report.CoreErrors));
            var rules = GrowthContentPackage.LoadRules(package.Content);
            VerifyOptionalAscensionBenefits(package, rules);
            VerifyCommandsAndRollback(package, rules);
            VerifyDiscoveryCapacityInvariant(package, rules);
            VerifyFreezeSettlementAndClone(package, rules);
            VerifyEverySpellCostAndConsumedRetry(package, rules);
            VerifyPermanentBattleGain(package, rules);
            VerifyTerminalBattleDoesNotGrantReusableGrowth(package, rules);
            VerifyV7Migration(package, rules);
            VerifyV7PendingNonCombatMigration(package, rules);
            GD.Print("GROWTH_RUN_CONTRACT_OK commands rollback freeze retry settlement clone migration discovery materials");
        }
        catch (Exception error) { code = 1; GD.PrintErr("GROWTH_RUN_CONTRACT_FAILED: " + error); }
        GetTree().Quit(code);
    }

    private static void VerifyOptionalAscensionBenefits(TowerAutobattler.Composition.CompiledGamePackage package, CompiledGrowthRules rules)
    {
        Require(rules.Heroes.Values.Count(hero => hero.AscensionBenefit is null) == 19,
            "unplanned benefits remain explicitly absent");
        foreach (var hero in rules.Heroes.Values.Where(hero => hero.AscensionBenefit is null))
            Require(ReferenceEquals(hero.ResolveBattleLoadout(true), hero.BaseLoadout),
                "blank ascension preserves authored base abilities: " + hero.ContentId);

        // A producer's authored growth loadout must also survive removing its benefit.
        var authored = (GrowthRulesDefinition)GD.Load<GrowthRulesDefinition>(GrowthContentPackage.RulesPath).Duplicate(true);
        var authoringHero = authored.Heroes.Single(hero => hero.ContentId == "hero_mx01");
        authoringHero.AscensionBenefit = null;
        authoringHero.AscensionDisplay = null;
        var withoutDisplayOrBenefit = GrowthRulesCompiler.Compile(authored, package.Content).Heroes[authoringHero.ContentId];
        Require(ReferenceEquals(withoutDisplayOrBenefit.ResolveBattleLoadout(true), withoutDisplayOrBenefit.BaseLoadout),
            "eligibility, display and battle benefits are independent");

        var save = new MemorySave();
        var app = NewRun(package, rules, save, "hero_mx10");
        var run = app.ActiveRun!;
        var heroInstance = run.Roster.Single(hero => hero.ContentId == "hero_mx10");
        run.Growth!.Materials = rules.AscensionCost;
        var before = Snapshot(run);
        save.FailWrites = true;
        Require(!app.AscendHero(heroInstance.InstanceId).Succeeded && Snapshot(app.ActiveRun!) == before,
            "blank benefit ascension still rolls back cost, identity and discovery on save failure");
        save.FailWrites = false;
        Require(app.AscendHero(heroInstance.InstanceId).Succeeded, "blank benefit permits ascension");
        var saved = new RunApplication(package.Content, save, package.Project, rules).ActiveRun;
        Require(saved is not null, "blank ascension save passes real load validation");
        var ascended = saved!.Roster.Single(hero => hero.InstanceId == heroInstance.InstanceId);
        Require(ascended.Growth.AscensionId == rules.Heroes[ascended.ContentId].AscensionId &&
            saved.Growth!.Materials == 0 && saved.Growth.PendingDiscovery?.CandidateIds.Count == 3,
            "saved blank ascension retains identity and frozen reward");
        Require(ReferenceEquals(RunBattlePreparationAdapter.ProjectGrowth(ascended, rules)!.LoadoutOverride,
            rules.Heroes[ascended.ContentId].BaseLoadout), "reloaded battle projection retains original abilities");

        string[] retired = ["item_war_drum", "item_field_rations", "item_soul_lantern", "item_last_banner", "item_aegis_standard", "item_clockwork_seed"];
        foreach (var id in retired)
            Require(!package.Project.Campaign.ItemRewardPool.ContentIds.Contains(id) &&
                !package.Project.Campaign.ShopPool.ContentIds.Contains(id) && package.Content.TryGet(id, out _),
                "retired supply excluded while old-save content remains resolvable: " + id);
    }

    private static void VerifyCommandsAndRollback(TowerAutobattler.Composition.CompiledGamePackage package, CompiledGrowthRules rules)
    {
        var save = new MemorySave();
        var app = NewRun(package, rules, save);
        var run = app.ActiveRun!;
        var producer = run.Roster.First(hero => rules.Heroes.TryGetValue(hero.ContentId, out var value) && !value.ProductionModes.IsEmpty);
        var target = run.Roster.First();
        var mode = rules.Heroes[producer.ContentId].ProductionModes.First();
        var targetId = mode == GrowthProductionMode.Research ? producer.InstanceId : target.InstanceId;
        var before = Snapshot(run);
        save.FailWrites = true;
        Require(!app.SetGrowthAssignment(producer.InstanceId, targetId, mode).Succeeded && Snapshot(run) == before,
            "failed assignment save preserves authoritative run");
        save.FailWrites = false;
        Require(app.SetGrowthAssignment(producer.InstanceId, targetId, mode).Succeeded, "assignment commits");

        var spell = rules.Spells.Values.First();
        run.Growth!.Research = spell.ResearchCost;
        before = Snapshot(run);
        save.FailWrites = true;
        Require(!app.CraftGrowthSpell(spell.StableId).Succeeded && Snapshot(run) == before,
            "failed craft saves neither cost nor inventory");
        save.FailWrites = false;
        Require(app.CraftGrowthSpell(spell.StableId).Succeeded && run.Growth.SpellInventory[spell.StableId] == 1,
            "craft pays research once");

        var battleNode = app.CurrentOptions().First(option => option.Type is TowerNodeType.Combat or TowerNodeType.Elite);
        Require(SelectTestNode(app, battleNode.Type), "select battle before deployment ascension");
        var deploymentEncounter = app.CurrentEncounter();
        run.Growth.Materials = Math.Max(0, rules.AscensionCost - 1);
        Require(!app.AscendHero(producer.InstanceId).Succeeded && string.IsNullOrEmpty(producer.Growth.AscensionId),
            "insufficient mixed materials reject without ascension");
        run.Growth.Materials = rules.AscensionCost;
        var ascendBefore = Snapshot(run);
        save.FailWrites = true;
        Require(!app.AscendHero(producer.InstanceId).Succeeded && Snapshot(run) == ascendBefore,
            "failed ascension save rolls back cost, mark and discovery");
        save.FailWrites = false;
        var ascended = app.AscendHero(producer.InstanceId);
        Require(ascended.Succeeded && run.Growth.PendingDiscovery?.CandidateIds.Count == 3 &&
            run.Growth.Materials == 0 && !string.IsNullOrEmpty(run.Roster.Single(hero => hero.InstanceId == producer.InstanceId).Growth.AscensionId),
            "ascension atomically creates fixed three-choice discovery");
        var candidate = run.Growth.PendingDiscovery!.CandidateIds[0];
        Require(!app.TryBeginGrowthBattle(deploymentEncounter).Succeeded,
            "paid discovery blocks starting the selected battle until claimed");
        Require(!app.SelectNode(app.CurrentOptions().FirstOrDefault()?.Type ?? TowerNodeType.Combat),
            "pending discovery blocks progression");
        Require(app.ChooseGrowthHero(candidate).Succeeded && run.Growth.PendingDiscovery is null &&
            run.Roster.Count(hero => hero.ContentId == candidate) == 1, "discovery claim adds exactly one hero");
        Require(app.TryBeginGrowthBattle(deploymentEncounter).Succeeded,
            "claiming deployment ascension discovery unblocks the already selected battle");
    }

    private static void VerifyFreezeSettlementAndClone(TowerAutobattler.Composition.CompiledGamePackage package, CompiledGrowthRules rules)
    {
        var save = new MemorySave();
        var app = NewRun(package, rules, save);
        var run = app.ActiveRun!;
        var persistence = new RunProgressionPersistenceService(package.Content, save, package.Project, rules);
        var growth = new GrowthRunService(package.Content, package.Project, rules, persistence);
        var producer = run.Roster.First(hero => rules.Heroes.TryGetValue(hero.ContentId, out var value) && !value.ProductionModes.IsEmpty);
        var producerRule = rules.Heroes[producer.ContentId];
        var mode = producerRule.ProductionModes.Contains(GrowthProductionMode.Research)
            ? GrowthProductionMode.Research : producerRule.ProductionModes[0];
        producer.Growth.ProductionMode = mode;
        producer.Growth.ProductionTargetInstanceId = producer.InstanceId;
        Require(growth.FreezeNonCombat(run), "noncombat node freezes production eligibility");
        var clone = persistence.CloneRun(run);
        clone.Growth!.PendingNode!.ParticipantInstanceIds.Clear();
        clone.Roster[0].Growth.History.Add(new GrowthGainDto());
        Require(run.Growth!.PendingNode!.ParticipantInstanceIds.Count > 0 && run.Roster[0].Growth.History.Count == 0,
            "growth run and hero histories are deeply cloned");
        Require(growth.SettleNode(run) && run.Growth.Materials == rules.MaterialsPerNode &&
            run.Growth.History.Count == 1 && !growth.SettleNode(run), "node settles exactly once and duplicate is rejected");

        var spell = rules.Spells.Values.First();
        run.Growth.SpellInventory[spell.StableId] = 1;
        run.Growth.EquippedSpellId = spell.StableId;
        run.Growth.SpellTargetInstanceId = run.Deployment.First(id => !string.IsNullOrEmpty(id));
        run.Growth.PendingNode = null;
        Require(SelectTestNode(app, TowerNodeType.Combat), "select explicit opponent for battle-start retry fixture");
        var encounter = app.CurrentEncounter();
        var legalDeployment = run.Deployment.ToList();
        var deployedId = run.Deployment.First(id => !string.IsNullOrEmpty(id));
        var duplicateSlot = run.Deployment.FindIndex(id => string.IsNullOrEmpty(id));
        Require(duplicateSlot >= 0, "fixture has an empty slot for illegal formation probe");
        run.Deployment[duplicateSlot] = deployedId;
        var invalidBefore = Snapshot(run);
        Require(!app.TryBeginGrowthBattle(encounter).Succeeded && Snapshot(run) == invalidBefore &&
            run.Growth.SpellInventory[spell.StableId] == 1 && run.Growth.PendingNode is null,
            "invalid formation cannot consume the spell or publish a started snapshot");
        run.Deployment = legalDeployment;
        var before = Snapshot(run);
        save.FailWrites = true;
        Require(!app.TryBeginGrowthBattle(encounter).Succeeded && Snapshot(run) == before,
            "failed battle-start save does not consume spell or publish snapshot");
        save.FailWrites = false;
        Require(app.TryBeginGrowthBattle(encounter).Succeeded && run.Growth.SpellInventory[spell.StableId] == 0 &&
            run.Growth.PendingNode is { IsBattle: true, ConsumedSpellId: { Length: > 0 } } &&
            string.IsNullOrEmpty(run.Growth.EquippedSpellId) && string.IsNullOrEmpty(run.Growth.SpellTargetInstanceId),
            "battle start consumes and freezes the one-shot preset atomically");
        var frozen = Snapshot(run);
        Require(app.TryBeginGrowthBattle(encounter).Succeeded && Snapshot(run) == frozen,
            "reloaded-style start retry reuses frozen consumed preset");
        Require(!app.MoveDeploymentUnit(run.Deployment.First(id => !string.IsNullOrEmpty(id)), 1),
            "committed battle snapshot persistently locks formation edits");
    }

    private static void VerifyDiscoveryCapacityInvariant(
        TowerAutobattler.Composition.CompiledGamePackage package, CompiledGrowthRules rules)
    {
        var fullSave = new MemorySave();
        var fullApp = NewRun(package, rules, fullSave);
        var fullRun = fullApp.ActiveRun!;
        FillRoster(fullApp, rules, fullRun.CurrentPopulation + package.Project.RunRules.ReserveCapacity);
        var fullProducer = fullRun.Roster.First(hero => rules.Heroes.TryGetValue(hero.ContentId, out var value) &&
            !value.ProductionModes.IsEmpty && string.IsNullOrEmpty(hero.Growth.AscensionId));
        Require(SelectTestNode(fullApp, fullApp.CurrentOptions().First(option => option.Type is TowerNodeType.Combat or TowerNodeType.Elite).Type),
            "select battle for full-roster ascension rejection");
        fullRun.Growth!.Materials = rules.AscensionCost;
        var fullBefore = Snapshot(fullRun);
        Require(!fullApp.AscendHero(fullProducer.InstanceId).Succeeded && Snapshot(fullRun) == fullBefore,
            "full roster rejects ascension without cost, mark or pending discovery");

        var lastSlotSave = new MemorySave();
        var lastSlotApp = NewRun(package, rules, lastSlotSave);
        var lastSlotRun = lastSlotApp.ActiveRun!;
        var capacity = lastSlotRun.CurrentPopulation + package.Project.RunRules.ReserveCapacity;
        FillRoster(lastSlotApp, rules, capacity - 1);
        var lastSlotProducer = lastSlotRun.Roster.First(hero => rules.Heroes.TryGetValue(hero.ContentId, out var value) &&
            !value.ProductionModes.IsEmpty && string.IsNullOrEmpty(hero.Growth.AscensionId));
        Require(SelectTestNode(lastSlotApp, lastSlotApp.CurrentOptions().First(option => option.Type is TowerNodeType.Combat or TowerNodeType.Elite).Type),
            "select battle for last-slot discovery");
        lastSlotRun.Growth!.Materials = rules.AscensionCost;
        Require(lastSlotApp.AscendHero(lastSlotProducer.InstanceId).Succeeded,
            "one remaining roster slot permits ascension");
        var lastSlotCandidate = lastSlotRun.Growth.PendingDiscovery!.CandidateIds[0];
        Require(lastSlotApp.ChooseGrowthHero(lastSlotCandidate).Succeeded &&
            lastSlotRun.Roster.Count == capacity && lastSlotRun.Growth.PendingDiscovery is null,
            "one remaining roster slot accepts the paid discovery hero");

        var invalidSave = new MemorySave();
        var invalidApp = NewRun(package, rules, invalidSave);
        var invalidRun = invalidApp.ActiveRun!;
        FillRoster(invalidApp, rules, invalidRun.CurrentPopulation + package.Project.RunRules.ReserveCapacity - 1);
        var invalidProducer = invalidRun.Roster.First(hero => rules.Heroes.TryGetValue(hero.ContentId, out var value) &&
            !value.ProductionModes.IsEmpty && string.IsNullOrEmpty(hero.Growth.AscensionId));
        Require(SelectTestNode(invalidApp, invalidApp.CurrentOptions().First(option => option.Type is TowerNodeType.Combat or TowerNodeType.Elite).Type),
            "select battle for invalid persisted discovery");
        invalidRun.Growth!.Materials = rules.AscensionCost;
        Require(invalidApp.AscendHero(invalidProducer.InstanceId).Succeeded,
            "create valid pending discovery before capacity corruption");
        var pendingIds = invalidRun.Growth.PendingDiscovery!.CandidateIds.ToHashSet(StringComparer.Ordinal);
        var fillerId = rules.Heroes.Keys.First(id => !pendingIds.Contains(id) && invalidRun.Roster.All(hero => hero.ContentId != id));
        invalidRun.Roster.Add(new RosterHeroInstanceDto
        {
            InstanceId = "invalid-full-discovery-filler",
            ContentId = fillerId,
            Growth = new HeroGrowthDto()
        });
        invalidSave.Json = Snapshot(invalidRun);
        var stored = invalidSave.Json;
        var rejected = new RunApplication(package.Content, invalidSave, package.Project, rules);
        Require(rejected.ActiveRun is null &&
            rejected.ActiveRunLoadDiagnostic?.Kind == ActiveRunLoadFailureKind.ValidationRejected &&
            invalidSave.Json == stored,
            "full roster with pending discovery is validation-rejected without altering the stored run");
    }

    private static void VerifyEverySpellCostAndConsumedRetry(
        TowerAutobattler.Composition.CompiledGamePackage package, CompiledGrowthRules rules)
    {
        foreach (var spell in rules.Spells.Values)
        {
            var save = new MemorySave();
            var app = NewRun(package, rules, save);
            var run = app.ActiveRun!;
            run.Growth!.Research = spell.ResearchCost - 1;
            var before = Snapshot(run);
            Require(!app.CraftGrowthSpell(spell.StableId).Succeeded && Snapshot(run) == before,
                $"{spell.StableId}: insufficient research must preserve inventory and research");
            run.Growth.Research = spell.ResearchCost;
            Require(app.CraftGrowthSpell(spell.StableId).Succeeded && run.Growth.Research == 0 &&
                run.Growth.SpellInventory.GetValueOrDefault(spell.StableId) == 1,
                $"{spell.StableId}: crafting must use its configured cost exactly once");
            var targetId = run.Deployment.First(id => !string.IsNullOrEmpty(id));
            Require(app.EquipGrowthSpell(spell.StableId, targetId).Succeeded &&
                run.Growth.SpellInventory.GetValueOrDefault(spell.StableId) == 1,
                $"{spell.StableId}: preset selection cannot consume inventory");
            var battleNode = app.CurrentOptions().First(option => option.Type is TowerNodeType.Combat or TowerNodeType.Elite);
            Require(SelectTestNode(app, battleNode.Type), $"{spell.StableId}: select battle for consumed retry");
            var encounter = app.CurrentEncounter();
            before = Snapshot(run);
            save.FailWrites = true;
            Require(!app.TryBeginGrowthBattle(encounter).Succeeded && Snapshot(run) == before,
                $"{spell.StableId}: a failed battle-start save cannot consume the preset");
            save.FailWrites = false;
            Require(app.TryBeginGrowthBattle(encounter).Succeeded &&
                run.Growth.SpellInventory.GetValueOrDefault(spell.StableId) == 0 &&
                run.Growth.PendingNode is { } started && started.ConsumedSpellId == spell.StableId &&
                started.SpellTargetInstanceId == targetId,
                $"{spell.StableId}: start must freeze the consumed spell and its target");
            var frozen = Snapshot(run);
            Require(app.TryBeginGrowthBattle(encounter).Succeeded && Snapshot(run) == frozen,
                $"{spell.StableId}: retry must not spend a second inventory item");
            var restored = new RunApplication(package.Content, save, package.Project, rules);
            Require(restored.ActiveRun is { } loaded && Snapshot(loaded) == frozen &&
                restored.TryBeginGrowthBattle(restored.CurrentEncounter()).Succeeded && Snapshot(restored.ActiveRun!) == frozen,
                $"{spell.StableId}: save reload must retain the consumed spell and support a free retry");
        }
    }

    private static void FillRoster(RunApplication app, CompiledGrowthRules rules, int targetCount)
    {
        var run = app.ActiveRun!;
        var discoveryPool = (run.FloorIndex < app.Project.Campaign.FloorsPerRegion
            ? rules.FirstDiscoveryPool : rules.AdvancedDiscoveryPool).ToHashSet(StringComparer.Ordinal);
        foreach (var contentId in rules.Heroes.Keys.Where(id => !discoveryPool.Contains(id)).OrderBy(id => id, StringComparer.Ordinal))
        {
            if (run.Roster.Count >= targetCount) break;
            if (run.Roster.All(hero => hero.ContentId != contentId)) Require(app.Recruit(contentId), "fill roster with " + contentId);
        }
        Require(run.Roster.Count == targetCount, $"fixture could not fill roster to {targetCount}");
    }

    private static void VerifyPermanentBattleGain(TowerAutobattler.Composition.CompiledGamePackage package, CompiledGrowthRules rules)
    {
        const string heroId = "hero_mx01";
        var save = new MemorySave();
        var app = NewRun(package, rules, save, heroId);
        EnsureFourFrostRoster(app);
        Require(SelectTestNode(app, TowerNodeType.Combat), "select permanent-growth battle");
        var encounter = app.CurrentEncounter();
        Require(app.TryBeginGrowthBattle(encounter).Succeeded, "freeze permanent-growth battle");
        using var battle = new BattleSimulation(app.BuildBattleConfig(encounter));
        var result = battle.RunToEnd();
        Require(!result.PermanentGains.IsDefaultOrEmpty && result.PermanentGains.Any(gain =>
            gain.SourceInstanceId == "player-hero" && gain.TargetInstanceId == "player-hero" &&
            gain.Attribute == TowerAutobattler.Attributes.CombatAttribute.MaxHealth && gain.Amount == 16),
            "real battle emits only the authored MX01 permanent gain receipt");
        var before = Snapshot(app.ActiveRun!);
        save.FailWrites = true;
        Require(!app.ResolveBattle(result, encounter).Accepted && Snapshot(app.ActiveRun!) == before,
            "failed battle settlement publishes neither permanent gain nor ordinary node rewards");
        save.FailWrites = false;
        var committed = app.ResolveBattle(result, encounter);
        var committedRun = app.ActiveRun!;
        Require(committed.Accepted &&
            committedRun.Roster.Single(hero => hero.InstanceId == "player-hero").Growth.AddedMaxHealth == 16 &&
            committedRun.Growth!.History.Single().Gains.Count(gain => gain.Source == "battle") == 1,
            "retry commits the permanent gain exactly once");
        Require(!app.ResolveBattle(result, encounter).Accepted &&
            committedRun.Roster.Single(hero => hero.InstanceId == "player-hero").Growth.AddedMaxHealth == 16,
            "duplicate battle receipt cannot duplicate permanent gain");
        while (app.PendingOffer is { } offer)
        {
            var choice = offer.AllowSkip ? null : offer.Choices.First(candidate => app.CheckOfferChoice(candidate).Succeeded).StableId;
            Require(app.ResolveOffer(offer.OfferId, choice).Succeeded, "clear each independent battle reward for next projection");
        }
        Require(SelectNextAuthoredBattle(app), "select next authored battle after permanent gain");
        var next = app.BuildBattleConfig(app.CurrentEncounter());
        Require(next.Spawns.Single(spawn => spawn.InstanceId == "player-hero").Unit.MaxHealth >= 416,
            "next battle projection includes settled permanent maximum health");
    }

    private static void VerifyV7Migration(TowerAutobattler.Composition.CompiledGamePackage package, CompiledGrowthRules rules)
    {
        var sourceSave = new MemorySave();
        var source = NewRun(package, rules, sourceSave).ActiveRun!;
        source.Version = 7;
        source.Growth = null;
        foreach (var hero in source.Roster) hero.Growth = null!;
        sourceSave.Json = Snapshot(source);
        var restored = new RunApplication(package.Content, sourceSave, package.Project, rules);
        Require(restored.ActiveRun is { Version: ActiveRunFormationSchema.CurrentVersion, Growth: not null } && restored.ActiveRun.Roster.All(hero => hero.Growth is not null) &&
            restored.ActiveRun.Growth.History.Count == 0 && restored.ActiveRun.Growth.Materials == 0,
            "v7 migration adds only deterministic empty growth state and does not invent production history");
    }

    private static void VerifyV7PendingNonCombatMigration(
        TowerAutobattler.Composition.CompiledGamePackage package, CompiledGrowthRules rules)
    {
        var seedSave = new MemorySave();
        var seedApp = NewRun(package, rules, seedSave);
        var source = seedApp.ActiveRun!;
        var persistence = new RunProgressionPersistenceService(package.Content, seedSave, package.Project, rules);
        var decisions = new RunDecisionService(package.Content, package.Project, persistence,
            new GrowthRunService(package.Content, package.Project, rules, persistence));
        source.SelectedNode = TowerNodeType.Rest;
        source.PendingNode = true;
        source.PendingOffer = decisions.CreateOffer(source, RunOfferKind.Rest);
        source.Version = 7;
        source.Growth = null;
        foreach (var hero in source.Roster) hero.Growth = null!;
        seedSave.Json = Snapshot(source);

        var migrated = new RunApplication(package.Content, seedSave, package.Project, rules);
        var run = migrated.ActiveRun!;
        Require(run.Growth?.PendingNode is { IsBattle: false, ParticipantInstanceIds: { Count: 0 }, Assignments: { Count: 0 } },
            "v7 pending noncombat migrates an explicit empty production snapshot");
        var offer = migrated.PendingOffer!;
        var choice = offer.AllowSkip ? null : offer.Choices.First(candidate => migrated.CheckOfferChoice(candidate).Succeeded).StableId;
        var before = Snapshot(run);
        seedSave.FailWrites = true;
        Require(!migrated.ResolveOffer(offer.OfferId, choice).Succeeded && Snapshot(run) == before,
            "failed migrated offer save preserves offer, empty snapshot and fixed material entitlement");
        seedSave.FailWrites = false;
        Require(migrated.ResolveOffer(offer.OfferId, choice).Succeeded && run.Growth!.PendingNode is null &&
            run.Growth.Materials == rules.MaterialsPerNode && run.Growth.History.Count == 1 && run.Growth.History[0].Gains.Count == 0,
            "migrated offer grants one fixed material settlement without invented production");
        var restored = new RunApplication(package.Content, seedSave, package.Project, rules);
        var restoredRun = restored.ActiveRun!;
        Require(restoredRun.Growth!.Materials == rules.MaterialsPerNode &&
            restoredRun.Growth.History.Count == 1 && restoredRun.PendingOffer is null,
            "reloading completed migrated node cannot settle it again");
    }

    private static void VerifyTerminalBattleDoesNotGrantReusableGrowth(
        TowerAutobattler.Composition.CompiledGamePackage package, CompiledGrowthRules rules)
    {
        var save = new MemorySave();
        var app = NewRun(package, rules, save, "hero_mx01");
        app.ActiveRun!.FloorIndex = package.Project.Campaign.TotalFloors - 1;
        Require(SelectTestNode(app, TowerNodeType.Boss), "select final boss");
        var encounter = app.CurrentEncounter();
        Require(app.TryBeginGrowthBattle(encounter).Succeeded, "freeze final boss growth receipt");
        using var battle = new BattleSimulation(app.BuildBattleConfig(encounter));
        var result = battle.RunToEnd();
        Require(result.Outcome is BattleOutcome.PlayerVictory or BattleOutcome.PlayerDefeat or BattleOutcome.Timeout,
            "final boss reaches a terminal battle outcome");
        Require(app.ResolveBattle(result, encounter).Accepted && app.ActiveRun is null,
            "terminal boss clears the consumed growth snapshot and completes without reusable node rewards");
    }

    private static RunApplication NewRun(TowerAutobattler.Composition.CompiledGamePackage package,
        CompiledGrowthRules rules, MemorySave save, string? selectedHeroId = null)
    {
        var app = new RunApplication(package.Content, save, package.Project, rules);
        var heroId = selectedHeroId ?? rules.Heroes.Keys.First(id => !rules.Heroes[id].ProductionModes.IsEmpty);
        if (!app.Meta.UnlockedHeroIds.Contains(heroId)) app.Meta.UnlockedHeroIds.Add(heroId);
        Require(app.StartNewRun(heroId, 0x61726f777468UL), "create growth run");
        return app;
    }

    private static void EnsureFourFrostRoster(RunApplication app)
    {
        string[] frostIds = ["hero_mx01", "hero_mx02", "hero_mx03", "hero_mx04"];
        foreach (var contentId in frostIds)
            if (app.ActiveRun!.Roster.All(hero => hero.ContentId != contentId))
                Require(app.Recruit(contentId), "recruit authored frost fixture member " + contentId);
        foreach (var hero in app.ActiveRun!.Roster.Where(hero => frostIds.Contains(hero.ContentId)))
        {
            if (app.ActiveRun.Deployment.Contains(hero.InstanceId)) continue;
            var slot = app.ActiveRun.Deployment.FindIndex(string.IsNullOrEmpty);
            Require(slot >= 0 && app.MoveDeploymentUnit(hero.InstanceId, slot), "deploy authored frost fixture member " + hero.ContentId);
        }
        Require(frostIds.All(id => app.ActiveRun.Roster.Any(hero => hero.ContentId == id &&
            app.ActiveRun.Deployment.Contains(hero.InstanceId))), "four authored frost members are deployed");
    }

    private static bool SelectNextAuthoredBattle(RunApplication app)
    {
        for (var step = 0; step < app.Project.Campaign.TotalFloors; step++)
        {
            var options = app.CurrentOptions();
            var battle = options.FirstOrDefault(option => option.Type is TowerNodeType.Combat or TowerNodeType.Elite or TowerNodeType.Boss);
            if (battle is not null) return SelectTestNode(app, battle.Type);
            var noncombat = options.FirstOrDefault();
            if (noncombat is null || !app.SelectNode(noncombat.Type) || app.PendingOffer is null) return false;
            while (app.PendingOffer is { } current)
            {
                var choice = current.AllowSkip
                    ? null
                    : current.Choices.FirstOrDefault(candidate => app.CheckOfferChoice(candidate).Succeeded)?.StableId;
                if (choice is null && !current.AllowSkip || !app.ResolveOffer(current.OfferId, choice).Succeeded) return false;
            }
        }
        return false;
    }

    private static bool SelectTestNode(RunApplication app, TowerNodeType type) =>
        app.SelectNode(type, app.CurrentOpponentOptions(type).FirstOrDefault()?.OptionId);

    private static string Snapshot(ActiveRunDto run) => JsonSerializer.Serialize(run);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private sealed class MemorySave : IRunSaveService
    {
        public string? Json { get; set; }
        public bool FailWrites { get; set; }
        private MetaProgressDto _meta = new();
        public MetaProgressDto LoadMeta() => _meta;
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => Json is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(Json);
        public bool SaveMeta(MetaProgressDto value) { _meta = value; return true; }
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) { if (FailWrites) return false; Json = Snapshot(value); return true; }
        public void DeleteActiveRun() => Json = null;
    }
}
