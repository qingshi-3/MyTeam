using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

public partial class CompactDetailFact : DetailExplainButton
{
    [Export] public CombatKeywordCatalog Vocabulary { get; set; } = null!;

    public void Bind(StringName icon, string value, string caption, string explanation, Color? tint = null)
    {
        var color = tint ?? Vocabulary?.Terms.FirstOrDefault(term => term.SemanticIcon == icon)?.Tint ?? Colors.White;
        var image = GetNode<TextureRect>("%FactIcon");
        image.Texture = string.IsNullOrEmpty(icon.ToString()) ? null : SemanticIcons.Catalog.ResolveIcon(icon);
        image.Visible = image.Texture is not null;
        image.Modulate = color;
        GetNode<Label>("%FactValue").Text = value;
        GetNode<Label>("%FactValue").AddThemeColorOverride("font_color", color);
        GetNode<Label>("%FactCaption").Text = caption;
        GetNode<Label>("%FactCaption").AddThemeColorOverride("font_color", color);
        BindExplanation(caption, explanation);
    }
}
