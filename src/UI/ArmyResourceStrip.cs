using Godot;

namespace TowerAutobattler.UI;

public partial class ArmyResourceStrip : HBoxContainer
{
    private SemanticChip _heroHealth = null!;
    private SemanticChip _deployed = null!;
    private SemanticChip _reserve = null!;
    private SemanticChip _items = null!;
    private SemanticChip _gold = null!;

    public override void _Ready()
    {
        _heroHealth = GetNode<SemanticChip>("%HeroHealth");
        _deployed = GetNode<SemanticChip>("%Deployed");
        _reserve = GetNode<SemanticChip>("%Reserve");
        _items = GetNode<SemanticChip>("%Items");
        _gold = GetNode<SemanticChip>("%Gold");
    }

    public void Bind(ArmyOverviewViewModel model)
    {
        _heroHealth.Bind(SemanticIconKeys.Hero, model.RosterHealthRatio.ToString("P0"), "HeroIdentity");
        _deployed.Bind(SemanticIconKeys.Melee, $"{model.Deployed}/{model.CurrentPopulation}", "PlayerLabel");
        _reserve.Bind(SemanticIconKeys.Ranged, model.Reserve.ToString(), "RangeValue");
        _items.Bind(SemanticIconKeys.Loot, model.ItemCount.ToString(), "PlayerLabel");
        _gold.Bind(SemanticIconKeys.Gold, model.Gold.ToString(), "GoldValue");
    }
}
