using Godot;
namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class GritPunchAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public string CounterKey { get; set; } = "grit";
    [Export] public float LowHealthRatio { get; set; } = .5f;
    [Export] public float CriticalHealthRatio { get; set; } = .2f;
    [Export] public float Range { get; set; } = 3;
    [Export] public float Radius { get; set; } = .8f;
    [Export] public int ChargeTicks { get; set; } = 6;
    [Export] public int RecoveryTicks { get; set; } = 4;
    [Export] public int ShieldTicks { get; set; } = 30;
    [Export] public float AttackRatio { get; set; } = 1;
    [Export] public float GritDamageRatio { get; set; } = .8f;
}
