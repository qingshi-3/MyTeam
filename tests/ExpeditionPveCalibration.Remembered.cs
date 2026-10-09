using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Expedition;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

public partial class ExpeditionPveCalibration
{
    private const string B3Profile = "gloam_elite_profile";
    private static readonly string[] B3Groups = ["early", "late", "missing"];
    private sealed record B3Checkpoint(string Group, ActiveRunDto Run, MetaProgressDto Meta,
        List<JsonElement> Route, int Attempt);
    private sealed class B3Unavailable(string message) : Exception(message);
    private sealed class B3Save : IRunSaveService
    {
        public string? Json;
        public MetaProgressDto Meta = new();
        public ActiveRunDto? LoadActiveRun() => Json is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(Json);
        public MetaProgressDto LoadMeta() => JsonSerializer.Deserialize<MetaProgressDto>(JsonSerializer.Serialize(Meta))!;
        public SettingsDto LoadSettings() => new();
        public bool SaveActiveRun(ActiveRunDto value) { Json = JsonSerializer.Serialize(value); return true; }
        public bool SaveMeta(MetaProgressDto value) { Meta = value; return true; }
        public bool SaveSettings(SettingsDto value) => true;
        public void DeleteActiveRun() => Json = null;
        public B3Save Fork() => new() { Json = Json, Meta = LoadMeta() };
    }

    private async Task CalibrateRemembered(CompiledGamePackage package, CompiledExpedition expedition, Options options)
    {
        Directory.CreateDirectory(options.Output);
        var attempts = new List<JsonElement>();
        var checkpoints = new Dictionary<string, B3Checkpoint>(StringComparer.Ordinal);
        if (options.RememberedReplay is { } replay)
        {
            replay = replay.StartsWith("res://", StringComparison.Ordinal) || replay.StartsWith("user://", StringComparison.Ordinal)
                ? ProjectSettings.GlobalizePath(replay) : Path.GetFullPath(replay);
            foreach (var group in B3Groups)
            {
                var checkpoint = JsonSerializer.Deserialize<B3Checkpoint>(File.ReadAllText(Path.Combine(replay, group + ".checkpoint.json")), PrettyJson)
                    ?? throw new InvalidOperationException("Missing B3 checkpoint " + group);
                Require(checkpoint.Group == group, "replay group identity");
                ValidateB3Checkpoint(package, checkpoint);
                checkpoints.Add(group, checkpoint);
            }
        }
        else
        {
            // A condition search, never a natural-acquisition-rate experiment.
            // At most 128 opening drafts and 24 committed four-action prefixes.
            var committed = 0;
            for (var draft = 0; draft < 128 && committed < 24 && checkpoints.Count < 3; draft++)
            {
                var seed = 20261007UL + (ulong)draft;
                var save = new B3Save();
                var app = new RunApplication(package, save, "b3-calibration");
                Require(app.BeginOpeningRecruitment(seed), "B3 actual six-choice opening");
                var opening = app.ActiveRun!.OpeningRecruitment!.CandidateIds.ToArray();
                if (!new[] { "prototype_u11", "prototype_u02" }.All(opening.Contains))
                {
                    attempts.Add(B3Json(new { Draft = draft, Seed = seed, Status = "opening-miss", Candidates = opening }));
                    continue;
                }
                Require(app.ToggleOpeningHero("prototype_u11") && app.ToggleOpeningHero("prototype_u02") && app.ConfirmOpeningRecruitment(),
                    "B3 chooses only the two actually offered E heroes");
                var route = new List<JsonElement> { B3Json(new { Kind = "opening", Seed = seed, app.ActiveRun!.RunId,
                    Candidates = opening, Selected = new[] { "prototype_u11", "prototype_u02" } }) };
                try
                {
                    var map = app.ActiveRun!.Expedition!;
                    var elite = map.Sites.Single(site => site.EncounterProfileId == "ember_elite_profile");
                    var approach = B3Route(map, elite.CellIndex, true);
                    if (approach is null || approach.Count(index => ExpeditionNavigation.IsBlocked(map, index)) > 5)
                        throw new B3Unavailable("ember elite needs more than four ordinary guards or a different-region guard");
                    committed++;
                    PlaceB3(app);
                    for (var action = 0; action < 4; action++)
                    {
                        var path = B3Route(app.ActiveRun!.Expedition!, elite.CellIndex, true)!;
                        var blocker = path.Select(index => ExpeditionNavigation.SiteAt(app.ActiveRun!.Expedition!, index))
                            .FirstOrDefault(site => site is { Cleared: false, EncounterProfileId.Length: > 0 });
                        var target = blocker is not null && blocker.SiteId != elite.SiteId ? blocker : FindB3Normal(app.ActiveRun.Expedition!);
                        await CommitB3Site(app, target, route);
                    }
                    OpenB3Discovery(app, "prototype_u02", route);
                    var first = app.ActiveRun!.Growth!.PendingDiscovery!.CandidateIds.ToArray();
                    attempts.Add(B3Json(new { Draft = draft, Attempt = committed, Seed = seed, app.ActiveRun.RunId,
                        Status = "first-discovery", Candidates = first }));
                    // Fork the already frozen offer, preserving RunId and sampling keys.
                    // Different legitimate choices are counterfactual branches, not rerolls.
                    if (!checkpoints.ContainsKey("early") && first.Contains("prototype_u13"))
                        await CompleteB3Branch("early", "prototype_u13", save.Fork(), new(route), committed, package, checkpoints, attempts);
                    if ((!checkpoints.ContainsKey("late") || !checkpoints.ContainsKey("missing")) && first.Contains("prototype_u08"))
                        await CompleteB3Branch("later", "prototype_u08", save.Fork(), new(route), committed, package, checkpoints, attempts);
                }
                catch (B3Unavailable error)
                {
                    attempts.Add(B3Json(new { Draft = draft, Attempt = committed, Seed = seed,
                        RunId = app.ActiveRun?.RunId, Status = "condition-unavailable", Reason = error.Message, Route = route }));
                }
                File.WriteAllText(Path.Combine(options.Output, "b3-attempts.json"), JsonSerializer.Serialize(attempts, PrettyJson));
                GD.Print($"B3_CHECKPOINT_SEARCH drafts={draft + 1} committed={committed} acquired={string.Join(',', checkpoints.Keys)}");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }

        foreach (var (group, checkpoint) in checkpoints)
            File.WriteAllText(Path.Combine(options.Output, group + ".checkpoint.json"), JsonSerializer.Serialize(checkpoint, PrettyJson));
        var rows = new List<JsonElement>();
        using var writer = new StreamWriter(Path.Combine(options.Output, "battles.jsonl"), false);
        foreach (var group in B3Groups.Where(checkpoints.ContainsKey))
        {
            // Deserialize exactly the published checkpoint rather than keeping a
            // mutable branch reference. Replay follows the same validation path.
            var checkpoint = JsonSerializer.Deserialize<B3Checkpoint>(File.ReadAllText(Path.Combine(options.Output, group + ".checkpoint.json")), PrettyJson)!;
            ValidateB3Checkpoint(package, checkpoint);
            var run = checkpoint.Run;
            var preparation = new RunBattlePreparationService(package.Content, package.Project,
                new RunRelicService(package.Content), package.GrowthRules);
            foreach (var seed in options.Seeds)
            {
                var config = CaptureConfiguration(preparation.Build(run, CreatePlan(expedition.Encounters[B3Profile]), true), seed);
                using var battle = new BattleSimulation(config);
                var initial = BattleSimulation.CapturePreparation(battle);
                var result = battle.RunToEnd();
                VerifyStatistics(result);
                var hunter = run.Roster.Single(hero => hero.ContentId == "prototype_u11");
                var hunterReport = result.Units.Single(unit => unit.SourceInstanceId == hunter.InstanceId && !unit.IsTemporary);
                var row = B3Json(new
                {
                    SchemaVersion = 1, SnapshotKind = "B3_formally_acquired_conditional_checkpoint", Group = group,
                    ContentFingerprint = package.DefaultProfile.ContentFingerprint, Profile = B3Profile,
                    RunSeed = run.Seed, BattleSeed = seed, run.RunId, run.NextActionSequence,
                    StageIndex = run.Expedition!.StageIndex, run.Expedition.RemainingActions,
                    Budget = new { ProducedMaterials = 8, SpentMaterials = 8, RemainingMaterials = run.Growth!.Materials,
                        run.Gold, HeroCount = run.Roster.Count, run.CurrentPopulation, Equipment = 0, Relics = 0 },
                    Roster = run.Roster.Select(hero => new { hero.InstanceId, hero.ContentId, hero.Growth }),
                    Deployment = run.Deployment, InitialUnits = initial.Units.Select(unit => InitialUnit(package.Content, unit)),
                    InitialTraits = initial.Traits, result.Outcome, result.Ticks, Seconds = result.Ticks * BattleTiming.TickSeconds,
                    result.Digest, Player = Summarize(result, initial, 0), Enemy = Summarize(result, initial, 1),
                    Hunter = ReportUnit(hunterReport), result.RememberedGains, Units = result.Units.Select(ReportUnit),
                    ControlledBattleCommittedToRun = false
                });
                rows.Add(row); writer.WriteLine(row.GetRawText()); writer.Flush();
                GD.Print($"B3_CALIBRATION_BATTLE group={group} seed={seed} outcome={result.Outcome} ticks={result.Ticks}");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }
        var missing = B3Groups.Except(checkpoints.Keys).ToArray();
        File.WriteAllText(Path.Combine(options.Output, "summary.json"), JsonSerializer.Serialize(new
        {
            SchemaVersion = 1, Mode = "b3-remembered", ContentFingerprint = package.DefaultProfile.ContentFingerprint,
            PlannedBattles = 3 * options.Seeds.Length, CompletedBattles = rows.Count, MissingGroups = missing,
            SharedOpeningRun = checkpoints.Values.Select(value => value.Run.RunId).Distinct().Count() == 1,
            Comparisons = new[] { "early vs late: same final roster and spend; inspect route/receipts for acquisition-history differences",
                "late vs missing: equal-spend functional replacement, including changed traits and targeting; not an isolated buff delta" },
            Expectations = new[] { "early may improve hunter durability/output but need not win every matchup",
                "late tests immediate dragon startup without saved gains", "missing may trade remembrance for assassin backline pressure" },
            Limits = new[] { "condition-selected samples, not acquisition or completion rates", "map knowledge is allowed only for this transaction fixture, not evidence of fog-limited player navigation",
                "all route outcomes and growth are actual; fixed-seed target encounters are controlled and never submitted to the journey",
                "different RunIds mean different route battle RNG; do not call that a pure timing causal estimate",
                "statistics cannot exactly attribute attribute-amplifier marginal damage or unrecorded interruption causes" },
            Rows = rows
        }, PrettyJson));
        if (missing.Length > 0)
            GD.Print($"B3_CALIBRATION_INCOMPLETE completed={rows.Count} missing={string.Join(',', missing)} output={options.Output}");
        else GD.Print($"B3_PVE_CALIBRATION_OK battles={rows.Count} output={options.Output}");
    }

    private async Task CompleteB3Branch(string branch, string firstHero, B3Save save, List<JsonElement> route, int attempt,
        CompiledGamePackage package, Dictionary<string, B3Checkpoint> checkpoints, List<JsonElement> attempts)
    {
        var app = new RunApplication(package, save, "b3-calibration");
        try
        {
            ChooseB3(app, firstHero, route);
            var elite = app.ActiveRun!.Expedition!.Sites.Single(site => site.EncounterProfileId == "ember_elite_profile");
            await CommitB3Site(app, elite, route);
            await CommitB3Site(app, FindB3Normal(app.ActiveRun!.Expedition!), route);
            await CommitB3Battle(app, app.PreviewExpeditionBoss(), route);
            Require(app.ActiveRun!.Expedition is { StageIndex: 1, RemainingActions: 6 }, "actual first Boss advances the stage");
            await CommitB3Site(app, FindB3Normal(app.ActiveRun.Expedition), route);
            OpenB3Discovery(app, "prototype_u11", route);
            var offered = app.ActiveRun.Growth!.PendingDiscovery!.CandidateIds.ToArray();
            var wanted = branch == "early" ? new[] { (Group: "early", Hero: "prototype_u08") }
                : new[] { (Group: "late", Hero: "prototype_u13"), (Group: "missing", Hero: "prototype_u17") };
            foreach (var (group, hero) in wanted.Where(value => !checkpoints.ContainsKey(value.Group) && offered.Contains(value.Hero)))
            {
                var finalSave = save.Fork();
                var finalApp = new RunApplication(package, finalSave, "b3-calibration");
                var finalRoute = new List<JsonElement>(route);
                ChooseB3(finalApp, hero, finalRoute);
                if (group == "early" && !finalApp.ActiveRun!.Roster.Single(value => value.ContentId == "prototype_u11")
                    .Growth.History.Any(gain => gain.Source == "remembered"))
                    throw new B3Unavailable("early dragon acquired legally but produced no actual saved remembrance in this route");
                var checkpoint = new B3Checkpoint(group, finalSave.LoadActiveRun()!, finalSave.LoadMeta(), finalRoute, attempt);
                ValidateB3Checkpoint(package, checkpoint);
                checkpoints.Add(group, checkpoint);
            }
            attempts.Add(B3Json(new { Attempt = attempt, Branch = branch, app.ActiveRun.RunId, Status = "second-discovery",
                Candidates = offered, Route = route }));
        }
        catch (B3Unavailable error)
        {
            attempts.Add(B3Json(new { Attempt = attempt, Branch = branch, RunId = app.ActiveRun?.RunId,
                Status = "condition-unavailable", Reason = error.Message, Route = route }));
        }
    }

    private static void OpenB3Discovery(RunApplication app, string contentId, List<JsonElement> route)
    {
        var run = app.ActiveRun!;
        var hero = run.Roster.Single(value => value.ContentId == contentId);
        var materials = run.Growth!.Materials;
        Require(materials == 4 && app.AscendHero(hero.InstanceId).Succeeded, "exact four earned materials pay one formal ascension");
        var discovery = run.Growth.PendingDiscovery!;
        Require(discovery.CandidateIds.Count == 3 && run.Growth.Materials == 0, "three real candidates and exact material debit");
        route.Add(B3Json(new { Kind = "ascend", run.RunId, Stage = run.Expedition!.StageIndex, run.NextActionSequence,
            Hero = contentId, hero.InstanceId, MaterialsBefore = materials, MaterialsAfter = run.Growth.Materials,
            discovery.OfferId, Candidates = discovery.CandidateIds }));
    }

    private static void ChooseB3(RunApplication app, string contentId, List<JsonElement> route)
    {
        Require(app.ActiveRun!.Growth!.PendingDiscovery!.CandidateIds.Contains(contentId) && app.ChooseGrowthHero(contentId).Succeeded,
            "take only an actually frozen discovery candidate");
        route.Add(B3Json(new { Kind = "choose", Hero = contentId, app.ActiveRun.RunId, app.ActiveRun.NextActionSequence }));
        PlaceB3(app);
    }

    private static void PlaceB3(RunApplication app)
    {
        // Stable content slots; U17 replaces U13's slot. U08 never occupies a
        // nearer dragon partner slot than U11. No formation is optimized post-result.
        var positions = new Dictionary<string, Vector2I>
        {
            ["prototype_u11"] = new(2, 2), ["prototype_u02"] = new(2, 4),
            ["prototype_u13"] = new(1, 2), ["prototype_u17"] = new(1, 2), ["prototype_u08"] = new(0, 5)
        };
        foreach (var (id, cell) in positions)
            if (app.ActiveRun!.Roster.FirstOrDefault(hero => hero.ContentId == id) is { } hero)
            {
                var slot = BattlefieldLayout.PlayerDeploymentSlot(cell);
                if (app.ActiveRun.Deployment[slot] != hero.InstanceId)
                    Require(app.MoveDeploymentUnit(hero.InstanceId, slot), "formal B3 placement " + id);
            }
    }

    private async Task CommitB3Site(RunApplication app, ExpeditionSiteDto target, List<JsonElement> route)
    {
        var map = app.ActiveRun!.Expedition!;
        var path = B3Route(map, target.CellIndex, false) ?? throw new B3Unavailable("required site blocked by another uncleared guard: " + target.SiteId);
        foreach (var cell in path.Take(path.Count - 1))
        {
            Require(app.MoveExpedition(cell), "formal adjacent revealed route step");
            route.Add(B3Json(new { Kind = "move", Cell = cell }));
        }
        Require(ExpeditionNavigation.CanInteractWithSite(app.ActiveRun!.Expedition!, target.SiteId), "target approached through legal movement");
        await CommitB3Battle(app, app.PreviewExpeditionEncounter(target.SiteId), route);
    }

    private async Task CommitB3Battle(RunApplication app, EncounterPlan encounter, List<JsonElement> route)
    {
        var run = app.ActiveRun!;
        var materialBefore = run.Growth!.Materials;
        var goldBefore = run.Gold;
        var stage = run.Expedition!.StageIndex;
        var sequence = run.NextActionSequence;
        Require(app.TryBeginGrowthBattle(encounter).Succeeded, "actual route battle freeze");
        using var battle = new BattleSimulation(app.BuildBattleConfig(encounter));
        var result = battle.RunToEnd();
        Require(app.ResolveBattle(result, encounter).Accepted, "submit unmodified actual route battle result");
        while (app.PendingOffer is { } offer)
            Require(app.ResolveOffer(offer.OfferId, null).Succeeded, "finish actual rewards without optional gifts");
        route.Add(B3Json(new { Kind = "battle", Stage = stage, Sequence = sequence, encounter.ExpeditionSiteId,
            encounter.ExpeditionProfileId, result.Outcome, result.Ticks, result.Digest, result.RememberedGains,
            MaterialsBefore = materialBefore, MaterialsAfter = app.ActiveRun?.Growth?.Materials,
            GoldBefore = goldBefore, GoldAfter = app.ActiveRun?.Gold }));
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (result.Outcome != BattleOutcome.PlayerVictory || app.ActiveRun is null)
            throw new B3Unavailable("actual route battle did not win: " + encounter.ExpeditionProfileId);
        Require(app.ActiveRun.Growth!.Materials == materialBefore + 1, "one actual nonterminal battle grants exactly one material");
    }

    private static ExpeditionSiteDto FindB3Normal(ExpeditionStateDto map) => map.Sites
        .Where(site => !site.Cleared && site.EncounterProfileId == "ember_combat_profile")
        .Select(site => (Site: site, Path: B3Route(map, site.CellIndex, false)))
        .Where(value => value.Path is not null).OrderBy(value => value.Path!.Count).ThenBy(value => value.Site.CellIndex)
        .Select(value => value.Site).FirstOrDefault() ?? throw new B3Unavailable("no reachable next ember ordinary without extra paid guards");

    // Fixture planning may inspect generated geometry. It never writes fog,
    // position, clears or inventory; every planned step goes through RunApplication.
    private static List<int>? B3Route(ExpeditionStateDto map, int target, bool allowEmberGuards)
    {
        var previous = new Dictionary<int, int> { [map.PositionIndex] = map.PositionIndex };
        var distance = new Dictionary<int, int> { [map.PositionIndex] = 0 };
        var queue = new PriorityQueue<int, int>(); queue.Enqueue(map.PositionIndex, 0);
        while (queue.TryDequeue(out var cell, out var cost))
        {
            if (cost != distance[cell]) continue;
            if (cell == target)
            {
                var path = new List<int>();
                for (; cell != map.PositionIndex; cell = previous[cell]) path.Add(cell);
                path.Reverse(); return path;
            }
            foreach (var next in ExpeditionNavigation.Neighbors(map.Size, cell))
            {
                if (!map.Cells[next].Walkable) continue;
                var blocked = ExpeditionNavigation.IsBlocked(map, next);
                if (blocked && next != target && (!allowEmberGuards || ExpeditionNavigation.SiteAt(map, next)?.EncounterProfileId != "ember_combat_profile")) continue;
                var candidate = cost + (blocked ? 100 : 1);
                if (distance.TryGetValue(next, out var known) && known <= candidate) continue;
                distance[next] = candidate; previous[next] = cell; queue.Enqueue(next, candidate);
            }
        }
        return null;
    }

    private static void ValidateB3Checkpoint(CompiledGamePackage package, B3Checkpoint checkpoint)
    {
        var save = new B3Save { Json = JsonSerializer.Serialize(checkpoint.Run), Meta = checkpoint.Meta };
        var persistence = new RunProgressionPersistenceService(package.Content, save, package.Project, package.GrowthRules, package.DefaultProfile);
        Require(persistence.ValidateRun(checkpoint.Run), "B3 full saved-run validation including growth/history/profile");
        Require(B3Groups.Contains(checkpoint.Group) && checkpoint.Route.Count > 0, "B3 declared group and acquisition evidence");
        var app = new RunApplication(package, save, "b3-calibration");
        var run = app.ActiveRun ?? throw new InvalidOperationException("B3 checkpoint must cold-resume");
        Require(run.Expedition is { StageIndex: 1, RemainingActions: 5 } && run.Action is null && run.PendingOffer is null &&
            run.Growth is { Materials: 0, PendingDiscovery: null } && run.Growth.History.Count == 8 &&
            run.Roster.Count == 4 && run.Deployment.Count(id => id.Length > 0) == 4 && run.Gold == 88,
            "B3 identical eight-production/two-ascension/four-hero/88-gold budget");
        var expected = checkpoint.Group == "missing" ? new[] { "prototype_u11", "prototype_u02", "prototype_u08", "prototype_u17" }
            : new[] { "prototype_u11", "prototype_u02", "prototype_u08", "prototype_u13" };
        Require(run.Roster.Select(hero => hero.ContentId).Order().SequenceEqual(expected.Order()) &&
            run.Roster.Where(hero => hero.Growth.AscensionId.Length > 0).Select(hero => hero.ContentId).Order()
                .SequenceEqual(new[] { "prototype_u02", "prototype_u11" }), "B3 declared lineup and identical ascended cores");
        Require(run.Growth!.History.Sum(node => node.MaterialsGranted) == 8 &&
            run.Roster.All(hero => hero.Growth.History.All(gain => gain.Source == "remembered")),
            "eight source-proven materials and no unrelated permanent growth");
        var hunter = run.Roster.Single(hero => hero.ContentId == "prototype_u11");
        var pairs = hunter.Growth.History.Count(gain => gain.Source == "remembered") / 2;
        Require(checkpoint.Group == "early" ? pairs > 0 && pairs <= 4 : pairs == 0, "B3 actual early gains, no gifted late/missing gains");
        Require(run.Items.Count == 0 && run.EquipmentInventory.Count == 0 && run.Roster.All(hero => hero.Equipment.Count == 0) &&
            run.Growth.SpellInventory.Count == 0 && run.Growth.EquippedSpellId.Length == 0,
            "B3 excludes equipment, relics and crafted spells");
    }

    private static JsonElement B3Json(object value) => JsonSerializer.SerializeToElement(value, CompactJson);
}
