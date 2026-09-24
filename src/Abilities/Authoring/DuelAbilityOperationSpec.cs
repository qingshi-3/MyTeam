using Godot;
namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class DuelAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public int DurationTicks { get; set; } = 40;
    [Export] public float BreakDistance { get; set; } = 4;
}
