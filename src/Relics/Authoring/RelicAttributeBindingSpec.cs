using Godot;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Relics;

public enum RelicAttributeStackPolicy
{
    PerStack,
    LinearAcrossStacksAndInstances
}

[GlobalClass]
public partial class RelicAttributeBindingSpec : Resource
{
    [Export] public string BindingId { get; set; } = string.Empty;
    [Export] public RelicUnitTargetSpec Target { get; set; } = null!;
    [Export] public RelicAttributeStackPolicy StackPolicy { get; set; }
    [Export] public AttributeModifierSpec Modifier { get; set; } = null!;
}
