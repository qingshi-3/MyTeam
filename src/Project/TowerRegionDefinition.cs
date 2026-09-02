using Godot;

namespace TowerAutobattler.Project;

[GlobalClass]
public partial class TowerRegionDefinition : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
    [Export] public Color AccentColor { get; set; } = Colors.White;
    [Export] public EncounterDefinition[] Encounters { get; set; } = [];
}
