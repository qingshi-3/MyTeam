using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.Components;

[GlobalClass]
public partial class UnitReadabilityComponent : Node2D
{
    private Label _heroMarker = null!;
    private Label _reachMarker = null!;

    public override void _Ready()
    {
        _heroMarker = GetNode<Label>("HeroMarker");
        _reachMarker = GetNode<Label>("ReachMarker");
    }

    public void Bind(UnitDefinition definition, int team)
    {
        _heroMarker.Visible = definition.IsHero;
        _heroMarker.Text = definition.IsHero ? "★ 英雄" : string.Empty;
        _reachMarker.Text = UnitRangeClassifier.Marker(definition.AttackRange);
        _reachMarker.TooltipText = $"{UnitRangeClassifier.Describe(definition.AttackRange)} · 攻击距离 {definition.AttackRange:0.#}";
        _reachMarker.Modulate = team == 0 ? Colors.White : new Color(1f, .78f, .78f);
    }
}
