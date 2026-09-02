using Godot;

namespace TowerAutobattler.Effects;

[GlobalClass]
public partial class RelativeTeamTargetQuerySpec : EffectTargetQuerySpec
{
    [Export] public EffectRelativeTeam Team { get; set; }
    [Export] public bool IncludeDefeated { get; set; }
    [Export] public StringName RequiredTag { get; set; } = new();
}
