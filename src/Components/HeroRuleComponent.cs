using Godot;

namespace TowerAutobattler.Components;

[GlobalClass]
public partial class HeroRuleComponent : Node
{
    [Export] public string RuleTitle { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string RuleDescription { get; set; } = string.Empty;
    [Export] public float SoldierHealthMultiplier { get; set; } = 1f;
    [Export] public float SoldierDamageMultiplier { get; set; } = 1f;
    [Export] public float HeroDamageMultiplier { get; set; } = 1f;
    [Export] public float EmptySlotHeroBonus { get; set; }
    [Export] public float EmptySlotHeroDefense { get; set; }
    [Export] public float EmptySlotStartShield { get; set; }
    [Export] public bool PreferBossTargets { get; set; }
    [Export] public StringName RequiredSoldierTag { get; set; } = new();
    [Export] public float TaggedSoldierHealthMultiplier { get; set; } = 1f;
    [Export] public float TaggedSoldierDamageMultiplier { get; set; } = 1f;
    [Export] public float FormationArmorBonus { get; set; }
    [Export] public float FormationDamageBonus { get; set; }
    [Export] public float KillGrowth { get; set; }
    [Export] public float HeroLifeStealBonus { get; set; }
    [Export] public bool SummonOnAllyDeath { get; set; }
    [Export] public bool AddBattleConstruct { get; set; }
    [Export] public int BattleGoldBonus { get; set; }
    [Export] public int RecruitConversionGold { get; set; }
    [Export] public string SummonContentId { get; set; } = string.Empty;
}
