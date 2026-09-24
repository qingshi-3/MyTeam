using System;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;
using TowerAutobattler.Project;

// Published HC06 skill and passive, held at controlled distances. Only test
// bodies/basic actions and initial mana are controlled; no private counter writes.
public partial class SurgeConductorContractSmoke : Node
{
    private const string HeroId = "hero_hc06_surge_conductor";
    private const string SkillId = "ability_hc06_a";

    public override async void _Ready()
    {
        var exit = 0;
        try
        {
            var gate = await GamePackagePublisher.CreateReadyAsync(this,
                GD.Load<GameProjectDefinition>("res://content/project/alpha_project.tres"));
            var package = gate.Package ?? throw new InvalidOperationException(string.Join(';', gate.Report.CoreErrors));
            Require(package.Content.TryGet(HeroId, out var entry), "published hero exists");
            var hero = BattleSetupFactory.Snapshot(entry, package.Content);
            VerifyRangedCast(hero);
            VerifyResonanceGate(hero);
            VerifyNoTargetKeepsResources(hero);
            GD.Print("SURGE_CONDUCTOR_CONTRACT_OK ranged-target-center enemy-only resonance-gate no-target-no-spend");
        }
        catch (Exception exception) { exit = 1; GD.PrintErr("SURGE_CONDUCTOR_CONTRACT_FAILED: " + exception); }
        GetTree().Quit(exit);
    }

    private static void VerifyRangedCast(UnitSnapshot hero)
    {
        using var battle = MakeBattle(hero, 5, new Vector2I(7, 2));
        Step(battle, 3);
        var owner = battle.Units.Single(unit => unit.RuntimeId == "caster");
        var main = battle.Units.Single(unit => unit.RuntimeId == "main");
        var splash = battle.Units.Single(unit => unit.RuntimeId == "splash");
        GD.Print($"SURGE_RANGE distance={owner.Position.DistanceTo(main.Position):0.##} reach={owner.AttackRange:0.##}" +
            $" allied-casts={battle.CombatEvents.Count(e => e.Kind == BattleCombatEventKind.ManaSkillResolved && e.SourceRuntimeId.StartsWith("supply-"))}" +
            $" own-casts={Casts(battle)} mana={owner.CurrentMana:0.##}/{owner.MaxMana:0.##} enemy-health={main.Health:0.##}");
        Require(owner.Position.DistanceTo(main.Position) > 4 && BattlefieldSpace.IsWithinReach(owner, main, owner.AttackRange),
            "target is outside old self radius but inside real attack reach");
        Require(Casts(battle) == 1 && owner.CurrentMana < owner.MaxMana,
            "full mana and five ally casts must release at normal ranged distance");
        Require(main.Health < main.MaxHealth && splash.Health < splash.MaxHealth,
            "blast damages selected enemy and nearby enemy");
        Require(battle.Units.Where(unit => unit.Team == 0).All(unit => Math.Abs(unit.Health - unit.MaxHealth) < .001f),
            "target-centered query never damages caster or target's nearby player ally");
        Require(battle.CombatEvents.Where(e => e.Kind == BattleCombatEventKind.DamageResolved && e.Source.StableId == SkillId)
            .Select(e => e.TargetRuntimeId).ToHashSet().SetEquals(["main", "splash"]), "actual damage belongs to HC06 skill");
        owner.CurrentMana = owner.MaxMana;
        Step(battle, 5);
        Require(Casts(battle) == 1 && owner.CurrentMana == owner.MaxMana, "spent resonance cannot fund a second full-mana cast");
    }

    private static void VerifyResonanceGate(UnitSnapshot hero)
    {
        using var battle = MakeBattle(hero, 4, new Vector2I(7, 2));
        Step(battle, 3);
        var owner = battle.Units.Single(unit => unit.RuntimeId == "caster");
        Require(Casts(battle) == 0 && owner.CurrentMana == owner.MaxMana, "four resonance preserves mana without casting");
        var supplier = battle.Units.Single(unit => unit.RuntimeId == "supply-0");
        supplier.CurrentMana = supplier.MaxMana;
        Step(battle, 4);
        Require(Casts(battle) == 1, "fifth actual allied mana cast releases the waiting skill");
    }

    private static void VerifyNoTargetKeepsResources(UnitSnapshot hero)
    {
        using var battle = MakeBattle(hero, 5, new Vector2I(9, 2));
        Step(battle, 3);
        var owner = battle.Units.Single(unit => unit.RuntimeId == "caster");
        Require(Casts(battle) == 0 && owner.CurrentMana == owner.MaxMana, "no enemy in attack reach keeps full mana");
        // Move only the controlled target fixture into reach. No new supply cast:
        // success proves failed targeting did not consume the shared resource.
        battle.Units.Single(unit => unit.RuntimeId == "main").Position = new Vector2(7, 2);
        Step(battle, 3);
        Require(Casts(battle) == 1, "retained resonance is available when a legal target arrives");
    }

    private static BattleSimulation MakeBattle(UnitSnapshot hero, int suppliers, Vector2I target)
    {
        var passiveBodies = new UnitBehaviorSnapshot(Stationary: true, DisableBasicAttacks: true);
        var config = new BattleConfig
        {
            Seed = 6127,
            FloorRule = new ClearFloorRuleRuntime("surge-test", "常规", ""),
            HeroRule = new HeroRuleSnapshot(1, 1, 1, 0, 0, 0, false, "", 1, 1, 0, 0, 0, 0, false, false, 0, 0, ""),
            Spawns = [new(hero with { Behavior = passiveBodies }, 0, new Vector2I(2, 2), "caster")]
        };
        using var definition = new UnitDefinition
        {
            Id = "surge-test-body", DisplayName = "定点测试单位", MaxHealth = 1000, AttackDamage = 0,
            IsHero = true, MaxMana = 1, StartingMana = 1, ManaPerSecond = 0, ManaPerAttack = 0, ManaPerDamageRatio = 0
        };
        var shield = new CompiledAbilityDefinition("surge-test-supply", "测试供能施法", "", AbilityActivationKind.Automatic,
            AbilityTriggerKind.ManaFull, 1, 0, 1, 0, 0,
            [new CompiledEffectAbilityOperation(new CompiledEffectBinding("supply-shield", 0,
                new CompiledEffectTrigger(EffectTriggerKind.Manual, EffectDomainEventKind.None), [], new CompiledOwnerTargetQuery(),
                [new CompiledEffectStep(EffectKind.Shield, EffectAmountSource.Fixed, 1)],
                new CompiledEffectBindingLimits(0, 0, 32, 8), null), AbilityInvocationValueSource.Fixed, 1)], null);
        var supplier = BattleSetupFactory.Snapshot(definition, abilityLoadout: new CompiledAbilityLoadout([shield])) with { Behavior = passiveBodies };
        var dummy = supplier with { AbilityLoadout = null };
        for (var i = 0; i < suppliers; i++) config.Spawns.Add(new(supplier, 0, new Vector2I(0, i), "supply-" + i));
        config.Spawns.Add(new(dummy, 1, target, "main"));
        config.Spawns.Add(new(dummy, 1, new Vector2I(9, 3), "splash"));
        config.Spawns.Add(new(dummy, 0, new Vector2I(7, 3), "nearby-ally"));
        var battle = new BattleSimulation(config);
        var caster = battle.Units.Single(unit => unit.RuntimeId == "caster");
        caster.CurrentMana = caster.MaxMana;
        return battle;
    }

    private static int Casts(BattleSimulation battle) => battle.CombatEvents.Count(e =>
        e.Kind == BattleCombatEventKind.ManaSkillResolved && e.SourceRuntimeId == "caster");
    private static void Step(BattleSimulation battle, int count) { for (var i = 0; i < count; i++) battle.Step(); }
    private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); }
}
