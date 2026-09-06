using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Traits;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Equipment;

[GlobalClass]
public partial class EquipmentDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public AttributeModifierSpec[] AttributeModifiers { get; set; } = [];
    [Export] public EquipmentReactiveStatusBindingSpec[] ReactiveStatusBindings { get; set; } = [];
    [Export] public TraitContributionSpec[] TraitContributions { get; set; } = [];
    [Export] public StatusDefinition[] GrantedStatuses { get; set; } = [];
}
