using System;
using System.Linq;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public partial class ShopScreenController : Control
{
    public event Action<string>? PurchaseRequested;
    public event Action? LeaveRequested;

    private SemanticChip _gold = null!;
    private Label _status = null!;
    private Container _choices = null!;
    private Button _leave = null!;

    public override void _Ready()
    {
        _gold = GetNode<SemanticChip>("Margin/Layout/Gold");
        _status = GetNode<Label>("Margin/Layout/Status");
        _choices = GetNode<Container>("Margin/Layout/Choices");
        _leave = GetNode<Button>("Margin/Layout/LeaveButton");
        _leave.Pressed += OnLeave;
    }

    public override void _ExitTree() => _leave.Pressed -= OnLeave;

    public void Bind(
        RunApplication app,
        PackedScene choiceTemplate,
        PackedScene itemTemplate,
        SemanticIconCatalog icons)
    {
        var run = app.ActiveRun ?? throw new InvalidOperationException("No active run for shop screen.");
        _gold.Bind(SemanticIconKeys.Gold, run.Gold.ToString(), "GoldValue");
        _status.Text = string.Empty;
        var models = app.ShopChoices(91).Select(entry =>
        {
            var definition = (ItemDefinition)entry.Definition;
            return new ChoiceCardViewModel(
                entry.StableId,
                definition.DisplayName,
                definition.Description,
                $"售价 {definition.Price}",
                Icon: definition.Icon ?? icons.ResolveIcon(SemanticIconKeys.Loot),
                FooterVariation: "HeroLabel",
                FooterSemanticKey: SemanticIconKeys.Gold,
                Template: itemTemplate,
                ItemRarity: definition.Rarity,
                ShopItem: true);
        }).ToArray();
        ChoiceCardListBinder.SyncChoices(_choices, models, choiceTemplate, OnPurchase);
    }

    public void ShowPurchaseResult(bool success)
    {
        _status.Text = success ? "购买成功。" : "金币不足。";
        _status.ThemeTypeVariation = success ? "FeedbackSuccess" : "FeedbackFailure";
    }

    private void OnPurchase(string stableId) => PurchaseRequested?.Invoke(stableId);
    private void OnLeave() => LeaveRequested?.Invoke();
}
