using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;
using TowerAutobattler.UI;

// Production content, isolated in-memory saves. No natural battle win is claimed.
public partial class RunFullHealthContractSmoke : Node
{
    public override async void _Ready()
    {
        var code = 0;
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            var save = new MemorySave(FileAccess.GetFileAsString("res://tests/fixtures/first-boss-ranged-stall.json"));
            var app = new RunApplication(package.Content, save, package.Project);
            var run = app.ActiveRun ?? throw new InvalidOperationException("fixture load failed");
            run.Roster[0].HealthRatio = .08f;
            run.Roster[1].HealthRatio = 0;
            Require(app.GrantItem("item_field_rations"), "grant real maximum-health relic");
            var encounter = app.CurrentEncounter();
            var before = JsonSerializer.Serialize(run);
            var writes = save.Writes;
            var config = app.BuildBattleConfig(encounter);
            using var battle = new BattleSimulation(config);
            Require(battle.Units.Where(unit => unit.Team == 0).All(unit => Math.Abs(unit.Health - unit.MaxHealth) < .001f),
                "old wound/death ratios cannot reduce Run entry health after modifiers");
            var hero = battle.Units.First(unit => unit.SourceInstanceId == run.Roster[0].InstanceId);
            var baseline = BattleSetupFactory.Snapshot(package.Content.Catalog.Heroes.Single(entry => entry.StableId == hero.Definition.ContentId), package.Content);
            Require(hero.MaxHealth > baseline.MaxHealth, "full entry uses the modified maximum, not the base maximum");
            Require(JsonSerializer.Serialize(run) == before && save.Writes == writes, "preparing a battle remains read-only");

            // The simulator retains explicit partial starts for lab/scenario tests and ordinary combat.
            config.Spawns[0] = config.Spawns[0] with { HealthRatio = .4f };
            using (var partial = new BattleSimulation(config))
                Require(partial.Units.Any(unit => unit.Team == 0 && unit.Health < unit.MaxHealth),
                    "full Run starts do not globally disable wounds in the simulation");

            var persistence = new RunProgressionPersistenceService(package.Content, save, package.Project);
            var rewards = new RunRewardEconomyService(package.Content, package.Project, persistence, run);
            var reports = battle.ReadUnitReports().Select(unit => unit.Team != 0 ? unit : unit with
            {
                Alive = unit.SourceInstanceId != run.Roster[1].InstanceId,
                FinalHealth = unit.SourceInstanceId == run.Roster[1].InstanceId ? 0 : unit.MaxHealth * .1f
            }).ToImmutableArray();
            rewards.ApplyBattleVictory(run, new BattleResult(BattleOutcome.PlayerVictory, 1, "health-contract", reports, 0), encounter, 0);
            Require(run.Roster.All(unit => unit.HealthRatio == 1) && !run.Deployment.Contains(run.Roster[1].InstanceId),
                "victory clears wounds while retaining the existing casualty deployment rule");
            var freeSlot = run.Deployment.FindIndex(string.IsNullOrEmpty);
            Require(app.MoveDeploymentUnit(run.Roster[1].InstanceId, freeSlot), "redeploy a recovered casualty");
            using (var next = new BattleSimulation(app.BuildBattleConfig(encounter)))
                Require(next.Units.Where(unit => unit.Team == 0).All(unit => Math.Abs(unit.Health - unit.MaxHealth) < .001f),
                    "survivors and a redeployed casualty both start the next battle full");

            var definition = (UnitDefinition)package.Content.Catalog.Heroes.Single(entry => entry.StableId == hero.Definition.ContentId).Definition;
            var prepared = new PreparedUnitDetails(hero.Definition,
                Enum.GetValues<CombatAttribute>().ToDictionary(attribute => attribute, attribute => hero.Attributes.GetValue(attribute)), 17, 12);
            foreach (var scene in new[] { "UnitVitals", "CardVitals" })
            {
                var view = GD.Load<PackedScene>($"res://scenes/ui/components/{scene}.tscn").Instantiate<UnitVitals>();
                AddChild(view);
                view.Bind(new UnitInformation("maximum", definition, baseline, prepared));
                Require(view.GetNode<Label>("%HealthValue").Text == (view.CompactSymbols ? $"{hero.MaxHealth:0.#}" : $"生命上限 {hero.MaxHealth:0.#}") &&
                    view.GetNode<DetailExplainButton>("%HealthFact").ExplanationTitle == "生命上限",
                    "both information layouts expose maximum health independently of transient health/shield");
                view.Free();
            }

            var decisions = new RunDecisionService(package.Content, package.Project, persistence);
            run.SelectedNode = TowerNodeType.Rest;
            run.PendingNode = true;
            run.PendingOffer = decisions.CreateOffer(run, RunOfferKind.Rest);
            Require(run.PendingOffer.Choices.Length == 2 && run.PendingOffer.Choices.Any(choice => choice.StableId == "recover_run_health"), "rest adds independent Run recovery alongside original gold");
            run.PendingOffer = run.PendingOffer with { Choices = run.PendingOffer.Choices.Add(new("recover", "全军休整", "旧存档回血", [], [],
                [new(RunOperationKind.RecoverRoster, Ratio: .35f)], FailureOperations: [])) };
            writes = save.Writes;
            Require(app.PendingOffer!.Choices.Length == 2 && app.PendingOffer.Choices.All(choice => choice.StableId != "recover") && save.Writes == writes, "old hero-wound choice is retired without writes; Run recovery remains");
            var gold = run.Gold;
            var offerId = run.PendingOffer.OfferId;
            before = JsonSerializer.Serialize(run);
            save.FailWrites = true;
            Require(!app.ResolveOffer(offerId, "gold").Succeeded && JsonSerializer.Serialize(run) == before, "failed rest save retains the opportunity");
            save.FailWrites = false;
            Require(app.ResolveOffer(offerId, "gold").Succeeded && run.Gold == gold + app.Rules.RestGold &&
                !app.ResolveOffer(offerId, "gold").Succeeded, "rest grants exactly the original gold once");

            foreach (var chance in new[] { 0f, 1f })
            {
                run.SelectedNode = TowerNodeType.Event;
                run.PendingNode = true;
                var offer = decisions.CreateOffer(run, RunOfferKind.Event);
                Require(offer.Choices.Single(choice => choice.StableId == "risky").FailureOperations.IsEmpty, "new event has no health loss");
                run.PendingOffer = offer with { Choices = offer.Choices.Select(choice => choice.StableId == "risky" ? choice with
                {
                    SuccessChance = chance,
                    FailureOperations = [new(RunOperationKind.RecoverRoster, Ratio: -.25f)]
                } : choice).ToImmutableArray() };
                gold = run.Gold;
                var result = app.ResolveOffer(offer.OfferId, "risky");
                Require(result.Succeeded && result.ChanceSucceeded == (chance == 1) &&
                    run.Gold == gold + (chance == 1 ? app.Rules.RiskyEventSuccessGold : 0) && run.Roster.All(unit => unit.HealthRatio == 1),
                    "saved default event retains its chance/reward and never subtracts health on failure");
            }
            GD.Print("RUN_FULL_HEALTH_CONTRACT_OK modified-max old-save casualty-next-battle simulation-wounds maximum-only-ui rest-once rest-rollback event-success-failure");
        }
        catch (Exception error) { code = 1; GD.PrintErr("RUN_FULL_HEALTH_CONTRACT_FAILED: " + error); }
        GetTree().Quit(code);
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class MemorySave(string json) : IRunSaveService
    {
        private string? _json = json;
        public int Writes { get; private set; }
        public bool FailWrites { get; set; }
        public MetaProgressDto LoadMeta() => new();
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => _json is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(_json);
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) { if (FailWrites) return false; Writes++; _json = JsonSerializer.Serialize(value); return true; }
        public void DeleteActiveRun() => _json = null;
    }
}
