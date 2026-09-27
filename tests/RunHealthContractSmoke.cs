using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;
using TowerAutobattler.Run;

// Controlled outcomes exercise real simulator receipts, not natural balance or win rates.
public partial class RunHealthContractSmoke : Node
{
    public override async void _Ready()
    {
        var code = 0;
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            VerifySettlement(package);
            VerifyRecovery(package);
            VerifyPersistence(package);
            RelicContractSmoke.ReactiveCounterRuntimeAndPersistence(package.Content, RelicBattleCompletionReason.PlayerDefeat);
            RelicContractSmoke.ReactiveCounterRuntimeAndPersistence(package.Content, RelicBattleCompletionReason.Timeout);
            GD.Print("RUN_HEALTH_CONTRACT_OK normal elite timeout boss exhausted victory gold relic-receipt duplicate rollback recovery migration reload");
        }
        catch (Exception error) { code = 1; GD.PrintErr("RUN_HEALTH_CONTRACT_FAILED: " + error); }
        GetTree().Quit(code);
    }

    private static void VerifySettlement(CompiledGamePackage package)
    {
        foreach (var type in new[] { TowerNodeType.Combat, TowerNodeType.Elite, TowerNodeType.Boss })
        foreach (var outcome in new[] { BattleOutcome.PlayerDefeat, BattleOutcome.Timeout, BattleOutcome.PlayerVictory })
        {
            var (app, save) = RunHealthFixture.New(package);
            var run = app.ActiveRun!;
            RunHealthFixture.BattleNode(app, type);
            var encounter = app.CurrentEncounter();
            // Existing real relics check that victory-only income cannot leak into defeat receipts.
            Require(app.GrantItem("item_gilded_contract"), "grant victory relic");
            var config = app.BuildBattleConfig(encounter, false);
            var result = RunHealthFixture.Result(config, outcome);
            var formation = run.Deployment.ToArray();
            var gold = run.Gold;
            var items = JsonSerializer.Serialize(run.Items);
            var floor = run.FloorIndex;
            var before = JsonSerializer.Serialize(run);
            save.FailWrites = true;
            var rejected = app.ResolveBattle(result, encounter);
            Require(!rejected.Accepted && rejected.Failure == RunBattleResolutionFailure.PersistenceFailed &&
                JsonSerializer.Serialize(run) == before, "failed save rolls back all battle consequences");
            save.FailWrites = false;
            var settled = app.ResolveBattle(result, encounter);
            Require(settled.Accepted, $"accept {type}/{outcome}");
            var ended = type == TowerNodeType.Boss && outcome != BattleOutcome.PlayerVictory;
            var loss = outcome == BattleOutcome.PlayerVictory || type == TowerNodeType.Boss ? 0 : RunHealthPolicy.DefeatLoss(app.Rules, type);
            Require(run.CurrentRunHealth == app.Rules.InitialRunHealth - loss && run.BattleNumber == 1 &&
                settled.Consequence?.HealthLost == loss && (app.ActiveRun is null) == ended, "health and terminal rule");
            Require(!app.ResolveBattle(result, encounter).Accepted, "duplicate settlement cannot charge or reward twice");
            if (!ended)
            {
                var restored = new RunApplication(package.Content, save, package.Project);
                Require(restored.ActiveRun?.CurrentRunHealth == run.CurrentRunHealth &&
                    restored.ActiveRun.LastBattleConsequence == settled.Consequence, "settled health and consequence survive reload");
            }
            if (outcome == BattleOutcome.PlayerVictory)
                Require(run.PendingOffer?.Kind == RunOfferKind.CombatReward && run.Gold > gold, "victory retains original rewards");
            else
            {
                Require(run.PendingOffer is null && run.Gold == gold - result.GoldSpent &&
                    run.Deployment.SequenceEqual(formation) && JsonSerializer.Serialize(run.Items) == items,
                    "defeat has no spoils, retains formation and valid relic state");
                if (!ended)
                {
                    Require(run.FloorIndex == floor + 1 && !run.PendingNode, "defeat progresses one floor");
                    RunHealthFixture.BattleNode(app, TowerNodeType.Combat);
                    using var next = new BattleSimulation(app.BuildBattleConfig(app.CurrentEncounter(), false));
                    Require(next.Units.Where(unit => unit.Team == 0).All(unit => unit.Health == unit.MaxHealth), "next battle full hero health");
                }
            }
        }

        {
            var (app, _) = RunHealthFixture.New(package);
            Require(app.BeginOpeningRecruitment(7788) && app.ActiveRun?.CurrentRunHealth == 100 &&
                app.ActiveRun.MaximumRunHealth == 100, "production opening initializes global health");
        }
        {
            var (app, _) = RunHealthFixture.New(package);
            RunHealthFixture.BattleNode(app, TowerNodeType.Boss);
            app.ActiveRun!.FloorIndex = app.Project.Campaign.TotalFloors - 1;
            var encounter = app.CurrentEncounter();
            var result = RunHealthFixture.Result(app.BuildBattleConfig(encounter, false), BattleOutcome.PlayerVictory);
            var resolved = app.ResolveBattle(result, encounter);
            Require(resolved.Accepted && resolved.Consequence is { RunEnded: true, HealthLost: 0 } &&
                app.ActiveRun is null && app.Meta.Victories == 1, "final victory retains completion and no health loss");
        }

        foreach (var health in new[] { 1, 20 })
        {
            var (app, save) = RunHealthFixture.New(package);
            var run = app.ActiveRun!;
            run.CurrentRunHealth = health;
            RunHealthFixture.BattleNode(app, TowerNodeType.Combat);
            var encounter = app.CurrentEncounter();
            var result = RunHealthFixture.Result(app.BuildBattleConfig(encounter, false), BattleOutcome.PlayerDefeat);
            save.FailDelete = true;
            Require(!app.ResolveBattle(result, encounter).Accepted && run.CurrentRunHealth == 0 &&
                run.TerminalCompletionId.Length > 0, "terminal cleanup failure leaves durable zero-health receipt");
            var json = save.Json;
            Require(!app.ResolveBattle(result, encounter).Accepted && save.Json == json, "terminal retry never applies loss again");
            var restored = new RunApplication(package.Content, save, package.Project);
            Require(restored.ActiveRun?.CurrentRunHealth == 0, "pending terminal reloads at zero HP");
            save.FailDelete = false;
            Require(restored.ResumeTerminalCompletion() && restored.ActiveRun is null && save.Json is null, "terminal resume cleans up");
        }

        // Real command activation spends gold; the accepted defeat must carry it into Run.
        {
            var (app, _) = RunHealthFixture.New(package);
            var run = app.ActiveRun!;
            run.Gold = 100;
            run.EquippedTacticalCommandIds = ["tactical_paid_reinforcement", "tactical_rally"];
            RunHealthFixture.BattleNode(app, TowerNodeType.Combat);
            var encounter = app.CurrentEncounter();
            using var battle = new BattleSimulation(app.BuildBattleConfig(encounter, false));
            Require(battle.TryUseTacticalCommand(0).Succeeded, "paid reinforcement activation");
            foreach (var unit in battle.Units.Where(unit => unit.Team == 0)) unit.Health = 0;
            var result = battle.RunToEnd();
            Require(result.Outcome == BattleOutcome.PlayerDefeat && result.GoldSpent > 0, "paid defeat fixture");
            Require(app.ResolveBattle(result, encounter).Accepted && run.Gold == 100 - result.GoldSpent, "paid command retained on defeat");
        }
    }

    private static void VerifyRecovery(CompiledGamePackage package)
    {
        foreach (var health in new[] { 40, 90, 100 })
        {
            var (app, save) = RunHealthFixture.New(package);
            var run = app.ActiveRun!;
            run.CurrentRunHealth = health;
            RunHealthFixture.Camp(app, save);
            var offer = app.PendingOffer!;
            var choice = offer.Choices.Single(c => c.StableId == "recover_run_health");
            Require(app.CheckOfferChoice(choice).Succeeded == (health < 100), "full HP eligibility");
            var before = JsonSerializer.Serialize(run);
            Require(!app.ResolveOffer("stale", choice.StableId).Succeeded && JsonSerializer.Serialize(run) == before, "stale recovery no mutation");
            save.FailWrites = true;
            Require(!app.ResolveOffer(offer.OfferId, choice.StableId).Succeeded && JsonSerializer.Serialize(run) == before, "recovery save failure atomic");
            save.FailWrites = false;
            var result = app.ResolveOffer(offer.OfferId, choice.StableId);
            if (health == 100)
            {
                Require(!result.Succeeded && app.PendingOffer is not null, "full HP rejection preserves camp");
                var gold = run.Gold;
                Require(app.ResolveOffer(offer.OfferId, "gold").Succeeded && run.Gold == gold + app.Rules.RestGold, "gold alternate");
            }
            else Require(result.Succeeded && run.CurrentRunHealth == Math.Min(100, health + 25) &&
                result.Changes.Any(change => change.Kind == RunChangeKind.RunHealth) && run.FloorIndex == 1, "capped recovery consumes camp once");
            Require(!app.ResolveOffer(offer.OfferId, "gold").Succeeded && run.PendingOffer is null, "camp rewards mutually exclusive");
            var restored = new RunApplication(package.Content, save, package.Project);
            Require(restored.ActiveRun?.CurrentRunHealth == run.CurrentRunHealth, "recovery persists");
        }
    }

    private static void VerifyPersistence(CompiledGamePackage package)
    {
        var (app, save) = RunHealthFixture.New(package);
        var run = app.ActiveRun!;
        RunHealthFixture.Camp(app, save);
        var originalOffer = JsonSerializer.Serialize(run.PendingOffer);
        var node = JsonNode.Parse(save.Json!)!.AsObject();
        node["Version"] = 6;
        node.Remove("CurrentRunHealth"); node.Remove("MaximumRunHealth"); node.Remove("LastBattleConsequence");
        var legacy = node.ToJsonString();
        save.Json = legacy;
        save.FailWrites = true;
        Require(new RunApplication(package.Content, save, package.Project).ActiveRun is null && save.Json == legacy,
            "failed v6 publication retains original");
        save.FailWrites = false;
        var migrated = new RunApplication(package.Content, save, package.Project);
        Require(migrated.ActiveRun?.CurrentRunHealth == 100 && migrated.ActiveRun.MaximumRunHealth == 100 &&
            JsonSerializer.Serialize(migrated.ActiveRun.PendingOffer) == originalOffer, "v6 migration preserves frozen pending offer");
        var valid = save.Json!;
        foreach (var field in new[] { "CurrentRunHealth", "MaximumRunHealth" })
        {
            var broken = JsonNode.Parse(valid)!.AsObject(); broken.Remove(field);
            save.Json = broken.ToJsonString();
            var before = save.Json;
            Require(new RunApplication(package.Content, save, package.Project).ActiveRun is null && save.Json == before,
                "missing v7 HP rejected without reset");
        }
        foreach (var health in new[] { -1, 0, 101 })
        {
            var broken = JsonNode.Parse(valid)!.AsObject(); broken["CurrentRunHealth"] = health;
            save.Json = broken.ToJsonString();
            Require(new RunApplication(package.Content, save, package.Project).ActiveRun is null, "invalid health rejected");
        }
        // Exercise the real file store's migration backup, isolated from production.
        var ns = $"tests/run-health/{Guid.NewGuid():N}";
        var files = new SaveService(ns);
        Require(files.SaveActiveRun(JsonSerializer.Deserialize<ActiveRunDto>(legacy)!), "write isolated v6 source");
        Require(new RunApplication(package.Content, files, package.Project).ActiveRun?.Version == ActiveRunFormationSchema.CurrentVersion, "real v6 to current schema write");
        var dir = ProjectSettings.GlobalizePath("user://" + ns);
        Require(System.IO.Directory.GetFiles(dir, "active_run.json.v6.*.bak").Any(), "v6 source backup exists");
        files.DeleteActiveRun();
    }

    internal static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

internal static class RunHealthFixture
{
    public static (RunApplication, RunHealthMemorySave) New(CompiledGamePackage package)
    {
        var save = new RunHealthMemorySave();
        var app = new RunApplication(package.Content, save, package.Project);
        RunHealthContractSmoke.Require(app.StartNewRun(app.Meta.UnlockedHeroIds[0], 1776), "new Run");
        return (app, save);
    }

    public static void BattleNode(RunApplication app, TowerNodeType type)
    {
        var run = app.ActiveRun!;
        run.PendingOffer = null;
        run.PendingNode = true;
        run.SelectedNode = type;
        if (type == TowerNodeType.Boss) run.FloorIndex = app.Project.Campaign.FloorsPerRegion - 1;
    }

    public static void Camp(RunApplication app, RunHealthMemorySave save)
    {
        var run = app.ActiveRun!;
        run.SelectedNode = TowerNodeType.Rest;
        run.PendingNode = true;
        var persistence = new RunProgressionPersistenceService(app.Content, save, app.Project);
        run.PendingOffer = new RunDecisionService(app.Content, app.Project, persistence).CreateOffer(run, RunOfferKind.Rest);
        save.SaveActiveRun(run);
    }

    public static BattleResult Result(BattleConfig config, BattleOutcome outcome)
    {
        using var battle = new BattleSimulation(config);
        if (outcome != BattleOutcome.Timeout)
        {
            foreach (var unit in battle.Units.Where(unit => unit.Team == (outcome == BattleOutcome.PlayerVictory ? 1 : 0))) unit.Health = 0;
        }
        else
        {
            // Keep actors alive until the simulator's genuine timeout boundary.
            while (battle.Outcome == BattleOutcome.Running)
            {
                foreach (var unit in battle.Units) { unit.MaxHealth = 1_000_000; unit.Health = unit.MaxHealth; unit.Shield = 1_000_000; }
                battle.Step();
            }
        }
        var result = battle.RunToEnd();
        RunHealthContractSmoke.Require(result.Outcome == outcome, "controlled simulator outcome: " + outcome);
        return result;
    }
}

internal sealed class RunHealthMemorySave : IRunSaveService
{
    public string? Json { get; set; }
    public bool FailWrites { get; set; }
    public bool FailDelete { get; set; }
    public MetaProgressDto Meta { get; private set; } = new();
    public MetaProgressDto LoadMeta() => JsonSerializer.Deserialize<MetaProgressDto>(JsonSerializer.Serialize(Meta))!;
    public SettingsDto LoadSettings() => new() { ReduceUiMotion = true };
    public ActiveRunDto? LoadActiveRun() => Json is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(Json);
    public bool SaveMeta(MetaProgressDto value) { Meta = JsonSerializer.Deserialize<MetaProgressDto>(JsonSerializer.Serialize(value))!; return true; }
    public bool SaveSettings(SettingsDto value) => true;
    public bool SaveActiveRun(ActiveRunDto value) { if (FailWrites) return false; Json = JsonSerializer.Serialize(value); return true; }
    public void DeleteActiveRun() { if (FailDelete) throw new System.IO.IOException("controlled failure"); Json = null; }
}
