using Godot;

namespace TowerAutobattler.UI;

public partial class StatBlock : PanelContainer
{
    [Export] public SemanticIconCatalog Catalog { get; set; } = null!;

    private TextureRect _icon = null!;
    private Label _value = null!;
    private Label _label = null!;

    public StringName SemanticKey { get; private set; } = new();
    public string ValueText => _value?.Text ?? string.Empty;

    public override void _Ready() => CacheNodes();

    public void Bind(StringName semanticKey, string value, string label, StringName valueVariation)
    {
        CacheNodes();
        SemanticKey = semanticKey;
        _icon.Texture = Catalog.TryResolve(semanticKey, out var entry) ? entry.Icon : null;
        _value.Text = value;
        _value.ThemeTypeVariation = valueVariation;
        _label.Text = label;
        _icon.Modulate = _value.GetThemeColor("font_color");
    }

    private void CacheNodes()
    {
        _icon ??= GetNode<TextureRect>("%StatIcon");
        _value ??= GetNode<Label>("%StatValue");
        _label ??= GetNode<Label>("%StatLabel");
    }
}
