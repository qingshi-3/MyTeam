using System;
using Godot;
using TowerAutobattler.Project;

namespace TowerAutobattler.UI;

public partial class RestScreenController : Control
{
    public event Action<bool>? ChoiceRequested;

    private OutcomeActionButton _gold = null!;
    private OutcomeActionButton _recover = null!;

    public override void _Ready()
    {
        _gold = GetNode<OutcomeActionButton>("Center/Panel/Layout/GoldButton");
        _gold.Pressed += OnGold;
        _recover = GetNode<OutcomeActionButton>("Center/Panel/Layout/RecoverButton");
        _recover.Pressed += OnRecover;
    }

    public override void _ExitTree()
    {
        _gold.Pressed -= OnGold;
        _recover.Pressed -= OnRecover;
    }

    public void Bind(CompiledRunRules rules, int currentHealth = 0, int maximumHealth = 0)
    {
        var healthFull = currentHealth >= maximumHealth;
        _recover.Bind(healthFull ? "生命已满" : "休养整顿",
            healthFull
                ? []
                : [new SemanticFact(SemanticIconKeys.Health, $"+{Math.Min(rules.RestRunHealthRecovery, maximumHealth - currentHealth)}", "HealthValue")],
            healthFull ? "当前全局生命已满。" : "恢复全局生命。", "PrimaryButton");
        _recover.Disabled = healthFull;
        GetNode<Label>("Center/Panel/Layout/Description").Text = $"全局生命 {currentHealth}/{maximumHealth} · 恢复生命或领取金币，只能选择一项。";
        _gold.Bind("整理战利品",
            [new SemanticFact(SemanticIconKeys.Gold, $"+{rules.RestGold}", "GoldValue")],
            "领取金币。",
            "PrimaryButton");
    }

    private void OnGold() => ChoiceRequested?.Invoke(true);
    private void OnRecover() => ChoiceRequested?.Invoke(false);
}
