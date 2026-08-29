using Godot;

namespace TowerAutobattler.Battle;

[GlobalClass]
public partial class OverclockCommandContent : HeroCommandContentRoot
{
    public override string Describe() => "清零全体友军的攻击与移动等待。";
    public override IHeroCommandRuntime CreateRuntime() => new OverclockCommandRuntime();
}
