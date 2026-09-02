using Godot;

namespace TowerAutobattler.Effects;

[GlobalClass]
public partial class EffectBindingLimitsSpec : Resource
{
    [Export] public int MaxUses { get; set; }
    [Export] public int MinimumIntervalTicks { get; set; }
    [Export] public int MaxDepth { get; set; }
    [Export] public int MaxRepeatedEdges { get; set; }
}
