using System;
using Godot;
using TowerAutobattler.Project;

namespace TowerAutobattler.UI;

public partial class EventScreenController : Control
{
    public event Action<bool>? ChoiceRequested;

    private OutcomeActionButton _risk = null!;
    private OutcomeActionButton _safe = null!;

    public override void _Ready()
    {
        _risk = GetNode<OutcomeActionButton>("Center/Panel/Layout/RiskButton");
        _safe = GetNode<OutcomeActionButton>("Center/Panel/Layout/SafeButton");
        _risk.Pressed += OnRisk;
        _safe.Pressed += OnSafe;
    }

    public override void _ExitTree()
    {
        _risk.Pressed -= OnRisk;
        _safe.Pressed -= OnSafe;
    }

    public void Bind(CompiledRunRules rules)
    {
        _risk.Bind("冒险开启",
            [new SemanticFact(SemanticIconKeys.Risk, $"{rules.RiskyEventSuccessChance:P0}", "RiskValue"),
                new SemanticFact(SemanticIconKeys.Gold, $"+{rules.RiskyEventSuccessGold}", "GoldValue"),
                new SemanticFact(SemanticIconKeys.Health, $"-{rules.RiskyEventHealthLoss:P0}", "DangerValue")],
            $"成功获得 {rules.RiskyEventSuccessGold} 金币；失败时英雄损失相当于最大生命 {rules.RiskyEventHealthLoss:P0} 的生命比例，最低保留 {rules.RiskyEventMinimumHealth:P0}。",
            "DangerButton");
        _safe.Bind("谨慎绕行",
            [new SemanticFact(SemanticIconKeys.Gold, $"+{rules.SafeEventGold}", "GoldValue")],
            $"稳定获得 {rules.SafeEventGold} 金币。",
            "PrimaryButton");
    }

    private void OnRisk() => ChoiceRequested?.Invoke(true);
    private void OnSafe() => ChoiceRequested?.Invoke(false);
}
