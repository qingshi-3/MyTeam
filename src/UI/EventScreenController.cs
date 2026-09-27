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
            [new SemanticFact(SemanticIconKeys.Risk, $"成功 {rules.RiskyEventSuccessChance:P0}", "RiskValue"),
                new SemanticFact(SemanticIconKeys.Gold, $"成功 +{rules.RiskyEventSuccessGold}", "GoldValue"),
                new SemanticFact(SemanticIconKeys.Risk, "失败无收益", "SecondaryLabel")],
            "成功时获得金币，失败时无收益。",
            "DangerButton");
        _safe.Bind("谨慎绕行",
            [new SemanticFact(SemanticIconKeys.Gold, $"+{rules.SafeEventGold}", "GoldValue")],
            "稳定获得，不承担失败风险。",
            "PrimaryButton");
    }

    private void OnRisk() => ChoiceRequested?.Invoke(true);
    private void OnSafe() => ChoiceRequested?.Invoke(false);
}
