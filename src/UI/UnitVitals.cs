using System;
using Godot;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.UI;

public partial class UnitVitals : VBoxContainer
{
    [Export] public bool FocusTooltip { get; set; }
    public event Action<DetailExplainButton>? ExplanationRequested;
    public override void _Ready()
    {
        GetNode<DetailExplainButton>("%HealthFact").ExplanationRequested += Explain;
        GetNode<DetailExplainButton>("%ManaFact").ExplanationRequested += Explain;
    }
    public override void _ExitTree()
    {
        GetNode<DetailExplainButton>("%HealthFact").ExplanationRequested -= Explain;
        GetNode<DetailExplainButton>("%ManaFact").ExplanationRequested -= Explain;
    }
    private void Explain(DetailExplainButton source) => ExplanationRequested?.Invoke(source);

    public void ForwardDrag(Callable canDrop, Callable drop)
    {
        foreach (var path in new[] { "%HealthFact", "%ManaFact" })
            GetNode<Control>(path).SetDragForwarding(Callable.From<Vector2, Variant>(_ => default), canDrop, drop);
    }

    public void Bind(UnitInformation model)
    {
        var maxHealth = model.Read(CombatAttribute.MaxHealth, model.Baseline.MaxHealth);
        var healthText = $"生命 {model.Health:0.#} / {maxHealth:0.#}" + (model.Shield > 0 ? $"  +盾 {model.Shield:0.#}" : "");
        GetNode<DetailExplainButton>("%HealthFact").BindExplanation("生命", healthText + "\n" +
            model.Explain(CombatAttribute.MaxHealth, "生命降至 0 时死亡。护盾单独承受伤害。", model.Baseline.MaxHealth));
        GetNode<ProgressBar>("%HealthGauge").MaxValue = Math.Max(1, maxHealth);
        GetNode<ProgressBar>("%HealthGauge").Value = model.Health;
        GetNode<Label>("%HealthValue").Text = healthText;
        var mana = model.Read(CombatAttribute.MaxMana);
        var currentMana = model.Read(CombatAttribute.StartingMana);
        var fact = GetNode<DetailExplainButton>("%ManaFact");
        fact.Visible = model.UsesMana && mana > 0;
        fact.BindExplanation("法力", model.Explain(CombatAttribute.MaxMana, "法力满时自动施放主动技能。") +
            $"\n初始法力 {currentMana:0.#}\n每秒回蓝 {model.Read(CombatAttribute.ManaPerSecond):0.#} · 普攻回蓝 {model.Read(CombatAttribute.ManaPerAttack):0.#}");
        GetNode<ProgressBar>("%ManaGauge").MaxValue = Math.Max(1, mana);
        GetNode<ProgressBar>("%ManaGauge").Value = currentMana;
        GetNode<Label>("%ManaValue").Text = $"法力 {currentMana:0.#} / {mana:0.#}";
        if (FocusTooltip)
            foreach (var path in new[] { "%HealthFact", "%ManaFact" })
            {
                var button = GetNode<DetailExplainButton>(path);
                BattleLabHoverHint.Bind(button, new BattleLabTooltipInfo(button.ExplanationTitle, Stats: button.ExplanationText));
            }
    }
}
