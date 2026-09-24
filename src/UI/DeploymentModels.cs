using System.Collections.Generic;
using Godot;
using TowerAutobattler.Content;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

public sealed record DeploymentUnitViewModel(
    string InstanceId, string DisplayName, string Description, float HealthRatio,
    UnitRole Role, float AttackRange, bool IsHero, int Slot, Vector2I? Cell,
    UnitPortraitDefinition? Portrait = null,
    IReadOnlyDictionary<Vector2I, FormationEvaluation>? TargetEvaluations = null,
    float BodyRadius = .32f);

public sealed record EnemyDeploymentViewModel(
    string InstanceId,
    string DisplayName,
    Vector2I Cell,
    UnitRole Role,
    float AttackRange,
    bool IsBoss,
    UnitPortraitDefinition? Portrait,
    float BodyRadius = .32f);
