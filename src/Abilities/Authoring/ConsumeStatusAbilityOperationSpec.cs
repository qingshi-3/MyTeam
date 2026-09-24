using Godot;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Abilities;

// Cash out only the scheduled remaining ticks of an explicitly named periodic status.
[GlobalClass]
public partial class ConsumeStatusAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public EffectTargetQuerySpec? TargetQuery { get; set; }
    [Export] public string StatusId { get; set; } = "";
    [Export] public float DamageMultiplier { get; set; } = 1;
    [Export] public EffectDamageType DamageType { get; set; } = EffectDamageType.Normal;
}
