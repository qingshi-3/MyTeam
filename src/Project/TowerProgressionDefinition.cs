using Godot;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class TowerProgressionDefinition : ProgressionDefinition
{
    [Export] public int FloorsPerRegion { get; set; } = 5;
    [Export] public TowerRegionDefinition[] Regions { get; set; } = [];
    [Export] public TowerNodeTableDefinition? NodeTable { get; set; }
}
