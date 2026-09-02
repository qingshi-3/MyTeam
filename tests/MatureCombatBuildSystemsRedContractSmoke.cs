using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using TowerAutobattler.Abilities;
using TowerAutobattler.Attributes;
using TowerAutobattler.Battle;
using TowerAutobattler.Content;
using TowerAutobattler.Domain;
using TowerAutobattler.Effects;
using TowerAutobattler.Equipment;
using TowerAutobattler.Project;
using TowerAutobattler.Relics;
using TowerAutobattler.Run;
using TowerAutobattler.Statuses;
using TowerAutobattler.TacticalCommands;
using TowerAutobattler.Traits;
using TowerAutobattler.UI;

public partial class MatureCombatBuildSystemsRedContractSmoke : Node
{
    private sealed record CapabilityContract(
        string Id,
        string ExpectedBehavior,
        IReadOnlyList<string> EvidenceRoots,
        IReadOnlyList<string> RequiredTokens,
        Action? BehaviorProbe = null);

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
            var missing = Contracts()
                .Select(contract => Inspect(contract))
                .Where(result => result.MissingTokens.Count > 0)
                .ToArray();

            if (missing.Length > 0)
            {
                foreach (var result in missing)
                    GD.PrintErr($"MATURE_COMBAT_BUILD_RED_GAP [{result.Contract.Id}] " +
                                $"missing={string.Join(',', result.MissingTokens)}; " +
                                $"contract={result.Contract.ExpectedBehavior}");
                GD.PrintErr($"MATURE_COMBAT_BUILD_RED_EXPECTED missing={missing.Length}/9 " +
                            $"ids={string.Join(',', missing.Select(result => result.Contract.Id))}");
                return 1;
            }

            GD.Print("MATURE_COMBAT_BUILD_CONTRACT_OK attributes=rollback-source events=typed-battle-local " +
                     "statuses=mature-aggregation traits=breakpoints equipment=hero-owned relics=reactive-counters " +
                     "roster=unified-10-18 tactics=two-slots-three-points frost=production-vertical-slice");
            return 0;
        }
        catch (Exception exception)
        {
            GD.PrintErr("MATURE_COMBAT_BUILD_RED_CONTRACT_FAILED: " + exception);
            return 1;
        }
    }

    private static IReadOnlyList<CapabilityContract> Contracts() =>
    [
        new(
            "attribute-rollback-source-identity",
            "base/current attributes evaluate add→multiply→override→clamp and remove one source without disturbing another",
            ["src/Attributes", "src/Battle"],
            [],
            VerifyAttributeBehavior),
        new(
            "battle-local-typed-attack-status-death-events",
            "one Battle-local pure C# pipeline publishes immutable attack/status/defeat/kill facts and queues non-reentrant reactions",
            ["src/Battle", "src/Effects", "src/Statuses"],
            [],
            VerifyCombatEventBehavior),
        new(
            "mature-status-stacking",
            "Statuses support source/target/independent aggregation, overflow, refresh/periodic reset, dispel/death policy, and state tags",
            ["src/Statuses"],
            [],
            VerifyMatureStatusBehavior),
        new(
            "tft-trait-breakpoints",
            "team Trait snapshots select one ordered breakpoint and reversibly replace the previous tier source",
            ["src/Traits", "src/Content", "src/Battle"],
            [],
            VerifyTraitBehavior),
        new(
            "hero-owned-equipment",
            "three authored equipment slots belong to one persistent hero instance and project/remove only that owner's Battle sources",
            ["src/Equipment", "src/Run", "src/Battle"],
            [],
            VerifyHeroOwnedEquipmentBehavior),
        new(
            "reactive-relic-counters",
            "Relics subscribe to typed events with source-isolated Battle/Run counters, thresholds, consumption, reset, and cleanup",
            ["src/Relics", "src/Run", "src/Battle"],
            [],
            VerifyReactiveRelicBehavior),
        new(
            "unified-hero-population",
            "one persistent hero roster enforces current population, ordinary cap 10, explicit above-cap sources, and physical ceiling 18",
            ["src/Run", "src/Battle", "src/Content"],
            [],
            VerifyUnifiedHeroPopulationBehavior),
        new(
            "independent-tactical-loadout",
            "Run owns exactly two hero-independent command ids and each Battle atomically spends exactly three shared tactical points",
            ["src/Run", "src/Battle", "src/Abilities"],
            [],
            VerifyIndependentTacticalLoadoutBehavior),
        new(
            "frost-freeze-production-slice",
            "production Equipment→AttackLanded→Frost threshold→controlled Freeze→Trait→presentation path is deterministic and reversible",
            ["src", "content"],
            [],
            VerifyFrostFreezeProductionBehavior)
    ];

    private static void VerifyFrostFreezeProductionBehavior()
    {
        var productionCatalog = GD.Load<ContentCatalog>("res://content/catalogs/alpha_catalog.tres") ??
            throw new InvalidOperationException("production catalog is missing for the Frost/Freeze probe");
        var production = ContentValidator.CompileProductionGraph(productionCatalog, []);
        var graph = production.Graph ?? throw new InvalidOperationException(
            "production package did not compile for the Frost/Freeze probe: " +
            string.Join(" | ", production.Report.CoreErrors));
        if (!graph.TryGetEquipment("equipment_rimebrand", out var equipment))
            throw new InvalidOperationException("production Frost Equipment is not published");
        if (!graph.TryGetStatus("status_frost", out var frost) ||
            !graph.TryGetStatus("status_freeze", out var freeze) ||
            !graph.TryGetStatus("status_rime_momentum", out var momentum))
            throw new InvalidOperationException("production Frost/Freeze/AttackSpeed Status graph is not published");
        if (!graph.TryGetTrait("trait_winterbound", out var trait))
            throw new InvalidOperationException("production Frost Trait is not published");
        if (equipment.ReactiveStatusBindings.Length != 2 ||
            equipment.ReactiveStatusBindings.Any(binding => binding.EventKind != BattleCombatEventKind.AttackLanded) ||
            equipment.ReactiveStatusBindings.Count(binding => binding.Target == EquipmentReactiveStatusTarget.Owner &&
                                                               ReferenceEquals(binding.Status, momentum)) != 1 ||
            equipment.ReactiveStatusBindings.Count(binding => binding.Target == EquipmentReactiveStatusTarget.EventTarget &&
                                                               ReferenceEquals(binding.Status, frost)) != 1 ||
            frost.OverflowTransition is null || !ReferenceEquals(frost.OverflowTransition.Target, freeze) ||
            equipment.TraitContributions is not [{ Value: 1 }] ||
            equipment.TraitContributions[0].TraitId != trait.StableId)
            throw new InvalidOperationException("production Frost compiled dependencies are not canonical");

        var authored = new Resource?[]
        {
            GD.Load<EquipmentDefinition>("res://content/equipment/definitions/equipment_rimebrand.tres"),
            GD.Load<StatusDefinition>("res://content/statuses/status_frost.tres"),
            GD.Load<StatusDefinition>("res://content/statuses/status_freeze.tres"),
            GD.Load<StatusDefinition>("res://content/statuses/status_rime_momentum.tres"),
            GD.Load<TraitDefinition>("res://content/traits/definitions/trait_winterbound.tres")
        };
        if (authored.Any(resource => resource is null))
            throw new InvalidOperationException("production Frost authored graph has a missing Resource");
        var authoredBefore = ResourceGraphFingerprint.Compute(authored);
        VerifyEquipmentReactiveBatchRollback(equipment, momentum);
        VerifyLethalAttackTargetEligibility(equipment, frost, freeze, momentum);
        var first = RunProductionFrostBattle(equipment, frost, freeze, momentum, trait);
        var second = RunProductionFrostBattle(equipment, frost, freeze, momentum, trait);
        if (!string.Equals(first, second, StringComparison.Ordinal))
            throw new InvalidOperationException("same-seed production Frost Battle changed event/cue/state ordering");
        if (ResourceGraphFingerprint.Compute(authored) != authoredBefore)
            throw new InvalidOperationException("production Frost Battle mutated shared authored Resources");
    }

    private static void VerifyLethalAttackTargetEligibility(
        CompiledEquipmentDefinition equipment,
        CompiledStatusDefinition frost,
        CompiledStatusDefinition freeze,
        CompiledStatusDefinition momentum)
    {
        var equipmentInstance = new EquipmentBattleInstanceSnapshot(
            "lethal-rime",
            equipment.StableId,
            "a_lethal_owner",
            0,
            equipment);
        using var battle = new BattleSimulation(new BattleConfig
        {
            Seed = 0x1E7A1UL,
            Identity = new BattleIdentity("production_frost_lethal_contract", TowerNodeType.Combat, 0x1E7A1UL, 5, 1),
            FloorRule = new ClearFloorRuleRuntime("production_frost_lethal", "production_frost_lethal", "test"),
            HeroRule = ProductionRule(),
            Equipment = new EquipmentBattlePreparation(
                EquipmentStateFingerprint.Compute([equipmentInstance]),
                [equipmentInstance]),
            Spawns =
            [
                new BattleSpawn(ProductionUnit("frost_lethal_owner", true, 100, 100, 10), 0,
                    new Vector2I(1, 1), "a_lethal_owner", IsPersistentRosterHero: true),
                new BattleSpawn(ProductionUnit("frost_lethal_target", false, 1, 0, 1), 1,
                    new Vector2I(2, 1), "b_lethal_target"),
                new BattleSpawn(ProductionUnit("frost_lethal_reserve", false, 10_000, 0, 1), 1,
                    new Vector2I(9, 5), "z_lethal_reserve")
            ]
        });

        battle.Step();
        var owner = battle.Units.Single(unit => unit.RuntimeId == "a_lethal_owner");
        var defeatedTarget = battle.Units.Single(unit => unit.RuntimeId == "b_lethal_target");
        if (battle.Outcome != BattleOutcome.Running || defeatedTarget.Alive ||
            owner.Statuses.SingleOrDefault(status => status.StableId == momentum.StableId) is null)
            throw new InvalidOperationException(
                "lethal AttackLanded did not preserve the surviving Equipment owner's momentum fact");
        if (defeatedTarget.Statuses.Any(status =>
                status.StableId == frost.StableId || status.StableId == freeze.StableId))
            throw new InvalidOperationException(
                "lethal AttackLanded reapplied Frost/Freeze after defeated-target Status cleanup");
        if (battle.CombatEvents.Any(item =>
                item.Tick == 1 && item.TargetRuntimeId == defeatedTarget.RuntimeId &&
                item.Kind == BattleCombatEventKind.StatusApplied &&
                (item.SubjectStableId == frost.StableId || item.SubjectStableId == freeze.StableId)))
            throw new InvalidOperationException(
                "lethal AttackLanded published a defeated-target StatusApplied fact");
        if (battle.StatusPresentationCues.Any(item =>
                item.Tick == 1 && item.Status.OwnerId == defeatedTarget.RuntimeId &&
                item.Lifecycle == StatusPresentationCueLifecycle.OnActive &&
                (item.Status.StableId == frost.StableId || item.Status.StableId == freeze.StableId)))
            throw new InvalidOperationException(
                "lethal AttackLanded published a defeated-target OnActive presentation cue");

        battle.Abort();
        AssertBattleScopesZero(battle, "production Frost lethal eligibility abort", BattleCombatCompletionReason.Abort);
    }

    private static string RunProductionFrostBattle(
        CompiledEquipmentDefinition equipment,
        CompiledStatusDefinition frost,
        CompiledStatusDefinition freeze,
        CompiledStatusDefinition momentum,
        CompiledTraitDefinition trait)
    {
        var equipmentInstances = ImmutableArray.Create(
            new EquipmentBattleInstanceSnapshot("rime-a", equipment.StableId, "a_frost_owner", 0, equipment),
            new EquipmentBattleInstanceSnapshot("rime-c", equipment.StableId, "c_frost_owner", 0, equipment));
        var equipmentPreparation = new EquipmentBattlePreparation(
            EquipmentStateFingerprint.Compute(equipmentInstances),
            equipmentInstances);
        var traitInputs = equipmentInstances.Select(instance => new TraitContributionInput(
            trait.StableId,
            1,
            0,
            TraitContributionSourceKind.Equipment,
            instance.InstanceId,
            instance.OwnerHeroInstanceId,
            equipment.StableId,
            true,
            false,
            true));
        var traitPreparation = TraitBattlePreparationBuilder.Build([trait], traitInputs);
        var resistantEnemy = ProductionUnit("frost_target", false, 10_000, 0, 5) with
        {
            AttributeDefinition = AttributeDefinitionCompiler.Legacy(new Dictionary<CombatAttribute, float>
            {
                [CombatAttribute.ControlResistance] = .5f
            })
        };
        using var battle = new BattleSimulation(new BattleConfig
        {
            Seed = 0xF2057UL,
            Identity = new BattleIdentity("production_frost_contract", TowerNodeType.Combat, 0xF2057UL, 5, 1),
            FloorRule = new ClearFloorRuleRuntime("production_frost", "production_frost", "test"),
            HeroRule = ProductionRule(),
            Equipment = equipmentPreparation,
            Traits = traitPreparation,
            Spawns =
            [
                new BattleSpawn(ProductionUnit("frost_owner_a", true, 100, 1, 5), 0,
                    new Vector2I(1, 1), "a_frost_owner", IsPersistentRosterHero: true),
                new BattleSpawn(ProductionUnit("frost_outsider", true, 100, 1, 5), 0,
                    new Vector2I(1, 2), "b_frost_outsider", IsPersistentRosterHero: true),
                new BattleSpawn(ProductionUnit("frost_owner_c", true, 100, 1, 5), 0,
                    new Vector2I(1, 3), "c_frost_owner", IsPersistentRosterHero: true),
                new BattleSpawn(resistantEnemy, 1, new Vector2I(3, 2), "z_frost_target")
            ]
        });

        if (battle.EquipmentSubscriptionCount != 2 || battle.EquipmentModifierCount != 0)
            throw new InvalidOperationException("production Frost Equipment subscription ownership changed");
        var traitValue = battle.TraitSnapshot.Resolve(trait.StableId, 0);
        if (traitValue.Value != 2 || traitValue.ActiveBreakpoint is null)
            throw new InvalidOperationException("ordinary production Frost Trait breakpoint did not activate");
        foreach (var unit in battle.Units.Where(unit => unit.Team == 0))
            Near(unit.Attributes.GetValue(CombatAttribute.AttackSpeed), 1.15f,
                "production Frost Trait AttackSpeed projection");

        battle.Step();
        var ownerA = battle.Units.Single(unit => unit.RuntimeId == "a_frost_owner");
        var outsider = battle.Units.Single(unit => unit.RuntimeId == "b_frost_outsider");
        var ownerC = battle.Units.Single(unit => unit.RuntimeId == "c_frost_owner");
        var enemy = battle.Units.Single(unit => unit.RuntimeId == "z_frost_target");
        Near(ownerA.Attributes.GetValue(CombatAttribute.AttackSpeed), 1.23f,
            "owner A AttackLanded momentum");
        Near(ownerC.Attributes.GetValue(CombatAttribute.AttackSpeed), 1.23f,
            "owner C AttackLanded momentum");
        Near(outsider.Attributes.GetValue(CombatAttribute.AttackSpeed), 1.15f,
            "non-owner attack did not gain Equipment momentum");
        var sharedFrost = enemy.Statuses.Single(status => status.StableId == frost.StableId);
        if (sharedFrost.Stacks != 2 ||
            !sharedFrost.SourceContributions.Select(source => (source.SourceId, source.Stacks))
                .SequenceEqual([("rime-a", 1), ("rime-c", 1)]) ||
            outsider.Statuses.Any(status => status.StableId == momentum.StableId))
            throw new InvalidOperationException("production Frost target aggregation/source isolation changed");

        battle.Step();
        enemy = battle.Units.Single(unit => unit.RuntimeId == "z_frost_target");
        var frozen = enemy.Statuses.Single(status => status.StableId == freeze.StableId);
        var nextFrost = enemy.Statuses.Single(status => status.StableId == frost.StableId);
        var freezeActive = battle.StatusPresentationCues.Single(cue =>
            cue.Lifecycle == StatusPresentationCueLifecycle.OnActive && cue.Status.StableId == freeze.StableId);
        var consumed = battle.StatusPresentationCues.Single(cue =>
            cue.Lifecycle == StatusPresentationCueLifecycle.Removed &&
            cue.Status.StableId == frost.StableId &&
            cue.RemovalReason == StatusRemovalReason.OverflowConsumed);
        if (freezeActive.Status.RemainingTicks != 3 || frozen.RemainingTicks != 2 ||
            !frozen.GrantedTags.Contains(StatusDefinitionCompiler.ActionDisabledTag, StringComparer.Ordinal) ||
            enemy.Mode != BattleUnitMode.Disabled || nextFrost.Stacks != 1 ||
            consumed.Status.Stacks != 3 ||
            !consumed.Status.SourceContributions.Select(source => (source.SourceId, source.Stacks))
                .SequenceEqual([("rime-a", 2), ("rime-c", 1)]))
            throw new InvalidOperationException("production Frost threshold, resistance, attribution, or action authority changed");
        var selectedAction = SelectedUnitPanel.DescribeAction(enemy);
        if (!selectedAction.Contains("0.2 秒", StringComparison.Ordinal) ||
            !enemy.Statuses.Any(status => status.ReportLabel == "冻结" &&
                                          status.SemanticIcon == "status.freeze"))
            throw new InvalidOperationException("immutable Freeze UI facts did not expose the effective control duration");

        var runningFingerprint = FrostBattleFingerprint(battle);
        battle.Abort();
        var equipmentTransition = battle.EquipmentTransition ??
                                  throw new InvalidOperationException("production Frost Equipment transition is missing");
        var statusTransition = battle.StatusTransition ??
                               throw new InvalidOperationException("production Frost Status transition is missing");
        var traitTransition = battle.TraitTransition ??
                              throw new InvalidOperationException("production Frost Trait transition is missing");
        if (equipmentTransition.RemainingInstances != 0 ||
            equipmentTransition.RemainingModifierHandles != 0 ||
            equipmentTransition.RemainingSubscriptions != 0 ||
            statusTransition.RemainingInstances != 0 || statusTransition.RemainingModifierHandles != 0 ||
            statusTransition.RemainingContributions != 0 || statusTransition.RemainingReactiveSubscriptions != 0 ||
            traitTransition.RemainingTiers != 0 || traitTransition.RemainingModifierHandles != 0 ||
            battle.Units.Any(unit => !unit.Statuses.IsEmpty))
            throw new InvalidOperationException("production Frost completion retained Equipment/Status/Trait state");
        AssertBattleScopesZero(battle, "production Frost abort", BattleCombatCompletionReason.Abort);
        if (!battle.StatusPresentationCues.Any(cue => cue.Status.StableId == freeze.StableId &&
                                                      cue.Lifecycle == StatusPresentationCueLifecycle.Removed &&
                                                      cue.RemovalReason == StatusRemovalReason.ScopeCompleted))
            throw new InvalidOperationException("production Freeze completion omitted its immutable Removed cue");
        return runningFingerprint + "||completed=" + FrostBattleFingerprint(battle) +
               $"|equipment={equipmentTransition}|status={statusTransition}|trait={traitTransition}";
    }

    private static string FrostBattleFingerprint(BattleSimulation battle)
    {
        var units = string.Join(";", battle.Units
            .OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal)
            .Select(unit =>
            {
                var statuses = string.Join(",", unit.Statuses.Select(status =>
                {
                    var sources = string.Join("+", status.SourceContributions.Select(source =>
                        $"{source.SourceId}:{source.Stacks}"));
                    return $"{status.StableId}:{status.Stacks}:{status.RemainingTicks}:{sources}";
                }));
                return $"{unit.RuntimeId}:{unit.Health:R}:" +
                       $"{unit.Attributes.GetValue(CombatAttribute.AttackSpeed):R}:{unit.Mode}:{statuses}";
            }));
        var cues = string.Join(";", battle.StatusPresentationCues.Select(cue =>
            $"{cue.Tick}:{cue.Lifecycle}:{cue.Cue}:{cue.Status.StableId}:{cue.Status.Stacks}:" +
            $"{cue.Status.RemainingTicks}:{cue.RemovalReason}"));
        return string.Join("|",
            $"tick={battle.TickIndex}",
            $"outcome={battle.Outcome}",
            $"trait={battle.TraitSnapshot.Fingerprint}",
            "units=" + units,
            "events=" + string.Join(";", battle.CombatEvents.Select(EventFingerprint)),
            "cues=" + cues);
    }

    private static void VerifyEquipmentReactiveBatchRollback(
        CompiledEquipmentDefinition productionEquipment,
        CompiledStatusDefinition momentum)
    {
        var invalid = CompileStatus(new StatusDefinition
        {
            StableId = "status_equipment_rollback_probe",
            DisplayName = "装备回滚探针",
            Behavior = StatusBehaviorKind.None,
            Disposition = StatusDisposition.Helpful,
            DurationKind = StatusDurationKind.Permanent,
            AggregationPolicy = StatusAggregationPolicy.BySource,
            StackLimit = 1,
            AttributeModifiers =
            [
                new AttributeModifierSpec
                {
                    Attribute = CombatAttribute.AttackSpeed,
                    Operation = AttributeModifierOperation.Add,
                    Magnitude = new SourceAttributeMagnitudeSpec
                    {
                        Attribute = CombatAttribute.AttackDamage,
                        CaptureMode = AttributeCaptureMode.Snapshot
                    },
                    SlotId = "equipment_rollback_attack_speed"
                }
            ],
            Magnitude = 1
        });
        var rollbackEquipment = productionEquipment with
        {
            ReactiveStatusBindings =
            [
                productionEquipment.ReactiveStatusBindings.Single(binding =>
                    binding.Target == EquipmentReactiveStatusTarget.Owner) with { Priority = 10 },
                new CompiledEquipmentReactiveStatusBinding(
                    BattleCombatEventKind.AttackLanded,
                    EquipmentReactiveStatusTarget.Owner,
                    EquipmentReactiveStatusSource.EquipmentInstance,
                    20,
                    invalid)
            ]
        };
        using var attributes = new BattleAttributeScope("equipment-reactive-rollback-attributes");
        var definition = AttributeDefinitionCompiler.Legacy(new Dictionary<CombatAttribute, float>());
        var owner = attributes.CreateSet("rollback-owner", definition);
        var target = attributes.CreateSet("rollback-target", definition);
        using var statuses = new BattleStatusScope(
            "equipment-reactive-rollback-statuses",
            (_, _) => { },
            id => id switch
            {
                "rollback-owner" => owner,
                "rollback-target" => target,
                _ => null
            },
            null,
            null,
            null);
        var snapshot = new EquipmentBattleInstanceSnapshot(
            "rollback-equipment",
            rollbackEquipment.StableId,
            "rollback-owner",
            0,
            rollbackEquipment);
        var preparation = new EquipmentBattlePreparation(
            EquipmentStateFingerprint.Compute([snapshot]),
            [snapshot]);
        using var equipment = new EquipmentBattleScope(
            "equipment-reactive-rollback",
            preparation,
            [new EquipmentOwnerBinding("rollback-owner", "rollback-owner", true, owner)]);
        using var combat = new BattleCombatEventPipeline("equipment-reactive-rollback-combat");
        var bindings = new BattleCombatBindingRegistry(combat);
        equipment.Activate(new EquipmentBattleRuntimeContext
        {
            CombatBindings = bindings,
            CanReceiveStatus = runtimeId => runtimeId is "rollback-owner" or "rollback-target",
            ApplyStatuses = requests => statuses.ApplyBatch(requests)
        });
        bindings.CloseRegistration();
        Near(owner.GetValue(CombatAttribute.AttackSpeed), 1, "Equipment reactive rollback baseline");
        ExpectThrows(() => combat.Publish(new BattleCombatEventDraft(
                BattleCombatEventKind.AttackLanded,
                CombatSourceRef.Unit("rollback-unit", "rollback-owner", "rollback-owner"),
                "rollback-owner",
                "rollback-target",
                1)),
            "Equipment reactive Status batch rollback");
        Near(owner.GetValue(CombatAttribute.AttackSpeed), 1, "Equipment reactive batch Attribute rollback");
        if (!statuses.SnapshotOwner("rollback-owner").IsEmpty ||
            equipment.LiveSubscriptionCount != 1 || combat.PendingReactionCount != 0)
            throw new InvalidOperationException("failed Equipment reactive batch retained Status or pending work");
        equipment.Complete(EquipmentBattleCompletionReason.Abort);
        statuses.Complete(StatusScopeCompletionReason.Abort, 1);
        combat.Complete(BattleCombatCompletionReason.Abort, 1);
        attributes.Complete(AttributeScopeCompletionReason.Abort, 1);
    }

    private static InspectionResult Inspect(CapabilityContract contract)
    {
        if (contract.BehaviorProbe is not null)
        {
            try
            {
                contract.BehaviorProbe();
                return new InspectionResult(contract, []);
            }
            catch (Exception exception)
            {
                return new InspectionResult(contract, [$"behavior:{exception.GetType().Name}:{exception.Message}"]);
            }
        }
        var source = string.Join('\n', contract.EvidenceRoots.Select(SourceTree));
        var missing = contract.RequiredTokens
            .Where(token => !source.Contains(token, StringComparison.Ordinal))
            .ToArray();
        return new InspectionResult(contract, missing);
    }

    private static string SourceTree(string relativeRoot)
    {
        var root = ProjectSettings.GlobalizePath("res://" + relativeRoot.TrimStart('/'));
        if (File.Exists(root)) return File.ReadAllText(root);
        if (!Directory.Exists(root)) return string.Empty;
        return string.Join('\n', Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".tres", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(File.ReadAllText));
    }

    private sealed record InspectionResult(
        CapabilityContract Contract,
        IReadOnlyList<string> MissingTokens);

    private static void VerifyReactiveRelicBehavior()
    {
        var authored = new RelicDefinition
        {
            StableId = "fixture_reactive_relic",
            ReactiveCounters =
            [
                new RelicReactiveCounterSpec
                {
                    CounterId = "attacks",
                    Scope = RelicCounterScope.Battle,
                    ResetPolicy = RelicCounterResetPolicy.BattleEnd,
                    Source = RelicCounterSourceKind.Attack,
                    Team = 0,
                    Threshold = 2,
                    Consumption = 2,
                    Priority = 4,
                    Target = RelicThresholdTargetKind.EventTarget,
                    TargetTeam = 1,
                    ThresholdEffect = new EffectBindingSpec
                    {
                        StableId = "fixture_reactive_relic_threshold",
                        Trigger = new EffectTriggerSpec
                        {
                            Kind = EffectTriggerKind.Manual,
                            EventKind = EffectDomainEventKind.None
                        },
                        TargetQuery = new ExplicitTargetQuerySpec(),
                        Effects =
                        [
                            new ShieldEffectSpec
                            {
                                AmountSource = EffectAmountSource.Fixed,
                                Amount = 3
                            }
                        ],
                        Limits = new EffectBindingLimitsSpec()
                    }
                }
            ]
        };
        var result = RelicDefinitionCompiler.Compile(authored);
        var compiled = result.Definition ?? throw new InvalidOperationException(
            "fixture Reactive Relic did not compile: " + string.Join("; ", result.Report.CoreErrors));
        if (compiled.ReactiveCounters.Length != 1 ||
            compiled.ReactiveCounters[0].EventKind != BattleCombatEventKind.AttackLanded ||
            compiled.ReactiveCounters[0].Threshold != 2 || compiled.ReactiveCounters[0].Consumption != 2)
            throw new InvalidOperationException("Reactive Relic compiler changed the typed counter contract");

        using var run = new RelicRunScope(new RelicRunKey(0xA11CEUL, "reactive_hero", 0, 0));
        using var registration = run.Activate(compiled, new RelicRunInstanceState
        {
            InstanceId = "reactive-instance",
            ContentId = compiled.StableId,
            Counters = RelicRunScope.InitialRunCounters(compiled).ToList()
        });
        using var attributes = new BattleAttributeScope("reactive-probe-attributes");
        var definition = new CompiledAttributeSetDefinition(
            [new CompiledAttributeDefinition(CombatAttribute.MaxHealth, 100, 0, 1000)],
            "reactive-probe-attributes");
        var units = ImmutableArray.Create(
            new RelicBattleUnitBinding("reactive_hero", 0, true, false, true, true,
                new CombatCell(0, 0), attributes.CreateSet("reactive_hero", definition)),
            new RelicBattleUnitBinding("reactive_enemy", 1, false, false, true, true,
                new CombatCell(1, 0), attributes.CreateSet("reactive_enemy", definition)));
        using var combat = new BattleCombatEventPipeline("reactive-probe-combat");
        using var battle = new RelicBattleScope(run.PrepareBattle());
        var bindings = new BattleCombatBindingRegistry(combat);
        var effectExecutions = 0;
        battle.Activate(new RelicBattleRuntimeContext
        {
            CombatBindings = bindings,
            QueryUnits = () => units,
            ExecuteEffect = (_, _, _, target, _, _) =>
            {
                if (target != "reactive_enemy")
                    throw new InvalidOperationException("Reactive Relic threshold target changed");
                effectExecutions++;
            },
            Summon = (_, _, _, _, _) => false,
            CurrentTick = () => 0
        });
        bindings.CloseRegistration();
        for (var tick = 1; tick <= 2; tick++)
        {
            var published = combat.Publish(new BattleCombatEventDraft(
                BattleCombatEventKind.AttackLanded,
                CombatSourceRef.Unit("reactive_hero", "reactive_hero", "reactive_hero"),
                "reactive_hero",
                "reactive_enemy",
                tick));
            if (!published.Accepted) throw new InvalidOperationException(published.Message);
        }
        if (battle.CounterValue("reactive-instance", "attacks") != 0 ||
            battle.CounterTransitions.Count != 2 || effectExecutions != 1)
            throw new InvalidOperationException("Reactive Relic threshold/consumption runtime changed");
        var transition = battle.Complete(RelicBattleCompletionReason.PlayerVictory);
        if (transition.RemainingBattleInstances != 0 || transition.RemainingCounters != 0 ||
            transition.RemainingSubscriptions != 0 || transition.RemainingModifierHandles != 0 ||
            transition.ProjectedInstances.Single().Counters.Length != 0 ||
            !run.Apply(transition).Succeeded)
            throw new InvalidOperationException("Reactive Relic reset, cleanup, or Run authentication changed");
        combat.Complete(BattleCombatCompletionReason.PlayerVictory, 2);
        attributes.Complete(AttributeScopeCompletionReason.BattleCompleted, 2);
    }

    private static void VerifyTraitBehavior()
    {
        var authored = TraitProbeDefinition();
        var authoredFingerprint = ResourceGraphFingerprint.Compute([authored]);
        var result = TraitDefinitionCompiler.Compile(authored);
        var compiled = result.Definition ?? throw new InvalidOperationException(
            "fixture Trait definition did not compile: " + string.Join("; ", result.Report.CoreErrors));
        if (compiled.Breakpoints.Length != 2 || compiled.Breakpoints[0].MinValue != 2 ||
            compiled.Breakpoints[0].MaxValue != 3 || compiled.Breakpoints[1].MinValue != 5 ||
            compiled.Breakpoints[1].MaxValue != 8)
            throw new InvalidOperationException("Trait compiler changed ordered inclusive breakpoint ranges");
        var fingerprint = compiled.Fingerprint;
        ((ConstantAttributeMagnitudeSpec)authored.Breakpoints[0].AttributeModifiers[0].Magnitude).Value = 6;
        var changed = TraitDefinitionCompiler.Compile(authored).Definition ??
            throw new InvalidOperationException("changed Trait definition did not compile");
        if (changed.Fingerprint == fingerprint)
            throw new InvalidOperationException("Trait compiled fingerprint ignored nested breakpoint magnitude");
        ((ConstantAttributeMagnitudeSpec)authored.Breakpoints[0].AttributeModifiers[0].Magnitude).Value = 5;
        compiled = TraitDefinitionCompiler.Compile(authored).Definition!;

        var overlap = TraitProbeDefinition();
        overlap.Breakpoints[1].MinValue = 3;
        if (!TraitDefinitionCompiler.Compile(overlap).Report.CoreErrors.Any(error =>
                error.Contains("overlaps", StringComparison.Ordinal)))
            throw new InvalidOperationException("Trait compiler accepted overlapping inclusive breakpoints");
        var emptyBreakpoints = TraitProbeDefinition();
        emptyBreakpoints.Breakpoints = [];
        if (!TraitDefinitionCompiler.Compile(emptyBreakpoints).Report.CoreErrors.Any(error =>
                error.Contains("at least one breakpoint", StringComparison.Ordinal)))
            throw new InvalidOperationException(
                "concrete Trait definition compiled without a breakpoint while only the production graph may be empty");
        var duplicateSlot = TraitProbeDefinition();
        duplicateSlot.Breakpoints[0].AttributeModifiers =
        [
            duplicateSlot.Breakpoints[0].AttributeModifiers[0],
            new AttributeModifierSpec
            {
                Attribute = CombatAttribute.AttackDamage,
                Operation = AttributeModifierOperation.Add,
                Magnitude = new ConstantAttributeMagnitudeSpec { Value = 1 },
                SlotId = "fixture_trait_attack"
            }
        ];
        if (!TraitDefinitionCompiler.Compile(duplicateSlot).Report.CoreErrors.Any(error =>
                error.Contains("duplicates", StringComparison.Ordinal)))
            throw new InvalidOperationException("Trait compiler accepted a duplicate modifier slot");
        var missingDependency = TraitProbeDefinition();
        missingDependency.Breakpoints[0].AttributeModifiers[0].Magnitude = new TraitValueAttributeMagnitudeSpec
        {
            TraitId = "trait_fixture_missing",
            Team = 0,
            CaptureMode = AttributeCaptureMode.Live
        };
        var missingDependencyBatch = TraitDefinitionCompiler.CompileBatch([missingDependency]);
        if (!missingDependencyBatch.Report.CoreErrors.Any(error =>
                error.Contains("missing Trait dependency", StringComparison.Ordinal)) ||
            !missingDependencyBatch.Definitions.IsEmpty)
            throw new InvalidOperationException("Trait graph retained a missing dependency publication");
        var invalidContribution = TraitDefinitionCompiler.CompileContributions(
            [new TraitContributionSpec { TraitId = compiled.StableId, Value = 0 }],
            "invalid contribution probe");
        if (!invalidContribution.Report.HasCoreErrors || !invalidContribution.Contributions.IsEmpty)
            throw new InvalidOperationException("Trait compiler accepted a non-positive contribution");

        AssertTraitBreakpoint(compiled, 1, null, "below first breakpoint");
        AssertTraitBreakpoint(compiled, 2, 0, "first lower bound");
        AssertTraitBreakpoint(compiled, 3, 0, "first upper bound");
        AssertTraitBreakpoint(compiled, 4, null, "allowed breakpoint gap");
        AssertTraitBreakpoint(compiled, 5, 1, "second lower bound");
        AssertTraitBreakpoint(compiled, 8, 1, "second upper bound");

        var countEach = TraitSnapshotBuilder.Build(
            [compiled],
            [
                TraitInput(compiled.StableId, 1, "hero-a", "hero-content", deployed: true),
                TraitInput(compiled.StableId, 1, "hero-b", "hero-content", deployed: true),
                TraitInput(compiled.StableId, 4, "reserve", "reserve-content", deployed: false),
                TraitInput(compiled.StableId, 1, "temporary", "temporary-content", deployed: true,
                    persistent: false, temporary: true),
                TraitInput(compiled.StableId, 2, "enemy-a", "enemy-content", team: 1, deployed: true)
            ]);
        if (countEach.Value(compiled.StableId, 0) != 3 || countEach.Value(compiled.StableId, 1) != 2)
            throw new InvalidOperationException("Trait deployed/temporary/team counting policy drifted");

        var uniqueAuthored = TraitProbeDefinition();
        uniqueAuthored.CountingPolicy!.DuplicateContentPolicy = TraitDuplicateContentPolicy.UniqueContent;
        uniqueAuthored.CountingPolicy.TemporaryUnitPolicy = TraitTemporaryUnitPolicy.Exclude;
        var unique = TraitDefinitionCompiler.Compile(uniqueAuthored).Definition!;
        var uniqueSnapshot = TraitSnapshotBuilder.Build(
            [unique],
            [
                TraitInput(unique.StableId, 1, "hero-a", "same-content", deployed: true),
                TraitInput(unique.StableId, 1, "hero-b", "same-content", deployed: true),
                TraitInput(unique.StableId, 1, "temporary", "temporary-content", deployed: true,
                    persistent: false, temporary: true)
            ]);
        if (uniqueSnapshot.Value(unique.StableId, 0) != 1)
            throw new InvalidOperationException("Trait unique-content or temporary exclusion policy drifted");

        var noEquipmentAuthored = TraitProbeDefinition();
        noEquipmentAuthored.CountingPolicy!.CountEquipment = false;
        noEquipmentAuthored.CountingPolicy.CountExplicitExtra = false;
        var noEquipment = TraitDefinitionCompiler.Compile(noEquipmentAuthored).Definition!;
        var disabledSources = TraitSnapshotBuilder.Build(
            [noEquipment],
            [
                TraitInput(noEquipment.StableId, 2, "equipment", "equipment-content", deployed: true,
                    sourceKind: TraitContributionSourceKind.Equipment, ownerRuntimeId: "hero-a"),
                TraitInput(noEquipment.StableId, 30, "extra", "extra-content", deployed: false,
                    persistent: false, sourceKind: TraitContributionSourceKind.ExplicitExtra)
            ]);
        if (disabledSources.Value(noEquipment.StableId, 0) != 0)
            throw new InvalidOperationException("Trait disabled Equipment/explicit-extra policy still counted sources");

        VerifyRunTraitSnapshot(compiled);
        VerifyBattleTraitScope(compiled);
        VerifyTraitBattleLifecycle(compiled);

        var productionCatalog = GD.Load<ContentCatalog>("res://content/catalogs/alpha_catalog.tres") ??
            throw new InvalidOperationException("production catalog missing for Trait graph probe");
        var production = ContentValidator.CompileProductionGraph(productionCatalog, []);
        if (production.Report.HasCoreErrors || production.Graph is null ||
            production.Graph.Traits.Any(definition => definition.Breakpoints.IsEmpty))
            throw new InvalidOperationException("production Trait graph was not a valid canonical publication: " +
                                                string.Join(" | ", production.Report.CoreErrors));
        if (ResourceGraphFingerprint.Compute([authored]) != authoredFingerprint)
            throw new InvalidOperationException("Trait runtime/compiler behavior mutated the shared authored Resource graph");
    }

    private static TraitDefinition TraitProbeDefinition() => new()
    {
        StableId = "trait_fixture_guard",
        DisplayName = "测试羁绊",
        SemanticIconKey = "trait.fixture.guard",
        CountingPolicy = new TraitCountingPolicySpec
        {
            DeploymentPolicy = TraitDeploymentPolicy.DeployedOnly,
            TemporaryUnitPolicy = TraitTemporaryUnitPolicy.Include,
            DuplicateContentPolicy = TraitDuplicateContentPolicy.CountEach,
            CountEquipment = true,
            CountExplicitExtra = true
        },
        Breakpoints =
        [
            new TraitBreakpointSpec
            {
                MinValue = 2,
                MaxValue = 3,
                DisplayStyle = "TraitTierOne",
                AttributeModifiers =
                [
                    new AttributeModifierSpec
                    {
                        Attribute = CombatAttribute.AttackDamage,
                        Operation = AttributeModifierOperation.Add,
                        Magnitude = new ConstantAttributeMagnitudeSpec { Value = 5 },
                        SlotId = "fixture_trait_attack"
                    }
                ]
            },
            new TraitBreakpointSpec
            {
                MinValue = 5,
                MaxValue = 8,
                DisplayStyle = "TraitTierTwo",
                AttributeModifiers =
                [
                    new AttributeModifierSpec
                    {
                        Attribute = CombatAttribute.AttackSpeed,
                        Operation = AttributeModifierOperation.Add,
                        Magnitude = new ConstantAttributeMagnitudeSpec { Value = 1 },
                        SlotId = "fixture_trait_speed"
                    }
                ]
            }
        ]
    };

    private static TraitContributionInput TraitInput(
        string traitId,
        int value,
        string sourceInstanceId,
        string contentIdentity,
        int team = 0,
        bool deployed = true,
        bool persistent = true,
        bool temporary = false,
        TraitContributionSourceKind sourceKind = TraitContributionSourceKind.Hero,
        string ownerRuntimeId = "") => new(
        traitId,
        value,
        team,
        sourceKind,
        sourceInstanceId,
        string.IsNullOrWhiteSpace(ownerRuntimeId) ? sourceInstanceId : ownerRuntimeId,
        contentIdentity,
        persistent,
        temporary,
        deployed);

    private static void AssertTraitBreakpoint(
        CompiledTraitDefinition definition,
        int value,
        int? expectedIndex,
        string label)
    {
        var snapshot = TraitSnapshotBuilder.Build(
            [definition],
            [TraitInput(definition.StableId, value, "source", "content")]);
        if (snapshot.Resolve(definition.StableId, 0).ActiveBreakpoint?.Index != expectedIndex)
            throw new InvalidOperationException(label + " selected the wrong Trait breakpoint");
    }

    private static void VerifyRunTraitSnapshot(CompiledTraitDefinition trait)
    {
        var equipmentAuthored = new EquipmentDefinition
        {
            StableId = "equipment_trait_fixture",
            TraitContributions = [new TraitContributionSpec { TraitId = trait.StableId, Value = 1 }]
        };
        var equipment = EquipmentDefinitionCompiler.Compile(equipmentAuthored).Definition ??
            throw new InvalidOperationException("Trait-only Equipment definition did not compile");
        var unitContributions = new Dictionary<string, ImmutableArray<CompiledTraitContribution>>(
            StringComparer.Ordinal)
        {
            ["hero-trait-content"] = [new CompiledTraitContribution(trait.StableId, 1)]
        };
        var graph = new CompiledContentGraph(
            [],
            ImmutableArray<CompiledAbilityDefinition>.Empty,
            ImmutableArray<CompiledStatusDefinition>.Empty,
            ImmutableArray<TowerAutobattler.Relics.CompiledRelicDefinition>.Empty,
            [equipment],
            traits: [trait],
            unitTraitContributions: unitContributions);
        var run = new ActiveRunDto
        {
            Roster =
            [
                new RosterHeroInstanceDto
                {
                    InstanceId = "hero-trait-a",
                    ContentId = "hero-trait-content",
                    Equipment =
                    [
                        new EquipmentInstanceState
                        {
                            InstanceId = "equipment-trait-a",
                            ContentId = equipment.StableId,
                            OwnerHeroInstanceId = "hero-trait-a",
                            SlotIndex = 0
                        }
                    ]
                },
                new RosterHeroInstanceDto
                {
                    InstanceId = "hero-trait-reserve",
                    ContentId = "hero-trait-content"
                }
            ],
            Deployment = ActiveRunFormationSchema.EmptyDeployment()
        };
        run.Deployment[0] = "hero-trait-a";
        var before = JsonSerializer.Serialize(run);
        var snapshot = RunTraitSnapshotBuilder.Build(
            run,
            graph,
            [new TraitExplicitContribution(trait.StableId, 28, 0, "extra-28", "extra-content")]);
        if (snapshot.Value(trait.StableId, 0) != 30 ||
            snapshot.Resolve(trait.StableId, 0).Contributions.Count(contribution =>
                contribution.SourceKind == TraitContributionSourceKind.Hero) != 1 ||
            snapshot.Resolve(trait.StableId, 0).Contributions.Count(contribution =>
                contribution.SourceKind == TraitContributionSourceKind.Equipment) != 1 ||
            snapshot.Resolve(trait.StableId, 0).Contributions.Count(contribution =>
                contribution.SourceKind == TraitContributionSourceKind.ExplicitExtra) != 1)
            throw new InvalidOperationException("Run Trait snapshot did not combine hero, Equipment, and value-30 extra");
        if (JsonSerializer.Serialize(run) != before)
            throw new InvalidOperationException("recomputed Run Trait snapshot mutated schema-v4 state");
        var semantic = TraitSemanticFacts.From(snapshot.Resolve(trait.StableId, 0).Presentation);
        if (semantic.Key.ToString() != trait.SemanticIconKey ||
            !semantic.Text.Contains("测试羁绊 30", StringComparison.Ordinal) ||
            semantic.ThemeTypeVariation?.ToString() != "TraitInactive")
            throw new InvalidOperationException("typed immutable Trait presentation fact lost semantic identity or value");
    }

    private static void VerifyBattleTraitScope(CompiledTraitDefinition trait)
    {
        var lowerInputs = new[]
        {
            TraitInput(trait.StableId, 2, "lower", "lower-content")
        };
        var preparation = TraitBattlePreparationBuilder.Build([trait], lowerInputs);
        using var attributes = new BattleAttributeScope("trait_failure_attributes");
        var fullDefinition = AttributeDefinitionCompiler.Legacy(new Dictionary<CombatAttribute, float>
        {
            [CombatAttribute.MaxHealth] = 100,
            [CombatAttribute.AttackDamage] = 10,
            [CombatAttribute.AttackSpeed] = 1,
            [CombatAttribute.MoveSpeed] = 1
        });
        var damageOnlyDefinition = new CompiledAttributeSetDefinition(
            [new CompiledAttributeDefinition(CombatAttribute.AttackDamage, 10, 0, 1_000)],
            "trait-damage-only");
        var good = attributes.CreateSet("trait-good", fullDefinition);
        var missingSpeed = attributes.CreateSet("trait-missing-speed", damageOnlyDefinition);
        using (var failureScope = new BattleTraitScope(
                   "trait_failure_scope",
                   preparation,
                   [
                       new TraitOwnerBinding("trait-good", 0, good),
                       new TraitOwnerBinding("trait-missing-speed", 0, missingSpeed)
                   ]))
        {
            Near(good.GetValue(CombatAttribute.AttackDamage), 15, "Trait lower tier good owner");
            Near(missingSpeed.GetValue(CombatAttribute.AttackDamage), 15, "Trait lower tier second owner");
            var before = (failureScope.Snapshot.Fingerprint, failureScope.LiveTierCount,
                failureScope.LiveModifierHandleCount, good.ModifierCount, missingSpeed.ModifierCount);
            ExpectThrows(
                () => failureScope.Refresh(
                    [TraitInput(trait.StableId, 5, "upper", "upper-content")]),
                "Trait tier apply failure");
            var after = (failureScope.Snapshot.Fingerprint, failureScope.LiveTierCount,
                failureScope.LiveModifierHandleCount, good.ModifierCount, missingSpeed.ModifierCount);
            if (before != after)
                throw new InvalidOperationException("failed Trait tier replacement changed snapshot, tier, or handles");
            Near(good.GetValue(CombatAttribute.AttackDamage), 15, "Trait failed replacement good rollback");
            Near(missingSpeed.GetValue(CombatAttribute.AttackDamage), 15, "Trait failed replacement second rollback");
            var transition = failureScope.Complete(TraitBattleCompletionReason.Exception);
            if (transition is not { RemainingTiers: 0, RemainingModifierHandles: 0 })
                throw new InvalidOperationException("failed Trait scope retained tier or handles on completion");
        }
        Near(good.GetValue(CombatAttribute.AttackDamage), 10, "Trait failure scope completion rollback");
        Near(missingSpeed.GetValue(CombatAttribute.AttackDamage), 10, "Trait second owner completion rollback");

        var success = attributes.CreateSet("trait-success", fullDefinition);
        using (var successScope = new BattleTraitScope(
                   "trait_success_scope",
                   preparation,
                   [new TraitOwnerBinding("trait-success", 0, success)]))
        {
            Near(success.GetValue(CombatAttribute.AttackDamage), 15, "Trait lower tier projection");
            successScope.Refresh([TraitInput(trait.StableId, 5, "upper", "upper-content")]);
            Near(success.GetValue(CombatAttribute.AttackDamage), 10, "Trait old tier source removed");
            Near(success.GetValue(CombatAttribute.AttackSpeed), 2, "Trait new tier source applied");
            if (successScope.LiveTierCount != 1 || successScope.LiveModifierHandleCount != 1)
                throw new InvalidOperationException("Trait replacement retained more than one active tier source");

            var traitCheckpoint = successScope.CaptureState();
            var attributeCheckpoint = attributes.CaptureState();
            var temporary = attributes.CreateSet("trait-temporary", fullDefinition);
            successScope.AddOwnerAndContributions(
                new TraitOwnerBinding("trait-temporary", 0, temporary),
                [TraitInput(trait.StableId, 1, "trait-temporary", "temporary-content", deployed: true,
                    persistent: false, temporary: true)]);
            if (successScope.LiveModifierHandleCount != 2 || successScope.Snapshot.Value(trait.StableId, 0) != 6)
                throw new InvalidOperationException("temporary Trait owner did not update tier projection");
            successScope.RestoreState(traitCheckpoint);
            attributes.RestoreState(attributeCheckpoint);
            if (successScope.LiveModifierHandleCount != 1 || successScope.Snapshot.Value(trait.StableId, 0) != 5)
                throw new InvalidOperationException("Trait/Attribute checkpoint did not restore pre-summon state");
            Near(success.GetValue(CombatAttribute.AttackSpeed), 2, "Trait checkpoint restored source handles");

            successScope.Refresh([TraitInput(trait.StableId, 1, "below", "below-content")]);
            Near(success.GetValue(CombatAttribute.AttackSpeed), 1, "Trait downgrade removed old tier source");
            if (successScope.LiveTierCount != 0 || successScope.LiveModifierHandleCount != 0)
                throw new InvalidOperationException("Trait downgrade retained an inactive tier source");
        }

        using var attributesA = new BattleAttributeScope("trait_isolation_attributes_a");
        using var attributesB = new BattleAttributeScope("trait_isolation_attributes_b");
        var sameA = attributesA.CreateSet("same-owner", fullDefinition);
        var sameB = attributesB.CreateSet("same-owner", fullDefinition);
        using var scopeA = new BattleTraitScope(
            "trait_scope_a",
            preparation,
            [new TraitOwnerBinding("same-owner", 0, sameA)]);
        using var scopeB = new BattleTraitScope(
            "trait_scope_b",
            preparation,
            [new TraitOwnerBinding("same-owner", 0, sameB)]);
        scopeA.Complete(TraitBattleCompletionReason.BattleCompleted);
        Near(sameA.GetValue(CombatAttribute.AttackDamage), 10, "first Trait scope cleanup");
        Near(sameB.GetValue(CombatAttribute.AttackDamage), 15, "same-definition/source cross-scope isolation");
        scopeB.Complete(TraitBattleCompletionReason.BattleCompleted);
        Near(sameB.GetValue(CombatAttribute.AttackDamage), 10, "second Trait scope cleanup");
    }

    private static void VerifyTraitBattleLifecycle(CompiledTraitDefinition trait)
    {
        var preparation = TraitBattlePreparationBuilder.Build(
            [trait],
            [TraitInput(trait.StableId, 2, "hero-trait", "hero-trait-content")]);

        var victory = new BattleSimulation(TraitResultConfig(0x7101UL, preparation, 100, 1, 0));
        if (victory.RunToEnd().Outcome != BattleOutcome.PlayerVictory)
            throw new InvalidOperationException("Trait victory fixture outcome changed");
        AssertTraitCompletion(victory, TraitBattleCompletionReason.BattleCompleted, "victory");

        var defeat = new BattleSimulation(TraitResultConfig(0x7102UL, preparation, 1, 100, 1_000, true));
        if (defeat.RunToEnd().Outcome != BattleOutcome.PlayerDefeat)
            throw new InvalidOperationException("Trait defeat fixture outcome changed");
        AssertTraitCompletion(defeat, TraitBattleCompletionReason.BattleCompleted, "defeat");

        var timeout = new BattleSimulation(TraitResultConfig(
            0x7103UL, preparation, 0, 1_000_000_000, 0, floor: new IsolatedEquipmentCellsFloorRule(),
            enemyCell: new Vector2I(10, 5)));
        if (timeout.RunToEnd().Outcome != BattleOutcome.Timeout)
            throw new InvalidOperationException("Trait timeout fixture outcome changed");
        AssertTraitCompletion(timeout, TraitBattleCompletionReason.BattleCompleted, "timeout");

        var abort = new BattleSimulation(TraitResultConfig(0x7104UL, preparation, 1, 1_000_000, 0));
        abort.Abort();
        AssertTraitCompletion(abort, TraitBattleCompletionReason.Abort, "abort");

        var replacement = new BattleSimulation(TraitResultConfig(0x7105UL, preparation, 1, 1_000_000, 0));
        replacement.Replace();
        AssertTraitCompletion(replacement, TraitBattleCompletionReason.Replacement, "replacement");

        var exception = new BattleSimulation(TraitResultConfig(
            0x7106UL, preparation, 1, 1_000_000, 0, floor: new ThrowingTickRule()));
        ExpectThrows(() => exception.Step(), "Trait exception lifecycle");
        AssertTraitCompletion(exception, TraitBattleCompletionReason.Exception, "exception");

        var disposal = new BattleSimulation(TraitResultConfig(0x7107UL, preparation, 1, 1_000_000, 0));
        disposal.Dispose();
        AssertTraitCompletion(disposal, TraitBattleCompletionReason.Disposal, "disposal");
    }

    private static BattleConfig TraitResultConfig(
        ulong seed,
        TraitBattlePreparation preparation,
        float playerDamage,
        float enemyHealth,
        float enemyDamage,
        bool enemyActsFirst = false,
        IBattleFloorRuleRuntime? floor = null,
        Vector2I? enemyCell = null) => new()
    {
        Seed = seed,
        Identity = new BattleIdentity("trait_contract", TowerNodeType.Combat, seed, 1, 1),
        FloorRule = floor ?? new ClearFloorRuleRuntime("trait_contract", "trait_contract", "test"),
        HeroRule = ProductionRule(),
        Traits = preparation,
        Spawns =
        [
            new BattleSpawn(ProductionUnit("trait_owner", true, 100, playerDamage, 1), 0,
                new Vector2I(1, 1), "hero-trait", IsPersistentRosterHero: true),
            new BattleSpawn(ProductionUnit("trait_enemy", false, enemyHealth, enemyDamage, 1), 1,
                enemyCell ?? new Vector2I(2, 1), enemyActsFirst ? "a-trait-enemy" : "z-trait-enemy")
        ]
    };

    private static void AssertTraitCompletion(
        BattleSimulation battle,
        TraitBattleCompletionReason reason,
        string label)
    {
        var transition = battle.TraitTransition ??
            throw new InvalidOperationException(label + " Trait transition missing");
        if (transition.Reason != reason || transition.RemainingTiers != 0 ||
            transition.RemainingModifierHandles != 0 ||
            battle.AttributeTransition is not { RemainingSets: 0, RemainingModifiers: 0 })
            throw new InvalidOperationException(label + " Trait/Attribute scope retained Battle state");
        battle.Dispose();
    }

    private static void VerifyIndependentTacticalLoadoutBehavior()
    {
        VerifySubscriptionHandleTransactionBehavior();

        var authoredRules = GD.Load<RunRulesDefinition>("res://content/project/alpha_run_rules.tres") ??
            throw new InvalidOperationException("production Run rules are missing for tactical probe");
        var rules = CompileRunRules(authoredRules);
        if (rules.TacticalCommandSlotCount != 2 ||
            !rules.StarterTacticalCommandIds.SequenceEqual(["tactical_rally", "tactical_time_stop"]))
            throw new InvalidOperationException("production starter tactical loadout changed");

        var firstNewRun = new ActiveRunDto
        {
            Roster = [new RosterHeroInstanceDto { InstanceId = "hero-a", ContentId = "hero_banner_marshal" }]
        };
        var secondNewRun = new ActiveRunDto
        {
            Roster = [new RosterHeroInstanceDto { InstanceId = "hero-b", ContentId = "hero_bone_regent" }]
        };
        ActiveRunFormationSchema.InitializeVersion4(firstNewRun, rules);
        ActiveRunFormationSchema.InitializeVersion4(secondNewRun, rules);
        if (!firstNewRun.EquippedTacticalCommandIds.SequenceEqual(rules.StarterTacticalCommandIds) ||
            !secondNewRun.EquippedTacticalCommandIds.SequenceEqual(firstNewRun.EquippedTacticalCommandIds))
            throw new InvalidOperationException("new Run tactical loadout depends on the selected hero");
        using (var document = JsonDocument.Parse(JsonSerializer.Serialize(firstNewRun)))
            if (!document.RootElement.TryGetProperty("EquippedTacticalCommandIds", out var equippedCommands) ||
                equippedCommands.ValueKind != JsonValueKind.Array || equippedCommands.GetArrayLength() != 2)
                throw new InvalidOperationException("serialized Run does not publish exactly two tactical slots");

        foreach (var mapping in rules.LegacyTacticalCommandByHeroId.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var legacy = LegacyTacticalRun(mapping.Key);
            if (!ActiveRunFormationSchema.TryMigrateToCurrent(legacy, rules) ||
                legacy.EquippedTacticalCommandIds.Count != 2 ||
                legacy.EquippedTacticalCommandIds[0] != mapping.Value ||
                legacy.EquippedTacticalCommandIds[1] != rules.StarterTacticalCommandIds.First(id => id != mapping.Value))
                throw new InvalidOperationException("legacy hero tactical migration is not deterministic: " + mapping.Key);
        }

        var productionCommands = LoadProductionTacticalCommands();
        var graph = new CompiledContentGraph(
            [],
            [],
            [],
            [],
            [],
            productionCommands.Compiled,
            productionCommands.Authored);
        foreach (var ids in new List<string>?[]
                 {
                     null,
                     [],
                     ["tactical_rally"],
                     ["tactical_rally", "tactical_time_stop", "tactical_beast_roar"],
                     ["tactical_rally", "tactical_rally"],
                     ["tactical_rally", "tactical_unknown"]
                 })
        {
            var invalid = new ActiveRunDto
            {
                Version = ActiveRunFormationSchema.CurrentVersion,
                EquippedTacticalCommandIds = ids!
            };
            if (ActiveRunTacticalCommandPolicy.Validate(invalid, rules, graph))
                throw new InvalidOperationException("invalid current tactical loadout was accepted");
        }

        for (var cost = 1; cost <= 3; cost++)
        {
            var command = TacticalFixtureCommand("tactical_cost_" + cost, cost);
            using var battle = TacticalBattle(command);
            var activation = battle.TryUseTacticalCommand(0);
            if (!activation.Succeeded || activation.TacticalPointsSpent != cost ||
                battle.TacticalPoints != BattleTacticalCommandScope.MaximumTacticalPoints - cost)
                throw new InvalidOperationException($"authored {cost}-point tactical cost was not authoritative");
        }

        var explicitCommand = TacticalFixtureCommand("tactical_explicit", 1, explicitTarget: true);
        using (var explicitBattle = TacticalBattle(explicitCommand))
        {
            var target = explicitBattle.Units.Single(unit => unit.RuntimeId == "enemy");
            target.AttackCooldown = 9;
            var failureBefore = TacticalFingerprint(explicitBattle);
            var missingTarget = explicitBattle.TryUseTacticalCommand(0);
            if (missingTarget.Succeeded || TacticalFingerprint(explicitBattle) != failureBefore)
                throw new InvalidOperationException("missing explicit target consumed or mutated tactical state");
            if (!explicitBattle.TryUseTacticalCommand(0, target.RuntimeId).Succeeded ||
                target.AttackCooldown != 0 || explicitBattle.TacticalPoints != 2)
                throw new InvalidOperationException("legal explicit tactical target/effect did not commit");
        }

        var lateStatusAuthored = PlainStatus(
            "tactical_late_status",
            StatusDurationKind.Permanent,
            0,
            StatusAggregationPolicy.BySource);
        lateStatusAuthored.AttributeModifiers = [DamageModifierSpec(1.1f)];
        lateStatusAuthored.Presentation = CuePresentation("tactical_late_status");
        lateStatusAuthored.CombatReactiveBindings =
        [
            new StatusCombatReactiveBindingSpec
            {
                EventKind = BattleCombatEventKind.AbilityResolved,
                OwnerRole = StatusReactiveOwnerRole.OwnerIsSource,
                EffectSourcePolicy = StatusReactiveEffectSourcePolicy.PrimaryContribution,
                Priority = -1,
                Binding = ManualEffectBinding("tactical_late_status_reactive")
            }
        ];
        var lateStatus = CompileStatus(lateStatusAuthored);
        var lateEffect = new CompiledEffectBinding(
            "tactical_late_shield",
            0,
            new CompiledEffectTrigger(EffectTriggerKind.Manual, EffectDomainEventKind.None),
            [],
            new CompiledOwnerTargetQuery(),
            [new CompiledEffectStep(EffectKind.Shield, EffectAmountSource.Fixed, 7)],
            new CompiledEffectBindingLimits(0, 0, 0, 0),
            null);
        var lateDamage = new CompiledEffectBinding(
            "tactical_late_damage",
            -10,
            new CompiledEffectTrigger(EffectTriggerKind.Manual, EffectDomainEventKind.None),
            [],
            new CompiledRelativeTeamTargetQuery(EffectRelativeTeam.Enemies, false),
            [new CompiledEffectStep(EffectKind.Damage, EffectAmountSource.Fixed, 20000)],
            new CompiledEffectBindingLimits(0, 0, 0, 0),
            null);
        var lateFailureCommand = TacticalFixtureCommand(
            "tactical_late_commit_failure",
            1,
            goldCost: 4,
            customOperations:
            [
                new CompiledEffectAbilityOperation(lateDamage, AbilityInvocationValueSource.Fixed, 0),
                new CompiledEffectAbilityOperation(lateEffect, AbilityInvocationValueSource.Fixed, 0),
                new CompiledCooldownAbilityOperation(
                    new CompiledOwnerTargetQuery(),
                    CooldownAdjustmentKind.Reset,
                    0,
                    CooldownAdjustmentKind.None,
                    0),
                new CompiledApplyStatusAbilityOperation(lateStatus, new CompiledOwnerTargetQuery()),
                new CompiledSummonAbilityOperation(
                    AbilitySummonProfile.Mercenary,
                    1,
                    1,
                    1,
                    1,
                    true,
                    "temporary_late_probe")
            ]);
        var lateTrait = TraitDefinitionCompiler.Compile(TraitProbeDefinition()).Definition ??
            throw new InvalidOperationException("late Tactical Trait fixture did not compile");
        var lateTraits = TraitBattlePreparationBuilder.Build(
            [lateTrait],
            [TraitInput(lateTrait.StableId, 1, "hero", "tactical_hero")]);
        var lateSummons = ImmutableDictionary<string, UnitSnapshot>.Empty.Add(
            "temporary_late_probe",
            ProductionUnit("temporary_late_probe", false, 20, 2, 1) with
            {
                TraitContributions = [new CompiledTraitContribution(lateTrait.StableId, 1)]
            });
        var lateRelics = ReactiveDeathRelicPreparation();
        var victimAbilityResolutionCount = 0;
        var lateTraitMutationObserved = false;
        var lateRelicMutationObserved = false;
        BattleSimulation? lateFailureBattle = null;
        IDisposable? lateVictimHandle = null;
        IDisposable? lateFailureHandle = null;
        IDisposable? lateRelicFailureHandle = null;
        using (var lateFailure = TacticalBattle(
                   lateFailureCommand,
                   startingGold: 10,
                   tacticalSummons: lateSummons,
                   configureCombatBindings: bindings =>
                   {
                       bindings.Subscribe(
                           BattleCombatEventKind.AbilityResolved,
                           CombatSourceRef.System("tactical_late_disposer"),
                           -100,
                           (_, _) => lateVictimHandle!.Dispose());
                       lateVictimHandle = bindings.Subscribe(
                           BattleCombatEventKind.AbilityResolved,
                           CombatSourceRef.System("tactical_late_victim"),
                           -90,
                           (_, _) => victimAbilityResolutionCount++);
                       lateFailureHandle = bindings.Subscribe(
                           BattleCombatEventKind.AbilityResolved,
                           CombatSourceRef.System("tactical_late_failure"),
                           100,
                           (_, _) =>
                           {
                               var activeTrait = lateFailureBattle!.TraitSnapshot.Resolve(lateTrait.StableId, 0);
                               var temporary = lateFailureBattle.Units.SingleOrDefault(unit => unit.IsTemporary);
                               var traitOwner = lateFailureBattle.Units.Single(unit => unit.RuntimeId == "hero");
                               if (activeTrait.Value != 2 || activeTrait.ActiveBreakpoint?.Index != 0 ||
                                   temporary is null || temporary.Attributes.ModifierCount != 1 ||
                                   Math.Abs(temporary.Attributes.GetValue(CombatAttribute.AttackDamage) - 7) > 0.0001f ||
                                   traitOwner.Attributes.ModifierCount < 2)
                                   throw new InvalidOperationException(
                                       "late Tactical failure did not observe temporary-summon Trait/Attribute mutation");
                               lateTraitMutationObserved = true;
                               var nested = lateFailureBattle!.TryUseTacticalCommand(1);
                               if (nested.Succeeded || nested.Failure != TacticalCommandActivationFailure.CommitFailed)
                                   throw new InvalidOperationException(
                                       "nested tactical world transaction was not rejected before mutation");
                           });
                       lateRelicFailureHandle = bindings.Subscribe(
                           BattleCombatEventKind.ShieldResolved,
                           CombatSourceRef.System("tactical_late_relic_failure"),
                           100,
                           (combatEvent, _) =>
                           {
                               if (Math.Abs(combatEvent.EffectiveValue - 3) > .0001f) return;
                               var owner = lateFailureBattle!.Units.Single(unit => unit.RuntimeId == "hero");
                               if (lateFailureBattle.RelicCounterTransitions.Count != 1 || owner.Shield < 10)
                                   throw new InvalidOperationException(
                                       "late Tactical Relic failure did not observe counter/Effect mutation");
                               lateRelicMutationObserved = true;
                               throw new InvalidOperationException("expected Reactive Relic late Effect failure");
                           });
                   },
                   traits: lateTraits,
                   relics: lateRelics))
        {
            lateFailureBattle = lateFailure;
            var owner = lateFailure.Units.Single(unit => unit.RuntimeId == "hero");
            owner.AttackCooldown = 9;
            var before = TacticalFingerprint(lateFailure);
            var failed = lateFailure.TryUseTacticalCommand(0);
            if (failed.Succeeded || failed.Failure != TacticalCommandActivationFailure.CommitFailed)
                throw new InvalidOperationException("late tactical world failure did not report CommitFailed");
            if (!lateTraitMutationObserved)
                throw new InvalidOperationException(
                    "late tactical world failure skipped Trait mutation evidence: " + failed.FailureReason);
            if (!lateRelicMutationObserved)
                throw new InvalidOperationException(
                    "late tactical world failure skipped Reactive Relic mutation evidence: " + failed.FailureReason);
            if (lateFailure.Outcome != BattleOutcome.Running)
                throw new InvalidOperationException(
                    "late tactical world failure ended the running Battle: " +
                    $"outcome={lateFailure.Outcome},points={lateFailure.TacticalPoints}," +
                    $"owner-cooldown={owner.AttackCooldown},legacy-events={lateFailure.PendingEvents.Count}," +
                    $"combat-events={lateFailure.CombatEvents.Count},effect-trace={lateFailure.EffectTrace.Count}," +
                    $"status-cues={lateFailure.StatusPresentationCues.Count}," +
                    $"tactical-transition={lateFailure.TacticalCommandTransition?.Reason}");
            if (lateFailure.EffectTransition is not null ||
                lateFailure.AttributeTransition is not null ||
                lateFailure.CombatTransition is not null ||
                lateFailure.AbilityTransition is not null ||
                lateFailure.StatusTransition is not null ||
                lateFailure.RelicTransition is not null ||
                lateFailure.EquipmentTransition is not null ||
                lateFailure.TacticalCommandTransition is not null)
                throw new InvalidOperationException("late tactical world failure completed one or more Battle scopes");
            if (TacticalFingerprint(lateFailure) != before)
                throw new InvalidOperationException("late tactical world failure leaked a partial Battle mutation");
            if (lateFailure.RelicCounterTransitions.Count != 0)
                throw new InvalidOperationException("late tactical world failure retained Reactive Relic counter history");

            if (victimAbilityResolutionCount != 1)
                throw new InvalidOperationException("late tactical victim did not run exactly once in the failed snapshot");
            lateVictimHandle!.Dispose();
            lateFailureHandle!.Dispose();
            lateRelicFailureHandle!.Dispose();

            if (!lateFailure.TryUseTacticalCommand(0).Succeeded)
                throw new InvalidOperationException("rolled-back tactical command could not retry successfully");
            if (victimAbilityResolutionCount != 1)
                throw new InvalidOperationException(
                    "pre-transaction Combat handle could not remove its restored registration after rollback");
            var recoveredTrait = lateFailure.TraitSnapshot.Resolve(lateTrait.StableId, 0);
            var recoveredTemporary = lateFailure.Units.SingleOrDefault(unit => unit.IsTemporary);
            if (recoveredTrait.Value != 2 || recoveredTrait.ActiveBreakpoint?.Index != 0 ||
                recoveredTemporary is null || recoveredTemporary.Attributes.ModifierCount != 1 ||
                lateFailure.RelicCounterTransitions.Count != 1)
                throw new InvalidOperationException(
                    "rolled-back tactical retry did not commit the Reactive Relic/temporary-summon Trait state");
            var recovered = TacticalFingerprint(lateFailure);
            IDisposable? cleanVictimHandle = null;
            IDisposable? cleanFailureHandle = null;
            IDisposable? cleanRelicFailureHandle = null;
            using var clean = TacticalBattle(
                lateFailureCommand,
                startingGold: 10,
                tacticalSummons: lateSummons,
                configureCombatBindings: bindings =>
                {
                    bindings.Subscribe(
                        BattleCombatEventKind.AbilityResolved,
                        CombatSourceRef.System("tactical_late_disposer"),
                        -100,
                        (_, _) => cleanVictimHandle!.Dispose());
                    cleanVictimHandle = bindings.Subscribe(
                        BattleCombatEventKind.AbilityResolved,
                        CombatSourceRef.System("tactical_late_victim"),
                        -90,
                        (_, _) => { });
                    cleanFailureHandle = bindings.Subscribe(
                        BattleCombatEventKind.AbilityResolved,
                        CombatSourceRef.System("tactical_late_failure"),
                        100,
                        (_, _) => throw new InvalidOperationException("disposed clean failure listener executed"));
                    cleanRelicFailureHandle = bindings.Subscribe(
                        BattleCombatEventKind.ShieldResolved,
                        CombatSourceRef.System("tactical_late_relic_failure"),
                        100,
                        (_, _) => throw new InvalidOperationException("disposed clean Relic failure listener executed"));
                },
                traits: lateTraits,
                relics: lateRelics);
            clean.Units.Single(unit => unit.RuntimeId == "hero").AttackCooldown = 9;
            cleanVictimHandle!.Dispose();
            cleanFailureHandle!.Dispose();
            cleanRelicFailureHandle!.Dispose();
            if (!clean.TryUseTacticalCommand(0).Succeeded || TacticalFingerprint(clean) != recovered)
                throw new InvalidOperationException(
                    "late tactical rollback did not restore deterministic retry ordering and state");
        }

        var goldCommand = TacticalFixtureCommand("tactical_gold", 1, goldCost: 5);
        using (var poor = TacticalBattle(goldCommand, startingGold: 4))
        {
            var before = TacticalFingerprint(poor);
            var failed = poor.TryUseTacticalCommand(0);
            if (failed.Succeeded || failed.Failure != TacticalCommandActivationFailure.InsufficientGold ||
                TacticalFingerprint(poor) != before)
                throw new InvalidOperationException("insufficient Gold changed tactical state");
        }
        using (var funded = TacticalBattle(goldCommand, startingGold: 5))
            if (!funded.TryUseTacticalCommand(0).Succeeded || funded.GoldSpent != 5 || funded.TacticalPoints != 2)
                throw new InvalidOperationException("funded tactical Gold cost did not commit atomically");

        var cooldownCommand = TacticalFixtureCommand("tactical_cooldown", 1, cooldownTicks: 5);
        using (var cooldown = TacticalBattle(cooldownCommand))
        {
            if (!cooldown.TryUseTacticalCommand(0).Succeeded) throw new InvalidOperationException("cooldown fixture failed");
            var before = TacticalFingerprint(cooldown);
            var failed = cooldown.TryUseTacticalCommand(0);
            if (failed.Failure != TacticalCommandActivationFailure.Cooldown || TacticalFingerprint(cooldown) != before)
                throw new InvalidOperationException("cooldown rejection changed tactical state");
            for (var tick = 0; tick < 5; tick++) cooldown.Step();
            if (!cooldown.TryUseTacticalCommand(0).Succeeded || cooldown.TacticalPoints != 1)
                throw new InvalidOperationException("tactical cooldown did not reopen deterministically");
        }

        var limitedCommand = TacticalFixtureCommand("tactical_limited", 1, maxUses: 1);
        using (var limited = TacticalBattle(limitedCommand))
        {
            if (!limited.TryUseTacticalCommand(0).Succeeded) throw new InvalidOperationException("usage fixture failed");
            var before = TacticalFingerprint(limited);
            var failed = limited.TryUseTacticalCommand(0);
            if (failed.Failure != TacticalCommandActivationFailure.UsageLimit || TacticalFingerprint(limited) != before)
                throw new InvalidOperationException("usage-limit rejection changed tactical state");
        }

        var summonCommand = TacticalFixtureCommand("tactical_summon", 1, summonContentId: "temporary_probe");
        using (var summons = TacticalBattle(
                   summonCommand,
                   tacticalSummons: ImmutableDictionary<string, UnitSnapshot>.Empty.Add(
                       "temporary_probe", ProductionUnit("temporary_probe", false, 20, 2, 1))))
        {
            if (!summons.TryUseTacticalCommand(0).Succeeded ||
                summons.Units.Count(unit => unit.IsTemporary && unit.Alive) != 1)
                throw new InvalidOperationException("tactical temporary summon did not commit");
            var before = TacticalFingerprint(summons);
            if (summons.TryUseTacticalCommand(0).Succeeded || TacticalFingerprint(summons) != before)
                throw new InvalidOperationException("temporary summon capacity failure changed tactical state");
        }

        using (var exhausted = TacticalBattle(TacticalFixtureCommand("tactical_exhaust", 3)))
        {
            if (!exhausted.TryUseTacticalCommand(0).Succeeded || exhausted.TacticalPoints != 0)
                throw new InvalidOperationException("three-point exhaustion fixture failed");
            var before = TacticalFingerprint(exhausted);
            var failed = exhausted.TryUseTacticalCommand(1);
            if (failed.Failure != TacticalCommandActivationFailure.InsufficientTacticalPoints ||
                TacticalFingerprint(exhausted) != before)
                throw new InvalidOperationException("insufficient tactical points changed state");
        }
        using (var restored = TacticalBattle(TacticalFixtureCommand("tactical_restored", 1)))
            if (restored.TacticalPoints != 3)
                throw new InvalidOperationException("new Battle did not restore three tactical points");

        VerifyTacticalSourceFallbackAndFailure();
        VerifyTacticalCompletionPaths();
    }

    private static void VerifySubscriptionHandleTransactionBehavior()
    {
        var failures = new List<string>();
        void Probe(string label, Action action)
        {
            try { action(); }
            catch (Exception exception) { failures.Add($"{label}:{exception.Message}"); }
        }

        var listenerSource = CombatSourceRef.System("subscription_handle_contract");
        var eventSource = CombatSourceRef.System("subscription_handle_event");

        Probe("combat-preexisting-rollback", () =>
        {
            using var pipeline = new BattleCombatEventPipeline("combat-handle-preexisting");
            var observed = 0;
            var handle = pipeline.Subscribe(
                BattleCombatEventKind.AttackLanded,
                listenerSource,
                0,
                (_, _) => observed++);
            var checkpoint = pipeline.CaptureState();
            handle.Dispose();
            pipeline.RestoreState(checkpoint);
            handle.Dispose();
            pipeline.Publish(new BattleCombatEventDraft(
                BattleCombatEventKind.AttackLanded,
                eventSource,
                "source",
                "target",
                1));
            if (observed != 0 || pipeline.SubscriptionCount != 0 ||
                pipeline.CaptureSubscriptionSnapshot().Length != 0)
                throw new InvalidOperationException(
                    "original handle did not remove its restored Combat registration");
        });

        Probe("combat-failed-new-handle-aba", () =>
        {
            using var pipeline = new BattleCombatEventPipeline("combat-handle-aba");
            var leakedObserved = 0;
            var replacementObserved = 0;
            var checkpoint = pipeline.CaptureState();
            var leaked = pipeline.Subscribe(
                BattleCombatEventKind.AttackLanded,
                listenerSource,
                0,
                (_, _) => leakedObserved++);
            pipeline.RestoreState(checkpoint);
            pipeline.Publish(new BattleCombatEventDraft(
                BattleCombatEventKind.AttackLanded,
                eventSource,
                "source",
                "target",
                1));
            if (leakedObserved != 0)
                throw new InvalidOperationException("failed-transaction Combat handle remained subscribed after rollback");

            using var replacement = pipeline.Subscribe(
                BattleCombatEventKind.AttackLanded,
                listenerSource,
                0,
                (_, _) => replacementObserved++);
            var beforeStaleDispose = pipeline.CaptureSubscriptionSnapshot();
            if (beforeStaleDispose is not [{ Sequence: 1 }])
                throw new InvalidOperationException("Combat registration sequence was not restored for ABA coverage");
            leaked.Dispose();
            if (!pipeline.CaptureSubscriptionSnapshot().SequenceEqual(beforeStaleDispose))
                throw new InvalidOperationException("stale Combat handle removed the reused-sequence registration");
            pipeline.Publish(new BattleCombatEventDraft(
                BattleCombatEventKind.AttackLanded,
                eventSource,
                "source",
                "target",
                2));
            if (replacementObserved != 1 || leakedObserved != 0)
                throw new InvalidOperationException("replacement Combat listener did not survive stale-handle disposal");
        });

        Probe("combat-successful-dispose", () =>
        {
            using var pipeline = new BattleCombatEventPipeline("combat-handle-success");
            var observed = 0;
            var handle = pipeline.Subscribe(
                BattleCombatEventKind.AttackLanded,
                listenerSource,
                0,
                (_, _) => observed++);
            _ = pipeline.CaptureState();
            handle.Dispose();
            handle.Dispose();
            pipeline.Publish(new BattleCombatEventDraft(
                BattleCombatEventKind.AttackLanded,
                eventSource,
                "source",
                "target",
                1));
            if (observed != 0 || pipeline.SubscriptionCount != 0)
                throw new InvalidOperationException("successful Combat handle disposal was not idempotent");
        });

        var manualDamage = HandleContractBinding(
            "handle_manual_damage",
            new CompiledEffectTrigger(EffectTriggerKind.Manual, EffectDomainEventKind.None),
            new CompiledExplicitTargetQuery(),
            EffectKind.Damage);
        var reactiveShield = HandleContractBinding(
            "handle_reactive_shield",
            new CompiledEffectTrigger(EffectTriggerKind.DomainEvent, EffectDomainEventKind.DamageResolved),
            new CompiledOwnerTargetQuery(),
            EffectKind.Shield);

        Probe("effect-preexisting-rollback", () =>
        {
            var world = new SubscriptionHandleEffectWorld();
            using var scope = new BattleEffectScope("effect-handle-preexisting", world);
            var handle = scope.ActivateReactiveBinding(reactiveShield, "source", "source");
            var checkpoint = scope.CaptureState();
            handle.Dispose();
            scope.RestoreState(checkpoint);
            handle.Dispose();
            ExecuteHandleContractRoot(scope, manualDamage, 1);
            if (world.CommitCount(reactiveShield.StableId) != 0 || scope.SubscriptionCount != 0)
                throw new InvalidOperationException(
                    "original handle did not remove its restored Effect registration");
        });

        Probe("effect-failed-new-handle-aba", () =>
        {
            var world = new SubscriptionHandleEffectWorld();
            using var scope = new BattleEffectScope("effect-handle-aba", world);
            var checkpoint = scope.CaptureState();
            var leaked = scope.ActivateReactiveBinding(reactiveShield, "source", "source");
            scope.RestoreState(checkpoint);
            ExecuteHandleContractRoot(scope, manualDamage, 1);
            if (world.CommitCount(reactiveShield.StableId) != 0)
                throw new InvalidOperationException("failed-transaction Effect handle remained subscribed after rollback");

            using var replacement = scope.ActivateReactiveBinding(reactiveShield, "source", "source");
            if (scope.SubscriptionCount != 1)
                throw new InvalidOperationException("replacement Effect registration was not created");
            leaked.Dispose();
            if (scope.SubscriptionCount != 1)
                throw new InvalidOperationException("stale Effect handle removed the reused-sequence registration");
            ExecuteHandleContractRoot(scope, manualDamage, 2);
            if (world.CommitCount(reactiveShield.StableId) != 1)
                throw new InvalidOperationException("replacement Effect listener did not survive stale-handle disposal");
        });

        Probe("effect-successful-dispose", () =>
        {
            var world = new SubscriptionHandleEffectWorld();
            using var scope = new BattleEffectScope("effect-handle-success", world);
            var handle = scope.ActivateReactiveBinding(reactiveShield, "source", "source");
            _ = scope.CaptureState();
            handle.Dispose();
            handle.Dispose();
            ExecuteHandleContractRoot(scope, manualDamage, 1);
            if (world.CommitCount(reactiveShield.StableId) != 0 || scope.SubscriptionCount != 0)
                throw new InvalidOperationException("successful Effect handle disposal was not idempotent");
        });

        Probe("effect-completion-handle", () =>
        {
            var world = new SubscriptionHandleEffectWorld();
            using var scope = new BattleEffectScope("effect-handle-completion", world);
            var handle = scope.ActivateReactiveBinding(reactiveShield, "source", "source");
            var transition = scope.Complete(BattleScopeCompletionReason.Abort, 1);
            handle.Dispose();
            handle.Dispose();
            if (transition.RemainingSubscriptions != 0 || scope.SubscriptionCount != 0)
                throw new InvalidOperationException("Effect completion retained subscription-handle state");
        });

        if (failures.Count > 0)
            throw new InvalidOperationException("subscription handle transaction contract: " + string.Join(" | ", failures));
    }

    private static CompiledEffectBinding HandleContractBinding(
        string stableId,
        CompiledEffectTrigger trigger,
        CompiledEffectTargetQuery target,
        EffectKind kind) => new(
        stableId,
        0,
        trigger,
        [],
        target,
        [new CompiledEffectStep(kind, EffectAmountSource.Fixed, 1)],
        new CompiledEffectBindingLimits(0, 0, 0, 0),
        null);

    private static void ExecuteHandleContractRoot(
        BattleEffectScope scope,
        CompiledEffectBinding manualDamage,
        int tick)
    {
        var result = scope.ExecuteImmediate(manualDamage, "source", "source", "target", tick);
        if (result.Status != EffectExecutionStatus.Succeeded)
            throw new InvalidOperationException(
                $"Effect handle root failed: {result.Status}/{result.Interruption}");
    }

    private static ActiveRunDto LegacyTacticalRun(string heroId) => new()
    {
        Version = 3,
        LegacyHeroId = heroId,
        LegacyHeroHealthRatio = 1,
        Roster = [],
        Deployment = ["", "", "", "", "", ""],
        LegacyHeroCell = FormationCellDto.FromCell(new Vector2I(2, 5)),
        LegacyDeploymentCells = ActiveRunFormationSchema.CloneCells(BattlefieldLayout.Version2SoldierCells)
    };

    private static (ImmutableArray<CompiledTacticalCommandDefinition> Compiled,
        TacticalCommandDefinition[] Authored) LoadProductionTacticalCommands()
    {
        var names = new[]
        {
            "tactical_beast_roar", "tactical_blood_rush", "tactical_duel_focus", "tactical_overclock",
            "tactical_paid_reinforcement", "tactical_raise_dead", "tactical_rally", "tactical_time_stop"
        };
        var authored = names.Select(name => GD.Load<TacticalCommandDefinition>(
                $"res://content/tactical-commands/definitions/{name}.tres") ??
            throw new InvalidOperationException("production tactical definition missing: " + name)).ToArray();
        var compiled = authored.Select(definition =>
        {
            var loadoutResult = AbilityDefinitionCompiler.CompileLoadout(definition.AbilityLoadout);
            var loadout = loadoutResult.Loadout ?? throw new InvalidOperationException(
                definition.StableId + " ability compile: " + string.Join(" | ", loadoutResult.Report.CoreErrors));
            var result = TacticalCommandDefinitionCompiler.Compile(
                definition,
                candidate => ReferenceEquals(candidate, definition.AbilityLoadout) ? loadout : null);
            return result.Definition ?? throw new InvalidOperationException(
                definition.StableId + " tactical compile: " + string.Join(" | ", result.Report.CoreErrors));
        }).ToImmutableArray();
        return (compiled, authored);
    }

    private static CompiledTacticalCommandDefinition TacticalFixtureCommand(
        string id,
        int tacticalPointCost,
        int goldCost = 0,
        int cooldownTicks = 0,
        int maxUses = 0,
        bool explicitTarget = false,
        string summonContentId = "",
        IEnumerable<CompiledAbilityOperation>? customOperations = null)
    {
        ImmutableArray<CompiledAbilityOperation> operations = customOperations?.ToImmutableArray() ??
            (string.IsNullOrWhiteSpace(summonContentId)
            ?
            [
                new CompiledCooldownAbilityOperation(
                    explicitTarget ? new CompiledExplicitTargetQuery() : new CompiledOwnerTargetQuery(),
                    CooldownAdjustmentKind.Reset,
                    0,
                    CooldownAdjustmentKind.None,
                    0)
            ]
            :
            [
                new CompiledSummonAbilityOperation(
                    AbilitySummonProfile.Mercenary,
                    1,
                    1,
                    1,
                    1,
                    true,
                    summonContentId)
            ]);
        var ability = new CompiledAbilityDefinition(
            "ability_" + id,
            id,
            id,
            AbilityActivationKind.ManualCommand,
            AbilityTriggerKind.None,
            0,
            goldCost,
            cooldownTicks,
            maxUses,
            0,
            operations,
            null);
        return new CompiledTacticalCommandDefinition(
            id,
            string.Empty,
            id,
            id,
            tacticalPointCost,
            ability,
            $"{id}:{tacticalPointCost}:{goldCost}:{cooldownTicks}:{maxUses}:{explicitTarget}:{summonContentId}");
    }

    private static TacticalCommandBattlePreparation TacticalFixturePreparation(
        CompiledTacticalCommandDefinition primary,
        CompiledTacticalCommandDefinition? secondary = null)
    {
        secondary ??= TacticalFixtureCommand("tactical_fixture_reserve", 3);
        var commands = ImmutableArray.Create(primary, secondary);
        return new TacticalCommandBattlePreparation(
            TacticalCommandBattlePreparationBuilder.Fingerprint(commands),
            commands);
    }

    private static RelicBattlePreparation ReactiveDeathRelicPreparation()
    {
        var authored = new RelicDefinition
        {
            StableId = "fixture_tactical_reactive_relic",
            ReactiveCounters =
            [
                new RelicReactiveCounterSpec
                {
                    CounterId = "enemy_deaths",
                    Scope = RelicCounterScope.Battle,
                    ResetPolicy = RelicCounterResetPolicy.BattleEnd,
                    Source = RelicCounterSourceKind.Death,
                    Team = 1,
                    Threshold = 1,
                    Consumption = 1,
                    Priority = 5,
                    Target = RelicThresholdTargetKind.FirstAliveTeamUnit,
                    TargetTeam = 0,
                    ThresholdEffect = new EffectBindingSpec
                    {
                        StableId = "fixture_tactical_reactive_shield",
                        Trigger = new EffectTriggerSpec { Kind = EffectTriggerKind.Manual },
                        TargetQuery = new ExplicitTargetQuerySpec(),
                        Effects =
                        [
                            new ShieldEffectSpec
                            {
                                AmountSource = EffectAmountSource.Fixed,
                                Amount = 3
                            }
                        ],
                        Limits = new EffectBindingLimitsSpec()
                    }
                }
            ]
        };
        var compiled = RelicDefinitionCompiler.Compile(authored).Definition ??
            throw new InvalidOperationException("Tactical Reactive Relic fixture did not compile");
        using var run = new RelicRunScope(new RelicRunKey(0x7AC71CUL, "tactical_hero", 0, 0));
        using var registration = run.Activate(compiled, new RelicRunInstanceState
        {
            InstanceId = "tactical-reactive-relic",
            ContentId = compiled.StableId,
            Counters = RelicRunScope.InitialRunCounters(compiled).ToList()
        });
        return run.PrepareBattle();
    }

    private static BattleSimulation TacticalBattle(
        CompiledTacticalCommandDefinition primary,
        int startingGold = 0,
        IReadOnlyDictionary<string, UnitSnapshot>? tacticalSummons = null,
        IBattleFloorRuleRuntime? floor = null,
        IEnumerable<BattleSpawn>? spawns = null,
        CompiledTacticalCommandDefinition? secondary = null,
        Action<BattleCombatBindingRegistry>? configureCombatBindings = null,
        TraitBattlePreparation? traits = null,
        RelicBattlePreparation? relics = null) => new(new BattleConfig
    {
        Seed = 0x7AC71CUL,
        Identity = new BattleIdentity("tactical-probe", TowerNodeType.Combat, 0x7AC71CUL, 0, 0),
        FloorRule = floor ?? new ClearFloorRuleRuntime("tactical", "tactical", "test"),
        HeroRule = ProductionRule(),
        StartingGold = startingGold,
        ConfigureCombatBindings = configureCombatBindings,
        TacticalCommands = TacticalFixturePreparation(primary, secondary),
        Traits = traits ?? TraitBattlePreparation.Empty,
        Relics = relics,
        TacticalSummons = tacticalSummons ?? ImmutableDictionary<string, UnitSnapshot>.Empty,
        Spawns = (spawns ??
        [
            new BattleSpawn(ProductionUnit("tactical_hero", true, 10000, 0, 1), 0,
                new Vector2I(1, 1), "hero"),
            new BattleSpawn(ProductionUnit("tactical_enemy", false, 10000, 0, 1), 1,
                new Vector2I(2, 1), "enemy")
        ]).ToList()
    });

    private static string TacticalFingerprint(BattleSimulation battle) => JsonSerializer.Serialize(new
    {
        battle.Outcome,
        battle.TickIndex,
        battle.RemainingGold,
        battle.GoldSpent,
        battle.SuccessfulTacticalCommandUses,
        TacticalCommands = battle.TacticalCommands,
        Units = battle.Units.OrderBy(unit => unit.RuntimeId, StringComparer.Ordinal).Select(unit => new
        {
            unit.RuntimeId,
            unit.SourceInstanceId,
            unit.Team,
            Cell = new { unit.Cell.X, unit.Cell.Y },
            unit.Health,
            unit.Shield,
            unit.AttackCooldown,
            unit.MoveCooldown,
            unit.DisabledTicks,
            unit.WaitingTicks,
            unit.IsTemporary,
            unit.IsPersistentRosterHero,
            unit.BossPhaseId,
            unit.Mode,
            unit.LastActionKind,
            unit.ActionTargetRuntimeId,
            unit.ActionTargetName,
            unit.Alive,
            AttributeModifierCount = unit.Attributes.ModifierCount,
            Attributes = Enum.GetValues<CombatAttribute>().Select(attribute => new
            {
                Attribute = attribute,
                Value = unit.Attributes.GetValue(attribute)
            }),
            Statuses = unit.Statuses
        }),
        PendingEvents = battle.PendingEvents,
        CombatEvents = battle.CombatEvents,
        CombatSubscriptions = battle.CombatSubscriptions,
        RelicCounterTransitions = battle.RelicCounterTransitions,
        battle.RelicModifierCount,
        EffectTrace = battle.EffectTrace,
        StatusPresentationCues = battle.StatusPresentationCues,
        Traits = new
        {
            battle.TraitSnapshot.Fingerprint,
            Values = battle.TraitSnapshot.Values
                .OrderBy(value => value.Team)
                .ThenBy(value => value.TraitId, StringComparer.Ordinal)
                .Select(value => new
                {
                    value.TraitId,
                    value.Team,
                    value.Value,
                    ActiveBreakpointIndex = value.ActiveBreakpoint?.Index,
                    ActiveBreakpointFingerprint = value.ActiveBreakpoint?.Fingerprint,
                    ContributionCount = value.Contributions.Length
                }),
            Contributions = battle.TraitSnapshot.Contributions
                .OrderBy(contribution => contribution.Team)
                .ThenBy(contribution => contribution.TraitId, StringComparer.Ordinal)
                .ThenBy(contribution => contribution.SourceInstanceId, StringComparer.Ordinal)
        },
        battle.EffectTransition,
        battle.AttributeTransition,
        battle.CombatTransition,
        battle.AbilityTransition,
        battle.StatusTransition,
        battle.RelicTransition,
        battle.EquipmentTransition,
        battle.TacticalCommandTransition,
        battle.TraitTransition,
        Result = battle.CreateResult()
    });

    private static void VerifyTacticalSourceFallbackAndFailure()
    {
        var command = TacticalFixtureCommand("tactical_source_fallback", 1);
        var spawns = new[]
        {
            new BattleSpawn(ProductionUnit("starting", true, 100, 0, 1), 0,
                new Vector2I(0, 1), "a_starting", IsPersistentRosterHero: true),
            new BattleSpawn(ProductionUnit("survivor", true, 100, 0, 1), 0,
                new Vector2I(1, 1), "b_survivor", IsPersistentRosterHero: true),
            new BattleSpawn(ProductionUnit("enemy", false, 10000, 0, 1), 1,
                new Vector2I(2, 1), "enemy")
        };
        using (var fallback = TacticalBattle(command, spawns: spawns))
        {
            var starting = fallback.Units.Single(unit => unit.RuntimeId == "a_starting");
            starting.Health = 0;
            starting.Mode = BattleUnitMode.Defeated;
            var survivor = fallback.Units.Single(unit => unit.RuntimeId == "b_survivor");
            survivor.AttackCooldown = 7;
            fallback.DrainEvents();
            if (!fallback.TryUseTacticalCommand(0).Succeeded || survivor.AttackCooldown != 0 ||
                !fallback.DrainEvents().Any(item =>
                    item.Type == "tactical_command" && item.SourceRuntimeId == survivor.RuntimeId))
                throw new InvalidOperationException(
                    "starting-hero defeat blocked a tactical command despite another living persistent hero");
        }

        using var unavailable = TacticalBattle(command);
        var onlyHero = unavailable.Units.Single(unit => unit.RuntimeId == "hero");
        onlyHero.Health = 0;
        onlyHero.Mode = BattleUnitMode.Defeated;
        var before = TacticalFingerprint(unavailable);
        var failed = unavailable.TryUseTacticalCommand(0);
        if (failed.Succeeded || failed.Failure != TacticalCommandActivationFailure.SourceUnavailable ||
            TacticalFingerprint(unavailable) != before)
            throw new InvalidOperationException("missing persistent tactical source changed Battle state");

        var invalidBefore = TacticalFingerprint(unavailable);
        var invalidSlot = unavailable.TryUseTacticalCommand(2);
        if (invalidSlot.Succeeded || invalidSlot.Failure != TacticalCommandActivationFailure.InvalidSlot ||
            TacticalFingerprint(unavailable) != invalidBefore)
            throw new InvalidOperationException("invalid tactical slot changed Battle state");
    }

    private static void VerifyTacticalCompletionPaths()
    {
        var command = TacticalFixtureCommand("tactical_completion", 1);
        BattleSimulation Battle(float playerHealth, float playerDamage, float enemyHealth, float enemyDamage,
            IBattleFloorRuleRuntime? floor = null) => TacticalBattle(
            command,
            floor: floor,
            spawns:
            [
                new BattleSpawn(ProductionUnit("completion_player", true, playerHealth, playerDamage, 1), 0,
                    new Vector2I(1, 1), "player"),
                new BattleSpawn(ProductionUnit("completion_enemy", false, enemyHealth, enemyDamage, 1), 1,
                    new Vector2I(2, 1), "enemy")
            ]);

        var victory = Battle(100, 100, 1, 0);
        if (!victory.TryUseTacticalCommand(0).Succeeded) throw new InvalidOperationException("victory tactical setup failed");
        victory.Step();
        AssertTacticalCompletion(victory, TacticalCommandScopeCompletionReason.BattleCompleted, "victory");
        victory.Dispose();

        var defeat = Battle(1, 0, 100, 100);
        if (!defeat.TryUseTacticalCommand(0).Succeeded) throw new InvalidOperationException("defeat tactical setup failed");
        defeat.Step();
        AssertTacticalCompletion(defeat, TacticalCommandScopeCompletionReason.BattleCompleted, "defeat");
        defeat.Dispose();

        var timeout = Battle(10000, 0, 10000, 0);
        if (!timeout.TryUseTacticalCommand(0).Succeeded) throw new InvalidOperationException("timeout tactical setup failed");
        timeout.RunToEnd();
        AssertTacticalCompletion(timeout, TacticalCommandScopeCompletionReason.BattleCompleted, "timeout");
        timeout.Dispose();

        var aborted = Battle(10000, 0, 10000, 0);
        if (!aborted.TryUseTacticalCommand(0).Succeeded) throw new InvalidOperationException("abort tactical setup failed");
        aborted.Abort();
        AssertTacticalCompletion(aborted, TacticalCommandScopeCompletionReason.Abort, "abort");
        aborted.Dispose();

        var replaced = Battle(10000, 0, 10000, 0);
        if (!replaced.TryUseTacticalCommand(0).Succeeded) throw new InvalidOperationException("replacement tactical setup failed");
        replaced.Replace();
        AssertTacticalCompletion(replaced, TacticalCommandScopeCompletionReason.Replacement, "replacement");
        replaced.Dispose();

        var throwing = Battle(10000, 0, 10000, 0, new ThrowingTickRule());
        if (!throwing.TryUseTacticalCommand(0).Succeeded) throw new InvalidOperationException("exception tactical setup failed");
        try { throwing.Step(); }
        catch (InvalidOperationException) { }
        AssertTacticalCompletion(throwing, TacticalCommandScopeCompletionReason.Exception, "exception");
        throwing.Dispose();

        var disposed = Battle(10000, 0, 10000, 0);
        if (!disposed.TryUseTacticalCommand(0).Succeeded) throw new InvalidOperationException("dispose tactical setup failed");
        disposed.Dispose();
        AssertTacticalCompletion(disposed, TacticalCommandScopeCompletionReason.Disposal, "disposal");
    }

    private static void AssertTacticalCompletion(
        BattleSimulation battle,
        TacticalCommandScopeCompletionReason reason,
        string label)
    {
        if (battle.TacticalCommandTransition is not { } transition ||
            transition.Reason != reason || transition.RemainingPoints != 0 ||
            transition.RemainingRuntimeInstances != 0 || battle.TacticalPoints != 0 ||
            battle.TacticalCommands.Slots.Length != 0)
            throw new InvalidOperationException(label + " tactical scope retained Battle state");
        var before = battle.SuccessfulTacticalCommandUses;
        if (battle.TryUseTacticalCommand(0).Succeeded || battle.SuccessfulTacticalCommandUses != before)
            throw new InvalidOperationException(label + " accepted a tactical command after completion");
    }

    private static void VerifyHeroOwnedEquipmentBehavior()
    {
        var authored = new EquipmentDefinition
        {
            StableId = "equipment_probe",
            AttributeModifiers =
            [
                new AttributeModifierSpec
                {
                    Attribute = CombatAttribute.MaxHealth,
                    Operation = AttributeModifierOperation.Add,
                    Magnitude = new ConstantAttributeMagnitudeSpec { Value = 100 },
                    SlotId = "probe_max_health"
                },
                new AttributeModifierSpec
                {
                    Attribute = CombatAttribute.AttackDamage,
                    Operation = AttributeModifierOperation.Add,
                    Magnitude = new ConstantAttributeMagnitudeSpec { Value = 5 },
                    SlotId = "probe_damage"
                }
            ]
        };
        var compiledResult = EquipmentDefinitionCompiler.Compile(authored);
        var compiled = compiledResult.Definition ?? throw new InvalidOperationException(
            "Equipment definition did not compile: " + string.Join("; ", compiledResult.Report.CoreErrors));
        var firstFingerprint = compiled.Fingerprint;
        ((ConstantAttributeMagnitudeSpec)authored.AttributeModifiers[1].Magnitude).Value = 6;
        var changed = EquipmentDefinitionCompiler.Compile(authored).Definition ??
            throw new InvalidOperationException("changed Equipment definition did not compile");
        if (changed.Fingerprint == firstFingerprint)
            throw new InvalidOperationException("Equipment deep fingerprint ignored nested modifier magnitude");
        ((ConstantAttributeMagnitudeSpec)authored.AttributeModifiers[1].Magnitude).Value = 5;
        compiled = EquipmentDefinitionCompiler.Compile(authored).Definition!;

        var invalid = EquipmentDefinitionCompiler.Compile(new EquipmentDefinition
        {
            StableId = "equipment_invalid",
            AttributeModifiers = [new AttributeModifierSpec { SlotId = "invalid" }]
        });
        if (!invalid.Report.HasCoreErrors || invalid.Definition is not null ||
            !invalid.Report.CoreErrors.Any(error => error.Contains("attribute modifier[0]", StringComparison.Ordinal)))
            throw new InvalidOperationException("Equipment compiler omitted indexed modifier diagnostics or retained partial output");

        var graph = new CompiledContentGraph(
            [],
            ImmutableArray<CompiledAbilityDefinition>.Empty,
            ImmutableArray<CompiledStatusDefinition>.Empty,
            ImmutableArray<TowerAutobattler.Relics.CompiledRelicDefinition>.Empty,
            [compiled]);
        var rules = EquipmentProbeRunRules();
        if (rules.EquipmentSlotCapacity != 3)
            throw new InvalidOperationException("Equipment slot capacity is not the authored three-slot contract");

        var run = new ActiveRunDto
        {
            Version = ActiveRunFormationSchema.CurrentVersion,
            Roster =
            [
                new RosterHeroInstanceDto { InstanceId = "hero-a", ContentId = "hero-a-content" },
                new RosterHeroInstanceDto { InstanceId = "hero-b", ContentId = "hero-b-content" }
            ],
            CurrentPopulation = 2,
            Deployment = ActiveRunFormationSchema.EmptyDeployment(),
            Items = []
        };
        var persistence = new EquipmentProbePersistence(graph, rules);
        var service = new RunEquipmentService(graph, rules, persistence);
        if (!service.Equip(run, "hero-a", 0, compiled.StableId) ||
            !service.Equip(run, "hero-b", 0, compiled.StableId))
            throw new InvalidOperationException("two persistent heroes could not equip the same definition");
        var equipmentA = run.Roster[0].Equipment.Single();
        var equipmentB = run.Roster[1].Equipment.Single();
        if (equipmentA.InstanceId == equipmentB.InstanceId ||
            equipmentA.OwnerHeroInstanceId != "hero-a" || equipmentB.OwnerHeroInstanceId != "hero-b")
            throw new InvalidOperationException("same-definition Equipment instances did not retain owner/source identity");
        if (service.Equip(run, "temporary-unit", 0, compiled.StableId) ||
            service.Equip(run, "hero-a", 3, compiled.StableId))
            throw new InvalidOperationException("temporary ownership or a fourth Equipment slot was accepted");
        if (!service.Equip(run, "hero-a", 2, compiled.StableId) || !service.Remove(run, "hero-a", 2))
            throw new InvalidOperationException("authored third Equipment slot or removal failed");

        var serialized = JsonSerializer.Serialize(run);
        var roundTrip = JsonSerializer.Deserialize<ActiveRunDto>(serialized) ??
            throw new InvalidOperationException("Equipment save round-trip returned null");
        if (roundTrip.Roster[0].Equipment.Single().OwnerHeroInstanceId != "hero-a" ||
            roundTrip.Roster[1].Equipment.Single().OwnerHeroInstanceId != "hero-b")
            throw new InvalidOperationException("Equipment save round-trip lost owner or slot state");

        var beforeFailure = JsonSerializer.Serialize(run);
        persistence.FailPublication = true;
        if (service.Equip(run, "hero-a", 0, compiled.StableId) ||
            JsonSerializer.Serialize(run) != beforeFailure)
            throw new InvalidOperationException("failed Equipment replacement mutated authoritative Run state");
        if (service.Remove(run, "hero-a", 0) || JsonSerializer.Serialize(run) != beforeFailure)
            throw new InvalidOperationException("failed Equipment removal mutated authoritative Run state");
        persistence.FailPublication = false;
        if (!service.Equip(run, "hero-a", 0, compiled.StableId))
            throw new InvalidOperationException("successful Equipment replacement failed");
        equipmentA = run.Roster[0].Equipment.Single();
        equipmentB = run.Roster[1].Equipment.Single();
        if (equipmentA.InstanceId == equipmentB.InstanceId)
            throw new InvalidOperationException("replacement reused another hero's Equipment instance identity");

        run.Deployment[0] = "hero-a";
        run.Deployment[1] = "hero-b";
        var preparation = EquipmentBattlePreparationBuilder.Build(run, graph, ["hero-a", "hero-b"]);
        using var attributes = new BattleAttributeScope("equipment_probe_attributes");
        var baseAttributes = AttributeDefinitionCompiler.Legacy(new Dictionary<CombatAttribute, float>
        {
            [CombatAttribute.MaxHealth] = 100,
            [CombatAttribute.AttackDamage] = 10,
            [CombatAttribute.AttackSpeed] = 1,
            [CombatAttribute.MoveSpeed] = 1
        });
        var ownerA = attributes.CreateSet("runtime-a", baseAttributes);
        var ownerB = attributes.CreateSet("runtime-b", baseAttributes);
        using (var battleEquipment = new EquipmentBattleScope(
                   "equipment_probe_battle",
                   preparation,
                   [
                       new EquipmentOwnerBinding("hero-a", "runtime-a", true, ownerA),
                       new EquipmentOwnerBinding("hero-b", "runtime-b", true, ownerB)
                   ]))
        {
            Near(ownerA.GetValue(CombatAttribute.AttackDamage), 15, "owner A Equipment projection");
            Near(ownerB.GetValue(CombatAttribute.AttackDamage), 15, "owner B Equipment projection");
            if (!battleEquipment.Remove(equipmentA.InstanceId))
                throw new InvalidOperationException("owner A Equipment removal failed");
            Near(ownerA.GetValue(CombatAttribute.AttackDamage), 10, "owner A Equipment rollback");
            Near(ownerB.GetValue(CombatAttribute.AttackDamage), 15, "owner B Equipment isolation");
            var transition = battleEquipment.Complete(EquipmentBattleCompletionReason.BattleCompleted);
            Near(ownerB.GetValue(CombatAttribute.AttackDamage), 10, "owner B Equipment completion rollback");
            if (transition is not
                { RemainingInstances: 0, RemainingModifierHandles: 0, RemainingSubscriptions: 0 })
                throw new InvalidOperationException(
                    "Equipment Battle scope retained instances, modifier handles, or subscriptions");
        }

        var temporaryAttributes = attributes.CreateSet("temporary-runtime", baseAttributes);
        ExpectThrows(
            () => new EquipmentBattleScope(
                "equipment_temporary_owner",
                EquipmentBattlePreparationBuilder.Build(run, graph, ["hero-a"]),
                [new EquipmentOwnerBinding("hero-a", "temporary-runtime", false, temporaryAttributes)]),
            "temporary Equipment owner");
        Near(temporaryAttributes.GetValue(CombatAttribute.AttackDamage), 10,
            "temporary owner rejected without modifier leakage");
        var attributeTransition = attributes.Complete(AttributeScopeCompletionReason.BattleCompleted, 0);
        if (attributeTransition is not { RemainingSets: 0, RemainingModifiers: 0 })
            throw new InvalidOperationException("Equipment Attribute scope did not clean completely");

        VerifyEquipmentBattleResultLifecycle(
            EquipmentBattlePreparationBuilder.Build(run, graph, ["hero-a"]));
    }

    private static void VerifyEquipmentBattleResultLifecycle(EquipmentBattlePreparation preparation)
    {
        var running = new BattleSimulation(EquipmentResultConfig(
            0xE001UL,
            preparation,
            enemyHealth: 1_000_000_000,
            enemyDamage: 0,
            floor: new OneShotLethalFloorRule("equipment_floor", "hero-a", 10)));
        AssertEquipmentResultProjection(running.CreateResult(), 150, "running initial projection");
        if (running.Step() != BattleOutcome.Running)
            throw new InvalidOperationException("running Equipment result fixture completed unexpectedly");
        var runningHealth = running.Units.Single(unit => unit.SourceInstanceId == "hero-a").Health;
        if (runningHealth >= 150)
            throw new InvalidOperationException("running Equipment result fixture did not mutate live health");
        AssertEquipmentResultProjection(running.CreateResult(), runningHealth, "running live projection");
        running.Abort();
        AssertEquipmentCompletion(running, EquipmentBattleCompletionReason.Abort,
            BattleCombatCompletionReason.Abort, "running abort");
        AssertEquipmentResultProjection(running.CreateResult(), runningHealth, "abort frozen projection");

        var victory = new BattleSimulation(EquipmentResultConfig(
            0xE002UL, preparation, playerDamage: 100, enemyHealth: 1, enemyDamage: 0));
        var victoryResult = victory.RunToEnd();
        if (victoryResult.Outcome != BattleOutcome.PlayerVictory)
            throw new InvalidOperationException("Equipment result victory fixture outcome changed");
        AssertEquipmentCompletion(victory, EquipmentBattleCompletionReason.BattleCompleted,
            BattleCombatCompletionReason.PlayerVictory, "natural victory");
        AssertEquipmentResultProjection(victoryResult, 150, "natural victory projection", 105);
        AssertRepeatedEquipmentResult(victory, victoryResult, "natural victory");

        var defeat = new BattleSimulation(EquipmentResultConfig(
            0xE003UL, preparation, enemyHealth: 100, enemyDamage: 1_000, enemyActsFirst: true));
        var defeatResult = defeat.RunToEnd();
        if (defeatResult.Outcome != BattleOutcome.PlayerDefeat)
            throw new InvalidOperationException("Equipment result defeat fixture outcome changed");
        AssertEquipmentCompletion(defeat, EquipmentBattleCompletionReason.BattleCompleted,
            BattleCombatCompletionReason.PlayerDefeat, "natural defeat");
        AssertEquipmentResultProjection(defeatResult, 0, "natural defeat projection");
        AssertRepeatedEquipmentResult(defeat, defeatResult, "natural defeat");

        var timeout = new BattleSimulation(EquipmentResultConfig(
            0xE004UL,
            preparation,
            enemyHealth: 1_000_000_000,
            enemyDamage: 0,
            floor: new IsolatedEquipmentCellsFloorRule(),
            enemyCell: new Vector2I(10, 5)));
        var timeoutResult = timeout.RunToEnd();
        if (timeoutResult.Outcome != BattleOutcome.Timeout)
            throw new InvalidOperationException("Equipment result timeout fixture outcome changed");
        AssertEquipmentCompletion(timeout, EquipmentBattleCompletionReason.BattleCompleted,
            BattleCombatCompletionReason.Timeout, "natural timeout");
        AssertEquipmentResultProjection(timeoutResult, 150, "natural timeout projection");
        AssertRepeatedEquipmentResult(timeout, timeoutResult, "natural timeout");

        var replacement = new BattleSimulation(EquipmentResultConfig(
            0xE005UL, preparation, enemyHealth: 1_000_000_000, enemyDamage: 0));
        replacement.Replace();
        AssertEquipmentCompletion(replacement, EquipmentBattleCompletionReason.Replacement,
            BattleCombatCompletionReason.Replacement, "replacement");
        AssertEquipmentResultProjection(replacement.CreateResult(), 150, "replacement frozen projection");

        var exception = new BattleSimulation(EquipmentResultConfig(
            0xE006UL,
            preparation,
            enemyHealth: 1_000_000_000,
            enemyDamage: 0,
            floor: new ThrowingTickRule()));
        ExpectThrows(() => exception.Step(), "Equipment result exception completion");
        AssertEquipmentCompletion(exception, EquipmentBattleCompletionReason.Exception,
            BattleCombatCompletionReason.Exception, "exception");
        AssertEquipmentResultProjection(exception.CreateResult(), 150, "exception frozen projection");

        var disposal = new BattleSimulation(EquipmentResultConfig(
            0xE007UL, preparation, enemyHealth: 1_000_000_000, enemyDamage: 0));
        disposal.Dispose();
        AssertEquipmentCompletion(disposal, EquipmentBattleCompletionReason.Disposal,
            BattleCombatCompletionReason.Disposal, "disposal");
        AssertEquipmentResultProjection(disposal.CreateResult(), 150, "disposal frozen projection");
    }

    private static BattleConfig EquipmentResultConfig(
        ulong seed,
        EquipmentBattlePreparation preparation,
        float playerDamage = 10,
        float enemyHealth = 100,
        float enemyDamage = 0,
        bool enemyActsFirst = false,
        IBattleFloorRuleRuntime? floor = null,
        Vector2I? enemyCell = null) =>
        new()
        {
            Seed = seed,
            Identity = new BattleIdentity("equipment_result_contract", TowerNodeType.Combat, seed, 1, 1),
            FloorRule = floor ?? new ClearFloorRuleRuntime("equipment_result", "equipment_result", "test"),
            HeroRule = ProductionRule(),
            Equipment = preparation,
            Spawns =
            [
                new BattleSpawn(ProductionUnit("equipment_owner", true, 100, playerDamage, 1), 0,
                    new Vector2I(1, 1), "hero-a", .75f, IsPersistentRosterHero: true),
                new BattleSpawn(ProductionUnit("equipment_enemy", false, enemyHealth, enemyDamage, 1), 1,
                    enemyCell ?? new Vector2I(2, 1),
                    enemyActsFirst ? "a-equipment-enemy" : "z-equipment-enemy")
            ]
        };

    private static void AssertEquipmentResultProjection(
        BattleResult result,
        float expectedHealth,
        string label,
        float expectedDamage = 15)
    {
        var owner = result.Units.Single(unit => unit.SourceInstanceId == "hero-a");
        Near(owner.FinalHealth, expectedHealth, label + " health");
        Near(owner.MaxHealth, 200, label + " max health");
        Near(owner.FinalDamage, expectedDamage, label + " damage");
        if (owner.Alive != (expectedHealth > 0))
            throw new InvalidOperationException(label + " alive state does not match combat-end health");
    }

    private static void AssertRepeatedEquipmentResult(
        BattleSimulation battle,
        BattleResult expected,
        string label)
    {
        var first = JsonSerializer.Serialize(expected);
        var second = JsonSerializer.Serialize(battle.CreateResult());
        var third = JsonSerializer.Serialize(battle.CreateResult());
        if (first != second || second != third)
            throw new InvalidOperationException(label + " repeated post-completion result changed");
    }

    private static void AssertEquipmentCompletion(
        BattleSimulation battle,
        EquipmentBattleCompletionReason equipmentReason,
        BattleCombatCompletionReason combatReason,
        string label)
    {
        var equipment = battle.EquipmentTransition ??
            throw new InvalidOperationException(label + " Equipment transition missing");
        if (equipment.Reason != equipmentReason ||
            equipment.RemainingInstances != 0 || equipment.RemainingModifierHandles != 0 ||
            equipment.RemainingSubscriptions != 0)
            throw new InvalidOperationException(label + " retained Equipment Battle state or used the wrong reason");
        AssertBattleScopesZero(battle, label, combatReason);
    }

    private sealed class IsolatedEquipmentCellsFloorRule()
        : ClearFloorRuleRuntime("equipment_isolated", "equipment_isolated", "test")
    {
        public override bool CanOccupy(Vector2I cell) =>
            cell == new Vector2I(1, 1) || cell == new Vector2I(10, 5);
    }

    private static CompiledRunRules EquipmentProbeRunRules() => new(
        10, 18, 3, 3, 7, 3,
        2, ImmutableArray.Create("tactical_rally", "tactical_time_stop"),
        ImmutableDictionary<string, string>.Empty,
        3, 3, 16, 7, 12, 18,
        .12f, .15f, .15f, .1f, .25f,
        18, .65f, .25f, .25f, 6, .35f, .45f, 8, 3);

    private static CompiledRunRules CompileRunRules(RunRulesDefinition authored) => new(
        authored.OrdinaryPopulationCap,
        authored.PhysicalDeploymentCeiling,
        authored.ReserveCapacity,
        authored.StarterRosterHeroCount,
        authored.InitialPopulation,
        authored.EquipmentSlotCapacity,
        ActiveRunTacticalCommandPolicy.SlotCount,
        authored.StarterTacticalCommands.Select(command => command.StableId).ToImmutableArray(),
        authored.LegacyHeroTacticalCommandMappings.ToImmutableDictionary(
            mapping => mapping.HeroContentId,
            mapping => mapping.Command.StableId,
            StringComparer.Ordinal),
        authored.RecruitmentChoiceCount,
        authored.ItemChoiceCount,
        authored.StartingGold,
        authored.NormalBattleGold,
        authored.EliteBattleGold,
        authored.BossBattleGold,
        authored.VictoryHeroRecovery,
        authored.VictorySoldierRecovery,
        authored.MinimumVictoryHeroHealth,
        authored.MinimumLivingSoldierHealth,
        authored.DefeatedSoldierHealth,
        authored.RiskyEventSuccessGold,
        authored.RiskyEventSuccessChance,
        authored.RiskyEventHealthLoss,
        authored.RiskyEventMinimumHealth,
        authored.SafeEventGold,
        authored.RestHeroHealing,
        authored.RestSoldierHealing,
        authored.RestGold,
        authored.InitialUnlockedHeroCount);

    private sealed class EquipmentProbePersistence(
        CompiledContentGraph graph,
        CompiledRunRules rules) : IRunEquipmentPersistence
    {
        public bool FailPublication { get; set; }

        public bool ValidateRun(ActiveRunDto run)
        {
            if (run.Roster is null || run.Items is null || run.Roster.Any(hero => hero?.Equipment is null))
                return false;
            var instances = new HashSet<string>(run.Roster.Select(hero => hero.InstanceId), StringComparer.Ordinal);
            foreach (var hero in run.Roster)
            {
                if (hero.Equipment.Count > rules.EquipmentSlotCapacity) return false;
                var slots = new HashSet<int>();
                foreach (var equipment in hero.Equipment)
                    if (equipment is null || equipment.OwnerHeroInstanceId != hero.InstanceId ||
                        string.IsNullOrWhiteSpace(equipment.InstanceId) || !instances.Add(equipment.InstanceId) ||
                        equipment.SlotIndex < 0 || equipment.SlotIndex >= rules.EquipmentSlotCapacity ||
                        !slots.Add(equipment.SlotIndex) || !graph.TryGetEquipment(equipment.ContentId, out _))
                        return false;
            }
            return true;
        }

        public ActiveRunDto CloneRun(ActiveRunDto source) => Clone(source);

        public bool TryPublish(ActiveRunDto working, ActiveRunDto authoritative)
        {
            if (FailPublication) return false;
            var copy = Clone(working);
            authoritative.Version = copy.Version;
            authoritative.Seed = copy.Seed;
            authoritative.Roster = copy.Roster;
            authoritative.CurrentPopulation = copy.CurrentPopulation;
            authoritative.PopulationCapSources = copy.PopulationCapSources;
            authoritative.Deployment = copy.Deployment;
            authoritative.Items = copy.Items;
            authoritative.Gold = copy.Gold;
            authoritative.FloorIndex = copy.FloorIndex;
            authoritative.BattleNumber = copy.BattleNumber;
            authoritative.PendingNode = copy.PendingNode;
            authoritative.SelectedNode = copy.SelectedNode;
            return true;
        }

        private static ActiveRunDto Clone(ActiveRunDto value) =>
            JsonSerializer.Deserialize<ActiveRunDto>(JsonSerializer.Serialize(value)) ??
            throw new InvalidOperationException("Equipment probe clone failed");
    }

    private static void VerifyUnifiedHeroPopulationBehavior()
    {
        var currentVersion = (int)(typeof(ActiveRunFormationSchema)
            .GetField(nameof(ActiveRunFormationSchema.CurrentVersion))?.GetRawConstantValue() ?? 0);
        if (currentVersion != 4)
            throw new InvalidOperationException("active-run schema did not advance incrementally from v3 to v4");

        var rosterProperty = typeof(ActiveRunDto).GetProperty(nameof(ActiveRunDto.Roster)) ??
            throw new InvalidOperationException("active run has no persistent roster");
        var rosterElement = rosterProperty.PropertyType.IsGenericType
            ? rosterProperty.PropertyType.GetGenericArguments().SingleOrDefault()
            : null;
        if (rosterElement?.Name != "RosterHeroInstanceDto")
            throw new InvalidOperationException("persistent roster entries are not unified hero instances");

        var authoredRules = GD.Load<RunRulesDefinition>("res://content/project/alpha_run_rules.tres") ??
            throw new InvalidOperationException("authored Run rules are missing");
        var compiledRules = CompileRunRules(authoredRules);
        if (authoredRules.OrdinaryPopulationCap != 10 || authoredRules.InitialPopulation != 7 ||
            authoredRules.PhysicalDeploymentCeiling != 18 ||
            BattlefieldLayout.PlayerDeploymentCells.Length != 18 ||
            BattlefieldLayout.PlayerDeploymentCells.Distinct().Count() != 18)
            throw new InvalidOperationException("ordinary 10 or physical 18 population authority changed");

        var legacy = new ActiveRunDto
        {
            Version = 3,
            LegacyHeroId = "hero_banner_marshal",
            LegacyHeroHealthRatio = .65f,
            Roster =
            [
                new RosterHeroInstanceDto
                {
                    InstanceId = "legacy-recruit",
                    ContentId = "legacy_recruit",
                    HealthRatio = .75f
                }
            ],
            Deployment = ["legacy-recruit", "", "", "", "", ""],
            LegacyHeroCell = FormationCellDto.FromCell(new Vector2I(2, 5)),
            LegacyDeploymentCells = ActiveRunFormationSchema.CloneCells(BattlefieldLayout.Version2SoldierCells)
        };
        if (!ActiveRunFormationSchema.TryMigrateToCurrent(legacy, compiledRules) || legacy.Version != 4 ||
            legacy.Roster.Count != 2 || legacy.Roster[0].ContentId != "hero_banner_marshal" ||
            legacy.Roster[0].HealthRatio != .65f || legacy.CurrentPopulation != 7 ||
            legacy.Deployment.Count != 18 ||
            BattlefieldLayout.PlayerDeploymentCells[legacy.Deployment.IndexOf(legacy.Roster[0].InstanceId)] !=
            new Vector2I(2, 5) ||
            BattlefieldLayout.PlayerDeploymentCells[legacy.Deployment.IndexOf("legacy-recruit")] !=
            BattlefieldLayout.Version2SoldierCells[0])
            throw new InvalidOperationException("lossless v3 roster/health/exact-cell migration changed");

        var ambiguous = new ActiveRunDto
        {
            Version = 3,
            LegacyHeroId = "hero_banner_marshal",
            LegacyHeroHealthRatio = 1,
            Roster = [new RosterHeroInstanceDto { InstanceId = "legacy-recruit", ContentId = "legacy_recruit" }],
            Deployment = ["legacy-recruit", "", "", "", "", ""],
            LegacyHeroCell = FormationCellDto.FromCell(BattlefieldLayout.Version2SoldierCells[0]),
            LegacyDeploymentCells = ActiveRunFormationSchema.CloneCells(BattlefieldLayout.Version2SoldierCells)
        };
        if (ActiveRunFormationSchema.TryMigrateToCurrent(ambiguous, compiledRules) || ambiguous.Version != 3 ||
            ambiguous.Roster.Count != 1)
            throw new InvalidOperationException("lossy v3 cell migration was not rejected atomically");

        using (var group = new BattleSimulation(PopulationOutcomeConfig(
                   new BattleSpawn(ProductionUnit("starting", true, 1, 0, 10), 0,
                       new Vector2I(0, 0), "b_starting", IsPersistentRosterHero: true),
                   new BattleSpawn(ProductionUnit("ally", false, 1000, 0, 10), 0,
                       new Vector2I(0, 5), "c_ally", IsPersistentRosterHero: true))))
        {
            group.Step();
            if (group.Units.Single(unit => unit.RuntimeId == "b_starting").Alive ||
                !group.Units.Single(unit => unit.RuntimeId == "c_ally").Alive ||
                group.Outcome != BattleOutcome.Running)
                throw new InvalidOperationException("starting-hero defeat still ended a living roster battle");
        }

        using (var temporaryOnly = new BattleSimulation(PopulationOutcomeConfig(
                   new BattleSpawn(ProductionUnit("persistent", true, 1, 0, 10), 0,
                       new Vector2I(0, 0), "b_persistent", IsPersistentRosterHero: true),
                   new BattleSpawn(ProductionUnit("temporary", false, 1000, 0, 10), 0,
                       new Vector2I(0, 5), "c_temporary", IsTemporary: true))))
        {
            temporaryOnly.Step();
            var temporary = temporaryOnly.Units.Single(unit => unit.RuntimeId == "c_temporary");
            if (!temporary.Alive || !temporary.IsTemporary || temporary.IsPersistentRosterHero ||
                !BattlefieldLayout.IsPlayerDeploymentCell(temporary.Cell) ||
                temporaryOnly.Outcome != BattleOutcome.PlayerDefeat)
                throw new InvalidOperationException(
                    "temporary occupancy consumed persistent identity or kept the defeated roster alive");
        }
    }

    private static BattleConfig PopulationOutcomeConfig(params BattleSpawn[] players) => new()
    {
        Seed = 0x4018UL,
        Identity = new BattleIdentity("population-contract", TowerNodeType.Combat, 0x4018UL, 0, 0),
        FloorRule = new ClearFloorRuleRuntime("population", "population", "test"),
        HeroRule = ProductionRule(),
        Spawns =
        [
            .. players,
            new BattleSpawn(ProductionUnit("enemy", false, 1000, 100, 10), 1,
                new Vector2I(1, 0), "a_enemy")
        ]
    };

    private sealed class ThrowAfterDisposeHandle(IDisposable inner) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            inner.Dispose();
            throw new InvalidOperationException("expected non-throwing cleanup wrapper probe");
        }
    }

    private static void VerifyMatureStatusBehavior()
    {
        var frostAuthored = GD.Load<StatusDefinition>(
            "res://tests/fixtures/phase2/statuses/status_phase2_frost.tres") ??
            throw new InvalidOperationException("phase2 Frost fixture is missing");
        var freezeAuthored = GD.Load<StatusDefinition>(
            "res://tests/fixtures/phase2/statuses/status_phase2_freeze.tres") ??
            throw new InvalidOperationException("phase2 Freeze fixture is missing");
        var graphCompilation = StatusDefinitionCompiler.CompileBatch([frostAuthored, freezeAuthored]);
        if (graphCompilation.Report.HasCoreErrors || graphCompilation.Definitions.Length != 2)
            throw new InvalidOperationException("phase2 Status graph: " +
                                                string.Join(" | ", graphCompilation.Report.CoreErrors));
        var frost = graphCompilation.Definitions.Single(status => status.StableId == frostAuthored.StableId);
        var freeze = graphCompilation.Definitions.Single(status => status.StableId == freezeAuthored.StableId);
        if (frost.OverflowTransition is null ||
            !ReferenceEquals(frost.OverflowTransition.Target, freeze) ||
            frost.ResourcePath != frostAuthored.ResourcePath || freeze.ResourcePath != freezeAuthored.ResourcePath)
            throw new InvalidOperationException("Status dependency graph was not canonical or path-attributed");

        var frostAbility = AuthoredStatusAbility("phase2_frost_root", frostAuthored);
        var frostLoadout = new AbilityLoadoutDefinition { Abilities = [frostAbility] };
        var reachability = ContentValidator.ValidateAbilityStatusAuthoredGraph(new AbilityStatusAuthoredGraph(
            [frostLoadout], [frostAbility], [frostAuthored, freezeAuthored], [frostLoadout],
            new HashSet<string>(StringComparer.Ordinal)));
        if (reachability.HasCoreErrors)
            throw new InvalidOperationException("Status dependency reachability: " +
                                                string.Join(" | ", reachability.CoreErrors));
        VerifyStatusDependencyRejection();

        var attributeDefinition = AttributeDefinitionCompiler.Legacy(new Dictionary<CombatAttribute, float>
        {
            [CombatAttribute.AttackDamage] = 10,
            [CombatAttribute.ControlResistance] = .5f
        });
        using var attributes = new BattleAttributeScope("phase2_status_attributes");
        var ownerAttributes = attributes.CreateSet("owner", attributeDefinition);
        var sourceAAttributes = attributes.CreateSet("source_a", attributeDefinition);
        var sourceBAttributes = attributes.CreateSet("source_b", attributeDefinition);
        var lifecycle = new List<StatusLifecycleEvent>();
        var cues = new List<StatusPresentationCue>();
        var effects = new List<StatusEffectInvocation>();
        using var scope = new BattleStatusScope(
            "phase2_mature_status",
            (_, _) => { },
            id => id switch
            {
                "owner" => ownerAttributes,
                "source_a" => sourceAAttributes,
                "source_b" => sourceBAttributes,
                _ => null
            },
            invocation =>
            {
                effects.Add(invocation);
                return true;
            },
            lifecycle.Add,
            cues.Add);

        var firstFrost = scope.Apply(frost, "source_a", "owner", 0).Status!;
        scope.Apply(frost, "source_b", "owner", 0);
        var shared = scope.SnapshotOwner("owner").Single();
        if (firstFrost.Stacks != 1 || shared.StableId != frost.StableId || shared.Stacks != 2 ||
            shared.SourceContributions.Length != 2 ||
            !shared.SourceContributions.Select(item => item.SourceId).SequenceEqual(["source_a", "source_b"]))
            throw new InvalidOperationException("shared-target multi-source aggregation or immutable attribution changed");
        var overflow = scope.Apply(frost, "source_a", "owner", 0);
        var frozen = scope.SnapshotOwner("owner").Single();
        if (!overflow.OverflowTriggered || frozen.StableId != freeze.StableId || frozen.RemainingTicks != 3 ||
            !scope.HasTag("owner", StatusDefinitionCompiler.ActionDisabledTag) ||
            !lifecycle.Any(item => item.Status.StableId == frost.StableId &&
                                   item.RemovalReason == StatusRemovalReason.OverflowConsumed) ||
            !cues.Any(item => item.Status.StableId == frost.StableId &&
                              item.Lifecycle == StatusPresentationCueLifecycle.Removed) ||
            !cues.Any(item => item.Status.StableId == freeze.StableId &&
                               item.Lifecycle == StatusPresentationCueLifecycle.OnActive))
            throw new InvalidOperationException("Frost threshold, Freeze resistance/tag, or cue lifecycle changed");
        var overflowRemoval = lifecycle.Last(item => item.Status.StableId == frost.StableId &&
                                                       item.RemovalReason == StatusRemovalReason.OverflowConsumed);
        if (overflowRemoval.Status.Stacks != 3 ||
            !overflowRemoval.Status.SourceContributions.Select(item => (item.SourceId, item.Stacks))
                .SequenceEqual([("source_a", 2), ("source_b", 1)]))
            throw new InvalidOperationException("overflow removal lost its pre-consume source contributions");
        if (scope.Dispel("owner", freeze.StableId, StatusDispelStrength.Ordinary) ||
            !scope.Dispel("owner", freeze.StableId, StatusDispelStrength.Strong) ||
            scope.HasTag("owner", StatusDefinitionCompiler.ActionDisabledTag))
            throw new InvalidOperationException("ordinary/strong dispel category or typed tag rollback changed");

        var sourcedDamage = CompileStatus(new StatusDefinition
        {
            StableId = "phase2_sourced_damage",
            DisplayName = "来源增伤",
            Behavior = StatusBehaviorKind.DamageMultiplier,
            DurationKind = StatusDurationKind.TimedTicks,
            DurationTicks = 4,
            AggregationPolicy = StatusAggregationPolicy.ByTarget,
            StackLimit = 3,
            DurationRefreshPolicy = StatusDurationRefreshPolicy.Reset,
            DispelCategory = StatusDispelCategory.Ordinary,
            AttributeModifiers = [DamageModifierSpec(1.25f)],
            Magnitude = 1.25f
        });
        scope.Apply(sourcedDamage, "source_a", "owner", 1);
        scope.Apply(sourcedDamage, "source_b", "owner", 1);
        Near(ownerAttributes.GetValue(CombatAttribute.AttackDamage), 15.625f,
            "shared-target sourced Attribute stacks");
        if (!scope.Dispel("owner", sourcedDamage.StableId, StatusDispelStrength.Ordinary, "source_a"))
            throw new InvalidOperationException("source-specific shared-target dispel failed");
        Near(ownerAttributes.GetValue(CombatAttribute.AttackDamage), 12.5f,
            "source-specific Attribute rollback");
        var remainingDamage = scope.SnapshotOwner("owner").Single(item => item.StableId == sourcedDamage.StableId);
        if (remainingDamage.Stacks != 1 || remainingDamage.SourceContributions.Single().SourceId != "source_b")
            throw new InvalidOperationException("source-specific dispel lost remaining attribution");
        scope.Dispel("owner", sourcedDamage.StableId, StatusDispelStrength.Ordinary, "source_b");
        var sourcedRemoval = lifecycle.Last(item => item.Status.StableId == sourcedDamage.StableId &&
                                                      item.Kind == StatusLifecycleKind.Removed);
        if (sourcedRemoval.Status.SourceId != "source_b" ||
            sourcedRemoval.Status.SourceContributions is not [{ SourceId: "source_b", Stacks: 1 }])
            throw new InvalidOperationException("final source-specific dispel lost removal attribution");
        Near(ownerAttributes.GetValue(CombatAttribute.AttackDamage), 10, "complete Attribute rollback");

        var bySource = CompileStatus(PlainStatus(
            "phase2_by_source", StatusDurationKind.Permanent, 0,
            StatusAggregationPolicy.BySource, stackLimit: 2));
        scope.Apply(bySource, "source_a", "owner", 2);
        scope.Apply(bySource, "source_b", "owner", 2);
        if (scope.SnapshotOwner("owner").Count(item => item.StableId == bySource.StableId) != 2)
            throw new InvalidOperationException("by-source aggregation did not isolate sources");

        var independent = CompileStatus(PlainStatus(
            "phase2_independent", StatusDurationKind.TimedTicks, 2,
            StatusAggregationPolicy.Independent, stackLimit: 1));
        scope.Apply(independent, "source_a", "owner", 2);
        scope.AdvanceOwner("owner", 3);
        scope.Apply(independent, "source_a", "owner", 3);
        var independentTimers = scope.SnapshotOwner("owner")
            .Where(item => item.StableId == independent.StableId)
            .Select(item => item.RemainingTicks).Order().ToArray();
        if (!independentTimers.SequenceEqual([1, 2]))
            throw new InvalidOperationException("independent Status instances did not retain separate timers");
        scope.AdvanceOwner("owner", 4);
        if (scope.SnapshotOwner("owner").Count(item => item.StableId == independent.StableId) != 1)
            throw new InvalidOperationException("independent Status expiry removed the wrong instance");

        var periodicBinding = ManualEffectBinding("phase2_periodic_binding");
        var periodic = CompileStatus(new StatusDefinition
        {
            StableId = "phase2_periodic",
            DisplayName = "周期状态",
            Behavior = StatusBehaviorKind.None,
            DurationKind = StatusDurationKind.TimedTicks,
            DurationTicks = 3,
            AggregationPolicy = StatusAggregationPolicy.BySource,
            StackLimit = 1,
            DurationRefreshPolicy = StatusDurationRefreshPolicy.Extend,
            PeriodicResetPolicy = StatusPeriodicResetPolicy.ResetOnApplication,
            PeriodicIntervalTicks = 2,
            PeriodicEffect = periodicBinding,
            Presentation = CuePresentation("periodic")
        });
        scope.Apply(periodic, "source_a", "owner", 5);
        scope.AdvanceOwner("owner", 6);
        scope.Apply(periodic, "source_a", "owner", 6);
        scope.AdvanceOwner("owner", 7);
        var periodicAdvance = scope.AdvanceOwner("owner", 8);
        if (periodicAdvance.PeriodicInvocations != 1 ||
            effects.Count(item => item.Kind == StatusEffectInvocationKind.Periodic &&
                                  item.Definition.StableId == periodic.StableId) != 1 ||
            !cues.Any(item => item.Status.StableId == periodic.StableId &&
                              item.Lifecycle == StatusPresentationCueLifecycle.WhileActive))
            throw new InvalidOperationException("duration extension, periodic reset/execution, or WhileActive cue changed");

        var instantBinding = ManualEffectBinding("phase2_instant_binding");
        var instant = CompileStatus(new StatusDefinition
        {
            StableId = "phase2_instant",
            DisplayName = "瞬发状态",
            Behavior = StatusBehaviorKind.None,
            DurationKind = StatusDurationKind.Instant,
            AggregationPolicy = StatusAggregationPolicy.Independent,
            StackLimit = 1,
            LifecycleBindings =
            [
                new StatusLifecycleBindingSpec
                {
                    Trigger = StatusLifecycleTriggerKind.Applied,
                    Binding = instantBinding
                }
            ],
            Presentation = CuePresentation("instant")
        });
        var instantResult = scope.Apply(instant, "source_a", "owner", 9);
        if (!instantResult.Applied || scope.SnapshotOwner("owner").Any(item => item.StableId == instant.StableId) ||
            !effects.Any(item => item.Kind == StatusEffectInvocationKind.Applied &&
                                 item.Definition.StableId == instant.StableId) ||
            !lifecycle.Any(item => item.Status.StableId == instant.StableId &&
                                   item.RemovalReason == StatusRemovalReason.InstantExecuted) ||
            !cues.Any(item => item.Status.StableId == instant.StableId &&
                              item.Lifecycle == StatusPresentationCueLifecycle.Executed))
            throw new InvalidOperationException("instant/lifecycle binding/Executed cue behavior changed");

        var deathRemove = CompileStatus(PlainStatus(
            "phase2_death_remove", StatusDurationKind.Permanent, 0,
            StatusAggregationPolicy.BySource, deathPolicy: StatusDeathPolicy.Remove));
        var deathPersist = CompileStatus(PlainStatus(
            "phase2_death_persist", StatusDurationKind.Permanent, 0,
            StatusAggregationPolicy.BySource, deathPolicy: StatusDeathPolicy.Persist));
        var nonDispellable = CompileStatus(PlainStatus(
            "phase2_non_dispellable", StatusDurationKind.Permanent, 0,
            StatusAggregationPolicy.BySource, dispel: StatusDispelCategory.NonDispellable));
        scope.Apply(deathRemove, "source_a", "owner", 10);
        scope.Apply(deathPersist, "source_a", "owner", 10);
        scope.Apply(nonDispellable, "source_a", "owner", 10);
        if (scope.Dispel("owner", nonDispellable.StableId, StatusDispelStrength.Strong))
            throw new InvalidOperationException("non-dispellable Status was removed by strong dispel");
        scope.HandleOwnerDeath("owner");
        var afterDeath = scope.SnapshotOwner("owner");
        if (afterDeath.Any(item => item.StableId == deathRemove.StableId) ||
            afterDeath.All(item => item.StableId != deathPersist.StableId) ||
            !lifecycle.Any(item => item.Status.StableId == deathRemove.StableId &&
                                   item.RemovalReason == StatusRemovalReason.OwnerDied))
            throw new InvalidOperationException("Status death policy or typed removal reason changed");
        scope.RemoveOwner("owner");
        if (scope.SnapshotOwner("owner").Length != 0 || scope.LiveModifierHandleCount != 0)
            throw new InvalidOperationException("forced owner cleanup retained Status state");

        VerifyStatusPolicyBranches(attributeDefinition);
        VerifyStatusProjectionIdentityAndMagnitudes(attributeDefinition);
        VerifyStatusReactiveBindings();
        VerifyStatusDispositionPurge();
        VerifyStatusMutationAtomicity(attributeDefinition);
        VerifyStatusReactionFailureCleanup();
        scope.Complete(StatusScopeCompletionReason.BattleCompleted, 11);
        if (scope.Transition is not { RemainingInstances: 0, RemainingModifierHandles: 0, RemainingContributions: 0 })
            throw new InvalidOperationException("Status completion retained Battle-owned state");
    }

    private static void VerifyStatusPolicyBranches(CompiledAttributeSetDefinition attributeDefinition)
    {
        var periodicEffects = new List<StatusEffectInvocation>();
        using var attributes = new BattleAttributeScope("phase2_policy_attributes");
        var keepLongerAttributes = attributes.CreateSet("keep_longer_owner", attributeDefinition);
        keepLongerAttributes.SetBaseValue(CombatAttribute.ControlResistance, 0);
        using var scope = new BattleStatusScope(
            "phase2_policy_branches",
            (_, _) => { },
            id => id == "keep_longer_owner" ? keepLongerAttributes : null,
            invocation =>
            {
                periodicEffects.Add(invocation);
                return true;
            },
            null,
            null);

        CompiledStatusDefinition Timed(
            string id,
            StatusDurationRefreshPolicy refresh,
            StatusOverflowPolicy overflow = StatusOverflowPolicy.RejectNewStacks,
            int stackLimit = 2)
        {
            var authored = PlainStatus(
                id, StatusDurationKind.TimedTicks, 3,
                StatusAggregationPolicy.BySource, stackLimit);
            authored.DurationRefreshPolicy = refresh;
            authored.OverflowPolicy = overflow;
            return CompileStatus(authored);
        }

        var none = Timed("phase2_refresh_none", StatusDurationRefreshPolicy.None);
        scope.Apply(none, "source", "none_owner", 0);
        scope.AdvanceOwner("none_owner", 1);
        scope.Apply(none, "source", "none_owner", 1);
        var noneSnapshot = scope.SnapshotOwner("none_owner").Single();
        if (noneSnapshot.Stacks != 2 || noneSnapshot.RemainingTicks != 2)
            throw new InvalidOperationException("duration refresh None did not preserve remaining time");

        var reset = Timed("phase2_refresh_reset", StatusDurationRefreshPolicy.Reset);
        scope.Apply(reset, "source", "reset_owner", 0);
        scope.AdvanceOwner("reset_owner", 1);
        scope.Apply(reset, "source", "reset_owner", 1);
        if (scope.SnapshotOwner("reset_owner").Single().RemainingTicks != 3)
            throw new InvalidOperationException("duration refresh Reset did not restore authored duration");

        var extend = Timed("phase2_refresh_extend", StatusDurationRefreshPolicy.Extend);
        scope.Apply(extend, "source", "extend_owner", 0);
        scope.AdvanceOwner("extend_owner", 1);
        scope.Apply(extend, "source", "extend_owner", 1);
        if (scope.SnapshotOwner("extend_owner").Single().RemainingTicks != 5)
            throw new InvalidOperationException("duration refresh Extend did not add authored duration");

        var keepLongerAuthored = PlainStatus(
            "phase2_refresh_keep_longer", StatusDurationKind.TimedTicks, 6,
            StatusAggregationPolicy.BySource, stackLimit: 2);
        keepLongerAuthored.DurationRefreshPolicy = StatusDurationRefreshPolicy.KeepLonger;
        keepLongerAuthored.Behavior = StatusBehaviorKind.DisableActions;
        keepLongerAuthored.GrantedTags = [StatusDefinitionCompiler.ActionDisabledTag];
        keepLongerAuthored.ControlDurationRule = StatusControlDurationRule.LinearResistanceCeiling;
        var keepLonger = CompileStatus(keepLongerAuthored);
        scope.Apply(keepLonger, "source", "keep_longer_owner", 0);
        scope.AdvanceOwner("keep_longer_owner", 1);
        keepLongerAttributes.SetBaseValue(CombatAttribute.ControlResistance, .5f);
        scope.Apply(keepLonger, "source", "keep_longer_owner", 1);
        if (scope.SnapshotOwner("keep_longer_owner").Single().RemainingTicks != 5)
            throw new InvalidOperationException("duration refresh KeepLonger replaced a longer remaining duration");

        var reject = Timed(
            "phase2_overflow_reject", StatusDurationRefreshPolicy.None,
            StatusOverflowPolicy.RejectNewStacks, stackLimit: 1);
        scope.Apply(reject, "source", "reject_owner", 0);
        scope.AdvanceOwner("reject_owner", 1);
        var rejected = scope.Apply(reject, "source", "reject_owner", 1);
        var rejectSnapshot = scope.SnapshotOwner("reject_owner").Single();
        if (rejected.AddedStacks != 0 || rejectSnapshot.Stacks != 1 || rejectSnapshot.RemainingTicks != 2)
            throw new InvalidOperationException("overflow RejectNewStacks changed stacks or duration");

        var refreshOverflow = Timed(
            "phase2_overflow_refresh", StatusDurationRefreshPolicy.None,
            StatusOverflowPolicy.RefreshDuration, stackLimit: 1);
        scope.Apply(refreshOverflow, "source", "refresh_overflow_owner", 0);
        scope.AdvanceOwner("refresh_overflow_owner", 1);
        var refreshed = scope.Apply(refreshOverflow, "source", "refresh_overflow_owner", 1);
        var refreshOverflowSnapshot = scope.SnapshotOwner("refresh_overflow_owner").Single();
        if (refreshed.AddedStacks != 0 || refreshOverflowSnapshot.Stacks != 1 ||
            refreshOverflowSnapshot.RemainingTicks != 3)
            throw new InvalidOperationException("overflow RefreshDuration did not refresh at the stack limit");

        CompiledStatusDefinition Periodic(string id, StatusPeriodicResetPolicy policy)
        {
            var authored = PlainStatus(
                id, StatusDurationKind.TimedTicks, 6,
                StatusAggregationPolicy.BySource, stackLimit: 2);
            authored.DurationRefreshPolicy = StatusDurationRefreshPolicy.Reset;
            authored.PeriodicIntervalTicks = 3;
            authored.PeriodicResetPolicy = policy;
            authored.PeriodicEffect = ManualEffectBinding(id + "_effect");
            return CompileStatus(authored);
        }

        var keepSchedule = Periodic("phase2_periodic_keep", StatusPeriodicResetPolicy.KeepSchedule);
        scope.Apply(keepSchedule, "source", "keep_schedule_owner", 0);
        scope.AdvanceOwner("keep_schedule_owner", 1);
        scope.Apply(keepSchedule, "source", "keep_schedule_owner", 1);
        scope.AdvanceOwner("keep_schedule_owner", 2);
        var keepDue = scope.AdvanceOwner("keep_schedule_owner", 3);
        if (keepDue.PeriodicInvocations != 1 || periodicEffects.Count(item =>
                item.Definition.StableId == keepSchedule.StableId && item.Tick == 3) != 1)
            throw new InvalidOperationException("periodic KeepSchedule moved the original due tick");

        var resetSchedule = Periodic("phase2_periodic_reset", StatusPeriodicResetPolicy.ResetOnApplication);
        scope.Apply(resetSchedule, "source", "reset_schedule_owner", 0);
        scope.AdvanceOwner("reset_schedule_owner", 1);
        scope.Apply(resetSchedule, "source", "reset_schedule_owner", 1);
        scope.AdvanceOwner("reset_schedule_owner", 2);
        var resetNotDue = scope.AdvanceOwner("reset_schedule_owner", 3);
        var resetDue = scope.AdvanceOwner("reset_schedule_owner", 4);
        if (resetNotDue.PeriodicInvocations != 0 || resetDue.PeriodicInvocations != 1 ||
            periodicEffects.Count(item => item.Definition.StableId == resetSchedule.StableId && item.Tick == 4) != 1)
            throw new InvalidOperationException("periodic ResetOnApplication did not move the due tick");
    }

    private static void VerifyStatusProjectionIdentityAndMagnitudes(
        CompiledAttributeSetDefinition attributeDefinition)
    {
        using (var attributes = new BattleAttributeScope("phase2_snapshot_projection_attributes"))
        {
            var owner = attributes.CreateSet("owner", attributeDefinition);
            var source = attributes.CreateSet("source", attributeDefinition);
            var failApplication = false;
            var lifecycle = new List<StatusLifecycleEvent>();
            using var scope = new BattleStatusScope(
                "phase2_snapshot_projection",
                (_, _) => { },
                id => id switch
                {
                    "owner" => owner,
                    "source" => source,
                    _ => null
                },
                null,
                item =>
                {
                    lifecycle.Add(item);
                    if (failApplication && item.Kind == StatusLifecycleKind.Applied)
                        throw new InvalidOperationException("expected snapshot projection sink failure");
                },
                null);
            var snapshotStatus = CompileStatus(new StatusDefinition
            {
                StableId = "phase2_stack_snapshot_magnitude",
                DisplayName = "逐层快照",
                Behavior = StatusBehaviorKind.None,
                DurationKind = StatusDurationKind.Permanent,
                AggregationPolicy = StatusAggregationPolicy.ByTarget,
                StackLimit = 2,
                DispelCategory = StatusDispelCategory.Ordinary,
                AttributeModifiers =
                [
                    new AttributeModifierSpec
                    {
                        Attribute = CombatAttribute.AttackDamage,
                        Operation = AttributeModifierOperation.Add,
                        Magnitude = new SourceAttributeMagnitudeSpec
                        {
                            Attribute = CombatAttribute.AttackDamage,
                            CaptureMode = AttributeCaptureMode.Snapshot
                        },
                        SlotId = "snapshot_source_damage"
                    }
                ]
            });

            scope.Apply(snapshotStatus, "source", "owner", 0);
            Near(owner.GetValue(CombatAttribute.AttackDamage), 20, "first stack snapshot magnitude");
            var firstProjection = scope.SnapshotModifierProjections("owner").Single();
            var firstSnapshot = scope.SnapshotOwner("owner").Single();
            if (firstSnapshot.CapturedMagnitudes.Length != 1 ||
                Math.Abs(firstSnapshot.CapturedMagnitudes[0].Value - 10) > .0001f)
                throw new InvalidOperationException("first Status stack did not expose its immutable capture");

            source.SetBaseValue(CombatAttribute.AttackDamage, 20);
            failApplication = true;
            ExpectThrows(() => scope.Apply(snapshotStatus, "source", "owner", 1),
                "snapshot stack sink rollback");
            failApplication = false;
            Near(owner.GetValue(CombatAttribute.AttackDamage), 20,
                "failed new stack retained original snapshot magnitude");
            var afterFailure = scope.SnapshotModifierProjections("owner").Single();
            if (afterFailure.Handle != firstProjection.Handle ||
                scope.SnapshotOwner("owner").Single().CapturedMagnitudes.Single().Value != 10)
                throw new InvalidOperationException("failed stack application replaced the original projection handle");

            scope.Apply(snapshotStatus, "source", "owner", 1);
            Near(owner.GetValue(CombatAttribute.AttackDamage), 40,
                "old/new Status stacks retain independent snapshot magnitudes");
            var captures = scope.SnapshotOwner("owner").Single().CapturedMagnitudes
                .OrderBy(item => item.StackApplicationSequence).Select(item => item.Value).ToArray();
            var projections = scope.SnapshotModifierProjections("owner");
            if (!captures.SequenceEqual([10f, 20f]) || projections.Length != 2 ||
                projections[0].Handle != firstProjection.Handle ||
                projections[1].Handle.Sequence != firstProjection.Handle.Sequence + 1)
                throw new InvalidOperationException("stack capture or incremental projection identity changed");

            if (!scope.Dispel("owner", snapshotStatus.StableId, StatusDispelStrength.Ordinary, "source"))
                throw new InvalidOperationException("snapshot Status source-specific removal failed");
            Near(owner.GetValue(CombatAttribute.AttackDamage), 10, "snapshot Status removal rollback");
            var removal = lifecycle.Last(item => item.Status.StableId == snapshotStatus.StableId &&
                                                   item.Kind == StatusLifecycleKind.Removed);
            if (removal.Status.Stacks != 2 ||
                removal.Status.SourceContributions is not [{ SourceId: "source", Stacks: 2 }] ||
                !removal.Status.CapturedMagnitudes.Select(item => item.Value).SequenceEqual([10f, 20f]))
                throw new InvalidOperationException("source-specific removal lost its pre-removal attribution snapshot");

            source.SetBaseValue(CombatAttribute.AttackDamage, 10);
            var liveStatus = CompileStatus(new StatusDefinition
            {
                StableId = "phase2_stack_live_magnitude",
                DisplayName = "实时来源",
                Behavior = StatusBehaviorKind.None,
                DurationKind = StatusDurationKind.Permanent,
                AggregationPolicy = StatusAggregationPolicy.BySource,
                StackLimit = 1,
                AttributeModifiers =
                [
                    new AttributeModifierSpec
                    {
                        Attribute = CombatAttribute.AttackDamage,
                        Operation = AttributeModifierOperation.Add,
                        Magnitude = new SourceAttributeMagnitudeSpec
                        {
                            Attribute = CombatAttribute.AttackDamage,
                            CaptureMode = AttributeCaptureMode.Live
                        },
                        SlotId = "live_source_damage"
                    }
                ]
            });
            scope.Apply(liveStatus, "source", "owner", 2);
            Near(owner.GetValue(CombatAttribute.AttackDamage), 20, "initial live Status magnitude");
            source.SetBaseValue(CombatAttribute.AttackDamage, 30);
            Near(owner.GetValue(CombatAttribute.AttackDamage), 40, "live Status magnitude context");
            if (!scope.SnapshotOwner("owner").Single().CapturedMagnitudes.IsEmpty)
                throw new InvalidOperationException("live Status magnitude was incorrectly captured");
        }

        using (var attributes = new BattleAttributeScope("phase2_typed_context_attributes"))
        {
            var owner = attributes.CreateSet("context_owner", attributeDefinition);
            var invocationValue = 2f;
            var teamValue = 3f;
            var traitValue = 4f;
            using var scope = new BattleStatusScope(
                "phase2_typed_context",
                (_, _) => { },
                id => id == "context_owner" ? owner : null,
                null,
                null,
                null,
                request => new BattleAttributeMagnitudeContext(
                    request.SourceAttributes,
                    request.TargetAttributes,
                    key => key == "status_power" ? invocationValue : 0,
                    (kind, team) => kind == AttributeTeamCountKind.Alive && team == 0 ? teamValue : 0,
                    (trait, team) => trait == "phase2_trait" && team == 0 ? traitValue : 0));
            var typed = CompileStatus(new StatusDefinition
            {
                StableId = "phase2_typed_status_magnitudes",
                DisplayName = "状态类型化数值",
                Behavior = StatusBehaviorKind.None,
                DurationKind = StatusDurationKind.Permanent,
                AggregationPolicy = StatusAggregationPolicy.BySource,
                StackLimit = 1,
                AttributeModifiers =
                [
                    new AttributeModifierSpec
                    {
                        Attribute = CombatAttribute.AttackDamage,
                        Operation = AttributeModifierOperation.Add,
                        Magnitude = new ContextAttributeMagnitudeSpec
                            { Key = "status_power", CaptureMode = AttributeCaptureMode.Live },
                        SlotId = "context_value"
                    },
                    new AttributeModifierSpec
                    {
                        Attribute = CombatAttribute.AttackDamage,
                        Operation = AttributeModifierOperation.Add,
                        Magnitude = new TeamCountAttributeMagnitudeSpec
                        {
                            CountKind = AttributeTeamCountKind.Alive,
                            Team = 0,
                            CaptureMode = AttributeCaptureMode.Live
                        },
                        SlotId = "team_count"
                    },
                    new AttributeModifierSpec
                    {
                        Attribute = CombatAttribute.AttackDamage,
                        Operation = AttributeModifierOperation.Add,
                        Magnitude = new TraitValueAttributeMagnitudeSpec
                        {
                            TraitId = "phase2_trait",
                            Team = 0,
                            CaptureMode = AttributeCaptureMode.Live
                        },
                        SlotId = "trait_value"
                    }
                ]
            });
            scope.Apply(typed, "system", "context_owner", 0);
            Near(owner.GetValue(CombatAttribute.AttackDamage), 19,
                "Status invocation/team/trait magnitude context");
            invocationValue = 3;
            teamValue = 4;
            traitValue = 5;
            Near(owner.GetValue(CombatAttribute.AttackDamage), 22,
                "live Status invocation/team/trait magnitude context");
        }

        using (var attributes = new BattleAttributeScope("phase2_override_projection_attributes"))
        {
            var owner = attributes.CreateSet("owner", attributeDefinition);
            var failRemoval = false;
            using var scope = new BattleStatusScope(
                "phase2_override_projection",
                (_, _) => { },
                id => id == "owner" ? owner : null,
                null,
                item =>
                {
                    if (failRemoval && item.Kind == StatusLifecycleKind.Removed)
                        throw new InvalidOperationException("expected Override removal sink failure");
                },
                null);

            CompiledStatusDefinition Override(string id, float value)
            {
                var authored = PlainStatus(
                    id, StatusDurationKind.TimedTicks, 5,
                    StatusAggregationPolicy.BySource, stackLimit: 1);
                authored.DurationRefreshPolicy = StatusDurationRefreshPolicy.Reset;
                authored.AttributeModifiers =
                [
                    new AttributeModifierSpec
                    {
                        Attribute = CombatAttribute.AttackDamage,
                        Operation = AttributeModifierOperation.Override,
                        Magnitude = new ConstantAttributeMagnitudeSpec { Value = value },
                        SlotId = "override_damage"
                    }
                ];
                return CompileStatus(authored);
            }

            var first = Override("phase2_override_first", 20);
            var winner = Override("phase2_override_winner", 30);
            scope.Apply(first, "source_a", "owner", 0);
            scope.Apply(winner, "source_b", "owner", 0);
            Near(owner.GetValue(CombatAttribute.AttackDamage), 30, "initial Status Override winner");
            var originalHandles = scope.SnapshotModifierProjections("owner").Select(item => item.Handle).ToArray();
            scope.AdvanceOwner("owner", 1);
            scope.Apply(first, "source_a", "owner", 1);
            var refreshedHandles = scope.SnapshotModifierProjections("owner").Select(item => item.Handle).ToArray();
            if (!refreshedHandles.SequenceEqual(originalHandles))
                throw new InvalidOperationException("duration refresh replaced stable Status modifier handles");
            Near(owner.GetValue(CombatAttribute.AttackDamage), 30,
                "duration refresh preserved Status Override application order");

            failRemoval = true;
            ExpectThrows(() => scope.Dispel("owner", winner.StableId, StatusDispelStrength.Ordinary),
                "Override winner removal rollback");
            failRemoval = false;
            var restoredHandles = scope.SnapshotModifierProjections("owner").Select(item => item.Handle).ToArray();
            if (!restoredHandles.SequenceEqual(originalHandles))
                throw new InvalidOperationException("failed Status removal did not restore the original handles");
            Near(owner.GetValue(CombatAttribute.AttackDamage), 30,
                "failed Status removal preserved Override winner");

            var shared = CompileStatus(new StatusDefinition
            {
                StableId = "phase2_incremental_source_removal",
                DisplayName = "来源增量移除",
                Behavior = StatusBehaviorKind.None,
                DurationKind = StatusDurationKind.Permanent,
                AggregationPolicy = StatusAggregationPolicy.ByTarget,
                StackLimit = 2,
                DispelCategory = StatusDispelCategory.Ordinary,
                AttributeModifiers =
                [
                    new AttributeModifierSpec
                    {
                        Attribute = CombatAttribute.AttackDamage,
                        Operation = AttributeModifierOperation.Add,
                        Magnitude = new ConstantAttributeMagnitudeSpec { Value = 5 },
                        SlotId = "shared_add"
                    }
                ]
            });
            scope.Apply(shared, "source_a", "owner", 2);
            scope.Apply(shared, "source_b", "owner", 2);
            var beforeRemoval = scope.SnapshotModifierProjections("owner")
                .Where(item => item.StatusInstanceId.Contains(shared.StableId, StringComparison.Ordinal)).ToArray();
            var retained = beforeRemoval.Single(item => item.SourceId == "source_b");
            scope.Dispel("owner", shared.StableId, StatusDispelStrength.Ordinary, "source_a");
            var afterRemoval = scope.SnapshotModifierProjections("owner")
                .Where(item => item.StatusInstanceId.Contains(shared.StableId, StringComparison.Ordinal)).ToArray();
            if (afterRemoval is not [{ SourceId: "source_b" }] || afterRemoval[0].Handle != retained.Handle)
                throw new InvalidOperationException("source-specific removal rebuilt an unaffected projection");
        }
    }

    private static void VerifyStatusReactiveBindings()
    {
        var attackEffect = ManualEffectBinding("phase2_reactive_attack_effect");
        var defeatEffect = ManualEffectBinding("phase2_reactive_defeat_effect");
        var authored = PlainStatus(
            "phase2_combat_reactive", StatusDurationKind.Permanent, 0,
            StatusAggregationPolicy.ByTarget, stackLimit: 2);
        authored.Disposition = StatusDisposition.Helpful;
        authored.CombatReactiveBindings =
        [
            new StatusCombatReactiveBindingSpec
            {
                EventKind = BattleCombatEventKind.AttackLanded,
                OwnerRole = StatusReactiveOwnerRole.OwnerIsSource,
                EffectSourcePolicy = StatusReactiveEffectSourcePolicy.PrimaryContribution,
                Priority = 2,
                Binding = attackEffect
            },
            new StatusCombatReactiveBindingSpec
            {
                EventKind = BattleCombatEventKind.UnitDefeated,
                OwnerRole = StatusReactiveOwnerRole.OwnerIsTarget,
                EffectSourcePolicy = StatusReactiveEffectSourcePolicy.PrimaryContribution,
                Priority = 3,
                Binding = defeatEffect
            }
        ];
        var reactive = CompileStatus(authored);
        var invocations = new List<StatusEffectInvocation>();
        using var pipeline = new BattleCombatEventPipeline("phase2_status_reactive_pipeline");
        using var scope = new BattleStatusScope(
            "phase2_status_reactive",
            (_, _) => { },
            _ => null,
            null,
            null,
            null,
            combatReactiveRegistrar: request => pipeline.Subscribe(
                request.EventKind, request.Source, request.Priority, request.Listener),
            reactiveEffectSink: invocation =>
            {
                invocations.Add(invocation);
                return true;
            });

        scope.Apply(reactive, "source_a", "owner", 0);
        scope.Apply(reactive, "source_b", "owner", 0);
        if (scope.LiveReactiveSubscriptionCount != 2 || pipeline.SubscriptionCount != 2)
            throw new InvalidOperationException("ByTarget multi-source Status duplicated reactive subscriptions");
        pipeline.Publish(new BattleCombatEventDraft(
            BattleCombatEventKind.AttackLanded,
            CombatSourceRef.Unit("other", "other", "other"),
            "other",
            "enemy",
            1,
            EffectiveValue: 7));
        if (invocations.Count != 0)
            throw new InvalidOperationException("owner-source reactive filter accepted another unit's attack");
        pipeline.Publish(new BattleCombatEventDraft(
            BattleCombatEventKind.AttackLanded,
            CombatSourceRef.Unit("owner", "owner", "owner"),
            "owner",
            "enemy",
            1,
            EffectiveValue: 7));
        var attack = invocations.Single();
        if (attack.Kind != StatusEffectInvocationKind.Reactive || attack.SourceId != "source_a" ||
            attack.ExplicitTargetId != "enemy" ||
            attack.CombatEvent is not
            { SourceRuntimeId: "owner", TargetRuntimeId: "enemy", EffectiveValue: 7, Tick: 1 })
            throw new InvalidOperationException("reactive attack invocation lost typed event context or attribution");

        scope.Dispel("owner", reactive.StableId, StatusDispelStrength.Ordinary, "source_a");
        if (scope.LiveReactiveSubscriptionCount != 2 || pipeline.SubscriptionCount != 2)
            throw new InvalidOperationException("partial ByTarget source removal replaced or lost shared subscriptions");
        pipeline.Publish(new BattleCombatEventDraft(
            BattleCombatEventKind.AttackLanded,
            CombatSourceRef.Unit("owner", "owner", "owner"),
            "owner",
            "enemy",
            2,
            EffectiveValue: 9));
        if (invocations.Last().SourceId != "source_b")
            throw new InvalidOperationException("shared reactive Status did not move attribution to its remaining source");
        pipeline.Publish(new BattleCombatEventDraft(
            BattleCombatEventKind.UnitDefeated,
            CombatSourceRef.Unit("enemy", "enemy", "enemy"),
            "enemy",
            "owner",
            3,
            EffectiveValue: 11));
        if (invocations.Last().Binding.StableId != defeatEffect.StableId ||
            invocations.Last().ExplicitTargetId != "enemy" ||
            invocations.Last().CombatEvent is not { TargetRuntimeId: "owner", Tick: 3 })
            throw new InvalidOperationException("owner-target reactive filter did not receive defeat context");

        VerifyStatusReactiveExplicitTargetProjection();

        scope.Dispel("owner", reactive.StableId, StatusDispelStrength.Ordinary, "source_b");
        if (scope.LiveReactiveSubscriptionCount != 0 || pipeline.SubscriptionCount != 0)
            throw new InvalidOperationException("Status removal retained combat reactive subscriptions");

        var failApply = true;
        using (var failing = new BattleStatusScope(
                   "phase2_status_reactive_failure",
                   (_, _) => { },
                   _ => null,
                   null,
                   lifecycle =>
                   {
                       if (failApply && lifecycle.Kind == StatusLifecycleKind.Applied)
                           throw new InvalidOperationException("expected reactive apply sink failure");
                   },
                   null,
                   combatReactiveRegistrar: request => pipeline.Subscribe(
                       request.EventKind, request.Source, request.Priority, request.Listener),
                   reactiveEffectSink: _ => true))
        {
            ExpectThrows(() => failing.Apply(reactive, "source", "owner", 4),
                "reactive subscription apply rollback");
            if (failing.LiveInstanceCount != 0 || failing.LiveReactiveSubscriptionCount != 0 ||
                pipeline.SubscriptionCount != 0)
                throw new InvalidOperationException("failed Status apply retained a reactive subscription");
            failApply = false;
        }

        using (var throwing = new BattleStatusScope(
                   "phase2_status_reactive_throwing_cleanup",
                   (_, _) => { },
                   _ => null,
                   null,
                   null,
                   null,
                   combatReactiveRegistrar: request => new ThrowAfterDisposeHandle(pipeline.Subscribe(
                       request.EventKind, request.Source, request.Priority, request.Listener)),
                   reactiveEffectSink: _ => true))
        {
            throwing.Apply(reactive, "source", "owner", 5);
            if (!throwing.Dispel("owner", reactive.StableId, StatusDispelStrength.Ordinary))
                throw new InvalidOperationException("throwing cleanup fixture failed to remove Status");
            if (throwing.LiveInstanceCount != 0 || throwing.LiveReactiveSubscriptionCount != 0 ||
                pipeline.SubscriptionCount != 0)
                throw new InvalidOperationException("throwing unsubscribe caused pseudo-rollback or retained subscription");
        }

        var removedEffects = 0;
        var removedFacts = new List<StatusLifecycleEvent>();
        var completionAuthored = PlainStatus(
            "phase2_completion_removed_binding", StatusDurationKind.Permanent, 0,
            StatusAggregationPolicy.BySource);
        completionAuthored.LifecycleBindings =
        [
            new StatusLifecycleBindingSpec
            {
                Trigger = StatusLifecycleTriggerKind.Removed,
                Binding = ManualEffectBinding("phase2_completion_removed_effect")
            }
        ];
        var completionStatus = CompileStatus(completionAuthored);
        using (var completion = new BattleStatusScope(
                   "phase2_completion_removed",
                   (_, _) => { },
                   _ => null,
                   invocation =>
                   {
                       removedEffects++;
                       return true;
                   },
                   removedFacts.Add,
                   null))
        {
            completion.Apply(completionStatus, "source", "owner", 0);
            completion.Complete(StatusScopeCompletionReason.BattleCompleted, 1);
            if (removedEffects != 0 ||
                !removedFacts.Any(item => item.Kind == StatusLifecycleKind.Removed &&
                                          item.RemovalReason == StatusRemovalReason.ScopeCompleted) ||
                completion.Transition is not
                { Reason: StatusScopeCompletionReason.BattleCompleted, RemainingInstances: 0,
                    RemainingReactiveSubscriptions: 0, RemainingModifierHandles: 0 })
                throw new InvalidOperationException(
                    "scope completion executed gameplay removal effects or lost immutable removal facts");
        }

        using (var throwingCompletion = new BattleStatusScope(
                   "phase2_status_reactive_throwing_completion",
                   (_, _) => { },
                   _ => null,
                   null,
                   null,
                   null,
                   combatReactiveRegistrar: request => new ThrowAfterDisposeHandle(pipeline.Subscribe(
                       request.EventKind, request.Source, request.Priority, request.Listener)),
                   reactiveEffectSink: _ => true))
        {
            throwingCompletion.Apply(reactive, "source", "owner", 6);
            ExpectThrows(
                () => throwingCompletion.Complete(StatusScopeCompletionReason.BattleCompleted, 7),
                "throwing reactive completion cleanup");
            if (throwingCompletion.Transition is not
                    { Reason: StatusScopeCompletionReason.Exception, RemainingInstances: 0,
                        RemainingReactiveSubscriptions: 0, RemainingModifierHandles: 0 } ||
                throwingCompletion.LiveInstanceCount != 0 ||
                throwingCompletion.LiveReactiveSubscriptionCount != 0 ||
                pipeline.SubscriptionCount != 0)
                throw new InvalidOperationException(
                    "throwing completion unsubscribe restored Status state or retained subscriptions");
        }

        VerifyStatusNaturalBattleCompletion();
    }

    private static void VerifyStatusReactiveExplicitTargetProjection()
    {
        const string bindingId = "phase2_reactive_counterpart_shield";
        var status = PlainStatus(
            "phase2_reactive_counterpart_status",
            StatusDurationKind.Permanent,
            0,
            StatusAggregationPolicy.BySource);
        status.CombatReactiveBindings =
        [
            new StatusCombatReactiveBindingSpec
            {
                EventKind = BattleCombatEventKind.DamageResolved,
                OwnerRole = StatusReactiveOwnerRole.OwnerIsTarget,
                EffectSourcePolicy = StatusReactiveEffectSourcePolicy.PrimaryContribution,
                Binding = new EffectBindingSpec
                {
                    StableId = bindingId,
                    Trigger = new EffectTriggerSpec { Kind = EffectTriggerKind.Manual },
                    Conditions = [],
                    TargetQuery = new ExplicitTargetQuerySpec(),
                    Effects =
                    [
                        new ShieldEffectSpec
                        {
                            AmountSource = EffectAmountSource.InvocationValue,
                            Amount = 1
                        }
                    ],
                    Limits = new EffectBindingLimitsSpec()
                }
            }
        ];
        var ability = AuthoredStatusAbility("phase2_reactive_counterpart_ability", status);
        var compilation = AbilityDefinitionCompiler.CompileLoadout(
            new AbilityLoadoutDefinition { Abilities = [ability] });
        var loadout = compilation.Loadout ?? throw new InvalidOperationException(
            "reactive counterpart fixture: " + string.Join(" | ", compilation.Report.CoreErrors));
        using var battle = new BattleSimulation(new BattleConfig
        {
            Seed = 8121,
            Identity = new BattleIdentity("phase2_reactive_counterpart", TowerNodeType.Combat, 8121, 1, 1),
            FloorRule = new ClearFloorRuleRuntime("reactive-counterpart", "reactive-counterpart", "test"),
            HeroRule = ProductionRule(),
            TacticalCommands = TacticalPreparation(loadout, ability.StableId),
            Spawns =
            [
                new BattleSpawn(ProductionUnit("z_owner", true, 100, 0, 1), 0,
                    new Vector2I(1, 1), "z_owner"),
                new BattleSpawn(ProductionUnit("a_attacker", false, 100, 7, 1), 1,
                    new Vector2I(2, 1), "a_attacker")
            ]
        });
        if (!battle.TryUseTacticalCommand(0).Succeeded)
            throw new InvalidOperationException("reactive counterpart fixture failed to apply owner Status");
        battle.Step();
        var damage = battle.CombatEvents.Single(item =>
            item.Kind == BattleCombatEventKind.DamageResolved &&
            item.SourceRuntimeId == "a_attacker" && item.TargetRuntimeId == "z_owner");
        var attacker = battle.Units.Single(item => item.RuntimeId == "a_attacker");
        var shield = battle.CombatEvents.Single(item =>
            item.Kind == BattleCombatEventKind.ShieldResolved && item.TargetRuntimeId == attacker.RuntimeId);
        var trace = battle.EffectTrace.Single(item =>
            item.BindingId == bindingId && item.Status == EffectExecutionStatus.Succeeded);
        if (trace.TargetId != attacker.RuntimeId ||
            Math.Abs(trace.AppliedAmount - damage.EffectiveValue) > .001f ||
            Math.Abs(shield.EffectiveValue - damage.EffectiveValue) > .001f)
            throw new InvalidOperationException(
                "OwnerIsTarget reactive ExplicitTargetQuery did not hit the attacker with EffectiveValue: " +
                $"target={trace.TargetId}, attacker={attacker.RuntimeId}, applied={trace.AppliedAmount:R}, " +
                $"shield={shield.EffectiveValue:R}, effective={damage.EffectiveValue:R}");
    }

    private static void VerifyStatusNaturalBattleCompletion()
    {
        const string removedBindingId = "phase2_natural_completion_removed_effect";
        var status = PlainStatus(
            "phase2_natural_completion_status",
            StatusDurationKind.Permanent,
            0,
            StatusAggregationPolicy.BySource);
        status.LifecycleBindings =
        [
            new StatusLifecycleBindingSpec
            {
                Trigger = StatusLifecycleTriggerKind.Removed,
                Binding = ManualEffectBinding(removedBindingId)
            }
        ];
        var ability = AuthoredStatusAbility("phase2_natural_completion_ability", status);
        var compilation = AbilityDefinitionCompiler.CompileLoadout(
            new AbilityLoadoutDefinition { Abilities = [ability] });
        var loadout = compilation.Loadout ?? throw new InvalidOperationException(
            "natural completion fixture: " + string.Join(" | ", compilation.Report.CoreErrors));
        using var battle = new BattleSimulation(new BattleConfig
        {
            Seed = 8122,
            Identity = new BattleIdentity("phase2_natural_completion", TowerNodeType.Combat, 8122, 1, 1),
            FloorRule = new ClearFloorRuleRuntime("natural-completion", "natural-completion", "test"),
            HeroRule = ProductionRule(),
            TacticalCommands = TacticalPreparation(loadout, ability.StableId),
            Spawns =
            [
                new BattleSpawn(ProductionUnit("a_owner", true, 100, 100, 1), 0,
                    new Vector2I(1, 1), "a_owner"),
                new BattleSpawn(ProductionUnit("z_enemy", false, 1, 0, 1), 1,
                    new Vector2I(2, 1), "z_enemy")
            ]
        });
        if (!battle.TryUseTacticalCommand(0).Succeeded)
            throw new InvalidOperationException("natural completion fixture failed to apply owner Status");
        battle.Step();
        if (battle.Outcome != BattleOutcome.PlayerVictory ||
            battle.StatusTransition is not
                { Reason: StatusScopeCompletionReason.BattleCompleted, RemainingInstances: 0,
                    RemainingModifierHandles: 0, RemainingContributions: 0,
                    RemainingReactiveSubscriptions: 0 } ||
            battle.EffectTransition is not
                { Reason: BattleScopeCompletionReason.PlayerVictory, RemainingSubscriptions: 0,
                    RemainingInvocations: 0, RemainingRuntimeInstances: 0 } ||
            battle.TacticalCommandTransition is not
                { Reason: TacticalCommandScopeCompletionReason.BattleCompleted, RemainingRuntimeInstances: 0,
                    RemainingPoints: 0 } ||
            battle.AttributeTransition is not
                { Reason: AttributeScopeCompletionReason.BattleCompleted, RemainingSets: 0,
                    RemainingModifiers: 0 } ||
            battle.CombatTransition is not
                { Reason: BattleCombatCompletionReason.PlayerVictory, RemainingSubscriptions: 0,
                    RemainingReactions: 0, RemainingRuntimeEntries: 0 } ||
            !battle.CombatEvents.Any(item =>
                item.Kind == BattleCombatEventKind.StatusRemoved &&
                item.SubjectStableId == status.StableId &&
                item.Reason == StatusRemovalReason.ScopeCompleted.ToString()) ||
            battle.EffectTrace.Any(item => item.BindingId == removedBindingId))
            throw new InvalidOperationException(
                "natural Battle completion executed Removed gameplay or retained a runtime scope: " +
                $"outcome={battle.Outcome}, status={battle.StatusTransition}, effect={battle.EffectTransition}, " +
                $"ability={battle.AbilityTransition}, attribute={battle.AttributeTransition}, combat={battle.CombatTransition}, " +
                $"removedEvent={battle.CombatEvents.Any(item => item.Kind == BattleCombatEventKind.StatusRemoved && item.SubjectStableId == status.StableId && item.Reason == StatusRemovalReason.ScopeCompleted.ToString())}, " +
                $"removedEffect={battle.EffectTrace.Any(item => item.BindingId == removedBindingId)}");
    }

    private static void VerifyStatusDispositionPurge()
    {
        using var scope = new BattleStatusScope("phase2_disposition_purge", (_, _) => { });

        CompiledStatusDefinition Status(
            string id,
            StatusDisposition disposition,
            StatusDispelCategory category)
        {
            var authored = PlainStatus(
                id, StatusDurationKind.Permanent, 0,
                StatusAggregationPolicy.BySource, dispel: category);
            authored.Disposition = disposition;
            return CompileStatus(authored);
        }

        var harmfulOrdinary = Status(
            "phase2_harmful_ordinary", StatusDisposition.Harmful, StatusDispelCategory.Ordinary);
        var harmfulStrong = Status(
            "phase2_harmful_strong", StatusDisposition.Harmful, StatusDispelCategory.StrongOnly);
        var helpfulOrdinary = Status(
            "phase2_helpful_ordinary", StatusDisposition.Helpful, StatusDispelCategory.Ordinary);
        var harmfulPermanent = Status(
            "phase2_harmful_non_dispellable", StatusDisposition.Harmful, StatusDispelCategory.NonDispellable);
        foreach (var status in new[] { harmfulOrdinary, harmfulStrong, helpfulOrdinary, harmfulPermanent })
            scope.Apply(status, "source", "owner", 0);

        if (scope.DispelOwner("owner", StatusDispelStrength.Ordinary, StatusDisposition.Harmful) != 1 ||
            scope.SnapshotOwner("owner").Any(item => item.StableId == harmfulOrdinary.StableId) ||
            scope.SnapshotOwner("owner").All(item => item.StableId != harmfulStrong.StableId))
            throw new InvalidOperationException("ordinary harmful purge ignored typed dispel categories");
        if (scope.DispelOwner("owner", StatusDispelStrength.Strong, StatusDisposition.Harmful) != 1 ||
            scope.SnapshotOwner("owner").Any(item => item.StableId == harmfulStrong.StableId) ||
            scope.SnapshotOwner("owner").All(item => item.StableId != harmfulPermanent.StableId))
            throw new InvalidOperationException("strong harmful purge removed the wrong category");
        if (scope.DispelOwner("owner", StatusDispelStrength.Strong) != 1 ||
            scope.SnapshotOwner("owner") is not [{ Disposition: StatusDisposition.Harmful,
                DispelCategory: StatusDispelCategory.NonDispellable }])
            throw new InvalidOperationException("non-dispellable Status did not survive owner purge");
    }

    private static void VerifyStatusDependencyRejection()
    {
        var missingTarget = PlainStatus(
            "phase2_missing_target", StatusDurationKind.TimedTicks, 2,
            StatusAggregationPolicy.ByTarget, stackLimit: 2);
        missingTarget.OverflowPolicy = StatusOverflowPolicy.ApplyStatusAndConsumeAtLimit;
        missingTarget.OverflowStatus = PlainStatus(
            "phase2_unregistered_target", StatusDurationKind.TimedTicks, 2,
            StatusAggregationPolicy.ByTarget);
        var missing = StatusDefinitionCompiler.CompileBatch([missingTarget]);
        if (!missing.Report.CoreErrors.Any(error =>
                error.Contains("not registered", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("missing Status dependency did not reject atomic publication");

        var cycleA = PlainStatus(
            "phase2_cycle_a", StatusDurationKind.TimedTicks, 2,
            StatusAggregationPolicy.ByTarget, stackLimit: 2);
        var cycleB = PlainStatus(
            "phase2_cycle_b", StatusDurationKind.TimedTicks, 2,
            StatusAggregationPolicy.ByTarget, stackLimit: 2);
        cycleA.OverflowPolicy = StatusOverflowPolicy.ApplyStatusAndConsumeAtLimit;
        cycleA.OverflowStatus = cycleB;
        cycleB.OverflowPolicy = StatusOverflowPolicy.ApplyStatusAndConsumeAtLimit;
        cycleB.OverflowStatus = cycleA;
        var cycle = StatusDefinitionCompiler.CompileBatch([cycleA, cycleB]);
        if (!cycle.Report.CoreErrors.Any(error =>
                error.Contains("dependency cycle", StringComparison.OrdinalIgnoreCase)) ||
            !cycle.Definitions.IsEmpty)
            throw new InvalidOperationException("cyclic Status dependency retained a partial publication");

        var duplicateProjection = PlainStatus(
            "phase2_duplicate_projection", StatusDurationKind.Permanent, 0,
            StatusAggregationPolicy.BySource);
        duplicateProjection.AttributeModifiers =
        [
            DamageModifierSpec(1.1f),
            DamageModifierSpec(1.2f)
        ];
        var duplicate = StatusDefinitionCompiler.Compile(duplicateProjection);
        if (!duplicate.Report.CoreErrors.Any(error =>
                error.Contains("duplicate attribute modifier projection", StringComparison.OrdinalIgnoreCase)) ||
            duplicate.Definition is not null)
            throw new InvalidOperationException("duplicate Status projection key did not reject compilation");
    }

    private static void VerifyStatusMutationAtomicity(CompiledAttributeSetDefinition attributeDefinition)
    {
        var projected = CompileStatus(new StatusDefinition
        {
            StableId = "phase2_atomic_projected",
            DisplayName = "原子投影",
            Behavior = StatusBehaviorKind.DamageMultiplier,
            DurationKind = StatusDurationKind.Permanent,
            AggregationPolicy = StatusAggregationPolicy.BySource,
            StackLimit = 1,
            DispelCategory = StatusDispelCategory.Ordinary,
            AttributeModifiers = [DamageModifierSpec(1.25f)],
            Magnitude = 1.25f
        });

        using (var attributes = new BattleAttributeScope("phase2_apply_failure_attributes"))
        {
            var owner = attributes.CreateSet("owner", attributeDefinition);
            using var failingApply = new BattleStatusScope(
                "phase2_apply_failure",
                (_, _) => { },
                id => id == "owner" ? owner : null,
                null,
                lifecycle =>
                {
                    if (lifecycle.Kind == StatusLifecycleKind.Applied)
                        throw new InvalidOperationException("expected apply sink failure");
                },
                null);
            ExpectThrows(() => failingApply.Apply(projected, "system", "owner", 0),
                "Status apply sink failure");
            Near(owner.GetValue(CombatAttribute.AttackDamage), 10, "failed apply Attribute rollback");
            if (failingApply.LiveInstanceCount != 0 || failingApply.LiveModifierHandleCount != 0 ||
                failingApply.ContributionCount != 0)
                throw new InvalidOperationException("failed Status apply retained state");
        }

        BattleStatusScope? reentrant = null;
        reentrant = new BattleStatusScope(
            "phase2_reentry",
            (_, _) => { },
            null,
            lifecycle =>
            {
                if (lifecycle.Kind == StatusLifecycleKind.Applied)
                    reentrant!.Apply(projected, "nested", "owner", 0);
            });
        using (reentrant)
        {
            ExpectThrows(() => reentrant.Apply(projected, "outer", "owner", 0),
                "synchronous Status reentry");
            if (reentrant.LiveInstanceCount != 0 || reentrant.ContributionCount != 0)
                throw new InvalidOperationException("rejected Status reentry retained state");
        }

        using (var attributes = new BattleAttributeScope("phase2_remove_failure_attributes"))
        {
            var owner = attributes.CreateSet("owner", attributeDefinition);
            var failRemoval = false;
            using var removal = new BattleStatusScope(
                "phase2_remove_failure",
                (_, _) => { },
                id => id == "owner" ? owner : null,
                null,
                lifecycle =>
                {
                    if (failRemoval && lifecycle.Kind == StatusLifecycleKind.Removed)
                        throw new InvalidOperationException("expected removal sink failure");
                },
                null);
            removal.Apply(projected, "system", "owner", 0);
            failRemoval = true;
            ExpectThrows(
                () => removal.Dispel("owner", projected.StableId, StatusDispelStrength.Ordinary),
                "Status dispel sink failure");
            Near(owner.GetValue(CombatAttribute.AttackDamage), 12.5f,
                "failed dispel restored Attribute projection");
            if (removal.LiveInstanceCount != 1 || removal.LiveModifierHandleCount != 1 ||
                removal.ContributionCount != 1)
                throw new InvalidOperationException("failed Status dispel left a half-removed instance");
            ExpectThrows(() => removal.RemoveOwner("owner"), "Status owner-removal sink failure");
            if (removal.LiveInstanceCount != 1 || removal.LiveModifierHandleCount != 1)
                throw new InvalidOperationException("failed owner removal left a half-removed Status");
            failRemoval = false;
            removal.RemoveOwner("owner");
            Near(owner.GetValue(CombatAttribute.AttackDamage), 10, "owner removal Attribute rollback");
        }

        var transitionEffect = ManualEffectBinding("phase2_overflow_failure_binding");
        var transitionTarget = PlainStatus(
            "phase2_overflow_failure_target", StatusDurationKind.TimedTicks, 2,
            StatusAggregationPolicy.ByTarget);
        transitionTarget.LifecycleBindings =
        [
            new StatusLifecycleBindingSpec
            {
                Trigger = StatusLifecycleTriggerKind.Applied,
                Binding = transitionEffect
            }
        ];
        var transitionSource = PlainStatus(
            "phase2_overflow_failure_source", StatusDurationKind.TimedTicks, 2,
            StatusAggregationPolicy.ByTarget, stackLimit: 2);
        transitionSource.OverflowPolicy = StatusOverflowPolicy.ApplyStatusAndConsumeAtLimit;
        transitionSource.OverflowStatus = transitionTarget;
        var transitionGraph = StatusDefinitionCompiler.CompileBatch([transitionSource, transitionTarget]);
        if (transitionGraph.Report.HasCoreErrors)
            throw new InvalidOperationException("atomic overflow fixture: " +
                                                string.Join(" | ", transitionGraph.Report.CoreErrors));
        var compiledSource = transitionGraph.Definitions.Single(item => item.StableId == transitionSource.StableId);
        using (var overflow = new BattleStatusScope(
                   "phase2_overflow_failure",
                   (_, _) => { },
                   _ => null,
                   _ => false,
                   null,
                   null))
        {
            overflow.Apply(compiledSource, "source_a", "owner", 0);
            ExpectThrows(() => overflow.Apply(compiledSource, "source_b", "owner", 0),
                "overflow transition scheduler failure");
            var restored = overflow.SnapshotOwner("owner").Single();
            if (restored.StableId != compiledSource.StableId || restored.Stacks != 1 ||
                restored.SourceContributions.Single().SourceId != "source_a" ||
                overflow.LiveModifierHandleCount != 0)
                throw new InvalidOperationException("failed overflow transition did not restore its complete pre-state");
        }

        using (var attributes = new BattleAttributeScope("phase2_source_requirement_attributes"))
        {
            var owner = attributes.CreateSet("owner", attributeDefinition);
            var sourceRequired = CompileStatus(new StatusDefinition
            {
                StableId = "phase2_source_required",
                DisplayName = "来源属性",
                Behavior = StatusBehaviorKind.None,
                DurationKind = StatusDurationKind.Permanent,
                AggregationPolicy = StatusAggregationPolicy.BySource,
                StackLimit = 1,
                AttributeModifiers =
                [
                    new AttributeModifierSpec
                    {
                        Attribute = CombatAttribute.AttackDamage,
                        Operation = AttributeModifierOperation.Add,
                        Magnitude = new SourceAttributeMagnitudeSpec
                        {
                            Attribute = CombatAttribute.SpellPower,
                            CaptureMode = AttributeCaptureMode.Snapshot
                        },
                        SlotId = "source_required"
                    }
                ]
            });
            using var sourceGuard = new BattleStatusScope(
                "phase2_source_requirement",
                (_, _) => { },
                id => id == "owner" ? owner : null,
                null,
                null,
                null);
            ExpectThrows(() => sourceGuard.Apply(sourceRequired, "missing_system_source", "owner", 0),
                "missing source AttributeSet");
            if (sourceGuard.LiveInstanceCount != 0 || sourceGuard.LiveModifierHandleCount != 0)
                throw new InvalidOperationException("missing source AttributeSet retained Status projection");
        }

        using (var attributes = new BattleAttributeScope("phase2_complete_failure_attributes"))
        {
            var owner = attributes.CreateSet("owner", attributeDefinition);
            var failComplete = false;
            using var completion = new BattleStatusScope(
                "phase2_complete_failure",
                (_, _) => { },
                id => id == "owner" ? owner : null,
                null,
                lifecycle =>
                {
                    if (failComplete && lifecycle.Kind == StatusLifecycleKind.Removed)
                        throw new InvalidOperationException("expected completion sink failure");
                },
                null);
            completion.Apply(projected, "system", "owner", 0);
            failComplete = true;
            ExpectThrows(() => completion.Complete(StatusScopeCompletionReason.BattleCompleted, 1),
                "Status completion sink failure");
            Near(owner.GetValue(CombatAttribute.AttackDamage), 10, "completion failure Attribute cleanup");
            if (completion.Transition is not
                { Reason: StatusScopeCompletionReason.Exception, RemainingInstances: 0,
                    RemainingModifierHandles: 0, RemainingContributions: 0 })
                throw new InvalidOperationException("completion sink failure retained Status state");
        }

        using (var cueFailure = new BattleStatusScope(
                   "phase2_complete_cue_failure",
                   (_, _) => { },
                   _ => null,
                   null,
                   null,
                   cue =>
                   {
                       if (cue.Lifecycle == StatusPresentationCueLifecycle.Removed)
                           throw new InvalidOperationException("expected completion cue failure");
                   }))
        {
            var presented = PlainStatus(
                "phase2_complete_cue_status",
                StatusDurationKind.Permanent,
                0,
                StatusAggregationPolicy.BySource);
            presented.Presentation = CuePresentation("complete_cue");
            cueFailure.Apply(CompileStatus(presented), "source", "owner", 0);
            ExpectThrows(
                () => cueFailure.Complete(StatusScopeCompletionReason.BattleCompleted, 1),
                "Status completion cue failure");
            if (cueFailure.Transition is not
                { Reason: StatusScopeCompletionReason.Exception, RemainingInstances: 0,
                    RemainingReactiveSubscriptions: 0 })
                throw new InvalidOperationException("completion cue failure retained Status state or reason");
        }
    }

    private static void VerifyStatusReactionFailureCleanup()
    {
        var status = new StatusDefinition
        {
            StableId = "phase2_reaction_status",
            DisplayName = "反应异常状态",
            Behavior = StatusBehaviorKind.DamageMultiplier,
            DurationKind = StatusDurationKind.Permanent,
            AggregationPolicy = StatusAggregationPolicy.BySource,
            StackLimit = 1,
            AttributeModifiers = [DamageModifierSpec(1.1f)],
            Magnitude = 1.1f,
            LifecycleBindings =
            [
                new StatusLifecycleBindingSpec
                {
                    Trigger = StatusLifecycleTriggerKind.Removed,
                    Binding = ManualEffectBinding("phase2_exception_removed_effect")
                }
            ]
        };
        var ability = AuthoredStatusAbility("phase2_reaction_ability", status);
        var compilation = AbilityDefinitionCompiler.CompileLoadout(
            new AbilityLoadoutDefinition { Abilities = [ability] });
        var loadout = compilation.Loadout ?? throw new InvalidOperationException(
            "reaction failure fixture: " + string.Join(" | ", compilation.Report.CoreErrors));
        var listenerSource = CombatSourceRef.System("phase2_status_reaction_failure");
        using var battle = new BattleSimulation(new BattleConfig
        {
            Seed = 8120,
            Identity = new BattleIdentity("phase2_status_reaction", TowerNodeType.Combat, 8120, 1, 1),
            FloorRule = new ClearFloorRuleRuntime("status-reaction", "status-reaction", "test"),
            HeroRule = ProductionRule(),
            TacticalCommands = TacticalPreparation(loadout, ability.StableId),
            ConfigureCombatBindings = bindings => bindings.Subscribe(
                BattleCombatEventKind.StatusApplied,
                listenerSource,
                0,
                (_, reactions) =>
                {
                    if (!reactions.Enqueue(listenerSource, 0,
                            _ => throw new InvalidOperationException("expected post-Status reaction failure")))
                        throw new InvalidOperationException("reaction failure fixture enqueue rejected");
                }),
            Spawns =
            [
                new BattleSpawn(ProductionUnit("status_reaction_hero", true, 100, 10, 1), 0,
                    new Vector2I(0, 1), "status_reaction_hero"),
                new BattleSpawn(ProductionUnit("status_reaction_enemy", false, 100, 0, 1), 1,
                    new Vector2I(9, 1), "status_reaction_enemy")
            ]
        });
        var before = TacticalFingerprint(battle);
        var result = battle.TryUseTacticalCommand(0);
        if (result.Succeeded || result.Failure != TacticalCommandActivationFailure.CommitFailed ||
            battle.Outcome != BattleOutcome.Running || TacticalFingerprint(battle) != before ||
            battle.StatusTransition is not null || battle.AttributeTransition is not null ||
            battle.CombatTransition is not null || battle.EffectTransition is not null)
            throw new InvalidOperationException(
                "post-Status reaction failure did not restore the complete running Battle world");
    }

    private static StatusDefinition PlainStatus(
        string id,
        StatusDurationKind durationKind,
        int durationTicks,
        StatusAggregationPolicy aggregation,
        int stackLimit = 1,
        StatusDeathPolicy deathPolicy = StatusDeathPolicy.Remove,
        StatusDispelCategory dispel = StatusDispelCategory.Ordinary) => new()
    {
        StableId = id,
        DisplayName = id,
        Behavior = StatusBehaviorKind.None,
        DurationKind = durationKind,
        DurationTicks = durationTicks,
        AggregationPolicy = aggregation,
        StackLimit = stackLimit,
        DurationRefreshPolicy = durationKind == StatusDurationKind.TimedTicks
            ? StatusDurationRefreshPolicy.Reset
            : StatusDurationRefreshPolicy.None,
        DeathPolicy = deathPolicy,
        DispelCategory = dispel
    };

    private static CompiledStatusDefinition CompileStatus(StatusDefinition authored)
    {
        var result = StatusDefinitionCompiler.Compile(authored);
        return result.Definition ?? throw new InvalidOperationException(
            $"{authored.StableId}: " + string.Join(" | ", result.Report.CoreErrors));
    }

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

    private static AttributeModifierSpec DamageModifierSpec(float multiplier) => new()
    {
        Attribute = CombatAttribute.AttackDamage,
        Operation = AttributeModifierOperation.Multiply,
        Magnitude = new ConstantAttributeMagnitudeSpec { Value = multiplier },
        SlotId = "damage_multiplier"
    };

    private static EffectBindingSpec ManualEffectBinding(string id) => new()
    {
        StableId = id,
        Trigger = new EffectTriggerSpec { Kind = EffectTriggerKind.Manual },
        Conditions = [],
        TargetQuery = new OwnerTargetQuerySpec(),
        Effects = [new ShieldEffectSpec { Amount = 1 }],
        Limits = new EffectBindingLimitsSpec()
    };

    private static StatusPresentationSpec CuePresentation(string prefix) => new()
    {
        SemanticIcon = prefix,
        ExecutedCue = prefix + "_executed",
        OnActiveCue = prefix + "_active",
        WhileActiveCue = prefix + "_while",
        RemovedCue = prefix + "_removed",
        ReportLabel = prefix
    };

    private static void VerifyAttributeBehavior()
    {
        var authored = new AttributeSetDefinition
        {
            Attributes =
            [
                Attribute(CombatAttribute.AttackDamage, 10, 0, 100),
                Attribute(CombatAttribute.MaxHealth, 50, 0, 500),
                Attribute(CombatAttribute.SpellPower, 2, 0, 100),
                Attribute(CombatAttribute.AttackSpeed, 1, .01f, 10),
                Attribute(CombatAttribute.AttackRange, 1, 0, 10),
                Attribute(CombatAttribute.MoveSpeed, 1, .01f, 10),
                Attribute(CombatAttribute.CriticalChance, 0, 0, 1),
                Attribute(CombatAttribute.LifeSteal, 0, 0, 1),
                Attribute(CombatAttribute.ControlResistance, 0, 0, 1),
                Attribute(CombatAttribute.MaxMana, 0, 0, 100),
                Attribute(CombatAttribute.StartingMana, 0, 0, 100),
                Attribute(CombatAttribute.HealingPower, 0, 0, 500),
                Attribute(CombatAttribute.MagicResistance, 0, 0, 100),
                Attribute(CombatAttribute.Armor, 0, 0, 100),
                Attribute(CombatAttribute.CriticalDamage, 0, 0, 100)
            ]
        };
        var compilation = AttributeDefinitionCompiler.Compile(authored);
        var definition = compilation.Definition ?? throw new InvalidOperationException(
            "attribute compilation: " + string.Join(';', compilation.Report.CoreErrors));
        var fingerprint = definition.Fingerprint;
        using var scope = new BattleAttributeScope("attribute-battle-a");
        using var otherBattle = new BattleAttributeScope("attribute-battle-b");
        var target = scope.CreateSet("target", definition);
        var source = scope.CreateSet("source", definition);
        var isolated = otherBattle.CreateSet("target", definition);

        var sourceA = CombatSourceRef.Unit("unit_a", "source", "instance_a");
        var sourceB = CombatSourceRef.Unit("unit_b", "source", "instance_b");
        var multiplierSource = CombatSourceRef.System("multiplier");
        var overrideSource = CombatSourceRef.System("override");
        var handleA = target.ApplyModifier(Constant(CombatAttribute.AttackDamage, AttributeModifierOperation.Add, 10), sourceA);
        target.ApplyModifier(Constant(CombatAttribute.AttackDamage, AttributeModifierOperation.Add, 10), sourceB);
        target.ApplyModifier(Constant(CombatAttribute.AttackDamage, AttributeModifierOperation.Multiply, 2), multiplierSource);
        var overrideHandle = target.ApplyModifier(Constant(CombatAttribute.AttackDamage, AttributeModifierOperation.Override, 150), overrideSource);
        Near(target.GetValue(CombatAttribute.AttackDamage), 100, "add→multiply→override→clamp");
        if (!target.Remove(overrideHandle)) throw new InvalidOperationException("override handle removal");
        Near(target.GetValue(CombatAttribute.AttackDamage), 60, "override rollback");

        var refreshed = target.ApplyModifier(Constant(CombatAttribute.AttackDamage, AttributeModifierOperation.Add, 5), sourceA);
        if (refreshed != handleA) throw new InvalidOperationException("same-source refresh changed authoritative handle");
        Near(target.GetValue(CombatAttribute.AttackDamage), 50, "same-source replace");
        if (!target.Remove(handleA)) throw new InvalidOperationException("source A handle removal");
        Near(target.GetValue(CombatAttribute.AttackDamage), 40, "different-source isolation");
        if (target.RemoveSource(sourceB) != 1) throw new InvalidOperationException("source B removal count");
        Near(target.GetValue(CombatAttribute.AttackDamage), 20, "source removal retained multiplier only");
        Near(isolated.GetValue(CombatAttribute.AttackDamage), 10, "cross-battle isolation");

        using (var sameSeedA = new BattleAttributeScope("battle_42_attributes"))
        using (var sameSeedB = new BattleAttributeScope("battle_42_attributes"))
        {
            var sameOwnerA = sameSeedA.CreateSet("same-owner", definition);
            var sameOwnerB = sameSeedB.CreateSet("same-owner", definition);
            var crossBattleHandle = sameOwnerA.ApplyModifier(
                Constant(CombatAttribute.AttackDamage, AttributeModifierOperation.Add, 5),
                CombatSourceRef.System("same-seed"));
            sameOwnerB.ApplyModifier(
                Constant(CombatAttribute.AttackDamage, AttributeModifierOperation.Add, 7),
                CombatSourceRef.System("same-seed"));
            if (sameOwnerB.Remove(crossBattleHandle))
                throw new InvalidOperationException("same-seed cross-Battle handle removed another scope's modifier");
            Near(sameOwnerB.GetValue(CombatAttribute.AttackDamage), 17, "same-seed handle isolation");
            ExpectThrows(
                () => sameOwnerB.ApplyModifier(
                    new CompiledAttributeModifier(
                        CombatAttribute.SpellPower,
                        AttributeModifierOperation.Add,
                        new CompiledSourceAttributeMagnitude(
                            CombatAttribute.SpellPower,
                            AttributeCaptureMode.Live),
                        0,
                        "same-seed-live"),
                    CombatSourceRef.System("same-seed-live"),
                    new BattleAttributeMagnitudeContext(sameOwnerA, sameOwnerB)),
                "same-seed cross-Battle live magnitude");
            sameSeedA.Complete(AttributeScopeCompletionReason.BattleCompleted, 1);
            sameSeedB.Complete(AttributeScopeCompletionReason.BattleCompleted, 1);
        }

        var slotOne = target.ApplyModifier(Constant(CombatAttribute.AttackDamage, AttributeModifierOperation.Add, 1, "slot_one"), sourceA);
        var slotTwo = target.ApplyModifier(Constant(CombatAttribute.AttackDamage, AttributeModifierOperation.Add, 2, "slot_two"), sourceA);
        if (slotOne == slotTwo) throw new InvalidOperationException("same source distinct slots shared a handle");
        Near(target.GetValue(CombatAttribute.AttackDamage), 26, "same-source distinct-slot stacking");
        target.Remove(slotOne);
        Near(target.GetValue(CombatAttribute.AttackDamage), 24, "slot-specific rollback");
        target.Remove(slotTwo);

        var lowOverride = target.ApplyModifier(
            new CompiledAttributeModifier(CombatAttribute.AttackDamage, AttributeModifierOperation.Override,
                new CompiledConstantMagnitude(31), 1, "low"), sourceA);
        var winningOverride = target.ApplyModifier(
            new CompiledAttributeModifier(CombatAttribute.AttackDamage, AttributeModifierOperation.Override,
                new CompiledConstantMagnitude(41), 2, "winner"), sourceB);
        target.ApplyModifier(
            new CompiledAttributeModifier(CombatAttribute.AttackDamage, AttributeModifierOperation.Override,
                new CompiledConstantMagnitude(99), 1, "late_low"), CombatSourceRef.System("late_low"));
        Near(target.GetValue(CombatAttribute.AttackDamage), 41, "override priority winner");
        var samePriority = CombatSourceRef.System("same_priority");
        var samePriorityHandle = target.ApplyModifier(
            new CompiledAttributeModifier(CombatAttribute.AttackDamage, AttributeModifierOperation.Override,
                new CompiledConstantMagnitude(51), 2, "same"), samePriority);
        Near(target.GetValue(CombatAttribute.AttackDamage), 51, "override application-order winner");
        var refreshedWinner = target.ApplyModifier(
            new CompiledAttributeModifier(CombatAttribute.AttackDamage, AttributeModifierOperation.Override,
                new CompiledConstantMagnitude(61), 2, "winner"), sourceB);
        if (refreshedWinner != winningOverride) throw new InvalidOperationException("override refresh changed handle");
        Near(target.GetValue(CombatAttribute.AttackDamage), 61, "override refresh winner");
        target.Remove(lowOverride);
        target.Remove(winningOverride);
        target.Remove(samePriorityHandle);
        target.RemoveSource(CombatSourceRef.System("late_low"));

        var invocation = 3f;
        var alive = 4f;
        var trait = 5f;
        var magnitudeContext = new BattleAttributeMagnitudeContext(
            source,
            target,
            key => key == "power" ? invocation : 0,
            (kind, team) => kind == AttributeTeamCountKind.Alive && team == 0 ? alive : 0,
            (id, team) => id == "frost" && team == 0 ? trait : 0);
        target.ApplyModifier(CompileModifier(CombatAttribute.MaxMana, AttributeModifierOperation.Add,
            new SourceAttributeMagnitudeSpec { Attribute = CombatAttribute.SpellPower, CaptureMode = AttributeCaptureMode.Snapshot }),
            CombatSourceRef.System("source_snapshot"), magnitudeContext);
        target.ApplyModifier(CompileModifier(CombatAttribute.StartingMana, AttributeModifierOperation.Add,
            new SourceAttributeMagnitudeSpec { Attribute = CombatAttribute.SpellPower, CaptureMode = AttributeCaptureMode.Live }),
            CombatSourceRef.System("source_live"), magnitudeContext);
        target.ApplyModifier(CompileModifier(CombatAttribute.HealingPower, AttributeModifierOperation.Add,
            new TargetAttributeMagnitudeSpec { Attribute = CombatAttribute.MaxHealth, CaptureMode = AttributeCaptureMode.Snapshot }),
            CombatSourceRef.System("target_snapshot"), magnitudeContext);
        target.ApplyModifier(CompileModifier(CombatAttribute.MagicResistance, AttributeModifierOperation.Add,
            new ContextAttributeMagnitudeSpec { Key = "power", CaptureMode = AttributeCaptureMode.Live }),
            CombatSourceRef.System("context_live"), magnitudeContext);
        target.ApplyModifier(CompileModifier(CombatAttribute.Armor, AttributeModifierOperation.Add,
            new TeamCountAttributeMagnitudeSpec { CountKind = AttributeTeamCountKind.Alive, Team = 0, CaptureMode = AttributeCaptureMode.Live }),
            CombatSourceRef.System("count_live"), magnitudeContext);
        target.ApplyModifier(CompileModifier(CombatAttribute.CriticalDamage, AttributeModifierOperation.Add,
            new TraitValueAttributeMagnitudeSpec { TraitId = "frost", Team = 0, CaptureMode = AttributeCaptureMode.Live }),
            CombatSourceRef.System("trait_live"), magnitudeContext);
        source.SetBaseValue(CombatAttribute.SpellPower, 7);
        invocation = 6;
        alive = 7;
        trait = 8;
        Near(target.GetValue(CombatAttribute.MaxMana), 2, "snapshot magnitude");
        Near(target.GetValue(CombatAttribute.StartingMana), 7, "live source magnitude");
        Near(target.GetValue(CombatAttribute.HealingPower), 50, "target magnitude");
        Near(target.GetValue(CombatAttribute.MagicResistance), 6, "context magnitude");
        Near(target.GetValue(CombatAttribute.Armor), 7, "team count magnitude");
        Near(target.GetValue(CombatAttribute.CriticalDamage), 8, "trait magnitude");

        var beforeRejected = target.ModifierCount;
        ExpectThrows(() => target.ApplyModifier(
            new CompiledAttributeModifier(CombatAttribute.AttackDamage, AttributeModifierOperation.Add,
                new CompiledSourceAttributeMagnitude(CombatAttribute.AttackDamage, AttributeCaptureMode.Live), 0, "cycle"),
            CombatSourceRef.System("cycle"), new BattleAttributeMagnitudeContext(target, target)), "live magnitude cycle");
        ExpectThrows(() => target.ApplyModifier(
            new CompiledAttributeModifier(CombatAttribute.MagicResistance, AttributeModifierOperation.Add,
                new CompiledContextValueMagnitude("nan", AttributeCaptureMode.Live), 0, "nan"),
            CombatSourceRef.System("nan"), new BattleAttributeMagnitudeContext(contextValue: _ => float.NaN)),
            "non-finite live magnitude");
        if (target.ModifierCount != beforeRejected) throw new InvalidOperationException("rejected live magnitude leaked a modifier");

        using (var baselineScope = new BattleAttributeScope("sequence-baseline"))
        using (var failureScope = new BattleAttributeScope("sequence-failure"))
        {
            var baseline = baselineScope.CreateSet("owner", definition);
            var afterFailure = failureScope.CreateSet("owner", definition);
            var firstSource = CombatSourceRef.System("sequence-first");
            var secondSource = CombatSourceRef.System("sequence-second");
            var baselineFirst = baseline.ApplyModifier(
                Constant(CombatAttribute.AttackDamage, AttributeModifierOperation.Override, 20, "order"),
                firstSource);
            var failureFirst = afterFailure.ApplyModifier(
                Constant(CombatAttribute.AttackDamage, AttributeModifierOperation.Override, 20, "order"),
                firstSource);
            ExpectThrows(
                () => afterFailure.ApplyModifier(
                    new CompiledAttributeModifier(
                        CombatAttribute.AttackDamage,
                        AttributeModifierOperation.Add,
                        new CompiledContextValueMagnitude("invalid", AttributeCaptureMode.Snapshot),
                        0,
                        "failed-new"),
                    CombatSourceRef.System("failed-new"),
                    new BattleAttributeMagnitudeContext(contextValue: _ => float.NaN)),
                "failed modifier sequence rollback");
            var baselineSecond = baseline.ApplyModifier(
                Constant(CombatAttribute.AttackDamage, AttributeModifierOperation.Override, 30, "order"),
                secondSource);
            var failureSecond = afterFailure.ApplyModifier(
                Constant(CombatAttribute.AttackDamage, AttributeModifierOperation.Override, 30, "order"),
                secondSource);
            if (baselineFirst.Sequence != failureFirst.Sequence || baselineSecond.Sequence != failureSecond.Sequence)
                throw new InvalidOperationException("failed modifier consumed an observable handle sequence");
            Near(baseline.GetValue(CombatAttribute.AttackDamage), 30, "baseline modifier order");
            Near(afterFailure.GetValue(CombatAttribute.AttackDamage), 30, "post-failure modifier order");

            ExpectThrows(
                () => afterFailure.ApplyModifier(
                    new CompiledAttributeModifier(
                        CombatAttribute.AttackDamage,
                        AttributeModifierOperation.Override,
                        new CompiledContextValueMagnitude("invalid-replace", AttributeCaptureMode.Live),
                        0,
                        "order"),
                    firstSource,
                    new BattleAttributeMagnitudeContext(contextValue: _ => float.NaN)),
                "failed modifier replacement rollback");
            Near(afterFailure.GetValue(CombatAttribute.AttackDamage), 30, "failed replacement restored order");
            baseline.ApplyModifier(
                Constant(CombatAttribute.AttackDamage, AttributeModifierOperation.Override, 40, "order"),
                firstSource);
            afterFailure.ApplyModifier(
                Constant(CombatAttribute.AttackDamage, AttributeModifierOperation.Override, 40, "order"),
                firstSource);
            Near(baseline.GetValue(CombatAttribute.AttackDamage), 40, "baseline replacement order");
            Near(afterFailure.GetValue(CombatAttribute.AttackDamage), 40, "post-failure replacement order");
            baselineScope.Complete(AttributeScopeCompletionReason.BattleCompleted, 1);
            failureScope.Complete(AttributeScopeCompletionReason.BattleCompleted, 1);
        }

        var invalidSourceMagnitude = AttributeDefinitionCompiler.Compile(new AttributeModifierSpec
        {
            Attribute = CombatAttribute.AttackDamage,
            Operation = AttributeModifierOperation.Add,
            SlotId = "invalid-source-attribute",
            Magnitude = new SourceAttributeMagnitudeSpec { Attribute = (CombatAttribute)999 }
        });
        if (invalidSourceMagnitude.Modifier is not null || !invalidSourceMagnitude.Report.HasCoreErrors)
            throw new InvalidOperationException("invalid source-attribute magnitude enum compiled");
        var invalidTargetMagnitude = AttributeDefinitionCompiler.Compile(new AttributeModifierSpec
        {
            Attribute = CombatAttribute.AttackDamage,
            Operation = AttributeModifierOperation.Add,
            SlotId = "invalid-target-attribute",
            Magnitude = new TargetAttributeMagnitudeSpec { Attribute = (CombatAttribute)999 }
        });
        if (invalidTargetMagnitude.Modifier is not null || !invalidTargetMagnitude.Report.HasCoreErrors)
            throw new InvalidOperationException("invalid target-attribute magnitude enum compiled");
        var invalidCountMagnitude = AttributeDefinitionCompiler.Compile(new AttributeModifierSpec
        {
            Attribute = CombatAttribute.AttackDamage,
            Operation = AttributeModifierOperation.Add,
            SlotId = "invalid-count-kind",
            Magnitude = new TeamCountAttributeMagnitudeSpec
                { CountKind = (AttributeTeamCountKind)999, Team = 0 }
        });
        if (invalidCountMagnitude.Modifier is not null || !invalidCountMagnitude.Report.HasCoreErrors)
            throw new InvalidOperationException("invalid team-count magnitude enum compiled");
        var invalidCountTeam = AttributeDefinitionCompiler.Compile(new AttributeModifierSpec
        {
            Attribute = CombatAttribute.AttackDamage,
            Operation = AttributeModifierOperation.Add,
            SlotId = "invalid-count-team",
            Magnitude = new TeamCountAttributeMagnitudeSpec
                { CountKind = AttributeTeamCountKind.Alive, Team = 2 }
        });
        if (invalidCountTeam.Modifier is not null || !invalidCountTeam.Report.HasCoreErrors)
            throw new InvalidOperationException("invalid team-count magnitude team compiled");
        var invalidTraitTeam = AttributeDefinitionCompiler.Compile(new AttributeModifierSpec
        {
            Attribute = CombatAttribute.AttackDamage,
            Operation = AttributeModifierOperation.Add,
            SlotId = "invalid-trait-team",
            Magnitude = new TraitValueAttributeMagnitudeSpec { TraitId = "phase1_trait", Team = 2 }
        });
        if (invalidTraitTeam.Modifier is not null || !invalidTraitTeam.Report.HasCoreErrors)
            throw new InvalidOperationException("invalid trait magnitude team compiled");

        var rejectBaseMutation = false;
        target.ApplyModifier(
            new CompiledAttributeModifier(
                CombatAttribute.AttackSpeed,
                AttributeModifierOperation.Add,
                new CompiledContextValueMagnitude("base_guard", AttributeCaptureMode.Live),
                0,
                "base_guard"),
            CombatSourceRef.System("base_guard"),
            new BattleAttributeMagnitudeContext(contextValue: _ => rejectBaseMutation ? float.NaN : 1));
        target.SetBaseValue(CombatAttribute.AttackSpeed, 2);
        rejectBaseMutation = true;
        ExpectThrows(() => target.SetBaseValue(CombatAttribute.AttackSpeed, 4), "failed base mutation");
        rejectBaseMutation = false;
        Near(target.GetBaseValue(CombatAttribute.AttackSpeed), 2, "failed base mutation rollback");
        Near(target.GetValue(CombatAttribute.AttackSpeed), 3, "failed base mutation projected rollback");

        var transition = scope.Complete(AttributeScopeCompletionReason.BattleCompleted, 9);
        if (transition.RemainingSets != 0 || transition.RemainingModifiers != 0 || scope.LiveSetCount != 0 || scope.ModifierCount != 0)
            throw new InvalidOperationException("attribute completion retained mutable state");
        if (definition.Fingerprint != fingerprint) throw new InvalidOperationException("shared compiled definition mutated");
        otherBattle.Complete(AttributeScopeCompletionReason.Abort, 9);
    }

    private static void VerifyCombatEventBehavior()
    {
        using var pipeline = new BattleCombatEventPipeline("event-battle");
        var listenerA = CombatSourceRef.System("listener_a");
        var listenerB = CombatSourceRef.System("listener_b");
        var observed = new List<string>();
        BattleCombatPublishResult? reentry = null;
        using var second = pipeline.Subscribe(BattleCombatEventKind.AttackDeclared, listenerB, 10,
            (_, _) => observed.Add("b"));
        using var first = pipeline.Subscribe(BattleCombatEventKind.AttackDeclared, listenerA, 0,
            (combatEvent, reactions) =>
            {
                observed.Add("a");
                reentry = pipeline.Publish(new BattleCombatEventDraft(
                    BattleCombatEventKind.StatusApplied, listenerA, "a", "target", combatEvent.Tick,
                    SubjectStableId: "status_probe", CurrentStacks: 1));
                ExpectThrows(() => pipeline.BeginAuthoritativeResolution(), "listener authoritative resolution re-entry");
                if (!reactions.Enqueue(listenerA, 0, context =>
                    {
                        observed.Add("reaction");
                        var result = context.Publish(new BattleCombatEventDraft(
                            BattleCombatEventKind.StatusApplied, listenerA, "a", "target", combatEvent.Tick,
                            SubjectStableId: "status_probe", CurrentStacks: 1));
                        if (!result.Accepted) throw new InvalidOperationException("queued reaction publish rejected");
                    }))
                    throw new InvalidOperationException("queued reaction rejected");
            });
        var attacker = CombatSourceRef.Unit("attacker", "attacker", "attacker-instance");
        var declared = pipeline.Publish(new BattleCombatEventDraft(
            BattleCombatEventKind.AttackDeclared, attacker, "attacker", "target", 3));
        if (!declared.Accepted) throw new InvalidOperationException("attack declared publish rejected");
        if (reentry is not { Accepted: false, Rejection: BattleCombatPublishRejection.SynchronousReentry })
            throw new InvalidOperationException("synchronous listener re-entry was not rejected");
        if (!observed.SequenceEqual(["a", "b", "reaction"]))
            throw new InvalidOperationException("deterministic listener/reaction order: " + string.Join(',', observed));

        pipeline.Publish(new BattleCombatEventDraft(BattleCombatEventKind.DamageResolved, attacker, "attacker", "target", 3,
            RequestedValue: 20, AppliedValue: 18, EffectiveValue: 18));
        pipeline.Publish(new BattleCombatEventDraft(BattleCombatEventKind.StatusStackChanged, attacker, "attacker", "target", 3,
            SubjectStableId: "status_probe", PreviousStacks: 1, CurrentStacks: 2));
        pipeline.Publish(new BattleCombatEventDraft(BattleCombatEventKind.StatusRemoved, attacker, "attacker", "target", 3,
            SubjectStableId: "status_probe", PreviousStacks: 2, Reason: "expired"));
        pipeline.Publish(new BattleCombatEventDraft(BattleCombatEventKind.UnitDefeated, CombatSourceRef.None, string.Empty, "target", 3));
        pipeline.Publish(new BattleCombatEventDraft(BattleCombatEventKind.UnitKilled, attacker, "attacker", "target", 3));
        pipeline.Publish(new BattleCombatEventDraft(BattleCombatEventKind.AttackLanded, attacker, "attacker", "target", 3,
            EffectiveValue: 18));
        var kinds = pipeline.Events.Select(item => item.Kind).ToArray();
        var expected = new[]
        {
            BattleCombatEventKind.AttackDeclared,
            BattleCombatEventKind.StatusApplied,
            BattleCombatEventKind.DamageResolved,
            BattleCombatEventKind.StatusStackChanged,
            BattleCombatEventKind.StatusRemoved,
            BattleCombatEventKind.UnitDefeated,
            BattleCombatEventKind.UnitKilled,
            BattleCombatEventKind.AttackLanded
        };
        if (!kinds.SequenceEqual(expected))
            throw new InvalidOperationException("typed event order: " + string.Join(',', kinds));
        if (pipeline.Events.Single(item => item.Kind == BattleCombatEventKind.UnitDefeated).Source.IsSpecified)
            throw new InvalidOperationException("uncredited defeat became a kill source");
        if (!pipeline.Events.Single(item => item.Kind == BattleCombatEventKind.UnitKilled).Source.IsSpecified)
            throw new InvalidOperationException("credited kill lost its source");
        if (pipeline.Events.Select(item => item.Sequence).Distinct().Count() != pipeline.Events.Count)
            throw new InvalidOperationException("event sequence is not unique");

        using (var rollbackPipeline = new BattleCombatEventPipeline("reaction-rollback"))
        {
            var executed = false;
            using var rollbackSubscription = rollbackPipeline.Subscribe(
                BattleCombatEventKind.AttackDeclared,
                listenerA,
                0,
                (_, reactions) => reactions.Enqueue(listenerA, 0, _ => executed = true));
            using (rollbackPipeline.BeginAuthoritativeResolution())
                rollbackPipeline.Publish(new BattleCombatEventDraft(
                    BattleCombatEventKind.AttackDeclared, attacker, "attacker", "target", 1));
            if (executed || rollbackPipeline.PendingReactionCount != 0)
                throw new InvalidOperationException("aborted authoritative resolution executed or retained a reaction");
        }

        using (var listenerFailure = new BattleCombatEventPipeline("listener-failure-rollback"))
        {
            var leakedExecution = 0;
            using var enqueueFirst = listenerFailure.Subscribe(
                BattleCombatEventKind.AttackDeclared,
                listenerA,
                0,
                (_, reactions) =>
                {
                    if (!reactions.Enqueue(listenerA, 0, _ => leakedExecution++))
                        throw new InvalidOperationException("listener failure fixture enqueue rejected");
                });
            using var throwSecond = listenerFailure.Subscribe(
                BattleCombatEventKind.AttackDeclared,
                listenerB,
                1,
                (_, _) => throw new InvalidOperationException("expected listener failure"));
            ExpectThrows(
                () => listenerFailure.Publish(new BattleCombatEventDraft(
                    BattleCombatEventKind.AttackDeclared, attacker, "attacker", "target", 1)),
                "listener failure reaction rollback");
            if (listenerFailure.PendingReactionCount != 0)
                throw new InvalidOperationException("listener failure retained an enqueued reaction");
            var failed = listenerFailure.Complete(BattleCombatCompletionReason.Exception, 1);
            if (leakedExecution != 0)
                throw new InvalidOperationException("completion executed a listener-failure reaction");
            AssertPipelineZero(listenerFailure, failed, "listener failure rollback");
        }

        using (var reactionFailure = new BattleCombatEventPipeline("reaction-failure-rollback"))
        {
            var trailingExecution = 0;
            using var subscription = reactionFailure.Subscribe(
                BattleCombatEventKind.AttackDeclared,
                listenerA,
                0,
                (_, reactions) =>
                {
                    if (!reactions.Enqueue(listenerA, 0, _ => throw new InvalidOperationException("expected reaction failure")) ||
                        !reactions.Enqueue(listenerB, 1, _ => trailingExecution++))
                        throw new InvalidOperationException("reaction failure fixture enqueue rejected");
                });
            ExpectThrows(
                () => reactionFailure.Publish(new BattleCombatEventDraft(
                    BattleCombatEventKind.AttackDeclared, attacker, "attacker", "target", 1)),
                "reaction failure queued-work rollback");
            if (reactionFailure.PendingReactionCount != 0)
                throw new InvalidOperationException("reaction failure retained queued work");
            var failed = reactionFailure.Complete(BattleCombatCompletionReason.Exception, 1);
            if (trailingExecution != 0)
                throw new InvalidOperationException("completion executed queued work after a reaction failure");
            AssertPipelineZero(reactionFailure, failed, "reaction failure rollback");
        }

        using (var lifo = new BattleCombatEventPipeline("resolution-lifo"))
        {
            var outer = lifo.BeginAuthoritativeResolution();
            var inner = lifo.BeginAuthoritativeResolution();
            outer.Commit();
            inner.Commit();
            ExpectThrows(outer.Dispose, "out-of-order authoritative resolution disposal");
            inner.Dispose();
            outer.Dispose();
            AssertPipelineZero(
                lifo,
                lifo.Complete(BattleCombatCompletionReason.Abort, 1),
                "authoritative resolution LIFO recovery");
        }

        if (pipeline.Events is List<BattleCombatEvent> || pipeline.Trace is List<BattleCombatTraceEntry> ||
            pipeline.Events is not ICollection<BattleCombatEvent> eventView || !eventView.IsReadOnly ||
            pipeline.Trace is not ICollection<BattleCombatTraceEntry> traceView || !traceView.IsReadOnly)
            throw new InvalidOperationException("event or trace collection exposed its mutable backing List");
        ExpectThrows(eventView.Clear, "event view mutation");
        ExpectThrows(traceView.Clear, "trace view mutation");

        var transition = pipeline.Complete(BattleCombatCompletionReason.PlayerVictory, 4);
        AssertPipelineZero(pipeline, transition, "victory");
        foreach (var reason in new[]
                 {
                     BattleCombatCompletionReason.PlayerDefeat,
                     BattleCombatCompletionReason.Timeout,
                     BattleCombatCompletionReason.Abort,
                     BattleCombatCompletionReason.Replacement,
                     BattleCombatCompletionReason.Exception
                 })
        {
            using var candidate = new BattleCombatEventPipeline("completion-" + reason);
            using var subscription = candidate.Subscribe(BattleCombatEventKind.AttackLanded, listenerA, 0, (_, _) => { });
            AssertPipelineZero(candidate, candidate.Complete(reason, 1), reason.ToString());
        }
        using (var completionFailure = new BattleCombatEventPipeline("completion-listener-failure"))
        {
            using var failingSubscription = completionFailure.Subscribe(
                BattleCombatEventKind.BattleCompleted,
                listenerA,
                0,
                (_, _) => throw new InvalidOperationException("expected completion listener failure"));
            ExpectThrows(
                () => completionFailure.Complete(BattleCombatCompletionReason.PlayerVictory, 1),
                "completion listener exception");
            var failedTransition = completionFailure.Transition ??
                throw new InvalidOperationException("completion listener failure transition missing");
            if (failedTransition.Reason != BattleCombatCompletionReason.Exception)
                throw new InvalidOperationException("completion listener failure did not become Exception");
            AssertPipelineZero(completionFailure, failedTransition, "completion listener failure");
        }
        var disposed = new BattleCombatEventPipeline("completion-disposal");
        using var disposedSubscription = disposed.Subscribe(BattleCombatEventKind.AttackLanded, listenerA, 0, (_, _) => { });
        disposed.Dispose();
        AssertPipelineZero(disposed, disposed.Transition ?? throw new InvalidOperationException("disposal transition missing"), "disposal");
        VerifyProductionCombatEventBehavior();
    }

    private static void VerifyProductionCombatEventBehavior()
    {
        var first = new BattleSimulation(ProductionConfig(8101, 100, 1000, 10, 0));
        var firstResult = first.RunToEnd();
        var transition = first.CombatTransition ?? throw new InvalidOperationException("production combat transition missing");
        var kinds = transition.Events.Select(item => item.Kind).ToArray();
        if (transition.Events.Any(item => item.Identity != firstResult.Identity))
            throw new InvalidOperationException("production combat event lost Battle identity");
        foreach (var required in new[]
                 {
                     BattleCombatEventKind.BattleStarted,
                     BattleCombatEventKind.AttackDeclared,
                     BattleCombatEventKind.DamageResolved,
                     BattleCombatEventKind.UnitDefeated,
                     BattleCombatEventKind.UnitKilled,
                     BattleCombatEventKind.AttackLanded,
                     BattleCombatEventKind.BattleCompleted
                 })
            if (!kinds.Contains(required)) throw new InvalidOperationException("production event missing: " + required);
        var ordered = new[]
        {
            Array.IndexOf(kinds, BattleCombatEventKind.AttackDeclared),
            Array.IndexOf(kinds, BattleCombatEventKind.DamageResolved),
            Array.IndexOf(kinds, BattleCombatEventKind.UnitDefeated),
            Array.IndexOf(kinds, BattleCombatEventKind.UnitKilled),
            Array.IndexOf(kinds, BattleCombatEventKind.AttackLanded)
        };
        if (!ordered.SequenceEqual(ordered.OrderBy(index => index)) || ordered.Any(index => index < 0))
            throw new InvalidOperationException("production attack/death event order");
        AssertBattleScopesZero(first, "victory", BattleCombatCompletionReason.PlayerVictory);

        var repeat = new BattleSimulation(ProductionConfig(8101, 100, 1000, 10, 0));
        var repeatResult = repeat.RunToEnd();
        if (firstResult.Digest != repeatResult.Digest || firstResult.Ticks != repeatResult.Ticks ||
            firstResult.Outcome != repeatResult.Outcome)
            throw new InvalidOperationException("attribute/event migration changed same-seed digest determinism");
        if (!transition.Events.Select(EventFingerprint).SequenceEqual(
                repeat.CombatTransition!.Events.Select(EventFingerprint)))
            throw new InvalidOperationException("production typed event order is nondeterministic");
        AssertBattleScopesZero(repeat, "repeat victory", BattleCombatCompletionReason.PlayerVictory);

        VerifyProductionBindingBehavior();
        VerifyProductionSetupOrdering();
        VerifyProductionMovementBatch();
        VerifyProductionAbilityStatusBehavior();
        VerifyProductionKillAttribution();

        var featureHero = ProductionUnit("feature_hero", true, 200, 0, 1);
        var temporary = ProductionUnit("feature_token", false, 20, 1, 1);
        var featureHealer = ProductionUnit(
            "feature_healer", false, 100, 0, 3, heal: 10,
            behavior: new UnitBehaviorSnapshot(
                PeriodicShieldTicks: 1,
                PeriodicShieldAmount: 5,
                PeriodicSummonTicks: 1,
                PeriodicSummonLimit: 1));
        var featureBattle = new BattleSimulation(new BattleConfig
        {
            Seed = 8102,
            FloorRule = new ClearFloorRuleRuntime("feature", "feature", "test"),
            HeroRule = ProductionRule(),
            Spawns =
            [
                new BattleSpawn(featureHero, 0, new Vector2I(0, 0), "feature_hero", .5f),
                new BattleSpawn(featureHealer, 0, new Vector2I(0, 1), "feature_healer", BehaviorSummon: temporary),
                new BattleSpawn(ProductionUnit("feature_enemy", false, 1_000_000, 0, 1), 1,
                    new Vector2I(9, 5), "feature_enemy")
            ]
        });
        featureBattle.Step();
        var featureKinds = featureBattle.CombatEvents.Select(item => item.Kind).ToHashSet();
        foreach (var required in new[]
                 {
                     BattleCombatEventKind.HealingResolved,
                     BattleCombatEventKind.ShieldResolved,
                     BattleCombatEventKind.UnitSummoned,
                     BattleCombatEventKind.UnitMoved
                 })
            if (!featureKinds.Contains(required)) throw new InvalidOperationException("production mutation event missing: " + required);
        featureBattle.Abort();
        AssertBattleScopesZero(featureBattle, "feature abort", BattleCombatCompletionReason.Abort);

        var defeat = new BattleSimulation(ProductionConfig(8103, 10, 0, 100, 1000, enemyActsFirst: true));
        if (defeat.RunToEnd().Outcome != BattleOutcome.PlayerDefeat) throw new InvalidOperationException("defeat fixture outcome");
        AssertBattleScopesZero(defeat, "defeat", BattleCombatCompletionReason.PlayerDefeat);

        var timeout = new BattleSimulation(ProductionConfig(8104, 1_000_000_000, 0, 1_000_000_000, 0));
        if (timeout.RunToEnd().Outcome != BattleOutcome.Timeout) throw new InvalidOperationException("timeout fixture outcome");
        AssertBattleScopesZero(timeout, "timeout", BattleCombatCompletionReason.Timeout);

        var abort = new BattleSimulation(ProductionConfig(8105, 100, 1, 100, 1));
        abort.Abort();
        AssertBattleScopesZero(abort, "abort", BattleCombatCompletionReason.Abort);
        var replacement = new BattleSimulation(ProductionConfig(8106, 100, 1, 100, 1));
        replacement.Replace();
        AssertBattleScopesZero(replacement, "replacement", BattleCombatCompletionReason.Replacement);
        var disposal = new BattleSimulation(ProductionConfig(8107, 100, 1, 100, 1));
        disposal.Dispose();
        AssertBattleScopesZero(disposal, "disposal", BattleCombatCompletionReason.Disposal);
        var exception = new BattleSimulation(ProductionConfig(
            8108, 100, 1, 100, 1, floor: new ThrowingTickRule()));
        ExpectThrows(() => exception.Step(), "production exception completion");
        AssertBattleScopesZero(exception, "exception", BattleCombatCompletionReason.Exception);
    }

    private static void VerifyProductionBindingBehavior()
    {
        var observed = new List<string>();
        BattleCombatBindingRegistry? escapedRegistry = null;
        var calculationSource = CombatSourceRef.System("production_damage_calculation");
        var defeatSource = CombatSourceRef.System("production_defeat_listener");
        var attackSource = CombatSourceRef.System("production_attack_listener");
        using var battle = new BattleSimulation(ProductionConfig(
            8110,
            100,
            1000,
            10,
            0,
            configureCombatBindings: bindings =>
            {
                escapedRegistry = bindings;
                bindings.SubscribeCalculation(
                    BattleCombatCalculationKind.Damage,
                    calculationSource,
                    0,
                    (_, current) =>
                    {
                        observed.Add("calculation");
                        return current;
                    });
                bindings.Subscribe(
                    BattleCombatEventKind.UnitDefeated,
                    defeatSource,
                    0,
                    (_, reactions) =>
                    {
                        observed.Add("defeated-listener");
                        if (!reactions.Enqueue(defeatSource, 0, _ => observed.Add("defeated-reaction")))
                            throw new InvalidOperationException("production defeat reaction was rejected");
                    });
                bindings.Subscribe(
                    BattleCombatEventKind.AttackLanded,
                    attackSource,
                    0,
                    (_, reactions) =>
                    {
                        observed.Add("attack-listener");
                        if (!reactions.Enqueue(attackSource, 0, _ => observed.Add("attack-reaction")))
                            throw new InvalidOperationException("production attack reaction was rejected");
                    });
            }));
        if (escapedRegistry is null)
            throw new InvalidOperationException("production combat binding registry was not configured");
        ExpectThrows(
            () => escapedRegistry.Subscribe(BattleCombatEventKind.AttackLanded, attackSource, 0, (_, _) => { }),
            "post-construction combat binding registration");

        battle.RunToEnd();
        var expected = new[]
        {
            "calculation",
            "defeated-listener",
            "attack-listener",
            "defeated-reaction",
            "attack-reaction"
        };
        if (!observed.SequenceEqual(expected))
            throw new InvalidOperationException("production binding order: " + string.Join(',', observed));
        AssertBattleScopesZero(battle, "production bindings", BattleCombatCompletionReason.PlayerVictory);
    }

    private static void VerifyProductionSetupOrdering()
    {
        var status = new StatusDefinition
        {
            StableId = "phase1_setup_status",
            DisplayName = "阶段一开战状态",
            Behavior = StatusBehaviorKind.DisableActions,
            DurationKind = StatusDurationKind.Permanent,
            AggregationPolicy = StatusAggregationPolicy.BySource,
            StackLimit = 1,
            GrantedTags = [StatusDefinitionCompiler.ActionDisabledTag],
            Magnitude = 1
        };
        var ability = new AbilityDefinition
        {
            StableId = "phase1_setup_ability",
            DisplayName = "阶段一开战能力",
            ActivationKind = AbilityActivationKind.Automatic,
            Trigger = AbilityTriggerKind.BattleStarted,
            MaxUses = 1,
            Operations =
            [
                new ApplyStatusAbilityOperationSpec
                {
                    Status = status,
                    TargetQuery = new OwnerTargetQuerySpec()
                }
            ]
        };
        var compilation = AbilityDefinitionCompiler.CompileLoadout(
            new AbilityLoadoutDefinition { Abilities = [ability] });
        var loadout = compilation.Loadout ?? throw new InvalidOperationException(
            "production setup fixture compilation: " + string.Join(';', compilation.Report.CoreErrors));
        var hero = ProductionUnit("setup_hero", true, 100, 0, 1) with { AbilityLoadout = loadout };
        using var battle = new BattleSimulation(new BattleConfig
        {
            Seed = 8114,
            Identity = new BattleIdentity("phase1_setup", TowerNodeType.Combat, 8114, 1, 1),
            FloorRule = new FloorStartDamageRule("setup_enemy", 5),
            HeroRule = ProductionRule(),
            Modifiers = new ModifierSnapshot(SummonToken: true),
            Summons = new SummonProfiles(ItemToken: ProductionUnit("setup_token", false, 20, 0, 1)),
            Spawns =
            [
                new BattleSpawn(hero, 0, new Vector2I(0, 1), "setup_hero"),
                new BattleSpawn(ProductionUnit("setup_enemy", false, 100, 0, 1), 1,
                    new Vector2I(9, 1), "setup_enemy")
            ]
        });
        var typedKinds = battle.CombatEvents.Select(item => item.Kind).ToArray();
        if (typedKinds.Length == 0 || typedKinds[0] != BattleCombatEventKind.BattleStarted)
            throw new InvalidOperationException("typed BattleStarted was not the first production combat event");
        var setupOrder = new[]
        {
            Array.IndexOf(typedKinds, BattleCombatEventKind.UnitSummoned),
            Array.IndexOf(typedKinds, BattleCombatEventKind.DamageResolved),
            Array.IndexOf(typedKinds, BattleCombatEventKind.StatusApplied),
            Array.IndexOf(typedKinds, BattleCombatEventKind.AbilityResolved)
        };
        if (setupOrder.Any(index => index <= 0) || !setupOrder.SequenceEqual(setupOrder.OrderBy(index => index)))
            throw new InvalidOperationException("typed setup events did not follow BattleStarted: " + string.Join(',', typedKinds));
        var legacyKinds = battle.PendingEvents.Select(item => item.Type).ToArray();
        if (!legacyKinds.SequenceEqual(new[] { "summoned", "damage", "battle_started" }))
            throw new InvalidOperationException("typed setup boundary changed legacy event/digest order: " +
                                                string.Join(',', legacyKinds));
        if (battle.Units.Any(unit => unit.Attributes.ScopeId != "battle_8114_attributes"))
            throw new InvalidOperationException("production setup unit bypassed Battle-owned Attribute scope");
        battle.Abort();
        AssertBattleScopesZero(battle, "production setup ordering", BattleCombatCompletionReason.Abort);
    }

    private static void VerifyProductionMovementBatch()
    {
        var reactionFactCounts = new List<int>();
        BattleSimulation? simulation = null;
        var reactionSource = CombatSourceRef.System("movement_batch_listener");
        var config = new BattleConfig
        {
            Seed = 8115,
            Identity = new BattleIdentity("phase1_movement_batch", TowerNodeType.Combat, 8115, 1, 1),
            FloorRule = new ClearFloorRuleRuntime("movement", "movement", "test"),
            HeroRule = ProductionRule(),
            ConfigureCombatBindings = bindings => bindings.Subscribe(
                BattleCombatEventKind.UnitMoved,
                reactionSource,
                0,
                (_, reactions) =>
                {
                    if (!reactions.Enqueue(reactionSource, 0, _ =>
                        {
                            var active = simulation ?? throw new InvalidOperationException("movement fixture missing simulation");
                            reactionFactCounts.Add(active.CombatEvents.Count(item =>
                                item.Kind == BattleCombatEventKind.UnitMoved && item.Tick == 1));
                        }))
                        throw new InvalidOperationException("movement batch reaction enqueue rejected");
                }),
            Spawns =
            [
                new BattleSpawn(ProductionUnit("move_hero", true, 1000, 0, 1), 0,
                    new Vector2I(0, 1), "a_move_hero"),
                new BattleSpawn(ProductionUnit("move_ally", false, 1000, 0, 1), 0,
                    new Vector2I(0, 4), "b_move_ally"),
                new BattleSpawn(ProductionUnit("move_enemy_a", false, 1000, 0, 1), 1,
                    new Vector2I(9, 1), "y_move_enemy"),
                new BattleSpawn(ProductionUnit("move_enemy_b", false, 1000, 0, 1), 1,
                    new Vector2I(9, 4), "z_move_enemy")
            ]
        };
        simulation = new BattleSimulation(config);
        using (simulation)
        {
            simulation.Step();
            var movedFacts = simulation.CombatEvents.Count(item =>
                item.Kind == BattleCombatEventKind.UnitMoved && item.Tick == 1);
            if (movedFacts < 2 || reactionFactCounts.Count != movedFacts ||
                reactionFactCounts.Any(count => count != movedFacts))
                throw new InvalidOperationException(
                    $"movement reactions observed a partial fact batch: facts={movedFacts}, " +
                    $"observed={string.Join(',', reactionFactCounts)}");
            if (simulation.Units.Any(unit => unit.Attributes.ScopeId != "battle_8115_attributes"))
                throw new InvalidOperationException("production movement unit bypassed Battle-owned Attribute scope");
            simulation.Abort();
            AssertBattleScopesZero(simulation, "production movement batch", BattleCombatCompletionReason.Abort);
        }
    }

    private static void VerifyProductionAbilityStatusBehavior()
    {
        var status = new StatusDefinition
        {
            StableId = "phase1_timed_stack",
            DisplayName = "阶段一限时叠层",
            Behavior = StatusBehaviorKind.DisableActions,
            DurationKind = StatusDurationKind.TimedTicks,
            DurationTicks = 2,
            AggregationPolicy = StatusAggregationPolicy.BySource,
            StackLimit = 2,
            DurationRefreshPolicy = StatusDurationRefreshPolicy.KeepLonger,
            DispelCategory = StatusDispelCategory.Ordinary,
            GrantedTags = [StatusDefinitionCompiler.ActionDisabledTag],
            Magnitude = 1
        };
        var ability = new AbilityDefinition
        {
            StableId = "phase1_periodic_status",
            DisplayName = "阶段一周期状态",
            ActivationKind = AbilityActivationKind.Automatic,
            Trigger = AbilityTriggerKind.PeriodicTick,
            IntervalTicks = 1,
            MaxUses = 2,
            Operations =
            [
                new ApplyStatusAbilityOperationSpec
                {
                    Status = status,
                    TargetQuery = new OwnerTargetQuerySpec()
                }
            ]
        };
        var compilation = AbilityDefinitionCompiler.CompileLoadout(
            new AbilityLoadoutDefinition { Abilities = [ability] });
        var loadout = compilation.Loadout ?? throw new InvalidOperationException(
            "production ability/status fixture compilation: " + string.Join(';', compilation.Report.CoreErrors));
        var identity = new BattleIdentity("phase1_ability_status", TowerNodeType.Combat, 8111, 1, 1);
        var owner = ProductionUnit("status_owner", true, 1_000_000, 0, 1) with { AbilityLoadout = loadout };
        using var battle = new BattleSimulation(new BattleConfig
        {
            Seed = 8111,
            Identity = identity,
            FloorRule = new ClearFloorRuleRuntime("status", "status", "test"),
            HeroRule = ProductionRule(),
            Spawns =
            [
                new BattleSpawn(owner, 0, new Vector2I(0, 0), "status_owner"),
                new BattleSpawn(ProductionUnit("status_enemy", false, 1_000_000, 0, 1), 1,
                    new Vector2I(9, 5), "status_enemy")
            ]
        });
        battle.Step();
        battle.Step();
        battle.Step();

        var actual = battle.CombatEvents
            .Where(item => item.SubjectStableId == status.StableId || item.SubjectStableId == ability.StableId)
            .Select(item => (item.Kind, item.Tick, item.PreviousStacks, item.CurrentStacks, item.Reason))
            .ToArray();
        var expected = new[]
        {
            (BattleCombatEventKind.StatusApplied, 1, 0, 1, string.Empty),
            (BattleCombatEventKind.AbilityResolved, 1, 0, 0, string.Empty),
            (BattleCombatEventKind.StatusApplied, 2, 1, 2, string.Empty),
            (BattleCombatEventKind.StatusStackChanged, 2, 1, 2, string.Empty),
            (BattleCombatEventKind.AbilityResolved, 2, 0, 0, string.Empty),
            (BattleCombatEventKind.StatusRemoved, 3, 2, 0, StatusRemovalReason.Expired.ToString())
        };
        if (!actual.SequenceEqual(expected))
            throw new InvalidOperationException("production ability/status events: " + string.Join('|', actual));
        if (battle.CombatEvents.Any(item => item.Identity != identity))
            throw new InvalidOperationException("production ability/status event lost Battle identity");

        battle.Abort();
        AssertBattleScopesZero(battle, "production ability/status", BattleCombatCompletionReason.Abort);
        if (battle.AbilityTransition is not { RemainingRuntimeInstances: 0 } ||
            battle.StatusTransition is not { RemainingInstances: 0 })
            throw new InvalidOperationException("production ability/status completion retained runtime state");
    }

    private static void VerifyProductionKillAttribution()
    {
        using (var credited = new BattleSimulation(ProductionConfig(
                   8112,
                   100,
                   0,
                   10,
                   0,
                   floor: new OneShotLethalFloorRule("a_player", "z_enemy", 1000))))
        {
            credited.RunToEnd();
            var killed = credited.CombatTransition!.Events.Single(item => item.Kind == BattleCombatEventKind.UnitKilled);
            if (killed.SourceRuntimeId != "a_player" || killed.Source.Kind != CombatSourceKind.Unit)
                throw new InvalidOperationException("runtime unit compatibility damage lost kill attribution");
            AssertBattleScopesZero(credited, "credited compatibility kill", BattleCombatCompletionReason.PlayerVictory);
        }

        using var hazard = new BattleSimulation(ProductionConfig(
            8113,
            100,
            0,
            10,
            0,
            floor: new OneShotLethalFloorRule("hazard", "z_enemy", 1000)));
        hazard.RunToEnd();
        var hazardEvents = hazard.CombatTransition!.Events;
        if (hazardEvents.Count(item => item.Kind == BattleCombatEventKind.UnitDefeated) != 1 ||
            hazardEvents.Any(item => item.Kind == BattleCombatEventKind.UnitKilled))
            throw new InvalidOperationException("unknown system damage produced credited kill semantics");
        var defeated = hazardEvents.Single(item => item.Kind == BattleCombatEventKind.UnitDefeated);
        if (defeated.SourceRuntimeId != "hazard" || defeated.Source.Kind != CombatSourceKind.System)
            throw new InvalidOperationException("system defeat source attribution changed");
        AssertBattleScopesZero(hazard, "uncredited compatibility defeat", BattleCombatCompletionReason.PlayerVictory);
    }

    private static BattleConfig ProductionConfig(
        ulong seed,
        float playerHealth,
        float playerDamage,
        float enemyHealth,
        float enemyDamage,
        bool enemyActsFirst = false,
        IBattleFloorRuleRuntime? floor = null,
        Action<BattleCombatBindingRegistry>? configureCombatBindings = null)
    {
        var playerId = enemyActsFirst ? "z_player" : "a_player";
        var enemyId = enemyActsFirst ? "a_enemy" : "z_enemy";
        return new BattleConfig
        {
            Seed = seed,
            Identity = new BattleIdentity("phase1_contract", TowerNodeType.Combat, seed, 1, 1),
            FloorRule = floor ?? new ClearFloorRuleRuntime("production", "production", "test"),
            HeroRule = ProductionRule(),
            ConfigureCombatBindings = configureCombatBindings,
            Spawns =
            [
                new BattleSpawn(ProductionUnit(playerId, true, playerHealth, playerDamage, 1), 0,
                    new Vector2I(1, 1), playerId),
                new BattleSpawn(ProductionUnit(enemyId, false, enemyHealth, enemyDamage, 1), 1,
                    new Vector2I(2, 1), enemyId)
            ]
        };
    }

    private static UnitSnapshot ProductionUnit(
        string id,
        bool hero,
        float health,
        float damage,
        float range,
        float heal = 0,
        UnitBehaviorSnapshot? behavior = null) =>
        new(id, id, UnitRole.Fighter, hero, false, health, damage, range, 1, 1,
            0, heal, 0, 0, Array.Empty<string>(), behavior ?? new UnitBehaviorSnapshot());

    private static HeroRuleSnapshot ProductionRule() => new(
        1, 1, 1, 0, 0, 0, false,
        string.Empty, 1, 1, 0, 0, 0, 0,
        false, false, 0, 0, string.Empty);

    private static TacticalCommandBattlePreparation TacticalPreparation(
        CompiledAbilityLoadout loadout,
        string abilityId,
        int tacticalPointCost = 1)
    {
        var ability = loadout.Find(abilityId) ??
            throw new InvalidOperationException("tactical fixture ability missing: " + abilityId);
        var primary = new CompiledTacticalCommandDefinition(
            "tactical_fixture_" + abilityId,
            string.Empty,
            ability.DisplayName,
            ability.Description,
            tacticalPointCost,
            ability,
            "fixture_" + abilityId);
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

    private static string EventFingerprint(BattleCombatEvent item) =>
        $"{item.Sequence}:{item.Identity}:{item.Tick}:{item.Kind}:{item.Source}:{item.SourceRuntimeId}:{item.TargetRuntimeId}:" +
        $"{item.RequestedValue:R}:{item.AppliedValue:R}:{item.EffectiveValue:R}:{item.Cell.X},{item.Cell.Y}:" +
        $"{item.SubjectStableId}:{item.PreviousStacks}:{item.CurrentStacks}:{item.Reason}";

    private static void AssertBattleScopesZero(
        BattleSimulation battle,
        string label,
        BattleCombatCompletionReason expectedReason)
    {
        var combat = battle.CombatTransition ?? throw new InvalidOperationException(label + " combat transition missing");
        var attributes = battle.AttributeTransition ?? throw new InvalidOperationException(label + " attribute transition missing");
        if (combat.Reason != expectedReason)
            throw new InvalidOperationException($"{label} combat completion: expected {expectedReason}, got {combat.Reason}");
        if (combat.RemainingSubscriptions != 0 || combat.RemainingReactions != 0 || combat.RemainingRuntimeEntries != 0 ||
            attributes.RemainingSets != 0 || attributes.RemainingModifiers != 0)
            throw new InvalidOperationException(label + " retained Battle-scoped attribute/event state");
    }

    private sealed class OneShotLethalFloorRule(string sourceRuntimeId, string targetRuntimeId, float amount)
        : ClearFloorRuleRuntime("lethal", "lethal", "test")
    {
        private bool _applied;

        public override void OnTick(BattleRuleContext context)
        {
            if (_applied) return;
            _applied = true;
            context.Damage(
                sourceRuntimeId,
                context.Units.Single(unit => unit.RuntimeId == targetRuntimeId),
                amount);
        }
    }

    private sealed class FloorStartDamageRule(string targetRuntimeId, float amount)
        : ClearFloorRuleRuntime("floor_start", "floor_start", "test")
    {
        public override void OnBattleStarted(BattleRuleContext context) => context.Damage(
            "setup_floor",
            context.Units.Single(unit => unit.RuntimeId == targetRuntimeId),
            amount);
    }

    private sealed class ThrowingTickRule() : ClearFloorRuleRuntime("throw", "throw", "test")
    {
        public override void OnTick(BattleRuleContext context) => throw new InvalidOperationException("expected tick failure");
    }

    private sealed class SubscriptionHandleEffectWorld : IEffectRuntimeWorld
    {
        private readonly Dictionary<string, int> _commitCounts = new(StringComparer.Ordinal);

        public int CommitCount(string bindingId) => _commitCounts.GetValueOrDefault(bindingId);

        public EffectWorldSnapshot CaptureSnapshot(int tick) => EffectWorldSnapshot.Create(
            tick,
            [
                new EffectEntitySnapshot("source", 0, true, 100, 100, 0),
                new EffectEntitySnapshot("target", 1, true, 100, 100, 0)
            ]);

        public EffectModifierResult ResolveModifiers(
            EffectModifierRequest request,
            EffectWorldSnapshot snapshot) => EffectModifierResult.Identity(request.RequestedAmount);

        public EffectCommitOutcome Commit(PreparedEffectMutation mutation)
        {
            _commitCounts[mutation.Request.BindingId] = CommitCount(mutation.Request.BindingId) + 1;
            return EffectCommitOutcome.Succeeded(
                mutation.Modifiers.ResolvedAmount,
                mutation.Modifiers.ResolvedAmount,
                mutation.Request.Kind switch
                {
                    EffectKind.Damage => EffectDomainEventKind.DamageResolved,
                    EffectKind.Heal => EffectDomainEventKind.HealingResolved,
                    EffectKind.Shield => EffectDomainEventKind.ShieldResolved,
                    _ => EffectDomainEventKind.None
                });
        }
    }

    private static AttributeDefinitionSpec Attribute(CombatAttribute attribute, float value, float minimum, float maximum) =>
        new() { Attribute = attribute, BaseValue = value, Minimum = minimum, Maximum = maximum };

    private static CompiledAttributeModifier Constant(
        CombatAttribute attribute,
        AttributeModifierOperation operation,
        float value,
        string slot = "default") =>
        new(attribute, operation, new CompiledConstantMagnitude(value), 0, slot);

    private static CompiledAttributeModifier CompileModifier(
        CombatAttribute attribute,
        AttributeModifierOperation operation,
        AttributeMagnitudeSpec magnitude)
    {
        var result = AttributeDefinitionCompiler.Compile(new AttributeModifierSpec
        {
            Attribute = attribute,
            Operation = operation,
            Magnitude = magnitude,
            SlotId = "default"
        });
        return result.Modifier ?? throw new InvalidOperationException("modifier compilation: " + string.Join(';', result.Report.CoreErrors));
    }

    private static void AssertPipelineZero(
        BattleCombatEventPipeline pipeline,
        BattleCombatTransitionResult transition,
        string label)
    {
        if (transition.RemainingSubscriptions != 0 || transition.RemainingReactions != 0 ||
            transition.RemainingRuntimeEntries != 0 || pipeline.SubscriptionCount != 0 || pipeline.PendingReactionCount != 0)
            throw new InvalidOperationException(label + " completion retained combat runtime state");
    }

    private static void Near(float actual, float expected, string label)
    {
        if (Math.Abs(actual - expected) > .001f)
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    }

    private static void ExpectThrows(Action action, string label)
    {
        try { action(); }
        catch { return; }
        throw new InvalidOperationException(label + " was accepted");
    }
}
