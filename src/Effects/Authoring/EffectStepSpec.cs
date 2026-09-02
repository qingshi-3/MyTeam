using Godot;

namespace TowerAutobattler.Effects;

[GlobalClass]
public partial class EffectStepSpec : Resource
{
    [Export] public EffectAmountSource AmountSource { get; set; }
    [Export] public float Amount { get; set; } = 1f;
}
