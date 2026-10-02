using Godot;

namespace TowerAutobattler.UI;

public partial class ArmyResourceStrip : HBoxContainer
{
    private SemanticChip _heroes = null!;
    private SemanticChip _deployed = null!;
    private SemanticChip _reserve = null!;
    private SemanticChip _items = null!;
    private SemanticChip _gold = null!;

    public override void _Ready()
    {
        _heroes = GetNode<SemanticChip>("%Heroes");
        _deployed = GetNode<SemanticChip>("%Deployed");
        _reserve = GetNode<SemanticChip>("%Reserve");
        _items = GetNode<SemanticChip>("%Items");
        _gold = GetNode<SemanticChip>("%Gold");
    }

    public void Bind(ArmyOverviewViewModel model)
    {
        GetNode<RunHealthDisplay>("%RunHealth").Bind(model.CurrentRunHealth, model.MaximumRunHealth);
        BindMetric(_heroes, SemanticIconKeys.Hero, "英雄", model.RosterCount.ToString());
        BindMetric(_deployed, SemanticIconKeys.Melee, "出战 / 人口", $"{model.Deployed}/{model.CurrentPopulation}");
        BindMetric(_reserve, SemanticIconKeys.Ranged, "后备", model.Reserve.ToString());
        BindMetric(_items, SemanticIconKeys.Loot, "背包", model.ItemCount.ToString());
        BindMetric(_gold, SemanticIconKeys.Gold, "金币", model.Gold.ToString(), "HudGold");
    }

    private static void BindMetric(SemanticChip chip, StringName icon, string caption, string value, string role = "HudValue")
    {
        chip.Bind(icon, value, role);
        var label = chip.GetNode<Label>("%SemanticCaption");
        label.Text = caption;
        label.Show();
        chip.AccessibilityName = caption + " " + value;
    }
}
