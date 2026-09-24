using System;
using System.Linq;
using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public sealed record RunOfferChoiceViewModel(
    string Id, string Title, string Identity, UnitPortraitDefinition? Portrait, Texture2D? Icon,
    bool IsUnit, string Health, string Damage, string Armor, string AttackRate,
    string Active, string Passive, string Effect, string Details, string Action,
    string Notice = "", bool Disabled = false);

// The card is an inspection surface. Only its explicit action button submits a Run command.
public partial class RunOfferChoiceCard : PanelContainer
{
    public string StableId { get; private set; } = string.Empty;
    public string TitleText => GetNode<Label>("Layout/Title").Text;
    public Button ConfirmButton => GetNode<Button>("Layout/ConfirmButton");
    public Button DetailsButton => GetNode<Button>("Layout/BodyScroll/Body/DetailsButton");
    public ScrollContainer BodyScroll => GetNode<ScrollContainer>("Layout/BodyScroll");
    public bool DetailsExpanded => DetailsButton.ButtonPressed;
    private Action<string>? _chosen;

    public override void _Ready()
    {
        ConfirmButton.Pressed += Confirm;
        DetailsButton.Toggled += ToggleDetails;
    }

    public override void _ExitTree()
    {
        ConfirmButton.Pressed -= Confirm;
        DetailsButton.Toggled -= ToggleDetails;
        _chosen = null;
    }

    public void Bind(RunOfferChoiceViewModel model, Action<string> chosen)
    {
        var changed = StableId != model.Id;
        StableId = model.Id;
        _chosen = chosen;
        GetNode<Label>("Layout/Title").Text = model.Title;
        GetNode<Label>("Layout/Identity").Text = model.Identity;
        GetNode<UnitPortrait>("Layout/Artwork/Portrait").Bind(model.Portrait, model.Icon);
        GetNode<Control>("Layout/Stats").Visible = model.IsUnit;
        GetNode<Label>("Layout/Stats/Health/Value").Text = model.Health;
        GetNode<Label>("Layout/Stats/Damage/Value").Text = model.Damage;
        GetNode<Label>("Layout/Stats/Armor/Value").Text = model.Armor;
        GetNode<Label>("Layout/Stats/AttackRate/Value").Text = model.AttackRate;
        BindCopy("Active", model.Active);
        BindCopy("Passive", model.Passive);
        BindCopy("Effect", model.Effect);
        var details = GetNode<CombatRichText>("Layout/BodyScroll/Body/Details");
        var usedText = model.Active + model.Passive + model.Effect;
        var glossary = details.Vocabulary.Terms.Where(term => usedText.Contains(term.Word, StringComparison.Ordinal))
            .Select(term => $"{term.Word}：{term.Explanation}").ToArray();
        details.Text = model.Details + (glossary.Length == 0 ? "" : "\n\n词条释义\n" + string.Join("\n\n", glossary));
        DetailsButton.Visible = !string.IsNullOrWhiteSpace(details.Text);
        ConfirmButton.Text = model.Action;
        ConfirmButton.Disabled = model.Disabled;
        var notice = GetNode<Label>("Layout/Notice");
        notice.Text = model.Notice;
        notice.Visible = !string.IsNullOrWhiteSpace(model.Notice);
        notice.ThemeTypeVariation = model.Disabled ? "FeedbackFailure" : "ChoiceFooter";
        if (changed)
        {
            DetailsButton.SetPressedNoSignal(false);
            ToggleDetails(false);
            BodyScroll.ScrollVertical = 0;
        }
    }

    public void SetAvailableHeight(float height)
    {
        CustomMinimumSize = new Vector2(280, Math.Max(360, height));
        GetNode<Control>("Layout/Artwork").CustomMinimumSize = new Vector2(0, Mathf.Clamp(height * .23f, 96, 168));
    }

    private void BindCopy(string name, string text)
    {
        var copy = GetNode<CombatRichText>("Layout/BodyScroll/Body/" + name);
        copy.Text = text;
        copy.Visible = !string.IsNullOrWhiteSpace(text);
    }

    private void ToggleDetails(bool expanded)
    {
        GetNode<Control>("Layout/BodyScroll/Body/Details").Visible = expanded;
        DetailsButton.Text = expanded ? "收起属性与规则  ▴" : "属性与规则  ▾";
    }

    private void Confirm()
    {
        if (!ConfirmButton.Disabled) _chosen?.Invoke(StableId);
    }
}
