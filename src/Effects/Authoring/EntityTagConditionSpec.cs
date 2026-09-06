using Godot;

namespace TowerAutobattler.Effects;

[GlobalClass]
public partial class EntityTagConditionSpec : EffectConditionSpec
{
    [Export] public EffectEntityReference Entity { get; set; } = EffectEntityReference.ExplicitTarget;
    [Export] public StringName Tag { get; set; } = new();
    [Export] public bool ExpectedPresent { get; set; } = true;
}
