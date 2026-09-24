using System;
using Godot;

namespace TowerAutobattler.UI;

public partial class UnitCoreStats : GridContainer
{
    [Export] public bool FocusTooltip { get; set; }
    public event Action<DetailExplainButton>? ExplanationRequested;
    private static readonly string[] Paths = ["%DamageFact", "%ArmorFact", "%AttackRateFact", "%RangeFact"];
    public override void _Ready()
    {
        foreach (var path in Paths) GetNode<CompactDetailFact>(path).ExplanationRequested += Explain;
    }
    public override void _ExitTree()
    {
        foreach (var path in Paths) GetNode<CompactDetailFact>(path).ExplanationRequested -= Explain;
    }
    private void Explain(DetailExplainButton source) => ExplanationRequested?.Invoke(source);

    public void ForwardDrag(Callable canDrop, Callable drop)
    {
        foreach (var path in Paths)
            GetNode<Control>(path).SetDragForwarding(Callable.From<Vector2, Variant>(_ => default), canDrop, drop);
    }

    public void Bind(UnitInformation model)
    {
        var facts = model.CoreStats();
        for (var i = 0; i < facts.Length; i++)
        {
            var fact = facts[i];
            var button = GetNode<CompactDetailFact>(Paths[i]);
            button.Bind(fact.Icon, fact.Value, fact.Caption, fact.Explanation, fact.Tint);
            if (FocusTooltip) BattleLabHoverHint.Bind(button, new BattleLabTooltipInfo(fact.Caption, Stats: fact.Explanation));
        }
    }
}
