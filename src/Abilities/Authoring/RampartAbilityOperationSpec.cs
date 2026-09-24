using Godot;
namespace TowerAutobattler.Abilities;
[GlobalClass]
public partial class RampartAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public string WallContentId { get; set; } = "";
    [Export] public int RaiseTicks { get; set; } = 10;
    [Export] public int IntervalTicks { get; set; } = 8;
    [Export] public int FissureWindupTicks { get; set; } = 10;
    [Export] public int RecoveryTicks { get; set; } = 15;
    [Export] public int WallLifetimeTicks { get; set; } = 30;
    [Export] public float Range { get; set; } = 6;
    [Export] public float FissureRadius { get; set; } = .28f;
    [Export] public float AttackMultiplier { get; set; } = 1.6f;
    [Export] public string Vfx { get; set; } = "ground_fissure";
}
