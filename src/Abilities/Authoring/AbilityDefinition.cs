using Godot;

namespace TowerAutobattler.Abilities;

public enum AbilityActivationKind
{
    ManualCommand,
    Automatic,
    Triggered,
    Passive
}

public enum AbilityTriggerKind
{
    None,
    BattleStarted,
    PeriodicTick,
    AttackHit,
    OwnerDefeated
}

[GlobalClass]
public partial class AbilityDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public AbilityActivationKind ActivationKind { get; set; }
    [Export] public AbilityTriggerKind Trigger { get; set; }
    [Export] public int ManaCost { get; set; }
    [Export] public int GoldCost { get; set; }
    [Export] public int CooldownTicks { get; set; }
    [Export] public int MaxUses { get; set; }
    [Export] public int IntervalTicks { get; set; }
    [Export] public Godot.Collections.Array<AbilityOperationSpec> Operations { get; set; } = [];
    [Export] public AbilityPresentationSpec? Presentation { get; set; }
}
