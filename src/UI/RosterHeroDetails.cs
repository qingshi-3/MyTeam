using System;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.UI;

public partial class RosterHeroDetails : VBoxContainer
{
    private string _identity = "";
    private DetailExplainButton? _source;
    private Button _close = null!;
    private CompactDetailFact[] _facts = [];
    private DetailExplainButton[] _skills = [];
    public override void _Ready()
    {
        _facts = Enum.GetValues<CombatAttribute>().Select(attribute => GetNode<CompactDetailFact>("%" + attribute)).ToArray();
        foreach (var fact in _facts) fact.ExplanationRequested += ShowExplanation;
        _skills = [GetNode<DetailExplainButton>("%Active"), GetNode<DetailExplainButton>("%Passive")];
        foreach (var skill in _skills) skill.ExplanationRequested += ShowExplanation;
        _close = GetNode<Button>("%ExplanationClose");
        _close.Pressed += CloseExplanation;
    }
    public override void _ExitTree()
    {
        foreach (var fact in _facts) if (IsInstanceValid(fact)) fact.ExplanationRequested -= ShowExplanation;
        foreach (var skill in _skills) if (IsInstanceValid(skill)) skill.ExplanationRequested -= ShowExplanation;
        if (IsInstanceValid(_close)) _close.Pressed -= CloseExplanation;
    }
    public void Bind(UnitInformation model, int rank = 1)
    {
        if (_identity != model.Identity) { GetNode<Control>("%Explanation").Hide(); GetNode<Control>("%MainContent").Show(); _source = null; }
        _identity = model.Identity;
        GetNode<Label>("%HeroName").Text = model.Definition.DisplayName;
        GetNode<Label>("%Context").Text = model.ContextCaption;
        GetNode<Label>("%DetailRank").Text = RosterHeroCard.FormatRank(rank);
        foreach (var attribute in Enum.GetValues<CombatAttribute>())
        {
            var fact = model.AttributeFact(attribute);
            var row = GetNode<CompactDetailFact>("%" + attribute);
            row.Bind(fact.Icon, fact.Value, fact.Caption, fact.Explanation);
            // Compact captions follow the approved sheet; full terms remain available
            // on the same focusable explanation source and its accessibility name.
            row.GetNode<Label>("%FactCaption").Text = attribute switch
            {
                CombatAttribute.AttackSpeed => "攻速",
                CombatAttribute.MoveSpeed => "移速",
                CombatAttribute.CriticalChance => "暴击",
                CombatAttribute.CriticalDamage => "爆伤",
                CombatAttribute.DodgeChance => "闪避",
                CombatAttribute.ControlResistance => "控抗",
                CombatAttribute.ManaPerDamageRatio => "承伤回蓝",
                CombatAttribute.ManaPerHitCap => "单次上限",
                _ => fact.Caption
            };
            row.AccessibilityName = fact.Caption + " " + fact.Value;
            BattleLabHoverHint.Bind(row, new BattleLabTooltipInfo(fact.Caption, Stats: fact.Explanation));
        }
        var groups = model.Skills.GroupBy(skill => skill.Category).ToArray();
        for (var index = 0; index < _skills.Length; index++)
        {
            var group = index < groups.Length ? groups[index].ToArray() : [];
            var category = group.Length == 0 ? string.Empty : groups[index].Key;
            var name = string.Join(" / ", group.Select(skill => skill.Name));
            var body = string.Join("\n\n", group.Select(skill => skill.Body));
            var explanation = string.Join("\n\n", group.Select(skill => skill.Name + "\n" + skill.Body + "\n" + skill.Timing));
            var button = _skills[index];
            button.Visible = group.Length > 0;
            button.GetNode<Label>("Layout/Copy/Category").Text = category.Length == 0 ? string.Empty : category + "技能";
            button.GetNode<Label>("Layout/Copy/Name").Text = name;
            button.GetNode<Label>("Layout/Copy/Body").Text = body.Replace("\n", " ");
            button.BindExplanation(name, explanation);
            button.AccessibilityName = category + "技能 " + name;
            BattleLabHoverHint.Bind(button, new BattleLabTooltipInfo(name, category, Abilities: explanation));
        }
        GetNode<Control>("MainContent/Content/Skills/Divider").Visible = _skills.All(skill => skill.Visible);
        // Equipment can change while an explanation is open. Its source was rebound
        // above (or by the adjacent summary), so refresh prose without stealing focus.
        if (GetNode<Control>("%Explanation").Visible && IsInstanceValid(_source))
        {
            GetNode<Label>("%ExplanationTitle").Text = _source!.ExplanationTitle;
            GetNode<Label>("%ExplanationText").Text = _source.ExplanationText;
        }
    }
    public void ShowExplanation(DetailExplainButton source)
    {
        _source = source;
        GetNode<Label>("%ExplanationTitle").Text = source.ExplanationTitle;
        GetNode<Label>("%ExplanationText").Text = source.ExplanationText;
        GetNode<Control>("%Explanation").Show();
        GetNode<Control>("%MainContent").Hide();
        BattleLabHoverHint.HideAll(true);
        _close.GrabFocus();
    }
    private void CloseExplanation()
    {
        GetNode<Control>("%Explanation").Hide();
        GetNode<Control>("%MainContent").Show();
        if (IsInstanceValid(_source) && _source!.IsVisibleInTree()) _source.GrabFocus();
    }
}
