using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

// Genuine simulator receipts, production growth content and isolated serialized
// saves. Checks entitlement/transactions rather than a second reward algorithm.
public partial class GrowthRewardGroupsContractSmoke : Node
{
    public override async void _Ready()
    {
        var exit = 0;
        try
        {
            var published = await GrowthContentPackage.CreateReadyAsync(this,
                defaultNewRunProfileId: RunProfileIds.GrowthTowerLegacy);
            var package = published.Package ?? throw new InvalidOperationException(string.Join(';', published.Report.CoreErrors));
            var rules = GrowthContentPackage.LoadRules(package.Content);
            VerifySettlementAndIndependentClaims(package, rules);
            VerifySkipAndDefeat(package, rules);
            VerifyLegacyPendingReward(package, rules);
            VerifyUniqueRelicSupply(package, rules);
            VerifyShopStock(package);
            VerifyInvalidQueue(package, rules);
            GD.Print("GROWTH_REWARD_GROUPS_CONTRACT_OK victory=atomic categories=independent claims=once skip=per-group reload=frozen legacy=preserved relic=unique shop=stock-consumed");
        }
        catch (Exception error) { exit = 1; GD.PrintErr("GROWTH_REWARD_GROUPS_CONTRACT_FAILED: " + error); }
        GetTree().Quit(exit);
    }

    private static void VerifySettlementAndIndependentClaims(CompiledGamePackage package, CompiledGrowthRules rules)
    {
        var (app, save) = New(package, rules);
        var run = app.ActiveRun!;
        var encounter = Prepare(app);
        var result = RunHealthFixture.Result(app.BuildBattleConfig(encounter, false), BattleOutcome.PlayerVictory);
        var before = Snapshot(run);
        var stored = save.Json;
        save.FailWrites = true;
        Require(!app.ResolveBattle(result, encounter).Accepted && Snapshot(run) == before && save.Json == stored,
            "failed victory leaves resources, growth, progression and complete reward queue uncommitted");
        save.FailWrites = false;
        var writes = save.SuccessfulWrites;
        Require(app.ResolveBattle(result, encounter).Accepted && save.SuccessfulWrites == writes + 1,
            "one victory save commits base gains, growth, advance and every independent group");
        Require(run.FloorIndex == 1 && run.BattleNumber == 1 && !run.PendingNode &&
            run.PendingOffer is { RewardCategory: RunRewardCategory.Equipment } &&
            run.PendingRewardGroups is { Length: 1 } && run.PendingRewardGroups[0].RewardCategory == RunRewardCategory.Relic,
            "victory freezes separate equipment and relic claim rights");
        var settled = Snapshot(run);
        Require(!app.ResolveBattle(result, encounter).Accepted && Snapshot(run) == settled,
            "duplicate victory receipt cannot repeat resources or reward groups");
        var equipmentOffer = app.PendingOffer!;
        var relicOffer = run.PendingRewardGroups[0];
        VerifyCategory(package, equipmentOffer, ItemProductKind.Equipment);
        VerifyCategory(package, relicOffer, ItemProductKind.Relic);
        var gold = run.Gold;
        var materials = run.Growth!.Materials;
        var research = run.Growth.Research;
        var history = JsonSerializer.Serialize(run.Growth.History);
        var equipmentCount = run.EquipmentInventory.Count;
        before = Snapshot(run);
        save.FailWrites = true;
        Require(app.ResolveOffer(equipmentOffer.OfferId, equipmentOffer.Choices[0].StableId).Failure == RunDecisionFailure.PersistenceFailed &&
            Snapshot(run) == before && save.Json == settled,
            "failed claim keeps the item and both independent rights untouched");
        save.FailWrites = false;
        Require(app.ResolveOffer(equipmentOffer.OfferId, equipmentOffer.Choices[0].StableId).Succeeded &&
            run.EquipmentInventory.Count == equipmentCount + 1 && run.PendingOffer == relicOffer && run.PendingRewardGroups.IsEmpty,
            "claiming equipment advances only to the already frozen relic group");
        before = Snapshot(run);
        Require(!app.ResolveOffer(equipmentOffer.OfferId, equipmentOffer.Choices[0].StableId).Succeeded && Snapshot(run) == before,
            "old group identity cannot claim a second item or consume the next group");
        var restored = new RunApplication(package.Content, save, package.Project, rules);
        Require(restored.ActiveRun is not null && Snapshot(restored.ActiveRun) == before &&
            JsonSerializer.Serialize(restored.PendingOffer) == JsonSerializer.Serialize(relicOffer),
            "partial claim reload restores exactly the remaining group");
        run = restored.ActiveRun!;
        var relicCount = run.Items.Count;
        Require(restored.ResolveOffer(relicOffer.OfferId, relicOffer.Choices[0].StableId).Succeeded &&
            run.Items.Count == relicCount + 1 && run.PendingOffer is null && run.PendingRewardGroups.IsEmpty,
            "relic can be independently claimed after equipment");
        Require(run.FloorIndex == 1 && run.BattleNumber == 1 && run.Gold == gold && run.Growth!.Materials == materials &&
            run.Growth.Research == research && JsonSerializer.Serialize(run.Growth.History) == history,
            "claims never award base resources again, settle growth again or advance another floor");
    }

    private static void VerifySkipAndDefeat(CompiledGamePackage package, CompiledGrowthRules rules)
    {
        var (app, save) = New(package, rules);
        Win(app);
        var run = app.ActiveRun!;
        var equipment = app.PendingOffer!;
        var relic = run.PendingRewardGroups[0];
        var gold = run.Gold;
        var materials = run.Growth!.Materials;
        var equipmentCount = run.EquipmentInventory.Count;
        Require(app.ResolveOffer(equipment.OfferId, null).Succeeded && run.PendingOffer == relic &&
            run.EquipmentInventory.Count == equipmentCount && run.Gold == gold && run.Growth.Materials == materials,
            "skip affects only this group and preserves the independent group and base resources");
        app = new RunApplication(package.Content, save, package.Project, rules);
        run = app.ActiveRun!;
        var before = Snapshot(run);
        save.FailWrites = true;
        Require(app.ResolveOffer(relic.OfferId, null).Failure == RunDecisionFailure.PersistenceFailed && Snapshot(run) == before,
            "failed last-group skip retains the remaining entitlement");
        save.FailWrites = false;
        Require(app.ResolveOffer(relic.OfferId, null).Succeeded && run.PendingOffer is null && run.PendingRewardGroups.IsEmpty &&
            run.FloorIndex == 1 && run.Gold == gold && run.Growth!.Materials == materials,
            "skip of final group finishes reward processing without compensation or extra advance");
        (app, _) = New(package, rules);
        run = app.ActiveRun!;
        var encounter = Prepare(app);
        var result = RunHealthFixture.Result(app.BuildBattleConfig(encounter, false), BattleOutcome.PlayerDefeat);
        gold = run.Gold;
        Require(app.ResolveBattle(result, encounter).Accepted && app.ActiveRun is not null && run.Gold == gold - result.GoldSpent &&
            run.PendingOffer is null && run.PendingRewardGroups.IsEmpty && run.FloorIndex == 1,
            "continuable defeat advances without any victory reward group");
    }

    private static void VerifyLegacyPendingReward(CompiledGamePackage package, CompiledGrowthRules rules)
    {
        var (app, save) = New(package, rules);
        var run = app.ActiveRun!;
        var persistence = new RunProgressionPersistenceService(package.Content, save, package.Project, rules);
        var decisions = new RunDecisionService(package.Content, package.Project, persistence);
        run.PendingOffer = decisions.CreateOffer(run, RunOfferKind.CombatReward);
        run.Version = 9;
        save.SaveActiveRun(run);
        var frozen = JsonSerializer.Serialize(run.PendingOffer);
        var oldJson = save.Json;
        save.FailWrites = true;
        var failed = new RunApplication(package.Content, save, package.Project, rules);
        Require(failed.ActiveRun is null && failed.ActiveRunLoadDiagnostic?.Kind == ActiveRunLoadFailureKind.MigrationPublicationFailed &&
            save.Json == oldJson, "failed schema upgrade preserves the historical snapshot");
        save.FailWrites = false;
        var restored = new RunApplication(package.Content, save, package.Project, rules);
        Require(restored.ActiveRun is not null && restored.ActiveRun.Version == ActiveRunFormationSchema.CurrentVersion &&
            restored.ActiveRun.PendingRewardGroups.IsEmpty && JsonSerializer.Serialize(restored.ActiveRun.PendingOffer) == frozen,
            "v9 mixed reward retains exactly its prior choices and single claim right without redraw or extra groups");
        var offer = restored.PendingOffer!;
        Require(restored.ResolveOffer(offer.OfferId, offer.Choices[0].StableId).Succeeded && restored.PendingOffer is null &&
            restored.ActiveRun!.PendingRewardGroups.IsEmpty, "one legacy claim cannot create newly designed rewards");
    }

    private static void VerifyUniqueRelicSupply(CompiledGamePackage package, CompiledGrowthRules rules)
    {
        var (app, save) = New(package, rules);
        var run = app.ActiveRun!;
        var relicId = package.Project.Campaign.ItemRewardPool.ContentIds.First(id => package.Content.TryGet(id, out var entry) &&
            entry.Definition is ItemDefinition { ProductKind: ItemProductKind.Relic, AllowRepeatAcquisition: false });
        Require(app.GrantItem(relicId), "owned default-unique relic fixture");
        var before = Snapshot(run);
        Require(!app.GrantItem(relicId) && !app.BuyItem(relicId) && Snapshot(run) == before,
            "duplicate primitive acquisition cannot charge or add a default-unique relic");
        var persistence = new RunProgressionPersistenceService(package.Content, save, package.Project, rules);
        var decisions = new RunDecisionService(package.Content, package.Project, persistence);
        Require(decisions.CreateCombatRewardGroups(run).SelectMany(group => group.Choices).SelectMany(choice => choice.Operations)
            .All(operation => operation.ContentId != relicId), "new reward groups exclude owned unique relics");
        var owned = new CompiledRunChoice("owned", "已有遗物", "", [], [],
            [new(RunOperationKind.GrantItem, 1, ContentId: relicId)], FailureOperations: [], ContentId: relicId);
        run.PendingOffer = new("legacy-owned-relic", RunOfferKind.CombatReward, "旧遗物机会", true, false,
            run.FloorIndex, run.BattleNumber, [owned]);
        save.SaveActiveRun(run);
        var stored = save.Json;
        var restored = new RunApplication(package.Content, save, package.Project, rules);
        Require(restored.PendingOffer is { Choices.IsEmpty: true, AllowSkip: true } && save.Json == stored,
            "read-only old snapshot projection hides owned relics without redraw or save-on-read");
        before = Snapshot(restored.ActiveRun!);
        Require(!restored.CheckOfferChoice(owned).Succeeded && !restored.ResolveOffer("legacy-owned-relic", "owned").Succeeded &&
            Snapshot(restored.ActiveRun!) == before, "stale unique relic claim is rejected again at consumption");
        Require(restored.ResolveOffer("legacy-owned-relic", null).Succeeded, "exhausted old relic opportunity can be skipped");
    }

    private static void VerifyShopStock(CompiledGamePackage package)
    {
        var save = new MemorySave();
        var app = new RunApplication(package.Content, save, package.Project);
        Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], 0x7720), "compatibility shop fixture");
        var run = app.ActiveRun!;
        run.Gold = 100;
        run.PendingNode = true;
        run.SelectedNode = TowerNodeType.Shop;
        var equipmentId = package.Project.Campaign.ItemRewardPool.ContentIds.First(id => package.Content.TryGet(id, out var entry) &&
            entry.Definition is ItemDefinition { ProductKind: ItemProductKind.Equipment });
        var first = new CompiledRunChoice("stock-a", "装备库存 A", "", [], [new(RunOperationKind.SpendGold, 2)],
            [new(RunOperationKind.GrantItem, 1, ContentId: equipmentId)], FailureOperations: [], ContentId: equipmentId);
        var second = first with { StableId = "stock-b", DisplayName = "装备库存 B" };
        run.PendingOffer = new("shop-stock", RunOfferKind.Shop, "固定现货", true, true, run.FloorIndex, run.BattleNumber, [first, second]);
        save.SaveActiveRun(run);
        var before = Snapshot(run);
        save.FailWrites = true;
        Require(app.ResolveOffer("shop-stock", "stock-a").Failure == RunDecisionFailure.PersistenceFailed && Snapshot(run) == before,
            "failed purchase does not consume stock or charge gold");
        save.FailWrites = false;
        var equipmentCount = run.EquipmentInventory.Count;
        Require(app.ResolveOffer("shop-stock", "stock-a").Succeeded && run.Gold == 98 &&
            run.EquipmentInventory.Count == equipmentCount + 1 && app.PendingOffer!.Choices.SequenceEqual([second]),
            "purchase consumes only its inventory slot and leaves remaining stock");
        var restored = new RunApplication(package.Content, save, package.Project);
        Require(!restored.ResolveOffer("shop-stock", "stock-a").Succeeded && restored.ActiveRun!.Gold == 98 &&
            restored.ResolveOffer("shop-stock", "stock-b").Succeeded && restored.ActiveRun.Gold == 96 &&
            restored.ActiveRun.EquipmentInventory.Count == equipmentCount + 2 &&
            restored.PendingOffer is { AllowSkip: true, Choices.IsEmpty: true },
            "reload cannot repurchase sold stock, while a distinct equipment copy remains legal");
        Require(restored.ResolveOffer("shop-stock", null).Succeeded && restored.ActiveRun!.FloorIndex == 1,
            "empty shop finishes normally once");
    }

    private static void VerifyInvalidQueue(CompiledGamePackage package, CompiledGrowthRules rules)
    {
        var (app, save) = New(package, rules);
        Win(app);
        var run = app.ActiveRun!;
        run.PendingRewardGroups = [run.PendingOffer!];
        save.SaveActiveRun(run);
        var stored = save.Json;
        Require(new RunApplication(package.Content, save, package.Project, rules).ActiveRun is null && save.Json == stored,
            "duplicate group identity cannot replay a consumed right through a corrupted queue");
        (app, save) = New(package, rules);
        Win(app);
        run = app.ActiveRun!;
        run.PendingOffer = run.PendingOffer! with { Choices = run.PendingRewardGroups[0].Choices };
        save.SaveActiveRun(run);
        stored = save.Json;
        Require(new RunApplication(package.Content, save, package.Project, rules).ActiveRun is null && save.Json == stored,
            "category mismatch rejects malformed saved entitlement without overwriting it");
    }

    private static (RunApplication App, MemorySave Save) New(CompiledGamePackage package, CompiledGrowthRules rules)
    {
        var save = new MemorySave();
        var app = new RunApplication(package.Content, save, package.Project, rules);
        Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], 0x7720), "new isolated growth fixture");
        return (app, save);
    }

    private static EncounterPlan Prepare(RunApplication app)
    {
        var options = app.CurrentOpponentOptions(TowerNodeType.Combat);
        Require(app.SelectNode(TowerNodeType.Combat, options.Count == 0 ? null : options[0].OptionId), "select fixture battle");
        var encounter = app.CurrentEncounter();
        Require(app.TryBeginGrowthBattle(encounter).Succeeded, "freeze genuine battle");
        return encounter;
    }

    private static void Win(RunApplication app)
    {
        var encounter = Prepare(app);
        Require(app.ResolveBattle(RunHealthFixture.Result(app.BuildBattleConfig(encounter, false), BattleOutcome.PlayerVictory), encounter).Accepted,
            "genuine victory fixture");
    }

    private static void VerifyCategory(CompiledGamePackage package, PendingRunOffer group, ItemProductKind kind) =>
        Require(!group.Choices.IsEmpty && group.Choices.All(choice => choice.Operations.Length == 1 &&
            package.Content.TryGet(choice.Operations[0].ContentId, out var entry) &&
            entry.Definition is ItemDefinition item && item.ProductKind == kind), "a category group contains only its own item kind");

    private static string Snapshot(ActiveRunDto run) => JsonSerializer.Serialize(run);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private sealed class MemorySave : IRunSaveService
    {
        public string? Json { get; private set; }
        public bool FailWrites { get; set; }
        public int SuccessfulWrites { get; private set; }
        public MetaProgressDto LoadMeta() => new();
        public SettingsDto LoadSettings() => new() { ReduceUiMotion = true };
        public ActiveRunDto? LoadActiveRun() => Json is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(Json);
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value)
        {
            if (FailWrites) return false;
            Json = Snapshot(value);
            SuccessfulWrites++;
            return true;
        }
        public void DeleteActiveRun() => Json = null;
    }
}
