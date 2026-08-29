using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.Battle;

[GlobalClass]
public partial class RallyCommandContent : HeroCommandContentRoot
{
    [Export] public float ShieldAmount { get; set; }
    [Export] public int AttackCooldownCapTicks { get; set; }

    public override string Describe() =>
        $"全体友军获得 {ShieldAmount:0.##} 点护盾，攻击等待最多缩短至 {AttackCooldownCapTicks * BattleSimulation.TickSeconds:0.##} 秒。";

    public override IHeroCommandRuntime CreateRuntime() => new RallyCommandRuntime(ShieldAmount, AttackCooldownCapTicks);

    public override ValidationReport ValidateAuthoring()
    {
        var report = base.ValidateAuthoring();
        if (ShieldAmount <= 0) report.Error($"{SceneFilePath}: rally shield amount must be positive");
        if (AttackCooldownCapTicks <= 0) report.Error($"{SceneFilePath}: rally cooldown cap must be positive");
        return report;
    }
}
