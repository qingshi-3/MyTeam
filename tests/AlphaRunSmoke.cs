using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

public partial class AlphaRunSmoke : Node
{
    private ContentRegistry _content = null!;
    private CompiledGameProject _project = null!;

    public override async void _Ready()
    {
        var code = await RunAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GetTree().Quit(code);
    }

    private async System.Threading.Tasks.Task<int> RunAsync()
    {
        try
        {
            var catalog = GD.Load<ContentCatalog>("res://content/catalogs/alpha_catalog.tres");
            var gate = await TestProjectFixture.PublishAsync(this);
            var package = gate.Package ?? throw new InvalidOperationException(string.Join("; ", gate.Report.CoreErrors));
            _content = package.Content;
            _project = package.Project;
            RunPath("commander", "hero_banner_marshal", 1101, 6,
                ["item_commander_map", "item_aegis_standard", "item_last_banner", "item_field_rations"]);
            RunPath("carry", "hero_crimson_count", 2202, 3,
                ["item_duelist_seal", "item_crimson_mail", "item_blood_chalice", "item_lone_crown", "item_last_banner"]);
            RunPath("solo", "hero_edge_ascetic", 3303, 0,
                ["item_duelist_seal", "item_duelist_seal", "item_crimson_mail", "item_blood_chalice", "item_lone_crown", "item_lone_crown"]);
            GD.Print("ALPHA_RUN_OK paths=commander,carry,solo regions=3 floors=15");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("ALPHA_RUN_FAILED: " + exception);
            return 1;
        }
    }

    private void RunPath(
        string name,
        string heroId,
        ulong seed,
        int legacySoldierDeploymentCount,
        IReadOnlyList<string> startingItems)
    {
        var app = new RunApplication(_content, new SaveService($"tests/alpha-{name}"), _project);
        if (!app.Meta.UnlockedHeroIds.Contains(heroId)) app.Meta.UnlockedHeroIds.Add(heroId);
        if (!app.StartNewRun(heroId, seed)) throw new InvalidOperationException(name + " start");
        foreach (var soldier in _content.Catalog.Soldiers.Select(entry => entry.StableId))
        {
            if (app.ActiveRun!.Roster.Count >= 9) break;
            app.Recruit(soldier);
        }
        foreach (var item in startingItems) app.GrantItem(item);
        foreach (var item in app.ActiveRun!.Items) item.Stacks = 4;

        var visited = 0;
        while (app.ActiveRun is not null && visited++ < 24)
        {
            // The legacy parameter counted soldiers because the starting hero was
            // deployed separately. Unified roster deployment must preserve that total.
            AutoDeploy(app, legacySoldierDeploymentCount + 1);
            var options = app.CurrentOptions();
            var option = options.FirstOrDefault(value => value.Type == TowerNodeType.Boss)
                ?? (app.ActiveRun.Roster.Average(hero => hero.HealthRatio) < .82f
                    ? options.FirstOrDefault(value => value.Type == TowerNodeType.Rest)
                    : null)
                ?? options.FirstOrDefault(value => value.Type is TowerNodeType.Recruitment or TowerNodeType.Event or TowerNodeType.Shop)
                ?? options.FirstOrDefault(value => value.Type == TowerNodeType.Combat)
                ?? options.FirstOrDefault(value => value.Type == TowerNodeType.Elite)
                ?? options[0];
            if (!app.SelectNode(option.Type)) throw new InvalidOperationException($"{name} select floor {app.ActiveRun.FloorIndex}");
            if (option.Type is TowerNodeType.Combat or TowerNodeType.Elite or TowerNodeType.Boss)
            {
                var encounter = app.CurrentEncounter();
                using var simulation = new BattleSimulation(app.BuildBattleConfig(encounter));
                while (simulation.Outcome == BattleOutcome.Running)
                {
                    if (simulation.TickIndex is 1 or 250 or 500) simulation.TryUseTacticalCommand(0);
                    simulation.Step();
                }
                var result = simulation.CreateResult();
                if (result.Outcome != BattleOutcome.PlayerVictory)
                {
                    var summary = string.Join(", ", result.Units.Select(unit => $"{unit.RuntimeId}:{unit.FinalHealth:0}/{unit.MaxHealth:0} shield={unit.FinalShield:0} dmg={unit.FinalDamage:0} cell={unit.FinalCell}"));
                    throw new InvalidOperationException($"{name} lost floor {app.ActiveRun.FloorIndex + 1}: {result.Outcome} ticks={result.Ticks} ratio={app.ActiveRun.Roster.Average(hero => hero.HealthRatio):0.00}; {summary}");
                }
                if (!app.CompleteBattle(result, encounter)) throw new InvalidOperationException(name + " completion");
                if (app.ActiveRun is not null) app.GrantItem(app.ItemChoices(visited).First().StableId);
            }
            else
            {
                switch (option.Type)
                {
                    case TowerNodeType.Recruitment:
                        if (app.ActiveRun.Roster.Count < 9) app.Recruit(app.RecruitmentChoices(visited).First().StableId);
                        break;
                    case TowerNodeType.Event: app.ResolveEvent(false); break;
                    case TowerNodeType.Rest: app.Rest(false); break;
                }
                app.FinishNonCombatNode();
            }
        }
        if (app.ActiveRun is not null || app.Meta.Victories <= 0) throw new InvalidOperationException(name + " did not finish tower");
    }

    private static void AutoDeploy(RunApplication app, int count)
    {
        for (var slot = 0; slot < app.Rules.PhysicalDeploymentCeiling; slot++) app.ClearDeploymentSlot(slot);
        if (app.ActiveRun is null) return;
        var units = app.ActiveRun.Roster.OrderByDescending(unit => unit.HealthRatio).Take(count).ToArray();
        for (var slot = 0; slot < units.Length; slot++) app.EquipDeployment(units[slot].InstanceId, slot);
    }
}
