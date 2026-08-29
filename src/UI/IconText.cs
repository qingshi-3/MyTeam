using Godot;

namespace TowerAutobattler.UI;

public partial class IconText : HBoxContainer
{
    private TextureRect _icon = null!;
    private Label _text = null!;

    public override void _Ready()
    {
        _icon = GetNode<TextureRect>("%Icon");
        _text = GetNode<Label>("%Text");
    }

    public void Bind(Texture2D? icon, string text, StringName? typeVariation = null)
    {
        _icon.Texture = icon;
        _icon.Visible = icon is not null;
        _text.Text = text;
        _text.ThemeTypeVariation = typeVariation ?? new StringName();
    }
}
