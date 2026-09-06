using Godot;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Effects;

[GlobalClass]
public partial class EffectStepSpec : Resource
{
    [Export] public EffectAmountSource AmountSource { get; set; }
    [Export] public float Amount { get; set; } = 1f;
    /// <summary>Optional formula replacing Amount/AmountSource; evaluated against the invocation wave snapshot.</summary>
    [Export] public AttributeMagnitudeSpec? Magnitude { get; set; }
}
