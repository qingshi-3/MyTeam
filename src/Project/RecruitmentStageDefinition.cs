using Godot;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class RecruitmentStageDefinition : Resource
{
    // Zero-based absolute floor, independent of region, combat count and roster power.
    [Export] public int StartFloorIndex { get; set; }
    [Export] public int[] TierWeights { get; set; } = [];
}
