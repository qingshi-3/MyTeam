using Godot;

namespace TowerAutobattler.Abilities;

public enum AbilityInvocationValueSource
{
    Fixed,
    OwnerMaxHealth
}

public enum CooldownAdjustmentKind
{
    None,
    Reset,
    Add,
    Cap,
    Divide
}

public enum AbilitySummonProfile
{
    DeathSummon,
    HeroConstruct,
    Mercenary,
    ItemToken,
    BehaviorSummon
}

[GlobalClass]
public partial class AbilityOperationSpec : Resource
{
}
