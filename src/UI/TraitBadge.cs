using Godot;

namespace TowerAutobattler.UI;

public partial class TraitBadge : PanelContainer
{
    [Export] public SemanticIconCatalog Catalog { get; set; } = null!;

    private TextureRect _icon = null!;
    private Label _label = null!;

    public StringName SemanticKey { get; private set; } = new();
    public string DisplayText => _label?.Text ?? string.Empty;

    public override void _Ready() => CacheNodes();

    public void Bind(SemanticFact fact)
    {
        CacheNodes();
        SemanticKey = fact.Key;
        _icon.Texture = Catalog.TryResolve(fact.Key, out var entry) ? entry.Icon : null;
        _label.Text = fact.Text;
        _label.ThemeTypeVariation = fact.ThemeTypeVariation ?? "TraitIdentity";
        _icon.Modulate = _label.GetThemeColor("font_color");
    }

    private void CacheNodes()
    {
        _icon ??= GetNode<TextureRect>("%TraitIcon");
        _label ??= GetNode<Label>("%TraitLabel");
    }
}
