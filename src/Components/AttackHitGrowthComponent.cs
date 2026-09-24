using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;

namespace TowerAutobattler.Components;

// Authored parameters only; counters and modifiers belong to each BattleUnitState.
[GlobalClass]
public partial class AttackHitGrowthComponent : Node
{
    [Export] public float AttackSpeedPerHit { get; set; } = .04f;
    [Export] public bool ResetOnTargetChange { get; set; } = true;
    [Export] public bool RetentionUpgradeAvailable { get; set; }

    public AttackHitGrowthSnapshot Snapshot() => new(AttackSpeedPerHit, ResetOnTargetChange, RetentionUpgradeAvailable);

    public ValidationReport ValidateAuthoring()
    {
        var report = new ValidationReport();
        if (!float.IsFinite(AttackSpeedPerHit) || AttackSpeedPerHit <= 0)
            report.Error("Attack hit growth requires a finite, positive increment.");
        return report;
    }
}
