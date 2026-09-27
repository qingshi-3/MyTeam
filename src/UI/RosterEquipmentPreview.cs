using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Equipment;
using TowerAutobattler.Growth;
using TowerAutobattler.Run;

namespace TowerAutobattler.UI;

// Outside a concrete encounter (including reserves), show the real self/equipment
// initialization without pretending that positional auras or floor rules apply.
public static class RosterEquipmentPreview
{
    public static PreparedUnitDetails Build(ContentRegistry content, RosterHeroInstanceDto hero, UnitSnapshot baseline,
        CompiledGrowthRules? growthRules = null)
    {
        baseline = RunBattlePreparationAdapter.ProjectGrowth(hero, growthRules)?.Apply(baseline) ?? baseline;
        var equipment = hero.Equipment.Select(item => new EquipmentBattleInstanceSnapshot(
            item.InstanceId, item.ContentId, hero.InstanceId, item.SlotIndex,
            content.Graph.ResolveEquipment(item.ContentId))).ToImmutableArray();
        using var simulation = new BattleSimulation(new BattleConfig
        {
            Seed = 1,
            FloorRule = new ClearFloorRuleRuntime("roster-inspection", "自身与装备", ""),
            Spawns = [new BattleSpawn(baseline, 0, new Vector2I(1, 2), hero.InstanceId)],
            HeroRule = HeroRuleSnapshot.Neutral,
            Equipment = new EquipmentBattlePreparation(EquipmentStateFingerprint.Compute(equipment), equipment)
        });
        var unit = simulation.Units.Single(unit => unit.SourceInstanceId == hero.InstanceId);
        return new PreparedUnitDetails(unit.Definition,
            Enum.GetValues<CombatAttribute>().ToDictionary(attribute => attribute, attribute => unit.Attributes.GetValue(attribute)),
            unit.Health, unit.Shield, "自身与装备 · 羁绊、遗物及站位加成以备战为准");
    }
}
