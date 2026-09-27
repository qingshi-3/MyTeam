using System;
using System.Linq;
using System.Collections.Immutable;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

public partial class EnemyDifficultyContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var authored = GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres");
            var gate = await GamePackagePublisher.CreateReadyAsync(this, authored);
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var generator = new TowerGenerator(package.Project.Campaign);
            var service = new RunBattlePreparationService(package.Content, package.Project, new RunRelicService(package.Content));
            var checkedPlans = 0;
            for (var floor = 0; floor < 15; floor++)
            foreach (var type in floor % 5 == 4 ? new[] { TowerNodeType.Boss } : new[] { TowerNodeType.Combat, TowerNodeType.Elite })
            foreach (var seed in new ulong[] { 1776, 2026, 9173, 82, 109 })
            {
                var (app, _) = RunHealthFixture.New(package);
                var run = app.ActiveRun!;
                run.Seed = seed; run.FloorIndex = floor;
                var plan = generator.Encounter(run, type);
                var repeat = generator.Encounter(run, type);
                Require(plan.Title == repeat.Title && plan.CompositionId == repeat.CompositionId &&
                    plan.EnemyIds.SequenceEqual(repeat.EnemyIds) && plan.EnemyCells.SequenceEqual(repeat.EnemyCells), "deterministic composition");
                var compiled = generator.RegionFor(floor).Encounters[type];
                var template = compiled.Compositions.Single(x => x.StableId == plan.CompositionId);
                Require(template.MinLocalFloor <= floor % 5 && template.MaxLocalFloor >= floor % 5 &&
                    template.EnemyIds.SequenceEqual(plan.EnemyIds), "eligible complete template");
                var preview = service.Build(run, plan, false);
                var battle = service.Build(run, plan, true);
                Require(preview.Spawns.Select(x => (x.InstanceId, x.Cell, x.Unit.ContentId, x.Unit.MaxHealth, x.Unit.Damage, x.Unit.Armor))
                    .SequenceEqual(battle.Spawns.Select(x => (x.InstanceId, x.Cell, x.Unit.ContentId, x.Unit.MaxHealth, x.Unit.Damage, x.Unit.Armor))),
                    "preview and battle use identical stats and placements");
                var enemies = battle.Spawns.Where(x => x.Team == 1).ToArray();
                foreach (var spawn in enemies)
                {
                    Require(BattlefieldSpace.IsPositionTerrainClear(BattlefieldSpace.CellCenter(spawn.Cell), spawn.Unit.BodyRadius,
                        BattlefieldLayout.Width, BattlefieldLayout.Height, battle.FloorRule.CanOccupy), "enemy body respects terrain");
                    Require(enemies.Where(x => x.InstanceId != spawn.InstanceId).All(other =>
                        other.Cell.DistanceTo(spawn.Cell) >= other.Unit.BodyRadius + spawn.Unit.BodyRadius + BattlefieldSpace.BodyClearance), "no enemy overlap");
                    var definition = (UnitDefinition)package.Content.Catalog.Enemies.Single(x => x.StableId == spawn.Unit.ContentId).Definition;
                    Require(Math.Abs(spawn.Unit.MaxHealth - definition.MaxHealth * compiled.EnemyHealthMultiplier *
                        compiled.LocalHealthMultipliers[floor % 5]) < .01f, "real stage health");
                    Require(Math.Abs(spawn.Unit.Damage - definition.AttackDamage * compiled.EnemyDamageMultiplier *
                        compiled.LocalDamageMultipliers[floor % 5]) < .01f, "real stage attack");
                }
                if (type == TowerNodeType.Boss)
                    Require(battle.BossTimeline?.BossContentId == enemies[0].Unit.ContentId &&
                        enemies.Count(x => x.Unit.ContentId == compiled.LeadEnemyId) == 1, "single original boss and timeline");
                checkedPlans++;
            }
            // Clone only the authoring path being edited. Content/loadout identity remains the published graph.
            void Rejected(Action<EncounterDefinition> edit, string expected, bool boss = false)
            {
                var project = (GameProjectDefinition)authored.Duplicate();
                project.Campaign = (CampaignDefinition)authored.Campaign!.Duplicate();
                project.Campaign.Regions = authored.Campaign.Regions.ToArray();
                var region = project.Campaign.Regions[0] = (TowerRegionDefinition)authored.Campaign.Regions[0].Duplicate();
                region.Encounters = authored.Campaign.Regions[0].Encounters.ToArray();
                var index = Array.FindIndex(region.Encounters, x => x.NodeType == (boss ? TowerNodeType.Boss : TowerNodeType.Combat));
                var encounter = region.Encounters[index] = (EncounterDefinition)region.Encounters[index].Duplicate();
                encounter.Compositions = encounter.Compositions.Select(x => (EncounterCompositionDefinition)x.Duplicate()).ToArray();
                edit(encounter);
                var result = GameProjectCompiler.Compile(project, package.Content.Graph);
                Require(result.Project is null && result.Report.CoreErrors.Any(x => x.Contains(expected)), "reject " + expected);
            }
            Rejected(x => x.LocalHealthMultipliers = [1, 2], "local multipliers");
            Rejected(x => x.LocalDamageMultipliers = [1, 1, float.NaN, 1, 1], "local multipliers");
            Rejected(x => x.Compositions = [x.Compositions.Last()], "no composition covers");
            Rejected(x => x.Compositions[0].Cells = [new Vector2I(0, 0)], "distinct enemy-side cells");
            Rejected(x => x.Compositions[0].EnemyIds = ["missing_enemy"], "unknown composition enemy");
            Rejected(x => x.Compositions[0].EnemyIds = ["enemy_crossbow"], "exactly one encounter boss", true);

            var (settlement, save) = RunHealthFixture.New(package);
            RunHealthFixture.BattleNode(settlement, TowerNodeType.Combat);
            var real = settlement.CurrentEncounter();
            var receipt = RunHealthFixture.Result(settlement.BuildBattleConfig(real), BattleOutcome.PlayerDefeat);
            Require(!settlement.ResolveBattle(receipt, real with { CompositionId = "forged" }).Accepted &&
                !settlement.ResolveBattle(receipt, real with { EnemyCells = real.EnemyCells.Reverse().ToImmutableArray() }).Accepted,
                "reject changed template/placement without consuming receipt");
            Require(settlement.ResolveBattle(receipt, real).Accepted && settlement.ActiveRun?.CurrentRunHealth == 80 &&
                new RunApplication(package.Content, save, package.Project).ActiveRun?.FloorIndex == 1,
                "new encounters retain persisted defeat continuation");
            GD.Print($"ENEMY_DIFFICULTY_CONTRACT_OK plans={checkedPlans} determinism placement scaling boss validation receipt health");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("ENEMY_DIFFICULTY_CONTRACT_FAILED " + error); GetTree().Quit(1); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
