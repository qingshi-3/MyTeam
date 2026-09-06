using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Effects;

namespace TowerAutobattler.Statuses;

public enum StatusReactiveOwnerRole
{
    OwnerIsSource,
    OwnerIsTarget
}

public enum StatusReactiveEffectSourcePolicy
{
    PrimaryContribution
}

[GlobalClass]
public partial class StatusCombatReactiveBindingSpec : Resource
{
    [Export] public BattleCombatEventKind EventKind { get; set; }
    [Export] public StatusReactiveOwnerRole OwnerRole { get; set; }
    [Export] public StatusReactiveEffectSourcePolicy EffectSourcePolicy { get; set; }
    [Export] public int Priority { get; set; }
    [Export] public CombatSourceKind SourceKind { get; set; }
    [Export] public bool FilterDamageType { get; set; }
    [Export] public EffectDamageType DamageType { get; set; }
    [Export] public EffectBindingSpec Binding { get; set; } = null!;
}
