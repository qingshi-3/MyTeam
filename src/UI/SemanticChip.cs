using Godot;

namespace TowerAutobattler.UI;

public partial class SemanticChip : HBoxContainer
{
    [Export] public SemanticIconCatalog Catalog { get; set; } = null!;
    [Export(PropertyHint.Range, "0,64,1")] public int FontSizeOverride { get; set; }

    private TextureRect _icon = null!;
    private Label _text = null!;
    private Label _caption = null!;

    public StringName SemanticKey { get; private set; } = new();
    public Texture2D? ResolvedIcon => _icon?.Texture;
    public string DisplayText => _text?.Text ?? string.Empty;

    public override void _Ready() => CacheNodes();

    public void Bind(StringName semanticKey, string text, StringName? typeVariation = null)
    {
        CacheNodes();
        _caption.Visible = false;
        _text.RemoveThemeColorOverride("font_color");
        SemanticKey = semanticKey;
        var resolved = Catalog is not null && Catalog.TryResolve(semanticKey, out var entry) ? entry : null;
        _icon.Texture = resolved?.Icon;
        _icon.Visible = _icon.Texture is not null;
        _text.Text = text;
        _text.ThemeTypeVariation = typeVariation is { } authored && !authored.IsEmpty
            ? authored
            : resolved is not null && !resolved.PresentationRole.IsEmpty
                ? resolved.PresentationRole
                : new StringName();
        _icon.Modulate = _text.GetThemeColor("font_color");
    }

    public void Bind(SemanticFact fact) => Bind(fact.Key, fact.Text, fact.ThemeTypeVariation);

    public void BindCaption(string caption, Color? tint = null)
    {
        CacheNodes();
        _caption.Text = caption;
        _caption.Visible = !string.IsNullOrWhiteSpace(caption);
        var color = tint ?? _text.GetThemeColor("font_color");
        _caption.AddThemeColorOverride("font_color", color);
        _text.AddThemeColorOverride("font_color", color);
        _icon.Modulate = color;
    }

    private void CacheNodes()
    {
        _icon ??= GetNode<TextureRect>("%SemanticIcon");
        _text ??= GetNode<Label>("%SemanticText");
        _caption ??= GetNode<Label>("%SemanticCaption");
        if (FontSizeOverride > 0) _caption.AddThemeFontSizeOverride("font_size", FontSizeOverride);
        else _caption.RemoveThemeFontSizeOverride("font_size");
        if (FontSizeOverride > 0) _text.AddThemeFontSizeOverride("font_size", FontSizeOverride);
        else _text.RemoveThemeFontSizeOverride("font_size");
    }
}
