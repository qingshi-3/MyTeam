using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Godot;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Pure Run projection/commit checks. It never creates or advances a BattleSimulation.
public partial class RunDecisionContractSmoke : Node
{
    public override async void _Ready()
    {
        var code = 0;
        try
        {
            var gate = await TestProjectFixture.PublishAsync(this);
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(";", gate.Report.CoreErrors));
            VerifyTransactions(package);
            VerifyTerminalRecovery(package);
            VerifyAuthoredCompilation(package);
            VerifyChoiceText();
            VerifyMixedCards(package);
            GD.Print("RUN_DECISION_CONTRACT_OK composite=atomic offer=persistent-once failure=retained terminal=idempotent combat=not-run");
        }
        catch (Exception exception) { code = 1; GD.PrintErr("RUN_DECISION_CONTRACT_FAILED: " + exception); }
        GetTree().Quit(code);
    }

    private static void VerifyTransactions(CompiledGamePackage package)
    {
        var save = new MemorySave();
        var app = new RunApplication(package.Content, save, package.Project);
        Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], 3971), "create isolated run");
        var run = app.ActiveRun!;
        run.Gold = 100;
        var content = package.Project.Campaign.ItemRewardPool.ContentIds[0];
        var choice = new CompiledRunChoice("bundle", "组合选择", "金币、物品与人口一起改变", [],
            [new(RunOperationKind.SpendGold, 10)],
            [new(RunOperationKind.GainGold, 4), new(RunOperationKind.GrantItem, 1, ContentId: content), new(RunOperationKind.GrantPopulation, 1)],
            FailureOperations: []);
        InstallOffer(run, choice);
        save.SaveActiveRun(run);
        var before = JsonSerializer.Serialize(run);
        var stored = save.RunJson;
        save.FailRun = true;
        var rejected = app.ResolveOffer("offer-probe", "bundle");
        Require(!rejected.Succeeded && rejected.Failure == RunDecisionFailure.PersistenceFailed &&
                JsonSerializer.Serialize(run) == before && save.RunJson == stored, "failed composite leaves opportunity/cost/effects unchanged");
        save.FailRun = false;
        var saveCount = save.RunSaves;
        var result = app.ResolveOffer("offer-probe", "bundle");
        Require(result.Succeeded && save.RunSaves == saveCount + 1 && run.Gold == 94 &&
                run.CurrentPopulation == package.Project.RunRules.InitialPopulation + 1 && run.FloorIndex == 1 && !run.PendingNode && run.PendingOffer is null,
            "one commit includes price, all grants, consumption and floor advance");
        Require(result.Changes.Any(change => change.Kind == RunChangeKind.Gold) && result.Changes.Any(change => change.Kind == RunChangeKind.ItemAdded), "receipt describes actual changes");
        Require(!app.ResolveOffer("offer-probe", "bundle").Succeeded, "repeat claim rejected");

        var impossible = new CompiledRunChoice("impossible", "失败回滚", "后续操作失败", [], [new(RunOperationKind.SpendGold, 9)],
            [new(RunOperationKind.GainGold, 3), new(RunOperationKind.GrantPopulation, 100)], FailureOperations: []);
        InstallOffer(run, impossible);
        before = JsonSerializer.Serialize(run);
        Require(!app.ResolveOffer("offer-probe", "impossible").Succeeded && JsonSerializer.Serialize(run) == before,
            "late operation failure restores earlier effects and costs");

        var chance = new CompiledRunChoice("chance", "概率失败", "只执行失败分支", [], [new(RunOperationKind.SpendGold, 10)],
            [new(RunOperationKind.GainGold, 100)], 0, [new(RunOperationKind.GainGold, 3)]);
        InstallOffer(run, chance);
        var gold = run.Gold;
        result = app.ResolveOffer("offer-probe", "chance");
        Require(result.Succeeded && !result.ChanceSucceeded && run.Gold == gold - 7, "failed roll still atomically applies configured costs/outcome");

        var persistence = new RunProgressionPersistenceService(package.Content, save, package.Project);
        var decisions = new RunDecisionService(package.Content, package.Project, persistence);
        run.PendingNode = false;
        run.PendingOffer = decisions.CreateOffer(run, RunOfferKind.CombatReward);
        save.SaveActiveRun(run);
        var restored = new RunApplication(package.Content, save, package.Project);
        Require(restored.PendingOffer?.OfferId == run.PendingOffer.OfferId, "unclaimed battle reward survives reload");
        var floor = restored.ActiveRun!.FloorIndex;
        Require(!restored.ResolveOffer("stale", restored.PendingOffer!.Choices[0].StableId).Succeeded, "stale receipt rejected");
        Require(restored.ResolveOffer(restored.PendingOffer.OfferId, null).Succeeded && restored.ActiveRun.FloorIndex == floor,
            "skipping battle reward consumes only reward, not another floor");

        var legacy = restored.ActiveRun;
        legacy.Version = 5;
        legacy.PendingNode = true;
        legacy.SelectedNode = TowerNodeType.Rest;
        legacy.PendingOffer = null;
        save.SaveActiveRun(legacy);
        var originalLegacy = save.RunJson;
        var migrated = new RunApplication(package.Content, save, package.Project);
        Require(migrated.ActiveRun is null && migrated.ActiveRunLoadDiagnostic?.Kind == ActiveRunLoadFailureKind.MigrationRejected &&
                save.RunJson == originalLegacy, "ambiguous v5 noncombat entitlement rejected without guessing or overwriting");
        legacy.PendingNode = false;
        save.SaveActiveRun(legacy);
        migrated = new RunApplication(package.Content, save, package.Project);
        Require(migrated.ActiveRun?.Version == ActiveRunFormationSchema.CurrentVersion && migrated.PendingOffer is null,
            "unambiguous v5 route state migrates without inventing a reward");
    }

    private static void VerifyTerminalRecovery(CompiledGamePackage package)
    {
        var save = new MemorySave();
        var app = new RunApplication(package.Content, save, package.Project);
        Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], 283), "terminal fixture");
        var run = app.ActiveRun!;
        run.TerminalCompletionId = Guid.NewGuid().ToString("N");
        run.TerminalVictory = true;
        save.SaveActiveRun(run);
        var persistence = new RunProgressionPersistenceService(package.Content, save, package.Project);
        save.FailMeta = true;
        Require(!persistence.TryCompleteTerminal(run) && save.RunJson is not null && persistence.Meta.Victories == 0,
            "failed Meta save never deletes Run or advances memory Meta");
        save.FailMeta = false;
        save.FailDelete = true;
        Require(!persistence.TryCompleteTerminal(run) && persistence.Meta.Victories == 1 && save.RunJson is not null,
            "successful Meta receipt survives failed cleanup");
        save.FailDelete = false;
        var resumed = new RunProgressionPersistenceService(package.Content, save, package.Project);
        Require(resumed.TryCompleteTerminal(run) && resumed.Meta.Victories == 1 && save.RunJson is null,
            "retry after reload cleans up without duplicate victory");
    }

    private static void VerifyAuthoredCompilation(CompiledGamePackage package)
    {
        using var authored = (GameProjectDefinition)TestProjectFixture.Authored().Duplicate();
        using var campaign = (CampaignDefinition)authored.Campaign!.Duplicate();
        using var rules = (RunRulesDefinition)authored.RunRules!.Duplicate();
        authored.Campaign = campaign;
        authored.RunRules = rules;
        rules.StarterRosterHeroCount = 7;
        rules.InitialPopulation = 8;
        var operation = new RunOperationDefinition { Kind = RunOperationKind.GainGold, Amount = 5 };
        var choice = new RunChoiceDefinition { StableId = "probe_choice", DisplayName = "测试选项", Operations = [operation] };
        var offer = new RunOfferDefinition { StableId = "probe_offer", Kind = RunOfferKind.Event, DisplayName = "测试事件", Choices = [choice] };
        campaign.RunOffers = [offer];
        var compiled = GameProjectCompiler.Compile(authored, package.Content.Graph);
        Require(compiled.Project is not null && !compiled.Report.HasCoreErrors, "authored recipe and seven starters compile");
        var app = new RunApplication(package.Content, new MemorySave(), compiled.Project!);
        Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], 291) && app.ActiveRun!.Roster.Count == 8,
            "seven initial recruits use general legal placement, not legacy six-element array");
        using var condition = new RunConditionDefinition
        {
            Kind = RunConditionKind.StartingHeroIs,
            ContentId = app.ActiveRun!.Roster.First(hero => hero.ContentId != app.ActiveRun.Roster[0].ContentId).ContentId
        };
        choice.Conditions = [condition];
        offer.AllowSkip = true;
        compiled = GameProjectCompiler.Compile(authored, package.Content.Graph);
        Require(compiled.Project is not null && !compiled.Report.HasCoreErrors,
            "conditional authored offer remains publishable for other starting heroes");
        var conditionalSave = new MemorySave();
        var conditionalApp = new RunApplication(package.Content, conditionalSave, compiled.Project!);
        Require(conditionalApp.StartNewRun(conditionalApp.Meta.UnlockedHeroIds[0], 291), "conditional offer fixture");
        var conditionalRun = conditionalApp.ActiveRun!;
        var persistence = new RunProgressionPersistenceService(package.Content, conditionalSave, compiled.Project!);
        var decisions = new RunDecisionService(package.Content, compiled.Project!, persistence);
        conditionalRun.SelectedNode = TowerNodeType.Event;
        conditionalRun.PendingNode = true;
        conditionalRun.PendingOffer = decisions.CreateOffer(conditionalRun, RunOfferKind.Event);
        Require(conditionalRun.PendingOffer.Choices.Length == 1 && persistence.ValidateRun(conditionalRun) &&
                conditionalApp.CheckOfferChoice(conditionalRun.PendingOffer.Choices[0]).Failure == RunDecisionFailure.NotEligible,
            "unmet starting-hero condition stays visible without making a valid offer empty");
        Require(conditionalApp.ResolveOffer(conditionalRun.PendingOffer.OfferId, null).Succeeded &&
                conditionalRun.PendingOffer is null && !conditionalRun.PendingNode && conditionalRun.FloorIndex == 1,
            "conditional offer with no eligible choice can still be skipped normally");
        operation.Kind = (RunOperationKind)999;
        Require(GameProjectCompiler.Compile(authored, package.Content.Graph).Report.HasCoreErrors, "unsupported operation rejected at publication");
        offer.Dispose(); choice.Dispose(); operation.Dispose();
    }

    private static void InstallOffer(ActiveRunDto run, CompiledRunChoice choice)
    {
        run.PendingNode = true;
        run.SelectedNode = TowerNodeType.Event;
        run.PendingOffer = new("offer-probe", RunOfferKind.Event, "测试机会", true, false, run.FloorIndex, run.BattleNumber, [choice]);
    }

    private static void VerifyChoiceText()
    {
        var choice = new CompiledRunChoice("text_probe", "风险交易", "",
            [new(RunConditionKind.GoldAtLeast, 12), new(RunConditionKind.HasContent, 2, ContentId: "probe"),
                new(RunConditionKind.RosterHealthAtLeast, Ratio: .45f), new(RunConditionKind.StartingHeroIs, ContentId: "probe"),
                new(RunConditionKind.PopulationBelowCap)],
            [new(RunOperationKind.SpendGold, 12), new(RunOperationKind.RecoverRoster, Ratio: -.2f, Target: RunRosterTarget.OtherHeroes, MinimumHealthRatio: .1f)],
            [new(RunOperationKind.GrantPopulation, 1), new(RunOperationKind.IncreasePopulationCap, 2, SourceId: "probe"),
                new(RunOperationKind.Recruit, 1, ContentId: "probe"), new(RunOperationKind.GrantItem, 3, ContentId: "probe")],
            .25f, [new(RunOperationKind.GainGold, 4)]);
        var description = RunDecisionText.Describe(choice, _ => "测试内容");
        Require(description.Contains("支付 12 金币") && description.Contains("最大生命的 20%") &&
                description.Contains("至少 10%") && description.Contains("除初始英雄外") && description.Contains("无论成败"),
            "generated description exposes all costs with their affected targets and payment bounds");
        Require(description.Contains("成功 25%") && description.Contains("失败 75%：获得 4 金币") &&
                description.Contains("人口增加 1") && description.Contains("人口上限增加 2") &&
                description.Contains("测试内容 × 3") && description.Contains("生命不低于 45%"),
            "generated description shows executable conditions, outcomes and failure probability without authored prose");
    }

    private void VerifyMixedCards(CompiledGamePackage package)
    {
        var app = new RunApplication(package.Content, new MemorySave(), package.Project);
        Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], 109), "mixed card fixture");
        var run = app.ActiveRun!;
        var unitId = package.Project.Campaign.RecruitmentPool.ContentIds
            .First(id => !run.Roster.Any(hero => hero.ContentId == id));
        var itemId = package.Project.Campaign.ItemRewardPool.ContentIds[0];
        var generic = new CompiledRunChoice("generic_probe", "通用选项", "", [], [],
            [new(RunOperationKind.GainGold, 1)], FailureOperations: []);
        var recruit = new CompiledRunChoice("recruit_probe", "招募选项", "",
            [new(RunConditionKind.GoldAtLeast, int.MaxValue)], [],
            [new(RunOperationKind.Recruit, 1, ContentId: unitId)], FailureOperations: [], ContentId: unitId);
        var item = new CompiledRunChoice("item_probe", "物品选项", "", [], [],
            [new(RunOperationKind.GrantItem, 1, ContentId: itemId)], FailureOperations: [], ContentId: itemId);
        InstallOffer(run, generic);
        run.PendingOffer = app.PendingOffer! with { Choices = [generic, recruit, item] };
        var container = new VBoxContainer();
        AddChild(container);
        try
        {
            var presentation = package.Project.Presentation;
            RunOfferCardBinder.Sync(container, app, presentation.ChoiceCard, presentation.ItemChoiceCard,
                presentation.SemanticIcons, _ => { });
            var unitCard = container.GetChild<RunOfferChoiceCard>(1);
            Require(container.GetChild<RunOfferChoiceCard>(0).StableId == generic.StableId &&
                    container.GetChild<RunOfferChoiceCard>(2).StableId == item.StableId &&
                    unitCard.StableId == recruit.StableId &&
                    unitCard.GetNode<UnitPortrait>("Layout/Artwork/Portrait").Definition is not null && unitCard.ConfirmButton.Disabled,
                "mixed offer preserves authored unit portrait, disabled qualification and original generic/unit/item order");
            run.PendingOffer = app.PendingOffer! with { Choices = [item, recruit] };
            RunOfferCardBinder.Sync(container, app, presentation.ChoiceCard, presentation.ItemChoiceCard,
                presentation.SemanticIcons, _ => { });
            Require(container.GetChild<RunOfferChoiceCard>(0).StableId == item.StableId && ReferenceEquals(container.GetChild(1), unitCard) &&
                    container.GetChildren().OfType<RunOfferChoiceCard>().Count(card => !card.IsQueuedForDeletion()) == 2,
                "mixed rebind retains the live portrait and retires removed generic choices");
        }
        finally { RemoveChild(container); container.Free(); }
    }
    private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
    private sealed class MemorySave : IRunSaveService
    {
        public string? RunJson { get; private set; }
        private string _meta = JsonSerializer.Serialize(new MetaProgressDto());
        public bool FailRun, FailMeta, FailDelete;
        public int RunSaves { get; private set; }
        public MetaProgressDto LoadMeta() => JsonSerializer.Deserialize<MetaProgressDto>(_meta)!;
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => RunJson is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(RunJson);
        public bool SaveMeta(MetaProgressDto value) { if (FailMeta) return false; _meta = JsonSerializer.Serialize(value); return true; }
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) { if (FailRun) return false; RunJson = JsonSerializer.Serialize(value); RunSaves++; return true; }
        public void DeleteActiveRun() { if (FailDelete) throw new InvalidOperationException("injected delete failure"); RunJson = null; }
    }
}
