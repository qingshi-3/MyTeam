using Godot;

namespace TowerAutobattler.Relics;

[GlobalClass]
public partial class RelicBattleStartShieldSpec : RelicBattleStartEffectSpec
{
    [Export] public int Amount { get; set; }
}
