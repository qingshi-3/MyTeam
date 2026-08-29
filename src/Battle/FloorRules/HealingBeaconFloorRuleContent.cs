using System;
using Godot;
namespace TowerAutobattler.Battle;
[GlobalClass] public partial class HealingBeaconFloorRuleContent : FloorRuleContentRoot { public override IBattleFloorRuleRuntime CreateRuntime() => new HealingBeaconRuntime(Id, DisplayName, PreviewText, Math.Max(1, Mathf.RoundToInt(PulseInterval / BattleSimulation.TickSeconds)), PulseAmount); }
