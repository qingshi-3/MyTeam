using Godot;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class RunChoiceDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
    [Export] public RunConditionDefinition[] Conditions { get; set; } = [];
    [Export] public RunOperationDefinition[] Costs { get; set; } = [];
    [Export] public RunOperationDefinition[] Operations { get; set; } = [];
    [Export(PropertyHint.Range, "0,1,0.01")] public float SuccessChance { get; set; } = 1;
    [Export] public RunOperationDefinition[] FailureOperations { get; set; } = [];
}
