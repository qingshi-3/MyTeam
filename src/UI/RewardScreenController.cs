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

    public override void _Ready()
    {
        _title = GetNode<Label>("Center/Panel/Layout/Title");
        _hint = GetNode<Label>("Center/Panel/Layout/Hint");
        _choices = GetNode<Container>("Center/Panel/Layout/ChoiceScroll/Choices");
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
        _title.Text = "战斗胜利";
        _hint.Text = "选择一件战利品。";
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
                ItemRarity: definition.Rarity);
        }).ToArray();
        ChoiceCardListBinder.SyncChoices(_choices, models, choiceTemplate, OnChoice);
        _convert.Visible = false;
        _continue.Text = "放弃战利品";
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

    private void OnChoice(string stableId) => ChoiceRequested?.Invoke(stableId);
    private void OnConvert() => ConvertRequested?.Invoke();
    private void OnContinue() => ContinueRequested?.Invoke();
}
