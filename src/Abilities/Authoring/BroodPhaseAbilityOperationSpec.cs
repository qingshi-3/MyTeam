using Godot;
namespace TowerAutobattler.Abilities;
[GlobalClass]
public partial class BroodPhaseAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public string EggContentId { get; set; } = "";
    [Export] public string LarvaContentId { get; set; } = "";
    [Export] public float HealthThreshold { get; set; } = .5f;
    [Export] public int BreakTicks { get; set; } = 12;
    [Export] public float SmallRadius { get; set; } = .7f;
    [Export] public float RetreatDistance { get; set; } = 2;
    [Export] public float RangedReach { get; set; } = 4;
    [Export] public int HatchTicks { get; set; } = 30;
    [Export] public int SpawnCycleTicks { get; set; } = 100;
    [Export] public int MaximumLiving { get; set; } = 2;
    [Export] public int MaximumBatches { get; set; } = 2;
    [Export] public float SwipeMultiplier { get; set; } = 1.1f;
    [Export] public string Vfx { get; set; } = "shell_break";
}
