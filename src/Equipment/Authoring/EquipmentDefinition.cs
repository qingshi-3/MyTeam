using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Traits;

namespace TowerAutobattler.Equipment;

[GlobalClass]
public partial class EquipmentDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public AttributeModifierSpec[] AttributeModifiers { get; set; } = [];
    [Export] public EquipmentReactiveStatusBindingSpec[] ReactiveStatusBindings { get; set; } = [];
    [Export] public TraitContributionSpec[] TraitContributions { get; set; } = [];
}
