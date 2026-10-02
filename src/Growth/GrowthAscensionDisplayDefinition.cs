using Godot;

namespace TowerAutobattler.Growth;

// Persistent presentation only; neither eligibility nor battle compilation reads it.
[GlobalClass]
public partial class GrowthAscensionDisplayDefinition : Resource
{
    [Export] public string DisplayName { get; set; } = "升阶";
    [Export] public string CompletedLabel { get; set; } = "已升阶";
}
