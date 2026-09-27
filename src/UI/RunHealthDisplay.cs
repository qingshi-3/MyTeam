using Godot;

namespace TowerAutobattler.UI;

// Receives a value snapshot; neither this view nor the battle simulator owns Run health.
public partial class RunHealthDisplay : VBoxContainer
{
    public void Bind(int current, int maximum)
    {
        Visible = maximum > 0;
        if (!Visible) return;
        var low = current <= maximum * .3;
        GetNode<SemanticChip>("Health").Bind(SemanticIconKeys.Health,
            $"全局生命 {current}/{maximum}" + (low ? " · 危急" : ""), low ? "WarningLabel" : "HealthValue");
        var gauge = GetNode<ProgressBar>("Gauge");
        gauge.MaxValue = maximum;
        gauge.Value = current;
    }
}
