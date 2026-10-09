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
using TowerAutobattler.Domain;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

public partial class ExpeditionPveCalibration
{
    private const int V1MaxDrafts = 32, V1MaxRoutes = 3, V1MaxDecisions = 40;
    private sealed record V1Probe(string Id, int[] Opening, int Core, int[] Priority, int[] Engine,
        bool Cultivate, string[] Profiles, string Expectation);
    private static readonly V1Probe[] V1Probes =
    [
        new("a_melee", [2,16], 2, [25,3,19,18,14,7,4,1], [16,25], true,
            ["ember_combat_profile","gloam_elite_profile","crown_boss_profile"], "熊承接培养，古树晚到不追溯；观察前排生存与多目标收益、最终岩座护卫下的集中核机会成本。"),
        new("a_ranged", [19,4], 19, [16,25,3,22,18,1,7,14], [16,25], true,
            ["ember_combat_profile","gloam_elite_profile","crown_boss_profile"], "雷脉培养应兑现持续输出；换位/喷流可能打断或威胁后排，新滑射者作为合法晚到副核。"),
        new("b_remembered", [11,4], 11, [13,8,18,3,21,14,2,7], [13], false,
            ["ember_combat_profile","gloam_elite_profile","crown_boss_profile"], "幼龙到队后才可能保存铭记；固定猎手邻位，记录实际授予/永久收益，缺幼龙继续实际替代。"),
        new("c_immediate", [2,3], 2, [4,19,7,18,1,6,22,20,14], [], false,
            ["ember_combat_profile","gloam_elite_profile","crown_boss_profile"], "不运行培养或铭记发动机，花同类机会于即时前排/输出/控制；后段是否有持续战力是待测问题。"),
        new("assassin_support", [5,4], 5, [17,20,11,16,18,21,3,7], [17], false,
            ["v1_ember_aid","gloam_elite_profile","crown_boss_profile"], "双刺客及晚来刃舞针对后排；密集保护和岩座护卫可能降低收益，森林支援有上场成本。"),
        new("a_b_mixed", [11,4], 11, [16,13,25,18,21,3,14,7], [16,13,25], true,
            ["ember_combat_profile","gloam_elite_profile","crown_boss_profile"], "培养与铭记同时投入必须占据支援位；记录发动机缺席/晚到与低即时输出，不能假定无代价通解。")
    ];
    private sealed record V1Checkpoint(string Group, int Stage, string ContentFingerprint, ActiveRunDto Run,
        MetaProgressDto Meta, List<JsonElement> Ledger, int Draft, int RouteAttempt);
    private sealed class V1Unavailable(string reason) : Exception(reason);

    private async Task CalibrateV1Content(CompiledGamePackage package, CompiledExpedition expedition, Options options)
    {
        Directory.CreateDirectory(options.Output);
        var selected = V1Probes.Where(p => options.Probe is null || p.Id == options.Probe).ToArray();
        var checkpoints = new List<V1Checkpoint>();
        var attempts = new List<JsonElement>();
        var rows = new List<JsonElement>();
        File.WriteAllText(Path.Combine(options.Output, "v1-plan.json"), JsonSerializer.Serialize(new
        {
            SchemaVersion = 1, Kind = "conditional_reachable_route_main_matrix", ContentFingerprint = package.DefaultProfile.ContentFingerprint,
            Seeds = options.Seeds, Probes = selected, PlannedBattles = selected.Length * 6,
            SnapshotRemainingActions = new[] { 2, 2, 0 }, V1MaxDrafts, V1MaxRoutes, V1MaxDecisions,
            SearchPolicy = "first offered opening pair; retry only incomplete route, never target-battle outcome; choose earliest route reaching all stages or furthest legal partial",
            RoutePolicy = "known geometry may guide shortest guarded routes; no unseen stock inspection; all movement/choices/growth/battle settlements use RunApplication",
            DeferredCoverage = new[] { "equal-budget matched opportunity branches", "stage1/2 Boss controlled supplements",
                "seven equipment and eleven relic normal/weak contexts", "R09×E06 and R07×E10 2x2 interactions" }
        }, PrettyJson));
        foreach (var probe in selected)
        {
            if (options.V1Replay is { } replay)
            {
                var directory = replay.StartsWith("res://") || replay.StartsWith("user://") ? ProjectSettings.GlobalizePath(replay) : Path.GetFullPath(replay);
                for (var stage = 0; stage < 3; stage++)
                {
                    var path = Path.Combine(directory, $"{probe.Id}-stage{stage + 1}.checkpoint.json");
                    if (!File.Exists(path)) continue;
                    var checkpoint = JsonSerializer.Deserialize<V1Checkpoint>(File.ReadAllText(path), PrettyJson)!;
                    ValidateV1Checkpoint(package, checkpoint, probe, stage); checkpoints.Add(checkpoint);
                }
            }
            else checkpoints.AddRange(await GenerateV1Checkpoints(package, expedition, probe, options.Output, attempts));
        }
        foreach (var checkpoint in checkpoints)
            File.WriteAllText(Path.Combine(options.Output, $"{checkpoint.Group}-stage{checkpoint.Stage + 1}.checkpoint.json"), JsonSerializer.Serialize(checkpoint, PrettyJson));
        using var writer = new StreamWriter(Path.Combine(options.Output, "battles.jsonl"), false);
        foreach (var probe in selected)
        for (var stage = 0; stage < 3; stage++)
        foreach (var seed in options.Seeds)
        {
            var checkpoint = checkpoints.SingleOrDefault(c => c.Group == probe.Id && c.Stage == stage);
            var profile = probe.Profiles[stage];
            JsonElement row;
            if (checkpoint is null)
                row = B3Json(new { Group = probe.Id, Stage = stage + 1, Seed = seed, Profile = profile,
                    Status = "checkpoint-unavailable", Reason = "bounded formal-route generation did not reach this checkpoint; no heroes or growth injected" });
            else
            {
                // Both seeds deserialize the exact same validated acquisition history.
                var source = JsonSerializer.Deserialize<V1Checkpoint>(JsonSerializer.Serialize(checkpoint, PrettyJson), PrettyJson)!;
                ValidateV1Checkpoint(package, source, probe, stage);
                var run = source.Run;
                var preparation = new RunBattlePreparationService(package.Content, package.Project, new RunRelicService(package.Content), package.GrowthRules);
                BattleConfig? config = null;
                try { config = CaptureConfiguration(preparation.Build(run, CreatePlan(expedition.Encounters[profile]), true), seed); }
                catch (InvalidOperationException error)
                {
                    row = B3Json(new { Group = probe.Id, Stage = stage + 1, Seed = seed, Profile = profile,
                        Status = "preparation-blocked", error.Message, Budget = V1Budget(run) });
                    rows.Add(row); writer.WriteLine(row.GetRawText()); writer.Flush(); continue;
                }
                using var battle = new BattleSimulation(config);
                var initial = BattleSimulation.CapturePreparation(battle);
                var result = battle.RunToEnd(); VerifyStatistics(result);
                var coreId = run.Roster.Single(h => h.ContentId == IdV1(probe.Core)).InstanceId;
                var core = result.Units.SingleOrDefault(u => u.SourceInstanceId == coreId && !u.IsTemporary);
                var missing = probe.Engine.Where(number => run.Roster.All(h => h.ContentId != IdV1(number))).Select(IdV1).ToArray();
                var flags = new List<string>();
                if (missing.Length > 0) flags.Add("engine-incomplete");
                if (core is null) flags.Add("core-not-deployed");
                else { if (!core.Alive) flags.Add("core-defeated"); if (core.DetailedStatistics!.ManaCasts == 0) flags.Add("core-no-mana-cast"); }
                if (result.Outcome != BattleOutcome.PlayerVictory) flags.Add("target-not-cleared");
                if (probe.Id.StartsWith("b_") || probe.Id == "a_b_mixed")
                    if (!run.Roster.Any(h => h.Growth.History.Any(g => g.Source == "remembered"))) flags.Add("no-saved-remembrance-yet");
                row = B3Json(new
                {
                    SchemaVersion = 1, Status = "completed", Group = probe.Id, Stage = stage + 1, Seed = seed, Profile = profile,
                    Kind = "formally_acquired_conditional_checkpoint", probe.Expectation, ExpectationFlags = flags,
                    MissingEngine = missing, ContentFingerprint = package.DefaultProfile.ContentFingerprint,
                    run.RunId, RunSeed = run.Seed, source.Draft, source.RouteAttempt, Budget = V1Budget(run),
                    Roster = run.Roster, run.Deployment, run.Items, run.EquipmentInventory,
                    InitialUnits = initial.Units.Select(u => InitialUnit(package.Content, u)), InitialTraits = initial.Traits,
                    result.Outcome, result.Ticks, Seconds = result.Ticks * BattleTiming.TickSeconds, result.Digest,
                    Player = Summarize(result, initial, 0), Enemy = Summarize(result, initial, 1),
                    Core = core is null ? null : ReportUnit(core), result.RememberedGains,
                    Units = result.Units.Select(ReportUnit), result.PermanentGains,
                    ControlledBattleCommittedToRun = false, OnlyBattleSeedReplaced = true,
                    Comparison = "role/weakness probe; route opportunities and acquisition costs differ, not an equal-budget strength ranking"
                });
            }
            rows.Add(row); writer.WriteLine(row.GetRawText()); writer.Flush();
            GD.Print($"V1_CONTENT_PROGRESS group={probe.Id} stage={stage + 1} seed={seed} status={row.GetProperty("Status").GetString()}");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        File.WriteAllText(Path.Combine(options.Output, "summary.json"), JsonSerializer.Serialize(new
        {
            SchemaVersion = 1, Mode = "v1-content", ContentFingerprint = package.DefaultProfile.ContentFingerprint,
            PlannedBattles = selected.Length * 6, CompletedBattles = rows.Count(r => r.GetProperty("Status").GetString() == "completed"),
            Checkpoints = checkpoints.Select(c => new { c.Group, Stage = c.Stage + 1, c.Run.RunId, c.Draft, c.RouteAttempt }),
            Rows = rows, Limits = new[] { "condition-selected route attempts, not acquisition or completion rates", "missed candidates are recorded, never replaced by gifted content",
                "route battles use real result submissions; evaluation battles do not alter saved growth", "same stage does not imply equal spend/opportunity; matched-budget comparisons remain pending",
                "attribute item marginal output needs controlled differencing, not source-table attribution", "few seeds cannot establish balance; visual startup/interruption diagnosis is not inferred from absent timings" }
        }, PrettyJson));
        var complete = rows.Count(r => r.GetProperty("Status").GetString() == "completed");
        GD.Print($"V1_CONTENT_MATRIX_{(complete == selected.Length * 6 ? "OK" : "INCOMPLETE")} battles={complete}/{selected.Length * 6} output={options.Output}");
    }

    private async Task<List<V1Checkpoint>> GenerateV1Checkpoints(CompiledGamePackage package, CompiledExpedition expedition,
        V1Probe probe, string output, List<JsonElement> attempts)
    {
        var best = new List<V1Checkpoint>(); var routes = 0;
        for (var draft = 0; draft < V1MaxDrafts && routes < V1MaxRoutes && best.Count < 3; draft++)
        {
            var seed = 20261007UL + (ulong)draft;
            var save = new B3Save(); var app = new RunApplication(package, save, "v1-content-calibration");
            Require(app.BeginOpeningRecruitment(seed), $"formal v1 opening rejected: group={probe.Id} draft={draft} seed={seed}");
            var opening = app.ActiveRun!.OpeningRecruitment!.CandidateIds.ToArray();
            if (!probe.Opening.Select(IdV1).All(opening.Contains))
            {
                attempts.Add(B3Json(new { probe.Id, Draft = draft, Seed = seed, Status = "opening-miss", Candidates = opening }));
                continue;
            }
            foreach (var id in probe.Opening.Select(IdV1)) Require(app.ToggleOpeningHero(id), $"offered opening selection rejected: group={probe.Id} hero={id} offered={string.Join(",", opening)}");
            Require(app.ConfirmOpeningRecruitment(), $"formal opening confirmation rejected: group={probe.Id} seed={seed}"); routes++;
            var ledger = new List<JsonElement> { B3Json(new { Kind = "opening", Seed = seed, app.ActiveRun!.RunId,
                Candidates = opening, Selected = probe.Opening.Select(IdV1), BudgetAfter = V1Budget(app.ActiveRun!) }) };
            var current = new List<V1Checkpoint>(); var ignored = new HashSet<string>();
            try
            {
                for (var decisions = 0; decisions < V1MaxDecisions; decisions++)
                {
                    PrepareV1(app, probe, ledger);
                    var run = app.ActiveRun!; var map = run.Expedition!;
                    if ((map.StageIndex < 2 && map.RemainingActions == 2 || map.StageIndex == 2 && map.RemainingActions == 0) &&
                        current.All(c => c.Stage != map.StageIndex))
                    {
                        var checkpoint = new V1Checkpoint(probe.Id, map.StageIndex, package.DefaultProfile.ContentFingerprint,
                            save.LoadActiveRun()!, save.LoadMeta(), new(ledger), draft, routes);
                        ValidateV1Checkpoint(package, checkpoint, probe, map.StageIndex); current.Add(checkpoint);
                        if (map.StageIndex == 2) break;
                    }
                    if (map.RemainingActions == 0)
                    {
                        await V1Battle(app, app.PreviewExpeditionBoss(), probe, ledger);
                        continue;
                    }
                    var candidate = V1NextSite(app, expedition, ignored);
                    var path = V1Route(map, candidate.CellIndex) ?? throw new V1Unavailable("no geometric route to selected site");
                    var blocker = path.Select(cell => ExpeditionNavigation.SiteAt(map, cell))
                        .FirstOrDefault(site => site is { Cleared: false, EncounterProfileId.Length: > 0 });
                    if (blocker is not null) candidate = blocker;
                    path = V1Route(map, candidate.CellIndex)!;
                    foreach (var cell in path.Take(Math.Max(0, path.Count - 1)))
                    {
                        if (!app.MoveExpedition(cell)) throw new V1Unavailable("formal movement rejected");
                        ledger.Add(B3Json(new { Kind = "move", Cell = cell }));
                    }
                    if (!app.CanInteractExpeditionSite(candidate.SiteId)) throw new V1Unavailable("site is not legally interactable after approach");
                    if (candidate.EncounterProfileId.Length > 0)
                        await V1Battle(app, app.PreviewExpeditionEncounter(candidate.SiteId), probe, ledger);
                    else
                    {
                        Require(app.InspectExpeditionSite(candidate.SiteId), $"inspect reached service rejected: site={candidate.SiteId} position={map.PositionIndex} target={candidate.CellIndex}");
                        var offer = app.PendingOffer ?? throw new V1Unavailable("reached service has no frozen offer");
                        var choice = ChooseV1Offer(app, offer, probe);
                        var before = V1Budget(app.ActiveRun!);
                        var result = app.ResolveOffer(offer.OfferId, choice?.StableId);
                        if (!result.Succeeded) throw new V1Unavailable("service choice rejected: " + result.Message);
                        ledger.Add(B3Json(new { Kind = "service", candidate.SiteId, offer.OfferId, OfferKind = offer.Kind, offer.RecipeId,
                            Candidates = offer.Choices, Selected = choice?.StableId, result.Changes, result.ChanceSucceeded,
                            BudgetBefore = before, BudgetAfter = V1Budget(app.ActiveRun!) }));
                        if (app.ActiveRun!.Action is not null) app.FinishNonCombatNode();
                        ignored.Add(candidate.SiteId); // A skip does not pay an action; do not visit it in a loop.
                    }
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                }
                if (current.Count < 3) throw new V1Unavailable("decision budget ended before all three stage checkpoints");
                attempts.Add(B3Json(new { probe.Id, Draft = draft, Route = routes, Seed = seed, Status = "route-complete", Ledger = ledger }));
            }
            catch (V1Unavailable error)
            {
                attempts.Add(B3Json(new { probe.Id, Draft = draft, Route = routes, Seed = seed, Status = "route-incomplete", Reason = error.Message,
                    CapturedStages = current.Select(c => c.Stage + 1), Ledger = ledger }));
            }
            if (current.Count > best.Count) best = current;
            File.WriteAllText(Path.Combine(output, "v1-route-attempts.json"), JsonSerializer.Serialize(attempts, PrettyJson));
            GD.Print($"V1_ROUTE_SEARCH group={probe.Id} draft={draft} routes={routes}/{V1MaxRoutes} stages={best.Count}/3");
        }
        File.WriteAllText(Path.Combine(output, "v1-route-attempts.json"), JsonSerializer.Serialize(attempts, PrettyJson));
        return best;
    }

    private static string IdV1(int number) => $"prototype_u{number:00}";
    private static int RankV1(V1Probe probe, string id)
    {
        var priorities = new[] { probe.Core }.Concat(probe.Opening).Concat(probe.Priority).Select(IdV1).Distinct().ToArray();
        var rank = Array.IndexOf(priorities, id); return rank < 0 ? 100 : rank;
    }
    private static bool AllowedV1(V1Probe probe, string id) => probe.Id != "c_immediate" ||
        id is not ("prototype_u11" or "prototype_u13" or "prototype_u16" or "prototype_u25");
    private static CompiledRunChoice? ChooseV1Offer(RunApplication app, PendingRunOffer offer, V1Probe probe)
    {
        var eligible = offer.Choices.Where(c => app.CheckOfferChoice(c).Succeeded);
        if (offer.Kind == RunOfferKind.Recruitment) eligible = eligible.Where(c => AllowedV1(probe, c.ContentId));
        return eligible.OrderBy(c => offer.Kind == RunOfferKind.Recruitment ? RankV1(probe, c.ContentId) : ItemRankV1(probe, c))
            .ThenBy(c => c.Costs.Where(o => o.Kind == RunOperationKind.SpendGold).Sum(o => o.Amount))
            .ThenBy(c => c.StableId, StringComparer.Ordinal).FirstOrDefault();
    }
    private static int ItemRankV1(V1Probe probe, CompiledRunChoice choice)
    {
        // Preregistered role preference, independent of battle outcomes; never buy unavailable stock.
        var order = probe.Id is "a_ranged" or "b_remembered" or "a_b_mixed"
            ? new[] { "e10", "e06", "e03", "e07", "e02", "e11", "r10", "r07", "r06", "r09", "r08", "r03", "r01", "r11", "r02", "r04", "r05" }
            : new[] { "e06", "e11", "e02", "e07", "e10", "e03", "r09", "r10", "r06", "r07", "r08", "r01", "r03", "r11", "r02", "r04", "r05" };
        var index = Array.FindIndex(order, token => choice.ContentId.EndsWith("_" + token, StringComparison.Ordinal));
        if (index >= 0) return index;
        if (choice.SuccessChance < 1) return 200;
        if (choice.Operations.Any(o => o.Kind is RunOperationKind.GainGold or RunOperationKind.RecoverRunHealth)) return 50;
        return 100;
    }
    private static void PrepareV1(RunApplication app, V1Probe probe, List<JsonElement> ledger)
    {
        var run = app.ActiveRun ?? throw new V1Unavailable("route is terminal");
        // One ascension per currently affordable tranche; real offered discovery only.
        for (var tranche = 0; tranche < 5 && run.Growth!.Materials >= app.GrowthRules!.AscensionCost; tranche++)
        {
            var candidates = run.Roster.Where(h => h.Growth.AscensionId.Length == 0)
                .OrderBy(h => RankV1(probe, h.ContentId)).ThenBy(h => h.ContentId, StringComparer.Ordinal).ToArray();
            var succeeded = false;
            foreach (var hero in candidates)
            {
                var before = V1Budget(run); var result = app.AscendHero(hero.InstanceId);
                if (!result.Succeeded) { ledger.Add(B3Json(new { Kind = "ascend-unavailable", Hero = hero.ContentId, result.Message })); continue; }
                var discovery = run.Growth!.PendingDiscovery!;
                var chosen = discovery.CandidateIds.Where(id => AllowedV1(probe, id)).OrderBy(id => RankV1(probe, id)).ThenBy(id => id, StringComparer.Ordinal).FirstOrDefault();
                if (chosen is null) throw new V1Unavailable("paid discovery has no content compatible with this preregistered group");
                var offered = discovery.CandidateIds.ToArray();
                var selectedHero = app.ChooseGrowthHero(chosen);
                Require(selectedHero.Succeeded, $"formal discovery selection rejected: hero={chosen} offered={string.Join(",", offered)} reason={selectedHero.Message}");
                ledger.Add(B3Json(new { Kind = "ascend-and-discover", Hero = hero.ContentId, hero.InstanceId,
                    Candidates = offered, Selected = chosen, BudgetBefore = before, BudgetAfter = V1Budget(run) }));
                succeeded = true; break;
            }
            if (!succeeded) break;
        }
        var selected = run.Roster.OrderBy(h => RankV1(probe, h.ContentId)).ThenBy(h => h.ContentId, StringComparer.Ordinal)
            .Take(run.CurrentPopulation).ToArray();
        var desired = V1FormationSlots(selected, run.Deployment.Count);
        if (run.Deployment.Where(id => id.Length > 0).Any(id => !desired.ContainsKey(id)) || desired.Any(pair => run.Deployment[pair.Value] != pair.Key))
        {
            foreach (var id in run.Deployment.Where(id => id.Length > 0 && !desired.ContainsKey(id)).ToArray())
            {
                var incoming = desired.Keys.FirstOrDefault(wanted => !run.Deployment.Contains(wanted));
                if (incoming is not null) Require(app.MoveDeploymentUnit(incoming, run.Deployment.IndexOf(id)), $"formal reserve replacement rejected: incoming={incoming} outgoing={id} slot={run.Deployment.IndexOf(id)} roster={run.Roster.Count} population={run.CurrentPopulation}");
                else Require(app.WithdrawDeploymentUnit(id), $"formal deployment withdraw rejected: hero={id} roster={run.Roster.Count} population={run.CurrentPopulation}");
            }
            foreach (var pair in desired)
                if (run.Deployment[pair.Value] != pair.Key) Require(app.MoveDeploymentUnit(pair.Key, pair.Value), $"formal predetermined deployment rejected: hero={pair.Key} slot={pair.Value} deployment={string.Join(",",run.Deployment)}");
            ledger.Add(B3Json(new { Kind = "formation", Policy = "v1_selected_legal_role_cells_with_hunter_dragon_neighbor", run.Deployment }));
        }
        if (probe.Cultivate)
        {
            var core = run.Roster.Single(h => h.ContentId == IdV1(probe.Core));
            foreach (var producer in run.Roster.Where(h => app.GrowthRules!.Heroes.TryGetValue(h.ContentId, out var rule) &&
                rule.ProductionModes.Contains(GrowthProductionMode.Cultivation)))
                if (producer.Growth.ProductionTargetInstanceId != core.InstanceId || producer.Growth.ProductionMode != GrowthProductionMode.Cultivation)
                {
                    var assignment = app.SetGrowthAssignment(producer.InstanceId, core.InstanceId, GrowthProductionMode.Cultivation);
                    Require(assignment.Succeeded, $"formal cultivation assignment rejected: producer={producer.ContentId}/{producer.InstanceId} target={core.ContentId}/{core.InstanceId} reason={assignment.Message}");
                    ledger.Add(B3Json(new { Kind = "assignment", Producer = producer.ContentId, Target = core.ContentId, Mode = "Cultivation" }));
                }
        }
        foreach (var item in run.EquipmentInventory.ToArray())
        {
            var recipients = run.Roster.Where(h => selected.Any(s => s.InstanceId == h.InstanceId)).OrderBy(h => RankV1(probe, h.ContentId));
            foreach (var hero in recipients)
            {
                if (hero.Equipment.Any(e => e.ContentId == item.ContentId)) continue;
                var slot = Enumerable.Range(0, app.Rules.EquipmentSlotCapacity).FirstOrDefault(i => hero.Equipment.All(e => e.SlotIndex != i), -1);
                if (slot < 0) continue;
                Require(app.EquipOwnedItem(item.InstanceId, hero.InstanceId, slot), $"equip owned item rejected: item={item.ContentId}/{item.InstanceId} owner={hero.ContentId}/{hero.InstanceId} slot={slot}");
                ledger.Add(B3Json(new { Kind = "equip", item.InstanceId, item.ContentId, Owner = hero.ContentId, Slot = slot })); break;
            }
        }
    }

    private static Dictionary<string,int> V1FormationSlots(RosterHeroInstanceDto[] selected,int slotCount)
    {
        // Nineteen possible identities share eighteen cells, but only selected members
        // receive positions. Preferences are not a nineteen-unit hard-coded deployment.
        var preferred = new Dictionary<int,Vector2I> {
            [1]=new(2,0),[2]=new(2,2),[3]=new(0,0),[4]=new(2,4),[5]=new(1,5),
            [6]=new(0,3),[7]=new(2,4),[8]=new(2,5),[11]=new(1,2),[13]=new(0,2),
            [14]=new(2,1),[16]=new(0,5),[17]=new(1,0),[18]=new(1,0),[19]=new(0,1),
            [20]=new(1,1),[21]=new(1,4),[22]=new(0,4),[25]=new(2,3) };
        var result = new Dictionary<string,int>(StringComparer.Ordinal);
        var used = new HashSet<Vector2I>();
        var paired = selected.Any(h=>h.ContentId==IdV1(11)) && selected.Any(h=>h.ContentId==IdV1(13));
        foreach(var hero in selected.OrderBy(h=>paired&&h.ContentId==IdV1(13)?0:paired&&h.ContentId==IdV1(11)?1:2))
        {
            var number=int.Parse(hero.ContentId[^2..]);
            var preference=preferred[number];
            var candidates=BattlefieldLayout.PlayerDeploymentCells.Where(cell=>BattlefieldLayout.IsPlayerDeploymentCell(cell)&&
                BattlefieldLayout.PlayerDeploymentSlot(cell)>=0&&BattlefieldLayout.PlayerDeploymentSlot(cell)<slotCount&&!used.Contains(cell));
            // Keep hunter uniquely nearer than the remaining bodies to dragon; the
            // second link remains a real nearest-neighbor selection, never injected.
            if(paired&&number is not (11 or 13))
                candidates=candidates.Where(cell=>((Vector2)cell).DistanceSquaredTo(new Vector2(0,2))>1.0001f);
            var ordered=candidates.OrderBy(cell=>Math.Abs(cell.X-preference.X)*4+Math.Abs(cell.Y-preference.Y))
                .ThenBy(cell=>BattlefieldLayout.PlayerDeploymentSlot(cell)).ToArray();
            if(ordered.Length==0)throw new V1Unavailable("selected roster has no remaining legal role cell");
            var chosen=ordered[0];var slot=BattlefieldLayout.PlayerDeploymentSlot(chosen);
            Require(BattlefieldLayout.IsPlayerDeploymentCell(chosen)&&slot>=0&&slot<slotCount,"v1 legal deployment cell and slot");
            used.Add(chosen);result.Add(hero.InstanceId,slot);
        }
        Require(result.Count==selected.Length&&result.Values.Distinct().Count()==selected.Length,"v1 unique selected-member deployment");
        return result;
    }

    private async Task V1Battle(RunApplication app, EncounterPlan encounter, V1Probe probe, List<JsonElement> ledger)
    {
        var before = V1Budget(app.ActiveRun!);
        var frozen = app.TryBeginGrowthBattle(encounter);
        if (!frozen.Succeeded) throw new V1Unavailable("route preparation blocked: " + frozen.Message);
        BattleResult result;
        using (var battle = new BattleSimulation(app.BuildBattleConfig(encounter))) result = battle.RunToEnd();
        var resolution = app.ResolveBattle(result, encounter);
        Require(resolution.Accepted, $"unmodified actual route result rejected: profile={encounter.ExpeditionProfileId} site={encounter.ExpeditionSiteId} outcome={result.Outcome} failure={resolution.Failure}");
        ledger.Add(B3Json(new { Kind = "battle", encounter.ExpeditionSiteId, encounter.ExpeditionProfileId,
            result.Outcome, result.Ticks, result.Digest, result.RememberedGains, result.PermanentGains,
            BudgetBefore = before, BudgetAfter = app.ActiveRun is { } after ? V1Budget(after) : null }));
        if (result.Outcome != BattleOutcome.PlayerVictory || app.ActiveRun is null)
            throw new V1Unavailable("actual route did not win; no result replacement or retry: " + encounter.ExpeditionProfileId);
        for (var group = 0; app.PendingOffer is { } offer && group < 8; group++)
        {
            var choice = ChooseV1Offer(app, offer, probe); var budget = V1Budget(app.ActiveRun!);
            var selected = app.ResolveOffer(offer.OfferId, choice?.StableId);
            Require(selected.Succeeded, $"actual earned reward choice rejected: offer={offer.OfferId} selected={choice?.StableId} reason={selected.Message}");
            ledger.Add(B3Json(new { Kind = "reward", offer.OfferId, offer.RewardCategory, Candidates = offer.Choices,
                Selected = choice?.StableId, selected.Changes, BudgetBefore = budget, BudgetAfter = V1Budget(app.ActiveRun!) }));
        }
        Require(app.PendingOffer is null && app.ActiveRun!.Action is null, $"bounded reward chain incomplete: pending={app.PendingOffer?.OfferId} phase={app.ActiveRun?.Action?.Phase}");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private static ExpeditionSiteDto V1NextSite(RunApplication app, CompiledExpedition expedition, HashSet<string> ignored)
    {
        var map = app.ActiveRun!.Expedition!;
        var choices = map.Sites.Where(s => !s.Cleared && !s.Handled && !ignored.Contains(s.SiteId))
            .Select(site => new { Site = site, Path = V1Route(map, site.CellIndex), Definition = expedition.Sites.Single(d => d.StableId == site.DefinitionId) })
            .Where(value => value.Path is not null)
            .Select(value => new
            {
                value.Site, value.Path,
                Score = value.Path!.Count(cell => ExpeditionNavigation.IsBlocked(map, cell)) * 12 + value.Path!.Count * .1 +
                    map.Cells[value.Site.CellIndex].BiomeIndex * 2 + (value.Definition.Kind switch
                    {
                        TowerNodeType.Recruitment => 0,
                        TowerNodeType.Shop when app.ActiveRun!.Gold >= 12 => 16,
                        TowerNodeType.Elite => 18,
                        TowerNodeType.Event => 24,
                        TowerNodeType.Combat => 28,
                        _ => 30
                    })
            }).OrderBy(value => value.Score).ThenBy(value => value.Site.SiteId, StringComparer.Ordinal).ToArray();
        return choices.FirstOrDefault()?.Site ?? throw new V1Unavailable("no remaining reachable site");
    }
    private static List<int>? V1Route(ExpeditionStateDto map, int target)
    {
        var previous = new Dictionary<int,int> { [map.PositionIndex] = map.PositionIndex };
        var distance = new Dictionary<int,int> { [map.PositionIndex] = 0 };
        var queue = new PriorityQueue<int,int>(); queue.Enqueue(map.PositionIndex, 0);
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
                var nextCost = cost + (ExpeditionNavigation.IsBlocked(map, next) ? 100 : 1);
                if (distance.TryGetValue(next, out var known) && known <= nextCost) continue;
                distance[next] = nextCost; previous[next] = cell; queue.Enqueue(next, nextCost);
            }
        }
        return null;
    }
    private static object V1Budget(ActiveRunDto run) => B3Json(new
    {
        run.Gold, run.CurrentRunHealth, run.CurrentPopulation, run.NextActionSequence,
        Stage = (run.Expedition?.StageIndex ?? -1) + 1, RemainingActions = run.Expedition?.RemainingActions,
        ProducedMaterials = run.Growth?.History.Sum(h => h.MaterialsGranted) ?? 0,
        Materials = run.Growth?.Materials ?? 0,
        Ascended = run.Roster.Where(h => h.Growth.AscensionId.Length > 0).Select(h => new { h.ContentId, h.Growth.AscensionId }),
        HeroCount = run.Roster.Count, Deployed = run.Deployment.Count(id => id.Length > 0),
        EquipmentCount = run.EquipmentInventory.Count + run.Roster.Sum(h => h.Equipment.Count),
        Relics = run.Items.Select(i => new { i.ContentId, i.Stacks, i.RunRuleStates }),
        Roster = run.Roster.Select(h => new { h.InstanceId, h.ContentId, h.Growth.AddedAttack, h.Growth.AddedMaxHealth,
            h.Growth.BreedingPoints, h.Growth.ProductionMode, h.Growth.ProductionTargetInstanceId,
            RememberedEntries = h.Growth.History.Count(g => g.Source == "remembered"), h.Equipment }),
        GrowthHistory = run.Growth?.History
    });
    private static void ValidateV1Checkpoint(CompiledGamePackage package, V1Checkpoint checkpoint, V1Probe probe, int stage)
    {
        Require(checkpoint.ContentFingerprint == package.DefaultProfile.ContentFingerprint && checkpoint.Group == probe.Id && checkpoint.Stage == stage,
            "v1 checkpoint version/group/stage identity");
        var save = new B3Save { Json = JsonSerializer.Serialize(checkpoint.Run), Meta = checkpoint.Meta };
        var persistence = new RunProgressionPersistenceService(package.Content, save, package.Project, package.GrowthRules, package.DefaultProfile);
        Require(persistence.ValidateRun(checkpoint.Run), $"v1 full run ledger validation rejected: group={checkpoint.Group} stage={stage+1} run={checkpoint.Run.RunId}");
        var app = new RunApplication(package, save, "v1-content-calibration");
        var run = app.ActiveRun ?? throw new InvalidOperationException("v1 checkpoint cold restore");
        Require(run.Expedition is { } map && map.StageIndex == stage && map.RemainingActions == (stage == 2 ? 0 : 2) &&
            run.Action is null && run.PendingOffer is null && run.Growth is { PendingDiscovery: null } && checkpoint.Ledger.Count > 0,
            "v1 exact reachable checkpoint boundary");
        Require(probe.Opening.Select(IdV1).All(id => run.Roster.Any(h => h.ContentId == id)), "v1 opening identity retained");
        var produced = run.Growth!.History.Sum(h => h.MaterialsGranted);
        var ascensions = run.Roster.Count(h => h.Growth.AscensionId.Length > 0);
        Require(produced == new[] { 4, 11, 20 }[stage] && produced - ascensions * package.GrowthRules!.AscensionCost == run.Growth!.Materials,
            $"material reconciliation failed: group={probe.Id} stage={stage+1} produced={produced} ascensions={ascensions} cost={package.GrowthRules!.AscensionCost} remaining={run.Growth!.Materials}");
        Require(run.Roster.Count <= run.CurrentPopulation + package.Project.RunRules.ReserveCapacity,
            "actual roster reception capacity");
    }
}
