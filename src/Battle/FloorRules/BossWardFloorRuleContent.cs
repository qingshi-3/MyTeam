using System;
using Godot;
namespace TowerAutobattler.Battle;
[GlobalClass] public partial class BossWardFloorRuleContent : FloorRuleContentRoot { public override IBattleFloorRuleRuntime CreateRuntime() => new BossWardRuntime(Id, DisplayName, PreviewText, Math.Max(1, Mathf.RoundToInt(PulseInterval / BattleSimulation.TickSeconds)), PulseAmount); }
