using Godot;

namespace TowerAutobattler.BattleLab;

[GlobalClass]
public partial class BattleLabBuiltInPreset : Resource
{
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string PresetJson { get; set; } = string.Empty;
}
