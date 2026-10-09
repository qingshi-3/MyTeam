using Godot;
namespace TowerAutobattler.Equipment;

[GlobalClass]
public partial class EquipmentLifeStealSpec : Resource
{
    [Export] public string RuleId { get; set; } = "health_damage_lifesteal";
    [Export] public float Ratio { get; set; } = .12f;
    [Export] public float AreaEfficiency { get; set; } = 1f / 3f;
}
