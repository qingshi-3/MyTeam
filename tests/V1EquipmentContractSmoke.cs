using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Equipment;
using TowerAutobattler.Statuses;

public partial class V1EquipmentContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var definitions = new[] { 2, 3, 6, 7, 10, 11, 15 }.ToDictionary(number => number, number =>
            {
                var result = EquipmentDefinitionCompiler.Compile(GD.Load<EquipmentDefinition>(
                    $"res://content/equipment/definitions/v1_equipment_e{number:00}.tres"),
                    status => StatusDefinitionCompiler.Compile(status).Definition);
                Require(result.Definition is not null, string.Join(";", result.Report.CoreErrors));
                return result.Definition!;
            });
            using (var attributes = new BattleAttributeScope("equipment-first-batch"))
            {
                var owner = attributes.CreateSet("hero", AttributeDefinitionCompiler.Legacy(new Dictionary<CombatAttribute, float>
                {
                    [CombatAttribute.MaxHealth] = 700, [CombatAttribute.AttackDamage] = 52,
                    [CombatAttribute.StartingMana] = 20, [CombatAttribute.ManaPerAttack] = 10
                }));
                var instances = new[] { 2, 2, 7 }.Select((number, slot) => new EquipmentBattleInstanceSnapshot(
                    "item-" + slot, definitions[number].StableId, "hero", slot, definitions[number])).ToImmutableArray();
                using var equipment = new EquipmentBattleScope("equipment-first-batch",
                    new(EquipmentStateFingerprint.Compute(instances), instances), [new("hero", "hero", true, owner)]);
                Require(owner.GetValue(CombatAttribute.AttackDamage) == 72 && owner.GetValue(CombatAttribute.StartingMana) == 50,
                    "repeated spears stack their ordinary attributes");
                Require(owner.GetValue(CombatAttribute.ManaPerAttack) == 15, "repeated spear mana rule applies only once");
                Require(owner.GetBaseValue(CombatAttribute.MaxHealth) == 950 && owner.GetValue(CombatAttribute.MaxHealth) == 950,
                    "belt belongs to removable base-health layer");
                owner.AddBaseValue(CombatAttribute.MaxHealth, 10);
                Require(owner.GetValue(CombatAttribute.MaxHealth) == 960, "permanent base increment counts belt only once");
                var checkpoint = owner.CaptureModifierState();
                Require(equipment.Remove("item-0") && owner.GetValue(CombatAttribute.ManaPerAttack) == 15 &&
                    owner.GetValue(CombatAttribute.AttackDamage) == 62, "removing unique provider transfers rule to remaining copy");
                Require(equipment.Remove("item-1") && owner.GetValue(CombatAttribute.ManaPerAttack) == 10,
                    "removing last copy removes the unique mana contribution");
                Require(equipment.Remove("item-2") && owner.GetBaseValue(CombatAttribute.MaxHealth) == 710,
                    "removing belt restores original base without residue");
                owner.RestoreModifierState(checkpoint);
                Require(owner.GetBaseValue(CombatAttribute.MaxHealth) == 960, "base-layer checkpoint restores removable modifiers");
            }
            var content = await SkillSampleContent.LoadAsync(this);
            var config = AbilityPrototypeContent.CreateBattle(content, [11, 13, 8, 0, 0, 0], [2, 4, 7, 19, 3, 5]);
            // Isolate base-health scaling from the dragon's separately tested fixed grants.
            config.Spawns[1] = config.Spawns[1] with { Unit = config.Spawns[1].Unit with { AbilityLoadout = null } };
            var belt = ImmutableArray.Create(new EquipmentBattleInstanceSnapshot("belt", definitions[7].StableId,
                "prototype-0-0", 0, definitions[7]));
            config = new BattleConfig { Seed = config.Seed, HeroRule = config.HeroRule, FloorRule = config.FloorRule,
                Spawns = config.Spawns, Traits = config.Traits, TacticalSummons = config.TacticalSummons,
                Equipment = new(EquipmentStateFingerprint.Compute(belt), belt) };
            using (var battle = new BattleSimulation(config))
            {
                var hunter = battle.Units.Single(unit => unit.SourceInstanceId == "prototype-0-0");
                Require(hunter.MaxHealth == 950, "real preparation includes belt before trait activation");
                for (var tick = 0; tick < 400 && !battle.CombatEvents.Any(value =>
                    value.Kind == BattleCombatEventKind.UnitTransformed && value.SourceRuntimeId == hunter.RuntimeId); tick++) battle.Step();
                Require(battle.CombatEvents.Any(value => value.Kind == BattleCombatEventKind.UnitTransformed &&
                    value.SourceRuntimeId == hunter.RuntimeId), "real hunter transformation occurred");
                Require(Math.Abs(hunter.MaxHealth - (950 * 1.4f)) < .01f,
                    $"shapeshifter scales the removable belt base: actual={hunter.MaxHealth}");
            }
            LifeStealCombat(content, definitions[10]);
            ArmorBreakCombat(content, definitions[11]);
            GD.Print("V1_EQUIPMENT_CONTRACT_OK seven-authored-items life-steal-health-area-overkill armor-break-hit-refresh repeated-unique-removal removable-base checkpoint real-shapeshifter");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("V1_EQUIPMENT_CONTRACT_FAILED " + error); GetTree().Quit(1); }
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
