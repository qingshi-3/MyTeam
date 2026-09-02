using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.UI;

namespace TowerAutobattler.Components;

[GlobalClass]
public partial class UnitReadabilityComponent : Node2D
{
    private TextureRect _heroMarker = null!;
    private TextureRect _reachMarker = null!;

    public override void _Ready()
    {
        _heroMarker = GetNode<TextureRect>("HeroMarker");
        _reachMarker = GetNode<TextureRect>("ReachMarker");
    }

    public void Bind(UnitDefinition definition, int team)
    {
        _heroMarker.Visible = definition.IsHero;
        _heroMarker.Texture = SemanticIcons.Catalog.ResolveIcon(SemanticIconKeys.Hero);
        _heroMarker.Modulate = new Color(1f, .82f, .25f);
        _heroMarker.TooltipText = definition.IsHero ? "英雄" : string.Empty;
        _reachMarker.Texture = SemanticIcons.Catalog.ResolveIcon(
            UnitRangeClassifier.Classify(definition.AttackRange) == UnitReachClass.Ranged
                ? SemanticIconKeys.Ranged
                : SemanticIconKeys.Melee);
        _reachMarker.TooltipText = $"{UnitRangeClassifier.Describe(definition.AttackRange)} · 攻击距离 {definition.AttackRange:0.#}";
        _reachMarker.Modulate = team == 0 ? Colors.White : new Color(1f, .78f, .78f);
    }
}
