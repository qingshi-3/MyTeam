using System;
using System.Linq;
using Godot;

namespace TowerAutobattler.Battle;

public sealed class NarrowLanesRuntime(string id, string name, string preview) : ClearFloorRuleRuntime(id, name, preview)
{
    public override bool CanOccupy(Vector2I cell) => !(cell.X is 4 or 5 && cell.Y is 2 or 3);
}

public sealed class HazardPulseRuntime(string id, string name, string preview, int interval, float amount) : ClearFloorRuleRuntime(id, name, preview)
{
    public override FloorCellPreview GetCellPreview(Vector2I cell) => (cell.X + cell.Y) % 3 == 0 ? FloorCellPreview.Hazard : FloorCellPreview.Normal;
    public override void OnTick(BattleRuleContext context)
    {
        if (context.Tick % interval != 0) return;
        foreach (var unit in context.Units.Where(unit => unit.Alive && (unit.Cell.X + unit.Cell.Y) % 3 == 0).ToArray())
            context.Damage("floor", unit, amount);
    }
}

public class HealingBeaconRuntime(string id, string name, string preview, int interval, float amount) : ClearFloorRuleRuntime(id, name, preview)
{
    public override FloorCellPreview GetCellPreview(Vector2I cell) =>
        cell == new Vector2I(BattleSimulation.Width / 2, BattleSimulation.Height / 2) ? FloorCellPreview.Objective : FloorCellPreview.Normal;
    public override void OnTick(BattleRuleContext context)
    {
        if (context.Tick % interval != 0) return;
        for (var team = 0; team <= 1; team++)
            if (context.BeaconControlled(team))
                foreach (var unit in context.Allies(team)) context.Heal(unit, amount);
        context.Emit("beacon_pulse", "floor", "", amount, new Vector2I(BattleSimulation.Width / 2, BattleSimulation.Height / 2), "skill_cast");
    }
}

public sealed class BossWardRuntime(string id, string name, string preview, int interval, float amount)
    : HealingBeaconRuntime(id, name, preview, interval, amount)
{
    public override float ModifyIncomingDamage(BattleRuleContext context, BattleUnitState target, float rawDamage) =>
        target.Definition.IsBoss && !context.BeaconControlled(0) ? rawDamage * .2f : rawDamage;
}
