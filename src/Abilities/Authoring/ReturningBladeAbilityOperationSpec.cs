using Godot;
namespace TowerAutobattler.Abilities;
[GlobalClass]
public partial class ReturningBladeAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public float Range { get; set; } = 6;
    [Export] public float Radius { get; set; } = .22f;
    [Export] public float Speed { get; set; } = 6;
    [Export] public float AttackMultiplier { get; set; } = .9f;
    [Export] public string Vfx { get; set; } = "returning_blade";
}
