using System;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Content;
using static TraitMatrixValidationSupport;

public static class TraitMatrixRollbackChecks
{
    public static object Run(BattleLabContentIndex index)
    {
        var original = Laboratory(index, ["hero_mx01"], ["soldier_dummy_static"], 20260926);
        var template = original.Spawns[0].Unit.AbilityLoadout!.Abilities.First(x => x.IsDisplayedActiveSkill);
        var report = new ValidationReport();
        CompiledMatrixOperation Compile(MatrixAbilityOperationSpec spec)
        {
            using (spec) return MatrixAbilityCompiler.Compile(spec, "rollback probe", report)
                ?? throw new InvalidOperationException("rollback operation failed compilation");
        }
        var ability = template with { StableId = "matrix_rollback_probe", CooldownTicks = 1, Operations =
        [
            Compile(new() { Kind = MatrixOperationKind.Counter, Target = MatrixTargetKind.Self, Amount = 1, Key = "rollback-counter" }),
            Compile(new() { Kind = MatrixOperationKind.Shield, Target = MatrixTargetKind.Self, Amount = 25, DurationTicks = 40 }),
            Compile(new() { Kind = MatrixOperationKind.Poison, Target = MatrixTargetKind.CurrentEnemy, Amount = 1 }),
            Compile(new() { Kind = MatrixOperationKind.NextAttackBoost, Target = MatrixTargetKind.Self, Amount = 1, Key = "rollback-boost", DurationTicks = 80 }),
            Compile(new() { Kind = MatrixOperationKind.Damage, Target = MatrixTargetKind.CurrentEnemy, Amount = 10, CounterRatio = 7, Key = "rollback-counter" })
        ] };
        var reject = true; var injected = 0;
        var config = new BattleConfig
        {
            Seed = original.Seed, FloorRule = original.FloorRule, HeroRule = original.HeroRule,
            Spawns = original.Spawns.Select((s, i) => s with { Cell = new Godot.Vector2I(2 + i, 2),
                Unit = s.Unit with { AbilityLoadout = i == 0 ? new([ability]) : null, MaxHealth = 100000,
                    Damage = 10, Armor = 0, LifeSteal = 0, HealPower = 0, Range = 20,
                    Behavior = new UnitBehaviorSnapshot(Stationary: true, DisableBasicAttacks: i != 0) } }).ToList(),
            ConfigureCombatBindings = bindings => bindings.Subscribe(BattleCombatEventKind.AbilityResolved,
                CombatSourceRef.System("matrix-rollback-probe"), 0, (fact, sink) =>
                {
                    if (!reject || fact.SubjectStableId != ability.StableId) return;
                    injected++;
                    sink.Enqueue(CombatSourceRef.System("matrix-rollback-probe"), 0,
                        _ => throw new InvalidOperationException("intentional matrix ability commit failure"));
                })
        };
        using var battle = new BattleSimulation(config);
        var owner = battle.Units.Single(x => x.Team == 0); var target = battle.Units.Single(x => x.Team == 1);
        owner.Attributes.SetBaseValue(CombatAttribute.CriticalChance, 0);
        owner.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond, 0);
        owner.CurrentMana = owner.MaxMana; owner.AttackCooldown = 1000;
        battle.Step();
        Require(injected == 1, "rollback fault injection never reached ability commit");
        Require(Math.Abs(owner.CurrentMana - owner.MaxMana) < .01f && owner.Shield == 0 && target.Health == target.MaxHealth,
            "failed matrix cast leaked mana, shield or damage");
        Require(!target.Statuses.Any(x => x.StableId == "matrix_poison"), "failed cast leaked poison status");
        Require(!battle.CombatEvents.Any(x => x.Kind == BattleCombatEventKind.AbilityResolved && x.SubjectStableId == ability.StableId), "failed cast published success");
        owner.CurrentMana = 0; owner.AttackCooldown = 0;
        var before = battle.CombatEvents.Count;
        for (var i = 0; i < 40 && !battle.CombatEvents.Skip(before).Any(x => x.Kind == BattleCombatEventKind.DamageResolved && x.DamageClass == CombatDamageClass.BasicAttack); i++) battle.Step();
        var basic = battle.CombatEvents.Skip(before).FirstOrDefault(x => x.Kind == BattleCombatEventKind.DamageResolved && x.DamageClass == CombatDamageClass.BasicAttack);
        Require(basic is not null && Math.Abs(basic.EffectiveValue - 10) < .01f, "failed cast leaked next-attack boost");
        reject = false; owner.CurrentMana = owner.MaxMana; owner.AttackCooldown = 1000;
        before = battle.CombatEvents.Count;
        for (var i = 0; i < 40 && !battle.CombatEvents.Skip(before).Any(x => x.Kind == BattleCombatEventKind.AbilityResolved && x.SubjectStableId == ability.StableId); i++) battle.Step();
        var committedDamage = battle.CombatEvents.Skip(before).Single(x => x.Kind == BattleCombatEventKind.DamageResolved && x.Source.StableId == ability.StableId);
        Require(Math.Abs(committedDamage.EffectiveValue - 17) < .01f, "failed cast leaked counter (retry must read exactly one increment)");
        Require(Math.Abs(owner.Shield - 25) < .01f && target.Statuses.Count(x => x.StableId == "matrix_poison") == 1, "retry did not commit exactly one shield/poison state");
        return new { Check = "fault-injected atomic matrix cast and subsequent real retry", InjectedFailures = injected,
            ManaRollback = true, CounterRollback = true, PoisonRollback = true, TimedShieldRollback = true, NextAttackRollback = true };
    }
}
