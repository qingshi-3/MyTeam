using System;
using Godot;
using TowerAutobattler.Project;

namespace TowerAutobattler.UI;

public partial class RestScreenController : Control
{
    public event Action<bool>? ChoiceRequested;

    private OutcomeActionButton _recover = null!;
    private OutcomeActionButton _gold = null!;

    public override void _Ready()
    {
        _recover = GetNode<OutcomeActionButton>("Center/Panel/Layout/RecoverButton");
        _gold = GetNode<OutcomeActionButton>("Center/Panel/Layout/GoldButton");
        _recover.Pressed += OnRecover;
        _gold.Pressed += OnGold;
    }

    public override void _ExitTree()
    {
        _recover.Pressed -= OnRecover;
        _gold.Pressed -= OnGold;
    }

    public void Bind(CompiledRunRules rules)
    {
        _recover.Bind("全军休整",
            [new SemanticFact(SemanticIconKeys.Healing, $"英雄 +{rules.RestHeroHealing:P0}", "HealingValue"),
                new SemanticFact(SemanticIconKeys.Health, $"士兵 +{rules.RestSoldierHealing:P0}", "HealthValue")],
            $"英雄恢复 {rules.RestHeroHealing:P0}，所有士兵恢复 {rules.RestSoldierHealing:P0}，均不超过满生命。",
            "PrimaryButton");
        _gold.Bind("整理战利品",
            [new SemanticFact(SemanticIconKeys.Gold, $"+{rules.RestGold}", "GoldValue")],
            $"获得 {rules.RestGold} 金币。",
            "SecondaryButton");
    }

    private void OnRecover() => ChoiceRequested?.Invoke(false);
    private void OnGold() => ChoiceRequested?.Invoke(true);
}
