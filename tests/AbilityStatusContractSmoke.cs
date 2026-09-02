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
using TowerAutobattler.Statuses;
using TowerAutobattler.TacticalCommands;

public partial class AbilityStatusContractSmoke : Node
{
    public override async void _Ready()
    {
        var exitCode = Run();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(exitCode);
    }

    private static int Run()
    {
        try
        {
            ProductionResourcesCompileAsOneImmutableGraph();
            SharedAbilityResourceCanonicalizesAcrossLoadouts();
            AuthoredDescriptionCostAndRuntimeAgree();
            EntryPointsCooldownUsageAndIsolation();
            FailedActivationsAreAtomic();
            StatusDurationRefreshStackDispelAndPeriodics();
            ProductionStatusBehaviorIsExact();
            PublicationRejectsInvalidGraphsTransactionally();
            EveryBattleTerminationCleansAllProductScopes();
            GD.Print("ABILITY_STATUS_CONTRACT_OK authoring=immutable-description-runtime entry=manual-automatic-triggered-passive " +
                     "failure=mana-gold-dead-template-cell-effect-preflight-limit-commit-atomic cooldown=max-use runtime=isolation " +
                     "status=source-owner-duration-refresh-stack-expiry-dispel-periodic-presentation " +
                     "behavior=time-stop-exact-pause,damage-tag-stack publication=shared-resource-canonical,batch-collision-orphan-dependency-summon-id " +
                     "lifecycle=victory-defeat-timeout-abort-replacement-exception-disposal-zero");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("ABILITY_STATUS_CONTRACT_FAILED: " + exception);
            return 1;
        }
    }

    private static void ProductionResourcesCompileAsOneImmutableGraph()
    {
        var loadoutPaths = new[]
        {
            "loadout_beast_roar", "loadout_blood_rush", "loadout_boreal_boss", "loadout_duel_focus",
            "loadout_overclock", "loadout_paid_reinforcement", "loadout_raise_dead", "loadout_rally",
            "loadout_shadow_boss", "loadout_time_stop"
        };
        var loadouts = loadoutPaths.Select(LoadoutResource).ToArray();
        var statuses = new[]
        {
            LoadStatus("status_beast_roar_damage"),
            LoadStatus("status_blood_rush_damage"),
            LoadStatus("status_time_stop_disabled")
        };
        var before = ProductionFingerprint(loadouts, statuses);
        var abilityBatch = AbilityDefinitionCompiler.CompileBatch(loadouts);
        var statusBatch = StatusDefinitionCompiler.CompileBatch(statuses);
        Expect(!abilityBatch.Report.HasCoreErrors && abilityBatch.Abilities.Length == 10,
            "production ability batch did not publish all ten definitions: " + string.Join(" | ", abilityBatch.Report.CoreErrors));
        Expect(!statusBatch.Report.HasCoreErrors && statusBatch.Definitions.Length == 3,
            "production status batch did not publish all three definitions: " + string.Join(" | ", statusBatch.Report.CoreErrors));
        Expect(abilityBatch.Abilities.Count(ability => ability.ActivationKind == AbilityActivationKind.ManualCommand) == 8 &&
               abilityBatch.Abilities.Count(ability => ability.ActivationKind == AbilityActivationKind.Automatic) == 2,
            "production ability activation categories changed");
        Expect(abilityBatch.Abilities.All(ability => !string.IsNullOrWhiteSpace(ability.Description)),
            "production ability omitted a generated description");

        var worldA = new ProbeAbilityWorld();
        var worldB = new ProbeAbilityWorld();
        using var scopeA = new BattleAbilityScope("immutable_a", worldA, 3);
        using var scopeB = new BattleAbilityScope("immutable_b", worldB, 3);
        var rally = AbilityDefinitionCompiler.CompileLoadout(LoadoutResource("loadout_rally")).Loadout!;
        scopeA.RegisterLoadout("owner", rally);
        scopeB.RegisterLoadout("owner", rally);
        Expect(scopeA.TryActivateManual("owner", "ability_rally", 0).Succeeded,
            "first runtime could not execute immutable production loadout");
        Expect(scopeB.CurrentMana == 3 && scopeB.TryActivateManual("owner", "ability_rally", 0).Succeeded,
            "one runtime instance leaked mutable state into another");
        Expect(before == ProductionFingerprint(loadouts, statuses),
            "runtime activation mutated shared authored ability/status resources");
    }

    private static void SharedAbilityResourceCanonicalizesAcrossLoadouts()
    {
        var shared = EntryAbility(
            "ability_shared_resource",
            AbilityActivationKind.ManualCommand,
            AbilityTriggerKind.None,
            manaCost: 1,
            maxUses: 1);
        var loadoutA = new AbilityLoadoutDefinition { Abilities = [shared] };
        var loadoutB = new AbilityLoadoutDefinition { Abilities = [shared] };
        var batch = AbilityDefinitionCompiler.CompileBatch([loadoutA, loadoutB, loadoutA]);
        Expect(!batch.Report.HasCoreErrors && batch.Abilities.Length == 1 && batch.Loadouts.Length == 2,
            "one shared Ability Resource did not produce one canonical definition and two loadouts: " +
            string.Join(" | ", batch.Report.CoreErrors));
        var compiledA = batch.Loadouts[0].Compiled.Abilities.Single();
        var compiledB = batch.Loadouts[1].Compiled.Abilities.Single();
        Expect(ReferenceEquals(compiledA, compiledB) && ReferenceEquals(compiledA, batch.Abilities.Single()),
            "loadouts did not reuse the same canonical compiled Ability record");

        var worldA = new ProbeAbilityWorld();
        var worldB = new ProbeAbilityWorld();
        using var scopeA = new BattleAbilityScope("shared_a", worldA, 3);
        using var scopeB = new BattleAbilityScope("shared_b", worldB, 3);
        scopeA.RegisterLoadout("owner", batch.Loadouts[0].Compiled);
        scopeB.RegisterLoadout("owner", batch.Loadouts[1].Compiled);
        Expect(scopeA.TryActivateManual("owner", shared.StableId, 0).Succeeded &&
               scopeA.TryActivateManual("owner", shared.StableId, 1).Failure == AbilityActivationFailure.UsageLimit &&
               scopeA.CurrentMana == 2,
            "first owner did not retain independent shared-Ability runtime state");
        Expect(scopeB.CurrentMana == 3 && scopeB.TryActivateManual("owner", shared.StableId, 1).Succeeded &&
               worldA.SuccessfulMutations == 1 && worldB.SuccessfulMutations == 1,
            "shared canonical definition leaked mana/use state between owners");

        var duplicateInsideOneLoadout = AbilityDefinitionCompiler.CompileBatch(
            [new AbilityLoadoutDefinition { Abilities = [shared, shared] }]);
        Expect(duplicateInsideOneLoadout.Report.CoreErrors.Any(error =>
                   error.Contains("Duplicate ability stable id in loadout", StringComparison.Ordinal)) &&
               duplicateInsideOneLoadout.Abilities.IsEmpty,
            "same-loadout duplicate semantics were unintentionally relaxed");

        var collision = EntryAbility(
            shared.StableId,
            AbilityActivationKind.ManualCommand,
            AbilityTriggerKind.None,
            manaCost: 1);
        var collisionBatch = AbilityDefinitionCompiler.CompileBatch(
            [loadoutA, new AbilityLoadoutDefinition { Abilities = [collision] }]);
        Expect(collisionBatch.Report.CoreErrors.Any(error =>
                   error.Contains("distinct resources", StringComparison.Ordinal)) &&
               collisionBatch.Abilities.IsEmpty && collisionBatch.Loadouts.IsEmpty,
            "distinct Ability Resources sharing one stable id did not reject the complete batch");
    }

    private static void AuthoredDescriptionCostAndRuntimeAgree()
    {
        var ability = new AbilityDefinition
        {
            StableId = "ability_sentinel_rally",
            DisplayName = "契约集结",
            ActivationKind = AbilityActivationKind.ManualCommand,
            Trigger = AbilityTriggerKind.None,
            ManaCost = 2,
            GoldCost = 3,
            CooldownTicks = 0,
            MaxUses = 0,
            Operations =
            [
                new EffectAbilityOperationSpec
                {
                    Binding = new EffectBindingSpec
                    {
                        StableId = "ability_sentinel_rally_shield",
                        Trigger = new EffectTriggerSpec { Kind = EffectTriggerKind.Manual },
                        Conditions = [],
                        TargetQuery = new RelativeTeamTargetQuerySpec
                            { Team = EffectRelativeTeam.Allies, IncludeDefeated = false },
                        Effects = [new ShieldEffectSpec { AmountSource = EffectAmountSource.Fixed, Amount = 37 }],
                        Limits = new EffectBindingLimitsSpec()
                    },
                    InvocationValueSource = AbilityInvocationValueSource.Fixed,
                    InvocationValueScale = 1
                },
                new CooldownAbilityOperationSpec
                {
                    TargetQuery = new RelativeTeamTargetQuerySpec
                        { Team = EffectRelativeTeam.Allies, IncludeDefeated = false },
                    AttackAdjustment = CooldownAdjustmentKind.Cap,
                    AttackValue = 4
                }
            ]
        };
        var loadoutResource = new AbilityLoadoutDefinition { Abilities = [ability] };
        var compiled = AbilityDefinitionCompiler.CompileLoadout(loadoutResource);
        Expect(!compiled.Report.HasCoreErrors && compiled.Loadout is not null,
            "sentinel authoring did not compile: " + string.Join(" | ", compiled.Report.CoreErrors));
        var sentinelLoadout = compiled.Loadout ?? throw new InvalidOperationException("sentinel loadout missing");
        var runtimeAbility = sentinelLoadout.Find(ability.StableId)!;
        Expect(runtimeAbility.ManaCost == 2 && runtimeAbility.GoldCost == 3 &&
               runtimeAbility.Description.Contains("37", StringComparison.Ordinal) &&
               runtimeAbility.Description.Contains("0.4", StringComparison.Ordinal),
            "description and resource costs did not derive from the sentinel authoring source");

        var rule = Rule(sentinelLoadout, ability.StableId, maximumMana: 4);
        using var battle = new BattleSimulation(Config(
        [
            Spawn(Hero("sentinel-hero"), 0, 0, 2, "hero"),
            Spawn(Unit("sentinel-ally"), 0, 1, 2, "ally"),
            Spawn(Unit("sentinel-enemy", health: 1000, damage: 0, moveTicks: 1000), 1, 9, 2, "enemy")
        ], rule, startingGold: 5));
        foreach (var unit in battle.Units.Where(unit => unit.Team == 0)) unit.AttackCooldown = 11;
        Expect(battle.TryUseTacticalCommand(0).Succeeded, "sentinel authored command failed at runtime");
        Expect(battle.TacticalPoints == 1 && battle.GoldSpent == 3,
            "runtime resource costs diverged from generated facts");
        foreach (var unit in battle.Units.Where(unit => unit.Team == 0))
        {
            Near(unit.Shield, 37, "sentinel authored shield");
            Expect(unit.AttackCooldown == 4, "sentinel authored cooldown cap diverged at runtime");
        }
    }

    private static void EntryPointsCooldownUsageAndIsolation()
    {
        var authored = new[]
        {
            EntryAbility("ability_entry_manual", AbilityActivationKind.ManualCommand, AbilityTriggerKind.None,
                manaCost: 1, cooldownTicks: 3, maxUses: 2),
            EntryAbility("ability_entry_battle_start", AbilityActivationKind.Automatic, AbilityTriggerKind.BattleStarted),
            EntryAbility("ability_entry_periodic", AbilityActivationKind.Automatic, AbilityTriggerKind.PeriodicTick, intervalTicks: 2),
            EntryAbility("ability_entry_triggered", AbilityActivationKind.Triggered, AbilityTriggerKind.AttackHit),
            EntryAbility("ability_entry_passive", AbilityActivationKind.Passive, AbilityTriggerKind.None)
        };
        var compiled = AbilityDefinitionCompiler.CompileLoadout(new AbilityLoadoutDefinition { Abilities = [.. authored] });
        Expect(!compiled.Report.HasCoreErrors && compiled.Loadout is not null,
            "entry-point authoring did not compile: " + string.Join(" | ", compiled.Report.CoreErrors));
        var entryLoadout = compiled.Loadout ?? throw new InvalidOperationException("entry loadout missing");
        var worldA = new ProbeAbilityWorld();
        var worldB = new ProbeAbilityWorld();
        using var scopeA = new BattleAbilityScope("entry_a", worldA, 10);
        using var scopeB = new BattleAbilityScope("entry_b", worldB, 10);
        scopeA.RegisterLoadout("owner", entryLoadout);
        scopeB.RegisterLoadout("owner", entryLoadout);

        Expect(scopeA.Passives("owner").Select(ability => ability.StableId).SequenceEqual(["ability_entry_passive"]),
            "passive entry point did not expose the immutable passive definition");
        Expect(scopeA.ActivateAutomatic("owner", 0).Select(result => result.AbilityId)
                   .SequenceEqual(["ability_entry_battle_start"]),
            "battle-start automatic entry point changed");
        Expect(scopeA.ActivateAutomatic("owner", 1).Length == 0 &&
               scopeA.ActivateAutomatic("owner", 2).Select(result => result.AbilityId)
                   .SequenceEqual(["ability_entry_periodic"]),
            "periodic automatic entry point changed");
        Expect(scopeA.ActivateTriggered("owner", AbilityTriggerKind.AttackHit, 2).Single().Succeeded,
            "triggered entry point did not execute");
        Expect(scopeA.TryActivateManual("owner", "ability_entry_triggered", 2).Failure == AbilityActivationFailure.WrongEntryPoint,
            "wrong entry point lacked typed failure evidence");

        Expect(scopeA.TryActivateManual("owner", "ability_entry_manual", 0).Succeeded,
            "manual entry point did not execute");
        Expect(scopeA.TryActivateManual("owner", "ability_entry_manual", 1).Failure == AbilityActivationFailure.Cooldown,
            "manual cooldown was not authoritative");
        Expect(scopeA.TryActivateManual("owner", "ability_entry_manual", 3).Succeeded,
            "manual ability did not become ready on its authored tick");
        Expect(scopeA.TryActivateManual("owner", "ability_entry_manual", 6).Failure == AbilityActivationFailure.UsageLimit,
            "manual maximum use count was not authoritative");
        Expect(scopeA.CurrentMana == 8, "failed manual attempts consumed mana");
        Expect(scopeB.CurrentMana == 10 && scopeB.TryActivateManual("owner", "ability_entry_manual", 1).Succeeded,
            "cooldown, use count, or mana leaked between runtime instances");
        Expect(scopeA.TryActivateManual("owner", "missing", 6).Failure == AbilityActivationFailure.MissingAbility,
            "missing ability lacked typed failure evidence");
    }

    private static void FailedActivationsAreAtomic()
    {
        var costly = CompileSingle(EntryAbility(
            "ability_atomic", AbilityActivationKind.ManualCommand, AbilityTriggerKind.None,
            manaCost: 3, cooldownTicks: 5, maxUses: 1));
        var lowManaWorld = new ProbeAbilityWorld();
        using (var lowMana = new BattleAbilityScope("atomic_mana", lowManaWorld, 2))
        {
            lowMana.RegisterLoadout("owner", costly);
            var result = lowMana.TryActivateManual("owner", "ability_atomic", 0);
            Expect(result.Failure == AbilityActivationFailure.InsufficientMana && lowMana.CurrentMana == 2 &&
                   lowManaWorld.PrepareCalls == 0 && lowManaWorld.CommitCalls == 0,
                "insufficient mana changed state or reached preparation");
        }

        AssertWorldFailureAtomic(AbilityActivationFailure.InsufficientGold, "金币不足。", prepareFailure: true);
        AssertWorldFailureAtomic(AbilityActivationFailure.SourceUnavailable, "能力拥有者已死亡。", prepareFailure: true);
        AssertWorldFailureAtomic(AbilityActivationFailure.ConditionsUnmet, "没有可用的召唤单位。", prepareFailure: true);
        AssertWorldFailureAtomic(AbilityActivationFailure.ConditionsUnmet, "没有合法落点。", prepareFailure: true);
        AssertWorldFailureAtomic(AbilityActivationFailure.CommitFailed, "提交失败。", prepareFailure: false);

        AssertBattleFailureAtomic(
            Loadout("loadout_paid_reinforcement"), "ability_paid_reinforcement",
            new SummonProfiles(Mercenary: Unit("mercenary")), startingGold: 0,
            fullBoard: false, "insufficient gold");
        AssertBattleFailureAtomic(
            Loadout("loadout_raise_dead"), "ability_raise_dead",
            new SummonProfiles(), startingGold: 0,
            fullBoard: false, "missing summon template");
        AssertBattleFailureAtomic(
            Loadout("loadout_raise_dead"), "ability_raise_dead",
            new SummonProfiles(DeathSummon: Unit("skeleton")), startingGold: 0,
            fullBoard: true, "no legal summon cell");

        EffectPreflightAndRuntimeLimitsAreAtomicInBattle();
    }

    private static void EffectPreflightAndRuntimeLimitsAreAtomicInBattle()
    {
        EffectAbilityOperationSpec EffectOperation(
            string bindingId,
            EffectBindingLimitsSpec? limits = null,
            Godot.Collections.Array<EffectConditionSpec>? conditions = null) => new()
        {
            Binding = new EffectBindingSpec
            {
                StableId = bindingId,
                Trigger = new EffectTriggerSpec { Kind = EffectTriggerKind.Manual },
                Conditions = conditions ?? [],
                TargetQuery = new OwnerTargetQuerySpec(),
                Effects = [new ShieldEffectSpec { AmountSource = EffectAmountSource.Fixed, Amount = 5 }],
                Limits = limits ?? new EffectBindingLimitsSpec()
            }
        };

        var conditionFailure = new AbilityDefinition
        {
            StableId = "ability_atomic_condition",
            DisplayName = "条件原子性",
            ActivationKind = AbilityActivationKind.ManualCommand,
            Trigger = AbilityTriggerKind.None,
            ManaCost = 1,
            Operations =
            [
                EffectOperation(
                    "ability_atomic_condition_effect",
                    conditions: [new EntityAliveConditionSpec { Entity = EffectEntityReference.Owner, ExpectedAlive = false }]),
                new CooldownAbilityOperationSpec
                {
                    TargetQuery = new OwnerTargetQuerySpec(),
                    AttackAdjustment = CooldownAdjustmentKind.Reset
                }
            ]
        };
        var conditionLoadout = CompileSingle(conditionFailure);
        using (var battle = new BattleSimulation(Config(
               [
                   Spawn(Hero("condition-hero"), 0, 0, 2, "hero"),
                   Spawn(Unit("condition-enemy", health: 10000, damage: 0, moveTicks: 1000), 1, 9, 2, "enemy")
               ], Rule(conditionLoadout, conditionFailure.StableId, 3))))
        {
            battle.DrainEvents();
            battle.Units.Single(unit => unit.RuntimeId == "hero").AttackCooldown = 9;
            var before = BattleFingerprint(battle);
            var activation = battle.TryUseTacticalCommand(0);
            Expect(!activation.Succeeded && !string.IsNullOrWhiteSpace(activation.FailureReason) &&
                   BattleFingerprint(battle) == before,
                "effect condition preflight consumed resources or committed a later operation");
        }

        var limited = new AbilityDefinition
        {
            StableId = "ability_atomic_effect_limit",
            DisplayName = "效果限次原子性",
            ActivationKind = AbilityActivationKind.ManualCommand,
            Trigger = AbilityTriggerKind.None,
            ManaCost = 1,
            Operations =
            [
                EffectOperation(
                    "ability_atomic_effect_limit_binding",
                    new EffectBindingLimitsSpec { MaxUses = 1 }),
                new CooldownAbilityOperationSpec
                {
                    TargetQuery = new OwnerTargetQuerySpec(),
                    AttackAdjustment = CooldownAdjustmentKind.Reset
                }
            ]
        };
        var limitedLoadout = CompileSingle(limited);
        using (var battle = new BattleSimulation(Config(
               [
                   Spawn(Hero("limit-hero"), 0, 0, 2, "hero"),
                   Spawn(Unit("limit-enemy", health: 10000, damage: 0, moveTicks: 1000), 1, 9, 2, "enemy")
               ], Rule(limitedLoadout, limited.StableId, 3))))
        {
            var hero = battle.Units.Single(unit => unit.RuntimeId == "hero");
            Expect(battle.TryUseTacticalCommand(0).Succeeded && battle.TacticalPoints == 2 && hero.Shield == 5,
                "effect usage-limit fixture did not commit its first activation");
            hero.AttackCooldown = 7;
            battle.DrainEvents();
            var before = BattleFingerprint(battle);
            var activation = battle.TryUseTacticalCommand(0);
            Expect(!activation.Succeeded && !string.IsNullOrWhiteSpace(activation.FailureReason) &&
                   BattleFingerprint(battle) == before,
                "effect usage-limit preflight consumed mana or committed a later operation");
        }

        var unsafeOrder = new AbilityDefinition
        {
            StableId = "ability_unsafe_late_effect",
            DisplayName = "非法后置效果",
            ActivationKind = AbilityActivationKind.ManualCommand,
            Trigger = AbilityTriggerKind.None,
            ManaCost = 1,
            Operations =
            [
                new CooldownAbilityOperationSpec
                {
                    TargetQuery = new OwnerTargetQuerySpec(),
                    AttackAdjustment = CooldownAdjustmentKind.Reset
                },
                EffectOperation("ability_unsafe_late_effect_binding")
            ]
        };
        var unsafeCompilation = AbilityDefinitionCompiler.Compile(unsafeOrder);
        Expect(unsafeCompilation.Ability is null && unsafeCompilation.Report.CoreErrors.Any(error =>
                   error.Contains("must be first", StringComparison.Ordinal)),
            "unsafe late effect operation was published without transactional batch support");
    }

    private static void AssertWorldFailureAtomic(
        AbilityActivationFailure failure,
        string reason,
        bool prepareFailure)
    {
        var loadout = CompileSingle(EntryAbility(
            "ability_atomic", AbilityActivationKind.ManualCommand, AbilityTriggerKind.None,
            manaCost: 1, cooldownTicks: 5, maxUses: 1));
        var world = new ProbeAbilityWorld();
        if (prepareFailure)
        {
            world.PrepareFailure = failure;
            world.FailureReason = reason;
        }
        else
        {
            world.CommitFailure = failure;
            world.FailureReason = reason;
        }
        using var scope = new BattleAbilityScope("atomic_" + failure, world, 3);
        scope.RegisterLoadout("owner", loadout);
        var failed = scope.TryActivateManual("owner", "ability_atomic", 0);
        Expect(!failed.Succeeded && failed.Failure == failure && scope.CurrentMana == 3 &&
               world.SuccessfulMutations == 0,
            $"{failure}: failed activation changed resources or world state");
        world.PrepareFailure = AbilityActivationFailure.None;
        world.CommitFailure = AbilityActivationFailure.None;
        Expect(scope.TryActivateManual("owner", "ability_atomic", 0).Succeeded && scope.CurrentMana == 2,
            $"{failure}: failed activation consumed cooldown or use count");
    }

    private static void AssertBattleFailureAtomic(
        CompiledAbilityLoadout loadout,
        string abilityId,
        SummonProfiles summons,
        int startingGold,
        bool fullBoard,
        string label)
    {
        var spawns = new List<BattleSpawn>();
        if (fullBoard)
        {
            for (var y = 0; y < BattleSimulation.Height; y++)
            for (var x = 0; x < BattleSimulation.Width; x++)
            {
                var hero = x == 0 && y == 0;
                spawns.Add(Spawn(
                    hero ? Hero("atomic-hero") : Unit($"occupant-{x}-{y}", health: 10000, damage: 0, moveTicks: 1000),
                    hero ? 0 : 1,
                    x,
                    y,
                    hero ? "hero" : $"occupant-{x}-{y}"));
            }
        }
        else
        {
            spawns.Add(Spawn(Hero("atomic-hero"), 0, 0, 2, "hero"));
            spawns.Add(Spawn(Unit("atomic-enemy", health: 10000, damage: 0, moveTicks: 1000), 1, 9, 2, "enemy"));
        }
        using var battle = new BattleSimulation(Config(spawns, Rule(loadout, abilityId, 3), summons, startingGold));
        battle.DrainEvents();
        var before = BattleFingerprint(battle);
        var result = battle.TryUseTacticalCommand(0);
        var after = BattleFingerprint(battle);
        Expect(!result.Succeeded && before == after, label + " left a partial battle mutation");
    }

    private static void StatusDurationRefreshStackDispelAndPeriodics()
    {
        var notifications = new Dictionary<string, ImmutableArray<StatusRuntimeSnapshot>>(StringComparer.Ordinal);
        var periodicCalls = 0;
        var periodicBinding = new CompiledEffectBinding(
            "status_periodic_probe",
            0,
            new CompiledEffectTrigger(EffectTriggerKind.Manual, EffectDomainEventKind.None),
            [],
            new CompiledOwnerTargetQuery(),
            [new CompiledEffectStep(EffectKind.Heal, EffectAmountSource.Fixed, 1)],
            new CompiledEffectBindingLimits(0, 0, 0, 0),
            null);
        using var scope = new BattleStatusScope(
            "status_contract",
            (owner, statuses) => notifications[owner] = statuses,
            invocation =>
            {
                periodicCalls++;
                return invocation.Binding == periodicBinding && invocation.SourceId == "source" && invocation.OwnerId == "owner";
            });

        var disable = Status(
            "status_refresh", StatusBehaviorKind.DisableActions, StatusDurationKind.TimedTicks,
            duration: 3, StatusDurationRefreshPolicy.KeepLonger, stackLimit: 1, dispellable: true);
        var first = scope.Apply(disable, "source", "owner", 0).Status!;
        Expect(first.SourceId == "source" && first.OwnerId == "owner" && first.RemainingTicks == 3,
            "status source/owner/duration attribution changed");
        scope.Apply(disable, "source", "owner", 0);
        Expect(scope.SnapshotOwner("owner").Single().RemainingTicks == 3,
            "short refresh shortened a timed status");
        Expect(scope.AdvanceOwner("owner", 1).ActionsDisabled &&
               scope.SnapshotOwner("owner").Single().RemainingTicks == 2,
            "timed status did not consume one owner action");
        scope.Apply(disable, "source", "owner", 1);
        Expect(scope.SnapshotOwner("owner").Single().RemainingTicks == 3,
            "longer refresh did not take the maximum duration");
        Expect(first.RemainingTicks == 3 && first.Stacks == 1,
            "previously published status presentation snapshot was mutable");
        Expect(scope.Dispel("owner", disable.StableId, "source") && scope.SnapshotOwner("owner").Length == 0,
            "dispellable status did not clear by source/owner identity");

        var stacks = Status(
            "status_stacks", StatusBehaviorKind.DamageMultiplier, StatusDurationKind.Permanent,
            duration: 0, StatusDurationRefreshPolicy.None, stackLimit: 2, dispellable: true, magnitude: 1.1f);
        var publishedOne = scope.Apply(stacks, "source", "owner", 2).Status!;
        scope.Apply(stacks, "source", "owner", 2);
        scope.Apply(stacks, "source", "owner", 2);
        scope.Apply(stacks, "second-source", "owner", 2);
        var stackSnapshots = scope.SnapshotOwner("owner");
        Expect(publishedOne.Stacks == 1 && stackSnapshots.Length == 2 &&
               stackSnapshots.Single(status => status.SourceId == "source").Stacks == 2,
            "status stack cap, source isolation, or immutable presentation changed");
        Expect(scope.Dispel("owner", stacks.StableId, "source") &&
               scope.SnapshotOwner("owner").Single().SourceId == "second-source",
            "source-specific dispel removed the wrong status instance");

        var permanent = Status(
            "status_nondispellable", StatusBehaviorKind.DamageMultiplier, StatusDurationKind.Permanent,
            duration: 0, StatusDurationRefreshPolicy.None, stackLimit: 1, dispellable: false, magnitude: 1.05f);
        scope.Apply(permanent, "source", "other", 0);
        Expect(!scope.Dispel("other", permanent.StableId), "non-dispellable status was removed");
        scope.RemoveOwner("other");
        Expect(scope.SnapshotOwner("other").Length == 0 && notifications["other"].Length == 0,
            "owner removal retained status state or presentation facts");

        var periodic = Status(
            "status_periodic", StatusBehaviorKind.DamageMultiplier, StatusDurationKind.Permanent,
            duration: 0, StatusDurationRefreshPolicy.None, stackLimit: 1, dispellable: false, magnitude: 1,
            periodicInterval: 2, periodicBinding: periodicBinding);
        scope.Apply(periodic, "source", "owner", 0);
        Expect(scope.AdvanceOwner("owner", 1).PeriodicInvocations == 0 &&
               scope.AdvanceOwner("owner", 2).PeriodicInvocations == 1 && periodicCalls == 1,
            "periodic status binding did not fire on its authored owner tick");

        using var exact = new BattleStatusScope("status_exact", (_, _) => { });
        exact.Apply(disable, "source", "owner", 0);
        Expect(exact.AdvanceOwner("owner", 1).ActionsDisabled &&
               exact.AdvanceOwner("owner", 2).ActionsDisabled &&
               exact.AdvanceOwner("owner", 3).ActionsDisabled &&
               !exact.AdvanceOwner("owner", 4).ActionsDisabled &&
               exact.SnapshotOwner("owner").Length == 0,
            "timed disable did not block exactly N owner actions before expiry");
    }

    private static void ProductionStatusBehaviorIsExact()
    {
        var timeStop = Loadout("loadout_time_stop");
        using (var battle = new BattleSimulation(Config(
               [
                   Spawn(Hero("time-hero", damage: 0, moveTicks: 1000), 0, 0, 2, "hero"),
                   Spawn(Unit("time-enemy", health: 10000, damage: 0, moveTicks: 1000), 1, 9, 2, "enemy")
               ], Rule(timeStop, "ability_time_stop", 3))))
        {
            var enemy = battle.Units.Single(unit => unit.RuntimeId == "enemy");
            enemy.AttackCooldown = 7;
            enemy.MoveCooldown = 5;
            Expect(battle.TryUseTacticalCommand(0).Succeeded, "production time-stop did not execute");
            var disabledActions = 0;
            for (var tick = 1; tick <= 18; tick++)
            {
                battle.Step();
                if (enemy.Mode == BattleUnitMode.Disabled) disabledActions++;
            }
            Expect(disabledActions == 18 && enemy.AttackCooldown == 7 && enemy.MoveCooldown == 5 &&
                   enemy.Statuses.Length == 0,
                "time-stop duration or cooldown pause changed");
            battle.Step();
            Expect(enemy.Mode != BattleUnitMode.Disabled && enemy.AttackCooldown == 6 && enemy.MoveCooldown == 4,
                "cooldowns did not resume on the first action after time-stop expiry");
        }

        var roar = Loadout("loadout_beast_roar");
        using var stacking = new BattleSimulation(Config(
        [
            Spawn(Hero("roar-hero", damage: 0, moveTicks: 1000), 0, 0, 2, "hero"),
            Spawn(Unit("tagged", damage: 10, tags: ["beast"]), 0, 1, 2, "tagged"),
            Spawn(Unit("untagged", damage: 10, tags: ["machine"]), 0, 1, 3, "untagged"),
            Spawn(Unit("roar-enemy", health: 10000, damage: 0, moveTicks: 1000), 1, 9, 2, "enemy")
        ], Rule(roar, "ability_beast_roar", 3)));
        Expect(stacking.TryUseTacticalCommand(0).Succeeded && stacking.TryUseTacticalCommand(0).Succeeded &&
               stacking.TryUseTacticalCommand(0).Succeeded,
            "production damage status could not stack through three command uses");
        var tagged = stacking.Units.Single(unit => unit.RuntimeId == "tagged");
        var untagged = stacking.Units.Single(unit => unit.RuntimeId == "untagged");
        Near(tagged.Damage, 10 * 1.12f * 1.12f * 1.12f, "ordered damage multiplier composition");
        Near(untagged.Damage, 10, "damage status tag filter");
        Expect(tagged.Statuses.Single().Stacks == 3 && untagged.Statuses.Length == 0,
            "damage status stack facts or tag targeting changed");

        DamageStatusCapsStayInSyncWithBattleAttributes();
    }

    private static void DamageStatusCapsStayInSyncWithBattleAttributes()
    {
        var cappedStatus = new StatusDefinition
        {
            StableId = "status_damage_cap",
            DisplayName = "限层增伤",
            Behavior = StatusBehaviorKind.DamageMultiplier,
            DurationKind = StatusDurationKind.Permanent,
            AggregationPolicy = StatusAggregationPolicy.BySource,
            StackLimit = 2,
            DispelCategory = StatusDispelCategory.NonDispellable,
            AttributeModifiers = [DamageModifier(1.1f)],
            Magnitude = 1.1f
        };
        var ability = AuthoredStatusAbility("ability_damage_cap", cappedStatus);
        var loadout = CompileSingle(ability);
        using (var battle = new BattleSimulation(Config(
               [
                   Spawn(Hero("cap-hero", damage: 10, moveTicks: 1000), 0, 0, 2, "hero"),
                   Spawn(Unit("cap-enemy", health: 10000, damage: 0, moveTicks: 1000), 1, 9, 2, "enemy")
               ], Rule(loadout, ability.StableId, 3))))
        {
            Expect(battle.TryUseTacticalCommand(0).Succeeded && battle.TryUseTacticalCommand(0).Succeeded &&
                   battle.TryUseTacticalCommand(0).Succeeded,
                "capped damage status fixture did not activate three times");
            var hero = battle.Units.Single(unit => unit.RuntimeId == "hero");
            Near(hero.Damage, 10 * 1.1f * 1.1f, "capped status battle damage");
            Expect(hero.Statuses.Single().Stacks == 2,
                "capped status facts diverged from battle damage");
        }

        var timed = new StatusDefinition
        {
            StableId = "status_timed_damage",
            DisplayName = "限时增伤",
            Behavior = StatusBehaviorKind.DamageMultiplier,
            DurationKind = StatusDurationKind.TimedTicks,
            DurationTicks = 3,
            AggregationPolicy = StatusAggregationPolicy.BySource,
            StackLimit = 1,
            DurationRefreshPolicy = StatusDurationRefreshPolicy.Reset,
            DispelCategory = StatusDispelCategory.Ordinary,
            AttributeModifiers = [DamageModifier(1.1f)],
            Magnitude = 1.1f
        };
        var timedCompilation = StatusDefinitionCompiler.Compile(timed);
        var timedDefinition = timedCompilation.Definition ?? throw new InvalidOperationException(
            "timed damage status did not compile: " + string.Join(" | ", timedCompilation.Report.CoreErrors));
        var attributeDefinition = AttributeDefinitionCompiler.Legacy(new Dictionary<CombatAttribute, float>
            { [CombatAttribute.AttackDamage] = 10 });
        using var attributes = new BattleAttributeScope("timed_status_attributes");
        var targetAttributes = attributes.CreateSet("owner", attributeDefinition);
        using var statuses = new BattleStatusScope(
            "timed_status_scope",
            (_, _) => { },
            id => id == "owner" ? targetAttributes : null,
            null,
            null,
            null);
        statuses.Apply(timedDefinition, "system_source", "owner", 0);
        Near(targetAttributes.GetValue(CombatAttribute.AttackDamage), 11, "timed status projection");
        statuses.AdvanceOwner("owner", 1);
        statuses.AdvanceOwner("owner", 2);
        statuses.AdvanceOwner("owner", 3);
        Near(targetAttributes.GetValue(CombatAttribute.AttackDamage), 10, "timed status expiry rollback");
        statuses.Apply(timedDefinition, "system_source", "owner", 4);
        Expect(statuses.Dispel("owner", timedDefinition.StableId, StatusDispelStrength.Ordinary),
            "ordinary dispel did not remove a reversible timed damage status");
        Near(targetAttributes.GetValue(CombatAttribute.AttackDamage), 10, "timed status dispel rollback");
    }

    private static void PublicationRejectsInvalidGraphsTransactionally()
    {
        var status = AuthoredStatus("status_graph");
        var ability = AuthoredStatusAbility("ability_graph", status);
        var loadout = new AbilityLoadoutDefinition { Abilities = [ability] };
        var valid = Graph([loadout], [ability], [status], [loadout], Ids("known_summon"));
        Expect(!ContentValidator.ValidateAbilityStatusAuthoredGraph(valid).HasCoreErrors,
            "valid authored ability/status graph was rejected");

        var duplicateAbility = AuthoredStatusAbility("ability_graph", status);
        var duplicateLoadout = new AbilityLoadoutDefinition { Abilities = [duplicateAbility] };
        var duplicateGraph = Graph(
            [loadout, duplicateLoadout], [ability, duplicateAbility], [status],
            [loadout, duplicateLoadout], Ids("known_summon"));
        var duplicateReport = ContentValidator.ValidateAbilityStatusAuthoredGraph(duplicateGraph);
        var duplicateBatch = AbilityDefinitionCompiler.CompileBatch([loadout, duplicateLoadout]);
        Expect(duplicateReport.CoreErrors.Any(error => error.Contains("Duplicate ability stable id", StringComparison.Ordinal)) &&
               duplicateBatch.Abilities.Length == 0,
            "stable-id collision did not reject the complete ability publication transaction");

        var orphanAbility = AuthoredStatusAbility("ability_orphan", status);
        Expect(ContentValidator.ValidateAbilityStatusAuthoredGraph(
                   Graph([loadout], [ability, orphanAbility], [status], [loadout], Ids("known_summon")))
               .CoreErrors.Any(error => error.Contains("Orphan ability definition", StringComparison.Ordinal)),
            "orphan ability definition passed publication");

        var orphanLoadout = new AbilityLoadoutDefinition { Abilities = [orphanAbility] };
        Expect(ContentValidator.ValidateAbilityStatusAuthoredGraph(
                   Graph([loadout, orphanLoadout], [ability, orphanAbility], [status], [loadout], Ids("known_summon")))
               .CoreErrors.Any(error => error.Contains("Orphan ability loadout", StringComparison.Ordinal)),
            "orphan ability loadout passed publication");

        Expect(ContentValidator.ValidateAbilityStatusAuthoredGraph(
                   Graph([loadout], [ability], [], [loadout], Ids("known_summon")))
               .CoreErrors.Any(error => error.Contains("unregistered status", StringComparison.Ordinal)),
            "missing status dependency passed publication");

        var orphanStatus = AuthoredStatus("status_orphan");
        var statusCollision = AuthoredStatus("status_graph");
        var statusBatch = StatusDefinitionCompiler.CompileBatch([status, statusCollision]);
        var orphanStatusReport = ContentValidator.ValidateAbilityStatusAuthoredGraph(
            Graph([loadout], [ability], [status, orphanStatus], [loadout], Ids("known_summon")));
        Expect(orphanStatusReport.CoreErrors.Any(error => error.Contains("Orphan status definition", StringComparison.Ordinal)) &&
               statusBatch.Definitions.Length == 0,
            "orphan status or status stable-id collision did not reject publication");

        var summonAbility = new AbilityDefinition
        {
            StableId = "ability_unknown_summon",
            DisplayName = "未知召唤",
            ActivationKind = AbilityActivationKind.Automatic,
            Trigger = AbilityTriggerKind.PeriodicTick,
            IntervalTicks = 10,
            Operations =
            [
                new SummonAbilityOperationSpec
                {
                    Profile = AbilitySummonProfile.BehaviorSummon,
                    Count = 1,
                    HealthMultiplier = 1,
                    DamageMultiplier = 1,
                    SummonContentId = "missing_content"
                }
            ]
        };
        var summonLoadout = new AbilityLoadoutDefinition { Abilities = [summonAbility] };
        Expect(ContentValidator.ValidateAbilityStatusAuthoredGraph(
                   Graph([summonLoadout], [summonAbility], [], [summonLoadout], Ids("known_summon")))
               .CoreErrors.Any(error => error.Contains("unknown content id", StringComparison.Ordinal)),
            "unknown summon content-id dependency passed publication");
    }

    private static void EveryBattleTerminationCleansAllProductScopes()
    {
        var roar = Loadout("loadout_beast_roar");
        var time = Loadout("loadout_time_stop");

        var victory = new BattleSimulation(Config(
        [
            Spawn(Hero("victory-hero", damage: 100, range: 10), 0, 0, 2, "hero"),
            Spawn(Unit("victory-enemy", health: 5, damage: 0), 1, 2, 2, "enemy")
        ], Rule(time, "ability_time_stop", 3)));
        Expect(victory.TryUseTacticalCommand(0).Succeeded, "victory lifecycle status setup failed");
        victory.Step();
        ExpectTransitions(victory, BattleScopeCompletionReason.PlayerVictory, TacticalCommandScopeCompletionReason.BattleCompleted,
            StatusScopeCompletionReason.BattleCompleted, "victory");
        victory.Dispose();

        var defeat = new BattleSimulation(Config(
        [
            Spawn(Hero("defeat-hero", health: 10, damage: 0, range: 10, tags: ["beast"]), 0, 0, 2, "z-hero"),
            Spawn(Unit("defeat-enemy", damage: 100, range: 10), 1, 2, 2, "a-enemy")
        ], Rule(roar, "ability_beast_roar", 3)));
        Expect(defeat.TryUseTacticalCommand(0).Succeeded, "defeat lifecycle status setup failed");
        defeat.Step();
        ExpectTransitions(defeat, BattleScopeCompletionReason.PlayerDefeat, TacticalCommandScopeCompletionReason.BattleCompleted,
            StatusScopeCompletionReason.BattleCompleted, "defeat");
        defeat.Dispose();

        var timeout = new BattleSimulation(Config(
        [
            Spawn(Hero("timeout-hero", health: 100000, damage: 0, range: 10, moveTicks: 1000, tags: ["beast"]), 0, 0, 2, "hero"),
            Spawn(Unit("timeout-enemy", health: 100000, damage: 0, range: 10, attackTicks: int.MaxValue, moveTicks: 1000), 1, 2, 2, "enemy")
        ], Rule(roar, "ability_beast_roar", 3)));
        Expect(timeout.TryUseTacticalCommand(0).Succeeded, "timeout lifecycle status setup failed");
        timeout.RunToEnd();
        ExpectTransitions(timeout, BattleScopeCompletionReason.Timeout, TacticalCommandScopeCompletionReason.BattleCompleted,
            StatusScopeCompletionReason.BattleCompleted, "timeout");
        timeout.Dispose();

        var aborted = ActiveStatusBattle(roar);
        aborted.Abort();
        ExpectTransitions(aborted, BattleScopeCompletionReason.Abort, TacticalCommandScopeCompletionReason.Abort,
            StatusScopeCompletionReason.Abort, "abort");
        aborted.Dispose();

        var replaced = ActiveStatusBattle(roar);
        replaced.Replace();
        ExpectTransitions(replaced, BattleScopeCompletionReason.Replacement, TacticalCommandScopeCompletionReason.Replacement,
            StatusScopeCompletionReason.Replacement, "replacement");
        replaced.Dispose();

        var disposed = ActiveStatusBattle(roar);
        disposed.Dispose();
        ExpectTransitions(disposed, BattleScopeCompletionReason.Disposal, TacticalCommandScopeCompletionReason.Disposal,
            StatusScopeCompletionReason.Disposal, "disposal");

        var throwing = ActiveStatusBattle(roar, new ThrowingTickRule());
        ExpectThrows(() => throwing.Step(), "battle tick exception");
        ExpectTransitions(throwing, BattleScopeCompletionReason.Exception, TacticalCommandScopeCompletionReason.Exception,
            StatusScopeCompletionReason.Exception, "tick exception");
        throwing.Dispose();

        var throwingEnd = ActiveStatusBattle(roar, new ThrowingEndRule());
        ExpectThrows(throwingEnd.Dispose, "battle end exception");
        ExpectTransitions(throwingEnd, BattleScopeCompletionReason.Exception, TacticalCommandScopeCompletionReason.Exception,
            StatusScopeCompletionReason.Exception, "end exception");
    }

    private static BattleSimulation ActiveStatusBattle(
        CompiledAbilityLoadout loadout,
        IBattleFloorRuleRuntime? floor = null)
    {
        var battle = new BattleSimulation(Config(
        [
            Spawn(Hero("lifecycle-hero", health: 10000, damage: 0, moveTicks: 1000, tags: ["beast"]), 0, 0, 2, "hero"),
            Spawn(Unit("lifecycle-enemy", health: 10000, damage: 0, moveTicks: 1000), 1, 9, 2, "enemy")
        ], Rule(loadout, "ability_beast_roar", 3), floor: floor));
        Expect(battle.TryUseTacticalCommand(0).Succeeded && battle.Units.Single(unit => unit.RuntimeId == "hero").Statuses.Length == 1,
            "lifecycle battle did not create live ability/status product state");
        return battle;
    }

    private static void ExpectTransitions(
        BattleSimulation battle,
        BattleScopeCompletionReason effectReason,
        TacticalCommandScopeCompletionReason tacticalReason,
        StatusScopeCompletionReason statusReason,
        string label)
    {
        var effect = battle.EffectTransition ?? throw new InvalidOperationException(label + ": missing effect transition");
        var tactical = battle.TacticalCommandTransition ?? throw new InvalidOperationException(label + ": missing tactical transition");
        var status = battle.StatusTransition ?? throw new InvalidOperationException(label + ": missing status transition");
        Expect(effect.Reason == effectReason && effect.Validate().IsValid &&
               effect.RemainingSubscriptions == 0 && effect.RemainingInvocations == 0 && effect.RemainingRuntimeInstances == 0,
            label + ": effect scope retained owned state");
        Expect(tactical.Reason == tacticalReason && tactical.RemainingRuntimeInstances == 0 &&
               tactical.RemainingPoints == 0,
            label + ": tactical scope retained owned state");
        Expect(status.Reason == statusReason && status.RemainingInstances == 0,
            label + ": status scope retained owned state");
    }

    private static AbilityDefinition EntryAbility(
        string id,
        AbilityActivationKind kind,
        AbilityTriggerKind trigger,
        int manaCost = 0,
        int cooldownTicks = 0,
        int maxUses = 0,
        int intervalTicks = 0) => new()
        {
            StableId = id,
            DisplayName = id,
            ActivationKind = kind,
            Trigger = trigger,
            ManaCost = manaCost,
            CooldownTicks = cooldownTicks,
            MaxUses = maxUses,
            IntervalTicks = intervalTicks,
            Operations =
            [
                new CooldownAbilityOperationSpec
                {
                    TargetQuery = new OwnerTargetQuerySpec(),
                    AttackAdjustment = CooldownAdjustmentKind.Reset
                }
            ]
        };

    private static StatusDefinition AuthoredStatus(string id) => new()
    {
        StableId = id,
        DisplayName = id,
        Behavior = StatusBehaviorKind.DamageMultiplier,
        DurationKind = StatusDurationKind.Permanent,
        AggregationPolicy = StatusAggregationPolicy.BySource,
        StackLimit = 0,
        AttributeModifiers = [DamageModifier(1.1f)],
        Magnitude = 1.1f
    };

    private static AbilityDefinition AuthoredStatusAbility(string id, StatusDefinition status) => new()
    {
        StableId = id,
        DisplayName = id,
        ActivationKind = AbilityActivationKind.ManualCommand,
        Trigger = AbilityTriggerKind.None,
        ManaCost = 1,
        Operations =
        [
            new ApplyStatusAbilityOperationSpec
            {
                Status = status,
                TargetQuery = new OwnerTargetQuerySpec()
            }
        ]
    };

    private static AbilityStatusAuthoredGraph Graph(
        IReadOnlyList<AbilityLoadoutDefinition?> loadouts,
        IReadOnlyList<AbilityDefinition?> abilities,
        IReadOnlyList<StatusDefinition?> statuses,
        IReadOnlyList<AbilityLoadoutDefinition?> references,
        IReadOnlySet<string> contentIds) =>
        new(loadouts, abilities, statuses, references, contentIds);

    private static IReadOnlySet<string> Ids(params string[] ids) =>
        new HashSet<string>(ids, StringComparer.Ordinal);

    private static CompiledAbilityLoadout CompileSingle(AbilityDefinition ability)
    {
        var result = AbilityDefinitionCompiler.CompileLoadout(new AbilityLoadoutDefinition { Abilities = [ability] });
        if (result.Report.HasCoreErrors || result.Loadout is null)
            throw new InvalidOperationException("ability fixture compile: " + string.Join(" | ", result.Report.CoreErrors));
        return result.Loadout;
    }

    private static CompiledStatusDefinition Status(
        string id,
        StatusBehaviorKind behavior,
        StatusDurationKind durationKind,
        int duration,
        StatusDurationRefreshPolicy refresh,
        int stackLimit,
        bool dispellable,
        float magnitude = 1,
        int periodicInterval = 0,
        CompiledEffectBinding? periodicBinding = null) => new(
        id,
        string.Empty,
        id,
        id,
        behavior,
        StatusDisposition.Neutral,
        durationKind,
        duration,
        StatusAggregationPolicy.BySource,
        stackLimit,
        StatusOverflowPolicy.RejectNewStacks,
        refresh,
        StatusPeriodicResetPolicy.KeepSchedule,
        dispellable ? StatusDispelCategory.Ordinary : StatusDispelCategory.NonDispellable,
        StatusDeathPolicy.Remove,
        StatusControlDurationRule.None,
        behavior == StatusBehaviorKind.DisableActions
            ? [StatusDefinitionCompiler.ActionDisabledTag]
            : [],
        [],
        magnitude,
        periodicInterval,
        periodicBinding,
        [],
        [],
        null,
        new CompiledStatusPresentation("status", "status_executed", "status_active", "status_while", "status_removed", id));

    private static AbilityLoadoutDefinition LoadoutResource(string name) =>
        GD.Load<AbilityLoadoutDefinition>($"res://content/abilities/loadouts/{name}.tres") ??
        throw new InvalidOperationException("loadout resource missing: " + name);

    private static CompiledAbilityLoadout Loadout(string name)
    {
        var result = AbilityDefinitionCompiler.CompileLoadout(LoadoutResource(name));
        if (result.Report.HasCoreErrors || result.Loadout is null)
            throw new InvalidOperationException($"{name}: " + string.Join(" | ", result.Report.CoreErrors));
        return result.Loadout;
    }

    private static StatusDefinition LoadStatus(string name) =>
        GD.Load<StatusDefinition>($"res://content/statuses/{name}.tres") ??
        throw new InvalidOperationException("status resource missing: " + name);

    private static string ProductionFingerprint(
        IEnumerable<AbilityLoadoutDefinition> loadouts,
        IEnumerable<StatusDefinition> statuses) =>
        string.Join("|", loadouts.SelectMany(loadout => loadout.Abilities).OrderBy(ability => ability.StableId)
            .Select(ability => $"{ability.StableId}:{ability.DisplayName}:{ability.ActivationKind}:{ability.Trigger}:" +
                               $"{ability.ManaCost}:{ability.GoldCost}:{ability.CooldownTicks}:{ability.MaxUses}:" +
                               $"{ability.IntervalTicks}:{string.Join(',', ability.Operations.Select(OperationFingerprint))}")) +
        "#" + string.Join("|", statuses.OrderBy(status => status.StableId).Select(status =>
            $"{status.StableId}:{status.DisplayName}:{status.Behavior}:{status.DurationKind}:{status.DurationTicks}:" +
            $"{status.AggregationPolicy}:{status.StackLimit}:{status.OverflowPolicy}:" +
            $"{status.DurationRefreshPolicy}:{status.PeriodicResetPolicy}:{status.DispelCategory}:" +
            $"{status.DeathPolicy}:{status.ControlDurationRule}:{status.Magnitude:R}:" +
            $"{status.PeriodicIntervalTicks}:{status.PeriodicEffect?.StableId}"));

    private static string OperationFingerprint(AbilityOperationSpec operation) => operation switch
    {
        EffectAbilityOperationSpec effect => $"effect:{effect.Binding.StableId}:{effect.InvocationValueSource}:{effect.InvocationValueScale:R}",
        CooldownAbilityOperationSpec cooldown => $"cooldown:{cooldown.AttackAdjustment}:{cooldown.AttackValue}:{cooldown.MoveAdjustment}:{cooldown.MoveValue}",
        ApplyStatusAbilityOperationSpec status => $"status:{status.Status.StableId}",
        SummonAbilityOperationSpec summon => $"summon:{summon.Profile}:{summon.Count}:{summon.HealthMultiplier:R}:{summon.DamageMultiplier:R}:{summon.MaximumLivingTemporaryUnits}:{summon.RequireAtLeastOne}:{summon.SummonContentId}",
        _ => operation.GetType().Name
    };

    private static AttributeModifierSpec DamageModifier(float multiplier) => new()
    {
        Attribute = CombatAttribute.AttackDamage,
        Operation = AttributeModifierOperation.Multiply,
        Magnitude = new ConstantAttributeMagnitudeSpec { Value = multiplier },
        SlotId = "damage_multiplier"
    };

    private static CompiledTacticalCommandDefinition Rule(
        CompiledAbilityLoadout loadout,
        string abilityId,
        int maximumMana)
    {
        var ability = loadout.Find(abilityId) ?? throw new InvalidOperationException("primary ability missing: " + abilityId);
        _ = maximumMana;
        return new CompiledTacticalCommandDefinition(
            "tactical_fixture_" + abilityId,
            string.Empty,
            ability.DisplayName,
            ability.Description,
            ability.ManaCost,
            ability,
            "fixture_" + abilityId);
    }

    private static BattleConfig Config(
        IEnumerable<BattleSpawn> spawns,
        CompiledTacticalCommandDefinition command,
        SummonProfiles? summons = null,
        int startingGold = 0,
        IBattleFloorRuleRuntime? floor = null) => new()
        {
            Seed = 2301,
            FloorRule = floor ?? new ClearFloorRuleRuntime("ability-status", "契约", "test"),
            Spawns = spawns.ToList(),
            HeroRule = new HeroRuleSnapshot(
                1, 1, 1, 0, 0, 0, false,
                string.Empty, 1, 1, 0, 0, 0, 0,
                false, false, 0, 0, string.Empty),
            Summons = summons ?? new SummonProfiles(),
            StartingGold = startingGold,
            TacticalCommands = TacticalPreparation(command)
        };

    private static TacticalCommandBattlePreparation TacticalPreparation(
        CompiledTacticalCommandDefinition primary)
    {
        var secondary = primary with
        {
            StableId = "tactical_fixture_secondary",
            DisplayName = "备用战术",
            Fingerprint = "fixture_secondary"
        };
        var commands = ImmutableArray.Create(primary, secondary);
        return new TacticalCommandBattlePreparation(
            TacticalCommandBattlePreparationBuilder.Fingerprint(commands),
            commands);
    }

    private static BattleSpawn Spawn(UnitSnapshot unit, int team, int x, int y, string id) =>
        new(unit, team, new Vector2I(x, y), id);

    private static UnitSnapshot Hero(
        string id,
        float health = 200,
        float damage = 10,
        float range = 1,
        int moveTicks = 1,
        IReadOnlyList<string>? tags = null) =>
        Unit(id, true, health, damage, range, 10, moveTicks, tags);

    private static UnitSnapshot Unit(
        string id,
        bool isHero = false,
        float health = 200,
        float damage = 10,
        float range = 1,
        int attackTicks = 10,
        int moveTicks = 1,
        IReadOnlyList<string>? tags = null) =>
        new(id, id, UnitRole.Fighter, isHero, false, health, damage, range, attackTicks, moveTicks,
            0, 0, 0, 0, tags ?? Array.Empty<string>(), new UnitBehaviorSnapshot());

    private static string BattleFingerprint(BattleSimulation battle) =>
        $"{battle.TickIndex}:{battle.TacticalPoints}:{battle.GoldSpent}:{battle.SuccessfulTacticalCommandUses}:" +
        $"{battle.CreateResult().Digest}:" + string.Join("|", battle.Units.Select(unit =>
            $"{unit.RuntimeId}:{unit.Cell.X},{unit.Cell.Y}:{unit.Health:R}:{unit.Damage:R}:{unit.Shield:R}:" +
            $"{unit.AttackCooldown}:{unit.MoveCooldown}:{unit.DisabledTicks}:{unit.IsTemporary}:" +
            $"{string.Join(',', unit.Statuses.Select(status => $"{status.StableId}:{status.SourceId}:{status.Stacks}:{status.RemainingTicks}"))}"));

    private static void Near(float actual, float expected, string label)
    {
        if (Math.Abs(actual - expected) > .001f)
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    }

    private static void ExpectThrows(Action action, string label)
    {
        try { action(); }
        catch { return; }
        throw new InvalidOperationException(label + " did not throw");
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class ProbeAbilityWorld : IAbilityRuntimeWorld
    {
        public AbilityActivationFailure PrepareFailure { get; set; }
        public AbilityActivationFailure CommitFailure { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public int PrepareCalls { get; private set; }
        public int CommitCalls { get; private set; }
        public int SuccessfulMutations { get; private set; }

        public AbilityWorldSnapshot CaptureSnapshot(int tick) => new(
            tick,
            new[] { new AbilityEntitySnapshot("owner", 0, true, 100, []) }
                .ToImmutableDictionary(entity => entity.RuntimeId, StringComparer.Ordinal));

        public AbilityPreparationResult Prepare(
            CompiledAbilityDefinition ability,
            string sourceId,
            string ownerId,
            string explicitTargetId,
            int tick)
        {
            PrepareCalls++;
            if (PrepareFailure != AbilityActivationFailure.None)
                return new AbilityPreparationResult(false, PrepareFailure, FailureReason, null);
            var operations = ability.Operations.Select((operation, index) =>
                new ResolvedAbilityOperation(index, operation, [ownerId], 0)).ToImmutableArray();
            return new AbilityPreparationResult(
                true,
                AbilityActivationFailure.None,
                string.Empty,
                new AbilityExecutionPlan(ability, sourceId, ownerId, tick, operations, [], ability.GoldCost));
        }

        public AbilityCommitResult Commit(AbilityExecutionPlan plan)
        {
            CommitCalls++;
            if (CommitFailure != AbilityActivationFailure.None)
                return new AbilityCommitResult(false, CommitFailure, FailureReason, []);
            SuccessfulMutations++;
            return new AbilityCommitResult(true, AbilityActivationFailure.None, string.Empty, [plan.Ability.StableId]);
        }
    }

    private sealed class ThrowingTickRule() : ClearFloorRuleRuntime("throw-tick", "异常", "test")
    {
        public override void OnTick(BattleRuleContext context) =>
            throw new InvalidOperationException("expected tick failure");
    }

    private sealed class ThrowingEndRule() : ClearFloorRuleRuntime("throw-end", "异常", "test")
    {
        public override void OnBattleEnded(BattleRuleContext context, BattleOutcome outcome) =>
            throw new InvalidOperationException("expected end failure");
    }
}
