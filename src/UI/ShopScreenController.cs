using System;
using Godot;
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
        _choices = GetNode<Container>("Margin/Layout/OfferBody/ChoiceScroll/Choices");
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
        RunOfferCardBinder.Sync(_choices, app, choiceTemplate, itemTemplate, icons, OnPurchase, shop: true);
    }

    public void ShowPurchaseResult(bool success)
    {
        _status.Text = success ? "已收入军团 · 装备可在队伍整备或战前部署时穿戴。" : "未能购买：金币不足或保存失败，原物品与金币保持不变。";
        _status.ThemeTypeVariation = success ? "FeedbackSuccess" : "FeedbackFailure";
    }
    public void ShowDecisionResult(RunDecisionResult result)
    {
        _status.Text = result.Message;
        _status.ThemeTypeVariation = result.Succeeded ? "FeedbackSuccess" : "FeedbackFailure";
    }

    private void OnPurchase(string stableId) => PurchaseRequested?.Invoke(stableId);
    private void OnLeave() => LeaveRequested?.Invoke();
}
