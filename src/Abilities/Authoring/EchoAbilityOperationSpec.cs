using Godot;
namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class EchoAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public float Range { get; set; } = 3;
    [Export] public bool BehindOnly { get; set; } = true;
}
