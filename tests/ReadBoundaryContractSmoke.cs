using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Growth;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

public partial class ReadBoundaryContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            VerifyPublicPorts();
            VerifyNestedViews();
            VerifyBattle();
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            VerifyRun(package);
            GD.Print("READ_BOUNDARY_CONTRACT_OK public-ports nested-collections live-identity save-failure replacement battle-step event-alias source-boundaries");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PrintErr("READ_BOUNDARY_CONTRACT_FAILED " + e); GetTree().Quit(1); }
    }

    private static void VerifyPublicPorts()
    {
        foreach (var type in new[] { typeof(RunView), typeof(RosterHeroView), typeof(RunEquipmentView),
            typeof(RunItemView), typeof(HeroGrowthView), typeof(GrowthRunView), typeof(BattleUnitView),
            typeof(BattleAttributeView) })
        {
            Require(type.GetFields(BindingFlags.Public | BindingFlags.Instance).Length == 0, type.Name + " fields");
            Require(type.GetProperties().All(p => p.SetMethod is null || !p.SetMethod.IsPublic), type.Name + " writable property");
            Require(type.GetMethods().All(m => m.ReturnType != typeof(ActiveRunDto) &&
                m.ReturnType != typeof(BattleUnitState) && m.ReturnType != typeof(TowerAutobattler.Attributes.BattleAttributeSet)),
                type.Name + " mutable return");
        }
        Require(typeof(RunApplication).GetProperty("ActiveRun") is null &&
            typeof(BattleSimulation).GetProperty("Units") is null &&
            typeof(RunBattleResolution).GetProperty("ActiveRun") is null, "authority escaped a public facade");
        foreach (var directory in new[] { "src/UI", "src/Presentation", "src/App" })
        foreach (var file in Directory.EnumerateFiles(ProjectSettings.GlobalizePath("res://" + directory), "*.cs"))
        {
            var text = File.ReadAllText(file);
            Require(!System.Text.RegularExpressions.Regex.IsMatch(text,
                @"\b(ActiveRunDto|RosterHeroInstanceDto|BattleUnitState)\b|\.ActiveRun\b"),
                "presentation bypassed read port: " + file);
        }
    }

    private static void VerifyNestedViews()
    {
        var dto = new ActiveRunDto { Growth = new GrowthRunDto(), Roster = [new() { InstanceId = "hero" }] };
        dto.Growth.SpellInventory["spell"] = 2;
        var view = RunView.For(dto);
        Require((object)view.Roster is not IList && (object)view.Deployment is not IList, "list cast leaks owner");
        var inventory = view.Growth!.SpellInventory;
        Require(inventory is not Dictionary<string, int>, "dictionary cast leaks owner");
        try { ((IDictionary<string,int>)inventory).Add("intruder", 1); throw new Exception("dictionary accepted write"); }
        catch (NotSupportedException) { }
        var roster = view.Roster;
        var hero = roster[0];
        dto.Roster[0].Rank = 2;
        Require(hero.Rank == 2 && ReferenceEquals(hero, roster[0]), "hero live identity");
        dto.Roster = [new() { InstanceId = "replacement" }];
        Require(roster[0].InstanceId == "replacement", "collection must follow committed list replacement");
        dto.Growth.SpellInventory = new() { ["new"] = 3 };
        Require(view.Growth.SpellInventory["new"] == 3, "dictionary replacement visible on next read");
    }

    private static void VerifyRun(CompiledGamePackage package)
    {
        var store = new MemorySave();
        var app = new RunApplication(package.Content, store, package.Project);
        Require(app.BeginOpeningRecruitment(51), "begin opening");
        var view = app.Run!;
        var id = view.OpeningRecruitment!.CandidateIds[0];
        store.Fail = true;
        Require(!app.ToggleOpeningHero(id) && ReferenceEquals(view, app.Run) &&
            view.OpeningRecruitment.SelectedIds.IsEmpty, "failed save leaked tentative selection");
        store.Fail = false;
        Require(app.ToggleOpeningHero(id) && ReferenceEquals(view, app.Run) &&
            view.OpeningRecruitment.SelectedIds.SequenceEqual([id]), "committed selection not visible");
        var second = view.OpeningRecruitment.CandidateIds[1];
        Require(app.ToggleOpeningHero(second) && app.ConfirmOpeningRecruitment(), "confirm opening");
        Require(ReferenceEquals(view, app.Run) && view.Roster.Count == 2 && view.OpeningRecruitment is null,
            "working-copy publication broke live root");
        var hero = view.Roster[0];
        var formation = view.Deployment.ToArray();
        store.Fail = true;
        Require(!app.WithdrawDeploymentUnit(hero.InstanceId) && formation.SequenceEqual(view.Deployment),
            "failed formation save leaked tentative placement");
        store.Fail = false;
        Require(app.WithdrawDeploymentUnit(hero.InstanceId) && !view.Deployment.Contains(hero.InstanceId),
            "successful command not visible");
        Require(app.BeginOpeningRecruitment(52) && !ReferenceEquals(view, app.Run) && view.Roster.Count == 2,
            "new run must have distinct identity without changing old read port");
    }

    private static void VerifyBattle()
    {
        UnitSnapshot Unit(string id, bool hero) => new(id, id, UnitRole.Ranged, hero, false,
            100, 20, 6, 100, 100, 0, 0, 0, 0, [], new UnitBehaviorSnapshot());
        using var battle = new BattleSimulation(new BattleConfig
        {
            Seed = 17, FloorRule = new ClearFloorRuleRuntime("clear", "常规", ""),
            HeroRule = HeroRuleSnapshot.Neutral,
            Spawns = [new(Unit("hero", true), 0, new(1, 2), "hero"), new(Unit("enemy", false), 1, new(4, 2), "enemy")]
        });
        var target = battle.UnitViews[1];
        Require((object)battle.UnitViews is not IList && battle.PendingEvents is not List<BattleEvent>, "battle collection alias");
        foreach (var state in battle.Units) state.MoveCooldown = 1000;
        battle.Units[1].AttackCooldown = 1000;
        battle.Step();
        Require(ReferenceEquals(target, battle.UnitViews[1]) && target.Health < 100 &&
            target.Health == battle.Units[1].Health, "live battle step projection");
        var events = battle.PendingEvents;
        battle.DrainEvents();
        Require(events.Count == 0, "event view must follow draining");
    }

    private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
    private sealed class MemorySave : IRunSaveService
    {
        private string? _json;
        public bool Fail;
        public MetaProgressDto LoadMeta() => new();
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => _json is null ? null : JsonSerializer.Deserialize<ActiveRunDto>(_json);
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) { if (Fail) return false; _json = JsonSerializer.Serialize(value); return true; }
        public void DeleteActiveRun() => _json = null;
    }
}
