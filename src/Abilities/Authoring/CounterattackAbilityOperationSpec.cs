using Godot;
namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class CounterattackAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public int HitsRequired { get; set; } = 3;
    [Export] public float AttackRatio { get; set; } = 1;
    [Export] public float LifestealRatio { get; set; } = .8f;
}
