using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public partial class ItemChoiceCard : ChoiceCard
{
    private ColorRect _rarityRail = null!;
    private SemanticChip _category = null!;

    public void BindItem(ItemRarity rarity, bool shop)
    {
        _rarityRail ??= GetNode<ColorRect>("%RarityRail");
        _category ??= GetNode<SemanticChip>("%ItemCategory");
        _rarityRail.Color = rarity switch
        {
            ItemRarity.Legendary => new Color(1f, .72f, .2f),
            ItemRarity.Rare => new Color(.65f, .42f, 1f),
            ItemRarity.Uncommon => new Color(.24f, .78f, .62f),
            _ => new Color(.48f, .58f, .66f)
        };
        _category.Bind(shop ? SemanticIconKeys.Gold : SemanticIconKeys.Loot,
            shop ? "商品" : PlayerFacingText.DescribeItemRarity(rarity),
            shop ? "GoldValue" : rarity == ItemRarity.Legendary ? "HeroIdentity" : "TraitIdentity");
    }
}
