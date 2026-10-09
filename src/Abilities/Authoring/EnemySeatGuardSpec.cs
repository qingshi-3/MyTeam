using Godot;
namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class EnemySeatGuardSpec : AbilityOperationSpec
{
    [Export] public Godot.Collections.Array<Vector2I> GuardCells { get; set; } = [new(6, 0), new(6, 2), new(6, 4)];
    [Export] public float DamageMultiplier { get; set; } = .8f;
}
