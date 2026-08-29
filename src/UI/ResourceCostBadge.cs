using Godot;

namespace TowerAutobattler.UI;

public partial class ResourceCostBadge : PanelContainer
{
    [Export] public SemanticIconCatalog Catalog { get; set; } = null!;

    private TextureRect _icon = null!;
    private Label _value = null!;

    public StringName SemanticKey { get; private set; } = new();
    public int Amount { get; private set; }
    public string DisplayText => _value?.Text ?? string.Empty;

    public override void _Ready() => CacheNodes();

    public void BindMana(int amount) => Bind(SemanticIconKeys.Mana, amount, "MP", "ManaValue", "ManaCostSurface");
    public void BindGold(int amount) => Bind(SemanticIconKeys.Gold, amount, "金币", "GoldValue", "GoldCostSurface");

    public void Bind(StringName semanticKey, int amount, string unit, StringName valueVariation, StringName surfaceVariation)
    {
        CacheNodes();
        SemanticKey = semanticKey;
        Amount = amount;
        _icon.Texture = Catalog.TryResolve(semanticKey, out var entry) ? entry.Icon : null;
        _value.Text = $"{amount} {unit}";
        _value.ThemeTypeVariation = valueVariation;
        ThemeTypeVariation = surfaceVariation;
        _icon.Modulate = _value.GetThemeColor("font_color");
        Visible = amount > 0;
    }

    private void CacheNodes()
    {
        _icon ??= GetNode<TextureRect>("%CostIcon");
        _value ??= GetNode<Label>("%CostValue");
    }
}
