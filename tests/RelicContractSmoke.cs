using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Effects;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;
using TowerAutobattler.Run;

public partial class RelicContractSmoke : Node
{
    public override async void _Ready()
    {
        var code = await RunAsync();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(code);
    }

    private async Task<int> RunAsync()
    {
        try
        {
            var catalog = GD.Load<ContentCatalog>("res://content/catalogs/alpha_catalog.tres")
                ?? throw new InvalidOperationException("catalog load");
            var gate = await TestProjectFixture.PublishAsync(this);
            var registry = gate.Package?.Content
                ?? throw new InvalidOperationException("content gate: " + string.Join("; ", gate.Report.CoreErrors));
            var definitions = ProductionDefinitionsAndValues(catalog);
            ProductionBattleSimulationEquivalence(definitions);
            ProductionStackedCompatibilityEquivalence(definitions);
            EmptySlotAdditiveCompatibility();
            PerStackPreservation(definitions);
            LinearProjectionCheckpointAndCleanup(definitions["item_lone_crown"]);
            RuntimeIsolationAndAggregation(definitions);
            CompilerAndPublicationFailures(definitions, catalog);
            ReactiveCounterRuntimeAndPersistence(registry);
            TransitionValidationAndLifecycle(definitions);
            RunApplicationExactlyOnce(registry);
            RunApplicationFailureTransitionGuards(registry);
            RunApplicationTransitionIdentityLifecycle(registry);
            BattleScopeSourceGuard();
            ItemCompatibilityAuthorityRemoved();
            GD.Print("RELIC_CONTRACT_OK production=current-values,scene-owned-resources runtime=isolated,ordered,pow-add-or transition=validated,exactly-once save=current-state-preserved lifecycle=zero scope=run-battle-separated compatibility=item-provider-removed");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("RELIC_CONTRACT_FAILED: " + exception);
            return 1;
        }
    }

    private static IReadOnlyDictionary<string, CompiledRelicDefinition> ProductionDefinitionsAndValues(ContentCatalog catalog)
    {
        var compiled = new Dictionary<string, CompiledRelicDefinition>(StringComparer.Ordinal);
        foreach (var entry in catalog.Items.Where(entry =>
                     entry.Definition is ItemDefinition { ProductKind: ItemProductKind.Relic }))
        {
            var root = entry.Scene.Instantiate<ItemContentRoot>();
            try
            {
                if (root.Relic is null || root.Relic.StableId != entry.StableId)
                    throw new InvalidOperationException($"{entry.StableId} did not reference its own relic resource");
                if (string.IsNullOrWhiteSpace(root.Relic.ResourcePath) ||
                    !root.Relic.ResourcePath.StartsWith("res://content/relics/definitions/", StringComparison.Ordinal))
                    throw new InvalidOperationException($"{entry.StableId} relic is not an independently authored production resource");
                var result = RelicDefinitionCompiler.Compile(root.Relic,
                    catalog.AllEntries().Select(value => value.StableId).ToHashSet(StringComparer.Ordinal));
                if (result.Definition is null || result.Report.HasCoreErrors)
                    throw new InvalidOperationException($"{entry.StableId} relic compile: {string.Join("; ", result.Report.CoreErrors)}");
                compiled.Add(entry.StableId, result.Definition);
            }
            finally
            {
                root.Free();
            }
        }

        ExpectStartShield(compiled, "item_aegis_standard", 15);
        ExpectAttribute<CompiledRelicPlayerHeroesTarget>(compiled, "item_blood_chalice",
            CombatAttribute.LifeSteal, AttributeModifierOperation.Add, .15f);
        ExpectStartSummon(compiled, "item_clockwork_seed", "soldier_void_mech", .85f, .9f);
        ExpectAttributes<CompiledRelicPlayerFormationAdjacentTarget>(compiled, "item_commander_map",
            (CombatAttribute.Armor, AttributeModifierOperation.Add, 4f),
            (CombatAttribute.AttackDamage, AttributeModifierOperation.Multiply, 1.08f));
        ExpectAttribute<CompiledRelicPlayerHeroesTarget>(compiled, "item_crimson_mail",
            CombatAttribute.MaxHealth, AttributeModifierOperation.Multiply, 1.3f);
        ExpectAttribute<CompiledRelicPlayerHeroesTarget>(compiled, "item_duelist_seal",
            CombatAttribute.AttackDamage, AttributeModifierOperation.Multiply, 1.25f);
        ExpectAttribute<CompiledRelicPlayerArmyTarget>(compiled, "item_field_rations",
            CombatAttribute.MaxHealth, AttributeModifierOperation.Multiply, 1.12f);
        ExpectOutcome(compiled, "item_gilded_contract", RelicRunOutcomeKind.VictoryGold, 3);
        ExpectAttributes<CompiledRelicPlayerArmyTarget>(compiled, "item_last_banner",
            (CombatAttribute.MaxHealth, AttributeModifierOperation.Multiply, 1.15f),
            (CombatAttribute.AttackDamage, AttributeModifierOperation.Multiply, 1.15f));
        ExpectAttribute<CompiledRelicPlayerEmptySlotHeroesTarget>(compiled, "item_lone_crown",
            CombatAttribute.AttackDamage, AttributeModifierOperation.Multiply, 1.08f);
        ExpectAttributes<CompiledRelicPlayerArmyTarget>(compiled, "item_soul_lantern",
            (CombatAttribute.MaxHealth, AttributeModifierOperation.Multiply, 1.06f),
            (CombatAttribute.AttackDamage, AttributeModifierOperation.Multiply, 1.06f));
        ExpectAttribute<CompiledRelicPlayerArmyTarget>(compiled, "item_war_drum",
            CombatAttribute.AttackDamage, AttributeModifierOperation.Multiply, 1.1f);
        if (compiled.Values.Any(definition => definition.BattleModifiers.Length != 0))
            throw new InvalidOperationException("production Relic still uses the legacy fixed-enum extension route");
        if (compiled["item_lone_crown"].AttributeBindings.Single().StackPolicy !=
                RelicAttributeStackPolicy.LinearAcrossStacksAndInstances ||
            compiled["item_clockwork_seed"].BattleStartEffects.Single().RepeatPolicy !=
                RelicBattleStartRepeatPolicy.OncePerBattleBinding ||
            compiled.Where(pair => pair.Key != "item_lone_crown")
                .SelectMany(pair => pair.Value.AttributeBindings)
                .Any(binding => binding.StackPolicy != RelicAttributeStackPolicy.PerStack) ||
            compiled.Where(pair => pair.Key != "item_clockwork_seed")
                .SelectMany(pair => pair.Value.BattleStartEffects)
                .Any(effect => effect.RepeatPolicy != RelicBattleStartRepeatPolicy.PerStack))
            throw new InvalidOperationException("production Relic stack/repeat policies changed");
        return compiled;
    }

    private static void ProductionBattleSimulationEquivalence(
        IReadOnlyDictionary<string, CompiledRelicDefinition> definitions)
    {
        var key = new RelicRunKey(0x12E11CUL, "production_relic_hero", 0, 0);
        using var run = new RelicRunScope(key);
        var registrations = definitions.Values
            .OrderBy(definition => definition.StableId, StringComparer.Ordinal)
            .Select((definition, index) => run.Activate(definition, State(
                $"production-relic-{index + 1}", definition.StableId, 1)))
            .ToArray();
        try
        {
            var summon = RelicUnit("soldier_void_mech", false, 100, 10, 1);
            using var battle = new BattleSimulation(new BattleConfig
            {
                Seed = key.Seed,
                Identity = new BattleIdentity("production-relic-equivalence", TowerNodeType.Combat,
                    key.Seed, key.FloorIndex, key.BattleNumber),
                FloorRule = new ClearFloorRuleRuntime("production-relic-equivalence", "生产遗物等价", "test"),
                HeroRule = NeutralHeroRule(),
                EmptyDeploymentSlots = 2,
                Relics = run.PrepareBattle(),
                RelicSummons = ImmutableDictionary<string, UnitSnapshot>.Empty
                    .Add("soldier_void_mech", summon),
                Spawns =
                [
                    new BattleSpawn(RelicUnit("production_hero", true, 100, 10, 20), 0,
                        new Vector2I(0, 0), "production-hero", IsPersistentRosterHero: true),
                    new BattleSpawn(RelicUnit("production_army_adjacent", false, 100, 10, 20), 0,
                        new Vector2I(1, 0), "production-army-adjacent", IsPersistentRosterHero: true),
                    new BattleSpawn(RelicUnit("production_army_far", false, 100, 10, 20), 0,
                        new Vector2I(0, 4), "production-army-far", IsPersistentRosterHero: true),
                    new BattleSpawn(RelicUnit("production_enemy", false, 1, 0, 1), 1,
                        new Vector2I(9, 5), "production-enemy")
                ]
            });

            var hero = battle.Units.Single(unit => unit.RuntimeId == "production-hero");
            var adjacentArmy = battle.Units.Single(unit => unit.RuntimeId == "production-army-adjacent");
            var farArmy = battle.Units.Single(unit => unit.RuntimeId == "production-army-far");
            var temporary = battle.Units.Single(unit => unit.IsTemporary);
            Near(hero.MaxHealth, 100f * 1.3f, "production Battle hero health");
            Near(hero.Damage, 10f * 1.25f * 1.16f * 1.08f,
                "production Battle hero damage, linear empty slots, adjacency");
            Near(hero.LifeSteal, .15f, "production Battle hero lifesteal");
            Near(hero.Armor, 4f, "production Battle hero adjacency armor");
            Near(hero.Shield, 15f, "production Battle hero start shield");

            var armyHealth = 100f * 1.12f * 1.15f * 1.06f;
            var armyDamage = 10f * 1.15f * 1.06f * 1.1f;
            Near(adjacentArmy.MaxHealth, armyHealth, "production Battle adjacent Army health");
            Near(adjacentArmy.Damage, armyDamage * 1.08f,
                "production Battle adjacent Army damage");
            Near(adjacentArmy.Armor, 4f, "production Battle adjacent Army armor");
            Near(adjacentArmy.Shield, 15f, "production Battle adjacent Army start shield");
            Near(farArmy.MaxHealth, armyHealth, "production Battle far Army health");
            Near(farArmy.Damage, armyDamage, "production Battle far Army damage");
            Near(farArmy.Armor, 0f, "production Battle far Army excludes adjacency");
            Near(farArmy.Shield, 15f, "production Battle far Army start shield");

            Near(temporary.MaxHealth, 100f * .85f, "production Battle Clockwork summon health");
            Near(temporary.Damage, 10f * .9f * 1.08f,
                "production Battle Clockwork summon dynamic adjacency damage");
            Near(temporary.Armor, 4f, "production Battle Clockwork summon dynamic adjacency armor");
            Near(temporary.Shield, 0f, "production Battle Clockwork summon excludes initial shield");
            if (temporary.Definition.ContentId != "soldier_void_mech" ||
                Math.Max(Math.Abs(temporary.Cell.X - hero.Cell.X), Math.Abs(temporary.Cell.Y - hero.Cell.Y)) > 1 ||
                battle.RelicModifierCount != 22)
                throw new InvalidOperationException(
                    "production Battle summon, dynamic adjacency, or typed Relic projection count changed");

            var result = battle.RunToEnd();
            if (result.Outcome != BattleOutcome.PlayerVictory ||
                result.RelicTransition is not
                {
                    Reason: RelicBattleCompletionReason.PlayerVictory,
                    GoldDelta: 3,
                    RemainingBattleInstances: 0,
                    RemainingCounters: 0,
                    RemainingSubscriptions: 0,
                    RemainingModifierHandles: 0
                } ||
                battle.RelicModifierCount != 0)
                throw new InvalidOperationException(
                    "production Battle Relic victory outcome or terminal cleanup changed");
        }
        finally
        {
            foreach (var registration in registrations) registration.Dispose();
        }
    }

    private static void ProductionStackedCompatibilityEquivalence(
        IReadOnlyDictionary<string, CompiledRelicDefinition> definitions)
    {
        float CrownDamage(params int[] stacks)
        {
            var key = new RelicRunKey(0xC20A1UL, "stacked_crown_hero", 0, stacks.Length);
            using var run = new RelicRunScope(key);
            var registrations = stacks.Select((count, index) => run.Activate(
                definitions["item_lone_crown"],
                State($"stacked-crown-{index}", "item_lone_crown", count))).ToArray();
            try
            {
                using var battle = new BattleSimulation(new BattleConfig
                {
                    Seed = key.Seed,
                    Identity = new BattleIdentity("stacked-crown", TowerNodeType.Combat,
                        key.Seed, key.FloorIndex, key.BattleNumber),
                    FloorRule = new ClearFloorRuleRuntime("stacked-crown", "孤王冠叠加", "test"),
                    HeroRule = NeutralHeroRule(),
                    EmptyDeploymentSlots = 2,
                    Relics = run.PrepareBattle(),
                    Spawns =
                    [
                        new BattleSpawn(RelicUnit("stacked_crown_hero", true, 100, 10, 20), 0,
                            new Vector2I(0, 0), "stacked-crown-hero", IsPersistentRosterHero: true),
                        new BattleSpawn(RelicUnit("stacked_crown_enemy", false, 100, 0, 1), 1,
                            new Vector2I(9, 5), "stacked-crown-enemy")
                    ]
                });
                return battle.Units.Single(unit => unit.RuntimeId == "stacked-crown-hero").Damage;
            }
            finally
            {
                foreach (var registration in registrations) registration.Dispose();
            }
        }

        (int Count, string Source) ClockworkSummons(params (string InstanceId, int Stacks)[] instances)
        {
            var key = new RelicRunKey(0xC10C0UL, "stacked_clockwork_hero", 0, instances.Length);
            using var run = new RelicRunScope(key);
            var registrations = instances.Select(instance => run.Activate(
                definitions["item_clockwork_seed"],
                State(instance.InstanceId, "item_clockwork_seed", instance.Stacks))).ToArray();
            try
            {
                using var battle = new BattleSimulation(new BattleConfig
                {
                    Seed = key.Seed,
                    Identity = new BattleIdentity("stacked-clockwork", TowerNodeType.Combat,
                        key.Seed, key.FloorIndex, key.BattleNumber),
                    FloorRule = new ClearFloorRuleRuntime("stacked-clockwork", "发条种子叠加", "test"),
                    HeroRule = NeutralHeroRule(),
                    Relics = run.PrepareBattle(),
                    RelicSummons = ImmutableDictionary<string, UnitSnapshot>.Empty.Add(
                        "soldier_void_mech", RelicUnit("soldier_void_mech", false, 100, 10, 1)),
                    Spawns =
                    [
                        new BattleSpawn(RelicUnit("stacked_clockwork_hero", true, 100, 10, 20), 0,
                            new Vector2I(0, 0), "stacked-clockwork-hero", IsPersistentRosterHero: true),
                        new BattleSpawn(RelicUnit("stacked_clockwork_enemy", false, 100, 0, 1), 1,
                            new Vector2I(9, 5), "stacked-clockwork-enemy")
                    ]
                });
                var summoned = battle.CombatEvents.Single(combatEvent =>
                    combatEvent.Kind == BattleCombatEventKind.UnitSummoned);
                return (
                    battle.Units.Count(unit => unit.IsTemporary && unit.Team == 0 &&
                        unit.Definition.ContentId == "soldier_void_mech"),
                    summoned.SourceRuntimeId);
            }
            finally
            {
                foreach (var registration in registrations) registration.Dispose();
            }
        }

        Near(CrownDamage(2), 13.2f, "stacked Lone Crown global linear multiplier");
        Near(CrownDamage(1, 1), 13.2f, "duplicate Lone Crown global linear multiplier");
        var stackedClockwork = ClockworkSummons(("clockwork-stacked-owner", 3));
        var duplicateClockwork = ClockworkSummons(
            ("clockwork-registration-first", 1),
            ("clockwork-lexical-first", 2));
        Equal(stackedClockwork.Count, 1, "stacked Clockwork Seed once-per-Battle summon");
        Equal(duplicateClockwork.Count, 1, "duplicate Clockwork Seed once-per-Battle summon");
        if (!stackedClockwork.Source.StartsWith("clockwork-stacked-owner:", StringComparison.Ordinal) ||
            !duplicateClockwork.Source.StartsWith("clockwork-registration-first:", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "once-per-Battle Relic binding did not use the stable first registered instance source");
    }

    private static void EmptySlotAdditiveCompatibility()
    {
        var authored = new RelicDefinition
        {
            StableId = "fixture_empty_slot_additive",
            AttributeBindings =
            [
                new RelicAttributeBindingSpec
                {
                    BindingId = "empty_slot_additive",
                    Target = new RelicPlayerEmptySlotHeroesTargetSpec(),
                    StackPolicy = RelicAttributeStackPolicy.LinearAcrossStacksAndInstances,
                    Modifier = new AttributeModifierSpec
                    {
                        Attribute = CombatAttribute.AttackDamage,
                        Operation = AttributeModifierOperation.Add,
                        Magnitude = new ConstantAttributeMagnitudeSpec { Value = 2 },
                        SlotId = "empty_slot_additive"
                    }
                }
            ]
        };
        var compilation = RelicDefinitionCompiler.Compile(authored);
        var definition = compilation.Definition ?? throw new InvalidOperationException(
            "empty-slot additive Relic fixture compile: " + string.Join("; ", compilation.Report.CoreErrors));

        float Damage(params int[] stacks)
        {
            var key = new RelicRunKey(0xADD17UL, "empty_slot_additive_hero", 0, stacks.Length);
            using var run = new RelicRunScope(key);
            var registrations = stacks.Select((count, index) => run.Activate(
                definition,
                State($"empty-slot-add-{index}", definition.StableId, count))).ToArray();
            try
            {
                using var battle = new BattleSimulation(new BattleConfig
                {
                    Seed = key.Seed,
                    Identity = new BattleIdentity("empty-slot-additive", TowerNodeType.Combat,
                        key.Seed, key.FloorIndex, key.BattleNumber),
                    FloorRule = new ClearFloorRuleRuntime("empty-slot-additive", "空位加法叠加", "test"),
                    HeroRule = NeutralHeroRule(),
                    EmptyDeploymentSlots = 3,
                    Relics = run.PrepareBattle(),
                    Spawns =
                    [
                        new BattleSpawn(RelicUnit("empty_slot_additive_hero", true, 100, 10, 20), 0,
                            new Vector2I(0, 0), "empty-slot-additive-hero", IsPersistentRosterHero: true),
                        new BattleSpawn(RelicUnit("empty_slot_additive_enemy", false, 100, 0, 1), 1,
                            new Vector2I(9, 5), "empty-slot-additive-enemy")
                    ]
                });
                return battle.Units.Single(unit => unit.RuntimeId == "empty-slot-additive-hero").Damage;
            }
            finally
            {
                foreach (var registration in registrations) registration.Dispose();
            }
        }

        Near(Damage(2), 22f, "stacked empty-slot additive linear scaling");
        Near(Damage(1, 1), 22f, "duplicate empty-slot additive linear scaling");
    }

    private static void PerStackPreservation(
        IReadOnlyDictionary<string, CompiledRelicDefinition> definitions)
    {
        var key = new RelicRunKey(0x5AACCUL, "per_stack_hero", 0, 0);
        using var run = new RelicRunScope(key);
        var states = new[]
        {
            State("per-stack-aegis", "item_aegis_standard", 2),
            State("per-stack-chalice", "item_blood_chalice", 2),
            State("per-stack-map", "item_commander_map", 2),
            State("per-stack-banner", "item_last_banner", 2),
            State("per-stack-gold", "item_gilded_contract", 2)
        };
        var registrations = states.Select(state => run.Activate(definitions[state.ContentId], state)).ToArray();
        try
        {
            using var battle = new BattleSimulation(new BattleConfig
            {
                Seed = key.Seed,
                Identity = new BattleIdentity("per-stack-preservation", TowerNodeType.Combat,
                    key.Seed, key.FloorIndex, key.BattleNumber),
                FloorRule = new ClearFloorRuleRuntime("per-stack-preservation", "逐层叠加保持", "test"),
                HeroRule = NeutralHeroRule(),
                Relics = run.PrepareBattle(),
                Spawns =
                [
                    new BattleSpawn(RelicUnit("per_stack_hero", true, 100, 10, 20), 0,
                        new Vector2I(0, 0), "per-stack-hero", IsPersistentRosterHero: true),
                    new BattleSpawn(RelicUnit("per_stack_army", false, 100, 10, 20), 0,
                        new Vector2I(1, 0), "per-stack-army", IsPersistentRosterHero: true),
                    new BattleSpawn(RelicUnit("per_stack_enemy", false, 1, 0, 1), 1,
                        new Vector2I(9, 5), "per-stack-enemy")
                ]
            });
            var hero = battle.Units.Single(unit => unit.RuntimeId == "per-stack-hero");
            var army = battle.Units.Single(unit => unit.RuntimeId == "per-stack-army");
            Near(hero.LifeSteal, .3f, "ordinary per-stack Add");
            Near(hero.Damage, 10f * MathF.Pow(1.08f, 2), "ordinary per-stack adjacent Multiply");
            Near(hero.Armor, 8f, "ordinary per-stack adjacent Add");
            Near(hero.Shield, 30f, "ordinary per-stack Battle-start shield");
            Near(army.MaxHealth, 100f * MathF.Pow(1.15f, 2), "ordinary per-stack health Multiply");
            Near(army.Damage, 10f * MathF.Pow(1.15f, 2) * MathF.Pow(1.08f, 2),
                "ordinary per-stack damage Multiply");
            Near(army.Armor, 8f, "ordinary per-stack Army adjacency Add");
            Near(army.Shield, 30f, "ordinary per-stack Army shield");
            var result = battle.RunToEnd();
            if (result.RelicTransition is not
                {
                    GoldDelta: 6,
                    RemainingModifierHandles: 0,
                    RemainingSubscriptions: 0
                } || battle.RelicModifierCount != 0)
                throw new InvalidOperationException(
                    "ordinary per-stack victory gold or terminal cleanup changed");
        }
        finally
        {
            foreach (var registration in registrations) registration.Dispose();
        }
    }

    private static void LinearProjectionCheckpointAndCleanup(CompiledRelicDefinition definition)
    {
        var key = new RelicRunKey(0x11EA2UL, "linear_checkpoint_hero", 0, 0);
        using var run = new RelicRunScope(key);
        using var first = run.Activate(
            definition,
            State("linear-registration-first", definition.StableId, 1));
        using var second = run.Activate(
            definition,
            State("linear-lexical-first", definition.StableId, 2));
        using var attributes = new BattleAttributeScope("linear-checkpoint-attributes");
        var attributeDefinition = new CompiledAttributeSetDefinition(
            [new CompiledAttributeDefinition(CombatAttribute.AttackDamage, 10, 0, 10000)],
            "linear-checkpoint-attributes");
        var hero = attributes.CreateSet("linear-checkpoint-hero", attributeDefinition);
        var units = ImmutableArray.Create(new RelicBattleUnitBinding(
            "linear-checkpoint-hero",
            0,
            true,
            false,
            true,
            true,
            new CombatCell(0, 0),
            hero));
        using var combat = new BattleCombatEventPipeline("linear-checkpoint-combat");
        using var relic = new RelicBattleScope(run.PrepareBattle());
        var bindings = new BattleCombatBindingRegistry(combat);
        relic.Activate(new RelicBattleRuntimeContext
        {
            CombatBindings = bindings,
            QueryUnits = () => units,
            ExecuteEffect = (_, _, _, _, _, _) => { },
            Summon = (_, _, _, _, _) => false,
            CurrentTick = () => 0,
            EmptyDeploymentSlots = 2
        });
        bindings.CloseRegistration();

        Near(hero.GetValue(CombatAttribute.AttackDamage), 14.8f,
            "linear group sums all registered instance stacks");
        if (relic.ModifierHandleCount != 1 || hero.ModifierCount != 1 ||
            relic.ModifierProjections is not [{ Source.InstanceId: var sourceInstance }] ||
            !sourceInstance.StartsWith("linear-registration-first:", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "linear Relic group did not retain one stable first-instance projection source");

        var attributeCheckpoint = attributes.CaptureState();
        var relicCheckpoint = relic.CaptureState();
        var transient = hero.ApplyModifier(
            new CompiledAttributeModifier(
                CombatAttribute.AttackDamage,
                AttributeModifierOperation.Add,
                new CompiledConstantMagnitude(1),
                100,
                "linear-checkpoint-transient"),
            CombatSourceRef.System("linear-checkpoint-transient"));
        relic.RestoreState(relicCheckpoint);
        attributes.RestoreState(attributeCheckpoint);
        var retried = hero.ApplyModifier(
            new CompiledAttributeModifier(
                CombatAttribute.AttackDamage,
                AttributeModifierOperation.Add,
                new CompiledConstantMagnitude(1),
                100,
                "linear-checkpoint-transient"),
            CombatSourceRef.System("linear-checkpoint-transient"));
        if (retried != transient || !hero.Remove(retried) || relic.ModifierHandleCount != 1 ||
            hero.ModifierCount != 1)
            throw new InvalidOperationException(
                "linear Relic rollback changed exact Attribute handle identity");

        var transition = relic.Complete(RelicBattleCompletionReason.Abort);
        if (transition.RemainingModifierHandles != 0 || hero.ModifierCount != 0)
            throw new InvalidOperationException("linear Relic cleanup retained its projection handle");
        Near(hero.GetValue(CombatAttribute.AttackDamage), 10f, "linear Relic cleanup rollback");
        combat.Complete(BattleCombatCompletionReason.Abort, 0);
        attributes.Complete(AttributeScopeCompletionReason.Abort, 0);
    }

    private static void ItemCompatibilityAuthorityRemoved()
    {
        var root = ProjectSettings.GlobalizePath("res://");
        foreach (var path in new[]
                 {
                     Path.Combine(root, "src", "Content", "ItemContentRoot.cs"),
                     Path.Combine(root, "src", "Content", "BindingContracts.cs"),
                     Path.Combine(root, "src", "Run", "RunRelicService.cs")
                 })
        {
            var source = File.ReadAllText(path);
            foreach (var forbidden in new[]
                     {
                         "RunModifierProviderComponent", "IRunModifierRegistry",
                         "AggregateCompatibilityModifiers", "Compatibility item content"
                     })
                if (source.Contains(forbidden, StringComparison.Ordinal))
                    throw new InvalidOperationException($"item compatibility authority remains in {Path.GetFileName(path)}: {forbidden}");
        }
        foreach (var obsolete in new[]
                 {
                     Path.Combine(root, "src", "Components", "RunModifierProviderComponent.cs"),
                     Path.Combine(root, "src", "Components", "ModifierProviderComponent.cs"),
                     Path.Combine(root, "scenes", "components", "ModifierProviderComponent.tscn")
                 })
            if (File.Exists(obsolete))
                throw new InvalidOperationException("obsolete item compatibility artifact remains: " + obsolete);
    }

    private static void RuntimeIsolationAndAggregation(IReadOnlyDictionary<string, CompiledRelicDefinition> definitions)
    {
        var runKey = new RelicRunKey(0xA11CEUL, "hero_contract", 4, 7);
        var stateA = State("isolated-a", "item_field_rations", 1, 2, 3);
        var stateB = State("isolated-b", "item_field_rations", 4, 5, 6);
        using var scopeA = new RelicRunScope(runKey);
        using var scopeB = new RelicRunScope(runKey);
        using var registrationA = scopeA.Activate(definitions[stateA.ContentId], stateA);
        using var registrationB = scopeB.Activate(definitions[stateB.ContentId], stateB);
        var preparationA = scopeA.PrepareBattle();
        var preparationB = scopeB.PrepareBattle();
        stateA.Stacks = 2;
        if (preparationA.Instances.Single().Stacks != 1 || preparationB.Instances.Single().Stacks != 4 ||
            preparationA.Instances.Single().Charges != 2 || preparationB.Instances.Single().Roll != 6)
            throw new InvalidOperationException("relic runtime instances or immutable battle snapshots leaked state");
        if (preparationA.Modifiers != new RelicBattleModifierSnapshot() ||
            preparationB.Modifiers != new RelicBattleModifierSnapshot())
            throw new InvalidOperationException("typed production Relic leaked through the legacy modifier adapter");

        using var aggregate = new RelicRunScope(runKey);
        var states = new[]
        {
            State("rations", "item_field_rations", 1),
            State("banner", "item_last_banner", 2),
            State("lantern", "item_soul_lantern", 4),
            State("drum", "item_war_drum", 2),
            State("mail", "item_crimson_mail", 2),
            State("seal", "item_duelist_seal", 4),
            State("chalice", "item_blood_chalice", 2),
            State("aegis", "item_aegis_standard", 4),
            State("crown", "item_lone_crown", 2),
            State("map", "item_commander_map", 4),
            State("clock-a", "item_clockwork_seed", 4),
            State("clock-b", "item_clockwork_seed", 1),
            State("gold-a", "item_gilded_contract", 2, 7, 11),
            State("gold-b", "item_gilded_contract", 4, 13, 17)
        };
        var registrations = states.Select(state => aggregate.Activate(definitions[state.ContentId], state)).ToArray();
        try
        {
            var preparation = aggregate.PrepareBattle();
            if (!preparation.Instances.Select(instance => instance.InstanceId).SequenceEqual(states.Select(state => state.InstanceId)))
                throw new InvalidOperationException("relic registration/list order was not preserved");
            if (preparation.Modifiers != new RelicBattleModifierSnapshot() ||
                preparation.Instances.Sum(instance => instance.Definition.AttributeBindings.Length) != 12 ||
                preparation.Instances.Sum(instance => instance.Definition.BattleStartEffects.Length) != 3)
                throw new InvalidOperationException("typed Relic preparation did not preserve independent declaration instances");

            using var battle = new RelicBattleScope(preparation);
            var transition = battle.Complete(RelicBattleCompletionReason.PlayerVictory);
            Equal(transition.GoldDelta, 18, "stacked duplicate victory gold");
            Equal(transition.Contributions.Length, 2, "duplicate relic contributions were merged");
            var apply = aggregate.Apply(transition);
            if (!apply.Succeeded || !apply.ProjectedInstances.Select(instance => instance.InstanceId)
                    .SequenceEqual(states.Select(state => state.InstanceId)))
                throw new InvalidOperationException("valid relic transition or persistent list order");
            var beforeRepeat = states.Select(Snapshot).ToArray();
            if (aggregate.Apply(transition).Succeeded || !states.Select(Snapshot).SequenceEqual(beforeRepeat))
                throw new InvalidOperationException("repeated transition mutated relic state");
        }
        finally
        {
            foreach (var registration in registrations) registration.Dispose();
        }
    }

    private static void CompilerAndPublicationFailures(
        IReadOnlyDictionary<string, CompiledRelicDefinition> definitions,
        ContentCatalog catalog)
    {
        var validIds = catalog.AllEntries().Select(entry => entry.StableId).ToHashSet(StringComparer.Ordinal);
        var collisionA = Authored("fixture_collision", RelicBattleModifierKind.StartBattleShield, 1);
        var collisionB = Authored("fixture_collision", RelicBattleModifierKind.StartBattleShield, 2);
        var collision = RelicDefinitionCompiler.CompileBatch([collisionA, collisionB], validIds);
        if (!collision.Report.HasCoreErrors || collision.Definitions.Length != 0)
            throw new InvalidOperationException("relic stable-id collision published a partial batch");

        ExpectCompileFailure(Authored("fixture_zero_multiplier", RelicBattleModifierKind.ArmyDamageMultiplier, 0), validIds, "multiplier");
        ExpectCompileFailure(Authored("fixture_nan", RelicBattleModifierKind.HeroLifeStealBonus, float.NaN), validIds, "finite");
        ExpectCompileFailure(Authored("fixture_fractional_shield", RelicBattleModifierKind.StartBattleShield, 1.5f), validIds, "integer");
        ExpectCompileFailure(Authored("fixture_unknown_summon", RelicBattleModifierKind.SummonToken, 1, "soldier_missing"), validIds, "unknown summon");
        ExpectCompileFailure(new RelicDefinition { StableId = "fixture_empty" }, validIds, "at least one");
        ExpectCompileFailure(new RelicDefinition
        {
            StableId = "fixture_null_modifiers",
            BattleModifiers = null!,
            VictoryOutcomes = [new RelicRunOutcomeSpec { Kind = RelicRunOutcomeKind.VictoryGold, Amount = 1 }]
        }, validIds, "Battle modifier collection is missing");
        ExpectCompileFailure(new RelicDefinition
        {
            StableId = "fixture_null_outcomes",
            BattleModifiers = [new RelicBattleModifierSpec
                { Kind = RelicBattleModifierKind.StartBattleShield, Amount = 1 }],
            VictoryOutcomes = null!
        }, validIds, "Victory outcome collection is missing");
        var invalidSource = ReactiveDefinition(
            "fixture_invalid_counter_source",
            new RelicReactiveCounterSpec
            {
                CounterId = "invalid_source",
                Scope = RelicCounterScope.Battle,
                ResetPolicy = RelicCounterResetPolicy.BattleEnd,
                Source = (RelicCounterSourceKind)999,
                Team = 0,
                Threshold = 1,
                Consumption = 1,
                Target = RelicThresholdTargetKind.EventSource,
                ThresholdEffect = ManualShieldSpec("invalid_source_effect")
            });
        ExpectCompileFailure(invalidSource, validIds, "counter source is invalid");
        var invalidSourceBatch = RelicDefinitionCompiler.CompileBatch(
            [invalidSource, Authored("fixture_valid_batch_peer", RelicBattleModifierKind.StartBattleShield, 1)],
            validIds);
        if (!invalidSourceBatch.Report.HasCoreErrors || invalidSourceBatch.Definitions.Length != 0)
            throw new InvalidOperationException("invalid Relic counter source published a partial batch");
        CompiledEffectFingerprintCompleteness(validIds);
        CompiledPolicyFingerprintSensitivity();
        CounterCanonicalRangeValidation();

        var invalidBattleReset = ReactiveDefinition(
            "fixture_invalid_battle_reset",
            new RelicReactiveCounterSpec
            {
                CounterId = "battle_counter",
                Scope = RelicCounterScope.Battle,
                ResetPolicy = RelicCounterResetPolicy.RunEnd,
                Source = RelicCounterSourceKind.Attack,
                Team = 0,
                Threshold = 1,
                Consumption = 1,
                Target = RelicThresholdTargetKind.EventSource,
                ThresholdEffect = ManualShieldSpec("battle_reset_effect")
            });
        ExpectCompileFailure(invalidBattleReset, validIds, "Battle counter must reset");
        var invalidRunReset = ReactiveDefinition(
            "fixture_invalid_run_reset",
            new RelicReactiveCounterSpec
            {
                CounterId = "run_counter",
                Scope = RelicCounterScope.Run,
                ResetPolicy = RelicCounterResetPolicy.BattleEnd,
                Source = RelicCounterSourceKind.Attack,
                Team = 0,
                Threshold = 1,
                Consumption = 1,
                Target = RelicThresholdTargetKind.EventSource,
                ThresholdEffect = ManualShieldSpec("run_reset_effect")
            });
        ExpectCompileFailure(invalidRunReset, validIds, "Run counter must reset");
        var bindingCollision = ReactiveDefinition(
            "fixture_binding_collision",
            new RelicReactiveCounterSpec
            {
                CounterId = "shared_binding",
                Scope = RelicCounterScope.Battle,
                ResetPolicy = RelicCounterResetPolicy.BattleEnd,
                Source = RelicCounterSourceKind.Attack,
                Team = 0,
                Threshold = 1,
                Consumption = 1,
                Target = RelicThresholdTargetKind.EventSource,
                ThresholdEffect = ManualShieldSpec("collision_effect")
            });
        bindingCollision.BattleStartEffects =
        [
            new RelicBattleStartShieldSpec { BindingId = "shared_binding", Amount = 1 }
        ];
        ExpectCompileFailure(bindingCollision, validIds, "conflicts with another Relic binding id");

        var authoredA = Authored("fixture_a", RelicBattleModifierKind.StartBattleShield, 1);
        var authoredB = Authored("fixture_b", RelicBattleModifierKind.StartBattleShield, 1);
        var graphIds = new HashSet<string>(["fixture_a", "fixture_b"], StringComparer.Ordinal);
        var orphan = ContentValidator.ValidateRelicGraph(
            [authoredA, authoredB],
            [("fixture_a", authoredA)],
            graphIds);
        ExpectReport(orphan, "Orphan relic definition", "orphan relic graph");
        var unregistered = ContentValidator.ValidateRelicGraph(
            [authoredA],
            [("fixture_a", authoredA), ("fixture_b", authoredB)],
            graphIds);
        ExpectReport(unregistered, "unregistered relic definition", "unregistered relic graph");

        if (definitions.Values.Any(definition => definition.StableId.Length == 0))
            throw new InvalidOperationException("compiled production relic lost stable identity");
    }

    private static void CompiledEffectFingerprintCompleteness(IReadOnlySet<string> validIds)
    {
        string CompiledRelicFingerprint(EffectBindingSpec effect)
        {
            var counter = Counter(
                "compiled_fingerprint_counter",
                RelicCounterScope.Battle,
                RelicCounterResetPolicy.BattleEnd,
                RelicCounterSourceKind.Attack,
                0,
                1,
                1,
                RelicThresholdTargetKind.EventSource,
                0);
            counter.ThresholdEffect = effect;
            var result = RelicDefinitionCompiler.Compile(
                ReactiveDefinition("fixture_compiled_effect_fingerprint", counter),
                validIds);
            return result.Definition?.Fingerprint ?? throw new InvalidOperationException(
                "valid compiled Relic fingerprint fixture failed: " + string.Join("; ", result.Report.CoreErrors));
        }

        var authoredBaseline = ManualShieldSpec("compiled_fingerprint_effect");
        var authoredCondition = ManualShieldSpec("compiled_fingerprint_effect");
        authoredCondition.Conditions =
        [
            new EntityAliveConditionSpec
                { Entity = EffectEntityReference.ExplicitTarget, ExpectedAlive = true }
        ];
        var authoredLimits = ManualShieldSpec("compiled_fingerprint_effect");
        authoredLimits.Limits.MaxUses = 2;
        authoredLimits.Limits.MinimumIntervalTicks = 3;
        authoredLimits.Limits.MaxDepth = 4;
        authoredLimits.Limits.MaxRepeatedEdges = 5;
        var authoredPresentation = ManualShieldSpec("compiled_fingerprint_effect");
        authoredPresentation.Presentation = new EffectPresentationSpec
        {
            DisplayName = "编译指纹",
            ReportLabel = "指纹报告",
            Cue = "fingerprint_cue"
        };
        var authoredFingerprint = CompiledRelicFingerprint(authoredBaseline);
        if (CompiledRelicFingerprint(authoredCondition) == authoredFingerprint ||
            CompiledRelicFingerprint(authoredLimits) == authoredFingerprint ||
            CompiledRelicFingerprint(authoredPresentation) == authoredFingerprint)
            throw new InvalidOperationException(
                "compiled Relic fingerprint omitted Effect conditions, limits, or presentation");

        var method = typeof(RelicDefinitionCompiler).GetMethod(
            "Effect",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(CompiledEffectBinding)],
            modifiers: null) ?? throw new InvalidOperationException(
            "Relic Effect fingerprint canonicalizer is missing");
        string Canonical(CompiledEffectBinding binding) =>
            method.Invoke(null, [binding]) as string ?? throw new InvalidOperationException(
                "Relic Effect fingerprint canonicalizer returned no value");
        void Different(string label, CompiledEffectBinding first, CompiledEffectBinding second)
        {
            if (Canonical(first) == Canonical(second))
                throw new InvalidOperationException("Relic Effect fingerprint omitted " + label);
        }

        var baseline = new CompiledEffectBinding(
            "compiled_fingerprint_effect",
            7,
            new CompiledEffectTrigger(EffectTriggerKind.DomainEvent, EffectDomainEventKind.DamageResolved),
            [new CompiledEntityAliveCondition(EffectEntityReference.Source, true)],
            new CompiledRelativeTeamTargetQuery(EffectRelativeTeam.Allies, false, "guard"),
            [new CompiledEffectStep(EffectKind.Shield, EffectAmountSource.Fixed, 1.25f)],
            new CompiledEffectBindingLimits(2, 3, 4, 5),
            new CompiledEffectPresentation("显示", "报告", "cue"));

        Different("trigger kind", baseline,
            baseline with { Trigger = baseline.Trigger with { Kind = EffectTriggerKind.Manual } });
        Different("trigger event kind", baseline,
            baseline with { Trigger = baseline.Trigger with { EventKind = EffectDomainEventKind.HealingResolved } });
        Different("condition entity", baseline,
            baseline with
            {
                Conditions = [new CompiledEntityAliveCondition(EffectEntityReference.Owner, true)]
            });
        Different("condition expected-alive", baseline,
            baseline with
            {
                Conditions = [new CompiledEntityAliveCondition(EffectEntityReference.Source, false)]
            });
        Different("condition order/count", baseline,
            baseline with
            {
                Conditions =
                [
                    new CompiledEntityAliveCondition(EffectEntityReference.Source, true),
                    new CompiledEntityAliveCondition(EffectEntityReference.Owner, false)
                ]
            });
        Different("target type", baseline,
            baseline with { TargetQuery = new CompiledExplicitTargetQuery() });
        Different("relative target team", baseline,
            baseline with
            {
                TargetQuery = new CompiledRelativeTeamTargetQuery(EffectRelativeTeam.Enemies, false, "guard")
            });
        Different("relative target defeated policy", baseline,
            baseline with
            {
                TargetQuery = new CompiledRelativeTeamTargetQuery(EffectRelativeTeam.Allies, true, "guard")
            });
        Different("relative target required tag", baseline,
            baseline with
            {
                TargetQuery = new CompiledRelativeTeamTargetQuery(EffectRelativeTeam.Allies, false, "ward")
            });
        Different("effect step kind", baseline,
            baseline with
            {
                Effects = [new CompiledEffectStep(EffectKind.Heal, EffectAmountSource.Fixed, 1.25f)]
            });
        Different("effect step amount source", baseline,
            baseline with
            {
                Effects = [new CompiledEffectStep(EffectKind.Shield, EffectAmountSource.InvocationValue, 1.25f)]
            });
        Different("effect step amount", baseline,
            baseline with
            {
                Effects = [new CompiledEffectStep(EffectKind.Shield, EffectAmountSource.Fixed, 1.5f)]
            });
        Different("effect step order/count", baseline,
            baseline with
            {
                Effects =
                [
                    new CompiledEffectStep(EffectKind.Shield, EffectAmountSource.Fixed, 1.25f),
                    new CompiledEffectStep(EffectKind.Heal, EffectAmountSource.Fixed, 2f)
                ]
            });
        Different("max-use limit", baseline, baseline with { Limits = baseline.Limits with { MaxUses = 6 } });
        Different("minimum-interval limit", baseline,
            baseline with { Limits = baseline.Limits with { MinimumIntervalTicks = 6 } });
        Different("max-depth limit", baseline, baseline with { Limits = baseline.Limits with { MaxDepth = 6 } });
        Different("repeated-edge limit", baseline,
            baseline with { Limits = baseline.Limits with { MaxRepeatedEdges = 6 } });
        Different("presentation presence", baseline, baseline with { Presentation = null });
        Different("presentation display name", baseline,
            baseline with { Presentation = baseline.Presentation! with { DisplayName = "变更" } });
        Different("presentation report label", baseline,
            baseline with { Presentation = baseline.Presentation! with { ReportLabel = "变更" } });
        Different("presentation cue", baseline,
            baseline with { Presentation = baseline.Presentation! with { Cue = "changed" } });

        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var french = Canonical(baseline);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            if (Canonical(baseline) != french)
                throw new InvalidOperationException("Relic Effect fingerprint depends on current culture");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        ExpectCanonicalFailure(
            () => Canonical(baseline with { Conditions = [new UnsupportedCompiledEffectCondition()] }),
            "condition");
        ExpectCanonicalFailure(
            () => Canonical(baseline with { TargetQuery = new UnsupportedCompiledEffectTargetQuery() }),
            "target");
    }

    private static void ExpectCanonicalFailure(Action action, string label)
    {
        try
        {
            action();
        }
        catch (TargetInvocationException exception) when (exception.InnerException is InvalidOperationException)
        {
            return;
        }
        catch (InvalidOperationException)
        {
            return;
        }
        throw new InvalidOperationException("Relic Effect fingerprint accepted an unsupported " + label + " type");
    }

    private sealed record UnsupportedCompiledEffectCondition : CompiledEffectCondition;
    private sealed record UnsupportedCompiledEffectTargetQuery : CompiledEffectTargetQuery;

    private static void CompiledPolicyFingerprintSensitivity()
    {
        var method = typeof(RelicDefinitionCompiler).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name == "Fingerprint" && candidate.GetParameters().Length == 6);
        var modifier = new CompiledAttributeModifier(
            CombatAttribute.AttackDamage,
            AttributeModifierOperation.Multiply,
            new CompiledConstantMagnitude(1.08f),
            0,
            "compiled_policy_modifier");
        var perStack = new CompiledRelicAttributeBinding(
            "compiled_policy_attribute",
            new CompiledRelicPlayerEmptySlotHeroesTarget(),
            RelicAttributeStackPolicy.PerStack,
            modifier);
        var linear = perStack with
        {
            StackPolicy = RelicAttributeStackPolicy.LinearAcrossStacksAndInstances
        };
        var perStart = new CompiledRelicBattleStartSummon(
            "compiled_policy_start",
            RelicBattleStartRepeatPolicy.PerStack,
            "fixture_summon",
            1,
            1);
        var onceStart = perStart with
        {
            RepeatPolicy = RelicBattleStartRepeatPolicy.OncePerBattleBinding
        };

        string Fingerprint(
            ImmutableArray<CompiledRelicAttributeBinding> attributes,
            ImmutableArray<CompiledRelicBattleStartEffect> starts) =>
            method.Invoke(null,
            [
                "fixture_compiled_policy",
                attributes,
                starts,
                ImmutableArray<CompiledRelicBattleModifier>.Empty,
                ImmutableArray<CompiledRelicReactiveCounter>.Empty,
                ImmutableArray<CompiledRelicRunOutcome>.Empty
            ]) as string ?? throw new InvalidOperationException(
                "compiled Relic policy fingerprint returned no value");

        var baseline = Fingerprint([perStack], [perStart]);
        if (Fingerprint([linear], [perStart]) == baseline ||
            Fingerprint([perStack], [onceStart]) == baseline)
            throw new InvalidOperationException(
                "compiled Relic fingerprint omitted stack or repeat policy");
    }

    private static void CounterCanonicalRangeValidation()
    {
        var authored = ReactiveDefinition(
            "fixture_counter_canonical_range",
            Counter(
                "run_range",
                RelicCounterScope.Run,
                RelicCounterResetPolicy.RunEnd,
                RelicCounterSourceKind.Attack,
                0,
                3,
                1,
                RelicThresholdTargetKind.EventSource,
                0));
        var compilation = RelicDefinitionCompiler.Compile(authored);
        var definition = compilation.Definition ?? throw new InvalidOperationException(
            "canonical counter-range fixture compile: " + string.Join("; ", compilation.Report.CoreErrors));
        var key = new RelicRunKey(0xCA110UL, "counter_range_hero", 0, 0);

        foreach (var invalidValue in new[] { 3, int.MaxValue })
        {
            using var invalidRun = new RelicRunScope(key);
            var state = ReactiveState("counter-range", definition.StableId, definition);
            state.Counters[0] = state.Counters[0] with { Value = invalidValue };
            ExpectThrows(
                () => invalidRun.Activate(definition, state),
                $"Run counter canonical range {invalidValue}");
        }

        using var validRun = new RelicRunScope(key);
        using var registration = validRun.Activate(
            definition,
            ReactiveState("counter-range", definition.StableId, definition));
        var validPreparation = validRun.PrepareBattle();
        foreach (var invalidValue in new[] { 3, int.MaxValue })
        {
            var instances = validPreparation.Instances.Select(instance => instance with
            {
                Counters = instance.Counters.Select(counter =>
                    counter with { Value = invalidValue }).ToImmutableArray()
            }).ToImmutableArray();
            var runProjection = instances.Select(instance => new RelicRunInstanceSnapshot(
                instance.InstanceId,
                instance.ContentId,
                instance.Stacks,
                instance.Charges,
                instance.Roll,
                instance.Counters)).ToImmutableArray();
            var fingerprint = RelicRunScope.Fingerprint(runProjection);
            var preparation = validPreparation with
            {
                TransitionId = RelicRunScope.TransitionIdentity(key, fingerprint),
                SourceFingerprint = fingerprint,
                Instances = instances
            };
            ExpectThrows(
                () => new RelicBattleScope(preparation).Dispose(),
                $"Battle preparation Run counter canonical range {invalidValue}");
        }
    }

    private static void ReactiveCounterRuntimeAndPersistence(ContentRegistry registry)
    {
        var authored = ReactiveDefinition(
            "fixture_reactive_runtime",
            Counter("population", RelicCounterScope.Battle, RelicCounterResetPolicy.BattleEnd,
                RelicCounterSourceKind.Population, 0, 2, 2, RelicThresholdTargetKind.FirstAliveTeamUnit, 0),
            Counter("alive", RelicCounterScope.Run, RelicCounterResetPolicy.RunEnd,
                RelicCounterSourceKind.Alive, 0, 99, 1, RelicThresholdTargetKind.FirstAliveTeamUnit, 0),
            Counter("attacks", RelicCounterScope.Run, RelicCounterResetPolicy.RunEnd,
                RelicCounterSourceKind.Attack, 0, 2, 2, RelicThresholdTargetKind.EventSource, 0),
            Counter("deaths", RelicCounterScope.Battle, RelicCounterResetPolicy.BattleEnd,
                RelicCounterSourceKind.Death, 1, 1, 1, RelicThresholdTargetKind.FirstAliveTeamUnit, 0));
        var compiled = RelicDefinitionCompiler.Compile(authored).Definition ??
            throw new InvalidOperationException("Reactive runtime fixture compile failed");
        var key = new RelicRunKey(0xC017UL, "hero_reactive", 1, 2);
        var stateA = ReactiveState("reactive-a", compiled.StableId, compiled);
        var stateB = ReactiveState("reactive-b", compiled.StableId, compiled);
        using var run = new RelicRunScope(key);
        using var registrationA = run.Activate(compiled, stateA);
        using var registrationB = run.Activate(compiled, stateB);
        var preparation = run.PrepareBattle();

        using var attributes = new BattleAttributeScope("reactive-attributes");
        var attributeDefinition = new CompiledAttributeSetDefinition(
            [new CompiledAttributeDefinition(CombatAttribute.MaxHealth, 100, 0, 10000)],
            "reactive-attributes");
        var units = ImmutableArray.Create(
            new RelicBattleUnitBinding("ally-a", 0, true, false, true, true, new CombatCell(0, 0),
                attributes.CreateSet("ally-a", attributeDefinition)),
            new RelicBattleUnitBinding("ally-b", 0, false, false, true, true, new CombatCell(1, 0),
                attributes.CreateSet("ally-b", attributeDefinition)),
            new RelicBattleUnitBinding("enemy", 1, false, false, true, true, new CombatCell(2, 0),
                attributes.CreateSet("enemy", attributeDefinition)));
        using var combat = new BattleCombatEventPipeline("reactive-combat");
        using var battle = new RelicBattleScope(preparation);
        var bindings = new BattleCombatBindingRegistry(combat);
        var effects = new List<string>();
        battle.Activate(new RelicBattleRuntimeContext
        {
            CombatBindings = bindings,
            QueryUnits = () => units,
            ExecuteEffect = (_, _, owner, target, _, _) => effects.Add($"{owner}->{target}"),
            Summon = (_, _, _, _, _) => false,
            CurrentTick = () => 0
        });
        bindings.CloseRegistration();

        Publish(combat, new BattleCombatEventDraft(
            BattleCombatEventKind.BattleStarted,
            CombatSourceRef.System("battle"),
            string.Empty,
            string.Empty,
            0));
        for (var tick = 1; tick <= 2; tick++)
            Publish(combat, new BattleCombatEventDraft(
                BattleCombatEventKind.AttackLanded,
                CombatSourceRef.Unit("hero", "ally-a", "ally-a"),
                "ally-a",
                "enemy",
                tick));
        Publish(combat, new BattleCombatEventDraft(
            BattleCombatEventKind.UnitDefeated,
            CombatSourceRef.Unit("hero", "ally-a", "ally-a"),
            "ally-a",
            "enemy",
            3));

        if (battle.CounterValue("reactive-a", "alive") != 2 ||
            battle.CounterValue("reactive-b", "alive") != 2 ||
            battle.CounterValue("reactive-a", "attacks") != 0 ||
            battle.CounterValue("reactive-b", "attacks") != 0 ||
            battle.CounterTransitions.Count != 10 || effects.Count != 6 ||
            !effects.Any(effect => effect.StartsWith("reactive-a->", StringComparison.Ordinal)) ||
            !effects.Any(effect => effect.StartsWith("reactive-b->", StringComparison.Ordinal)))
            throw new InvalidOperationException("typed Reactive Relic counters were not isolated or deterministic");

        var transition = battle.Complete(RelicBattleCompletionReason.PlayerVictory);
        if (transition.RemainingBattleInstances != 0 || transition.RemainingCounters != 0 ||
            transition.RemainingSubscriptions != 0 || transition.RemainingModifierHandles != 0 ||
            transition.ProjectedInstances.Any(instance =>
                instance.Counters.Length != 2 ||
                instance.Counters.Single(counter => counter.CounterId == "alive").Value != 2 ||
                instance.Counters.Single(counter => counter.CounterId == "attacks").Value != 0))
            throw new InvalidOperationException("Reactive Relic completion did not reset Battle state or project Run counters");
        combat.Complete(BattleCombatCompletionReason.PlayerVictory, 3);
        attributes.Complete(AttributeScopeCompletionReason.BattleCompleted, 3);
        var applied = run.Apply(transition);
        if (!applied.Succeeded || stateA.Counters.Single(counter => counter.CounterId == "alive").Value != 2 ||
            stateB.Counters.Single(counter => counter.CounterId == "alive").Value != 2)
            throw new InvalidOperationException("authenticated Reactive Relic Run counters did not apply");

        var dto = new ItemInstanceDto
        {
            InstanceId = "reactive-dto",
            ContentId = compiled.StableId,
            Counters = stateA.Counters.Select(counter => new RelicCounterStateDto
            {
                CounterId = counter.CounterId,
                Value = counter.Value
            }).ToList()
        };
        var json = JsonSerializer.Serialize(dto);
        var roundTrip = JsonSerializer.Deserialize<ItemInstanceDto>(json) ??
            throw new InvalidOperationException("Reactive Relic DTO round-trip returned null");
        if (!RelicRunScope.HasExactRunCounterSet(compiled,
                roundTrip.Counters.Select(counter => new RelicCounterStateSnapshot(counter.CounterId, counter.Value)), out _))
            throw new InvalidOperationException("schema-v4 Reactive Relic counter round-trip changed valid state");
        IEnumerable<RelicCounterStateSnapshot>?[] invalidSets =
        [
            null,
            [new RelicCounterStateSnapshot("alive", 0), new RelicCounterStateSnapshot("alive", 1)],
            [new RelicCounterStateSnapshot("unknown", 0), new RelicCounterStateSnapshot("attacks", 0)],
            [new RelicCounterStateSnapshot("alive", -1), new RelicCounterStateSnapshot("attacks", 0)],
            [new RelicCounterStateSnapshot("population", 0), new RelicCounterStateSnapshot("attacks", 0)]
        ];
        foreach (var invalid in invalidSets)
            if (RelicRunScope.HasExactRunCounterSet(compiled, invalid, out _))
                throw new InvalidOperationException("schema-v4 accepted an invalid persisted Relic counter collection");

        _ = registry;
    }

    private static void TransitionValidationAndLifecycle(IReadOnlyDictionary<string, CompiledRelicDefinition> definitions)
    {
        var key = new RelicRunKey(91, "hero_guard", 3, 5);
        var state = State("guard-item", "item_gilded_contract", 2, 4, 8);
        using var run = new RelicRunScope(key);
        using var registration = run.Activate(definitions[state.ContentId], state);
        var preparation = run.PrepareBattle();
        var forgedFingerprint = new string('f', 64);
        var forgedPreparation = preparation with
        {
            SourceFingerprint = forgedFingerprint,
            TransitionId = RelicRunScope.TransitionIdentity(key, forgedFingerprint)
        };
        try
        {
            using var accepted = new RelicBattleScope(forgedPreparation);
            throw new InvalidOperationException("battle scope accepted a fingerprint not derived from its instance projection");
        }
        catch (ArgumentException)
        {
            // Expected: Battle authenticates the complete preparation before owning instances.
        }
        using var battle = new RelicBattleScope(preparation);
        var valid = battle.Complete(RelicBattleCompletionReason.PlayerVictory);
        var baseline = Snapshot(state);
        ExpectApplyFailure(run, valid with { RunKey = key with { Seed = key.Seed + 1 } }, state, baseline, "wrong run");
        ExpectApplyFailure(run, valid with { RunKey = key with { FloorIndex = key.FloorIndex + 1 } }, state, baseline, "wrong floor");
        ExpectApplyFailure(run, valid with { RunKey = key with { BattleNumber = key.BattleNumber + 1 } }, state, baseline, "wrong battle");
        ExpectApplyFailure(run, valid with { SourceFingerprint = "wrong" }, state, baseline, "wrong fingerprint");
        ExpectApplyFailure(run, valid with { GoldDelta = valid.GoldDelta + 99 }, state, baseline, "forged outcome");
        ExpectApplyFailure(run, valid with
        {
            ProjectedInstances = [valid.ProjectedInstances[0] with { Charges = valid.ProjectedInstances[0].Charges + 1 }]
        }, state, baseline, "forged projection");
        if (!run.Apply(valid).Succeeded) throw new InvalidOperationException("valid transition was rejected after guard failures");

        foreach (var reason in Enum.GetValues<RelicBattleCompletionReason>().Where(reason => reason != RelicBattleCompletionReason.None))
        {
            using var lifecycleRun = new RelicRunScope(key);
            using var lifecycleRegistration = lifecycleRun.Activate(
                definitions["item_aegis_standard"],
                State("lifecycle-" + reason, "item_aegis_standard", 1));
            using var lifecycleBattle = new RelicBattleScope(lifecycleRun.PrepareBattle());
            var transition = lifecycleBattle.Complete(reason);
            if (transition.RemainingBattleInstances != 0 || lifecycleBattle.LiveBattleInstanceCount != 0 ||
                !ReferenceEquals(transition, lifecycleBattle.Complete(reason)))
                throw new InvalidOperationException($"relic battle lifecycle did not clean idempotently: {reason}");
            if (!lifecycleRun.Validate(transition, reason).Succeeded)
                throw new InvalidOperationException($"valid relic completion projection was rejected: {reason}");
            if (reason != RelicBattleCompletionReason.PlayerVictory &&
                (lifecycleRun.Validate(transition with { GoldDelta = 1 }, reason).Succeeded ||
                 lifecycleRun.Validate(transition with
                 {
                     Contributions = [new RelicRunOutcomeContribution(
                         "forged", "item_aegis_standard", RelicRunOutcomeKind.VictoryGold, 1)]
                 }, reason).Succeeded))
                throw new InvalidOperationException($"non-victory relic transition accepted forged outcomes: {reason}");
        }

        var disposableRun = new RelicRunScope(key);
        disposableRun.Activate(definitions["item_aegis_standard"], State("run-dispose", "item_aegis_standard", 1));
        disposableRun.Dispose();
        if (disposableRun.LiveRunInstanceCount != 0 || disposableRun.Transition?.Reason != RelicRunCompletionReason.Disposal)
            throw new InvalidOperationException("relic run disposal retained mutable instances");
        using var disposableBattle = new RelicBattleScope(preparation);
        disposableBattle.Dispose();
        if (disposableBattle.LiveBattleInstanceCount != 0 || disposableBattle.Transition?.Reason != RelicBattleCompletionReason.Disposal)
            throw new InvalidOperationException("relic battle disposal retained mutable instances");
    }

    private static void RunApplicationExactlyOnce(ContentRegistry registry)
    {
        var save = new TransactionalRunSaveService(registry.Catalog.Heroes.Select(entry => entry.StableId));
        var app = new RunApplication(registry, save, TestProjectFixture.Load(registry));
        var heroId = registry.Catalog.Heroes[0].StableId;
        if (!app.StartNewRun(heroId, 0xBEEFUL) || !app.GrantItem("item_gilded_contract"))
            throw new InvalidOperationException("exactly-once run setup");
        var active = app.ActiveRun ?? throw new InvalidOperationException("exactly-once active run");
        var item = active.Items.Single(value => value.ContentId == "item_gilded_contract");
        item.Stacks = 2;
        item.Charges = 5;
        item.Roll = 13;
        var node = app.CurrentOptions().First(option => option.Type is TowerNodeType.Combat or TowerNodeType.Elite or TowerNodeType.Boss);
        if (!app.SelectNode(node.Type)) throw new InvalidOperationException("exactly-once select combat node");
        var encounter = app.CurrentEncounter();
        var config = app.BuildBattleConfig(encounter);
        using var relicBattle = new RelicBattleScope(config.Relics ?? throw new InvalidOperationException("missing run relic preparation"));
        var relicTransition = relicBattle.Complete(RelicBattleCompletionReason.PlayerVictory);
        var result = new BattleResult(BattleOutcome.PlayerVictory, 1, "relic-exactly-once", [], 0,
            RelicTransition: relicTransition, Identity: config.Identity);

        var before = save.Serialize(active);
        var savesBeforeGuards = save.ActiveRunSaveCalls;
        var wrongEncounter = encounter with { IsBoss = !encounter.IsBoss };
        if (app.CompleteBattle(result, wrongEncounter) || save.Serialize(active) != before || save.ActiveRunSaveCalls != savesBeforeGuards)
            throw new InvalidOperationException("encounter source guard mutated Run state");
        var wrongKeyResult = result with
        {
            RelicTransition = relicTransition with { RunKey = relicTransition.RunKey with { BattleNumber = relicTransition.RunKey.BattleNumber + 1 } }
        };
        if (app.CompleteBattle(wrongKeyResult, encounter) || save.Serialize(active) != before || save.ActiveRunSaveCalls != savesBeforeGuards)
            throw new InvalidOperationException("RunApplication accepted a stale battle key");

        save.FailNextActiveRunSave = true;
        if (app.CompleteBattle(result, encounter)) throw new InvalidOperationException("failed save reported battle completion success");
        if (!ReferenceEquals(active, app.ActiveRun) || save.Serialize(active) != before ||
            active.Gold != 16 || active.BattleNumber != 0 || active.FloorIndex != 0 ||
            item.Stacks != 2 || item.Charges != 5 || item.Roll != 13)
            throw new InvalidOperationException("failed battle save mutated the authoritative in-memory Run");

        var expectedReward = encounter.IsBoss ? 18 : encounter.IsElite ? 12 : 7;
        var expectedGold = 16 + expectedReward + config.HeroRule.BattleGoldBonus + 6;
        if (!app.CompleteBattle(result, encounter)) throw new InvalidOperationException("same relic transition did not retry after save recovery");
        if (!ReferenceEquals(active, app.ActiveRun) || active.Gold != expectedGold || active.BattleNumber != 1 ||
            active.FloorIndex != 1 || active.PendingNode || active.Items.Single().Stacks != 2 ||
            active.Items.Single().Charges != 5 || active.Items.Single().Roll != 13)
            throw new InvalidOperationException("successful retry did not apply exactly one complete Run projection");
        var after = save.Serialize(active);
        var savesAfterSuccess = save.ActiveRunSaveCalls;
        if (app.CompleteBattle(result, encounter) || save.Serialize(active) != after || save.ActiveRunSaveCalls != savesAfterSuccess)
            throw new InvalidOperationException("stale relic transition reapplied after successful persistence");
        var persisted = save.LoadActiveRun() ?? throw new InvalidOperationException("successful retry was not persisted");
        var persistedItem = persisted.Items.Single();
        if (persistedItem.InstanceId != item.InstanceId || persistedItem.ContentId != item.ContentId ||
            persistedItem.Stacks != 2 || persistedItem.Charges != 5 || persistedItem.Roll != 13)
            throw new InvalidOperationException("current save projection lost authoritative relic state");
        Equal(persisted.Version, ActiveRunFormationSchema.CurrentVersion,
            "relic persistence changed the current Run schema");
    }

    private static void RunApplicationFailureTransitionGuards(ContentRegistry registry)
    {
        var save = new TransactionalRunSaveService(registry.Catalog.Heroes.Select(entry => entry.StableId));
        var app = new RunApplication(registry, save, TestProjectFixture.Load(registry));
        var heroId = registry.Catalog.Heroes[0].StableId;
        if (!app.StartNewRun(heroId, 0xFA11UL)) throw new InvalidOperationException("failure-guard run setup");
        var active = app.ActiveRun ?? throw new InvalidOperationException("failure-guard active run");
        var node = app.CurrentOptions().First(option => option.Type is TowerNodeType.Combat or TowerNodeType.Elite or TowerNodeType.Boss);
        if (!app.SelectNode(node.Type)) throw new InvalidOperationException("failure-guard node selection");
        var encounter = app.CurrentEncounter();
        var config = app.BuildBattleConfig(encounter);
        var before = save.Serialize(active);
        var deletesBefore = save.ActiveRunDeleteCalls;

        var missingTransition = new BattleResult(BattleOutcome.PlayerDefeat, 1, "missing-transition", [], 0,
            Identity: config.Identity);
        if (app.CompleteBattle(missingTransition, encounter) || !ReferenceEquals(active, app.ActiveRun) ||
            save.Serialize(active) != before || save.ActiveRunDeleteCalls != deletesBefore)
            throw new InvalidOperationException("missing defeat transition deleted or mutated the active Run");

        using var defeatBattle = new RelicBattleScope(config.Relics ?? throw new InvalidOperationException("failure-guard relic preparation"));
        var defeatTransition = defeatBattle.Complete(RelicBattleCompletionReason.PlayerDefeat);
        var forgedDefeat = new BattleResult(BattleOutcome.PlayerDefeat, 1, "forged-defeat", [], 0,
            RelicTransition: defeatTransition with
            {
                RunKey = defeatTransition.RunKey with { BattleNumber = defeatTransition.RunKey.BattleNumber + 1 }
            }, Identity: config.Identity);
        if (app.CompleteBattle(forgedDefeat, encounter) || !ReferenceEquals(active, app.ActiveRun) ||
            save.Serialize(active) != before || save.ActiveRunDeleteCalls != deletesBefore)
            throw new InvalidOperationException("forged defeat transition deleted or mutated the active Run");

        var reasonMismatch = new BattleResult(BattleOutcome.Timeout, 1, "reason-mismatch", [], 0,
            RelicTransition: defeatTransition, Identity: config.Identity);
        if (app.CompleteBattle(reasonMismatch, encounter) || !ReferenceEquals(active, app.ActiveRun) ||
            save.Serialize(active) != before || save.ActiveRunDeleteCalls != deletesBefore)
            throw new InvalidOperationException("mismatched failure reason deleted or mutated the active Run");

        var validDefeat = new BattleResult(BattleOutcome.PlayerDefeat, 1, "valid-defeat", [], 0,
            RelicTransition: defeatTransition, Identity: config.Identity);
        if (app.CompleteBattle(validDefeat, encounter) || app.ActiveRun is not null ||
            save.ActiveRunDeleteCalls != deletesBefore + 1)
            throw new InvalidOperationException("validated defeat did not delete the active Run exactly once");

        if (!app.StartNewRun(heroId, 0xFA12UL)) throw new InvalidOperationException("timeout-guard run setup");
        var timeoutNode = app.CurrentOptions().First(option => option.Type is TowerNodeType.Combat or TowerNodeType.Elite or TowerNodeType.Boss);
        if (!app.SelectNode(timeoutNode.Type)) throw new InvalidOperationException("timeout-guard node selection");
        var timeoutEncounter = app.CurrentEncounter();
        var timeoutConfig = app.BuildBattleConfig(timeoutEncounter);
        using var timeoutBattle = new RelicBattleScope(timeoutConfig.Relics ?? throw new InvalidOperationException("timeout-guard relic preparation"));
        var timeoutTransition = timeoutBattle.Complete(RelicBattleCompletionReason.Timeout);
        var validTimeout = new BattleResult(BattleOutcome.Timeout, 1, "valid-timeout", [], 0,
            RelicTransition: timeoutTransition, Identity: timeoutConfig.Identity);
        var deletesBeforeTimeout = save.ActiveRunDeleteCalls;
        if (app.CompleteBattle(validTimeout, timeoutEncounter) || app.ActiveRun is not null ||
            save.ActiveRunDeleteCalls != deletesBeforeTimeout + 1)
            throw new InvalidOperationException("validated timeout did not delete the active Run exactly once");
    }

    private static void RunApplicationTransitionIdentityLifecycle(ContentRegistry registry)
    {
        var save = new TransactionalRunSaveService(registry.Catalog.Heroes.Select(entry => entry.StableId));
        var app = new RunApplication(registry, save, TestProjectFixture.Load(registry));
        var heroId = registry.Catalog.Heroes[0].StableId;
        const ulong seed = 0x1D3UL;

        string CompleteFirstBattle()
        {
            if (!app.StartNewRun(heroId, seed)) throw new InvalidOperationException("same-seed run setup");
            var node = app.CurrentOptions().First(option => option.Type is TowerNodeType.Combat or TowerNodeType.Elite or TowerNodeType.Boss);
            if (!app.SelectNode(node.Type)) throw new InvalidOperationException("same-seed node selection");
            var encounter = app.CurrentEncounter();
            var config = app.BuildBattleConfig(encounter);
            using var battle = new RelicBattleScope(config.Relics ?? throw new InvalidOperationException("same-seed relic preparation"));
            var transition = battle.Complete(RelicBattleCompletionReason.PlayerVictory);
            var result = new BattleResult(BattleOutcome.PlayerVictory, 1, "same-seed", [], 0,
                RelicTransition: transition, Identity: config.Identity);
            if (!app.CompleteBattle(result, encounter))
                throw new InvalidOperationException("same-seed new Run was rejected by prior transition identity");
            return transition.TransitionId;
        }

        var first = CompleteFirstBattle();
        var second = CompleteFirstBattle();
        if (first != second)
            throw new InvalidOperationException("same source Run projection did not reproduce the transition identity fixture");
        app.AbandonRun();
    }

    private static void BattleScopeSourceGuard()
    {
        var battleRoot = ProjectSettings.GlobalizePath("res://src/Battle");
        var forbidden = new[]
        {
            "ActiveRunDto", "IRunSaveService", "SaveActiveRun", "SaveMeta(", "RunApplication",
            "RelicRunScope", "RunRelicService"
        };
        foreach (var path in Directory.EnumerateFiles(battleRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(path);
            var violation = forbidden.FirstOrDefault(source.Contains);
            if (violation is not null)
                throw new InvalidOperationException($"Battle scope directly references Run/Save authority in {path}: {violation}");
        }
        Equal(ActiveRunFormationSchema.CurrentVersion, 4, "active-run schema version");
    }

    private static RelicDefinition Authored(
        string stableId,
        RelicBattleModifierKind kind,
        float amount,
        string contentId = "") => new()
    {
        StableId = stableId,
        BattleModifiers = [new RelicBattleModifierSpec { Kind = kind, Amount = amount, ContentId = contentId }]
    };

    private static RelicDefinition ReactiveDefinition(
        string stableId,
        params RelicReactiveCounterSpec[] counters) => new()
    {
        StableId = stableId,
        ReactiveCounters = counters
    };

    private static RelicReactiveCounterSpec Counter(
        string counterId,
        RelicCounterScope scope,
        RelicCounterResetPolicy reset,
        RelicCounterSourceKind source,
        int team,
        int threshold,
        int consumption,
        RelicThresholdTargetKind target,
        int targetTeam) => new()
    {
        CounterId = counterId,
        Scope = scope,
        ResetPolicy = reset,
        Source = source,
        Team = team,
        Threshold = threshold,
        Consumption = consumption,
        Target = target,
        TargetTeam = targetTeam,
        ThresholdEffect = ManualShieldSpec(counterId + "_effect")
    };

    private static EffectBindingSpec ManualShieldSpec(string stableId) => new()
    {
        StableId = stableId,
        Trigger = new EffectTriggerSpec
        {
            Kind = EffectTriggerKind.Manual,
            EventKind = EffectDomainEventKind.None
        },
        TargetQuery = new ExplicitTargetQuerySpec(),
        Effects = [new ShieldEffectSpec { AmountSource = EffectAmountSource.Fixed, Amount = 3 }],
        Limits = new EffectBindingLimitsSpec()
    };

    private static RelicRunInstanceState ReactiveState(
        string instanceId,
        string contentId,
        CompiledRelicDefinition definition) => new()
    {
        InstanceId = instanceId,
        ContentId = contentId,
        Stacks = 1,
        Counters = RelicRunScope.InitialRunCounters(definition).ToList()
    };

    private static void Publish(BattleCombatEventPipeline pipeline, BattleCombatEventDraft draft)
    {
        var result = pipeline.Publish(draft);
        if (!result.Accepted)
            throw new InvalidOperationException("Reactive Relic fixture event was rejected: " + result.Message);
    }

    private static RelicRunInstanceState State(
        string instanceId,
        string contentId,
        int stacks,
        int charges = 0,
        int roll = 0) => new()
    {
        InstanceId = instanceId,
        ContentId = contentId,
        Stacks = stacks,
        Charges = charges,
        Roll = roll
    };

    private static RelicRunInstanceSnapshot Snapshot(RelicRunInstanceState state) => new(
        state.InstanceId,
        state.ContentId,
        state.Stacks,
        state.Charges,
        state.Roll,
        state.Counters.OrderBy(counter => counter.CounterId, StringComparer.Ordinal).ToImmutableArray());

    private static void ExpectModifier(
        IReadOnlyDictionary<string, CompiledRelicDefinition> definitions,
        string stableId,
        RelicBattleModifierKind kind,
        float amount,
        string contentId = "") =>
        ExpectModifiers(definitions, stableId, (kind, amount, contentId));

    private static void ExpectModifiers(
        IReadOnlyDictionary<string, CompiledRelicDefinition> definitions,
        string stableId,
        params (RelicBattleModifierKind Kind, float Amount, string ContentId)[] expected)
    {
        if (!definitions.TryGetValue(stableId, out var definition))
            throw new InvalidOperationException($"missing current relic definition: {stableId}");
        if (definition.BattleModifiers.Length != expected.Length || definition.VictoryOutcomes.Length != 0)
            throw new InvalidOperationException($"{stableId} modifier/outcome shape changed");
        for (var index = 0; index < expected.Length; index++)
        {
            var actual = definition.BattleModifiers[index];
            if (actual.Kind != expected[index].Kind || actual.ContentId != expected[index].ContentId)
                throw new InvalidOperationException($"{stableId} modifier[{index}] identity changed");
            Near(actual.Amount, expected[index].Amount, $"{stableId} modifier[{index}] amount");
        }
    }

    private static void ExpectOutcome(
        IReadOnlyDictionary<string, CompiledRelicDefinition> definitions,
        string stableId,
        RelicRunOutcomeKind kind,
        int amount)
    {
        if (!definitions.TryGetValue(stableId, out var definition) || definition.BattleModifiers.Length != 0 ||
            definition.AttributeBindings.Length != 0 || definition.BattleStartEffects.Length != 0 ||
            definition.ReactiveCounters.Length != 0 ||
            definition.VictoryOutcomes.Length != 1 || definition.VictoryOutcomes[0].Kind != kind ||
            definition.VictoryOutcomes[0].Amount != amount)
            throw new InvalidOperationException($"{stableId} victory outcome changed");
    }

    private static void ExpectAttribute<TTarget>(
        IReadOnlyDictionary<string, CompiledRelicDefinition> definitions,
        string stableId,
        CombatAttribute attribute,
        AttributeModifierOperation operation,
        float amount) where TTarget : CompiledRelicUnitTarget =>
        ExpectAttributes<TTarget>(definitions, stableId, (attribute, operation, amount));

    private static void ExpectAttributes<TTarget>(
        IReadOnlyDictionary<string, CompiledRelicDefinition> definitions,
        string stableId,
        params (CombatAttribute Attribute, AttributeModifierOperation Operation, float Amount)[] expected)
        where TTarget : CompiledRelicUnitTarget
    {
        if (!definitions.TryGetValue(stableId, out var definition) ||
            definition.BattleModifiers.Length != 0 || definition.BattleStartEffects.Length != 0 ||
            definition.ReactiveCounters.Length != 0 || definition.VictoryOutcomes.Length != 0 ||
            definition.AttributeBindings.Length != expected.Length)
            throw new InvalidOperationException($"{stableId} typed Attribute binding shape changed");
        for (var index = 0; index < expected.Length; index++)
        {
            var actual = definition.AttributeBindings[index];
            if (actual.Target is not TTarget || actual.Modifier.Attribute != expected[index].Attribute ||
                actual.Modifier.Operation != expected[index].Operation ||
                actual.Modifier.Magnitude is not CompiledConstantMagnitude magnitude)
                throw new InvalidOperationException(
                    $"{stableId} typed Attribute binding[{index}] changed: " +
                    $"target={actual.Target.GetType().Name},attribute={actual.Modifier.Attribute}," +
                    $"operation={actual.Modifier.Operation},magnitude={actual.Modifier.Magnitude.GetType().Name}");
            Near(magnitude.Value, expected[index].Amount, $"{stableId} typed Attribute binding[{index}] amount");
        }
    }

    private static void ExpectStartShield(
        IReadOnlyDictionary<string, CompiledRelicDefinition> definitions,
        string stableId,
        int amount)
    {
        if (!definitions.TryGetValue(stableId, out var definition) ||
            definition.BattleModifiers.Length != 0 || definition.AttributeBindings.Length != 0 ||
            definition.ReactiveCounters.Length != 0 || definition.VictoryOutcomes.Length != 0 ||
            definition.BattleStartEffects is not [CompiledRelicBattleStartShield shield] || shield.Amount != amount)
            throw new InvalidOperationException($"{stableId} typed Battle-start shield changed");
    }

    private static void ExpectStartSummon(
        IReadOnlyDictionary<string, CompiledRelicDefinition> definitions,
        string stableId,
        string contentId,
        float healthMultiplier,
        float damageMultiplier)
    {
        if (!definitions.TryGetValue(stableId, out var definition) ||
            definition.BattleModifiers.Length != 0 || definition.AttributeBindings.Length != 0 ||
            definition.ReactiveCounters.Length != 0 || definition.VictoryOutcomes.Length != 0 ||
            definition.BattleStartEffects is not [CompiledRelicBattleStartSummon summon] ||
            summon.ContentId != contentId)
            throw new InvalidOperationException($"{stableId} typed Battle-start summon changed");
        Near(summon.HealthMultiplier, healthMultiplier, stableId + " summon health multiplier");
        Near(summon.DamageMultiplier, damageMultiplier, stableId + " summon damage multiplier");
    }

    private static void ExpectCompileFailure(
        RelicDefinition definition,
        IReadOnlySet<string> validContentIds,
        string expectedMessage)
    {
        var result = RelicDefinitionCompiler.Compile(definition, validContentIds);
        if (result.Definition is not null || !result.Report.CoreErrors.Any(error => error.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"invalid relic did not fail with '{expectedMessage}'");
    }

    private static void ExpectReport(ValidationReport report, string expectedMessage, string label)
    {
        if (!report.HasCoreErrors || !report.CoreErrors.Any(error => error.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"{label} did not produce '{expectedMessage}'");
    }

    private static void ExpectApplyFailure(
        RelicRunScope scope,
        RelicBattleTransitionResult transition,
        RelicRunInstanceState state,
        RelicRunInstanceSnapshot baseline,
        string label)
    {
        if (scope.Apply(transition).Succeeded || Snapshot(state) != baseline)
            throw new InvalidOperationException($"{label} transition mutated Run relic state");
    }

    private static UnitSnapshot RelicUnit(
        string id,
        bool hero,
        float health,
        float damage,
        float range) =>
        new(id, id, UnitRole.Fighter, hero, false,
            health, damage, range, 1, 1, 0, 0, 0, 0,
            Array.Empty<string>(), new UnitBehaviorSnapshot());

    private static HeroRuleSnapshot NeutralHeroRule() => new(
        1, 1, 1, 0, 0, 0, false,
        string.Empty, 1, 1, 0, 0, 0, 0,
        false, false, 0, 0, string.Empty);

    private static IReadOnlyList<TowerRegionDefinition> Regions() =>
    [
        GD.Load<TowerRegionDefinition>("res://content/tower/region_ember_foundry.tres"),
        GD.Load<TowerRegionDefinition>("res://content/tower/region_gloam_crypt.tres"),
        GD.Load<TowerRegionDefinition>("res://content/tower/region_crown_engine.tres")
    ];

    private static void Near(float actual, float expected, string label)
    {
        if (Math.Abs(actual - expected) > .0001f)
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    }

    private static void ExpectThrows(Action action, string label)
    {
        try
        {
            action();
        }
        catch
        {
            return;
        }
        throw new InvalidOperationException(label + " did not reject invalid state");
    }

    private static void Equal<T>(T actual, T expected, string label) where T : IEquatable<T>
    {
        if (!actual.Equals(expected))
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    }

    private sealed class TransactionalRunSaveService(IEnumerable<string> unlockedHeroIds) : IRunSaveService
    {
        private readonly JsonSerializerOptions _json = new();
        private readonly MetaProgressDto _meta = new() { UnlockedHeroIds = unlockedHeroIds.ToList() };
        private readonly SettingsDto _settings = new();
        private ActiveRunDto? _run;

        public bool FailNextActiveRunSave { get; set; }
        public int ActiveRunSaveCalls { get; private set; }
        public int ActiveRunDeleteCalls { get; private set; }
        public MetaProgressDto LoadMeta() => _meta;
        public SettingsDto LoadSettings() => _settings;
        public ActiveRunDto? LoadActiveRun() => Clone(_run);
        public bool SaveMeta(MetaProgressDto value) => true;
        public bool SaveSettings(SettingsDto value) => true;
        public bool SaveActiveRun(ActiveRunDto value)
        {
            ActiveRunSaveCalls++;
            if (FailNextActiveRunSave)
            {
                FailNextActiveRunSave = false;
                return false;
            }
            _run = Clone(value);
            return true;
        }
        public void DeleteActiveRun()
        {
            ActiveRunDeleteCalls++;
            _run = null;
        }
        public string Serialize(ActiveRunDto value) => JsonSerializer.Serialize(value, _json);

        private ActiveRunDto? Clone(ActiveRunDto? value) => value is null
            ? null
            : JsonSerializer.Deserialize<ActiveRunDto>(JsonSerializer.Serialize(value, _json), _json);
    }
}
