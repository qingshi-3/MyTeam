using Godot;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class ApplyStatusAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public StatusDefinition Status { get; set; } = null!;
    [Export] public EffectTargetQuerySpec TargetQuery { get; set; } = null!;
}
