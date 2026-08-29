using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.Battle;

[GlobalClass]
public partial class RaiseDeadCommandContent : HeroCommandContentRoot
{
    [Export] public int SummonCount { get; set; }
    [Export] public float HealthMultiplier { get; set; }
    [Export] public float DamageMultiplier { get; set; }

    public override string Describe() =>
        $"在英雄附近最多召唤 {SummonCount} 名临时骸骨，生命为 {Percent(HealthMultiplier)}，伤害为 {Percent(DamageMultiplier)}。";

    public override IHeroCommandRuntime CreateRuntime() => new RaiseDeadCommandRuntime(SummonCount, HealthMultiplier, DamageMultiplier);

    public override ValidationReport ValidateAuthoring()
    {
        var report = base.ValidateAuthoring();
        if (SummonCount <= 0) report.Error($"{SceneFilePath}: raise-dead summon count must be positive");
        if (HealthMultiplier <= 0) report.Error($"{SceneFilePath}: raise-dead health multiplier must be positive");
        if (DamageMultiplier <= 0) report.Error($"{SceneFilePath}: raise-dead damage multiplier must be positive");
        return report;
    }
}
