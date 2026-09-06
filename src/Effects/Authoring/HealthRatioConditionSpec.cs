using Godot;

namespace TowerAutobattler.Effects;

[GlobalClass]
public partial class HealthRatioConditionSpec : EffectConditionSpec
{
    [Export] public EffectEntityReference Entity { get; set; } = EffectEntityReference.ExplicitTarget;
    [Export] public EffectComparison Comparison { get; set; } = EffectComparison.Less;
    [Export] public float Ratio { get; set; } = .5f;
}
