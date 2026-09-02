using Godot;

namespace TowerAutobattler.Effects;

[GlobalClass]
public partial class EffectTriggerSpec : Resource
{
    [Export] public EffectTriggerKind Kind { get; set; }
    [Export] public EffectDomainEventKind EventKind { get; set; }
}
