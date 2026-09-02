using Godot;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class TowerNodeTableDefinition : Resource
{
    [Export] public TowerNodeDefinition[] Nodes { get; set; } = [];
    [Export] public TowerNodeDefinition[] Rotation { get; set; } = [];
    [Export] public int BossLocalFloor { get; set; } = 4;
    [Export] public int RegularOptionCount { get; set; } = 3;
    [Export] public int RotationStride { get; set; } = 2;
    [Export] public int FloorSeedStride { get; set; } = 7;
}
