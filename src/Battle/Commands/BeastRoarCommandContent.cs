using Godot;
using TowerAutobattler.Content;

namespace TowerAutobattler.Battle;

[GlobalClass]
public partial class BeastRoarCommandContent : HeroCommandContentRoot
{
    [Export] public StringName SynergyTag { get; set; } = new();
    [Export] public float DamageMultiplier { get; set; }

    public override string Describe() =>
        $"所有{DescribeTag(SynergyTag)}友军在本场战斗中伤害提高 {Percent(DamageMultiplier - 1f)}。";

    public override IHeroCommandRuntime CreateRuntime() => new BeastRoarCommandRuntime(SynergyTag.ToString(), DamageMultiplier);

    public override ValidationReport ValidateAuthoring()
    {
        var report = base.ValidateAuthoring();
        if (string.IsNullOrWhiteSpace(SynergyTag.ToString())) report.Error($"{SceneFilePath}: beast-roar synergy tag is required");
        if (DamageMultiplier <= 0) report.Error($"{SceneFilePath}: beast-roar damage multiplier must be positive");
        return report;
    }

    private static string DescribeTag(StringName tag) => tag.ToString() switch
    {
        "beast" => "野兽",
        "machine" => "机械",
        "undead" => "亡灵",
        "frost" => "霜寒",
        "desert" => "沙海",
        "order" => "秩序",
        var value => $"具备「{value}」标签的"
    };
}
