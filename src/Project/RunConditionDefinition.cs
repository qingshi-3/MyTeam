using Godot;

namespace TowerAutobattler.Project;

public enum RunConditionKind { GoldAtLeast, RosterHealthAtLeast, HasContent, PopulationBelowCap, StartingHeroIs }

[GlobalClass]
public partial class RunConditionDefinition : Resource
{
    [Export] public RunConditionKind Kind { get; set; }
    [Export] public int Amount { get; set; } = 1;
    [Export] public float Ratio { get; set; }
    [Export] public string ContentId { get; set; } = string.Empty;
}
