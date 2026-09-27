using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.ValidationMatrix;
using static TraitMatrixValidationSupport;

public partial class TraitMatrixMatchupDiagnostics : Node
{
    private sealed record Progression(string Id, int Floor, int[] Tiers);
    private sealed record Team(string Id, string Direction, string Progression, string[] Heroes, int[] Tiers, object[] ActualTraits);
    private sealed record Record(string TeamId, string Direction, string Progression, int Floor, string Kind, ulong Seed,
        string EncounterId, string CompositionId, string FloorRuleId, int Population, int[] TierVector,
        int EquipmentBudget, int RelicBudget, string[] Heroes, object[] ActualTraits, Observation Battle);
    private static readonly ulong[] Seeds = [1776, 2026, 9173];
    private static readonly Progression[] Stages = [new("early", 3, [1, 1, 1, 2, 2]),
        new("middle", 8, [1, 1, 2, 2, 3, 3]), new("late", 13, [1, 1, 1, 2, 2, 2, 3, 3, 4, 4])];

    public override async void _Ready()
    {
        try
        {
            var plan = ReadPlan();
            var publication = await ValidationMatrixPackage.CreateReadyAsync(this);
            var package = publication.Package ?? throw new InvalidOperationException(string.Join(';', publication.Report.CoreErrors));
            var fingerprint = Fingerprint();
            var stages = Stages.Concat(DeepRosters.Select(pair => new Progression("system-" + pair.Key, 13,
                pair.Value.Select(id => plan.Units.Single(x => x.Id == id).Tier).Order().ToArray()))).ToArray();
            var teams = stages.SelectMany(stage => SelectTeams(plan, stage)).ToArray();
            var records = new List<Record>();
            foreach (var stage in stages)
            foreach (var team in teams.Where(x => x.Progression == stage.Id))
            foreach (var seed in Seeds)
            foreach (var kind in new[] { TowerNodeType.Combat, TowerNodeType.Elite, TowerNodeType.Boss })
            {
                var floor = kind == TowerNodeType.Boss ? stage.Floor + 1 : stage.Floor;
                var (config, encounter) = Prepare(package, team, floor, kind, seed);
                var observation = Observe(package, config);
                Require(observation.Outcome != BattleOutcome.Running.ToString(), "diagnostic stopped before terminal outcome");
                records.Add(new(team.Id, team.Direction, team.Progression, floor + 1, kind.ToString(), seed,
                    encounter.EncounterId, encounter.CompositionId, encounter.FloorRuleId, team.Heroes.Length, team.Tiers, 0, 0,
                    team.Heroes, team.ActualTraits, observation));
                if (records.Count % 9 == 0) { GD.Print($"TRAIT_MATRIX_MATCHUP_PROGRESS {records.Count}"); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            }
            Require(records.Count == teams.Length * Seeds.Length * 3, "incomplete team/enemy/seed Cartesian product");
            Require(records.Select(x => (x.TeamId, x.Kind, x.Seed)).Distinct().Count() == records.Count, "duplicate diagnostic sample");
            foreach (var group in records.GroupBy(x => (x.Progression, x.Kind, x.Seed)))
            {
                Require(group.Select(x => x.Population).Distinct().Count() == 1, "unequal population comparison");
                Require(group.Select(x => string.Join(',', x.TierVector)).Distinct().Count() == 1, "unequal tier investment comparison");
                Require(group.Select(x => x.EncounterId + ":" + x.CompositionId + ":" + x.FloorRuleId).Distinct().Count() == 1, "enemy fixture changed across compared teams");
            }
            // One replay per progression catches state leaking through published shared resources.
            foreach (var stage in Stages)
            {
                var team = teams.First(x => x.Progression == stage.Id);
                var (config, _) = Prepare(package, team, stage.Floor, TowerNodeType.Combat, Seeds[0]);
                var replay = Observe(package, config);
                Require(replay.Digest == records.Single(x => x.TeamId == team.Id && x.Seed == Seeds[0] && x.Kind == TowerNodeType.Combat.ToString()).Battle.Digest,
                    $"determinism replay mismatch {stage.Id}");
            }
            Require(fingerprint == Fingerprint(), "source changed during diagnostics; discard stale mixed-version results");
            var summary = records.GroupBy(x => (x.TeamId, x.Kind)).Select(g => new { g.Key.TeamId, g.Key.Kind,
                Samples = g.Count(), Wins = g.Count(x => x.Battle.Outcome == BattleOutcome.PlayerVictory.ToString()),
                Timeouts = g.Count(x => x.Battle.Outcome == BattleOutcome.Timeout.ToString()), MeanTicks = g.Average(x => x.Battle.Ticks),
                MeanPlayerHealthRatio = g.Average(x => x.Battle.Units.Where(u => u.Team == 0 && !u.Temporary).Sum(u => u.FinalHealth) /
                    Math.Max(1, x.Battle.Units.Where(u => u.Team == 0 && !u.Temporary).Sum(u => u.MaximumHealth))) }).ToArray();
            Write("matchups.json", new { GeneratedUtc = DateTime.UtcNow, SourceFingerprint = fingerprint, Seeds,
                DeterminismReplays = Stages.Length, Teams = teams, RecordCount = records.Count, Records = records, Summary = summary,
                Limitation = "Fixed-seed equal-tier/equal-population, zero-equipment/relic-budget diagnostics. Not recruitment accessibility, natural win rates, or balance acceptance." });
            GD.Print($"TRAIT_MATRIX_MATCHUP_OK records={records.Count}"); GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("TRAIT_MATRIX_MATCHUP_FAILED " + error); GetTree().Quit(1); }
    }

    private static IEnumerable<Team> SelectTeams(Plan plan, Progression stage)
    {
        // Deterministic candidate sampling plus system-biased samples finds concentrated and mixed rosters
        // without claiming an exhaustive optimizer. The realized tags are always saved beside the label.
        var rng = new Random(20260926 + stage.Floor);
        var candidates = new Dictionary<string, PlanUnit[]>();
        foreach (var bias in new[] { "" }.Concat(plan.Traits.Where(x => x.System).Select(x => x.Id)))
        for (var iteration = 0; iteration < 1000; iteration++)
        {
            var picks = new List<PlanUnit>();
            foreach (var bucket in stage.Tiers.GroupBy(x => x))
            {
                var pool = plan.Units.Where(x => x.Tier == bucket.Key).Select(x => (Unit: x,
                    Score: rng.NextDouble() + (bias.Length > 0 && x.Systems.Contains(bias) ? 2 : 0)))
                    .OrderByDescending(x => x.Score).Take(bucket.Count()).Select(x => x.Unit);
                picks.AddRange(pool);
            }
            var ordered = picks.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            if (!RoleReady(ordered)) continue;
            candidates.TryAdd(string.Join(',', ordered.Select(x => x.Id)), ordered);
        }
        int Count(PlanUnit[] units, PlanTrait trait) => units.Count(x => x.Tags.Contains(trait.Id));
        int Tier(PlanUnit[] units, PlanTrait trait) => trait.Thresholds.Count(t => Count(units, trait) >= t);
        double Score(PlanUnit[] units, string direction)
        {
            var systems = plan.Traits.Where(x => x.System).ToArray();
            var active = plan.Traits.Count(t => Tier(units, t) > 0);
            var max = systems.Max(t => Tier(units, t));
            var sum = systems.Sum(t => Tier(units, t));
            return direction switch
            {
                "dispersed" => -plan.Traits.Sum(t => Tier(units, t) * Tier(units, t)) * 100 - active,
                "shallow" => -systems.Sum(t => Math.Max(0, Tier(units, t) - 1)) * 1000 + systems.Count(t => Tier(units, t) == 1) * 100 - active,
                "concentrated" => max * 10000 + systems.Max(t => Count(units, t)) * 100 - sum,
                _ => systems.Count(t => Tier(units, t) >= 2) * 10000 + systems.Count(t => Tier(units, t) > 0) * 100 + sum
            };
        }
        var used = new HashSet<string>();
        var systemId = stage.Id.StartsWith("system-", StringComparison.Ordinal) ? stage.Id[7..] : "";
        if (systemId.Length > 0)
        {
            var roster = DeepRosters[systemId].Select(id => plan.Units.Single(x => x.Id == id)).ToArray();
            var requested = plan.Traits.Single(t => t.Id == systemId);
            Require(RoleReady(roster) && Count(roster, requested) >= requested.Thresholds.Max(), "deep roster does not actually reach highest tier: " + systemId);
            used.Add(string.Join(',', roster.OrderBy(x => x.Id, StringComparer.Ordinal).Select(x => x.Id)));
            var actual = plan.Traits.Select(t => (object)new { t.Id, Value = Count(roster, t),
                ActiveMin = t.Thresholds.Where(n => Count(roster, t) >= n).Select(n => (int?)n).LastOrDefault() }).ToArray();
            yield return new(stage.Id + "-deep", "deep", stage.Id, roster.Select(x => x.ContentId).ToArray(), stage.Tiers, actual);
        }
        foreach (var direction in systemId.Length > 0 ? new[] { "dispersed", "shallow" } : new[] { "dispersed", "shallow", "concentrated", "cross-system" })
        {
            var chosen = candidates.Where(x => !used.Contains(x.Key)).OrderByDescending(x => Score(x.Value, direction)).ThenBy(x => x.Key, StringComparer.Ordinal).First();
            used.Add(chosen.Key);
            Require(chosen.Value.Select(x => x.Tier).Order().SequenceEqual(stage.Tiers.Order()), "candidate tier budget invalid");
            var actual = plan.Traits.Select(t => (object)new { t.Id, Value = Count(chosen.Value, t),
                ActiveMin = t.Thresholds.Where(n => Count(chosen.Value, t) >= n).Select(n => (int?)n).LastOrDefault() }).ToArray();
            yield return new(stage.Id + "-" + direction, direction, stage.Id,
                chosen.Value.Select(x => x.ContentId).ToArray(), stage.Tiers.Order().ToArray(), actual);
        }
    }

    private static bool RoleReady(PlanUnit[] units) => units.Count(x => x.Classes.Any(c => c is "guard" or "fighter")) >= 2 &&
        units.Any(x => x.Classes.Any(c => c is "ranger" or "mage"));

    // Each roster includes native generators, payoff units and a survival/support interface.
    // An external fighter fills systems with only one native frontline; it is part of the exact budget.
    private static readonly Dictionary<string, string[]> DeepRosters = new()
    {
        ["frost"] = ["MX01", "MX02", "MX03", "MX04", "MX05", "MX36"],
        ["poison"] = ["MX06", "MX07", "MX08", "MX09", "MX10", "MX46", "MX49"],
        ["ember"] = ["MX11", "MX12", "MX13", "MX14", "MX15", "MX49"],
        ["death"] = ["MX16", "MX17", "MX19", "MX20", "MX38", "MX39"],
        ["construct"] = ["MX21", "MX22", "MX23", "MX24", "MX25", "MX43"],
        ["blood"] = ["MX26", "MX27", "MX28", "MX29", "MX30", "MX49"],
        ["astral"] = ["MX31", "MX32", "MX33", "MX34", "MX35", "MX49"]
    };

    private static (BattleConfig Config, EncounterPlan Encounter) Prepare(CompiledGamePackage package, Team team, int floor, TowerNodeType kind, ulong seed)
    {
        var run = new ActiveRunDto { Seed = seed, FloorIndex = floor, BattleNumber = floor, CurrentPopulation = team.Heroes.Length,
            CurrentRunHealth = 100, MaximumRunHealth = 100, Gold = 0,
            EquippedTacticalCommandIds = package.Project.RunRules.StarterTacticalCommandIds.ToList() };
        var encounter = new TowerGenerator(package.Project.Campaign).Encounter(run, kind);
        var floorRoot = package.Project.FloorRules[encounter.FloorRuleId].Instantiate<FloorRuleContentRoot>();
        System.Collections.Generic.HashSet<Vector2I> available;
        try
        {
            var floorRuntime = floorRoot.CreateRuntime();
            available = BattlefieldLayout.PlayerDeploymentCells.Where(floorRuntime.CanOccupy).ToHashSet();
        }
        finally { floorRoot.Free(); }
        Require(available.Count >= team.Heroes.Length, "not enough legal deployment cells for diagnostic");
        for (var i = 0; i < team.Heroes.Length; i++)
        {
            var id = team.Heroes[i];
            var definition = (UnitDefinition)package.Content.Catalog.Heroes.Single(x => x.StableId == id).Definition;
            var cell = available.OrderBy(c => definition.AttackRange < 2 ? -c.X : c.X).ThenBy(c => Math.Abs(c.Y - 2.5f)).ThenBy(c => c.Y).First();
            available.Remove(cell);
            var instance = new RosterHeroInstanceDto { InstanceId = "matrix-" + i, ContentId = id };
            run.Roster.Add(instance); run.Deployment[BattlefieldLayout.PlayerDeploymentSlot(cell)] = instance.InstanceId;
        }
        Require(run.Deployment.Count(x => !string.IsNullOrEmpty(x)) == team.Heroes.Length, "deployment lost a hero");
        var config = new RunBattlePreparationService(package.Content, package.Project, new RunRelicService(package.Content)).Build(run, encounter, true);
        Require(config.Spawns.Where(x => x.Team == 0 && !x.IsTemporary).Select(x => x.InstanceId).ToHashSet().SetEquals(run.Roster.Select(x => x.InstanceId)), "prepared roster mismatch");
        return (config, encounter);
    }
}
