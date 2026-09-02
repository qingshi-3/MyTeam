using Godot;

namespace TowerAutobattler.Attributes;

[GlobalClass]
public partial class TeamCountAttributeMagnitudeSpec : AttributeMagnitudeSpec
{
    [Export] public AttributeTeamCountKind CountKind { get; set; }
    [Export] public int Team { get; set; }
}
