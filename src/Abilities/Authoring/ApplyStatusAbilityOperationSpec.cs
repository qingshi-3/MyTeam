using Godot;
using TowerAutobattler.Effects;
using TowerAutobattler.Statuses;

namespace TowerAutobattler.Abilities;

[GlobalClass]
public partial class ApplyStatusAbilityOperationSpec : AbilityOperationSpec
{
    [Export] public StatusDefinition Status { get; set; } = null!;
    [Export] public EffectTargetQuerySpec TargetQuery { get; set; } = null!;
    // One area cue for this operation, using the same finite source/owner query as gameplay.
    [Export] public string ApplicationVfx { get; set; } = string.Empty;
}
