using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

public partial class CompactDetailFact : DetailExplainButton
{
    [Export] public CombatKeywordCatalog Vocabulary { get; set; } = null!;
    [Export] public bool ShowIcon { get; set; } = true;
    [Export] public bool UseSemanticColor { get; set; } = true;
    [Export] public bool UseMaterialIcon { get; set; }

    public void Bind(StringName icon, string value, string caption, string explanation, Color? tint = null)
    {
        var color = tint ?? Vocabulary?.Terms.FirstOrDefault(term => term.SemanticIcon == icon)?.Tint ?? Colors.White;
        var image = GetNode<TextureRect>("%FactIcon");
        var material = UseMaterialIcon ? UnitCardIcons.Catalog.ResolveIcon(icon) : null;
        image.Texture = material ?? (string.IsNullOrEmpty(icon.ToString()) ? null : SemanticIcons.Catalog.ResolveIcon(icon));
        image.Visible = ShowIcon && image.Texture is not null;
        image.Modulate = material is null ? color : Colors.White;
        GetNode<Label>("%FactValue").Text = value;
        if (UseSemanticColor) GetNode<Label>("%FactValue").AddThemeColorOverride("font_color", color);
        GetNode<Label>("%FactCaption").Text = caption;
        if (UseSemanticColor) GetNode<Label>("%FactCaption").AddThemeColorOverride("font_color", color);
        BindExplanation(caption, explanation);
    }
}
