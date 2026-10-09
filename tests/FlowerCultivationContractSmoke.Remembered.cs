using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Growth;
using TowerAutobattler.Expedition;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

public partial class FlowerCultivationContractSmoke
{
    private static void VerifyRememberedTransactions(CompiledGamePackage package)
    {
        var (app, save) = NewRememberingRun(package);
        var run = app.ActiveRun!;
        var hunter = run.Roster.Single(hero => hero.ContentId == "prototype_u11");
        var dragon = run.Roster.Single(hero => hero.ContentId == "prototype_u13");
        Require(app.MoveDeploymentUnit(hunter.InstanceId, BattlefieldLayout.PlayerDeploymentSlot(new Vector2I(2, 2))) &&
            app.MoveDeploymentUnit(dragon.InstanceId, BattlefieldLayout.PlayerDeploymentSlot(new Vector2I(1, 2))),
            "formally place the two acquired members");
        var encounter = app.PreviewExpeditionBoss();
        Require(app.TryBeginGrowthBattle(encounter).Succeeded, "freeze a real first-stage encounter");
        var config = app.BuildBattleConfig(encounter);
        var opening = BattlePreparationProjection.Build(config);
        using var battle = new BattleSimulation(config);
        var result = battle.RunToEnd();
        Require(result.RememberedGains.Length == 2 && result.RememberedGains.All(gain =>
            gain.SourceInstanceId == dragon.InstanceId && gain.TargetInstanceId == hunter.InstanceId),
            "actual formal battle emits only one pair for the eligible hunter");
        var persistence = new RunProgressionPersistenceService(package.Content, save, package.Project,
            package.GrowthRules, package.DefaultProfile);
        var growth = new GrowthRunService(package.Content, package.Project, package.GrowthRules, persistence);
        var before = Snapshot(run);
        var disk = save.Json;
        var attack = result.RememberedGains.Single(gain => gain.Attribute == CombatAttribute.AttackDamage);
        var health = result.RememberedGains.Single(gain => gain.Attribute == CombatAttribute.MaxHealth);
        Reject([attack]);
        Reject([attack, attack]);
        Reject([attack, health with { GrantedTick = health.GrantedTick + 1 }]);
        Reject([attack with { Amount = 13 }, health]);
        Reject([attack with { SourceInstanceId = null! }, health]);
        Reject([attack with { TargetInstanceId = dragon.InstanceId }, health with { TargetInstanceId = dragon.InstanceId }]);
        Reject([attack with { GrantOrdinal = 2 }, health with { GrantOrdinal = 2 }]);
        Reject([attack with { OperationKey = "false_group" }, health with { OperationKey = "false_group" }]);
        Reject(result.RememberedGains, result.Units.Add(null!));
        Reject(result.RememberedGains, result.Units.Select(unit => unit.SourceInstanceId == hunter.InstanceId
            ? unit with { DefeatTick = attack.GrantedTick - 1 } : unit).ToImmutableArray());
        var sameTickDeath = result with { Units = result.Units.Select(unit => unit.SourceInstanceId == hunter.InstanceId
            ? unit with { Alive = false, FinalHealth = 0, DefeatTick = attack.GrantedTick } : unit).ToImmutableArray() };
        var sameTickProjection = persistence.CloneRun(run);
        Require(growth.ApplyBattleRememberedGains(run, sameTickProjection, sameTickDeath, opening, true),
            "a recipient defeated later in the same tick still retains the actual grant");
        var noApply = persistence.CloneRun(run);
        Require(growth.ApplyBattleRememberedGains(run, noApply, result, opening, false) && Snapshot(noApply) == before,
            "no-apply validates receipts without publishing permanent gains");

        // The receipt and combat above are real. Controlled outcome fixtures below
        // isolate victory/defeat persistence policy from this lineup's balance.
        var victory = RunHealthFixture.Result(config, BattleOutcome.PlayerVictory) with
        { Ticks = result.Ticks, Units = result.Units, RememberedGains = result.RememberedGains };
        save.FailWrites = true;
        Require(!app.ResolveBattle(victory, encounter).Accepted && Snapshot(run) == before && save.Json == disk,
            "failed publication keeps both permanent attributes and the original save unchanged");
        save.FailWrites = false;
        Require(app.ResolveBattle(victory, encounter).Accepted, "the same frozen result can retry once");
        hunter = run.Roster.Single(hero => hero.ContentId == "prototype_u11");
        Require(hunter.Growth.AddedAttack == 12 && hunter.Growth.AddedMaxHealth == 120 &&
            hunter.Growth.History.Count(gain => gain.Source == "remembered") == 2, "one committed pair is permanent");
        var saved = Snapshot(run);
        Require(!app.ResolveBattle(victory, encounter).Accepted && Snapshot(run) == saved,
            "already accepted results cannot grant another pair");
        while (app.PendingOffer is { } reward) Require(app.ResolveOffer(reward.OfferId, null).Succeeded, "settled reward queue finishes");
        app = new RunApplication(package, save, "remembered-contract");
        run = app.ActiveRun ?? throw new InvalidOperationException("remembered save must resume");
        hunter = run.Roster.Single(hero => hero.ContentId == "prototype_u11");
        Require(hunter.Growth.AddedAttack == 12 && hunter.Growth.AddedMaxHealth == 120 && growth.Validate(run),
            "resume preserves the source metadata and complete pair");
        var altered = persistence.CloneRun(run);
        altered.Roster.Single(hero => hero.InstanceId == hunter.InstanceId).Growth.History[0].RememberOperationKey = "wrong";
        Require(!growth.Validate(altered), "tampered remembrance qualification cannot load");
        altered = persistence.CloneRun(run);
        altered.Roster.Single(hero => hero.InstanceId == hunter.InstanceId).Growth.History.RemoveAt(0);
        Require(!growth.Validate(altered), "a half historical pair cannot load");
        var second = app.PreviewExpeditionBoss();
        var next = BattlePreparationProjection.Build(app.BuildBattleConfig(second));
        var nextHunter = next.Units.Single(unit => unit.SourceInstanceId == hunter.InstanceId && !unit.IsTemporary);
        Require(nextHunter.Definition.MaxHealth >= 820 && nextHunter.Definition.Damage >= 64,
            "next battle receives saved permanent stats once as base values");
        Require(next.OpeningAllyLinks.Any(link => link.SourceInstanceId == dragon.InstanceId && link.TargetInstanceId == hunter.InstanceId),
            "opening bindings remain rebuilt from formal inputs");
        GD.Print($"REMEMBERED_GROWTH_TRANSACTION_OK actual-outcome={result.Outcome} paired qualification ticks nulls rollback retry dedupe restore next-battle");

        void Reject(ImmutableArray<BattleRememberedGain> malformed,
            ImmutableArray<BattleUnitReportSnapshot> reports = default)
        {
            var projection = persistence.CloneRun(run);
            Require(!growth.ApplyBattleRememberedGains(run, projection,
                result with { RememberedGains = malformed, Units = reports.IsDefault ? result.Units : reports }, opening, true) &&
                Snapshot(projection) == before, "malformed receipt rejects before mutating either attribute");
        }
    }

    private static (RunApplication, MemorySave) NewRememberingRun(CompiledGamePackage package)
    {
        // Search actual deterministic opening/recruitment outcomes. No edited
        // roster, unlocked pool, injected gold or direct recruit bypass is used.
        var save = new MemorySave();
        var app = new RunApplication(package, save, "remembered-contract");
        var openings = 0; var offeredDragons = 0;
        for (ulong seed = 20261007; seed < 20261135; seed++)
        {
            Require(app.BeginOpeningRecruitment(seed), "generate the actual opening offer");
            if (!new[] { "prototype_u11", "prototype_u02" }.All(id => app.ActiveRun!.OpeningRecruitment!.CandidateIds.Contains(id))) continue;
            Require(app.ToggleOpeningHero("prototype_u11") && app.ToggleOpeningHero("prototype_u02") && app.ConfirmOpeningRecruitment(),
                "select two eligible opening heroes");
            var run = app.ActiveRun!;
            openings++;
            offeredDragons += run.Expedition!.Sites.Count(site => site.ServiceOffer is { } stock && stock.Choices.Any(choice =>
                choice.Operations.Any(operation => operation.Kind == RunOperationKind.Recruit && operation.ContentId == "prototype_u13")));
            var sites = run.Expedition!.Sites.Where(site => site.ServiceOffer is { } offer && offer.Choices.Any(choice =>
                choice.Operations.Any(operation => operation.Kind == RunOperationKind.Recruit && operation.ContentId == "prototype_u13")))
                .Select(site => (Site: site, Route: GrowthRouteThroughGuards(run.Expedition, site.CellIndex)))
                .Where(value => value.Route is not null).OrderBy(value => value.Route!.Count(index =>
                    ExpeditionNavigation.IsBlocked(run.Expedition, index))).ThenBy(value => value.Route!.Count).ToArray();
            if (sites.Length == 0) continue;
            var reached = true;
            foreach (var cell in sites[0].Route!)
            {
                if (run.Expedition.RemainingActions == 0 || app.ActiveRun is null) { reached = false; break; }
                if (ExpeditionNavigation.SiteAt(run.Expedition, cell) is { Cleared: false, EncounterProfileId.Length: > 0 } guard)
                {
                    var guardedEncounter = app.PreviewExpeditionEncounter(guard.SiteId);
                    Require(app.TryBeginGrowthBattle(guardedEncounter).Succeeded, "approached guard freezes a real battle");
                    using var guardBattle = new BattleSimulation(app.BuildBattleConfig(guardedEncounter));
                    var guardResult = guardBattle.RunToEnd();
                    Require(app.ResolveBattle(guardResult, guardedEncounter).Accepted, "natural guard outcome commits");
                    if (guardResult.Outcome != BattleOutcome.PlayerVictory) { reached = false; break; }
                    while (app.PendingOffer is { } reward) Require(app.ResolveOffer(reward.OfferId, null).Succeeded, "complete actual guard reward");
                }
                if (!app.MoveExpedition(cell)) { reached = false; break; }
            }
            if (!reached || run.Expedition.RemainingActions == 0) continue;
            Require(app.InspectExpeditionSite(sites[0].Site.SiteId), "inspect actual recruitment site");
            var offer = app.PendingOffer!;
            var choice = offer.Choices.Single(value => value.Operations.Any(operation =>
                operation.Kind == RunOperationKind.Recruit && operation.ContentId == "prototype_u13"));
            Require(app.ResolveOffer(offer.OfferId, choice.StableId).Succeeded, "formally acquire the offered young dragon");
            return (app, save);
        }
        throw new InvalidOperationException($"No eligible natural opening/recruitment in bounded seed search: openings={openings} offered-dragons={offeredDragons}.");
    }

    // This contract fixture may inspect the generated map to choose a short
    // deterministic route. Every step, guard battle and acquisition remains a
    // formal command. Fog-limited player input is tested by the separate GUI path.
    private static List<int>? GrowthRouteThroughGuards(ExpeditionStateDto map, int target)
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
                var candidate = cost + (ExpeditionNavigation.IsBlocked(map, next) ? 100 : 1);
                if (distance.TryGetValue(next, out var known) && known <= candidate) continue;
                distance[next] = candidate; previous[next] = cell; queue.Enqueue(next, candidate);
            }
        }
        return null;
    }
}
