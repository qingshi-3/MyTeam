using Godot;

namespace TowerAutobattler.Relics;

public enum RelicBattleStartRepeatPolicy
{
    PerStack,
    OncePerBattleBinding
}

[GlobalClass]
public partial class RelicBattleStartEffectSpec : Resource
{
    [Export] public string BindingId { get; set; } = string.Empty;
    [Export] public RelicBattleStartRepeatPolicy RepeatPolicy { get; set; }
}
