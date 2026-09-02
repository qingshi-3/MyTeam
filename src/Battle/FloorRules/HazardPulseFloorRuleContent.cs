using System;
using Godot;
namespace TowerAutobattler.Battle;
[GlobalClass] public partial class HazardPulseFloorRuleContent : FloorRuleContentRoot { public override IBattleFloorRuleRuntime CreateRuntime() => new HazardPulseRuntime(Id, DisplayName, PreviewText, Math.Max(1, Mathf.RoundToInt(PulseInterval / Domain.BattleTiming.TickSeconds)), PulseAmount); }
