using System;
using Godot;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.UI;

// The selected hero keeps its art visible while the adjacent sheet handles reading.
public partial class WorkbenchHeroSummary : VBoxContainer
{
    public event Action<DetailExplainButton>? FactInspected;
    private static readonly CombatAttribute[] Attributes = [CombatAttribute.AttackDamage, CombatAttribute.SpellPower,
        CombatAttribute.MaxHealth, CombatAttribute.Armor, CombatAttribute.AttackSpeed, CombatAttribute.AttackRange,
        CombatAttribute.CriticalChance, CombatAttribute.CriticalDamage];
    public override void _Ready()
    {
        _facts = Array.ConvertAll(Attributes, attribute => GetNode<CompactDetailFact>("%" + attribute));
        foreach (var fact in _facts) fact.ExplanationRequested += Inspect;
    }
    public override void _ExitTree()
    {
        foreach (var child in _facts) if (IsInstanceValid(child)) child.ExplanationRequested -= Inspect;
    }
    private CompactDetailFact[] _facts = [];
    private void Inspect(DetailExplainButton source) => FactInspected?.Invoke(source);
    public void Bind(UnitInformation model)
    {
        var portrait = GetNode<UnitPortrait>("%Portrait");
        if (portrait.Definition != model.Definition.Portrait) portrait.Bind(model.Definition.Portrait, model.Definition.Icon);
        GetNode<Label>("%HeroName").Text = model.Definition.DisplayName;
        for (var i = 0; i < Attributes.Length; i++)
        {
            var attribute = Attributes[i];
            var fact = attribute == CombatAttribute.AttackSpeed ? model.CoreStats()[2] : model.AttributeFact(attribute);
            var button = _facts[i];
            button.Bind(fact.Icon, fact.Value.Replace("/秒", "/s"), fact.Caption, fact.Explanation);
            button.AccessibilityName = fact.Caption + " " + fact.Value;
            BattleLabHoverHint.Bind(button, new BattleLabTooltipInfo(fact.Caption, Stats: fact.Explanation));
        }
    }
    public void ForwardDrag(Callable canDrop, Callable drop)
    {
        foreach (var attribute in Attributes)
            GetNode<Control>("%" + attribute).SetDragForwarding(Callable.From<Vector2, Variant>(_ => default), canDrop, drop);
    }
}
