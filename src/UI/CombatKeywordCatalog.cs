using Godot;

namespace TowerAutobattler.UI;

[GlobalClass]
public partial class CombatKeywordCatalog : Resource
{
    [Export] public Godot.Collections.Array<CombatKeyword> Terms { get; set; } = [];
}
