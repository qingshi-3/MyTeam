using System;
using System.Linq;
using System.Text.Json;
using Godot;
using TowerAutobattler.Composition;
using TowerAutobattler.Expedition;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

public partial class JourneyRestorationContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var result = await GrowthContentPackage.CreateReadyAsync(this);
            var package = result.Package ?? throw new InvalidOperationException(string.Join("; ", result.Report.CoreErrors));
            Require(package.DefaultNewRunProfileId == RunProfileIds.EightTraitExpedition, "new content has its own save identity");
            Require(package.Project.Campaign.RecruitmentPool.ContentIds.Order().SequenceEqual(
                AbilityPrototypeContent.UnitNumbers.Select(AbilityPrototypeContent.Id).Order()), "exact current playable roster");
            var expectedEquipmentIds = new[]
                { "v1_equipment_e02", "v1_equipment_e03", "v1_equipment_e06", "v1_equipment_e07", "v1_equipment_e10", "v1_equipment_e11" };
            Require(package.Project.Campaign.ItemRewardPool.ContentIds.Order(StringComparer.Ordinal)
                    .SequenceEqual(expectedEquipmentIds) &&
                package.Project.Campaign.ShopPool.ContentIds.Order(StringComparer.Ordinal)
                    .SequenceEqual(expectedEquipmentIds), "current equipment pool is available to shop and item rewards");
            var supply = package.Project.Campaign.RecruitmentSupply ?? throw new InvalidOperationException("Current recruitment supply missing.");
            Require(supply.TierByHero.Count == AbilityPrototypeContent.UnitNumbers.Length &&
                supply.TierByHero.Values.Count(tier => tier == 1) == 8 &&
                supply.TierByHero.Values.Count(tier => tier == 2) == 11 &&
                supply.OpeningTierWeights.SequenceEqual([100, 0]), "current E8/M11 supply with E-only opening");
            var expectedMIds = new[]
            {
                "prototype_u01", "prototype_u06", "prototype_u08", "prototype_u13", "prototype_u14",
                "prototype_u17", "prototype_u18", "prototype_u20", "prototype_u21", "prototype_u22", "prototype_u25"
            };
            Require(supply.TierByHero.Where(pair => pair.Value == 2).Select(pair => pair.Key)
                    .Order(StringComparer.Ordinal).SequenceEqual(expectedMIds.Order(StringComparer.Ordinal)) &&
                package.GrowthRules!.FirstDiscoveryPool.Order(StringComparer.Ordinal)
                    .SequenceEqual(expectedMIds.Order(StringComparer.Ordinal)) &&
                package.GrowthRules!.AdvancedDiscoveryPool.Order(StringComparer.Ordinal)
                    .SequenceEqual(expectedMIds.Order(StringComparer.Ordinal)), "both discovery pools match the exact M11 roster");
            var ascensionBenefitIds = package.GrowthRules!.Heroes
                .Where(pair => pair.Value.AscensionBenefit is not null)
                .Select(pair => pair.Key)
                .ToHashSet(StringComparer.Ordinal);
            Require(ascensionBenefitIds.SetEquals(new[]
                { "prototype_u02", "prototype_u07", "prototype_u11", "prototype_u17", "prototype_u18", "prototype_u19", "prototype_u21", "prototype_u22" }),
                "the eight authored ascension benefits match U02, U07, U11, U17, U18, U19, U21 and U22");

            var save = new ExpeditionRunMemorySave();
            var app = new RunApplication(package, save, "tests/journey-restoration");
            Require(app.BeginOpeningRecruitment(20261006), "new opening draft");
            var candidates = app.ActiveRun!.OpeningRecruitment!.CandidateIds;
            Require(candidates.Length == 6 && candidates.Distinct().Count() == 6 &&
                candidates.All(id => id.StartsWith("prototype_u", StringComparison.Ordinal)), "six current distinct heroes");
            Require(app.ToggleOpeningHero(candidates[0]), "select first hero");
            var draft = save.Json;
            var writes = save.SuccessfulRunWrites;
            var restored = new RunApplication(package, save, "tests/journey-restoration");
            Require(save.Json == draft && save.SuccessfulRunWrites == writes &&
                restored.ActiveRun!.OpeningRecruitment!.SelectedIds.SequenceEqual([candidates[0]]), "continue preserves draft without writes");
            save.FailWrites = true;
            Require(!restored.BeginOpeningRecruitment(4) && save.Json == draft, "failed new game preserves draft");
            save.FailWrites = false;
            Require(restored.ToggleOpeningHero(candidates[1]) && restored.ConfirmOpeningRecruitment(), "confirm into real expedition");
            var run = restored.ActiveRun!;
            Require(run.Expedition is not null && run.Roster.Count == 2 && run.OpeningRecruitment is null, "map and roster persisted");
            var map = (CompiledExpedition)package.Project.Campaign.Progression;
            ExpeditionSiteDto? shopToCancel = null;
            foreach (var site in run.Expedition!.Sites)
            {
                var kind = map.Sites.Single(x => x.StableId == site.DefinitionId).Kind;
                if (kind == TowerNodeType.Shop)
                {
                    var shopOffer = site.ServiceOffer ?? throw new InvalidOperationException("frozen shop offer missing: " + site.SiteId);
                    var stockIds = shopOffer.Choices.Select(choice => choice.ContentId).ToArray();
                    Require(shopOffer.Kind == RunOfferKind.Shop && shopOffer.AllowSkip && shopOffer.Repeatable &&
                        stockIds.Length == package.Project.RunRules.ItemChoiceCount &&
                        stockIds.Distinct(StringComparer.Ordinal).Count() == stockIds.Length &&
                        stockIds.All(expectedEquipmentIds.Contains), "shop freezes distinct real equipment stock and allows leaving");
                    shopToCancel ??= site;
                }
                if (kind == TowerNodeType.Recruitment) Require(site.ServiceOffer!.Choices.Length == 3, "recruitment uses three candidates");
            }
            var shop = shopToCancel ?? throw new InvalidOperationException("map has no frozen shop site.");
            var shopDefinition = map.Sites.Single(x => x.StableId == shop.DefinitionId);
            for (var step = 0; step < run.Expedition!.Cells.Count && !restored.CanInteractExpeditionSite(shop.SiteId); step++)
            {
                var expedition = run.Expedition;
                var approaches = ExpeditionNavigation.Neighbors(expedition.Size, shop.CellIndex).Append(shop.CellIndex)
                    .Where(index => expedition.Cells[index].Revealed && !ExpeditionNavigation.IsBlocked(expedition, index))
                    .Select(index => new { Index = index, Path = ExpeditionNavigation.FindKnownPath(expedition, index) })
                    .Where(value => !value.Path.IsEmpty).OrderBy(value => value.Path.Length).ToArray();
                if (approaches.Length > 0)
                {
                    foreach (var cell in approaches[0].Path)
                        Require(restored.MoveExpedition(cell), "known legal route moves beside the published shop");
                    continue;
                }
                var frontier = expedition.Cells.Where(cell => cell.Revealed && cell.Walkable &&
                        !ExpeditionNavigation.IsBlocked(expedition, cell.Index) &&
                        ExpeditionNavigation.RevealsNewCells(expedition, cell.Index))
                    .Select(cell => ExpeditionNavigation.FindKnownPath(expedition, cell.Index))
                    .Where(path => !path.IsEmpty).OrderBy(path => path.Length).FirstOrDefault();
                Require(!frontier.IsEmpty && restored.MoveExpedition(frontier[^1]),
                    "legal map movement reveals the frozen shop without spending an action");
            }
            Require(restored.CanInteractExpeditionSite(shop.SiteId) && shopDefinition.Kind == TowerNodeType.Shop,
                "shop is reached through the generated map before preview");
            var beforeShopPreview = JsonSerializer.Serialize(run);
            var shopWritesBefore = save.SuccessfulRunWrites;
            Require(restored.InspectExpeditionSite(shop.SiteId), "inspect reached shop");
            var preview = restored.PreviewExpeditionSiteOffer(shop.SiteId) ?? throw new InvalidOperationException("shop preview missing");
            Require(preview.Kind == RunOfferKind.Shop && preview.AllowSkip &&
                preview.Choices.Select(choice => choice.ContentId).SequenceEqual(shop.ServiceOffer!.Choices.Select(choice => choice.ContentId)),
                "shop preview uses the exact frozen stock");
            Require(restored.ResolveOffer(preview.OfferId, null).Succeeded &&
                JsonSerializer.Serialize(restored.ActiveRun) == beforeShopPreview && save.Json == beforeShopPreview &&
                save.SuccessfulRunWrites == shopWritesBefore,
                "leaving the shop preview preserves the complete saved run and frozen stock without a write");
            var snapshot = JsonSerializer.Serialize(run);
            writes = save.SuccessfulRunWrites;
            var continued = new RunApplication(package, save, "tests/journey-restoration");
            Require(JsonSerializer.Serialize(continued.ActiveRun) == snapshot && save.SuccessfulRunWrites == writes,
                "continue restores entire map and inventories without writes");
            var previousProtocol = save.LoadActiveRun()!;
            previousProtocol.ProfileCompatibilityVersion = package.DefaultProfile.CompatibilityVersion - 1;
            save.SetFixtureJson(JsonSerializer.Serialize(previousProtocol));
            var protocolJson = save.Json;
            var refusedProtocol = new RunApplication(package, save, "tests/journey-restoration");
            Require(refusedProtocol.ActiveRun is null && refusedProtocol.ActiveRunLoadDiagnostic is not null && save.Json == protocolJson &&
                save.SuccessfulRunWrites == writes, "previous breeding protocol is explicitly refused and preserved");
            save.SetFixtureJson(snapshot);
            var incompatible = save.LoadActiveRun()!;
            incompatible.ProfileId = RunProfileIds.GrowthExpedition;
            save.SetFixtureJson(JsonSerializer.Serialize(incompatible));
            var old = save.Json;
            var rejected = new RunApplication(package, save, "tests/journey-restoration");
            Require(rejected.ActiveRun is null && rejected.ActiveRunLoadDiagnostic is not null && save.Json == old &&
                save.SuccessfulRunWrites == writes, "retired save is rejected without replacing it");
            GD.Print("JOURNEY_RESTORATION_CONTRACT_OK ready current-heroes equipment-pool E8-M11-opening exact-discovery-pools eight-ascension-benefits opening map recruitment published-shop-preview-cancel save-atomicity exact-continue old-save-protected");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("JOURNEY_RESTORATION_CONTRACT_FAILED " + error); GetTree().Quit(1); }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
