using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.BattleLab;
using TowerAutobattler.Content;
using TowerAutobattler.Statuses;
using static TraitMatrixValidationSupport;

public static class TraitMatrixNativeSkillChecks
{
    public static object[] Run(BattleLabContentIndex index) => [PoisonLanding(index), VolleyProgress(index), SameTickDispel(index),
        ContactDash(index, "hero_mx36", false), ContactDash(index, "hero_mx08", false),
        ContactDash(index, "hero_mx36", true), ContactDash(index, "hero_mx08", true)];

    private static object ContactDash(BattleLabContentIndex index, string heroId, bool blocked)
    {
        var config = Laboratory(index, [heroId, "soldier_dummy_static"], ["soldier_dummy_static"], 20260926);
        var poison = Ability("matrix_contact_poison_setup", [
            Operation(new() { Kind = MatrixOperationKind.Poison, Target = MatrixTargetKind.NearestEnemies, Amount = 5 })], AbilityTriggerKind.BattleStarted);
        var nativeRange = config.Spawns[0].Unit.Range;
        Prepare(config, heroId, poison);
        config.Spawns[0] = config.Spawns[0] with { Unit = config.Spawns[0].Unit with { Range = nativeRange } };
        // An allied stationary body blocks the far target without becoming an enemy fallback.
        config.Spawns[1] = config.Spawns[1] with { Cell = new Vector2I(blocked ? 5 : 2, 2) };
        config.Spawns[2] = config.Spawns[2] with { Cell = new Vector2I(blocked ? 7 : 4, 2) };
        using var battle = new BattleSimulation(config);
        var owner = battle.Units.Single(x => x.Definition.ContentId == heroId);
        var target = battle.Units.Single(x => x.Team == 1);
        if (!blocked)
        {
            // Real, non-overlapping contact geometry, inside the native attack reach and the
            // stopping circle that previously made Charge reject an otherwise useful cast.
            target.Position = owner.Position + Vector2.Right * (owner.BodyRadius + target.BodyRadius + BattlefieldSpace.BodyClearance + .02f);
            Require(BattlefieldSpace.IsWithinReach(owner, target, owner.AttackRange), heroId + " contact fixture outside native attack reach");
        }
        var start = owner.Position;
        var abilityId = owner.Definition.AbilityLoadout!.Abilities.Single(x => x.IsDisplayedActiveSkill).StableId;
        owner.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond, 0);
        owner.CurrentMana = owner.MaxMana;
        Require(target.Statuses.Count(x => x.StableId == "matrix_poison") == 5, heroId + " contact fixture lacks five poison layers");
        battle.Step();
        var hits = Hits(battle, abilityId);
        var casts = battle.CombatEvents.Count(e => e.Kind == BattleCombatEventKind.ManaSkillResolved && e.SourceRuntimeId == owner.RuntimeId);
        Require(owner.Position.DistanceTo(start) < .001f && !battle.PendingEvents.Any(e => e.Type == "displacement" && e.SourceRuntimeId == owner.RuntimeId),
            heroId + " contact/blocked cast fabricated displacement");
        if (blocked)
        {
            Require(casts == 0 && hits.Length == 0 && Math.Abs(owner.CurrentMana - owner.MaxMana) < .001f,
                heroId + " blocked distant dash committed payment or damage");
            Require(target.Statuses.Count(x => x.StableId == "matrix_poison") == 5 && !target.Statuses.Any(x => x.StableId == "matrix_chill"),
                heroId + " blocked distant dash applied child effects");
        }
        else
        {
            Require(casts == 1 && owner.CurrentMana < owner.MaxMana && hits.Length > 0 && hits.All(e => e.TargetRuntimeId == target.RuntimeId),
                heroId + " legal contact dash failed to commit mana and impact on target");
            Require(battle.CombatEvents.Any(e => e.Kind == BattleCombatEventKind.SkillHitLanded && e.Source.StableId == abilityId && e.TargetRuntimeId == target.RuntimeId),
                heroId + " contact dash omitted real skill hit fact");
            Require(heroId == "hero_mx08"
                    ? target.Statuses.Count(x => x.StableId == "matrix_poison") == 2 && hits.Length >= 2
                    : target.Statuses.Any(x => x.StableId == "matrix_chill"),
                heroId + " contact dash omitted native poison payoff/chill child effect");
        }
        return new { Unit = heroId, Check = blocked ? "distant body-blocked native dash preserves mana and applies no effects" :
            "native contact dash commits mana, impact and child effects without displacement", Casts = casts, Hits = hits.Length };
    }

    private static object SameTickDispel(BattleLabContentIndex index)
    {
        var config = Laboratory(index, ["hero_mx08", "soldier_dummy_static"], ["soldier_dummy_static", "soldier_dummy_static"], 20260926);
        var setup = Ability("matrix_status_setup", [
            Operation(new() { Kind = MatrixOperationKind.Poison, Target = MatrixTargetKind.NearestEnemies, Amount = 5 }),
            Operation(new() { Kind = MatrixOperationKind.Chill, Target = MatrixTargetKind.NearestEnemies, Amount = 1 }),
            Operation(new() { Kind = MatrixOperationKind.Ember, Target = MatrixTargetKind.NearestEnemies, Amount = 3 })], AbilityTriggerKind.BattleStarted);
        Prepare(config, "hero_mx08", setup);
        using var battle = new BattleSimulation(config);
        var target = battle.Units.Single(x => x.Team == 1 && x.Statuses.Any(s => s.StableId == "matrix_poison"));
        var other = battle.Units.Single(x => x.Team == 1 && x != target);
        // Obtain the production scope, then use its ordinary public dispel command. The private queries
        // remain private in production; reflection here only observes the just-invalidated effect ledger.
        var scope = (BattleStatusScope)(typeof(BattleSimulation).GetField("_statusScope", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(battle)
            ?? throw new InvalidOperationException("battle status scope unavailable"));
        object? Query(string method, params object?[] args) => typeof(BattleSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(battle, args)
            ?? throw new InvalidOperationException("missing matrix query " + method);
        var tick = battle.TickIndex;
        foreach (var id in new[] { "matrix_poison", "matrix_chill", "matrix_ember" })
            Require(scope.Dispel(target.RuntimeId, id, StatusDispelStrength.Ordinary), "public dispel did not remove " + id);
        Require((int)Query("MatrixReadPoison", target.RuntimeId, null)! == 0 && (int)Query("MatrixReadChill", target.RuntimeId)! == 0 &&
            (int)Query("MatrixReadEmber", target.RuntimeId)! == 0, "same-tick dispel left readable ghost layers");
        Require((int)Query("MatrixConsumePoison", target.RuntimeId, 2)! == 0 && (int)Query("MatrixConsumeEmber", target.RuntimeId, 2)! == 0,
            "same-tick dispel left consumable ghost layers");
        var transfers = (ImmutableArray<MatrixPoisonTransferReceipt>)Query("MatrixTransferPoison", target.RuntimeId, other.RuntimeId, 5, null)!;
        Require(transfers.IsEmpty && !other.Statuses.Any(s => s.StableId == "matrix_poison") && battle.TickIndex == tick,
            "same-tick dispel allowed poison transfer or advanced simulation");
        return new { Check = "public status dispel immediately invalidates poison/chill/ember reads, consumption and transfer in the same tick" };
    }

    private static object PoisonLanding(BattleLabContentIndex index)
    {
        var config = Laboratory(index, ["hero_mx08", "soldier_dummy_static"], ["soldier_dummy_static", "soldier_dummy_static"], 20260926);
        var setup = Ability("matrix_poison_setup", [
            Operation(new() { Kind = MatrixOperationKind.Poison, Target = MatrixTargetKind.NearestEnemies, Amount = 1, MaxTargets = 1 }),
            Operation(new() { Kind = MatrixOperationKind.Poison, Target = MatrixTargetKind.LowestHealthEnemies, Amount = 5, MaxTargets = 1 })], AbilityTriggerKind.BattleStarted);
        Prepare(config, "hero_mx08", setup);
        // The nearer low-poison body is off the dash line, so the higher-poison target remains reachable.
        config.Spawns[2] = config.Spawns[2] with { Cell = new Vector2I(5, 4) };
        config.Spawns[3] = config.Spawns[3] with { HealthRatio = .9f };
        using var battle = new BattleSimulation(config);
        var owner = battle.Units.Single(x => x.Definition.ContentId == "hero_mx08");
        var target = battle.Units.Single(x => x.Team == 1 && x.Statuses.Count(s => s.StableId == "matrix_poison") == 5);
        var nearer = battle.Units.Single(x => x.Team == 1 && x != target);
        Require(nearer.Position.DistanceTo(owner.Position) < target.Position.DistanceTo(owner.Position) && nearer.Statuses.Count(s => s.StableId == "matrix_poison") == 1,
            "MX08 target-selection fixture requires nearer low-poison and farther high-poison enemies");
        var abilityId = owner.Definition.AbilityLoadout!.Abilities.Single(x => x.IsDisplayedActiveSkill).StableId;
        owner.CurrentMana = owner.MaxMana;
        battle.Step();
        Require(battle.PendingEvents.Any(e => e.Type == "displacement" && e.SourceRuntimeId == owner.RuntimeId && e.Displacement is { Finished: false }), "MX08 did not start a real displacement");
        Require(target.Statuses.Count(x => x.StableId == "matrix_poison") == 5, "MX08 consumed poison before landing");
        Require(!battle.CombatEvents.Any(e => e.Kind == BattleCombatEventKind.DamageResolved && e.Source.StableId == abilityId), "MX08 damaged before landing");
        for (var tick = 0; tick < 12 && !battle.PendingEvents.Any(e => e.Type == "displacement" && e.SourceRuntimeId == owner.RuntimeId && e.Displacement is { Finished: true, Cancelled: false }); tick++) battle.Step();
        Require(battle.PendingEvents.Any(e => e.Type == "displacement" && e.SourceRuntimeId == owner.RuntimeId && e.Displacement is { Finished: true, Cancelled: false }), "MX08 never completed displacement");
        Require(target.Statuses.Count(x => x.StableId == "matrix_poison") == 2,
            $"MX08 landing must consume exactly three of five poison layers; high={target.Statuses.Count(x => x.StableId == "matrix_poison")} low={nearer.Statuses.Count(x => x.StableId == "matrix_poison")} owner={owner.Position} selected={target.Position}");
        Require(nearer.Statuses.Count(x => x.StableId == "matrix_poison") == 1, "MX08 consumed nearer low-poison target instead of selected high-poison target");
        var hits = battle.CombatEvents.Where(e => e.Kind == BattleCombatEventKind.DamageResolved && e.Source.StableId == abilityId).ToArray();
        Require(hits.Length >= 2 && hits.All(e => e.TargetRuntimeId == target.RuntimeId), "MX08 impact and poison payoff must hit selected poisoned target");
        return new { Unit = "MX08", Check = "native dash begins before poison consumption; landing consumes three layers and damages original selected target", Hits = hits.Length };
    }

    private static object VolleyProgress(BattleLabContentIndex index)
    {
        var config = Laboratory(index, ["hero_mx23", "soldier_dummy_static"], ["soldier_dummy_static", "soldier_dummy_static"], 20260926);
        var kill = Ability("matrix_target_removal", [Operation(new() { Kind = MatrixOperationKind.Damage, Target = MatrixTargetKind.CurrentEnemy, Amount = 2000000, Maximum = 2000000 })]);
        Prepare(config, "hero_mx23", kill);
        using var battle = new BattleSimulation(config);
        var owner = battle.Units.Single(x => x.Definition.ContentId == "hero_mx23");
        var helper = battle.Units.Single(x => x.Team == 0 && x != owner);
        helper.Attributes.SetBaseValue(CombatAttribute.MaxMana, 70); helper.CurrentMana = 0;
        helper.Attributes.SetBaseValue(CombatAttribute.ManaPerSecond, 0);
        var abilityId = owner.Definition.AbilityLoadout!.Abilities.Single(x => x.IsDisplayedActiveSkill).StableId;
        owner.CurrentMana = owner.MaxMana;
        for (var tick = 0; tick < 120 && Hits(battle, abilityId).Length < 5; tick++) battle.Step();
        var first = Hits(battle, abilityId);
        Require(first.Length == 5 && first.Select(x => x.TargetRuntimeId).Distinct().Count() == 1, "MX23 first native sequence must deliver five hits to one target");
        Require(first.Zip(first.Skip(1)).All(pair => pair.Second.EffectiveValue > pair.First.EffectiveValue + .01f), "MX23 same-target native volley damage must increase each shot");
        owner.CurrentMana = 0;
        var victimId = first[0].TargetRuntimeId;
        helper.CurrentMana = helper.MaxMana;
        for (var tick = 0; tick < 40 && battle.Units.Single(x => x.RuntimeId == victimId).Alive; tick++) battle.Step();
        Require(battle.CombatEvents.Any(e => e.Kind == BattleCombatEventKind.UnitDefeated && e.TargetRuntimeId == victimId), "target switch fixture did not resolve a real enemy defeat");
        helper.CurrentMana = 0;
        owner.CurrentMana = owner.MaxMana;
        var before = Hits(battle, abilityId).Length;
        for (var tick = 0; tick < 120 && Hits(battle, abilityId).Length == before; tick++) battle.Step();
        var next = Hits(battle, abilityId).Skip(before).FirstOrDefault();
        Require(next is not null && next.TargetRuntimeId != victimId, "MX23 failed to retarget its next native volley");
        Require(Math.Abs(next!.EffectiveValue - first[0].EffectiveValue) < .02f, "MX23 new target retained previous target damage progression");
        return new { Unit = "MX23", Check = "five native arrows increase actual same-target damage; after real target defeat next volley resets", FirstSequence = first.Select(x => x.EffectiveValue).ToArray(), NextTargetFirstDamage = next.EffectiveValue };
    }

    private static BattleCombatEvent[] Hits(BattleSimulation battle, string abilityId) => battle.CombatEvents
        .Where(e => e.Kind == BattleCombatEventKind.DamageResolved && e.Source.StableId == abilityId).ToArray();

    private static void Prepare(BattleConfig config, string heroId, CompiledAbilityDefinition helperAbility)
    {
        for (var i = 0; i < config.Spawns.Count; i++)
        {
            var s = config.Spawns[i];
            config.Spawns[i] = s with { Cell = new Vector2I(i == 0 ? 3 : i == 1 ? 2 : i + 4, 2), Unit = s.Unit with
            {
                MaxHealth = s.Team == 1 ? 1000000 : s.Unit.MaxHealth, Armor = 0, Range = 20,
                AbilityLoadout = s.Unit.ContentId == heroId ? s.Unit.AbilityLoadout : s.Team == 0 ? new([helperAbility]) : null,
                Behavior = new UnitBehaviorSnapshot(Stationary: true, DisableBasicAttacks: true)
            } };
        }
    }
    private static CompiledMatrixOperation Operation(MatrixAbilityOperationSpec operation)
    {
        using (operation) return MatrixAbilityCompiler.Compile(operation, "native skill fixture", new ValidationReport())
            ?? throw new InvalidOperationException("native skill fixture failed to compile");
    }
    private static CompiledAbilityDefinition Ability(string id, CompiledMatrixOperation[] operations, AbilityTriggerKind trigger = AbilityTriggerKind.ManaFull) =>
        new(id, "夹具能力", "测试专用", AbilityActivationKind.Automatic, trigger, 0, 0, 1, trigger == AbilityTriggerKind.BattleStarted ? 1 : 0, 0,
            operations.Cast<CompiledAbilityOperation>().ToImmutableArray(), null);
}
