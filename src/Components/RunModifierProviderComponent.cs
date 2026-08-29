using Godot;

namespace TowerAutobattler.Components;

public abstract partial class RunModifierProviderComponent : Node
{
    [Export] public float ArmyHealthMultiplier { get; set; } = 1f;
    [Export] public float ArmyDamageMultiplier { get; set; } = 1f;
    [Export] public float HeroHealthMultiplier { get; set; } = 1f;
    [Export] public float HeroDamageMultiplier { get; set; } = 1f;
    [Export] public float ArmyLifeStealBonus { get; set; }
    [Export] public float HeroLifeStealBonus { get; set; }
    [Export] public int StartBattleShield { get; set; }
    [Export] public int GoldPerBattle { get; set; }
    [Export] public int EmptySlotPower { get; set; }
    [Export] public bool SummonToken { get; set; }
    [Export] public float FormationAdjacentArmor { get; set; }
    [Export] public float FormationAdjacentDamageMultiplier { get; set; } = 1f;
    [Export] public string SummonContentId { get; set; } = string.Empty;
}
