using Godot;

namespace TowerAutobattler.Presentation;

// Read-only visual recipe. No run, encounter, simulation, or save state.
[GlobalClass]
public partial class WeatherPresentationStyle : Resource
{
    [Export] public string DisplayName { get; set; } = "晴天";
    [Export] public string Description { get; set; } = "流动云影与林间日光";
    [Export(PropertyHint.Range, "0,1")] public float Overcast { get; set; }
    [Export] public Color SkyTint { get; set; } = Colors.White;
    [Export(PropertyHint.Range, "0,1")] public float Rain { get; set; }
    [Export(PropertyHint.Range, "0,1")] public float Snow { get; set; }
    [Export(PropertyHint.Range, "0,1")] public float Wetness { get; set; }
    [Export(PropertyHint.Range, "0,1")] public float SnowCover { get; set; }
    [Export(PropertyHint.Range, "0,1")] public float Mist { get; set; }
    [Export(PropertyHint.Range, "-0.5,0.5")] public float Wind { get; set; } = .16f;
    [Export(PropertyHint.Range, "0,1")] public float Lightning { get; set; }
}
