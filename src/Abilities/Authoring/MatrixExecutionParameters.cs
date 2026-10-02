using Godot;
using TowerAutobattler.Attributes;

namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class MatrixExecutionParameters : Resource
{
    [Export] public string Key { get; set; } = "";
    [Export] public string RequiredCounter { get; set; } = "";
    [Export] public float CounterThreshold { get; set; } = 1;
    [Export] public bool ConsumeCounter { get; set; }
    [Export] public float CounterConsumeRatio { get; set; } = 1;
    [Export] public bool ResetOnTargetChange { get; set; }
    [Export] public int DurationTicks { get; set; } = 30;
    [Export] public int Count { get; set; } = 1;
    [Export] public int IntervalTicks { get; set; } = 10;
    [Export] public CombatAttribute Attribute { get; set; } = CombatAttribute.AttackDamage;
    [Export] public string ContentId { get; set; } = "";
    [Export] public bool ClearOnDeath { get; set; }
}
