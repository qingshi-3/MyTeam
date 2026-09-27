using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

// Same naked four-unit bench for each candidate, not a tier list or acquisition simulation.
public partial class PlayerUnitBalanceCalibration : Node
{
    public override async void _Ready()
    {
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var records = new List<object>();
            var filter = System.Environment.GetEnvironmentVariable("PLAYER_BALANCE_HEROES")?.Split(',');
            foreach (var candidate in package.Content.Catalog.Heroes.OrderBy(x => x.StableId, StringComparer.Ordinal)
                         .Where(x => filter is null || filter.Contains(x.StableId)))
            {
                foreach (var floor in new[] { 3, 5 })
                {
                    var run = new ActiveRunDto { Seed = 2026, FloorIndex = floor, BattleNumber = floor,
                        CurrentPopulation = 4, CurrentRunHealth = 100, MaximumRunHealth = 100,
                        EquippedTacticalCommandIds = package.Project.RunRules.StarterTacticalCommandIds.ToList() };
                    var ids = new[] { candidate.StableId, "hero_hc03_iron_guard", "hero_hc01_crossbow", "hero_hc18_healing_reader",
                        "hero_hc15_shield_grower" }.Distinct().Take(4).ToArray();
                    var melee = 0; var ranged = 0;
                    for (var i = 0; i < ids.Length; i++)
                    {
                        var hero = new RosterHeroInstanceDto { InstanceId = "bench-" + i, ContentId = ids[i] };
                        run.Roster.Add(hero);
                        var definition = (UnitDefinition)package.Content.Catalog.Heroes.Single(x => x.StableId == ids[i]).Definition;
                        var cell = definition.AttackRange < 2 ? new Vector2I(2, 1 + melee++ * 2) : new Vector2I(0, 1 + ranged++);
                        run.Deployment[BattlefieldLayout.PlayerDeploymentSlot(cell)] = hero.InstanceId;
                    }
                    var plan = new TowerGenerator(package.Project.Campaign).Encounter(run, TowerNodeType.Elite);
                    var config = new RunBattlePreparationService(package.Content, package.Project, new RunRelicService(package.Content)).Build(run, plan, true);
                    using var battle = new BattleSimulation(config);
                    var result = battle.RunToEnd();
                    var player = result.Units.Where(x => x.Team == 0 && !x.IsTemporary).ToArray();
                    var subject = player.Single(x => x.RuntimeId == "bench-0");
                    var events = battle.CombatEvents.Where(x => x.SourceRuntimeId == "bench-0").ToArray();
                    records.Add(new { Hero = candidate.StableId, Name = subject.DisplayName, Floor = floor + 1,
                        Outcome = result.Outcome.ToString(), result.Ticks,
                        Remaining = player.Sum(x => x.FinalHealth) / player.Sum(x => x.MaxHealth),
                        subject.DamageDealt, subject.HealingDone, subject.DamageTaken, subject.ShieldAbsorbed,
                        subject.AttackActions, subject.DefeatTick,
                        Casts = events.Count(x => x.Kind == BattleCombatEventKind.ManaSkillResolved),
                        Punches = battle.PendingEvents.Count(x => x.SourceRuntimeId == "bench-0" && x.Type == "line_release"),
                        TeamDamage = player.Sum(x => x.DamageDealt), result.Digest });
                }
                GD.Print("PLAYER_BALANCE_PROGRESS " + candidate.StableId);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            var label = System.Environment.GetEnvironmentVariable("PLAYER_BALANCE_LABEL") ?? "after";
            if (label.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-')) throw new InvalidOperationException("Unsafe label");
            var directory = ProjectSettings.GlobalizePath("res://design-discussion/04-content-validation/artifacts/player-balance");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, label + ".json"), JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }));
            GD.Print("PLAYER_BALANCE_CALIBRATION_OK " + records.Count);
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("PLAYER_BALANCE_CALIBRATION_FAILED " + error); GetTree().Quit(1); }
    }
}
