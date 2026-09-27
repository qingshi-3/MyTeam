using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Composition;
using TowerAutobattler.Content;
using TowerAutobattler.Traits;
using static TraitMatrixValidationSupport;

public static class TraitMatrixDirectedBehaviorChecks
{
    public static object[] Run(CompiledGamePackage package, BattleLabContentIndex index, Plan plan)
    {
        var evidence = new List<object>();
        foreach (var trait in plan.Traits)
        foreach (var threshold in trait.Thresholds)
        {
            if (trait.Id is "guard" or "fighter" or "ranger" or "assassin" or "mage")
            {
                using var baseline = new BattleSimulation(Isolated(index, plan, trait, threshold, enabled: false));
                using var active = new BattleSimulation(Isolated(index, plan, trait, threshold));
                foreach (var unit in active.Units.Where(x => x.Team == 0))
                {
                    var original = baseline.Units.Single(x => x.RuntimeId == unit.RuntimeId);
                    if (trait.Id == "guard")
                    {
                        Near(unit.MaxHealth, original.MaxHealth * (threshold == 2 ? 1.08f : 1.15f), "guard health");
                        Near(unit.Armor, original.Armor + (threshold == 2 ? 8 : 16), "guard armor");
                    }
                    if (trait.Id == "fighter") Near(unit.MaxHealth, original.MaxHealth * (1 + threshold * .05f), "fighter health");
                    if (trait.Id == "ranger") Near(unit.Attributes.GetValue(CombatAttribute.AttackSpeed),
                        original.Attributes.GetValue(CombatAttribute.AttackSpeed) + (threshold == 2 ? .15f : .30f), "ranger speed");
                    if (trait.Id == "assassin") Near(unit.Attributes.GetValue(CombatAttribute.CriticalChance),
                        original.Attributes.GetValue(CombatAttribute.CriticalChance) + (threshold == 2 ? .15f : threshold == 3 ? .25f : .35f), "assassin crit chance");
                    if (trait.Id == "mage") Near(unit.Attributes.GetValue(CombatAttribute.ManaPerSecond),
                        original.Attributes.GetValue(CombatAttribute.ManaPerSecond) + threshold / 2f, "mage natural mana");
                }
                evidence.Add(new { trait.Id, Threshold = threshold, Check = "isolated opening attribute delta versus identical no-trait fixture" });
            }
            if (trait.Id == "construct")
            {
                using var battle = new BattleSimulation(Isolated(index, plan, trait, threshold));
                var percent = threshold == 2 ? .10f : threshold == 4 ? .18f : .26f;
                foreach (var unit in battle.Units.Where(x => x.Team == 0)) Near(unit.Shield, unit.MaxHealth * percent, "construct opening shield");
                for (var tick = 0; tick < 90; tick++) battle.Step();
                Require(battle.Units.Where(x => x.Team == 0).All(x => Math.Abs(x.Shield) < .01f), "construct shields must expire");
                Require(!battle.CombatEvents.Any(x => x.SubjectStableId == "matrix_shield_broken"), "expiration falsely triggers broken-shield event");
                Require(!battle.CombatEvents.Any(x => x.Kind == BattleCombatEventKind.DamageResolved), "expiration retaliated without enemy damage");
                evidence.Add(new { trait.Id, Threshold = threshold, Check = "opening shield fraction, expiry, no retaliation/repair on expiry" });
            }
            if (trait.Id is "ranger" or "frost" or "poison")
            {
                using var battle = new BattleSimulation(Isolated(index, plan, trait, threshold, attack: true));
                for (var tick = 0; tick < 240; tick++) battle.Step();
                var events = battle.CombatEvents.ToArray();
                var traitId = "mx_" + trait.Id;
                var attacker = battle.Units.First(x => x.Team == 0);
                var hits = events.Where(e => e.Kind == BattleCombatEventKind.AttackLanded && e.SourceRuntimeId == attacker.RuntimeId).ToArray();
                Require(hits.Length >= 4, $"invalid directed fixture: {trait.Id}/{threshold} did not attack");
                if (trait.Id == "ranger")
                {
                    var derived = events.Where(e => e.Kind == BattleCombatEventKind.DamageResolved && e.Source.StableId == traitId).ToArray();
                    var actions = hits.Select(e => e.ActionId).Distinct().Count();
                    Require(hits.All(e => !string.IsNullOrEmpty(e.ActionId)), "basic hits need stable action identity");
                    Require(derived.Length == actions / 4, "ranger must pay once per four attacks; derived damage must not recurse");
                    Require(derived.All(e => e.DamageClass == CombatDamageClass.Derived), "ranger payoff misclassified as attack/active");
                }
                if (trait.Id == "frost")
                {
                    var chill = events.Where(e => e.Kind == BattleCombatEventKind.StatusApplied && e.SubjectStableId == "matrix_chill").ToArray();
                    Require(chill.Length > 0, "frost attacks never apply chill");
                    var freezes = events.Where(e => e.Kind == BattleCombatEventKind.ControlApplied && e.SubjectStableId == "matrix_freeze").ToArray();
                    Require(threshold == 2 ? freezes.Length == 0 : freezes.Length > 0, "frost freeze tier gate wrong");
                    if (threshold > 2)
                    {
                        Require(freezes.All(e => Math.Abs(e.EffectiveValue - (threshold == 4 ? 5 : 8)) < .01f), "frost freeze duration wrong");
                        foreach (var target in freezes.GroupBy(e => e.TargetRuntimeId))
                        {
                            var times = target.Select(e => e.Tick).ToArray();
                            Require(times.Zip(times.Skip(1)).All(pair => pair.Second - pair.First >= 30), "freeze internal cooldown violated");
                        }
                    }
                }
                if (trait.Id == "poison")
                {
                    var poison = events.Where(e => e.Kind == BattleCombatEventKind.DamageResolved && e.Source.StableId == "matrix_poison").ToArray();
                    Require(poison.Length > 0, "poison attacks never tick");
                    foreach (var e in poison)
                    {
                        Near(e.EffectiveValue, 2 * (1 + threshold * .1f), "poison per-layer tier damage");
                        Require(e.DamageClass == CombatDamageClass.Periodic && e.DamageType == TowerAutobattler.Effects.EffectDamageType.True,
                            "poison must stay periodic true damage");
                    }
                }
                evidence.Add(new { trait.Id, Threshold = threshold, Check = "isolated native basic-attack event behavior", AttackActions = hits.Select(e => e.ActionId).Distinct().Count() });
            }
            if (trait.Id is "mage" or "support" or "blood" or "ember" or "astral")
            {
                var operations = trait.Id == "support"
                    ? new[] { Atom(MatrixOperationKind.Heal, MatrixTargetKind.Self, 20), Atom(MatrixOperationKind.Shield, MatrixTargetKind.Self, 20) }
                    : new[] { Atom(MatrixOperationKind.Damage, MatrixTargetKind.CurrentEnemy, 20) };
                using var battle = new BattleSimulation(Isolated(index, plan, trait, threshold, probeOperations: operations));
                var caster = battle.Units.First(x => x.Team == 0);
                foreach (var u in battle.Units.Where(x => x.Team == 0))
                {
                    u.CurrentMana = 0;
                    u.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond, 0);
                    u.Attributes.SetBaseValue(CombatAttribute.ManaPerAttack, 0);
                }
                caster.Attributes.SetBaseValue(CombatAttribute.CriticalChance, 0);
                caster.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond, 0);
                caster.Attributes.SetBaseValue(CombatAttribute.ManaPerAttack, 0);
                caster.Health = caster.MaxHealth * (trait.Id == "blood" ? .4f : .5f);
                var beforeHealth = caster.Health;
                var wanted = trait.Id == "ember" ? 6 : trait.Id == "astral" ? (threshold == 2 ? 4 : 3) : 1;
                for (var cast = 0; cast < wanted; cast++)
                {
                    caster.CurrentMana = caster.MaxMana;
                    var prior = battle.CombatEvents.Count(e => e.Kind == BattleCombatEventKind.AbilityResolved && e.SubjectStableId == "matrix_directed_probe");
                    for (var tick = 0; tick < 40 && battle.CombatEvents.Count(e => e.Kind == BattleCombatEventKind.AbilityResolved && e.SubjectStableId == "matrix_directed_probe") == prior; tick++) battle.Step();
                    Require(battle.CombatEvents.Count(e => e.Kind == BattleCombatEventKind.AbilityResolved && e.SubjectStableId == "matrix_directed_probe") == prior + 1,
                        $"invalid cast fixture {trait.Id}/{threshold}/{cast}");
                    // Respect resource recovery before refilling the next independent cast.
                    for (var tick = 0; tick < 12; tick++) battle.Step();
                }
                var events = battle.CombatEvents.ToArray();
                if (trait.Id == "mage")
                {
                    var damage = events.Single(e => e.Kind == BattleCombatEventKind.DamageResolved && e.Source.StableId == "matrix_directed_probe");
                    Near(damage.EffectiveValue, 20 * (1 + threshold / 2 * .15f), "mage active direct damage multiplier");
                }
                if (trait.Id == "support")
                {
                    var multiplier = threshold == 2 ? 1.15f : 1.25f;
                    var heal = events.Single(e => e.Kind == BattleCombatEventKind.HealingResolved && e.Source.StableId == "matrix_directed_probe");
                    var shield = events.Single(e => e.Kind == BattleCombatEventKind.ShieldResolved && e.Source.StableId == "matrix_directed_probe");
                    Near(heal.EffectiveValue, 20 * multiplier, "support effective healing multiplier");
                    Near(shield.EffectiveValue, 20 * multiplier, "support shield multiplier");
                }
                if (trait.Id == "blood")
                {
                    var bonus = threshold == 2 ? .10f : threshold == 3 ? .18f : .26f;
                    var steal = threshold == 2 ? .03f : threshold == 3 ? .06f : .09f;
                    var damage = events.Single(e => e.Kind == BattleCombatEventKind.DamageResolved && e.Source.StableId == "matrix_directed_probe");
                    Near(damage.EffectiveValue, 20 * (1 + bonus), "blood low-health direct damage");
                    Near(caster.Health - beforeHealth, damage.EffectiveValue * steal, "blood direct lifesteal");
                }
                if (trait.Id == "ember")
                {
                    var detonations = events.Where(e => e.SubjectStableId == "matrix_ember_detonation").ToArray();
                    Require(detonations.Length == (threshold == 2 ? 0 : 1), "ember threshold/five-old-layer detonation wrong");
                    Require(events.Count(e => e.Kind == BattleCombatEventKind.AbilityResolved && e.SubjectStableId == "matrix_directed_probe") == 6, "derived detonation added a cast");
                    Require(events.Count(e => e.Kind == BattleCombatEventKind.SkillHitLanded && e.Source.StableId == "matrix_directed_probe") == 6, "derived detonation added a direct skill hit");
                    var payoff = events.Where(e => e.Kind == BattleCombatEventKind.DamageResolved && e.Source.StableId == "mx_ember").ToArray();
                    Require(payoff.Length == (threshold == 2 ? 0 : 1) && payoff.All(e => e.DamageClass == CombatDamageClass.Derived), "ember payoff recursed or wrong damage class");
                }
                if (trait.Id == "astral")
                {
                    var recipients = battle.Units.Where(x => x.Team == 0 && x.CurrentMana > .01f).ToArray();
                    Require(recipients.Length == (threshold == 2 ? 1 : 2), "astral wrong recipient count");
                    foreach (var u in recipients) Near(u.CurrentMana, threshold == 2 ? 10 : threshold == 3 ? 12 : 15, "astral mana amount");
                    Require(events.Count(e => e.Kind == BattleCombatEventKind.ManaSkillResolved) == wanted, "astral refund counted as a cast");
                    if (threshold == 5)
                    {
                        var before = battle.CombatEvents.Count;
                        caster.CurrentMana = caster.MaxMana;
                        for (var tick = 0; tick < 40 && !battle.CombatEvents.Skip(before).Any(e => e.Kind == BattleCombatEventKind.AbilityResolved); tick++) battle.Step();
                        var boosted = battle.CombatEvents.Skip(before).Single(e => e.Kind == BattleCombatEventKind.DamageResolved && e.Source.StableId == "matrix_directed_probe");
                        Near(boosted.EffectiveValue, 24, "astral next-active boost");
                    }
                }
                evidence.Add(new { trait.Id, Threshold = threshold, Check = "isolated successful active effect amounts and event boundaries" });
            }
            if (trait.Id is "summoner" or "death")
            {
                var summon = MatrixAbilityCompiler.Compile(new MatrixAbilityOperationSpec { Kind = MatrixOperationKind.Summon,
                    Target = MatrixTargetKind.Self, ContentId = "matrix_spirit_bone", Count = 1,
                    Maximum = trait.Id == "summoner" ? 3 : 10, DurationTicks = 500 }, "summon probe", new ValidationReport())!;
                using var battle = new BattleSimulation(Isolated(index, plan, trait, threshold, probeOperations: [summon],
                    enemyOperations: trait.Id == "death" ? [Atom(MatrixOperationKind.Damage, MatrixTargetKind.LowestHealthEnemies, 10000)] : null));
                var caster = battle.Units.First(x => x.Team == 0); var enemy = battle.Units.Single(x => x.Team == 1);
                foreach (var u in battle.Units)
                {
                    u.CurrentMana = 0; u.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond, 0);
                    u.Attributes.SetBaseValue(CombatAttribute.ManaPerAttack, 0);
                    if (trait.Id == "death" && u.Team == 0) u.Health = u.MaxHealth * .5f;
                }
                if (trait.Id == "summoner")
                {
                    CastOnce(battle, caster);
                    var products = battle.Units.Where(x => x.IsTemporary && x.Team == 0 && x.Alive).ToArray();
                    Require(products.Length == (threshold == 2 ? 1 : 2), "summoner first successful cast additional product count");
                    foreach (var product in products)
                    {
                        Near(product.MaxHealth, 65 * (threshold == 2 ? 1.15f : threshold == 3 ? 1.25f : 1.35f), "summoner product health");
                    }
                    var productIds = products.Select(x => x.RuntimeId).ToHashSet();
                    for (var tick = 0; tick < 80 && !battle.CombatEvents.Any(e => e.Kind == BattleCombatEventKind.DamageResolved && productIds.Contains(e.SourceRuntimeId)); tick++) battle.Step();
                    var productHit = battle.CombatEvents.FirstOrDefault(e => e.Kind == BattleCombatEventKind.DamageResolved && productIds.Contains(e.SourceRuntimeId));
                    Require(productHit is not null, "summoner fixture never dealt product damage");
                    Near(productHit!.EffectiveValue, 8 * (threshold == 2 ? 1.10f : threshold == 3 ? 1.15f : 1.20f), "summoner effective product damage");
                    CastOnce(battle, caster);
                    if (threshold == 2) CastOnce(battle, caster);
                    Require(battle.Units.Count(x => x.IsTemporary && x.Team == 0 && x.Alive) == 3, "summoner extra cast recursed or ignored living cap");
                    var count = battle.CombatEvents.Count(x => x.Kind == BattleCombatEventKind.AbilityResolved);
                    caster.CurrentMana = caster.MaxMana;
                    for (var tick = 0; tick < 20; tick++) battle.Step();
                    Require(battle.Units.Count(x => x.IsTemporary && x.Team == 0 && x.Alive) == 3 &&
                        battle.CombatEvents.Count(x => x.Kind == BattleCombatEventKind.AbilityResolved) == count, "full summon cap consumed a cast or made a fourth unit");
                }
                else
                {
                    enemy.Attributes.SetBaseValue(CombatAttribute.MaxMana, 70);
                    var requiredDeaths = threshold == 2 ? 4 : 3;
                    for (var death = 0; death < requiredDeaths; death++)
                    {
                        CastOnce(battle, caster);
                        var victim = battle.Units.Single(x => x.IsTemporary && x.Team == 0 && x.Alive);
                        victim.Health = victim.MaxHealth * .1f;
                        CastOnce(battle, enemy);
                        Require(!victim.Alive, "death-resource fixture did not kill the intended owned summon");
                    }
                    var healing = battle.CombatEvents.Where(e => e.Kind == BattleCombatEventKind.HealingResolved && e.Source.StableId == "mx_death").ToArray();
                    Require(healing.Length == (threshold == 2 ? 1 : 2), "death soul threshold or recipient count wrong");
                    foreach (var heal in healing)
                    {
                        var target = battle.Units.Single(x => x.RuntimeId == heal.TargetRuntimeId);
                        Near(heal.EffectiveValue, target.MaxHealth * (threshold == 2 ? .08f : .10f), "death soul effective healing amount");
                    }
                    Require(battle.Units.Count(x => x.IsTemporary && x.Team == 0 && x.Alive) == (threshold == 6 ? 1 : 0), "death highest-tier product missing or leaked to lower tier");
                }
                Require(battle.TraitSnapshot.Resolve("mx_" + trait.Id, 0).Value == threshold, "summoned/dead units changed opening contribution count");
                evidence.Add(new { trait.Id, Threshold = threshold, Check = "actual summon lifecycle, owned source, tier amounts and population exclusion" });
            }
        }
        evidence.Add(BloodDerivedBoundary(index, plan));
        return evidence.ToArray();
    }

    private static object BloodDerivedBoundary(BattleLabContentIndex index, Plan plan)
    {
        var config = Isolated(index, plan, plan.Traits.Single(x => x.Id == "blood"), 2,
            probeOperations: [Atom(MatrixOperationKind.Damage, MatrixTargetKind.CurrentEnemy, 20), Atom(MatrixOperationKind.Poison, MatrixTargetKind.CurrentEnemy, 1)],
            enemyOperations: [Atom(MatrixOperationKind.Damage, MatrixTargetKind.LowestHealthEnemies, 10000)]);
        var source = config.Spawns[0];
        config.Spawns[0] = source with { HealthRatio = .4f, Unit = source.Unit with
        { AbilityLoadout = new([source.Unit.AbilityLoadout!.Abilities[0] with { Trigger = AbilityTriggerKind.BattleStarted, MaxUses = 1 }]) } };
        using var battle = new BattleSimulation(config);
        var owner = battle.Units.First(x => x.Team == 0); var enemy = battle.Units.Single(x => x.Team == 1);
        var direct = battle.CombatEvents.Single(e => e.Kind == BattleCombatEventKind.DamageResolved && e.Source.StableId == "matrix_directed_probe");
        Require(direct.DamageClass == CombatDamageClass.Derived, "blood fixture must exercise derived direct damage");
        Near(owner.Health - owner.MaxHealth * .4f, direct.EffectiveValue * .03f, "blood derived direct leech");
        var health = owner.Health;
        for (var tick = 0; tick < 12; tick++) battle.Step();
        Near(owner.Health, health, "blood periodic poison must not leech");
        var before = battle.CombatEvents.Where(e => e.Kind == BattleCombatEventKind.DamageResolved && e.Source.StableId == "matrix_poison").LastOrDefault();
        Require(before is not null, "blood poison fixture never ticked before source death");
        enemy.Attributes.SetBaseValue(CombatAttribute.MaxMana, 70);
        CastOnce(battle, enemy);
        Require(!owner.Alive, "blood source-death fixture did not defeat source");
        var defeatTick = battle.CombatEvents.Single(e => e.Kind == BattleCombatEventKind.UnitDefeated && e.TargetRuntimeId == owner.RuntimeId).Tick;
        for (var tick = 0; tick < 12; tick++) battle.Step();
        var after = battle.CombatEvents.Where(e => e.Kind == BattleCombatEventKind.DamageResolved && e.Source.StableId == "matrix_poison" && e.Tick > defeatTick).ToArray();
        Require(after.Length > 0 && after.All(e => e.EffectiveValue <= before!.EffectiveValue + .01f), "dead poison source gained a low-health damage bonus");
        return new { Id = "blood", Threshold = 2, Check = "derived direct damage leeches; periodic poison does not; dead source does not gain low-health poison bonus" };
    }

    private static CompiledMatrixOperation Atom(MatrixOperationKind kind, MatrixTargetKind target, float amount)
    {
        using var spec = new MatrixAbilityOperationSpec { Kind = kind, Target = target, Amount = amount, DurationTicks = 100 };
        return MatrixAbilityCompiler.Compile(spec, "directed trait effect", new ValidationReport()) ?? throw new InvalidOperationException("failed to compile directed trait probe");
    }

    private static void CastOnce(BattleSimulation battle, BattleUnitState caster)
    {
        var before = battle.CombatEvents.Count(e => e.Kind == BattleCombatEventKind.AbilityResolved && e.SourceRuntimeId == caster.RuntimeId);
        caster.CurrentMana = caster.MaxMana;
        for (var tick = 0; tick < 50 && battle.CombatEvents.Count(e => e.Kind == BattleCombatEventKind.AbilityResolved && e.SourceRuntimeId == caster.RuntimeId) == before; tick++) battle.Step();
        Require(battle.CombatEvents.Count(e => e.Kind == BattleCombatEventKind.AbilityResolved && e.SourceRuntimeId == caster.RuntimeId) == before + 1, "directed cast did not succeed exactly once");
        for (var tick = 0; tick < 12; tick++) battle.Step();
    }

    private static BattleConfig Isolated(BattleLabContentIndex index, Plan plan, PlanTrait trait, int threshold, bool enabled = true, bool attack = false,
        CompiledMatrixOperation[]? probeOperations = null, CompiledMatrixOperation[]? enemyOperations = null)
    {
        var members = plan.Units.Where(x => x.Tags.Contains(trait.Id)).OrderBy(x => x.Tier).ThenBy(x => x.Id, StringComparer.Ordinal).Take(threshold).Select(x => x.ContentId).ToArray();
        var original = Laboratory(index, members, ["soldier_dummy_static"], 20260926);
        var def = original.Traits.Definitions.Single(x => x.StableId == "mx_" + trait.Id);
        var contributions = enabled ? original.Traits.Contributions.Where(x => x.TraitId == def.StableId).ToArray() : [];
        var spawns = original.Spawns.Select((s, i) => s with
        {
            Cell = probeOperations is not null && s.Team == 1 ? new Godot.Vector2I(3, 0) : s.Cell,
            Unit = s.Unit with { AbilityLoadout = s.Team == 1 && enemyOperations is not null ? new CompiledAbilityLoadout([
                new CompiledAbilityDefinition("matrix_enemy_probe", "敌方定向效果", "测试专用", AbilityActivationKind.Automatic, AbilityTriggerKind.ManaFull,
                    0, 0, 1, 0, 0, enemyOperations.Cast<CompiledAbilityOperation>().ToImmutableArray(), null)]) : s.Team == 0 && (i == 0 || trait.Id == "astral") && probeOperations is not null ? new CompiledAbilityLoadout([
                new CompiledAbilityDefinition("matrix_directed_probe", "定向效果", "测试专用", AbilityActivationKind.Automatic, AbilityTriggerKind.ManaFull,
                    0, 0, 1, 0, 0, probeOperations.Cast<CompiledAbilityOperation>().ToImmutableArray(), null)]) : null,
                Damage = 10, Armor = 0, LifeSteal = 0, HealPower = 0, Range = probeOperations is not null ? 20 : s.Unit.Range,
                MaxHealth = s.Team == 1 ? 1000000 : s.Unit.MaxHealth,
                Behavior = new UnitBehaviorSnapshot(Stationary: !attack || i != 0, DisableBasicAttacks: !attack || i != 0) }
        }).ToList();
        return new BattleConfig
        {
            Seed = original.Seed, Identity = original.Identity, FloorRule = original.FloorRule, Spawns = spawns,
            HeroRule = original.HeroRule, Modifiers = original.Modifiers, Summons = original.Summons,
            EmptyDeploymentSlots = original.EmptyDeploymentSlots, StartingGold = original.StartingGold, Relics = original.Relics,
            RelicSummons = original.RelicSummons, Equipment = original.Equipment,
            Traits = TraitBattlePreparationBuilder.Build([def], contributions), TacticalCommands = original.TacticalCommands,
            TacticalSummons = original.TacticalSummons, BossTimeline = original.BossTimeline,
            AdditionalBossTimelines = original.AdditionalBossTimelines, ConfigureCombatBindings = original.ConfigureCombatBindings
        };
    }
    private static void Near(float actual, float expected, string context) => Require(Math.Abs(actual - expected) < .02f, $"{context}: {actual} != {expected}");
}
