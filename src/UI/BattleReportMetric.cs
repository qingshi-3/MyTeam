using Godot;

namespace TowerAutobattler.UI;

public partial class BattleReportMetric : PanelContainer
{
    private TextureRect _icon = null!;
    private Label _label = null!;
    private Label _value = null!;

    public override void _Ready()
    {
        _icon = GetNode<TextureRect>("%MetricIcon");
        _label = GetNode<Label>("%MetricLabel");
        _value = GetNode<Label>("%MetricValue");
    }

    public void Bind(Texture2D icon, string label, string value, StringName valueVariation, bool muted = false)
    {
        var displayValue = string.IsNullOrWhiteSpace(value) ? "0" : value;
        _icon.Texture = icon;
        _label.Text = label;
        _value.Text = displayValue;
        _value.ThemeTypeVariation = muted ? "SecondaryLabel" : valueVariation;
        _icon.Modulate = _value.GetThemeColor("font_color");
    }
}
