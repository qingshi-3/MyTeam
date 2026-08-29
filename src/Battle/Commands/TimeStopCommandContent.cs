using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.Battle;

[GlobalClass]
public partial class TimeStopCommandContent : HeroCommandContentRoot
{
    [Export] public int DisableTicks { get; set; }
    [Export] public int AllyCooldownDivisor { get; set; }

    public override string Describe() =>
        $"敌军禁用 {DisableTicks * BattleSimulation.TickSeconds:0.##} 秒，友军攻击等待缩短为原来的 1/{AllyCooldownDivisor}。";

    public override IHeroCommandRuntime CreateRuntime() => new TimeStopCommandRuntime(DisableTicks, AllyCooldownDivisor);

    public override ValidationReport ValidateAuthoring()
    {
        var report = base.ValidateAuthoring();
        if (DisableTicks <= 0) report.Error($"{SceneFilePath}: time-stop duration must be positive");
        if (AllyCooldownDivisor <= 0) report.Error($"{SceneFilePath}: time-stop cooldown divisor must be positive");
        return report;
    }
}
