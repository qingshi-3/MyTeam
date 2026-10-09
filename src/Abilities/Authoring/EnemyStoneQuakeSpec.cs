using Godot;
namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class EnemyStoneQuakeSpec : AbilityOperationSpec
{
    [Export] public float Radius { get; set; } = 2;
    [Export] public int WindupTicks { get; set; } = 7;
    [Export] public int PulseIntervalTicks { get; set; } = 5;
    [Export] public int PulseCount { get; set; } = 3;
    [Export] public int RecoveryTicks { get; set; } = 5;
    [Export] public float AttackMultiplier { get; set; } = .7f;
    [Export] public float ArmorMultiplier { get; set; } = 8;
    [Export] public float CompletionArmor { get; set; } = 4;
    [Export] public int ArmorDurationTicks { get; set; } = 60;
}
