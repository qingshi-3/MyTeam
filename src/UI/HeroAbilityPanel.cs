using Godot;

namespace TowerAutobattler.UI;

public partial class HeroAbilityPanel : PanelContainer
{
    [Export] public SemanticIconCatalog Catalog { get; set; } = null!;

    private TextureRect _icon = null!;
    private Label _name = null!;
    private Label _effect = null!;
    private ResourceCostBadge _mana = null!;
    private ResourceCostBadge _gold = null!;

    public override void _Ready() => CacheNodes();

    public void Bind(string commandName, string effect, int manaCost, int goldCost)
    {
        CacheNodes();
        _icon.Texture = Catalog.ResolveIcon(SemanticIconKeys.Hero);
        _icon.Modulate = _name.GetThemeColor("font_color");
        _name.Text = commandName;
        _effect.Text = effect;
        _mana.BindMana(manaCost);
        _gold.BindGold(goldCost);
    }

    private void CacheNodes()
    {
        _icon ??= GetNode<TextureRect>("%AbilityIcon");
        _name ??= GetNode<Label>("%AbilityName");
        _effect ??= GetNode<Label>("%AbilityEffect");
        _mana ??= GetNode<ResourceCostBadge>("%ManaCostBadge");
        _gold ??= GetNode<ResourceCostBadge>("%GoldCostBadge");
    }
}
