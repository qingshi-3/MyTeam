using System;
using Godot;

namespace TowerAutobattler.UI;

// The category is a silhouette on the card and text in its accessible explanation.
public partial class CardSkillButton : Button
{
    [Export] public string Category { get; set; } = "主动";
    [Export] public bool Featured { get; set; }

    public void BindName(string name)
    {
        var texture = GD.Load<Texture2D>(Category == "主动"
            ? "res://assets/ui/icons/skill-active.svg" : "res://assets/ui/icons/skill-passive.svg");
        var symbol = GetNode<TextureRect>("Face/Lines/Category");
        symbol.Texture = texture;
        symbol.Modulate = Category == "主动" ? new Color(0.78f, 0.85f, 0.91f) : new Color(0.75f, 0.67f, 0.49f);
        var label = GetNode<Label>("Face/Lines/Name");
        label.Text = name;
        Text = string.Empty;
        if (Featured)
        {
            var size = Category == "主动" ? 18 : 14;
            label.AddThemeFontSizeOverride("font_size", size);
            label.AddThemeColorOverride("font_color", Category == "主动" ? new Color(0.94f, 0.88f, 0.74f) : new Color(0.69f, 0.66f, 0.57f));
            // Center the icon/name as one group. Long grouped skills clip here and
            // remain complete in the existing hover/inspection content.
            label.CustomMinimumSize = new Vector2(Math.Min(220, label.GetThemeFont("font").GetStringSize(name, fontSize: size).X + 2), 0);
        }
        TooltipText = Category + " · " + name;
        AccessibilityName = TooltipText;
    }
}
