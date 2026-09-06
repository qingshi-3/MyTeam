using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Traits;

public enum TraitTargetPolicy { AllTeam, Contributors }

[GlobalClass]
public partial class TraitBreakpointSpec : Resource
{
    [Export(PropertyHint.Range, "0,999,1")] public int MinValue { get; set; }
    [Export(PropertyHint.Range, "0,999,1")] public int MaxValue { get; set; }
    [Export] public string DisplayStyle { get; set; } = string.Empty;
    [Export] public AttributeModifierSpec[] AttributeModifiers { get; set; } = [];
    [Export] public TraitTargetPolicy TargetPolicy { get; set; }
    [Export] public StatusDefinition[] GrantedStatuses { get; set; } = [];
}
