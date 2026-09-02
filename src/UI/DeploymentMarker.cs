using Godot;

namespace TowerAutobattler.UI;

public partial class DeploymentMarker : Control
{
    private TextureRect _icon = null!;

    public override void _Ready() => _icon = GetNode<TextureRect>("%MarkerIcon");

    public void Bind(StringName semanticKey, string detail, Color tint)
    {
        _icon ??= GetNode<TextureRect>("%MarkerIcon");
        _icon.Texture = SemanticIcons.Catalog.ResolveIcon(semanticKey);
        _icon.Modulate = tint;
        TooltipText = detail;
    }
}
