using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.UI;

public partial class UnitDetailView : PanelContainer
{
    private string _identity = "";
    private PackedScene _factScene = null!;
    private PackedScene _skillScene = null!;
    [Export] public PackedScene TraitBadgeScene { get; set; } = null!;
    private readonly List<CompactDetailFact> _extraFacts = [];
    private readonly List<UnitSkillSummary> _skills = [];
    private DetailExplainButton? _explanationSource;
    private UnitVitals _vitals = null!;
    private UnitCoreStats _stats = null!;
    private Button _closeExplanation = null!;

    public override void _Ready()
    {
        _factScene = GD.Load<PackedScene>("res://scenes/ui/components/CompactDetailFact.tscn");
        _skillScene = GD.Load<PackedScene>("res://scenes/ui/components/UnitSkillSummary.tscn");
        _vitals = GetNode<UnitVitals>("%Vitals");
        _stats = GetNode<UnitCoreStats>("%CoreStats");
        _closeExplanation = GetNode<Button>("%CloseExplanation");
        _vitals.ExplanationRequested += ShowExplanation;
        _stats.ExplanationRequested += ShowExplanation;
        _closeExplanation.Pressed += CloseExplanation;
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_vitals)) _vitals.ExplanationRequested -= ShowExplanation;
        if (IsInstanceValid(_stats)) _stats.ExplanationRequested -= ShowExplanation;
        if (IsInstanceValid(_closeExplanation)) _closeExplanation.Pressed -= CloseExplanation;
        foreach (var fact in _extraFacts) fact.ExplanationRequested -= ShowExplanation;
        foreach (var skill in _skills) skill.ExplanationRequested -= ShowExplanation;
    }

    public void Bind(UnitInformation model)
    {
        var identity = model.Identity;
        var definition = model.Definition;
        GetNode<UnitPortrait>("%Portrait").Bind(definition.Portrait, definition.Icon);
        GetNode<Label>("%UnitName").Text = definition.DisplayName;
        var contextLabel = GetNode<Label>("%Context");
        contextLabel.Text = model.Context.Split('（')[0].Trim();
        contextLabel.TooltipText = model.Context;
        var description = GetNode<Label>("%Description");
        description.Text = RunOfferDetailText.UnitSummary(definition);
        // Definition prose can describe the base skill; effective skill data below owns
        // upgraded behavior. Use legacy prose only when no inspectable skill is supplied.
        description.Visible = model.Skills.IsEmpty && !string.IsNullOrWhiteSpace(description.Text);
        var traits = GetNode<Container>("%Traits");
        foreach (var child in traits.GetChildren()) { traits.RemoveChild(child); child.QueueFree(); }
        foreach (var fact in new[] { UnitSemanticFacts.Responsibility(definition.Role) }.Concat(UnitSemanticFacts.Traits(definition.Faction, definition.Tags)))
        {
            var badge = TraitBadgeScene.Instantiate<TraitBadge>();
            traits.AddChild(badge);
            badge.Bind(fact);
        }
        GetNode<UnitVitals>("%Vitals").Bind(model);
        GetNode<UnitCoreStats>("%CoreStats").Bind(model);
        float Base(CombatAttribute attribute) => model.Base(attribute);
        float Read(CombatAttribute attribute) => model.Read(attribute);
        string Values(CombatAttribute attribute, string rule, bool percent = false) => model.Explain(attribute, rule, percent: percent);
        var mana = Read(CombatAttribute.MaxMana);

        ClearDynamicContent();
        var secondary = GetNode<GridContainer>("%SecondaryStats");
        void Secondary(CombatAttribute attribute, string caption, StringName icon, string rule, bool percent = false, bool always = false)
        {
            var value = Read(attribute);
            if (!always && Math.Abs(value) < .0001f && Math.Abs(Base(attribute)) < .0001f) return;
            AddFact(secondary, icon, percent ? (value * 100).ToString("0.##") + "%" : value.ToString("0.##"), caption, Values(attribute, rule, percent: percent));
        }
        Secondary(CombatAttribute.SpellPower, "法强", SemanticIconKeys.SpellPower, "仅增强明确读取法强的效果。");
        Secondary(CombatAttribute.HealingPower, "治疗强度", SemanticIconKeys.Healing, "仅影响明确读取治疗强度的恢复效果。");
        Secondary(CombatAttribute.CriticalChance, "暴击率", SemanticIconKeys.Damage, "具有暴击资格的命中按此概率判定。", true);
        if (Read(CombatAttribute.CriticalChance) > 0) Secondary(CombatAttribute.CriticalDamage, "暴击倍率", SemanticIconKeys.Damage, "暴击伤害倍率；1.5 表示 150%。");
        Secondary(CombatAttribute.LifeSteal, "吸血", SemanticIconKeys.LifeSteal, "符合吸血资格的伤害按此比例恢复生命。", true);
        Secondary(CombatAttribute.ControlResistance, "控制抗性", SemanticIconKeys.Shield, "减轻可受控制抗性影响的控制。", true);
        Secondary(CombatAttribute.DodgeChance, "闪避率", SemanticIconKeys.Risk, "对可闪避的攻击按此概率判定。", true);
        Secondary(CombatAttribute.MoveSpeed, "移速倍率", SemanticIconKeys.Time, "移动速度倍率；1 为基础速度。");
        if (mana > 0)
        {
            Secondary(CombatAttribute.ManaPerSecond, "每秒回蓝", SemanticIconKeys.Mana, "每秒恢复的法力。", always: true);
            Secondary(CombatAttribute.ManaPerAttack, "普攻回蓝", SemanticIconKeys.Mana, "符合回蓝条件的普通攻击提供的法力。", always: true);
            Secondary(CombatAttribute.ManaPerDamageRatio, "承伤回蓝", SemanticIconKeys.Mana, "实际承伤参与回蓝时使用的系数。单次回蓝还受上限约束。");
            Secondary(CombatAttribute.ManaPerHitCap, "承伤回蓝上限", SemanticIconKeys.Mana, "单次承伤事件能够提供的法力上限。");
        }
        GetNode<FoldableContainer>("%SecondaryAttributes").Title = $"更多属性 · {secondary.GetChildCount()} 项";

        var vocabulary = GetNode<CombatRichText>("%ExplanationText").Vocabulary;
        var bodies = new List<string>();
        void Skill(string category, string name, string body, string timing)
        {
            bodies.Add(body);
            var terms = vocabulary.Terms.Where(term => body.Contains(term.Word, StringComparison.Ordinal));
            var explanation = (timing.Length == 0 ? "" : timing + "\n\n") + body +
                string.Concat(terms.Select(term => $"\n\n{term.Word} · {term.Explanation}"));
            var skill = _skillScene.Instantiate<UnitSkillSummary>();
            GetNode<VBoxContainer>("%SkillList").AddChild(skill);
            skill.Bind(category, name, body, explanation);
            skill.ExplanationRequested += ShowExplanation;
            _skills.Add(skill);
        }
        foreach (var skill in model.Skills)
            Skill(skill.Category, skill.Name, skill.Body, skill.Timing);
        var usedTerms = vocabulary.Terms.Where(term => bodies.Any(body => body.Contains(term.Word, StringComparison.Ordinal))).ToArray();
        foreach (var term in usedTerms)
            AddFact(GetNode<HFlowContainer>("%KeywordChoices"), new StringName(), "解释", term.Word, term.Explanation, term.Tint);
        var glossary = GetNode<FoldableContainer>("%KeywordGlossary");
        glossary.Title = $"词条说明 · {usedTerms.Length} 项";
        glossary.Visible = usedTerms.Length > 0;
        if (_identity != identity)
        {
            GetNode<ScrollContainer>("%DetailsScroll").ScrollVertical = 0;
            GetNode<FoldableContainer>("%SecondaryAttributes").Folded = true;
            glossary.Folded = true;
        }
        GetNode<Control>("%Explanation").Visible = false;
        _explanationSource = null;
        _identity = identity;
    }

    private void AddFact(Container parent, StringName icon, string value, string caption, string explanation, Color? tint = null)
    {
        var fact = _factScene.Instantiate<CompactDetailFact>();
        parent.AddChild(fact);
        fact.Bind(icon, value, caption, explanation, tint);
        fact.ExplanationRequested += ShowExplanation;
        _extraFacts.Add(fact);
    }

    private void ClearDynamicContent()
    {
        foreach (var fact in _extraFacts)
        {
            fact.ExplanationRequested -= ShowExplanation;
            fact.GetParent().RemoveChild(fact);
            fact.QueueFree();
        }
        _extraFacts.Clear();
        foreach (var skill in _skills)
        {
            skill.ExplanationRequested -= ShowExplanation;
            skill.GetParent().RemoveChild(skill);
            skill.QueueFree();
        }
        _skills.Clear();
    }

    private void ShowExplanation(DetailExplainButton source)
    {
        _explanationSource = source;
        GetNode<Label>("%ExplanationTitle").Text = source.ExplanationTitle;
        GetNode<CombatRichText>("%ExplanationText").Text = source.ExplanationText;
        GetNode<Control>("%Explanation").Visible = true;
        // Same local explanation as hover, also reachable through button activation with a keyboard.
        Callable.From(() => GetNode<ScrollContainer>("%DetailsScroll").EnsureControlVisible(GetNode<Control>("%ExplanationTitle"))).CallDeferred();
    }

    private void CloseExplanation()
    {
        GetNode<Control>("%Explanation").Visible = false;
        if (_explanationSource is not null && IsInstanceValid(_explanationSource)) _explanationSource.GrabFocus();
    }
}
