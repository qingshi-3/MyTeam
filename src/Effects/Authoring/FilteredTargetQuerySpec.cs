using Godot;

namespace TowerAutobattler.Effects;

[GlobalClass]
public partial class FilteredTargetQuerySpec : EffectTargetQuerySpec
{
    [Export] public EffectRelativeTeam Team { get; set; }
    [Export] public EffectEntityReference Anchor { get; set; } = EffectEntityReference.Owner;
    [Export] public bool IncludeDefeated { get; set; }
    [Export] public bool IncludeAnchor { get; set; } = true;
    [Export] public StringName RequiredTag { get; set; } = new();
    [Export] public float Range { get; set; } = -1f;
    [Export] public int MaxTargets { get; set; }
    [Export] public EffectTargetOrder Order { get; set; }
}
