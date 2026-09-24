using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Godot;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

// Production content, isolated memory saves, no battle or player-save access.
public partial class RecruitmentUniquenessContractSmoke : Node
{
    public override async void _Ready()
    {
        var exit = 0;
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            VerifyCandidates(package);
            VerifySavedOffers(package);
            VerifyRepeatablePurchase(package);
            VerifyCompositeBranches(package);
            GD.Print("RECRUITMENT_UNIQUENESS_CONTRACT_OK owned-and-reserve batch exhaustion reload atomic-purchase branches items-unchanged");
        }
        catch (Exception exception) { exit = 1; GD.PrintErr("RECRUITMENT_UNIQUENESS_CONTRACT_FAILED: " + exception); }
        GetTree().Quit(exit);
    }

    private static void VerifyCandidates(CompiledGamePackage package)
    {
        var (app, save) = Start(package);
        var run = app.ActiveRun!;
        var recruitId = Unowned(app)[0];
        Require(app.Recruit(recruitId) && !run.Deployment.Contains(run.Roster.Last().InstanceId), "reserve recruitment fixture");
        var before = JsonSerializer.Serialize(run);
        var stored = save.RunJson;
        var writes = save.Attempts;
        foreach (var owned in run.Roster.ToArray())
            Require(!app.Recruit(owned.ContentId), "deployed and reserve duplicates rejected");
        Require(JsonSerializer.Serialize(run) == before && save.RunJson == stored && save.Attempts == writes,
            "duplicate rejection preserves state, instance sequence and saved bytes without a write");

        var persistence = new RunProgressionPersistenceService(package.Content, save, app.Project);
        var rewards = new RunRewardEconomyService(package.Content, app.Project, persistence, run);
        var definitions = app.Project.Campaign.RecruitmentPool;
        var repeatedPool = definitions with { ContentIds = definitions.ContentIds.AddRange(definitions.ContentIds) };
        var eligible = Unowned(app);
        for (var seed = 1UL; seed <= 64; seed++)
        {
            run.Seed = seed;
            var cards = rewards.PickEntries(run, repeatedPool, 6, (int)seed).Select(card => card.StableId).ToArray();
            Require(cards.Length == Math.Min(6, eligible.Length) && cards.Distinct().Count() == cards.Length &&
                    cards.All(eligible.Contains), "all seeds exclude owned heroes and deduplicate before taking slots");
            Require(cards.SequenceEqual(rewards.PickEntries(run, repeatedPool, 6, (int)seed).Select(card => card.StableId)),
                "same opportunity is deterministic");
        }
        var smallPool = definitions with { ContentIds = [run.Roster[0].ContentId, recruitId, eligible[0], eligible[0]] };
        Require(rewards.PickEntries(run, smallPool, 6, 0).Select(card => card.StableId).SequenceEqual([eligible[0]]),
            "short pool does not pad with duplicates");
        Require(rewards.PickEntries(run, smallPool with { ContentIds = [recruitId] }, 6, 0).Count == 0,
            "exhausted direct pool is empty");
        var itemId = app.Project.Campaign.ItemRewardPool.ContentIds[0];
        Require(app.GrantItem(itemId), "owned item fixture");
        var itemPool = app.Project.Campaign.ItemRewardPool with { ContentIds = [itemId, itemId] };
        Require(rewards.PickEntries(run, itemPool, 6, 0).Count == 2, "hero rule does not change item supply");

        var a = Hero("a", eligible[0]);
        var duplicate = Hero("same-hero-different-card", eligible[0]) with { ContentId = eligible[1] };
        var b = Hero("b", eligible[1]);
        var c = Hero("c", eligible[2]);
        var offerProject = WithOffer(app.Project, [a], [Hero("owned", recruitId), duplicate, b, c], 3);
        var decisions = new RunDecisionService(package.Content, offerProject, persistence);
        var generated = decisions.CreateOffer(run, RunOfferKind.Recruitment);
        Require(generated.Choices.SelectMany(choice => choice.Operations).Select(op => op.ContentId).ToHashSet()
                .SetEquals(eligible.Take(3)) && generated.Choices.Length == 3 && generated.Choices[0].StableId == "a",
            "fixed and pooled cards share identity exclusion, independent of presentation id");
        Require(JsonSerializer.Serialize(generated) == JsonSerializer.Serialize(decisions.CreateOffer(run, RunOfferKind.Recruitment)),
            "offer creation is stable and viewing unselected heroes does not consume them");

        var openingId = app.Meta.UnlockedHeroIds[0];
        var startProject = app.Project with
        {
            Campaign = app.Project.Campaign with
            {
                StarterPool = app.Project.Campaign.StarterPool with { ContentIds = [openingId, eligible[0], eligible[0], eligible[1]] }
            },
            RunRules = app.Rules with { StarterRosterHeroCount = 2 }
        };
        var starter = new RunApplication(package.Content, new MemorySave(), startProject);
        Require(starter.StartNewRun(openingId, 399) && starter.ActiveRun!.Roster.Select(hero => hero.ContentId).Distinct().Count() == 3,
            "starting hero and duplicate starter pool entries cannot create duplicate companions");
    }

    private static void VerifySavedOffers(CompiledGamePackage package)
    {
        var (app, save) = Start(package);
        var run = app.ActiveRun!;
        var nextId = Unowned(app)[0];
        var owned = Hero("owned", run.Roster[0].ContentId);
        var available = Hero("available", nextId);
        Install(run, [owned, available, Hero("duplicate", nextId)], allowSkip: false);
        Require(save.SaveActiveRun(run), "save stale mixed opportunity");
        var stored = save.RunJson;
        var restored = new RunApplication(package.Content, save, app.Project);
        Require(restored.ActiveRun is not null && restored.PendingOffer!.Choices.Select(card => card.StableId).SequenceEqual(["available"])
                && save.RunJson == stored, "reload hides stale candidates without rewriting the saved snapshot");
        var before = JsonSerializer.Serialize(restored.ActiveRun);
        var writes = save.Attempts;
        Require(!restored.ResolveOffer("uniqueness-probe", "owned").Succeeded &&
                !restored.ResolveOffer("uniqueness-probe", "duplicate").Succeeded &&
                JsonSerializer.Serialize(restored.ActiveRun) == before && save.Attempts == writes,
            "stale or duplicate card id cannot be claimed");
        Require(restored.ResolveOffer("uniqueness-probe", "available").Succeeded &&
                restored.ActiveRun!.Roster.Count(hero => hero.ContentId == nextId) == 1,
            "remaining candidate still resolves normally");

        run = restored.ActiveRun!;
        Install(run, [Hero("only-owned", nextId)], allowSkip: false);
        save.SaveActiveRun(run);
        var exhausted = new RunApplication(package.Content, save, app.Project);
        var floor = exhausted.ActiveRun!.FloorIndex;
        Require(exhausted.PendingOffer is { AllowSkip: true, Choices.IsEmpty: true }, "forced stale offer becomes skippable");
        Require(exhausted.ResolveOffer("uniqueness-probe", null).Succeeded && exhausted.ActiveRun.FloorIndex == floor + 1 &&
                !exhausted.ResolveOffer("uniqueness-probe", null).Succeeded, "empty stale opportunity advances exactly once");

        var emptyProject = WithOffer(app.Project, [], [owned], 3);
        var persistence = new RunProgressionPersistenceService(package.Content, save, emptyProject);
        var decision = new RunDecisionService(package.Content, emptyProject, persistence);
        run = exhausted.ActiveRun;
        run.PendingNode = true;
        run.SelectedNode = TowerNodeType.Recruitment;
        run.PendingOffer = decision.CreateOffer(run, RunOfferKind.Recruitment);
        Require(run.PendingOffer is { AllowSkip: true, Choices.IsEmpty: true } && persistence.ValidateRun(run),
            "new exhausted offer remains a valid persisted opportunity");
        save.SaveActiveRun(run);
        var emptyReload = new RunApplication(package.Content, save, emptyProject);
        Require(emptyReload.ActiveRun is not null && emptyReload.ResolveOffer(run.PendingOffer.OfferId, null).Succeeded,
            "empty generated offer survives reload and can finish");
    }

    private static void VerifyRepeatablePurchase(CompiledGamePackage package)
    {
        var (app, save) = Start(package);
        var run = app.ActiveRun!;
        run.Gold = 100;
        var id = Unowned(app)[0];
        var hero = Hero("hero", id) with { Costs = [new(RunOperationKind.SpendGold, 7)] };
        var item = new CompiledRunChoice("item", "测试物品", "", [], [new(RunOperationKind.SpendGold, 2)],
            [new(RunOperationKind.GrantItem, 1, ContentId: app.Project.Campaign.ItemRewardPool.ContentIds[0])], FailureOperations: []);
        Install(run, [hero, item], shop: true);
        save.SaveActiveRun(run);
        var before = JsonSerializer.Serialize(run);
        var stored = save.RunJson;
        save.Fail = true;
        Require(app.ResolveOffer("uniqueness-probe", "hero").Failure == RunDecisionFailure.PersistenceFailed &&
                JsonSerializer.Serialize(run) == before && save.RunJson == stored, "failed purchase retains price, hero, sequence and offer");
        save.Fail = false;
        Require(app.ResolveOffer("uniqueness-probe", "hero").Succeeded && run.Gold == 93 &&
                run.Roster.Count(member => member.ContentId == id) == 1 &&
                app.PendingOffer!.Choices.Select(card => card.StableId).SequenceEqual(["item"]),
            "one successful purchase atomically removes hero card and keeps repeatable item");
        var writes = save.Attempts;
        Require(!app.ResolveOffer("uniqueness-probe", "hero").Succeeded && save.Attempts == writes && run.Gold == 93,
            "second hero purchase has no effect");
        var reloaded = new RunApplication(package.Content, save, app.Project);
        Require(reloaded.ResolveOffer("uniqueness-probe", "item").Succeeded &&
                reloaded.ResolveOffer("uniqueness-probe", "item").Succeeded && reloaded.ActiveRun!.Gold == 89,
            "repeatable item purchase unchanged after reload");
    }

    private static void VerifyCompositeBranches(CompiledGamePackage package)
    {
        var (app, save) = Start(package);
        var run = app.ActiveRun!;
        var id = Unowned(app)[0];
        var one = Hero("one", id);
        foreach (var invalid in new[]
        {
            one with { Operations = [new(RunOperationKind.Recruit, 2, ContentId: id)] },
            one with { Operations = one.Operations.AddRange(one.Operations) },
            one with { SuccessChance = .5f, FailureOperations = [new(RunOperationKind.Recruit, 1, ContentId: run.Roster[0].ContentId)] }
        })
        {
            Install(run, [invalid with { Costs = [new(RunOperationKind.SpendGold, 1)] }]);
            var before = JsonSerializer.Serialize(run);
            var writes = save.Attempts;
            Require(!app.CheckOfferChoice(invalid).Succeeded && !app.ResolveOffer("uniqueness-probe", "one").Succeeded &&
                    JsonSerializer.Serialize(run) == before && save.Attempts == writes, "invalid composite never charges or grants partially");
        }
        var alternate = one with { SuccessChance = .5f, FailureOperations = one.Operations };
        Install(run, [alternate]);
        Require(app.ResolveOffer("uniqueness-probe", "one").Succeeded && run.Roster.Count(hero => hero.ContentId == id) == 1,
            "exclusive alternative branches may grant the same single hero");
        var next = Unowned(app)[0];
        var failureOnly = Hero("failure", id) with { SuccessChance = 0, FailureOperations = [new(RunOperationKind.Recruit, 1, ContentId: next)] };
        Install(run, [failureOnly]);
        Require(app.ResolveOffer("uniqueness-probe", "failure").Succeeded && run.Roster.Count(hero => hero.ContentId == next) == 1,
            "unreachable success branch does not exclude a legal failure-only reward");
    }

    private static CompiledGameProject WithOffer(CompiledGameProject project, ImmutableArray<CompiledRunChoice> fixedChoices,
        ImmutableArray<CompiledRunChoice> pooled, int count) => project with
    {
        Campaign = project.Campaign with
        {
            RunOffers = project.Campaign.RunOffers.SetItem(RunOfferKind.Recruitment,
                new CompiledRunOffer("uniqueness", RunOfferKind.Recruitment, "测试招募", false, false, null, RunPoolAction.Recruit, count, fixedChoices)
                { PoolChoices = pooled })
        }
    };
    private static CompiledRunChoice Hero(string key, string id) => new(key, "测试英雄", "", [], [],
        [new(RunOperationKind.Recruit, 1, ContentId: id)], FailureOperations: [], ContentId: id);
    private static string[] Unowned(RunApplication app) => app.Project.Campaign.RecruitmentPool.ContentIds
        .Distinct().Where(id => app.ActiveRun!.Roster.All(hero => hero.ContentId != id)).ToArray();
    private static void Install(ActiveRunDto run, ImmutableArray<CompiledRunChoice> choices, bool allowSkip = true, bool shop = false)
    {
        run.PendingNode = true;
        run.SelectedNode = shop ? TowerNodeType.Shop : TowerNodeType.Recruitment;
        run.PendingOffer = new("uniqueness-probe", shop ? RunOfferKind.Shop : RunOfferKind.Recruitment,
            "测试招募", allowSkip, shop, run.FloorIndex, run.BattleNumber, choices);
    }
    private static (RunApplication App, MemorySave Save) Start(CompiledGamePackage package)
    {
        var save = new MemorySave();
        // This fixture isolates generic operation uniqueness, including custom composite
        // choices. Production stage gates have their own opening/supply contract probe.
        var app = new RunApplication(package.Content, save, package.Project with
        {
            Campaign = package.Project.Campaign with { RecruitmentSupply = null }
        });
        Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], 391), "production start");
        return (app, save);
    }
    private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
    private sealed class MemorySave : IRunSaveService
    {
        public string? RunJson;
        public int Attempts;
        public bool Fail;
        private string _meta = JsonSerializer.Serialize(new MetaProgressDto());
        public MetaProgressDto LoadMeta() => JsonSerializer.Deserialize<MetaProgressDto>(_meta)!;
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => RunJson is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(RunJson);
        public bool SaveMeta(MetaProgressDto value) { _meta = JsonSerializer.Serialize(value); return true; }
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) { Attempts++; if (Fail) return false; RunJson = JsonSerializer.Serialize(value); return true; }
        public void DeleteActiveRun() => RunJson = null;
    }
}
