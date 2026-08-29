using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public partial class ArmyDrawerRow : PanelContainer
{
    [Export] public PackedScene SemanticChipScene { get; set; } = null!;

    private UnitPortrait _portrait = null!;
    private Label _title = null!;
    private Label _details = null!;
    private HFlowContainer _facts = null!;
    private HFlowContainer _costs = null!;
    private ResourceCostBadge _manaCost = null!;
    private ResourceCostBadge _goldCost = null!;
    private Label _footer = null!;

    public override void _Ready()
    {
        _portrait = GetNode<UnitPortrait>("%UnitPortrait");
        _title = GetNode<Label>("%RowTitle");
        _details = GetNode<Label>("%RowDetails");
        _facts = GetNode<HFlowContainer>("%RowFacts");
        _costs = GetNode<HFlowContainer>("%RowCosts");
        _manaCost = GetNode<ResourceCostBadge>("%ManaCostBadge");
        _goldCost = GetNode<ResourceCostBadge>("%GoldCostBadge");
        _footer = GetNode<Label>("%RowFooter");
    }

    public void Bind(ArmyOverviewRowViewModel model)
    {
        _portrait.Visible = model.Portrait is not null;
        if (model.Portrait is not null)
            _portrait.Bind(model.Portrait, Fallback(model.Role, model.IsHero));
        _title.Text = model.Title;
        _details.Text = model.Details;
        BindFacts(model.Facts ?? []);
        _manaCost.BindMana(model.ManaCost);
        _goldCost.BindGold(model.GoldCost);
        _costs.Visible = model.ManaCost > 0 || model.GoldCost > 0;
        _footer.Text = model.Footer;
        _footer.Visible = !string.IsNullOrWhiteSpace(model.Footer);
    }

    private void BindFacts(System.Collections.Generic.IEnumerable<SemanticFact> facts)
    {
        foreach (var child in _facts.GetChildren())
        {
            _facts.RemoveChild(child);
            child.Free();
        }
        foreach (var fact in facts)
        {
            var chip = SemanticChipScene.Instantiate<SemanticChip>();
            _facts.AddChild(chip);
            chip.Bind(fact);
        }
        _facts.Visible = _facts.GetChildCount() > 0;
    }

    private static Texture2D? Fallback(UnitRole? role, bool isHero) => SemanticIcons.Catalog.ResolveIcon(
        isHero ? SemanticIconKeys.Hero : role is UnitRole.Ranged or UnitRole.Artillery ? SemanticIconKeys.Ranged : SemanticIconKeys.Melee);
}
