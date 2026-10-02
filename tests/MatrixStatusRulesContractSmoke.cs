using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;
using TowerAutobattler.Run;

public partial class MatrixStatusRulesContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var project = GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres");
            var publication = await GamePackagePublisher.CreateReadyAsync(this, project);
            var package = publication.Package ?? throw new InvalidOperationException(string.Join(';', publication.Report.CoreErrors));
            Require(package.Project.MatrixStatusRules == CompiledMatrixStatusRules.Default, "authored defaults changed combat");
            using var custom = new MatrixStatusRulesDefinition { PoisonIntervalTicks = 2, PoisonDurationTicks = 8,
                PoisonDamagePerLayer = 7, ChillDurationTicks = 6, ChillDefaultStackLimit = 3,
                ChillSlowPerLayer = .1f, EmberDurationTicks = 9, EmberStackLimit = 4 };
            var report = new ValidationReport();
            var compiled = CompiledMatrixStatusRules.Compile(custom, report);
            Require(!report.HasCoreErrors, "valid rules rejected");
            custom.PoisonDamagePerLayer = 999;
            Require(compiled.PoisonDamagePerLayer == 7, "compiled rules retained mutable authoring alias");
            VerifyCombat(compiled);
            using var invalid = (GameProjectDefinition)project.Duplicate();
            invalid.MatrixStatusRules = custom;
            custom.PoisonIntervalTicks = 0;
            // Re-publication must retain standalone authored wrappers from the live
            // package even when the managed collector runs between attempts.
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            var rejected = await GamePackagePublisher.CreateReadyAsync(this, invalid);
            Require(rejected.Package is null && rejected.PublishedVersion == 0 && rejected.Report.CoreErrors.Any(e => e.Contains("interval")),
                "invalid rules published a package");
            custom.PoisonIntervalTicks = 2;
            custom.ChillSlowPerLayer = .5f;
            report = new ValidationReport();
            _ = CompiledMatrixStatusRules.Compile(custom, report);
            Require(report.HasCoreErrors, "nonpositive aggregate attack/movement speed accepted");
            custom.ChillSlowPerLayer = .1f;
            var republished = await GamePackagePublisher.CreateReadyAsync(this, invalid);
            Require(republished.Package is not null && republished.PublishedVersion > 0,
                "valid retry failed after invalid package rejection");

            var configured = package.Project with { MatrixStatusRules = compiled };
            var app = new RunApplication(package.Content, new MemorySave(), configured);
            Require(app.BeginOpeningRecruitment(37), "begin fixture");
            var choices = app.Run!.OpeningRecruitment!.CandidateIds;
            Require(app.ToggleOpeningHero(choices[0]) && app.ToggleOpeningHero(choices[1]) && app.ConfirmOpeningRecruitment(), "opening fixture");
            var config = new RunBattlePreparationService(package.Content, configured, new RunRelicService(package.Content))
                .Build(app.ActiveRun!, app.CurrentEncounter(), false);
            Require(ReferenceEquals(config.MatrixStatusRules, compiled), "Run preparation dropped compiled rules");
            GD.Print("MATRIX_STATUS_RULES_CONTRACT_OK authored-defaults immutable-compile invalid-publication retained-resource-reload slow-validation run-binding actual-poison-chill-ember");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PrintErr("MATRIX_STATUS_RULES_CONTRACT_FAILED " + e); GetTree().Quit(1); }
    }

    private static void VerifyCombat(CompiledMatrixStatusRules rules)
    {
        using var poison = new MatrixAbilityOperationSpec { Kind = MatrixOperationKind.Poison, Target = MatrixTargetKind.NearestEnemies, Amount = 1 };
        using var chill = new MatrixAbilityOperationSpec { Kind = MatrixOperationKind.Chill, Target = MatrixTargetKind.NearestEnemies, Amount = 10 };
        using var ember = new MatrixAbilityOperationSpec { Kind = MatrixOperationKind.Ember, Target = MatrixTargetKind.NearestEnemies, Amount = 10 };
        using var ability = new AbilityDefinition { StableId = "status_rules_probe", DisplayName = "规则检查",
            ActivationKind = AbilityActivationKind.Automatic, Trigger = AbilityTriggerKind.BattleStarted, MaxUses = 1,
            Operations = [poison, chill, ember] };
        using var authoredLoadout = new AbilityLoadoutDefinition { Abilities = [ability] };
        var compilation = AbilityDefinitionCompiler.CompileLoadout(authoredLoadout);
        var loadout = compilation.Loadout ?? throw new InvalidOperationException(string.Join(';', compilation.Report.CoreErrors));
        UnitSnapshot Unit(string id, bool hero) => new(id, id, UnitRole.Fighter, hero, false, 1000, 0, 10, 10, 10,
            0, 0, 0, 0, [], new UnitBehaviorSnapshot(Stationary: true, DisableBasicAttacks: true));
        using var battle = new BattleSimulation(new BattleConfig { MatrixStatusRules = rules, Seed = 1,
            FloorRule = new ClearFloorRuleRuntime("clear", "常规", ""), HeroRule = HeroRuleSnapshot.Neutral,
            Spawns = [new(Unit("source", true) with { AbilityLoadout = loadout }, 0, new(1, 2), "source"),
                new(Unit("target", false), 1, new(4, 2), "target")] });
        var target = battle.UnitViews.Single(u => u.RuntimeId == "target");
        Require(target.Statuses.Single(s => s.StableId == "matrix_poison").RemainingTicks == 8, "poison duration");
        Require(target.Statuses.Single(s => s.StableId == "matrix_chill") is { Stacks: 3, RemainingTicks: 6 }, "chill duration/cap");
        Require(target.Statuses.Single(s => s.StableId == "matrix_ember") is { Stacks: 4, RemainingTicks: 9 }, "ember duration/cap");
        Require(Math.Abs(target.Attributes.GetValue(CombatAttribute.AttackSpeed) - .7f) < .001f, "chill linear slow");
        battle.Step();
        Require(target.Health == 1000, "poison tick arrived early");
        battle.Step();
        Require(target.Health == 993, "poison damage/interval");
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class MemorySave : IRunSaveService
    {
        public MetaProgressDto LoadMeta() => new();
        public SettingsDto LoadSettings() => new();
        public ActiveRunDto? LoadActiveRun() => null;
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value) => true;
        public void DeleteActiveRun() { }
    }
}
