using System;
using Godot;

namespace TowerAutobattler.UI;

public partial class DetailExplainButton : Button
{
    [Export] public PackedScene ExplanationTooltip { get; set; } = null!;
    public event Action<DetailExplainButton>? ExplanationRequested;
    public string ExplanationTitle { get; private set; } = "";
    public string ExplanationText { get; private set; } = "";

    public override void _Ready() => Pressed += OnExplain;
    public override void _ExitTree() => Pressed -= OnExplain;
    private void OnExplain() => ExplanationRequested?.Invoke(this);

    public void BindExplanation(string title, string explanation)
    {
        ExplanationTitle = title;
        ExplanationText = explanation;
        TooltipText = title + "\n" + explanation;
    }

    public override GodotObject _MakeCustomTooltip(string forText)
    {
        // A host can replace native hover with a keyboard-aware hint by clearing
        // TooltipText. Do not create a second popup behind that hint.
        if (string.IsNullOrWhiteSpace(TooltipText)) return null!;
        var tooltip = ExplanationTooltip.Instantiate<PanelContainer>();
        tooltip.GetNode<Label>("%TooltipTitle").Text = ExplanationTitle;
        var copy = tooltip.GetNode<CombatRichText>("%TooltipCopy");
        // Native tooltip windows measure before container sorting. Give wrapped text its
        // authored width now so a one-character-wide first pass cannot leave a tall popup.
        copy.Size = new Vector2(copy.CustomMinimumSize.X, 0);
        copy.Text = ExplanationText;
        return tooltip;
    }
}
