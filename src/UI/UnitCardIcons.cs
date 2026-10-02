using Godot;

namespace TowerAutobattler.UI;

// Card-only artwork: retain the monochrome vocabulary used by other HUD surfaces.
public static class UnitCardIcons
{
    private static SemanticIconCatalog? _catalog;
    public static SemanticIconCatalog Catalog => _catalog ??=
        GD.Load<SemanticIconCatalog>("res://content/ui/unit_card_icon_catalog.tres");

    public static Texture2D? Resolve(StringName key) =>
        Catalog.ResolveIcon(key) ?? SemanticIcons.Catalog.ResolveIcon(key);
}
