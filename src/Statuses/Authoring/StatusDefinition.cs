using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Statuses;

public enum StatusBehaviorKind
{
    DisableActions,
    DamageMultiplier,
    None
}

public enum StatusDisposition
{
    Neutral,
    Helpful,
    Harmful
}

public enum StatusDurationKind
{
    Permanent,
    TimedTicks,
    Instant
}

public enum StatusAggregationPolicy
{
    BySource,
    ByTarget,
    Independent
}

public enum StatusOverflowPolicy
{
    RejectNewStacks,
    RefreshDuration,
    ApplyStatusAndConsumeAtLimit
}

public enum StatusDurationRefreshPolicy
{
    None,
    Reset,
    KeepLonger,
    Extend
}

public enum StatusPeriodicResetPolicy
{
    KeepSchedule,
    ResetOnApplication
}

public enum StatusDispelCategory
{
    NonDispellable,
    Ordinary,
    StrongOnly
}

public enum StatusDeathPolicy
{
    Remove,
    Persist
}

public enum StatusControlDurationRule
{
    None,
    LinearResistanceCeiling
}

[GlobalClass]
public partial class StatusDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public StatusBehaviorKind Behavior { get; set; }
    [Export] public StatusDisposition Disposition { get; set; }
    [Export] public StatusDurationKind DurationKind { get; set; }
    [Export] public int DurationTicks { get; set; }
    [Export] public StatusAggregationPolicy AggregationPolicy { get; set; }
    [Export] public int StackLimit { get; set; } = 1;
    [Export] public StatusOverflowPolicy OverflowPolicy { get; set; }
    [Export] public StatusDefinition? OverflowStatus { get; set; }
    [Export] public int OverflowConsumeStacks { get; set; }
    [Export] public StatusDurationRefreshPolicy DurationRefreshPolicy { get; set; }
    [Export] public StatusPeriodicResetPolicy PeriodicResetPolicy { get; set; }
    [Export] public StatusDispelCategory DispelCategory { get; set; }
    [Export] public StatusDeathPolicy DeathPolicy { get; set; }
    [Export] public StatusControlDurationRule ControlDurationRule { get; set; }
    [Export] public Godot.Collections.Array<StringName> GrantedTags { get; set; } = [];
    [Export] public Godot.Collections.Array<AttributeModifierSpec> AttributeModifiers { get; set; } = [];
    [Export] public float Magnitude { get; set; } = 1f;
    [Export] public int PeriodicIntervalTicks { get; set; }
    [Export] public EffectBindingSpec? PeriodicEffect { get; set; }
    [Export] public Godot.Collections.Array<StatusLifecycleBindingSpec> LifecycleBindings { get; set; } = [];
    [Export] public Godot.Collections.Array<StatusCombatReactiveBindingSpec> CombatReactiveBindings { get; set; } = [];
    [Export] public StatusPresentationSpec? Presentation { get; set; }
}
