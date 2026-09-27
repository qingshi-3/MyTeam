using Godot;
namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class GritStorageAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public string CounterKey { get; set; } = "grit";
    // Zero retains grit until the authored consumer or the battle lifecycle clears it.
    [Export] public int WindowTicks { get; set; } = 60;
    [Export] public float MaximumHealthRatio { get; set; } = .5f;
}
