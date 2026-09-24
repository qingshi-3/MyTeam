using Godot;
namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class GritStorageAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public string CounterKey { get; set; } = "grit";
    [Export] public int WindowTicks { get; set; } = 60;
    [Export] public float MaximumHealthRatio { get; set; } = .5f;
}
