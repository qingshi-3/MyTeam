using System;
using System.Linq;
using Godot;
using TowerAutobattler.Components;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public partial class RewardScreenController : Control
{
    public event Action<string>? ChoiceRequested;
    public event Action? ConvertRequested;
    public event Action? ContinueRequested;

    private Label _title = null!;
    private Label _hint = null!;
    private Container _choices = null!;
    private Button _convert = null!;
    private Button _continue = null!;
    private EquipmentLoadoutPanel? _equipmentPanel;
    private bool _claimed;

    public override void _Ready()
    {
        _title = GetNode<Label>("Center/Panel/Layout/Title");
        _hint = GetNode<Label>("Center/Panel/Layout/Hint");
        _choices = GetNode<Container>("Center/Panel/Layout/ChoiceScroll/Choices");
        _convert = GetNode<Button>("Center/Panel/Layout/ConvertButton");
        _continue = GetNode<Button>("Center/Panel/Layout/ContinueButton");
        _equipmentPanel = GetNodeOrNull<EquipmentLoadoutPanel>("%RewardEquipmentPanel");
        _convert.Pressed += OnConvert;
        _continue.Pressed += OnContinue;
    }

    public override void _ExitTree()
    {
        _convert.Pressed -= OnConvert;
        _continue.Pressed -= OnContinue;
    }

    public void BindRecruitment(
        RunApplication app,
        PackedScene unitTemplate,
        SemanticIconCatalog icons)
    {
        _claimed = false;
        _choices.GetParent<Control>().Visible = true;
        _title.Text = "征募新兵";
        _hint.Text = "选择一名士兵加入军团。";
        var models = app.RecruitmentChoices().Select(entry =>
        {
            var definition = (UnitDefinition)entry.Definition;
            return new UnitChoiceCardViewModel(
                entry.StableId,
                definition,
                definition.Description,
                "加入军团",
                MetaVariation: "PlayerLabel");
        }).ToArray();
        ChoiceCardListBinder.SyncUnits(_choices, models, unitTemplate, icons, OnChoice);
        var conversion = CurrentHeroConversion(app);
        _convert.Visible = conversion > 0;
        _convert.Text = $"转为 {conversion} 金币";
        _continue.Text = "跳过征募";
    }

    public void BindCombatReward(
        RunApplication app,
        PackedScene choiceTemplate,
        PackedScene itemTemplate,
        SemanticIconCatalog icons)
    {
        _claimed = false;
        _choices.GetParent<Control>().Visible = true;
        if (_equipmentPanel is not null) _equipmentPanel.Visible = false;
        _title.Text = "战斗胜利";
        _hint.Text = "带走一件战利品 · 装备交给英雄，遗物作用于军团。";
        var models = app.ItemChoices(37).Select(entry =>
        {
            var definition = (ItemDefinition)entry.Definition;
            return new ChoiceCardViewModel(
                entry.StableId,
                definition.DisplayName,
                definition.Description,
                PlayerFacingText.DescribeItemRarity(definition.Rarity),
                Icon: definition.Icon ?? icons.ResolveIcon(SemanticIconKeys.Loot),
                TitleVariation: definition.Rarity == ItemRarity.Legendary ? new StringName("HeroLabel") : null,
                FooterVariation: definition.Rarity == ItemRarity.Legendary ? "HeroLabel" : "PlayerLabel",
                Template: itemTemplate,
                ItemRarity: definition.Rarity,
                ProductKind: definition.ProductKind);
        }).ToArray();
        ChoiceCardListBinder.SyncChoices(_choices, models, choiceTemplate, OnChoice);
        _convert.Visible = false;
        _continue.Text = "放弃战利品";
    }

    public void ShowClaimed(RunApplication app, string itemId)
    {
        if (!app.Content.TryGet(itemId, out var entry) || entry.Definition is not ItemDefinition item) return;
        _claimed = true;
        _title.Text = $"获得 · {item.DisplayName}";
        _hint.Text = item.ProductKind == ItemProductKind.Equipment
            ? "装备已收入背包。现在交给合适的英雄，或留到下一战准备时调整。"
            : "遗物已加入军团。本次整备可顺手调整英雄装备。";
        _choices.GetParent<Control>().Visible = false;
        _equipmentPanel?.Bind(app);
        _continue.Text = "收好，继续旅程";
        _continue.GrabFocus();
    }

    public void ShowClaimFailure() => _hint.Text = "战利品未能保存，尚未领取，请重试。";
    public void ShowDecisionMessage(string message) => _hint.Text = message;
    public void ShowResolved(RunApplication app, string title, RunDecisionResult result)
    {
        _claimed = true;
        _title.Text = title;
        string Name(string id) => app.Content.TryGet(id, out var entry) ? entry.Definition switch
        { UnitDefinition unit => unit.DisplayName, ItemDefinition item => item.DisplayName, _ => "内容" } : "内容";
        _hint.Text = result.Message + "\n" + string.Join("\n", result.Changes.Select(change => change.Kind switch
        {
            RunChangeKind.Gold => $"金币 {change.Before:0} → {change.After:0}",
            RunChangeKind.Population => $"人口 {change.Before:0} → {change.After:0}",
            RunChangeKind.PopulationCap => $"额外人口上限 {change.Before:0} → {change.After:0}",
            RunChangeKind.HeroAdded => $"{Name(change.ContentId)} 加入队伍",
            RunChangeKind.ItemAdded => $"获得 {Name(change.ContentId)} × {change.After:0}",
            RunChangeKind.HeroHealth => $"{Name(change.ContentId)} 生命 {change.Before:P0} → {change.After:P0}",
            _ => ""
        }));
        _choices.GetParent<Control>().Visible = false;
        _convert.Visible = false;
        _continue.Visible = true;
        _continue.Text = "继续旅程";
        _equipmentPanel?.Bind(app);
        _continue.GrabFocus();
    }

    public void ShowTerminalFailure(string message)
    {
        _claimed = true;
        _title.Text = "征程结算待保存";
        _hint.Text = message;
        _choices.GetParent<Control>().Visible = false;
        _convert.Visible = false;
        if (_equipmentPanel is not null) _equipmentPanel.Visible = false;
        _continue.Visible = true;
        _continue.Text = "重试保存";
    }

    public void BindOffer(RunApplication app, PackedScene choiceTemplate, PackedScene itemTemplate, SemanticIconCatalog icons)
    {
        var offer = app.PendingOffer ?? throw new InvalidOperationException("No pending Run offer.");
        _claimed = false;
        _choices.GetParent<Control>().Visible = true;
        if (_equipmentPanel is not null) _equipmentPanel.Visible = false;
        _title.Text = offer.DisplayName;
        _hint.Text = "选择一项继续 · 悬停或用键盘聚焦卡片，查看完整条件、代价与结果。";
        RunOfferCardBinder.Sync(_choices, app, choiceTemplate, itemTemplate, icons, OnChoice,
            inspected: text => _hint.Text = text);
        _convert.Visible = false;
        _continue.Visible = offer.AllowSkip;
        _continue.Text = "放弃此次机会，继续";
    }

    private static int CurrentHeroConversion(RunApplication app)
    {
        if (app.ActiveRun is not { Roster.Count: > 0 } run ||
            !app.Content.TryGet(run.Roster[0].ContentId, out var entry))
            return 0;
        var root = entry.Scene.Instantiate<UnitContentRoot>();
        try { return root.HeroRule?.RecruitConversionGold ?? 0; }
        finally { root.Free(); }
    }

    private void OnChoice(string stableId) { if (!_claimed) ChoiceRequested?.Invoke(stableId); }
    private void OnConvert() => ConvertRequested?.Invoke();
    private void OnContinue() => ContinueRequested?.Invoke();
}
