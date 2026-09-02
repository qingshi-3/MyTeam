using Godot;

namespace TowerAutobattler.Attributes;

[GlobalClass]
public partial class AttributeMagnitudeSpec : Resource
{
    [Export] public AttributeCaptureMode CaptureMode { get; set; } = AttributeCaptureMode.Snapshot;
}
