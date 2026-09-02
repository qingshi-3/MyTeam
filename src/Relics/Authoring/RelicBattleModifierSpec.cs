using Godot;

namespace TowerAutobattler.Relics;

[GlobalClass]
public partial class RelicBattleModifierSpec : Resource
{
    [Export] public RelicBattleModifierKind Kind { get; set; }
    [Export] public float Amount { get; set; }
    [Export] public string ContentId { get; set; } = string.Empty;
}
