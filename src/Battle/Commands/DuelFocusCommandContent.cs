using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.Battle;

[GlobalClass]
public partial class DuelFocusCommandContent : HeroCommandContentRoot
{
    [Export] public float ShieldRatio { get; set; }

    public override string Describe() =>
        $"英雄获得相当于最大生命 {Percent(ShieldRatio)} 的护盾，并清零攻击等待。";

    public override IHeroCommandRuntime CreateRuntime() => new DuelFocusCommandRuntime(ShieldRatio);

    public override ValidationReport ValidateAuthoring()
    {
        var report = base.ValidateAuthoring();
        if (ShieldRatio <= 0) report.Error($"{SceneFilePath}: duel-focus shield ratio must be positive");
        return report;
    }
}
