using System;
using Godot;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.UI;

public partial class UnitVitals : VBoxContainer
{
    [Export] public bool FocusTooltip { get; set; }
    [Export] public bool ShowMana { get; set; } = true;
    [Export] public bool CompactSymbols { get; set; }
    public event Action<DetailExplainButton>? ExplanationRequested;
    private DetailExplainButton _healthFact = null!, _manaFact = null!;
    public override void _Ready()
    {
        _healthFact = GetNode<DetailExplainButton>("%HealthFact");
        _manaFact = GetNode<DetailExplainButton>("%ManaFact");
        _healthFact.ExplanationRequested += Explain;
        _manaFact.ExplanationRequested += Explain;
    }
    public override void _ExitTree()
    {
        // Unique-name registration can be gone when an instanced scene exits.
        if (IsInstanceValid(_healthFact)) _healthFact.ExplanationRequested -= Explain;
        if (IsInstanceValid(_manaFact)) _manaFact.ExplanationRequested -= Explain;
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
        var healthText = $"生命上限 {maxHealth:0.#}";
        GetNode<DetailExplainButton>("%HealthFact").BindExplanation("生命上限", healthText + "\n" +
            model.Explain(CombatAttribute.MaxHealth, "每场战斗以生命上限入场。战斗中的伤害、治疗与死亡仍正常结算。", model.Baseline.MaxHealth));
        GetNode<Label>("%HealthValue").Text = CompactSymbols ? $"{maxHealth:0.#}" : healthText;
        GetNode<DetailExplainButton>("%HealthFact").AccessibilityName = healthText;
        var mana = model.Read(CombatAttribute.MaxMana);
        var currentMana = model.Read(CombatAttribute.StartingMana);
        var fact = GetNode<DetailExplainButton>("%ManaFact");
        fact.Visible = ShowMana && model.UsesMana && mana > 0;
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
