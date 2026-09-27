using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;
using TowerAutobattler.Traits;
using TowerAutobattler.ValidationMatrix;
using static TraitMatrixValidationSupport;

public partial class TraitMatrixContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var plan = ReadPlan();
            var fingerprint = Fingerprint();
            var publication = await ValidationMatrixPackage.CreateReadyAsync(this);
            var package = publication.Package ?? throw new InvalidOperationException(string.Join(';', publication.Report.CoreErrors));
            var index = new BattleLabContentIndex(package);
            Require(index.PlayerHeroes.Where(x => x.StableId.StartsWith("hero_mx", StringComparison.Ordinal)).Select(x => x.StableId).ToHashSet()
                .SetEquals(plan.Units.Select(x => x.ContentId)), "all 49 matrix heroes must be published exactly once");
            var unitResults = new List<object>(); var tierResults = new List<object>();
            foreach (var unit in plan.Units)
            {
                var ally = unit.Id == "MX47" ? "hero_mx04" : unit.Id is "MX16" or "MX31" or "MX39" or "MX41" ? "hero_mx20" : "soldier_dummy_static";
                var config = Laboratory(index, [unit.ContentId, ally],
                    ["soldier_dummy_melee", "soldier_dummy_static", "soldier_dummy_static"], 20260926);
                var spawn = config.Spawns.Single(x => x.Unit.ContentId == unit.ContentId);
                var definitions = config.Traits.Definitions;
                var expectedTraits = plan.Traits.Where(t => unit.Tags.Contains(t.Id)).Select(t =>
                    definitions.Single(d => d.DisplayName == t.Name).StableId).ToHashSet();
                Require(spawn.Unit.TraitContributions.Select(x => x.TraitId).ToHashSet().SetEquals(expectedTraits), $"native membership mismatch {unit.Id}");
                Require(spawn.Unit.TraitContributions.All(x => x.Value == 1), $"unit contributes more than one {unit.Id}");
                var abilities = spawn.Unit.AbilityLoadout?.Abilities.ToArray() ?? [];
                Require(abilities.Any(x => x.IsDisplayedActiveSkill), $"no authored active skill {unit.Id}");
                // Durable targets permit utility/damage casts; one injured allied target permits effective healing.
                for (var i = 0; i < config.Spawns.Count; i++)
                {
                    var s = config.Spawns[i];
                    if (s.Unit.ContentId.StartsWith("soldier_dummy", StringComparison.Ordinal) || s.Unit.ContentId == ally && s.Team == 0)
                        config.Spawns[i] = s with { Unit = s.Unit with { MaxHealth = 20000, Damage = s.Team == 1 ? 6 : 0 }, HealthRatio = s.Team == 0 ? .3f : 1f };
                }
                var observed = Observe(package, config, 600, b =>
                {
                    var caster = b.Units.Single(x => x.Definition.ContentId == unit.ContentId);
                    caster.CurrentMana = caster.MaxMana;
                });
                var own = observed.Units.Single(x => x.ContentId == unit.ContentId && !x.Temporary);
                Require(own.ActiveCasts > 0, $"active skill never committed {unit.Id}; fixture or implementation is invalid");
                unitResults.Add(new { unit.Id, unit.ContentId, unit.Tier, unit.Tags, Fixture = "full-mana-durable-targets-injured-ally", Observation = observed });
                if (unitResults.Count % 7 == 0) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            foreach (var trait in plan.Traits)
            {
                var members = plan.Units.Where(x => x.Tags.Contains(trait.Id)).OrderBy(x => x.Tier).ThenBy(x => x.Id, StringComparer.Ordinal).ToArray();
                foreach (var threshold in trait.Thresholds)
                {
                    Require(members.Length >= threshold, $"not enough unique members {trait.Id}/{threshold}");
                    var config = Laboratory(index, members.Take(threshold).Select(x => x.ContentId), ["soldier_dummy_melee", "soldier_dummy_static"], 20260926);
                    var definition = config.Traits.Definitions.Single(x => x.DisplayName == trait.Name);
                    Require(definition.Breakpoints.Select(x => x.MinValue).SequenceEqual(trait.Thresholds), $"threshold definition mismatch {trait.Id}");
                    var snapshot = TraitSnapshotBuilder.Build(config.Traits.Definitions, config.Traits.Contributions);
                    Require(snapshot.Resolve(definition.StableId, 0).Value == threshold && snapshot.Resolve(definition.StableId, 0).ActiveBreakpoint?.MinValue == threshold,
                        $"native roster does not activate exact threshold {trait.Id}/{threshold}");
                    for (var i = 0; i < config.Spawns.Count; i++)
                        if (config.Spawns[i].Team == 1) { var s = config.Spawns[i]; config.Spawns[i] = s with { Unit = s.Unit with { MaxHealth = 200000, Damage = 12 } }; }
                    var observation = Observe(package, config, 300, afterStep: b => Require(b.TraitSnapshot.Resolve(definition.StableId, 0).ActiveBreakpoint?.MinValue == threshold,
                        $"opening tier changed during battle {trait.Id}/{threshold}"));
                    // Boundary checks use actual prepared native contributions. Reserve/summon inputs are excluded.
                    var inputs = config.Traits.Contributions.Where(x => x.TraitId == definition.StableId && x.Team == 0).ToArray();
                    var first = inputs.First();
                    var augmented = config.Traits.Contributions.Concat(new[] { first,
                        first with { SourceInstanceId = "badge-probe", SourceKind = TraitContributionSourceKind.Equipment, ContentIdentity = "badge-probe" },
                        first with { SourceInstanceId = "reserve-probe", OwnerRuntimeId = "reserve-probe", ContentIdentity = "reserve-probe", IsDeployed = false },
                        first with { SourceInstanceId = "summon-probe", OwnerRuntimeId = "summon-probe", ContentIdentity = "summon-probe", IsPersistent = false, IsTemporary = true } });
                    Require(TraitSnapshotBuilder.Build(config.Traits.Definitions, augmented).Value(definition.StableId, 0) == threshold,
                        $"duplicate/native-badge/reserve/summon changes count {trait.Id}/{threshold}");
                    var newMemberBadge = first with { SourceInstanceId = "other-badge", OwnerRuntimeId = "other-owner", SourceKind = TraitContributionSourceKind.Equipment, ContentIdentity = "badge-probe" };
                    Require(TraitSnapshotBuilder.Build(config.Traits.Definitions, augmented.Append(newMemberBadge)).Value(definition.StableId, 0) == threshold + 1,
                        $"badge on a different member does not add one {trait.Id}/{threshold}");
                    var belowInputs = config.Traits.Contributions.Where(x => x != first);
                    var below = TraitSnapshotBuilder.Build(config.Traits.Definitions, belowInputs).Resolve(definition.StableId, 0);
                    var previous = trait.Thresholds.Where(x => x < threshold).Select(x => (int?)x).LastOrDefault();
                    Require(below.ActiveBreakpoint?.MinValue == previous, $"one-below boundary selects wrong tier {trait.Id}/{threshold}");
                    tierResults.Add(new { trait.Id, StableId = definition.StableId, Threshold = threshold,
                        Members = members.Take(threshold).Select(x => x.ContentId).ToArray(), Observation = observation,
                        Coverage = "published native roster, exact/below threshold, duplicate/reserve/summon exclusion, fixed opening tier, runtime observation" });
                }
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            Require(unitResults.Count == 49 && tierResults.Count == 39, "contract coverage incomplete");
            var directed = TraitMatrixDirectedBehaviorChecks.Run(package, index, plan);
            var rollback = TraitMatrixRollbackChecks.Run(index);
            var native = TraitMatrixNativeSkillChecks.Run(index);
            Require(fingerprint == Fingerprint(), "source changed during contract batch; rerun frozen content");
            Write("contracts.json", new { GeneratedUtc = DateTime.UtcNow, SourceFingerprint = fingerprint, UnitCount = unitResults.Count, TierCount = tierResults.Count,
                Units = unitResults, Tiers = tierResults, Directed = directed, Rollback = rollback, Native = native,
                Limitation = "Activation/counting and runtime probes do not alone prove every conditional trait clause; directed behavior assertions are listed explicitly." });
            GD.Print("TRAIT_MATRIX_CONTRACT_OK units=49 tiers=39");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("TRAIT_MATRIX_CONTRACT_FAILED " + error); GetTree().Quit(1); }
    }
}
