using Godot;

namespace TowerAutobattler.Relics;

[GlobalClass]
public partial class RelicRunOutcomeSpec : Resource
{
    [Export] public RelicRunOutcomeKind Kind { get; set; }
    [Export] public int Amount { get; set; }
}
