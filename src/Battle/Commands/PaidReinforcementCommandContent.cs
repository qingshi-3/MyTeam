using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.Battle;

[GlobalClass]
public partial class PaidReinforcementCommandContent : HeroCommandContentRoot
{
    [Export] public float HealthMultiplier { get; set; }
    [Export] public float DamageMultiplier { get; set; }

    public override string Describe() =>
        $"在英雄附近召唤一名临时雇佣兵，生命为 {Percent(HealthMultiplier)}，伤害为 {Percent(DamageMultiplier)}。";

    public override IHeroCommandRuntime CreateRuntime() => new PaidReinforcementCommandRuntime(GoldCost, HealthMultiplier, DamageMultiplier);

    public override ValidationReport ValidateAuthoring()
    {
        var report = base.ValidateAuthoring();
        if (GoldCost <= 0) report.Error($"{SceneFilePath}: paid-reinforcement gold cost must be positive");
        if (HealthMultiplier <= 0) report.Error($"{SceneFilePath}: paid-reinforcement health multiplier must be positive");
        if (DamageMultiplier <= 0) report.Error($"{SceneFilePath}: paid-reinforcement damage multiplier must be positive");
        return report;
    }
}
