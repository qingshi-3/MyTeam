using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public sealed record ChoiceCardViewModel(
    string Id,
    string Title,
    string Description,
    string Footer,
    bool Disabled = false,
    Texture2D? Icon = null,
    StringName? TitleVariation = null,
    StringName? FooterVariation = null,
    StringName? FooterSemanticKey = null,
    PackedScene? Template = null,
    ItemRarity? ItemRarity = null,
    bool ShopItem = false);

public sealed record UnitChoiceCardViewModel(
    string Id,
    UnitDefinition Definition,
    string Description,
    string Meta,
    bool Disabled = false,
    StringName? NameVariation = null,
    StringName? MetaVariation = null,
    PackedScene? Template = null);

public static class ChoiceCardListBinder
{
    public static void SyncChoices(
        Container parent,
        IReadOnlyList<ChoiceCardViewModel> models,
        PackedScene defaultTemplate,
        Action<string> chosen)
    {
        var existing = parent.GetChildren().OfType<ChoiceCard>()
            .Where(card => !card.IsQueuedForDeletion())
            .ToDictionary(card => card.StableId, StringComparer.Ordinal);
        for (var index = 0; index < models.Count; index++)
        {
            var model = models[index];
            if (!existing.Remove(model.Id, out var card))
            {
                card = (model.Template ?? defaultTemplate).Instantiate<ChoiceCard>();
                parent.AddChild(card);
            }
            card.ConnectChosen(chosen);
            card.Bind(model.Id, model.Title, model.Description, model.Footer, model.Icon,
                model.TitleVariation, model.FooterVariation, model.FooterSemanticKey);
            if (card is ItemChoiceCard itemCard && model.ItemRarity is { } rarity)
                itemCard.BindItem(rarity, model.ShopItem);
            card.Disabled = model.Disabled;
            card.Visible = true;
            card.FocusMode = Control.FocusModeEnum.All;
            parent.MoveChild(card, index);
        }
        Retire(existing.Values);
    }

    public static void SyncUnits(
        Container parent,
        IReadOnlyList<UnitChoiceCardViewModel> models,
        PackedScene defaultTemplate,
        SemanticIconCatalog icons,
        Action<string> chosen)
    {
        var existing = parent.GetChildren().OfType<UnitChoiceCard>()
            .Where(card => !card.IsQueuedForDeletion())
            .ToDictionary(card => card.StableId, StringComparer.Ordinal);
        for (var index = 0; index < models.Count; index++)
        {
            var model = models[index];
            if (!existing.Remove(model.Id, out var card))
            {
                card = (model.Template ?? defaultTemplate).Instantiate<UnitChoiceCard>();
                parent.AddChild(card);
            }
            card.ConnectChosen(chosen);
            var definition = model.Definition;
            card.Bind(model.Id, definition,
                definition.Icon ?? icons.ResolveIcon(definition.IsHero
                    ? SemanticIconKeys.Hero
                    : definition.AttackRange > 3 ? SemanticIconKeys.Ranged : SemanticIconKeys.Melee),
                model.Description, model.Meta, model.NameVariation, model.MetaVariation);
            card.Disabled = model.Disabled;
            card.Visible = true;
            card.FocusMode = Control.FocusModeEnum.All;
            parent.MoveChild(card, index);
        }
        Retire(existing.Values);
    }

    private static void Retire<T>(IEnumerable<T> controls) where T : BaseButton
    {
        foreach (var control in controls)
        {
            control.Disabled = true;
            control.FocusMode = Control.FocusModeEnum.None;
            control.Visible = false;
            control.QueueFree();
        }
    }
}
