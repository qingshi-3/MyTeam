using Godot;

namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class EnemyOriginalAttackCycleSpec : AbilityOperationSpec
{
    [Export] public int Every { get; set; } = 4;
    [Export] public int WindupTicks { get; set; } = 4;
    [Export] public float DamageMultiplier { get; set; } = 1.8f;
}
