using Godot;

namespace TowerAutobattler.Attributes;

[GlobalClass]
public partial class TraitValueAttributeMagnitudeSpec : AttributeMagnitudeSpec
{
    [Export] public string TraitId { get; set; } = string.Empty;
    [Export] public int Team { get; set; }
}
