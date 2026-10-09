using Godot;

namespace TowerAutobattler.Equipment;

[GlobalClass]
public partial class EquipmentControlGuardSpec : Resource
{
    [Export] public string RuleId { get; set; } = "first_hard_control";
    [Export] public int WindowTicks { get; set; } = 30;
    [Export] public float SubsequentDurationMultiplier { get; set; } = .5f;
}
