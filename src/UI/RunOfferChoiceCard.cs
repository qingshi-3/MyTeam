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
    int Tier = 0, Texture2D? RoleIcon = null, Texture2D? DeliveryIcon = null, string Range = "", bool IsOpportunity = false,
    string SpellPower = "0", UnitInformation? Information = null, string GrowthTitle = "", string GrowthRules = "");

// The card is an inspection surface. Only its explicit action button submits a Run command.
public partial class RunOfferChoiceCard : Control, IUiMotionHost
{
    public string StableId { get; private set; } = string.Empty;
    public string TitleText => GetNode<Label>("Layout/Title").Text;
    public Button ConfirmButton => GetNode<Button>("Actions/ConfirmButton");
    public Button DetailsButton => GetNode<Button>("Actions/DetailsButton");
    public ScrollContainer BodyScroll => GetNode<ScrollContainer>("Layout/BodyScroll");
    public bool DetailsExpanded => IsUnit ? Disclosure.IsOpen : DetailsButton.ButtonPressed;
    public UnitCardOrbit Orbit => GetNode<UnitCardOrbit>("Orbit");
    public UiCardDisclosure Disclosure => Orbit.Disclosure;
    public bool IsUnit { get; private set; }
    private Action<string>? _chosen;
    public Func<bool> ReduceUiMotion { get; set; } = () => false;
    public bool RecruitmentSelected { get; private set; }
    public bool RecruitmentPresentation { get; private set; }
    public UnitPortrait Portrait => GetNode<UnitPortrait>("Layout/Artwork/Portrait");
    private Action<string>? _inspected;
    private Tween? _recruitmentMotion;
    private Control _recruitmentGlow = null!;
    private bool _recruitmentHovered, _recruitmentEntering, _recruitmentReduced, _recruitmentAccepted;
    private float _entryDelay, _lightEnergy;


    public override void _Ready()
    {
        _recruitmentGlow = GetNode<Control>("RecruitmentGlow");
        FocusEntered += InspectRecruitment;
        VisibilityChanged += RecruitmentVisibility;
        ConfirmButton.Pressed += Confirm;
        DetailsButton.Toggled += ToggleDetails;
        GetNode<Button>("Layout/Skills/Active").Pressed += OpenDetails;
        GetNode<Button>("Layout/Skills/Passive").Pressed += OpenDetails;
        BodyScroll.GetVScrollBar().ThemeTypeVariation = "CardScrollBar";
        Orbit.Configure(this, GetNode<Control>("CardFrame"), null);
    }

    public override void _ExitTree()
    {
        ConfirmButton.Pressed -= Confirm;
        DetailsButton.Toggled -= ToggleDetails;
        GetNode<Button>("Layout/Skills/Active").Pressed -= OpenDetails;
        GetNode<Button>("Layout/Skills/Passive").Pressed -= OpenDetails;
        FocusEntered -= InspectRecruitment;
        VisibilityChanged -= RecruitmentVisibility;
        SettleRecruitment();
        _chosen = _inspected = null;
    }

    public void Bind(RunOfferChoiceViewModel model, Action<string> chosen)
    {
        var changed = StableId != model.Id;
        StableId = model.Id;
        IsUnit = model.IsUnit;
        FocusMode = IsUnit ? FocusModeEnum.All : FocusModeEnum.None;
        AccessibilityName = model.Title + " · " + model.Identity;
        if (changed) Disclosure.Reset();
        _chosen = chosen;
        GetNode<Label>("Layout/Title").Text = model.Title;
        GetNode<Label>("Layout/DetailHeading").Text = model.Title;
        GetNode<Label>("Layout/Artwork/ArtTitle").Text = model.Title;
        GetNode<Label>("Layout/Artwork/ArtTitle").TooltipText = model.Title;
        var identity = GetNode<Control>("Layout/Artwork/ArtIdentity");
        identity.AccessibilityName = model.Identity;
        var tierSymbols = GetNode<Control>("Layout/Artwork/ArtIdentity/TierSymbols");
        tierSymbols.Visible = model.Tier > 0;
        tierSymbols.AccessibilityName = $"{model.Tier} 阶英雄";
        GetNode<Label>("Layout/Artwork/ArtIdentity/TierSymbols/Tier").Text = model.Tier > 0 ? $"{model.Tier} 阶" : "";
        BattleLabHoverHint.Bind(tierSymbols, IsUnit && model.Tier > 0
            ? new("英雄阶位", Stats: $"招募阶位为 {model.Tier} 阶。", SingleEntry: true) : null);
        var roleSymbols = GetNode<Control>("Layout/Artwork/ArtIdentity/RoleSymbols");
        roleSymbols.AccessibilityName = "职业与攻击类型 · " + model.Identity;
        BattleLabHoverHint.Bind(roleSymbols, IsUnit
            ? new("职业与攻击类型", Stats: model.Identity, SingleEntry: true) : null);
        GetNode<TextureRect>("Layout/Artwork/ArtIdentity/RoleSymbols/Role").Texture = model.RoleIcon;
        GetNode<TextureRect>("Layout/Artwork/ArtIdentity/RoleSymbols/Delivery").Texture = model.DeliveryIcon;
        GetNode<Label>("Layout/BodyScroll/Body/DetailTitle").Text = model.Title + " · " + model.Identity;
        GetNode<Control>("Layout/Title").Visible = !model.IsUnit;
        GetNode<Control>("Layout/Identity").Visible = !model.IsUnit;
        GetNode<Control>("Layout/Artwork/ArtTitle").Visible = model.IsUnit;
        GetNode<Control>("Layout/Artwork/ArtIdentity").Visible = model.IsUnit;
        GetNode<Control>("Layout/Artwork/CoreReadingSurface").Visible = model.IsUnit;
        GetNode<Control>("Layout/Skills").Visible = model.IsUnit;
        GetNode<Control>("Layout/BodyScroll/Body/DetailTitle").Visible = model.IsUnit;
        GetNode<Label>("Layout/Identity").Text = model.Identity;
        GetNode<UnitPortrait>("Layout/Artwork/Portrait").Bind(model.Portrait, model.Icon);
        GetNode<UnitPortrait>("Layout/Artwork/Portrait").Visible = !model.IsOpportunity;
        var opportunityIcon = GetNode<TextureRect>("Layout/Artwork/OpportunityIcon");
        opportunityIcon.Texture = model.Icon;
        opportunityIcon.Visible = model.IsOpportunity;
        GetNode<Control>("Layout/Artwork/OpportunityMount").Visible = model.IsOpportunity;
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
        DetailsButton.Visible &= !IsUnit;
        if (model.Information is { } information)
            Orbit.Bind(information, model.GrowthTitle, model.GrowthRules,
                string.Join(" · ", UnitSemanticFacts.Traits(information.Definition.Faction, information.Definition.Tags)
                    .Select(fact => fact.Text)));
        Orbit.BindEmptyEquipment();
        BindPrimary("PrimarySpell", model.IsUnit, SemanticIconKeys.SpellPower, model.SpellPower, "法强", "仅增强明确读取法强的效果。");
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
        ToggleDetails(IsUnit ? false : DetailsExpanded);
        Orbit.Visible = IsUnit;
        // Non-unit offers keep their explicit text disclosure and never open a hidden unit orbit.
        Disclosure.SetProcess(IsUnit);
        if (!IsUnit) Disclosure.Reset();
        ApplyFrameLayout();
        if (RecruitmentPresentation) RefreshRecruitment();
    }

    public void SetAvailableHeight(float height)
    {
        SetAvailableSize(new Vector2(IsUnit ? 490 : 280, Math.Max(IsUnit ? 540 : 240, height)));
    }

    public void SetAvailableSize(Vector2 size)
    {
        CustomMinimumSize = size;
        SizeFlagsHorizontal = IsUnit ? SizeFlags.ShrinkCenter : SizeFlags.ExpandFill;
        SizeFlagsVertical = IsUnit ? SizeFlags.ShrinkBegin : SizeFlags.ExpandFill;
        ApplyFrameLayout();
        GetNode<Control>("Layout/Artwork").CustomMinimumSize = new Vector2(0,
            IsUnit ? Math.Max(RecruitmentPresentation ? 220 : 272, size.Y - 252) : Mathf.Clamp(size.Y * .32f, 64, 164));
        GetNode<Control>("Layout/Artwork").SizeFlagsVertical = IsUnit ? SizeFlags.ExpandFill : SizeFlags.Fill;
    }

    private void ApplyFrameLayout()
    {
        var frame = GetNode<Control>("CardFrame");
        frame.OffsetLeft = IsUnit ? 36 : 0; frame.OffsetRight = IsUnit ? -36 : 0;
        // The lower action belongs to the offer, not the illustrated unit frame.
        frame.OffsetTop = IsUnit ? 28 : 0; frame.OffsetBottom = IsUnit ? -88 : 0;
        Orbit.OffsetBottom = IsUnit ? frame.OffsetBottom : -28;
        // The roster strip makes recruitment cards shorter. Reserve the core-stat
        // plaque below the complete disclosure instead of hiding its final row.
        var detailLift = RecruitmentPresentation ? 24f : 0f;
        foreach (var name in new[] { "Left", "Right" })
        {
            var group = Orbit.GetNode<Control>(name);
            group.OffsetTop = 126 - detailLift;
            group.OffsetBottom = 285 - detailLift;
        }
        var readingSurface = Orbit.GetNode<Control>("EdgeShade/DetailReadingSurface");
        readingSurface.OffsetTop = 118 - detailLift;
        readingSurface.OffsetBottom = 298 - detailLift;
        var skillDock = Orbit.GetNode<Control>("TopDock");
        skillDock.OffsetTop = RecruitmentPresentation ? 42 : 50;
        skillDock.OffsetBottom = RecruitmentPresentation ? 90 : 98;
        if (_recruitmentGlow is not null)
        {
            _recruitmentGlow.OffsetLeft = frame.OffsetLeft - 10;
            _recruitmentGlow.OffsetRight = frame.OffsetRight + 10;
            _recruitmentGlow.OffsetTop = frame.OffsetTop - 12;
            _recruitmentGlow.OffsetBottom = frame.OffsetBottom + 12;
        }
        var layout = GetNode<Control>("Layout");
        layout.OffsetLeft = IsUnit ? 52 : 20; layout.OffsetRight = IsUnit ? -52 : -20;
        layout.OffsetTop = IsUnit ? 46 : 20; layout.OffsetBottom = IsUnit ? -106 : -76;
        var actions = GetNode<Control>("Actions");
        actions.OffsetLeft = IsUnit ? 52 : 20; actions.OffsetRight = IsUnit ? -52 : -20;
        actions.OffsetTop = IsUnit ? -60 : -64; actions.OffsetBottom = IsUnit ? -12 : -20;
    }

    private void BindCopy(string name, string text)
    {
        var copy = GetNode<CombatRichText>("Layout/BodyScroll/Body/" + name);
        copy.Text = text;
        copy.Visible = !string.IsNullOrWhiteSpace(text);
    }

    private void ToggleDetails(bool expanded)
    {
        if (IsUnit)
        {
            GetNode<Control>("Layout/Stats").Hide();
            GetNode<Control>("Layout/Skills").Hide();
            GetNode<Control>("Layout/Artwork").Show();
            GetNode<Control>("Layout/DetailHeading").Hide();
            BodyScroll.Hide();
            return;
        }
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
        BattleLabHoverHint.Bind(fact, visible ? new BattleLabTooltipInfo(caption + " " + value, Stats: explanation, SingleEntry: true) : null);
    }

    private void OpenDetails()
    {
        if (IsUnit) { Disclosure.Pin(); return; }
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

    // Optional display behavior. Every other offer retains its original binding and input.
    public void ConfigureRecruitment(bool enabled, Action<string>? inspected = null)
    {
        SettleRecruitment();
        _recruitmentAccepted = false;
        RecruitmentPresentation = enabled && IsUnit;
        _inspected = RecruitmentPresentation ? inspected : null;
        RecruitmentSelected = false;
        OffsetTransformEnabled = RecruitmentPresentation;
        OffsetTransformVisualOnly = false;
        OffsetTransformPivotRatio = new Vector2(.5f, .5f);
        _recruitmentGlow.Visible = RecruitmentPresentation;
        if (!RecruitmentPresentation) return;
        ApplyFrameLayout();
        BeginRecruitmentEntry();
    }

    private void BeginRecruitmentEntry()
    {
        SettleRecruitment();
        if (ReduceUiMotion() || !IsVisibleInTree()) { RefreshRecruitment(); return; }
        _recruitmentEntering = true;
        _entryDelay = GetIndex() * .075f;
        OffsetTransformPosition = new Vector2(0, 26);
        OffsetTransformScale = Vector2.One * .95f;
        Modulate = new Color(1, 1, 1, 0);
    }

    public void SetRecruitmentSelected(bool selected)
    {
        if (RecruitmentSelected == selected) return;
        RecruitmentSelected = selected;
        if (!_recruitmentEntering) RefreshRecruitment();
    }

    public void PresentRecruitmentDeparture(bool source)
    {
        if (!RecruitmentPresentation) return;
        SettleRecruitment();
        _recruitmentAccepted = true;
        ConfirmButton.Disabled = true;
        Disclosure.Reset(true);
        _recruitmentGlow.Hide();
        Modulate = new Color(source ? .62f : .84f, source ? .62f : .84f, source ? .62f : .84f, 1);
    }

    public override void _GuiInput(InputEvent input)
    {
        if (!RecruitmentPresentation || _recruitmentAccepted) return;
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        { GrabFocus(); InspectRecruitment(); AcceptEvent(); }
        else if (input.IsActionPressed("ui_accept"))
        { InspectRecruitment(); Disclosure.Pin(); AcceptEvent(); }
    }

    private void InspectRecruitment()
    {
        if (RecruitmentPresentation && !_recruitmentAccepted) _inspected?.Invoke(StableId);
    }

    public override void _Process(double delta)
    {
        if (!RecruitmentPresentation || !IsVisibleInTree() || _recruitmentAccepted) return;
        var reduced = ReduceUiMotion();
        if (reduced != _recruitmentReduced)
        {
            _recruitmentReduced = reduced;
            if (reduced) SettleRecruitment();
            RefreshRecruitment();
        }
        if (_recruitmentEntering)
        {
            _entryDelay -= (float)delta;
            if (_entryDelay <= 0 && _recruitmentMotion is null)
            {
                _recruitmentMotion = RecruitmentTween();
                _recruitmentMotion.TweenProperty(this, "offset_transform_position", Vector2.Zero, .34);
                _recruitmentMotion.TweenProperty(this, "offset_transform_scale", Vector2.One, .34)
                    .SetTrans(Tween.TransitionType.Back);
                _recruitmentMotion.TweenProperty(this, "modulate:a", 1f, .13);
                _recruitmentMotion.Chain().TweenCallback(Callable.From(() =>
                { _recruitmentEntering = false; RefreshRecruitment(); }));
            }
        }
        var hovered = GetViewport().GuiGetHoveredControl();
        var focus = GetViewport().GuiGetFocusOwner();
        var inside = hovered == this || hovered is not null && IsAncestorOf(hovered) ||
            focus == this || focus is not null && IsAncestorOf(focus) || Disclosure.IsOpen;
        if (inside != _recruitmentHovered)
        {
            _recruitmentHovered = inside;
            if (!_recruitmentEntering) RefreshRecruitment();
        }
        // Eligibility comes exclusively from the actual confirmation button.
        var available = !ConfirmButton.Disabled;
        var energy = RecruitmentSelected ? .68f : inside ? .28f : available ? .065f : 0;
        _lightEnergy = reduced ? energy : Mathf.MoveToward(_lightEnergy, energy, (float)delta * 4.5f);
        _recruitmentGlow.Modulate = new Color(RecruitmentSelected ? 1 : .82f, RecruitmentSelected ? .9f : .95f,
            RecruitmentSelected ? .62f : 1, _lightEnergy);
    }

    private Tween RecruitmentTween() => CreateTween().SetIgnoreTimeScale(true)
        .SetPauseMode(Tween.TweenPauseMode.Process).SetParallel()
        .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

    private void RefreshRecruitment()
    {
        _recruitmentMotion?.Kill(); _recruitmentMotion = null;
        Modulate = Colors.White;
        ZIndex = _recruitmentHovered ? 20 : RecruitmentSelected ? 2 : 0;
        if (ReduceUiMotion() || !RecruitmentPresentation || !IsVisibleInTree())
        { OffsetTransformScale = Vector2.One; OffsetTransformPosition = Vector2.Zero; return; }
        _recruitmentMotion = RecruitmentTween();
        _recruitmentMotion.TweenProperty(this, "offset_transform_scale",
            Vector2.One * (_recruitmentHovered ? 1.018f : RecruitmentSelected ? 1.007f : 1), .22)
            .SetTrans(Tween.TransitionType.Back);
        _recruitmentMotion.TweenProperty(this, "offset_transform_position",
            new Vector2(0, _recruitmentHovered ? -5 : RecruitmentSelected ? -3 : 0), .20);
    }

    private void SettleRecruitment()
    {
        _recruitmentMotion?.Kill(); _recruitmentMotion = null;
        _recruitmentEntering = _recruitmentHovered = false;
        OffsetTransformScale = Vector2.One; OffsetTransformPosition = Vector2.Zero;
        Modulate = Colors.White; ZIndex = 0;
        _lightEnergy = 0;
        if (_recruitmentGlow is not null) _recruitmentGlow.Modulate = new Color(1, 1, 1, 0);
    }

    private void RecruitmentVisibility()
    {
        if (!IsVisibleInTree()) SettleRecruitment();
        else if (RecruitmentPresentation && !_recruitmentAccepted) BeginRecruitmentEntry();
    }

    private void Confirm()
    {
        if (!ConfirmButton.Disabled) _chosen?.Invoke(StableId);
    }
}
