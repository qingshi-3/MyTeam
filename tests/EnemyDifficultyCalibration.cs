using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

// Fixed-budget combat probes, not an estimate of natural acquisition or player win rates.
// All actors, equipment, relics, terrain and boss timelines use the published Run path.
public partial class EnemyDifficultyCalibration : Node
{
    private sealed record Probe(string Name, string[] Heroes, (int Owner, string Item)[] Gear, string Relic);
    private sealed record Observation(string Team, int Floor, string Kind, ulong Seed, int Heroes, int ItemBudget,
        string[] Roster, string[] Equipment, string Relic, string Encounter, string Rule, string[] Enemies,
        string Outcome, int Ticks, int Survivors, double HealthLeft, double EnemyHealthLeft, string Digest);
    private static string H(string name) => "hero_" + name;
    private static string E(string name) => "equipment_" + name;

    public override async void _Ready()
    {
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var records = new List<Observation>();
            var selectedFloors = System.Environment.GetEnvironmentVariable("CALIBRATION_FLOORS")?.Split(',').Select(int.Parse).ToArray()
                ?? [0, 3, 4, 5, 8, 9, 10, 13, 14];
            foreach (var floor in selectedFloors)
            {
                foreach (var probe in Probes(floor))
                foreach (var seed in new ulong[] { 1776, 2026, 9173 })
                foreach (var type in floor % 5 == 4 ? new[] { TowerNodeType.Boss } :
                    floor == 0 ? new[] { TowerNodeType.Combat } : new[] { TowerNodeType.Combat, TowerNodeType.Elite })
                {
                    var run = new ActiveRunDto { Seed = seed, FloorIndex = floor, BattleNumber = floor,
                        CurrentPopulation = 7, CurrentRunHealth = 100, MaximumRunHealth = 100, Gold = 0,
                        EquippedTacticalCommandIds = package.Project.RunRules.StarterTacticalCommandIds.ToList() };
                    // Front tank, rear carry and support; remaining melee/ranged placed by their real range.
                    for (var i = 0; i < probe.Heroes.Length; i++)
                    {
                        var hero = new RosterHeroInstanceDto { InstanceId = "probe-" + i, ContentId = H(probe.Heroes[i]) };
                        run.Roster.Add(hero);
                        var unit = (UnitDefinition)package.Content.Catalog.Heroes.Single(x => x.StableId == hero.ContentId).Definition;
                        var cell = i >= 5 ? new Vector2I(2, i == 5 ? 0 : 3) : i == 0 ? new Vector2I(2, 2) : unit.AttackRange < 2
                            ? new Vector2I(2, i < 3 ? i - 1 : i + 1) : new Vector2I(i % 2, i < 3 ? i + 1 : i - 2);
                        run.Deployment[BattlefieldLayout.PlayerDeploymentSlot(cell)] = hero.InstanceId;
                    }
                    foreach (var (owner, item) in probe.Gear)
                    {
                        var hero = run.Roster[owner];
                        hero.Equipment.Add(new() { InstanceId = $"gear-{owner}-{hero.Equipment.Count}", ContentId = E(item),
                            OwnerHeroInstanceId = hero.InstanceId, SlotIndex = hero.Equipment.Count });
                    }
                    if (probe.Relic.Length > 0) run.Items.Add(new() { InstanceId = "relic", ContentId = probe.Relic });
                    var encounter = new TowerGenerator(package.Project.Campaign).Encounter(run, type);
                    var config = new RunBattlePreparationService(package.Content, package.Project, new RunRelicService(package.Content))
                        .Build(run, encounter, true);
                    using var battle = new BattleSimulation(config);
                    var result = battle.RunToEnd();
                    var allies = result.Units.Where(x => x.Team == 0 && !x.IsTemporary).ToArray();
                    var enemies = result.Units.Where(x => x.Team == 1).ToArray();
                    records.Add(new(probe.Name, floor + 1, type.ToString(), seed, probe.Heroes.Length,
                        probe.Gear.Length * 18 + (probe.Relic.Length > 0 ? 24 : 0), probe.Heroes,
                        probe.Gear.Select(x => $"{x.Owner}:{x.Item}").ToArray(), probe.Relic, encounter.Title,
                        encounter.FloorRuleId, encounter.EnemyIds.ToArray(), result.Outcome.ToString(), result.Ticks,
                        allies.Count(x => x.Alive), Math.Round(allies.Sum(x => x.FinalHealth) / allies.Sum(x => x.MaxHealth), 3),
                        Math.Round(enemies.Sum(x => x.FinalHealth) / enemies.Sum(x => x.MaxHealth), 3), result.Digest));
                }
                GD.Print($"CALIBRATION_PROGRESS floor={floor + 1} battles={records.Count}");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            var label = System.Environment.GetEnvironmentVariable("CALIBRATION_LABEL") ?? "current";
            if (label.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-')) throw new InvalidOperationException("Unsafe output label");
            var directory = ProjectSettings.GlobalizePath("res://design-discussion/04-content-validation/artifacts/difficulty");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, label + ".json"), JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }));
            foreach (var group in records.GroupBy(x => new { x.Floor, x.Kind, x.Team }))
                GD.Print($"CALIBRATION {group.Key} wins={group.Count(x => x.Outcome == "PlayerVictory")}/{group.Count()} " +
                    $"ticks={group.Average(x => x.Ticks):0} hp={group.Average(x => x.HealthLeft):0.00} enemy={group.Average(x => x.EnemyHealthLeft):0.00}");
            GD.Print("ENEMY_DIFFICULTY_CALIBRATION_OK " + records.Count);
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("ENEMY_DIFFICULTY_CALIBRATION_FAILED " + error); GetTree().Quit(1); }
    }

    private static IEnumerable<Probe> Probes(int floor)
    {
        if (floor == 0)
        {
            yield return new("opening-guard", ["hc03_iron_guard", "hc01_crossbow"], [], "");
            yield return new("opening-shield", ["hc15_shield_grower", "hc08_poison_keeper"], [], "");
            yield return new("opening-swarm", ["hc24_swarm_keeper", "hc09_breach_scribe"], [], "");
            yield break;
        }
        var mid = floor >= 5;
        var late = floor >= 10;
        Probe Make(string name, string[] heroes, (int, string)[] gear, string relic) =>
            new(name, heroes.Take(late ? 5 : mid ? 4 : 3).ToArray(), gear.Take(late ? 4 : mid ? 2 : 1).ToArray(), mid ? relic : "");
        yield return Make("scatter-a", ["hc03_iron_guard", "hc01_crossbow", "hc15_shield_grower", "hc20_blood_drummer", "hc39_hook_machine"],
            [(0,"ne10_swift_gloves"),(2,"field_focus"),(1,"vanguard_insignia"),(3,"ne10_winter_badge")], "item_ne01_opening_bulwark");
        yield return Make("scatter-b", ["hc15_shield_grower", "hc01_crossbow", "hc39_hook_machine", "hc38_grit_brawler", "hc20_blood_drummer"],
            [(0,"ne10_swift_gloves"),(2,"field_focus"),(1,"vanguard_insignia"),(3,"ne10_winter_badge")], "item_ne01_opening_bulwark");
        yield return Make("attack-pair", ["hc03_iron_guard", "hc01_crossbow", "hc09_breach_scribe", "hc18_healing_reader", "hc39_hook_machine"],
            [(1,"ne10_swift_gloves"),(2,"ne01_mana_charm"),(1,"field_focus"),(3,"ne01_mana_charm")], "item_ne01_opening_bulwark");
        yield return Make("poison", ["hc03_iron_guard", "hc08_poison_keeper", "hc18_healing_reader", "hc15_shield_grower", "hc09_breach_scribe"],
            [(1,"ne01_mana_charm"),(2,"ne01_mana_charm"),(0,"vanguard_insignia"),(1,"field_focus")], "item_ne01_opening_bulwark");
        if (!mid) yield break; // T3/T4 cores cannot be assumed before the first boss.
        yield return Make("summon", ["hc03_iron_guard", "hc25_death_provider", "hc24_swarm_keeper", "hc18_healing_reader", "hc07_death_echo"],
            [(1,"ne01_mana_charm"),(2,"ne01_mana_charm"),(3,"ne01_mana_charm"),(4,"ne01_mana_charm")], "item_ne02_bone_vigor");
        yield return Make("winter", ["hc03_iron_guard", "hc23_frost_historian", "hc01_crossbow", "hc18_healing_reader", "hc09_breach_scribe"],
            [(2,"ne10_winter_badge"),(1,"ne10_swift_gloves"),(0,"ne10_winter_badge"),(3,"ne10_winter_badge")], "item_ne10_control_shelter");
        yield return Make("winter-missing", ["hc03_iron_guard", "hc23_frost_historian", "hc01_crossbow", "hc18_healing_reader", "hc09_breach_scribe"],
            [(2,"ne01_mana_charm"),(1,"ne10_swift_gloves"),(0,"field_focus"),(3,"ne10_swift_gloves")], "item_ne10_control_shelter");
        // Skill-only healing no longer masks an unprotected frontline. Two badges already
        // reach four members at the middle checkpoint; the late support adds real defense.
        yield return Make("winter-formed", ["hc03_iron_guard", "hc23_frost_historian", "hc01_crossbow", "hc18_healing_reader", "hc30_armor_broadcaster"],
            [(2,"ne10_winter_badge"),(3,"ne10_winter_badge"),(1,"ne10_swift_gloves"),(0,"vanguard_insignia")], "item_ne10_control_shelter");
        if (!late) yield break;
        // Additional stress probes: more bodies at a lower total cost, and no redundant member badge.
        yield return new("population-only", ["hc03_iron_guard", "hc01_crossbow", "hc15_shield_grower", "hc20_blood_drummer", "hc39_hook_machine", "hc38_grit_brawler", "hc26_ash_returner"],
            [(0,"ne10_swift_gloves"),(2,"field_focus"),(1,"vanguard_insignia")], "item_ne01_opening_bulwark");
        yield return Make("winter-efficient", ["hc03_iron_guard", "hc23_frost_historian", "hc01_crossbow", "hc18_healing_reader", "hc09_breach_scribe"],
            [(2,"ne10_winter_badge"),(1,"ne10_swift_gloves"),(1,"field_focus"),(3,"ne10_winter_badge")], "item_ne10_control_shelter");
        yield return Make("poison-missing", ["hc03_iron_guard", "hc08_poison_keeper", "hc20_blood_drummer", "hc15_shield_grower", "hc09_breach_scribe"],
            [(1,"ne01_mana_charm"),(2,"ne01_mana_charm"),(0,"vanguard_insignia"),(1,"field_focus")], "item_ne01_opening_bulwark");
        yield return Make("summon-missing", ["hc03_iron_guard", "hc38_grit_brawler", "hc24_swarm_keeper", "hc18_healing_reader", "hc07_death_echo"],
            [(1,"ne01_mana_charm"),(2,"ne01_mana_charm"),(3,"ne01_mana_charm"),(4,"ne01_mana_charm")], "item_ne02_bone_vigor");
    }
}
