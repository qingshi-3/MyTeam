using Godot;

namespace TowerAutobattler.Attributes;

[GlobalClass]
public partial class ContextAttributeMagnitudeSpec : AttributeMagnitudeSpec
{
    [Export] public string Key { get; set; } = string.Empty;
}
