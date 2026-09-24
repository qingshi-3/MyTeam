using System;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Domain;
using TowerAutobattler.Project;

// Focused headless simulation checks. No UI, screenshots, campaign or save writes.
public partial class GritActionQueueContractSmoke : Node
{
    public override async void _Ready()
    {
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            Require(package.Content.TryGet("hero_hc38_grit_brawler", out var entry), "published brawler");
            var hero = BattleSetupFactory.Snapshot(entry, package.Content);
            Run(hero);
            Require(package.Content.TryGet("hero_hc26_ash_returner", out var reviver), "published revival fixture");
            CheckRevival(hero, BattleSetupFactory.Snapshot(reviver, package.Content));
            GD.Print("GRIT_ACTION_QUEUE_OK no-mana,full-grit,thresholds,coalescing,ordered-recovery,independent-units,control,expiry,rollback,revival-once,determinism");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr("GRIT_ACTION_QUEUE_FAILED " + error); GetTree().Quit(1); }
    }

    public static void Run(UnitSnapshot hero)
    {
        hero = hero with { Behavior = new(Stationary: true, DisableBasicAttacks: true) };
        var skill = hero.AbilityLoadout!.Abilities.Single(a => a.Operations.Any(o => o is CompiledGritPunchOperation));
        var punch = skill.Operations.OfType<CompiledGritPunchOperation>().Single();
        Require(skill.Trigger == AbilityTriggerKind.ActionQueued && skill.ManaCost == 0 && skill.IsActiveSkill,
            "conditional skill remains active without mana");
        using (var battle = Create(hero))
        {
            var unit = Unit(battle); Step(battle, 12);
            Require(!unit.HasManaSkill && unit.MaxMana == 0 && unit.CurrentMana == 0 && Charges(battle).Length == 0,
                "high health and empty grit do not cast or regenerate mana");
        }
        using (var battle = Create(hero, new Probe(c =>
               {
                   var unit = c.Units.Single(u => u.RuntimeId == "hero");
                   if (c.Tick == 1) c.Damage("front", unit, 400);
                   if (c.Tick == 2) c.Heal(unit, 300);
                   if (c.Tick == 3) c.Damage("front", unit, 100);
               })))
        {
            Step(battle, 5);
            Require(Unit(battle).Health == 800 && Unit(battle).Shield == 500 && Charges(battle).Length == 1,
                "full grit alone fires above half health and consumes the capped ledger");
            Require(!battle.CombatEvents.Any(e => e.Kind == BattleCombatEventKind.ManaSkillResolved),
                "conditional skill never fabricates a mana-spend event");
        }
        using (var battle = Create(hero))
        {
            var unit = Unit(battle);
            unit.Health = 500; Step(battle, 1);
            Require(Charges(battle).Length == 0, "exactly 50 percent is not below half");
            unit.Health = 499; Step(battle, 1);
            var first = Charges(battle).Single();
            unit.Health = 200; Step(battle, 1);
            Require(Charges(battle).Length == 1, "exactly 20 percent does not trigger critical");
            unit.Health = 199;
            var ready = first.Tick + punch.ChargeTicks + punch.RecoveryTicks;
            while (battle.TickIndex < ready - 1) battle.Step();
            Require(Charges(battle).Length == 1 && Releases(battle).Length == 1,
                "critical request waits through windup, release and recovery");
            battle.Step();
            Require(Charges(battle).Length == 2 && Charges(battle)[1].Tick == ready,
                "queued critical starts at the first legal action boundary");
            Step(battle, 32);
            Require(Charges(battle).Length == 2, "remaining below both thresholds does not spam casts");
            unit.Health = 600; Step(battle, 1); unit.Health = 499; Step(battle, 1);
            Require(Charges(battle).Length == 3, "healing above half rearms the half-health crossing");
            unit.Health = 199; Step(battle, 20);
            Require(Charges(battle).Length == 3, "critical trigger stays spent after healing and another drop");
        }
        using (var battle = Create(hero))
        {
            Unit(battle).Health = 199; Step(battle, 20);
            Require(Charges(battle).Length == 1 && Releases(battle).Length == 1,
                "simultaneous half and critical triggers merge into one action");
            Require(Math.Abs(Unit(battle, "front").Health - (10000 - Unit(battle).Damage)) < .01f,
                "zero-grit threshold cast retains base damage");
        }
        using (var battle = Create(hero, new Probe(c =>
               {
                   var unit = c.Units.Single(u => u.RuntimeId == "hero");
                   if (c.Tick == 1) { c.Damage("front", unit, 400); c.Heal(unit, 400); }
                   if (c.Tick == 2) c.Damage("front", unit, 200);
               })))
        {
            Unit(battle).DisabledTicks = 100; Step(battle, 4);
            Require(battle.DescribeBattleResources("hero").Contains("怒劲 500"), "grit cap survives control");
            Step(battle, 61);
            Require(battle.DescribeBattleResources("hero").Contains("怒劲 0"), "grit samples expire independently");
            Unit(battle).DisabledTicks = 0; Step(battle, 5);
            Require(Charges(battle).Length == 0, "expired full-grit-only request is removed before release");
        }
        CheckShieldAndCancellation(hero);
        CheckIndependentQueues(hero, punch);
        var reject = true;
        using (var battle = Create(hero, bindings: bindings => bindings.Subscribe(BattleCombatEventKind.AbilityResolved,
                   CombatSourceRef.System("grit-rollback"), 0, (e, sink) =>
                   {
                       if (reject && e.Source.StableId == skill.StableId)
                           sink.Enqueue(CombatSourceRef.System("grit-rollback"), 0, _ => throw new InvalidOperationException("grit rollback probe"));
                   })))
        {
            Unit(battle).Health = 199; Step(battle, 2);
            Require(Charges(battle).Length == 0, "failed commits roll back cast and queue consumption");
            reject = false; Step(battle, 20);
            Require(Charges(battle).Length == 1, "failed commit does not spend the single critical entitlement");
        }
        using (var a = Create(hero)) using (var b = Create(hero))
        {
            Unit(a).Health = Unit(b).Health = 499; Step(a, 2); Step(b, 2);
            Unit(a).Health = Unit(b).Health = 199; Step(a, 30); Step(b, 30);
            Require(a.CreateResult().Digest == b.CreateResult().Digest, "ordered queue is deterministic");
        }
    }

    private static void CheckShieldAndCancellation(UnitSnapshot hero)
    {
        using (var battle = Create(hero, new Probe(c =>
               { if (c.Tick == 2) c.Damage("front", c.Units.Single(u => u.RuntimeId == "hero"), 200); })))
        {
            var unit = Unit(battle); unit.Shield = 50; Step(battle, 3);
            Require(battle.DescribeBattleResources("hero").Contains("怒劲 150"), "shield absorption is excluded from grit");
            unit.Health = 499; battle.Step();
            var charge = Charges(battle).Single();
            Require(unit.Shield == 150, "stored health loss becomes the actual timed shield");
            unit.Shield += 77; Unit(battle, "front").Position += new Vector2(0, 2);
            Unit(battle, "far").Position = unit.Position + new Vector2(2.8f, 0);
            Step(battle, 7);
            Require(Releases(battle).Single().Position == charge.Position && Unit(battle, "far").Health < 10000 &&
                Unit(battle, "front").Health == 10000, "queued punch retains locked direction");
            Step(battle, 25);
            Require(Math.Abs(unit.Shield - 77) < .01f, "timed shield expires without deleting unrelated shield");
        }
        using (var battle = Create(hero))
        {
            Unit(battle).Health = 499; battle.Step();
            Unit(battle).Health = 199; Unit(battle).DisabledTicks = 8; Step(battle, 7);
            Require(battle.PendingEvents.Any(e => e.Type == "line_cancel") && Releases(battle).Length == 0 &&
                Charges(battle).Length == 1, "control cancels current action and holds the queued critical");
            Step(battle, 20);
            Require(Charges(battle).Length == 2 && Releases(battle).Length == 1,
                "queued critical survives control and does not overlap interrupted action");
        }
    }

    private static void CheckIndependentQueues(UnitSnapshot hero, CompiledGritPunchOperation punch)
    {
        var config = Config(hero);
        config.Spawns.Add(new(hero, 0, new(1, 4), "other"));
        config.Spawns.Add(new(Dummy(), 1, new(3, 4), "other-target"));
        using var battle = new BattleSimulation(config);
        Normalize(Unit(battle)); Normalize(Unit(battle, "other"));
        Unit(battle).Health = Unit(battle, "other").Health = 499; battle.Step();
        Unit(battle).Health = Unit(battle, "other").Health = 199;
        Step(battle, punch.ChargeTicks + punch.RecoveryTicks);
        var actions = Charges(battle);
        Require(actions.Count(e => e.SourceRuntimeId == "hero") == 2 && actions.Count(e => e.SourceRuntimeId == "other") == 2,
            "each instance owns its own queue and critical-use allowance");
        Require(actions.Where(e => e.SourceRuntimeId == "hero").Select(e => e.Tick)
            .SequenceEqual(actions.Where(e => e.SourceRuntimeId == "other").Select(e => e.Tick)),
            "one unit's recovery does not block another unit");
    }

    private static void CheckRevival(UnitSnapshot hero, UnitSnapshot reviver)
    {
        var revive = reviver.AbilityLoadout!.Abilities.First(a => a.Operations.OfType<CompiledLifecycleOperation>()
            .Any(o => o.Kind == LifecycleAbilityKind.ReviveOwner));
        revive = revive with { StableId = "test_grit_revive", MaxUses = 1,
            Operations = [revive.Operations.OfType<CompiledLifecycleOperation>().First() with { DelayTicks = 1, HealthRatio = .4f }] };
        hero = hero with { Behavior = new(Stationary: true, DisableBasicAttacks: true),
            AbilityLoadout = new(hero.AbilityLoadout!.Abilities.Add(revive)) };
        var config = Config(hero, new Probe(c =>
        {
            var unit = c.Units.Single(u => u.RuntimeId == "hero");
            if (c.Tick == 15) c.Damage("front", unit, 10000);
            if (c.Tick == 20) c.Damage("front", unit, 210);
        }));
        config.Spawns.Add(new(Dummy(), 0, new(1, 5), "ally"));
        using var battle = new BattleSimulation(config);
        Normalize(Unit(battle)); Unit(battle).Health = 199; Step(battle, 35);
        Require(battle.CombatEvents.Any(e => e.Kind == BattleCombatEventKind.UnitRevived && e.TargetRuntimeId == "hero"),
            "real revival path executed");
        Require(Unit(battle).Alive && Unit(battle).Health < 200 && Charges(battle).Length == 1,
            "critical entitlement is not restored by death and revival");
    }

    private sealed class Probe(Action<BattleRuleContext> action) : ClearFloorRuleRuntime("grit-probe", "测试", "")
    { public override void OnTick(BattleRuleContext context) => action(context); }
    private static BattleConfig Config(UnitSnapshot hero, IBattleFloorRuleRuntime? floor = null,
        Action<BattleCombatBindingRegistry>? bindings = null) => new()
    {
        Seed = 20260920, FloorRule = floor ?? new ClearFloorRuleRuntime("clear", "常规", ""),
        HeroRule = new(1,1,1,0,0,0,false,"",1,1,0,0,0,0,false,false,0,0,""),
        Spawns = [new(hero,0,new(1,2),"hero"),new(Dummy(),1,new(3,2),"front"),new(Dummy(),1,new(7,2),"far")],
        ConfigureCombatBindings = bindings
    };
    private static BattleSimulation Create(UnitSnapshot hero, IBattleFloorRuleRuntime? floor = null,
        Action<BattleCombatBindingRegistry>? bindings = null)
    { var battle = new BattleSimulation(Config(hero, floor, bindings)); Normalize(Unit(battle)); return battle; }
    private static UnitSnapshot Dummy()
    {
        using var definition = new TowerAutobattler.Content.UnitDefinition { Id = "grit_dummy", DisplayName = "目标", MaxHealth = 10000, AttackDamage = 0, Armor = 0 };
        return BattleSetupFactory.Snapshot(definition) with { Behavior = new(Stationary: true, DisableBasicAttacks: true) };
    }
    private static void Normalize(BattleUnitState unit)
    { unit.MaxHealth = 1000; unit.Health = 1000; unit.Attributes.SetBaseValue(CombatAttribute.Armor, 0); unit.ManaLockedUntilTick = 0; }
    private static BattleUnitState Unit(BattleSimulation battle, string id = "hero") => battle.Units.Single(u => u.RuntimeId == id);
    private static BattleEvent[] Charges(BattleSimulation battle) => battle.PendingEvents.Where(e => e.Type == "line_charge").ToArray();
    private static BattleEvent[] Releases(BattleSimulation battle) => battle.PendingEvents.Where(e => e.Type == "line_release").ToArray();
    private static void Step(BattleSimulation battle, int count) { for (var i = 0; i < count; i++) battle.Step(); }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
