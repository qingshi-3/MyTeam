using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public static class RunOfferCardBinder
{
    private const string CardScenePath = "res://scenes/ui/components/RunOfferChoiceCard.tscn";

    // Existing screen binding signature remains stable; offer cards now own a separate confirmation button.
    public static void Sync(Container parent, RunApplication app, PackedScene choiceTemplate,
        PackedScene itemTemplate, SemanticIconCatalog icons, Action<string> chosen, bool shop = false,
        Action<string>? inspected = null)
    {
        var offer = app.PendingOffer ?? throw new InvalidOperationException("No pending Run offer.");
        string Name(string id) => app.Content.TryGet(id, out var entry) ? entry.Definition switch
        {
            UnitDefinition unit => unit.DisplayName,
            ItemDefinition item => item.DisplayName,
            _ => id
        } : id;
        var models = offer.Choices.Select(choice =>
        {
            var eligibility = app.CheckOfferChoice(choice);
            var definition = app.Content.TryGet(choice.ContentId, out var entry) ? entry.Definition : null;
            var contentDescription = definition switch
            {
                UnitDefinition unit => unit.Description,
                ItemDefinition item => item.Description,
                _ => string.Empty
            };
            var decision = choice with { Description = string.IsNullOrWhiteSpace(contentDescription)
                ? choice.Description : choice.Description.Replace(contentDescription, string.Empty, StringComparison.Ordinal).Trim() };
            var rules = RunDecisionText.Describe(decision, Name);
            var costs = choice.Costs.IsDefaultOrEmpty ? "" :
                choice.Costs.All(cost => cost.Kind == RunOperationKind.SpendGold)
                    ? $"{choice.Costs.Sum(cost => cost.Amount)} 金币" : RunDecisionText.Costs(choice, Name);
            var action = shop ? "购买" : definition is UnitDefinition ? "招募" : definition is ItemDefinition ? "领取" : "选择";
            if (shop && choice.Costs.IsDefaultOrEmpty) action = "领取";
            var notice = costs;
            if (choice.SuccessChance < 1)
                notice = $"成功率 {choice.SuccessChance:P0}" + (costs.Length == 0 ? "" : $" · 无论成败支付 {costs}");
            if (!eligibility.Succeeded)
                notice = eligibility.Message + (costs.Length == 0 ? "" : $"\n代价：{costs}");
            return RunOfferDetailText.Card(app, choice.ContentId, choice.StableId, choice.DisplayName,
                action, rules, notice, !eligibility.Succeeded, icons.ResolveIcon(SemanticIconKeys.Loot));
        }).ToArray();
        SyncModels(parent, models, chosen);
        if (models.Length > 0) inspected?.Invoke(models[0].Title);
    }

    public static void SyncContent(Container parent, RunApplication app, IEnumerable<string> contentIds,
        SemanticIconCatalog icons, Action<string> chosen, string action)
    {
        var models = contentIds.Select(id => RunOfferDetailText.Card(app, id, id, id, action,
            "", "", false, icons.ResolveIcon(SemanticIconKeys.Loot))).ToArray();
        SyncModels(parent, models, chosen);
    }

    private static void SyncModels(Container parent, IReadOnlyList<RunOfferChoiceViewModel> models, Action<string> chosen)
    {
        var existing = parent.GetChildren().OfType<RunOfferChoiceCard>().Where(card => !card.IsQueuedForDeletion())
            .ToDictionary(card => card.StableId, StringComparer.Ordinal);
        foreach (var child in parent.GetChildren().Where(child => child is not RunOfferChoiceCard))
        {
            parent.RemoveChild(child);
            child.QueueFree();
        }
        var scene = GD.Load<PackedScene>(CardScenePath);
        for (var index = 0; index < models.Count; index++)
        {
            var model = models[index];
            if (!existing.Remove(model.Id, out var card))
            {
                card = scene.Instantiate<RunOfferChoiceCard>();
                parent.AddChild(card);
            }
            card.Bind(model, chosen);
            parent.MoveChild(card, index);
        }
        foreach (var stale in existing.Values)
        {
            parent.RemoveChild(stale);
            stale.QueueFree();
        }
        if (parent is RunOfferChoiceGrid grid) grid.RefreshLayout();
    }
}
