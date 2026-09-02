using Godot;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Statuses;

public enum StatusLifecycleTriggerKind
{
    Applied,
    StackChanged,
    Removed
}

[GlobalClass]
public partial class StatusLifecycleBindingSpec : Resource
{
    [Export] public StatusLifecycleTriggerKind Trigger { get; set; }
    [Export] public EffectBindingSpec Binding { get; set; } = null!;
}
