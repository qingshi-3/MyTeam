using System;
using Godot;

namespace TowerAutobattler.UI;

public partial class CombatFactTile : Button
{
    public event Action<CombatFactTile>? DetailRequested;
    private TextureRect _icon = null!;
    private Label _name = null!;
    private Label _value = null!;
    private string _detail = "";
    public string DetailText => _detail;

    public override void _Ready()
    {
        _icon = GetNode<TextureRect>("%FactIcon");
        _name = GetNode<Label>("%FactName");
        _value = GetNode<Label>("%FactValue");
        Pressed += ShowDetail;
    }

    public void Bind(Texture2D? icon, string name, string value, string detail)
    {
        _icon.Texture = icon;
        _icon.Modulate = icon is null ? new Color(1, 1, 1, .25f) : Colors.White;
        _name.Text = name;
        _value.Text = value;
        _detail = detail;
        TooltipText = detail;
    }

    private void ShowDetail() => DetailRequested?.Invoke(this);
    public override void _ExitTree() => Pressed -= ShowDetail;
}
