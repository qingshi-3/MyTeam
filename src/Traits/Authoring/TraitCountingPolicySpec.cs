using Godot;

namespace TowerAutobattler.Traits;

public enum TraitDeploymentPolicy { AllEligible, DeployedOnly }
public enum TraitTemporaryUnitPolicy { Exclude, Include }
public enum TraitDuplicateContentPolicy { CountEach, UniqueContent }

[GlobalClass]
public partial class TraitCountingPolicySpec : Resource
{
    [Export] public TraitDeploymentPolicy DeploymentPolicy { get; set; } = TraitDeploymentPolicy.DeployedOnly;
    [Export] public TraitTemporaryUnitPolicy TemporaryUnitPolicy { get; set; } = TraitTemporaryUnitPolicy.Exclude;
    [Export] public TraitDuplicateContentPolicy DuplicateContentPolicy { get; set; } = TraitDuplicateContentPolicy.CountEach;
    [Export] public bool CountEquipment { get; set; } = true;
    [Export] public bool CountExplicitExtra { get; set; } = true;
}
