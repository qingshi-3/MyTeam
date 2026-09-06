using Godot;

namespace TowerAutobattler.Project;

public enum RunOperationKind { GainGold, SpendGold, GrantItem, Recruit, RecoverRoster, GrantPopulation, IncreasePopulationCap }
public enum RunRosterTarget { All, StartingHero, OtherHeroes }

[GlobalClass]
public partial class RunOperationDefinition : Resource
{
    [Export] public RunOperationKind Kind { get; set; }
    [Export] public int Amount { get; set; } = 1;
    [Export] public float Ratio { get; set; }
    [Export] public string ContentId { get; set; } = string.Empty;
    [Export] public string SourceId { get; set; } = string.Empty;
    [Export] public RunRosterTarget Target { get; set; }
    [Export] public float MinimumHealthRatio { get; set; }
}
