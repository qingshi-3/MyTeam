using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public static class RunOfferCardBinder
{
    public static void Sync(Container parent, RunApplication app, PackedScene choiceTemplate,
        PackedScene itemTemplate, SemanticIconCatalog icons, Action<string> chosen, bool shop = false,
        Action<string>? inspected = null)
    {
        var offer = app.PendingOffer ?? throw new InvalidOperationException("No pending Run offer.");
        var choices = new List<ChoiceCardViewModel>();
        var units = new List<UnitChoiceCardViewModel>();
        string Name(string id) => app.Content.TryGet(id, out var entry) ? entry.Definition switch
        {
            UnitDefinition unit => unit.DisplayName,
            ItemDefinition item => item.DisplayName,
            _ => id
        } : id;
        foreach (var choice in offer.Choices)
        {
            var eligibility = app.CheckOfferChoice(choice);
            var description = RunDecisionText.Describe(choice, Name);
            var footer = eligibility.Succeeded
                ? choice.Costs.IsDefaultOrEmpty ? "选择并领取" :
                    choice.Costs.All(cost => cost.Kind == TowerAutobattler.Project.RunOperationKind.SpendGold)
                        ? RunDecisionText.Costs(choice, Name) : "需支付代价 · 查看详情"
                : eligibility.Message;
            var definition = !string.IsNullOrEmpty(choice.ContentId) && app.Content.TryGet(choice.ContentId, out var entry)
                ? entry.Definition : null;
            if (definition is UnitDefinition unit)
            {
                units.Add(new(choice.StableId, unit, description, footer,
                    Disabled: !eligibility.Succeeded, MetaVariation: "PlayerLabel"));
                continue;
            }
            var item = definition as ItemDefinition;
            choices.Add(new(choice.StableId, choice.DisplayName, description, footer,
                Disabled: !eligibility.Succeeded, Icon: item?.Icon ?? icons.ResolveIcon(SemanticIconKeys.Loot),
                Template: item is null ? choiceTemplate : itemTemplate, ItemRarity: item?.Rarity,
                ShopItem: shop, ProductKind: item?.ProductKind));
        }
        ChoiceCardListBinder.SyncMixed(parent, choices, units, offer.Choices.Select(choice => choice.StableId).ToArray(),
            choiceTemplate, app.Project.Presentation.UnitChoiceCard, icons, chosen);
        // Unit cards intentionally retain their compact two-line authored body.
        // Hover/focus exposes the complete rule text in the screen's existing detail label.
        var details = offer.Choices.ToDictionary(choice => choice.StableId,
            choice => choice.DisplayName + "\n" + RunDecisionText.Describe(choice, Name), StringComparer.Ordinal);
        void Inspect(string id) { if (details.TryGetValue(id, out var text)) inspected?.Invoke(text); }
        foreach (var child in parent.GetChildren().Where(child => !child.IsQueuedForDeletion()))
        {
            if (child is UnitChoiceCard unit) unit.ConnectInspected(Inspect);
            else if (child is ChoiceCard choice) choice.ConnectInspected(Inspect);
        }
    }
}
