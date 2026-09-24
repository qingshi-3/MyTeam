using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;

// Focused rules only; intentionally not a full-run or hands-on experience acceptance.
public partial class HeroManaContractSmoke : Node
{
    public override void _Ready()
    {
        try
        {
            IndependentRecoveryAndCaps();
            AutomaticSpendAndControl();
            NoTargetAndFailedCommitKeepMana();
            HealingSelectsWoundedAlly();
            LethalEffectsDoNotRestoreDefeatedTargets();
            TemporaryCopiesHaveNoHeroMana();
            GD.Print("HERO_MANA_CONTRACT_OK isolation recovery cap control target atomicity healing");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PrintErr("HERO_MANA_CONTRACT_FAILED: " + exception);
            GetTree().Quit(1);
        }
    }

    private static void IndependentRecoveryAndCaps()
    {
        using var battle = Battle(Unit(Skill(EffectKind.Shield, true), 0), Unit(null, 0));
        var caster = battle.Units.First(unit => unit.Team == 0);
        var other = battle.Units.First(unit => unit.Team == 1);
        BattleHeroMana.Advance(caster, 1);
        Near(caster.CurrentMana, .5f, "passive recovery uses fixed simulation time");
        BattleHeroMana.OnAttack(caster, 1);
        Near(caster.CurrentMana, 10.5f, "one basic attack recovery");
        BattleHeroMana.OnDamage(caster, caster.MaxHealth * .1f, 1);
        Near(caster.CurrentMana, 15.5f, "damage uses effective max-health fraction");
        BattleHeroMana.OnDamage(caster, caster.MaxHealth * 10, 1);
        Near(caster.CurrentMana, 35.5f, "one damage event cap");
        BattleHeroMana.OnDamage(caster, 0, 1);
        Near(caster.CurrentMana, 35.5f, "zero damage gives no mana");
        Near(other.CurrentMana, 0, "another owner is unaffected");
        for (var count = 0; count < 20; count++) BattleHeroMana.OnAttack(caster, 1);
        Near(caster.CurrentMana, caster.MaxMana, "full mana clamps");
        BattleHeroMana.Spend(caster, 1, 10);
        BattleHeroMana.OnDamage(caster, caster.MaxHealth, 2);
        BattleHeroMana.OnAttack(caster, 2);
        BattleHeroMana.Advance(caster, 2);
        Near(caster.CurrentMana, 0, "all recovery is locked during cast recovery");
        caster.Health = 0;
        BattleHeroMana.OnAttack(caster, 20);
        Near(caster.CurrentMana, 0, "defeated owners never regenerate");
    }

    private static void AutomaticSpendAndControl()
    {
        using var battle = Battle(Unit(Skill(EffectKind.Shield, true), 100), Unit(null, 0));
        var caster = battle.Units.First(unit => unit.Team == 0);
        caster.DisabledTicks = 1;
        battle.Step();
        Near(caster.CurrentMana, 100, "control preserves full mana");
        Require(!battle.PendingEvents.Any(item => item.Type == "ability"), "control prevents cast");
        battle.Step();
        Near(caster.CurrentMana, 0, "successful cast consumes owner mana");
        Require(caster.LastActionKind == BattleActionKind.Ability && caster.Shield > 0,
            "successful cast has authoritative action and effect");
        Require(battle.PendingEvents.Count(item => item.Type == "ability") == 1, "one cast, one presentation fact");
    }

    private static void NoTargetAndFailedCommitKeepMana()
    {
        using (var noTarget = Battle(Unit(Skill(EffectKind.Damage, false), 100, range: .1f), Unit(null, 0)))
        {
            noTarget.Step();
            Near(noTarget.Units.First(unit => unit.Team == 0).CurrentMana, 100, "out-of-range target costs nothing");
            Require(!noTarget.PendingEvents.Any(item => item.Type == "ability"), "no fabricated cast");
        }
        using var failed = Battle(Unit(Skill(EffectKind.Shield, true), 100), Unit(null, 0), bindings =>
            bindings.Subscribe(BattleCombatEventKind.AbilityResolved, CombatSourceRef.System("failure_probe"), 0,
                (_, sink) => sink.Enqueue(CombatSourceRef.System("failure_probe"), 0,
                    _ => throw new InvalidOperationException("injected reaction failure"))));
        failed.Step();
        var caster = failed.Units.First(unit => unit.Team == 0);
        Near(caster.CurrentMana, 100, "failed commit restores mana");
        Near(caster.Shield, 0, "failed commit restores effect");
        Require(caster.ManaLockedUntilTick == 0 && caster.LastAbilityName == string.Empty,
            "failed commit restores recovery and presentation state");
        Require(!failed.PendingEvents.Any(item => item.Type == "ability"), "failed cast emits no presentation fact");
    }

    private static void HealingSelectsWoundedAlly()
    {
        var healing = Skill(EffectKind.Heal, false) with { AutomaticTarget = AbilityAutomaticTargetKind.WoundedAlly };
        using var battle = Battle(Unit(healing, 100), Unit(null, 0));
        var caster = battle.Units.First(unit => unit.Team == 0);
        caster.Health = caster.MaxHealth - 100;
        var before = caster.Health;
        battle.Step();
        Require(battle.PendingEvents.Any(item => item.Type == "ability" && item.TargetRuntimeId == caster.RuntimeId),
            "wounded-ally target includes self");
        Require(caster.Health > before, "healing applies to selected wounded ally");
    }

    private static void LethalEffectsDoNotRestoreDefeatedTargets()
    {
        var damage = Skill(EffectKind.Damage, false);
        var statusResource = new TowerAutobattler.Statuses.StatusDefinition
        {
            StableId = "mana_contract_control", DisplayName = "契约冻结",
            Behavior = TowerAutobattler.Statuses.StatusBehaviorKind.DisableActions,
            GrantedTags = [TowerAutobattler.Statuses.StatusDefinitionCompiler.ActionDisabledTag],
            DurationKind = TowerAutobattler.Statuses.StatusDurationKind.TimedTicks, DurationTicks = 6
        };
        var compiled = TowerAutobattler.Statuses.StatusDefinitionCompiler.Compile(statusResource);
        var status = compiled.Definition
            ?? throw new InvalidOperationException("control fixture did not compile: " + string.Join(';', compiled.Report.CoreErrors));
        statusResource.Dispose();
        var mixed = damage with { Operations = damage.Operations.Add(
            new CompiledApplyStatusAbilityOperation(status, new CompiledExplicitTargetQuery())) };
        using var battle = Battle(Unit(mixed, 100), Unit(null, 0));
        var enemy = battle.Units.First(unit => unit.Team == 1);
        enemy.Health = 1;
        battle.Step();
        Require(!enemy.Alive && enemy.Statuses.IsDefaultOrEmpty, "lethal skill cannot attach control to defeated target");
        Require(battle.PendingEvents.Any(item => item.Type == "ability"), "lethal cast remains a successful cast");

        // The same lethal chain followed by a real reaction failure must restore the kill,
        // mana and all queued facts, not merely the health display.
        using var rollback = Battle(Unit(mixed, 100), Unit(null, 0), bindings =>
            bindings.Subscribe(BattleCombatEventKind.AbilityResolved, CombatSourceRef.System("lethal_failure"), 0,
                (_, sink) => sink.Enqueue(CombatSourceRef.System("lethal_failure"), 0,
                    _ => throw new InvalidOperationException("lethal transaction failure"))));
        var restoredEnemy = rollback.Units.First(unit => unit.Team == 1);
        var caster = rollback.Units.First(unit => unit.Team == 0);
        restoredEnemy.Health = 1;
        caster.Attributes.SetBaseValue(CombatAttribute.AttackDamage, 0);
        rollback.Step();
        Require(restoredEnemy.Alive && restoredEnemy.Statuses.IsDefaultOrEmpty, "failed lethal chain restores target");
        Near(caster.CurrentMana, 100, "failed lethal chain restores mana");
        Require(!rollback.PendingEvents.Any(item => item.Type is "ability" or "defeated"), "failed lethal chain restores facts");
    }

    private static void TemporaryCopiesHaveNoHeroMana()
    {
        using var battle = new BattleSimulation(new BattleConfig
        {
            Seed = 4708, FloorRule = new ClearFloorRuleRuntime("temporary_mana", "临时模板", ""),
            Spawns = [new BattleSpawn(Unit(Skill(EffectKind.Shield, true), 100), 0, new Vector2I(2, 2), "copy", IsTemporary: true),
                new BattleSpawn(Unit(null, 0), 0, new Vector2I(1, 2), "roster", IsPersistentRosterHero: true),
                new BattleSpawn(Unit(null, 0), 1, new Vector2I(4, 2), "enemy")],
            HeroRule = new HeroRuleSnapshot(1, 1, 1, 0, 0, 0, false, "", 1, 1, 0, 0, 0, 0, false, false, 0, 0, "")
        });
        var copy = battle.Units.First(unit => unit.IsTemporary);
        Near(copy.MaxMana, 0, "temporary copy has no mana capacity");
        battle.Step();
        Require(!battle.PendingEvents.Any(item => item.Type == "ability" && item.SourceRuntimeId == copy.RuntimeId),
            "reused temporary template does not inherit hero skill casting");
    }

    private static CompiledAbilityDefinition Skill(EffectKind kind, bool self) => new(
        "mana_contract_skill", "法力契约技能", "", AbilityActivationKind.Automatic, AbilityTriggerKind.ManaFull,
        100, 0, 10, 0, 0,
        [new CompiledEffectAbilityOperation(new CompiledEffectBinding(
            "mana_contract_effect", 0, new CompiledEffectTrigger(EffectTriggerKind.Manual, EffectDomainEventKind.None),
            [], self ? new CompiledOwnerTargetQuery() : new CompiledExplicitTargetQuery(),
            [new CompiledEffectStep(kind, EffectAmountSource.Fixed, 80)],
            new CompiledEffectBindingLimits(0, 0, 32, 8), null), AbilityInvocationValueSource.Fixed, 1)],
        null);

    private static UnitSnapshot Unit(CompiledAbilityDefinition? ability, float startingMana, float range = 2)
    {
        var definition = new UnitDefinition
        {
            Id = ability is null ? "mana_contract_enemy" : "mana_contract_hero", DisplayName = "契约角色",
            IsHero = ability is not null, MaxHealth = 1000, AttackDamage = 1, AttackRange = range,
            MaxMana = ability is null ? 0 : 100, StartingMana = startingMana,
            ManaPerSecond = 5, ManaPerAttack = 10, ManaPerDamageRatio = 50, ManaPerHitCap = 20
        };
        try { return BattleSetupFactory.Snapshot(definition, abilityLoadout: ability is null ? null : new CompiledAbilityLoadout([ability])); }
        finally { definition.Dispose(); }
    }

    private static BattleSimulation Battle(UnitSnapshot player, UnitSnapshot enemy,
        Action<BattleCombatBindingRegistry>? configure = null) => new(new BattleConfig
    {
        Seed = 4707,
        FloorRule = new ClearFloorRuleRuntime("mana_contract", "法力契约", ""),
        Spawns = [new BattleSpawn(player, 0, new Vector2I(2, 2), "player"),
            new BattleSpawn(enemy, 1, new Vector2I(4, 2), "enemy")],
        HeroRule = new HeroRuleSnapshot(1, 1, 1, 0, 0, 0, false, "", 1, 1, 0, 0, 0, 0, false, false, 0, 0, ""),
        ConfigureCombatBindings = configure
    });

    private static void Near(float actual, float expected, string message) =>
        Require(Math.Abs(actual - expected) < .001f, $"{message}: {actual} != {expected}");
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
