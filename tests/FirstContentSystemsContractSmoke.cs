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
using TowerAutobattler.Relics;
using TowerAutobattler.Statuses;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.Traits;

// Synthetic content exercises the production compiler and combat authority. No saves,
// production catalog, renderer or private-state reflection are involved.
public partial class FirstContentSystemsContractSmoke : Node
{
    public override void _Ready()
    {
        try
        {
            ScaledStatusUsesCommittedTargetStacks();
            ChanceBoundariesDoNotCancelOtherOperations();
            TraitChanceUsesOwnerMembershipAndCap();
            FailedCommitRestoresStateAndRandomSequence();
            ProjectileSequenceHitsMultipleEnemies();
            TraitMembershipCountsOwners();
            SummonRelicAppliesToLaterSummons();
            GD.Print("FIRST_CONTENT_SYSTEMS_CONTRACT_OK scaled-status probability rollback multi-target owner-membership later-summon-relic");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PrintErr("FIRST_CONTENT_SYSTEMS_CONTRACT_FAILED: " + exception);
            GetTree().Quit(1);
        }
    }

    private static void ScaledStatusUsesCommittedTargetStacks()
    {
        using var poison = Status("probe_poison");
        using var operation = new ScaledStatusAbilityOperationSpec
        {
            Status = poison, TargetQuery = new ExplicitTargetQuerySpec(),
            BaseStacks = 3, StatusId = poison.StableId, ExistingStackRatio = .5f
        };
        var ability = Compile("probe_scale", operation);
        using var battle = Battle();
        Require(Execute(battle, ability).Succeeded, "zero-poison application succeeds");
        Require(Stacks(battle, "enemy", poison.StableId) == 3, "zero poison still receives three base stacks");
        Require(Execute(battle, ability).Succeeded, "existing-poison application succeeds");
        Require(Stacks(battle, "enemy", poison.StableId) == 7,
            "three existing stacks receive floor(3 + 3 * .5) = four, without self-compounding");
        Require(Execute(battle, ability).Succeeded && Stacks(battle, "enemy", poison.StableId) == 13,
            "next cast reads the newly committed seven stacks");
        Require(poison.StackLimit == 0 && operation.BaseStacks == 3 && operation.ExistingStackRatio == .5f,
            "runtime does not write stack state into shared authoring resources");
    }

    private static void ChanceBoundariesDoNotCancelOtherOperations()
    {
        using var frost = Status("probe_frost");
        using var freeze = Status("probe_freeze");
        using var marker = Status("probe_marker");
        foreach (var chance in new[] { 0f, 1f })
        {
            using var application = new ScaledStatusAbilityOperationSpec
            {
                Status = freeze, TargetQuery = new ExplicitTargetQuerySpec(), Chance = chance
            };
            using var companion = new ApplyStatusAbilityOperationSpec
            {
                Status = marker, TargetQuery = new OwnerTargetQuerySpec()
            };
            var ability = Compile("probe_chance", application, companion);
            using var battle = Battle();
            var result = Execute(battle, ability);
            Require(result.Succeeded, "chance boundary remains a successful ability");
            Require(Stacks(battle, "enemy", freeze.StableId) == (int)chance,
                "zero chance applies nothing; one applies one stack");
            Require(Stacks(battle, "owner", marker.StableId) == 1,
                "a missed probability roll does not cancel a subsequent operation");
        }

        using var seed = new ScaledStatusAbilityOperationSpec
        {
            Status = frost, TargetQuery = new ExplicitTargetQuerySpec(), BaseStacks = 20
        };
        using var scaledChance = new ScaledStatusAbilityOperationSpec
        {
            Status = freeze, TargetQuery = new ExplicitTargetQuerySpec(),
            Chance = .2f, ChanceStatusId = frost.StableId, ChancePerStack = .05f
        };
        using var supported = Battle();
        Require(Execute(supported, Compile("probe_frost_seed", seed)).Succeeded, "chance-state seed succeeds");
        Require(Execute(supported, Compile("probe_frost_freeze", scaledChance)).Succeeded &&
            Stacks(supported, "enemy", freeze.StableId) == 1,
            "per-stack chance reads the other status and clamps above one");
    }


    private static void TraitChanceUsesOwnerMembershipAndCap()
    {
        using var status = Status("probe_trait_freeze");
        var winter = TraitDefinitionCompiler.Compile(
            GD.Load<TraitDefinition>("res://content/traits/definitions/trait_ne10_winter.tres")).Definition
            ?? throw new InvalidOperationException("The production winter trait must compile.");
        using var authoredFreeze = GD.Load<AbilityDefinition>("res://content/abilities/triggered/ability_ne10_chill_hit.tres");
        var production = AbilityDefinitionCompiler.Compile(authoredFreeze).Ability
            ?? throw new InvalidOperationException("The production freeze ability must compile.");
        var rule = production.Operations.OfType<CompiledScaledStatusAbilityOperation>().Single();
        Require(production.Operations.Length == 1 && rule.Status.StableId == "status_ne10_freeze" &&
            rule.Status.DurationTicks == 8 && rule.Status.StackLimit == 1 && rule.ExistingStackRatio == 0 &&
            rule.ChancePerStack == 0 && string.IsNullOrEmpty(rule.ChanceStatusId),
            "production cold is one 0.8-second freeze, with no chill application or stack-based chance");
        Near(rule.Chance, .25f, "base probability");
        Near(rule.ChanceByTraitTier[0], .35f, "two-member probability");
        Near(rule.ChanceByTraitTier[1], .5f, "four-member probability");
        Near(rule.MaximumChance, .5f, "production probability cap");

        using var operation = new ScaledStatusAbilityOperationSpec
        {
            Status = status, TargetQuery = new ExplicitTargetQuerySpec(), Chance = rule.Chance,
            ChanceTraitId = winter.StableId, ChanceByTraitTier = rule.ChanceByTraitTier.ToArray(),
            MaximumChance = rule.MaximumChance
        };
        var scaled = Compile("probe_trait_probability", operation);
        var memberIds = new[] { "owner", "ally", "member_2", "member_3", "member_4", "member_5" };
        var moreUnits = Enumerable.Range(2, 4).Select(i =>
            new BattleSpawn(Unit("member_" + i), 0, new Vector2I(i, 0), "member_" + i)).ToArray();
        foreach (var memberCount in new[] { 0, 1, 2, 3, 4, 6 })
        {
            var contributions = memberIds.Take(memberCount).Select(id => new TraitContributionInput(
                winter.StableId, 1, 0, TraitContributionSourceKind.Hero, id, id, id, true, false, true)).ToList();
            if (memberCount > 0)
                contributions.Add(new TraitContributionInput(winter.StableId, 1, 0,
                    TraitContributionSourceKind.Equipment, "duplicate_badge", "owner", "badge", true, false, true));
            var preparation = TraitBattlePreparationBuilder.Build([winter], contributions);
            var expected = memberCount >= 4 ? .5f : memberCount >= 2 ? .35f : .25f;
            using var referenceOperation = new ScaledStatusAbilityOperationSpec
            { Status = status, TargetQuery = new ExplicitTargetQuerySpec(), Chance = expected };
            var reference = Compile("probe_reference_probability", referenceOperation);
            using var actual = Battle(traits: preparation, additionalSpawns: moreUnits);
            using var control = Battle(traits: preparation, additionalSpawns: moreUnits);
            Require(actual.TraitSnapshot.Value(winter.StableId, 0) == memberCount,
                "native member plus badge still counts one owner");
            for (var roll = 0; roll < 128; roll++)
            {
                Require(Execute(actual, scaled).Succeeded && Execute(control, reference).Succeeded,
                    "trait and constant reference applications commit");
                Require(Stacks(actual, "enemy", status.StableId) == Stacks(control, "enemy", status.StableId),
                    $"member count {memberCount}, roll {roll}: exact random-stream outcomes match {expected}");
            }
            var successes = Stacks(actual, "enemy", status.StableId);
            Require(successes > 0 && successes < 128, "every tier exercises success and miss outcomes");
            // Enemy owner must use its own team, not the target's active four/six-member trait.
            using var outsider = Battle(traits: preparation, additionalSpawns: moreUnits);
            using var outsiderReference = Battle(traits: preparation, additionalSpawns: moreUnits);
            using var baselineOperation = new ScaledStatusAbilityOperationSpec
            { Status = status, TargetQuery = new ExplicitTargetQuerySpec(), Chance = .25f };
            var baseline = Compile("probe_outsider_reference", baselineOperation);
            for (var roll = 0; roll < 64; roll++)
            {
                Require(Execute(outsider, scaled, "enemy", "owner").Succeeded &&
                    Execute(outsiderReference, baseline, "enemy", "owner").Succeeded, "opposing team rolls commit");
                Require(Stacks(outsider, "owner", status.StableId) == Stacks(outsiderReference, "owner", status.StableId),
                    "enemy does not borrow target-team probability");
            }
            GD.Print($"FROST_CHANCE members={memberCount} chance={expected:P0} successes={successes}/128 reference-match");
        }

        // More additive inputs can never bypass an authored 50% ceiling.
        using var stackSeed = new ScaledStatusAbilityOperationSpec
        { Status = status, TargetQuery = new ExplicitTargetQuerySpec(), BaseStacks = 100 };
        operation.ChanceStatusId = status.StableId;
        operation.ChancePerStack = .1f;
        var capped = Compile("probe_capped_probability", operation);
        using var halfOperation = new ScaledStatusAbilityOperationSpec
        { Status = status, TargetQuery = new ExplicitTargetQuerySpec(), Chance = .5f };
        var half = Compile("probe_half_probability", halfOperation);
        var seed = Compile("probe_seed_stacks", stackSeed);
        using var cappedBattle = Battle();
        using var halfBattle = Battle();
        Require(Execute(cappedBattle, seed).Succeeded && Execute(halfBattle, seed).Succeeded, "cap fixtures seeded");
        for (var roll = 0; roll < 64; roll++)
        {
            Require(Execute(cappedBattle, capped).Succeeded && Execute(halfBattle, half).Succeeded, "cap rolls commit");
            Require(Stacks(cappedBattle, "enemy", status.StableId) == Stacks(halfBattle, "enemy", status.StableId),
                "100 supporting stacks still produce exactly the 50% reference sequence");
        }
        operation.ChancePerStack = 0;
        operation.ChanceStatusId = "";
        operation.ChanceByTraitTier = [.4f, .5f];
        string Fingerprint(CompiledAbilityDefinition ability)
        {
            using var loadout = new AbilityLoadoutDefinition();
            using var command = new TacticalCommandDefinition
            { StableId = "probe_trait_command", DisplayName = "probe", AbilityLoadout = loadout, PrimaryAbilityId = ability.StableId };
            return TacticalCommandDefinitionCompiler.Compile(command, _ => new CompiledAbilityLoadout([ability])).Definition?.Fingerprint
                ?? throw new InvalidOperationException("Probe command must compile for identity verification.");
        }
        Require(Fingerprint(Compile("probe_trait_probability", operation)) != Fingerprint(scaled),
            "tier probability participates in publication identity");
        operation.MaximumChance = .3f;
        using var invalid = new AbilityDefinition
        { StableId = "probe_invalid_chance", DisplayName = "invalid", ActivationKind = AbilityActivationKind.ManualCommand, ManaCost = 1 };
        invalid.Operations.Add(operation);
        Require(AbilityDefinitionCompiler.Compile(invalid).Report.HasCoreErrors,
            "tier values exceeding their ceiling are rejected");
    }

    private static void FailedCommitRestoresStateAndRandomSequence()
    {
        using var status = Status("probe_random_status");
        using var marker = Status("probe_rollback_marker");
        using var randomOperation = new ScaledStatusAbilityOperationSpec
        {
            Status = status, TargetQuery = new ExplicitTargetQuerySpec(), Chance = .5f
        };
        using var guaranteedOperation = new ApplyStatusAbilityOperationSpec
        {
            Status = marker, TargetQuery = new OwnerTargetQuerySpec()
        };
        var ability = Compile("probe_random_commit", randomOperation, guaranteedOperation);
        var injectFailure = true;
        using var retry = Battle(bindings: registry => registry.Subscribe(
            BattleCombatEventKind.AbilityResolved, CombatSourceRef.System("probe_commit_failure"), 0,
            (fact, sink) =>
            {
                if (injectFailure && fact.SubjectStableId == ability.StableId)
                    sink.Enqueue(CombatSourceRef.System("probe_commit_failure"), 0,
                        _ => throw new InvalidOperationException("deliberate post-operation commit failure"));
            }));
        using var control = Battle();
        var failure = Execute(retry, ability);
        Require(!failure.Succeeded, "injected failure aborts the transaction");
        Require(Stacks(retry, "enemy", status.StableId) == 0 && Stacks(retry, "owner", marker.StableId) == 0,
            "failure restores probabilistic and guaranteed state mutations");
        Require(!retry.CombatEvents.Any(fact => fact.SubjectStableId == ability.StableId),
            "failure removes the successful ability fact");
        injectFailure = false;

        var successes = 0;
        for (var index = 0; index < 32; index++)
        {
            var before = Stacks(control, "enemy", status.StableId);
            Require(Execute(retry, ability).Succeeded && Execute(control, ability).Succeeded,
                "same-seed committed retries succeed");
            var expected = Stacks(control, "enemy", status.StableId);
            successes += expected - before;
            Require(Stacks(retry, "enemy", status.StableId) == expected,
                $"failed transaction preserved the random stream at draw {index}");
            Require(Stacks(retry, "owner", marker.StableId) == index + 1,
                "rollback does not leave a hidden extra guaranteed application");
        }
        Require(successes > 0 && successes < 32, "fixture exercises both probability outcomes");
    }

    private static void ProjectileSequenceHitsMultipleEnemies()
    {
        using var operation = new ProjectileSequenceAbilityOperationSpec
        {
            ShotCount = 6, MaxTargets = 3, AttackIntervalRatio = .5f, AttackDamageMultiplier = 1
        };
        var ability = Compile("probe_multi_arrow", operation);
        using var battle = Battle(extraEnemies: true, rangedOwner: true);
        Require(Execute(battle, ability).Succeeded, "multi-target sequence starts");
        for (var tick = 0; tick < 80 && battle.Outcome == BattleOutcome.Running; tick++) battle.Step();
        var hits = battle.CombatEvents.Where(fact => fact.Kind == BattleCombatEventKind.SkillHitLanded &&
            fact.Source.StableId == ability.StableId).ToArray();
        var enemyIds = battle.Units.Where(unit => unit.Team == 1).Select(unit => unit.RuntimeId).ToHashSet();
        Require(hits.Length == 6, "six scheduled arrows produce six actual skill hits in the unobstructed fixture");
        Require(hits.Select(fact => fact.TargetRuntimeId).Distinct().Count() == 3,
            "three-target sequence actually reaches three different enemies");
        Require(hits.All(fact => enemyIds.Contains(fact.TargetRuntimeId)), "all sequence hits belong to legal enemies");
        Require(battle.Units.Where(unit => unit.Team == 0).All(unit => Math.Abs(unit.Health - unit.MaxHealth) < .001f),
            "sequence never harms the owner or allied bystander");
    }

    private static void TraitMembershipCountsOwners()
    {
        using var modifier = new AttributeModifierSpec
        {
            Attribute = CombatAttribute.AttackDamage, Operation = AttributeModifierOperation.Add,
            Magnitude = new ConstantAttributeMagnitudeSpec { Value = 1 }, SlotId = "probe_membership_attack"
        };
        using var definition = new TraitDefinition
        {
            StableId = "probe_membership", DisplayName = "归属检查", SemanticIconKey = "probe.membership",
            CountingPolicy = new TraitCountingPolicySpec
            {
                DeploymentPolicy = TraitDeploymentPolicy.DeployedOnly,
                TemporaryUnitPolicy = TraitTemporaryUnitPolicy.Exclude,
                DuplicateContentPolicy = TraitDuplicateContentPolicy.UniqueOwner, CountEquipment = true
            },
            Breakpoints = [new TraitBreakpointSpec
            { MinValue = 1, MaxValue = 10, DisplayStyle = "TraitTierOne", AttributeModifiers = [modifier] }]
        };
        var compilation = TraitDefinitionCompiler.Compile(definition);
        var compiled = compilation.Definition ?? throw new InvalidOperationException(string.Join(" | ", compilation.Report.CoreErrors));
        TraitContributionInput Input(string owner, string source, string content, TraitContributionSourceKind kind) =>
            new(compiled.StableId, 1, 0, kind, source, owner, content, true, false, true);
        var native = Input("hero_a", "hero_a", "native_hero", TraitContributionSourceKind.Hero);
        var badgeA = Input("hero_a", "badge_a", "same_badge", TraitContributionSourceKind.Equipment);
        var badgeB = Input("hero_b", "badge_b", "same_badge", TraitContributionSourceKind.Equipment);
        Require((int)TraitDuplicateContentPolicy.UniqueOwner == 2, "new serialized policy keeps its assigned enum value");
        Require(TraitSnapshotBuilder.Build([compiled], [native, badgeA]).Value(compiled.StableId, 0) == 1,
            "native membership and a badge on one hero count once");
        Require(TraitSnapshotBuilder.Build([compiled], [badgeA, badgeB]).Value(compiled.StableId, 0) == 2,
            "identical badges worn by two heroes count both owners");
        Require(TraitSnapshotBuilder.Build([compiled], [native, badgeA, badgeB]).Value(compiled.StableId, 0) == 2,
            "mixed native and equipment membership preserves two unique recipients");
    }

    private static void SummonRelicAppliesToLaterSummons()
    {
        using var relic = new RelicDefinition
        {
            StableId = "probe_summon_relic",
            AttributeBindings = [new RelicAttributeBindingSpec
            {
                BindingId = "summon_attack", Target = new RelicPlayerSummonsTargetSpec(),
                Modifier = new AttributeModifierSpec
                {
                    Attribute = CombatAttribute.AttackDamage, Operation = AttributeModifierOperation.Add,
                    Magnitude = new ConstantAttributeMagnitudeSpec { Value = 7 }, SlotId = "summon_attack"
                }
            }, new RelicAttributeBindingSpec
            {
                BindingId = "summon_health", Target = new RelicPlayerSummonsTargetSpec(),
                Modifier = new AttributeModifierSpec
                {
                    Attribute = CombatAttribute.MaxHealth, Operation = AttributeModifierOperation.Add,
                    Magnitude = new ConstantAttributeMagnitudeSpec { Value = 200 }, SlotId = "summon_health"
                }
            }]
        };
        var compilation = RelicDefinitionCompiler.Compile(relic);
        var compiled = compilation.Definition ?? throw new InvalidOperationException(string.Join(" | ", compilation.Report.CoreErrors));
        using var run = new RelicRunScope(new RelicRunKey(913, "probe_owner", 0, 0));
        using var registration = run.Activate(compiled, new RelicRunInstanceState
        { InstanceId = "probe_relic_instance", ContentId = compiled.StableId });
        using var deathDamage = new BattleValueAbilityOperationSpec
        {
            Action = BattleValueAction.Damage, DamageType = EffectDamageType.True,
            TargetQuery = new FilteredTargetQuerySpec { Team = EffectRelativeTeam.Enemies },
            Terms = [new BattleValueTermSpec
            { Metric = BattleValueMetric.Attribute, Subject = BattleValueSubject.EventTarget,
                Attribute = CombatAttribute.MaxHealth, Scale = .1f }]
        };
        using var deathAbility = new AbilityDefinition
        {
            StableId = "probe_summon_death", DisplayName = "临时友军亡语检查",
            ActivationKind = AbilityActivationKind.Triggered, Trigger = AbilityTriggerKind.AllyTemporaryDefeated,
            Operations = [deathDamage]
        };
        var deathCompilation = AbilityDefinitionCompiler.Compile(deathAbility);
        var deathCompiled = deathCompilation.Ability ?? throw new InvalidOperationException(string.Join(" | ", deathCompilation.Report.CoreErrors));
        using var battle = Battle(relics: run.PrepareBattle(), summon: Unit("summon"),
            ownerAbilities: new CompiledAbilityLoadout([deathCompiled]));
        Require(battle.Units.All(unit => !unit.IsTemporary), "fixture begins before any summons exist");
        using var summonOperation = new SummonAbilityOperationSpec
        { Profile = AbilitySummonProfile.DeathSummon, Count = 1, RequireAtLeastOne = true };
        var ability = Compile("probe_late_summon", summonOperation);
        Require(Execute(battle, ability).Succeeded, "player summons after relic activation");
        var summoned = battle.Units.Single(unit => unit.IsTemporary && unit.Team == 0);
        Near(summoned.Damage, 17, "new allied summon receives the active relic contribution");
        Near(summoned.MaxHealth, 1200, "new allied summon receives increased maximum health");
        Near(summoned.Health, 1200, "new allied summon initializes current health after relic projection");
        Near(Find(battle, "owner").Damage, 10, "permanent hero is not included in summon-only targets");

        using var lethalOperation = new BattleValueAbilityOperationSpec
        {
            Action = BattleValueAction.Damage, Amount = 5000, DamageType = EffectDamageType.True,
            TargetQuery = new ExplicitTargetQuerySpec()
        };
        var lethalAbility = Compile("probe_summon_kill", lethalOperation);
        Require(Execute(battle, lethalAbility, ownerContentId: "enemy", targetContentId: "summon").Succeeded,
            "summon dies through real attributed damage");
        battle.Step();
        var explosions = battle.CombatEvents.Where(fact => fact.Kind == BattleCombatEventKind.DamageResolved &&
            fact.Source.StableId == deathCompiled.StableId).ToArray();
        Require((int)AbilityTriggerKind.AllyTemporaryDefeated == 23 && explosions.Length == 1,
            "one allied temporary death triggers one explosion through its typed event");
        Near(explosions[0].EffectiveValue, 120,
            "death reader sees the summon's relic-enhanced maximum health after defeat");
        Require(Execute(battle, lethalAbility, ownerContentId: "enemy", targetContentId: "ally").Succeeded,
            "permanent allied bystander dies through the same damage path");
        battle.Step();
        Require(battle.CombatEvents.Count(fact => fact.Kind == BattleCombatEventKind.DamageResolved &&
            fact.Source.StableId == deathCompiled.StableId) == 1,
            "permanent ally death does not trigger temporary-only death rewards");

        Require(Execute(battle, ability, ownerContentId: "enemy", targetContentId: "owner").Succeeded,
            "enemy summon uses the same public summoning path");
        Near(battle.Units.Single(unit => unit.IsTemporary && unit.Team == 1).Damage, 10,
            "enemy summons do not inherit the player's relic");
        Near(battle.Units.Single(unit => unit.IsTemporary && unit.Team == 1).Health, 1000,
            "enemy summon birth health excludes the player's relic");
    }

    private static StatusDefinition Status(string id) => new()
    {
        StableId = id, DisplayName = id, Behavior = StatusBehaviorKind.None,
        DurationKind = StatusDurationKind.Permanent, AggregationPolicy = StatusAggregationPolicy.ByTarget,
        StackLimit = 0
    };

    private static CompiledAbilityDefinition Compile(string id, params AbilityOperationSpec[] operations)
    {
        using var authored = new AbilityDefinition
        { StableId = id, DisplayName = id, ActivationKind = AbilityActivationKind.ManualCommand, ManaCost = 1 };
        foreach (var operation in operations) authored.Operations.Add(operation);
        var compilation = AbilityDefinitionCompiler.Compile(authored);
        return compilation.Ability ?? throw new InvalidOperationException(string.Join(" | ", compilation.Report.CoreErrors));
    }

    private static AbilityCommitResult Execute(BattleSimulation battle, CompiledAbilityDefinition ability,
        string ownerContentId = "owner", string targetContentId = "enemy")
    {
        var owner = Find(battle, ownerContentId);
        var target = Find(battle, targetContentId);
        var world = (IAbilityRuntimeWorld)battle;
        var preparation = world.Prepare(ability, owner.RuntimeId, owner.RuntimeId, target.RuntimeId, battle.TickIndex);
        Require(preparation.Succeeded && preparation.Plan is not null, $"ability {ability.StableId} must prepare: {preparation.FailureReason}");
        return world.Commit(preparation.Plan!);
    }

    private static BattleUnitState Find(BattleSimulation battle, string contentId) =>
        battle.Units.Single(unit => unit.Definition.ContentId == contentId);
    private static int Stacks(BattleSimulation battle, string contentId, string statusId) =>
        Find(battle, contentId).Statuses.Where(status => status.StableId == statusId).Sum(status => status.Stacks);

    private static UnitSnapshot Unit(string id, bool ranged = false)
    {
        using var definition = new UnitDefinition
        {
            Id = id, DisplayName = id, IsHero = id == "owner", MaxHealth = 1000, AttackDamage = 10,
            AttackRange = 8, AttackCooldown = .2f, BodyRadius = .2f,
            AttackDelivery = ranged ? AttackDelivery.Projectile : AttackDelivery.Melee,
            ProjectileSpeed = 20, ProjectileLifetime = 3, ProjectileWindupSeconds = 0,
            ManaPerSecond = 0, ManaPerAttack = 0, ManaPerDamageRatio = 0
        };
        return BattleSetupFactory.Snapshot(definition) with
        { Behavior = new UnitBehaviorSnapshot(Stationary: true, DisableBasicAttacks: true) };
    }

    private static BattleSimulation Battle(Action<BattleCombatBindingRegistry>? bindings = null,
        bool extraEnemies = false, bool rangedOwner = false, RelicBattlePreparation? relics = null,
        UnitSnapshot? summon = null, CompiledAbilityLoadout? ownerAbilities = null,
        TraitBattlePreparation? traits = null, IEnumerable<BattleSpawn>? additionalSpawns = null)
    {
        var spawns = new List<BattleSpawn>
        {
            new(Unit("owner", rangedOwner) with { AbilityLoadout = ownerAbilities }, 0, new Vector2I(2, 3), "owner", IsPersistentRosterHero: true),
            new(Unit("enemy"), 1, new Vector2I(5, 3), "enemy"),
            new(Unit("ally"), 0, new Vector2I(1, 1), "ally")
        };
        if (additionalSpawns is not null) spawns.AddRange(additionalSpawns);
        if (extraEnemies)
        {
            spawns.Add(new BattleSpawn(Unit("enemy_b"), 1, new Vector2I(5, 1), "enemy_b"));
            spawns.Add(new BattleSpawn(Unit("enemy_c"), 1, new Vector2I(5, 5), "enemy_c"));
        }
        return new BattleSimulation(new BattleConfig
        {
            Seed = 913, FloorRule = new ClearFloorRuleRuntime("first_content_probe", "首版机制检查", ""),
            Spawns = spawns, ConfigureCombatBindings = bindings, Relics = relics,
            Traits = traits ?? TraitBattlePreparation.Empty,
            Summons = new SummonProfiles(DeathSummon: summon),
            HeroRule = new HeroRuleSnapshot(1, 1, 1, 0, 0, 0, false, "", 1, 1, 0, 0, 0, 0, false, false, 0, 0, "")
        });
    }

    private static void Near(float actual, float expected, string message) =>
        Require(Math.Abs(actual - expected) < .001f, $"{message}: {actual} != {expected}");
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
