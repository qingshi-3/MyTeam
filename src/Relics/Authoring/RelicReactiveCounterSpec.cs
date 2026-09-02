using Godot;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Relics;

public enum RelicCounterScope
{
    Battle,
    Run
}

public enum RelicCounterResetPolicy
{
    BattleEnd,
    RunEnd
}

public enum RelicCounterSourceKind
{
    Population,
    Alive,
    Attack,
    Death
}

public enum RelicThresholdTargetKind
{
    EventSource,
    EventTarget,
    FirstAliveTeamUnit
}

[GlobalClass]
public partial class RelicReactiveCounterSpec : Resource
{
    [Export] public string CounterId { get; set; } = string.Empty;
    [Export] public RelicCounterScope Scope { get; set; }
    [Export] public RelicCounterResetPolicy ResetPolicy { get; set; }
    [Export] public RelicCounterSourceKind Source { get; set; }
    [Export] public int Team { get; set; }
    [Export] public bool IncludeTemporary { get; set; }
    [Export] public int Threshold { get; set; } = 1;
    [Export] public int Consumption { get; set; } = 1;
    [Export] public int Priority { get; set; }
    [Export] public RelicThresholdTargetKind Target { get; set; }
    [Export] public int TargetTeam { get; set; }
    [Export] public EffectBindingSpec ThresholdEffect { get; set; } = null!;
}
