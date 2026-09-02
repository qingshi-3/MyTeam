using Godot;

namespace TowerAutobattler.Relics;

public enum RelicBattleModifierKind
{
    ArmyHealthMultiplier,
    ArmyDamageMultiplier,
    HeroHealthMultiplier,
    HeroDamageMultiplier,
    ArmyLifeStealBonus,
    HeroLifeStealBonus,
    StartBattleShield,
    EmptySlotPower,
    SummonToken,
    FormationAdjacentArmor,
    FormationAdjacentDamageMultiplier
}

public enum RelicRunOutcomeKind
{
    VictoryGold
}

[GlobalClass]
public partial class RelicDefinition : Resource
{
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public RelicAttributeBindingSpec[] AttributeBindings { get; set; } = [];
    [Export] public RelicBattleStartEffectSpec[] BattleStartEffects { get; set; } = [];
    [Export] public RelicBattleModifierSpec[] BattleModifiers { get; set; } = [];
    [Export] public RelicReactiveCounterSpec[] ReactiveCounters { get; set; } = [];
    [Export] public RelicRunOutcomeSpec[] VictoryOutcomes { get; set; } = [];
}
