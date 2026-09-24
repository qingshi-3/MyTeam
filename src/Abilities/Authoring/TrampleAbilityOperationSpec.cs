using Godot;

namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class TrampleAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public float Range { get; set; } = 12;
    [Export] public float Distance { get; set; } = 8;
    [Export] public float Speed { get; set; } = 5;
    [Export] public int ChargeTicks { get; set; } = 12;
    [Export] public int RecoveryTicks { get; set; } = 10;
    [Export] public float SideDistance { get; set; } = 2.4f;
    [Export] public float AttackMultiplier { get; set; } = 1.2f;
    [Export] public string WarningVfx { get; set; } = "trample_warning";
    [Export] public string RushVfx { get; set; } = "trample_rush";
}
