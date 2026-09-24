using Godot;
namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class HookAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public float Range { get; set; } = 7;
    [Export] public float Radius { get; set; } = .16f;
    [Export] public float Speed { get; set; } = 10;
    [Export] public int WindupTicks { get; set; } = 3;
    [Export] public int ReturnTicks { get; set; } = 6;
    [Export] public float AttackRatio { get; set; } = 1;
    [Export] public bool ExcludeBoss { get; set; } = true;
}
