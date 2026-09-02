using Godot;

namespace TowerAutobattler.BattleLab;

[GlobalClass]
public partial class BattleLabPresetCatalog : Resource
{
    [Export] public string DefaultPresetName { get; set; } = string.Empty;
    [Export] public Godot.Collections.Array<BattleLabBuiltInPreset> Presets { get; set; } = [];
}
