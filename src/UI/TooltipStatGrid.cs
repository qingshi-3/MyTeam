using System.Collections.Immutable;
using System.Linq;
using Godot;

namespace TowerAutobattler.UI;

// Repeated authored chips; the tooltip owns no attribute calculation or combat state.
public partial class TooltipStatGrid : GridContainer
{
    [Export] public PackedScene FactScene { get; set; } = null!;
    [Export] public CombatKeywordCatalog Vocabulary { get; set; } = null!;

    public void Bind(ImmutableArray<UnitAttributeFact> facts)
    {
        var count = facts.IsDefault ? 0 : facts.Length;
        Visible = count > 0;
        while (GetChildCount() < count)
        {
            var chip = FactScene.Instantiate<SemanticChip>();
            chip.FontSizeOverride = 14;
            chip.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            AddChild(chip);
        }
        for (var i = 0; i < GetChildCount(); i++)
        {
            var chip = GetChild<SemanticChip>(i);
            chip.Visible = i < count;
            if (i >= count) continue;
            chip.Bind(facts[i].Icon, facts[i].Value);
            chip.BindCaption(facts[i].Caption,
                Vocabulary.Terms.FirstOrDefault(term => term.SemanticIcon == facts[i].Icon)?.Tint);
            chip.AccessibilityName = facts[i].Caption + " " + facts[i].Value + " " + facts[i].Explanation;
        }
    }
}
