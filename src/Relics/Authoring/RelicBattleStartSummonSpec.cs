using Godot;

namespace TowerAutobattler.Relics;

[GlobalClass]
public partial class RelicBattleStartSummonSpec : RelicBattleStartEffectSpec
{
    [Export] public string ContentId { get; set; } = string.Empty;
    [Export] public float HealthMultiplier { get; set; } = 0.85f;
    [Export] public float DamageMultiplier { get; set; } = 0.9f;
}
