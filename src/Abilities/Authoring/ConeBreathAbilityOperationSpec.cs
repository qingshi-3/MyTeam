using Godot;
namespace TowerAutobattler.Abilities;
[GlobalClass]
public partial class ConeBreathAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public float Range { get; set; } = 3;
    [Export] public float AngleDegrees { get; set; } = 70;
    [Export] public int WindupTicks { get; set; } = 10;
    [Export] public int PulseIntervalTicks { get; set; } = 6;
    [Export] public int PulseCount { get; set; } = 3;
    [Export] public int RecoveryTicks { get; set; } = 12;
    [Export] public float AttackMultiplier { get; set; } = .65f;
    [Export] public string Vfx { get; set; } = "cone_breath";
}
