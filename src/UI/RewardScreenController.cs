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
    private Control _offerBody = null!;
    private bool _claimed;

    public override void _Ready()
    {
        _title = GetNode<Label>("Center/Panel/Layout/Title");
        _hint = GetNode<Label>("Center/Panel/Layout/Hint");
        _choices = GetNode<Container>("Center/Panel/Layout/OfferBody/ChoiceScroll/Choices");
        _offerBody = GetNode<Control>("Center/Panel/Layout/OfferBody");
        _convert = GetNode<Button>("Center/Panel/Layout/ConvertButton");
        _continue = GetNode<Button>("Center/Panel/Layout/ContinueButton");
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
        _offerBody.Visible = true;
        _continue.Visible = true;
        _title.Text = "征募新兵";
        _hint.Text = "比较能力，选择一位加入队伍。";
        RunOfferCardBinder.SyncContent(_choices, app, app.RecruitmentChoices().Select(entry => entry.StableId),
            icons, OnChoice, "招募");
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
        _offerBody.Visible = true;
        _title.Text = "战斗胜利";
        _continue.Visible = true;
        _hint.Text = "选择一件战利品，补强你的队伍。";
        RunOfferCardBinder.SyncContent(_choices, app, app.ItemChoices(37).Select(entry => entry.StableId),
            icons, OnChoice, "领取");
        _convert.Visible = false;
        _continue.Text = "放弃战利品";
    }

    public void ShowClaimFailure() => _hint.Text = "战利品未能保存，尚未领取，请重试。";
    public void ShowDecisionMessage(string message) => _hint.Text = message;

    public void ShowTerminalFailure(string message)
    {
        _claimed = true;
        _title.Text = "征程结算待保存";
        _hint.Text = message;
        _offerBody.Visible = false;
        _convert.Visible = false;
        _continue.Visible = true;
        _continue.Text = "重试保存";
    }

    public void BindOffer(RunApplication app, PackedScene choiceTemplate, PackedScene itemTemplate, SemanticIconCatalog icons)
    {
        var offer = app.PendingOffer ?? throw new InvalidOperationException("No pending Run offer.");
        _claimed = false;
        _offerBody.Visible = true;
        _title.Text = offer.DisplayName;
        _hint.Text = "比较候选内容，再按卡片下方按钮确认。";
        if (offer.Kind == TowerAutobattler.Project.RunOfferKind.Recruitment)
            _hint.Text = offer.Choices.IsEmpty ? "本阶段已无可招募的新英雄，可以继续前进。" :
                $"第 {app.ActiveRun!.FloorIndex + 1} 层 · 本阶段的新英雄候选，已排除队伍与后备中持有的英雄。";
        RunOfferCardBinder.Sync(_choices, app, choiceTemplate, itemTemplate, icons, OnChoice);
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
