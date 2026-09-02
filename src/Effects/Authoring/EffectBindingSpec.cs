using Godot;

namespace TowerAutobattler.Effects;

[GlobalClass]
public partial class EffectBindingSpec : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public int Priority { get; set; }
    [Export] public EffectTriggerSpec Trigger { get; set; } = null!;
    [Export] public Godot.Collections.Array<EffectConditionSpec> Conditions { get; set; } = [];
    [Export] public EffectTargetQuerySpec TargetQuery { get; set; } = null!;
    [Export] public Godot.Collections.Array<EffectStepSpec> Effects { get; set; } = [];
    [Export] public EffectBindingLimitsSpec Limits { get; set; } = null!;
    [Export] public EffectPresentationSpec? Presentation { get; set; }
}
