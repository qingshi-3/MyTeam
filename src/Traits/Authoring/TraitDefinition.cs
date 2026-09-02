using Godot;

namespace TowerAutobattler.Traits;

[GlobalClass]
public partial class TraitDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public string SemanticIconKey { get; set; } = string.Empty;
    [Export] public TraitCountingPolicySpec? CountingPolicy { get; set; }
    [Export] public TraitBreakpointSpec[] Breakpoints { get; set; } = [];
}
