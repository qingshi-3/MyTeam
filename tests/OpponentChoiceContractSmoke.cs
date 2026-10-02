using System;
using System.Linq;
using System.Text.Json;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

// Uses genuine simulator receipts with controlled outcomes to verify Run transactions.
public partial class OpponentChoiceContractSmoke : Node
{
    public override async void _Ready()
    {
        var code = 0;
        try
        {
            var published = await GrowthContentPackage.CreateReadyAsync(this);
            var package = published.Package;
            if (package is null)
            {
                var authored = GD.Load<EncounterDefinition>("res://content/growth/encounters/encounter_ember_combat.tres");
                var loaded = authored is null ? "missing ember resource" : $"choice-count={authored.OpponentChoiceCount};" +
                    string.Join(';', authored.Compositions.Select(composition => $"{composition.StableId}:pressure={composition.PressureSummary}," +
                        $"gold={composition.RewardGold},research={composition.RewardResearch},materials={composition.RewardMaterials},only={composition.OpponentChoiceOnly}"));
                throw new InvalidOperationException(string.Join(';', published.Report.CoreErrors) + "\nLoaded authored values: " + loaded);
            }
            var rules = GrowthContentPackage.LoadRules(package.Content);
            VerifyOffersAndSelection(package, rules);
            VerifyOpeningAndAlpha(package, rules);
            VerifyOpponentReceiptBinding(package, rules);
            VerifySettlement(package, rules);
            VerifySettlement(package, rules, legacyOrdinary: true);
            VerifyLegacyMigration(package, rules);
            GD.Print("OPPONENT_CHOICE_CONTRACT_OK elite-only ordinary-direct stable preview lock reload rewards victory defeat duplicate rollback v8-v9-migration alpha boss");
        }
        catch (Exception error) { code = 1; GD.PrintErr("OPPONENT_CHOICE_CONTRACT_FAILED: " + error); }
        GetTree().Quit(code);
    }

    private static void VerifyOffersAndSelection(CompiledGamePackage package, CompiledGrowthRules rules)
    {
        var (app, save) = New(package, rules);
        var run = app.ActiveRun!;
        var tower = app.Tower;
        for (var floor = 0; floor < package.Project.Campaign.TotalFloors; floor++)
        foreach (var type in new[] { TowerNodeType.Combat, TowerNodeType.Elite })
        {
            var probe = new ActiveRunDto { Seed = run.Seed, FloorIndex = floor, Growth = run.Growth };
            var options = tower.OpponentOptions(probe, type);
            Require(options.Count is >= 2 and <= 3 && options.Select(option => option.OptionId).Distinct().Count() == options.Count,
                "each growth region/floor has distinct, bounded real opponents");
            Require(options.Select(option => string.Join(',', option.Encounter.EnemyIds)).Distinct().Count() == options.Count &&
                options.All(option => option.Encounter.EnemyIds.Count == option.Encounter.EnemyCells.Length &&
                    !string.IsNullOrWhiteSpace(option.PressureSummary)), "choices expose distinct complete armies and pressure");
            Require(options.Select(option => (option.RewardGold, option.RewardResearch, option.RewardMaterials)).Distinct().Count() >= 2,
                "choices differ in actual reward type, beyond raw difficulty tiers");
            var first = JsonSerializer.Serialize(options);
            probe.Gold += 123;
            probe.BattleNumber += 7;
            probe.Roster = [];
            Require(JsonSerializer.Serialize(tower.OpponentOptions(probe, type)) == first,
                "candidate armies and rules are stable and independent of player roster/economy");
            Require(tower.SelectableOpponentOptions(probe, type).Count == (type == TowerNodeType.Elite ? options.Count : 0),
                "new opponent choices appear only for elite challenges");
        }

        Require(app.CurrentOpponentOptions(TowerNodeType.Combat).Count == 0, "ordinary battles do not offer opponent choices");
        var ordinaryBefore = Snapshot(run);
        var obsoleteChoice = tower.OpponentOptions(run, TowerNodeType.Combat)[0];
        Require(!app.SelectNode(TowerNodeType.Combat, obsoleteChoice.OptionId) && Snapshot(run) == ordinaryBefore,
            "new ordinary battles reject even a valid old opponent ID");
        save.FailWrites = true;
        Require(!app.SelectNode(TowerNodeType.Combat) && Snapshot(run) == ordinaryBefore,
            "failed ordinary battle selection preserves the unlocked run");
        save.FailWrites = false;
        var ordinaryPlan = tower.Encounter(run, TowerNodeType.Combat);
        Require(app.SelectNode(TowerNodeType.Combat) && run.PendingNode &&
            run.SelectedOpponentOptionId.Length == 0 && !run.LegacyPendingOpponent,
            "ordinary battles commit directly without an opponent ID or migration marker");
        VerifySameEncounter(ordinaryPlan, app.CurrentEncounter());
        var ordinaryRestored = new RunApplication(package.Content, save, package.Project, rules);
        Require(ordinaryRestored.ActiveRun is { PendingNode: true, SelectedOpponentOptionId.Length: 0, LegacyPendingOpponent: false },
            "direct ordinary battle selection survives reload");
        VerifySameEncounter(ordinaryPlan, ordinaryRestored.CurrentEncounter());
        Require(ordinaryRestored.TryBeginGrowthBattle(ordinaryPlan).Succeeded, "direct ordinary battle can begin");
        var ordinaryRun = ordinaryRestored.ActiveRun!;
        var ordinaryGold = ordinaryRun.Gold;
        var ordinaryResearch = ordinaryRun.Growth!.Research;
        var ordinaryMaterials = ordinaryRun.Growth.Materials;
        var productionResearch = ordinaryRun.Growth.PendingNode!.Assignments.Where(assignment => assignment.Mode == GrowthProductionMode.Research)
            .Sum(assignment => rules.Heroes[ordinaryRun.Roster.Single(hero => hero.InstanceId == assignment.ProducerInstanceId).ContentId].ResearchYield);
        var ordinaryResult = RunHealthFixture.Result(ordinaryRestored.BuildBattleConfig(ordinaryPlan, false), BattleOutcome.PlayerVictory);
        var ordinaryHeroBonus = ordinaryRestored.Rules.StartingHeroEconomy.TryGetValue(ordinaryRun.Roster[0].ContentId, out var ordinaryEconomy)
            ? ordinaryEconomy.BattleGoldBonus : 0;
        Require(ordinaryRestored.ResolveBattle(ordinaryResult, ordinaryPlan).Accepted &&
            ordinaryRun.Gold == ordinaryGold - ordinaryResult.GoldSpent + ordinaryRestored.Rules.NormalBattleGold + ordinaryHeroBonus &&
            ordinaryRun.Growth.Research == ordinaryResearch + productionResearch &&
            ordinaryRun.Growth.Materials == ordinaryMaterials + rules.MaterialsPerNode,
            "new direct ordinary victory grants base rewards without an old candidate bonus");

        (app, save) = NewChallenge(package, rules, TowerNodeType.Elite);
        run = app.ActiveRun!;
        var choices = app.CurrentOpponentOptions(TowerNodeType.Elite);
        Require(choices.Count is >= 2 and <= 3, "elite challenge offers distinct concrete opponents");
        var before = Snapshot(run);
        Require(!app.SelectNode(TowerNodeType.Elite) && !app.SelectNode(TowerNodeType.Elite, "unknown") &&
            Snapshot(run) == before, "new elite battles require an explicit legal opponent ID");
        var choice = choices.Single(option => option.RewardResearch > 0);
        save.FailWrites = true;
        Require(!app.SelectNode(TowerNodeType.Elite, choice.OptionId) && Snapshot(run) == before,
            "failed selection publication does not lock or spend anything");
        save.FailWrites = false;
        Require(app.SelectNode(TowerNodeType.Elite, choice.OptionId) && run.PendingNode &&
            run.SelectedOpponentOptionId == choice.OptionId && !run.LegacyPendingOpponent,
            "selection commits one opponent");
        Require(!app.SelectNode(TowerNodeType.Elite, choices[0].OptionId), "selected opponent cannot be replaced");
        VerifySameEncounter(choice.Encounter, app.CurrentEncounter());
        var restored = new RunApplication(package.Content, save, package.Project, rules);
        Require(restored.ActiveRun?.SelectedOpponentOptionId == choice.OptionId, "opponent ID survives reload");
        VerifySameEncounter(choice.Encounter, restored.CurrentEncounter());
        var config = restored.BuildBattleConfig(restored.CurrentEncounter());
        Require(config.Spawns.Where(spawn => spawn.Team == 1).Select(spawn => spawn.Unit.ContentId)
            .SequenceEqual(choice.Encounter.EnemyIds), "preview army is the actual battle army");
        run.SelectedOpponentOptionId = $"opponent:{run.Seed:x16}:{run.FloorIndex + 1}:{(int)TowerNodeType.Elite}:{choice.Encounter.CompositionId}";
        save.Json = Snapshot(run);
        Require(new RunApplication(package.Content, save, package.Project, rules).ActiveRun is null,
            "wrong-floor/stale selection cannot be loaded");
    }

    private static void VerifyOpeningAndAlpha(CompiledGamePackage package, CompiledGrowthRules rules)
    {
        var save = new RunHealthMemorySave();
        var app = new RunApplication(package.Content, save, package.Project, rules);
        Require(app.BeginOpeningRecruitment(0x1230), "start saved draft");
        foreach (var candidate in app.ActiveRun!.OpeningRecruitment!.CandidateIds.Take(2))
            Require(app.ToggleOpeningHero(candidate), "choose opening hero");
        Require(app.ConfirmOpeningRecruitment() && app.ActiveRun!.OpeningRecruitment is null &&
            !app.ActiveRun.PendingNode && app.CurrentOpponentOptions(TowerNodeType.Combat).Count == 0,
            "growth draft confirmation reaches the route without an ordinary opponent choice");
        var savedCommands = app.ActiveRun!.EquippedTacticalCommandIds.ToArray();
        Require(app.SelectNode(TowerNodeType.Combat),
            "select pure automatic battle fixture");
        var growthConfig = app.BuildBattleConfig(app.CurrentEncounter());
        using (var growthBattle = new BattleSimulation(growthConfig))
            Require(growthConfig.TacticalCommands is null && growthBattle.TacticalPoints == 0 &&
                growthBattle.TacticalCommands.Slots.IsEmpty && !growthBattle.TryUseTacticalCommand(0).Succeeded &&
                growthBattle.SuccessfulTacticalCommandUses == 0 && app.ActiveRun.EquippedTacticalCommandIds.SequenceEqual(savedCommands),
                "growth battles have no usable combat commands while preserving compatible saved ids");

        var alpha = new RunApplication(package.Content, new RunHealthMemorySave(), package.Project);
        Require(alpha.StartNewRun(alpha.Meta.UnlockedHeroIds[0], 0x1230) &&
            alpha.CurrentOpponentOptions(TowerNodeType.Combat).Count == 0 && alpha.SelectNode(TowerNodeType.Combat),
            "runtime without growth preserves legacy node selection");
        var alphaConfig = alpha.BuildBattleConfig(alpha.CurrentEncounter());
        using (var alphaBattle = new BattleSimulation(alphaConfig))
            Require(alphaConfig.TacticalCommands is not null && alphaBattle.TacticalPoints > 0 &&
                alphaBattle.TacticalCommands.Slots.Length == 2, "alpha retains its two independent combat command slots");
        var (boss, _) = New(package, rules);
        boss.ActiveRun!.FloorIndex = package.Project.Campaign.FloorsPerRegion - 1;
        Require(boss.CurrentOpponentOptions(TowerNodeType.Boss).Count == 0 && boss.SelectNode(TowerNodeType.Boss) &&
            boss.CurrentEncounter().IsBoss, "boss remains one mandatory encounter");
    }

    private static void VerifySettlement(CompiledGamePackage package, CompiledGrowthRules rules, bool legacyOrdinary = false)
    {
        foreach (var rewardKind in new[] { "gold", "research", "materials" })
        foreach (var outcome in new[] { BattleOutcome.PlayerVictory, BattleOutcome.PlayerDefeat })
        {
            var type = legacyOrdinary ? TowerNodeType.Combat : TowerNodeType.Elite;
            var (app, save) = NewChallenge(package, rules, type);
            var run = app.ActiveRun!;
            var candidates = legacyOrdinary ? app.Tower.OpponentOptions(run, type) : app.CurrentOpponentOptions(type);
            var choice = candidates.Single(option => rewardKind switch
            {
                "gold" => option.RewardGold > 0,
                "research" => option.RewardResearch > 0,
                _ => option.RewardMaterials > 0
            });
            if (legacyOrdinary)
            {
                // Reconstruct an already committed v9 ordinary choice, never a new player action.
                run.SelectedNode = type;
                run.PendingNode = true;
                run.SelectedOpponentOptionId = choice.OptionId;
                run.Version = 9;
                save.Json = Snapshot(run);
                app = new RunApplication(package.Content, save, package.Project, rules);
                run = app.ActiveRun ?? throw new InvalidOperationException("old v9 chosen ordinary battle must load");
                Require(run.Version == ActiveRunFormationSchema.CurrentVersion && run.SelectedOpponentOptionId == choice.OptionId &&
                    !run.LegacyPendingOpponent, "v9 chosen ordinary battle upgrades with its original identity");
                VerifySameEncounter(choice.Encounter, app.CurrentEncounter());
            }
            else Require(app.SelectNode(type, choice.OptionId), "select reward fixture opponent");
            var encounter = app.CurrentEncounter();
            if (legacyOrdinary)
            {
                run.Growth!.SpellInventory["suppress_front"] = 1;
                Require(app.EquipGrowthSpell("suppress_front", run.Roster[0].InstanceId).Succeeded,
                    "old chosen ordinary battle can retain a spell preset");
            }
            Require(app.TryBeginGrowthBattle(encounter).Succeeded, "freeze real growth battle");
            if (legacyOrdinary)
            {
                run.Version = 9;
                save.Json = Snapshot(run);
                app = new RunApplication(package.Content, save, package.Project, rules);
                run = app.ActiveRun ?? throw new InvalidOperationException("old v9 started ordinary battle must load");
                VerifySameEncounter(encounter, app.CurrentEncounter());
                Require(run.Growth!.SpellInventory["suppress_front"] == 0 &&
                    run.Growth.PendingNode!.ConsumedSpellId == "suppress_front",
                    "v9 started ordinary battle retains its consumed spell and frozen snapshot");
                var frozen = Snapshot(run);
                Require(app.TryBeginGrowthBattle(encounter).Succeeded && Snapshot(run) == frozen,
                    "continuing an old v9 started ordinary battle does not spend the spell twice");
            }
            var ordinaryResearch = run.Growth!.PendingNode!.Assignments.Where(assignment => assignment.Mode == GrowthProductionMode.Research)
                .Sum(assignment => rules.Heroes[run.Roster.Single(hero => hero.InstanceId == assignment.ProducerInstanceId).ContentId].ResearchYield);
            var gold = run.Gold;
            var research = run.Growth.Research;
            var materials = run.Growth.Materials;
            var result = RunHealthFixture.Result(app.BuildBattleConfig(encounter, false), outcome);
            var before = Snapshot(run);
            save.FailWrites = true;
            Require(!app.ResolveBattle(result, encounter).Accepted && Snapshot(run) == before,
                "failed result save preserves reward, opponent and battle snapshot");
            save.FailWrites = false;
            Require(app.ResolveBattle(result, encounter).Accepted, "genuine result settles");
            var won = outcome == BattleOutcome.PlayerVictory;
            var heroBonus = app.Rules.StartingHeroEconomy.TryGetValue(run.Roster[0].ContentId, out var economy) ? economy.BattleGoldBonus : 0;
            var baseGold = type == TowerNodeType.Elite ? app.Rules.EliteBattleGold : app.Rules.NormalBattleGold;
            Require(run.Gold == gold - result.GoldSpent + (won ? baseGold + heroBonus + choice.RewardGold : 0) &&
                run.Growth.Research == research + ordinaryResearch + (won ? choice.RewardResearch : 0) &&
                run.Growth.Materials == materials + rules.MaterialsPerNode + (won ? choice.RewardMaterials : 0),
                "only victory awards exact selected opponent bonuses alongside existing settlement");
            Require(!run.PendingNode && run.SelectedOpponentOptionId.Length == 0 && !run.LegacyPendingOpponent,
                "advancing consumes the opponent selection");
            var settled = Snapshot(run);
            Require(!app.ResolveBattle(result, encounter).Accepted && Snapshot(run) == settled,
                "duplicate receipts cannot award opponent rewards again");
            Require(Snapshot(new RunApplication(package.Content, save, package.Project, rules).ActiveRun!) == settled,
                "settled bonuses and cleared choice survive reload");
        }
    }

    private static void VerifyOpponentReceiptBinding(CompiledGamePackage package, CompiledGrowthRules rules)
    {
        var (app, _) = NewChallenge(package, rules, TowerNodeType.Elite);
        var choices = app.CurrentOpponentOptions(TowerNodeType.Elite);
        var selected = choices[0];
        var other = choices[1];
        Require(selected.Encounter.EncounterId == other.Encounter.EncounterId &&
            selected.Encounter.CompositionId != other.Encounter.CompositionId, "fixture shares encounter but has different armies");
        Require(app.SelectNode(TowerNodeType.Elite, selected.OptionId), "select receipt binding fixture");
        var beforeStart = Snapshot(app.ActiveRun!);
        Require(!app.TryBeginGrowthBattle(other.Encounter).Succeeded && Snapshot(app.ActiveRun!) == beforeStart,
            "starting another candidate cannot consume the selected battle's spell or snapshot");
        Require(app.TryBeginGrowthBattle(selected.Encounter).Succeeded, "freeze selected opponent battle");
        var resultFromOther = RunHealthFixture.Result(app.BuildBattleConfig(other.Encounter, false), BattleOutcome.PlayerVictory);
        var before = Snapshot(app.ActiveRun!);
        Require(resultFromOther.Identity?.CompositionId == other.Encounter.CompositionId &&
            !app.ResolveBattle(resultFromOther, selected.Encounter).Accepted && Snapshot(app.ActiveRun!) == before,
            "another candidate's victory receipt cannot settle or claim selected rewards");
        var selectedResult = RunHealthFixture.Result(app.BuildBattleConfig(selected.Encounter, false), BattleOutcome.PlayerVictory);
        var oldUnboundReceipt = selectedResult with { Identity = selectedResult.Identity! with { CompositionId = string.Empty } };
        Require(!app.ResolveBattle(oldUnboundReceipt, selected.Encounter).Accepted && Snapshot(app.ActiveRun!) == before,
            "an unbound old receipt cannot authenticate a composed encounter");
        Require(app.ResolveBattle(selectedResult, selected.Encounter).Accepted, "selected composition receipt remains accepted");
    }

    private static void VerifyLegacyMigration(CompiledGamePackage package, CompiledGrowthRules rules)
    {
        var (source, save) = New(package, rules);
        var run = source.ActiveRun!;
        run.Version = 8;
        run.SelectedNode = TowerNodeType.Combat;
        run.PendingNode = true;
        var legacyEncounter = source.Tower.Encounter(run, TowerNodeType.Combat);
        Require(legacyEncounter.CompositionId == "ember_combat_patrol", "old opening random path excludes new choice-only formations");
        save.Json = Snapshot(run);
        var oldJson = save.Json;
        save.FailWrites = true;
        var rejected = new RunApplication(package.Content, save, package.Project, rules);
        Require(rejected.ActiveRun is null && rejected.ActiveRunLoadDiagnostic?.Kind == ActiveRunLoadFailureKind.MigrationPublicationFailed &&
            save.Json == oldJson, "failed migration publication retains old v8 save");
        save.FailWrites = false;
        var restored = new RunApplication(package.Content, save, package.Project, rules);
        Require(restored.ActiveRun is { LegacyPendingOpponent: true, SelectedOpponentOptionId.Length: 0 } &&
            restored.ActiveRun.Version == ActiveRunFormationSchema.CurrentVersion,
            "v8 committed fight migrates without inventing a chosen opponent");
        VerifySameEncounter(legacyEncounter, restored.CurrentEncounter());
        restored.ActiveRun!.Version = 9;
        save.Json = Snapshot(restored.ActiveRun);
        restored = new RunApplication(package.Content, save, package.Project, rules);
        Require(restored.ActiveRun!.LegacyPendingOpponent, "old v9 migration continuation survives upgrade and reload");
        VerifySameEncounter(legacyEncounter, restored.CurrentEncounter());
        Require(restored.TryBeginGrowthBattle(restored.CurrentEncounter()).Succeeded, "legacy continuation can start");
        var gold = restored.ActiveRun.Gold;
        var result = RunHealthFixture.Result(restored.BuildBattleConfig(restored.CurrentEncounter(), false), BattleOutcome.PlayerDefeat);
        Require(restored.ResolveBattle(result, restored.CurrentEncounter()).Accepted && !restored.ActiveRun!.LegacyPendingOpponent &&
            restored.ActiveRun.SelectedOpponentOptionId.Length == 0 && restored.ActiveRun.Gold == gold,
            "legacy fight continues, grants no opponent bonus and clears continuation marker");
        PlaceAtNode(restored, TowerNodeType.Combat);
        Require(restored.CurrentOpponentOptions(TowerNodeType.Combat).Count == 0 && restored.SelectNode(TowerNodeType.Combat),
            "after legacy continuation a new ordinary battle uses the direct current flow");

        var (unselected, unselectedSave) = New(package, rules);
        unselected.ActiveRun!.Version = 8;
        unselectedSave.Json = Snapshot(unselected.ActiveRun);
        var migrated = new RunApplication(package.Content, unselectedSave, package.Project, rules);
        Require(migrated.ActiveRun is { LegacyPendingOpponent: false, PendingNode: false } &&
            migrated.ActiveRun.Version == ActiveRunFormationSchema.CurrentVersion &&
            migrated.CurrentOpponentOptions(TowerNodeType.Combat).Count == 0 && migrated.SelectNode(TowerNodeType.Combat),
            "v8 unselected progression migrates into direct ordinary battles");
    }

    private static (RunApplication, RunHealthMemorySave) NewChallenge(
        CompiledGamePackage package, CompiledGrowthRules rules, TowerNodeType type)
    {
        var (app, save) = New(package, rules);
        PlaceAtNode(app, type);
        return (app, save);
    }

    private static void PlaceAtNode(RunApplication app, TowerNodeType type)
    {
        for (var floor = 0; floor < app.Project.Campaign.TotalFloors; floor++)
        {
            app.ActiveRun!.FloorIndex = floor;
            if (app.CurrentOptions().Any(option => option.Type == type)) return;
        }
        throw new InvalidOperationException("Campaign has no fixture opportunity for " + type);
    }

    private static (RunApplication, RunHealthMemorySave) New(CompiledGamePackage package, CompiledGrowthRules rules)
    {
        var save = new RunHealthMemorySave();
        var app = new RunApplication(package.Content, save, package.Project, rules);
        var heroId = rules.Heroes.Keys.First();
        if (!app.Meta.UnlockedHeroIds.Contains(heroId)) app.Meta.UnlockedHeroIds.Add(heroId);
        Require(app.StartNewRun(heroId, 0x1230), "create growth fixture");
        return (app, save);
    }

    private static void VerifySameEncounter(EncounterPlan expected, EncounterPlan actual) => Require(
        expected.Title == actual.Title && expected.EncounterId == actual.EncounterId && expected.CompositionId == actual.CompositionId &&
        expected.FloorRuleId == actual.FloorRuleId && expected.EnemyIds.SequenceEqual(actual.EnemyIds) &&
        expected.EnemyCells.SequenceEqual(actual.EnemyCells), "enemy preview, committed fight and reload are identical");

    private static string Snapshot(ActiveRunDto run) => JsonSerializer.Serialize(run);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
