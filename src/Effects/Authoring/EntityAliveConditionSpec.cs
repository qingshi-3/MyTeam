using Godot;

namespace TowerAutobattler.Effects;

[GlobalClass]
public partial class EntityAliveConditionSpec : EffectConditionSpec
{
    [Export] public EffectEntityReference Entity { get; set; } = EffectEntityReference.ExplicitTarget;
    [Export] public bool ExpectedAlive { get; set; } = true;
}
