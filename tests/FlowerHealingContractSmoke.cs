using System;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;

// Uses the published U16 resource. Controlled positions and disabled ordinary attacks
// isolate the contract; this does not certify balance, natural supply or visual quality.
public partial class FlowerHealingContractSmoke : Node
{
    private const string Flower = "prototype_u16";
    private const string Skill = "prototype_u16_a";

    public override async void _Ready()
    {
        try
        {
            var content = await SkillSampleContent.LoadAsync(this);
            AuthoredContract(content);
            EmptyTargetPreservesManaAndShield(content);
            RatioOrderAndStableTie(content);
            SelfHealingAndReleaseTime(content);
            LockedTargetAndOverheal(content);
            HardControlCancelsRelease(content);
            FailedCastRestoresManaShieldAndJobs(content);
            ForestCopyHasIndependentHealing(content);
            DeterministicReplay(content);
            GD.Print("FLOWER_HEALING_CONTRACT_OK authored no-target-no-payment ratio stable-tie self-heal three-tick-release locked-target overheal hard-control cast-rollback forest-copy deterministic");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PrintErr("FLOWER_HEALING_CONTRACT_FAILED " + error);
            GetTree().Quit(1);
        }
    }

    private static void AuthoredContract(ContentRegistry content)
    {
        var profile = AbilityPrototypeContent.Snapshot(content, 16);
        var skill = profile.AbilityLoadout!.Find(Skill)!;
        Require(profile.MaxHealth == 650 && profile.Damage == 34 && profile.Armor == 3 && profile.Range == 3,
            "authored support base attributes");
        Require(profile.AttributeDefinition is not null && skill.ManaCost == 60, "authored mana skill");
        Require(skill.Operations is [CompiledMatrixOperation { Kind: MatrixOperationKind.Windup,
            Target: MatrixTargetKind.LowestWoundedHealthAllies, Range: 4, DurationTicks: 3, MaxTargets: 1,
            Effects: [CompiledMatrixOperation { Kind: MatrixOperationKind.Heal, Target: MatrixTargetKind.EventTarget,
                SpellPowerRatio: 1.6f }] }], "single interruptible heal replaces former damage and control");
        using var battle = New(content);
        var owner = Healer(battle);
        Near(owner.MaxMana, 60, "max mana");
        Near(owner.Attributes.GetValue(CombatAttribute.SpellPower), 100, "base spell power");
    }

    private static void EmptyTargetPreservesManaAndShield(ContentRegistry content)
    {
        using var battle = New(content);
        Quiet(battle);
        var owner = Healer(battle);
        owner.CurrentMana = owner.MaxMana;
        // A wounded ally beyond four cells, dead ally and wounded enemy are all illegal.
        var allies = battle.Units.Where(unit => unit.Team == 0 && unit != owner).ToArray();
        allies[0].Health /= 2; allies[0].Position = new(8, 4);
        allies[1].Health = 0;
        battle.Units.Single(unit => unit.Team == 1).Health /= 4;
        Steps(battle, 8);
        Near(owner.CurrentMana, owner.MaxMana, "empty legal selection retains full mana");
        Near(owner.Shield, 0, "failed preparation never starts protector shield");
        Require(!Casts(battle).Any() && !Heals(battle).Any() && owner.DisabledTicks == 0,
            "no legal wounded target means no cast, windup or heal");
        allies[0].Position = owner.Position + new Vector2(4, 0);
        Steps(battle, 1);
        Require(Casts(battle).Count() == 1, "exact four-cell boundary accepts a wounded ally");
    }

    private static void RatioOrderAndStableTie(ContentRegistry content)
    {
        foreach (var tie in new[] { false, true })
        {
            // Reverse spawn enumeration so stable identity, rather than insertion order, wins ties.
            using var battle = New(content, reverseSpawns: tie);
            Quiet(battle);
            var owner = Healer(battle);
            var first = battle.Units.Single(unit => unit.SourceInstanceId == "prototype-0-1");
            var second = battle.Units.Single(unit => unit.SourceInstanceId == "prototype-0-2");
            first.Health = first.MaxHealth * .5f;
            second.Health = second.MaxHealth * (tie ? .5f : .6f);
            Require(tie || second.Health < first.Health, "ratio fixture differs from absolute-health priority");
            owner.CurrentMana = owner.MaxMana;
            Steps(battle, 4);
            var heal = Heals(battle).Single();
            Require(heal.TargetRuntimeId == first.RuntimeId, tie ? "stable identity tie-break" : "lowest ratio beats lowest absolute health");
            Near(heal.RequestedValue, 160, "160 percent spell-power heal");
        }
    }

    private static void SelfHealingAndReleaseTime(ContentRegistry content)
    {
        using var battle = New(content);
        Quiet(battle);
        var owner = Healer(battle);
        owner.Health = 200;
        owner.CurrentMana = owner.MaxMana;
        Steps(battle, 1);
        var cast = Casts(battle).Single();
        Near(owner.Health, 200, "no immediate heal at activation");
        Near(owner.CurrentMana, 0, "successful cast consumes full mana");
        Near(owner.Shield, owner.MaxHealth * .2f, "protector starts when cast commits");
        Steps(battle, 2);
        Require(!Heals(battle).Any(), "no release before three simulation ticks");
        Steps(battle, 1);
        var heal = Heals(battle).Single();
        Require(heal.TargetRuntimeId == owner.RuntimeId && heal.Tick == cast.Tick + 3, "self target releases after exactly 0.3 seconds");
        Near(owner.Health, 360, "self-heal uses spell power");
        Require(battle.PendingEvents.Any(fact => fact.Type == "heal" && fact.SourceRuntimeId == owner.RuntimeId),
            "existing healing presentation fact is emitted");
    }

    private static void LockedTargetAndOverheal(ContentRegistry content)
    {
        using var battle = New(content);
        Quiet(battle);
        var owner = Healer(battle);
        var selected = battle.Units.Single(unit => unit.SourceInstanceId == "prototype-0-1");
        var other = battle.Units.Single(unit => unit.SourceInstanceId == "prototype-0-2");
        selected.Health = selected.MaxHealth * .5f;
        owner.CurrentMana = owner.MaxMana;
        Steps(battle, 1);
        selected.Health = selected.MaxHealth - 10;
        selected.Position = new(8, 4);
        other.Health = 10;
        Steps(battle, 3);
        var heal = Heals(battle).Single();
        Require(heal.TargetRuntimeId == selected.RuntimeId, "windup retains the selected stable target after leaving selection range");
        Near(heal.RequestedValue, 160, "heal amount remains authored");
        Near(heal.EffectiveValue, 10, "resolver clamps useful healing and retains overheal");
        Near(other.Health, 10, "late injury does not silently retarget a started cast");
    }

    private static void HardControlCancelsRelease(ContentRegistry content)
    {
        var control = new MatrixAbilityOperationSpec { Kind = MatrixOperationKind.Control,
            Target = MatrixTargetKind.NearestEnemies, Range = 30, MaxTargets = 64, DurationTicks = 5 };
        var report = new ValidationReport();
        var operation = MatrixAbilityCompiler.Compile(control, "flower-control-fixture", report)
            ?? throw new InvalidOperationException("fixture control compile");
        Require(!report.HasCoreErrors, "fixture control validation");
        var ability = new CompiledAbilityDefinition("flower_contract_control", "测试眩晕", "", AbilityActivationKind.Automatic,
            AbilityTriggerKind.ManaFull, 1, 0, 0, 0, 0, [operation], null);
        using var battle = New(content, enemyAbility: ability);
        Quiet(battle);
        var owner = Healer(battle);
        owner.Health = 200; owner.CurrentMana = owner.MaxMana;
        Steps(battle, 1);
        battle.Units.Single(unit => unit.Team == 1).CurrentMana = battle.Units.Single(unit => unit.Team == 1).MaxMana;
        Steps(battle, 1);
        Require(battle.CombatEvents.Any(fact => fact.Kind == BattleCombatEventKind.ControlApplied && fact.TargetRuntimeId == owner.RuntimeId),
            "real status control reaches flower during windup");
        Steps(battle, 7);
        Require(!Heals(battle).Any(), "hard control removes interruptible healing job");
        Near(owner.Health, 200, "cancelled cast does not heal");
        Near(owner.CurrentMana, 0, "interrupted committed cast retains paid mana");
    }

    private static void FailedCastRestoresManaShieldAndJobs(ContentRegistry content)
    {
        var reject = true;
        var injected = 0;
        using var battle = New(content, bindings: bindings => bindings.Subscribe(BattleCombatEventKind.AbilityResolved,
            CombatSourceRef.System("flower-rollback"), 0, (fact, sink) =>
            {
                if (!reject || fact.SubjectStableId != Skill) return;
                injected++;
                sink.Enqueue(CombatSourceRef.System("flower-rollback"), 0,
                    _ => throw new InvalidOperationException("intentional healing cast rollback"));
            }));
        Quiet(battle);
        var owner = Healer(battle);
        owner.Health = 200; owner.CurrentMana = owner.MaxMana;
        Steps(battle, 1);
        Require(injected == 1 && !Casts(battle).Any(), "failed committed invocation leaves no cast fact");
        Near(owner.CurrentMana, owner.MaxMana, "rollback restores mana");
        Near(owner.Shield, 0, "rollback restores protector grant");
        Require(owner.DisabledTicks == 0 && owner.ManaLockedUntilTick == 0, "rollback restores windup action locks");
        owner.Health = owner.MaxHealth;
        Steps(battle, 6);
        Require(!Heals(battle).Any(), "rolled-back windup cannot release later");
        reject = false; owner.Health = 200;
        Steps(battle, 4);
        Require(Heals(battle).Count() == 1, "retry releases exactly one restored clean cast");
    }

    private static void ForestCopyHasIndependentHealing(ContentRegistry content)
    {
        for (ulong seed = 1; seed <= 64; seed++)
        {
            using var battle = New(content, forest: true, seed: seed);
            var clone = battle.Units.Single(unit => unit.Team == 0 && unit.IsTemporary);
            if (clone.Definition.ContentId != Flower) continue;
            Quiet(battle);
            var owner = Healer(battle);
            clone.Position = new(2, 2); owner.Position = new(2, 3);
            clone.Health = 100; owner.CurrentMana = 0; clone.CurrentMana = clone.MaxMana;
            Require(clone.Definition.AbilityLoadout!.Find(Skill) is not null && clone.HasManaSkill,
                "forest copy retains the same compiled healing operation");
            Steps(battle, 4);
            Require(Heals(battle).Single().SourceRuntimeId == clone.RuntimeId && Casts(battle).Single().SourceRuntimeId == clone.RuntimeId,
                "temporary copy can independently cast its own healing skill");
            Near(owner.CurrentMana, 0, "copy cast does not spend original mana");
            owner.Health = 100; owner.CurrentMana = owner.MaxMana;
            Steps(battle, 4);
            Require(Heals(battle).Count(fact => fact.SourceRuntimeId == owner.RuntimeId) == 1,
                "original cast has independent use and cooldown state");
            return;
        }
        throw new InvalidOperationException("bounded forest seeds did not select U16");
    }

    private static void DeterministicReplay(ContentRegistry content)
    {
        foreach (var seed in new ulong[] { 20261006, 1776 })
        {
            using var a = New(content, seed: seed);
            using var b = New(content, seed: seed);
            foreach (var battle in new[] { a, b })
            {
                Quiet(battle);
                var owner = Healer(battle);
                owner.Health = 100; owner.CurrentMana = owner.MaxMana;
            }
            Steps(a, 20); Steps(b, 20);
            Require(a.CreateResult().Digest == b.CreateResult().Digest && Heals(a).Select(fact => (fact.Tick, fact.TargetRuntimeId, fact.EffectiveValue))
                .SequenceEqual(Heals(b).Select(fact => (fact.Tick, fact.TargetRuntimeId, fact.EffectiveValue))),
                "same seed reproduces heal timeline and full battle digest");
        }
    }

    private static BattleSimulation New(ContentRegistry content, bool reverseSpawns = false, bool forest = false,
        ulong seed = 20261006, CompiledAbilityDefinition? enemyAbility = null, Action<BattleCombatBindingRegistry>? bindings = null)
    {
        var source = AbilityPrototypeContent.CreateBattle(content, forest ? [16, 11, 17, 0, 0, 0] : [16, 18, 3, 0, 0, 0], [4, 0, 0, 0, 0, 0], seed);
        var spawns = source.Spawns.Select(spawn => spawn with { Unit = spawn.Unit with
        {
            Behavior = spawn.Unit.Behavior with { Stationary = true, DisableBasicAttacks = true },
            AbilityLoadout = spawn.Team == 1 && enemyAbility is not null ? new CompiledAbilityLoadout([enemyAbility]) : spawn.Unit.AbilityLoadout
        }}).ToList();
        if (reverseSpawns) spawns.Reverse();
        return new BattleSimulation(new BattleConfig { Seed = source.Seed, HeroRule = source.HeroRule,
            FloorRule = source.FloorRule, Spawns = spawns, Traits = source.Traits,
            TacticalSummons = source.TacticalSummons, ConfigureCombatBindings = bindings });
    }
    private static BattleUnitState Healer(BattleSimulation battle) => battle.Units.Single(unit => unit.Definition.ContentId == Flower && !unit.IsTemporary);
    private static System.Collections.Generic.IEnumerable<BattleCombatEvent> Casts(BattleSimulation battle) =>
        battle.CombatEvents.Where(fact => fact.Kind == BattleCombatEventKind.AbilityResolved && fact.SubjectStableId == Skill);
    private static System.Collections.Generic.IEnumerable<BattleCombatEvent> Heals(BattleSimulation battle) =>
        battle.CombatEvents.Where(fact => fact.Kind == BattleCombatEventKind.HealingResolved && fact.Source.StableId == Skill);
    private static void Quiet(BattleSimulation battle)
    {
        foreach (var unit in battle.Units)
        {
            unit.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond, 0);
            unit.Attributes.SetBaseValue(CombatAttribute.ManaPerAttack, 0);
            unit.Attributes.SetBaseValue(CombatAttribute.ManaPerDamageRatio, 0);
            unit.CurrentMana = 0; unit.AttackCooldown = 10000;
        }
    }
    private static void Steps(BattleSimulation battle, int count) { for (var i = 0; i < count; i++) battle.Step(); }
    private static void Near(float actual, float expected, string message) => Require(Math.Abs(actual - expected) < .01f, message + $" actual={actual} expected={expected}");
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
