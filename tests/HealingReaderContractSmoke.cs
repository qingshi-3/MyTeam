using System;
using System.Linq;
using Godot;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Project;

public partial class HealingReaderContractSmoke : Node
{
    public override async void _Ready()
    {
        var exit = 0;
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            Require(package.Content.TryGet("hero_hc18_healing_reader", out var entry), "published healer");
            var hero = BattleSetupFactory.Snapshot(entry, package.Content);
            using (var battle = MakeBattle(hero, 5, .1f))
            {
                Step(battle, 40);
                var owner = battle.Units.Single(u => u.RuntimeId == "healer");
                var ally = battle.Units.Single(u => u.RuntimeId == "ally");
                GD.Print($"HEALER_RANGE casts={Casts(battle)} mana={owner.CurrentMana}/{owner.MaxMana} distance={owner.Position.DistanceTo(ally.Position):0.###} x={owner.Position.X:0.###} health={ally.Health}");
                Require(Casts(battle) > 0 && owner.Position.X > 1f, "full-mana healer approaches a wounded ally beyond skill range instead of idling at basic-heal range");
                Require(battle.CombatEvents.Where(e => e.Kind == BattleCombatEventKind.HealingResolved && e.Source.StableId == "ability_hc18_a")
                    .All(e => e.TargetRuntimeId == "ally" && e.EffectiveValue > 0), "active healing targets wounded allies only");
            }
            using (var battle = MakeBattle(hero, 3, 1))
            {
                Step(battle, 10);
                var owner = battle.Units.Single(u => u.RuntimeId == "healer");
                Require(Casts(battle) == 0 && owner.CurrentMana == owner.MaxMana, "healthy team retains mana");
                battle.Units.Single(u => u.RuntimeId == "ally").Health -= 100;
                Step(battle, 2);
                Require(Casts(battle) == 1, "wound inside range immediately releases waiting skill");
            }
            using (var battle = MakeBattle(hero with { Behavior = hero.Behavior with { Stationary = true } }, 5, .1f))
            {
                Step(battle, 20);
                Require(Casts(battle) == 0 && battle.Units.Single(u => u.RuntimeId == "healer").Position.X == 1f,
                    "stationary units cannot move or cast beyond authored range");
            }
            using (var battle = MakeBattle(hero, 5, .1f))
            {
                var owner = battle.Units.Single(u => u.RuntimeId == "healer");
                owner.DisabledTicks = 10;
                Step(battle, 8);
                Require(Casts(battle) == 0 && owner.Position.X == 1f && owner.CurrentMana == owner.MaxMana,
                    "control blocks approach and cast without spending mana");
                Step(battle, 35);
                Require(Casts(battle) > 0, "approach resumes when control ends");
            }
            using (var first = MakeBattle(hero, 5, .1f))
            using (var second = MakeBattle(hero, 5, .1f))
            {
                Step(first, 40); Step(second, 40);
                Require(first.CreateResult().Digest == second.CreateResult().Digest, "approach and cast are deterministic");
            }
            using (var battle = MakeBattle(hero, 3, .1f))
            {
                var owner = battle.Units.Single(u => u.RuntimeId == "healer");
                owner.Health -= 150;
                battle.Step();
                var heals = battle.CombatEvents.Where(e => e.Kind == BattleCombatEventKind.HealingResolved && e.Source.StableId == "ability_hc18_a").ToArray();
                Require(heals.Length == 2 && heals.All(e => Math.Abs(e.EffectiveValue - 70) < .001f) &&
                    heals.Select(e => e.TargetRuntimeId).ToHashSet().SetEquals(["healer", "ally"]), "two wounded recipients including self receive unchanged healing");
            }
            using (var battle = MakeBattle(hero, 5, .1f))
            {
                var owner = battle.Units.Single(u => u.RuntimeId == "healer");
                owner.CurrentMana = 0;
                Step(battle, 4);
                Require(owner.Position.X == 1 && Casts(battle) == 0, "unready skill does not shorten normal healing range");
            }
            using (var battle = MakeBattle(hero, 5, .1f))
            {
                battle.Step();
                var ally = battle.Units.Single(u => u.RuntimeId == "ally");
                ally.Health = ally.MaxHealth;
                battle.Step();
                Require(Casts(battle) == 0 && battle.Units.Single(u => u.RuntimeId == "healer").ActionTargetRuntimeId != "ally",
                    "a recovered target cancels skill pursuit without consuming mana");
            }
            CheckRangeCheckpoint(hero);
            GD.Print("HEALING_READER_CONTRACT_OK approach authored-range healthy-no-spend stationary control deterministic two-target self unready invalidation range-checkpoint");
        }
        catch (Exception error) { exit = 1; GD.PrintErr("HEALING_READER_CONTRACT_FAILED " + error); }
        GetTree().Quit(exit);
    }

    private static void CheckRangeCheckpoint(UnitSnapshot hero)
    {
        using var battle = MakeBattle(hero, 5, .1f);
        var owner = battle.Units.Single(u => u.RuntimeId == "healer");
        var ally = battle.Units.Single(u => u.RuntimeId == "ally");
        using var movement = new DeterministicContinuousMovementService(BattleSimulation.Width, BattleSimulation.Height,
            () => battle.Units, _ => true, (_, _) => true, 1801);
        movement.BeginTick();
        movement.SelectTarget(owner, [ally], 4);
        movement.QueueMove(owner);
        var checkpoint = movement.CaptureState();
        movement.SelectTarget(owner, [ally]);
        movement.ResolveIntents((_, _) => { });
        Require(owner.Position.X == 1, "normal healing already in reach does not inherit skill range");
        movement.RestoreState(checkpoint);
        movement.ResolveIntents((_, _) => { });
        Require(owner.Position.X > 1, "rollback restores skill center range and pending approach");
        movement.ReleaseUnit(owner.RuntimeId);
        Require(!movement.HasPlanningState(owner.RuntimeId) && movement.PlanningStateCount == 0, "owner release clears action range and movement plans");
    }

    public static BattleSimulation MakeBattle(UnitSnapshot hero, int distance, float healthRatio)
    {
        using var definition = new UnitDefinition { Id = "healer-test-body", DisplayName = "测试目标", MaxHealth = 1000, AttackDamage = 0 };
        var dummy = BattleSetupFactory.Snapshot(definition) with { Behavior = new UnitBehaviorSnapshot(Stationary: true, DisableBasicAttacks: true) };
        var config = new BattleConfig
        {
            Seed = 1801,
            FloorRule = new ClearFloorRuleRuntime("healer-test", "常规", ""),
            HeroRule = new HeroRuleSnapshot(1, 1, 1, 0, 0, 0, false, "", 1, 1, 0, 0, 0, 0, false, false, 0, 0, ""),
            Spawns = [new(hero, 0, new Vector2I(1, 2), "healer"),
                new(dummy, 0, new Vector2I(1 + distance, 2), "ally", HealthRatio: healthRatio),
                new(dummy, 1, new Vector2I(9, 4), "enemy")]
        };
        var battle = new BattleSimulation(config);
        var caster = battle.Units.Single(u => u.RuntimeId == "healer");
        caster.CurrentMana = caster.MaxMana;
        return battle;
    }
    private static int Casts(BattleSimulation battle) => battle.CombatEvents.Count(e => e.Kind == BattleCombatEventKind.ManaSkillResolved && e.SourceRuntimeId == "healer");
    private static void Step(BattleSimulation battle, int count) { for (var i = 0; i < count; i++) battle.Step(); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

