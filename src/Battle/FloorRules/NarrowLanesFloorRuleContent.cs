using Godot;
namespace TowerAutobattler.Battle;
[GlobalClass] public partial class NarrowLanesFloorRuleContent : FloorRuleContentRoot { public override IBattleFloorRuleRuntime CreateRuntime() => new NarrowLanesRuntime(Id, DisplayName, PreviewText); }
