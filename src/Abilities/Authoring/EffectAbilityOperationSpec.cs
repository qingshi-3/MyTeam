using Godot;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class EffectAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public EffectBindingSpec Binding { get; set; } = null!;
    [Export] public AbilityInvocationValueSource InvocationValueSource { get; set; }
    [Export] public float InvocationValueScale { get; set; } = 1f;
}
