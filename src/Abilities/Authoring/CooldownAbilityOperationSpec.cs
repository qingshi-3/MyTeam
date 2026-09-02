using Godot;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class CooldownAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public EffectTargetQuerySpec TargetQuery { get; set; } = null!;
    [Export] public CooldownAdjustmentKind AttackAdjustment { get; set; }
    [Export] public int AttackValue { get; set; }
    [Export] public CooldownAdjustmentKind MoveAdjustment { get; set; }
    [Export] public int MoveValue { get; set; }
}
