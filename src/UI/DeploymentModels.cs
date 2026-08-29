using TowerAutobattler.Content;

namespace TowerAutobattler.UI;

public sealed record DeploymentUnitViewModel(
    string InstanceId, string DisplayName, string Description, float HealthRatio,
    UnitRole Role, float AttackRange, int Slot, UnitPortraitDefinition? Portrait = null);
