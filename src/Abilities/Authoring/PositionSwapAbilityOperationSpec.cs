using Godot;
namespace TowerAutobattler.Abilities;
[GlobalClass]
public partial class PositionSwapAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public float Range { get; set; } = 12;
    [Export] public int PrepareTicks { get; set; } = 11;
    [Export] public int RecoveryTicks { get; set; } = 8;
    [Export] public string Vfx { get; set; } = "position_swap";
}
