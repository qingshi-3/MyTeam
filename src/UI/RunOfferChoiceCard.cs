using System;
using System.Linq;
using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public sealed record RunOfferChoiceViewModel(
    string Id, string Title, string Identity, UnitPortraitDefinition? Portrait, Texture2D? Icon,
    bool IsUnit, string Health, string Damage, string Armor, string AttackRate,
    string Active, string Passive, string Effect, string Details, string Action,
    string Notice = "", bool Disabled = false, string ActiveName = "主动技能", string PassiveName = "被动技能",
    int Tier = 0, Texture2D? RoleIcon = null, Texture2D? DeliveryIcon = null, string Range = "", bool IsOpportunity = false);

// The card is an inspection surface. Only its explicit action button submits a Run command.
public partial class RunOfferChoiceCard : PanelContainer
{
    public string StableId { get; private set; } = string.Empty;
    public string TitleText => GetNode<Label>("Layout/Title").Text;
    public Button ConfirmButton => GetNode<Button>("Layout/Footer/ConfirmButton");
    public Button DetailsButton => GetNode<Button>("Layout/Footer/DetailsButton");
    public ScrollContainer BodyScroll => GetNode<ScrollContainer>("Layout/BodyScroll");
    public bool DetailsExpanded => DetailsButton.ButtonPressed;
    public bool IsUnit { get; private set; }
    private Action<string>? _chosen;

    public override void _Ready()
    {
        ConfirmButton.Pressed += Confirm;
        DetailsButton.Toggled += ToggleDetails;
        GetNode<Button>("Layout/Skills/Active").Pressed += OpenDetails;
        GetNode<Button>("Layout/Skills/Passive").Pressed += OpenDetails;
        GetNode<Button>("Layout/Artwork/PrimaryAttack").Pressed += OpenDetails;
        GetNode<Button>("Layout/Artwork/PrimaryHealth").Pressed += OpenDetails;
        BodyScroll.GetVScrollBar().ThemeTypeVariation = "CardScrollBar";
    }

    public override void _ExitTree()
    {
        ConfirmButton.Pressed -= Confirm;
        DetailsButton.Toggled -= ToggleDetails;
        GetNode<Button>("Layout/Skills/Active").Pressed -= OpenDetails;
        GetNode<Button>("Layout/Skills/Passive").Pressed -= OpenDetails;
        GetNode<Button>("Layout/Artwork/PrimaryAttack").Pressed -= OpenDetails;
        GetNode<Button>("Layout/Artwork/PrimaryHealth").Pressed -= OpenDetails;
        _chosen = null;
    }

    public void Bind(RunOfferChoiceViewModel model, Action<string> chosen)
    {
        var changed = StableId != model.Id;
        StableId = model.Id;
        IsUnit = model.IsUnit;
        _chosen = chosen;
        GetNode<Label>("Layout/Title").Text = model.Title;
        GetNode<Label>("Layout/DetailHeading").Text = model.Title;
        GetNode<Label>("Layout/Artwork/ArtTitle").Text = model.Title;
        var identity = GetNode<Control>("Layout/Artwork/ArtIdentity");
        GetNode<Control>("Layout/Artwork/ArtIdentity/TierSymbols").TooltipText = model.Identity;
        GetNode<Control>("Layout/Artwork/ArtIdentity/RoleSymbols").TooltipText = model.Identity;
        identity.AccessibilityName = model.Identity;
        GetNode<Label>("Layout/Artwork/ArtIdentity/TierSymbols/Tier").Text = model.Tier > 0 ? model.Tier.ToString() : "";
        GetNode<Control>("Layout/Artwork/ArtIdentity/TierSymbols/Star").Visible = model.Tier > 0;
        GetNode<TextureRect>("Layout/Artwork/ArtIdentity/RoleSymbols/Role").Texture = model.RoleIcon;
        GetNode<TextureRect>("Layout/Artwork/ArtIdentity/RoleSymbols/Delivery").Texture = model.DeliveryIcon;
        GetNode<Label>("Layout/BodyScroll/Body/DetailTitle").Text = model.Title + " · " + model.Identity;
        GetNode<Control>("Layout/Title").Visible = !model.IsUnit;
        GetNode<Control>("Layout/Identity").Visible = !model.IsUnit;
        GetNode<Control>("Layout/Artwork/ArtTitle").Visible = model.IsUnit;
        GetNode<Control>("Layout/Artwork/ArtIdentity").Visible = model.IsUnit;
        GetNode<Control>("Layout/Skills").Visible = model.IsUnit;
        GetNode<Control>("Layout/BodyScroll/Body/DetailTitle").Visible = model.IsUnit;
        GetNode<Label>("Layout/Identity").Text = model.Identity;
        GetNode<UnitPortrait>("Layout/Artwork/Portrait").Bind(model.Portrait, model.Icon);
        GetNode<UnitPortrait>("Layout/Artwork/Portrait").Visible = !model.IsOpportunity;
        var opportunityIcon = GetNode<TextureRect>("Layout/Artwork/OpportunityIcon");
        opportunityIcon.Texture = model.Icon;
        opportunityIcon.Visible = model.IsOpportunity;
        GetNode<Control>("Layout/Stats").Visible = model.IsUnit;
        GetNode<Label>("Layout/Stats/Health/Value").Text = model.Health;
        GetNode<Label>("Layout/Stats/Damage/Value").Text = model.Damage;
        GetNode<Label>("Layout/Stats/Armor/Value").Text = model.Armor;
        GetNode<Label>("Layout/Stats/AttackRate/Value").Text = model.AttackRate;
        GetNode<Label>("Layout/Stats/Range/Value").Text = model.Range;
        BindPrimary("PrimaryAttack", model.IsUnit, SemanticIconKeys.Attack, model.Damage, "基础攻击", "基础攻击力；装备与开战加成见整备详情。");
        BindPrimary("PrimaryHealth", model.IsUnit, SemanticIconKeys.Health, model.Health, "生命上限", "基础生命上限；每战满血入场，装备与开战加成见整备详情。");
        BindCopy("Active", model.Active);
        BindCopy("Passive", model.Passive);
        BindCopy("Effect", model.Effect);
        BindSkill("Active", model.ActiveName, model.Active);
        BindSkill("Passive", model.PassiveName, model.Passive);
        var details = GetNode<CombatRichText>("Layout/BodyScroll/Body/Details");
        var usedText = model.Active + model.Passive + model.Effect;
        var glossary = details.Vocabulary.Terms.Where(term => usedText.Contains(term.Word, StringComparison.Ordinal))
            .Select(term => $"{term.Word}：{term.Explanation}").ToArray();
        details.Text = model.Details + (glossary.Length == 0 ? "" : "\n\n词条释义\n" + string.Join("\n\n", glossary));
        DetailsButton.Visible = model.IsUnit || !string.IsNullOrWhiteSpace(details.Text);
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
        ToggleDetails(DetailsExpanded);
    }

    public void SetAvailableHeight(float height)
    {
        CustomMinimumSize = new Vector2(280, Math.Max(360, height));
        GetNode<Control>("Layout/Artwork").CustomMinimumSize = new Vector2(0,
            IsUnit ? Math.Max(280, height * .67f) : Mathf.Clamp(height * .25f, 112, 144));
        GetNode<Control>("Layout/Artwork").SizeFlagsVertical = IsUnit ? SizeFlags.ExpandFill : SizeFlags.Fill;
    }

    private void BindCopy(string name, string text)
    {
        var copy = GetNode<CombatRichText>("Layout/BodyScroll/Body/" + name);
        copy.Text = text;
        copy.Visible = !string.IsNullOrWhiteSpace(text);
    }

    private void ToggleDetails(bool expanded)
    {
        GetNode<Control>("Layout/Stats/Health").Visible = expanded;
        GetNode<Control>("Layout/Stats/Damage").Visible = expanded;
        GetNode<Control>("Layout/Stats/Range").Visible = !expanded;
        GetNode<Control>("Layout/BodyScroll/Body/Details").Visible = expanded;
        GetNode<Control>("Layout/Artwork").Visible = !IsUnit || !expanded;
        BodyScroll.Visible = !IsUnit || expanded;
        GetNode<Control>("Layout/Skills").Visible = IsUnit && !expanded;
        GetNode<Control>("Layout/DetailHeading").Visible = IsUnit && expanded;
        DetailsButton.Text = expanded ? (IsUnit ? "返回立绘" : "收起详情") : "详情";
    }

    private void BindPrimary(string path, bool visible, StringName icon, string value, string caption, string explanation)
    {
        var fact = GetNode<CompactDetailFact>("Layout/Artwork/" + path);
        fact.Visible = visible;
        fact.Bind(icon, value, caption, explanation);
        fact.AccessibilityName = caption + " " + value;
        BattleLabHoverHint.Bind(fact, visible ? new BattleLabTooltipInfo(caption + " " + value, Stats: explanation) : null);
    }

    private void OpenDetails()
    {
        DetailsButton.ButtonPressed = true;
        BodyScroll.ScrollVertical = 0;
        DetailsButton.GrabFocus();
    }

    private void BindSkill(string path, string name, string body)
    {
        var button = GetNode<Button>("Layout/Skills/" + path);
        if (button is CardSkillButton cardSkill) cardSkill.BindName(name);
        else button.Text = name;
        button.Visible = !string.IsNullOrWhiteSpace(body);
        BattleLabHoverHint.Bind(button, button.Visible ? new BattleLabTooltipInfo(name, Abilities: body) : null);
    }

    private void Confirm()
    {
        if (!ConfirmButton.Disabled) _chosen?.Invoke(StableId);
    }
}
