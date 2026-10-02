using Godot;

namespace TowerAutobattler.UI;

// Receives a value snapshot; neither this view nor the battle simulator owns Run health.
public partial class RunHealthDisplay : VBoxContainer
{
    [Export] public StyleBox? GaugeNormalFill { get; set; }
    [Export] public StyleBox? GaugeCriticalFill { get; set; }

    public void Bind(int current, int maximum)
    {
        Visible = maximum > 0;
        if (!Visible) return;
        var low = current <= maximum * .3;
        var health = GetNode<SemanticChip>("Health");
        health.Bind(SemanticIconKeys.Health, $"{current}/{maximum}", low ? "HudCriticalHealth" : "HudHealth");
        var caption = health.GetNode<Label>("%SemanticCaption");
        caption.Text = low ? "全局生命 · 危急" : "全局生命";
        caption.Show();
        health.AccessibilityName = caption.Text + " " + current + "/" + maximum;
        var gauge = GetNode<ProgressBar>("Gauge");
        var fill = low ? GaugeCriticalFill : GaugeNormalFill;
        if (fill is not null) gauge.AddThemeStyleboxOverride("fill", fill);
        gauge.MaxValue = maximum;
        gauge.Value = current;
    }
}
