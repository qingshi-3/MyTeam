using System;
using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

// Information only. Selection and equipment policies belong to the embedding card.
public partial class UnitInfoCard : VBoxContainer
{
    [Export] public bool PreferIllustration { get; set; }
    public event Action? Activated;
    private UnitVitals _vitals = null!;
    private UnitCoreStats _stats = null!;
    private Button _active = null!, _passive = null!;
    private UnitDetailView? _expanded;
    private Button? _back;
    private Label? _inspectionHeading;
    private UnitInformation? _model;
    private Button? _inspectionSource;
    private DetailExplainButton? _rosterState;
    private CompactDetailFact? _primaryAttack;
    public DetailExplainButton AttackFact => _primaryAttack ?? (DetailExplainButton)_stats.GetNode<CompactDetailFact>("%DamageFact");
    public override void _Ready()
    {
        _vitals = GetNode<UnitVitals>("%Vitals");
        _stats = GetNode<UnitCoreStats>("%CoreStats");
        _active = GetNode<Button>("%Active");
        _passive = GetNode<Button>("%Passive");
        _vitals.ExplanationRequested += OnFact;
        _stats.ExplanationRequested += OnFact;
        _active.Pressed += OnActivated;
        _passive.Pressed += OnActivated;
        _expanded = GetNodeOrNull<UnitDetailView>("%ExpandedDetails");
        _back = GetNodeOrNull<Button>("%InspectionBack");
        _inspectionHeading = GetNodeOrNull<Label>("%InspectionHeading");
        if (_back is not null) _back.Pressed += CloseDetails;
        _rosterState = GetNodeOrNull<DetailExplainButton>("%RosterState");
        if (_rosterState is not null) _rosterState.ExplanationRequested += OnFact;
        _primaryAttack = GetNodeOrNull<CompactDetailFact>("%PrimaryAttack");
        if (_primaryAttack is not null) _primaryAttack.ExplanationRequested += OnFact;
    }
    public override void _ExitTree()
    {
        // Instanced scene roots can lose unique-name registration during teardown.
        if (IsInstanceValid(_vitals)) _vitals.ExplanationRequested -= OnFact;
        if (IsInstanceValid(_stats)) _stats.ExplanationRequested -= OnFact;
        if (IsInstanceValid(_active)) _active.Pressed -= OnActivated;
        if (IsInstanceValid(_passive)) _passive.Pressed -= OnActivated;
        if (IsInstanceValid(_back)) _back!.Pressed -= CloseDetails;
        if (IsInstanceValid(_rosterState)) _rosterState!.ExplanationRequested -= OnFact;
        if (IsInstanceValid(_primaryAttack)) _primaryAttack!.ExplanationRequested -= OnFact;
    }
    private void OnFact(DetailExplainButton _) => OnActivated();
    private void OnActivated()
    {
        if (_expanded is not null && _back is not null && _model is not null && !_expanded.Visible)
        {
            _inspectionSource = GetViewport().GuiGetFocusOwner() as Button;
            var frontHeight = GetCombinedMinimumSize().Y;
            _expanded.Bind(_model);
            var headingHeight = _inspectionHeading?.GetCombinedMinimumSize().Y ?? 0;
            var gaps = _inspectionHeading is null ? 1 : 2;
            _expanded.CustomMinimumSize = new Vector2(0, Math.Max(0,
                frontHeight - headingHeight - _back.GetCombinedMinimumSize().Y - gaps * GetThemeConstant("separation")));
            SetDetailMode(true);
            _back.GrabFocus();
        }
        Activated?.Invoke();
    }

    private void CloseDetails()
    {
        SetDetailMode(false);
        if (IsInstanceValid(_inspectionSource) && _inspectionSource!.IsVisibleInTree()) _inspectionSource.GrabFocus();
    }

    private void SetDetailMode(bool expanded)
    {
        foreach (var path in new[] { "Artwork", "%Vitals", "%CoreStats", "Skills" })
            GetNode<Control>(path).Visible = !expanded;
        // The illustrated template keeps status and health on the same reading line.
        var condition = GetNodeOrNull<Control>("Condition");
        if (condition is not null) condition.Visible = !expanded;
        _expanded!.Visible = expanded;
        _back!.Visible = expanded;
        if (_inspectionHeading is not null) _inspectionHeading.Visible = expanded;
        BattleLabHoverHint.HideAll(true);
    }

    public void ForwardDrag(Callable canDrop, Callable drop)
    {
        GetNode<UnitVitals>("%Vitals").ForwardDrag(canDrop, drop);
        GetNode<UnitCoreStats>("%CoreStats").ForwardDrag(canDrop, drop);
        foreach (var path in new[] { "%Active", "%Passive" })
            GetNode<Control>(path).SetDragForwarding(Callable.From<Vector2, Variant>(_ => default), canDrop, drop);
        _rosterState?.SetDragForwarding(Callable.From<Vector2, Variant>(_ => default), canDrop, drop);
        _primaryAttack?.SetDragForwarding(Callable.From<Vector2, Variant>(_ => default), canDrop, drop);
    }

    public void BindRosterState(bool deployed, int rank)
    {
        if (_rosterState is null) return;
        GetNode<Label>("%HeroState").Text = rank.ToString();
        var icon = GetNode<TextureRect>("%DeploymentIcon");
        icon.Texture = GD.Load<Texture2D>(deployed
            ? "res://assets/ui/icons/tower-combat.svg" : "res://assets/ui/icons/roster-reserve.svg");
        icon.Modulate = deployed ? new Color(0.84f, 0.80f, 0.67f) : new Color(0.57f, 0.63f, 0.66f);
        var title = $"{(deployed ? "出战" : "后备")} · {rank} 阶";
        _rosterState.BindExplanation(title, deployed ? "已编入当前阵容。" : "未编入当前阵容，可在布阵中上场。");
        _rosterState.AccessibilityName = title;
        BattleLabHoverHint.Bind(_rosterState, new BattleLabTooltipInfo(title, Stats: _rosterState.ExplanationText));
    }

    public void Bind(UnitInformation model, string context)
    {
        _model = model;
        if (_inspectionHeading is not null) _inspectionHeading.Text = model.Definition.DisplayName;
        if (_expanded?.Visible == true) _expanded.Bind(model);
        var portrait = GetNode<UnitPortrait>("%Portrait");
        var presentationChanged = portrait.PreferIllustration != PreferIllustration;
        portrait.PreferIllustration = PreferIllustration;
        if (presentationChanged || portrait.Definition != model.Definition.Portrait)
            portrait.Bind(model.Definition.Portrait, model.Definition.Icon);
        GetNode<Label>("%HeroName").Text = model.Definition.DisplayName;
        GetNode<Label>("%HeroState").Text = context;
        GetNode<Label>("%AttributeContext").Text = model.ContextCaption;
        GetNode<UnitVitals>("%Vitals").Bind(model);
        GetNode<UnitCoreStats>("%CoreStats").Bind(model);
        if (_primaryAttack is not null)
        {
            var attack = model.CoreStats()[0];
            _primaryAttack.Bind(attack.Icon, attack.Value, attack.Caption, attack.Explanation, attack.Tint);
            _primaryAttack.AccessibilityName = attack.Caption + " " + attack.Value;
            BattleLabHoverHint.Bind(_primaryAttack, new BattleLabTooltipInfo(attack.Caption, Stats: attack.Explanation));
        }
        foreach (var active in new[] { true, false })
        {
            var category = active ? "主动" : "被动";
            var skills = model.Skills.Where(skill => skill.Category == category).ToArray();
            var names = string.Join(" / ", skills.Select(skill => skill.Name));
            var target = GetNode<Button>(active ? "%Active" : "%Passive");
            if (target is CardSkillButton cardSkill) cardSkill.BindName(skills.Length == 0 ? "无" : names);
            else target.Text = category + " · " + (skills.Length == 0 ? "无" : names);
            BattleLabHoverHint.Bind(target, new BattleLabTooltipInfo(names, category,
                Abilities: skills.Length == 0 ? "该单位没有此类能力。" :
                    string.Join("\n\n", skills.Select(skill => skill.Name + "\n" + skill.Body + "\n" + skill.Timing))));
        }
    }
}
