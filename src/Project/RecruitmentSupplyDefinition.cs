using Godot;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class RecruitmentSupplyDefinition : Resource
{
    [Export] public RecruitmentTierDefinition[] Tiers { get; set; } = [];
    [Export] public int[] OpeningTierWeights { get; set; } = [];
    [Export] public RecruitmentStageDefinition[] Stages { get; set; } = [];
}
