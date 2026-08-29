using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.Battle;

[GlobalClass]
public partial class BloodRushCommandContent : HeroCommandContentRoot
{
    [Export] public float HealRatio { get; set; }
    [Export] public float DamageMultiplier { get; set; }

    public override string Describe() =>
        $"英雄恢复最大生命的 {Percent(HealRatio)}，伤害提高 {Percent(DamageMultiplier - 1f)}，并清零攻击等待。";

    public override IHeroCommandRuntime CreateRuntime() => new BloodRushCommandRuntime(HealRatio, DamageMultiplier);

    public override ValidationReport ValidateAuthoring()
    {
        var report = base.ValidateAuthoring();
        if (HealRatio <= 0 || HealRatio > 1) report.Error($"{SceneFilePath}: blood-rush heal ratio must be within (0, 1]");
        if (DamageMultiplier <= 0) report.Error($"{SceneFilePath}: blood-rush damage multiplier must be positive");
        return report;
    }
}
